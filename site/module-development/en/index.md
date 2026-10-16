---
layout: default
lang: en
parent: Develop
permalink: /en/module-development/
title: Deployed module package development guide
---
# Deployed module package development guide

## Scope

New business modules use the **trusted deployed module package** mechanism. It keeps the ASP.NET Web Forms and `.ascx`
user-control route: module source files are written into the site by a trusted development, build and deployment pipeline,
and administrators only register and enable modules from verified packages.

The current version does **not** support background ZIP/DLL upload, online compilation, online editing of module files,
external URLs, remote downloads, automatic JavaScript loading, or database migrations shipped with a package. Existing
flat `DesktopModules/*.ascx` and `Admin/*.ascx` files continue to run as Legacy modules; bulk migration is not required.

## Layout and manifest

A new module package lives at `src/Portal/DesktopModules/{PackageFolder}/`, for example:

```text
DesktopModules/
  ModuleProbe/
    ModuleProbe.ascx
    ModuleProbe.ascx.cs
    ModuleProbe.ascx.designer.cs
    module.json
    Styles/
      ModuleProbe.css
```

`PackageFolder` must match `^[A-Za-z][A-Za-z0-9_-]{0,63}$`. For read-only reference packages (such as `ModuleProbe`), the
`module.json` under `moduleFolder` currently uses `schemaVersion: 1`:

```json
{
  "schemaVersion": 1,
  "packageId": "HIA.ModuleProbe",
  "displayName": "模块验证 / Module Probe",
  "version": "1.0.0",
  "minimumPortalVersion": "1.0",
  "desktopEntry": "DesktopModules/ModuleProbe/ModuleProbe.ascx",
  "resources": [
    "Styles/ModuleProbe.css"
  ]
}
```

> **Current state of `schemaVersion` and `capability`**: read-only reference packages such as `ModuleProbe` use `1`; **all business modules (`HIA.*`) actually have
> manifest `2`**. The `capability` block is **introduced by `schemaVersion: 2` but is optional** — when undeclared the module
> is Unclassified (legal), not missing. Once declared, `capabilityId` must resolve in the capability registry and `role` must
> belong to the closed five-value vocabulary (`Primary` / `Contributor` / `Projection` / `Adapter` / `Block`). `MyWorkItems`
> is in fact `schemaVersion: 2` with no `capability` block, which is a legal shape.
>
> **Gate wording has been calibrated** (C-anp-P19 / P21): `Test-PortalBusinessModuleSmoke.ps1` no longer requires "always
> 1" but decides per scenario — `2` means a compliant business module; `1` with no `capability` means a compliant read-only
> reference package. **Do not** downgrade a business module to 1 just to pass a gate, and do not bolt a `capability` block
> onto a module that does not need one just to "look complete".

The rules are:

1. `packageId` must match `^[A-Za-z][A-Za-z0-9_.-]{0,99}$` and stay unique among deployed packages.
2. `desktopEntry` must be an existing `.ascx` file inside the current package directory and must pass the portal's existing
   `DesktopModules/` path validation.
3. `resources` must be existing relative files inside the package directory; `.css`, `.png`, `.jpg`, `.jpeg`, `.gif` and
   `.webp` are allowed. Only declared CSS is automatically de-duplicated and mounted by the portal host.
4. The manifest must not contain `script`, `scripts`, `externalUrl`, `externalUrls`, `assembly`, `assemblies` or
   `packageUrl`. Those capabilities need a future trusted-deployment mechanism, designed and reviewed separately.
5. A new module should inherit `PortalModuleControl<T>` and implement `IPortalModuleControl`. When public APIs,
   configuration, security boundaries or complex flows are involved, use the bilingual Chinese/English paragraphs inside
   standard XML comments.

## Code skeleton

The entry control's directive line uses the **fully qualified class name** (namespace `ASPNET.StarterKit.Portal`):

```aspx
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileConfirm.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileConfirm" %>
```

> **The fully qualified name is mandatory.** With only the class name in `Inherits`, the markup cannot find the class at
> runtime and throws `CS0103`; **MSBuild does not surface this error** (the code-behind is compiled separately into
> `bin/Portal.dll`). Such problems can only be caught by runtime gates — see the
> [developer guide](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/dev-guide.md) and
> `Test-PortalAscxCompilationContract.ps1`.

Comments in markup and code-behind use bilingual blocks:

```aspx
<%--
    <lang>
        <zh-CN>说明：员工只确认自己当前绑定的低敏资料，不提供上传或外部资源。</zh-CN>
        <en>Note: employees only confirm their currently bound low-sensitivity profile data; no upload or external resource capability is provided.</en>
    </lang>
--%>
```

