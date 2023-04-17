using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage;

[Serializable, NetSerializable]
public sealed class SalvageExpeditionConsoleState : BoundUserInterfaceState
{
    public TimeSpan NextOffer;
    public bool Claimed;
    public bool Cooldown;
    public ushort ActiveMission;
    public List<SalvageMissionParams> Missions;

    public SalvageExpeditionConsoleState(TimeSpan nextOffer, bool claimed, bool cooldown, ushort activeMission, List<SalvageMissionParams> missions)
    {
        NextOffer = nextOffer;
        Claimed = claimed;
        Cooldown = cooldown;
        ActiveMission = activeMission;
        Missions = missions;
    }
}

/// <summary>
/// Used to interact with salvage expeditions and claim them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class SalvageExpeditionConsoleComponent : Component
{

}

[Serializable, NetSerializable]
public sealed class ClaimSalvageMessage : BoundUserInterfaceMessage
{
    public ushort Index;
}

/// <summary>
/// Added per station to store data on their available salvage missions.
/// </summary>
[RegisterComponent]
public sealed class SalvageExpeditionDataComponent : Component
{
    /// <summary>
    /// Is there an active salvage expedition.
    /// </summary>
    [ViewVariables]
    public bool Claimed => ActiveMission != 0;

    /// <summary>
    /// Are we actively cooling down from the last salvage mission.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public bool Cooldown = false;

    /// <summary>
    /// Nexy time salvage missions are offered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextOffer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextOffer;

    [ViewVariables]
    public readonly Dictionary<ushort, SalvageMissionParams> Missions = new();

    [ViewVariables] public ushort ActiveMission;

    public ushort NextIndex = 1;
}

[Serializable, NetSerializable]
public sealed record SalvageMissionParams
{
    [ViewVariables]
    public ushort Index;

    [ViewVariables(VVAccess.ReadWrite), DataField("config", required: true, customTypeSerializer:typeof(SalvageMissionPrototype))]
    public string Config = default!;

    [ViewVariables(VVAccess.ReadWrite)] public int Seed;

    /// <summary>
    /// Base difficulty for this mission.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public DifficultyRating Difficulty;
}

/// <summary>
/// Created from <see cref="SalvageMissionParams"/>. Only needed for data the client also needs for mission
/// display.
/// </summary>
public sealed record SalvageMission(
    int Seed,
    DifficultyRating Difficulty,
    float RemainingDifficulty,
    string Dungeon,
    string Faction,
    string Mission,
    string Biome,
    Color? Color,
    TimeSpan Duration,
    List<string> Loot,
    List<string> Modifiers)
{
    /// <summary>
    /// Seed used for the mission.
    /// </summary>
    public readonly int Seed = Seed;

    /// <summary>
    /// Base difficulty rating.
    /// </summary>
    public DifficultyRating Difficulty = Difficulty;

    public float RemainingDifficulty = RemainingDifficulty;

    /// <summary>
    /// <see cref="SalvageDungeonMod"/> to be used.
    /// </summary>
    public readonly string Dungeon = Dungeon;

    /// <summary>
    /// <see cref="SalvageFactionPrototype"/> to be used.
    /// </summary>
    public readonly string Faction = Faction;

    /// <summary>
    /// Underlying mission params that generated this.
    /// </summary>
    public readonly string Mission = Mission;

    /// <summary>
    /// Biome to be used for the mission.
    /// </summary>
    public readonly string Biome = Biome;

    /// <summary>
    /// Lighting color to be used (AKA outdoor lighting).
    /// </summary>
    public readonly Color? Color = Color;

    /// <summary>
    /// Mission duration.
    /// </summary>
    public TimeSpan Duration = Duration;

    public List<string> Loot = Loot;

    /// <summary>
    /// Modifiers (outside of the above) applied to the mission.
    /// </summary>
    public List<string> Modifiers = Modifiers;
}

[Serializable, NetSerializable]
public enum SalvageEnvironment : byte
{
    Invalid = 0,
    Caves,
}

[Serializable, NetSerializable]
public enum SalvageConsoleUiKey : byte
{
    Expedition,
}
