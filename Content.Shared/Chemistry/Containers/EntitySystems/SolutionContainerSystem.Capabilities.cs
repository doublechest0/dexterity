using Content.Shared.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Solutions;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Content.Shared.Chemistry.Containers.EntitySystems;

public sealed partial class SolutionContainerSystem
{
    public bool TryGetInjectableSolution(EntityUid targetUid,
        [NotNullWhen(true)] out Solution? solution,
        InjectableSolutionComponent? injectable = null,
        SolutionContainerManagerComponent? manager = null
    )
    {
        if (!Resolve(targetUid, ref manager, ref injectable, false)
            || !TryGetSolution(targetUid, injectable.Solution, out solution, manager))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetRefillableSolution(EntityUid targetUid,
        [NotNullWhen(true)] out Solution? solution,
        SolutionContainerManagerComponent? solutionManager = null,
        RefillableSolutionComponent? refillable = null)
    {
        if (!Resolve(targetUid, ref solutionManager, ref refillable, false)
            || !TryGetSolution(targetUid, refillable.Solution, out var refillableSolution, solutionManager))
        {
            solution = null;
            return false;
        }

        solution = refillableSolution;
        return true;
    }

    public bool TryGetDrainableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        DrainableSolutionComponent? drainable = null,
        SolutionContainerManagerComponent? manager = null)
    {
        if (!Resolve(uid, ref drainable, ref manager, false)
            || !TryGetSolution(uid, drainable.Solution, out solution, manager))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetDumpableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        DumpableSolutionComponent? dumpable = null,
        SolutionContainerManagerComponent? manager = null)
    {
        if (!Resolve(uid, ref dumpable, ref manager, false)
            || !TryGetSolution(uid, dumpable.Solution, out solution, manager))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetDrawableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        DrawableSolutionComponent? drawable = null,
        SolutionContainerManagerComponent? manager = null)
    {
        if (!Resolve(uid, ref drawable, ref manager, false)
            || !TryGetSolution(uid, drawable.Solution, out solution, manager))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetFitsInDispenser(EntityUid owner,
        [NotNullWhen(true)] out Solution? solution,
        FitsInDispenserComponent? dispenserFits = null,
        SolutionContainerManagerComponent? solutionManager = null)
    {
        if (!Resolve(owner, ref dispenserFits, ref solutionManager, false)
            || !TryGetSolution(owner, dispenserFits.Solution, out solution, solutionManager))
        {
            solution = null;
            return false;
        }

        return true;
    }

    public bool TryGetMixableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {

        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solution = null;
            return false;
        }

        var getMixableSolutionAttempt = new GetMixableSolutionAttemptEvent(uid);
        RaiseLocalEvent(uid, ref getMixableSolutionAttempt);
        if (getMixableSolutionAttempt.MixedSolution != null)
        {
            solution = getMixableSolutionAttempt.MixedSolution;
            return true;
        }

        var tryGetSolution = EnumerateSolutions((uid, solutionsMgr)).FirstOrNull(x => x.Solution.CanMix);
        if (tryGetSolution.HasValue)
        {
            solution = tryGetSolution.Value.Solution;
            return true;
        }

        solution = null;
        return false;
    }


    public void Refill(EntityUid targetUid, Solution targetSolution, Solution addedSolution,
        RefillableSolutionComponent? refillableSolution = null)
    {
        if (!Resolve(targetUid, ref refillableSolution, false))
            return;

        _solutionSystem.TryAddSolution(targetUid, targetSolution, addedSolution);
    }

    public void Inject(EntityUid targetUid, Solution targetSolution, Solution addedSolution,
        InjectableSolutionComponent? injectableSolution = null)
    {
        if (!Resolve(targetUid, ref injectableSolution, false))
            return;

        _solutionSystem.TryAddSolution(targetUid, targetSolution, addedSolution);
    }

    public Solution Draw(EntityUid targetUid, Solution solution, FixedPoint2 amount,
        DrawableSolutionComponent? drawableSolution = null)
    {
        if (!Resolve(targetUid, ref drawableSolution, false))
            return new Solution();

        return _solutionSystem.SplitSolution(targetUid, solution, amount);
    }

    public Solution Drain(EntityUid targetUid, Solution targetSolution, FixedPoint2 amount,
        DrainableSolutionComponent? drainableSolution = null)
    {
        if (!Resolve(targetUid, ref drainableSolution, false))
            return new Solution();

        return _solutionSystem.SplitSolution(targetUid, targetSolution, amount);
    }


    public FixedPoint2 DrainAvailable(EntityUid uid)
    {
        return !TryGetDrainableSolution(uid, out var solution)
            ? FixedPoint2.Zero
            : solution.Volume;
    }

    public float PercentFull(EntityUid uid)
    {
        if (!TryGetDrainableSolution(uid, out var solution) || solution.MaxVolume.Equals(FixedPoint2.Zero))
            return 0;

        return solution.FillFraction * 100;
    }


    public static string ToPrettyString(Solution solution)
    {
        var sb = new StringBuilder();
        if (solution.Name == null)
            sb.Append("[");
        else
            sb.Append($"{solution.Name}:[");
        var first = true;
        foreach (var (id, quantity) in solution.Contents)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.Append(", ");
            }

            sb.AppendFormat("{0}: {1}u", id, quantity);
        }

        sb.Append(']');
        return sb.ToString();
    }
}
