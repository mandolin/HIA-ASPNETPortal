---
layout: default
lang: en
parent: Reference
nav_order: 29
permalink: /en/contracts-readme/
title: Technical contracts
---
# Technical contracts (HIA-ASPNETPortal)

> This directory holds this project's **machine-readable contracts plus human-readable
> explanation**, corresponding to the "API and technical contracts" documentation family. The authoritative source is always
> **the code and configuration files themselves**; the explanations here are extracted snapshots — **when the two conflict,
> the source wins**.

## 1. Boundary decision (read this first)

This project is an **ASP.NET Web Forms portal (.NET Framework 4.8)** with **no external REST / GraphQL API** and provides no
machine-readable interface contract of the OpenAPI kind.

Therefore:

| Common "API documentation" form | Applicable here? | Reason |
| --- | --- | --- |
| Public REST API reference | **Not applicable** | The architecture has no such external surface |
| GraphQL schema reference | **Not applicable** | Same |
| OpenAPI / Swagger | **Not applicable** | Same |
| **Module contract** | **Applicable** | Trusted module packages declare it in `module.json`, with schema validation |
| **Configuration contract** | **Applicable** | The `appSettings` flat dotted-key family |
| **Enterprise capability object contract** | **Applicable** | Capability keys and roles form a closed vocabulary |

The full boundary decision is registered in
[documentation-registry.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-registry.md), family 2.

## 2. Contract list

| Contract | Authoritative source (machine-readable) | Human-readable explanation |
| --- | --- | --- |
| **Module contract** | `DesktopModules/<module>/module.json`; validation in `src/Portal/Components/PortalModulePackage.cs` | [module-manifest.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/contracts/module-manifest.md) |
| **Configuration contract** | `<appSettings>` in `src/Portal/Web.config`; key constants `src/Portal/Components/PortalSettingKeys.cs`; environment overrides `src/Portal/Config/appSettings*.json` | [app-settings.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/contracts/app-settings.md) |
| **Enterprise capability object contract** | Capability registry `src/Portal.Components/PortalCapabilityRegistry.cs`; role vocabulary `src/Portal.Components/PortalCapabilityRoles.cs` | The "Enterprise capability (capability)" section of [module-manifest.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/contracts/module-manifest.md) |

## 3. Shared constraints

1. **Trusted boundary**: module packages follow a "trusted deployment" model — only controlled ASCX entry points and in-site
 static resources are allowed; a manifest declaring `script` / `scripts` / `externalUrl` / `externalUrls` / an assembly / a
 package URL is **explicitly rejected** — a manifest declaration cannot change execution or download capability.
2. **Defaults are taken from the source**: this directory does not copy default values (they would go stale). For defaults,
 look at `<appSettings>` in `src/Portal/Web.config` and the per-environment `appSettings*.json`.
3. **Configuration effective sources**: `Web.config` holds the base values; only an `appSettings.json` **deployed to the site
 root directory** overrides keys of the same name (silently skipped if absent). See item 4 of "Hidden traps when adding a
 module" in [module-development-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md).
4. **Contract changes must be synced**: when changing a contract implementation, update the corresponding explanation in this
 directory and constant sources such as `PortalSettingKeys.cs`; the verification method is to run the gate suite
 (`dev/scripts/Invoke-PortalGateSuite.ps1`).

## 4. Verification

- Machine validation of the module contract: `dev/scripts/Test-PortalNewModuleReadiness.ps1` and
 `Test-PortalBusinessModuleSmoke.ps1` (L0).
- Asset and theme contracts: `Test-PortalModuleSkinCoverage.ps1` (L0).
- For concrete commands see
 [testing-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/testing-checklist.md).

<!-- build-stamp: BC1D38097435 -->
