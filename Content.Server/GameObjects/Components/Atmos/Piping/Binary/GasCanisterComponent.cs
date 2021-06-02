using Content.Server.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Binary
{
    [RegisterComponent]
    public class GasCanisterComponent : Component
    {
        public override string Name => "GasCanister";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
        public string PortName { get; set; } = "port";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("tank")]
        public string TankName { get; set; } = "tank";

        /// <summary>
        ///     Container name for the gas tank holder.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("container")]
        public string ContainerName { get; set; } = "GasCanisterTankHolder";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasMixture")]
        public GasMixture InitialMixture { get; } = new();

        /// <summary>
        ///     Stores the last pressure the tank had, for appearance-updating purposes.
        /// </summary>
        [ViewVariables]
        public float LastPressure { get; set; } = 0f;
    }
}
