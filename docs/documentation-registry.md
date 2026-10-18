# 文档登记表（HIA-ASPNETPortal）

> 按《HIA 项目文档化结构与模板说明》的 7 文档族登记本项目"适用 / 不适用"，并标注权威位置、维护责任、同步触发、验证方式。内部规划 / ADR / 研究草稿留**独立私有仓库**，仅审阅后适合受众内容进公开 `docs/`。
> 原则四（文档化贯穿开发周期）落地载体：本表是文档工程的"总账"，后续 `W-anp-P124`–`P127` 据此补齐缺口（2026-10-14 起本 Cycle 改用 W 编号，原 D1–D7 仅作历史映射）。

## 登记总览

| # | 文档族 | 适用性 | 权威位置 | 维护责任 | 同步触发 | 验证方式 |
|---|--------|--------|----------|----------|----------|----------|
| 1 | 代码内文档 | 适用 | 源码注释 + ROP 治理 | dev | 每 Cycle 代码变更 | ROP 门禁 `Get-PortalCommentDebtInventory` |
| 2 | API 与技术契约 | 适用（边界判定见下） | `docs/contracts/`（待建） | dev | 模块 / 接口 / 配置变更 | 契约一致性门禁 |
| 3 | 产品功能与使用 | 适用 | `docs/user-guide.md` + 待补 how-to/FAQ | docs | 功能变更 | 可读性走查 / 示例运行 |
| 4 | 开发者与贡献者 | 适用 | `docs/dev-guide.md` 等 | docs | 架构 / 工具变更 | 示例运行 |
| 5 | 运维部署安全 | 适用 | `docs/deployment-*`、`operations-runbook` 等 | docs | 部署 / 依赖变更 | 部署演练 |
| 6 | 架构设计决策 | 适用 | `docs/architecture.md` + 内部 ADR | docs+内部 | 重大决策 | 评审 |
| 7 | 变更兼容发布 | 适用 | `docs/versioning.md` + 根 `CHANGELOG` | docs+root | 发版 | 版本一致性门禁 |

## 逐项说明与缺口

1. **代码内文档**：`C-anp-P19`（`W-anp-P108`–`P112`）**已收口**。2026-10-14 实测基线 —— **源码层**：486 文件、33 有发现（`HighRiskScriptCandidate` 29、`TodoOrDeferredMarker` **4**；`MissingNodeDocumentation` 与乱码 / 旧双语 / 客户端 HTML 注释 / 低价值复述五类已清零）；**PowerShell 层**：87 脚本、comment-based help **87/87**、标准 `<lang>` 块 1689 个、legacy `.LANG` **0**、混合语言表面 **0**、高风险脚本 12 个且缺 HIA 标记 **0**，已由 `Test-PortalRopCommentBaseline.ps1`（L0）门禁化。
   - ⚠️ 本行原记"474 文件 / 35 有发现 / 5 待办 / 2 缺节点文档"是 **v1.0.0 时期登记**，现据实测刷新。
   - `HighRiskScriptCandidate`（29）是按**文件名关键词**自动生成的候选集，**不是缺陷计数**；它与 PowerShell 层的 `RiskLevel=High`（12）是两套不同集合。
   - **语义边界**：结构标记齐备 ≠ 注释语义充分。语义质量由 `C-anp-P25`（`W-anp-P113` 起）人工抽样承接，首批（安全路径）已完成。
2. **API 与技术契约（边界判定）**：本项目为 **Web Forms 门户（.NET FW4.8）**，**无对外 REST/GraphQL API** → "公开 REST/GraphQL API 参考"标记**不适用**（理由：架构无此类对外面）。但存在**模块契约 / 配置契约 / 企业能力对象契约** → 适用。**`W-anp-P125` 已建 `docs/contracts/`**：`README.md`（边界判定表 + 权威源）、`module-manifest.md`（`module.json` 契约）、`app-settings.md`（`appSettings` 契约），收纳机器可读契约 + 人类可读说明。
   - **据实订正**（据 `PortalModulePackage.cs` 实测）：`schemaVersion: 2` **不要求**必须带 `capability` 段 —— `capability` 是**可选**的，未声明时模块为 Unclassified（合法）；`module-development-guide.md` 中"加载器要求 capability 块"的表述**不准确**，已登记复核修正。
