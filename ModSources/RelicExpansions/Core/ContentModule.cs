// Unofficial Terraria adaptation of Celestial Artifacts/Core by Xiaoyue and contributors.
// Java/Forge to C#/tModLoader rewrite; publication cleanup modified 2026-10-31.
// SPDX-License-Identifier: LGPL-2.1-only
// See LICENSE and THIRD_PARTY_NOTICES.md for the separately licensed dependencies.
using Terraria.ModLoader;

namespace RelicExpansions.Core;

// Separate Mod owners are essential: disabling a module must remove its item,
// recipe and localization registrations without disabling its siblings.
public abstract class ContentModule : Mod
{
    public abstract string SourceId { get; }
    public override void Load() => Catalog.RegisterModule(this, SourceId);
    public override object Call(params object[] args) => args.Length == 2 && args[0] is "GetItemType" && args[1] is string id && id.StartsWith(SourceId + ":", System.StringComparison.Ordinal)
        ? Catalog.Type(id) : base.Call(args);
}
