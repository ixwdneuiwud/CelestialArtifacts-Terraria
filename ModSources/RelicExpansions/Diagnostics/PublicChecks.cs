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
using RelicExpansions.Content;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using RelicExpansions.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RelicExpansions.Diagnostics;

// Explicitly invoked by the dependency's isolated-profile diagnostic runner.
public static class PublicChecks
{
    public static object Run(Action<string, bool> check, bool registrationOnly = false)
    {
        var details = new List<string>();
        var active = Catalog.ActiveItems.ToArray();
        check("Public Celestial: module registrations match the enabled content mods", Catalog.ModuleNames.All(pair => Catalog.HasSource(pair.Key) == ModLoader.HasMod(pair.Value)));
        foreach (var module in Catalog.ModuleNames)
        {
            var items = Catalog.Items.Values.Where(d => d.SourceMod == module.Key).ToArray();
            check("Public Celestial: independent content ownership " + module.Key,
                items.Length == (module.Key == "celestial_core" ? 82 : 107) &&
                items.All(d => Catalog.HasSource(module.Key) ? Catalog.Type(d.Id) > 0 && Catalog.HasTexture(d) : Catalog.Type(d.Id) == 0));
        }
        check("Public Celestial: all enabled item variants and textures resolve", Catalog.Items.Count == 189 && active.All(d => Catalog.Type(d.Id) > 0 && Catalog.HasTexture(d)));
        check("Public Celestial: every original item has an effect review", Catalog.Items.Keys.All(Catalog.Effects.ContainsKey));
        check("Public Celestial: all enabled crafting inputs resolve", (active.Length == 0 || ExpansionRecipes.Registered.Count > 0) && ExpansionRecipes.Deferred.Count == 0);
        if (!registrationOnly)
        {
            const int slot = 253;
            int mode = Main.netMode, playerIndex = Main.myPlayer;
            Player previous = Main.player[slot];
            try
            {
                Main.netMode = NetmodeID.SinglePlayer; Main.myPlayer = slot;
                Player p = new() { whoAmI = slot, active = true, name = "Celestial source fixture", position = new Vector2(Main.spawnTileX * 16, Main.spawnTileY * 16) };
                Main.player[slot] = p;
                using var context = new Main.CurrentPlayerOverride(p);
                var e = p.GetModPlayer<ExpansionPlayer>();
                foreach (var d in Catalog.Items.Values.Where(d => d.Equippable || d.Kind == "armor"))
                {
                    p.ResetEffects(); e.ResetEffects(); p.GetModPlayer<LegacyPlayer>().ResetEffects();
                    p.statLife = p.statLifeMax = p.statLifeMax2 = 500;
                    for (int n = 0; n < p.armor.Length; n++) p.armor[n] = new Item();
                    for (int n = 0; n < p.buffType.Length; n++) { p.buffType[n] = p.buffTime[n] = 0; }
                    p.armor[3] = new Item(Catalog.Type("celestial_artifacts:catastrophe_scroll"));
                    p.armor[4] = new Item(Catalog.Type(d.Id));
                    e.Equip((ExpansionItem)p.armor[3].ModItem); e.Equip((ExpansionItem)p.armor[4].ModItem);
                    ExpansionEquipment.Invalidate(p); e.PostUpdateEquips(); e.PostUpdate();
                    check("Public Celestial equipment: " + d.Id, e.Has(d.Id) && p.statLifeMax2 > 0 && float.IsFinite(p.moveSpeed) && float.IsFinite(p.GetTotalDamage(DamageClass.Melee).ApplyTo(100)));
                }
                var dedicated = ModContent.GetContent<TypedRelicSlot>().First(s => s.Category == "catastrophe");
                check("Public Celestial: catastrophe slot accepts only its own relic", dedicated.CanAcceptItem(new Item(Catalog.Type("celestial_artifacts:catastrophe_scroll")), AccessorySlotType.FunctionalSlot) && !dedicated.CanAcceptItem(new Item(ItemID.HermesBoots), AccessorySlotType.FunctionalSlot));
                var ids = new HashSet<string> { "celestial_artifacts:catastrophe_scroll" };
                check("Public Celestial: seven etching slots remain type-specific", ExpansionEquipment.Capacity("etching", ids, false) == 7 && ExpansionEquipment.Capacity("curio", ids, false) == 0);
                var bag = (ExpansionItem)new Item(Catalog.Type("celestial_artifacts:potions_bag")).ModItem;
                bag.EnsureStorage(); bag.Storage[0] = new Item(ItemID.IronskinPotion, 3); bag.Set("fixture", 9);
                var restored = (ExpansionItem)ItemIO.Load(ItemIO.Save(bag.Item)).ModItem;
                check("Public Celestial: potion-bag storage and progression survive save/load", restored.Storage.Length == 27 && restored.Storage[0].type == ItemID.IronskinPotion && restored.Storage[0].stack == 3 && restored.Count("fixture") == 9);
                CelestialAcquisitionChecks.Run(p, check, details);
            }
            catch (Exception ex) { check("Public Celestial: unhandled runtime exception", false); details.Add(ex.ToString()); }
            finally { Main.netMode = mode; Main.myPlayer = playerIndex; Main.player[slot] = previous; }
        }
        return new { version = Catalog.Owner.Version.ToString(), details, registered = ExpansionRecipes.Registered, deferred = ExpansionRecipes.Deferred,
            loadedMods = ModLoader.Mods.Select(m => new { m.Name, version = m.Version.ToString() }).ToArray() };
    }
}

internal static class ChestAcquisitionChecks
{
    internal sealed class FirstRoll : Terraria.Utilities.UnifiedRandom
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
