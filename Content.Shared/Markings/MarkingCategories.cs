using Robust.Shared.Serialization;

namespace Content.Shared.Markings
{
    [Serializable, NetSerializable]
    public enum MarkingCategories : byte
    {
        Hair,
        FacialHair,
        Head,
        HeadTop,
        HeadSide,
        Snout,
        Chest,
        Arms,
        Legs,
        Tail,
        Overlay
    }
}
