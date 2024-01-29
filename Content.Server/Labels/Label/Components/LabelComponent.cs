namespace Content.Server.Labels.Components
{
    /// <summary>
    /// Makes entities have a label in their name. Labels are normally given by <see cref="HandLabelerComponent"/>
    /// </summary>
    [RegisterComponent]
    public sealed partial class LabelComponent : Component
    {
        /// <summary>
        /// Current text on the label. If set before map init, during map init this string will be localized.
        /// This permits localized preset labels with fallback to the text written on the label.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("currentLabel")]
        public string? CurrentLabel { get; set; }

        /// <summary>
        ///  The original name of the entity
        ///  Used for reverting the modified entity name when the label is removed
        /// </summary>
        [DataField("originalName")]
        public string? OriginalName { get; set; }
    }
}
