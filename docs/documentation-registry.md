# 文档登记表（HIA-ASPNETPortal）

> **P21 D1 交付物**。按《HIA 项目文档化结构与模板说明》的 7 文档族登记本项目"适用 / 不适用"，并标注权威位置、维护责任、同步触发、验证方式。内部规划 / ADR / 研究草稿留 WorkZone，仅审阅后适合受众内容进公开 `docs/`。
> 原则四（文档化贯穿开发周期）落地载体：本表是文档工程的"总账"，后续 D2–D6 据此补齐缺口。

## 登记总览

| # | 文档族 | 适用性 | 权威位置 | 维护责任 | 同步触发 | 验证方式 |
|---|--------|--------|----------|----------|----------|----------|
| 1 | 代码内文档 | 适用 | 源码注释 + ROP 治理（C-anp-P19） | dev | 每 Cycle 代码变更 | ROP 门禁 `Get-PortalCommentDebtInventory` |
| 2 | API 与技术契约 | 适用（边界判定见下） | `docs/contracts/`（待建） | dev | 模块 / 接口 / 配置变更 | 契约一致性门禁 |
| 3 | 产品功能与使用 | 适用 | `docs/user-guide.md` + 待补 how-to/FAQ | docs | 功能变更 | 可读性走查 / 示例运行 |
| 4 | 开发者与贡献者 | 适用 | `docs/dev-guide.md` 等 | docs | 架构 / 工具变更 | 示例运行 |
| 5 | 运维部署安全 | 适用 | `docs/deployment-*`、`operations-runbook` 等 | docs | 部署 / 依赖变更 | 部署演练 |
| 6 | 架构设计决策 | 适用 | `docs/architecture.md` + WorkZone ADR | docs+WZ | 重大决策 | 评审 |
| 7 | 变更兼容发布 | 适用 | `docs/versioning.md` + 根 `CHANGELOG` | docs+root | 发版 | 版本一致性门禁 |

## 逐项说明与缺口

1. **代码内文档**：v1.0.0 重盘 474 文件扫描、35 有发现（29 高风险脚本候选 + 5 待办/延期标记 + 2 缺节点文档）。由 `C-anp-P19` 接续 `W-anp-P32`–`W-anp-P37` 收口。
2. **API 与技术契约（边界判定）**：本项目为 **Web Forms 门户（.NET FW4.8）**，**无对外 REST/GraphQL API** → "公开 REST/GraphQL API 参考"标记**不适用**（理由：架构无此类对外面）。但存在**模块契约 / 配置契约 / 接口契约**（模块 Profile、`appSettings` 契约、企业能力对象契约）→ 适用，建议建 `docs/contracts/` 收纳机器可读契约 + 人类可读说明。
3. **产品功能与使用**：`user-guide.md` 已存在但偏总览。缺口 = 逐功能 how-to / 教程 / FAQ / 故障排除 + 产品介绍与定位专页（README 兼任，建议 `docs/products/intro`）。`C-anp-P20` 新模块须在此族登记使用文档。
4. **开发者与贡献者**：`dev-guide` / `module-development-guide` / `frontend-asset-guide` / `theme-package-guide` 已较全。
5. **运维部署安全**：`deployment-checklist` / `deployment-default-credentials` / `deployment-rollback-guide` / `operations-runbook` / `audit-retention-policy` / `font-policy-and-audit` / `third-party-dependencies` 已较全。
6. **架构设计决策**：`architecture.md` + 两个状态机文档 + WorkZone ADR / milestones。
7. **变更兼容发布**：`versioning.md` + `CHANGELOG` + `release-notes-template.md`。

## 待建（本 Cycle D2–D6）

- 文档地图 / 导航（`docs/INDEX` 或 README 导航，串 7 族读者路径）
- 产品介绍与定位专页
- 逐功能 how-to / FAQ / 故障排除（与 `C-anp-P20` 新模块同步）
- API/集成契约文档（先按边界判定建 `docs/contracts/`）
- 文档验证门禁（链接检查 / 示例运行 / 双语一致 / 可读性走查）
