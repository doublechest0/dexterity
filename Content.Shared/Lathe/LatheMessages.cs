using Content.Shared.Research.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

[Serializable, NetSerializable]
public sealed class LatheUpdateState : BoundUserInterfaceState
{
    public List<string> Recipes;

    public List<LatheRecipePrototype> Queue;

    public LatheRecipePrototype? CurrentlyProducing;

    public Dictionary<string, int> Materials;

    public LatheUpdateState(List<string> recipes, List<LatheRecipePrototype> queue, Dictionary<string, int> materials, LatheRecipePrototype? currentlyProducing = null)
    {
        Recipes = recipes;
        Queue = queue;
        CurrentlyProducing = currentlyProducing;
        Materials = materials;
    }
}

/// <summary>
///     Sent to the server to sync material storage and the recipe queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheSyncRequestMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Sent to the server to sync the lathe's technology database with the research server.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheServerSyncMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Sent to the server to open the ResearchClient UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheServerSelectionMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Sent to the server when a client queues a new recipe.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheQueueRecipeMessage : BoundUserInterfaceMessage
{
    public readonly string ID;
    public readonly int Quantity;
    public LatheQueueRecipeMessage(string id, int quantity)
    {
        ID = id;
        Quantity = quantity;
    }
}

[NetSerializable, Serializable]
public enum LatheUiKey
{
    Key,
}
