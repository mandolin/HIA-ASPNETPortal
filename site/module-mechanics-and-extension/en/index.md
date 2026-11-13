---
layout: default
lang: en
parent: Reference
nav_order: 2
permalink: /en/module-mechanics-and-extension/
title: Module mechanics and secondary development
---
# Module mechanics and secondary development: how a module gets onto the portal

This page is the **overview**: a map of the layers a module passes through to appear on a page, where each layer's configuration lives, and which constraint will send your change back. Every layer has its own page; this one gives you the skeleton.

## Five layers

| Layer | What it is | Where it lives | Who normally changes it |
| --- | --- | --- | --- |
| Module package | A deployable module unit with a manifest, `module.json` | `src/Portal/DesktopModules/<name>/module.json` | Developers |
| Profile | The list of packages a profile lets through | `Portal.ModuleProfiles.*` in `src/Portal/Config/appSettings*.json` | The deployment side |
| Module type definition | The **type** of a module: identity, source file, title | Table `PortalCfg_ModuleDefinitions` | Administrators / deployment |
| Module instance | One **mounting** of a definition on a tab, with title, order and editor roles | Table `PortalCfg_Modules` | Administrators ("Module settings") |
| Tab | The container for navigation and content | Table `PortalCfg_Tabs` | Administrators ("Tab name and layout") |

The manifest looks like this (the field names are fixed):

```json
{
  "schemaVersion": 2,
  "packageId": "HIA.Help",
  "displayName": "帮助与说明 / Help",
  "version": "1.0.0",
  "minimumPortalVersion": "2.0.0",
  "desktopEntry": "DesktopModules/Help/Help.ascx",
  "resources": []
}
```

`packageId` is the package's identity; profiles, gates and registries all reconcile against it.

## Profiles decide what may run

- `Portal.ModuleProfiles.Active` selects the profile in force (for example `CoreOnly` or `BusinessWorkflow`).
- `Portal.ModuleProfiles.<profile>.Includes` names the other profiles this one **includes**.
- `Portal.ModuleProfiles.<profile>.Packages` names the packages this profile **lets through**.

A package not covered by any profile in force does not render. A profile is a group switch, not a per-module tick list.

## Adding a module of your own

1. **Write the module**: the control (`*.ascx`) plus the manifest (`module.json`). Inside the control, do not read raw configuration keys and do not decide permissions in markup.
2. **Register the type definition**: its identity and source file (administration "Module type definitions", or the database at deployment time).
3. **Mount an instance**: put an instance on the target tab with a title, an order and its editor roles (administration "Module settings").
4. **Confirm profile coverage**: add the `packageId` to the `Packages` of the profile in force; a package you do **not** intend to mount has to be registered in `notMounted`, or the package-contract gate will report it as "covered by no profile".

A complete reproducible walkthrough of these four steps from scratch (a `module.json` example, build-item registration, Register / mounting an instance, gate commands and runtime confirmation) is in the "End-to-end example" section of [module-development-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md).

## Changing the look

The portal ships **eight themes** (Enterprise / StateClassic / Oa in Light and Dark, plus Default and ThemeProbe). Theme packages target these scope classes:

> Terminology: eight is the **number of directories** under `App_Themes/`. When adding a visible element that needs theme-layer scope rules, count **7 themes** (six official skins plus the `Default` fallback) — `ThemeProbe` is a diagnostic probe and is explicitly excluded. Both `dev/scripts/Test-PortalModuleSkinCoverage.ps1` and `Test-PortalNavigationGroupRender.ps1` assert against 7 skins, so the two numbers do not conflict.

| Class | Meaning |
| --- | --- |
| `portal-theme-{name}` | The theme in force |
| `portal-tab-{tab id}` | The tab in force |
| `portal-module-{instance id}` | The module instance in force |
| `portal-pane-{pane name}` | The pane it sits in (left / content / right) |

The hard constraints on theme packages (stated in the comments of `Default/Default.css`): **no remote resources, no theme scripts, no CSS variables, no Grid and no Flexbox** — this project supports IE9, and colours must be written as two declarations with the explicit hex first and the variable second.

## Four gates are watching

| Gate | What it watches |
| --- | --- |
| Module package contract (M1–M5) | Whether package registration and profile coverage are self-consistent: every `Enabled` entry must be registered first; take the union of all profiles' `Packages`; every `module.json` `packageId` must be covered by "profile union / Enabled / notMounted"; a `notMounted` package must not also be covered by a profile; `appendAllowList` and `notMounted` must not overlap |
| Resource contract (C1–C6) | The key sets of the neutral / Chinese / English resx files agree; strongly typed properties and keys correspond **one to one, both ways**; every `<summary>` carries both locale blocks; English must not drift; neutral values must not contain CJK; every key referenced in markup or code must exist |
| Accessibility | `alt` / `label` / heading semantics in markup |
| Legacy CSS | The IE9 base path (no Grid / Flexbox / `var()` fallback) and two-declaration colours |