## Permissions and data scope

Permission keys are defined centrally in `src/Portal.Components/PortalPermissions.cs`, using dotted naming
(`domain.object.action`):

```csharp
public const string SettingsView = "Settings.View";
public const string SettingsEdit = "Settings.Edit";
public const string OpsDiagnosticsDetail = "Ops.Diagnostics.Detail";
```

Modules reference these constants from the code-behind, **never hard-code string literals inside the module**:

```csharp
ReviewRoleKey = PortalPermissionKeys.BusinessApplicationReview
```

Data scope (visible rows) is controlled uniformly by the Foundation layer: a module that needs filtering by organisation
subtree should use the employee directory query's subtree parameter instead of assembling filter conditions itself, to avoid
bypassing scope control.

When adding a permission key, also synchronise the database authorisation data (scripts such as
`src/Setup/PortalCfg_RolePermissions.sql`) and run `Test-PortalBusinessModuleSmoke.ps1` plus the relevant authorisation
gates before committing.

## Semantic markup and accessibility conventions

The portal has consistent conventions for semantic markup that **new modules must follow** (already guarded by runtime
gates):

| Scenario | Correct form | Note |
| --- | --- | --- |
| Data table header | `<th scope="col">` | Do not leave a bare `<th>` |
| Form field label | `<label class="…" for="<%= Xxx.ClientID %>">` | `class` keeps the existing style class; only add `for` |
| **Read-only** fields | `<dl>` + `<dt>` + `<dd>` | When a label is followed by a display value rather than a form control, **do not** use `label for` |
| Module title | Keep the element name and class; only add `role="heading"` and `aria-level` | Front desktop uses `aria-level="1"`; back-office pages use `= "2"` |
| Empty state | Use the shared empty-state markup | Do not invent your own "No data" wording |

Two easy-to-get-wrong points:

- **`for` may only point at a form control.** Adding `for` to a read-only display field is wrong semantics — assistive
  technology will associate the label with an unrelated control, which is worse than omitting it. Read-only fields use
  `dl/dt/dd`.
- **Do not change element names for semantics.** `asp:Label` renders as `span`; changing it to a native `h1` changes the
  visuals (unless precisely equivalent CSS exists elsewhere) and requires a new custom rendering control. An ARIA heading
  is semantically equivalent and visually zero-change.

CSS should use the stable scopes emitted by the portal (`portal-module`, `portal-module-{id}`, `portal-pane-{pane}`,
`portal-package-{packageId}`), avoiding global element selectors that pollute other modules; do not declare or distribute
proprietary fonts with a package, and follow the project's font licensing rules.

## Installation and enabling

1. Build the solution first in Visual Studio or VSCode, then deploy the package directory to `DesktopModules/` after
   completing the trusted deployment pipeline.
2. Execute `src/Setup/PortalCfg_ModulePackageStates.sql` against the target database, or use the SQL compatibility script:

   ```powershell
   & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 `
       -ConnectionStringsConfigPath $testConfig -ApplyP3Migrations -RequireP3Migrations
   ```

3. Visit `Admin/ModuleCatalog.aspx` as `Admins`. The catalogue page shows only deployed packages that pass validation;
   clicking **Register** creates a legacy module definition record pointing at the manifest's `desktopEntry`.
4. Add instances from the registered definitions in the existing tab layout management page. New modules default to
   `CacheTimeout=0`; the caching policy needs separate evaluation before adjustment.
5. A registered package can be **Enabled** or **Disabled** from the catalogue page. State is written to
   `PortalCfg_ModulePackageStates` and recorded in the operations audit.

When the package state table has no record for a package, a verified package is treated as enabled, for compatibility with
deployments that have not configured state rows. When the state table is missing or unreadable, the front end likewise keeps
the default enabled state, but back-office state writes will prompt for the migration to be run first.

## Smoke gate and hard constraints

Every new module should be wired into the gate orchestration and pass the smoke check individually:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalBusinessModuleSmoke.ps1 `
    -ModuleName HIA.YourModule `
    -SqlMigrationFile src\Setup\PortalBiz_YourEntities.sql
```

The smoke gate enforces the following constraints (**all Fail-level, no bypass**):

