---
layout: default
lang: en
parent: Changes
nav_order: 1
permalink: /en/versioning/
title: Versioning policy
---
# Versioning policy

This document defines HIA-ASPNETPortal's **product version scheme**. A version number is both a release anchor and the
carrier of the criteria for "publicity / first-release milestone".

## 1. Version numbering rules (SemVer)

`MAJOR.MINOR.PATCH` is used, with pre-release suffixes `-alpha.<n>` / `-beta.<n>` / `-rc.<n>`.

| Segment | When to increment |
| --- | --- |
| `MAJOR` | A change in the external compatibility commitment, or a milestone-level leap (e.g. first release `v1.0.0`, production-ready `v2.0.0`) |
| `MINOR` | A new capability, or completion of a Cycle's thematic goal (e.g. initial completion of a capability layer) |
| `PATCH` | Bug fixes, documentation and metadata corrections, with no change to external behaviour |
| Pre-release | Release candidate: `-rc`; internal validation: `-alpha` / `-beta` |

## 2. Relationship between versions and milestones

| Version | Milestone meaning |
| --- | --- |
| `v0.1.0` | Closed-loop baseline for the mechanism (the four layers capability ↔ module ↔ Profile ↔ permission connected and verifiable locally) |
| `v0.2.0` – `v0.6.0` | Evolution versions on the way to the first release |
| **`v1.0.0`** | **First formal release** — capability coverage, quality gates, documentation and open-source readiness all complete |
| `v2.0.0` (reserved) | The production-ready version, once real-environment validation evidence is obtained |

**Criteria must never be relaxed because a version advanced**: replacing "gather evidence" with "change the criteria" is not
allowed.

## 3. Linking versions to assembly versions

- Product version `vX.Y.Z` corresponds to assembly version `X.Y.Z.0`.
- `AssemblyVersion`, `AssemblyFileVersion` and `AssemblyInformationalVersion` stay consistent across all three.
- When the version changes, these must be updated together: the `CHANGELOG.md` entry, the `git tag`, and the assembly version.
- Consistency is enforced by `dev/scripts/Test-PortalVersionConsistency.ps1` (see section 5).

## 4. Release process

1. Confirm the milestone criteria for the version are met (see the corresponding Cycle document).
2. Update `CHANGELOG.md`: turn `Unreleased` into a version entry and fill in the date.
3. Synchronise the assembly version (all three fields).
4. Run the consistency and build gates (build with 0 errors and 0 warnings, unit tests passing).
5. Tag: `git tag -a vX.Y.Z -m "..."`, then push with `git push origin vX.Y.Z`.
6. Product version tags are created **in the main repository only**; the private repository carries no product version tags.

## 5. Consistency gate

`dev/scripts/Test-PortalVersionConsistency.ps1` verifies that:

- the latest `CHANGELOG.md` entry version equals the latest `git tag` version;
- the assembly versions (five `AssemblyInfo.cs` files) match that version;
- duplicate tagging is blocked when the version has not changed.

## 6. Current version

**`v1.2.0`** (2026-10-22, current) — verification and release mechanics were closed out: post-publish live re-check, build stamps with a precise propagation criterion, handwritten page freshness, build rehearsal and output redirection switches, a local injection-track tool, and the release-readiness gate. The previous version `v1.1.0` (2026-10-17) — the product website and documentation subsite were overhauled. The first formal
release `v1.0.0` (2026-10-09) completed capability coverage, quality gates, documentation and open-source readiness;
the external wording remains limited to "development, testing, demonstration and reference" (see section 6).
See `CHANGELOG.md` for details.

## 7. `minimumPortalVersion` semantics in module manifests

In a module package's `module.json`, `minimumPortalVersion` expresses the **minimum portal-kernel capability baseline that
the module requires**. It is a **separate dimension** from the product version (`MAJOR.MINOR.PATCH`):

- the product version evolves with releases (see section 2) and may be incremented by any change;
- `minimumPortalVersion` is the module's **compatibility declaration** about its host portal, and needs to be raised only
 when the portal kernel introduces an **incompatible change**.

**Current disposition (2026-09-26)**: the `"1.0"` values written in historical `module.json` files are **early placeholder
values** and do **not** mean the product has released `v1.0.0`. To avoid the ambiguity of "requires 1.0 while the product is
the portal-kernel capability baseline is not the same dimension", the convention is:

1. before the first release (`v1.0.0`), new modules uniformly use a value consistent with the current portal-kernel
 baseline;
2. the portal kernel's compatibility baseline is maintained by this document, and a module's value must be traceable to a
 meaning here;
3. validation behaviour (whether loading is forcibly blocked) is defined by the module-loading logic; this document defines
 the semantics only.

<!-- build-stamp: 1A55CF994A1B -->
