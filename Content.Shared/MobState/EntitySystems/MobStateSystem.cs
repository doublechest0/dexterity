using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Content.Shared.Movement;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.EntitySystems
{
    public class MobStateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MobStateComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<MobStateComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<MobStateComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<MobStateComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<MobStateComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MobStateComponent, EquipAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<MobStateComponent, UnequipAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<MobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
            SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(UpdateState);
            SubscribeLocalEvent<MobStateComponent, MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<MobStateComponent, StandAttemptEvent>(OnStandAttempt);
            // Note that there's no check for Down attempts because if a mob's in crit or dead, they can be downed...
        }

        #region ActionBlocker

        private void OnChangeDirectionAttempt(EntityUid uid, MobStateComponent component, ChangeDirectionAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnUseAttempt(EntityUid uid, MobStateComponent component, UseAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnInteractAttempt(EntityUid uid, MobStateComponent component, InteractionAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnThrowAttempt(EntityUid uid, MobStateComponent component, ThrowAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnSpeakAttempt(EntityUid uid, MobStateComponent component, SpeakAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnEquipAttempt(EntityUid uid, MobStateComponent component, EquipAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        private void OnUnequipAttempt(EntityUid uid, MobStateComponent component, UnequipAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
        }

        #endregion

        private void OnStartPullAttempt(EntityUid uid, MobStateComponent component, StartPullAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }

        public void UpdateState(EntityUid _, MobStateComponent component, DamageChangedEvent args)
        {
            component.UpdateState(args.Damageable.TotalDamage);
        }

        private void OnMoveAttempt(EntityUid uid, MobStateComponent component, MovementAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedCriticalMobState:
                case SharedDeadMobState:
                    args.Cancel();
                    return;
                default:
                    return;
            }
        }

        private void OnStandAttempt(EntityUid uid, MobStateComponent component, StandAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }
    }
}
