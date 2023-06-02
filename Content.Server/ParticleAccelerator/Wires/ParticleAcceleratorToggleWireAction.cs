using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Wires;
using Robust.Server.GameObjects;

namespace Content.Server.ParticleAccelerator.Wires;

public sealed class ParticleAcceleratorToggleWireAction : ComponentWireAction<ParticleAcceleratorControlBoxComponent>
{
    public override string Name { get; set; } = "wire-name-pa-toggle";
    public override Color Color { get; set; } = Color.Yellow;
    public override object StatusKey { get; } = ParticleAcceleratorControlBoxWires.Toggle;

    public override StatusLightState? GetLightState(Wire wire, ParticleAcceleratorControlBoxComponent component)
    {
        if (!component.CanBeEnabled)
            return StatusLightState.Off;
        return component.Enabled ? StatusLightState.On : StatusLightState.BlinkingSlow;
    }

    public override bool Cut(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        var userSession = EntityManager.TryGetComponent<ActorComponent>(user, out var actor) ? actor.PlayerSession : null;

        controller.CanBeEnabled = false;
        paSystem.SwitchOff(wire.Owner, userSession, controller);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.CanBeEnabled = true;
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        var userSession = EntityManager.TryGetComponent<ActorComponent>(user, out var actor) ? actor.PlayerSession : null;

        if (controller.Enabled)
            paSystem.SwitchOff(wire.Owner, userSession, controller);
        else if (controller.Assembled)
            paSystem.SwitchOn(wire.Owner, userSession, controller);
    }
}
