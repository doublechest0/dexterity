using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.HUD;
using Content.Client.Inventory;
using Content.Client.Items.Managers;
using Content.Client.Items.UI;
using Content.Client.Resources;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Hands.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class HandsGui : HudWidget
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;
        private string StorageTexture => "back.png";
        private Texture BlockedTexture => _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");
        public HandsGui()
        {
            RobustXamlLoader.Load(this);
        }
    }
}
