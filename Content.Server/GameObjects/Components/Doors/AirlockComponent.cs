#nullable enable
using System;
using System.Threading;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.Interfaces.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;
using static Content.Shared.GameObjects.Components.SharedWiresComponent.WiresAction;

namespace Content.Server.GameObjects.Components.Doors
{
    /// <summary>
    /// Companion component to AirlockComponent that handles firelock-specific behavior.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDoorCheck))]
    public class AirlockComponent : Component, IWires, IDoorCheck
    {
        public override string Name => "Airlock";

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        private static readonly TimeSpan PowerWiresTimeout = TimeSpan.FromSeconds(5.0);

        private CancellationTokenSource _powerWiresPulsedTimerCancel = new();

        private bool _powerWiresPulsed;

        /// <summary>
        /// True if either power wire was pulsed in the last <see cref="PowerWiresTimeout"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private bool PowerWiresPulsed
        {
            get => _powerWiresPulsed;
            set
            {
                _powerWiresPulsed = value;
                UpdateWiresStatus();
                UpdatePowerCutStatus();
            }
        }

        private bool _boltsDown;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool BoltsDown
        {
            get => _boltsDown;
            set
            {
                _boltsDown = value;
                UpdateBoltLightStatus();
            }
        }

        private bool _boltLightsWirePulsed = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool BoltLightsVisible
        {
            get => _boltLightsWirePulsed && BoltsDown && IsPowered()
                && Owner.GetComponent<ServerDoorComponent>().State == SharedDoorComponent.DoorState.Closed;
            set
            {
                _boltLightsWirePulsed = value;
                UpdateBoltLightStatus();
            }
        }

