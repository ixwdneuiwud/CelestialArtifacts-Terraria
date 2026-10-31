// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using RelicExpansions.Core;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RelicExpansions.Systems;

// Keep the framework's old name for ModPlayer/ModSystem saves; only the item
// owner changes. Hook the native loader so chests, bags and unloaded items use
// exactly the same migration and retain native prefix/global-item data.
public sealed class LegacyItemMigration : ModSystem
{
    private delegate void OriginalLoad(Item item, TagCompound tag);
    public override void Load() => MonoModHooks.Add(typeof(ItemIO).GetMethod(nameof(ItemIO.Load), new[] { typeof(Item), typeof(TagCompound) }),
        (Action<OriginalLoad, Item, TagCompound>)LoadItem);
    private static void LoadItem(OriginalLoad original, Item item, TagCompound tag)
    {
        if (tag.GetString("mod") != "RelicExpansions") { original(item, tag); return; }
        string name = tag.GetString("name");
        var definition = Catalog.Items.Values.FirstOrDefault(d => d.InternalName == name);
        if (definition == null || Catalog.ItemOwner(definition.Id) is not Mod owner) { original(item, tag); return; }
        TagCompound migrated = new();
        foreach (var entry in tag) migrated[entry.Key] = entry.Value;
        migrated["mod"] = owner.Name;
        original(item, migrated);
    }
}
