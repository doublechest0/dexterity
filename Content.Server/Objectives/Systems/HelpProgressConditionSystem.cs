using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles help progress condition logic and picking random help targets.
/// </summary>
public sealed class HelpProgressConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ObjectiveSystem _objective = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HelpProgressConditionComponent, ConditionGetInfoEvent>(OnGetInfo);

        SubscribeLocalEvent<RandomTraitorProgressComponent, ConditionAssignedEvent>(OnTraitorAssigned);
    }

    private void OnGetInfo(EntityUid uid, HelpProgressConditionComponent comp, ref ConditionGetInfoEvent args)
    {
        if (comp.Target == null)
            return;

        args.Info.Title = GetTitle(comp.Target.Value);
    }

    private void OnTraitorAssigned(EntityUid uid, RandomTraitorProgressComponent comp, ref ConditionAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<HelpProgressConditionComponent>(uid, out var help))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind)
            .Select(pair => pair.Item1)
            .ToHashSet();
        var removeList = new List<EntityUid>();

        // cant help anyone who is tasked with helping:
        // 1. thats boring
        // 2. no cyclic progress dependencies!!!
        foreach (var traitor in traitors)
        {
            // TODO: replace this with TryComp<ObjectivesComponent>(traitor) or something when objectives are moved out of mind
            if (!TryComp<MindComponent>(traitor, out var mind))
                continue;

            foreach (var objective in mind.AllObjectives)
            {
                foreach (var condition in objective.Conditions)
                {
                    if (HasComp<HelpProgressConditionComponent>(condition))
                        removeList.Add(traitor);
                }
            }
        }

        foreach (var tot in removeList)
        {
            traitors.Remove(tot);
        }

        // no more helpable traitors
        if (traitors.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        help.Target = _random.Pick(traitors);
    }

    // TODO: TargetedObjective, Title field
    private string GetTitle(EntityUid target)
    {
        var targetName = "Unknown";
        var jobName = _job.MindTryGetJobName(target);

        if (TryComp<MindComponent>(target, out var mind) && mind.OwnedEntity != null)
        {
            targetName = Name(mind.OwnedEntity.Value);
        }

        return Loc.GetString("objective-condition-other-traitor-progress-title", ("targetName", targetName), ("job", jobName));
    }

    private float GetProgress(EntityUid target)
    {
        var total = 0f; // how much progress they have
        var max = 0f; // how much progress is needed for 100%

        if (TryComp<MindComponent>(target, out var mind))
        {
            foreach (var objective in mind.AllObjectives)
            {
                foreach (var condition in objective.Conditions)
                {
                    max++; // things can only be up to 100% complete yeah

                    // this has the potential to loop forever, anything setting target has to check that there is no HelpProgressCondition.
                    var info = _objective.GetConditionInfo(condition, target, mind);
                    total += info.Progress ?? 0f;
                }
            }
        }

        // no objectives that can be helped with...
        if (max == 0f)
            return 1f;

        // require 50% completion for this one to be complete
        var completion = total / max;
        return completion >= 0.5f ? 1f : completion / 0.5f;
    }
}
