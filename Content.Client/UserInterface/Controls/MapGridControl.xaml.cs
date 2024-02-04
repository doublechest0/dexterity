using System.Numerics;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Handles generic grid-drawing data, with zoom and dragging.
/// </summary>
[GenerateTypedNameReferences]
[Virtual]
public partial class MapGridControl : BoxContainer
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    /* Dragging */
    protected virtual bool Draggable { get; } = false;

    /// <summary>
    /// Control offset from whatever is being tracked.
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// If the control is being recentered what is the target offset to reach.
    /// </summary>
    public Vector2 TargetOffset;

    private bool _draggin;
    protected Vector2 StartDragPosition;
    protected bool Recentering;

    protected const float ScrollSensitivity = 8f;

    protected float RecenterMinimum = 0.05f;

    /// <summary>
    /// UI pixel radius.
    /// </summary>
    public const int UIDisplayRadius = 320;
    protected const int MinimapMargin = 4;

    protected float WorldMinRange;
    protected float WorldMaxRange;
    public float WorldRange;
    public Vector2 WorldRangeVector => new Vector2(WorldRange, WorldRange);

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    protected float ActualRadarRange;

    protected float CornerRadarRange => MathF.Sqrt(ActualRadarRange * ActualRadarRange + ActualRadarRange * ActualRadarRange);

    /// <summary>
    /// Controls the maximum distance that will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;

    public Vector2 MaxRadarRangeVector => new Vector2(MaxRadarRange, MaxRadarRange);

    protected Vector2 MidPointVector => new Vector2(MidPoint, MidPoint);

    protected int MidPoint => SizeFull / 2;
    protected int SizeFull => (int) ((UIDisplayRadius + MinimapMargin) * 2 * UIScale);
    protected int ScaledMinimapRadius => (int) (UIDisplayRadius * UIScale);
    protected float MinimapScale => WorldRange != 0 ? ScaledMinimapRadius / WorldRange : 0f;

    public event Action<float>? WorldRangeChanged;

    public MapGridControl() : this(32f, 32f, 32f) {}

    public MapGridControl(float minRange, float maxRange, float range)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        SetSize = new Vector2(SizeFull, SizeFull);
        RectClipContent = true;
        MouseFilter = MouseFilterMode.Stop;
        ActualRadarRange = WorldRange;
        WorldMinRange = minRange;
        WorldMaxRange = maxRange;
        WorldRange = range;
        ActualRadarRange = range;
    }

    public void ForceRecenter()
    {
        Recentering = true;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!Draggable)
            return;

        if (args.Function == EngineKeyFunctions.Use)
        {
            StartDragPosition = args.PointerLocation.Position;
            _draggin = true;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (!Draggable)
            return;

        if (args.Function == EngineKeyFunctions.Use)
            _draggin = false;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_draggin)
            return;

        Recentering = false;
        Offset -= new Vector2(args.Relative.X, -args.Relative.Y) / MidPoint * WorldRange;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        AddRadarRange(-args.Delta.Y * 1f / ScrollSensitivity * ActualRadarRange);
    }

    public void AddRadarRange(float value)
    {
        ActualRadarRange = Math.Clamp(ActualRadarRange + value, WorldMinRange, WorldMaxRange);
    }

    /// <summary>
    /// Converts map coordinates to the local control.
    /// </summary>
    protected Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPointVector;
    }

    /// <summary>
    /// Converts local coordinates on the control to map coordinates.
    /// </summary>
    protected Vector2 InverseMapPosition(Vector2 value)
    {
        var inversePos = (value - MidPointVector) / MinimapScale;

        inversePos = inversePos with { Y = -inversePos.Y };
        inversePos = Matrix3.CreateTransform(Offset, Angle.Zero).Transform(inversePos);
        return inversePos;
    }

    /// <summary>
    /// Handles re-centering the control's offset.
    /// </summary>
    /// <returns></returns>
    public bool DrawRecenter()
    {
        // Map re-centering
        if (Recentering)
        {
            var frameTime = Timing.FrameTime;
            var diff = (TargetOffset - Offset) * (float) frameTime.TotalSeconds;

            if (Offset.LengthSquared() < RecenterMinimum)
            {
                Offset = TargetOffset;
                Recentering = false;
            }
            else
            {
                Offset += diff * 5f;
                return false;
            }
        }

        return Offset == TargetOffset;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (!ActualRadarRange.Equals(WorldRange))
        {
            var diff = ActualRadarRange - WorldRange;
            const float lerpRate = 10f;

            WorldRange += (float) Math.Clamp(diff, -lerpRate * MathF.Abs(diff) * Timing.FrameTime.TotalSeconds, lerpRate * MathF.Abs(diff) * Timing.FrameTime.TotalSeconds);
            WorldRangeChanged?.Invoke(WorldRange);
        }
    }
}
