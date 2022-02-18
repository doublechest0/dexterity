using System.Text;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using static Content.Shared.HealthScanner.SharedHealthScannerComponent;

namespace Content.Client.HealthScanner.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class HealthScannerWindow : DefaultWindow
    {
        public HealthScannerWindow()
        {
            RobustXamlLoader.Load(this);
        }


        public void Populate(HealthComponentDamageMessage state)
        {
            var text = new StringBuilder();

            if (state.TargetName == null || state.IsAlive == null)
            {
                Diagnostics.Text = Loc.GetString("health-scanner-window-no-patient-data-text");
                SetSize = (250, 600);
            }
            else
            {
                text.Append($"{Loc.GetString("health-scanner-window-entity-health-text", ("entityName", state.TargetName))}\n");
                var totalDamageAmount = state.TotalDamage != null ? state.TotalDamage : "Unknown";
                text.Append($"{Loc.GetString("health-scanner-window-entity-damage-total-text", ("amount", totalDamageAmount))}\n");


                // Show the total damage and type breakdown for each damage group.
                foreach (var damageGroup in state.DamageGroups)
                {
                    string damageGroupName = damageGroup.GroupName != null ? damageGroup.GroupName : "Unknown";
                    string damageGroupTotalDamage = damageGroup.GroupTotalDamage != null ? damageGroup.GroupTotalDamage : "Unknown";
                    text.Append($"\n{Loc.GetString("health-scanner-window-damage-group-text", ("damageGroup", damageGroupName), ("amount", damageGroupTotalDamage))}");

                    // Show the damage for each type in that group.
                    if (damageGroup.GroupedMinorDamages != null)
                    {
                        foreach (var minorDamage in damageGroup.GroupedMinorDamages)
                        {
                            text.Append($"\n- {Loc.GetString("health-scanner-window-damage-type-text", ("damageType", minorDamage.Key), ("amount", minorDamage.Value))}");
                        }
                    }
                    text.Append('\n');
                }
                Diagnostics.Text = text.ToString();
            }
            SetSize = (250, 600);
        }
    }
}
