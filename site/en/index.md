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

| Section | Documents |
| --- | --- |
| **Product overview** | [Product overview]({{ site.baseurl }}/en/overview/) · [Documentation index]({{ site.baseurl }}/en/docs-index/) |
| **Evaluate** | [Architecture overview]({{ site.baseurl }}/en/architecture/) |
| **Quick start** | [User guide]({{ site.baseurl }}/en/user-guide/) |
| **Learn** | [P12 employee-profile correction sample business path]({{ site.baseurl }}/en/p12-sample-business-flow/) |
| **Reference** | [Testing checklist]({{ site.baseurl }}/en/testing-checklist/) · [Theme semantic variables]({{ site.baseurl }}/en/theme-semantic-variables/) · [Theme-module appearance contract]({{ site.baseurl }}/en/theme-module-contract/) · [Third-party dependency audit]({{ site.baseurl }}/en/third-party-dependencies/) · [Font policy and audit]({{ site.baseurl }}/en/font-policy-and-audit/) · [Business application state machine]({{ site.baseurl }}/en/business-application-state-machine/) · [Collaboration item state machine]({{ site.baseurl }}/en/collaboration-item-state-machine/) · [Audit retention policy]({{ site.baseurl }}/en/audit-retention-policy/) · [Documentation registry]({{ site.baseurl }}/en/documentation-registry/) · [Documentation review walkthrough checklist]({{ site.baseurl }}/en/documentation-review-checklist/) · [Documentation map]({{ site.baseurl }}/en/documentation-map/) · [Documentation artefacts and toolchain boundaries]({{ site.baseurl }}/en/documentation-artifacts-guide/) · [Technical contracts]({{ site.baseurl }}/en/contracts-readme/) · [Module contract]({{ site.baseurl }}/en/contracts-module-manifest/) · [Configuration contract]({{ site.baseurl }}/en/contracts-app-settings/) · [Site visual baseline]({{ site.baseurl }}/en/site-visual-baseline/) |
| **Develop** | [Deployed module package development guide]({{ site.baseurl }}/en/module-development/) · [Frontend assets guide]({{ site.baseurl }}/en/frontend-asset-guide/) · [Theme package deployment guide]({{ site.baseurl }}/en/theme-package-guide/) · [Developer guide]({{ site.baseurl }}/en/dev-guide/) |
| **Operate** | [Release and compatibility checklist]({{ site.baseurl }}/en/deployment-checklist/) · [Operations runbook]({{ site.baseurl }}/en/operations-runbook/) · [Default credentials and legacy password governance]({{ site.baseurl }}/en/deployment-default-credentials/) · [Release rollback guide]({{ site.baseurl }}/en/deployment-rollback-guide/) |
| **Changes** | [Versioning policy]({{ site.baseurl }}/en/versioning/) · [Release notes template]({{ site.baseurl }}/en/release-notes-template/) |

## Where to go next

Document entry points for each capability are in the Reference / Develop / Operate sections above; the
[documentation map]({{ site.baseurl }}/en/documentation-map/) provides reading paths by role (user / developer / auditor).
