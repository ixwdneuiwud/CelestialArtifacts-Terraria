// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Systems;

// Defense alone does not mean worn armor (slimes and shells also have defense).
// Other ports/mods can explicitly register their armored enemies through Mod.Call.
public static class ArmoredEnemies
{
    private static readonly HashSet<int> extra = new();
    private static readonly HashSet<int> vanilla = new();
    public static bool Register(int type)
    {
        if (type <= 0 || type >= NPCLoader.NPCCount) throw new ArgumentOutOfRangeException(nameof(type));
        return extra.Add(type);
    }
    public static void Setup()
    {
        foreach (string name in new[] { "GoblinWarrior", "GreekSkeleton", "AngryBonesBigHelmet", "ArmoredSkeleton", "ArmoredViking",
            "BlueArmoredBones", "BlueArmoredBonesMace", "BlueArmoredBonesNoPants", "BlueArmoredBonesSword",
            "HellArmoredBones", "HellArmoredBonesMace", "HellArmoredBonesSpikeShield", "HellArmoredBonesSword",
            "RustyArmoredBonesAxe", "RustyArmoredBonesFlail", "RustyArmoredBonesSword",
            "SkeletonCommando", "SkeletonSniper", "TacticalSkeleton", "Paladin", "HeadlessHorseman" })
            if (NPCID.Search.TryGetId(name, out int type)) vanilla.Add(type);
    }
    public static bool Matches(NPC npc) => vanilla.Contains(npc.type) || extra.Contains(npc.type);
    public static void Clear() { extra.Clear(); vanilla.Clear(); }
}
