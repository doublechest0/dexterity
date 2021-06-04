using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Binary;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Binary
{
    [UsedImplicitly]
    public class GasPressurePumpSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceUpdateEvent>(OnPumpUpdated);
        }

        private void OnPumpUpdated(EntityUid uid, GasPressurePumpComponent pump, AtmosDeviceUpdateEvent args)
        {
            var appearance = pump.Owner.GetComponentOrNull<AppearanceComponent>();
            appearance?.SetData(PumpVisuals.Enabled, false);

            if (!pump.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(pump.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(pump.OutletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;

            if (MathHelper.CloseTo(pump.TargetPressure, outputStartingPressure))
                return; // No need to pump gas if target has been reached.

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                appearance?.SetData(PumpVisuals.Enabled, true);

                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = pump.TargetPressure - outputStartingPressure;
                var transferMoles = pressureDelta * outlet.Air.Volume / inlet.Air.Temperature * Atmospherics.R;

                var removed = inlet.Air.Remove(transferMoles);
                outlet.Air.Merge(removed);
            }
        }
    }
}
