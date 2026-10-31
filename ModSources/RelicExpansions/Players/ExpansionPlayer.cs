// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnigmaticLegacy.Core;
using EnigmaticLegacy.Players;
using RelicExpansions.Content.Items;
using RelicExpansions.Core;
using RelicExpansions.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RelicExpansions.Players;

public sealed partial class ExpansionPlayer : ModPlayer
{
    public readonly Dictionary<string, ExpansionItem> Equipped = new(StringComparer.Ordinal);
    public readonly Dictionary<string, ExpansionItem> Carried = new(StringComparer.Ordinal);
    public Dictionary<string, int> Cooldowns = new();
    public bool Gifts, CelestialGifts, BoundCatastrophe, NetherStage;
    public int CurseHistory, Deaths;
    public float AllStatsFactor = 1;
    public int SinceHurt = 10000, Ticks, BladeTicks, HiddenTicks;
    public float Toughness, Healing = 1, Luck, CritDamage, ProjectileDamage, IncomingMultiplier = 1, FlatReduction, KnockbackResistance, ArrowSpeed, ArrowKnockback, FlightSpeed = 1;
    public float LootChance, FortuneChance, ArrowDamage;
    private Item flightWings;
    private float healingRemainder;
    public string DeathWorld = "";
    public Vector2 DeathPosition;
    public int AbyssWill;
    public long AbyssWillDamage;
    internal ulong LastDeathEffectTick;
    private readonly Dictionary<int, int> priorBuffs = new();
    private readonly Dictionary<string, (float Base, StatModifier Modifier, float Applied)> attributeGroups = new();
    internal float AttributeValue(string stat, string operation, float amount, float current, bool integral = false)
    {
        var state = attributeGroups.TryGetValue(stat, out var old) ? old : (Base: current, Modifier: StatModifier.Default, Applied: current);
        state.Modifier = DamageArithmetic.Attribute(state.Modifier, operation == "add" ? DamageOperation.Addition : operation == "multBase" ? DamageOperation.MultiplyBase : DamageOperation.MultiplyTotal, amount);
        float next = state.Modifier.ApplyTo(state.Base);
        if (integral) next = MathF.Round(next);
        float result = current + next - state.Applied;
        attributeGroups[stat] = (state.Base, state.Modifier, next);
        return result;
    }
    private void Attribute(string stat, string operation, float value) => ExpansionStats.Apply(this, stat, operation, value);
    private string priorRealm = "";
    public Vector2 AimDirection = Vector2.UnitX;
    public bool CatastropheOn => Wears("celestial_artifacts:catastrophe_scroll");
    public bool Has(string id) => Equipped.ContainsKey(id);
    public bool HasCA(string key) => Has("celestial_artifacts:" + key);
    public ExpansionItem Get(string id) => Equipped.GetValueOrDefault(id) ?? Carried.GetValueOrDefault(id);
    public bool Wears(string id)
    {
        bool Match(Item item) => item.ModItem is ExpansionItem i && i.Id == id;
        for (int n = 3; n < 10; n++) if (Player.IsItemSlotUnlockedAndUsable(n) && Match(Player.armor[n])) return true;
        using var context = new Main.CurrentPlayerOverride(Player);
        foreach (var slot in ModContent.GetContent<ModAccessorySlot>())
            if (Match(slot.FunctionalItem) && (slot is Content.TypedRelicSlot typed ? typed.ActiveForEligibility : slot.IsEnabled())) return true;
        return false;
    }
    public bool CanUse(ExpansionItem item) => item.Definition.Gate switch {
        "catastrophe" => CatastropheOn,
        "cursed" or "eldritch" => BridgeExtensions.HasAccess(Player, item.Definition.Gate, item.Id),
        "either" => BridgeExtensions.HasAccess(Player, "cursed", item.Id),
        _ => true
    };

