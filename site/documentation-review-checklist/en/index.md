---
layout: default
lang: en
parent: Reference (section)
permalink: /en/documentation-review-checklist/
title: Documentation review walkthrough checklist
---
# Documentation review walkthrough checklist

> This is a **manual walkthrough checklist** covering the part of documentation
> validation that "automation cannot prove".
> Automated gates prove only structure (links resolve, references exist, bilingual structure is complete, index coverage);
> **whether the semantics are correct, whether the examples actually run, and whether the wording misleads readers must be
> done by people**.

## 1. When to walk through

| Trigger | Walkthrough scope |
| --- | --- |
| A new or heavily revised public document | That document in full |
| Code changing an interface / configuration / process | **All** documents referencing it |
| Before a release | The product family (`product-overview.md`, `user-guide.md`) in full |
| A reader reports a document is wrong | That document + other documents with the same wording |

## 2. Walkthrough items

### A. Correctness (most important)

- [ ] **Paths, commands, parameters and key names** in the document match the current source (the paths in command examples are
      guaranteed to exist by `Test-PortalDocumentationExamples.ps1`, but **parameters and output need manual checking**)
- [ ] Cited **defaults / version numbers / counts** are not a copied old snapshot (were they updated when the code changed?)
- [ ] The stated **boundaries and "things we do not do"** still hold (for example, "no external REST API" — is that still
      true?)
- [ ] The **where-it-fits description** has not been inadvertently overstated (for example, do not write "first formal
      release" as "production ready")

### B. Bilingual equivalence

- [ ] The Chinese section and the `## English` section of the root `README.md` are **factually consistent** (version number,
      capability list and where-it-fits description aligned item by item)
- [ ] The `<lang>` blocks in code comments are **semantically equivalent between Chinese and English**, not a one-way
      translation (semantic equivalence, not word-for-word correspondence)
- [ ] Terminology is consistent (one concept does not mix several translated names)

### C. Reader reachability

- [ ] A new reader can find what they need via the **reader paths** in
      [documentation-map.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-map.md) without getting
      lost
- [ ] Links between documents form a closed loop; there is no "referenced but not indexed" orphan
- [ ] Examples **run when copied directly** (or their prerequisites are stated explicitly)

### D. Relationship to the gates (avoid over-trusting them)

- [ ] "The gate passed" has not been treated as evidence that "the document content is correct" — gates cover structure only
- [ ] If a document contains **teaching placeholders** (e.g. `HIA.YourModule`), the reader can recognise it as a placeholder
      rather than a real path

## 3. Already covered by automation (do not repeat manually)

| Check | Gate |
| --- | --- |
| Markdown relative link validity | `Test-PortalPublicDocumentation.ps1` |
| Public documentation index coverage | `Test-PortalPublicDocumentation.ps1` |
| Private-repository links / absolute paths / credential shapes (`docs/`) | `Test-PortalPublicDocumentation.ps1` |
| Private-repository names / credential shapes / absolute paths / internal plan paths (**site publication surface `site/`**) | `Test-PortalSitePublicSurface.ps1` |
| Command examples referencing existing script / SQL paths | `Test-PortalDocumentationExamples.ps1` |
| Root README bilingual structure | `Test-PortalDocumentationExamples.ps1` |
| Version consistency (CHANGELOG / git tag / assemblies) | `Test-PortalVersionConsistency.ps1` |
| Code XML documentation coverage | `Test-PortalXmlDocumentation.ps1` |
| Comment `<lang>` bilingual structure (in source) | `Test-PortalRopCommentBaseline.ps1` |

## 4. Recording requirements

Issues found in a walkthrough are handled in two categories:

1. **Immediately fixable** (typos, stale numbers, broken links) → fix directly and explain it in the commit message;
2. **Needs a decision or out of scope** → register as a deferred item, stating the **owner, the decision basis and the
   re-entry condition**; never shelve it silently.

Walkthrough conclusions (including "no issues") should be recorded in the cycle's **internal completion record** — **never
do the work without recording it**.
