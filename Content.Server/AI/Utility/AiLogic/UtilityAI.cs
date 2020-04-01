using System;
using System.Collections.Generic;
using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.BehaviorSets;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Content.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems.AI.LoadBalancer;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.Interfaces.Chat;
using Robust.Server.AI;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.AI.Utility.AiLogic
{
    public abstract class UtilityAi : AiLogicProcessor
    {
        // TODO
        // Potentially just have a constructed dict of all the considerations and then just pass in the context for each one
        // The actual action itself should just have the consideration type and the response curve
        private AiActionSystem _planner;
        private Blackboard _blackboard;

        /// <summary>
        /// The sum of all behaviorsets gives us what actions the AI can take
        /// </summary>
        public Dictionary<Type, BehaviorSet> BehaviorSets { get; } = new Dictionary<Type, BehaviorSet>();
        private readonly List<IAiUtility> _availableActions = new List<IAiUtility>();

        /// <summary>
        /// The currently running action; most importantly are the operators.
        /// </summary>
        public UtilityAction CurrentAction { get; private set; }

        /// <summary>
        /// How frequently we can re-plan. If an AI's in combat you could decrease the cooldown,
        /// or if there's no players nearby increase it.
        /// </summary>
        public float PlanCooldown { get; } = 0.5f;
        private float _planCooldownRemaining;

        /// <summary>
        /// If we've requested a plan then wait patiently for the action
        /// </summary>
        private AiActionRequestJob _actionRequest;

        /// <summary>
        /// If we can't do anything then stop thinking; should probably use ActionBlocker instead
        /// </summary>
        private bool _isDead = false;

        // These 2 methods will be used eventually if / when we get a director AI
        public void AddBehaviorSet(BehaviorSet behaviorSet)
        {
            if (BehaviorSets.TryAdd(behaviorSet.GetType(), behaviorSet))
            {
                SortActions();
            }
        }

        public void RemoveBehaviorSet(Type behaviorSet)
        {
            DebugTools.Assert(behaviorSet.IsInstanceOfType(typeof(BehaviorSet)));

            if (BehaviorSets.ContainsKey(behaviorSet))
            {
                BehaviorSets.Remove(behaviorSet);
                SortActions();
            }
        }

        /// <summary>
        /// Whenever the behavior sets are changed we'll re-sort the actions by bonus
        /// </summary>
        protected void SortActions()
        {
            _availableActions.Clear();
            foreach (var set in BehaviorSets.Values)
            {
                foreach (var action in set.Actions)
                {
                    var found = false;

                    for (var i = 0; i < _availableActions.Count; i++)
                    {
                        if (_availableActions[i].Bonus < action.Bonus)
                        {
                            _availableActions.Insert(i, action);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        _availableActions.Add(action);
                    }
                }
            }
        }

        // TODO. This also ties into the TODO on adding a Finalize / Startup Method to each operator
        // This would then call an event with an enum of the BarkEvent and each AI could do its own bark accordingly.
        public void Bark(string message)
        {
            var chatManager = IoCManager.Resolve<IChatManager>();
            chatManager.EntitySay(SelfEntity, message);
        }

        public override void Setup()
        {
            base.Setup();
            _planCooldownRemaining = PlanCooldown;
            _blackboard = new Blackboard(SelfEntity);
            _planner = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiActionSystem>();
            if (SelfEntity.TryGetComponent(out DamageableComponent damageableComponent))
            {
                damageableComponent.DamageThresholdPassed += DeathHandle;
            }
        }

        public void Shutdown()
        {
            // TODO: If DamageableComponent removed still need to unsubscribe?
            if (SelfEntity.TryGetComponent(out DamageableComponent damageableComponent))
            {
                damageableComponent.DamageThresholdPassed -= DeathHandle;
            }
        }

        private void DeathHandle(object sender, DamageThresholdPassedEventArgs eventArgs)
        {
            if (eventArgs.DamageThreshold.ThresholdType == ThresholdType.Death)
            {
                _isDead = true;
            }

            // TODO: If we get healed - double-check what it should be
            if (eventArgs.DamageThreshold.ThresholdType == ThresholdType.None)
            {
                _isDead = false;
            }
        }

        private void ReceivedAction()
        {
            var action = _actionRequest.Result;
            _actionRequest = null;
            // Actions with lower scores should be implicitly dumped by GetAction
            // If we're not allowed to replace the action with an action of the same type then dump.
            if (action == null || !action.CanOverride && CurrentAction?.GetType() == action.GetType())
            {
                return;
            }

            CurrentAction = action;
            action.SetupOperators(_blackboard);
        }

        public override void Update(float frameTime)
        {
            // If we can't do anything then there's no point thinking
            if (_isDead || BehaviorSets.Count == 0)
            {
                _blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                CurrentAction = null;
                return;
            }

            // If we asked for a new action we don't want to dump the existing one.
            if (_actionRequest != null)
            {
                if (_actionRequest.Status != Status.Finished)
                {
                    return;
                }

                ReceivedAction();
                // Do something next tick
                return;
            }

            _planCooldownRemaining -= frameTime;

            // Might find a better action while we're doing one already
            if (_planCooldownRemaining <= 0.0f)
            {
                _planCooldownRemaining = PlanCooldown;
                _actionRequest = _planner.RequestAction(new AiActionRequest(SelfEntity.Uid, _blackboard, _availableActions));

                return;
            }

            // When we spawn in we won't get an action for a bit
            if (CurrentAction == null)
            {
                return;
            }

            var outcome = CurrentAction.Execute(frameTime);

            switch (outcome)
            {
                case Outcome.Success:
                    if (CurrentAction.ActionOperators.Count == 0)
                    {
                        CurrentAction = null;
                        // Nothing to compare new action to
                        _blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                    }
                    break;
                case Outcome.Continuing:
                    break;
                case Outcome.Failed:
                    CurrentAction = null;
                    _blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
