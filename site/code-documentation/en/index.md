---
layout: default
lang: en
parent: Develop
nav_order: 6
permalink: /en/code-documentation/
title: Code documentation
---
# Code documentation

This page explains the project's **code documentation system** — API-level documentation generated from source comments by
`HIA-Documentation-Sys` — together with where it is currently published and how to refresh it.

## System composition (HIA-Documentation-Sys)

| Half | Inputs | Artifact form | Current status |
| --- | --- | --- | --- |
| **JSDoc (JavaScript)** | `src/Portal/gulpfile.js`, `src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js` | **Bilingual HTML pages** + theme assets | ✅ **Published** — see "Published code documentation" below |
| **DotNetDoc (C# XML documentation)** | 4 XML documentation outputs such as `src/Portal/bin/Portal.xml`, plus `src/master.sln` | **Intermediate JSON** (14 artifacts / 60.8 MB, no rendering surface yet; that figure is one measurement taken when the generation pipeline was first proven and may differ on a re-run) | ⏸ **Not published** — will be added through the same manifest mechanism once a rendering/publishing component exists |

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

## Published code documentation (DotNetDoc)

- Entry: `/api/dotnetdoc/` (**multi-page site**: the entry page is a navigation shell with a type tree and search; every type/member gets its own page)
- Coverage: **four API projects** (`Portal` / `Portal.Components` / `Portal.Components.Data` / `Portal.Components.Data1`) and the **markup comments**; the source comments follow the bilingual `<lang><zh-CN>…<en>…</lang>` convention.
- **[Measured coverage, reviewed 2026-10-22] this is *partially* bilingual, not fully bilingual**: about **62% of the text spans on the Chinese pages actually contain Chinese**; for the remaining ~38% of members only an English comment exists, and **the Chinese page shows the English text verbatim**. Per half: `markup` 51.2%, `api/Portal` 61.0%, `api/Portal.Components` 62.7%, `api/Portal.Components.Data` 66.7%, `api/Portal.Components.Data1` 67.9%. **This is the honestly recorded state rather than a defect** — adding Chinese comments is a separate piece of work (it changes C# source comments, out of scope for this cycle).
- **The surface and project-discovery halves are English-only**: they are structural descriptions (route inventory, solution composition), not human-written comments, so there is no Chinese content. **Published as English honestly, rather than fabricating Chinese directories to look bilingual.**
- Size: about 91 MB of rendered artifacts (11475 files / 5366 entry pages). The single-page era was about 16.8 MB (15 huge pages, 4.58 MB for the Chinese Portal page alone): **total bytes grew 5x, but per-load cost dropped from MB to KB** — the latter is what readers actually feel.
- Refresh: `dev/scripts/Publish-PortalDotNetDoc.ps1` (first run `Build-PortalDotNetDocPilot.ps1` to produce the HIA artifacts; this script then renders **once via `--project-manifest` into a multi-page site** under `site/api/dotnetdoc/`).

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

<!-- build-stamp: 137E0219D5F6 -->
