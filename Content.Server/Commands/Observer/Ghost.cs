﻿#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Observer
{
    [AnyCommand]
    public class Ghost : IServerCommand
    {
        public string Command => "ghost";
        public string Description => "Give up on life and become a ghost.";
        public string Help => "ghost";
        public bool CanReturn { get; set; } = true;

        public void Execute(IServerConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell?.WriteLine("You have no session, you can't ghost.");
                return;
            }

            var mind = player.ContentData()?.Mind;
            if (mind == null)
            {
                shell?.WriteLine("You have no Mind, you can't ghost.");
                return;
            }

            if (!IoCManager.Resolve<IGameTicker>().OnGhostAttempt(mind, CanReturn))
            {
                shell?.WriteLine("You can't ghost right now.");
                return;
            }
        }
    }
}
