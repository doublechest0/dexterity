﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Utility;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Materials;
using Microsoft.CodeAnalysis.Options;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction
{
    public class ConstructionMenu : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;

        protected override Vector2? CustomSize => (400, 575);

        private ConstructionPrototype? _selected;
        private string[] _categories = Array.Empty<string>();

        private readonly ItemList _recipes;
        private readonly ItemList _stepList;
        private readonly Button _buildButton;
        private readonly Button _eraseButton;
        private readonly LineEdit _searchBar;
        private readonly OptionButton _category;
        private readonly TextureRect _targetTexture;
        private readonly RichTextLabel _targetName;
        private readonly RichTextLabel _targetDescription;

        public ConstructionMenu()
        {
            IoCManager.InjectDependencies(this);

            _placementManager.PlacementChanged += PlacementChanged;

            Title = "Construction";

            var vbox = new VBoxContainer() {SizeFlagsVertical = SizeFlags.FillExpand};

            var recipeContainer = new VBoxContainer() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.4f};

            var searchContainer = new HBoxContainer() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.1f};
            _searchBar = new LineEdit() {PlaceHolder = "Search", SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.6f};
            _category = new OptionButton() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.4f};

            _recipes = new ItemList() {SelectMode = ItemList.ItemListSelectMode.Single, SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.9f};

            var stepsContainer = new VBoxContainer() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.4f};
            var targetContainer = new HBoxContainer() {Align = BoxContainer.AlignMode.Center, SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.25f};
            _targetTexture = new TextureRect() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.15f, Stretch = TextureRect.StretchMode.KeepCentered};
            var targetInfoContainer = new VBoxContainer() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.85f};
            _targetName = new RichTextLabel() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.1f};
            _targetDescription = new RichTextLabel() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.9f};

            _stepList = new ItemList() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.75f, SelectMode = ItemList.ItemListSelectMode.None};

            var buttonContainer = new VBoxContainer() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.1f};
            _buildButton = new Button() {Disabled = true, ToggleMode = true, Text = "Place construction ghost", SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.5f};

            var eraseContainer = new HBoxContainer() {SizeFlagsVertical = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.5f};
            _eraseButton = new Button() {Text = "Eraser Mode", ToggleMode = true, SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.7f};
            var clearButton = new Button() {Text = "Clear All", SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 0.3f};

            recipeContainer.AddChild(searchContainer);
            recipeContainer.AddChild(_recipes);

            searchContainer.AddChild(_searchBar);
            searchContainer.AddChild(_category);

            targetInfoContainer.AddChild(_targetName);
            targetInfoContainer.AddChild(_targetDescription);

            targetContainer.AddChild(_targetTexture);
            targetContainer.AddChild(targetInfoContainer);

            stepsContainer.AddChild(targetContainer);
            stepsContainer.AddChild(_stepList);

            eraseContainer.AddChild(_eraseButton);
            eraseContainer.AddChild(clearButton);

            buttonContainer.AddChild(_buildButton);
            buttonContainer.AddChild(eraseContainer);

            vbox.AddChild(recipeContainer);
            vbox.AddChild(stepsContainer);
            vbox.AddChild(buttonContainer);
            Contents.AddChild(vbox);

            _recipes.OnItemSelected += RecipeSelected;
            _recipes.OnItemDeselected += RecipeDeselected;

            _searchBar.OnTextChanged += SearchTextChanged;
            _category.OnItemSelected += CategorySelected;

            _buildButton.OnToggled += BuildButtonToggled;
            clearButton.OnPressed += ClearAllButtonPressed;
            _eraseButton.OnToggled += EraseButtonToggled;

            PopulateCategories();
            PopulateAll();
        }

        private void PlacementChanged(object? sender, EventArgs e)
        {
            _buildButton.Pressed = false;
            _eraseButton.Pressed = false;
        }

        private void PopulateAll()
        {
            foreach (var recipe in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                _recipes.Add(GetItem(recipe, _recipes));
            }
        }

        private static ItemList.Item GetItem(ConstructionPrototype recipe, ItemList itemList)
        {
            return new ItemList.Item(itemList)
            {
                Metadata = recipe,
                Text = recipe.Name,
                Icon = recipe.Icon.Frame0(),
                TooltipEnabled = true,
                TooltipText = recipe.Description,
            };
        }

        private void PopulateBy(string search, string category)
        {
            _recipes.Clear();

            foreach (var recipe in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                if (!string.IsNullOrEmpty(search))
                {
                    if (!recipe.Name.ToLowerInvariant().Contains(search.Trim().ToLowerInvariant()))
                        continue;
                }

                if (!string.IsNullOrEmpty(category) && category != "All")
                {
                    if (recipe.Category != category)
                        continue;
                }

                _recipes.Add(GetItem(recipe, _recipes));
            }
        }

        private void PopulateCategories()
        {
            var uniqueCategories = new HashSet<string>();

            // hard-coded to show all recipes
            uniqueCategories.Add("All");

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                if (!string.IsNullOrEmpty(prototype.Category))
                    uniqueCategories.Add(prototype.Category);
            }

            _category.Clear();

            var array = uniqueCategories.ToArray();
            Array.Sort(array);

            for (var i = 0; i < array.Length; i++)
            {
                var category = array[i];
                _category.AddItem(category, i);
            }

            _categories = array;
        }

        private void PopulateInfo(ConstructionPrototype prototype)
        {
            ClearInfo();

            var isItem = prototype.Type == ConstructionType.Item;

            _buildButton.Disabled = false;
            _buildButton.Text = Loc.GetString(!isItem ? "Place construction ghost" : "Craft");
            _targetName.SetMessage(prototype.Name);
            _targetDescription.SetMessage(prototype.Description);
            _targetTexture.Texture = prototype.Icon.Frame0();

            if (!_prototypeManager.TryIndex(prototype.Graph, out ConstructionGraphPrototype graph))
                return;

            var startNode = graph.Nodes[prototype.StartNode];
            var targetNode = graph.Nodes[prototype.TargetNode];

            var path = graph.Path(startNode.Name, targetNode.Name);

            var current = startNode;

            var stepNumber = 1;

            Texture? GetTextureForStep(ConstructionGraphStep step)
            {
                switch (step)
                {
                    case MaterialConstructionGraphStep materialStep:
                        switch (materialStep.Material)
                        {
                            case StackType.Metal:
                                return _resourceCache.GetTexture("/Textures/Objects/Materials/sheet_metal.png");

                            case StackType.Glass:
                                return _resourceCache.GetTexture("/Textures/Objects/Materials/sheet_glass.png");

                            case StackType.Plasteel:
                                return _resourceCache.GetTexture("/Textures/Objects/Materials/sheet_plasteel.png");

                            case StackType.Phoron:
                                return _resourceCache.GetTexture("/Textures/Objects/Materials/sheet_phoron.png");

                            case StackType.Cable:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/cable_coil.png");

                        }
                        break;

                    case ToolConstructionGraphStep toolStep:
                        switch (toolStep.Tool)
                        {
                            case ToolQuality.Anchoring:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/wrench.rsi/icon.png");
                            case ToolQuality.Prying:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/crowbar.rsi/icon.png");
                            case ToolQuality.Screwing:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/screwdriver.rsi/screwdriver-map.png");
                            case ToolQuality.Cutting:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/wirecutters.rsi/cutters-map.png");
                            case ToolQuality.Welding:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/welder.rsi/welder.png");
                            case ToolQuality.Multitool:
                                return _resourceCache.GetTexture("/Textures/Objects/Tools/multitool.rsi/multitool.png");
                        }

                        break;

                    case ComponentConstructionGraphStep componentStep:
                        return componentStep.Icon?.Frame0();

                    case PrototypeConstructionGraphStep prototypeStep:
                        return prototypeStep.Icon?.Frame0();

                    case NestedConstructionGraphStep _:
                        return null;
                }

                return null;
            }

            foreach (var node in path)
            {
                var edge = current.GetEdge(node.Name);

                if (isItem && current == startNode)
                {
                    _stepList.AddItem(Loc.GetString($"{stepNumber++}. To craft this item, you need:"));
                }

                foreach (var step in edge.Steps)
                {
                    var icon = GetTextureForStep(step);

                    switch (step)
                    {
                        case MaterialConstructionGraphStep materialStep:
                            if(!isItem || current != startNode)
                                _stepList.AddItem(Loc.GetString($"{stepNumber++}. Add {materialStep.Amount} sheets of {materialStep.Material}"), icon);
                            else
                            {
                                _stepList.AddItem(Loc.GetString($"{materialStep.Amount}x {materialStep.Material}"), icon);
                            }
                            break;

                        case ToolConstructionGraphStep toolStep:
                            _stepList.AddItem(Loc.GetString($"{stepNumber++}. Use {0:a} {toolStep.Tool} tool.", toolStep.Tool), icon);
                            break;

                        case PrototypeConstructionGraphStep prototypeStep:
                            _stepList.AddItem(Loc.GetString($"{stepNumber++}. Add {0:a} {prototypeStep.Name}.", prototypeStep.Name), icon);
                            break;

                        case ComponentConstructionGraphStep componentStep:
                            _stepList.AddItem(Loc.GetString($"{stepNumber++}. Add {0:a} {componentStep.Name}.",
                                componentStep.Name), icon);
                            break;

                        case NestedConstructionGraphStep nestedStep:
                            var parallelNumber = 1;
                            _stepList.AddItem(Loc.GetString($"{stepNumber++}. In parallel..."));

                            foreach (var steps in nestedStep.Steps)
                            {
                                var subStepNumber = 1;

                                foreach (var subStep in steps)
                                {
                                    icon = GetTextureForStep(subStep);

                                    switch (subStep)
                                    {
                                        case MaterialConstructionGraphStep materialStep:
                                            if (!isItem)
                                                _stepList.AddItem(Loc.GetString($"    {stepNumber}.{parallelNumber}.{subStepNumber++}. Add {materialStep.Amount} sheets of {materialStep.Material}"), icon);
                                            break;

                                        case ToolConstructionGraphStep toolStep:
                                            _stepList.AddItem(Loc.GetString($"    {stepNumber}.{parallelNumber}.{subStepNumber++}. Use {0:a} {toolStep.Tool} tool.", toolStep.Tool), icon);
                                            break;

                                        case PrototypeConstructionGraphStep prototypeStep:
                                            _stepList.AddItem(Loc.GetString($"    {stepNumber}.{parallelNumber}.{subStepNumber++}. Add {0:a} {prototypeStep.Name}.", prototypeStep.Name), icon);
                                            break;

                                        case ComponentConstructionGraphStep componentStep:
                                            _stepList.AddItem(Loc.GetString($"    {stepNumber}.{parallelNumber}.{subStepNumber++}. Add {0:a} {componentStep.Name}.", componentStep.Name), icon);
                                            break;
                                    }
                                }

                                parallelNumber++;
                            }

                            break;
                    }
                }

                current = node;
            }
        }

        private void ClearInfo()
        {
            _buildButton.Disabled = true;
            _targetName.SetMessage(string.Empty);
            _targetDescription.SetMessage(string.Empty);
            _targetTexture.Texture = null;
            _stepList.Clear();
        }

        private void RecipeSelected(ItemList.ItemListSelectedEventArgs obj)
        {
            _selected = (ConstructionPrototype) obj.ItemList[obj.ItemIndex].Metadata!;
            PopulateInfo(_selected);
        }

        private void RecipeDeselected(ItemList.ItemListDeselectedEventArgs obj)
        {
            _selected = null;
            ClearInfo();
        }

        private void CategorySelected(OptionButton.ItemSelectedEventArgs obj)
        {
            _category.SelectId(obj.Id);
            PopulateBy(_searchBar.Text, _categories[obj.Id]);
        }

        private void SearchTextChanged(LineEdit.LineEditEventArgs obj)
        {
            PopulateBy(_searchBar.Text, _categories[_category.SelectedId]);
        }

        private void BuildButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Pressed)
            {
                if (_selected == null) return;

                var constructSystem = EntitySystem.Get<ConstructionSystem>();

                if (_selected.Type == ConstructionType.Item)
                {
                    constructSystem.TryStartItemConstruction(_selected.ID);
                    _buildButton.Pressed = false;
                    return;
                }

                _placementManager.BeginPlacing(new PlacementInformation()
                {
                    IsTile = false,
                    PlacementOption = _selected.PlacementMode,
                }, new ConstructionPlacementHijack(constructSystem, _selected));
            }
            else
            {
                _placementManager.Clear();
            }

            _buildButton.Pressed = args.Pressed;
        }

        private void EraseButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Pressed) _placementManager.Clear();
            _placementManager.ToggleEraserHijacked(new ConstructionPlacementHijack(_systemManager.GetEntitySystem<ConstructionSystem>(), null));
            _eraseButton.Pressed = args.Pressed;
        }

        private void ClearAllButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            var constructionSystem = EntitySystem.Get<ConstructionSystem>();

            constructionSystem.ClearAllGhosts();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _placementManager.PlacementChanged -= PlacementChanged;
            }
        }
    }
}
