﻿using Content.Server.Mining.Components;
using Content.Server.Stack;
using Content.Shared.Destructible;
using Content.Shared.Mining;
using Content.Shared.Prototypes;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Mining;

/// <summary>
/// This handles creating ores when the entity is destroyed.
/// </summary>
public sealed class MiningSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OreVeinComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OreVeinComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnDestruction(EntityUid uid, OreVeinComponent component, DestructionEventArgs args)
    {
        if (component.CurrentOre == null)
            return;

        if (!_proto.TryIndex<OrePrototype>(component.CurrentOre, out var proto))
            return;

        if (proto.OreEntity == null)
            return;

        var coords = Transform(uid).Coordinates;
        var toSpawn = _random.Next(proto.MinOreYield, proto.MaxOreYield);
        var oreEntity = _proto.Index<EntityPrototype>(proto.OreEntity);
        if (oreEntity.HasComponent<StackComponent>())
        {
            while (toSpawn > 0)
            {
                var ent = Spawn(proto.OreEntity, coords.Offset(_random.NextVector2(0.3f)));
                var stack = EntityManager.GetComponent<StackComponent>(ent);
                var amountOnStack = Math.Min(stack.MaxCount, toSpawn);
                _stack.SetCount(ent, amountOnStack, stack);
                toSpawn -= amountOnStack;
            }
        }
        else
        {
            for (var i = 0; i < toSpawn; i++)
            {
                Spawn(proto.OreEntity, coords.Offset(_random.NextVector2(0.3f)));
            }
        }
    }

    private void OnMapInit(EntityUid uid, OreVeinComponent component, MapInitEvent args)
    {
        if (component.CurrentOre != null || component.OreRarityPrototypeId == null || !_random.Prob(component.OreChance))
            return;

        component.CurrentOre = _proto.Index<WeightedRandomPrototype>(component.OreRarityPrototypeId).Pick(_random);
    }
}
