using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Server.Speech.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Ghost.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class MakeSentient : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // This makes it so it doesn't affect things that are already sentient
        if (entityManager.HasComponent<MindContainerComponent>(uid))
        {
            return;
        }

        // This makes it so it doesn't add a ghost role to things that are already sentient
        if (entityManager.HasComponent<MindContainerComponent>(uid))
        {
            return;
        }

        // No idea what anything past this point does
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole) ||
            entityManager.TryGetComponent(uid, out GhostTakeoverAvailableComponent? takeOver))
        {
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}

// Original code written by areitpog on GitHub in Issue #7666, then I (Interrobang01) copied it and used my nonexistant C# skills to try to make it work again.
