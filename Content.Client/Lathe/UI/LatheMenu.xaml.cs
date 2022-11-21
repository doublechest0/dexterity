﻿using System.Text;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Lathe.UI;

[GenerateTypedNameReferences]
public sealed partial class LatheMenu : DefaultWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly SpriteSystem _spriteSystem;
    private readonly LatheSystem _lathe;

    public event Action<BaseButton.ButtonEventArgs>? OnQueueButtonPressed;
    public event Action<BaseButton.ButtonEventArgs>? OnServerListButtonPressed;
    public event Action<BaseButton.ButtonEventArgs>? OnServerSyncButtonPressed;
    public event Action<string, int>? RecipeQueueAction;

    public List<string> Recipes = new();

    public LatheMenu(LatheBoundUserInterface owner)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _lathe = _entityManager.EntitySysManager.GetEntitySystem<LatheSystem>();

        Title = _entityManager.GetComponent<MetaDataComponent>(owner.Lathe).EntityName;

        SearchBar.OnTextChanged += _ =>
        {
            PopulateRecipes(owner.Lathe);
        };
        AmountLineEdit.OnTextChanged += _ =>
        {
            PopulateRecipes(owner.Lathe);
        };

        QueueButton.OnPressed += a => OnQueueButtonPressed?.Invoke(a);
        ServerListButton.OnPressed += a => OnServerListButtonPressed?.Invoke(a);

        //refresh the bui state
        ServerSyncButton.OnPressed += a => OnServerSyncButtonPressed?.Invoke(a);

        if (_entityManager.TryGetComponent<LatheComponent>(owner.Lathe, out var latheComponent))
        {
            if (latheComponent.DynamicRecipes == null)
            {
                ServerListButton.Visible = false;
                ServerSyncButton.Visible = false;
            }
        }
    }

    public void PopulateMaterials(EntityUid lathe)
    {
        if (!_entityManager.TryGetComponent<MaterialStorageComponent>(lathe, out var materials))
            return;

        Materials.Clear();
        foreach (var (id, amount) in materials.Storage)
        {
            if (!_prototypeManager.TryIndex(id, out MaterialPrototype? material))
                continue;
            var name = Loc.GetString(material.Name);
            var mat = Loc.GetString("lathe-menu-material-display",
                ("material", name), ("amount", amount));
            Materials.AddItem(mat, _spriteSystem.Frame0(material.Icon), false);
        }
        PopulateRecipes(lathe);
    }

    /// <summary>
    /// Populates the list of all the recipes
    /// </summary>
    /// <param name="lathe"></param>
    public void PopulateRecipes(EntityUid lathe)
    {
        if (!_entityManager.TryGetComponent<LatheComponent>(lathe, out var component))
            return;

        var recipesToShow = new List<LatheRecipePrototype>();
        foreach (var recipe in Recipes)
        {
            if (!_prototypeManager.TryIndex<LatheRecipePrototype>(recipe, out var proto))
                continue;

            if (SearchBar.Text.Trim().Length != 0)
            {
                if (proto.Name.ToLowerInvariant().Contains(SearchBar.Text.Trim().ToLowerInvariant()))
                    recipesToShow.Add(proto);
            }
            else
            {
                recipesToShow.Add(proto);
            }
        }

        if (!int.TryParse(AmountLineEdit.Text, out var quantity) || quantity <= 0)
            quantity = 1;

        RecipeList.Children.Clear();
        foreach (var prototype in recipesToShow)
        {
            StringBuilder sb = new();
            var first = true;
            foreach (var (id, amount) in prototype.RequiredMaterials)
            {
                if (!_prototypeManager.TryIndex<MaterialPrototype>(id, out var proto))
                    continue;

                if (first)
                    first = false;
                else
                    sb.Append('\n');

                var adjustedAmount = amount;
                if (prototype.ApplyMaterialDiscount)
                    adjustedAmount = (int) (adjustedAmount * component.MaterialUseMultiplier);

                sb.Append(adjustedAmount);
                sb.Append(' ');
                sb.Append(Loc.GetString(proto.Name));
            }

            var icon = _spriteSystem.Frame0(prototype.Icon);
            var canProduce = _lathe.CanProduce(lathe, prototype, quantity);

            var control = new RecipeControl(prototype, sb.ToString(), canProduce, icon);
            control.OnButtonPressed += s =>
            {
                if (!int.TryParse(AmountLineEdit.Text, out var amount) || amount <= 0)
                    amount = 1;
                RecipeQueueAction?.Invoke(s, amount);
            };
            RecipeList.AddChild(control);
        }
    }
}
