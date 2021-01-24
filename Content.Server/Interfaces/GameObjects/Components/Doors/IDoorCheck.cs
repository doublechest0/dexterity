using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.Interfaces.GameObjects.Components;

namespace Content.Server.Interfaces.GameObjects.Components.Doors
{
    public interface IDoorCheck
    {
        /// <summary>
        /// Called when the door's State variable is changed to a new variable that it was not equal to before.
        /// </summary>
        public void OnStateChange(SharedDoorComponent.DoorState doorState) { }

        /// <summary>
        /// Called when the door is determining whether it is able to open.
        /// </summary>
        /// <returns>True if the door should open, false if it should not.</returns>
        public bool OpenCheck() => true;

        /// <summary>
        /// Called when the door is determining whether it is able to close.
        /// </summary>
        /// <returns>True if the door should close, false if it should not.</returns>
        public bool CloseCheck() => true;

        /// <summary>
        /// Called when the door is determining whether it is able to deny.
        /// </summary>
        /// <returns>True if the door should deny, false if it should not.</returns>
        public bool DenyCheck() => true;

        /// <summary>
        /// Gets an override for the amount of time to pry open the door, or null if there is no override.
        /// </summary>
        /// <returns>Float if there is an override, null otherwise.</returns>
        public float? GetPryTime() => null;

        /// <summary>
        /// Gets an override for the amount of time before the door automatically closes, or null if there is no override.
        /// </summary>
        /// <returns>Float if there is an override, null otherwise.</returns>
        public float? GetCloseSpeed() => null;

        /// <summary>
        /// A check to determine whether or not a click on the door should interact with it with the intent to open/close.
        /// </summary>
        /// <returns>True if the door's IActivate should not run, false otherwise.</returns>
        public bool BlockActivate(ActivateEventArgs eventArgs) => false;

        /// <summary>
        /// Called when somebody begins to pry open the door.
        /// </summary>
        /// <param name="eventArgs">The eventArgs of the InteractUsing method that called this function.</param>
        public void OnStartPry(InteractUsingEventArgs eventArgs) { }

        /// <summary>
        /// Check representing whether or not the door can be pried open.
        /// </summary>
        /// <param name="eventArgs">The eventArgs of the InteractUsing method that called this function.</param>
        /// <returns>True if the door can be pried open, false if it cannot.</returns>
        public bool CanPryCheck(InteractUsingEventArgs eventArgs) => true;
    }
}
