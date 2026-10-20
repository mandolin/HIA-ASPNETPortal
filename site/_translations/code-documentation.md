---
layout: default
title: Code documentation
lang: en
parent: 开发
nav_order: 1
---

# Code documentation

This page explains the project's **code documentation system** — API-level documentation generated from source comments by
`HIA-Documentation-Sys` — together with where it is currently published and how to refresh it.

## System composition (HIA-Documentation-Sys)

| Half | Inputs | Artifact form | Current status |
| --- | --- | --- | --- |
| **JSDoc (JavaScript)** | `src/Portal/gulpfile.js`, `src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js` | **Bilingual HTML pages** + theme assets | ✅ **Published** — see "Published code documentation" below |
| **DotNetDoc (C# XML documentation)** | 4 XML documentation outputs such as `src/Portal/bin/Portal.xml`, plus `src/master.sln` | **Intermediate JSON** (14 artifacts / 60.8 MB, no rendering surface yet) | ⏸ **Not published** — will be added through the same manifest mechanism once a rendering/publishing component exists |

The two entry scripts (under `dev/scripts/`) stay isolated from each other and from the portal runtime, front-end
packages and existing Task Runner bindings:

- `Build-PortalDotNetDocPilot.ps1` — XML precheck → `npm ci` → `docs:build` → `docs:verify`;
- `Build-PortalJsdocPilot.ps1` — restore the isolated workspace dependencies → `docs:build` (`jsdoc -c jsdoc.conf.json`)
  → `docs:verify`.

## Published code documentation (JSDoc)

- Chinese: `/api/jsdoc/index.zh-CN.html`
- English: `/api/jsdoc/index.en.html`
- Default entry: `/api/jsdoc/index.html`

The artifact shape is guarded by a gate: no internal work-item numbers (`W-anp-P*` / `C-anp-P*`) and no local absolute
paths.

## Refresh procedure (snapshot mechanism)

The site is published from **committed static files**, so the code documentation is a **snapshot** rather than live
generation. To refresh:

1. Run `dev/scripts/Build-PortalJsdocPilot.ps1` (artifacts land in `temp/documentation/jsdoc`, which is git-ignored);
2. Copy the artifacts to `site/api/jsdoc/` and update `generatedUtc` and `sourceFingerprint` in `code-docs-manifest.json`
   in the same folder;
3. Commit and push.

The L0 gate `Test-PortalCodeDocs.ps1` checks the **source fingerprint** (SHA-256 over the two JSDoc input files) and the
artifact shape: if the sources changed but the snapshot was not refreshed, the gate fails.

## Dependency installation note (environment-specific)

The JSDoc pilot workspace (`dev/documentation/jsdoc`) pins the **official npmjs registry** through a workspace-level
`.npmrc`. The reason: `npm ci` rewrites tarball locations according to the currently configured registry, and some
mirrors return 400 for npm's tarball requests (while metadata requests succeed), which makes installation fail
inevitably. If the official registry is unstable on your network, switch to a local mirror — but that mirror must serve
tarballs correctly, otherwise the project convention treats it as a missing prerequisite and never skips silently.

## Related documentation

- Module and theme contracts: `contracts/module-manifest.md`, `contracts/app-settings.md`;
- Source comment conventions (bilingual `<lang>` blocks): see the design and development pages;
- Documentation family overview and gaps: `documentation-map.md`.
