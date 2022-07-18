using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class MovementIgnoreGravityComponent : Component
    {
        /// <summary>
        /// Whether or not gravity is on or off for this object.
        /// </summary>
        [DataField("gravityState")] public bool Weightless = false;
    }


    [NetSerializable, Serializable]
    public sealed class MovementIgnoreGravityComponentState : ComponentState
    {
        public bool Weightless;

        public MovementIgnoreGravityComponentState(MovementIgnoreGravityComponent component)
        {
            Weightless = component.Weightless;
        }
    }

    public static class GravityExtensions
    {
        [Obsolete("Use GravitySystem")]
        public static bool IsWeightless(this EntityUid entity, PhysicsComponent? body = null, EntityCoordinates? coords = null, IMapManager? mapManager = null, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (body == null)
                entityManager.TryGetComponent(entity, out body);

            if ((body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
                return false;

            if (entityManager.TryGetComponent<MovementIgnoreGravityComponent>(entity, out var ignoreGravityComponent))
                return ignoreGravityComponent.Weightless;

            var transform = entityManager.GetComponent<TransformComponent>(entity);
            var gridId = transform.GridUid;
            mapManager ??= IoCManager.Resolve<IMapManager>();

            if ((entityManager.TryGetComponent<GravityComponent>(transform.GridUid, out var gravity) ||
                entityManager.TryGetComponent(transform.MapUid, out gravity)) && gravity.Enabled)
                return false;

            if (gridId == null)
            {
                return true;
            }

            var grid = mapManager.GetGrid(gridId.Value);
            var invSys = EntitySystem.Get<InventorySystem>();

            if (invSys.TryGetSlotEntity(entity, "shoes", out var ent))
            {
                if (entityManager.TryGetComponent<MagbootsComponent>(ent, out var boots) && boots.On)
                    return false;
            }

            if (!entityManager.GetComponent<GravityComponent>(grid.GridEntityId).Enabled)
            {
                return true;
            }

            coords ??= transform.Coordinates;

            if (!coords.Value.IsValid(entityManager))
            {
                return true;
            }

            var tile = grid.GetTileRef(coords.Value).Tile;
            return tile.IsEmpty;
        }
    }
}
