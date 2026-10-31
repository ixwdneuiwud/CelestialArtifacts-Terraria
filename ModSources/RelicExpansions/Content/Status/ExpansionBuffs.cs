// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using EnigmaticLegacy.Content.Status;
using EnigmaticLegacy.Core;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using RelicExpansions.Players;

namespace RelicExpansions.Content.Status;
public abstract class ExpansionBuff : LegacyBuff
{
    public override string Texture => "RelicExpansions/Assets/Status/" + (this switch {
        CriticalRate => "celestial_core_crit_rate",
        CriticalDamage => "celestial_core_crit_damage", HealingPower => "celestial_core_regen_rate",
        ArrowPower => "celestial_core_arrow_damage", _ => "celestial_core_unyielding" });
}
public sealed class StrengthIII : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Strength; public override int Level => 3; }
public sealed class HasteIII : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Haste; public override int Level => 3; }
public sealed class HealthBoostIII : ExpansionBuff { public override void Update(Player p, ref int buffIndex) => p.statLifeMax2 += BridgeUnits.Health(12); }
public sealed class LeapingIII : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Leaping; public override int Level => 3; }
public sealed class SwiftnessIII : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Swiftness; public override int Level => 3; }
public sealed class SlownessVI : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Slowness; public override int Level => 6; }
public sealed class SlownessII : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Slowness; public override int Level => 2; }
public sealed class ResistanceV : LeveledLegacyBuff { public override LegacyEffect Effect => LegacyEffect.Resistance; public override int Level => 5; }
public sealed class LuckI : ExpansionBuff { public override string Texture => $"Terraria/Images/Buff_{BuffID.Lucky}"; public override void Update(Player p, ref int index) => p.GetModPlayer<ExpansionPlayer>().Luck += 1; }
public sealed class CriticalRate : ExpansionBuff { public override void Update(Player p, ref int index) => p.GetCritChance(DamageClass.Generic) += 10; }
public sealed class CriticalDamage : ExpansionBuff { public override void Update(Player p, ref int index) => p.GetModPlayer<ExpansionPlayer>().CritDamage += .1f; }
public sealed class HealingPower : ExpansionBuff { public override void Update(Player p, ref int index) => p.GetModPlayer<ExpansionPlayer>().Healing += .2f; }
public sealed class ArrowPower : ExpansionBuff { public override void Update(Player p, ref int index) => p.GetModPlayer<ExpansionPlayer>().ArrowDamage += .2f; }
public sealed class PoisonIII : LegacyBuff
{
    public override void SetStaticDefaults() { base.SetStaticDefaults(); Main.debuff[Type] = true; }
    public override void Update(Player p, ref int index)
    {
        if (Main.netMode == NetmodeID.Server || p.whoAmI != Main.myPlayer || p.buffTime[index] % 18 != 0 || p.dead) return;
        p.statLife = System.Math.Max(1, p.statLife - BridgeUnits.Health(1));
        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, p.whoAmI);
    }
    public override void Update(NPC n, ref int index)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || n.buffTime[index] % 18 != 0 || n.immortal || n.life <= 1 || Compatibility.HasNpcTag(n, "undead")) return;
        n.life = System.Math.Max(1, n.life - BridgeUnits.Health(1)); n.netUpdate = true;
    }
}
