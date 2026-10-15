---
layout: default
title: Documentation registry
lang: en
parent: 参考
nav_order: 1
---

# Documentation registry (HIA-ASPNETPortal)

> **A P21 D1 deliverable.** Registers this project's "applicable / not applicable" stance against the 7 documentation
> families of the *HIA project documentation structure and template specification*, and records the authoritative location,
> maintenance responsibility, sync trigger and verification method for each. Internal planning / ADRs / research drafts stay
> in a **separate private repository**; only content reviewed as suitable for its audience enters the public `docs/`.
> Vehicle for Principle 4 (documentation throughout the development cycle): this table is the "ledger" of documentation
> engineering, and `W-anp-P124`–`P127` use it to close the gaps (from 2026-10-14 this Cycle switched to W numbering; the old
> D1–D7 remain only as a historical mapping).

## Registry overview

| # | Documentation family | Applicability | Authoritative location | Maintenance responsibility | Sync trigger | Verification method |
|---|---|---|---|---|---|---|
| 1 | In-code documentation | Applicable | Source comments + ROP governance (C-anp-P19) | dev | Per-cycle code changes | ROP gate `Get-PortalCommentDebtInventory` |
| 2 | API and technical contracts | Applicable (boundary decision below) | `docs/contracts/` (to be built) | dev | Module / interface / configuration changes | Contract consistency gate |
| 3 | Product features and usage | Applicable | `docs/user-guide.md` + pending how-to/FAQ | docs | Feature changes | Readability walkthrough / example runs |
| 4 | Developers and contributors | Applicable | `docs/dev-guide.md` and others | docs | Architecture / tool changes | Example runs |
| 5 | Operations, deployment, security | Applicable | `docs/deployment-*`, `operations-runbook` and others | docs | Deployment / dependency changes | Deployment rehearsal |
| 6 | Architecture design decisions | Applicable | `docs/architecture.md` + internal ADRs | docs+internal | Major decisions | Review |
| 7 | Change, compatibility and release | Applicable | `docs/versioning.md` + root `CHANGELOG` | docs+root | Releases | Version consistency gate |

## Item-by-item notes and gaps

1. **In-code documentation**: `C-anp-P19` (`W-anp-P108`–`P112`) is **closed**. Measured baseline as of 2026-10-14 —
   **source layer**: 486 files, 33 findings (`HighRiskScriptCandidate` 29, `TodoOrDeferredMarker` **4**; the five categories
   `MissingNodeDocumentation`, mojibake / old bilingual / client-side HTML comments / low-value restatement are all at zero);
   **PowerShell layer**: 87 scripts, comment-based help **87/87**, 1689 standard `<lang>` blocks, legacy `.LANG` **0**,
   mixed-language surfaces **0**, 12 high-risk scripts with **0** missing the HIA marker — all gated by
   `Test-PortalRopCommentBaseline.ps1` (L0).
   - ⚠️ This line originally recorded "474 files / 35 findings / 5 to-dos / 2 missing node documentation", registered **at the
     time of v1.0.0** and since changed by the P19 governance; `W-anp-P123` refreshed it against measurement.
   - `HighRiskScriptCandidate` (29) is a candidate set generated **by filename keyword** and is **not a defect count**; it
     and the PowerShell layer's `RiskLevel=High` (12) are two different sets.
   - **Semantic boundary**: complete structural markers ≠ semantically sufficient comments. Semantic quality is carried by
     `C-anp-P25` (`W-anp-P113` onward) through manual sampling, and the first batch (security paths) is complete.
