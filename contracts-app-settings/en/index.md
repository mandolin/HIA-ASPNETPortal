---
layout: default
lang: en
parent: Reference
nav_order: 13
permalink: /en/contracts-app-settings/
title: Configuration contract (appSettings)
---
# Configuration contract (`appSettings`)

> Authoritative sources: `<appSettings>` in `src/Portal/Web.config` (base values); key constants are defined in several
> places, with the main key family in `src/Portal/Components/PortalSettingKeys.cs` and `PortalModuleProfileResolver.cs`;
> environment overrides are in `src/Portal/Config/appSettings*.json`. This page is an extracted snapshot — **when the two
> conflict, the source wins**.

## 1. Effective mechanism (read this first)

Configuration is read in two layers, decided by **whether the file exists in the site root directory**:

1. **Base values**: `<appSettings>` in `Web.config` always takes effect first;
2. **Override values**: `AppSettingsLoader` writes the file's `appSettings` keys into
 `ConfigurationManager.AppSettings` (overriding keys of the same name) **only when the JSON file it points at actually
 exists in the site root directory**; **if the file is absent it is silently skipped**.

Two practical consequences follow:

- Changes to the uncommitted `src/Portal/Config/appSettings.dev.json` on a development machine **do not take effect** — the
 local runtime reads the existing `Web.config` values;
- In a deployment environment, the file's overrides take effect **only once** `appSettings.json` is placed in the site root
 directory.

Key names are **flat dotted keys** (e.g. `Portal.ModuleProfiles.Active`). Assigning with `$obj.'a.b.c'` directly on the
configuration object is treated as nested path navigation and throws; keys must be handled literally.

For the full mechanism and troubleshooting see item 4 of "Hidden traps when adding a module" in
[module-development-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md).

## 2. Key families

Default values are **not copied here** (they would go stale) — for defaults see `<appSettings>` in `src/Portal/Web.config` and
the per-environment `appSettings*.json`.

| Key family | Representative keys | Purpose | Constant / read location |
| --- | --- | --- | --- |
| Theme | `Portal.Theme.Name` | Current skin name | `PortalSettingKeys.ThemeName` |
| Navigation | `Portal.Navigation.TabGateEnabled` | Tab visibility switch (enabled only by an explicit true value) | `PortalNavigationVisibilityPolicy.IsTabGateEnabled`; read at `DesktopPortalBanner.ascx.cs` |
| Module profile | `Portal.ModuleProfiles.Active` | Currently active profile name | `PortalModuleProfileResolver` (`ActiveProfileKey`) |
| Module profile | `Portal.ModuleProfiles.<Profile>.Packages` | Packages included in that profile | `PortalModuleProfileResolver` (prefix `Portal.ModuleProfiles.`) |
| Module profile | `Portal.ModuleProfiles.<Profile>.Includes` | Other profiles that profile **includes** (composite profiles) | Same |
| Module package enablement | `Portal.ModulePackages.Enabled` | Cross-profile deployment package enablement (profile-independent) | `PortalModuleProfileResolver` (`EnabledPackagesKey`) |
| Diagnostics | `Portal.Diagnostics.LogDirectory` / `EnableDetailedErrors` / `MaxFileBytes` / `RetentionDays` / `AllowAdminDetailView` | Log directory, detailed errors, per-file size limit, retention days, admin detail view | `PortalSettingKeys` |
| Documents | `Portal.Documents.AllowedExtensions` / `MaxUploadBytes` | Upload extension allow-list and size limit | `PortalSettingKeys` |
| Security | `Portal.Security.AllowSelfRegistration` / `RequireRegistrationApproval` / `RequireEncryptedLoginPassword` | Self-registration, registration approval, encrypted login password submission | `PortalSettingKeys` |
| Password policy | `Portal.Security.Password.MinimumLength` / `RequiredCategoryCount` / `WeakDictionaryEnabled` / `DisallowContextTerms` / `IterationCount` | Password length, character category count, weak dictionary, context-term blocking, PBKDF2 iteration count | `PortalSettingKeys` |
| Cookie | `Portal.Security.Cookies.Secure` / `SameSite` | Cookie security attributes | `PortalSettingKeys` |
| Registration | `Portal.Registration.InviteDefaultExpiryDays` / `AllowPendingEmployeeBinding` | Invite default expiry, pending employee binding | `PortalSettingKeys` |
| Instance | `Portal.Hia.InstanceId` | Portal instance identifier | `PortalSettingKeys` |

## 3. Module profiles vs package enablement (easily confused, listed separately)

The two mechanisms are **independent of each other** and easy to conflate:

| Mechanism | Key | Semantics |
| --- | --- | --- |
| **Profile** | `Portal.ModuleProfiles.Active` + `Portal.ModuleProfiles.<Profile>.Packages` | Decides "which packages this profile includes"; a composite profile can pull in other profiles via `.Includes` |
| **Package enablement (Enabled)** | `Portal.ModulePackages.Enabled` | **Profile-independent**; used to enable deployment packages across profiles. Business modules (e.g. leave / expense reimbursement) are not enabled by the core profile by default and must be added here |

> Practical point: when adding a business module **both places must change** — the package must be added to the `Enabled`
> allow-list, and its profile must be activated by `Active` or pulled in via `.Includes`. If a change does not take effect,
> check the "effective mechanism" in section 1 first.

## 4. Related documents

- Operating steps for enabling business modules: the "Business modules: leave requests and expense reimbursement" section of
 [user-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/user-guide.md).
- Deployment configuration: [deployment-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-checklist.md).
- External connection strings (**not committed**): see
 [dev-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/dev-guide.md) and
 `src/Portal/Config/Templates/connectionStrings.config`.

<!-- build-stamp: C517644EEB2F -->
