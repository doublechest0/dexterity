﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration.Logs;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.Administration.UI.Logs;

[GenerateTypedNameReferences]
public partial class AdminLogsWindow : SS14Window
{
    private readonly Comparer<AdminLogTypeButton> _adminLogTypeButtonComparer =
        Comparer<AdminLogTypeButton>.Create((a, b) =>
            string.Compare(a.Type.ToString(), b.Type.ToString(), StringComparison.Ordinal));

    private readonly Comparer<AdminLogPlayerButton> _adminLogPlayerButtonComparer =
        Comparer<AdminLogPlayerButton>.Create((a, b) =>
            string.Compare(a.Text, b.Text, StringComparison.Ordinal));

    public AdminLogsWindow()
    {
        RobustXamlLoader.Load(this);

        TypeSearch.OnTextChanged += TypeSearchChanged;
        PlayerSearch.OnTextChanged += PlayerSearchChanged;
        LogSearch.OnTextChanged += LogSearchChanged;

        SelectAllTypesButton.OnPressed += SelectAllTypes;

        SelectNoTypesButton.OnPressed += SelectNoTypes;
        SelectNoPlayersButton.OnPressed += SelectNoPlayers;

        SetTypes(Enum.GetValues<LogType>());
    }

    private void TypeSearchChanged(LineEditEventArgs args)
    {
        UpdateTypes();
    }

    private void PlayerSearchChanged(LineEditEventArgs obj)
    {
        UpdatePlayers();
    }

    private void LogSearchChanged(LineEditEventArgs args)
    {
        UpdateLogs();
    }

    private void SelectAllTypes(ButtonEventArgs obj)
    {
        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            type.Pressed = true;
        }

        UpdateLogs();
    }

    private void SelectNoTypes(ButtonEventArgs obj)
    {
        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            type.Pressed = false;
            type.Visible = ShouldShowType(type);
        }

        UpdateLogs();
    }

    private void SelectNoPlayers(ButtonEventArgs obj)
    {
        foreach (var control in PlayersContainer.Children)
        {
            if (control is not AdminLogPlayerButton player)
            {
                continue;
            }

            player.Pressed = false;
        }

        UpdateLogs();
    }

    public void UpdateTypes()
    {
        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            type.Visible = ShouldShowType(type);
        }
    }

    private void UpdatePlayers()
    {
        foreach (var control in PlayersContainer.Children)
        {
            if (control is not AdminLogPlayerButton player)
            {
                continue;
            }

            player.Visible = ShouldShowPlayer(player);
        }
    }

    private void UpdateLogs()
    {
        foreach (var child in LogsContainer.Children)
        {
            if (child is not AdminLogLabel log)
            {
                continue;
            }

            child.Visible = ShouldShowLog(log);
        }
    }

    private bool ShouldShowType(AdminLogTypeButton button)
    {
        return button.Text != null &&
               button.Text.Contains(TypeSearch.Text, StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldShowPlayer(AdminLogPlayerButton button)
    {
        return button.Text != null &&
               button.Text.Contains(PlayerSearch.Text, StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldShowLog(AdminLogLabel label)
    {
        return label.Log.Message.Contains(LogSearch.Text, StringComparison.OrdinalIgnoreCase);
    }

    private void TypeButtonPressed(ButtonEventArgs args)
    {
        UpdateLogs();
    }

    private void PlayerButtonPressed(ButtonEventArgs args)
    {
        UpdateLogs();
    }

    public void SetTypes(LogType[] types)
    {
        var newTypes = types.ToHashSet();
        var buttons = new SortedSet<AdminLogTypeButton>(_adminLogTypeButtonComparer);

        foreach (var control in TypesContainer.Children.ToArray())
        {
            if (control is not AdminLogTypeButton type ||
                !newTypes.Remove(type.Type))
            {
                continue;
            }

            buttons.Add(type);
        }

        foreach (var type in newTypes)
        {
            var button = new AdminLogTypeButton(type)
            {
                Text = type.ToString(),
                ToggleMode = true,
                Pressed = true
            };

            button.OnPressed += TypeButtonPressed;

            buttons.Add(button);
        }

        TypesContainer.RemoveAllChildren();

        foreach (var type in buttons)
        {
            TypesContainer.AddChild(type);
        }

        UpdateLogs();
    }

    public void SetPlayers(Dictionary<Guid, string> players)
    {
        var buttons = new SortedSet<AdminLogPlayerButton>(_adminLogPlayerButtonComparer);

        foreach (var control in PlayersContainer.Children.ToArray())
        {
            if (control is not AdminLogPlayerButton player ||
                !players.Remove(player.Id))
            {
                continue;
            }

            buttons.Add(player);
        }

        foreach (var (id, name) in players)
        {
            var button = new AdminLogPlayerButton(id)
            {
                Text = name,
                ToggleMode = true
            };

            button.OnPressed += PlayerButtonPressed;

            buttons.Add(button);
        }

        PlayersContainer.RemoveAllChildren();

        foreach (var player in buttons)
        {
            PlayersContainer.AddChild(player);
        }

        UpdateLogs();
    }

    public void AddLogs(SharedAdminLog[] logs)
    {
        for (var i = 0; i < logs.Length; i++)
        {
            ref var log = ref logs[i];
            var label = new AdminLogLabel(ref log);
            label.Visible = ShouldShowLog(label);

            LogsContainer.AddChild(label);
            LogsContainer.AddChild(new HSeparator());
        }
    }

    public void SetLogs(SharedAdminLog[] logs)
    {
        LogsContainer.RemoveAllChildren();
        AddLogs(logs);
    }

    public List<LogType> GetSelectedLogTypes()
    {
        var types = new List<LogType>();

        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton {Text: { }, Pressed: true} type)
            {
                continue;
            }

            types.Add(Enum.Parse<LogType>(type.Text));
        }

        return types;
    }

    public Guid[] GetSelectedPlayerIds()
    {
        var players = new List<Guid>();

        foreach (var control in PlayersContainer.Children)
        {
            if (control is not AdminLogPlayerButton {Pressed: true} player)
            {
                continue;
            }

            players.Add(player.Id);
        }

        return players.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        TypeSearch.OnTextChanged -= TypeSearchChanged;
        PlayerSearch.OnTextChanged -= PlayerSearchChanged;
        LogSearch.OnTextChanged -= LogSearchChanged;

        SelectAllTypesButton.OnPressed -= SelectAllTypes;

        SelectNoTypesButton.OnPressed -= SelectNoTypes;
        SelectNoPlayersButton.OnPressed -= SelectNoPlayers;
    }
}
