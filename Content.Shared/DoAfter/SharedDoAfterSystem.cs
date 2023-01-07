﻿using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Robust.Shared.GameStates;

namespace Content.Shared.DoAfter;

public abstract class SharedDoAfterSystem : EntitySystem
{
        // We cache the list as to not allocate every update tick...
        private readonly Queue<DoAfter> _pending = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoAfterComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<DoAfterComponent, MobStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<DoAfterComponent, ComponentGetState>(OnDoAfterGetState);
        }

        public void Add(DoAfterComponent component, DoAfter doAfter)
        {
            doAfter.ID = component.RunningIndex;
            doAfter.Delay = doAfter.EventArgs.Delay;
            component.DoAfters.Add(component.RunningIndex, doAfter);
            EnsureComp<ActiveDoAfterComponent>(component.Owner);
            component.RunningIndex++;
            Dirty(component);
        }

        private void OnDoAfterGetState(EntityUid uid, DoAfterComponent component, ref ComponentGetState args)
        {
            args.State = new DoAfterComponentState(component.DoAfters);
        }

        public void Cancelled(DoAfterComponent component, DoAfter doAfter)
        {
            if (!component.DoAfters.TryGetValue(doAfter.ID, out var index))
                return;

            component.DoAfters.Remove(doAfter.ID);

            if (component.DoAfters.Count == 0)
                RemComp<ActiveDoAfterComponent>(component.Owner);

            RaiseNetworkEvent(new CancelledDoAfterMessage(component.Owner, index.ID));
        }

        /// <summary>
        ///     Call when the particular DoAfter is finished.
        ///     Client should be tracking this independently.
        /// </summary>
        public void Finished(DoAfterComponent component, DoAfter doAfter)
        {
            if (!component.DoAfters.ContainsKey(doAfter.ID))
                return;

            component.DoAfters.Remove(doAfter.ID);

            if (component.DoAfters.Count == 0)
                RemComp<ActiveDoAfterComponent>(component.Owner);
        }

        private void OnStateChanged(EntityUid uid, DoAfterComponent component, MobStateChangedEvent args)
        {
            if(args.CurrentMobState != DamageState.Dead || args.CurrentMobState != DamageState.Critical)
                return;

            foreach (var (_, doAfter) in component.DoAfters)
            {
                doAfter.Cancel();
            }
        }

        /// <summary>
        /// Cancels DoAfter if it breaks on damage and it meets the threshold
        /// </summary>
        /// <param name="_">
        /// The EntityUID of the user
        /// </param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        public void OnDamage(EntityUid _, DoAfterComponent component, DamageChangedEvent args)
        {
            if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null)
                return;

            foreach (var (_, doAfter) in component.DoAfters)
            {
                if (doAfter.EventArgs.BreakOnDamage && args.DamageDelta?.Total.Float() > doAfter.EventArgs.DamageThreshold)
                {
                    doAfter.Cancel();
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, comp) in EntityManager.EntityQuery<ActiveDoAfterComponent, DoAfterComponent>())
            {
                foreach (var (_, doAfter) in comp.DoAfters.ToArray())
                {
                    doAfter.Run(EntityManager);

                    switch (doAfter.Status)
                    {
                        case DoAfterStatus.Running:
                            break;
                        case DoAfterStatus.Cancelled:
                            _pending.Enqueue(doAfter);
                            break;
                        case DoAfterStatus.Finished:
                            _pending.Enqueue(doAfter);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                while (_pending.TryDequeue(out var doAfter))
                {
                    if (doAfter.Status == DoAfterStatus.Cancelled)
                    {
                        Cancelled(comp, doAfter);
                        var ev = new DoAfterEvent(true, doAfter.EventArgs);

                        if (EntityManager.EntityExists(doAfter.EventArgs.User))
                            RaiseLocalEvent(doAfter.EventArgs.User, ev, doAfter.EventArgs.Broadcast);

                        if (doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target))
                            RaiseLocalEvent(target, ev, doAfter.EventArgs.Broadcast);

                        if (doAfter.EventArgs.Used is {} used && EntityManager.EntityExists(used))
                            RaiseLocalEvent(used, ev, doAfter.EventArgs.Broadcast);
                    }

                    if (doAfter.Status == DoAfterStatus.Finished)
                    {
                        Finished(comp, doAfter);
                        var ev = new DoAfterEvent(false, doAfter.EventArgs);

                        if (EntityManager.EntityExists(doAfter.EventArgs.User))
                            RaiseLocalEvent(doAfter.EventArgs.User, ev, doAfter.EventArgs.Broadcast);

                        if (doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target))
                            RaiseLocalEvent(target, ev, doAfter.EventArgs.Broadcast);

                        if (doAfter.EventArgs.Used is {} used && EntityManager.EntityExists(used))
                            RaiseLocalEvent(used, ev, doAfter.EventArgs.Broadcast);
                    }
                }
            }
        }

        /// <summary>
        ///     Tasks that are delayed until the specified time has passed
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public async Task<DoAfterStatus> WaitDoAfter(DoAfterEventArgs eventArgs)
        {
            var doAfter = CreateDoAfter(eventArgs);

            await doAfter.AsTask;

            return doAfter.Status;
        }

        /// <summary>
        ///     Creates a DoAfter without waiting for it to finish. You can use events with this.
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        public void DoAfter(DoAfterEventArgs eventArgs)
        {
            CreateDoAfter(eventArgs);
        }

        private DoAfter CreateDoAfter(DoAfterEventArgs eventArgs)
        {
            // Setup
            var doAfter = new DoAfter(eventArgs, EntityManager);
            // Caller's gonna be responsible for this I guess
            var doAfterComponent = Comp<DoAfterComponent>(eventArgs.User);
            Add(doAfterComponent, doAfter);
            return doAfter;
        }
}
