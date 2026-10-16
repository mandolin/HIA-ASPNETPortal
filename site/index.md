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

> **适用场景**
> 当前版本适合**开发、测试、演示与参考**场景；已具备权限与数据范围、流程状态机、审计、模块包治理、
> 主题治理与自动化门禁等完整能力。
> 在投入生产环境前，请自行完成安全评估、备份与运维演练 —— 本项目**未提供生产环境验证证据**。
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

| 分区 | 内容 |
| --- | --- |
| **[产品概览]({{ site.baseurl }}/sections/product-overview/)** | 产品定位、适用场景与全部文档索引 |
| **[评估]({{ site.baseurl }}/sections/evaluate/)** | 系统结构、数据访问与当前架构风险 |
| **[快速上手]({{ site.baseurl }}/sections/quick-start/)** | 本地运行、外置配置与常见模块 |
| **[学习]({{ site.baseurl }}/sections/learn/)** | 完整业务路径，理解各能力如何协同 |
| **[参考]({{ site.baseurl }}/sections/reference/)** | 测试分层、主题契约、状态机、合规策略与文档体系 |
| **[开发]({{ site.baseurl }}/sections/develop/)** | 模块与主题开发、前端资产、开发环境 |
| **[运维]({{ site.baseurl }}/sections/operate/)** | 部署、运维、默认凭据治理与回滚 |
| **[变更]({{ site.baseurl }}/sections/changes/)** | 版本规则与发布说明模板 |

## 主要能力明细

各能力对应的文档入口见上方「参考 / 开发 / 运维」分区；[文档地图]({{ site.baseurl }}/documentation-map/)提供按角色（使用者 / 开发者 / 审计）的阅读路径。
