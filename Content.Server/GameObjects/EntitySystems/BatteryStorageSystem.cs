﻿using Content.Server.GameObjects.Components.NewPower.PowerNetComponents;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class BatteryStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(BatteryStorageComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<BatteryStorageComponent>().Update(frameTime);
            }
        }
    }
}
