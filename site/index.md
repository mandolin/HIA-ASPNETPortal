---
layout: home
title: 首页
parent: 产品概览
nav_order: 1
lang: zh-CN
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="hia-home-hero">
  <p class="hia-home-eyebrow">HIA 系列 · ASP.NET Web Forms 门户</p>
  <h1 class="hia-home-title">企业内网 Web Forms 门户平台</h1>
  <p class="hia-home-sub">fork 自 ASP.NET Portal Starter Kit，在其基础上补齐了权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁。</p>
  <p class="hia-home-cta">
    <a class="hia-btn hia-btn-primary" href="{{ site.baseurl }}/user-guide/">快速上手</a>
    <a class="hia-btn hia-btn-ghost" href="{{ site.baseurl }}/product-overview/">了解产品</a>
  </p>
</div>

**这是什么？** 一套面向企业内网的门户底座：账户注册与角色授权、按组织子树的数据范围、显式迁移表的流程状态机、运营审计、受信任的模块包与主题包治理，以及分层自动化门禁（L0 静态 / L1 构建后 / L2 运行期）。技术栈是 ASP.NET **Web Forms** 与 **.NET Framework 4.8**，运行于 Windows 的 IIS / IIS Express，数据在 SQL Server 或 LocalDB，MIT 许可。

> **适用场景**　当前版本适合**开发、测试、演示与参考**；已具备上述完整能力。在投入生产环境前，请自行完成安全评估、备份与运维演练 —— 本项目**未提供生产环境验证证据**。详见[版本策略]({{ site.baseurl }}/versioning/)。

## 文档

<ul class="hia-doclist">
  <li><a href="{{ site.baseurl }}/sections/product-overview/"><strong>产品概览</strong></a> — 定位、适用场景与全部文档索引</li>
  <li><a href="{{ site.baseurl }}/sections/evaluate/"><strong>评估</strong></a> — 系统结构、数据访问与当前架构风险</li>
  <li><a href="{{ site.baseurl }}/sections/quick-start/"><strong>快速上手</strong></a> — 本地运行、外置配置与常见模块</li>
  <li><a href="{{ site.baseurl }}/sections/learn/"><strong>学习</strong></a> — 完整业务路径，理解各能力如何协同</li>
  <li><a href="{{ site.baseurl }}/sections/reference/"><strong>参考</strong></a> — 测试分层、主题契约、状态机、合规策略与文档体系</li>
  <li><a href="{{ site.baseurl }}/sections/develop/"><strong>开发</strong></a> — 模块与主题开发、前端资产、开发环境</li>
  <li><a href="{{ site.baseurl }}/sections/operate/"><strong>运维</strong></a> — 部署、运维、默认凭据治理与回滚</li>
  <li><a href="{{ site.baseurl }}/sections/changes/"><strong>变更</strong></a> — 版本规则与发布说明模板</li>
</ul>

按角色（使用者 / 开发者 / 审计）的阅读路径见[文档地图]({{ site.baseurl }}/documentation-map/)。

## 主要能力

- **身份与授权**：账户注册、角色权限实体授权、按组织子树的数据范围控制
- **流程状态机**：业务申请与协同事项的显式迁移表，写入前 fail-closed，迁移必写事件
- **审计**：运营审计表、保留期策略、日志维护与归档
- **模块包治理**：受信任模块包的注册、启停、移除与边界校验
- **主题治理**：主题包契约校验与回退；7 套正式皮肤的语义变量矩阵
- **国际化**：中文 / 英文资源、强类型资源键
- **自动化门禁**：按 L0（静态）/ L1（构建后）/ L2（运行期）分层统一编排
<!-- markdownlint-enable MD033 -->
