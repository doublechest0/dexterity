using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Glue;

[RegisterComponent]
public sealed class GluedComponent : Component
{
    /// <summary>
    /// Reverts name to before prefix event (essentially removes prefix).
    /// </summary>
    [DataField("beforeGluedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeGluedEntityName = string.Empty;

    [DataField("until", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Until;

    [DataField("duration", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration;
}
