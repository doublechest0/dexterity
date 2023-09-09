using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;

namespace Content.Server.Temperature.Systems;

public sealed class TemperatureSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    /// <summary>
    ///     All the components that will have their damage updated at the end of the tick.
    ///     This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    ///     that we need some mechanism to ensure it doesn't double dip on damage for both calls.
    /// </summary>
    public HashSet<EntityUid> ShouldUpdateDamage = new();

    public float UpdateInterval = 1.0f;

    private float _accumulatedFrameTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<TemperatureComponent, OnTemperatureChangeEvent>(EnqueueDamage);
        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(ServerAlert);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(
            OnTemperatureChangeAttempt);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(
            OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(
            OnParentThresholdShutdown);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedFrameTime += frameTime;

        if (_accumulatedFrameTime < UpdateInterval)
            return;
        _accumulatedFrameTime -= UpdateInterval;

        if (!ShouldUpdateDamage.Any())
            return;


        foreach (var uid in ShouldUpdateDamage)
        {
            MetaDataComponent? metaData = null;

            if (!TryComp<TemperatureComponent>(uid, out var comp))
                return;

            if (Deleted(uid, metaData) || Paused(uid, metaData))
                continue;

            ChangeDamage(uid, comp);
        }

        ShouldUpdateDamage.Clear();
    }

    public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
    {
        if (!Resolve(uid, ref temperature))
            return;

        var lastTemp = temperature.CurrentTemperature;
        var delta = temperature.CurrentTemperature - temp;
        temperature.CurrentTemperature = temp;
        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta),
                true);
    }

    public void ChangeHeat(EntityUid uid, float heatAmount, bool ignoreHeatResistance = false,
            TemperatureComponent? temperature = null)
    {
        if (Resolve(uid, ref temperature))
        {
            if (!ignoreHeatResistance)
            {
                var ev = new ModifyChangedTemperatureEvent(heatAmount);
                RaiseLocalEvent(uid, ev);
                heatAmount = ev.TemperatureDelta;
            }

            var lastTemp = temperature.CurrentTemperature;
            temperature.CurrentTemperature += heatAmount / temperature.HeatCapacity;
            var delta = temperature.CurrentTemperature - lastTemp;

            RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta),
                    true);
        }
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature,
            ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);

        var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
        var tileHeatCapacity =
                _atmosphereSystem.GetTileHeatCapacity(transform.GridUid, transform.MapUid.Value, position);
        var heat = temperatureDelta * (tileHeatCapacity * temperature.HeatCapacity /
                                           (tileHeatCapacity + temperature.HeatCapacity));
        ChangeHeat(uid, heat * temperature.AtmosTemperatureTransferEfficiency, temperature: temperature);
    }

    private void OnRejuvenate(EntityUid uid, TemperatureComponent comp, RejuvenateEvent args)
    {
        ForceChangeTemperature(uid, Atmospherics.T20C, comp);
    }

    private void ServerAlert(EntityUid uid, AlertsComponent status, OnTemperatureChangeEvent args)
    {
        if (TryComp<TemperatureComponent>(uid, out var temperature))
        {
            var cold3 = temperature.ColdDamageThreshold;
            var cold2 = cold3 + 20;
            var cold1 = cold3 + 32;
            var hot3 = temperature.HeatDamageThreshold;
            var hot2 = hot3 - 20;
            var hot1 = hot3 - 33;

            if (temperature.ColdDamageThreshold + 32 >= temperature.HeatDamageThreshold - 32)
            {
                if (temperature.ColdDamageThreshold < temperature.HeatDamageThreshold)
                {
                    var range = temperature.HeatDamageThreshold - temperature.ColdDamageThreshold;
                    cold2 = cold3 + range * 0.2f;
                    cold1 = cold3 + range * 0.4f;
                    hot1 = hot3 - range * 0.4f;
                    hot2 = hot3 - range * 0.2f;
                }
                else
                {
                    _alertsSystem.ClearAlertCategory(uid, AlertCategory.Temperature);
                    return;
                }
            }

            // Dynamic variables can't be used in a Switch, so this has to be an If
            //
            // Dangerously cold
            if (args.CurrentTemperature <= cold3)
            {
                _alertsSystem.ShowAlert(uid, AlertType.Cold, 3);
            }
            // Cold warnings
            else if (args.CurrentTemperature <= cold2 && args.CurrentTemperature > cold3)
            {
                _alertsSystem.ShowAlert(uid, AlertType.Cold, 2);
            }
            else if (args.CurrentTemperature <= cold1 && args.CurrentTemperature > cold2)
            {
                _alertsSystem.ShowAlert(uid, AlertType.Cold, 1);
            }
            // Safe
            else if (args.CurrentTemperature <= hot1 && args.CurrentTemperature > cold1)
            {
                _alertsSystem.ClearAlertCategory(uid, AlertCategory.Temperature);
            }
            // Hot warnings
            else if (args.CurrentTemperature <= hot2 && args.CurrentTemperature > hot1)
            {
                _alertsSystem.ShowAlert(uid, AlertType.Hot, 1);
            }
            else if (args.CurrentTemperature <= hot3 && args.CurrentTemperature > hot2)
            {
                _alertsSystem.ShowAlert(uid, AlertType.Hot, 2);
            } // Dangerously hot
            else if (args.CurrentTemperature > hot3)
            {
                _alertsSystem.ShowAlert(uid, AlertType.Hot, 3);
            }
        }
    }

    private void EnqueueDamage(EntityUid uid, TemperatureComponent component, OnTemperatureChangeEvent args)
    {
        ShouldUpdateDamage.Add(uid);
    }

    private void ChangeDamage(EntityUid uid, TemperatureComponent temperature)
    {
        if (!HasComp<DamageableComponent>(uid))
            return;

        // See this link for where the scaling func comes from:
        // https://www.desmos.com/calculator/0vknqtdvq9
        // Based on a logistic curve, which caps out at MaxDamage
        var heatK = 0.005;
        var a = 1;
        var y = temperature.DamageCap;
        var c = y * 2;

        var heatDamageThreshold = temperature.ParentHeatDamageThreshold ?? temperature.HeatDamageThreshold;
        var coldDamageThreshold = temperature.ParentColdDamageThreshold ?? temperature.ColdDamageThreshold;

        if (temperature.CurrentTemperature >= heatDamageThreshold)
        {
            if (!temperature.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking high temperature damage");
                temperature.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - heatDamageThreshold);
            var tempDamage = c / (1 + a * Math.Pow(Math.E, -heatK * diff)) - y;
            _damageableSystem.TryChangeDamage(uid, temperature.HeatDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.CurrentTemperature <= coldDamageThreshold)
        {
            if (!temperature.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking low temperature damage");
                temperature.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - coldDamageThreshold);
            var tempDamage = Math.Sqrt(diff * (Math.Pow(temperature.DamageCap.Double(), 2) / coldDamageThreshold));
            _damageableSystem.TryChangeDamage(uid, temperature.ColdDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.TakingDamage)
        {
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
            temperature.TakingDamage = false;
        }
    }

    private void OnTemperatureChangeAttempt(EntityUid uid, TemperatureProtectionComponent component,
        InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        args.Args.TemperatureDelta *= component.Coefficient;
    }

    private void OnParentChange(EntityUid uid, TemperatureComponent component, ref EntParentChangedMessage args)
    {
        var temperatureQuery = GetEntityQuery<TemperatureComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();
        var thresholdsQuery = GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>();
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value, transformQuery, thresholdsQuery)
            : (null, null);
        var newThresholds = RecalculateParentThresholds(transformQuery.GetComponent(uid).ParentUid, transformQuery, thresholdsQuery);

        if (oldThresholds != newThresholds)
        {
            RecursiveThresholdUpdate(uid, temperatureQuery, transformQuery, thresholdsQuery);
        }
    }

    private void OnParentThresholdStartup(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentStartup args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    private void OnParentThresholdShutdown(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentShutdown args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureComponent>(), GetEntityQuery<TransformComponent>(),
                GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private void RecursiveThresholdUpdate(EntityUid root, EntityQuery<TemperatureComponent> temperatureQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> tempThresholdsQuery)
    {
        RecalculateAndApplyParentThresholds(root, temperatureQuery, transformQuery, tempThresholdsQuery);

        foreach (var child in Transform(root).ChildEntities)
        {
            RecursiveThresholdUpdate(child, temperatureQuery, transformQuery, tempThresholdsQuery);
        }
    }

    /// <summary>
    /// Recalculate parent thresholds and apply them on the uid temperature component.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private void RecalculateAndApplyParentThresholds(EntityUid uid,
        EntityQuery<TemperatureComponent> temperatureQuery, EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> tempThresholdsQuery)
    {
        if (!temperatureQuery.TryGetComponent(uid, out var temperature))
        {
            return;
        }

        var newThresholds = RecalculateParentThresholds(transformQuery.GetComponent(uid).ParentUid, transformQuery, tempThresholdsQuery);
        temperature.ParentHeatDamageThreshold = newThresholds.Item1;
        temperature.ParentColdDamageThreshold = newThresholds.Item2;
    }

    /// <summary>
    /// Recalculate Parent Heat/Cold DamageThreshold by recursively checking each ancestor and fetching the
    /// maximum HeatDamageThreshold and the minimum ColdDamageThreshold if any exists (aka the best value for each).
    /// </summary>
    /// <param name="initialParentUid"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private (float?, float?) RecalculateParentThresholds(
        EntityUid initialParentUid,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> tempThresholdsQuery)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (tempThresholdsQuery.TryGetComponent(parentUid, out var newThresholds))
            {
                if (newThresholds.HeatDamageThreshold != null)
                {
                    newHeatThreshold = Math.Max(newThresholds.HeatDamageThreshold.Value,
                        newHeatThreshold ?? 0);
                }

                if (newThresholds.ColdDamageThreshold != null)
                {
                    newColdThreshold = Math.Min(newThresholds.ColdDamageThreshold.Value,
                        newColdThreshold ?? float.MaxValue);
                }
            }

            parentUid = transformQuery.GetComponent(parentUid).ParentUid;
        }

        return (newHeatThreshold, newColdThreshold);
    }
}

public sealed class OnTemperatureChangeEvent : EntityEventArgs
{
    public float CurrentTemperature { get; }
    public float LastTemperature { get; }
    public float TemperatureDelta { get; }

    public OnTemperatureChangeEvent(float current, float last, float delta)
    {
        CurrentTemperature = current;
        LastTemperature = last;
        TemperatureDelta = delta;
    }
}
