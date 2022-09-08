using Content.Client.Administration.UI.CustomControls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface
{
    [GenerateTypedNameReferences]
    public sealed partial class StatsWindow : DefaultWindow
    {
        public StatsWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
        }

        public void UpdateValues(List<string> headers, List<string[]> values)
        {
            Values.DisposeAllChildren();
            Values.Columns = headers.Count;

            for (var i = 0; i < headers.Count; i++)
            {
                var item = headers[i];
                Values.AddChild(new Label()
                {
                    Text = item,
                });
            }

            values.Sort((x, y) => string.Compare(x[0], y[0], StringComparison.Ordinal));

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];

                for (var j = 0; j < value.Length; j++)
                {
                    Values.AddChild(new Label()
                    {
                        Text = value[j],
                    });
                }
            }
        }
    }
}
