using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///  Used by ghost for be able to see revolutionary and zombies
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowAntagIconsComponent : Component {
    public bool Hideheadrev = true;

    // Parameterless constructor
    public ShowAntagIconsComponent() { }

    public ShowAntagIconsComponent(bool hideheadrev)
    {
        Hideheadrev = hideheadrev;
    }
}
