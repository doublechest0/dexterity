using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.GameObjects;
using BoundUserInterface = Robust.Shared.GameObjects.BoundUserInterface;

namespace Content.Client.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringBoundUserInterface : Robust.Shared.GameObjects.BoundUserInterface
    {
        [ViewVariables]
        private CrewMonitoringWindow? _menu;

        public CrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            EntityUid? gridUid = null;

            if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
            {
                gridUid = xform.GridUid;
            }

            _menu = new CrewMonitoringWindow(gridUid);

            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case CrewMonitoringState st:
                    EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);

                    _menu?.ShowSensors(st.Sensors, xform?.Coordinates, st.Snap, st.Precision);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
