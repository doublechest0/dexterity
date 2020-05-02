using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Melee;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Mobs;
using Content.Server.GameObjects.Components.Movement;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Melee
{
    public sealed class MeleeAttackNearbySpeciesExp : ExpandableUtilityAction
    {
        public override float Bonus => 30.0f;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!owner.HasComponent<AiControllerComponent>())
            {
                throw new InvalidOperationException();
            }

            foreach (var entity in context.GetState<NearbySpeciesState>().GetValue())
            {
                yield return new MeleeAttackEntity(owner, entity, Bonus);
            }
        }
    }
}
