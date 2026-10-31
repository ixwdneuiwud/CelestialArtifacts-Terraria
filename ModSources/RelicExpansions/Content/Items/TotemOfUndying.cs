// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using EnigmaticLegacy.Core;
using RelicExpansions.Core;
using RelicExpansions.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Content.Items;

// The MC off-hand is represented by a functional charm/accessory slot. A totem
// carried in the backpack or a vanity slot cannot prevent death.
public sealed class TotemOfUndying : ModItem
{
    public const string Id = "minecraft:totem_of_undying";
    public override string Texture => "RelicExpansions/Assets/Items/minecraft/totem_of_undying";
    public override void SetDefaults()
    {
        Item.width = Item.height = 32; Item.maxStack = 1;
        Item.accessory = true; Item.rare = ItemRarityID.Orange; Item.value = Item.buyPrice(gold: 1);
    }
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void AddRecipes()
    {
        if (Catalog.HasSource("celestial_core")) { ExpansionRecipes.AddSourceRecipes(this, Id, ""); return; }
        // Native fallback when only the shared framework is enabled.
        CreateRecipe().AddRecipeGroup("EnigmaticLegacy:GoldBar",8).AddIngredient(ItemID.LifeCrystal)
            .AddIngredient(ItemID.FallenStar,3).AddIngredient(ItemID.GoldCoin).AddTile(TileID.WorkBenches).Register();
        ExpansionRecipes.Registered["special/shared_totem_without_core"] = Id;
    }
}
