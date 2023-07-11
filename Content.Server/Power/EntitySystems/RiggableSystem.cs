using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Stunnable.Components;
using Content.Shared.Database;
using Content.Shared.Rejuvenate;

namespace Content.Server.Power.EntitySystems;

/// <summary>
///  Handles sabotaged/rigged objects
/// </summary>
public sealed class RiggableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RiggableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RiggableComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<RiggableComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnRejuvenate(EntityUid uid, RiggableComponent component, RejuvenateEvent args)
    {
        component.IsRigged = false;
    }

    private void OnMicrowaved(EntityUid uid, RiggableComponent component, BeingMicrowavedEvent args)
    {
        if (TryComp<BatteryComponent>(uid, out var batteryComponent))
        {
            if (batteryComponent.CurrentCharge == 0)
                return;
        }

        args.Handled = true;

        // What the fuck are you doing???
        Explode(uid, batteryComponent, args.User);
    }

    private void OnSolutionChanged(EntityUid uid, RiggableComponent component, SolutionChangedEvent args)
    {
        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            IsRigged(uid, args);
        }

        if (component.IsRigged)
        {
            _adminLogger.Add(LogType.Explosion, LogImpact.Medium, $"{ToPrettyString(uid)} has been rigged up to explode when used.");
        }
    }

    public void IsRigged(EntityUid uid, SolutionChangedEvent args)
    {
        if (TryComp<RiggableComponent>(uid, out var riggableComp))
        {
            riggableComp.IsRigged = _solutionsSystem.TryGetSolution(uid, RiggableComponent.SolutionName, out var solution)
                                    && solution.TryGetReagent("Plasma", out var plasma)
                                    && plasma >= 5;
        }
    }

    public void Explode(EntityUid uid, BatteryComponent? battery = null, EntityUid? cause = null)
    {
        if (!Resolve(uid, ref battery))
            return;

        var radius = MathF.Min(5, MathF.Sqrt(battery.CurrentCharge) / 9);

        _explosionSystem.TriggerExplosive(uid, radius: radius, user:cause);
        QueueDel(uid);
    }
}
