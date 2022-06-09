﻿using Content.Server.Database;
using Content.Server.RoleTimers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class GetRoleTimerCommand : IConsoleCommand
    {
        public string Command => "getroletimers";
        public string Description => "Gets all or one role timers from a player";
        public string Help => $"Usage: {Command} <name or user ID> [from database]";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();

            var target = args[0];
            IPlayerSession targetSessionInst;
            if (playerManager.TryGetSessionByUsername(target, out var targetSession))
            {
                targetSessionInst = targetSession;
            }
            else
            {
                shell.WriteLine("Not a valid player");
                return;
            }

            if (args.Length >= 2)
            {
                bool.TryParse(args[1], out var useDb);
                if (useDb)
                {
                    var db = IoCManager.Resolve<IServerDbManager>();
                    var timers = await db.GetRoleTimers(targetSessionInst.UserId);
                    foreach (var timer in timers)
                    {
                        shell.WriteLine($"Role: {timer.Role}, Playtime: {timer.TimeSpent}");
                    }
                }
                else
                {
                    var rt = IoCManager.Resolve<RoleTimerSystem>();
                    var timers = rt.GetCachedRoleTimers(targetSessionInst);
                    if (timers == null)
                    {
                        shell.WriteLine("Couldn't get any information from cache (player info may not be cached yet)");
                        return;
                    }

                    foreach (var timer in timers)
                    {
                        shell.WriteLine($"Role: {timer.Key}, Playtime: {timer.Value.Item2}");
                    }
                }
            }
        }
    }
}
