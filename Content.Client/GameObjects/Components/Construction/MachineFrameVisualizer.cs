﻿using Content.Shared.GameObjects.Components.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Client.GameObjects.Components.Construction
{
    [UsedImplicitly]
    public class MachineFrameVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<int>(MachineFrameVisuals.State, out var data))
            {
                var sprite = component.Owner.GetComponent<ISpriteComponent>();

                sprite.LayerSetState(0, $"box_{data}");
            }
        }

        public override IDeepClone DeepClone()
        {
            return new MachineFrameVisualizer();
        }
    }
}
