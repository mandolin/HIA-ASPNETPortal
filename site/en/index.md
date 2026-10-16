---
layout: default
title: Home
parent: Product overview
nav_order: 1
lang: en
---

<!-- markdownlint-disable MD033 -->
<p><a href="{{ site.baseurl }}/">← 中文</a></p>
<!-- markdownlint-enable MD033 -->

# HIA-ASPNETPortal

**An enterprise intranet portal platform on ASP.NET Web Forms.** Forked from the ASP.NET Portal Starter Kit, with
**permissions and data scope, workflow state machines, auditing, module package governance, theme governance and automated
gates** built out on top of it.

<p>
  <a href="{{ site.baseurl }}/en/user-guide/" class="btn btn-primary">Quick start</a>
  <a href="{{ site.baseurl }}/en/docs-index/" class="btn btn-outline">All documentation</a>
</p>

| | |
| --- | --- |
| **Current version** | v1.0.0 (first release, 2026-10-09) |
| **Stack** | ASP.NET Web Forms · .NET Framework 4.8 |
| **Runs on** | Windows · IIS / IIS Express · SQL Server or LocalDB |
| **Licence** | MIT |

> **Maturity framing**
> This release meets all seven criteria of `M-ANP-RELEASE-READY-PORTAL` (L10) and is positioned as a
> **usable / reference / research baseline**. Production readiness under real production evidence
> (`M-ANP-TRUSTED-PORTAL` / L7) has **not** been achieved, so **no production-grade trust is claimed**.
> See the [versioning policy]({{ site.baseurl }}/en/versioning/).

## What this is

| Item | Description |
| --- | --- |
| Form | An ASP.NET **Web Forms** portal (**.NET Framework 4.8**) |
| Runs on | Windows; IIS Express (local) / IIS (deployed); SQL Server or LocalDB |
| Licence | **MIT** |
| Upstream | Forked from the ASP.NET Portal Starter Kit |

## Key capabilities

- **Identity and authorisation**: account registration, role-permission entity authorisation, data scope control by
  organisational subtree
- **Workflow state machines**: explicit transition tables for business applications and collaboration items, fail-closed
  before write, every transition writes an event
- **Auditing**: operational audit table, retention policy, logging maintenance and archiving
- **Module package governance**: registration, enable/disable, removal and boundary validation for trusted module packages
- **Theme governance**: theme package contract validation and fallback; a semantic variable matrix across 7 official skins
- **Internationalisation**: Chinese / English resources, strongly typed resource keys
- **Automated gates**: unified orchestration layered as L0 (static) / L1 (post-build) / L2 (runtime)

## Browse by section

| Section | Contents |
| --- | --- |
| **[Product overview]({{ site.baseurl }}/en/sections/product-overview/)** | Positioning, maturity framing and the full documentation index |
| **[Evaluate]({{ site.baseurl }}/en/sections/evaluate/)** | System structure, data access and current architecture risks |
| **[Quick start]({{ site.baseurl }}/en/sections/quick-start/)** | Run locally, external configuration and common modules |
| **[Learn]({{ site.baseurl }}/en/sections/learn/)** | A complete business path showing how the capabilities work together |
| **[Reference]({{ site.baseurl }}/en/sections/reference/)** | Test tiers, theme contracts, state machines, compliance policy and the documentation system |
| **[Develop]({{ site.baseurl }}/en/sections/develop/)** | Module and theme development, frontend assets, development environment |
| **[Operate]({{ site.baseurl }}/en/sections/operate/)** | Deployment, operations, default credential governance and rollback |
| **[Changes]({{ site.baseurl }}/en/sections/changes/)** | Version rules and the release notes template |

## Where to go next

Document entry points for each capability are in the Reference / Develop / Operate sections above; the
[documentation map]({{ site.baseurl }}/en/documentation-map/) provides reading paths by role (user / developer / auditor).