3. **产品功能与使用**：`W-anp-P124` 已补强 —— 新增 **`product-overview.md`**（产品概览与定位：是什么 / 给谁用 / 能力地图 / 边界 / 兼容性 / 许可证 / 适用场景）；`user-guide.md` 补入**典型门户配置流程**、**管理员常用操作**、**故障排除**三节，并把原「待补充」收敛为据实的「尚未覆盖」清单。**`C-anp-P20` 新模块已在此族登记**：`user-guide.md` 含「业务模块：请假申请与费用报销」节（启用前提、字段与校验、待办生成、常见问题）。
   - **剩余缺口**（如实登记）：请假与报销的审批流转界面（本期仅落库与生成待办，审批操作不在本模块内）；`HTML` / `QuickLinks` / `XML` 三个模块的逐项操作说明。（~~运行截图~~ 已由 `W-anp-P151` 交付：固定视口可复现采集 + 证据清单入库；~~既有内容模块逐项操作说明~~ 已由 `W-anp-P152` 交付，见 `user-guide.md`「常见模块」各子节）
4. **开发者与贡献者**：`dev-guide` / `module-development-guide` / `frontend-asset-guide` / `theme-package-guide` / `theme-semantic-variables` / `theme-module-contract` 已较全。其中 `theme-semantic-variables.md` 为 `C-anp-P18` / `W-anp-P97` 产出，登记主题层 `--portal-*` 语义变量矩阵（7 套主题 × **14 个变量** —— 第 14 个 `--portal-text-subtle` 由 `C-anp-P23` D3 裁定新增，用于副标题与标题的色层次）；`theme-module-contract.md` 为 `W-anp-P99` 产出，登记模块外观契约与 `var()` 回退值审计结论 —— 原记的**待裁定 D3**（副标题与标题颜色层次在变量迁移后被抹平）已由 `C-anp-P23` **裁定为（b）新增 `--portal-text-subtle` 并落地**，不再是未决项；该周期同时把主题层模块语义类覆盖补齐至 8 模块 × 7 皮肤 100%，并将覆盖缺口升为门禁硬失败。两者权威来源均为 CSS 本身与 `W-anp-P100` 门禁，文档为提取快照，过期时以门禁为准。
5. **运维部署安全**：`deployment-checklist` / `deployment-default-credentials` / `deployment-rollback-guide` / `operations-runbook` / `audit-retention-policy` / `font-policy-and-audit` / `third-party-dependencies` 已较全。
6. **架构设计决策**：`architecture.md` + 两个状态机文档 + 内部 ADR / milestones。
7. **变更兼容发布**：`versioning.md` + `CHANGELOG` + `release-notes-template.md`。

## 待建（`W-anp-P124`–`P127`；原 D3–D6，2026-10-14 改 W 编号）

- ✅ ~~文档地图 / 导航~~ —— **已交付**：`docs/documentation-map.md`（读者路径 + 7 族索引）与 `docs/README.md`（入口导航）。
- 产品介绍与定位专页 —— `W-anp-P124`
- 逐功能 how-to / FAQ / 故障排除 —— `W-anp-P124`（`C-anp-P20` 两个新模块**已同步**入 `user-guide.md`；其余既有模块的逐功能 how-to 仍缺）
- API/集成契约文档（按边界判定建 `docs/contracts/`）—— `W-anp-P125`
- 既有文档一致性 / 版本 / 链接复核 —— `W-anp-P126`
- 文档验证门禁（链接检查 / 示例运行 / 双语一致 / 可读性走查）—— `W-anp-P127`
