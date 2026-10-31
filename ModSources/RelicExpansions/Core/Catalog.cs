// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Terraria.ModLoader;

namespace RelicExpansions.Core;

public sealed class Definition
{
    public string Id = "", SourceMod = "", Key = "", InternalName = "", Name = "", EnglishName = "", Kind = "", Gate = "", Category = "", Texture = "";
    public int Frames = 1, FrameTicks = 3, FrameColumns = 1;
    public string Potion = "", Delivery = "";
    public bool Equippable => Kind is "accessory" or "boots";
}
public sealed class AttributeEffect { public string Stat = "", Op = ""; public float Value; }
public sealed class EffectDefinition
{
    public List<AttributeEffect> Attrs = new();
    public List<string> Implemented = new(), Reserved = new(), Paused = new();
    public int Fortune, Loot;
}
public static class Catalog
{
    private static readonly JsonSerializerOptions Options = new() { IncludeFields = true, PropertyNameCaseInsensitive = true };
    public static Mod Owner;
    public static readonly Dictionary<string, string> ModuleNames = new(StringComparer.Ordinal) {
        ["celestial_core"] = "CelestialCore", ["celestial_artifacts"] = "CelestialArtifacts"
    };
    public static readonly Dictionary<string, Mod> Modules = new(StringComparer.Ordinal);
    public static IEnumerable<Definition> ActiveItems => Items.Values.Where(d => HasSource(d.SourceMod));
    public static bool HasSource(string source) => Modules.ContainsKey(source);
    public static void RegisterModule(Mod module, string source)
    {
        if (!ModuleNames.TryGetValue(source, out string expected) || expected != module.Name)
            throw new ArgumentException("Unknown original content module: " + source);
        Modules.Add(source, module);
    }
    public static Mod ItemOwner(string id) => Items.TryGetValue(id, out var d) ? Modules.GetValueOrDefault(d.SourceMod) : null;
    public static string ItemTexture(string id) => Items[id].Texture.StartsWith("Terraria/", StringComparison.Ordinal) ? Items[id].Texture : ItemOwner(id).Name + "/" + Items[id].Texture;
    public static bool HasTexture(Definition d) => d.Texture.StartsWith("Terraria/", StringComparison.Ordinal) || ItemOwner(d.Id) is Mod mod && (mod.FileExists(d.Texture + ".png") || mod.FileExists(d.Texture + ".rawimg"));
    public static Dictionary<string, Definition> Items = new();
    public static Dictionary<string, EffectDefinition> Effects = new();
    public static Dictionary<string, float> Config = new();
    public static void Load(Mod mod)
    {
        if (Owner == mod) return;
        Owner = mod;
        Items = JsonSerializer.Deserialize<List<Definition>>(mod.GetFileBytes("Data/inventory.json"), Options).ToDictionary(d => d.Id, StringComparer.Ordinal);
        Effects = JsonSerializer.Deserialize<Dictionary<string, EffectDefinition>>(mod.GetFileBytes("Data/effects.json"), Options);
        Config = JsonSerializer.Deserialize<Dictionary<string, float>>(mod.GetFileBytes("Data/config-defaults.json"));
    }
    public static float C(string key) => Config[key];
    public static int Type(string id) => Items.TryGetValue(id, out var d) && ItemOwner(id) is Mod mod && mod.TryFind<ModItem>(d.InternalName, out var item) ? item.Type : 0;
    public static bool TryType(string id, out int type) { type = Type(id); return type > 0; }
    public static void Clear() { Owner = null; Modules.Clear(); Items.Clear(); Effects.Clear(); Config.Clear(); }
}
