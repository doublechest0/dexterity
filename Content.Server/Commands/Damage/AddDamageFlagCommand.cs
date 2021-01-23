﻿#nullable enable
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Commands.Damage
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddDamageFlagCommand : DamageFlagCommand
    {
        public override string Command => "adddamageflag";
        public override string Description => "Adds a damage flag to your entity or another.";
        public override string Help => $"Usage: {Command} <flag> / {Command} <entityUid> <flag>";

        public override void Execute(IServerConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (!TryGetEntity(shell, player, args, true, out var entity, out var flag, out var damageable))
            {
                return;
            }

            damageable.AddFlag(flag);
            shell.WriteLine($"Added damage flag {flag} to entity {entity.Name}");
        }
    }
}
