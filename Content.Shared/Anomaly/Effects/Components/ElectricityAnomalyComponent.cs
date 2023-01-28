﻿namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed class ElectricityAnomalyComponent : Component
{
    /// <summary>
    /// The maximum radius of the passive electrocution effect
    /// scales with stability
    /// </summary>
    [DataField("maxElectrocutionRange"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxElectrocuteRange = 7f;

    /// <summary>
    /// The maximum amount of damage the electrocution can do
    /// scales with severity
    /// </summary>
    [DataField("maxElectrocuteDamage"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxElectrocuteDamage = 20f;

    /// <summary>
    /// The maximum amount of time the electrocution lasts
    /// scales with severity
    /// </summary>
    [DataField("maxElectrocuteDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxElectrocuteDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The maximum chance that each second, when in range of the anomaly, you will be electrocuted.
    /// scales with stability
    /// </summary>
    [DataField("passiveElectrocutionChance"), ViewVariables(VVAccess.ReadWrite)]
    public float PassiveElectrocutionChance = 0.05f;
}
