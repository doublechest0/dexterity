using Robust.Client.Animations;
using Robust.Client.Graphics;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// A component that makes foam play an animation when it dissolves.
/// </summary>
[RegisterComponent]
[Access(typeof(FoamVisualizerSystem))]
public sealed class FoamVisualsComponent : Component
{
    /// <summary>
    /// The id of the animation used when the foam dissolves.
    /// </summary>
    public const string AnimationKey = "foamdissolve_animation";

    /// <summary>
    /// How long the foam visually dissolves for.
    /// </summary>
    [DataField("animationTime")]
    public float AnimationTime = 0.6f;

    /// <summary>
    /// The state of the entities base sprite RSI that is displayed when the foam dissolves.
    /// Cannot use <see cref="RSI.StateKey"/> because it does not have <see cref="DataDefinitionAttribute"/> and I am not making an engine PR at this time.
    /// </summary>
    [DataField("animationState")]
    public string State = "foam-dissolve";

    /// <summary>
    /// The animation used while the foam dissolves.
    /// Generated by <see cref="FoamVisualizerSystem.OnComponentInit"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Animation Animation = default!;
}
