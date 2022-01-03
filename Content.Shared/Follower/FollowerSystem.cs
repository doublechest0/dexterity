﻿using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Shared.Follower;

public class FollowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetAlternativeVerbsEvent>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<FollowerComponent, RelayMoveInputEvent>(OnFollowerMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var follower in EntityManager.EntityQuery<FollowerComponent>())
        {
            var xform = Transform(follower.Owner);
            var targetXform = Transform(follower.Following);
            xform.Coordinates = targetXform.Coordinates;
        }
    }

    private void OnGetAlternativeVerbs(GetAlternativeVerbsEvent ev)
    {
        if (!HasComp<SharedGhostComponent>(ev.User))
            return;

        var verb = new Verb
        {
            Priority = 10,
            Act = (() =>
            {
                var follower = EnsureComp<FollowerComponent>(ev.User);
                follower.Following = ev.Target;
            }),
            Impact = LogImpact.Low,
            Text = Loc.GetString("verb-follow-text"),
            IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png",
        };

        ev.Verbs.Add(verb);
    }

    private void OnFollowerMove(EntityUid uid, FollowerComponent component, RelayMoveInputEvent args)
    {
        RemComp<FollowerComponent>(uid);
    }
}
