// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using EnigmaticLegacy.Systems;
using Microsoft.Xna.Framework;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace RelicExpansions.Systems;

// This migration is independent of EnigmaticLegacy's chestLootSeededV1.
// Only the authority writes loot; opening a chest never rolls its ordinary loot again.
public sealed class ExpansionChestWorld : ModSystem
{
    private const string SeedTag = "expansionChestLootSeededV1", PendingTag = "expansionPendingChestsV1";
    private bool seeded;
    private readonly HashSet<string> seededSources = new(StringComparer.Ordinal);
    private readonly Dictionary<Point, (int Tile, int Style)> pending = new();

    public static bool Matches(string pool, int tileType, int style, int y, bool dungeonWall)
    {
        if (tileType is not (TileID.Containers or TileID.Containers2)) return false;
        bool Native(int item) => ContentSamples.ItemsByType[item] is var sample && tileType == sample.createTile && style == sample.placeStyle;
        var gold = ContentSamples.ItemsByType[ItemID.GoldChest];
        bool isGold = tileType == gold.createTile && (style == gold.placeStyle || style == gold.placeStyle + 1);
        bool overworld = y + 1 < Main.UnderworldLayer;
        if (!overworld) return pool == "hell_shadow" && LegacyAcquisition.MatchesChest("hell_shadow", tileType, style, y, dungeonWall);
        return pool switch {
            "surface_wood" => Native(ItemID.Chest) && y < Main.worldSurface,
            "underground_gold" => isGold && !dungeonWall && y >= Main.worldSurface,
            "dungeon_gold" => isGold && dungeonWall,
            "ice" => Native(ItemID.IceChest),
            "water" => Native(ItemID.WaterChest),
            "ivy" => Native(ItemID.IvyChest),
            "living_wood" => Native(ItemID.LivingWoodChest),
            "desert" => Native(ItemID.DesertChest),
            "sky" => Native(ItemID.SkywareChest) && y < Main.worldSurface,
            _ => false
        };
    }
    private static int NormalStyle(int tileType, int style) => tileType == TileID.Containers && style is 2 or 4 ? style - 1 : style;
    public static bool Inspect(Chest chest, out int tileType, out int style, out bool dungeon)
    {
        tileType = style = 0; dungeon = false;
        if (chest == null || !WorldGen.InWorld(chest.x, chest.y)) return false;
        Tile tile = Main.tile[chest.x, chest.y];
        if (!tile.HasTile || tile.TileType is not (TileID.Containers or TileID.Containers2)) return false;
        tileType = tile.TileType; style = tile.TileFrameX / 36; dungeon = Main.wallDungeon[tile.WallType];
        return true;
    }
    public static bool Append(Chest chest, ExpansionChestRule rule, UnifiedRandom random, out int slot)
    {
        slot = Array.FindIndex(chest.item, i => i == null || i.IsAir);
        if (slot < 0 || random.Next(rule.Denominator) >= rule.Numerator) return false;
        chest.item[slot] = new Item(ExpansionAcquisition.Type(rule.Item), random.Next(rule.Minimum, rule.Maximum + 1));
        return true;
    }
    public override void ClearWorld() { seeded = false; seededSources.Clear(); pending.Clear(); }
    public override void LoadWorldData(TagCompound tag)
    {
        seeded = tag.GetBool(SeedTag); seededSources.Clear(); pending.Clear();
        if (tag.ContainsKey("expansionSeededSources")) seededSources.UnionWith(tag.GetList<string>("expansionSeededSources"));
        else if (seeded) seededSources.UnionWith(Catalog.ModuleNames.Keys);
        foreach (var c in tag.GetList<TagCompound>(PendingTag))
            pending[new Point(c.GetInt("x"), c.GetInt("y"))] = (c.GetInt("tile"), c.GetInt("style"));
    }
    public override void SaveWorldData(TagCompound tag)
    {
        tag[SeedTag] = seeded;
        tag["expansionSeededSources"] = seededSources.ToList();
        tag[PendingTag] = pending.Select(c => new TagCompound { ["x"] = c.Key.X, ["y"] = c.Key.Y,
            ["tile"] = c.Value.Tile, ["style"] = c.Value.Style }).ToList();
    }
    public override void PostWorldGen() => SeedChests();
    public override void PreUpdateWorld() { if (!seeded || Catalog.Modules.Keys.Any(s => !seededSources.Contains(s))) SeedChests(); }
    public void SeedChests() => SeedChests(Main.chest, Main.rand);
    internal void SeedChests(IEnumerable<Chest> chests, UnifiedRandom random)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Catalog.Modules.Keys.All(seededSources.Contains)) return;
        foreach (Chest chest in chests)
        {
            if (!Inspect(chest, out int tile, out int style, out bool dungeon) || !chest.item.Any(i => i != null && !i.IsAir)) continue;
            foreach (var rule in ExpansionAcquisition.Rules.ChestRules)
            {
                if (seededSources.Contains(rule.Item.Split(':')[0])) continue;
                if (!Matches(rule.Pool, tile, style, chest.y, dungeon)) continue;
                if (rule.RequiredItem == "") Append(chest, rule, random, out _);
                else pending[new Point(chest.x, chest.y)] = (tile, NormalStyle(tile, style));
            }
        }
        seededSources.UnionWith(Catalog.Modules.Keys);
        seeded = true;
    }
    internal bool TryOpen(Player player, UnifiedRandom random) => false;

    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (Player player in Main.ActivePlayers) TryOpen(player, Main.rand);
    }
}
