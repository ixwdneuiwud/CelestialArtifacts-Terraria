// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System.Linq;
using EnigmaticLegacy.Core;
using RelicExpansions.Core;
using Terraria;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;
public sealed class RelicRecipeGroups : ModSystem
{
    public override void AddRecipeGroups()
    {
        RecipeGroup.RegisterGroup("RelicExpansions:Flower", new RecipeGroup(() => BridgeUnits.Text("任意花", "Any flower"),
            Terraria.ID.ItemID.Daybloom, Terraria.ID.ItemID.Moonglow, Terraria.ID.ItemID.Blinkroot, Terraria.ID.ItemID.Deathweed,
            Terraria.ID.ItemID.Waterleaf, Terraria.ID.ItemID.Fireblossom, Terraria.ID.ItemID.Shiverthorn,
            Terraria.ID.ItemID.YellowMarigold, Terraria.ID.ItemID.SkyBlueFlower, Terraria.ID.ItemID.PinkPricklyPear,
            Terraria.ID.ItemID.JungleRose, Terraria.ID.ItemID.NaturesGift, Terraria.ID.ItemID.AbigailsFlower));
        RecipeGroup.RegisterGroup("RelicExpansions:EvilPickaxe", new RecipeGroup(() => "梦魇镐或死亡使者镐", Terraria.ID.ItemID.NightmarePickaxe, Terraria.ID.ItemID.DeathbringerPickaxe));
        RecipeGroup.RegisterGroup("RelicExpansions:EvilAxe", new RecipeGroup(() => "暗夜战斧或嗜血狂斧", Terraria.ID.ItemID.WarAxeoftheNight, Terraria.ID.ItemID.BloodLustCluster));
        RecipeGroup.RegisterGroup("RelicExpansions:EvilSword", new RecipeGroup(() => "魔光剑或血腥屠刀", Terraria.ID.ItemID.LightsBane, Terraria.ID.ItemID.BloodButcherer));
    }
}
