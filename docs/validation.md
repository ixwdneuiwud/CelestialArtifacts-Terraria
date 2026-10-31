# 公开快照验证（2026-10-31）

这份记录针对公开源码快照，使用 tModLoader 2026.8.3.0 / Terraria 1.4.4.9。四个包均成功编译，0 错误、0 警告。

| 场景 | 范围 | 检查数 | 结果 |
| --- | --- | --- | --- |
| legacy-only | isolated-runtime | 292 | 通过 |
| core-only | registration | 10 | 通过 |
| celestial | isolated-runtime | 441 | 通过 |
| calamity | isolated-runtime | 445 | 通过 |

每次检查都确认请求的模组实际加载；核心独立场景加载 EnigmaticLegacy、RelicExpansions、CelestialCore，未加载 CelestialArtifacts。灾厄场景额外加载 CalamityMod 及其强制依赖 CalamityModMusic。

运行检查包含物品、配方、前缀、效果钩子、死亡与存档、获取事件；星月检查另外包含 82 个核心变体与 107 个遗物变体的注册、装备、专用槽、刻蚀板、诅咒反转、药水袋存档和条件掉落。核心独立场景只做注册与配方边界检查。

构建与检查均使用项目内的可丢弃 profile，没有改动正常游戏的启用列表、角色或世界。初次独立注册诊断错误地要求灾厄，现已修复为仅在明确要求时强制验证；联合环境最初缺少灾厄音乐前置，补入后重新验证通过。历史包含旅途大陆的全量专服诊断发生过第三方 ResetEffects 异常，未将其报告为通过。

这轮没有重新进行完整客户端、美术、语音、双客户端联机或全 60 模组验收。此前私人整合版的结果不等于该公开快照完成了全部实机验收；不能将检查数量解读为每个物品的全部分支均经过实战测试。

公开包后续增加简介、图标等发布元数据，行为源码与该轮检查保持一致。构建与诊断入口均随源码提供；首次公开版本为 EnigmaticLegacy 0.1.19、RelicExpansions 0.2.2、CelestialCore / CelestialArtifacts 0.1.2。

最终发布元数据重新构建后，灾厄联合场景再次通过 445 项检查，所有请求模组均已加载；`validation-summary.json` 中该场景记录最终二进制的 SHA-256。其他场景记录元数据补入前的同一行为源码构建。
