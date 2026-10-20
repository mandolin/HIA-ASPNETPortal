---
layout: default
lang: en
parent: Reference
permalink: /en/documentation-artifacts-guide/
title: Documentation artefacts and toolchain boundaries
---
# Documentation artefacts and toolchain boundaries

This document describes the boundaries between HIA-ASPNETPortal's publicly maintained documentation, documentation inputs and
reproducible artefacts. The goal is that a maintainer can repeat existing verification **without** depending on a
**separate private repository**, the portal runtime, or local directories of other HIA projects.

## Public documentation and private material

- The root `README.md` and the Markdown under this directory are the main repository's publicly maintained documentation.

- Documentation may describe reproducible configuration using `{ExternalCfgPath}`, `$env:USERPROFILE`, template file names and
  logical connection-string names, but must not write real connection strings, passwords, tokens, cookies, certificates,
  production backups or machine-specific absolute paths.

## JavaScript documentation

`dev/documentation/jsdoc/` is a standalone HIA JSDoc pilot tool project. Its current scope reads only the tracked
`src/Portal/gulpfile.js` and `src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js`, to verify bilingual content,
source links, HIA metadata and integration JSON. Both inputs are on a controlled allow-list; adding or removing one must be
accompanied by a review of the configuration, the readiness gate and this document.

For how to run it see the [developer guide](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/dev-guide.md#javascript-documentation-pilot).
The first run executes `npm ci` in that tool directory; afterwards you can use the
`portal: build JavaScript documentation pilot` VSCode task or run `dev/scripts/Build-PortalJsdocPilot.ps1`.

The generated HTML, metadata and integration JSON are written only to the ignored `temp/documentation/jsdoc/`. These files are
local verification artefacts — not portal runtime input, not Gulp asset input, and not a public release artefact; do not copy
them into `src/Documentation/`, `src/DoxyGen/` or `src/Portal/Documentation/`, and do not commit them to the main repository.

## .NET DotNetDoc pilot

`dev/documentation/dotnetdoc/` is a standalone HIA DotNetDoc pilot tool project. Its `package.json` declares
`@hia-doc/dotnetdoc-runner@^0.1.8`, while the version-controlled lock file resolves to `0.1.8`; together they form the current
reproducible contract. The tool directory's local `.npmrc` uses the npm official registry, to avoid a mirror's metadata lag
making a version unavailable. The package does not reuse `src/Portal/package.json` and takes no part in Gulp, IIS Express, the
Visual Studio Task Runner or release-package builds.

The default configuration is the root-level `dotnetdoc.config.json`; the first round reads only the four Debug XML
documentation files, representative Web Forms markup comments, one Web Forms surface and solution/project discovery. The
generated DotNetDoc extraction, HIA document and producer result are written only to the ignored
`temp/documentation/dotnetdoc/` and must not be committed.

Run it with `dev/scripts/Build-PortalDotNetDocPilot.ps1` or the VSCode task `portal: build .NET DotNetDoc pilot`. By default
the script first calls `Test-PortalXmlDocumentation.ps1 -Build` to produce the XML documentation, then runs the runner and the
output checks; when Debug XML already exists, pass `-SkipXmlBuild` explicitly.

`dotnetdoc.api-only.config.json` is the troubleshooting fallback configuration, consuming only the four XML documentation
files. `dotnetdoc.source-probe.config.json` is a source-relation research configuration, used to observe how the C# source
extractor behaves when full project context is missing; it is not a default gate and does not block the portal documentation

## Coverage tiers

Documentation coverage is not expressed as a single percentage; it is split by risk and delivery value into
three tiers:

| Tier | Scope | Current requirement |
| --- | --- | --- |
| Required | Release, security, configuration, external connection strings, logging, audit, permissions, upload, path validation and public contracts. | When added or structurally changed, accurate bilingual XML/JSDoc comments must be added and the code must enter the relevant gate or evidence package. |
| Recommended | Main business services, business data objects, Admin key page code-behind, HIA boundary proofs and sample business flows. | Complete the semantic description when touched; historical code progresses gradually via touch-improve. |
| Deferred | Historical Web Forms page details, designer-generated members, old samples, temporary generated output and directories of undetermined ownership. | Not a current blocker; governed in batches once the toolchain and refactoring boundaries stabilise. |

`Get-PortalDocumentationBaseline.ps1` outputs an inventory, not a coverage score. It can record which areas contain XML
comments and which directories belong to generated-output boundaries, but it does not replace human judgement on whether the
comments are accurate.


## Generated directories and commit policy

The following directories or file types currently fall into generated output, historical output, or areas of undetermined
ownership:

- `temp/documentation/`: local verification output from the JSDoc and DotNetDoc pilots, protected by `.gitignore`.
- `bin/`: assembly and XML documentation output from Visual Studio and MSBuild, protected by `.gitignore`.
- `src/Documentation/`, `src/Portal/Documentation/`, `src/Portal.Components.Data/Documentation/`: historical SHFB/Doxygen
  generated output; currently not public documentation input and not a release directory.
- `src/DoxyGen/`, `src/Portal.shfbproj`: historical Doxygen/SHFB tool sites or configuration candidates; not currently part of
  a formal release package.

`portal: verify public documentation` checks that these unconfirmed generated directories have not entered Git-tracked files.
If versioned documentation ever needs to be published, ownership, licence, cleanup method, publication location, access
boundary and rollback plan must be defined first, and this guide, the public index and the gates must be updated.


## Local verification order

1. Run `portal: verify public documentation` to check public entry points, relative links, privacy and generated-directory
   boundaries.
2. To confirm the JavaScript documentation tool, run the JSDoc pilot; keep its output in the ignored directory.
3. To confirm C# XML output, run `portal: verify .NET XML documentation`; if necessary let that task build the Debug
   configuration first.
4. To confirm the HIA DotNetDoc output contract, run `portal: build .NET DotNetDoc pilot`.
5. To retain documentation evidence, run:

```powershell
dev/scripts/New-PortalDocumentationEvidencePackage.ps1
```

That evidence package orchestrates readiness, the documentation baseline, the public documentation gate, .NET XML
documentation verification, the JSDoc pilot and HIA notification reading. It does not modify source comments, does not commit
generated output, does not read sensitive configuration, and does not connect to a database.

These verifications are all local / VSCode gates. CI, online link monitoring, a documentation site, search and versioned
releases will be designed separately once requirements, secrets, caching, branching and release policy are settled.
