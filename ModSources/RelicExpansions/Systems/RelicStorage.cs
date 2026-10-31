// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using EnigmaticLegacy.Content.Items;
using EnigmaticLegacy.Content.Status;
using EnigmaticLegacy.Core;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

public static class RelicStorage
{
    public static bool IsPotion(Item i) => i.buffType > 0 && i.buffTime > 0 && i.consumable && !Main.vanityPet[i.buffType] && !Main.lightPet[i.buffType];
    public static void UsePotions(Player p, ExpansionItem bag)
    {
        bag.EnsureStorage();
        foreach (Item potion in bag.Storage)
        {
            if (!IsPotion(potion) || p.HasBuff(potion.buffType) || !CombinedHooks.CanUseItem(p, potion)) continue;
            if (ItemLoader.UseItem(potion, p) == false) continue;
            p.AddBuff(potion.buffType, potion.buffTime);
            if (potion.consumable && ItemLoader.ConsumeItem(potion, p)) { ItemLoader.OnConsumeItem(potion, p); potion.stack--; if (potion.stack <= 0) potion.TurnToAir(); }
        }
    }
}
