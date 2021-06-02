﻿using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class GasCanisterVisualizer : AppearanceVisualizer
    {
        [DataField("pressureStates")]
        private readonly string[] _statePressure = {"", "", "", ""};

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.PressureLight, sprite.AddLayerState(_statePressure[0]));
            sprite.LayerSetShader(Layers.PressureLight, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            // Update the canister lights
            if (component.TryGetData(GasCanisterVisuals.PressureState, out int pressureState))
                if ((pressureState >= 0) && (pressureState < _statePressure.Length))
                    sprite.LayerSetState(Layers.PressureLight, _statePressure[pressureState]);
        }

        private enum Layers
        {
            PressureLight
        }
    }
}
