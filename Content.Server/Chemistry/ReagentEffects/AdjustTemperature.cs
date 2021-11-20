﻿using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects
{
    public class AdjustTemperature : ReagentEffect
    {
        [DataField("amount")]
        public float Amount;

        public override void Metabolize(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out TemperatureComponent temp))
            {
                var sys = args.EntityManager.EntitySysManager.GetEntitySystem<TemperatureSystem>();
                sys.ChangeHeat(args.SolutionEntity, Amount, true, temp);
            }
        }
    }
}
