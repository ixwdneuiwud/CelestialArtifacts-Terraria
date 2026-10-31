// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using EnigmaticLegacy.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.ModLoader;

namespace RelicExpansions.Core;
public static class ExpansionStats
{
    public static void Apply(ExpansionPlayer e, AttributeEffect a)
    {
        Apply(e, a.Stat, a.Op, a.Value);
    }
    public static void Apply(ExpansionPlayer e, string stat, string operation, float v)
    {
        Player p = e.Player; bool add = operation == "add";
        float Value(float current, float amount, bool integral = false) => e.AttributeValue(stat, operation, amount, current, integral);
        switch (stat)
        {
            case "health": p.statLifeMax2 = Math.Max(1, (int)Value(p.statLifeMax2,add ? v * PortConfig.Current.HealthAndDamageScale : v,true)); break;
            case "armor": p.statDefense += (int)MathF.Round(Value((float)p.statDefense,add ? v * PortConfig.Current.ArmorScale : v,true) - (float)p.statDefense); break;
            case "toughness": e.Toughness = Value(e.Toughness,v); break;
            case "damage": // Imported ATTACK_DAMAGE is melee, not universal damage.
            case "meleeDamage": ApplyDamage(p, DamageClass.Melee, operation, v); break;
            case "magicDamage": ApplyDamage(p, DamageClass.Magic, operation, v); break;
            case "rangedDamage":
            case "projectileDamage": ApplyDamage(p, DamageClass.Ranged, operation, v); break;
            case "summonDamage": ApplyDamage(p, DamageClass.Summon, operation, v); break;
            case "attackSpeed": p.GetAttackSpeed(DamageClass.Generic) = Value(p.GetAttackSpeed(DamageClass.Generic),add ? v / 4 : v); break;
            case "speed": p.moveSpeed = Value(p.moveSpeed,v); break;
            case "flightSpeed": e.FlightSpeed = Value(e.FlightSpeed,v); break;
            case "knockback": p.GetKnockback(DamageClass.Generic) = DamageArithmetic.Attribute(p.GetKnockback(DamageClass.Generic),Operation(operation),v); break;
            case "knockbackResistance": e.KnockbackResistance = Math.Clamp(Value(e.KnockbackResistance,v), 0, 1); if (e.KnockbackResistance >= 1) p.noKnockback = true; break;
            case "luck": e.Luck = Value(e.Luck,v); break;
            case "crit": p.GetCritChance(DamageClass.Generic) = Value(p.GetCritChance(DamageClass.Generic),add ? v * 100 : v); break;
            case "critDamage": e.CritDamage = Value(2 + e.CritDamage,v) - 2; break;
            case "arrowDamage": e.ArrowDamage = Value(1 + e.ArrowDamage,v) - 1; break;
            case "incomingDamage": e.IncomingMultiplier = Value(e.IncomingMultiplier,v); break;
            case "flatReduction": e.FlatReduction = Value(e.FlatReduction,add ? v * PortConfig.Current.HealthAndDamageScale : v); break;
            case "healing": e.Healing = Value(e.Healing,v); break;
            case "reach": p.blockRange = (int)Value(p.blockRange + 5,v,true) - 5; break;
            case "swimSpeed": if (p.wet) { p.accRunSpeed = e.AttributeValue("swimAcceleration",operation,v,p.accRunSpeed); p.maxRunSpeed = Value(p.maxRunSpeed,v); p.ignoreWater = true; } break;
            case "arrowSpeed": e.ArrowSpeed = Value(1 + e.ArrowSpeed,v) - 1; break;
            case "arrowKnockback": e.ArrowKnockback = Value(e.ArrowKnockback,v); break;
        }
    }
    internal static DamageOperation Operation(string operation) => operation == "add" ? DamageOperation.Addition : operation == "multBase" ? DamageOperation.MultiplyBase : DamageOperation.MultiplyTotal;
    private static void ApplyDamage(Player p, DamageClass category, string operation, float value)
    {
        DamageOperation op = Operation(operation);
        p.GetDamage(category) = DamageArithmetic.Attribute(p.GetDamage(category),op, value * (op == DamageOperation.Addition ? PortConfig.Current.HealthAndDamageScale : 1));
    }
    public static string Describe(AttributeEffect a)
    {
        string label = a.Stat switch { "health" => "最大生命", "armor" => "防御", "toughness" => "韧性", "damage" or "meleeDamage" => "近战伤害", "rangedDamage" or "projectileDamage" => "远程伤害", "magicDamage" => "魔法伤害", "summonDamage" => "召唤伤害", "attackSpeed" => "攻击速度", "speed" => "移动速度", "luck" => "幸运", "crit" => "暴击率", "critDamage" => "暴击伤害", "healing" => "治疗效率", "incomingDamage" => "承受伤害倍率", "flatReduction" => "固定减伤", _ => a.Stat };
        float value = a.Value;
        if (a.Op == "add" && a.Stat is "health" or "damage" or "flatReduction") value *= PortConfig.Current.HealthAndDamageScale;
        if (a.Op == "add" && a.Stat == "armor") value *= PortConfig.Current.ArmorScale;
        bool percent = a.Op != "add" || a.Stat is "crit" or "critDamage" or "healing" or "projectileDamage" or "knockbackResistance";
        return $"{label}：{(value >= 0 ? "+" : "")}{(percent ? value * 100 : value):0.##}{(percent ? "%" : "")}";
    }
}
