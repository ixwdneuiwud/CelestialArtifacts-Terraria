// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Linq;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace RelicExpansions.Systems;

public sealed class RelicStorageUI : ModSystem
{
    private static ExpansionItem opened;
    public static void Open(ExpansionItem item)
    {
        if (Main.dedServ) return;
        if (opened != null) Close();
        item.EnsureStorage(); opened = item; Main.playerInventory = true;
        { }
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }
    public static void Close()
    {
        { }
        if (opened != null && Main.netMode == NetmodeID.MultiplayerClient)
            for (int n = 0; n < Main.LocalPlayer.inventory.Length; n++)
                if (ReferenceEquals(Main.LocalPlayer.inventory[n].ModItem, opened)) NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, n);
        opened = null;
        if (Main.netMode == NetmodeID.MultiplayerClient) ExpansionNetwork.State(Main.LocalPlayer.GetModPlayer<ExpansionPlayer>());
    }
    public override void OnWorldUnload() => opened = null;
    public override void PostDrawInterface(SpriteBatch batch)
    {
        if (opened == null || Main.gameMenu) return;
        Player p = Main.LocalPlayer;
        if (!Main.playerInventory || p.dead || !p.inventory.Any(i => ReferenceEquals(i.ModItem, opened))) { Close(); return; }
        int w = 516, h = 260;
        Vector2 at = new(Math.Clamp(560, 8, Math.Max(8, Main.screenWidth - w - 12)), Math.Clamp(300, 8, Math.Max(8, Main.screenHeight - h - 12)));
        var rect = new Rectangle((int)at.X, (int)at.Y, w, h);
        batch.Draw(TextureAssets.MagicPixel.Value, rect, new Color(25, 32, 58, 245));
        if (rect.Contains(Main.mouseX, Main.mouseY)) { p.mouseInterface = true; p.controlUseItem = false; }
        Text(batch, opened.Item.Name, at + new Vector2(12, 10), Color.Gold);
        if (Button(batch, "关闭", at + new Vector2(w - 65, 9), 55)) { Close(); return; }
        {
            Item[] inventory = opened.Storage;
            int columns = 9;
            for (int n = 0; n < inventory.Length; n++)
            {
                int index = n;
                Slot(batch, inventory, n, at + new Vector2(12 + n % columns * 55, 49 + n / columns * 55), RelicStorage.IsPotion);
            }
            Text(batch, "仅存放药水；按住下键使用药水袋可批量饮用", at + new Vector2(12, h - 42), Color.LightGray, .75f);
        }
    }
    private static void Slot(SpriteBatch batch, Item[] inv, int n, Vector2 pos, Func<Item, bool> allowed)
    {
        float old = Main.inventoryScale; Main.inventoryScale = .85f;
        var rect = new Rectangle((int)pos.X, (int)pos.Y, 46, 46);
        if (rect.Contains(Main.mouseX, Main.mouseY))
        {
            Main.LocalPlayer.mouseInterface = true;
            if (Main.mouseItem.IsAir || allowed(Main.mouseItem)) ItemSlot.Handle(inv, ItemSlot.Context.BankItem, n);
            ItemSlot.MouseHover(inv, ItemSlot.Context.BankItem, n);
        }
        ItemSlot.Draw(batch, inv, ItemSlot.Context.BankItem, n, pos);
        Main.inventoryScale = old;
    }
    private static void Text(SpriteBatch batch, string text, Vector2 at, Color color, float scale = .9f) => Utils.DrawBorderString(batch, text, at, color, scale);
    private static bool Button(SpriteBatch batch, string text, Vector2 at, int width)
    {
        bool hover = new Rectangle((int)at.X, (int)at.Y, width, 29).Contains(Main.mouseX, Main.mouseY);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)at.X, (int)at.Y, width, 29), hover ? new Color(80, 92, 135) : new Color(48, 60, 95));
        Text(batch, text, at + new Vector2(5, 4), Color.White, .75f);
        return hover && Main.mouseLeft && Main.mouseLeftRelease;
    }
}
