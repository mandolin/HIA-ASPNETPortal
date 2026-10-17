---
layout: home
title: 产品总览
parent: 产品概览
lang: zh-CN
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="oc-page">
<section class="oc-hero">
  <span class="oc-hero-tag">产品总览</span>
  <h1 class="oc-hero-title">一个门户平台，两种使用方式</h1>
  <p class="oc-hero-lead">HIA-ASPNETPortal 是面向企业内网的 Web Forms 门户平台：既能作为<strong>现成的门户底座</strong>直接启用，也能作为<strong>参考基线</strong>，借鉴其中的状态机、审计与治理设计。</p>
  <p class="oc-cta-row">
    <a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/user-guide/">快速上手</a>
    <a class="oc-btn oc-btn-ghost" href="{{ site.baseurl }}/product-overview/">阅读详细定位</a>
  </p>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">方式一</p>
  <h2 class="oc-h2">作为现成的门户底座</h2>
  <p class="oc-lead">部署后即得到一个带登录、组织与授权、流程与审计的内网站点，再按需开关业务模块（员工资料、用电申请、费用报销等样例模块随源码提供）。</p>
  <div class="oc-grid">
    <div class="oc-card"><h3 class="oc-card-title">统一身份与授权</h3><p>账户注册、角色与实体授权、组织子树数据范围。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">流程与审计</h3><p>显式迁移表的状态机与可追溯的运营审计。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">模块化扩展</h3><p>受信任模块包注册与边界校验，新业务以模块接入。</p></div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">方式二</p>
  <h2 class="oc-h2">作为参考基线</h2>
  <p class="oc-lead">如果暂不整体采用，状态机迁移表、审计保留期、模块包与主题包契约、外置配置等设计都可以单独参考——文档把这些设计写成了可对照的契约与清单。</p>
  <div class="oc-grid">
    <a class="oc-card" href="{{ site.baseurl }}/business-application-state-machine/"><h3 class="oc-card-title">状态机设计</h3><p>业务申请与协同事项的显式迁移表。</p><span class="oc-card-more">查看 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/audit-retention-policy/"><h3 class="oc-card-title">审计与保留期</h3><p>运营审计表与保留期策略。</p><span class="oc-card-more">查看 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/contracts-readme/"><h3 class="oc-card-title">契约体系</h3><p>技术、模块、配置三类契约。</p><span class="oc-card-more">查看 →</span></a>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">技术形态</p>
  <h2 class="oc-h2">构建在既有的 .NET 资产之上</h2>
  <p class="oc-lead">ASP.NET Web Forms（.NET Framework 4.8）、Windows + IIS（或 IIS Express）、SQL Server / LocalDB，MIT 许可。它不是单页应用，也没有对外 REST / GraphQL API —— 集成边界见<a href="{{ site.baseurl }}/product-overview/">产品概览</a>。</p>
  <div class="oc-band">
    <p class="oc-eyebrow">下一步</p>
    <h2 class="oc-h2">从本地跑通开始</h2>
    <p class="oc-cta-row"><a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/user-guide/">打开用户指南</a></p>
  </div>
</section>
</div>
<!-- markdownlint-enable MD033 -->
