using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Generates dungeon flooring based on the specified noise.
/// </summary>
public sealed partial class NoiseDunGen : IDunGen
{
    /*
     * Floodfills out from 0 until it finds a valid tile.
     * From here it then floodfills until it can no longer fill in an area and generates a dungeon from that.
     */

    // At some point we may want layers masking each other like a simpler version of biome code but for now
    // we'll just make it circular.

    [DataField]
    public int Iterations = 1;

    [DataField(required: true)]
    public List<NoiseDunGenLayer> Layers = new();
}

[DataRecord]
public record struct NoiseDunGenLayer
{
    /// <summary>
    /// If the noise value is above this then it gets output.
    /// </summary>
    [DataField]
    public float Threshold;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public FastNoiseLite Noise;
}
