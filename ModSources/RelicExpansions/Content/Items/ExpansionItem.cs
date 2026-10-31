// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnigmaticLegacy.Core;
using RelicExpansions.Core;
using RelicExpansions.Players;
using RelicExpansions.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RelicExpansions.Content.Items;

public abstract class ExpansionItem : ModItem
{
    public abstract string Id { get; }
    public Definition Definition => Catalog.Items[Id];
    public override string Texture => Definition.Texture.StartsWith("Terraria/", StringComparison.Ordinal) ? Definition.Texture : Mod.Name + "/" + Definition.Texture;
    public HashSet<string> Kills = new(StringComparer.Ordinal);
    public Dictionary<string, int> Progress = new();
    public string OwnerId = "", BoundWorld = "";
    public Vector2 BoundPosition;
    public Item[] Storage = Array.Empty<Item>();
    public void EnsureStorage()
    {
        int size = Id == "celestial_artifacts:potions_bag" ? 27 : 0;
        if (Storage.Length == size) return;
        Storage = Enumerable.Range(0, size).Select(_ => new Item()).ToArray();
    }
    public int Count(string key) => Progress.GetValueOrDefault(key);
    public void Set(string key, int value) => Progress[key] = value;
    public override void Load()
    {
        if (Definition.Kind == "armor")
        {
            EquipType equip = Definition.Key.EndsWith("chestplate") ? EquipType.Body : Definition.Key.EndsWith("leggings") ? EquipType.Legs : EquipType.Head;
            string armor = Definition.Key[..Definition.Key.LastIndexOf('_')];
            EquipLoader.AddEquipTexture(Mod, Mod.Name + "/Assets/Armor/" + armor + "_" + equip, equip, this);
        }
    }
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = Definition.Kind == "material" ? 25 : 1;
        if (Definition.Frames > 1) { Main.RegisterItemAnimation(Type, new AtlasAnimation(Definition.FrameTicks, Definition.Frames, Definition.FrameColumns)); ItemID.Sets.AnimatesAsSoul[Type] = true; }
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 32; Item.maxStack = Definition.Kind == "material" ? 9999 : 1;
        Item.value = Item.buyPrice(silver: 20);
        Item.rare = Definition.Gate == "eldritch" ? ItemRarityID.Red : Definition.Gate != "" ? ItemRarityID.Purple : ItemRarityID.Orange;
        Item.accessory = Definition.Equippable || Definition.Kind == "armor";
        if (Definition.Kind == "armor")
        {
            EquipType equip = Definition.Key.EndsWith("chestplate") ? EquipType.Body : Definition.Key.EndsWith("leggings") ? EquipType.Legs : EquipType.Head;
            int slot = EquipLoader.GetEquipSlot(Mod, Name, equip);
            if (equip == EquipType.Head) Item.headSlot = slot; else if (equip == EquipType.Body) Item.bodySlot = slot; else Item.legSlot = slot;
        }
        if (Definition.SourceMod == "celestial_core")
        {
            Item.defense = ExpansionWeapons.Armor(Id);
        }
        if (Definition.Kind == "weapon")
        {
            // Prefix data is usable while the attack itself awaits user design.
            Item.damage = BridgeUnits.Health(8); Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5; Item.useAnimation = Item.useTime = 30; Item.noMelee = true;
            ExpansionWeapons.Defaults(Item, Definition);
        }
        if (ExpansionActions.Usable(Id))
        {
            Item.useStyle = ItemUseStyleID.HoldUp; Item.useTime = Item.useAnimation = 24; Item.UseSound = SoundID.Item4;
            Item.consumable = ExpansionActions.Consumable(Id); if (Item.consumable) Item.maxStack = 999;
        }
        if (Definition.Potion != "")
        {
            Item.maxStack = 9999; Item.consumable = true; Item.useTime = Item.useAnimation = 24;
            Item.useStyle = Definition.Delivery == "" ? ItemUseStyleID.DrinkLiquid : ItemUseStyleID.Swing;
            Item.UseSound = Definition.Delivery == "" ? SoundID.Item3 : SoundID.Item1;
            if (Definition.Delivery == "") { var effect = ExpansionPotions.Effect(Definition); Item.buffType = effect.Buff; Item.buffTime = effect.Ticks; }
            else { Item.shoot = ModContent.ProjectileType<Content.Projectiles.ExpansionPotionProjectile>(); Item.shootSpeed = 9; Item.noUseGraphic = true; Item.noMelee = true; }
        }
    }
    public override bool WeaponPrefix() => Definition.Kind == "weapon";
    public override bool MeleePrefix() => Definition.Kind == "weapon" && Item.CountsAsClass(DamageClass.Melee);
    public override bool MagicPrefix() => Definition.Kind == "weapon" && Item.CountsAsClass(DamageClass.Magic);
    public override bool RangedPrefix() => Definition.Kind == "weapon" && Item.CountsAsClass(DamageClass.Ranged);
    public override bool CanEquipAccessory(Player player, int slot, bool modded) => Definition.Equippable && player.GetModPlayer<ExpansionPlayer>().CanUse(this);
    public override bool CanAccessoryBeEquippedWith(Item a, Item b, Player player)
    {
        if (a.type == b.type) return false;
        return EquipmentRules.Compatible(a, b);
    }
    public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<ExpansionPlayer>().Equip(this);
    public override void UpdateEquip(Player player) => player.GetModPlayer<ExpansionPlayer>().Equip(this);
    public override void UpdateInventory(Player player)
    {
        player.GetModPlayer<ExpansionPlayer>().Carry(this);
    }
    public override void HoldItem(Player player) => player.GetModPlayer<ExpansionPlayer>().Carry(this);


    public override bool CanUseItem(Player player) => player.GetModPlayer<ExpansionPlayer>().CanUse(this) && (Definition.Kind == "weapon" ? ExpansionWeapons.CanUse(player, this) : Definition.Potion != "" ? ExpansionPotions.Effect(Definition).Buff > 0 : ExpansionActions.CanUse(player, this));
    public override bool? CanHitNPC(Player player, NPC target) => Definition.Kind == "weapon" && (!ExpansionWeapons.Implemented(Id) || Item.noMelee || !player.GetModPlayer<ExpansionPlayer>().CanUse(this)) ? false : null;

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            if (Definition.Potion != "") { if (Definition.Delivery == "") ExpansionPotions.Apply(player, Definition); }
            else ExpansionActions.Use(player, this);
        }
        return true;
    }
    public override bool Shoot(Player p, EntitySource_ItemUse_WithAmmo source, Vector2 at, Vector2 velocity, int type, int damage, float knockback)
    {
        if (Definition.Kind == "weapon") return true;
        if (Definition.Potion == "") return true;
        Projectile.NewProjectile(source, at, velocity, type, 0, 0, p.whoAmI, Type, Definition.Delivery == "lingering" ? 1 : 0); return false;
    }
    public override void AddRecipes() { ExpansionRecipes.AddFor(this); ExpansionPotions.AddRecipe(this); }

    public override ModItem Clone(Item newEntity)
    {
        var copy = (ExpansionItem)base.Clone(newEntity);
        copy.Kills = new(Kills, StringComparer.Ordinal); copy.Progress = new(Progress);
        copy.Storage = Storage.Select(i => i.Clone()).ToArray();
        return copy;
    }
    public override void SaveData(TagCompound tag)
    {
        tag["kills"] = Kills.OrderBy(s => s).ToList(); tag["progress"] = Progress.Select(kv => new TagCompound { ["key"] = kv.Key, ["value"] = kv.Value }).ToList();
        tag["owner"] = OwnerId; tag["world"] = BoundWorld; tag["position"] = BoundPosition;
        EnsureStorage(); tag["storage"] = Storage.Select(ItemIO.Save).ToList();
    }
    public override void LoadData(TagCompound tag)
    {
        Kills = new(tag.GetList<string>("kills"), StringComparer.Ordinal);
        Progress = tag.GetList<TagCompound>("progress").ToDictionary(t => t.GetString("key"), t => t.GetInt("value"));
        OwnerId = tag.GetString("owner"); BoundWorld = tag.GetString("world"); BoundPosition = tag.Get<Vector2>("position");
        EnsureStorage(); var storage = tag.GetList<TagCompound>("storage");
        for (int n = 0; n < Math.Min(Storage.Length, storage.Count); n++) Storage[n] = ItemIO.Load(storage[n]);
    }
    public override void NetSend(BinaryWriter w) { TagCompound tag = new(); SaveData(tag); TagIO.Write(tag, w); }
    public override void NetReceive(BinaryReader r) => LoadData(TagIO.Read(r));
    public override void ModifyTooltips(List<TooltipLine> lines) => RelicTooltips.Append(Mod, Item, Id, lines, ExpansionWeapons.Implemented(Id));

}
