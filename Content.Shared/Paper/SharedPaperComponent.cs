using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

public abstract partial class SharedPaperComponent : Component
{
    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;
        public readonly TimeSpan TimeWrite;

        public PaperBoundUserInterfaceState(string text, TimeSpan timeWrite, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
            TimeWrite = timeWrite;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;
        public readonly TimeSpan TimeWrite;

        public PaperInputTextMessage(string text, TimeSpan timeWrite)
        {
            Text = text;
            TimeWrite = timeWrite;
        }
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}
