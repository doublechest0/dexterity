﻿using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction.Events
{
    public class InteractionAttemptEvent : CancellableEntityEventArgs
    {
        public InteractionAttemptEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public static class InteractionAttemptExtensions
    {
        public static bool CanInteract(this IEntity entity)
        {
            var ev = new InteractionAttemptEvent(entity);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, ev);
            return !ev.Cancelled;
        }
    }
}
