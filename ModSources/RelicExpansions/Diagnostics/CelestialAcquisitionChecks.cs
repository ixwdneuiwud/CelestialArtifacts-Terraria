// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Players;
using Microsoft.Xna.Framework;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using RelicExpansions.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace RelicExpansions.Diagnostics;

internal static class CelestialAcquisitionChecks
{
    public static void Run(Player p, Action<string, bool> check, List<string> details)
    {
        int mode = Main.netMode; var random = Main.rand; float luck = p.luck;
        var e = p.GetModPlayer<ExpansionPlayer>(); var legacy = p.GetModPlayer<LegacyPlayer>();
        var rules = ExpansionAcquisition.Rules.NpcRules;
        ExpansionDrop Rule(string key) => rules.Single(r => r.Item.EndsWith(":" + key));
        NPC Enemy(int type) { var n = new NPC(); n.SetDefaults(type); n.active = true; n.lastInteraction = p.whoAmI; n.GetGlobalNPC<MutualDeathNPC>().OnSpawn(n, p.GetSource_Misc("CelestialAcquisitionFixture")); return n; }
        void Equip(params string[] ids)
        {
            e.ResetEffects(); legacy.ResetEffects(); p.dead = false; p.active = true;
            Array.Clear(p.buffType); Array.Clear(p.buffTime);
            for (int n = 0; n < p.armor.Length; n++) p.armor[n] = new Item();
            for (int n = 0; n < ids.Length; n++) { p.armor[n + 3] = new Item(Catalog.Type(ids[n])); ((ExpansionItem)p.armor[n + 3].ModItem).UpdateAccessory(p, false); }
        }
        int Count(string id) => Main.item.Where(i => i.active && i.type == Catalog.Type(id)).Sum(i => i.stack);
        try
        {
            Main.netMode = NetmodeID.SinglePlayer; Main.rand = new UnifiedRandom(29331); p.luck = 0;
            using var context = new Main.CurrentPlayerOverride(p);
            Equip(); var enemy = Enemy(NPCID.BlueSlime); enemy.lifeMax = 9999;
            bool below = ExpansionAcquisition.CanDrop(Rule("twisted_heart"), enemy, p);
            enemy.lifeMax = 10000;
            check("Celestial acquisition: the agreed 10000-health threshold applies to both independent materials", !below &&
                ExpansionAcquisition.CanDrop(Rule("twisted_heart"), enemy, p) && ExpansionAcquisition.CanDrop(Rule("death_essence"), enemy, p) &&
                Rule("twisted_heart").Denominator == 10 && Rule("death_essence").Denominator == 10);
            enemy.SpawnedFromStatue = true;
            check("Celestial acquisition: high-health statue enemies still cannot produce materials", !ExpansionAcquisition.CanDrop(Rule("death_essence"), enemy, p));
            var guide = Enemy(NPCID.Guide);
            check("Celestial acquisition: player-killed town residents qualify for the original ten-percent Brain chance", ExpansionAcquisition.CanDrop(Rule("twisted_brain"), guide, p) &&
                !ExpansionAcquisition.CanDrop(Rule("twisted_brain"), Enemy(NPCID.BlueSlime), p) && Rule("twisted_brain").Numerator == 1 && Rule("twisted_brain").Denominator == 10);
            guide.lastInteraction = -1;
            check("Celestial acquisition: an uncredited town death never rewards a nearby player", !ExpansionAcquisition.CanDrop(Rule("twisted_brain"), guide, p));
            var slime = Enemy(NPCID.BlueSlime);
            bool bare = ExpansionAcquisition.CanDrop(Rule("the_end_dust"), slime, p);
            Equip("celestial_artifacts:catastrophe_scroll");
            check("Celestial acquisition: one-percent End Dust requires the actual worn Scroll", !bare && ExpansionAcquisition.CanDrop(Rule("the_end_dust"), slime, p) && Rule("the_end_dust").Denominator == 100);
            string[] etchings = { "chaotic_etching", "origin_etching", "life_etching", "truth_etching", "desire_etching", "nihility_etching", "end_etching" };
            foreach (string key in etchings)
            {
                Equip("celestial_artifacts:catastrophe_scroll"); e.CurseHistory = 127;
                var rule = Rule(key); NPC npc = Enemy(NPCID.Search.GetId(rule.Npcs.Single()));
                var database = new ItemDropDatabase(); database.RegisterToNPC(npc.type, ExpansionAcquisition.CreateRule(rule));
                var resolver = new ItemDropResolver(database); int plates = 0, cubes = 0; bool exclusive = true;
                int plateType = Catalog.Type(rule.Item), cubeType = Catalog.Type(rule.Fallback);
                for (int n = 0; n < 400; n++)
                {
                    foreach (var item in Main.item.Where(i => i.active && (i.type == plateType || i.type == cubeType))) item.active = false;
                    resolver.TryDropping(new DropAttemptInfo { npc = npc, player = p, rng = Main.rand });
                    int plate = Count(rule.Item), cube = Count(rule.Fallback);
                    exclusive &= plate + cube == 1; plates += plate; cubes += cube;
                }
                check("Celestial acquisition: native fifty-percent etching with exclusive Cube fallback " + key, exclusive && plates is > 150 and < 250 && cubes == 400 - plates);
                Equip("celestial_artifacts:catastrophe_scroll", "celestial_artifacts:" + key); e.CurseHistory = 127;
                check("Celestial acquisition: a reversed curse blocks both its plate and its Cube fallback " + key, !ExpansionAcquisition.CanDrop(rule, npc, p));
            }
            Equip("celestial_artifacts:catastrophe_scroll"); e.CurseHistory = 0;
            check("Curses: unsupported enchantment durability and gaze triggers are already cursed", e.Cursing(0) && e.Cursing(1) && e.Cursing(6));
            var book = p.armor[3];
            foreach (int n in Enumerable.Range(0, 7))
            {
                Equip("celestial_artifacts:catastrophe_scroll", "celestial_artifacts:" + etchings[n]); e.CurseHistory = 127;
                string curse = new[] { "chaos", "origin", "life", "truth", "desire", "nihility", "end" }[n];
                var lines = RelicTooltips.Content(Catalog.Owner, p.armor[3], "celestial_artifacts:catastrophe_scroll", true, false);
                check("Curses: matching etching reverses one curse, updates original Shift text and removes one stack " + curse,
                    e.Blessing(n) && !e.Cursing(n) && e.ActiveCurses == 6 && lines.Any(l => l.Name.EndsWith(curse + "_bonus")) &&
                    !lines.Any(l => l.Name.Contains(curse + "_curse")));
            }
            Equip("celestial_artifacts:catastrophe_scroll"); e.CurseHistory = 127; legacy.BoundCurses = true;
            p.selectedItem = 0; p.inventory[0] = new Item(ItemID.IronBroadsword); p.inventory[0].Prefix(PrefixID.Broken);
            check("Curses: Scroll combines seven Ring curses seven Catastrophe curses and one negative prefix", LegacyPrefixes.TotalCurses(p) == 15);
            var fishing = new FishingAttempt { heightLevel = 0, questFish = -1 };
            check("Celestial acquisition: native high-altitude water fishing is eligible", AcquisitionFishing.Eligible(fishing, ItemID.Bass, 0));
            fishing.heightLevel = 1; bool low = AcquisitionFishing.Eligible(fishing, ItemID.Bass, 0); fishing.heightLevel = 0; fishing.crate = true;
            bool crate = AcquisitionFishing.Eligible(fishing, ItemID.Bass, 0); fishing.crate = false; fishing.inLava = true;
            check("Celestial acquisition: fishing preserves low-altitude lava crates quest fish enemies and mod catches", !low && !crate && !AcquisitionFishing.Eligible(fishing, ItemID.Bass, 0) &&
                !AcquisitionFishing.Eligible(new FishingAttempt { heightLevel = 0, questFish = ItemID.Bass }, ItemID.Bass, 0) &&
                !AcquisitionFishing.Eligible(new FishingAttempt { heightLevel = 0 }, Catalog.Type("celestial_artifacts:magic_horseshoe"), 0) && !AcquisitionFishing.Eligible(new FishingAttempt { heightLevel = 0 }, ItemID.Bass, NPCID.Zombie));
            Main.rand = new ChestAcquisitionChecks.FirstRoll(); int caught = ItemID.Bass, spawn = 0; var sonar = new AdvancedPopupRequest(); var position = Vector2.Zero;
            fishing = new FishingAttempt { heightLevel = 0, questFish = -1 };
            p.GetModPlayer<AcquisitionFishing>().CatchFish(fishing, ref caught, ref spawn, ref sonar, ref position);
            check("Celestial acquisition: the actual native fishing hook yields Magic Horseshoe", caught == Catalog.Type("celestial_artifacts:magic_horseshoe") && spawn == 0);
            check("Celestial acquisition: flowers have an independent real Sakura Fragment recipe", ExpansionRecipes.Registered.ContainsKey("special/sakura_fragment_flower") &&
                RecipeGroup.recipeGroups[RecipeGroup.recipeGroupIDs["RelicExpansions:Flower"]].ValidItems.Contains(ItemID.Daybloom));
            Equip(); var a = Enemy(NPCID.BlueSlime); int before = Count("celestial_core:heart_fragment");
            MutualDeath.ForgetPlayer(p.whoAmI); MutualDeath.Killed(a); MutualDeath.RecordDeath(p, a.GetGlobalNPC<MutualDeathNPC>().Life);
            check("Celestial acquisition: enemy-first mutual death emits exactly one Heart Fragment", Count("celestial_core:heart_fragment") == before + 1);
            before++; MutualDeath.ForgetPlayer(p.whoAmI); a = Enemy(NPCID.BlueSlime);
            MutualDeath.RecordDeath(p, a.GetGlobalNPC<MutualDeathNPC>().Life); MutualDeath.Killed(a); MutualDeath.Killed(a);
            check("Celestial acquisition: player-first mutual death emits once despite duplicate kill callbacks", Count("celestial_core:heart_fragment") == before + 1);
            before++; MutualDeath.ForgetPlayer(p.whoAmI); a = Enemy(NPCID.BlueSlime); var b = Enemy(NPCID.BlueSlime);
            MutualDeath.Killed(a); MutualDeath.RecordDeath(p, b.GetGlobalNPC<MutualDeathNPC>().Life);
            check("Celestial acquisition: unrelated enemies or reused NPC identities cannot cause a mutual-death reward", Count("celestial_core:heart_fragment") == before);
            MutualDeath.ForgetPlayer(p.whoAmI); MutualDeath.Killed(a); MutualDeath.RecordDeath(p, 0);
            check("Celestial acquisition: an environmental death cannot create a mutual-death reward", Count("celestial_core:heart_fragment") == before);
            details.Add("Celestial sources: seven native conditional Boss plate rules with mutually exclusive Cube fallback; high-health >=10000, independent 10% materials; town Brain 10%; worn Scroll dust 1%; sky water fishing 5%; flowers 1:1; same-attacker mutual death within 60 ticks.");
        }
        finally { Main.netMode = mode; Main.rand = random; p.luck = luck; MutualDeath.ForgetPlayer(p.whoAmI); Equip(); p.inventory[0] = new Item(); }
    }
}