2. **API and technical contracts (boundary decision)**: this project is a **Web Forms portal (.NET FW4.8)** with **no
   external REST/GraphQL API** → "public REST/GraphQL API reference" is marked **not applicable** (reason: the architecture
   has no such external surface). But **module contracts / configuration contracts / enterprise capability object
   contracts** do exist → applicable. **`W-anp-P125` has created `docs/contracts/`**: `README.md` (boundary decision table +
   authoritative sources), `module-manifest.md` (the `module.json` contract) and `app-settings.md` (the `appSettings`
   contract), holding machine-readable contracts plus human-readable explanation.
   - **Corrected to the facts** (W125 measured `PortalModulePackage.cs`): `schemaVersion: 2` does **not** require a
     `capability` section — `capability` is **optional**, and a module that does not declare it is Unclassified (legal); the
     wording in `module-development-guide.md` that "the loader requires a capability block" is **inaccurate** and has been
     logged for `W-anp-P126` to review and fix.
3. **Product features and usage**: strengthened by `W-anp-P124` — added **`product-overview.md`** (product overview and
   positioning: what it is / who it is for / capability map / boundaries / compatibility / licence / maturity framing);
   `user-guide.md` gained **a typical portal configuration flow**, **common administrator operations** and
   **troubleshooting**, and the original "to be supplemented" list was tightened into an honest "not yet covered" list.
   **The new modules of `C-anp-P20` are registered in this family**: `user-guide.md` contains a "Business modules: leave
   requests and expense reimbursement" section (enabling prerequisites, fields and validation, to-do generation, FAQ).
   - **Remaining gaps** (registered honestly): runtime screenshots; item-by-item operating instructions for existing content
     modules (announcements, contacts, discussions, documents, events, images, links, etc.); the approval flow UI for leave
     and reimbursement (this cycle only persists data and generates to-dos; approval actions are not inside these modules).
4. **Developers and contributors**: `dev-guide`, `module-development-guide`, `frontend-asset-guide`,
   `theme-package-guide`, `theme-semantic-variables` and `theme-module-contract` are fairly complete. Of these,
   `theme-semantic-variables.md` is the output of `C-anp-P18` / `W-anp-P97` and registers the theme layer's `--portal-*`
   semantic variable matrix (7 themes × **14 variables** — the 14th, `--portal-text-subtle`, was added by the `C-anp-P23` D3
   ruling to restore the colour layering between subtitle and title); `theme-module-contract.md` is the output of
   `W-anp-P99` and registers the module appearance contract plus the `var()` fallback audit conclusions — the originally
   recorded **pending D3 decision** (the subtitle/title colour layering being flattened by the variable migration) was
   **ruled (b), adding `--portal-text-subtle` and landing it**, by `C-anp-P23`, and is no longer an open item; that cycle
   also brought the theme layer's module semantic class coverage to 100% across 8 modules × 7 skins and raised the coverage
   gap to a hard gate failure. Both take the CSS itself and the `W-anp-P100` gate as their authoritative source; the
   documents are extracted snapshots, and when stale the gate wins.
5. **Operations, deployment, security**: `deployment-checklist`, `deployment-default-credentials`,
   `deployment-rollback-guide`, `operations-runbook`, `audit-retention-policy`, `font-policy-and-audit` and
   `third-party-dependencies` are fairly complete.
6. **Architecture design decisions**: `architecture.md` + the two state-machine documents + internal ADRs / milestones.
7. **Change, compatibility and release**: `versioning.md` + `CHANGELOG` + `release-notes-template.md`.

## To be built (`W-anp-P124`–`P127`; formerly D3–D6, renumbered to W on 2026-10-14)

- ✅ ~~Documentation map / navigation~~ — **delivered**: `docs/documentation-map.md` (reader paths + 7-family index) and
  `docs/README.md` (entry navigation).
- Product introduction and positioning page — `W-anp-P124`
- Per-feature how-to / FAQ / troubleshooting — `W-anp-P124` (the two new `C-anp-P20` modules are **already synced** into
  `user-guide.md`; per-feature how-tos for the remaining existing modules are still missing)
- API / integration contract documentation (build `docs/contracts/` per the boundary decision) — `W-anp-P125`
- Consistency / version / link review of existing documentation — `W-anp-P126`
- Documentation verification gates (link check / example runs / bilingual consistency / readability walkthrough) —
  `W-anp-P127`
