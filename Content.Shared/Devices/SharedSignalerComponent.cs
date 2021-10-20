using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Devices
{
    [RegisterComponent]
    public class SharedSignalerComponent : Component
    {
        public override string Name => "Signaler";

        public const int MaxFrequency = 300;
        public const int MinFrequency = 1;

        [DataField("frequency")]
        public int Frequency = 100;
    }

    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum SignalerUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents a <see cref="SharedSignalerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class SignalerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int Frequency { get; }

        public SignalerBoundUserInterfaceState(int frequency)
        {
            Frequency = frequency;
        }
    }

    [Serializable, NetSerializable]
    public class SignalerSendSignalMessage : BoundUserInterfaceMessage
    {
        public SignalerSendSignalMessage() {}
    }

    [Serializable, NetSerializable]
    public class SignalerUpdateFrequencyMessage : BoundUserInterfaceMessage
    {
        public int Frequency { get; }

        public SignalerUpdateFrequencyMessage( int frequency )
        {
            Frequency = frequency;
        }
    }
}
