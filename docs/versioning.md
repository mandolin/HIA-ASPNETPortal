# 版本策略

本文件定义 HIA-ASPNETPortal 的**产品版本号机制**。版本号是发布锚点，也是"宣传推广 / 首发发行版里程碑"的判据载体。

## 一、版本号规则（SemVer）

采用 `MAJOR.MINOR.PATCH`，预发布后缀为 `-alpha.<n>` / `-beta.<n>` / `-rc.<n>`。

| 段 | 递增条件 |
| --- | --- |
| `MAJOR` | 对外兼容性承诺发生变化，或里程碑级别跃迁（如首发 `v1.0.0`、生产就绪 `v2.0.0`） |
| `MINOR` | 新增能力 / 完成一个 Cycle 的主题目标（如完成某能力层的初步完善） |
| `PATCH` | 缺陷修复、文档与元数据修正，不改变对外行为 |
| 预发布 | 发布候选：`-rc`；内部验证：`-alpha` / `-beta` |

## 二、版本与里程碑的关系

| 版本 | 里程碑含义 |
| --- | --- |
| `v0.1.0` | 机制闭环基线（能力↔模块↔Profile↔权限四层贯通并本机可验证） |
| `v0.2.0` – `v0.6.0` | 通往首发的演进版本（详见 `work-zone/dev/plans/C-anp-P10.md` 路线图） |
| **`v1.0.0`** | **首发发行版 = `M-ANP-RELEASE-READY-PORTAL`（L10）宣传推广里程碑** |
| `v2.0.0`（预留） | `M-ANP-TRUSTED-PORTAL`（L7）真实环境证据达成后的生产就绪版本 |

**判据不得因版本推进而放宽**：不得用"改判据"代替"补证据"。

## 三、版本与程序集版本联动

- 产品版本 `vX.Y.Z` 对应程序集版本 `X.Y.Z.0`。
- 程序集 `AssemblyVersion` / `AssemblyFileVersion` / `AssemblyInformationalVersion` 三者保持一致。
- 版本变更时必须同步：`CHANGELOG.md` 条目、`git tag`、程序集版本。
- 一致性由 `dev/scripts/Test-PortalVersionConsistency.ps1` 校验（见第五节）。

## 四、发布流程

1. 确认该版本的里程碑判据已满足（见对应 Cycle 文档）。
2. 更新 `CHANGELOG.md`：将 `Unreleased` 转为版本号条目并填日期。
3. 同步程序集版本（三个字段）。
4. 运行一致性门禁与构建门禁（构建 0 错 0 警、单测通过）。
5. 打标签：`git tag -a vX.Y.Z -m "..."`，推送 `git push origin vX.Y.Z`。
6. 产品版本标签**只在主仓**打；WorkZone 私有仓不打产品版本标签。

## 五、一致性门禁

`dev/scripts/Test-PortalVersionConsistency.ps1` 校验：

- `CHANGELOG.md` 最新条目版本 == 最新 `git tag` 版本；
- 程序集版本（5 个 `AssemblyInfo.cs`）与上述版本一致；
- 版本未变更时阻止重复打标签。

## 六、宣传口径约束

- `v1.0.0` 之前：**不对外宣传**。
- `v1.0.0` 起：口径限于"**可用 / 参考 / 研究基线**"。
- **不得宣称生产级 / 企业级可信**——`M-ANP-TRUSTED-PORTAL`（L7）真实环境证据仍为独立里程碑，未达成前禁用该表述。

## 七、当前版本

`v0.1.0`（机制闭环基线）。详见 `CHANGELOG.md`。

## 八、模块 manifest 的 `minimumPortalVersion` 语义

模块包 `module.json` 中的 `minimumPortalVersion` 表示**该模块要求的门户内核最低能力基线**，与产品版本号（`MAJOR.MINOR.PATCH`）是**两个独立维度**：

- 产品版本随发布演进（见第二节），可因任何变更递增；
- `minimumPortalVersion` 是模块对宿主门户的**兼容性声明**，仅在门户内核出现**不兼容变更**时才需提升。

**当前处置（2026-09-26）**：历史各 `module.json` 中填写的 `"1.0"` 属**早期占位值**，并不表示产品已发布 `v1.0.0`。为避免"要求 1.0、产品仅 `v0.1.0`"的歧义，约定：

1. 在首发（`v1.0.0`）之前，新模块统一填写与当前门户内核基线一致的值；
2. 门户内核的兼容性基线由本文件维护，模块填写值须能在此查到对应含义；
3. 校验行为（是否强制阻断加载）由模块加载逻辑定义，本文档只约定语义。
