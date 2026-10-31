// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using EnigmaticLegacy.Core;
using RelicExpansions.Content.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace RelicExpansions.Core;
public static class ExpansionWeapons
{
    public static int Tier(string id) => id.StartsWith("celestial_core:sakura_") ? 1 : id.StartsWith("celestial_core:ocean_") || id.StartsWith("celestial_artifacts:earth_") ? 2 : id.StartsWith("celestial_core:virtual_gold_") ? 3 : 0;
    public static bool Implemented(string id) => Tier(id) > 0 && Catalog.Items.TryGetValue(id, out var d) && d.Kind == "weapon";
    public static bool Qualified(Player p, string id) => Implemented(id) && p.GetModPlayer<Players.ExpansionPlayer>().CanUse((ExpansionItem)ContentSamples.ItemsByType[Catalog.Type(id)].ModItem);
    public static bool CanUse(Player p, ExpansionItem item) => Qualified(p, item.Id);
    public static int Armor(string id)
    {
        int tier = Tier(id); if (tier == 0) return 0;
        return id[(id.LastIndexOf('_') + 1)..] switch { "helmet" => tier + 1, "chestplate" => tier + 3, "leggings" => tier + 2, "boots" => tier == 3 ? 2 : 1, _ => 0 };
    }
    public static void Defaults(Item item, Definition d)
    {
        if (!Implemented(d.Id)) return;
        int tier = Tier(d.Id);
        item.CloneDefaults(ItemID.NightsEdge);
        item.accessory = false; item.maxStack = 1; item.consumable = item.channel = false;
        item.useAmmo = item.mana = item.pick = item.axe = item.hammer = item.shoot = 0;
        item.shootSpeed = 0; item.noMelee = item.noUseGraphic = false;
        item.DamageType = DamageClass.Melee; item.useStyle = ItemUseStyleID.Swing; item.autoReuse = true;
        item.width = item.height = 42; item.knockBack = 5; item.UseSound = SoundID.Item1; item.rare = ItemRarityID.Green;
        item.useTime = item.useAnimation = 30 - tier * 3; item.damage = 12 + tier * 6; item.value = Item.buyPrice(gold: tier);
        if (tier > 0)
        {
            if (!d.Key.EndsWith("sword")) item.damage -= 4;
            if (d.Key.EndsWith("pickaxe") || d.Key.EndsWith("shovel")) { item.pick = 35 + tier * 10; item.useTime = 15 - tier * 2; }
            if (d.Key.EndsWith("_axe")) item.axe = 5 + tier * 5;
            return;
        }
    }
}
