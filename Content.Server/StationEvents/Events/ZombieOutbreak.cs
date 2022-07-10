using Content.Server.Chat;
using Robust.Shared.Random;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared.MobState.Components;
using Content.Shared.Sound;
using Content.Server.Zombies;

namespace Content.Server.StationEvents.Events
{
    /// <summary>
    /// Revives several dead entities as zombies
    /// </summary>
    public sealed class ZombieOutbreak : StationEventSystem
    {
        [Dependency] private readonly IRobustRandom RobustRandom = default!;
        [Dependency] private readonly IEntityManager EntityManager = default!;

        public override string Prototype => "ZombieOutbreak";
        public override int EarliestStart => 50;
        public override float Weight => WeightLow / 2;
        public override SoundSpecifier? StartAudio => new SoundPathSpecifier("/Audio/Announcements/bloblarm.ogg");
        protected override float EndAfter => 1.0f;
        public override int? MaxOccurrences => 1;
        public override bool AnnounceEvent => false;

        /// <summary>
        /// Finds 1-3 random, dead entities accross the station
        /// and turns them into zombies.
        /// </summary>
        public override void Started()
        {
            base.Started();
            HashSet<EntityUid> stationsToNotify = new();
            List<MobStateComponent> deadList = new();
            foreach (var mobState in EntityManager.EntityQuery<MobStateComponent>())
            {
                if (mobState.IsDead() || mobState.IsCritical())
                    deadList.Add(mobState);
            }
            RobustRandom.Shuffle(deadList);

            var toInfect = RobustRandom.Next(1, 3);

            var zombifysys = EntityManager.EntitySysManager.GetEntitySystem<ZombifyOnDeathSystem>();

            // Now we give it to people in the list of dead entities earlier.
            var entSysMgr = IoCManager.Resolve<IEntitySystemManager>();
            var stationSystem = entSysMgr.GetEntitySystem<StationSystem>();
            var chatSystem = entSysMgr.GetEntitySystem<ChatSystem>();

            foreach (var target in deadList)
            {
                if (toInfect-- == 0)
                    break;

                zombifysys.ZombifyEntity(target.Owner);

                var station = stationSystem.GetOwningStation(target.Owner);
                if(station == null) continue;
                stationsToNotify.Add((EntityUid) station);
            }

            if (!AnnounceEvent)
                return;
            foreach (var station in stationsToNotify)
            {
                chatSystem.DispatchStationAnnouncement((EntityUid) station, Loc.GetString("station-event-zombie-outbreak-announcement"),
                    playDefaultSound: false, colorOverride: Color.DarkMagenta);
            }
        }
    }
}
