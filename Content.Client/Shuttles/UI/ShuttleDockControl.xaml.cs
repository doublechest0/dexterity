using System.Numerics;
using Content.Client.Shuttles.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using FastAccessors;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class ShuttleDockControl : BaseShuttleControl
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly DockingSystem _dockSystem;
    private readonly SharedShuttleSystem _shuttles;
    private readonly SharedTransformSystem _xformSystem;

    public NetEntity? HighlightedDock;

    public NetEntity? ViewedDock => _viewedState?.Entity;
    private DockingPortState? _viewedState;

    public EntityUid? GridEntity;

    private EntityCoordinates? _coordinates;
    private Angle? _angle;

    public DockingInterfaceState? DockState = null;

    private List<Entity<MapGridComponent>> _grids = new();

    private readonly HashSet<DockingPortState> _drawnDocks = new();
    private readonly Dictionary<DockingPortState, Button> _dockButtons = new();

    /// <summary>
    /// Store buttons for every other dock
    /// </summary>
    private readonly Dictionary<DockingPortState, PanelContainer> _dockContainers = new();

    public event Action<NetEntity>? OnViewDock;

    public ShuttleDockControl() : base(2f, 32f, 8f)
    {
        RobustXamlLoader.Load(this);
        _dockSystem = EntManager.System<DockingSystem>();
        _shuttles = EntManager.System<SharedShuttleSystem>();
        _xformSystem = EntManager.System<SharedTransformSystem>();
        MinSize = new Vector2(SizeFull, SizeFull);
    }

    public void SetViewedDock(DockingPortState? dockState)
    {
        _viewedState = dockState;

        if (dockState != null)
        {
            _coordinates = EntManager.GetCoordinates(dockState.Coordinates);
            _angle = dockState.Angle;
            OnViewDock?.Invoke(dockState.Entity);
        }
        else
        {
            _coordinates = null;
            _angle = null;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        _drawnDocks.Clear();
        DrawBacking(handle);

        if (_coordinates == null ||
            _angle == null ||
            DockState == null ||
            !EntManager.TryGetComponent<TransformComponent>(GridEntity, out var gridXform))
        {
            DrawNoSignal(handle);
            HideDocks();
            return;
        }

        DrawCircles(handle);
        var gridNent = EntManager.GetNetEntity(GridEntity);
        var mapPos = _xformSystem.ToMapCoordinates(_coordinates.Value);
        var ourGridMatrix = _xformSystem.GetWorldMatrix(gridXform.Owner);
        var dockMatrix = Matrix3.CreateTransform(_coordinates.Value.Position, Angle.Zero);
        Matrix3.Multiply(dockMatrix, ourGridMatrix, out var offsetMatrix);

        offsetMatrix = offsetMatrix.Invert();

        // Draw nearby grids
        var boundsEnlargement = (Vector2.One * MinimapScale).Floored();

        var controlBounds = UIBox2i.FromDimensions(PixelPosition - boundsEnlargement, PixelSize + boundsEnlargement * 2);
        _grids.Clear();
        _mapManager.FindGridsIntersecting(gridXform.MapID, new Box2(mapPos.Position - WorldRangeVector, mapPos.Position + WorldRangeVector), ref _grids);

        // offset the dotted-line position to the bounds.
        Vector2? viewedDockPos = _viewedState != null ? MidPointVector : null;

        if (viewedDockPos != null)
        {
            viewedDockPos = viewedDockPos.Value + _angle.Value.RotateVec(new Vector2(0f,-0.6f) * MinimapScale);
        }

        var lineOffset = (float) _timing.RealTime.TotalSeconds * 30f;

        foreach (var grid in _grids)
        {
            EntManager.TryGetComponent(grid.Owner, out IFFComponent? iffComp);

            if (grid.Owner != GridEntity && !_shuttles.CanDraw(grid.Owner, iffComp: iffComp))
                continue;

            var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
            var color = _shuttles.GetIFFColor(grid.Owner, grid.Owner == GridEntity, component: iffComp);

            DrawGrid(handle, matty, grid, color);

            // Draw any docks on that grid
            if (!DockState.Docks.TryGetValue(EntManager.GetNetEntity(grid), out var gridDocks))
                continue;

            foreach (var dock in gridDocks)
            {
                if (ViewedDock == dock.Entity)
                    continue;

                var position = matty.Transform(dock.Coordinates.Position);

                var otherDockRotation = Matrix3.CreateRotation(dock.Angle);
                var scaledPos = ScalePosition(position with {Y = -position.Y});

                if (!controlBounds.Contains(scaledPos.Floored()))
                    continue;

                // Draw the dock's collision
                var collisionBL = matty.Transform(dock.Coordinates.Position +
                                                  otherDockRotation.Transform(new Vector2(-0.2f, -0.7f)));
                var collisionBR = matty.Transform(dock.Coordinates.Position +
                                                  otherDockRotation.Transform(new Vector2(0.2f, -0.7f)));
                var collisionTR = matty.Transform(dock.Coordinates.Position +
                                                  otherDockRotation.Transform(new Vector2(0.2f, -0.5f)));
                var collisionTL = matty.Transform(dock.Coordinates.Position +
                                                  otherDockRotation.Transform(new Vector2(-0.2f, -0.5f)));

                var verts = new[]
                {
                    collisionBL,
                    collisionBR,
                    collisionBR,
                    collisionTR,
                    collisionTR,
                    collisionTL,
                    collisionTL,
                    collisionBL,
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                var collisionCenter = verts[0] + verts[1] + verts[3] + verts[5];

                var otherDockConnection = Color.ToSrgb(Color.Pink);
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, otherDockConnection.WithAlpha(0.2f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, verts, otherDockConnection);

                // Draw the dock itself
                var dockBL = matty.Transform(dock.Coordinates.Position + new Vector2(-0.5f, -0.5f));
                var dockBR = matty.Transform(dock.Coordinates.Position + new Vector2(0.5f, -0.5f));
                var dockTR = matty.Transform(dock.Coordinates.Position + new Vector2(0.5f, 0.5f));
                var dockTL = matty.Transform(dock.Coordinates.Position + new Vector2(-0.5f, 0.5f));

                verts = new[]
                {
                    dockBL,
                    dockBR,
                    dockBR,
                    dockTR,
                    dockTR,
                    dockTL,
                    dockTL,
                    dockBL
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                Color otherDockColor;

                if (HighlightedDock == dock.Entity)
                {
                    otherDockColor = Color.ToSrgb(Color.Magenta);
                }
                else
                {
                    otherDockColor = Color.ToSrgb(Color.Purple);
                }

                // If the dock is in range then also do highlighting
                if (viewedDockPos != null && dock.Coordinates.NetEntity != gridNent)
                {
                    collisionCenter /= 4;
                    var range = viewedDockPos.Value - collisionCenter;
                    var dockButton = _dockButtons[dock];

                    if (range.Length() < SharedDockingSystem.DockingHiglightRange * MinimapScale)
                    {
                        var canDock = _dockSystem.CanDock(
                            _viewedState!.Coordinates, _viewedState.Angle,
                            dock.Coordinates, dock.Angle);

                        dockButton.Disabled = !canDock;

                        var lineColor = canDock ? Color.Lime : Color.Red;
                        handle.DrawDottedLine(viewedDockPos.Value, collisionCenter, lineColor, offset: lineOffset);
                    }
                    else
                    {
                        dockButton.Disabled = true;
                    }
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, otherDockColor.WithAlpha(0.2f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, verts, otherDockColor);

                // Position the dock control above it
                var container = _dockContainers[dock];
                container.Visible = true;

                var containerPos = scaledPos - container.Size / 2 - new Vector2(0f, 0.5f) * MinimapScale;

                LayoutContainer.SetPosition(container, containerPos);
                _drawnDocks.Add(dock);
            }
        }

        // TODO: Need to do the dotted line to nearby docks
        // TODO: If dock in right range and angle then change colour to green and undisable the uhh button.
        // TODO: Set dock position for each dock in range, if not drawn then set visible false.

        // Draw the dock's collision
        var invertedPosition = Vector2.Zero;
        invertedPosition.Y = -invertedPosition.Y;
        var rotation = Matrix3.CreateRotation(-_angle.Value + MathF.PI);
        var ourDockConnection = new UIBox2(
            ScalePosition(rotation.Transform(new Vector2(-0.2f, -0.7f))),
            ScalePosition(rotation.Transform(new Vector2(0.2f, -0.5f))));

        var ourDock = new UIBox2(
            ScalePosition(rotation.Transform(new Vector2(-0.5f, 0.5f))),
            ScalePosition(rotation.Transform(new Vector2(0.5f, -0.5f))));

        var dockColor = Color.Magenta;
        var connectionColor = Color.Pink;

        handle.DrawRect(ourDockConnection, connectionColor.WithAlpha(0.2f));
        handle.DrawRect(ourDockConnection, connectionColor, filled: false);

        // Draw the dock itself
        handle.DrawRect(ourDock, dockColor.WithAlpha(0.2f));
        handle.DrawRect(ourDock, dockColor, filled: false);

        HideDocks();
    }

    private void HideDocks()
    {
        foreach (var (dock, control) in _dockContainers)
        {
            if (_drawnDocks.Contains(dock))
                continue;

            control.Visible = false;
        }
    }

    public void BuildDocks(EntityUid? shuttle)
    {
        foreach (var btn in _dockButtons.Values)
        {
            btn.Dispose();
        }

        foreach (var container in _dockContainers.Values)
        {
            container.Dispose();
        }

        _dockButtons.Clear();
        _dockContainers.Clear();

        if (DockState == null)
            return;

        var gridNent = EntManager.GetNetEntity(GridEntity);

        foreach (var (otherShuttle, docks) in DockState.Docks)
        {
            // If it's our shuttle we add a view button

            foreach (var dock in docks)
            {
                var container = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Vertical,
                };

                var panel = new PanelContainer()
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    PanelOverride = new StyleBoxFlat()
                    {
                        BorderColor = Color.Orange,
                        BorderThickness = new Thickness(1),
                        BackgroundColor = Color.Orange.WithAlpha(0.05f),
                    },
                    Children =
                    {
                        container,
                    }
                };

                Button button;

                if (otherShuttle == gridNent)
                {
                    button = new Button()
                    {
                        Text = Loc.GetString("shuttle-console-view"),
                        Margin = new Thickness(2),
                    };

                    button.OnPressed += args =>
                    {
                        SetViewedDock(dock);
                    };
                }
                else
                {
                    if (dock.Connected)
                    {
                        button = new Button()
                        {
                            Text = Loc.GetString("shuttle-console-undock-button"),
                            Margin = new Thickness(2),
                        };

                        button.OnPressed += args =>
                        {

                        };
                    }
                    else
                    {
                        button = new Button()
                        {
                            Text = Loc.GetString("shuttle-console-dock-button"),
                            Margin = new Thickness(2),
                            Disabled = true,
                        };

                        /*
                         *   - Add dock button functionality
                             - Maybe make the leading line green if the angle is good and speed based on distance
                             - Make the line segments longer too I think
                             - Once all that done just need to nuke docking fixtures and shiit, also autodock
                             - Also need to make sure panel works, then we do TODO cleanup and make the other UIs diegetic again
                             - Add dock settings back in for drawing panels
                         */

                        button.OnPressed += args =>
                        {

                        };

                        _dockButtons.Add(dock, button);
                    }
                }

                container.AddChild(new Label()
                {
                    Text = dock.Name,
                    HorizontalAlignment = HAlignment.Center,
                });

                button.HorizontalAlignment = HAlignment.Center;
                container.AddChild(button);
                UserInterfaceManager.RootControl.AddChild(panel);
                _dockContainers[dock] = panel;
            }
        }
    }
}
