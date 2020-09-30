﻿using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.Materials;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class MaterialConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public StackType Material { get; private set; }
        public int Amount { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Material, "material", StackType.Metal);
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Next, insert [color=yellow]{0}[/color] sheets of [color=yellow]{1}[/color].", Amount, Material));
        }
    }
}