    public void Equip(ExpansionItem item) { if (CanUse(item)) Equipped.TryAdd(item.Id, item); }
    public void Carry(ExpansionItem item) { if (CanUse(item)) Carried.TryAdd(item.Id, item); }
    public override void ResetEffects()
    {
        Equipped.Clear(); Carried.Clear();
        attributeGroups.Clear();
        Toughness = Luck = CritDamage = ProjectileDamage = FlatReduction = KnockbackResistance = ArrowSpeed = ArrowKnockback = LootChance = FortuneChance = ArrowDamage = 0;
        Healing = IncomingMultiplier = FlightSpeed = AllStatsFactor = 1;
    }
    public override void PostUpdateEquips()
    {
        if (HasCA("catastrophe_scroll")) BoundCatastrophe = true;
        foreach (var pair in Equipped)
        {
            var effect = Catalog.Effects[pair.Key];
            foreach (var a in effect.Attrs) ExpansionStats.Apply(this, a);
            LootChance += .1f * effect.Loot; FortuneChance += .1f * effect.Fortune;
        }
        ApplyCore(); ApplyCelestial();
        // Native ammunition calculation snapshots this once into the arrow's
        // damage; derivative fields/projectiles inherit it without another hit bonus.
        Player.arrowDamage *= Math.Max(.01f,1 + ArrowDamage);
        if (Toughness != 0) Player.statDefense += (int)MathF.Round(Toughness * PortConfig.Current.ArmorScale);
        Player.statLifeMax2 = Math.Max(1, Player.statLifeMax2);
    }
    public override void ModifyLuck(ref float luck) { luck += Luck; if (luck > 0) luck *= AllStatsFactor; }
    public override void UpdateLifeRegen() { if (Player.lifeRegen > 0) Player.lifeRegen = Math.Max(0, (int)(Player.lifeRegen * Healing)); }
    public override void PostUpdate()
    {
        Ticks++; SinceHurt++;
        foreach (string key in Cooldowns.Keys.ToArray()) if (Cooldowns[key] > 0) Cooldowns[key]--;
        if (BladeTicks > 0) BladeTicks--;
        if (HiddenTicks > 0) HiddenTicks--;
        TickCelestial();
        if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
        {
            AimDirection = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
            if (Main.netMode == NetmodeID.MultiplayerClient && Ticks % 6 == 0) ExpansionNetwork.Aim(AimDirection);
        }
        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer && Ticks % 120 == 0) ExpansionNetwork.State(this);
    }
    public override void OnEnterWorld()
    {
        if (Player.whoAmI != Main.myPlayer || CelestialGifts || !Catalog.HasSource("celestial_artifacts")) return;
        Give("celestial_artifacts:heirloom_necklace"); Give("celestial_artifacts:catastrophe_scroll");
        Gifts = CelestialGifts = true;
    }

    public void Give(string id) { if (Catalog.TryType(id, out int type)) Player.QuickSpawnItem(Player.GetSource_Misc("RelicProgression"), type); }
    public bool Ready(string id) => Cooldowns.GetValueOrDefault(id) <= 0;
    public void Cooldown(string id, int ticks) => Cooldowns[id] = Math.Max(0, ticks);
    public float Missing => 1 - Math.Clamp((float)Player.statLife / Math.Max(1, Player.statLifeMax2), 0, 1);
    public int PositiveBuffs => Player.buffType.Count(t => t > 0 && !Main.debuff[t] && !Main.vanityPet[t] && !Main.lightPet[t]);
    public int NegativeBuffs => Player.buffType.Count(t => t > 0 && Main.debuff[t] && !Compatibility.ProtectedBuff(t));
    public void Heal(float amount)
    {
        if (Player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server || amount <= 0) return;
        healingRemainder += amount;
        int whole = (int)healingRemainder; healingRemainder -= whole;
        if (whole > 0) Player.Heal(whole);
    }
    public void GrantFlight(bool infinite)
    {
        flightWings ??= new Item(ItemID.AngelWings);
        LegacyFlight.Grant(Player, flightWings, infinite);
    }
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (RelicExpansions.RelicKey.JustPressed) ExpansionActions.Activate(Player);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        MutualDeath.Died(Player, damageSource, pvp);
        Deaths++; DeathPosition = Player.position; DeathWorld = Main.ActiveWorldFileData.UniqueId.ToString();
        if (!pvp && Player.whoAmI == Main.myPlayer) ExpansionNetwork.Death(Player, damageSource.SourceNPCIndex);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["netherStage"] = NetherStage; tag["gifts"] = Gifts; tag["celestialGifts"] = CelestialGifts;
        tag["catastrophe"] = BoundCatastrophe; tag["history"] = CurseHistory; tag["deaths"] = Deaths;
        tag["deathWorld"] = DeathWorld; tag["deathPosition"] = DeathPosition;
        tag["cooldowns"] = Cooldowns.Select(kv => new TagCompound { ["key"] = kv.Key, ["ticks"] = kv.Value }).ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        NetherStage = tag.GetBool("netherStage"); Gifts = tag.GetBool("gifts");
        CelestialGifts = tag.ContainsKey("celestialGifts") ? tag.GetBool("celestialGifts") : Gifts;
        BoundCatastrophe = tag.GetBool("catastrophe"); CurseHistory = tag.GetInt("history") & 127; Deaths = tag.GetInt("deaths");
        DeathWorld = tag.GetString("deathWorld"); DeathPosition = tag.Get<Vector2>("deathPosition");
        Cooldowns = tag.GetList<TagCompound>("cooldowns").ToDictionary(t => t.GetString("key"), t => t.GetInt("ticks"));
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => ExpansionNetwork.State(this, toWho, fromWho);
    internal void Write(BinaryWriter w) { TagCompound state = new(); SaveData(state); state["itemProgress"] = WriteItemProgress(); TagIO.Write(state, w); }
    internal void Read(BinaryReader r) { var state = TagIO.Read(r); LoadData(state); ReadItemProgress(state.GetList<TagCompound>("itemProgress")); }
}
