using Content.Client.Pinpointer.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Power;

[GenerateTypedNameReferences]
public sealed partial class PowerMonitoringWindow : FancyWindow
{
    private readonly IEntityManager _entManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly IGameTiming _gameTiming;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    private EntityUid? _trackedEntity;
    private float? _nextScrollValue;

    public event Action<NetEntity?>? RequestPowerMonitoringUpdateAction;

    public PowerMonitoringWindow(PowerMonitoringConsoleBoundUserInterface userInterface, EntityUid? owner)
    {
        RobustXamlLoader.Load(this);
        var cache = IoCManager.Resolve<IResourceCache>();
        _entManager = IoCManager.Resolve<IEntityManager>();
        _gameTiming = IoCManager.Resolve<IGameTiming>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        // Get grid uid
        if (_entManager.TryGetComponent<TransformComponent>(owner, out var xform))
            NavMap.MapUid = xform.GridUid;

        else
            NavMap.Visible = false;

        // Set UI tab titles
        MasterTabContainer.SetTabTitle(0, Loc.GetString("power-monitoring-window-label-sources"));
        MasterTabContainer.SetTabTitle(1, Loc.GetString("power-monitoring-window-label-smes"));
        MasterTabContainer.SetTabTitle(2, Loc.GetString("power-monitoring-window-label-substation"));
        MasterTabContainer.SetTabTitle(3, Loc.GetString("power-monitoring-window-label-apc"));

        // Set UI toggles
        ShowHVCable.OnToggled += _ => OnShowCableToggled(NavMapLineGroup.HighVoltage);
        ShowMVCable.OnToggled += _ => OnShowCableToggled(NavMapLineGroup.MediumVoltage);
        ShowLVCable.OnToggled += _ => OnShowCableToggled(NavMapLineGroup.Apc);

        // Set colors
        NavMap.TileColor = PowerMonitoringHelper.TileColor;
        NavMap.WallColor = PowerMonitoringHelper.WallColor;

        // Set power monitoring update request action
        RequestPowerMonitoringUpdateAction += userInterface.RequestPowerMonitoringUpdate;

        // Recenter map
        NavMap.ForceRecenter();
    }

    private void OnShowCableToggled(NavMapLineGroup lineGroup)
    {
        if (NavMap.HiddenLineGroups.Contains(lineGroup))
            NavMap.HiddenLineGroups.Remove(lineGroup);
        else
            NavMap.HiddenLineGroups.Add(lineGroup);
    }

