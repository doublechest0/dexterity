using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Mech.Ui;

[GenerateTypedNameReferences]
public sealed partial class MechMenu : FancyWindow
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private EntityUid _mech;

    public MechMenu(EntityUid mech)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _mech = mech;

        if (!_ent.TryGetComponent<SpriteComponent>(mech, out var sprite))
            return;

        MechView.Sprite = sprite;

        if (!_ent.TryGetComponent<MechComponent>(mech, out var mechComp))
            return;

        foreach (var e in mechComp.EquipmentContainer.ContainedEntities)
        {
            if (!_ent.TryGetComponent<SpriteComponent>(e, out var spr) ||
                !_ent.TryGetComponent<MetaDataComponent>(e, out var me))
                continue;

            var foo = new MechEquipmentControl(me.EntityName, spr);
            StoreListingsContainer.AddChild(foo);
        }
    }
}

