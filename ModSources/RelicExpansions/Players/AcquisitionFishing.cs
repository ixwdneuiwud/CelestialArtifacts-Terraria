// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using RelicExpansions.Core;
using RelicExpansions.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RelicExpansions.Players;

public sealed class AcquisitionFishing : ModPlayer
{
    public static bool Eligible(FishingAttempt attempt, int itemDrop, int npcSpawn) => attempt.heightLevel == 0 &&
        !attempt.inLava && !attempt.inHoney && !attempt.crate && itemDrop > 0 && itemDrop < ItemID.Count &&
        itemDrop != attempt.questFish && npcSpawn <= 0;
    public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
    {
        if (Catalog.TryType("celestial_artifacts:magic_horseshoe", out int horseshoe) && Eligible(attempt, itemDrop, npcSpawn) && Main.rand.NextBool(ExpansionAcquisition.Rules.SkyFishingDenominator))
            itemDrop = horseshoe;
    }
}
