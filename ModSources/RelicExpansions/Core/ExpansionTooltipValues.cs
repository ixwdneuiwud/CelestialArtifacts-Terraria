// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Players;
using RelicExpansions.Content.Items;
using RelicExpansions.Players;
using Terraria;
using Terraria.ID;

namespace RelicExpansions.Core;

// Reading a tooltip must not bind equipment, advance progress, or consume RNG.
public static class ExpansionTooltipValues
{

    private static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    public static string Resolve(Item item, string token)
    {
        if (item.ModItem is not ExpansionItem relic) return null;
        var p = Main.LocalPlayer; var e = p.GetModPlayer<ExpansionPlayer>(); var l = p.GetModPlayer<LegacyPlayer>();
        string[] curses = { "chaos", "origin", "life", "truth", "desire", "nihility", "end" };
        if (token.StartsWith("curseHistory_")) return (Array.IndexOf(curses, token[13..]) is int n && n >= 0 && (e.EffectiveCurseHistory & (1 << n)) != 0) ? "true" : "false";
        if (token.StartsWith("curseBless_")) return (Array.IndexOf(curses, token[11..]) is int n && n >= 0 && e.Blessing(n)) ? "true" : "false";
        return token switch {
            "equipped" => e.Has(relic.Id) ? "true" : "false",
            "scabbardStacks" => relic.Count("kills").ToString(),
            "soulTotemStacks" => relic.Count("souls").ToString(),
            "abyssStacks" => e.AbyssWill.ToString(),
            "cooldown" => N(e.Cooldowns.GetValueOrDefault(relic.Id) / 60d),
            "boundX" => N(relic.BoundPosition.X / 16), "boundY" => N(relic.BoundPosition.Y / 16),
            "realm" => relic.BoundWorld.Length == 0 ? BridgeUnits.Text("未绑定", "Unbound") :
                (relic.BoundWorld == Main.ActiveWorldFileData.UniqueId.ToString() ? "" : BridgeUnits.Text("其他世界 / ", "Another world / ")) + LegacyRealms.Label(LegacyRealms.At(relic.BoundPosition + p.Size / 2)),
            "killTypes" => relic.Kills.Count.ToString(),
            "owner" => relic.OwnerId.Length == 0 ? BridgeUnits.Text("未绑定", "Unbound") : relic.OwnerId == l.OwnerId ? p.name : BridgeUnits.Text("其他角色", "Another character"),
            "mode" => BridgeUnits.Text("已停用", "Deactivated"),
            "storedEntity" => "",
            "charges" => relic.Count("charges").ToString(),
            "lootTable" => relic.Definition.Key switch {
                "desert_pyramid_loot_box" => BridgeUnits.Text("沙漠神殿", "Desert Pyramid"),
                "igloo_loot_box" => BridgeUnits.Text("雪屋", "Igloo"),
                "jungle_pyramid_loot_box" => BridgeUnits.Text("丛林神庙", "Jungle Pyramid"),
                "manson_loot_box" => BridgeUnits.Text("林地府邸", "Woodland Mansion"),
                _ => BridgeUnits.Text("掠夺者前哨站", "Pillager Outpost")
            },
            _ => null
        };
    }
}
