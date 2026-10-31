// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Content.Status;
using Microsoft.Xna.Framework;
using RelicExpansions.Players;
using Terraria;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

// Scale the RESULT of inheritance, once, including mod-added DamageClasses.
// Private mod statistics opt in through the public callback, without reflection.
public sealed class AllStatBridge : ModSystem
{
    private static readonly List<Action<Player, float>> Providers = new();
    public static bool Register(Action<Player, float> provider) { if (provider == null) throw new ArgumentNullException(nameof(provider)); Providers.Add(provider); return true; }
    public static void ApplyPrivate(Player player, float factor) { foreach (var provider in Providers.ToArray()) provider(player, factor); }
    public override void Load()
    {
        On_Player.GetTotalDamage += Damage;
        On_Player.GetTotalCritChance += Crit;
        On_Player.GetTotalAttackSpeed += Speed;
        On_Player.GetTotalArmorPenetration += Penetration;
        On_Player.GetTotalKnockback += Knockback;
        On_Player.Heal += Heal;
        On_Player.UpdateLifeRegen += Regeneration;
    }
    public override void Unload() => Providers.Clear();
    public static StatModifier Scale(StatModifier value, float factor)
    {
        // ApplyTo(x) = (x + Base) * Additive * Multiplicative + Flat.
        // Both terms must scale, after inheritance has been resolved.
        value *= factor; value.Flat *= factor; return value;
    }
    private static StatModifier Damage(On_Player.orig_GetTotalDamage orig, Player p, DamageClass type) => Scale(orig(p, type), p.GetModPlayer<ExpansionPlayer>().AllStatsFactor);
    private static float Positive(float value, float factor) => value > 0 ? value * factor : value;
    private static float Crit(On_Player.orig_GetTotalCritChance orig, Player p, DamageClass type) => Positive(orig(p, type), p.GetModPlayer<ExpansionPlayer>().AllStatsFactor);
    private static float Speed(On_Player.orig_GetTotalAttackSpeed orig, Player p, DamageClass type) => Positive(orig(p, type), p.GetModPlayer<ExpansionPlayer>().AllStatsFactor);
    private static float Penetration(On_Player.orig_GetTotalArmorPenetration orig, Player p, DamageClass type) => Positive(orig(p, type), p.GetModPlayer<ExpansionPlayer>().AllStatsFactor);
    private static StatModifier Knockback(On_Player.orig_GetTotalKnockback orig, Player p, DamageClass type) => Scale(orig(p, type), p.GetModPlayer<ExpansionPlayer>().AllStatsFactor);
    private static void Heal(On_Player.orig_Heal orig, Player p, int amount)
    {
        var e = p.GetModPlayer<ExpansionPlayer>();
        int scaled = Math.Max(0, (int)MathF.Round(amount * Math.Max(0, e.Healing)));
        if (scaled <= 0) return;
        int before = p.statLife; orig(p, scaled);
        e.OnActualHeal(Math.Max(0, Math.Min(p.statLife, p.statLifeMax2) - before));
    }

    private static void Regeneration(On_Player.orig_UpdateLifeRegen orig, Player p)
    {
        int before = p.statLife; orig(p);
        p.GetModPlayer<ExpansionPlayer>().OnActualHeal(Math.Max(0, Math.Min(p.statLife, p.statLifeMax2) - before));
    }

}
