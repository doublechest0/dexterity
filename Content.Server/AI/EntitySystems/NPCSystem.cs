using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.AI.Components;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.MobState.States;
using Content.Shared;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.AI.EntitySystems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    [UsedImplicitly]
    internal class NPCSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        /// <summary>
        ///     To avoid iterating over dead AI continuously they can wake and sleep themselves when necessary.
        /// </summary>
        private readonly HashSet<AiControllerComponent> _awakeNPCs = new();

        /// <summary>
        /// Whether any NPCs are allowed to run at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AiControllerComponent, MobStateChangedEvent>(OnMobStateChange);
            SubscribeLocalEvent<AiControllerComponent, ComponentInit>(OnNPCInit);
            SubscribeLocalEvent<AiControllerComponent, ComponentShutdown>(OnNPCShutdown);
            _configurationManager.OnValueChanged(CCVars.NPCEnabled, SetEnabled, true);
            _awakeNPCs.EnsureCapacity(_configurationManager.GetCVar(CCVars.NPCMaxUpdates));
        }

        private void SetEnabled(bool value) => Enabled = value;

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CCVars.NPCEnabled, SetEnabled);
        }

        private void OnNPCInit(EntityUid uid, AiControllerComponent component, ComponentInit args)
        {
            if (!component.Awake) return;

            _awakeNPCs.Add(component);
        }

        private void OnNPCShutdown(EntityUid uid, AiControllerComponent component, ComponentShutdown args)
        {
            _awakeNPCs.Remove(component);
        }

        /// <summary>
        /// Allows the NPC to actively be updated.
        /// </summary>
        /// <param name="component"></param>
        public void WakeNPC(AiControllerComponent component)
        {
            _awakeNPCs.Add(component);
        }

        /// <summary>
        /// Stops the NPC from actively being updated.
        /// </summary>
        /// <param name="component"></param>
        public void SleepNPC(AiControllerComponent component)
        {
            _awakeNPCs.Remove(component);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            if (!Enabled) return;

            var cvarMaxUpdates = _configurationManager.GetCVar(CCVars.NPCMaxUpdates);

            if (cvarMaxUpdates <= 0) return;

            var count = 0;

            foreach (var npc in _awakeNPCs.ToArray())
            {
                if (npc.Deleted)
                    continue;

                if (npc.Paused)
                    continue;

                if (count >= cvarMaxUpdates)
                    break;

                npc.Update(frameTime);
                count++;
            }
        }

        private void OnMobStateChange(EntityUid uid, AiControllerComponent component, MobStateChangedEvent args)
        {
            switch (args.CurrentMobState)
            {
                case NormalMobState:
                    component.Awake = true;
                    break;
                case CriticalMobState:
                case DeadMobState:
                    component.Awake = false;
                    break;
            }
        }
    }
}
