// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using EnigmaticLegacy.Content.Status;
using EnigmaticLegacy.Core;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using S = RelicExpansions.Content.Status;

namespace RelicExpansions.Systems;

public static class ExpansionPotions
{
    public static (int Buff, int Ticks) Effect(Definition d)
    {
        string key = d.Potion;
        int ticks = key.StartsWith("long_") ? 28800 : key.StartsWith("ultimate_") ? 57600 : key.StartsWith("extreme_") ? 5400 : d.SourceMod == "celestial_core" ? 18000 : 10800;
        if (key == "luck") return (ModContent.BuffType<S.LuckI>(), 18000);
        key = key.Replace("long_", "").Replace("ultimate_", "").Replace("extreme_", "");
        if (key.Contains("mining_fatigue")) return (0, 0); // Explicitly deferred by the user.
        return key switch {
            "leaping" => (ModContent.BuffType<S.LeapingIII>(), ticks),
            "swiftness" => (ModContent.BuffType<S.SwiftnessIII>(), ticks),
            "slowness" or "turtle_master" => (ModContent.BuffType<S.SlownessVI>(), 1200),
            "poison" => (ModContent.BuffType<S.PoisonIII>(), 1260),
            "regeneration" => (ModContent.BuffType<RegenerationIII>(), 1320),
            "strength" => (ModContent.BuffType<S.StrengthIII>(), ticks),
            "weakness" => (ModContent.BuffType<WeaknessII>(), ticks),
            "haste" => (ModContent.BuffType<S.HasteIII>(), ticks),
            "crit_rate" => (ModContent.BuffType<S.CriticalRate>(), ticks),
            "crit_damage" => (ModContent.BuffType<S.CriticalDamage>(), ticks),
            "regen_rate" => (ModContent.BuffType<S.HealingPower>(), ticks),
            "arrow_damage" => (ModContent.BuffType<S.ArrowPower>(), ticks),
            _ => (0, 0)
        };
    }
    public static void Apply(Player p, Definition d, float factor = 1)
    {
        var (buff, ticks) = Effect(d); if (buff <= 0) return;
        LegacyStatus.Apply(p, buff, Math.Max(1, (int)MathF.Floor(ticks * factor + .5f)));
        if (d.Potion == "extreme_turtle_master") LegacyStatus.Apply(p, ModContent.BuffType<S.ResistanceV>(), Math.Max(1, (int)(1200 * factor)));
    }
    public static void Apply(NPC n, Definition d, float factor = 1)
    {
        var (buff, ticks) = Effect(d); if (buff <= 0 || n.immortal || n.dontTakeDamage) return;
        n.AddBuff(buff, Math.Max(1, (int)MathF.Floor(ticks * factor + .5f)));
        if (d.Potion == "extreme_turtle_master") n.AddBuff(ModContent.BuffType<S.ResistanceV>(), Math.Max(1, (int)(1200 * factor)));
    }
    public static void AddRecipe(ExpansionItem item)
    {
        var d = item.Definition; if (d.Potion == "") return;
        string ns = d.SourceMod + ":";
        string Key(string potion) => ns + (potion.StartsWith("ultimate_") || potion.StartsWith("extreme_") ? "ultimate_" : "common_") + potion;
        var recipe = item.CreateRecipe();
        if (d.Delivery != "")
        {
            string before = d.Id[..^(d.Delivery.Length + 1)] + (d.Delivery == "lingering" ? "_splash" : "");
            recipe.AddIngredient(Catalog.Type(before));
            recipe.AddIngredient(d.Delivery == "splash" ? ItemID.ExplosivePowder : ItemID.FragmentNebula);
        }
        else switch (d.Potion)
        {
            default:
                if (d.SourceMod == "celestial_core")
                {
                    if (d.Potion.StartsWith("long_")) recipe.AddIngredient(Catalog.Type(Key(d.Potion[5..]))).AddIngredient(ItemID.Wire);
                    else recipe.AddIngredient(ItemID.BottledWater).AddIngredient(Catalog.Type("celestial_core:" + (d.Potion switch { "crit_rate" => "ocean_essence", "crit_damage" => "fire_essence", "regen_rate" => "heart_fragment", _ => "light_fragment" })));
                }
                else
                {
                    int input = d.Potion switch {
                        "extreme_leaping" => ItemID.FeatherfallPotion, "extreme_swiftness" or "extreme_slowness" => ItemID.SwiftnessPotion,
                        "extreme_turtle_master" => ItemID.EndurancePotion, "extreme_poison" => ItemID.BottledWater,
                        "extreme_regeneration" => ItemID.RegenerationPotion, "extreme_strength" => ItemID.WrathPotion,
                        "extreme_haste" => ImportCatalog.Type("common_strong_haste"), _ => ItemID.BottledWater };
                    recipe.AddIngredient(input);
                    if (d.Potion == "extreme_poison") recipe.AddIngredient(ItemID.Stinger);
                    if (d.Potion == "extreme_slowness") recipe.AddIngredient(ItemID.BlackLens);
                    // Vanilla weakness uses fermented spider eye; keep the same
                    // Deathweed analogue as the base port's weakness recipe.
                    if (d.Potion == "extreme_weakness") recipe.AddIngredient(ItemID.Deathweed);
                }
                break;
        }
        recipe.AddTile(TileID.Bottles).Register();
        ExpansionRecipes.Registered["adapted_brewing/" + d.Id] = d.Id;
    }
}
