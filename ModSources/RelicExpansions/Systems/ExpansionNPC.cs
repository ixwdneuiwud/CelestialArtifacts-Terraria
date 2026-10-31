// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.IO;
using EnigmaticLegacy.Core;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RelicExpansions.Systems;

public sealed class ExpansionNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public int WeakTicks, AbyssStacks, AbyssTicks;
    private int exposeLevel, exposeTicks, maxLifeRemoved, tick;
    private readonly System.Collections.Generic.Dictionary<int, ulong> lastHits = new();
    public bool TwistedCopy;
    public bool AcceptHit(int player, ExpansionNetwork.TargetAction action)
    {
        int key = player * 256 + (int)action;
        if (lastHits.GetValueOrDefault(key) == Main.GameUpdateCount + 1) return false;
        lastHits[key] = Main.GameUpdateCount + 1; return true;
    }
    public void Expose(int level, int duration) { exposeLevel = Math.Max(exposeLevel, level); exposeTicks = Math.Max(exposeTicks, duration); }
    public override void ResetEffects(NPC npc)
    {
        // Restore only our contribution; never replace another mod's maximum life.
        if (maxLifeRemoved != 0) { npc.lifeMax += maxLifeRemoved; maxLifeRemoved = 0; }
    }
    public override void PostAI(NPC npc)
    {
        tick++;
        if (WeakTicks > 0) WeakTicks--;
        if (AbyssTicks > 0) AbyssTicks--; else AbyssStacks = 0;
        if (exposeTicks > 0)
        {
            exposeTicks--;
            maxLifeRemoved = (int)(Math.Max(1, npc.lifeMax) * Math.Min(.9f, .1f * exposeLevel));
            npc.lifeMax = Math.Max(1, npc.lifeMax - maxLifeRemoved);
            if (Main.netMode != NetmodeID.MultiplayerClient) npc.life = Math.Min(npc.life, npc.lifeMax);
        }
        else exposeLevel = 0;
        if (!npc.boss && (AbyssTicks > 0 || exposeTicks > 0)) npc.position -= npc.velocity * Math.Min(.9f, .1f * Math.Max(exposeLevel, AbyssStacks));
    }
    public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
    {
        if (WeakTicks > 0) modifiers.FinalDamage *= .8f;
    }

    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter w)
    { w.Write(WeakTicks); w.Write(AbyssStacks); w.Write(AbyssTicks); w.Write(exposeLevel); w.Write(exposeTicks); w.Write(TwistedCopy); }

    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader r)
    { WeakTicks = r.ReadInt32(); AbyssStacks = r.ReadInt32(); AbyssTicks = r.ReadInt32(); exposeLevel = r.ReadInt32(); exposeTicks = r.ReadInt32(); TwistedCopy = r.ReadBoolean(); }


    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.SpawnedFromStatue || npc.lifeMax <= 5) return;
        int killer = npc.lastInteraction;
        if (killer >= 0 && killer < Main.maxPlayers && Main.player[killer].active) ExpansionNetwork.Killed(Main.player[killer], npc);
    }
}
