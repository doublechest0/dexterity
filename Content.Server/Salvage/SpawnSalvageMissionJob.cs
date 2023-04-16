using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.CPUJob.JobQueues;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Salvage;

public sealed class SpawnSalvageMissionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly ITileDefinitionManager _tileDefManager;
    private readonly BiomeSystem _biome;
    private readonly DungeonSystem _dungeon;
    private readonly SalvageSystem _salvage;

    public readonly EntityUid Station;
    private readonly SalvageMissionParams _missionParams;

    public SpawnSalvageMissionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        ITileDefinitionManager tileDefManager,
        BiomeSystem biome,
        DungeonSystem dungeon,
        SalvageSystem salvage,
        EntityUid station,
        SalvageMissionParams missionParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _tileDefManager = tileDefManager;
        _biome = biome;
        _dungeon = dungeon;
        _salvage = salvage;
        Station = station;
        _missionParams = missionParams;
    }

    protected override async Task<bool> Process()
    {
        Logger.DebugS("salvage", $"Spawning salvage mission with seed {_missionParams.Seed}");
        var config = _prototypeManager.Index<SalvageMissionPrototype>(_missionParams.Config);
        var mapId = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(mapId);
        _mapManager.AddUninitializedMap(mapId);
        MetaDataComponent? metadata = null;
        var grid = _entManager.EnsureComponent<MapGridComponent>(mapUid);
        var random = new Random(_missionParams.Seed);

        // Setup mission configs
        // As we go through the config the rating will deplete so we'll go for most important to least important.

        var mission = _entManager.System<SharedSalvageSystem>()
            .GetMission(_missionParams.Config, _missionParams.Difficulty, _missionParams.Seed);

        var missionBiome = _prototypeManager.Index<SalvageBiomeMod>(mission.Biome);

        if (missionBiome.BiomePrototype != null)
        {
            var biome = _entManager.AddComponent<BiomeComponent>(mapUid);
            var biomeSystem = _entManager.System<BiomeSystem>();
            biomeSystem.SetPrototype(biome, mission.Biome);
            biomeSystem.SetSeed(biome, mission.Seed);
            _entManager.Dirty(biome);

            // Gravity
            var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
            gravity.Enabled = true;
            _entManager.Dirty(gravity, metadata);

            // Atmos
            var atmos = _entManager.EnsureComponent<MapAtmosphereComponent>(mapUid);
            atmos.Space = false;
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;

            atmos.Mixture = new GasMixture(2500)
            {
                Temperature = 293.15f,
                Moles = moles,
            };

            if (mission.Color != null)
            {
                var lighting = _entManager.EnsureComponent<MapLightComponent>(mapUid);
                lighting.AmbientLightColor = mission.Color.Value;
                _entManager.Dirty(lighting);
            }
        }

        _mapManager.DoMapInitialize(mapId);
        _mapManager.SetMapPaused(mapId, true);

        // Setup expedition
        var expedition = _entManager.AddComponent<SalvageExpeditionComponent>(mapUid);
        expedition.Station = Station;
        expedition.EndTime = _timing.CurTime + mission.Duration;
        expedition.MissionParams = _missionParams;

        var ftlUid = _entManager.SpawnEntity("FTLPoint", new EntityCoordinates(mapUid, Vector2.Zero));
        _entManager.GetComponent<MetaDataComponent>(ftlUid).EntityName = SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>(config.NameProto), _missionParams.Seed);

        var landingPadRadius = 24;
        var minDungeonOffset = landingPadRadius + 32;
        var maxDungeonOffset = minDungeonOffset + 32;

        var dungeonOffsetDistance = (minDungeonOffset + (maxDungeonOffset - minDungeonOffset) * random.NextFloat());
        var dungeonOffset = new Vector2(dungeonOffsetDistance, 0f);
        dungeonOffset = new Angle(random.NextDouble() * Math.Tau).RotateVec(dungeonOffset);
        var dungeonConfig = _prototypeManager.Index<DungeonConfigPrototype>(mission.Dungeon);
        var dungeon =
            await WaitAsyncTask(_dungeon.GenerateDungeonAsync(dungeonConfig, mapUid, grid, (Vector2i) dungeonOffset,
                _missionParams.Seed));

        // Aborty
        if (dungeon.Rooms.Count == 0)
        {
            return false;
        }

        // Handle loot
        foreach (var loot in mission.Loot)
        {
            var lootProto = _prototypeManager.Index<SalvageLootPrototype>(loot);
            await SpawnDungeonLoot(dungeon, lootProto, mapUid, grid, random);
        }

        // Setup the landing pad
        var landingPadExtents = new Vector2i(landingPadRadius, landingPadRadius);
        var tiles = new List<(Vector2i Indices, Tile Tile)>(landingPadExtents.X * landingPadExtents.Y * 2);

        // Set the tiles themselves
        var seed = new FastNoiseLite(_missionParams.Seed);
        var landingTile = new Tile(_tileDefManager["FloorSteel"].TileId);

        foreach (var tile in grid.GetTilesIntersecting(new Circle(Vector2.Zero, landingPadRadius), false))
        {
            if (!_biome.TryGetBiomeTile(mapUid, grid, seed, tile.GridIndices, out _))
                continue;

            tiles.Add((tile.GridIndices, landingTile));
        }

        grid.SetTiles(tiles);

        await SetupMission(mission.Mission, mission, (Vector2i) dungeonOffset, dungeon, grid, random, seed.GetSeed());
        return true;
    }

    private async Task SpawnDungeonLoot(Dungeon dungeon, SalvageLootPrototype loot, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        foreach (var rule in loot.LootRules)
        {
            switch (rule)
            {
                // Spawns a cluster (like an ore vein) nearby.
                case ClusterLoot cluster:
                    // TODO: Copy code from old PR.
                    break;
            }
        }
    }

    #region Mission Specific

    private async Task SetupMission(string missionMod, SalvageMission mission, Vector2i dungeonOffset, Dungeon dungeon, MapGridComponent grid, Random random, int seed)
    {
        switch (missionMod)
        {
            case "Structure":
                await SetupStructure(mission, dungeonOffset, dungeon, grid, random, seed);
                return;
            default:
                return;
        }
    }

    private async Task SetupStructure(SalvageMission mission, Vector2i dungeonOffset, Dungeon dungeon, MapGridComponent grid, Random random, int seed)
    {
        var structureComp = _entManager.GetComponent<SalvageStructureExpeditionComponent>(grid.Owner);
        var availableRooms = dungeon.Rooms.ToList();
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        var groupSpawns = (int) mission.Difficulty + mission.RemainingDifficulty;

        for (var i = 0; i < groupSpawns; i++)
        {
            var mobGroupIndex = random.Next(faction.MobGroups.Count);
            var mobGroup = faction.MobGroups[mobGroupIndex];

            var spawnRoomIndex = random.Next(dungeon.Rooms.Count);
            var spawnRoom = dungeon.Rooms[spawnRoomIndex];
            var spawnTile = spawnRoom.Tiles.ElementAt(random.Next(spawnRoom.Tiles.Count));
            spawnTile += dungeonOffset;
            var spawnPosition = grid.GridTileToLocal(spawnTile);

            foreach (var entry in EntitySpawnCollection.GetSpawns(mobGroup.Entries, random))
            {
                _entManager.SpawnEntity(entry, spawnPosition);
            }

            await SuspendIfOutOfTime();
        }

        var structureCount = _salvage.GetStructureCount(mission.Difficulty);
        var shaggy = faction.Configs["DefenseStructure"];

        // Spawn the objectives
        for (var i = 0; i < structureCount; i++)
        {
            var structureRoom = availableRooms[random.Next(availableRooms.Count)];
            var spawnTile = structureRoom.Tiles.ElementAt(random.Next(structureRoom.Tiles.Count)) + dungeonOffset;
            var uid = _entManager.SpawnEntity(shaggy, grid.GridTileToLocal(spawnTile));
            structureComp.Structures.Add(uid);
        }
    }

    #endregion
}
