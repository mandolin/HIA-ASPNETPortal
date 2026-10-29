---
layout: default
lang: en
parent: Reference
nav_order: 9
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

## Two reader tracks and section ownership (established 2026-10-29)

The site is split **by reader**, and the two tracks never restate each other (restating is how documents drift):

| Section | Reader | Question it answers | Content |
| --- | --- | --- | --- |
| **Learn** | Site operators / end users | "How do I use this feature? How do I get a task done?" | The 20 module usage guides (`docs/modules/`) plus business flows (`p12-sample-business-flow.md` and others) |
| **Reference** | Developers / secondary developers | "How does it work? What do I change or extend? Where is the API?" | Contracts and state machines (`docs/contracts/`, the two state-machine pages), theme and asset documents, the **developer-guide family** (module development, theme packages, theme module contract, frontend assets, developer overview, code documentation explainer), plus the testing checklist and documentation-artifacts guide |

> **Rule of thumb**: if the reader's task is to **change the system**, it belongs in Reference; if it is to **use the
> system**, it belongs in Learn. The former "Develop" section has been merged into "Reference", so the navigation now runs
> seven sections: Product overview → Evaluate → Quick start → Learn → Reference → Operate → Changes.
> The house style and prohibitions live in an internal standard (internal material, not published).

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
- `first-use-portal.md` — **First use of the portal**: the entry page of the operator track (sign in → recognize the three
  areas of the home page → recognize the menu after signing in → view to-dos → handle it in the source module →
  troubleshooting table), written on the Learn skeleton of the two-track standard
- `business-application-workflow.md` — **Business requests: from submission to closure** (an end-to-end task path: submit →
  what happens after submitting → amend and resubmit when returned → why you cannot see the approval trail → when the
  submission did not go through)
- `leave-and-expense-workflow.md` — **Leave and expense: from filling in to appearing in to-dos** (two structurally identical
  modules covered together: how their fields differ, what lands where after submitting, and the failure paths)
- `profile-confirm-and-correct.md` — **Employee profile: confirm and correct** (read-only confirmation → spot an error →
  file a correction request → who handles it → when you cannot see the progress)
- `operator-handbook.md` — **Operator handbook: running the portal day to day** (publish an announcement → adjust tabs and
  visibility → enable and configure modules → a quick map of the administration pages; it states plainly that the
  announcement guest-visibility flag and tab ordering have no administration entry point)
- `business-application-review.md` — **Reviewer path: how a business application gets reviewed** (the administration review
  page → status filter → approve / return / reject → where to look afterwards; including the boundary that a failed work-item
  projection does not roll back the application state, and that there is no approval-trail screen)
- `leave-and-expense-review.md` — **Leave and expense: the review side today (no administration entry point yet)** (the
  administration to-do page is read-only and its business-entry allowlist recognises only three kinds, so leave and expense
  fall through to a not-implemented placeholder page)
- `profile-correction-review.md` — **Profile correction: the reviewer path** (Reviewed / Rejected / Closed; the key boundary
  is that it **does not modify employee master data** — changing the profile goes through the employee directory)
- `module-mechanics-and-extension.md` — **Module mechanics and secondary development** (the Reference overview: the five
  layers → profiles → adding a module → changing the look → the four gates → where the API documentation lives)
- **Gaps registered after the two-track re-cut (2026-10-29) — now filled**: three end-to-end task paths (business requests /
  leave and expense / profile confirmation and correction), the operator handbook, and the Reference overview are all
  published; `p12-sample-business-flow.md` stays as the **implementation and regression** view and has moved to the
  Reference section.
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

### 4. Developers and contributors

- `dev-guide.md` — general development guide
- `module-development-guide.md` — module development guide
- `frontend-asset-guide.md` — front-end asset guide
- `theme-package-guide.md` — theme package guide
- `theme-semantic-variables.md` — theme semantic variable matrix (7 themes × **14 variables**, purpose quick reference, usage rules)
- `theme-module-contract.md` — module appearance contract (division of labour between layers, appearance class families, fallback value meaning and differences)
- `documentation-artifacts-guide.md` — documentation artifacts guide
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

<!-- build-stamp: C7E76E8CFF2B -->
