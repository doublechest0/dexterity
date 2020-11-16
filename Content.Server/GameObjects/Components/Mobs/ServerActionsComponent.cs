﻿using System;
using Content.Server.Actions;
using Content.Server.Commands;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ServerActionsComponent : SharedActionsComponent
    {

        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override ComponentState GetComponentState()
        {
            return new ActionComponentState(CreateActionStatesArray());
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!(message is PerformActionMessage performMsg)) return;

            var player = session.AttachedEntity;
            if (player != Owner) return;

            if (!TryGetGrantedActionState(performMsg.ActionType, out var actionState))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " action {1} which is not granted to them", player.Name,
                    performMsg.ActionType);
            }

            if (!ActionManager.TryGet(performMsg.ActionType, out var action))
            {
                Logger.DebugS("action", "user {0} attempted to" +
                                        " perform unrecognized instant action {1}", player.Name,
                    performMsg.ActionType);
                return;
            }

            switch (performMsg)
            {
                case PerformInstantActionMessage msg:
                    if (action.InstantAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as an instant action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    action.InstantAction.DoInstantAction(new InstantActionEventArgs(player));

                    break;
                case PerformToggleActionMessage msg:
                    if (action.ToggleAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a toggle action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (msg.ToggleOn == actionState.ToggledOn)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " toggle action {1} to {2}, but it is already toggled {2}", player.Name,
                            msg.ActionType, actionState.ToggledOn ? "on" : "off");
                        return;
                    }

                    ToggleAction(action.ActionType, msg.ToggleOn);

                    action.ToggleAction.DoToggleAction(new ToggleActionEventArgs(player, msg.ToggleOn));
                    break;
                case PerformTargetPointActionMessage msg:
                    if (action.TargetPointAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target point action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (ActionBlockerSystem.CanChangeDirection(player))
                    {
                        var diff = msg.Target.ToMapPos(_entityManager) - player.Transform.MapPosition.Position;
                        if (diff.LengthSquared > 0.01f)
                        {
                            player.Transform.LocalRotation = new Angle(diff);
                        }
                    }

                    action.TargetPointAction.DoTargetPointAction(new TargetPointActionEventArgs(player, msg.Target));
                    break;
                case PerformTargetEntityActionMessage msg:
                    if (action.TargetEntityAction == null)
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform action {1} as a target entity action, but it isn't one", player.Name,
                            msg.ActionType);
                        return;
                    }

                    if (!_entityManager.TryGetEntity(msg.Target, out var entity))
                    {
                        Logger.DebugS("action", "user {0} attempted to" +
                                                " perform target entity action {1} but could not find entity with " +
                                                "provided uid {2}", player.Name, msg.ActionType, msg.Target);
                        return;
                    }

                    if (ActionBlockerSystem.CanChangeDirection(player))
                    {
                        var diff = entity.Transform.MapPosition.Position - player.Transform.MapPosition.Position;
                        if (diff.LengthSquared > 0.01f)
                        {
                            player.Transform.LocalRotation = new Angle(diff);
                        }
                    }

                    action.TargetEntityAction.DoTargetEntityAction(new TargetEntitytActionEventArgs(player, entity));
                    break;
            }
        }
    }

    public sealed class GrantAction : IClientCommand
    {
        public string Command => "grantaction";
        public string Description => "Grants an action to a player, defaulting to current player";
        public string Help => "grantaction <actionType> <name or userID, omit for current player>";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!Commands.CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;


            if (!attachedEntity.TryGetComponent(out ServerActionsComponent actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionType = args[0];
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(Enum.Parse<ActionType>(actionType), out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }
            actionsComponent.GrantAction(action.ActionType);
        }
    }

    public sealed class RevokeAction : IClientCommand
    {
        public string Command => "revokeaction";
        public string Description => "Revokes an action from a player, defaulting to current player";
        public string Help => "revokeaction <actionType> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!CommandUtils.ValidateAttachedEntity(shell, player, attachedEntity)) return;

            if (!attachedEntity.TryGetComponent(out ServerActionsComponent actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionType = args[0];
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(Enum.Parse<ActionType>(actionType), out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }

            actionsComponent.RevokeAction(action.ActionType);
        }
    }
}
