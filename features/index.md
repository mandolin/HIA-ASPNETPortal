---
layout: home
title: 功能
parent: 产品概览
lang: zh-CN
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="oc-page">
<section class="oc-hero">
  <span class="oc-hero-tag">功能</span>
  <h1 class="oc-hero-title">开箱即用的那些能力</h1>
  <p class="oc-lead" style="max-width:46rem">下面这些不需要外挂组件：部署后即可使用，或以模块/主题包的形式按需接入。每项都有对应文档与自动化检查。</p>
</section>

<section class="oc-section">
  <div class="oc-grid-2">
    <a class="oc-card" href="{{ site.baseurl }}/user-guide/"><h3 class="oc-card-title">身份与授权</h3><p>账户注册、角色权限实体授权、按组织子树的数据范围控制。同一套页面对不同组织的人给出不同的可见范围。</p><span class="oc-card-more">用户指南 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/business-application-state-machine/"><h3 class="oc-card-title">业务申请状态机</h3><p>显式迁移表定义每次状态变化；写入前 fail-closed，迁移必写事件，非法迁移直接被拒绝。</p><span class="oc-card-more">查看迁移表 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/collaboration-item-state-machine/"><h3 class="oc-card-title">协同事项状态机</h3><p>跨部门的协同事项同样走显式迁移表，处理进度与责任方可追溯。</p><span class="oc-card-more">查看迁移表 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/audit-retention-policy/"><h3 class="oc-card-title">审计</h3><p>运营审计表记录"谁在什么时候改了什么"；保留期策略、日志维护与归档可配置。</p><span class="oc-card-more">保留期策略 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/module-development/"><h3 class="oc-card-title">模块包治理</h3><p>受信任模块包的注册、启停、移除与边界校验；新业务以模块包接入，不改动门户核心。</p><span class="oc-card-more">模块开发指南 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/theme-semantic-variables/"><h3 class="oc-card-title">主题治理</h3><p>主题包契约校验与回退；7 套正式皮肤共享一套语义变量矩阵，换肤不改业务代码。</p><span class="oc-card-more">语义变量矩阵 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/contracts-app-settings/"><h3 class="oc-card-title">外置配置</h3><p>连接串与关键配置外置，本地、测试与部署环境互不污染；配置契约有自动化核对。</p><span class="oc-card-more">配置契约 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/p12-sample-business-flow/"><h3 class="oc-card-title">样例业务路径</h3><p>员工资料更正的完整业务路径样例：从申请、审批到审计落表，演示各能力如何协同。</p><span class="oc-card-more">业务路径 →</span></a>
  </div>
</section>

<section class="oc-section">
  <p class="oc-eyebrow">质量保障</p>
  <h2 class="oc-h2">自动化门禁</h2>
  <p class="oc-lead">分层编排的自动化检查把质量约束跑成命令：L0 静态检查、L1 构建后检查、L2 运行期检查。</p>
  <div class="oc-grid">
    <a class="oc-card" href="{{ site.baseurl }}/testing-checklist/"><h3 class="oc-card-title">测试分层</h3><p>单元 / 契约 / Smoke 分层，SQL 审核与外置 test 配置约束。</p><span class="oc-card-more">测试清单 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/site-visual-baseline/"><h3 class="oc-card-title">站点视觉基线</h3><p>本站自身的配色、排版与双语骨架也纳入门禁。</p><span class="oc-card-more">视觉基线 →</span></a>
    <a class="oc-card" href="{{ site.baseurl }}/third-party-dependencies/"><h3 class="oc-card-title">依赖审计</h3><p>第三方依赖的许可证与用途逐项登记。</p><span class="oc-card-more">依赖审计 →</span></a>
  </div>
</section>
</div>
<!-- markdownlint-enable MD033 -->