| Constraint | Gate check name |
| --- | --- |
| Package directory and `module.json` exist and parse | `Module directory` / `Module manifest` |
| `desktopEntry` must be an `.ascx` inside the package directory, with no escape | `Desktop entry safety` / `Desktop entry file` |
| Resource files must exist and must not escape the package directory | `Manifest resource safety` / `Manifest resource file` |
| The package must contain **no DLL, ZIP, executable or script files** | `Module package static asset policy` |
| **JavaScript files are forbidden by default** (unless `-AllowModuleScripts` is explicit) | `Module script policy` |
| **Inline `<script>` blocks are forbidden by default** | `Inline script policy` |
| A business migration script must be named `PortalBiz_*.sql` | `SQL migration file` / `SQL migration naming` |
| A migration script **must not contain `USE [database]`** (must be portable to any database name) | `SQL migration portability` |
| `packageId` should follow the `HIA.{ModuleName}` style | `Package id convention` (Warning level) |
| `version` should be SemVer-shaped | `Module version` (Warning level) |

> **Name migration files after the business entity, not the module.** For example `HIA.MyWorkItems` maps to
> `PortalBiz_WorkItems.sql`. When no corresponding file can be found, use `-SkipSqlMigrationCheck` to exempt it explicitly
> and state the reason in the commit message — this is an **honest exemption**, not a fabrication to pass a gate.

## Lifecycle and removal

The module package lifecycle is `Available` -> `Registered` -> `Enabled` or `Disabled` -> `UninstallReady`.

Disabling only blocks that package's instance rendering and CSS mounting; it does not delete module instances, module
business data or the physical directory. The catalogue page's **Preflight** shows the corresponding definition and instance
counts. When instances exist, the legacy definition page refuses direct deletion — you must first disable, migrate, or
explicitly clean up the instances and their business data.

Deleting the physical directory remains a trusted deployment operation: complete the preflight and instance cleanup first,
then remove the package directory in the deployment pipeline, and finally restart the application or wait for the app domain
to recycle. The back office never deletes a module's physical directory.

## Verification and troubleshooting

`ModuleProbe` is a read-only reference package used to verify registration, enable/disable, CSS, cache identity and virtual
paths; it writes no business data. When troubleshooting a new package:

1. First confirm that `module.json`, the entry control and the declared resources are all deployed, and that path casing
   matches the manifest.
2. Confirm in `Admin/ModuleCatalog.aspx` that the package was discovered, and run **Preflight**.
3. Confirm the registered definition and tab instance exist; when disabled, the front end should skip that package's
   instances.
4. Check `ModulePackage.*` events in `Admin/DiagnosticsLogs.aspx`. Logs must never rely on connection strings, cookies,
   tokens or passwords.

Common faults and where to look:

| Symptom | Check first |
| --- | --- |
| Front end 500 saying the class cannot be found | Whether `Inherits` uses the fully qualified name (see "Code skeleton") |
| The package does not appear in the catalogue | Whether `module.json` is deployed, path casing, whether the state table disables it |
| The module renders but styling does not apply | Whether `resources` declares the CSS, whether the selector uses a stable scope |
| The gate reports `Desktop entry safety` | Whether `desktopEntry` points outside the package directory or is not an `.ascx` |
| The instance renders but data is empty | Whether data-scope control was bypassed, or the migration script was not run |
| The front-end module is a whole blank page but the build passes | Whether it is registered in `Portal.csproj`'s `<Compile>`; whether the `.ascx` uses `CodeBehind`; whether the markup class names match the module CSS; whether the business package was added to `Portal.ModulePackages.Enabled` |
| Styling is lost in some skins | Whether the corresponding skin's `Default.css` has the `.<module prefix class>` theme scope rule (the module's own `Styles/*.css` alone is not enough) |

## Hidden traps when adding a module

The following four classes of defect **do not appear when builds and unit tests are green; they surface only at runtime as
"blank page / gate misjudgement"**, and were found one by one while  delivered the two business modules. Run
through them before adding any module:

1. **The code-behind must be registered in `Portal.csproj`.** `src/Portal` is a **Web Application project** (it has
   `Portal.csproj`), and existing modules' `.ascx.cs` files compile into `bin/Portal.dll`. If a new module only puts files
   on disk without registering `<Compile Include="DesktopModules\{Module}\{Module}.ascx.cs">` (and
   `.ascx.designer.cs`) and `<Content Include="...{Module}.ascx / module.json / Styles/*.css">` in the csproj, then
   `CodeBehind` reports "could not load type ASPNET.StarterKit.Portal.{Module}" at runtime; more insidiously, **when
   unregistered MSBuild does not compile these files**, so compile errors such as `out var` or `long→string` only blow up at
   runtime.
