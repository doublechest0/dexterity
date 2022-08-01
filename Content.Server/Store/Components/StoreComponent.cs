using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Store.Components;

[RegisterComponent]
public sealed class StoreComponent : Component
{
    /// <summary>
    /// All the listing categories that are available on this store.
    /// The available listings are partially based on the categories.
    /// </summary>
    [DataField("categories", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StoreCategoryPrototype>))]
    public HashSet<string> Categories = new() { "Debug" }; //todo: remove

    /// <summary>
    /// The total amount of currency that can be used in the store.
    /// The string represents the ID of te currency prototype, where the
    /// float is that amount.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("balance", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> Balance = new();

    /// <summary>
    /// The list of currencies that can be inserted into this store.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("currencyWhitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<CurrencyPrototype>))]
    public HashSet<string> CurrencyWhitelist = new() { "Telecrystal", "DebugDollar" }; //todo: remove

    /// <summary>
    /// Whether or not this store can be activated by clicking on it (like an uplink)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activateInHand")]
    public bool ActivateInHand = true;

    /// <summary>
    /// The person who "owns" the store/account. Used if you want the listings to be fixed
    /// regardless of who activated it. I.E. role specific items for uplinks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AccountOwner = null;

    /// <summary>
    /// All listings, including those that aren't available to the buyer
    /// </summary>
    public HashSet<ListingData> Listings = new();

    /// <summary>
    /// All available listings from the last time that it was checked.
    /// </summary>
    [ViewVariables]
    public HashSet<ListingData> LastAvailableListings = new();
}
