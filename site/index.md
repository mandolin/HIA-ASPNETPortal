---
layout: home
title: 首页
parent: 产品概览
nav_order: 1
lang: zh-CN
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="oc-page">
<section class="oc-hero">
  <span class="oc-hero-tag">企业内网门户</span>
  <h1 class="oc-hero-title">面向企业内网的 Web Forms 门户平台</h1>
  <p class="oc-hero-lead">账户与角色授权、按组织子树的数据范围、显式迁移表的流程状态机、运营审计、模块包与主题治理，以及分层自动化门禁 —— 一套面向企业内网的门户底座，ASP.NET Web Forms / .NET Framework 4.8，MIT 许可。</p>
  <div class="oc-code">
    <button class="oc-code-copy" type="button" data-oc-copy>复制</button>
    <pre><span class="oc-code-comment"># 还原 NuGet 包（经典 packages.config）</span>
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Restore-NuGetPackages.ps1

<span class="oc-code-comment"># 构建解决方案：Debug | Any CPU</span>
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Build-Solution.ps1 -Configuration Debug -Platform "Any CPU"</pre>
  </div>
  <p class="oc-cta-row">
    <a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/user-guide/">快速上手</a>
    <a class="oc-btn oc-btn-ghost" href="{{ site.baseurl }}/architecture/">查看架构</a>
  </p>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">为什么选它</p>
  <h2 class="oc-h2">为已经有 .NET 资产的团队准备</h2>
  <p class="oc-lead">如果你希望在既有 .NET Framework 4.8 / Web Forms 技术栈与运维体系上继续建设内网门户，本项目提供缺少的那几块：细粒度授权、流程状态机、审计、模块与主题治理，以及把质量约束跑成命令门的自动化门禁。</p>
  <div class="oc-stats">
    <div><div class="oc-stat-val">v2.1.1</div><div class="oc-stat-label">当前版本</div></div>
    <div><div class="oc-stat-val">MIT</div><div class="oc-stat-label">开源许可</div></div>
    <div><div class="oc-stat-val">7</div><div class="oc-stat-label">套正式皮肤</div></div>
    <div><div class="oc-stat-val">双语</div><div class="oc-stat-label">中 / 英文资源</div></div>
    <div><div class="oc-stat-val">56</div><div class="oc-stat-label">篇文档</div></div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">核心能力</p>
  <h2 class="oc-h2">开箱就有，也能按自己的方式扩展</h2>
  <p class="oc-lead">下面五类是这个门户平台的核心能力：它们彼此独立，可以按需要启用，也可以只参考其中一部分。</p>
  <div class="oc-grid-2">
    <a class="oc-card" href="{{ site.baseurl }}/business-application-state-machine/">
      <h3 class="oc-card-title">权限与数据范围</h3>
      <p>账户注册、角色与实体授权，以及按组织子树收敛的数据范围控制，让同一套页面对不同组织的人给出不同的可见范围。</p>
      <span class="oc-card-more">查看业务申请状态机 →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/collaboration-item-state-machine/">
      <h3 class="oc-card-title">流程状态机</h3>
      <p>业务申请与协同事项都有显式迁移表：写入前 fail-closed，每次迁移必写事件，状态变化可追溯。</p>
      <span class="oc-card-more">查看协同事项状态机 →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/audit-retention-policy/">
      <h3 class="oc-card-title">审计与合规</h3>
      <p>运营审计表配合保留期策略、日志维护与归档，把“谁在什么时候改了什么”留在可控的时间窗内。</p>
      <span class="oc-card-more">查看审计保留期策略 →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/theme-module-contract/">
    <h3 class="oc-card-title">模块与主题治理</h3>
    <p>受信任模块包可注册、启停、移除并做边界校验；主题包有契约校验与回退，7 套正式皮肤共用一套语义变量矩阵。</p>
    <span class="oc-card-more">查看模块外观契约 →</span>
    </a>
    <a class="oc-card" href="{{ site.baseurl }}/code-documentation/">
    <h3 class="oc-card-title">代码文档</h3>
    <p>由 HIA-Documentation-Sys 从源码注释生成 API 级文档：JSDoc 半边已发布中英双语页面，DotNetDoc 半边生成中间产物、待渲染组件就位后补入。</p>
    <span class="oc-card-more">查看代码文档说明 →</span>
    </a>
    </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">功能一览</p>
  <h2 class="oc-h2">已经内置的那些</h2>
  <p class="oc-lead">从身份认证到自动化门禁，下面这些都不需要外挂组件就能用。</p>
  <div class="oc-grid">
    <div class="oc-card"><h3 class="oc-card-title">身份与授权</h3><p>账户注册、角色权限实体授权、按组织子树的数据范围控制。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">流程状态机</h3><p>显式迁移表，写入前 fail-closed，迁移必写事件。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">审计</h3><p>运营审计表、保留期策略、日志维护与归档。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">模块包治理</h3><p>受信任模块包的注册、启停、移除与边界校验。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">主题治理</h3><p>主题包契约校验与回退；7 套皮肤共享语义变量矩阵。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">国际化</h3><p>中文 / 英文资源与强类型资源键。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">自动化门禁</h3><p>按 L0（静态）/ L1（构建后）/ L2（运行期）分层编排。</p></div>
    <div class="oc-card"><h3 class="oc-card-title">默认凭据治理</h3><p>初始化口令的处置路径与旧口令迁移策略可查可管。</p></div>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">文档</p>
  <h2 class="oc-h2">按要解决的问题读文档</h2>
  <p class="oc-lead">文档按用途分成八个分区，每个分区都有对应的中英两版页面。</p>
  <div class="oc-grid">
    <a class="oc-card" href="{{ site.baseurl }}/sections/product-overview/"><h3 class="oc-card-title">产品概览</h3><p>定位、适用边界与全部文档索引。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/evaluate/"><h3 class="oc-card-title">评估</h3><p>系统结构、数据访问与当前架构风险。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/quick-start/"><h3 class="oc-card-title">快速上手</h3><p>本地运行、外置配置与常见模块。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/learn/"><h3 class="oc-card-title">学习</h3><p>完整业务路径，理解各能力如何协同。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/reference/"><h3 class="oc-card-title">参考</h3><p>测试分层、主题契约、状态机与合规策略。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/develop/"><h3 class="oc-card-title">开发</h3><p>模块与主题开发、前端资产、开发环境。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/operate/"><h3 class="oc-card-title">运维</h3><p>部署、运维、默认凭据治理与回滚。</p><span class="oc-card-more">进入分区 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/sections/changes/"><h3 class="oc-card-title">变更</h3><p>版本规则与发布说明模板。</p><span class="oc-card-more">进入分区 →</span></a>
  </div>
  <div class="oc-band">
    <p class="oc-eyebrow">还不清楚从哪读起</p>
    <h2 class="oc-h2">文档地图给出按角色的阅读路径</h2>
    <p class="oc-lead">使用者、开发者、审计三条入口，各自列出该读哪几篇。</p>
    <p class="oc-cta-row"><a class="oc-btn oc-btn-primary" href="{{ site.baseurl }}/documentation-map/">打开文档地图</a></p>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">开始使用</p>
  <h2 class="oc-h2">三条常见的起手方式</h2>
  <div class="oc-grid">
    <a class="oc-card" href="{{ site.baseurl }}/user-guide/"><h3 class="oc-card-title">用 Visual Studio 跑起来</h3><p>打开 <code>src/master.sln</code>，还原 NuGet 包，选择 Debug | Any CPU 后构建运行。</p><span class="oc-card-more">打开用户指南 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/dev-guide/"><h3 class="oc-card-title">用命令行构建</h3><p>仓库内脚本可自动定位 MSBuild 与 NuGet CLI，不依赖 IDE。</p><span class="oc-card-more">打开开发指南 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/operations-runbook/"><h3 class="oc-card-title">部署到 IIS</h3><p>外置连接串、应用池与站点配置的运维手册。</p><span class="oc-card-more">打开运维手册 →</span></a>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">常见问题</p>
  <h2 class="oc-h2">选型之前常被问到的几个问题</h2>
  <div class="oc-faq">
    <details>
      <summary>它是什么形态的项目？</summary>
      <p>ASP.NET Web Forms 门户，运行在 .NET Framework 4.8 上，Windows + IIS（或 IIS Express）+ SQL Server / LocalDB。它不是单页应用，也没有单页路由；页面与模块以 Web Forms 控件树方式呈现。</p>
    </details>
    <details>
      <summary>适合什么样的团队？</summary>
      <p>希望沿用 .NET Framework 4.8 / Web Forms 既有技术栈与运维体系的团队，尤其是需要在一个门户里统一权限、流程与审计的组织。</p>
    </details>
    <details>
      <summary>它能直接用到生产环境吗？</summary>
      <p>当前版本适合开发、测试、演示与参考场景。项目尚未提供生产环境验证证据（真实 IIS、TLS、应用池 ACL、目标 SQL Server 与企业扫描等），投入生产前请自行完成安全评估、备份与运维演练。详见<a href="{{ site.baseurl }}/versioning/">版本策略</a>。</p>
    </details>
    <details>
      <summary>有对外 API 吗？</summary>
      <p>没有对外 REST / GraphQL API。本项目是 Web Forms 门户，集成边界见<a href="{{ site.baseurl }}/product-overview/">产品概览</a>。</p>
    </details>
    <details>
      <summary>许可与版本规则是什么？</summary>
      <p>MIT 许可；版本号与语义规则（含模块 manifest 的 <code>minimumPortalVersion</code> 判定）见<a href="{{ site.baseurl }}/versioning/">版本策略</a>。</p>
    </details>
  </div>
</section>
</div>

<script>
  document.querySelectorAll('[data-oc-copy]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var code = btn.parentElement.querySelector('pre').innerText;
      if (navigator.clipboard) { navigator.clipboard.writeText(code); }
      var old = btn.textContent; btn.textContent = '已复制';
      setTimeout(function () { btn.textContent = old; }, 1600);
    });
  });
</script>
<!-- markdownlint-enable MD033 -->
