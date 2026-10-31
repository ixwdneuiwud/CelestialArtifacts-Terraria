// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using EnigmaticLegacy.Core;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

public static class ExpansionRecipes
{
    private static JsonArray source;
    private static JsonObject map;
    private static JsonObject aliases;
    public static readonly Dictionary<string, string> Deferred = new();
    public static readonly Dictionary<string, string> Registered = new();
    public static void Clear() { source = null; map = aliases = null; Deferred.Clear(); Registered.Clear(); }
    public static void AddFor(ExpansionItem item)
    {
        if (item.Id == "celestial_core:sakura_fragment")
        {
            item.CreateRecipe().AddRecipeGroup("RelicExpansions:Flower").Register();
            Registered["special/sakura_fragment_flower"] = item.Id;
        }
        { }
        { }
        AddSourceRecipes(item, item.Id, item.Definition.Gate);
    }
    public static void AddSourceRecipes(ModItem item, string itemId, string gate)
    {
        source ??= JsonNode.Parse(Catalog.Owner.GetFileBytes("Data/source-recipes.json")).AsArray();
        map ??= JsonNode.Parse(Catalog.Owner.GetFileBytes("Data/vanilla-map.json")).AsObject();
        foreach (JsonObject entry in source.OfType<JsonObject>())
        {
            JsonNode result = entry["result"];
            string id = result is JsonObject obj ? (string)obj["item"] : result is JsonValue value ? value.GetValue<string>() : "";
            if (id != itemId) continue;
            string key = (string)entry["sourceFile"] ?? id, type = (string)entry["type"] ?? "";
            var ingredients = new Dictionary<int, int>(); string reason = ""; bool valid = true;
            if (entry["pattern"] is JsonArray pattern && entry["key"] is JsonObject keys)
                foreach (var symbol in string.Concat(pattern.Select(p => (string)p)).Where(c => c != ' ').GroupBy(c => c))
                    valid &= Add(keys[symbol.Key.ToString()], symbol.Count(), ingredients, ref reason);
            else if (type.Contains("smithing"))
            {
                foreach (string part in new[] { "base", "addition", "template" })
                    if (entry[part] is JsonNode input) valid &= Add(input, 1, ingredients, ref reason);
            }
            else if (entry["ingredients"] is JsonArray list)
                foreach (var ingredient in list) valid &= Add(ingredient, 1, ingredients, ref reason);
            else if (entry["ingredient"] is JsonNode single) valid &= Add(single, 1, ingredients, ref reason);
            else { valid = false; reason = "特殊配方序列化方式未映射"; }
            if (!valid || ingredients.Count == 0) { Deferred[key] = reason; continue; }
            if (!(type.Contains("crafting") || type.Contains("smelting") || type.Contains("blasting") || type.Contains("smithing") || type.EndsWith("shapeless_no_return_craft")))
            { Deferred[key] = "特殊配方类型：" + type; continue; }
            var recipe = item.CreateRecipe(result is JsonObject ? (int?)result["count"] ?? 1 : 1);
            foreach (var ingredient in ingredients)
            {
                if (ingredient.Key < 0) { recipe.AddRecipeGroup(-ingredient.Key - 1, ingredient.Value); continue; }
                string ownGroup = ingredient.Key switch { ItemID.NightmarePickaxe => "EvilPickaxe", ItemID.WarAxeoftheNight => "EvilAxe", ItemID.LightsBane => "EvilSword", _ => null };
                if (ownGroup != null) { recipe.AddRecipeGroup("RelicExpansions:" + ownGroup, ingredient.Value); continue; }
                string group = ingredient.Key switch { ItemID.IronBar => "IronBar", ItemID.GoldBar => "GoldBar", ItemID.DemoniteBar => "EvilBar", _ => null };
                if (group != null) recipe.AddRecipeGroup("EnigmaticLegacy:" + group, ingredient.Value);
                else recipe.AddIngredient(ingredient.Key, ingredient.Value);
            }
            recipe.AddTile(type.Contains("smelting") || type.Contains("blasting") ? TileID.Furnaces : type.Contains("smithing") ? TileID.Anvils : TileID.WorkBenches);
            if (item is ExpansionItem expansion && gate is "cursed" or "either" or "eldritch" or "catastrophe")
                recipe.AddCondition(new Condition(Language.GetOrRegister("Mods.RelicExpansions.Conditions." + gate, () => "需要对应遗物使用资格"),
                    () => Main.LocalPlayer.GetModPlayer<Players.ExpansionPlayer>().CanUse(expansion)));
            recipe.Register(); Registered[key] = id;
        }
    }
    private static bool Add(JsonNode ingredient, int n, Dictionary<int, int> output, ref string reason)
    {
        var options = ingredient is JsonArray array ? array.AsEnumerable() : new[] { ingredient };
        foreach (var option in options)
            if (Resolve(option, out int type, out int units))
            { output[type] = checked(output.GetValueOrDefault(type) + n * units); return true; }
        reason = ingredient?.ToJsonString() ?? "空材料"; return false;
    }
    public static bool Resolve(JsonNode ingredient, out int type, out int units)
    {
        type = 0; units = 1;
        if (ingredient is not JsonObject obj) return false;
        string id = (string)obj["item"] ?? (string)obj["tag"] ?? "";
        aliases ??= JsonNode.Parse(Catalog.Owner.GetFileBytes("Data/material-aliases.json")).AsObject();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (aliases.TryGetPropertyValue(id, out var alias))
        {
            if (!visited.Add(id) || alias is not JsonValue) return false;
            id = (string)alias;
            if (string.IsNullOrEmpty(id)) return false;
        }
        if (id == TotemOfUndying.Id) { type = ModContent.ItemType<TotemOfUndying>(); return true; }
        if (Catalog.TryType(id, out type)) return true;
        if (id == "minecraft:nether_star") return ImportCatalog.TryType("nether_star", out type);
        if (id.StartsWith("enigmaticlegacy:", StringComparison.Ordinal)) return ImportCatalog.TryType(id[16..], out type);
        map ??= JsonNode.Parse(Catalog.Owner.GetFileBytes("Data/vanilla-map.json")).AsObject();
        if (!map.TryGetPropertyValue(id, out JsonNode replacement)) return false;
        string vanilla;
        if (replacement is JsonObject conversion) { vanilla = (string)conversion["item"]; units = (int?)conversion["count"] ?? 1; }
        else vanilla = (string)replacement;
        return units > 0 && vanilla != null && ItemID.Search.TryGetId(vanilla, out type);
    }
}
