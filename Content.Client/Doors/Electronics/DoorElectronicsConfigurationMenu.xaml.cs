using System.Linq;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Content.Client.Doors.Electronics;
using Content.Shared.Access;
using Content.Shared.Doors.Electronics;
using Robust.Shared.Prototypes;
using FancyWindow = Content.Client.UserInterface.Controls.FancyWindow;

namespace Content.Client.Doors.Electronics
{
    [GenerateTypedNameReferences]
    public sealed partial class DoorElectronicsConfigurationMenu : FancyWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<string, Button> _accessButtons = new();
        private readonly DoorElectronicsBoundUserInterface _owner;

        public DoorElectronicsConfigurationMenu(DoorElectronicsBoundUserInterface ui, List<string> accessLevels, IPrototypeManager prototypeManager)
        {
            RobustXamlLoader.Load(this);

            _owner = ui;

            foreach (var access in accessLevels)
            {
                if (!prototypeManager.TryIndex<AccessLevelPrototype>(access, out var accessLevel))
                {
                    continue;
                }

                var newButton = new Button
                {
                    Text = GetAccessLevelName(accessLevel),
                    ToggleMode = true,
                };
                AccessLevelGrid.AddChild(newButton);
                _accessButtons.Add(accessLevel.ID, newButton);
                newButton.OnPressed += _ => UpdateConfiguration();
            }
        }

        private static string GetAccessLevelName(AccessLevelPrototype prototype)
        {
            if (prototype.Name is { } name)
                return Loc.GetString(name);

            return prototype.ID;
        }

        public void UpdateState(SharedDoorElectronicsComponent.ConfigurationState state)
        {
            foreach (var (accessName, button) in _accessButtons)
            {
                button.Pressed = state.accessList.Contains(accessName);
            }
        }

        private void UpdateConfiguration()
        {
            _owner.UpdateConfiguration(
                _accessButtons.Where(x => x.Value.Pressed).Select(x => x.Key).ToList());
        }
    }
}
