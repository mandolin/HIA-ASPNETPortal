---
layout: default
lang: en
parent: Reference
nav_order: 23
permalink: /en/third-party-dependencies/
title: Third-party dependency audit
---
# Third-party dependency audit

This document records third-party dependencies newly introduced or upgraded **after** this project established its AI
collaboration governance. Historical dependencies will be filled in gradually in later documentation cycles; the absence of
an old package from this list does **not** mean its licence has been audited.

## Dependency list

| Dependency | Locked version | Purpose | Licence / basis | Current boundary |
| --- | --- | --- | --- | --- |
| `System.Data.SQLite.Core` | `1.0.119.0` | Used to verify SQLite ADO.NET capability under .NET Framework 4.8. | The package metadata links to the SQLite Public Domain copyright notice; its net46 build target is also declared Public Domain. | Used only by the standalone proof project; it does not enter the portal's main business runtime path. |

## Usage and distribution constraints

1. The proof project restores packages with `packages.config`; `src/packages/` remains excluded by the existing ignore rules,
 so no binary packages are committed.
2. SQLite runtime files land in `temp/provider-proof/` with the proof output; they never enter the site's `bin`, a release
 package, or the portal's normal startup path.
3. Generated `.sqlite` files are for development/test only and are recreated by default by the scripts; they must not be
 placed in the repository, internal directories, or external production configuration directories.
4. If SQLite, MySQL, PostgreSQL or other providers are ever used for real portal capabilities, a corresponding audit of
 version, licence, native runtime and deployment notes must be added, together with a standalone ADR and regression run.

## Machine-readable baseline and change rules

`dev/dependency-baseline.json` is the machine snapshot of the **audited dependency set** (every `packages.config`
declaration plus the development-time dependencies listed above). Any addition, removal or version change in that set
must be synchronised in three places: ① registration in this file; ② a re-assessment of the vulnerability surface (the
L0 gate scans the NuGet vulnerability data source automatically); ③ a deliberate update of the baseline. Missing any one
of them makes the gate `Test-PortalDependencyVulnerabilityBaseline.ps1` fail (default-deny: unaudited dependency changes
are not allowed).

## External references

1. [System.Data.SQLite.Core 1.0.119 NuGet package page](https://www.nuget.org/packages/System.Data.SQLite.Core/1.0.119)
2. [SQLite copyright and licence](https://www.sqlite.org/copyright.html)

<!-- build-stamp: 2CE69C8EEAC5 -->
