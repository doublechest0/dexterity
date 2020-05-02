using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Clothing.Shoes;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.ExpandableActions.Clothing.Shoes
{
    public sealed class PickUpAnyNearbyShoesExp : ExpandableUtilityAction
    {
        public override float Bonus => 5.0f;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!owner.TryGetComponent(out AiControllerComponent controller))
            {
                throw new InvalidOperationException();
            }

            foreach (var entity in context.GetState<NearbyClothingState>().GetValue())
            {
                if (entity.TryGetComponent(out ClothingComponent clothing) &&
                    (clothing.SlotFlags & EquipmentSlotDefines.SlotFlags.SHOES) != 0)
                {
                    yield return new PickUpShoes(owner, entity, Bonus);
                }
            }
        }
    }
}