    public void ShowEntites
        (double totalSources,
        double totalBatteryUsage,
        double totalLoads,
        PowerMonitoringConsoleEntry[] allEntries,
        PowerMonitoringConsoleEntry[] focusSources,
        PowerMonitoringConsoleEntry[] focusLoads,
        Dictionary<Vector2i, NavMapChunkPowerCables> powerCableChunks,
        Dictionary<Vector2i, NavMapChunkPowerCables>? focusCableChunks,
        PowerMonitoringFlags flags,
        EntityCoordinates? monitorCoords)
    {
        if (!_entManager.TryGetComponent<MapGridComponent>(NavMap.MapUid, out var grid))
            return;

        // Reset nav map values
        NavMap.TrackedCoordinates.Clear();
        NavMap.TrackedEntities.Clear();
        NavMap.FocusCableNetwork = null;

        // Determine what color scheme to use
        bool useDarkColors = focusSources.Any() || focusLoads.Any();

        // Update nav map power cable networks
        NavMap.PowerCableNetwork = NavMap.GetDecodedPowerCableChunks(powerCableChunks, grid, useDarkColors);

        if (focusCableChunks != null)
            NavMap.FocusCableNetwork = NavMap.GetDecodedPowerCableChunks(focusCableChunks, grid);

        // Draw all entities on the map
        foreach (var entry in allEntries)
        {
            if (entry.Coordinates == null)
                continue;

            AddTrackedEntityToNavMap(_entManager.GetEntity(entry.NetEntity), _entManager.GetCoordinates(entry.Coordinates.Value), entry, useDarkColors);
        }

        // Draw the sources for the focused device
        foreach (var source in focusSources)
        {
            if (source.Coordinates == null)
                continue;

            AddTrackedEntityToNavMap(_entManager.GetEntity(source.NetEntity), _entManager.GetCoordinates(source.Coordinates.Value), source);
        }

        // Draw the loads for the focused device
        foreach (var load in focusLoads)
        {
            if (load.Coordinates == null)
                continue;

            AddTrackedEntityToNavMap(_entManager.GetEntity(load.NetEntity), _entManager.GetCoordinates(load.Coordinates.Value), load);
        }

        // Show monitor location
        var monitor = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(PowerMonitoringHelper.CircleIconPath)));

        if (monitorCoords != null)
            NavMap.TrackedEntities.Add(monitorCoords.Value, (true, Color.Cyan, monitor));

        // Update power status text
        TotalSources.Text = Loc.GetString("power-monitoring-window-value", ("value", totalSources));
        TotalBatteryUsage.Text = Loc.GetString("power-monitoring-window-value", ("value", totalBatteryUsage));
        TotalLoads.Text = Loc.GetString("power-monitoring-window-value", ("value", totalLoads));

        // 10+% of station power is being drawn from batteries
        TotalBatteryUsage.FontColorOverride = (totalSources * 0.1111f) < totalBatteryUsage ? new Color(180, 0, 0) : Color.White;

        // Station generator and battery output is less than the current demand
        TotalLoads.FontColorOverride = (totalSources + totalBatteryUsage) < totalLoads &&
            !MathHelper.CloseToPercent(totalSources + totalBatteryUsage, totalLoads, 0.1f) ? new Color(180, 0, 0) : Color.White;

        // Update lists
        var generatorList = allEntries.Where(x => x.Group == PowerMonitoringConsoleGroup.Generator);
        UpdateAllConsoleEntries(SourcesList, generatorList.ToArray(), null, focusLoads);

        var smesList = allEntries.Where(x => x.Group == PowerMonitoringConsoleGroup.SMES);
        UpdateAllConsoleEntries(SMESList, smesList.ToArray(), focusSources, focusLoads);

        var substationList = allEntries.Where(x => x.Group == PowerMonitoringConsoleGroup.Substation);
        UpdateAllConsoleEntries(SubstationList, substationList.ToArray(), focusSources, focusLoads);

        var apcList = allEntries.Where(x => x.Group == PowerMonitoringConsoleGroup.APC);
        UpdateAllConsoleEntries(ApcList, apcList.ToArray(), focusSources, null);

        // Update system warnings
        UpdateWarningLabel(flags);
    }

    private void AddTrackedEntityToNavMap(EntityUid uid, EntityCoordinates coords, PowerMonitoringConsoleEntry entry, bool useDarkColors = false)
    {
        if (!NavMap.Visible)
            return;

        var colorMap = useDarkColors ? PowerMonitoringHelper.DarkPowerIconColors : PowerMonitoringHelper.PowerIconColors;
        var color = uid == _trackedEntity ? Color.White : colorMap[entry.Group];

        var iconPath = PowerMonitoringHelper.CircleIconPath;

        switch (entry.Group)
        {
            case PowerMonitoringConsoleGroup.Generator:
                iconPath = PowerMonitoringHelper.CircleIconPath; break;
            case PowerMonitoringConsoleGroup.SMES:
                iconPath = PowerMonitoringHelper.HexagonIconPath; break;
            case PowerMonitoringConsoleGroup.Substation:
                iconPath = PowerMonitoringHelper.SquareIconPath; break;
            case PowerMonitoringConsoleGroup.APC:
                iconPath = PowerMonitoringHelper.TriangleIconPath; break;
        }

        var icon = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(iconPath)));

        // We expect a single tracked entity at a given coordinate
        if (NavMap.TrackedEntities.ContainsKey(coords))
            NavMap.TrackedEntities[coords] = (true, color, icon);

        else
            NavMap.TrackedEntities.Add(coords, (true, color, icon));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        TryToScrollToFocus();

        _updateTimer += args.DeltaSeconds;

        // Warning sign pulse
        var blinkFrequency = 1f;
        var lit = _gameTiming.RealTime.TotalSeconds % blinkFrequency > blinkFrequency / 2f;
        SystemWarningPanel.Modulate = lit ? Color.White : new Color(178, 178, 178);

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            // Request update from power monitoring system
            RequestPowerMonitoringUpdateAction?.Invoke(_entManager.GetNetEntity(_trackedEntity));
        }
    }
}
