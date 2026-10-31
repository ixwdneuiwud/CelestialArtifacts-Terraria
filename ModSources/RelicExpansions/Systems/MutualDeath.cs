// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RelicExpansions.Core;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

// Match the same attacker/victim in either event order, including lingering
// hostile projectiles. Slot reuse must never turn an unrelated death into a reward.
public sealed class MutualDeath : ModSystem
{
    private static long nextLife;
    private static readonly Dictionary<int, List<(long Life, ulong Tick)>> kills = new();
    private static readonly Dictionary<int, (long Life, ulong Tick, Vector2 Position)> deaths = new();
    internal static long NewLife() => ++nextLife;
    internal static void ForgetPlayer(int player) { kills.Remove(player); deaths.Remove(player); }
    public override void OnWorldUnload() { kills.Clear(); deaths.Clear(); }
    internal static bool Recent(ulong tick) => Main.GameUpdateCount >= tick && Main.GameUpdateCount - tick <= (ulong)ExpansionAcquisition.Rules.MutualDeathWindow;
    internal static void Killed(NPC npc)
    {
        if (!Catalog.HasSource("celestial_core") || Main.netMode == NetmodeID.MultiplayerClient || !EnigmaticLegacy.Systems.LegacyAcquisition.Eligible(npc)) return;
        int killer = npc.lastInteraction;
        if (killer < 0 || killer >= Main.maxPlayers || !Main.player[killer].active) return;
        if (!kills.TryGetValue(killer, out var list)) kills[killer] = list = new();
        list.RemoveAll(k => !Recent(k.Tick));
        list.Add((npc.GetGlobalNPC<MutualDeathNPC>().Life, Main.GameUpdateCount));
        Resolve(killer);
    }
    internal static void Died(Player player, PlayerDeathReason source, bool pvp)
    {
        if (!Catalog.HasSource("celestial_core") || Main.netMode == NetmodeID.MultiplayerClient || pvp) return;
        long life = 0;
        if (source.SourceNPCIndex >= 0 && source.SourceNPCIndex < Main.maxNPCs)
            life = Main.npc[source.SourceNPCIndex].GetGlobalNPC<MutualDeathNPC>().Life;
        else if (source.SourceProjectileLocalIndex >= 0 && source.SourceProjectileLocalIndex < Main.maxProjectiles)
        {
            Projectile projectile = Main.projectile[source.SourceProjectileLocalIndex];
            if (projectile.type == source.SourceProjectileType) life = projectile.GetGlobalProjectile<MutualDeathProjectile>().AttackerLife;
        }
        RecordDeath(player, life);
    }
    internal static void RecordDeath(Player player, long attackerLife)
    {
        deaths.Remove(player.whoAmI);
        if (attackerLife <= 0) return;
        deaths[player.whoAmI] = (attackerLife, Main.GameUpdateCount, player.Center);
        Resolve(player.whoAmI);
    }
    private static void Resolve(int player)
    {
        if (!deaths.TryGetValue(player, out var death) || !Recent(death.Tick) || !kills.TryGetValue(player, out var list) ||
            !list.Any(k => k.Life == death.Life && Recent(k.Tick))) return;
        Item.NewItem(new EntitySource_Misc("MutualDeathHeartFragment"), (int)death.Position.X, (int)death.Position.Y, 16, 16, Catalog.Type("celestial_core:heart_fragment"));
        deaths.Remove(player); kills.Remove(player);
    }
}

public sealed class MutualDeathNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal long Life;
    public override void OnSpawn(NPC npc, IEntitySource source) => Life = MutualDeath.NewLife();
    public override void OnKill(NPC npc) => MutualDeath.Killed(npc);
}

public sealed class MutualDeathProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    internal long AttackerLife;
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is not EntitySource_Parent parent) return;
        if (parent.Entity is NPC npc) AttackerLife = npc.GetGlobalNPC<MutualDeathNPC>().Life;
        else if (parent.Entity is Projectile p) AttackerLife = p.GetGlobalProjectile<MutualDeathProjectile>().AttackerLife;
    }
}
