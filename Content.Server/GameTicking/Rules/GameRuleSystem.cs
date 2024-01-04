using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] protected readonly IChatManager ChatManager = default!;
    [Dependency] protected readonly GameTicker GameTicker = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    private List<GameRuleTask<T>> _scheduledTasks = new List<GameRuleTask<T>>();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<T, GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<T, GameRuleEndedEvent>(OnGameRuleEnded);
    }

    private void OnGameRuleAdded(EntityUid uid, T component, ref GameRuleAddedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Added(uid, component, ruleData, args);
    }

    private void OnGameRuleStarted(EntityUid uid, T component, ref GameRuleStartedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Started(uid, component, ruleData, args);
    }

    private void OnGameRuleEnded(EntityUid uid, T component, ref GameRuleEndedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Ended(uid, component, ruleData, args);
    }


    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule begins
    /// </summary>
    protected virtual void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule ends
    /// </summary>
    protected virtual void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {

    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected virtual void ActiveTick(EntityUid uid, T component, GameRuleComponent gameRule, float frameTime)
    {
        var toRemove = new List<GameRuleTask<T>>();
        var now = Timing.CurTime;
        foreach (var task in _scheduledTasks)
        {
            if (task.NextRunTime <= now)
            {
                task.Action(uid, component, gameRule, frameTime);
                if (task.Oneshot)
                {
                    toRemove.Add(task);
                }
                else
                {
                    if (!task.Interval.HasValue)
                    {
                        toRemove.Add(task);
                    }
                    else
                    {
                        task.NextRunTime = now + task.Interval.Value;
                    }
                }
            }
        }

        //Remove expired tasks
        foreach (var taskToRemove in toRemove)
            _scheduledTasks.Remove(taskToRemove);
    }

    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected bool TryRoundStartAttempt(RoundStartAttemptEvent ev, string localizedPresetName)
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return false;

            if (ev.Players.Length == 0)
            {
                ChatManager.DispatchServerAnnouncement(Loc.GetString("preset-no-one-ready", ("presetName", localizedPresetName)));
                ev.Cancel();
                continue;
            }

            var minPlayers = gameRule.MinPlayers;
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                ChatManager.SendAdminAnnouncement(Loc.GetString("preset-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", minPlayers),
                    ("presetName", localizedPresetName)));
                ev.Cancel();
                continue;
            }
        }

        return !ev.Cancelled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp2))
                continue;

            ActiveTick(uid, comp1, comp2, frameTime);
        }
    }

    /// <summary>
    /// Schedule a task to run repeatedly every interval
    /// </summary>
    /// <param name="task">An action accepting : Rule entity, T Component, GameRuleComponent and frameTime</param>
    /// <param name="interval">Timespan specifying the interval to run the task</param>
    public void ScheduleRecurringTask(Action<EntityUid, T, GameRuleComponent, float> task, TimeSpan interval)
    {
        var now = Timing.CurTime;
        _scheduledTasks.Add(new GameRuleTask<T>(task, now + interval, false, interval));
    }
    /// <summary>
    /// Schedule a task to be run once after a delay
    /// </summary>
    /// <param name="task">An action accepting : Rule entity, T Component, GameRuleComponent and frameTime</param>
    /// <param name="delay">Timespan specifying how long to delay before running the task</param>
    public void ScheduleOneshotTask(Action<EntityUid, T, GameRuleComponent, float> task, TimeSpan delay)
    {
        var now = Timing.CurTime;
        _scheduledTasks.Add(new GameRuleTask<T>(task, now + delay, true));
    }

    private sealed class GameRuleTask<T2> where T2 : IComponent
    {
        public Action<EntityUid, T2, GameRuleComponent, float> Action { get; private set; }
        public TimeSpan? Interval { get; private set; }
        public bool Oneshot { get; private set; }
        public TimeSpan NextRunTime { get; set; }

        public GameRuleTask(Action<EntityUid, T2, GameRuleComponent, float> action, TimeSpan nextRunTime, bool oneshot = false, TimeSpan? interval = null)
        {
            Action = action;
            Interval = interval;
            Oneshot = oneshot;
            NextRunTime = nextRunTime;
        }
    }
}
