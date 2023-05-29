using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Standing
{
    [Access(typeof(StandingStateSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class StandingStateComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("downSound")]
        public SoundSpecifier DownSound { get; } = new SoundCollectionSpecifier("BodyFall");

        [DataField("standing")]
        public bool Standing { get; set; } = true;

        /// <summary>
        ///     List of fixtures that had their collision mask changed when the entity was downed.
        ///     Required for re-adding the collision mask.
        /// </summary>
        [DataField("changedFixtures")]
        public List<string> ChangedFixtures = new();

        /// <summary>
        ///     The action to lie down on ground.
        /// </summary>
        [DataField("lie-down-action")]
        public InstantAction LieDownAction { get; set; } = new();

        /// <summary>
        ///     The action to lie down or stand up.
        /// </summary>
        [DataField("stand-up-action")]
        public InstantAction StandUpAction { get; set; } = new();
    }

    public sealed class LieDownActionEvent : InstantActionEvent {}
    public sealed class StandUpActionEvent : InstantActionEvent {}
}
