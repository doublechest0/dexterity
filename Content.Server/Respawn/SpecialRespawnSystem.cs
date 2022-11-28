﻿using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Maps;
using Content.Shared.Respawn;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Respawn;

public sealed class SpecialRespawnSystem : SharedSpecialRespawnSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<SpecialRespawnComponent, SpecialRespawnSetupEvent>(OnSpecialRespawnSetup);
        SubscribeLocalEvent<SpecialRespawnComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SpecialRespawnComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        //Try to compensate for restartroundnow command
        if (ev.Old == GameRunLevel.InRound && ev.New == GameRunLevel.PreRoundLobby)
            OnRoundEnd();

        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                OnRoundEnd();
                break;
        }
    }

    private void OnSpecialRespawnSetup(EntityUid uid, SpecialRespawnComponent component, SpecialRespawnSetupEvent ev)
    {
        var originStation = _stationSystem.GetOwningStation(uid);
        var xform = Transform(uid);

        if (originStation != null)
            component.Station = originStation.Value;

        if (xform.GridUid != null)
            component.StationMap = (xform.MapUid, xform.GridUid);
    }

    private void OnRoundEnd()
    {
        var specialRespawnQuery = EntityQuery<SpecialRespawnComponent>();

        //Turn respawning off so the entity doesn't respawn during reset
        foreach (var entity in specialRespawnQuery)
        {
            entity.Respawn = false;
        }
    }

    private void OnStartup(EntityUid uid, SpecialRespawnComponent component, ComponentStartup args)
    {
        var ev = new SpecialRespawnSetupEvent();
        RaiseLocalEvent(uid, ev);
    }

    private void OnShutdown(EntityUid uid, SpecialRespawnComponent component, ComponentShutdown args)
    {
        var entityMapUid = component.StationMap.Item1;
        var entityGridUid = component.StationMap.Item2;

        if (!component.Respawn || !HasComp<StationMemberComponent>(entityGridUid) || entityMapUid == null)
            return;

        if (TryFindRandomTile(entityGridUid.Value, entityMapUid.Value, 10, out var coords))
        {
            Respawn(component.Prototype, coords);
        }

        //If the above fails, spawn at the center of the grid on the station
        else
        {
            var xform = Transform(entityGridUid.Value);
            var pos = xform.Coordinates;
            var mapPos = xform.MapPosition;
            var circle = new Circle(mapPos.Position, 2);

            if (!_mapManager.TryGetGrid(entityGridUid.Value, out var grid))
                return;

            var found = false;

            foreach (var tile in grid.GetTilesIntersecting(circle))
            {
                if (tile.IsSpace(_tileDefinitionManager) || tile.IsBlockedTurf(true) || !_atmosphere.IsTileMixtureProbablySafe(entityGridUid, entityMapUid.Value, grid.TileIndicesFor(mapPos)))
                    continue;

                pos = tile.GridPosition();
                found = true;

                if (found)
                    break;
            }

            Respawn(component.Prototype, pos);
        }
    }

    /// <summary>
    /// Respawn the entity and log it.
    /// </summary>
    /// <param name="prototype">The prototype being spawned</param>
    /// <param name="coords">The place where it will be spawned</param>
    private void Respawn(string prototype, EntityCoordinates coords)
    {
        var entity = Spawn(prototype, coords);
        var name = MetaData(entity).EntityName;
        _adminLog.Add(LogType.Respawn, LogImpact.High, $"{name} was deleted and was respawned at {coords.ToMap(EntityManager)}");
    }

        /// <summary>
    /// Try to find a random safe tile on the supplied grid
    /// </summary>
    /// <param name="targetGrid">The grid that you're looking for a safe tile on</param>
    /// <param name="targetMap">The map that you're looking for a safe tile on</param>
    /// <param name="maxAttempts">The maximum amount of attempts it should try before it gives up</param>
    /// <param name="targetCoords">If successful, the coordinates of the safe tile</param>
    /// <returns></returns>
    public bool TryFindRandomTile(EntityUid targetGrid, EntityUid targetMap, int maxAttempts, out EntityCoordinates targetCoords)
    {
        targetCoords = EntityCoordinates.Invalid;

        if (!_mapManager.TryGetGrid(targetGrid, out var grid))
            return false;

        var xform = Transform(targetGrid);

        if (!grid.TryGetTileRef(xform.Coordinates, out var tileRef))
            return false;

        var tile = tileRef.GridIndices;

        var found = false;
        var (gridPos, _, gridMatrix) = xform.GetWorldPositionRotationMatrix();
        var gridBounds = gridMatrix.TransformBox(grid.LocalAABB);

        //Obviously don't put anything ridiculous in here
        for (var i = 0; i < maxAttempts; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            var mapPos = grid.GridTileToWorldPos(tile);
            var mapTarget = grid.WorldToTile(mapPos);
            var circle = new Circle(mapPos, 2);

            foreach (var newTileRef in grid.GetTilesIntersecting(circle))
            {
                if (newTileRef.IsSpace(_tileDefinitionManager) || newTileRef.IsBlockedTurf(true) || !_atmosphere.IsTileMixtureProbablySafe(targetGrid, targetMap, mapTarget))
                    continue;

                found = true;
                targetCoords = grid.GridTileToLocal(tile);
                break;
            }

            //Found a safe tile, no need to continue
            if (found)
                break;
        }

        if (!found)
            return false;

        return true;
    }
}
