// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using EnigmaticLegacy.Content.Items;
using EnigmaticLegacy.Core;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace RelicExpansions.Content;

public abstract class TypedRelicSlot : ModAccessorySlot, IRelicAccessorySlot
{
    public abstract string Category { get; }
    public string AccessoryCategory => Category;
    public override Vector2? CustomLocation => RelicSlotGrid.Location(this);
    public override void PostDraw(AccessorySlotType context, Item item, Vector2 position, bool isHovered) => RelicSlotGrid.DrawGlyph(FunctionalTexture, context, item, position, isHovered);
    public virtual int Ordinal => 1;
    public override string FunctionalTexture => Category switch {
        "seven_curses" => "EnigmaticLegacy/Assets/Slots/ring",
        "catastrophe" or "etching" or "heart" or "pendant" => "RelicExpansions/Assets/Slots/" + Category,
        _ => "EnigmaticLegacy/Assets/Slots/" + Category
    };
    public override string VanityTexture => FunctionalTexture;
    public override string DyeTexture => "EnigmaticLegacy/Assets/Slots/cosmetic";
    // These functional-only slots must not advertise unusable vanity/dye cells.
    // Keep any previously saved items visible so they can still be recovered.
    public override bool DrawVanitySlot => !VanityItem.IsAir;
    public override bool DrawDyeSlot => !DyeItem.IsAir;
    public bool ActiveForEligibility => ExpansionEquipment.Enabled(Player, this);
    public override bool HasEquipmentLoadoutSupport => false;
    public override bool IsEnabled() => ActiveForEligibility;
    public override bool ModifyDefaultSwapSlot(Item item, int accSlotToSwapTo) =>
        Category is "seven_curses" or "catastrophe" &&
        IsEnabled() && FunctionalItem.IsAir && CanAcceptItem(item, AccessorySlotType.FunctionalSlot);
    public override bool CanAcceptItem(Item item, AccessorySlotType context)
    {
        if (item.IsAir) return true;
        if (context != AccessorySlotType.FunctionalSlot) return false;
        if (Category == "seven_curses") return item.ModItem is ImportedItem { ImportId: "cursed_ring" };
        if (Category == "catastrophe") return item.ModItem is ExpansionItem { Id: "celestial_artifacts:catastrophe_scroll" };
        return AccessoryChannels.AcceptsUniversalItem(item) && (Category == "curio" || BridgeExtensions.Category(item) == Category);
    }
    public override bool IsVisibleWhenNotEnabled() => !FunctionalItem.IsAir || !VanityItem.IsAir || !DyeItem.IsAir;
    public override void OnMouseHover(AccessorySlotType context) => Main.hoverItemName = "遗物专用槽：" + (Category switch
    { "seven_curses" => "七咒之戒", "catastrophe" => "灾厄之册", "heart" => "心脏", "pendant" => "吊坠", "head" => "头饰", "body" => "身饰", "back" => "背饰", "bracelet" => "手环", "necklace" => "项链", "scroll" => "奥秘卷轴", "ring" => "戒指", "charm" => "护符／术石", "etching" => "刻蚀板", "curio" => "通用饰品", _ => Category });
}

// Resolve capacities from initially enabled equipment to a fixed point. An item
// in an unavailable slot cannot activate itself or a circular chain of slots.
public static class ExpansionEquipment
{
    private sealed class Cache { public ulong Tick = ulong.MaxValue; public bool Building; public HashSet<int> Slots = new(); }
    private static ConditionalWeakTable<Player, Cache> caches = new();
    public static void Invalidate(Player player) { caches.GetOrCreateValue(player).Tick = ulong.MaxValue; RelicSlotGrid.Invalidate(); }
    public static void Clear() => caches = new();
    public static bool Enabled(Player player, TypedRelicSlot slot) => Active(player).Contains(slot.Type);
    public static HashSet<int> Active(Player player)
    {
        var cache = caches.GetOrCreateValue(player);
        if (cache.Building || cache.Tick == Main.GameUpdateCount) return cache.Slots;
        cache.Building = true;
        try { cache.Slots = Resolve(player); cache.Tick = Main.GameUpdateCount; return cache.Slots; }
        finally { cache.Building = false; }
    }
    private static HashSet<int> Resolve(Player player)
    {
        using var context = new Main.CurrentPlayerOverride(player);
        var typed = ModContent.GetContent<ModAccessorySlot>().OfType<TypedRelicSlot>().ToArray();
        var items = new List<Item>();
        for (int n = 0; n < 10; n++) if (n < 3 || player.IsItemSlotUnlockedAndUsable(n)) items.Add(player.armor[n]);
        foreach (var other in ModContent.GetContent<ModAccessorySlot>())
            if (other is not TypedRelicSlot && other.IsEnabled()) items.Add(other.FunctionalItem);
        var ids = new HashSet<string>(items.Select(Id).Where(s => s != ""));
        var active = new HashSet<int>();
        int generalBase = GeneralBaseCapacity(player);
        bool changed;
        do
        {
            changed = false;
            foreach (var slot in typed)
                if (!active.Contains(slot.Type) && slot.Ordinal <= Capacity(slot.Category, ids, false, generalBase))
                { active.Add(slot.Type); string id = Id(slot.FunctionalItem); if (id != "") ids.Add(id); changed = true; }
        } while (changed);
        return active;
    }
    public static string Id(Item item) => item.ModItem is ExpansionItem i ? i.Id : item.ModItem is TotemOfUndying ? TotemOfUndying.Id : item.ModItem is ImportedItem legacy ? "enigmaticlegacy:" + legacy.ImportId : "";
    public static int GeneralBaseCapacity(Player player)
    {
        using var context = new Main.CurrentPlayerOverride(player);
        return AccessoryChannels.NativeCapacity(player) + ModContent.GetContent<ModAccessorySlot>().Count(s => s is not TypedRelicSlot && s.IsEnabled() && AccessoryChannels.IsUniversal(s));
    }
    public static int Capacity(string category, HashSet<string> ids, bool voidTome, int universalSlots = -1)
    {
        bool CA(string key) => ids.Contains("celestial_artifacts:" + key);
        return category switch
        {
            "seven_curses" => 1,
            "catastrophe" => Catalog.HasSource("celestial_artifacts") ? 1 : 0,
            "heart" or "pendant" or "head" or "body" or "back" or "bracelet" or "necklace" or "spellstone" => Catalog.Modules.Count > 0 ? 1 : 0,
            "scroll" => 1,
            "ring" => 2 + (CA("precious_bracelet") ? 1 : 0),
            "charm" => 1 + (CA("catastrophe_scroll") ? 3 : 0) + (CA("precious_necklace") ? 1 : 0),
            "etching" => CA("catastrophe_scroll") ? 7 : 0,
            _ => 0
        };
    }
}
