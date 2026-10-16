---
layout: default
title: 首页
parent: 产品概览
nav_order: 1
lang: zh-CN
---

<!-- markdownlint-disable MD033 -->
<p><a href="{{ site.baseurl }}/en/">English →</a></p>
<!-- markdownlint-enable MD033 -->

# HIA-ASPNETPortal

**企业内网 Web Forms 门户平台。** fork 自 ASP.NET Portal Starter Kit，在其基础上补齐了**权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁**。

<p>
  <a href="{{ site.baseurl }}/user-guide/" class="btn btn-primary">快速上手</a>
  <a href="{{ site.baseurl }}/docs-index/" class="btn btn-outline">全部文档</a>
</p>

| | |
| --- | --- |
| **当前版本** | v1.0.0（2026-10-09 首发） |
| **技术栈** | ASP.NET Web Forms · .NET Framework 4.8 |
| **运行环境** | Windows · IIS / IIS Express · SQL Server 或 LocalDB |
| **许可** | MIT |

> **成熟度口径**
> 本版本达成 `M-ANP-RELEASE-READY-PORTAL`（L10）七条判据，定位为**可用 / 参考 / 研究基线**。
> 真实生产环境证据下的生产就绪（`M-ANP-TRUSTED-PORTAL` / L7）**尚未达成**，因此**不宣称生产级可信**。
> 详见[版本策略]({{ site.baseurl }}/versioning/)。

## 这是什么

| 项 | 说明 |
| --- | --- |
| 形态 | ASP.NET **Web Forms** 门户（**.NET Framework 4.8**） |
| 运行环境 | Windows；IIS Express（本地）/ IIS（部署）；SQL Server 或 LocalDB |
| 许可 | **MIT** |
| 上游 | fork 自 ASP.NET Portal Starter Kit |

## 主要能力

- **身份与授权**：账户注册、角色权限实体授权、按组织子树的数据范围控制
- **流程状态机**：业务申请与协同事项的显式迁移表，写入前 fail-closed，迁移必写事件
- **审计**：运营审计表、保留期策略、日志维护与归档
- **模块包治理**：受信任模块包的注册、启停、移除与边界校验
- **主题治理**：主题包契约校验与回退；7 套正式皮肤的语义变量矩阵
- **国际化**：中文 / 英文资源、强类型资源键
- **自动化门禁**：按 L0（静态）/ L1（构建后）/ L2（运行期）分层统一编排

## 按分区浏览

| 分区 | 文档 |
| --- | --- |
| **产品概览** | [产品概览与定位]({{ site.baseurl }}/overview/) · [文档索引]({{ site.baseurl }}/docs-index/) |
| **评估** | [架构概览]({{ site.baseurl }}/architecture/) |
| **快速上手** | [用户指南]({{ site.baseurl }}/user-guide/) |
| **学习** | [P12 员工资料更正样板业务路径]({{ site.baseurl }}/p12-sample-business-flow/) |
| **参考** | [测试清单]({{ site.baseurl }}/testing-checklist/) · [主题语义变量矩阵]({{ site.baseurl }}/theme-semantic-variables/) · [模块外观契约]({{ site.baseurl }}/theme-module-contract/) · [第三方依赖审计]({{ site.baseurl }}/third-party-dependencies/) · [字体政策与审计]({{ site.baseurl }}/font-policy-and-audit/) · [业务申请状态机]({{ site.baseurl }}/business-application-state-machine/) · [协同事项状态机]({{ site.baseurl }}/collaboration-item-state-machine/) · [审计保留期策略]({{ site.baseurl }}/audit-retention-policy/) · [文档登记表]({{ site.baseurl }}/documentation-registry/) · [文档复核走查清单]({{ site.baseurl }}/documentation-review-checklist/) · [文档地图]({{ site.baseurl }}/documentation-map/) · [文档产物与工具链边界]({{ site.baseurl }}/documentation-artifacts-guide/) · [技术契约]({{ site.baseurl }}/contracts-readme/) · [模块契约]({{ site.baseurl }}/contracts-module-manifest/) · [配置契约]({{ site.baseurl }}/contracts-app-settings/) · [官网视觉基线]({{ site.baseurl }}/site-visual-baseline/) |
| **开发** | [模块包开发指南]({{ site.baseurl }}/module-development/) · [前端资源指南]({{ site.baseurl }}/frontend-asset-guide/) · [主题包部署指南]({{ site.baseurl }}/theme-package-guide/) · [开发指南]({{ site.baseurl }}/dev-guide/) |
| **运维** | [发布与兼容检查清单]({{ site.baseurl }}/deployment-checklist/) · [运维运行手册]({{ site.baseurl }}/operations-runbook/) · [默认凭据与旧口令治理]({{ site.baseurl }}/deployment-default-credentials/) · [发布回滚指南]({{ site.baseurl }}/deployment-rollback-guide/) |
| **变更** | [版本策略]({{ site.baseurl }}/versioning/) · [发布说明模板]({{ site.baseurl }}/release-notes-template/) |

## 主要能力明细

各能力对应的文档入口见上方「参考 / 开发 / 运维」分区；[文档地图]({{ site.baseurl }}/documentation-map/)提供按角色（使用者 / 开发者 / 审计）的阅读路径。
