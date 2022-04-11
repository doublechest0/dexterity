using Content.Server.Disease.Components;
using Content.Server.Weapon.Melee;
using System.Linq;
using Robust.Shared.Random;
using Content.Shared.Movement.EntitySystems;
using Content.Server.Speech.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Server.Body.Components;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Atmos.Components;

namespace Content.Server.Disease
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class DiseaseZombieSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly HandVirtualItemSystem _handVirtualItem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiseaseZombieComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DiseaseZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<DiseaseZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        }
        /// <remarks>
        /// I would imagine that if this component got assigned to something other than a mob, it would throw hella errors.
        /// </remarks>
        private void OnComponentInit(EntityUid uid, DiseaseZombieComponent component, ComponentInit args)
        {
            if (!component.ApplyZombieTraits)
                return;

            EntityManager.RemoveComponent<RespiratorComponent>(uid);
            EntityManager.RemoveComponent<BarotraumaComponent>(uid);

            EntityManager.EnsureComponent<DamageableComponent>(uid, out var damageable);
            _damageable.SetAllDamage(damageable, 0);

            //nullifies bleed damage while still allowing there to be blood and solution
            if (EntityManager.TryGetComponent<BloodstreamComponent>(uid, out var bloodstream)) 
            {
                _bloodstream.SetBloodLossThreshold(uid, 0f, bloodstream);
                _bloodstream.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
                _bloodstream.TryModifyBloodLevel(uid, bloodstream.BloodSolution.AvailableVolume, bloodstream);
            }

            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            EntityManager.EnsureComponent<ReplacementAccentComponent>(uid).Accent = "zombie";

            uid.PopupMessageEveryone(Loc.GetString("zombie-transform", ("target", uid)));
            SetupGhostRole(uid);
        }

        private void SetupGhostRole(EntityUid uid)
        {
            //idk if not having a meta comp is ever gonna be an issue but just in case
            if(EntityManager.TryGetComponent<MetaDataComponent>(uid, out var metacomp))
                metacomp.EntityName = Loc.GetString("zombie-name-prefix") + " " + metacomp.EntityName;

            EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid, out var ghostcomp);
            ghostcomp.RoleName = metacomp.EntityName;
            ghostcomp.RoleDescription = "A malevolent creature of the dead."; //TODO: loc string
            ghostcomp.RoleRules = "You are an antagonist. Search out the living and bite them to infect and convert them into zombies. Propagate the zombie hoard across the station";
        }

        ///<summary>
        ///This handles zombie disease transfer when a entity is hit.
        ///</summary>
        private void OnMeleeHit(EntityUid uid, DiseaseZombieComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            foreach (EntityUid entity in args.HitEntities)
            {
                if (entity == uid || HasComp<DiseaseZombieComponent>(entity))
                    continue;

                if (HasComp<DiseaseCarrierComponent>(entity)) //can only infect disease carrying entities. TODO: make it work for all mobs.
                {
                    if (_robustRandom.Prob(component.Probability))
                    {
                        _disease.TryAddDisease(entity, "ZombieInfection");
                    }

                    EntityManager.EnsureComponent<MobStateComponent>(entity, out var mobState);

                    if (mobState.IsDead() || mobState.IsCritical()) //dead entities are eautomatically infected. MAYBE: have activated infect ability?
                    {
                        EntityManager.EnsureComponent<DiseaseZombieComponent>(entity);
                    }
                    else if (mobState.IsAlive()) //heals when zombies bite live entities
                    {
                        var healingSolution = new Solution();
                        healingSolution.AddReagent("Bicaridine", 1.00); //if OP, reduce/change chem
                        _bloodstream.TryAddToChemicals(uid, healingSolution);
                    }
                }
            }
        }

        private void OnRefreshMovementSpeedModifiers(EntityUid uid, DiseaseZombieComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.SlowAmount, component.SlowAmount);
        }
    }
}
