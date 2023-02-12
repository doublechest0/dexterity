using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
public sealed class DoAfterComponent : Component
{
    public readonly Dictionary<byte, DoAfter> DoAfters = new();
    public readonly Dictionary<byte, DoAfter> CancelledDoAfters = new();

    // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
    // we'll just send them the index. Doesn't matter if it wraps around.
    public byte RunningIndex;
}

[Serializable, NetSerializable]
public sealed class DoAfterComponentState : ComponentState
{
    public Dictionary<byte, DoAfter> DoAfters;

    public DoAfterComponentState(Dictionary<byte, DoAfter> doAfters)
    {
        DoAfters = doAfters;
    }
}

/// <summary>
/// Use this event to raise your DoAfter events now.
/// Check for cancelled, and if it is, then null the token there.
/// </summary>
/// TODO: Keep as overload for the classes that don't need additional data
/// TODO: Add a networked DoAfterEvent to pass in AdditionalData for the future
[Serializable, NetSerializable]
public sealed class DoAfterEvent : HandledEntityEventArgs
{
    public bool Cancelled;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent(bool cancelled, DoAfterEventArgs args)
    {
        Cancelled = cancelled;
        Args = args;
    }
}

/// <summary>
/// Use this event to raise your DoAfter events now.
/// Check for cancelled, and if it is, then null the token there.
/// Can't be serialized
/// </summary>
/// TODO: Net/Serilization isn't supported so this needs to be networked somehow
public sealed class DoAfterEvent<T> : HandledEntityEventArgs
{
    public T AdditionalData;
    public bool Cancelled;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent(T additionalData, bool cancelled, DoAfterEventArgs args)
    {
        AdditionalData = additionalData;
        Cancelled = cancelled;
        Args = args;
    }
}

public sealed class DoAfterEvent2<TEvent> : HandledEntityEventArgs where TEvent : EntityEventArgs
{
    public TEvent DoAfterExtraEvent;
    public bool Cancelled;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent2(TEvent doAfterExtraEvent, bool cancelled, DoAfterEventArgs args)
    {
        DoAfterExtraEvent = doAfterExtraEvent;
        Cancelled = cancelled;
        Args = args;
    }
}

public sealed class DoAfterEvent<TEvent, TData> : HandledEntityEventArgs where TEvent : EntityEventArgs where TData : AdditionalData
{
    public TEvent DoAfterExtraEvent;
    public TData AdditionalData;
    public bool Cancelled;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent(TEvent doAfterExtraEvent, TData additionalData, bool cancelled, DoAfterEventArgs args)
    {
        DoAfterExtraEvent = doAfterExtraEvent;
        AdditionalData = additionalData;
        Cancelled = cancelled;
        Args = args;
    }
}

[Serializable, NetSerializable]
public sealed class CancelledDoAfterMessage : EntityEventArgs
{
    public EntityUid Uid;
    public byte ID;

    public CancelledDoAfterMessage(EntityUid uid, byte id)
    {
        Uid = uid;
        ID = id;
    }
}

[Serializable, NetSerializable]
public enum DoAfterStatus : byte
{
    Running,
    Cancelled,
    Finished,
}

[Serializable, NetSerializable]
public sealed class MoreData : AdditionalData
{
    public EntityUid UID;
    public bool BAR;

    public MoreData(EntityUid uid, bool bar)
    {
        UID = uid;
        BAR = bar;
    }
}

[Serializable, NetSerializable]
public abstract class AdditionalData
{

}
