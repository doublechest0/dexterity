using Content.Server.Atmos.Components;
using Content.Server.Body.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private void InitializeBreathTool()
    {
        SubscribeLocalEvent<BreathToolComponent, ComponentShutdown>(OnBreathToolShutdown);
    }

    private void OnBreathToolShutdown(EntityUid uid, BreathToolComponent component, ComponentShutdown args)
    {
        DisconnectInternals(component);
    }

    public void DisconnectInternals(BreathToolComponent component)
    {
        var old = component.ConnectedInternalsEntity;
        component.ConnectedInternalsEntity = default;

        if (old != default && TryComp<InternalsComponent>(old, out var internalsComponent))
        {
            _internals.DisconnectBreathTool(internalsComponent);
        }

        component.IsFunctional = false;
    }
}
