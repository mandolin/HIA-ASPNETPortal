---
layout: default
title: Documentation map and navigation index
lang: en
parent: 参考
nav_order: 1
---

# Documentation map / navigation index (HIA-ASPNETPortal)

> **A P21 D2 deliverable.** A navigation index of the existing `docs/` against the 7 documentation families of the *HIA
> project documentation structure and template specification*, with gaps marked. It pairs with
> `documentation-registry.md` (D1, which registers applicable/not applicable): the registry records "should this exist",
> this map records "where it is and how it connects".

## Reader paths

- **User / implementer**: product features and usage → operations, deployment, security → change, compatibility and release
- **Developer / contributor**: architecture design decisions → developers and contributors → API and technical contracts →
  in-code documentation (in source)
- **Auditor / security**: operations, deployment, security → architecture design decisions → change, compatibility and
  release

## Documentation family index

### 1. In-code documentation (→ C-anp-P19 ROP, source comments)

Not in `docs/`; lives in source node/flow comments, safeguarded by the ROP gate `Get-PortalCommentDebtInventory`.

### 2. API and technical contracts (`docs/contracts/` built by `W-anp-P125`)

- Boundary decision: as a Web Forms portal there is **no external REST/GraphQL API**; what applies is "module contract /
  configuration contract / enterprise capability object contract".
- `contracts/README.md` — contract overview, boundary decision table (REST/GraphQL/OpenAPI **not applicable**),
  authoritative-source pointers and verification method
- `contracts/module-manifest.md` — the module contract (`module.json` fields, `schemaVersion` 1/2, the optional
  `capability`, and declarations that are explicitly rejected)
- `contracts/app-settings.md` — the configuration contract (`appSettings` key families, the two-layer effective mechanism,
  and the difference between profiles and package release approval)

### 3. Product features and usage

- `product-overview.md` — **product overview and positioning** (a `W-anp-P124` deliverable): what it is / who it is for /
  capability map / boundaries / compatibility and runtime environment / licence and attribution / maturity framing
- `user-guide.md` — how to run and use the portal; includes the **typical portal configuration flow**, **common
  administrator operations** and **troubleshooting** (strengthened by `W-anp-P124`)
- `p12-sample-business-flow.md` — a sample business flow
- **Remaining gaps**: runtime screenshots; item-by-item operating instructions for existing content modules
  (announcements, contacts, discussions, documents, events, images, links, etc.); the approval flow UI for leave and
  reimbursement (this cycle only persists data and generates to-dos; approval is not inside these modules)

### 4. Developers and contributors

- `dev-guide.md` — the general development guide
- `module-development-guide.md` — module development guide
- `frontend-asset-guide.md` — frontend asset guide
- `theme-package-guide.md` — theme package guide (links to C-anp-P18)
- `theme-semantic-variables.md` — the theme semantic variable matrix (7 themes × **14 variables**, purpose quick-reference,
  usage rules; the 14th, `--portal-text-subtle`, was added by the `C-anp-P23` D3 ruling)
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
- (Major-decision ADRs / milestones live in the **separate private repository** and are not published with the public
  documentation.)

### 7. Change, compatibility and release

- `versioning.md` — versioning and compatibility policy
- `release-notes-template.md` — release notes template
- `CHANGELOG.md` (repository root) — the changelog

## Meta-documents

- `documentation-registry.md` — the D1 registry (applicable/not applicable + responsibility/trigger/verification)
- `documentation-map.md` — this map (navigation)
- `site-visual-baseline.md` — the **site visual baseline** (palette / fonts / typographic hierarchy / code-block and table
  colours / navigation skeleton)
- `documentation-review-checklist.md` — the **manual walkthrough checklist** (what automated gates cannot prove:
  correctness / bilingual equivalence / reader reachability / not over-trusting the gates)
- `README.md` — the repository entry point (with documentation section navigation pointing to this map)

## Public release form (the website)

`docs/` is the **Chinese authoritative source**; `site/_translations/` holds the **English translations**.
`dev/scripts/Build-PortalSite.ps1` generates the bilingual site from both and enforces "a page may only be declared published
when both the Chinese and English versions exist", then publishes to the `gh-pages` branch.
A document in this map therefore **appears on the website only once its translation is complete**; a document not yet
published has no corresponding site page, which is expected rather than a gap.