The resource contract has one trap worth remembering: **key drift fails silently**. When a key is missing from the resource file, `GetString` returns `null` instead of throwing, so the interface shows blank text with no trace in the log. That is why key existence is enforced by a gate rather than left to exceptions.

## Where the API documentation is

- `/api/dotnetdoc/`: **source-level** API index (types, members, source lines), generated from the XML documentation comments.
- `/api/jsdoc/`: API documentation for the front-end scripts.

Both are static artifacts of the site, regenerated by the publishing script with its snapshot committed; change a source comment without refreshing the snapshot and the code-documentation gate reports a source-fingerprint mismatch.

## Developer path map

The sections above answer "how a module gets onto the portal", but three other reference-track questions live on different pages: where the code is, how a request travels, and in what order to read things when changing the system. This section joins them into a map — **navigation only; it does not repeat the specialist pages**.

**Division of responsibility (the two tracks do not repeat each other)**: the Learn track answers "how do I use it, how do I complete a task"; the Reference track answers "why does it run this way, where do I change or extend it". This page and every link below belong to the latter, so no operating steps or UI screenshots belong here.

| What you want to understand | Read first | Then read | Source of truth |
| --- | --- | --- | --- |
| Code structure / solution layout | "Solution structure" and "Web project structure" in [the architecture overview]({{ site.baseurl }}/en/architecture/) | Environment, build and debugging in [the developer guide]({{ site.baseurl }}/en/dev-guide/) | `src/master.sln`, `src/Portal/Portal.csproj` |
| How one request reaches module rendering | "Five layers" and "Profile" on this page | "Two-level navigation groups" and "Independent modules" in [the developer guide]({{ site.baseurl }}/en/dev-guide/) | `Global.asax.cs` (per-request culture and profile), `DesktopPortalBanner.ascx` (group rendering), `PortalCfg_*` tables |
| Process logic / state transitions | [Business application state machine]({{ site.baseurl }}/en/business-application-state-machine/) and [collaboration item state machine]({{ site.baseurl }}/en/collaboration-item-state-machine/) | [Sample business flow]({{ site.baseurl }}/en/p12-sample-business-flow/) | `src/Setup/PortalBiz_*.sql` and the migration tables |
| Adding a module from scratch | The four steps in "Adding your own module" on this page | "End-to-end example" in [the module development guide]({{ site.baseurl }}/en/module-development/) | `module.json`, `Portal.csproj`, the two smoke gates |
| Contract and configuration boundaries | [Contract overview]({{ site.baseurl }}/en/contracts-readme/), [manifest contract]({{ site.baseurl }}/en/contracts-module-manifest/), [application settings contract]({{ site.baseurl }}/en/contracts-app-settings/) | [Theme package guide]({{ site.baseurl }}/en/theme-package-guide/) and [frontend asset guide]({{ site.baseurl }}/en/frontend-asset-guide/) | `src/Portal/Config/appSettings*.json` |
| Code documentation and snapshots | [Code documentation explainer]({{ site.baseurl }}/en/code-documentation/) | "Where the API docs are" on this page | `/api/jsdoc/`, `/api/dotnetdoc/` |

**Verification order after changing the system** (aligned with [the testing checklist]({{ site.baseurl }}/en/testing-checklist/), not a second workflow): L0 static gates first (contracts, resources, navigation shape, code-doc source fingerprint), then L1 build and unit tests, then L2 runtime evidence. **Running only the build and unit tests without looking at the live page can ship a module that compiles but renders blank** — a lesson this project has learned repeatedly.

**Navigation baseline versus optional-module increments (factual wording)**: the tab list in the site documentation describes the **baseline shape** of `src/Setup/Portal_NavigationSeed.sql`, checked item by item against the baseline document by `Test-PortalNavigationShape.ps1`; tabs added by optional modules must **not** be written into that baseline narrative (the gate would fail). For example, running `src/Setup/PortalCfg_FormBuilder_Provision.sql` to install an independent module adds a `Form Builder` tab. That is a **deployment-time increment**, not a baseline definition; understand it via the provisioning script and the "Five layers" section above. Likewise, the signed-in display order follows the group vocabulary's `SortOrder` (see "Two-level navigation groups" in the developer guide), which is not the same thing as the flat `TabOrder` in the seed — neither number should be read as the other's definition.

## What this page does not cover

- The full criteria of the package contract → [the module manifest contract]({{ site.baseurl }}/en/contracts-module-manifest/)
- The exact boundaries and registration steps of module development → [the module development guide]({{ site.baseurl }}/en/module-development/)
- How to write a theme package → [the theme package guide]({{ site.baseurl }}/en/theme-package-guide/) and [the theme module contract]({{ site.baseurl }}/en/theme-module-contract/)
- How tab visibility is configured → [application settings contract]({{ site.baseurl }}/en/contracts-app-settings/)

<!-- build-stamp: 7443AE9F3C06 -->
