using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Server.Examine;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Damage.Systems;

public sealed class DamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
        SubscribeLocalEvent<DamageOtherOnHitComponent, GetVerbsEvent<ExamineVerb>>(OnExamine);
    }

    private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
    {
        var dmg = _damageableSystem.TryChangeDamage(args.Target, component.Damage, component.IgnoreResistances, origin: args.Component.Thrower);

        // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
        if (dmg != null && HasComp<MobStateComponent>(args.Target))
            _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.Total:damage} damage from collision");

        RaiseNetworkEvent(new ColorFlashEffectEvent(Color.Red, new List<EntityUid> { args.Target }), Filter.Pvs(args.Target, entityManager: EntityManager));
        _guns.PlayImpactSound(args.Target, dmg, null, false);
        if (TryComp<PhysicsComponent>(uid, out var body) && body.LinearVelocity.LengthSquared() > 0f)
        {
            var direction = body.LinearVelocity.Normalized();
            _sharedCameraRecoil.KickCamera(args.Target, direction);
        }

        _thrownItem.LandComponent(args.Thrown, args.Component, playSound: false);
    }

    private void OnExamine(EntityUid uid, DamageOtherOnHitComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = _damageable.GetDamageExamine(component.Damage, Loc.GetString("damage-throw"));
                _examine.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("damage-examinable-verb-text"),
            Message = Loc.GetString("damage-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }
}
