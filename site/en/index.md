---
layout: default
title: Home
parent: Product overview
nav_order: 1
lang: en
toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="hia-hero">
  <p class="hia-hero-eyebrow">HIA series · ASP.NET Web Forms portal</p>
  <h1 class="hia-hero-title">HIA-ASPNETPortal documentation</h1>
  <p class="hia-hero-sub">An intranet Web Forms portal platform: forked from the ASP.NET Portal Starter Kit, extended with permissions and data scope, workflow state machines, auditing, module package governance, theme governance and automated gates.</p>
  <div class="hia-hero-cta">
    <a class="hia-btn hia-btn-primary" href="{{ site.baseurl }}/en/sections/quick-start/">Get started</a>
    <a class="hia-btn hia-btn-ghost" href="{{ site.baseurl }}/en/sections/product-overview/">About the product</a>
  </div>
</div>

<table>
  <tbody>
    <tr><td><strong>Current version</strong></td><td>v1.0.0 (first release 2026-10-09)</td></tr>
    <tr><td><strong>Stack</strong></td><td>ASP.NET Web Forms · .NET Framework 4.8</td></tr>
    <tr><td><strong>Environment</strong></td><td>Windows · IIS / IIS Express · SQL Server or LocalDB</td></tr>
    <tr><td><strong>Licence</strong></td><td>MIT</td></tr>
  </tbody>
</table>

<blockquote>
  <p><strong>Where it fits</strong>　The current version suits <strong>development, testing, demonstration and reference</strong> scenarios, and already provides the full set of capabilities: permissions and data scope, workflow state machines, auditing, module package governance, theme governance and automated gates. Before putting it into production, complete your own security assessment, backup and operations rehearsal — this project <strong>provides no production-environment validation evidence</strong>. See the <a href="{{ site.baseurl }}/en/versioning/">versioning policy</a>.</p>
</blockquote>

<h2 id="browse-by-section">Browse by section</h2>

<div class="hia-cards">
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/product-overview/">
    <p class="hia-card-name">Product overview</p>
    <p class="hia-card-desc">Positioning, where it fits and the full documentation index</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/evaluate/">
    <p class="hia-card-name">Evaluate</p>
    <p class="hia-card-desc">System structure, data access and current architecture risks</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/quick-start/">
    <p class="hia-card-name">Quick start</p>
    <p class="hia-card-desc">Run locally, external configuration and common modules</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/learn/">
    <p class="hia-card-name">Learn</p>
    <p class="hia-card-desc">A complete business path showing how the capabilities work together</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/reference/">
    <p class="hia-card-name">Reference</p>
    <p class="hia-card-desc">Test tiers, theme contracts, state machines, compliance policy and the documentation system</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/develop/">
    <p class="hia-card-name">Develop</p>
    <p class="hia-card-desc">Module and theme development, frontend assets, development environment</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/operate/">
    <p class="hia-card-name">Operate</p>
    <p class="hia-card-desc">Deployment, operations, default credential governance and rollback</p>
  </a>
  <a class="hia-card" href="{{ site.baseurl }}/en/sections/changes/">
    <p class="hia-card-name">Changes</p>
    <p class="hia-card-desc">Version rules and the release notes template</p>
  </a>
</div>

<h2 id="key-capabilities">Key capabilities</h2>

<ul>
  <li><strong>Identity and authorisation</strong>: account registration, role-based entity permissions, org-subtree data scope</li>
  <li><strong>Workflow state machines</strong>: explicit transition tables for business applications and collaboration items, fail-closed before write, transition events always written</li>
  <li><strong>Auditing</strong>: operational audit table, retention policy, log maintenance and archiving</li>
  <li><strong>Module package governance</strong>: registration, enable/disable, removal and boundary checks for trusted module packages</li>
  <li><strong>Theme governance</strong>: theme package contract validation and fallback; semantic variable matrix for 7 official skins</li>
  <li><strong>Internationalisation</strong>: Chinese / English resources, strongly typed resource keys</li>
  <li><strong>Automated gates</strong>: layered orchestration across L0 (static) / L1 (post-build) / L2 (runtime)</li>
</ul>

<p>Documentation for each capability lives in the sections above; the <a href="{{ site.baseurl }}/en/documentation-map/">documentation map</a> provides reader paths by role (user / developer / auditor).</p>
<!-- markdownlint-enable MD033 -->
