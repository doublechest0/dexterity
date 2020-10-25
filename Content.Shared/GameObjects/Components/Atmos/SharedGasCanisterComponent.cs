﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    public class SharedGasCanisterComponent : Component
    {
        public override string Name => "GasCanister";

        /// <summary>
        /// Key representing which <see cref="BoundUserInterface"/> is currently open.
        /// Useful when there are multiple UI for an object. Here it's future-proofing only.
        /// </summary>
        [Serializable, NetSerializable]
        public enum GasCanisterUiKey
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public enum UiButton
    {
        Test
    }

    /// <summary>
    /// Represents a <see cref="GasCanisterComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class GasCanisterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string CanisterName;
        public readonly float Volume;
        public readonly float ReleasePressure;

        public GasCanisterBoundUserInterfaceState(string canName, float volume, float releasePressure)
        {
            CanisterName = canName;
            Volume = volume;
            ReleasePressure = releasePressure;
        }

        public bool Equals(GasCanisterBoundUserInterfaceState? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CanisterName == other.CanisterName &&
                   Volume.Equals(other.Volume) &&
                   ReleasePressure.Equals(other.ReleasePressure);
        }
    }

    /// <summary>
    /// Message sent from the client to the server when a gas canister button is pressed
    /// </summary>
    [Serializable, NetSerializable]
    public class UiButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly UiButton Button;

        public UiButtonPressedMessage(UiButton button)
        {
            Button = button;
        }
    }

    /// <summary>
    /// Message sent when the release pressure is changed client side
    /// </summary>
    [Serializable, NetSerializable]
    public class ReleasePressureButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly float ReleasePressure;

        public ReleasePressureButtonPressedMessage(float val) : base()
        {
            ReleasePressure = val;
        }
    }


}
