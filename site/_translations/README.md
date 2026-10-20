---
layout: default
title: HIA-ASPNETPortal documentation
lang: en
parent: 产品概览
nav_order: 1
---

# HIA-ASPNETPortal documentation

This directory is the project's public maintained-documentation entry point, addressed to users, developers and future
maintainers. Internal planning and collaboration material are not published with this site.

## Documentation index

- `architecture.md`: system structure and main module descriptions.
- `dev-guide.md`: local development, build, configuration and debugging guide.
- `versioning.md`: version numbering rules, the relationship between versions and milestones, assembly version coupling,
  the release process and publicity framing constraints.
- `frontend-asset-guide.md`: Web Forms presentation, themes, module CSS, Gulp and frontend asset boundaries.
- `theme-package-guide.md`: directory, enablement, fallback and release boundaries for trusted theme packages.
- `theme-semantic-variables.md`: the theme layer's `--portal-*` semantic variable matrix (7 themes × **14 variables**),
  a quick-reference table of purposes, usage rules and a reproduction command.
- `theme-module-contract.md`: the module appearance contract (division of labour between theme and module layers, the seven
  appearance class families and the variables they use, the real meaning of `var()` fallbacks and the audited differences).
- `module-development-guide.md`: development, registration, enable/disable and removal boundaries for trusted deployed module
  packages.
- `product-overview.md`: product overview and positioning (what it is, who it is for, capability map, boundaries,
  compatibility and licence, where it fits).
- `user-guide.md`: how to run and use the portal, including the portal configuration flow, common administrator operations
  and troubleshooting.
- `testing-checklist.md`: build, test, manual verification and pre-release checklists.
- `documentation-artifacts-guide.md`: input/output boundaries for public documentation, JSDoc, XML documentation and
  generated output.
- `documentation-review-checklist.md`: the documentation review **manual walkthrough checklist** (correctness / bilingual
  equivalence / reader reachability / not trusting the gates blindly), covering what the automated gates cannot prove.
- `contracts/`: the technical contract directory — boundary decision (no external REST/GraphQL), the
  [module contract](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/contracts/module-manifest.md)
  (`module.json`) and the
  [configuration contract](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/contracts/app-settings.md)
  (`appSettings`); entry point:
  [contracts/README.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/contracts/README.md).
- `site-visual-baseline.md`: the **site visual baseline** (palette / fonts / typographic hierarchy / code-block and table
  colours / navigation skeleton), the basis for `site/` values.
- `documentation-registry.md`: the applicability registry for the 7 documentation families (applicable parties, maintenance
  responsibility, update triggers) and the registered gaps.
- `documentation-map.md`: the navigation map, reading paths and gap pointers for the 7 families, for locating documents
  quickly by role.
- `deployment-checklist.md`: SQL Server, IIS, external configuration and post-release regression checklist.
- `deployment-rollback-guide.md`: preparation notes for file, configuration, database and evidence rollback.
- `release-notes-template.md`: a template for version number, changes, verification summary, deferred evidence and rollback
  entry points.
- `operations-runbook.md`: operations entry points, routine checks, logging maintenance, audit queries, backup reminders and
  the evidence package.
- `deployment-default-credentials.md`: default accounts, sample passwords in initialisation scripts and legacy credential
  governance.
- `p12-sample-business-flow.md`: the employee-profile-correction sample business path, sample data SQL and the acceptance
  entry.
- `business-application-state-machine.md`: the status vocabulary, allowed transitions and fail-closed write constraints of
  business applications.
- `collaboration-item-state-machine.md`: the status vocabulary, allowed transitions and handling-role semantics of
  collaboration items.
- `audit-retention-policy.md`: retention principles and their basis for the operational audit table.
- `font-policy-and-audit.md`: font usage principles, licence boundaries and current audit results.
- `third-party-dependencies.md`: purpose, licence and distribution boundaries of newly added third-party
- `code-documentation.md`: the **code documentation system** — API-level documentation generated from source comments by
  HIA-Documentation-Sys (the JSDoc half is published as bilingual pages; the DotNetDoc half is intermediate output), plus
  refresh procedure and dependency-installation notes. dependencies.

## How to use this documentation

- This documentation is available in both Chinese and English, switchable at any time.
- Documents are organised into eight sections — product overview / evaluate / quick start / learn / reference / develop /
  operate / changes; see the [documentation map](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-map.md).
