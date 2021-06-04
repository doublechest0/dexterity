using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Atmos.Piping.Binary;
using Content.Server.GameObjects.Components.Atmos.Piping.Unary;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Unary
{
    [UsedImplicitly]
    public class GasPortableSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPortableComponent, AnchorAttemptEvent>(OnPortableAnchorAttempt);
            SubscribeLocalEvent<GasPortableComponent, AnchoredEvent>(OnPortableAnchored);
            SubscribeLocalEvent<GasPortableComponent, UnanchoredEvent>(OnPortableUnanchored);
        }

        private void OnPortableAnchorAttempt(EntityUid uid, GasPortableComponent component, AnchorAttemptEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out ITransformComponent? transform))
                return;

            // If we can't find any ports, cancel the anchoring.
            if(!FindGasPortIn(transform.GridID, transform.Coordinates, out _))
                args.Cancel();
        }

        private void OnPortableAnchored(EntityUid uid, GasPortableComponent portable, AnchoredEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(portable.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = true;

            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(GasPortableVisuals.ConnectedState, true);
            }
        }

        private void OnPortableUnanchored(EntityUid uid, GasPortableComponent portable, UnanchoredEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(portable.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = false;

            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(GasPortableVisuals.ConnectedState, false);
            }
        }

        private bool FindGasPortIn(GridId gridId, EntityCoordinates coordinates, [NotNullWhen(true)] out GasPortComponent? port)
        {
            port = null;

            if (!gridId.IsValid())
                return false;

            var grid = _mapManager.GetGrid(gridId);

            foreach (var entityUid in grid.GetLocal(coordinates))
            {
                if (ComponentManager.TryGetComponent<GasPortComponent>(entityUid, out port))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