        private const float AutoCloseDelayFast = 1;
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _normalCloseSpeed = true;

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(DoorVisuals.Powered, receiver.Powered);
                }
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerDeviceOnOnPowerStateChanged(powerChanged);
                    break;
            }
        }

        public void OnStateChange(SharedDoorComponent.DoorState doorState)
        {
            // Only show the maintenance panel if the airlock is closed
            if (Owner.TryGetComponent(out WiresComponent? wires))
            {
                wires.IsPanelVisible = doorState != SharedDoorComponent.DoorState.Open;
            }
            // If the door is closed, we should look if the bolt was locked while closing
            UpdateBoltLightStatus();
        }

        public bool OpenCheck() => CanChangeState();

        public bool CloseCheck() => CanChangeState();

        public bool DenyCheck() => CanChangeState();

        public float? GetCloseSpeed()
        {
            if (_normalCloseSpeed)
            {
                return null;
            }
            return AutoCloseDelayFast;
        }

        public bool BlockActivate(ActivateEventArgs eventArgs)
        {
            if (Owner.TryGetComponent(out WiresComponent? wires) &&
                wires.IsPanelOpen && eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                wires.OpenInterface(actor.playerSession);
                return true;
            }
            return false;
        }

        public bool CanPryCheck(InteractUsingEventArgs eventArgs)
        {
            if (IsBolted())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The airlock's bolts prevent it from being forced!"));
                return false;
            }
            if (IsPowered())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The powered motors block your efforts!"));
                return false;
            }
            return true;
        }

        private bool CanChangeState()
        {
            return IsPowered() && !IsBolted();
        }

        private bool IsBolted()
        {
            return _boltsDown;
        }

        private bool IsPowered()
        {
            return !Owner.TryGetComponent(out PowerReceiverComponent? receiver)
                   || receiver.Powered;
        }

        private void UpdateBoltLightStatus()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DoorVisuals.BoltLights, BoltLightsVisible);
            }
        }

        private void UpdateWiresStatus()
        {
            WiresComponent? wires;
            var powerLight = new StatusLightData(Color.Yellow, StatusLightState.On, "POWR");
            if (PowerWiresPulsed)
            {
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "POWR");
            }
            else if (Owner.TryGetComponent(out wires) &&
                     wires.IsWireCut(Wires.MainPower) &&
                     wires.IsWireCut(Wires.BackupPower))
            {
                powerLight = new StatusLightData(Color.Red, StatusLightState.On, "POWR");
            }

            var door = Owner.GetComponent<ServerDoorComponent>();
            var boltStatus =
                new StatusLightData(Color.Red, BoltsDown ? StatusLightState.On : StatusLightState.Off, "BOLT");
            var boltLightsStatus = new StatusLightData(Color.Lime,
                _boltLightsWirePulsed ? StatusLightState.On : StatusLightState.Off, "BLTL");

            var timingStatus =
                new StatusLightData(Color.Orange, !door.AutoClose ? StatusLightState.Off :
                                                    !_normalCloseSpeed ? StatusLightState.BlinkingSlow :
                                                    StatusLightState.On,
                                                    "TIME");

            var safetyStatus =
                new StatusLightData(Color.Red, door.Safety ? StatusLightState.On : StatusLightState.Off, "SAFE");

            if (!Owner.TryGetComponent(out wires))
            {
                return;
            }

            wires.SetStatus(AirlockWireStatus.PowerIndicator, powerLight);
            wires.SetStatus(AirlockWireStatus.BoltIndicator, boltStatus);
            wires.SetStatus(AirlockWireStatus.BoltLightIndicator, boltLightsStatus);
            wires.SetStatus(AirlockWireStatus.AIControlIndicator, new StatusLightData(Color.Purple, StatusLightState.BlinkingSlow, "AICT"));
            wires.SetStatus(AirlockWireStatus.TimingIndicator, timingStatus);
            wires.SetStatus(AirlockWireStatus.SafetyIndicator, safetyStatus);
            /*
            _wires.SetStatus(6, powerLight);
            _wires.SetStatus(7, powerLight);
            _wires.SetStatus(8, powerLight);
            _wires.SetStatus(9, powerLight);
            _wires.SetStatus(10, powerLight);
            _wires.SetStatus(11, powerLight);*/
        }

        private void UpdatePowerCutStatus()
        {
            if (!Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                return;
            }

            if (PowerWiresPulsed)
            {
                receiver.PowerDisabled = true;
                return;
            }

            if (!Owner.TryGetComponent(out WiresComponent? wires))
            {
                return;
            }

            receiver.PowerDisabled =
                wires.IsWireCut(Wires.MainPower) ||
                wires.IsWireCut(Wires.BackupPower);
        }

        private void PowerDeviceOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DoorVisuals.Powered, e.Powered);
            }

            // BoltLights also got out
            UpdateBoltLightStatus();
        }

        private enum Wires
        {
            /// <summary>
            /// Pulsing turns off power for <see cref="AirlockComponent.PowerWiresTimeout"/>.
            /// Cutting turns off power permanently if <see cref="BackupPower"/> is also cut.
            /// Mending restores power.
            /// </summary>
            MainPower,

            /// <see cref="MainPower"/>
            BackupPower,

            /// <summary>
            /// Pulsing causes for bolts to toggle (but only raise if power is on)
            /// Cutting causes Bolts to drop
            /// Mending does nothing
            /// </summary>
            Bolts,

            /// <summary>
            /// Pulsing causes light to toggle
            /// Cutting causes light to go out
            /// Mending causes them to go on again
            /// </summary>
            BoltLight,

            // Placeholder for when AI is implemented
            AIControl,

            /// <summary>
            /// Pulsing causes door to close faster
            /// Cutting disables door timer, causing door to stop closing automatically
            /// Mending restores door timer
            /// </summary>
            Timing,

            /// <summary>
            /// Pulsing toggles safety
            /// Cutting disables safety
            /// Mending enables safety
            /// </summary>
            Safety,
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.MainPower);
            builder.CreateWire(Wires.BackupPower);
            builder.CreateWire(Wires.Bolts);
            builder.CreateWire(Wires.BoltLight);
            builder.CreateWire(Wires.Timing);
            builder.CreateWire(Wires.Safety);
            /*
            builder.CreateWire(6);
            builder.CreateWire(7);
            builder.CreateWire(8);
            builder.CreateWire(9);
            builder.CreateWire(10);
            builder.CreateWire(11);*/
            UpdateWiresStatus();
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            var door = Owner.GetComponent<ServerDoorComponent>();
            if (args.Action == Pulse)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        PowerWiresPulsed = true;
                        _powerWiresPulsedTimerCancel.Cancel();
                        _powerWiresPulsedTimerCancel = new CancellationTokenSource();
                        Owner.SpawnTimer(PowerWiresTimeout,
                            () => PowerWiresPulsed = false,
                            _powerWiresPulsedTimerCancel.Token);
                        break;
                    case Wires.Bolts:
                        if (!BoltsDown)
                        {
                            SetBoltsWithAudio(true);
                        }
                        else
                        {
                            if (IsPowered()) // only raise again if powered
                            {
                                SetBoltsWithAudio(false);
                            }
                        }

                        break;
                    case Wires.BoltLight:
                        // we need to change the property here to set the appearance again
                        BoltLightsVisible = !_boltLightsWirePulsed;
                        break;
                    case Wires.Timing:
                        _normalCloseSpeed = !_normalCloseSpeed;
                        break;
                    case Wires.Safety:
                        door.Safety = !door.Safety;
                        break;
                }
            }

            if (args.Action == Mend)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        // mending power wires instantly restores power
                        _powerWiresPulsedTimerCancel?.Cancel();
                        PowerWiresPulsed = false;
                        break;
                    case Wires.BoltLight:
                        BoltLightsVisible = true;
                        break;
                    case Wires.Timing:
                        door.AutoClose = true;
                        break;
                    case Wires.Safety:
                        door.Safety = true;
                        break;
                }
            }

            if (args.Action == Cut)
            {
                switch (args.Identifier)
                {
                    case Wires.Bolts:
                        SetBoltsWithAudio(true);
                        break;
                    case Wires.BoltLight:
                        BoltLightsVisible = false;
                        break;
                    case Wires.Timing:
                        door.AutoClose = false;
                        break;
                    case Wires.Safety:
                        door.Safety = false;
                        break;
                }
            }

            UpdateWiresStatus();
            UpdatePowerCutStatus();
        }

        public void SetBoltsWithAudio(bool newBolts)
        {
            if (newBolts == BoltsDown)
            {
                return;
            }

            BoltsDown = newBolts;

            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity(newBolts ? "/Audio/Machines/boltsdown.ogg" : "/Audio/Machines/boltsup.ogg", Owner);
        }
    }
}
