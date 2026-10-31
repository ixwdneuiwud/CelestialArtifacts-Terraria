// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using EnigmaticLegacy.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Systems;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;

namespace RelicExpansions.Content.Projectiles;

public sealed class ExpansionPotionProjectile : ImportedAreaPotion
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.BottledWater;
    private ExpansionItem Potion => ContentSamples.ItemsByType.TryGetValue((int)Projectile.ai[0], out var item) ? item.ModItem as ExpansionItem : null;
    protected override bool Valid => Potion != null;
    protected override bool Negative => Potion != null && (Potion.Definition.Potion.Contains("poison") || Potion.Definition.Potion.Contains("weakness") || Potion.Definition.Potion.Contains("slowness"));
    protected override int EffectTicks => Potion == null ? 0 : ExpansionPotions.Effect(Potion.Definition).Ticks;
    protected override void Apply(NPC npc, Player owner, float factor) => ExpansionPotions.Apply(npc, Potion.Definition, factor);
    protected override void Apply(Player player, float factor) => ExpansionPotions.Apply(player, Potion.Definition, factor);
    public override bool PreDraw(ref Color lightColor)
    {
        if (Cloud || Potion == null) return false;
        Texture2D texture = TextureAssets.Item[Potion.Type].Value;
        Rectangle frame = Main.itemAnimations[Potion.Type]?.GetFrame(texture) ?? texture.Bounds;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2, .7f, SpriteEffects.None);
        return false;
    }
}

