﻿using System.Globalization;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chat.V2;

public abstract partial class SharedChatSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly IConfigurationManager Configuration = default!;

    protected bool UpperCaseMessagesMeanShouting;
    protected bool CurrentCultureIsSomeFormOfEnglish;
    protected bool ShouldPunctuate;
    protected int MaxChatMessageLength;
    protected int MaxAnnouncementMessageLength;

    public static string GetStringInsideTag(string message, string tag)
    {
        var tagStart = message.IndexOf($"[{tag}]", StringComparison.Ordinal);
        var tagEnd = message.IndexOf($"[/{tag}]", StringComparison.Ordinal);
        if (tagStart < 0 || tagEnd < 0)
            return "";
        tagStart += tag.Length + 2;
        return message.Substring(tagStart, tagEnd - tagStart);
    }

    public override void Initialize()
    {
        base.Initialize();

        CurrentCultureIsSomeFormOfEnglish = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
                                    || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        Configuration.OnValueChanged(CCVars.ChatPunctuation, shouldPunctuate => ShouldPunctuate = shouldPunctuate, true);
        Configuration.OnValueChanged(CCVars.ChatMaxAnnouncementLength, maxLen => MaxChatMessageLength = maxLen, true);
        Configuration.OnValueChanged(CCVars.ChatMaxMessageLength, maxLen => MaxAnnouncementMessageLength = maxLen, true);
        Configuration.OnValueChanged(CCVars.ChatUpperCaseMeansShouting, maxLen => UpperCaseMessagesMeanShouting = maxLen, true);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

        InitializeEmote();
        InitializeRadio();
    }

    protected void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<RadioChannelPrototype>())
            CacheRadios();

        if (obj.WasModified<EmotePrototype>())
            CacheEmotes();
    }

}
