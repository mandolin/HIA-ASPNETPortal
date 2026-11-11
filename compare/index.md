---
layout: home
title: 选型对比
parent: 产品概览
lang: zh-CN
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="oc-page">
<section class="oc-hero">
  <span class="oc-hero-tag">选型对比</span>
  <h1 class="oc-hero-title">直接采用，还是从零自建？</h1>
  <p class="oc-lead" style="max-width:46rem">对要建内网门户的团队，现实的选项通常是两条：采用本项目，或基于原始 Starter Kit / 空白项目自行开发。下表按事实对比。</p>
</section>

<section class="oc-section">
  <div class="oc-grid-2">
    <div class="oc-card">
      <h3 class="oc-card-title">采用本项目</h3>
      <p>权限与数据范围、流程状态机、审计、模块包与主题包治理、外置配置、自动化门禁均已内建并有文档与检查清单；源码开放（MIT），业务以模块包接入，不锁死在核心里。</p>
    </div>
    <div class="oc-card">
      <h3 class="oc-card-title">基于 Starter Kit 自行开发</h3>
      <p>原始 Starter Kit 只提供最基本的页面框架与样例模块：授权粒度、状态机、审计、治理与自动化检查都需要自行设计与实现，等于把这些设计决策留给了团队。</p>
    </div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">逐项对比</p>
  <h2 class="oc-h2">能力清单</h2>
  <table>
    <thead>
      <tr><th>能力</th><th>本项目</th><th>从 Starter Kit 自建</th></tr>
    </thead>
    <tbody>
      <tr><td>授权粒度</td><td>角色 × 实体授权 + 组织子树数据范围</td><td>需自行设计</td></tr>
      <tr><td>流程控制</td><td>显式迁移表，fail-closed，迁移必写事件</td><td>需自行设计</td></tr>
      <tr><td>审计</td><td>运营审计表 + 保留期策略 + 归档</td><td>需自行设计</td></tr>
      <tr><td>模块接入</td><td>受信任模块包：注册 / 启停 / 边界校验</td><td>直接改源码</td></tr>
      <tr><td>主题</td><td>7 套皮肤 + 语义变量矩阵 + 契约回退</td><td>手工样式</td></tr>
      <tr><td>配置</td><td>外置配置 + 契约核对</td><td>通常写在仓库里</td></tr>
      <tr><td>质量门禁</td><td>L0 / L1 / L2 分层自动化检查</td><td>视团队自觉</td></tr>
    </tbody>
  </table>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">什么时候不适合</p>
  <h2 class="oc-h2">诚实的边界</h2>
  <p class="oc-lead">需要对外 REST / GraphQL API、单页应用前端或脱离 Windows / IIS 运行环境的团队，本项目不是合适选择 —— 它是 Web Forms 门户，集成边界见<a href="{{ site.baseurl }}/product-overview/">产品概览</a>。另外，当前版本尚未提供生产环境验证证据，投产前请自行完成安全评估与运维演练。</p>
  <div class="oc-band">
    <p class="oc-eyebrow">下一步</p>
    <h2 class="oc-h2">看完边界，跑一遍再判断</h2>
    <p class="oc-cta-row"><a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/user-guide/">快速上手</a><a class="oc-btn oc-btn-ghost" href="{{ site.baseurl }}/product-overview/">产品概览</a></p>
  </div>
</section>
</div>
<!-- markdownlint-enable MD033 -->
