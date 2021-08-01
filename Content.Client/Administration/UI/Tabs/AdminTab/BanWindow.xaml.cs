﻿using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.Tabs.AdminTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class BanWindow : SS14Window
    {

        protected override void EnteredTree()
        {
            PlayerNameLine.OnTextChanged += PlayerNameLineOnOnTextChanged;
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
        }

        private void PlayerNameLineOnOnTextChanged(LineEdit.LineEditEventArgs obj)
        {
            SubmitButton.Disabled = string.IsNullOrEmpty(PlayerNameLine.Text);
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            // Small verification if Player Name exists
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"ban \"{PlayerNameLine.Text}\" \"{CommandParsing.Escape(ReasonLine.Text)}\" {MinutesLine.Text}");
        }
    }
}
