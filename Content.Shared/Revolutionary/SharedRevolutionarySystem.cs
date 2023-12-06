using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Revolutionary;

public sealed class SharedRevolutionarySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _revIconGhostVisibility;
    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(CCVars.RevIconsVisibleToGhosts, value => _revIconGhostVisibility = value, true);

        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(MindShieldImplanted);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentGetStateAttemptEvent>(OnRevCompGetStateAttempt);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentGetStateAttemptEvent>(OnRevCompGetStateAttempt);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentStartup>(OnRevCompStartup);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentStartup>(OnHeadRevCompStartup);
        SubscribeLocalEvent<ShowRevIconsComponent, ComponentStartup>(OnShowRevIconsCompStartup);
    }

    /// <summary>
    /// When the mindshield is implanted in the rev it will popup saying they were deconverted. In Head Revs it will remove the mindshield component.
    /// </summary>
    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, MapInitEvent init)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemCompDeferred<MindShieldComponent>(uid);
            return;
        }

        if (HasComp<RevolutionaryComponent>(uid))
        {
            var stunTime = TimeSpan.FromSeconds(4);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryParalyze(uid, stunTime, true);
            _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
    }

    /// <summary>
    /// Determines if a HeadRev component should be sent to the client.
    /// </summary>
    private void OnRevCompGetStateAttempt(EntityUid uid, HeadRevolutionaryComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// Determines if a Rev component should be sent to the client.
    /// </summary>
    private void OnRevCompGetStateAttempt(EntityUid uid, RevolutionaryComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// The criteria that determine whether a Rev/HeadRev component should be sent to a client.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player is null)
            return true;

        var uid = player.AttachedEntity;

        if (HasComp<RevolutionaryComponent>(uid) || HasComp<HeadRevolutionaryComponent>(uid))
            return true;

        if (_revIconGhostVisibility && HasComp<GhostComponent>(uid))
            return true;

        return HasComp<ShowRevIconsComponent>(uid);
    }

    private void OnRevCompStartup (EntityUid uid, RevolutionaryComponent comp, ComponentStartup ev)
    {
        DirtyRevComps();
    }

    private void OnHeadRevCompStartup(EntityUid uid, HeadRevolutionaryComponent comp, ComponentStartup ev)
    {
        DirtyRevComps();
    }

    private void OnShowRevIconsCompStartup(EntityUid uid, ShowRevIconsComponent comp, ComponentStartup ev)
    {
        DirtyRevComps();
    }

    /// <summary>
    /// Dirties all the Rev components so they are sent to clients.
    ///
    /// We need to do this because if a rev component was not earlier sent to a client and for example the client
    /// becomes a rev then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    /// </summary>
    private void DirtyRevComps()
    {
        var revComps = EntityQueryEnumerator<RevolutionaryComponent>();
        while (revComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var headRevComps = EntityQueryEnumerator<HeadRevolutionaryComponent>();
        while (headRevComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }
}
