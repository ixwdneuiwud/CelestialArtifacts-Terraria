# 星月遗物：Terraria 非官方移植

**Celestial Artifacts 1.6.0 + Celestial Core 1.6.1** → tModLoader 1.4.4。包括灾厄之册及七种刻蚀板。原作由 **Xiaoyue / Minecraft-Celestial 及贡献者**创作，按 **LGPL-2.1** 开放；本仓库由 ixwdneuiwud 维护，Terraria 移植开发、代码改写、资源适配及修复完全由 AI（GPT）完成，非官方版本。

[星月遗物原作](https://github.com/Minecraft-Celestial/CelestialArtifacts) · [星月核心原作](https://github.com/Minecraft-Celestial/CelestialCore) · [许可证](LICENSE) · [素材与依赖归属](THIRD_PARTY_NOTICES.md)

## 公开内容

本仓库包含三份真实构建源码，而非只有注册物品的空壳：

| 模组 | 公开快照版本 | 用途 |
| --- | --- | --- |
| `RelicExpansions` | 0.2.2 | 星月系列所需的共享运行逻辑、伤害适配、专用饰品槽、配方、事件、存档和网络 |
| `CelestialCore` | 0.1.2 | 82 个核心物品变体及素材，包括防具、工具、药水 |
| `CelestialArtifacts` | 0.1.2 | 107 个星月遗物变体及素材，包括灾厄之册、刻蚀板、饰品 |

另有共享的不死图腾。运行时依赖独立的 [神秘遗物移植](https://github.com/ixwdneuiwud/EnigmaticLegacy-Terraria) **0.1.19+**。本快照的共享核心只包含星月遗物和星月核心，没有把神秘遗物附加、月之石等尚无明确再分发许可的内容夹带公开；不要用它覆盖包含这些私人模块的旧整合包核心。

## 当前实现

灾厄之册的七种诅咒记录、对应刻蚀板反转、反转后的原文说明、未反转诅咒计入千咒卷轴；专用饰品类别与容量、原生通用饰品栏、原图槽位标识；饰品属性与状态、受伤/攻击/免死事件；樱花/海洋/虚金及大地武器工具、防具；药效、27 格药水袋、主动技能、掉落及原版宝箱奖励均在共享核心中实现。

近战/远程/魔法等对应 Terraria 原生类别；原作独立事件乘区与属性运算分开。维度按主世界/地狱高度区分。材料与获取渠道使用本移植确定的 Terraria 替代；附魔、饥饿、红石等指定机制暂停，部分结构战利品和指定物品继续占位。数值、获取与二维行为属于适配，不能视为原作完全相同。

许可清理带来一项显示差异：核心药水的瓶子改为运行时引用 Terraria 原生瓶装水图，不继续携带神秘遗物附加的药水贴图。其他已接入的星月资源沿用原作转换图。

## 从源码构建（Windows）

需要正版 Terraria、tModLoader 1.4.4、Python 3.10+、本地 Minecraft 1.20.1 客户端 JAR，以及从配套仓库构建的 `EnigmaticLegacy.tmod`。测试使用 tModLoader **2026.8.3.0**，不要求单独的 .NET SDK、Forge 或 MC 前置模组 JAR。

```powershell
python -m pip install -r tools/requirements.txt
.\build.ps1 -TModLoaderPath 'D:\SteamLibrary\steamapps\common\tModLoader' -MinecraftJar 'D:\Minecraft\versions\1.20.1\1.20.1.jar' -EnigmaticLegacyPackage '..\EnigmaticLegacy-Terraria\.artifacts\EnigmaticLegacy.tmod'
```

脚本依次构建共享核心、星月核心和星月遗物，产物在 `.artifacts/`；所有构建缓存和依赖副本在 `.build/PortBuildProfile`。脚本不更改正常游戏的模组列表、角色或世界。安装时三个 `.tmod` 与神秘遗物前置分别安装一次。

不死图腾保留 Minecraft 原图，由构建工具从本机客户端恢复，不纳入 Git；权利归 Mojang/Microsoft，原模组 LGPL 许可不重新授权这张图片。

## 源码与验证

- `ModSources/` 包含三个模组的完整对应源码、数据和可再分发素材。
- `build.ps1` 是依赖顺序明确的构建入口；`external-assets.json` 记录外部构建输入。
- `docs/validation.md` 记录当前公开快照的实际检查与局限。
- `ModSources/RelicExpansions/Diagnostics/` 可由神秘遗物的隔离诊断入口运行，涵盖物品、穿戴、配方、专槽、存档、诅咒与获取事件。

本仓库只交付上述三个公开模块，创意工坊实际发布状态见下表。修改与分发请保留 LGPL、原作者和修改声明，并随二进制提供完整对应源码与构建说明；依赖和第三方素材按各自许可处理。

<!-- workshop-publication:start -->
## 创意工坊发布记录

| 模组 | 版本 | 状态 | 订阅地址 | 必需前置 |
| --- | --- | --- | --- | --- |
| 遗物移植兼容核心 / `RelicExpansions` | 0.2.2 | 已上传并设为公开；等待 Steam 版务审核 | [创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3830633611) | EnigmaticLegacy |
| 星月核心 / `CelestialCore` | 0.1.2 | 已上传并设为公开；等待 Steam 版务审核 | [创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3830636996) | EnigmaticLegacy、RelicExpansions |
| 星月遗物 / `CelestialArtifacts` | 0.1.2 | 已上传并设为公开；等待 Steam 版务审核 | [创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3830640138) | EnigmaticLegacy、RelicExpansions、CelestialCore |

每个模组单独发布，星月系列的必需前置同时在 `.tmod` 和创意工坊条目关联。原作、许可依据、AI 移植声明、视频与 QQ 反馈群均在条目简介中。

Steam 账号的条目详情页明确提示本次提交正在等待版务人员批准；公开设置已经核验，但审核通过前其他玩家仍无法看到条目。本次尚未确认玩家端检索订阅可用。
<!-- workshop-publication:end -->
