using System.Collections.Generic;
using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.AI.Operators.Combat.Ranged;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat;
using Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.Utils;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.AI.WorldState.States.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan
{
    public sealed class HitscanAttackEntity : UtilityAction
    {
        private IEntity _entity;
        private MoveToEntityOperator _moveOperator;

        public HitscanAttackEntity(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            _moveOperator = new MoveToEntityOperator(Owner, _entity);
            _moveOperator.MovedATile += InLos;

            // TODO: Accuracy in blackboard
            ActionOperators = new Queue<IOperator>(new IOperator[]
            {
                _moveOperator,
                new ShootAtEntityOperator(Owner, _entity, 0.7f),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(_entity);
            context.GetState<MoveTargetState>().SetValue(_entity);
            var equipped = context.GetState<EquippedEntityState>().GetValue();
            context.GetState<WeaponEntityState>().SetValue(equipped);
        }

        protected override Consideration[] Considerations { get; } = {
            // Check if we have a weapon; easy-out
            new HitscanWeaponEquippedCon(
                new BoolCurve()),
            new HitscanChargeCon(
                new QuadraticCurve(1.0f, 0.1f, 0.0f, 0.0f)),
            // Don't attack a dead target
            new TargetIsDeadCon(
                new InverseBoolCurve()),
            // Deprioritise a target in crit
            new TargetIsCritCon(
                new QuadraticCurve(-0.8f, 1.0f, 1.0f, 0.0f)),
            // Somewhat prioritise distance
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.07f, 0.0f)),
            // Prefer weaker targets
            new TargetHealthCon(
                new QuadraticCurve(1.0f, 0.4f, 0.0f, -0.02f)),
        };

        ~HitscanAttackEntity()
        {
            if (_moveOperator != null)
            {
                _moveOperator.MovedATile -= InLos;
            }
        }

        private void InLos()
        {
            // This should only be called if the movement operator is the current one;
            // if that turns out not to be the case we can just add a check here.
            if (Visibility.InLineOfSight(Owner, _entity))
            {
                _moveOperator.HaveArrived();
                ActionOperators.Dequeue();
            }
        }
    }
}
