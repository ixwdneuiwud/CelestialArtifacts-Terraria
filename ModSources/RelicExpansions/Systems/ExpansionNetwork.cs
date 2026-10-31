// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System.IO;
using System;
using System.Linq;
using EnigmaticLegacy.Core;
using Microsoft.Xna.Framework;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

// Equipment itself uses tML's native inventory/ModAccessorySlot synchronization.
public static class ExpansionNetwork
{
    private enum Packet : byte { State = 0, Skill = 1, Killed = 2, Target = 4, Aim = 5, Death = 6 }


    public enum TargetAction : byte { AbyssLock = 0, Deer = 2, SoulBoxHigh = 4, SoulBoxLow = 5 }
    public static void Aim(Vector2 direction)
    {
        var packet = RelicExpansions.Instance.GetPacket(); packet.Write((byte)Packet.Aim); packet.Write(direction.X); packet.Write(direction.Y); packet.Send();
    }
    public static void Debuff(Player player, NPC target, TargetAction action, int damage = 0)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) { ApplyTarget(player, target, action, damage); return; }
        var packet = RelicExpansions.Instance.GetPacket(); packet.Write((byte)Packet.Target); packet.Write((short)target.whoAmI); packet.Write((byte)action); packet.Write(damage); packet.Send();
    }
    private static void ApplyTarget(Player player, NPC target, TargetAction action, int damage)
    {
        float reach = 2000;
        if (!player.active || !target.active || target.friendly || target.immortal || target.dontTakeDamage || player.DistanceSQ(target.Center) > reach * reach) return;
        var e = player.GetModPlayer<ExpansionPlayer>(); var n = target.GetGlobalNPC<ExpansionNPC>();
        switch (action)
        {
            case TargetAction.AbyssLock when e.HasCA("lock_of_abyss"):
                if (!n.AcceptHit(player.whoAmI, action)) return;
                if (n.AbyssStacks >= Catalog.C("lockOfAbyssThreshold"))
                {
                    n.AbyssStacks = n.AbyssTicks = 0;
                    int limit = (int)Math.Min(100000000, Math.Max(1d, player.GetWeaponDamage(player.HeldItem)) * 40);
                    // The follow-up bypasses the original hit hook, so it cannot grow its own stacks.
                    target.SimpleStrikeNPC((int)(Math.Clamp(damage, 0, limit) * Catalog.C("lockOfAbyssExtraDamage")), 0, false, 0, ModContent.GetInstance<Content.AbyssDamage>());
                }
                else { n.AbyssStacks++; n.AbyssTicks = (int)Catalog.C("lockOfAbyssDuration") * 60; }
                break;
            case TargetAction.Deer when e.HasCA("deer_inscribed_amulet"):
                n.WeakTicks = Math.Max(n.WeakTicks, 300); target.AddBuff(BuffID.Bleeding, 300); break;
            case TargetAction.SoulBoxHigh when e.HasCA("soul_box"):
                n.Expose(1 + (int)Catalog.C("soulBoxShatterHighLevel"), (int)Catalog.C("soulBoxShatterHighDuration") * 60); break;
            case TargetAction.SoulBoxLow when e.HasCA("soul_box"):
                n.Expose(1 + (int)Catalog.C("soulBoxShatterLowLevel"), (int)Catalog.C("soulBoxShatterLowDuration") * 60); break;
            default: return;
        }
        target.netUpdate = true;
    }
    public static void Death(Player player, int source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        { var p = RelicExpansions.Instance.GetPacket(); p.Write((byte)Packet.Death); p.Write((short)source); p.Send(); return; }
        var e = player.GetModPlayer<ExpansionPlayer>();
        if (e.LastDeathEffectTick == Main.GameUpdateCount + 1) return;
        e.LastDeathEffectTick = Main.GameUpdateCount + 1;
        if (!e.HasCA("sacrificial_object")) return;
        if (Main.rand.NextFloat() < Catalog.C("sacrificialObjectHeritage")) Item.NewItem(player.GetSource_Death(), player.getRect(), ItemID.GoldBar);
        foreach (NPC npc in Main.npc.Where(n => n.active && !n.friendly && !n.boss && n.realLife < 0 && n.lifeMax < player.statLifeMax2 && n.DistanceSQ(player.Center) < 256 * 256 && !n.dontTakeDamage && !n.immortal))
            if (Main.rand.NextFloat() < Catalog.C("sacrificialObjectSacrifice")) npc.StrikeInstantKill();
    }

    public static void State(ExpansionPlayer e, int to = -1, int ignore = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) return;
        var packet = RelicExpansions.Instance.GetPacket(); packet.Write((byte)Packet.State); packet.Write((byte)e.Player.whoAmI);
        e.Write(packet); packet.Send(to, ignore);
    }
    public static void Skill(bool bind)
    {
        var packet = RelicExpansions.Instance.GetPacket(); packet.Write((byte)Packet.Skill); packet.Write(bind); packet.Send();
    }
    public static void Killed(Player player, NPC npc)
    {
        ExpansionAcquisition.RecordKill(player, npc.type);
        if (Main.netMode == NetmodeID.SinglePlayer) { player.GetModPlayer<ExpansionPlayer>().RecordKill(npc.type, npc.lifeMax, npc.friendly); return; }
        if (Main.netMode != NetmodeID.Server) return;
        var packet = RelicExpansions.Instance.GetPacket(); packet.Write((byte)Packet.Killed); packet.Write(npc.type); packet.Write(npc.lifeMax); packet.Write(npc.friendly); packet.Send(player.whoAmI);
    }
    public static void Handle(BinaryReader reader, int who)
    {
        switch ((Packet)reader.ReadByte())
        {
            case Packet.Aim:
                float x = reader.ReadSingle(), y = reader.ReadSingle();
                if (Main.netMode != NetmodeID.Server || who < 0 || who >= Main.maxPlayers || !float.IsFinite(x) || !float.IsFinite(y)) return;
                Main.player[who].GetModPlayer<ExpansionPlayer>().AimDirection = new Vector2(x, y).SafeNormalize(Vector2.UnitX);
                break;
            case Packet.Target:
                int npc = reader.ReadInt16(); TargetAction action = (TargetAction)reader.ReadByte(); int dealt = reader.ReadInt32();
                if (Main.netMode != NetmodeID.Server || who < 0 || who >= Main.maxPlayers || npc < 0 || npc >= Main.maxNPCs || Main.player[who].dead) return;
                ApplyTarget(Main.player[who], Main.npc[npc], action, dealt); break;
            case Packet.Death:
                int source = reader.ReadInt16();
                if (Main.netMode == NetmodeID.Server && who >= 0 && who < Main.maxPlayers && Main.player[who].active && Main.player[who].dead) Death(Main.player[who], source);
                break;
            case Packet.State:
                int id = reader.ReadByte();
                if (id < 0 || id >= Main.maxPlayers || Main.netMode == NetmodeID.Server && id != who) return;
                var e = Main.player[id].GetModPlayer<ExpansionPlayer>(); e.Read(reader);
                if (Main.netMode == NetmodeID.Server) State(e, -1, who);
                break;
            case Packet.Skill:
                bool bind = reader.ReadBoolean();
                if (Main.netMode != NetmodeID.Server || who < 0 || who >= Main.maxPlayers || !Main.player[who].active || Main.player[who].dead) return;
                ExpansionActions.Activate(Main.player[who], bind); break;
            case Packet.Killed:
                int type = reader.ReadInt32(), health = reader.ReadInt32(); bool friendly = reader.ReadBoolean();
                if (Main.netMode == NetmodeID.MultiplayerClient && type > 0 && type < Terraria.ModLoader.NPCLoader.NPCCount)
                    Main.LocalPlayer.GetModPlayer<ExpansionPlayer>().RecordKill(type, health, friendly);
                break;
        }
    }
}
