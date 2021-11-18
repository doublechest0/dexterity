﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Administration.Logs.Converters;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Shared.Administration.Logs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

namespace Content.Server.Administration.Logs;

public class AdminLogSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private ISawmill _log = default!;
    private JsonSerializerOptions _jsonOptions = default!;

    private int _roundId;

    public override void Initialize()
    {
        base.Initialize();

        _log = _logManager.GetSawmill("admin.logs");
        _jsonOptions = new JsonSerializerOptions();

        foreach (var converter in _reflection.FindTypesWithAttribute<AdminLogConverterAttribute>())
        {
            var instance = _typeFactory.CreateInstance<JsonConverter>(converter);
            _jsonOptions.Converters.Add(instance);
        }

        var converterNames = _jsonOptions.Converters.Select(converter => converter.GetType().Name);
        _log.Info($"Admin log converters found: {string.Join(" ", converterNames)}");

        SubscribeLocalEvent<RoundStartingEvent>(RoundStarting);
    }

    private void RoundStarting(RoundStartingEvent ev)
    {
        _roundId = ev.RoundId;
    }

    private async void Add(LogType type, string message, JsonDocument json, List<Guid> players)
    {
        // TODO ADMIN LOGGING batch all these adds per tick
        await _db.AddAdminLog(_roundId, type, message, json, players);
        json.Dispose();
    }

    public void Add(LogType type, ref LogStringHandler handler)
    {
        var (json, players) = handler.ToJson(_jsonOptions, _entities);
        var message = handler.ToStringAndClear();

        Add(type, message, json, players);
    }

    public IAsyncEnumerable<LogRecord> All(LogFilter? filter = null)
    {
        return _db.GetAdminLogs(filter);
    }

    public IAsyncEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        return _db.GetAdminLogMessages(filter);
    }

    public Task<Round> Round(int roundId)
    {
        return _db.GetRound(roundId);
    }

    public IAsyncEnumerable<LogRecord> CurrentRoundLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _roundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _roundId;
        return AllMessages(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(_roundId);
    }
}
