---
layout: home
title: Home
parent: Product overview
nav_order: 1
lang: en
has_toc: false
---

<!-- markdownlint-disable MD033 -->
<div class="hia-home-hero">
  <p class="hia-home-eyebrow">HIA series · ASP.NET Web Forms portal</p>
  <h1 class="hia-home-title">An intranet Web Forms portal platform</h1>
  <p class="hia-home-sub">Forked from the ASP.NET Portal Starter Kit and extended with permissions and data scope, workflow state machines, auditing, module package governance, theme governance and automated gates.</p>
  <p class="hia-home-cta">
    <a class="hia-btn hia-btn-primary" href="{{ site.baseurl }}/en/user-guide/">Get started</a>
    <a class="hia-btn hia-btn-ghost" href="{{ site.baseurl }}/en/product-overview/">About the product</a>
  </p>
</div>

**What is it?** An intranet portal foundation: account registration and role-based authorisation, org-subtree data scope, workflow state machines with explicit transition tables, operational auditing, trusted module and theme package governance, and layered automated gates (L0 static / L1 post-build / L2 runtime). The stack is ASP.NET **Web Forms** on **.NET Framework 4.8**, running on Windows IIS / IIS Express with SQL Server or LocalDB, MIT licensed.

> **Where it fits**　The current version suits **development, testing, demonstration and reference**; the full capability set above is in place. Before putting it into production, complete your own security assessment, backup and operations rehearsal — this project **provides no production-environment validation evidence**. See the [versioning policy]({{ site.baseurl }}/en/versioning/).

## Documentation

<ul class="hia-doclist">
  <li><a href="{{ site.baseurl }}/en/sections/product-overview/"><strong>Product overview</strong></a> — positioning, where it fits and the full documentation index</li>
  <li><a href="{{ site.baseurl }}/en/sections/evaluate/"><strong>Evaluate</strong></a> — system structure, data access and current architecture risks</li>
  <li><a href="{{ site.baseurl }}/en/sections/quick-start/"><strong>Quick start</strong></a> — run locally, external configuration and common modules</li>
  <li><a href="{{ site.baseurl }}/en/sections/learn/"><strong>Learn</strong></a> — a complete business path showing how the capabilities work together</li>
  <li><a href="{{ site.baseurl }}/en/sections/reference/"><strong>Reference</strong></a> — test tiers, theme contracts, state machines, compliance policy and the documentation system</li>
  <li><a href="{{ site.baseurl }}/en/sections/develop/"><strong>Develop</strong></a> — module and theme development, frontend assets, development environment</li>
  <li><a href="{{ site.baseurl }}/en/sections/operate/"><strong>Operate</strong></a> — deployment, operations, default credential governance and rollback</li>
  <li><a href="{{ site.baseurl }}/en/sections/changes/"><strong>Changes</strong></a> — version rules and the release notes template</li>
</ul>

Reader paths by role (user / developer / auditor) are in the [documentation map]({{ site.baseurl }}/en/documentation-map/).

## Key capabilities

- **Identity and authorisation**: account registration, role-based entity permissions, org-subtree data scope
- **Workflow state machines**: explicit transition tables for business applications and collaboration items, fail-closed before write, transition events always written
- **Auditing**: operational audit table, retention policy, log maintenance and archiving
- **Module package governance**: registration, enable/disable, removal and boundary checks for trusted module packages
- **Theme governance**: theme package contract validation and fallback; semantic variable matrix for 7 official skins
- **Internationalisation**: Chinese / English resources, strongly typed resource keys
- **Automated gates**: layered orchestration across L0 (static) / L1 (post-build) / L2 (runtime)
<!-- markdownlint-enable MD033 -->
