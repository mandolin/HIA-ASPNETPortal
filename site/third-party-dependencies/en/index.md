---
layout: default
lang: en
parent: 参考
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
| `System.Data.SQLite.Core` | `1.0.119.0` | The .NET Framework 4.8 SQLite ADO.NET capability proof for . | The package metadata links to the SQLite Public Domain copyright notice; its net46 build target is also declared Public Domain. | Used only by the standalone proof project; it does not enter the portal's main business runtime path. |

## Usage and distribution constraints

1. The proof project restores packages with `packages.config`; `src/packages/` remains excluded by the existing ignore rules,
   so no binary packages are committed.
2. SQLite runtime files land in `temp/provider-proof/` with the proof output; they never enter the site's `bin`, a release
   package, or the portal's normal startup path.
3. Generated `.sqlite` files are for development/test only and are recreated by default by the scripts; they must not be
   placed in the repository, internal directories, or external production configuration directories.
4. If SQLite, MySQL, PostgreSQL or other providers are ever used for real portal capabilities, a corresponding audit of
   version, licence, native runtime and deployment notes must be added, together with a standalone ADR and regression run.

## External references

1. [System.Data.SQLite.Core 1.0.119 NuGet package page](https://www.nuget.org/packages/System.Data.SQLite.Core/1.0.119)
2. [SQLite copyright and licence](https://www.sqlite.org/copyright.html)
