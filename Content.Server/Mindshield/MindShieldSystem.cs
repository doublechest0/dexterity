using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Database;
using Content.Server.Administration.Logs;

namespace Content.Server.Mindshield;
/// <summary>
/// System used for checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindShieldComponent, ComponentInit>(MindShieldAdded);
    }

    /// <summary>
    /// When the MindShield is added this will trigger to check if the implanted is a Rev or Head Rev and will remove Rev or "destroy" implant respectively.
    /// </summary>
    private void MindShieldAdded(EntityUid uid, MindShieldComponent comp, ComponentInit init)
    {
        if (HasComp<RevolutionaryComponent>(uid) && !HasComp<HeadRevolutionaryComponent>(uid))
        {
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to being implanted with a Mindshield.");

        }
        else if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemComp<MindShieldComponent>(uid);
            _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), uid);
        }
    }
}
