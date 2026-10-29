---
layout: default
lang: en
parent: Product overview
nav_order: 2
permalink: /en/docs-index/
title: HIA-ASPNETPortal documentation
---
# HIA-ASPNETPortal documentation

This directory is the project's public maintained-documentation entry point, addressed to users, developers and future
maintainers.

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
 appearance class families and the variables they use, the real meaning of `var` fallbacks and the audited differences).
- `module-development-guide.md`: development, registration, enable/disable and removal boundaries for trusted deployed module
 packages.
- `product-overview.md`: product overview and positioning (what it is, who it is for, capability map, boundaries,
 compatibility and licence, where it fits).
- `user-guide.md`: how to run and use the portal, including the portal configuration flow, common administrator operations
and troubleshooting.
- `first-use-portal.md`: **First use of the portal** (the entry page of the operator track: sign in → recognize the three
areas of the home page → recognize the menu after signing in → view to-dos → handle it in the source module → what to check
when something is missing), with screenshots of the running portal.
- `business-application-workflow.md`: **Business requests: from submission to closure** (an end-to-end task path: submit →
what happens after submitting → amend and resubmit when returned → why you cannot see the approval trail → when the
submission did not go through).
- `leave-and-expense-workflow.md`: **Leave and expense: from filling in to appearing in to-dos** (two structurally identical
modules covered together: how their fields differ, what lands where after submitting, and the failure paths).
- `profile-confirm-and-correct.md`: **Employee profile: confirm and correct** (read-only confirmation → spot an error →
file a correction request → who handles it → when you cannot see the progress).
- `operator-handbook.md`: **Operator handbook: running the portal day to day** (publish an announcement → adjust tabs and
visibility → enable and configure modules → a quick map of the administration pages).
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
- `module-mechanics-and-extension.md`: **Module mechanics and secondary development** (the Reference overview: the five layers
→ profiles → adding a module → changing the look → the four gates → where the API documentation lives).
- `business-application-state-machine.md`: the status vocabulary, allowed transitions and fail-closed write constraints of
 business applications.
- `collaboration-item-state-machine.md`: the status vocabulary, allowed transitions and handling-role semantics of
 collaboration items.
- `audit-retention-policy.md`: retention principles and their basis for the operational audit table.
- `font-policy-and-audit.md`: font usage principles, licence boundaries and current audit results.
- `third-party-dependencies.md`: purpose, licence and distribution boundaries of newly added third-party
- `modules/announcements.md`: detailed operating instructions for the **Announcements** module — field table,
 required fields and the date comparison check, expiry semantics, storage and audit, permissions, common questions.
- `modules/contacts.md`: detailed operating instructions for the **Contacts** module — field table, the single required
 field, the risk of having no format validation, storage and audit, permissions, common questions.
- `modules/discussion.md`: detailed operating instructions for the **Discussion** module — detail-page parameters, topic and
 reply hierarchy, the fact that no field is required, storage and audit, permissions, common questions.
- `modules/document.md`: detailed operating instructions for the **Document** module — field table, upload and path security
 policies, storage and audit, permissions, common questions.
- `modules/events.md`: detailed operating instructions for the **Events** module — field table, all four fields required,
 expiry filtering, storage and audit, permissions, common questions.
- `modules/image-module.md`: detailed operating instructions for the **ImageModule** module — field table, no required fields and
 no validation, text settings only with no upload, storage and audit, permissions, common questions.
- `modules/links.md`: detailed operating instructions for the **Links** module — field table, three required fields and the integer
 order check, link-ownership check, the test-tab-only deployment status, storage and audit, common questions.
- `modules/quick-links.md`: detailed operating instructions for the **QuickLinks** module — shared data and edit page with the Links
 module, how the navigation policy suppresses non-compliant addresses, permission model, storage and audit, common questions.
- `modules/html-module.md`: detailed operating instructions for the **HtmlModule** module — field table, no required fields, the narrow
 raw-HTML permission and the no-sanitisation security boundary, storage and audit, common questions.
- `modules/xml-module.md`: detailed operating instructions for the **XmlModule** module — field table, path-compliance checks and neutral
 runtime degradation, virtual paths only with no upload, storage and audit, common questions.
- `modules/index.md`: the **index of the module operating guides** — one page per module (purpose and prerequisites,
 operating steps, field table, required fields and validation, storage and audit, permissions, common questions,
 boundaries and non-goals), plus three cross-cutting facts (only Signin writes operational audit, validation strength
 varies enormously between modules, and deployment status differs).
- `modules/signin.md`: detailed operating instructions for the **Signin** module — view controls and submission
 protection (one-time RSA public key encryption), the validation flow, the **only path in this repository that writes
 operational audit**, the permission model beyond `ContentRawHtmlEdit`, and the evidence boundary of **no edit page /
 0 instances / source-level description only**.
- `code-documentation.md`: the **code documentation system** — API-level documentation generated from source comments by
 HIA-Documentation-Sys (the JSDoc half is published as bilingual pages; the DotNetDoc half is intermediate output), plus
 refresh procedure and dependency-installation notes. dependencies.

## How to use this documentation

- This documentation is available in both Chinese and English, switchable at any time.
- Documents are organised into eight sections — product overview / evaluate / quick start / learn / reference / develop /
 operate / changes; see the [documentation map](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-map.md).

<!-- build-stamp: BA300C565212 -->
