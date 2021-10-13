using System;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Server)]
    public class CallShuttleCommand : IConsoleCommand
    {
        public string Command => "callshuttle";
        public string Description => Loc.GetString("call-shuttle-command-description");
        public string Help => Loc.GetString("call-shuttle-command-help-text", ("command",Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (args.Length == 1 && TimeSpan.TryParseExact(args[0], @"%m\:ss", loc.DefaultCulture, out var timeSpan))
            {
                EntitySystem.Get<RoundEndSystem>().RequestRoundEnd(timeSpan, false);
            }
            else if (args.Length == 1)
            {
                shell.WriteLine(Loc.GetString("shell-timespan-minutes-must-be-correct"));
            }
            else
            {
                EntitySystem.Get<RoundEndSystem>().RequestRoundEnd(false);
            }
        }
    }

    [AdminCommand(AdminFlags.Server)]
    public class RecallShuttleCommand : IConsoleCommand
    {
        public string Command => "recallshuttle";
        public string Description => Loc.GetString("recall-shuttle-command-description");
        public string Help => Loc.GetString("recall-shuttle-command-help-text", ("command",Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<RoundEndSystem>().CancelRoundEndCountdown(false);
        }
    }
}
