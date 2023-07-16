﻿using Content.Server.Mind.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);

        SubscribeLocalEvent<MMILinkedComponent, MindAddedMessage>(OnMMILinkedMindAdded);
        SubscribeLocalEvent<MMILinkedComponent, EntGotRemovedFromContainerMessage>(OnMMILinkedRemoved);
    }

    private void OnMMIInit(EntityUid uid, MMIComponent component, ComponentInit args)
    {
        if (_itemSlots.TryGetSlot(uid, component.BrainSlotId, out var slot))
            component.BrainSlot = slot;
        else
            _itemSlots.AddItemSlot(uid, component.BrainSlotId, component.BrainSlot);
    }

    private void OnMMIEntityInserted(EntityUid uid, MMIComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.BrainSlotId)
            return;

        var ent = args.Entity;
        var linked = EnsureComp<MMILinkedComponent>(ent);
        linked.LinkedMMI = uid;

        if (_mind.TryGetMind(ent, out var mind))
        {
            _mind.TransferTo(mind, uid, true);
        }

        UpdateMMIVisuals(uid, true, component);
    }

    private void OnMMIMindAdded(EntityUid uid, MMIComponent component, MindAddedMessage args)
    {
        UpdateMMIVisuals(uid, true, component);
    }

    private void OnMMIMindRemoved(EntityUid uid, MMIComponent component, MindRemovedMessage args)
    {
        UpdateMMIVisuals(uid, true, component);
    }

    private void OnMMILinkedMindAdded(EntityUid uid, MMILinkedComponent component, MindAddedMessage args)
    {
        if (!_mind.TryGetMind(uid, out var mind) || component.LinkedMMI == null)
            return;
        _mind.TransferTo(mind, component.LinkedMMI, true);
    }

    private void OnMMILinkedRemoved(EntityUid uid, MMILinkedComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (component.LinkedMMI is not {} linked)
            return;
        if (_mind.TryGetMind(linked, out var mind))
            _mind.TransferTo(mind, uid, true);
        UpdateMMIVisuals(linked, false);
        RemCompDeferred(uid, component);
    }

    private void UpdateMMIVisuals(EntityUid uid, bool hasBrain, MMIComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, MMIVisuals.BrainPresent, hasBrain, appearance);
        var hasMind = TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind;
        _appearance.SetData(uid, MMIVisuals.HasMind, hasMind, appearance);
    }
}
