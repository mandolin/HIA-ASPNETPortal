---
layout: default
title: 首页
parent: 产品概览
nav_order: 1
lang: zh-CN
toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="hia-hero">
  <p class="hia-hero-eyebrow">HIA 系列 · ASP.NET Web Forms 门户</p>
  <h1 class="hia-hero-title">HIA-ASPNETPortal 文档站</h1>
  <p class="hia-hero-sub">企业内网 Web Forms 门户平台：fork 自 ASP.NET Portal Starter Kit，并补齐了权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁。</p>
  <div class="hia-hero-cta">
    <a class="hia-btn hia-btn-primary" href="{{ site.baseurl }}/sections/quick-start/">快速上手</a>
    <a class="hia-btn hia-btn-ghost" href="{{ site.baseurl }}/sections/product-overview/">了解产品</a>
  </div>
</div>

<table>
  <tbody>
    <tr><td><strong>当前版本</strong></td><td>v1.0.0（2026-10-09 首发）</td></tr>
    <tr><td><strong>技术栈</strong></td><td>ASP.NET Web Forms · .NET Framework 4.8</td></tr>
    <tr><td><strong>运行环境</strong></td><td>Windows · IIS / IIS Express · SQL Server 或 LocalDB</td></tr>
    <tr><td><strong>许可</strong></td><td>MIT</td></tr>
  </tbody>
</table>

<blockquote>
  <p><strong>适用场景</strong>　当前版本适合<strong>开发、测试、演示与参考</strong>场景；已具备权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁等完整能力。在投入生产环境前，请自行完成安全评估、备份与运维演练 —— 本项目<strong>未提供生产环境验证证据</strong>。详见<a href="{{ site.baseurl }}/versioning/">版本策略</a>。</p>
</blockquote>

<h2 id="按分区浏览">按分区浏览</h2>

<div class="hia-cards">
  <a class="hia-card" href="{{ site.baseurl }}/sections/product-overview/">
    <p class="hia-card-name">产品概览</p>
    <p class="hia-card-desc">产品定位、适用场景与全部文档索引</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/evaluate/">
    <p class="hia-card-name">评估</p>
    <p class="hia-card-desc">系统结构、数据访问与当前架构风险</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/quick-start/">
    <p class="hia-card-name">快速上手</p>
    <p class="hia-card-desc">本地运行、外置配置与常见模块</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/learn/">
    <p class="hia-card-name">学习</p>
    <p class="hia-card-desc">完整业务路径，理解各能力如何协同</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/reference/">
    <p class="hia-card-name">参考</p>
    <p class="hia-card-desc">测试分层、主题契约、状态机、合规策略与文档体系</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/develop/">
    <p class="hia-card-name">开发</p>
    <p class="hia-card-desc">模块与主题开发、前端资产、开发环境</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/operate/">
    <p class="hia-card-name">运维</p>
    <p class="hia-card-desc">部署、运维、默认凭据治理与回滚</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/sections/changes/">
    <p class="hia-card-name">变更</p>
    <p class="hia-card-desc">版本规则与发布说明模板</p>
  </a>
</div>

<h2 id="主要能力">主要能力</h2>

<ul>
  <li><strong>身份与授权</strong>：账户注册、角色权限实体授权、按组织子树的数据范围控制</li>
  <li><strong>流程状态机</strong>：业务申请与协同事项的显式迁移表，写入前 fail-closed，迁移必写事件</li>
  <li><strong>审计</strong>：运营审计表、保留期策略、日志维护与归档</li>
  <li><strong>模块包治理</strong>：受信任模块包的注册、启停、移除与边界校验</li>
  <li><strong>主题治理</strong>：主题包契约校验与回退；7 套正式皮肤的语义变量矩阵</li>
  <li><strong>国际化</strong>：中文 / 英文资源、强类型资源键</li>
  <li><strong>自动化门禁</strong>：按 L0（静态）/ L1（构建后）/ L2（运行期）分层统一编排</li>
</ul>

<p>各能力对应的文档入口见上方分区；<a href="{{ site.baseurl }}/documentation-map/">文档地图</a>提供按角色（使用者 / 开发者 / 审计）的阅读路径。</p>
<!-- markdownlint-enable MD033 -->
