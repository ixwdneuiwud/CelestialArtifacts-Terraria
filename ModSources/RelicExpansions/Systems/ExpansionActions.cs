// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Content.Status;
using RelicExpansions.Content.Items;
using RelicExpansions.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

public static class ExpansionActions
{
    public static bool Usable(string id) => id is "celestial_artifacts:backtrack_mirror" or "celestial_artifacts:repent_mirror" or "celestial_artifacts:unlucky_potato" or "celestial_artifacts:potions_bag";
    public static bool Consumable(string id) => id == "celestial_artifacts:unlucky_potato";
    public static bool CanUse(Player p, ExpansionItem item)
    {
        var e = p.GetModPlayer<ExpansionPlayer>();
        if (!Usable(item.Id)) return false;
        { }
        { }
        { }
        if (item.Id.EndsWith("_mirror")) return e.Ready(item.Id) && (item.Definition.Key != "repent_mirror" || e.DeathWorld == Main.ActiveWorldFileData.UniqueId.ToString());
        return e.Ready(item.Id);
    }
    public static void Use(Player p, ExpansionItem item)
    {
        var e = p.GetModPlayer<ExpansionPlayer>();
        switch (item.Id)
        {
            case "celestial_artifacts:potions_bag":
                if (p.controlDown) RelicStorage.UsePotions(p, item); else RelicStorageUI.Open(item);
                e.Cooldown(item.Id, 30); break;
            case "celestial_artifacts:backtrack_mirror":
                Vector2 home = new((p.SpawnX >= 0 ? p.SpawnX : Main.spawnTileX) * 16, ((p.SpawnY >= 0 ? p.SpawnY : Main.spawnTileY) * 16) - p.height);
                if (Teleport(p, home)) e.Cooldown(item.Id, (int)Core.Catalog.C("backtrackMirrorCooldown") * 60); break;
            case "celestial_artifacts:repent_mirror":
                if (e.DeathWorld == Main.ActiveWorldFileData.UniqueId.ToString() && Teleport(p, e.DeathPosition)) e.Cooldown(item.Id, (int)Core.Catalog.C("repentMirrorCooldown") * 60); break;
            case "celestial_artifacts:unlucky_potato": p.AddBuff(BuffID.Slow, 3600); break;
        }
    }
    public static int ClearEffects(Player p, bool harmfulOnly)
    {
        int removed = 0;
        for (int n = p.buffType.Length - 1; n >= 0; n--)
        {
            int type = p.buffType[n];
            if (type > 0 && !Compatibility.ProtectedBuff(type) && !Main.vanityPet[type] && !Main.lightPet[type] && (!harmfulOnly || Main.debuff[type]))
            { p.DelBuff(n); removed++; }
        }
        return removed;
    }
    public static bool Teleport(Player p, Vector2 destination)
    {
        for (int y = 0; y <= 12; y++)
        {
            Vector2 at = destination - new Vector2(0, y * 16);
            if (at.X < 32 || at.Y < 32 || at.X + p.width >= (Main.maxTilesX - 2) * 16 || at.Y + p.height >= (Main.maxTilesY - 2) * 16 || Collision.SolidCollision(at, p.width, p.height)) continue;
            p.Teleport(at, TeleportationStyleID.RodOfDiscord); p.velocity = Vector2.Zero; p.fallStart = (int)p.position.Y / 16;
            if (Main.netMode != NetmodeID.SinglePlayer) NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, p.whoAmI, at.X, at.Y, TeleportationStyleID.RodOfDiscord);
            return true;
        }
        return false;
    }
    public static void Activate(Player p, bool? bindOverride = null)
    {
        var e = p.GetModPlayer<ExpansionPlayer>(); bool bind = bindOverride ?? p.controlDown;
        if (Main.netMode == NetmodeID.MultiplayerClient) { ExpansionNetwork.Skill(bind); }
        if (p.dead || !e.Ready("skill")) return;
        { }
        if (e.HasCA("sea_god_crown") && e.Ready("rain") && !Main.raining && Main.netMode != NetmodeID.MultiplayerClient)
        { Main.StartRain(); Main.rainTime = Math.Max(Main.rainTime, 60000); e.Cooldown("rain", (int)Core.Catalog.C("seaGodCrownCoolDown") * 60); if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData); }
        if (e.HasCA("corrupt_badge") && e.Ready("corrupt") && p.whoAmI == Main.myPlayer)
        { int time = (int)Core.Catalog.C("corruptBadgeDebuffDuration") * 60; p.AddBuff(BuffID.Poisoned, time); p.AddBuff(BuffID.Slow, time); e.Cooldown("corrupt", (int)Core.Catalog.C("corruptBadgeCooldown") * 60); }
        if (e.HasCA("skywalker_scroll"))
        {
            var i = e.Get("celestial_artifacts:skywalker_scroll");
            if (bind) { i.BoundWorld = Main.ActiveWorldFileData.UniqueId.ToString(); i.BoundPosition = p.position; }
            else if (i.BoundWorld == Main.ActiveWorldFileData.UniqueId.ToString() && p.whoAmI == Main.myPlayer && Teleport(p, i.BoundPosition)) e.Cooldown("skill", (int)Core.Catalog.C("skyWalkerCooldown") * 60);
        }
        if (e.HasCA("abyss_will_badge") && e.Ready("abyssWill"))
        {
            e.AbyssWill = (int)Core.Catalog.C("abyssWillBadgeInitialLevel"); e.AbyssWillDamage = 0; e.Cooldown("abyssWill", (int)Core.Catalog.C("abyssWillBadgeDuration") * 60);
        }
        { }
        { }
        { }
        { }
    }
}
