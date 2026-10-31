// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using EnigmaticLegacy.Content.Status;
using EnigmaticLegacy.Core;
using RelicExpansions.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Players;
public sealed partial class ExpansionPlayer
{
    private static readonly string[] Etchings = { "chaotic_etching", "origin_etching", "life_etching", "truth_etching", "desire_etching", "nihility_etching", "end_etching" };
    private readonly LegacyLight light = new();
    public float Brightness = 1;
    private int spiritBowTicks;
    private bool hiddenEquipped;
    public bool SpiritSet => HasCA("spirit_crown") && HasCA("spirit_bracelet") && HasCA("spirit_necklace") && HasCA("spirit_arrow_bag");
    // Enchantment, durability and Enderman gaze have no active Terraria trigger.
    // They still count as recorded curses while the Scroll is actually equipped.
    public int EffectiveCurseHistory => CurseHistory | (CatastropheOn ? (1 << 0) | (1 << 1) | (1 << 6) |
        (Equipped.Values.Any(i => i.Definition.Category == "etching") ? 1 << 3 : 0) | (PositiveBuffs > 0 ? 1 << 5 : 0) : 0);
    public bool Cursing(int index) => CatastropheOn && (EffectiveCurseHistory & (1 << index)) != 0 && !HasCA(Etchings[index]);
    public bool Blessing(int index) => CatastropheOn && (EffectiveCurseHistory & (1 << index)) != 0 && HasCA(Etchings[index]);
    public int ActiveCurses => Enumerable.Range(0, 7).Count(Cursing);
    private void Status<T>(int duration = 120) where T : ModBuff
    {
        int type = ModContent.BuffType<T>();
        int index = Player.FindBuffIndex(type);
        if (index < 0 || Player.buffTime[index] < duration - 3) LegacyStatus.Apply(Player, type, duration);
    }
    private void FireProtection()
    {
        Player.lavaImmune = true; Player.fireWalk = true;
        LegacyStatus.ProtectFire(Player);
    }
    private void ApplyCore()
    {
        bool Full(string prefix) => Has("celestial_core:" + prefix + "_helmet") && Has("celestial_core:" + prefix + "_chestplate") && Has("celestial_core:" + prefix + "_leggings") && Has("celestial_core:" + prefix + "_boots");
        if (Full("ocean")) Player.gills = true;
        if (Full("virtual_gold")) FireProtection();
        foreach (var pair in Equipped)
            if (pair.Value.Definition.SourceMod == "celestial_core" && pair.Value.Definition.Kind is "armor" or "boots" && !pair.Value.Definition.Key.StartsWith("sakura_"))
                Toughness += ExpansionWeapons.Tier(pair.Key) == 3 ? .75f : .5f;
        if (Player.wet && Player.HeldItem.ModItem is Content.Items.ExpansionItem i && i.Definition.Key.StartsWith("ocean_")) Player.pickSpeed /= 5;
    }
    private void ApplyCelestial()
    {
        if (HasCA("gold_ring")) Luck += 1;
        if (HasCA("amethyst_ring")) Player.nightVision = true;
        if (HasCA("diamond_ring")) Status<StrengthI>();
        if (HasCA("flight_ring")) GrantFlight(true);
        if (HasCA("netherite_ring") || HasCA("nether_fire")) FireProtection();
        if (HasCA("freeze_ring")) { Player.buffImmune[BuffID.Frozen] = true; Player.buffImmune[BuffID.Chilled] = true; }
        if (HasCA("war_dead_badge"))
        {
            float missingPercent = Missing * 100;
            Player.GetDamage(DamageClass.Melee) += missingPercent * Catalog.C("warDeadBadgeAtk");
            Attribute("toughness","multBase",missingPercent * Catalog.C("warDeadBadgeArmor"));
            Attribute("speed","multBase",missingPercent * Catalog.C("warDeadBadgeSpeed"));
        }
        if (HasCA("corrupt_badge"))
        {
            int n = NegativeBuffs;
            Player.GetDamage(DamageClass.Melee) += n * Catalog.C("corruptBadgeAttackBonus");
            Attribute("attackSpeed","multBase",n * Catalog.C("corruptBadgeAttackSpeedBonus"));
            Player.pickSpeed /= 1 + n * Catalog.C("corruptBadgeDigSpeedBonus");
        }
        if (HasCA("angel_pearl"))
        {
            Attribute("healing","add",PositiveBuffs * Catalog.C("angelPearlRegen"));
            Attribute("armor","add",PositiveBuffs * Catalog.C("angelPearlArmor"));
        }
        if (HasCA("demon_heart"))
        {
            Attribute("toughness","add",ActiveCurses);
            IncomingMultiplier *= Math.Max(0, 1 - ActiveCurses * Catalog.C("demonHeartDamageReduction"));
            if (ActiveCurses >= 3) FireProtection();
            if (ActiveCurses >= 5)
            {
                Player.buffImmune[BuffID.Slow] = true; Player.buffImmune[BuffID.Weak] = true;
                foreach (var buff in ModContent.GetContent<LeveledLegacyBuff>())
                    if (buff.Effect is LegacyEffect.Slowness or LegacyEffect.Weakness) Player.buffImmune[buff.Type] = true;
            }
        }
        if (HasCA("twisted_heart"))
        {
            float sign = Cursing(5) ? 1 : -1;
            Player.GetDamage(DamageClass.Melee) *= 1 + sign * Catalog.C("twistedHeartDamage");
            Attribute("toughness","multTotal",sign * Catalog.C("twistedHeartToughness"));
        }
        if (HasCA("traveler_scroll"))
        {
            float speed = Catalog.C("travellerScrollSpeedBonus");
            Attribute("speed","multBase",speed); Attribute("flightSpeed","multBase",speed); Attribute("swimSpeed","multBase",speed);
        }
        if (HasCA("sea_god_scroll") && Player.wet) { Player.pickSpeed /= 5; Status<EnigmaticLegacy.Content.Status.LegacyStrength>(); Status<ResistanceI>(); }
        if (HasCA("sea_god_scroll") && HasCA("sea_god_crown") && Player.HeldItem.shoot > 0 && Terraria.ID.ItemID.Sets.Spears[Player.HeldItem.type])
        { Player.GetDamage(DamageClass.Melee) += C("seaGodMelee"); Player.GetDamage(DamageClass.Ranged) += C("seaGodThrow"); }
        if (HasCA("sea_god_crown") && Player.wet) { Player.gills = true; Player.nightVision = true; }
        if (HasCA("guardian_eye") && Player.wet) { Player.gills = true; Player.ignoreWater = true; }
        if (HasCA("life_bracelet")) Status<RegenerationI>();
        if (HasCA("bearing_stamen")) Status<StrongRegeneration>();
        if (HasCA("star_necklace") && !Main.dayTime) Status<ResistanceI>();
        if (HasCA("forest_cloak")) Player.jumpSpeedBoost += Player.jumpSpeed * Catalog.C("forestCloakJumpPower");
        if (HasCA("yellow_duck") && Player.wet && !Player.controlDown && Player.velocity.Y > 0) Player.velocity.Y = 0;
        if (HasCA("angel_desire")) { Player.noFallDmg = true; if (Player.wingTime > 0 && Player.velocity.Y != 0) FlightSpeed *= 2; }
        if (HasCA("sakura_hairpin")) Player.GetCritChance(DamageClass.Generic) += (Player.luck + Luck) * Catalog.C("sakuraHairpinCritBonusFromLuck") * 100;
        if (HasCA("chaotic_pendant")) Player.manaRegenBonus += 3;
        if (BladeTicks > 0) { Player.GetDamage(DamageClass.Melee) += .1f; Player.GetCritChance(DamageClass.Generic) += 25; }
        if (HasCA("twisted_scabbard")) Player.GetDamage(DamageClass.Melee).Base += BridgeUnits.Health(1 + Get("celestial_artifacts:twisted_scabbard").Count("kills") * Catalog.C(Cursing(6) ? "twistedScabbardAttackEnd" : "twistedScabbardAttack"));
        if (HasCA("emerald_ring") && HasCA("emerald_necklace") && HasCA("emerald_bracelet"))
        { Luck += Catalog.C("emeraldLuck"); Player.GetCritChance(DamageClass.Generic) += Catalog.C("emeraldCrit") * 100; }
        if (HasCA("deer_inscribed_amulet") && HasCA("nostalgic_butterfly_ring")) CritDamage += Catalog.C("deerButterFlyDmgMul");
        if (CatastropheOn)
        {
            CurseHistory = EffectiveCurseHistory;
            if (Equipped.Values.Any(i => i.Definition.Category == "etching")) CurseHistory |= 1 << 3;
            if (PositiveBuffs > 0) CurseHistory |= 1 << 5;
            if (Cursing(1)) Player.GetDamage(DamageClass.Melee) *= 1 - Catalog.C("originCurseDamage");
            if (Blessing(1)) { Player.GetDamage(DamageClass.Melee) *= 1 + Catalog.C("originBlessDamage"); Player.pickSpeed /= 1 + Catalog.C("originBlessDamage"); }
            if (Cursing(2)) { Attribute("health","multTotal",-Catalog.C("lifeCurseHealth")); Attribute("healing","add",-Catalog.C("lifeCurseHeal")); }
            if (Blessing(2)) { Attribute("health","multTotal",Catalog.C("lifeBlessHealth")); Attribute("healing","add",Catalog.C("lifeBlessHeal")); }
        }
        if (HasCA("hidden_bracelet") && HiddenTicks > 0) Player.invis = true;
        if (HasCA("evil_eye")) { Player.buffImmune[BuffID.Darkness] = true; Player.buffImmune[BuffID.Blackout] = true; Player.buffImmune[BuffID.Obstructed] = true; }
        if (Cursing(4)) Player.aggro += 400;
        if (HasCA("demon_curse"))
        {
            float bonus = Math.Max(0, Healing - 1) * 100;
            Player.GetDamage(DamageClass.Melee) += bonus * Catalog.C("demonCurseAttackBonus"); Player.moveSpeed += bonus * Catalog.C("demonCurseSpeedBonus"); Healing = 0;
        }
    }
    private void TickCelestial()
    {
        if (SpiritSet && Player.HeldItem.useAmmo == AmmoID.Arrow && Player.controlUseItem && !Player.noItems && !Player.CCed)
        {
            spiritBowTicks++;
            if (spiritBowTicks >= C("spiritPullDuration") * 60) Status<Content.Status.ArrowPower>(120);
        }
        else spiritBowTicks = 0;
        if (Main.netMode != NetmodeID.MultiplayerClient && HasCA("holy_talisman") && Ticks % Math.Max(1, (int)C("holyTalismanWeakenInterval") * 60) == 0)
        {
            var nearby = Main.npc.Where(n => n.active && !n.friendly && !n.immortal && n.DistanceSQ(Player.Center) <= 256 * 256).ToArray();
            foreach (NPC n in nearby) n.AddBuff(ModContent.BuffType<LegacyWeakness>(), Math.Max(1, nearby.Length) * (int)C("holyTalismanEffectDuration") * 60);
        }
        if (Main.netMode != NetmodeID.MultiplayerClient && HasCA("twisted_scroll") && Ticks % 60 == 0)
            foreach (NPC npc in Main.npc.Where(n => n.active && !n.friendly && !n.boss && !n.dontTakeDamage && !n.immortal && n.realLife < 0 && n.aiStyle != 6 && !NPCID.Sets.ShouldBeCountedAsBoss[n.type] && !Compatibility.HasNpcTag(n, "boss") && n.DistanceSQ(Player.Center) < 768 * 768).ToArray())
            {
                var data = npc.GetGlobalNPC<Systems.ExpansionNPC>();
                if (data.TwistedCopy) continue;
                data.TwistedCopy = true; npc.netUpdate = true;
                if (Main.rand.NextFloat() >= C("twistedDuplicateChance")) continue;
                int copy = NPC.NewNPC(Player.GetSource_Misc("TwistedScroll"), (int)npc.Center.X, (int)npc.Bottom.Y, npc.type);
                if (copy < Main.maxNPCs) { Main.npc[copy].GetGlobalNPC<Systems.ExpansionNPC>().TwistedCopy = true; Main.npc[copy].netUpdate = true; }
            }
        string realm = LegacyRealms.At(Player.Center);
        if (priorRealm != "" && realm != priorRealm && HasCA("traveler_scroll")) { Status<SwiftnessI>(900); Status<RegenerationI>(900); }
        priorRealm = realm;
        if (HasCA("abyss_will_badge"))
        {
            if (Ticks % Math.Max(60, (int)C("abyssWillBadgeGrowInterval") * 60) == 0 && AbyssWill < C("abyssWillBadgeLimit")) AbyssWill++;
            if (Ready("abyssWill"))
            {
                if (AbyssWill > C("abyssWillBadgeLimit") + 1) AbyssWill = 0;
                if (AbyssWillDamage > 0) { if (Player.whoAmI == Main.myPlayer) Player.statLife = Math.Max(1, (int)(Player.statLife * C("abyssWillBadgeRemainingHealth"))); AbyssWillDamage = 0; }
            }
        }
        if (HasCA("solar_magnet"))
            for (int n = Player.buffType.Length - 1; n >= 0; n--) if (Player.buffType[n] is BuffID.OnFire or BuffID.OnFire3 or BuffID.Burning) Player.DelBuff(n);
        if (HasCA("twisted_scabbard") && Ticks % 60 == 0)
        {
            var i = Get("celestial_artifacts:twisted_scabbard"); i.Set("decay", i.Count("decay") + 1);
            if (i.Count("decay") >= C("twistedScabbardInterval")) { i.Set("kills", Math.Max(0, i.Count("kills") - 1)); i.Set("decay", 0); }
        }
        if (Ticks % 30 == 0 && HasCA("shadow_pendant"))
        { light.Scan(Player.Center, 2); Brightness = Math.Clamp(light.Brightness(Player.Center), 0, 1); }
        if (HasCA("angel_heart"))
        {
            if (Ticks % ((int)Catalog.C("angelHeartBloodInterval") * 60) == 0) Heal(BridgeUnits.Health(Catalog.C("angelHeartHealAmount")));
            if (Ticks % ((int)Catalog.C("angelHeartRemoveInterval") * 60) == 0) ClearDebuffs();
        }
        if (HasCA("angel_pearl") && Ticks % 300 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            foreach (Player nearby in Main.ActivePlayers)
                if (nearby != Player && !nearby.dead && LegacyRealms.Same(nearby.Center, Player.Center) && nearby.DistanceSQ(Player.Center) <= 256 * 256)
                    LegacyStatus.Apply(nearby, ModContent.BuffType<RegenerationI>(), 300);
        if (HasCA("hidden_bracelet"))
        {
            if (!hiddenEquipped) { HiddenTicks = 0; Cooldown("hidden", (int)(Catalog.C("hiddenBraceletInterval") * 60)); }
            else if (HiddenTicks > 0 || Ready("hidden")) HiddenTicks = 120;
            hiddenEquipped = true;
        }
        else { hiddenEquipped = false; HiddenTicks = 0; }
        if (HasCA("deer_inscribed_amulet") && Ready("deer_charge")) Get("celestial_artifacts:deer_inscribed_amulet").Set("charged", 1);
        foreach (string id in new[] { "iron_scabbard", "leech_scabbard", "titan_scabbard", "twisted_scabbard" })
            if (HasCA(id) && Ready("blade:" + id))
            { BladeTicks = id == "iron_scabbard" ? 120 : 180; Cooldown("blade:" + id, (int)(Catalog.C(id switch { "iron_scabbard" => "ironScabbardBladeInterval", "leech_scabbard" => "leechScabbardBladeInterval", "titan_scabbard" => "titanScabbardBladeInterval", _ => "twistedScabbardBladeInterval" }) * 60)); }
        for (int n = 0; n < Player.buffType.Length; n++)
        {
            int type = Player.buffType[n], time = Player.buffTime[n]; if (type <= 0 || time <= 0) continue;
            if (Cursing(5) && Main.debuff[type] && !Compatibility.ProtectedBuff(type) && !priorBuffs.ContainsKey(type))
                Player.buffTime[n] = Math.Min(360000, (int)(time * (1 + Catalog.C("nihilityCurseDuration"))));
        }
        priorBuffs.Clear(); for (int n = 0; n < Player.buffType.Length; n++) if (Player.buffTime[n] > 0) priorBuffs[Player.buffType[n]] = Player.buffTime[n];
    }
    public void ClearDebuffs()
    {
        for (int n = Player.buffType.Length - 1; n >= 0; n--)
            if (Player.buffType[n] > 0 && Main.debuff[Player.buffType[n]] && !Compatibility.ProtectedBuff(Player.buffType[n])) Player.DelBuff(n);
    }
}