2. **All seven official skins (including the `Default` fallback theme) must get the theme scope rule.** Writing only the
   module's own `Styles/{Module}.css` while **not adding `body.portal-theme-<normalised name> .<module prefix class>` to
   `src/Portal/App_Themes/<Theme>/Default.css`** makes the module lose themed styling in most skins (a hard AGENTS.md
   requirement). Values are taken from each skin's existing `my-work-items` palette family, with an explicit fallback for
   IE9+ which does not support `var()`. `Default`, although a fallback theme, is also bound by the coverage gate — you
   cannot cover only six official skins and miss it.
3. **Markup and CSS class names must match.** When the markup uses `form-grid` / `form-field` / `list` while the module CSS
   writes `.grid` / `.field` / `.recent`, the module styles and theme rules **all fall silent with no error at all**. Treat
   the markup (`.ascx`'s `class=` and Web Forms controls' `CssClass=`) as authoritative; module CSS and theme rules carry
   layout and colour only.
4. **Which configuration takes effect has two layers; the test is "does the JSON file exist in the site root".** Business
   packages must be allowed through the `Portal.ModulePackages.Enabled` allow-list. The mechanism (see
   `src/Portal/Util/AppSettingsLoader.cs`):
   - **Base values**: `<appSettings>` in `Web.config` always applies first;
   - **Override values**: `AppSettingsLoader.LoadConfig` only `Set`s that JSON's `appSettings` keys into
     `ConfigurationManager.AppSettings` (overriding same-named keys; undeclared keys are preserved or removed per
     `preservePrevious`) when the file it points at **actually exists in the site root**; **when the file does not exist it
     is silently skipped**, with no override at all.
   - Two practical consequences: ① changing the `Set` values in
     `src/Portal/Config/appSettings.dev.json` on a development machine (gitignored, not committed, and usually not in the
     site root) **does not take effect** — the local runtime reads the existing `<appSettings>` from `Web.config`; ② only
     after the deployment environment places `appSettings.json` in the site root does that file's override **take effect**
     (see "Enabling prerequisites" in
     [user-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/user-guide.md)).
   - When allowing a business package, change both places and confirm the profile key (`Portal.ModuleProfiles.Active`)
     contains the package's profile.

> Items 1–3 above are guarded by the static gate `Test-PortalNewModuleReadiness.ps1` (L0 layer, source-only, no IIS/DB
> needed):
>
> ```powershell
> & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalNewModuleReadiness.ps1 `
>     -ModuleName LeaveRequest -ModuleCssPrefix leave-request -ExpectedPackageId HIA.LeaveRequest
> ```
>
> Gate checks: R1 csproj registration, R2 `.ascx` uses `CodeBehind`, R3 all seven skins (including the `Default` fallback
> theme) contain the `<prefix>` theme rule, R4 every module CSS class selector appears in the `.ascx` (including
> `CssClass`). When a class genuinely belongs to JS or code-behind, use `-SkipClassConsistency` to exempt it honestly and
> say so in the commit message.

## Pre-commit checklist

- [ ] `Inherits` uses the fully qualified class name
- [ ] `module.json` fields are compliant; `desktopEntry` and `resources` are both inside the package directory
- [ ] Permission keys reference `PortalPermissions.cs` constants, with no hard-coded literals
- [ ] Table headers carry `scope="col"`; form labels use `label for`; read-only fields use `dl/dt/dd`
- [ ] Module titles carry `role="heading"` and `aria-level`
- [ ] No JavaScript, no inline scripts, no DLLs or executables
- [ ] Business migration scripts are named `PortalBiz_*.sql` and contain no `USE [database]`
- [ ] `.ascx.cs` and `.ascx.designer.cs` are registered in `Portal.csproj`'s `<Compile>` (Web Application project; omitting
      this makes `CodeBehind` report "could not load type" at runtime)
- [ ] All seven official skins' (including the `Default` fallback theme) `Default.css` have the `.<module prefix class>`
      theme scope rule
- [ ] `Test-PortalNewModuleReadiness.ps1 -ModuleName <Module>` passes (csproj / CodeBehind / 7 skins / markup-CSS class
      consistency)
- [ ] `Test-PortalBusinessModuleSmoke.ps1 -ModuleName HIA.YourModule` passes
- [ ] Wire into the orchestration: add this module's smoke and readiness checks to
      `dev/scripts/Invoke-PortalGateSuite.ps1` (L1 smoke / L0 readiness)
- [ ] The whole gate suite is green: `Invoke-PortalGateSuite.ps1`
