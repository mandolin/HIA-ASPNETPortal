---
layout: default
lang: zh-CN
parent: 参考
permalink: /documentation-map/
title: 文档地图 / 导航索引（HIA-ASPNETPortal）
---
# 文档地图 / 导航索引（HIA-ASPNETPortal）

> **P21 D2 交付物**。按《HIA 项目文档化结构与模板说明》7 文档族对 `docs/` 现有文档做导航索引，并标注缺口。与 `documentation-registry.md`（D1，登记适用/不适用）配套：registry 记"该不该有"，本图记"在哪、怎么串"。

## 读者路径
- **用户/实施方**：产品功能与使用 → 运维部署安全 → 变更兼容发布
- **开发者/贡献者**：架构设计决策 → 开发者与贡献者 → API 与技术契约 → 代码内文档（源码）
- **审计/安全**：运维部署安全 → 架构设计决策 → 变更兼容发布

## 文档族索引

### 1. 代码内文档（→ C-anp-P19 ROP，源码注释）
不在 `docs/`，位于源码节点/流程注释；由 ROP 门禁 `Get-PortalCommentDebtInventory` 保障。

### 2. API 与技术契约（`W-anp-P125` 已建 `docs/contracts/`）
- 边界判定：Web Forms 门户**无对外 REST/GraphQL API**；适用"模块契约 / 配置契约 / 企业能力对象契约"。
- `contracts/README.md` — 契约总览、边界判定表（REST/GraphQL/OpenAPI **不适用**）、权威源指向与验证方式
- `contracts/module-manifest.md` — 模块契约（`module.json` 字段、`schemaVersion` 1/2、可选 `capability`、被显式拒绝的声明）
- `contracts/app-settings.md` — 配置契约（`appSettings` 键族、两层生效机制、档位与包放行的区别）

### 3. 产品功能与使用
- `product-overview.md` — **产品概览与定位**（`W-anp-P124` 交付）：是什么 / 给谁用 / 能力地图 / 边界 / 兼容性与运行环境 / 许可证与归属 / 成熟度口径
- `user-guide.md` — 运行与使用入口；含**典型门户配置流程**、**管理员常用操作**、**故障排除**（`W-anp-P124` 补强）
- `p12-sample-business-flow.md` — 示例业务流程
- **剩余缺口**：运行截图；既有内容模块（公告、联系人、讨论、文档、事件、图片、链接等）的逐项操作说明；请假与报销的审批流转界面（本期仅落库与生成待办，审批不在本模块内）

### 4. 开发者与贡献者
- `dev-guide.md` — 开发总指南
- `module-development-guide.md` — 模块开发指南
- `frontend-asset-guide.md` — 前端资源指南
- `theme-package-guide.md` — 主题包指南（衔接 C-anp-P18）
- `theme-semantic-variables.md` — 主题语义变量矩阵（7 主题 × **14 变量**、用途速查、使用规则；第 14 个 `--portal-text-subtle` 由 `C-anp-P23` D3 裁定新增）
- `theme-module-contract.md` — 模块外观契约（层间分工、外观类族、回退值含义与差异）
- `documentation-artifacts-guide.md` — 文档产物指南
- `testing-checklist.md` — 测试清单

### 5. 运维部署安全
- `deployment-checklist.md` — 部署清单
- `deployment-default-credentials.md` — 默认凭据处置
- `deployment-rollback-guide.md` — 回滚指南
- `operations-runbook.md` — 运维手册
- `audit-retention-policy.md` — 审计留存策略
- `font-policy-and-audit.md` — 字体策略与审计
- `third-party-dependencies.md` — 第三方依赖

### 6. 架构设计决策
- `architecture.md` — 总体架构
- `business-application-state-machine.md` — 业务应用状态机
- `collaboration-item-state-machine.md` — 协作项状态机
- （重大决策 ADR / milestones 见**独立私有仓库**，不随公开文档发布）

### 7. 变更兼容发布
- `versioning.md` — 版本与兼容策略
- `release-notes-template.md` — 发布说明模板
- `CHANGELOG.md`（仓库根）— 变更日志

## 元文档
- `documentation-registry.md` — D1 登记表（适用/不适用 + 责任/触发/验证）
- `documentation-map.md` — 本图（导航）
- `site-visual-baseline.md` — **官网视觉基线**（配色 / 字体 / 排版层级 / 代码块与表格取色 / 导航骨架）
- `documentation-review-checklist.md` — **人工走查清单**（自动化门禁无法证明的部分：正确性 / 双语对等 / 读者可达性 / 不误信门禁）
- `README.md` — 仓库入口（含文档分区导航指向本图）

## 对外发布形态（官网）

`docs/` 是**中文权威源**；`site/_translations/` 是**英文译本**。`dev/scripts/Build-PortalSite.ps1` 从两者生成双语站点并强制「中英两版齐备方可声明上架」，再发布到 `gh-pages` 分支。
因此本图所列文档**只有在译本齐备后才出现在官网**；未上架文档在站点上不存在对应页面，这是预期行为而非缺失。
