using Content.Shared.StationRecords;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.StationRecords;

[GenerateTypedNameReferences]
public sealed partial class GeneralStationRecordConsoleWindow : DefaultWindow
{
    public Action<uint?>? OnKeySelected;

    public Action<GeneralStationRecordFilterType, string>? OnFiltersChanged;

    private bool _isPopulating;

    private GeneralStationRecordFilterType _currentFilterType;

    public GeneralStationRecordConsoleWindow()
    {
        RobustXamlLoader.Load(this);

        _currentFilterType = GeneralStationRecordFilterType.Name;

        foreach (var item in Enum.GetValues<GeneralStationRecordFilterType>())
        {
            StationRecordsFilterType.AddItem(GetTypeFilterLocals(item), (int)item);
        }

        RecordListing.OnItemSelected += args =>
        {
            if (_isPopulating || RecordListing[args.ItemIndex].Metadata is not uint cast)
                return;

            OnKeySelected?.Invoke(cast);
        };

        RecordListing.OnItemDeselected += _ =>
        {
            if (!_isPopulating)
                OnKeySelected?.Invoke(null);
        };

        StationRecordsFilterType.OnItemSelected += eventArgs =>
        {
            var type = (GeneralStationRecordFilterType) eventArgs.Id;

            if (_currentFilterType != type)
            {
                _currentFilterType = type;
                FilterListingOfRecords();
            }
        };

        StationRecordsFiltersValue.OnTextEntered += args =>
        {
            FilterListingOfRecords(args.Text);
        };

        StationRecordsFilters.OnPressed += _ =>
        {
            FilterListingOfRecords(StationRecordsFiltersValue.Text);
        };

        StationRecordsFiltersReset.OnPressed += _ =>
        {
            StationRecordsFiltersValue.Text = "";
            FilterListingOfRecords();
        };
    }

    public void UpdateState(GeneralStationRecordConsoleState state)
    {
        if (state.Filter != null)
        {
            if (state.Filter.Type != _currentFilterType)
            {
                _currentFilterType = state.Filter.Type;
            }

            if (state.Filter.Value != StationRecordsFiltersValue.Text)
            {
                StationRecordsFiltersValue.Text = state.Filter.Value;
            }
        }

        StationRecordsFilterType.SelectId((int)_currentFilterType);

        if (state.RecordListing == null)
        {
            RecordListingStatus.Visible = true;
            RecordListing.Visible = false;
            RecordListingStatus.Text = Loc.GetString("general-station-record-console-empty-state");
            RecordContainer.Visible = false;
            RecordContainerStatus.Visible = false;
            return;
        }

        RecordListingStatus.Visible = false;
        RecordListing.Visible = true;
        RecordContainer.Visible = true;

        PopulateRecordListing(state.RecordListing!, state.SelectedKey);

        RecordContainerStatus.Visible = state.Record == null;

        if (state.Record != null)
        {
            RecordContainerStatus.Visible = state.SelectedKey == null;
            RecordContainerStatus.Text = state.SelectedKey == null
                ? Loc.GetString("general-station-record-console-no-record-found")
                : Loc.GetString("general-station-record-console-select-record-info");
            PopulateRecordContainer(state.Record);
        }
        else
        {
            RecordContainer.DisposeAllChildren();
            RecordContainer.RemoveAllChildren();
        }
    }
    private void PopulateRecordListing(Dictionary<uint, string> listing, uint? selected)
    {
        RecordListing.Clear();
        RecordListing.ClearSelected();

        _isPopulating = true;

        foreach (var (key, name) in listing)
        {
            var item = RecordListing.AddItem(name);
            item.Metadata = key;
            item.Selected = key == selected;
        }
        _isPopulating = false;

        RecordListing.SortItemsByText();
    }

    private void PopulateRecordContainer(GeneralStationRecord record)
    {
        RecordContainer.DisposeAllChildren();
        RecordContainer.RemoveAllChildren();
        // sure
        var recordControls = new Control[]
        {
            new Label()
            {
                Text = record.Name,
                StyleClasses = { "LabelBig" }
            },
            new Label()
            {
                Text = Loc.GetString("general-station-record-console-record-age", ("age", record.Age.ToString()))

            },
            new Label()
            {
                Text = Loc.GetString("general-station-record-console-record-title", ("job", Loc.GetString(record.JobTitle)))
            },
            new Label()
            {
                Text = Loc.GetString("general-station-record-console-record-species", ("species", record.Species))
            },
            new Label()
            {
                Text = Loc.GetString("general-station-record-console-record-gender", ("gender", record.Gender.ToString()))
            },
            new Label()
            {
                Text = Loc.GetString("general-station-record-console-record-fingerprint", ("fingerprint", record.Fingerprint ?? Loc.GetString("generic-not-available-shorthand")))
            },
            new Label()
            {
                Text = Loc.GetString("general-station-record-console-record-dna", ("dna", record.DNA ?? Loc.GetString("generic-not-available-shorthand")))
            }
        };

        foreach (var control in recordControls)
        {
            RecordContainer.AddChild(control);
        }
    }

    private void FilterListingOfRecords(string text = "")
    {
        if (!_isPopulating)
        {
            OnFiltersChanged?.Invoke(_currentFilterType, text);
        }
    }

    private string GetTypeFilterLocals(GeneralStationRecordFilterType type)
    {
        return Loc.GetString($"general-station-record-{type.ToString().ToLower()}-filter");
    }
}
