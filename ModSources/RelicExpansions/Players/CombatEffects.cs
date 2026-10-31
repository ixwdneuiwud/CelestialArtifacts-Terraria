// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using EnigmaticLegacy.Content;
using EnigmaticLegacy.Content.Status;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Players;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Players;
public sealed partial class ExpansionPlayer
{
    internal bool ApplyingSecondaryDamage;
    private int lastTarget = -1, lastDamage;
    private bool meleeHit, projectileHit, arrowHit;
    private bool lightningIncoming, magicIncoming, explosionIncoming, fireIncoming;
    private float C(string key) => Catalog.C(key);
    public void OnActualHeal(int actual)
    {
        if (actual > 0 && HasCA("holy_necklace") && Ready("holyNecklace"))
        { LegacyStatus.Apply(Player, ModContent.BuffType<ResistanceI>(), (int)C("holyNecklaceDuration") * 60); Cooldown("holyNecklace", (int)C("holyNecklaceCooldown") * 60); }
    }
    private NPC SourceNPC(PlayerDeathReason source)
    {
        int n = source.SourceNPCIndex;
        return n >= 0 && n < Main.maxNPCs && Main.npc[n].active ? Main.npc[n] : null;
    }
    private bool Tagged(PlayerDeathReason source, string tag)
    {
        int n = source.SourceProjectileLocalIndex;
        return n >= 0 && n < Main.maxProjectiles && Main.projectile[n].active && Compatibility.HasProjectileTag(Main.projectile[n], tag);
    }
    private void Secondary(NPC npc, float amount, DamageClass damage = null)
    {
        if (!npc.active || npc.friendly || amount <= 0 || ApplyingSecondaryDamage || Player.whoAmI != Main.myPlayer) return;
        try { ApplyingSecondaryDamage = true; Player.ApplyDamageToNPC(npc, Math.Max(1, (int)Math.Min(amount, 2000000000)), 0, 0, false, damage ?? ModContent.GetInstance<NemesisDamage>()); }
        finally { ApplyingSecondaryDamage = false; }
    }
    public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) { meleeHit = item.CountsAsClass(DamageClass.Melee); projectileHit = arrowHit = false; }
    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        meleeHit = proj.CountsAsClass(DamageClass.Melee); projectileHit = true; arrowHit = proj.arrow;
        if (!ApplyingSecondaryDamage)
        {

            if (proj.arrow && HasCA("spirit_crown"))
            {
                if (Main.npc.Count(n => n.active && n.DistanceSQ(Player.Center) <= 192 * 192) <= C("spiritCrownMaxEntityCount")) modifiers.FinalDamage *= 1 + C("spiritCrownArrowDamageMultiplier");
                modifiers.FinalDamage *= 1 + target.Distance(Player.Center) / 32 * C("spiritCrownDistanceDamage");
            }
            if (proj.arrow && HasCA("spirit_crown") && HasCA("spirit_bracelet") && HasCA("spirit_necklace") && HasCA("spirit_arrow_bag") && Math.Sign(target.Center.X - Player.Center.X) == target.direction)
                modifiers.FinalDamage *= 1 + C("spiritBackShootBonus");
        }
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (ApplyingSecondaryDamage) return;
        var l = Player.GetModPlayer<LegacyPlayer>();
        // Original L2 DamageModifier.multTotal is an event layer, not an
        // ATTACK_DAMAGE attribute. It affects every profession independently.
        if (HasCA("demon_heart")) modifiers.FinalDamage *= 1 + ActiveCurses * C("demonHeartDamageBonus");

        modifiers.CritDamage += CritDamage;
        modifiers.CritDamage = AllStatBridge.Scale(modifiers.CritDamage, Math.Max(.001f, AllStatsFactor));
        if (HasCA("destroyer_badge") && Missing >= 1 - C("destroyerBadgeThreshold")) modifiers.FinalDamage *= 1 + C("destroyerBadgeHurtBonus");
        if (HasCA("holy_sword")) modifiers.FinalDamage *= 1 + (Player.statLifeMax2 - Player.statLife) / (float)PortConfig.Current.HealthAndDamageScale * C("holySwordLostLifeAddDamage");
        if (HasCA("solar_magnet") && Main.dayTime) modifiers.FinalDamage *= 1 + C("solarMagnetDamageBonus");
        if (HasCA("sands_talisman") && (Player.ZoneDesert || Player.ZoneUnderworldHeight)) modifiers.FinalDamage *= 1 + C("sandsTalismanDamageBonus");
        if (HasCA("heart_of_revenge") && SinceHurt <= C("heartOfRevengeValidTime") * 60) modifiers.FinalDamage *= 1 + C("heartOfRevengeDamageBonus");
        if (HasCA("star_necklace") && Math.Sign(target.Center.X - Player.Center.X) == target.direction) modifiers.FinalDamage *= 1 + C("starNecklaceDamageBonus");
        if (HasCA("fang_necklace") && target.defense > 0) modifiers.FinalDamage *= 1 + C("fangNecklaceDamageBonus");
        if (HasCA("gallop_necklace")) modifiers.SourceDamage.Flat += Math.Max(0, Player.moveSpeed) * C("gallopNecklaceDamageFactor") * PortConfig.Current.HealthAndDamageScale;
        if (HasCA("angel_desire") && Player.wingsLogic > 0 && Player.velocity.Y != 0) modifiers.FinalDamage *= 1 + C("angelDesireDamageBonus");
        if (HasCA("titan_scabbard") && meleeHit && BladeTicks > 0 && target.lifeMax > Player.statLifeMax2) modifiers.FinalDamage *= 1 + C("titanScabbardDamageFactor");
        if (HasCA("hidden_bracelet") && HiddenTicks > 0) modifiers.FinalDamage *= 1 + C("hiddenBraceletDamageBoost");
        if (HasCA("scarlet_bracelet") && Player.statLife > Player.statLifeMax2 * C("scarletBraceletMaxCost"))
        {
            int remaining = Math.Max(1, (int)(Player.statLifeMax2 * C("scarletBraceletMaxCost"))); float cost = (Player.statLife - remaining) / (float)PortConfig.Current.HealthAndDamageScale;
            Player.statLife = remaining; modifiers.SourceDamage.Flat += target.lifeMax * Math.Min(C("scarletBraceletDamageLimit"), cost * C("scarletBraceletDamageBonus"));
        }
        if (HasCA("deer_inscribed_amulet") && CatastropheOn) modifiers.FinalDamage *= 1 + (ActiveCurses == 0 ? 8 : 8 - ActiveCurses) * C("deerInscribedAmuletBlessBonus");
        if (HasCA("shadow_pendant")) modifiers.FinalDamage *= 1 + Math.Max(0, C("shadowPendantLightLevel") - Brightness * 15) * C("shadowPendantDamageBonus");
        if (HasCA("abyss_will_badge"))
        {
            float roll = Main.rand.NextFloat(); float chance = C("abyssWillBadgeChanceLarge");
            modifiers.FinalDamage *= (roll < chance ? C("abyssWillBadgeBonusSmall") : roll < chance + C("abyssWillBadgeChanceSmall") ? C("abyssWillBadgeBonusLarge") : 1) * (1 + AbyssWill * C("abyssWillBadgeDamageBonus"));
        }











    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (ApplyingSecondaryDamage || Player.whoAmI != Main.myPlayer) return;
        int actual = Math.Min(Math.Max(0, damageDone), Math.Max(1, target.lifeMax));

        lastTarget = target.whoAmI; lastDamage = actual;


        if (HasCA("nether_fire")) target.AddBuff(BuffID.OnFire, (int)C("netherFireRingFireTime") * 60);
        if (arrowHit && HasCA("flame_arrow_bag")) target.AddBuff(BuffID.OnFire, (int)C("flameArrowBagTime") * 60);
        if (HasCA("fang_necklace") && Main.rand.NextFloat() < C("fangNecklacePoisonChance")) target.AddBuff(BuffID.Poisoned, (int)C("fangNecklacePoisonDuration") * 60);
        if (HasCA("shadow_pendant")) Heal(actual * C("shadowPendantDamageHeal"));
        if (HasCA("war_dead_badge") && Cursing(0) && Missing > 1 - C("warDeadBadgeThreshold"))
        {
            int n = Main.npc.Count(n => n.active && n.DistanceSQ(Player.Center) <= 256 * 256);
            Heal((Player.statLifeMax2 - Player.statLife) * C("warDeadBadgeHeal") * n);
        }
        if (HasCA("holy_sword") && Compatibility.HasNpcTag(target, "undead")) Heal(actual);
        if (HasCA("leech_scabbard") && meleeHit && BladeTicks > 0) Heal(actual * C("leechScabbardHealFactor"));
        if (Blessing(6)) Heal((Player.statLifeMax2 - Player.statLife) * C("endBlessHeal"));
        if (HasCA("spirit_bracelet") && arrowHit) Player.AddBuff(BuffID.Swiftness, (int)C("spiritBraceletEffectDuration") * 60);
        // SpiritSet inflicts mining fatigue in Minecraft. That mechanic is
        // explicitly paused; it must not slow movement as a replacement.
        if (HasCA("lock_of_abyss"))
        {
            ExpansionNetwork.Debuff(Player, target, ExpansionNetwork.TargetAction.AbyssLock, actual);
        }







        if (CatastropheOn && NPCID.Sets.CountsAsCritter[target.type]) CurseHistory |= 1 << 4;
        if (HasCA("deer_inscribed_amulet") && Ready("deer_charge")) { ExpansionNetwork.Debuff(Player, target, ExpansionNetwork.TargetAction.Deer); Cooldown("deer_charge", (int)C("deerInscribedAmuletStrengthInterval") * 60); }
        if (!Ready("abyssWill") && HasCA("abyss_will_badge")) AbyssWillDamage += actual;
        if (HiddenTicks > 0) { HiddenTicks = 0; Cooldown("hidden", (int)(C("hiddenBraceletInterval") * 60)); }
    }

    public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
    {
        if (SpiritSet && proj.arrow && (Player.HasBuff(BuffID.Swiftness) || LegacyStatus.Level(Player, LegacyEffect.Swiftness) > 0)) modifiers.FinalDamage *= 1 - C("spiritProtect");
        magicIncoming = proj.CountsAsClass(DamageClass.Magic) || Compatibility.HasProjectileTag(proj, "magic");
        lightningIncoming = Compatibility.HasProjectileTag(proj, "lightning"); explosionIncoming = Compatibility.HasProjectileTag(proj, "explosion"); fireIncoming = Compatibility.HasProjectileTag(proj, "fire");



        if (HasCA("precious_bracelet") && magicIncoming) modifiers.FinalDamage *= 1 - C("preciousBraceletMagicReduction");
        if (HasCA("red_ruby_bracelet") && fireIncoming) { modifiers.FinalDamage *= 1 - C("redRubyBraceletFireReduction"); Status<StrengthI>(180); }


        if (Blessing(5) && magicIncoming) modifiers.FinalDamage *= 1 - C("nihilityBlessReduction");
    }
    public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
    {
        magicIncoming = lightningIncoming = explosionIncoming = fireIncoming = false;
        if (HasCA("holy_talisman")) modifiers.FinalDamage *= 1 - C(Compatibility.HasNpcTag(npc, "undead") ? "holyTalismanProtectionUndead" : "holyTalismanProtection");
        if (HasCA("sea_god_crown") && HasCA("sea_god_scroll") && Compatibility.IsAquatic(npc)) modifiers.FinalDamage *= 1 - C("seaGodProtect");
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        // Environmental/custom hits do not call ModifyHitByProjectile. Never
        // inherit the classification of a previous projectile hit.
        int sourceProjectile = modifiers.DamageSource.SourceProjectileLocalIndex;
        Projectile source = sourceProjectile >= 0 && sourceProjectile < Main.maxProjectiles ? Main.projectile[sourceProjectile] : null;
        magicIncoming = source != null && source.active && (source.CountsAsClass(DamageClass.Magic) || Compatibility.HasProjectileTag(source, "magic"));
        modifiers.FinalDamage *= Math.Max(0, IncomingMultiplier);
        modifiers.SourceDamage.Flat -= Math.Max(0, FlatReduction);
        modifiers.Knockback *= Math.Clamp(1 - KnockbackResistance, 0, 1);



        if (HasCA("abyss_will_badge"))
        {
            float roll = Main.rand.NextFloat(), chance = C("abyssWillBadgeChanceSmall");
            modifiers.FinalDamage *= (roll < chance ? C("abyssWillBadgePenaltyLarge") : roll > C("abyssWillBadgeChanceLarge") && roll < chance + C("abyssWillBadgeChanceLarge") ? C("abyssWillBadgePenaltySmall") : 1) * (1 + AbyssWill * C("abyssWillBadgeDamagePenalty"));
        }
        if (HasCA("magic_horseshoe") && modifiers.DamageSource.SourceOtherIndex == 0) modifiers.FinalDamage *= 1 - C("magicHorseshoeFallReduction");
        if (HasCA("netherite_ring") && LegacyRealms.At(Player.Center) == LegacyRealms.Nether) modifiers.FinalDamage *= 1 - C("netheriteRingProtection");
        if (HasCA("angel_heart") && Missing >= 1 - C("angelHeartThreshold")) modifiers.FinalDamage *= 1 - C("angelHeartProtection");
        if (HasCA("guardian_eye") && Player.wet) modifiers.FinalDamage *= 1 - C("guardianEyeProtection");
        if (HasCA("prayer_crown") && Player.controlDown) modifiers.FinalDamage *= 1 - C("prayerCrownProtection");
        if (HasCA("cursed_protector") && EnigmaticLegacy.Systems.LegacyActions.SourceInFront(Player, modifiers.DamageSource)) modifiers.FinalDamage *= 1 - C("cursedProtectorFrontProtect");
        if (HasCA("shadow_pendant")) modifiers.FinalDamage *= Math.Max(0, 1 - Math.Max(0, C("shadowPendantLightLevel") - Brightness * 15) * C("shadowPendantDamageReduction"));
        if (Blessing(0)) modifiers.FinalDamage *= 1 - C("chaoticBlessDamageReduction") * Missing;
        if (Cursing(0)) modifiers.FinalDamage *= 1 + (Tagged(modifiers.DamageSource, "explosion") ? C("chaoticExplosionDamage") : C("chaoticOtherDamage"));


        modifiers.ModifyHurtInfo += FinalizeHurt;
    }
    private void FinalizeHurt(ref Player.HurtInfo info)
    {
        var incoming = Player.GetModPlayer<LegacyPlayer>().IncomingDamageType;



        bool creature = LegacyDamage.IsCreatureSource(info.DamageSource);
        if (HasCA("cursed_protector") && info.Damage > Player.statLife * C("cursedProtectorThreshold")) info.Damage = (int)(info.Damage * (1 - C("cursedProtectorReduction")));
        if (creature && Cursing(3)) info.Damage = Math.Max(info.Damage, (int)(Player.statLifeMax2 * C("truthCurseMinDamage")));
        if (creature && Blessing(3)) info.Damage = Math.Min(info.Damage, (int)(Player.statLifeMax2 * C("truthBlessMaxDamage")));
        if (HasCA("abyss_core") && Ready("abyssCore") && info.Damage >= BridgeUnits.Health(C("abyssCoreDamageJudgement"))) { info.Damage = BridgeUnits.Health(C("abyssCoreReduce")); Cooldown("abyssCore", (int)C("abyssCoreCooldown") * 60); }






        info.Damage = Math.Max(0, info.Damage);
    }
    public override bool FreeDodge(Player.HurtInfo info)
    {

        if (HasCA("thunder_ring") && Tagged(info.DamageSource, "lightning")) return true;


        if (HasCA("forest_cloak") && info.DamageSource.SourceProjectileLocalIndex >= 0 && Main.rand.NextFloat() < C("forestCloakDodgeChance")) return true;
        if (HasCA("twisted_brain") && Main.rand.NextFloat() < C("twistedbrainAvoidChance")) { Status<LegacyStrength>((int)C("twistedBrainEffectDuration") * 60); return true; }
        NPC source = SourceNPC(info.DamageSource);

        return false;
    }

    public override void OnHurt(Player.HurtInfo info)
    {

        SinceHurt = 0;
        if (CatastropheOn) CurseHistory |= 1 << 2;
        if (HasCA("emerald_bracelet") && Player.luck >= 2 && Main.rand.NextFloat() < C("emeraldBraceletEffChance")) Status<AbsorptionII>(300);
        if (HasCA("spirit_necklace")) Status<SwiftnessI>(300);
        if (HasCA("heart_of_revenge")) { Status<LegacyStrength>((int)C("heartOfRevengeValidTime") * 60); Status<ResistanceI>((int)C("heartOfRevengeValidTime") * 60); }
        if (HasCA("cursed_totem")) { var i = Get("celestial_artifacts:cursed_totem"); i.Set("souls", Math.Min((int)C("cursedTotemMaxLevel"), i.Count("souls") + 1)); }
        if (Cursing(6) && info.Damage >= Player.statLifeMax2 * C("endCurseThreshold")) { Player.AddBuff(BuffID.Weak, (int)C("endCurseDuration") * 60); Player.AddBuff(BuffID.Slow, (int)C("endCurseDuration") * 60); }
        if (HasCA("cross_necklace") || HasCA("holy_necklace")) { int time = (int)Math.Max(HasCA("cross_necklace") ? C("crossNecklaceInvulTick") * 3 : 0, HasCA("holy_necklace") ? C("holyNecklaceInvulTick") * 3 : 0); Player.immuneTime += time; }
        if (HasCA("prayer_crown")) Player.immuneTime += 30;
        if (HasCA("prayer_crown") && Player.controlDown && Main.rand.NextFloat() < C("prayerCrownHealChance")) Heal(info.Damage * C("prayerCrownHealAmount"));
        NPC source = SourceNPC(info.DamageSource);
        if (source != null)
        {
            if (Blessing(5)) source.AddBuff(ModContent.BuffType<Content.Status.PoisonIII>(), (int)C("nihilityBlessDuration") * 60);
            if (HasCA("holy_sword")) Secondary(source, info.Damage * C("holySwordReflect"));
            if (HasCA("soul_box")) ExpansionNetwork.Debuff(Player, source, ExpansionNetwork.TargetAction.SoulBoxLow);


        }


    }
    public override bool PreKill(double damage, int direction, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason source)
    {
        if (pvp) return true;

        string rescue = null; int cd = 0;
        if (HasCA("holy_talisman") && Ready("holyTotem")) { rescue = "holyTotem"; cd = (int)C("holyTalismanCooldown") * 60; }
        else if (HasCA("soul_box") && Ready("soulBox")) { rescue = "soulBox"; cd = (int)C("soulBoxCooldown") * 60; }
        else if (HasCA("cursed_totem") && Get("celestial_artifacts:cursed_totem").Count("souls") >= C("cursedTotemConsumption"))
        { rescue = "cursedTotem"; var i = Get("celestial_artifacts:cursed_totem"); i.Set("souls", i.Count("souls") - (int)C("cursedTotemConsumption")); }
        else if (HasCA("undead_charm") && Ready("undeadCharm")) { rescue = "undeadCharm"; cd = (int)C("undeadCharmCooldown") * 60; }
        else if (Carried.TryGetValue("celestial_artifacts:gaia_totem", out var gaia)) { rescue = "gaia"; gaia.Item.stack--; if (gaia.Item.stack <= 0) gaia.Item.TurnToAir(); }
        if (rescue == null) return true;
        Player.statLife = BridgeUnits.Health(1);
        ClearDebuffs(); Status<StrongRegeneration>(2700); Status<AbsorptionII>(300); Status<LegacyFireResistance>(2400);
        if (rescue == "holyTotem") Heal(Player.statLifeMax2);
        Cooldown(rescue, cd); Player.immune = true; Player.immuneTime = rescue == "gaia" ? 300 : 120;
        if (rescue == "soulBox" && SourceNPC(source) is NPC npc) { ExpansionNetwork.Debuff(Player, npc, ExpansionNetwork.TargetAction.SoulBoxHigh); Secondary(npc, Player.statLifeMax2 * C("soulBoxReflect"), ModContent.GetInstance<VoidDamage>()); }
        if (rescue == "cursedTotem" && SourceNPC(source) is NPC totemSource) totemSource.AddBuff(BuffID.WitheredWeapon, (int)C("cursedTotemEffectDuration") * 60);
        return false;
    }

    public static string NpcIdentity(int type) => type < NPCID.Count ? "Terraria:" + type : ModContent.GetModNPC(type)?.FullName ?? "Terraria:" + type;
    public void RecordKill(int type, int maxLife, bool friendly)
    {
        ExpansionAcquisition.RecordKill(Player, type);
        if (Player.whoAmI != Main.myPlayer) return;
        string id = NpcIdentity(type);





        if (HasCA("twisted_scabbard")) { var i = Get("celestial_artifacts:twisted_scabbard"); i.Set("kills", Math.Min(int.MaxValue - 1, i.Count("kills")) + 1); i.Set("decay", 0); }
        if (HasCA("emerald_necklace") && Main.rand.NextFloat() < C("emeraldNecklaceDrop") * Math.Max(0, 1 + Player.luck)) Player.QuickSpawnItem(Player.GetSource_Misc("EmeraldNecklace"), ItemID.GoldCoin);

    }
}

