---
layout: default
title: Home
nav_order: 1
lang: en
---

<!-- markdownlint-disable MD033 -->
<p><a href="{{ site.baseurl }}/">← 中文</a></p>
<!-- markdown-enable MD033 -->

# HIA-ASPNETPortal

**Enterprise intranet portal platform** on ASP.NET Web Forms. Forked from the original ASP.NET Portal Starter Kit on Codeplex and
extended with permission and data-scope enforcement, workflow state machines, auditing, module package governance, theme
governance, and an automated gate suite.

Current version: **v1.0.0** (first release, 2026-10-09).

## What it is

| Item | Description |
| --- | --- |
| Form | ASP.NET **Web Forms** portal (**.NET Framework 4.8**) |
| Runtime | Windows; IIS Express (local) / IIS (deployment); SQL Server or LocalDB |
| License | **MIT** |
| Upstream | Forked from the ASP.NET Portal Starter Kit |

> **Maturity wording (important)**: this release meets the seven criteria of `M-ANP-RELEASE-READY-PORTAL` (**L10**) and is
> stated as **usable / reference / research baseline**; production readiness under real target-environment evidence
> (`M-ANP-TRUSTED-PORTAL` / L7) is **not** yet achieved, so production-grade reliability is **not** claimed.
> See [versioning.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/versioning.md).

## Key capabilities

- **Identity & authorization**: account registration, role-permission entity grants, **data scoping by organization subtree**
- **Workflow state machines**: **explicit transition tables** for business applications and collaboration items; fail-closed
  before writes, events recorded on every transition
- **Auditing**: operational audit tables, retention policy, log maintenance and archival
- **Module package governance**: registration, enable/disable, removal and boundary validation for trusted packages
  (`module.json`)
- **Theme governance**: theme package contract validation and fallback; `--portal-*` semantic variable matrix across 7 official
  skins (14 variables)
- **Internationalization**: Chinese / English resources, strongly typed resource keys, resource contract gate
- **Automated gates**: unified orchestration layered as L0 (static) / L1 (post-build) / L2 (runtime)

## Start here

> All **31 pages** are now published with both Chinese and English versions. They are grouped into eight sections following
> the reader path in the [documentation map](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-map.md).

**Product overview**

- [Product overview]({{ site.baseurl }}/en/overview/) — what it is, who it is for, and its boundaries
- [Documentation index]({{ site.baseurl }}/en/docs-index/) — a one-line description of every publicly maintained document

**Evaluate**

- [Architecture overview]({{ site.baseurl }}/en/architecture/) — system structure, data access and the four current architecture risks

**Quick start**

- [User guide]({{ site.baseurl }}/en/user-guide/) — run locally, external configuration, initial accounts and common modules

**Learn**

- [P12 employee-profile correction sample business path]({{ site.baseurl }}/en/p12-sample-business-flow/) — a sample business flow, sample data SQL and the acceptance entry

**Reference**

- [Testing checklist]({{ site.baseurl }}/en/testing-checklist/) — the seven-layer automated regression tiers and pre-release checks
- [Technical contracts (entry)]({{ site.baseurl }}/en/contracts-readme/) — boundary decision: no external REST/GraphQL; module, configuration and enterprise capability object contracts apply
- [Module contract (`module.json`)]({{ site.baseurl }}/en/contracts-module-manifest/) — fields, `schemaVersion` 1/2, the optional `capability`, and explicitly rejected declarations
- [Configuration contract (`appSettings`)]({{ site.baseurl }}/en/contracts-app-settings/) — the two-layer effective mechanism, key families, and the difference between profiles and package enablement
- [Theme semantic variables]({{ site.baseurl }}/en/theme-semantic-variables/) — 7 themes × 14 variables
- [Theme-module appearance contract]({{ site.baseurl }}/en/theme-module-contract/) — division of labour between theme and module layers, the real meaning of fallbacks
- [Third-party dependency audit]({{ site.baseurl }}/en/third-party-dependencies/) — purpose, licence and distribution boundaries
- [Font policy and audit]({{ site.baseurl }}/en/font-policy-and-audit/) — usage principles and licence boundaries
- [Business application state machine]({{ site.baseurl }}/en/business-application-state-machine/) — status vocabulary, transition table and fail-closed constraints
- [Collaboration item state machine]({{ site.baseurl }}/en/collaboration-item-state-machine/) — status vocabulary, transition table and handling-role semantics
- [Audit retention policy]({{ site.baseurl }}/en/audit-retention-policy/) — retention and disposal rules for the operational audit table
- [Documentation registry]({{ site.baseurl }}/en/documentation-registry/) — the applicability registry for the 7 documentation families
- [Documentation review walkthrough checklist]({{ site.baseurl }}/en/documentation-review-checklist/) — what the automated gates cannot prove
- [Documentation map]({{ site.baseurl }}/en/documentation-map/) — navigation map and reader paths for the 7 families
- [Documentation artefacts and toolchain boundaries]({{ site.baseurl }}/en/documentation-artifacts-guide/) — public documentation, documentation inputs and reproducible artefacts
- [Site visual baseline]({{ site.baseurl }}/en/site-visual-baseline/) — the basis for palette, type and typography values

**Develop**

- [Deployed module package development guide]({{ site.baseurl }}/en/module-development/) — module package development, registration, enable/disable and hidden traps
- [Frontend assets guide]({{ site.baseurl }}/en/frontend-asset-guide/) — Web Forms presentation, themes, module CSS and asset boundaries
- [Theme package deployment guide]({{ site.baseurl }}/en/theme-package-guide/) — theme package directory, manifest, selection and release rollback
- [Developer guide]({{ site.baseurl }}/en/dev-guide/) — environment, build, debug, tests and database initialisation

**Operate**

- [Release and compatibility checklist]({{ site.baseurl }}/en/deployment-checklist/) — pre-release boundaries, IIS, database and post-release regression
- [Operations runbook]({{ site.baseurl }}/en/operations-runbook/) — routine checks, logging maintenance, audit queries and evidence packages
- [Default credentials and legacy password governance]({{ site.baseurl }}/en/deployment-default-credentials/) — default accounts and the legacy MD5 compatibility path
- [Release rollback guide]({{ site.baseurl }}/en/deployment-rollback-guide/) — file, configuration, database and evidence rollback preparation

**Changes**

- [Versioning policy]({{ site.baseurl }}/en/versioning/) — version numbering rules, milestone relationships and publicity framing constraints
- [Release notes template]({{ site.baseurl }}/en/release-notes-template/) — version, changes, verification summary and rollback entry points

> This site is **bilingual (Chinese and English in parallel)**: every section has a corresponding English page (path prefixed
> with `/en/`). Chinese lives at the root and English under `/en/`, as two one-to-one trees; a page may only be published
> when both versions exist (enforced by `Build-PortalSite.ps1`).

---

<!-- markdownlint-disable MD033 -->
<p>This site is generated by <code>dev/scripts/Build-PortalSite.ps1</code> from the repository's <code>docs/</code> —
<strong>no separate, code-detached documentation set</strong> (Principle 4).</p>
<!-- markdown-enable MD033 -->
