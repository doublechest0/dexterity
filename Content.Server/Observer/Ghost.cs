﻿using Content.Server.DamageSystem;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.DamageSystem;
using Content.Shared.GameObjects;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Timers;

namespace Content.Server.Observer
{
    public class Ghost : IClientCommand
    {
        public string Command => "ghost";
        public string Description => "Give up on life and become a ghost.";
        public string Help => "ghost";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Nah");
                return;
            }

            var mind = player.ContentData().Mind;
            var canReturn = player.AttachedEntity != null;
            var name = player.AttachedEntity?.Name ?? player.Name;

            if (player.AttachedEntity != null && player.AttachedEntity.HasComponent<GhostComponent>())
                return;

            if (mind.VisitingEntity != null)
            {
                mind.UnVisit();
                mind.VisitingEntity.Delete();
            }

            var position = player.AttachedEntity?.Transform.GridPosition ?? IoCManager.Resolve<IGameTicker>().GetObserverSpawnPoint();

            if (canReturn && player.AttachedEntity.TryGetComponent(out IDamageableComponent damageable))
            {
                switch (damageable.CurrentDamageState)
                {
                    case DamageState.Dead:
                        canReturn = true;
                        break;
                    case DamageState.Critical:
                        canReturn = true;
                        damageable.ChangeDamage(DamageType.Asphyxiation, 100, null, true); //todo: what if they dont breathe lol
                        break;
                    case DamageState.Alive:
                    default:
                        canReturn = false;
                        break;
                }
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.SpawnEntity("MobObserver", position);
            ghost.Name = mind.CharacterName;

            var ghostComponent = ghost.GetComponent<GhostComponent>();
            ghostComponent.CanReturnToBody = canReturn;

            if(canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
        }
    }
}
