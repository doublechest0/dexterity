using System.Collections.Generic;
using Content.Server.AI.Utils;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public sealed class NearbyDrinkState: CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyDrink";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(Owner.Transform.GridPosition, typeof(DrinkComponent), controller.VisionRadius))
            {
                result.Add(entity);
            }

            return result;
        }
    }
}
