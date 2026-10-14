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

- [产品概览与定位](/overview/) —— 它是什么、给谁用、边界在哪
- [快速上手](/quickstart/) —— 本地跑起来
- [开发者文档](/develop/) —— 开发模块与主题
- [运维文档](/operate/) —— 部署与运维
- [变更记录](/changes/) —— 发布说明

---

<!-- markdownlint-disable MD033 -->
<p>本站点内容由 <code>dev/scripts/Build-PortalSite.ps1</code> 从仓库 <code>docs/</code> 生成 —— <strong>不另起一套脱离代码的文档</strong>（原则4）。</p>
<!-- markdown-enable MD033 -->
