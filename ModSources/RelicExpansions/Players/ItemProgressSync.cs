// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Systems;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RelicExpansions.Players;

public sealed partial class ExpansionPlayer
{
    // Native equipment messages do not detect changes inside a ModItem's
    // dictionaries. Synchronize progression explicitly, without replacing
    // other mods' items, slots, or the separate container inventory protocol.
    private Dictionary<string, Item> ProgressSlots()
    {
        var slots = new Dictionary<string, Item>(StringComparer.Ordinal);
        for (int n = 0; n < Player.inventory.Length; n++) slots["inventory:" + n] = Player.inventory[n];
        for (int n = 0; n < Player.armor.Length; n++) slots["armor:" + n] = Player.armor[n];
        using var context = new Main.CurrentPlayerOverride(Player);
        foreach (var slot in ModContent.GetContent<ModAccessorySlot>()) slots["accessory:" + slot.FullName] = slot.FunctionalItem;
        return slots;
    }
    private List<TagCompound> WriteItemProgress() => ProgressSlots().Where(p => p.Value.ModItem is ExpansionItem).Select(p => {
        var i = (ExpansionItem)p.Value.ModItem;
        return new TagCompound {
            ["slot"] = p.Key, ["id"] = i.Id,
            ["progress"] = i.Progress.Select(v => new TagCompound { ["key"] = v.Key, ["value"] = v.Value }).ToList(),
            ["kills"] = i.Kills.ToList(), ["owner"] = i.OwnerId,
            ["world"] = i.BoundWorld, ["position"] = i.BoundPosition
        };
    }).ToList();
    private void ReadItemProgress(IList<TagCompound> snapshots)
    {
        var slots = ProgressSlots();
        foreach (var state in snapshots.Take(slots.Count))
        {
            if (!slots.TryGetValue(state.GetString("slot"), out Item target) || target.ModItem is not ExpansionItem i) continue;
            string id = state.GetString("id");
            if (id != i.Id) continue;
            var earnedChestFlags = ExpansionAcquisition.Rules.ChestRules.Where(rule => rule.RequiredItem == i.Id && i.Count(rule.Flag) > 0).Select(rule => rule.Flag).ToArray();
            i.Progress = state.GetList<TagCompound>("progress").Take(128).GroupBy(t => t.GetString("key")).ToDictionary(g => g.Key, g => g.Last().GetInt("value"));
            foreach (string flag in earnedChestFlags) i.Set(flag, 1); // Stale equipment snapshots cannot rearm a chest reward.
            i.Kills = new(state.GetList<string>("kills").Take(10000), StringComparer.Ordinal);
            i.OwnerId = state.GetString("owner"); i.BoundWorld = state.GetString("world"); i.BoundPosition = state.Get<Microsoft.Xna.Framework.Vector2>("position");
        }
    }
}
