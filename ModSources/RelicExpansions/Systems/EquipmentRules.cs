// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using EnigmaticLegacy.Core;
using RelicExpansions.Content;
using RelicExpansions.Content.Items;
using RelicExpansions.Players;
using RelicExpansions.Core;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ID;

namespace RelicExpansions.Systems;

public sealed class EquipmentRules : ModSystem
{
    public static bool Compatible(Item a, Item b) => PortConfig.Current.AllowMultipleSpellstones || BridgeExtensions.Category(a) != "spellstone" || BridgeExtensions.Category(b) != "spellstone";

    public static bool Bound(Player p, Item item) => p.difficulty != 3 && (ExpansionEquipment.Id(item) == "celestial_artifacts:catastrophe_scroll" || ExpansionEquipment.Id(item) == "enigmaticlegacy:cursed_ring" && PortConfig.Current.PermanentCurses);

    private static bool Blocked(Item[] items, int context, int slot)
    {
        if (slot < 0 || slot >= items.Length) return false;
        bool equipped = Math.Abs(context) is ItemSlot.Context.EquipArmor or ItemSlot.Context.EquipAccessory;
        if (equipped && Bound(Main.LocalPlayer, items[slot])) return true;
        return false;
    }
    public override void Load()
    {
        On_ItemSlot.LeftClick_ItemArray_int_int += Left;
        On_ItemSlot.RightClick_ItemArray_int_int += Right;
        On_ItemSlot.ArmorSwap += Armor;
        On_Player.TrySwitchingLoadout += Loadout;
    }
    // isEquipLocked also rejects incoming accessories in native AccCheck. Binding
    // must protect the occupied destination instead of locking an entire item type.
    public static bool DestinationBound(Player p, Item incoming, int slot, bool modded)
    {
        if (!modded) return slot >= 0 && slot < p.armor.Length && !ReferenceEquals(incoming, p.armor[slot]) && Bound(p, p.armor[slot]);
        using var context = new Main.CurrentPlayerOverride(p);
        foreach (var extra in ModContent.GetContent<ModAccessorySlot>())
            if (extra.Type == slot) return !ReferenceEquals(incoming, extra.FunctionalItem) && Bound(p, extra.FunctionalItem);
        return false;
    }
    private static Item Armor(On_ItemSlot.orig_ArmorSwap orig, Item item, out bool success)
    {
        int target = item.headSlot >= 0 ? 0 : item.bodySlot >= 0 ? 1 : item.legSlot >= 0 ? 2 : -1;
        if (target >= 0 && Bound(Main.LocalPlayer, Main.LocalPlayer.armor[target]))
        { success = false; return item; }
        return orig(item, out success);
    }
    private static void Loadout(On_Player.orig_TrySwitchingLoadout orig, Player p, int index)
    { if (index != p.CurrentLoadoutIndex && Array.Exists(p.armor, i => Bound(p, i))) return; orig(p, index); ExpansionEquipment.Invalidate(p); }
    private static void Left(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot)
    { if (Blocked(inv, context, slot)) return; orig(inv, context, slot); ExpansionEquipment.Invalidate(Main.LocalPlayer); }
    private static void Right(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot)
    { if (Blocked(inv, context, slot)) return; orig(inv, context, slot); ExpansionEquipment.Invalidate(Main.LocalPlayer); }
}

public sealed class CrossPortEquipmentRules : GlobalItem
{
    public override void OnConsumeItem(Item item, Player player)
    {
        if (item.useStyle != ItemUseStyleID.EatFood || !player.GetModPlayer<ExpansionPlayer>().HasCA("gluttony_badge")) return;
        int duration = (int)Catalog.C("gluttonyBadgeEffectDuration") * 60;
        LegacyStatus.Apply(player, ModContent.BuffType<EnigmaticLegacy.Content.Status.StrengthI>(), duration);
        LegacyStatus.Apply(player, ModContent.BuffType<EnigmaticLegacy.Content.Status.RegenerationI>(), duration);
    }
    public override bool CanEquipAccessory(Item item, Player p, int slot, bool modded) => !EquipmentRules.DestinationBound(p, item, slot, modded);
    public override float UseSpeedMultiplier(Item item, Player player)
    {
        var e = player.GetModPlayer<ExpansionPlayer>();
        float speed = item.useAmmo == AmmoID.Arrow && e.HasCA("spirit_bracelet") ? 2 : 1;
        { }
        return speed;
    }

    public override void GrabRange(Item item, Player player, ref int grabRange)
    { if (player.GetModPlayer<ExpansionPlayer>().HasCA("solar_magnet")) grabRange = Math.Max(grabRange, 160); }
    public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => EquipmentRules.Compatible(equippedItem, incomingItem);
    public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
    { float f = player.GetModPlayer<ExpansionPlayer>().FlightSpeed * player.GetModPlayer<ExpansionPlayer>().AllStatsFactor; speed *= f; acceleration *= f; }
    public override void VerticalWingSpeeds(Item item, Player player, ref float falling, ref float rising, ref float maxCan, ref float max, ref float constant)
    { float f = player.GetModPlayer<ExpansionPlayer>().FlightSpeed * player.GetModPlayer<ExpansionPlayer>().AllStatsFactor; falling *= f; rising *= f; constant *= f; }
}
