---
layout: default
title: 首页
nav_order: 1
lang: zh-CN
---

<!-- markdownlint-disable MD033 -->
<p><a href="{{ site.baseurl }}/en/">English →</a></p>
<!-- markdownlint-enable MD033 -->

# HIA-ASPNETPortal

**企业内网 Web Forms 门户平台**。fork 自 Codeplex 上的 ASP.NET Portal Starter Kit，在其基础上补齐了**权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁**。

当前版本：**v1.0.0**（2026-10-09 首发发行版）。

## 这是什么

| 项 | 说明 |
| --- | --- |
| 形态 | ASP.NET **Web Forms** 门户（**.NET Framework 4.8**） |
| 运行环境 | Windows；IIS Express（本地）/ IIS（部署）；SQL Server 或 LocalDB |
| 许可 | **MIT** |
| 上游 | fork 自 ASP.NET Portal Starter Kit |

> **成熟度口径（重要）**：本版本达成 `M-ANP-RELEASE-READY-PORTAL`（**L10**）七条判据，
> 定位为**可用 / 参考 / 研究基线**；但**真实生产环境证据下的生产就绪（`M-ANP-TRUSTED-PORTAL` / L7）尚未达成**，
> 因此**不宣称生产级可信**。详见[版本策略](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/versioning.md)。

## 主要能力

- **身份与授权**：账户注册、角色权限实体授权、**按组织子树的数据范围控制**
- **流程状态机**：业务申请与协同事项的**显式迁移表**，写入前 fail-closed，迁移必写事件
- **审计**：运营审计表、保留期策略、日志维护与归档
- **模块包治理**：受信任模块包（`module.json`）的注册、启停、移除与边界校验
- **主题治理**：主题包契约校验与回退；7 套正式皮肤的 `--portal-*` 语义变量矩阵（14 个变量）
- **国际化**：中文 / 英文双语资源、强类型资源键、资源契约门禁
- **自动化门禁**：按 L0（静态）/ L1（构建后）/ L2（运行期）分层统一编排

## 从这里开始

> 全部 **31 个页面**已中英双语齐备并上架。按读者路径分为八个分区 —— 顺序沿用[文档地图](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-map.md)的读者路径。

**产品概览**

- [产品概览与定位]({{ site.baseurl }}/overview/) —— 它是什么、给谁用、边界在哪
- [文档索引]({{ site.baseurl }}/docs-index/) —— 全部公开维护文档的一句话说明

**评估**

- [架构概览]({{ site.baseurl }}/architecture/) —— 系统结构、数据访问与四条当前架构风险

**快速上手**

- [用户指南]({{ site.baseurl }}/user-guide/) —— 本地运行、外置配置、初始账号与常见模块

**学习**

- [P12 员工资料更正样板业务路径]({{ site.baseurl }}/p12-sample-business-flow/) —— 示例业务流程、样例数据 SQL 与验收入口

**参考**

- [测试清单]({{ site.baseurl }}/testing-checklist/) —— 七层自动化回归分层与发布前检查
- [技术契约（入口）]({{ site.baseurl }}/contracts-readme/) —— 边界判定：无对外 REST/GraphQL，适用模块/配置/企业能力对象契约
- [模块契约（`module.json`）]({{ site.baseurl }}/contracts-module-manifest/) —— 字段、`schemaVersion` 1/2、可选 `capability` 与被显式拒绝的声明
- [配置契约（`appSettings`）]({{ site.baseurl }}/contracts-app-settings/) —— 两层生效机制、键族、档位与包放行的区别
- [主题语义变量矩阵]({{ site.baseurl }}/theme-semantic-variables/) —— 7 套主题 × 14 个变量
- [模块外观契约]({{ site.baseurl }}/theme-module-contract/) —— 主题层与模块层的分工、回退值真实含义
- [第三方依赖审计]({{ site.baseurl }}/third-party-dependencies/) —— 用途、许可证与分发边界
- [字体政策与审计]({{ site.baseurl }}/font-policy-and-audit/) —— 使用原则与许可证边界
- [业务申请状态机]({{ site.baseurl }}/business-application-state-machine/) —— 状态词汇、迁移表与 fail-closed 约束
- [协同事项状态机]({{ site.baseurl }}/collaboration-item-state-machine/) —— 状态词汇、迁移表与处理角色语义
- [审计保留期策略]({{ site.baseurl }}/audit-retention-policy/) —— 运营审计表的保留期与处置口径
- [文档登记表]({{ site.baseurl }}/documentation-registry/) —— 7 个文档族的适用性登记
- [文档复核走查清单]({{ site.baseurl }}/documentation-review-checklist/) —— 自动化门禁无法证明的部分
- [文档地图]({{ site.baseurl }}/documentation-map/) —— 7 族文档的导航地图与读者路径
- [文档产物与工具链边界]({{ site.baseurl }}/documentation-artifacts-guide/) —— 公开文档、文档化输入与可再生产物的边界
- [官网视觉基线]({{ site.baseurl }}/site-visual-baseline/) —— 配色、字体与排版取值的依据

**开发**

- [部署式模块包开发指南]({{ site.baseurl }}/module-development/) —— 模块包的开发、注册、启停与隐藏陷阱
- [前端资源指南]({{ site.baseurl }}/frontend-asset-guide/) —— Web Forms 呈现、主题、模块 CSS 与资产边界
- [主题包部署指南]({{ site.baseurl }}/theme-package-guide/) —— 主题包目录、清单、选择与发布回退
- [开发指南]({{ site.baseurl }}/dev-guide/) —— 环境、构建、调试、测试与数据库初始化

**运维**

- [发布与兼容检查清单]({{ site.baseurl }}/deployment-checklist/) —— 发布前边界、IIS、数据库与发布后回归
- [运维运行手册]({{ site.baseurl }}/operations-runbook/) —— 例行检查、日志维护、审计查询与证据包
- [默认凭据与旧口令治理]({{ site.baseurl }}/deployment-default-credentials/) —— 默认账号与旧 MD5 兼容路径
- [发布回滚指南]({{ site.baseurl }}/deployment-rollback-guide/) —— 文件、配置、数据库与证据回滚准备

**变更**

- [版本策略]({{ site.baseurl }}/versioning/) —— 版本号规则、里程碑关系与宣传口径约束
- [发布说明模板]({{ site.baseurl }}/release-notes-template/) —— 版本、变更、验证摘要与回滚入口

> 本站点为**中英双语并行**：每个分区下均有对应的英文页面（路径加 `/en/` 前缀）。
> 中文位于根、英文位于 `/en/`，两棵一一对应；页面只有中英两版齐备才允许上架（由 `Build-PortalSite.ps1` 强制）。

---

<!-- markdownlint-disable MD033 -->
<p>本站点内容由 <code>dev/scripts/Build-PortalSite.ps1</code> 从仓库 <code>docs/</code> 生成 —— <strong>不另起一套脱离代码的文档</strong>（原则4）。</p>
<!-- markdown-enable MD033 -->
