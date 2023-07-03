using Content.Shared.Disposal.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using static Content.Shared.Disposal.Components.DisposalUnitComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Client-side UI used to control a <see cref="DisposalUnitComponent"/>
    /// </summary>
    [GenerateTypedNameReferences]
    public sealed partial class DisposalUnitWindow : DefaultWindow
    {
        public DisposalUnitWindow()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);
        }

        /// <summary>
        /// Update the interface state for the disposals window.
        /// </summary>
        /// <returns>true if we should stop updating every frame.</returns>
        public bool UpdateState(DisposalUnitBoundUserInterfaceState state)
        {
            Title = state.UnitName;
            UnitState.Text = state.UnitState;
            Power.Pressed = state.Powered;
            Engage.Pressed = state.Engaged;

            return !state.Powered || PressureBar.UpdatePressure(state.FullPressureTime);
        }
    }
}
