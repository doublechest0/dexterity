using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

public sealed class ApcVisualizerSystem : VisualizerSystem<ApcVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ApcVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Handle APC screen overlay:
        if(!AppearanceSystem.TryGetData<ApcChargeState>(uid, ApcVisuals.ChargeState, out var chargeState, args.Component))
            chargeState = ApcChargeState.Lack;

        if (chargeState >= 0 && chargeState < ApcChargeState.NumStates)
        {
            args.Sprite.LayerSetState(ApcVisualLayers.ChargeState, $"{comp.ScreenPrefix}-{comp.ScreenSuffixes[(sbyte)chargeState]}");

            if (AppearanceSystem.TryGetData<byte>(uid, ApcVisuals.LockState, out var lockStates, args.Component))
            {
                for(var i = 0; i < comp.LockIndicators; ++i)
                {
                    var layer = ((byte)ApcVisualLayers.LockIndicatorOverlayStart + i);
                    sbyte lockState = (sbyte)((lockStates >> (i << (sbyte)ApcLockState.LogWidth)) & (sbyte)ApcLockState.All);
                    args.Sprite.LayerSetState(layer, $"{comp.LockPrefix}{i}-{comp.LockSuffixes[lockState]}");
                    args.Sprite.LayerSetVisible(layer, true);
                }
            }

            if (AppearanceSystem.TryGetData<byte>(uid, ApcVisuals.ChannelState, out var channelStates, args.Component))
            {
                for(var i = 0; i < comp.ChannelIndicators; ++i)
                {
                    var layer = ((byte)ApcVisualLayers.ChannelIndicatorOverlayStart + i);
                    sbyte channelState = (sbyte)((channelStates >> (i << (sbyte)ApcChannelState.LogWidth)) & (sbyte)ApcChannelState.All);
                    args.Sprite.LayerSetState(layer, $"{comp.ChannelPrefix}{i}-{comp.ChannelSuffixes[channelState]}");
                    args.Sprite.LayerSetVisible(layer, true);
                }
            }

            if (TryComp<SharedPointLightComponent>(uid, out var light))
                light.Color = comp.ScreenColors[(sbyte)chargeState];
        }
        else
        {
            /// Overrides all of the lock and channel indicators.
            args.Sprite.LayerSetState(ApcVisualLayers.ChargeState, comp.EmaggedScreenState);
            for(var i = 0; i < comp.LockIndicators; ++i)
            {
                var layer = ((byte)ApcVisualLayers.LockIndicatorOverlayStart + i);
                args.Sprite.LayerSetVisible(layer, false);
            }
            for(var i = 0; i < comp.ChannelIndicators; ++i)
            {
                var layer = ((byte)ApcVisualLayers.ChannelIndicatorOverlayStart + i);
                args.Sprite.LayerSetVisible(layer, false);
            }

            if (TryComp<SharedPointLightComponent>(uid, out var light))
                light.Color = comp.EmaggedScreenColor;
        }
    }
}

enum ApcVisualLayers : byte
{
    /// <summary>
    /// The sprite layer used for the interface lock indicator light overlay.
    /// </summary>
    InterfaceLock = 0,
    /// <summary>
    /// The sprite layer used for the panel lock indicator light overlay.
    /// </summary>
    PanelLock = 1,
    /// <summary>
    /// The first of the lock indicator light layers.
    /// </summary>
    LockIndicatorOverlayStart = InterfaceLock,

    /// <summary>
    /// The sprite layer used for the equipment channel indicator light overlay.
    /// </summary>
    Equipment = 2,
    /// <summary>
    /// The sprite layer used for the lighting channel indicator light overlay.
    /// </summary>
    Lighting = 3,
    /// <summary>
    /// The sprite layer used for the environment channel indicator light overlay.
    /// </summary>
    Environment = 4,
    /// <summary>
    /// The first of the channel status indicator light layers.
    /// </summary>
    ChannelIndicatorOverlayStart = Equipment,

    /// <summary>
    /// The sprite layer used for the APC screen overlay.
    /// </summary>
    ChargeState = 5,
}
