using System.Collections.Generic;
using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Ranged;
using Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan
{
    public sealed class PickUpHitscanWeapon : UtilityAction
    {
        private IEntity _entity;

        public PickUpHitscanWeapon(IEntity owner, IEntity entity, BonusWeight weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators =  new Queue<IOperator>(new IOperator[]
            {
                new MoveToEntityOperator(Owner, _entity),
                new SwapToFreeHandOperator(Owner),
                new PickupEntityOperator(Owner, _entity),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<MoveTargetState>().SetValue(_entity);
            context.GetState<TargetEntityState>().SetValue(_entity);
            context.GetState<WeaponEntityState>().SetValue(_entity);
        }

        protected override Consideration[] Considerations { get; } = {
            new HeldRangedWeaponsCon(
                new QuadraticCurve(-1.0f, 1.0f, 1.0f, 0.0f)),
            new TargetNotInAnyHandsCon(
                new BoolCurve()),
            new FreeHandCon(
                new BoolCurve()),
            // For now don't grab empty guns - at least until we can start storing stuff in inventory
            new HitscanChargeCon(
                new BoolCurve()),
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.02f, 0.0f)),
            // TODO: Weapon charge level
            new RangedWeaponFireRateCon(
                new QuadraticCurve(1.0f, 0.5f, 0.0f, 0.0f)),
            new HitscanWeaponDamageCon(
                new QuadraticCurve(1.0f, 0.25f, 0.0f, 0.0f)),
        };
    }
}
