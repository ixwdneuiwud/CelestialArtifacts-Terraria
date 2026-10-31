// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace RelicExpansions.Core;

// Reuse Terraria's update clock, including frame overrides used by item drawing.
// Minecraft sprites have no two-pixel Terraria strip padding to trim off.
public sealed class AtlasAnimation : DrawAnimationVertical
{
    public readonly int Columns;
    public int Rows => (FrameCount + Columns - 1) / Columns;

    public AtlasAnimation(int ticks, int frames, int columns) : base(ticks, frames)
        => Columns = columns;

    public override Rectangle GetFrame(Texture2D texture, int frameCounterOverride = -1)
    {
        int index = frameCounterOverride == -1 ? Frame : frameCounterOverride / TicksPerFrame % FrameCount;
        int width = texture.Width / Columns, height = texture.Height / Rows;
        return new Rectangle(index % Columns * width, index / Columns * height, width, height);
    }
}
