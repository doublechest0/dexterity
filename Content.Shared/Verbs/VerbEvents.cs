using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Shared.Verbs
{
    [Serializable, NetSerializable]
    public sealed class RequestServerVerbsEvent : EntityEventArgs
    {
        public readonly EntityUid EntityUid;

        public readonly List<string> VerbTypes = new();

        /// <summary>
        ///     If the target item is inside of some storage (e.g., backpack), this is the entity that owns that item
        ///     slot. Needed for validating that the user can access the target item.
        /// </summary>
        public readonly EntityUid? SlotOwner;

        public readonly bool AdminRequest;

        public RequestServerVerbsEvent(EntityUid entityUid, List<Type> verbTypes, EntityUid? slotOwner = null, bool adminRequest = false)
        {
            EntityUid = entityUid;
            SlotOwner = slotOwner;
            AdminRequest = adminRequest;

            foreach (var verbType in verbTypes)
            {
                if (Verb.VerbTypes.TryGetValue(verbType, out var key))
                    VerbTypes.Add(key);
                else
                    Logger.Error($"Unknown verb Type: {verbType}");
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed class VerbsResponseEvent : EntityEventArgs
    {
        public readonly List<Verb>? Verbs;
        public readonly EntityUid Entity;

        public VerbsResponseEvent(EntityUid entity, SortedSet<Verb>? verbs)
        {
            Entity = entity;

            if (verbs == null)
                return;

            // Apparently SortedSet is not serializable, so we cast to List<Verb>.
            Verbs = new(verbs);
        }
    }

    [Serializable, NetSerializable]
    public sealed class ExecuteVerbEvent : EntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly Verb RequestedVerb;

        public ExecuteVerbEvent(EntityUid target, Verb requestedVerb)
        {
            Target = target;
            RequestedVerb = requestedVerb;
        }
    }

    /// <summary>
    ///     Directed event that requests verbs from any systems/components on a target entity.
    /// </summary>
    public sealed class GetVerbsEvent<TVerb> : EntityEventArgs where TVerb : Verb
    {
        /// <summary>
        ///     Event output. Set of verbs that can be executed.
        /// </summary>
        public readonly SortedSet<TVerb> Verbs = new();

        /// <summary>
        ///     Can the user physically access the target?
        /// </summary>
        /// <remarks>
        ///     This is a combination of <see cref="ContainerHelpers.IsInSameOrParentContainer"/> and
        ///     <see cref="SharedInteractionSystem.InRangeUnobstructed"/>.
        /// </remarks>
        public readonly bool CanAccess = false;

        /// <summary>
        ///     The entity being targeted for the verb.
        /// </summary>
        public readonly EntityUid Target;

        /// <summary>
        ///     The entity that will be "performing" the verb.
        /// </summary>
        public readonly EntityUid User;

        /// <summary>
        ///     Can the user physically interact?
        /// </summary>
        /// <remarks>
        ///     This is a just a cached <see cref="ActionBlockerSystem.CanInteract"/> result. Given that many verbs need
        ///     to check this, it prevents it from having to be repeatedly called by each individual system that might
        ///     contribute a verb.
        /// </remarks>
        public readonly bool CanInteract;

        /// <summary>
        ///     The User's hand component.
        /// </summary>
        /// <remarks>
        ///     This may be null if the user has no hands.
        /// </remarks>
        public readonly SharedHandsComponent? Hands;

        /// <summary>
        ///     The entity currently being held by the active hand.
        /// </summary>
        /// <remarks>
        ///     This is only ever not null when <see cref="ActionBlockerSystem.CanUse(EntityUid)"/> is true and the user
        ///     has hands.
        /// </remarks>
        public readonly EntityUid? Using;

        public GetVerbsEvent(EntityUid user, EntityUid target, EntityUid? @using, SharedHandsComponent? hands, bool canInteract, bool canAccess)
        {
            User = user;
            Target = target;
            Using = @using;
            Hands = hands;
            CanAccess = canAccess;
            CanInteract = canInteract;
        }
    }
}
