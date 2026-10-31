// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using System.Text.Json;
using EnigmaticLegacy.Systems;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

public sealed class ExpansionDrop
{
    public string Item = "", Condition = "", Description = "", EnglishDescription = "", Original = "", Group = "", Fallback = "";
    public string[] Npcs = Array.Empty<string>();
    public int Numerator = 1, Denominator = 1;
}
public sealed class ExpansionAcquisitionData
{
    public ExpansionDrop[] NpcRules = Array.Empty<ExpansionDrop>();
    public ExpansionChestRule[] ChestRules = Array.Empty<ExpansionChestRule>();
    public ProgressReward[] ProgressRewards = Array.Empty<ProgressReward>();
    public int BrokenTotemNumerator = 1, BrokenTotemDenominator = 2, PureStarBuffCount = 12;
    public int HighHealthMinimum = 10000, SkyFishingDenominator = 20, MutualDeathWindow = 60;
}
public sealed class ExpansionChestRule
{
    public string Item = "", Pool = "", RequiredItem = "", Flag = "", Description = "";
    public string[] OriginalSources = Array.Empty<string>();
    public int Numerator = 1, Denominator = 1, Minimum = 1, Maximum = 1;
}
public sealed class ProgressReward
{
    public string Item = "", Npc = "", RequiredItem = "", Flag = "", Description = "", Original = "";
}

// Native loot rules preserve Bestiary/Recipe Browser support and the shared
// Looting roll. Conditions always use the credited killer, not the nearest player.
public static class ExpansionAcquisition
{
    private static ExpansionAcquisitionData data;
    public static ExpansionAcquisitionData Rules
    {
        get
        {
            if (data != null) return data;
            data = JsonSerializer.Deserialize<ExpansionAcquisitionData>(Catalog.Owner.GetFileBytes("Data/acquisition.json"), new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true });
            data.NpcRules = data.NpcRules.Where(r => Type(r.Item) > 0).ToArray();
            data.ChestRules = data.ChestRules.Where(r => Type(r.Item) > 0).ToArray();
            data.ProgressRewards = data.ProgressRewards.Where(r => Type(r.Item) > 0).ToArray();
            return data;
        }
    }
    public static void Clear() => data = null;
    public static int Type(string id) => id == TotemOfUndying.Id ? ModContent.ItemType<TotemOfUndying>() : Catalog.Type(id);
    public static bool Matches(ExpansionDrop rule, int type) => rule.Group is "hostile" or "town" || rule.Npcs.Any(name => NPCID.Search.TryGetId(name, out int id) && id == type);
    public static int BeneficialEffects(Player p) => p.buffType.Where((t, i) => t > 0 && p.buffTime[i] > 0 &&
        !Main.debuff[t] && !Main.vanityPet[t] && !Main.lightPet[t]).Distinct().Count();
    public static bool CanDrop(ExpansionDrop rule, NPC npc, Player player)
    {
        if (player == null || !player.active || npc.lastInteraction != player.whoAmI || !Matches(rule, npc.type)) return false;
        if (rule.Group == "town") { if (!npc.townNPC || NPCID.Sets.IsTownPet[npc.type] || npc.SpawnedFromStatue || npc.immortal) return false; }
        else if (!LegacyAcquisition.Eligible(npc)) return false;
        if (rule.Condition.StartsWith("etching_", StringComparison.Ordinal))
            return int.TryParse(rule.Condition[8..], out int curse) && curse >= 0 && curse < 7 && player.GetModPlayer<ExpansionPlayer>().Cursing(curse);
        return rule.Condition switch {
            "" => true,
            "nether_stage" => player.GetModPlayer<ExpansionPlayer>().NetherStage,
            "space" => npc.Center.Y / 16f < Main.worldSurface * .35,
            "beneficial_effects" => BeneficialEffects(player) >= Rules.PureStarBuffCount,
            "high_health" => npc.lifeMax >= Rules.HighHealthMinimum,
            "explosion_death" => LegacyDamageContext.IsExplosionDeath(npc),
            "catastrophe" => player.GetModPlayer<ExpansionPlayer>().CatastropheOn,
            "armored" => ArmoredEnemies.Matches(npc),
            _ => false
        };
    }
    public static IItemDropRule CreateRule(ExpansionDrop rule)
    {
        var result = ItemDropRule.ByCondition(new ExpansionDropCondition(rule), Type(rule.Item), rule.Denominator, 1, 1, rule.Numerator);
        if (rule.Fallback != "" && Type(rule.Fallback) > 0) result.OnFailedRoll(ItemDropRule.Common(Type(rule.Fallback)));
        return result;
    }
    public static void RecordKill(Player p, int type)
    {
        if (type == NPCID.WallofFlesh) p.GetModPlayer<ExpansionPlayer>().NetherStage = true;
    }

}

internal sealed class ExpansionDropCondition : IItemDropRuleCondition
{
    private readonly ExpansionDrop rule;
    public ExpansionDropCondition(ExpansionDrop rule) => this.rule = rule;
    public bool CanDrop(DropAttemptInfo info) => ExpansionAcquisition.CanDrop(rule, info.npc, LegacyAcquisition.CreditedKiller(info.npc));
    public bool CanShowItemDropInUI() => true;
    public string GetConditionDescription() => EnigmaticLegacy.Core.BridgeUnits.Text(rule.Description, rule.EnglishDescription);
}

public sealed class ExpansionAcquisitionNPC : GlobalNPC
{
    public override void ModifyNPCLoot(NPC npc, NPCLoot loot)
    {
        foreach (var rule in ExpansionAcquisition.Rules.NpcRules)
            if (rule.Group == "" && ExpansionAcquisition.Matches(rule, npc.type)) loot.Add(ExpansionAcquisition.CreateRule(rule));
    }
    public override void ModifyGlobalLoot(GlobalLoot loot)
    {
        foreach (var rule in ExpansionAcquisition.Rules.NpcRules)
            if (rule.Group != "") loot.Add(ExpansionAcquisition.CreateRule(rule));
    }
}
