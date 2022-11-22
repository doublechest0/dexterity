using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechBoundUserInterface : BoundUserInterface
{
    private EntityUid _mech;

    private MechMenu? _menu;

    public MechBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        _mech = owner.Owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(_mech);

        _menu.OnClose += Close;
        _menu?.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Close();
    }
}

