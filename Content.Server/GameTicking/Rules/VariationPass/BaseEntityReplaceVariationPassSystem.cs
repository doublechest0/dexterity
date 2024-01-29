﻿using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="EntityReplaceVariationPassComponent"/>
/// <summary>
///     A base system for fast replacement of entities utilizing a query, rather than having to iterate every entity
///     To use, you must have a marker component to use for <see cref="TEntComp"/>--each replaceable entity must have it
///     Then you need an inheriting system as well as a unique game rule component for <see cref="TGameRuleComp"/>
///
///     This means a bit more boilerplate for each one, but significantly faster to actually execute.
///     See <see cref="WallReplaceVariationPassSystem"/>
/// </summary>
public abstract class BaseEntityReplaceVariationPassSystem<TEntComp, TGameRuleComp> : VariationPassSystem<TGameRuleComp>
    where TEntComp: IComponent
    where TGameRuleComp: IComponent
{
    protected override void ApplyVariation(Entity<TGameRuleComp> ent, ref StationVariationPassEvent args)
    {
        if (!TryComp<EntityReplaceVariationPassComponent>(ent, out var pass))
            return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var replacementMod = Random.NextGaussian(pass.EntitiesPerReplacementAverage, pass.EntitiesPerReplacementStdDev);
        var prob = (float) Math.Clamp(1 / replacementMod, 0f, 1f);

        if (prob == 0)
            return;

        var enumerator = AllEntityQuery<TEntComp, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var xform))
        {
            if (!IsMemberOfStation((uid, xform), ref args))
                continue;

            if (RobustRandom.Prob(prob))
                Replace((uid, xform), pass.Replacements);
        }

        Log.Debug($"Entity replacement took {stopwatch.Elapsed} with {Stations.GetTileCount(args.Station)} tiles");
    }

    private void Replace(Entity<TransformComponent> ent, List<EntitySpawnEntry> replacements)
    {
        var coords = ent.Comp.Coordinates;
        var rot = ent.Comp.LocalRotation;
        QueueDel(ent);

        foreach (var spawn in EntitySpawnCollection.GetSpawns(replacements, RobustRandom))
        {
            var newEnt = Spawn(spawn, coords);
            Transform(newEnt).LocalRotation = rot;
        }
    }
}
