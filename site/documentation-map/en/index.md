---
layout: default
lang: en
parent: Reference
permalink: /en/documentation-map/
title: Documentation map and navigation index
---
# Documentation map / navigation index (HIA-ASPNETPortal)

> A navigation index of the existing `docs/` against the 7 documentation families, with gaps marked: this map records "where it is and how it connects"\.

## Reader paths

- **User / implementer**: product features and usage → operations, deployment, security → change, compatibility and release
- **Developer / contributor**: architecture design decisions → developers and contributors → API and technical contracts →
 in-code documentation (in source)
- **Auditor / security**: operations, deployment, security → architecture design decisions → change, compatibility and
 release

## Documentation family index

### 1. In-code documentation (source comments)

Not in `docs/`; lives in source node/flow comments.

### 2. API and technical contracts (`docs/contracts/`)

- Boundary decision: as a Web Forms portal there is **no external REST/GraphQL API**; what applies is "module contract /
 configuration contract / enterprise capability object contract".
- `contracts/README.md` — contract overview, boundary decision table (REST/GraphQL/OpenAPI **not applicable**),
 authoritative-source pointers and verification method
- `contracts/module-manifest.md` — the module contract (`module.json` fields, `schemaVersion` 1/2, the optional
 `capability`, and declarations that are explicitly rejected)
- `contracts/app-settings.md` — the configuration contract (`appSettings` key families, the two-layer effective mechanism,
 and the difference between profiles and package release approval)

### 3. Product features and usage

- `product-overview.md` — **product overview and positioning** : what it is / who it is for /
 capability map / boundaries / compatibility and runtime environment / licence and attribution / where it fits
- `user-guide.md` — how to run and use the portal; includes the **typical portal configuration flow**, **common
 administrator operations** and **troubleshooting** 
- `p12-sample-business-flow.md` — a sample business flow
- **Remaining gaps**: **none for module-level instructions**. (~~Runtime screenshots~~ as a
 reproducible baseline; ~~item-by-item instructions for existing content modules~~;
 ~~the approval-flow UI for leave and reimbursement~~ as an explicit boundary statement, see
 "Approval flow boundary (required reading)" in `user-guide.md`; ~~`HTML`~~ delivered; ~~`QuickLinks`~~ delivered; ~~`XML / XSL`~~ delivered; ~~`Signin`~~ delivered)
 - **Evidence boundary (registered honestly)**: the `Signin` module has **0 instances** in the development database, so
 it is documented at source level only and **has no runtime evidence**; re-entry condition: mount the module instance
 in the target environment and collect runtime evidence.
 - **Code documentation **: the JSDoc half is published (`/api/jsdoc/`, bilingual, explainer
 page `code-documentation.md`, source fingerprint guarded by an L0 gate); the DotNetDoc half is still **intermediate
 JSON and not published** — re-entry condition: add it through the same snapshot-manifest mechanism once a
 rendering/publishing component exists.

- `theme-module-contract.md` — module appearance contract (layer division of labour, appearance class families, fallback
 meaning and differences)
- `documentation-artifacts-guide.md` — documentation artefacts guide
- `testing-checklist.md` — testing checklist

### 5. Operations, deployment, security

- `deployment-checklist.md` — deployment checklist
- `deployment-default-credentials.md` — default credential handling
- `deployment-rollback-guide.md` — rollback guide
- `operations-runbook.md` — operations runbook
- `audit-retention-policy.md` — audit retention policy
- `font-policy-and-audit.md` — font policy and audit
- `third-party-dependencies.md` — third-party dependencies

### 6. Architecture design decisions

- `architecture.md` — overall architecture
- `business-application-state-machine.md` — business application state machine
- `collaboration-item-state-machine.md` — collaboration item state machine

### 7. Change, compatibility and release

- `versioning.md` — versioning and compatibility policy
- `release-notes-template.md` — release notes template
- `CHANGELOG.md` (repository root) — the changelog

## Meta-documents

- `site-visual-baseline.md` — the **site visual baseline** (palette / fonts / typographic hierarchy / code-block and table
 colours / navigation skeleton)
 correctness / bilingual equivalence / reader reachability / not over-trusting the gates)
- `README.md` — the repository entry point (with documentation section navigation pointing to this map)
