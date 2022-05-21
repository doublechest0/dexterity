using Content.Shared.DeviceNetwork;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.SurveillanceCamera.UI;

[GenerateTypedNameReferences]
public sealed partial class SurveillanceCameraSetupWindow : DefaultWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public Action<string>? OnNameConfirm;
    public Action<int>? OnNetworkConfirm;

    public SurveillanceCameraSetupWindow()
    {
        RobustXamlLoader.Load(this);

        IoCManager.InjectDependencies(this);

        NetworkConfirm.OnPressed += _ => OnNetworkConfirm!(NetworkSelector.SelectedId);
        NameConfirm.OnPressed += _ => OnNameConfirm!(DeviceName.Text);
        NetworkSelector.OnItemSelected += args => NetworkSelector.SelectId(args.Id);
    }

    public void HideNameSelector() => NamingSection.Visible = false;

    public void UpdateState(string name, bool disableNaming, bool disableNetworkSelector)
    {
        DeviceName.Text = name;
        DeviceName.Editable = !disableNaming;
        NameConfirm.Disabled = disableNaming;

        NetworkSelector.Disabled = disableNetworkSelector;
        NetworkConfirm.Disabled = disableNetworkSelector;
    }

    // Pass in a list of frequency prototype IDs.
    public void LoadAvailableNetworks(string currentNetwork, List<string> networks)
    {
        NetworkSelector.Clear();

        if (networks.Count == 0)
        {
            NetworkSection.Visible = false;
            return;
        }

        var id = 0;
        foreach (var network in networks)
        {
            if (!_prototypeManager.TryIndex(network, out DeviceFrequencyPrototype? frequency)
                || frequency.Name == null)
            {
                id++;
                continue;
            }

            NetworkSelector.AddItem(Loc.GetString(frequency.Name), id);
            if (network == currentNetwork)
            {
                NetworkSelector.SelectId(id);
            }

            id++;
        }
    }
}
