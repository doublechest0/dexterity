using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class FlashComponent : Component
    {
        public override string Name => "Flash";

        [DataField("duration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int FlashDuration => 5000;

        [DataField("uses")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Uses { get; set; } = 5;

        [DataField("range")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Range => 7f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("aoeFlashDuration")]
        public int AoeFlashDuration => 2000;

        [DataField("slowTo")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowTo => 0.5f;

        public bool Flashing;

        public bool HasUses => Uses > 0;
    }
}
