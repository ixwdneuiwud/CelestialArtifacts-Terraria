// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using RelicExpansions.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
namespace RelicExpansions.Systems;
public sealed class ExpansionProjectile : GlobalProjectile
{
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (!projectile.arrow || source is not EntitySource_ItemUse || projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;
        var player = Main.player[projectile.owner]; if (!player.active) return;
        var e = player.GetModPlayer<ExpansionPlayer>();
        projectile.velocity *= Math.Max(.01f, 1 + e.ArrowSpeed);
        projectile.knockBack *= Math.Max(0, 1 + e.ArrowKnockback);
    }
}
