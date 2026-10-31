// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.IO;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Players;
using RelicExpansions.Core;
using RelicExpansions.Players;
using RelicExpansions.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions;

public sealed class RelicExpansions : Mod
{
    public static RelicExpansions Instance;
    public static ModKeybind RelicKey;
    private readonly Func<Player, float> loot = p => p.GetModPlayer<ExpansionPlayer>().LootChance;
    private readonly Func<Player, float> fortune = p => p.GetModPlayer<ExpansionPlayer>().FortuneChance;
    private readonly Func<Player, int> curses = p => { var e = p.GetModPlayer<ExpansionPlayer>(); return e.ActiveCurses; };
    public override void Load()
    {
        Instance = this; Catalog.Load(this);
        RelicKey = KeybindLoader.RegisterKeybind(this, "RelicSkill", "N");
        BridgeExtensions.LootProviders.Add(loot);
        BridgeExtensions.FortuneProviders.Add(fortune);
        BridgeExtensions.CurseProviders.Add(curses);
        RelicTooltips.ValueProviders.Add(ExpansionTooltipValues.Resolve);
    }
    public override void PostSetupContent()
    {
        ArmoredEnemies.Setup();
        BridgeExtensions.RegisterCategory(ModContent.ItemType<Content.Items.TotemOfUndying>(), "charm");
        foreach (var d in Catalog.ActiveItems)
            if (d.Equippable) BridgeExtensions.RegisterCategory(Catalog.Type(d.Id), d.Category);
        foreach (string name in new[] { "CultistBossLightningOrbArc", "VortexLightning", "MartianTurretBolt", "VortexVortexLightning" })
            if (ProjectileID.Search.TryGetId(name, out int type))
                ModLoader.GetMod("EnigmaticLegacy").Call("RegisterProjectileTag", type, "lightning");
    }
    public override object Call(params object[] args)
    {
        if (args.Length == 0 || args[0] is not string cmd) throw new ArgumentException("Expected a bridge command");
        return cmd switch {
            "GetItemType" when args.Length == 2 && args[1] is string id => Catalog.TryType(id, out int t) ? t : 0,
            "RegisterAllStatModifier" when args.Length == 2 && args[1] is Action<Player, float> provider => AllStatBridge.Register(provider),
            "RegisterArmoredNPC" when args.Length == 2 && args[1] is int npcType => ArmoredEnemies.Register(npcType),
            "GetApiVersion" => 1,
            "RunPortDiagnostics" when args.Length == 3 && args[1] is Action<string, bool> check && args[2] is bool registrationOnly => Diagnostics.PublicChecks.Run(check, registrationOnly),
            "RunPortDiagnostics" when args.Length == 2 && args[1] is Action<string, bool> check => Diagnostics.PublicChecks.Run(check),
            _ => throw new ArgumentException("Unknown RelicExpansions bridge call: " + cmd)
        };
    }
    public override void HandlePacket(BinaryReader reader, int whoAmI) => ExpansionNetwork.Handle(reader, whoAmI);
    public override void Unload()
    {
        BridgeExtensions.LootProviders.Remove(loot); BridgeExtensions.FortuneProviders.Remove(fortune);
        BridgeExtensions.CurseProviders.Remove(curses);
        RelicTooltips.ValueProviders.Remove(ExpansionTooltipValues.Resolve);
        ExpansionRecipes.Clear(); ExpansionAcquisition.Clear(); ArmoredEnemies.Clear(); Content.ExpansionEquipment.Clear(); Catalog.Clear(); Instance = null; RelicKey = null;
    }
}
