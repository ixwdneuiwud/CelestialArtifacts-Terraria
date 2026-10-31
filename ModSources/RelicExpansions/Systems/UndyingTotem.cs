// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using EnigmaticLegacy.Content.Status;
using EnigmaticLegacy.Core;
using Microsoft.Xna.Framework;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace RelicExpansions.Systems;

public sealed class UndyingTotem : ModSystem
{
    private delegate bool OriginalPreKill(Player p, double damage, int direction, bool pvp, ref bool sound, ref bool gore, ref PlayerDeathReason reason);
    private delegate bool PreKillHook(OriginalPreKill orig, Player p, double damage, int direction, bool pvp, ref bool sound, ref bool gore, ref PlayerDeathReason reason);
    public override void Load() => MonoModHooks.Add(typeof(PlayerLoader).GetMethod(nameof(PlayerLoader.PreKill)), (PreKillHook)AfterPreKill);
    private static bool AfterPreKill(OriginalPreKill orig, Player p, double damage, int direction, bool pvp, ref bool sound, ref bool gore, ref PlayerDeathReason reason)
    {
        // Run every other mod's normal rescue first. Spend no item if any of them
        // already cancelled death. This also avoids consuming on remote replicas.
        if (!orig(p, damage, direction, pvp, ref sound, ref gore, ref reason)) return false;
        return !TryRescue(p, pvp, reason);
    }
    public static Item ReadyTotem(Player p)
    {
        int type = ModContent.ItemType<TotemOfUndying>();
        bool IsTotem(Item i) => i != null && !i.IsAir && i.type == type;
        if (IsTotem(p.HeldItem)) return p.HeldItem;
        for (int n = 3; n < 10; n++) if (p.IsItemSlotUnlockedAndUsable(n) && IsTotem(p.armor[n])) return p.armor[n];
        using var context = new Main.CurrentPlayerOverride(p);
        foreach (var slot in ModContent.GetContent<ModAccessorySlot>())
            if (slot.IsEnabled() && IsTotem(slot.FunctionalItem)) return slot.FunctionalItem;
        return null;
    }
    public static bool RollsBrokenTotem(UnifiedRandom rng) => rng.Next(ExpansionAcquisition.Rules.BrokenTotemDenominator) < ExpansionAcquisition.Rules.BrokenTotemNumerator;
    public static bool TryRescue(Player p, bool pvp, PlayerDeathReason reason = null)
    {
        if (!p.active || p.dead || pvp || Main.netMode == NetmodeID.Server || p.whoAmI != Main.myPlayer || p.creativeGodMode) return false;
        Item totem = ReadyTotem(p);
        if (totem == null) return false;
        totem.stack--; if (totem.stack <= 0) totem.TurnToAir();
        for (int n = p.buffType.Length - 1; n >= 0; n--)
            if (p.buffType[n] > 0 && !Compatibility.ProtectedBuff(p.buffType[n])) p.DelBuff(n);
        p.statLife = Math.Min(p.statLifeMax2, BridgeUnits.Health(1));
        LegacyStatus.Apply(p, ModContent.BuffType<StrongRegeneration>(), 45 * 60);
        LegacyStatus.Apply(p, ModContent.BuffType<LegacyFireResistance>(), 40 * 60);
        LegacyStatus.Apply(p, ModContent.BuffType<AbsorptionII>(), 5 * 60);
        p.immune = true; p.immuneTime = 120;
        if (Catalog.TryType("celestial_core:broken_totem", out int broken) && RollsBrokenTotem(Main.rand)) p.QuickSpawnItem(p.GetSource_Misc("CelestialBrokenTotem"), broken);
        if (!Main.dedServ)
        {
            SoundEngine.PlaySound(SoundID.Item29, p.Center);
            for (int n = 0; n < 36; n++) Dust.NewDustPerfect(p.Center, n % 2 == 0 ? DustID.GoldCoin : DustID.GreenTorch,
                Vector2.UnitX.RotatedBy(n * MathHelper.TwoPi / 36) * Main.rand.NextFloat(2, 5), 100, default, 1.2f).noGravity = true;
        }
        return true;
    }
}
