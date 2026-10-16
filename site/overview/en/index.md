---
layout: default
lang: en
parent: Product overview
permalink: /en/overview/
title: Product overview and positioning
---
# Product overview and positioning (HIA-ASPNETPortal)

> This page answers "what it is, who it is for, what it can do, and where its
> boundaries lie". For step-by-step operations see
> [user-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/user-guide.md); for deployment and
> operations see `deployment-checklist.md` and `operations-runbook.md`.

## 1. Product overview

**HIA-ASPNETPortal** is an **enterprise intranet portal platform** providing **permission and data scoping, workflow state machines, auditing, module package governance, theme governance, and automated gates**.

Technical form: **ASP.NET Web Forms** (.NET Framework 4.8), Windows + IIS Express + SQL Server / LocalDB. It is not an
SPA and has no client-side routing; pages and modules render through the Web Forms control tree.

> Provenance: this project derives from the ASP.NET Portal Starter Kit published on CodePlex, and has since been
> reworked and extended extensively.

Current version: **v1.0.0** (first release, 2026-10-09).

### Where it fits

This is the **first formal release**, with complete capability coverage, quality gates, documentation and open-source
readiness, version anchoring, and consistent wording.

However, **no production-environment validation evidence is provided yet** (real IIS, TLS, application pool ACLs, target
SQL Server, enterprise scanning, and so on), so **direct production use is not recommended**. Full version rules are in
`docs/versioning.md`.

## 2. Who it is for

**Suitable for**:

- Scenarios that need an **enterprise intranet** portal quickly (tabs + modular content + role permissions + admin console);
- Teams already invested in the .NET Framework 4.8 / Web Forms stack and its operations practices;
- Readers using the project as a **reference baseline** for how portal permission models, module package governance, theme
  governance, and automated gates can be implemented.

**Not suitable for**:

- Integration scenarios that need an **external REST / GraphQL API** — this project is a Web Forms portal with **no such
  external surface**;
- Scenarios requiring modern frontend frameworks (React / Vue) or a separated frontend/backend architecture;
- Deployments that demand "production-grade trust evidence" — that evidence is not yet achieved.

## 3. Capability map

| Capability | Content | Authoritative doc |
| --- | --- | --- |
| Identity & authorization | Account registration, role-permission entity grants, **data scoping by organization subtree**, auth role cookie | `docs/architecture.md` |
| Workflow state machines | **Explicit transition tables** for business applications and collaboration items; fail-closed before writes, events on transitions | `docs/business-application-state-machine.md`, `docs/collaboration-item-state-machine.md` |
| Auditing | Operational audit tables, retention policy, log maintenance and archival | `docs/audit-retention-policy.md`, `docs/operations-runbook.md` |
| Module package governance | Registration, enable/disable, removal and boundary validation for trusted `module.json` packages | `docs/module-development-guide.md` |
| Theme governance | Theme package contract validation and fallback; `--portal-*` semantic matrix across 7 skins (14 variables) | `docs/theme-*`.md |
| Business modules | Employee directory, work-item console, employee profile confirmation / correction, business applications, plus leave requests and expense reimbursements | `docs/user-guide.md` |
| Internationalization | Chinese / English resources, strongly typed keys, resource contract gate | `docs/dev-guide.md` |
| Automated gates | Unified orchestration layered L0 (static) / L1 (post-build) / L2 (runtime) | `docs/testing-checklist.md` |

> **Gate count is whatever the run says**: the number grows with governance, so run
> `dev/scripts/Invoke-PortalGateSuite.ps1` and read the current summary instead of quoting a fixed figure.

## 4. Boundaries (explicitly out of scope)

- **No external REST / GraphQL API**, and no machine-readable interface contract such as OpenAPI;
- **No claim of production-grade trust** (L7 evidence not achieved);
- No support for modern frontend frameworks or a separated frontend/backend architecture;
- Generated documentation directories (`src/Documentation/`, `src/Portal.Components.Data/Documentation/`, `src/DoxyGen/`)
  are **generated output**, not maintained documentation entry points;
- Internal plans, ADRs and AI-collaboration material stay in the private `a separate private repository/` repo, not in public `docs/`.

## 5. Compatibility and runtime

| Item | Requirement / note |
| --- | --- |
| OS | Windows |
| Runtime | **.NET Framework 4.8** (Developer Pack required) |
| Build | Visual Studio 2022, or VS / Build Tools able to build .NET Framework 4.8 |
| Database | SQL Server or SQL Server LocalDB |
| Web server | IIS Express (local) / IIS (deployment) |
| Browser baseline | **IE9+** (CSS uses the "explicit value first, `var()` second" double-declaration pattern for fallback) |
| Node.js | Needed **only** when maintaining frontend asset builds |

## 6. License

This project is licensed under **[MIT](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/LICENSE)**.

It is forked from the **ASP.NET Portal Starter Kit**; the upstream license and attribution are recorded in `README-old.md`.
Third-party dependency purposes, licenses and distribution boundaries are in `docs/third-party-dependencies.md`, and font
matters in `docs/font-policy-and-audit.md`.

## 7. Where to go next

- To **run it**: `docs/user-guide.md` → `docs/dev-guide.md`
- To **use it** (modules, leave / reimbursement): `docs/user-guide.md`
- To **build modules**: `docs/module-development-guide.md`
- To **work on themes**: `docs/theme-package-guide.md` → `docs/theme-semantic-variables.md`
- To **deploy it**: `docs/deployment-checklist.md` → `docs/operations-runbook.md`
- To **find everything**: `docs/documentation-map.md`
