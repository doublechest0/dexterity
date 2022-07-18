﻿
using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AirlockSystem _airlockSystem = default!;
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly AdminTestArenaSystem _adminTestArenaSystem = default!;

    private void AddTricksVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
        {
            if (TryComp<AirlockComponent>(args.Target, out var airlock))
            {
                Verb bolt = new()
                {
                    Text = airlock.BoltsDown ? "Unbolt" : "Bolt",
                    Category = VerbCategory.Tricks,
                    IconTexture = airlock.BoltsDown ? "/Textures/Interface/AdminActions/unbolt.png" : "/Textures/Interface/AdminActions/bolt.png",
                    Act = () =>
                    {
                        airlock.SetBoltsWithAudio(!airlock.BoltsDown);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString(airlock.BoltsDown ? "admin-trick-unbolt-description": "admin-trick-bolt-description"),
                    Priority = (int) (airlock.BoltsDown ? TricksVerbPriorities.Unbolt : TricksVerbPriorities.Bolt),

                };
                args.Verbs.Add(bolt);

                Verb emergencyAccess = new()
                {
                    Text = airlock.EmergencyAccess ? "Emergency Access Off" : "Emergency Access On",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Interface/AdminActions/emergency_access.png",
                    Act = () =>
                    {
                        _airlockSystem.ToggleEmergencyAccess(airlock);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString(airlock.EmergencyAccess ? "admin-trick-emergency-access-off-description" : "admin-trick-emergency-access-on-description" ),
                    Priority = (int) (airlock.EmergencyAccess ? TricksVerbPriorities.EmergencyAccessOff : TricksVerbPriorities.EmergencyAccessOn),
                };
                args.Verbs.Add(emergencyAccess);
            }

            if (HasComp<DamageableComponent>(args.Target))
            {
                Verb rejuvenate = new()
                {
                    Text = "Rejuvenate",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Interface/AdminActions/rejuvenate.png",
                    Act = () =>
                    {
                        RejuvenateCommand.PerformRejuvenate(args.Target);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-rejuvenate-description"),
                    Priority = (int) TricksVerbPriorities.Rejuvenate,
                };
                args.Verbs.Add(rejuvenate);
            }

            if (!_godmodeSystem.HasGodmode(args.Target))
            {
                Verb makeIndestructible = new()
                {
                    Text = "Make Indestructible",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Interface/VerbIcons/plus.svg.192dpi.png",
                    Act = () =>
                    {
                        _godmodeSystem.EnableGodmode(args.Target);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-make-indestructible-description"),
                    Priority = (int) TricksVerbPriorities.MakeIndestructible,
                };
                args.Verbs.Add(makeIndestructible);
            }
            else
            {
                Verb makeVulnerable = new()
                {
                    Text = "Make Vulnerable",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Interface/VerbIcons/plus.svg.192dpi.png",
                    Act = () =>
                    {
                        _godmodeSystem.DisableGodmode(args.Target);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-make-vulnerable-description"),
                    Priority = (int) TricksVerbPriorities.MakeVulnerable,
                };
                args.Verbs.Add(makeVulnerable);
            }

            if (TryComp<BatteryComponent>(args.Target, out var battery))
            {
                Verb refillBattery = new()
                {
                    Text = "Refill Battery",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Power/power_cells.rsi/medium.png",
                    Act = () =>
                    {
                        battery.CurrentCharge = battery.MaxCharge;
                        Dirty(battery);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-refill-battery-description"),
                    Priority = (int) TricksVerbPriorities.RefillBattery,
                };
                args.Verbs.Add(refillBattery);
            }

            if (TryComp<AnchorableComponent>(args.Target, out var anchor))
            {
                Verb blockUnanchor = new()
                {
                    Text = "Block Unanchoring",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Interface/VerbIcons/anchor.svg.192dpi.png",
                    Act = () =>
                    {
                        RemComp(args.Target, anchor);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-block-unanchoring-description"),
                    Priority = (int) TricksVerbPriorities.BlockUnanchoring,
                };
                args.Verbs.Add(blockUnanchor);
            }

            if (TryComp<GasTankComponent>(args.Target, out var tank))
            {
                Verb refillInternalsO2 = new()
                {
                    Text = "Refill Internals Oxygen",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Tanks/oxygen.rsi/icon.png",
                    Act = () =>
                    {
                        RefillGasTank(args.Target, Gas.Oxygen, tank);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-internals-refill-oxygen-description"),
                    Priority = (int) TricksVerbPriorities.RefillOxygen,
                };
                args.Verbs.Add(refillInternalsO2);

                Verb refillInternalsN2 = new()
                {
                    Text = "Refill Internals Nitrogen",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Tanks/red.rsi/icon.png",
                    Act = () =>
                    {
                        RefillGasTank(args.Target, Gas.Nitrogen, tank);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-internals-refill-nitrogen-description"),
                    Priority = (int) TricksVerbPriorities.RefillNitrogen,
                };
                args.Verbs.Add(refillInternalsN2);

                Verb refillInternalsPlasma = new()
                {
                    Text = "Refill Internals Plasma",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Tanks/plasma.rsi/icon.png",
                    Act = () =>
                    {
                        RefillGasTank(args.Target, Gas.Plasma, tank);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-internals-refill-plasma-description"),
                    Priority = (int) TricksVerbPriorities.RefillPlasma,
                };
                args.Verbs.Add(refillInternalsPlasma);
            }

            if (TryComp<InventoryComponent>(args.Target, out var inventory))
            {
                Verb refillInternalsO2 = new()
                {
                    Text = "Refill Internals Oxygen",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Tanks/oxygen.rsi/icon.png",
                    Act = () =>
                    {
                        foreach (var slot in _inventorySystem.GetSlots(args.Target))
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var entity))
                                continue;

                            if (!TryComp<GasTankComponent>(entity, out var tank))
                                continue;

                            RefillGasTank(entity.Value, Gas.Oxygen, tank);
                        }
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-internals-refill-oxygen-description"),
                    Priority = (int) TricksVerbPriorities.RefillOxygen,
                };
                args.Verbs.Add(refillInternalsO2);

                Verb refillInternalsN2 = new()
                {
                    Text = "Refill Internals Nitrogen",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Tanks/red.rsi/icon.png",
                    Act = () =>
                    {
                        foreach (var slot in _inventorySystem.GetSlots(args.Target))
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var entity))
                                continue;

                            if (!TryComp<GasTankComponent>(entity, out var tank))
                                continue;

                            RefillGasTank(entity.Value, Gas.Nitrogen, tank);
                        }
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-internals-refill-nitrogen-description"),
                    Priority = (int) TricksVerbPriorities.RefillNitrogen,
                };
                args.Verbs.Add(refillInternalsN2);

                Verb refillInternalsPlasma = new()
                {
                    Text = "Refill Internals Plasma",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Tanks/plasma.rsi/icon.png",
                    Act = () =>
                    {
                        foreach (var slot in _inventorySystem.GetSlots(args.Target))
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var entity))
                                continue;

                            if (!TryComp<GasTankComponent>(entity, out var tank))
                                continue;

                            RefillGasTank(entity.Value, Gas.Plasma, tank);
                        }
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-internals-refill-plasma-description"),
                    Priority = (int) TricksVerbPriorities.RefillPlasma,
                };
                args.Verbs.Add(refillInternalsPlasma);
            }

            Verb sendToTestArena = new()
            {
                Text = "Send to test arena",
                Category = VerbCategory.Tricks,
                IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png",
                Act = () =>
                {
                    var (_, arenaGrid) = _adminTestArenaSystem.AssertArenaLoaded(player);

                    Transform(args.Target).Coordinates = new EntityCoordinates(arenaGrid, Vector2.One);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-send-to-test-arena-description"),
                Priority = (int) TricksVerbPriorities.SendToTestArena,
            };
            args.Verbs.Add(sendToTestArena);

            var activeId = FindActiveId(args.Target);

            if (activeId is not null)
            {
                Verb grantAllAccess = new()
                {
                    Text = "Grant All Access",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Misc/id_cards.rsi/centcom.png",
                    Act = () =>
                    {
                        GiveAllAccess(activeId.Value);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-grant-all-access-description"),
                    Priority = (int) TricksVerbPriorities.GrantAllAccess,
                };
                args.Verbs.Add(grantAllAccess);

                Verb revokeAllAccess = new()
                {
                    Text = "Revoke All Access",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Misc/id_cards.rsi/default.png",
                    Act = () =>
                    {
                        RevokeAllAccess(activeId.Value);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-revoke-all-access-description"),
                    Priority = (int) TricksVerbPriorities.RevokeAllAccess,
                };
                args.Verbs.Add(revokeAllAccess);
            }

            if (HasComp<AccessComponent>(args.Target))
            {
                Verb grantAllAccess = new()
                {
                    Text = "Grant All Access",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Misc/id_cards.rsi/centcom.png",
                    Act = () =>
                    {
                        GiveAllAccess(args.Target);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-grant-all-access-description"),
                    Priority = (int) TricksVerbPriorities.GrantAllAccess,
                };
                args.Verbs.Add(grantAllAccess);

                Verb revokeAllAccess = new()
                {
                    Text = "Revoke All Access",
                    Category = VerbCategory.Tricks,
                    IconTexture = "/Textures/Objects/Misc/id_cards.rsi/default.png",
                    Act = () =>
                    {
                        RevokeAllAccess(args.Target);
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-revoke-all-access-description"),
                    Priority = (int) TricksVerbPriorities.RevokeAllAccess,
                };
                args.Verbs.Add(revokeAllAccess);
            }
        }
    }

    private void RefillGasTank(EntityUid tank, Gas gasType, GasTankComponent? tankComponent)
    {
        if (!Resolve(tank, ref tankComponent))
            return;

        var mixSize = tankComponent.Air.Volume;
        var newMix = new GasMixture(mixSize);
        newMix.SetMoles(gasType, (1000.0f * mixSize) / (Atmospherics.R * Atmospherics.T20C)); // Fill the tank to 1000KPA.
        newMix.Temperature = Atmospherics.T20C;
        tankComponent.Air = newMix;
    }

    private EntityUid? FindActiveId(EntityUid target)
    {
        if (_inventorySystem.TryGetSlotEntity(target, "id", out var slotEntity))
        {
            if (HasComp<AccessComponent>(slotEntity))
            {
                return slotEntity.Value;
            }
            else if (TryComp<PDAComponent>(slotEntity, out var pda))
            {
                if (pda.ContainedID != null)
                {
                    return pda.ContainedID.Owner;
                }
            }
        }
        else if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var held in _handsSystem.EnumerateHeld(target))
            {
                if (HasComp<AccessComponent>(slotEntity))
                {
                    return slotEntity.Value;
                }
            }
        }

        return null;
    }

    private void GiveAllAccess(EntityUid entity)
    {
        var allAccess = _prototypeManager
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => p.ID).ToArray();

        _accessSystem.TrySetTags(entity, allAccess);
    }

    private void RevokeAllAccess(EntityUid entity)
    {
        _accessSystem.TrySetTags(entity, new string[]{});
    }

    public enum TricksVerbPriorities
    {
        Bolt = 0,
        Unbolt = 0,
        EmergencyAccessOn = -1, // These are separate intentionally for `invokeverb` shenanigans.
        EmergencyAccessOff = -1,
        MakeIndestructible = -2,
        MakeVulnerable = -2,
        BlockUnanchoring = -3,
        RefillBattery = -4,
        RefillOxygen = -5,
        RefillNitrogen = -6,
        RefillPlasma = -7,
        SendToTestArena = -8,
        GrantAllAccess = -9,
        RevokeAllAccess = -10,
        Rejuvenate = -11,
    }
}
