using System.Collections.Generic;
using Content.Server.GameObjects.Components.Materials;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Research;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheComponent : SharedLatheComponent, IAttackHand, IAttackby
    {
        public Dictionary<StackType, uint> MaterialStorage;
        public List<StackType> AcceptedMaterials = new List<StackType>() {StackType.Metal, StackType.Glass};

        bool IAttackHand.Attackhand(IEntity user)
        {
            user.TryGetComponent(out BasicActorComponent actor);

            if (actor == null) return false;

            SendNetworkMessage(new LatheMenuOpenMessage(), actor.playerSession?.ConnectedClient);

            return false;
        }

        bool IAttackby.Attackby(IEntity user, IEntity attackwith)
        {
            return InsertMaterial(attackwith);
        }

        bool InsertMaterial(IEntity entity)
        {
            entity.TryGetComponent(out StackComponent stack);

            if (stack == null) return false;

            switch (stack.StackType)
            {
                case StackType.Metal:
                    return true;
                case StackType.Glass:
                    return true;
                default:
                    return false;
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {

        }
    }
}
