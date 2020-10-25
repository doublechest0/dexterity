﻿#nullable enable
using System.Diagnostics;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Content.Client.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Shared.Localization;

namespace Content.Client.GameObjects.Components.Atmos
{
    /// <summary>
    /// Initializes a <see cref="GasCanisterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class GasCanisterBoundUserInterface : BoundUserInterface
    {

        private GasCanisterWindow? _window;

        public GasCanisterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        /// <summary>
        /// When a button is pressed, send a network message to the server
        /// </summary>
        /// <param name="button">Which button has been pressed, as an enum item</param>
        private void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }

        /// <summary>
        /// When the release pressure is changed
        /// </summary>
        /// <param name="value">The pressure value</param>
        private void ReleasePressureButtonPressed(float value)
        {
            SendMessage(new ReleasePressureButtonPressedMessage(value));
        }

        protected override void Open()
        {
            base.Open();

            _window = new GasCanisterWindow();
            _window.Title = Loc.GetString("Gas Canister");

            _window.OpenCentered();
            _window.OnClose += Close;

            // Bind buttons OnPressed event
            _window.Test.OnPressed += _ => ButtonPressed(UiButton.Test);
            foreach (ReleasePressureButton btn in _window.ReleasePressureButtons)
            {
                btn.OnPressed += _ => ReleasePressureButtonPressed(btn.PressureChange);
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (!(state is GasCanisterBoundUserInterfaceState cast))
            {
                return;
            }

            _window?.UpdateState(cast);
        }
    }
}
