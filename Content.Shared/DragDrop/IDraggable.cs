namespace Content.Shared.DragDrop;

/// <summary>
/// Raised directed on an entity when attempting to start a drag.
/// </summary>
[ByRefEvent]
public record struct CanDragEvent
{
    public bool Cancelled;
}

/// <summary>
/// Raised directed on a dragged entity to indicate whether it has interactions with the target entity.
/// </summary>
[ByRefEvent]
public record struct CanDropEvent(EntityUid Target)
{
    public readonly EntityUid Target = Target;
    public bool Handled = false;

    /// <summary>
    /// Can we drop the entity onto the target? If the event is not handled then there is no supported interactions.
    /// </summary>
    public bool CanDrop = false;
}

/// <summary>
/// Raised directed on the target entity to indicate whether it has interactions with the dragged entity.
/// </summary>
public record struct CanDropOnEvent(EntityUid Dragged)
{
    public readonly EntityUid Dragged = Dragged;
    public bool Handled = false;

    /// <summary>
    /// <see cref="CanDropEvent"/>
    /// </summary>
    public bool CanDrop = false;
}

/// <summary>
/// Raised directed on a dragged entity when it is dropped on a target entity.
/// </summary>
[ByRefEvent]
public readonly record struct DragDropEvent(EntityUid Target)
{
    public readonly EntityUid Target = Target;
}
