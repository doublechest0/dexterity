using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Utility
{
    /// <summary>
    /// Convenient methods for checking for various conditions commonly needed
    /// for interactions.
    /// </summary>
    public static class InteractionChecks
    {

        /// <summary>
        /// Default interaction check for targeted attack interaction types.
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, but defaults to allow inside blockers.
        /// Validates that attacker is in range of the attacked entity. Additionally shows a popup if
        /// validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(ITargetedAttackEventArgs eventArgs, bool insideBlockerValid = true)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                eventArgs.Attacked.Transform.WorldPosition, ignoredEnt: eventArgs.Attacked, insideBlockerValid: insideBlockerValid))
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.Attacked.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Default interaction check for after attack interaction types.
        /// Same as <see cref="SharedInteractionSystem.InRangeUnobstructed"/>, but defaults to allow inside blockers.
        /// Validates that attacker is in range of the attacked entity, if there is such an entity.
        /// If there is no attacked entity, validates that they are in range of the clicked position.
        /// Additionally shows a popup if validation fails.
        /// </summary>
        public static bool InRangeUnobstructed(AfterAttackEventArgs eventArgs, bool insideBlockerValid = true)
        {
            if (eventArgs.Attacked != null)
            {
                if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.Attacked.Transform.WorldPosition, ignoredEnt: eventArgs.Attacked, insideBlockerValid: insideBlockerValid))
                {
                    var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                    eventArgs.Attacked.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                    return false;
                }
            }
            else
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.ClickLocation.ToMapPos(mapManager), ignoredEnt: eventArgs.User, insideBlockerValid: insideBlockerValid))
                {
                    var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                    eventArgs.User.PopupMessage(eventArgs.User, localizationManager.GetString("You can't reach there!"));
                    return false;
                }
            }


            return true;
        }

        /// <summary>
        /// Convenient static alternative to <see cref="SharedInteractionSystem.InRangeUnobstructed"/>.
        /// </summary>
        public static bool InRangeUnobstructed(MapCoordinates coords, Vector2 otherCoords,
            float range = SharedInteractionSystem.InteractionRange,
            int collisionMask = (int) CollisionGroup.Impassable, IEntity ignoredEnt = null,
            bool insideBlockerValid = false)
        {
            var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
            return interactionSystem.InRangeUnobstructed(coords, otherCoords, range, collisionMask,
                ignoredEnt, insideBlockerValid);
        }



    }
}
