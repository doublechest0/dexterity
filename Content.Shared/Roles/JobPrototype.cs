using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public sealed class JobPrototype : IPrototype
    {
        private string _name = string.Empty;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("supervisors")]
        public string Supervisors { get; } = "nobody";

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("joinNotifyCrew")]
        public bool JoinNotifyCrew { get; } = false;

        [DataField("requireAdminNotify")]
        public bool RequireAdminNotify { get; } = false;

        [DataField("setPreference")]
        public bool SetPreference { get; } = true;

        [DataField("canBeAntag")]
        public bool CanBeAntag { get; } = true;

        [DataField("difficulty")]
        public string Difficulty { get; } = string.Empty;

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        [DataField("weight")]
        public int Weight { get; private set; }

        [DataField("startingGear", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
        public string? StartingGear { get; private set; }

        [DataField("icon")] public string Icon { get; } = string.Empty;

        [DataField("special", serverOnly:true)]
        public JobSpecial[] Special { get; private set; } = Array.Empty<JobSpecial>();

        [DataField("departments")]
        public IReadOnlyCollection<string> Departments { get; } = Array.Empty<string>();

        [DataField("access", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
        public IReadOnlyCollection<string> Access { get; } = Array.Empty<string>();

        [DataField("accessGroups", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessGroupPrototype>))]
        public IReadOnlyCollection<string> AccessGroups { get; } = Array.Empty<string>();

        [DataField("extendedAccess", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
        public IReadOnlyCollection<string> ExtendedAccess { get; } = Array.Empty<string>();

        [DataField("extendedAccessGroups", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessGroupPrototype>))]
        public IReadOnlyCollection<string> ExtendedAccessGroups { get; } = Array.Empty<string>();
    }
}
