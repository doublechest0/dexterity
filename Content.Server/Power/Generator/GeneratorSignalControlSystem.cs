using System.ComponentModel;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Power.Generator;

namespace Content.Server.Power.Generator;

public sealed class GeneratorSignalControlSystem: EntitySystem
{
    [Dependency] private GeneratorSystem _generator = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneratorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(EntityUid uid, GeneratorSignalControlComponent component, SignalReceivedEvent args)
    {
        if (!TryComp<FuelGeneratorComponent>(uid, out var generator))
            return;

        if (args.Port == component.OnPort)
        {
            _generator.SetFuelGeneratorOn(uid, true, generator);
        }
        else if (args.Port == component.OffPort)
        {
            _generator.SetFuelGeneratorOn(uid, false, generator);
        }
        else if (args.Port == component.TogglePort)
        {
            _generator.SetFuelGeneratorOn(uid, !generator.On, generator);
        }
    }
}
