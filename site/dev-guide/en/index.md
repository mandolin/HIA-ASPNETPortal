---
layout: default
lang: en
parent: Develop (section)
permalink: /en/dev-guide/
title: Developer guide
---
# Developer guide

## Environment requirements

Recommended:

- A Windows development environment.
- Visual Studio 2022, or a Visual Studio able to build .NET Framework 4.8 projects.
- .NET Framework 4.8 Developer Pack.
- NuGet package restore capability.
- SQL Server or SQL Server LocalDB.
- IIS Express.
- Node.js and npm, only when you need to maintain the frontend asset build.
- For VSCode, the `Csharp_Webform` Profile is recommended, with C#, .NET Runtime and this project's recommended extensions enabled.

## Getting dependencies

This project uses the classic NuGet `packages.config`. The recommended path is to open `src/master.sln` in Visual Studio and enable NuGet Restore.

VSCode / command line:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Restore-NuGetPackages.ps1
```

`nuget.exe` is not currently on the ordinary PowerShell PATH. The script prefers the NuGet CLI; if it is not installed but `src\packages` already exists, the explicit restore is skipped so the legacy project stays buildable as it is. For a future full restore, install the NuGet CLI or use Visual Studio's NuGet Restore.

## Build

Recommended:

1. Open `src/master.sln` in Visual Studio.
2. Restore NuGet packages.
3. Select `Debug|Any CPU`.
4. Build the solution.

VSCode / command line:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Build-Solution.ps1 -Configuration Debug -Platform "Any CPU"
```

The script locates MSBuild from Visual Studio / Build Tools via `dev\scripts\Find-MsBuild.ps1`. It has been verified to find an installed MSBuild automatically and complete a `Debug|Any CPU` build; the actual install path varies with the local Visual Studio / Build Tools setup.

## VSCode tasks

`.vscode/tasks.json` provides these common tasks:

- `nuget: restore packages`: restore or check NuGet packages.
- `build: solution Debug`: build `src/master.sln`, and acts as the default build task.
- `iisexpress: start Portal (40001)`: start `src/Portal` on the fixed port `40001`.
- `iisexpress: stop Portal (40001)`: stop only IIS Express processes matching the port or site path.
- `iisexpress: restart Portal (40001)`: run stop then start in sequence.
- `gulp: assets:build`: run the frontend asset build once.
- `gulp: startWatch` / `gulp: stopWatch`: keep the original Gulp watch tasks available from VSCode.
- `portal: build assets and start`: build the solution, build frontend assets and start IIS Express.
- `portal: documentation baseline`: emit a documentation inventory JSON of tracked sources.
- `portal: build JavaScript documentation pilot`: generate and verify the HIA JSDoc pilot standalone.
- `portal: build .NET DotNetDoc pilot`: generate and verify the HIA DotNetDoc pilot standalone.
- `portal: verify .NET XML documentation`: after a Debug build, check the existing C# XML documentation output.
- `portal: verify frontend contracts`: read-only check of tracked Web Forms presentation, themes, module CSS and Gulp contracts.
- `portal: verify public documentation`: read-only check of public Markdown entry points, relative file links, privacy boundaries and generated-directory boundaries.

These tasks only call helper scripts and npm scripts inside the repository and do not modify `.sln`, `.csproj` or `.csproj.user`, so they do not overwrite Visual Studio's existing debug settings.

## Documentation baseline

The public documentation gate reads only the root README, `docs/` and the JSDoc tool README; it does not access the network, does not read private directories, does not generate documentation, and never touches configuration or a database:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalPublicDocumentation.ps1
```

The gate verifies public entry points, relative file links, the documentation index, absolute project paths / credential shapes and the Git boundary of unconfirmed generated directories. It allows reproducible examples such as `$env:USERPROFILE`, `{ExternalCfgPath}` and the common PowerShell 7 install path, but does not verify complex Markdown anchors, external links or network reachability.

`Get-PortalDocumentationBaseline.ps1` is the read-only baseline tool of . It only counts Git-tracked `src/` sources and outputs file counts for C#, Web Forms, frontend and configuration, an inventory of C# XML documentation, and the boundary state of known generated / untracked directories; these numbers are not a documentation quality or coverage percentage.

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDocumentationBaseline.ps1 -AsJson
```

To write the baseline into the evidence directory (substitute `<internal evidence directory>` with this project's actual internal path):

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDocumentationBaseline.ps1 -OutputJson <internal evidence directory>\p13.3\documentation-baseline.json
```

The script never generates, publishes, deletes or modifies documentation. `src/Documentation/`, `src/DoxyGen/`, `src/Portal/Documentation/`, `src/Portal/js/`, `src/Portal/css/` and `temp/` are never pulled in as inputs automatically; JSDoc verification output goes to the ignored `temp/documentation/`. For the public release policy see [Documentation artifacts and toolchain boundaries](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/documentation-artifacts-guide.md).

From P13.3 on, documentation readiness and the evidence package can be run with:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalDocumentationReadiness.ps1
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalDocumentationEvidencePackage.ps1
```

Readiness checks the Required / Recommended / Deferred coverage scopes, JSDoc pilot isolation, the .NET XML deferral boundary, that generated output is not committed, and HIA notification reading. The evidence package is written to the internal evidence directory by default (not published with the public documentation); it does not modify source comments, does not commit generated output, does not read sensitive configuration, and does not connect to a database.

## JavaScript documentation pilot

`dev/documentation/jsdoc/` is a standalone HIA JSDoc tool project. It pins `@mandolin/jsdoc-plugin-hia-sys@0.1.0`, `@mandolin/jsdoc-theme-hia@0.1.0` and `jsdoc@4.0.5`, and does not touch `src/Portal/package.json`, so it does not interfere with the existing Gulp or Visual Studio Task Runner.

The current pilot reads only two tracked, reviewed inputs: `src/Portal/gulpfile.js` and `src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js`. Run `portal: build JavaScript documentation pilot` in VSCode, or directly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Build-PortalJsdocPilot.ps1
```

The first run executes `npm ci` in the tool directory; after generating HTML, HIA metadata and integration JSON it verifies bilingual content, source links and local-path leakage. Output exists only in `temp/documentation/jsdoc/` and must not be committed.

## .NET XML documentation verification

`Test-PortalXmlDocumentation.ps1` checks only the four existing `Debug|Any CPU` C# XML documentation outputs: `Portal`, `Portal.Components`, `Portal.Components.Data` and `Portal.Components.Data1`. It does not modify `.csproj`, does not generate HIA artifacts, and sets no `CS1591` count gate.

By default it only verifies existing build output:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalXmlDocumentation.ps1
```

To have the script run the existing solution build first, pass `-Build` explicitly; the VSCode `portal: verify .NET XML documentation` task uses this mode:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalXmlDocumentation.ps1 -Build
```

The verifier checks only XML structure, assembly name and at least one member entry. XML remains a local build artefact; standalone integration will be considered once the HIA C# producer and source-linkage contract stabilise.

## .NET DotNetDoc pilot

`dev/documentation/dotnetdoc/` is a standalone HIA DotNetDoc tool project. Its manifest declares `@hia-doc/dotnetdoc-runner@^0.1.8`, the version-controlled lock file resolves to `0.1.8`, and it uses the npm official registry through a local `.npmrc` in the tool directory. This affects only the DotNetDoc pilot and does not change Portal frontend dependencies or the Gulp workflow.

By default the pilot reads the four Debug XML documentation files, representative Web Forms markup comments, one Web Forms surface and solution/project discovery, writing to the ignored `temp/documentation/dotnetdoc/`:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Build-PortalDotNetDocPilot.ps1
```

When the Debug XML documentation already exists, the build can be skipped:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Build-PortalDotNetDocPilot.ps1 -SkipXmlBuild
```

`-ApiOnly` uses `dotnetdoc.api-only.config.json` to troubleshoot the XML documentation pipeline; `-SourceProbe` uses `dotnetdoc.source-probe.config.json` to study C# source relations. The current source probe can emit `CS0246`/`CS0103` diagnostics because per-file Roslyn parsing lacks full project context, so it is not a default gate.

## VSCode debugging

`.vscode/launch.json` provides two debug configurations:

- `Portal: Start IIS Express (40001), then attach`: start IIS Express, then attach to `iisexpress.exe` through the process picker.
- `Portal: Attach to IIS Express`: for manual attachment when IIS Express is already running.

Usage:

1. Open the Debug panel in VSCode.
2. Select `Portal: Start IIS Express (40001), then attach`.
3. When the process picker appears, select `iisexpress.exe`.
4. Visit `http://localhost:40001/` in a browser.
5. After debugging ends, `iisexpress: stop Portal (40001)` runs.

IIS Express has been verified to return `HTTP 200 OK` at `http://localhost:40001/`.

To reach IIS Express from a Win7 VM or another LAN device you cannot use the default simple localhost mode. Stop any existing instance first, then bind the host IP explicitly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Stop-IISExpress.ps1 -Port 40001
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Start-IISExpress.ps1 -Port 40001 -HostName 192.168.199.21
```

The script keeps the `localhost` binding and also prints the external URL. If the VM still cannot connect, follow the script's hint and add a URLACL or firewall rule in an elevated host command prompt. Without `-HostName` the original localhost startup is used, leaving Visual Studio and VSCode debug paths untouched.

## Automated tests, smoke and SQL Server compatibility checks

From P28 there is a formal MSTest runner; by default it runs only local unit / contract tests, does not connect to a database, does not start IIS and does not create accounts:

```powershell
mise exec -- pwsh -NoLogo -NoProfile -File dev/scripts/Invoke-PortalTests.ps1
```

To reuse already-built test assemblies quickly, skip restore and build explicitly:

```powershell
mise exec -- pwsh -NoLogo -NoProfile -File dev/scripts/Invoke-PortalTests.ps1 -NoRestore -NoBuild
```

The HTTP smoke added in P2.5 by default only checks an already-running site, writes nothing to the database and needs no administrator credentials:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSmoke.ps1
```

To have the script start and stop a local IIS Express itself, pass explicitly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSmoke.ps1 -StartIISExpress -StopWhenComplete
```

Admin page regression needs an explicit username; the password is read as an interactive `SecureString` and must not be written into a command, script or configuration file:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSmoke.ps1 -AdminUser admin
```

A compatibility check against a non-LocalDB SQL Server must use an external connection-string file pointing at a dedicated test database. The default is read-only preflight; only when you pass `-ApplyP2Migrations`, `-ApplyP3Migrations`, `-ApplyP5Migrations`, `-ApplyP6UserProfileMigration` or `-ApplyP6EmployeeOrganizationMigration` and confirm does it run the corresponding idempotent incremental migrations:

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath $testConfig -RequireP2Migrations -RequireP3Migrations -RequireP5Migrations -RequireP6UserProfileMigration -RequireP6EmployeeOrganizationMigration
```

The P6 user-profile migration first checks whether legacy user names and emails contain historical data that would break uniqueness; the P6.3 employee-organization migration runs in the order `PortalBiz_OrganizationUnits.sql`, `PortalBiz_Employees.sql`, `PortalBiz_UserEmployeeBindings.sql`. Both must point only at an isolated development or test database, never at production configuration.

The SQL Server version-matrix preflight supports the multi-version evidence collection after P11. By default it is a static scan only: it does not connect to a database, create one or execute SQL:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlVersionMatrix.ps1
```

To add version, compatibility level and target-database metadata for an isolated SQL Server test instance, pass a connection-string file outside the repository. The script reads only metadata and never prints server names, connection strings or passwords; pass conclusions for SQL Server 2016/2017/2019 must still be evidenced by the corresponding real instances and cannot be substituted by a newer version:

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlVersionMatrix.ps1 -ConnectionStringsConfigPath $testConfig
```

From P11.2 there is a read-only data-access inventory for scanning tracked sources for SQL Server-specific dependencies, SQL needing provider dialect handling, abstractable entry points and provider proof boundaries. It does not connect to a database, does not read connection-string files outside the repository, and does not scan untracked generated directories:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDataAccessInventory.ps1
```

To keep JSON evidence, specify the output path explicitly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDataAccessInventory.ps1 -OutputJson <internal evidence directory>\p11.2\data-access-inventory.json
```

From P11.3 there is a read-only migration manifest check, inventorying the execution order, migration type, idempotency, provider tag, rollback mode and legacy risk of `src/Setup` scripts:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalMigrationManifest.ps1
```

To keep JSON evidence, specify the output path explicitly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalMigrationManifest.ps1 -OutputJson <internal evidence directory>\p11.3\migration-manifest.json
```

When an existing test database needs P5/P6 schema completion, you must explicitly use the external `test` connection string and the `-Apply*` switches of `Test-PortalSqlCompatibility.ps1`. This writes to the target test database and must point only at an isolated test instance:

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath $testConfig -ApplyP5Migrations -ApplyP6UserProfileMigration -ApplyP6EmployeeOrganizationMigration -ApplyP6BusinessModuleMigration -RequireP2Migrations -RequireP3Migrations -RequireP5Migrations -RequireP6UserProfileMigration -RequireP6EmployeeOrganizationMigration -RequireP6BusinessModuleMigration
```

When creating an isolated SQL Server 2016+ test database for the first time, you can run the initialisation script explicitly. It reads the target database name from the external connection string, requires that the target database does not exist, and after confirmation imports the historical base data plus the P2, P3 and P5 migrations; it never prints or stores the connection string:

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Initialize-PortalTestDatabase.ps1 -ConnectionStringsConfigPath $testConfig
```

You can add `-WhatIf` first to check the target database and intended action. The script never overwrites an existing database and never drops one automatically after a failed step; use it only on isolated test instances. Before a release use [deployment-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-checklist.md).

## Data provider proof

P3.3 provides a SQLite capability proof that is not added to `src/master.sln`, verifying a new provider profile, ADO.NET factory, parameterised reads/writes, UTC, transactions and unique constraints. It does not switch the portal's main business database, does not require modifying the external `Portal` connection string, and does not take part in site publishing.

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalDataProvider.ps1 -Configuration Debug
```

The script restores the proof project's packages.config separately, builds into `temp\provider-proof\bin\`, and regenerates the SQLite file under `temp\provider-proof\data\`. `-DatabasePath` accepts only paths inside that temporary data directory; by default the previous proof files are deleted and rebuilt, and only `-KeepDatabase` retains them.

Provider-specific DDL goes in `src\Setup\Providers\{ProviderId}\`. Currently only the `SQLite` proof exists; future providers such as MySQL and PostgreSQL should each add their own directory, dependency/licence audit, dialect implementation, migration and regression, and must not overwrite the existing SQL Server scripts.

## HIA boundary contract proof

P3.4 provides a standalone HIA peripheral capability description proof. It verifies only the JSON envelope, version, field range and privacy boundary that the portal owns; it does not load HIA external assemblies, does not open HTTP/message/file transports, and does not affect normal portal startup or publishing.

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalHiaBoundary.ps1 -Configuration Debug
```

The proof project lives at `src\Portal.HiaBoundaryProof\`, is not added to `src\master.sln`, and writes only to the ignored `temp\hia-boundary-proof\`. The current draft allows module, theme, settings registry metadata, health state and restricted diagnostic references; it does not accept users/roles/authentication, actual settings values, log details, audit bodies, paths, connection strings or any credentials.

`Portal.Hia.InstanceId` is an optional deployment-level, non-sensitive stable identifier. It is empty by default, and leaving it empty enables no external adapter. When preparing for future controlled collaboration it may be set to a GUID or a stable identifier of lowercase letters, digits, `.`, `_` and `-`; it must not be derived from a machine name, database name, domain name, username or other personal/environmental information. If a real HIA consumer or transport is later connected, update the ADR, contract version and standalone regression first.

From P11.4 you can generate a HIA integration boundary inventory. It only inventories the current contract, proof, draft fixtures, the documentation notification reader and candidate layering; it does not enable real HIA transport and does not read connection strings, credentials or log bodies:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalHiaIntegrationInventory.ps1
```

To keep JSON evidence, specify the output path explicitly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalHiaIntegrationInventory.ps1 -OutputJson <internal evidence directory>\p11.4\hia-integration-inventory.json
```

If HIA-Documentation-Sys is not in the default sibling directory, pass `-DocumentationSysRoot`; the script records only notification titles, relative paths and whether the current project is mentioned, and never copies notification bodies.

## P3 extensibility acceptance

`Test-PortalExtensionSmoke.ps1` aggregates the P3 build, frontend assets, SQLite provider, HIA contract, root site and virtual-directory regressions. By default it does not write to a database, needs no administrator credentials, and does not stop an in-use `40001` site; the virtual directory uses a temporary IIS Express port and stops it afterwards.

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1
```

SQL Server schema, administrator interaction and cache isolation are explicit capabilities. The connection string is read only from a file outside the repository and the script never prints its value:

```powershell
$config = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1 -IncludeSqlCompatibility -ConnectionStringsConfigPath $config
```

The theme and cache proofs briefly write verification data into the specified development or test database and restore the original state in a `finally` block. The theme proof verifies the database-global `ThemeProbe`, tab override priority and invalid-theme fallback; the cache proof creates temporary ModuleProbe definitions, instances and package states. Both must be enabled explicitly and must not point at production configuration:

```powershell
$config = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1 -IncludeThemeMutation -IncludeCacheMutation -ConnectionStringsConfigPath $config
```

Administrator interaction regression uses `SecureString` password input and never records the password. It verifies that a signed-in user can reach the system health, log, audit, theme settings and module catalogue pages; actual business operations on themes/modules should still be accepted against the phase acceptance checklist on isolated data:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1 -IncludeAdmin -AdminUser admin
```

## Database initialisation

See `src/ReadMe.txt`; the basic flow is:

1. Prepare an external `connectionStrings.config` for the isolated environment and make sure the target database does not exist.
2. Prefer running `Initialize-PortalTestDatabase.ps1` explicitly for base initialisation, then complete later idempotent migrations with the `-Apply*` switches of `Test-PortalSqlCompatibility.ps1`; the historical SQL files are kept for manual maintenance in Visual Studio/SSMS.
3. If you must do it by hand, follow the order `Portal_CreateDB.sql`, `Portal_LoadConfig.sql`, `Portal_LoadData.sql`, `PortalCfg_SystemSettings.sql`, `PortalCfg_UserRegistration.sql`, `PortalCfg_OperationAudits.sql`, `PortalCfg_TabThemeOverrides.sql`, `PortalCfg_ModulePackageStates.sql`, `Portal_UserCredentials.sql`, `PortalCfg_RolePermissions.sql`, `PortalBiz_UserProfiles.sql`, `PortalBiz_OrganizationUnits.sql`, `PortalBiz_Employees.sql`, `PortalBiz_UserEmployeeBindings.sql`, `PortalBiz_EmployeeProfileConfirmations.sql`, `PortalBiz_EmployeeProfileCorrectionRequests.sql`, `PortalBiz_WorkItems.sql`, `PortalBiz_WorkItemEvents.sql`, `PortalBiz_BusinessApplications.sql`, `PortalBiz_WorkflowEvents.sql`, and confirm the database context matches the connection string.
4. Copy `src/Portal/Config/Templates/connectionStrings.config` to the external configuration directory and edit the `Portal` connection string in it.
5. Build and run the site.

The P19.5 business-application sample page can first generate reviewable SQL without modifying the database directly:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BusinessApplicationScenarioSql.ps1
```

To execute against a confirmed development/test database, complete the P5/P12/P19 migrations first, then pass the external connection string explicitly and use `-Apply`:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath '<external>\connectionStrings.config' -ApplyP5Migrations -ApplyP12WorkItemMigration -ApplyP19BusinessApplicationMigration -RequireP5Migrations -RequireP12WorkItemMigration -RequireP19BusinessApplicationMigration -Confirm:$false

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BusinessApplicationScenarioSql.ps1 -ConnectionStringsConfigPath '<external>\connectionStrings.config' -Apply -Confirm:$false
```

To complete the P19.5 authenticated browser regression, use the restricted fixture helper. It accepts only an external `connectionStrings.config` whose immediate parent directory is named `test`, and creates exactly one ordinary participant and one administrator holding only the `Admins` entity role, both determined by `FixtureId`. The Create password must be entered interactively as a `SecureString` by a short-lived process; passwords must not be written to the command line, scripts, logs or configuration. After completing submission and administrator review, you must Inspect before Remove and confirm both final counts are zero:

```powershell
$testConfig = '<external>\test\connectionStrings.config'
$participantPassword = Read-Host -AsSecureString 'P19 fixture participant password'
$administratorPassword = Read-Host -AsSecureString 'P19 fixture administrator password'

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BrowserSmokeFixture.ps1 `
  -ConnectionStringsConfigPath $testConfig `
  -Action Create `
  -FixtureId 'p19-local-001' `
  -ParticipantPassword $participantPassword `
  -AdministratorPassword $administratorPassword `
  -Confirm:$false

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BrowserSmokeFixture.ps1 `
  -ConnectionStringsConfigPath $testConfig `
  -Action Inspect `
  -FixtureId 'p19-local-001'

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BrowserSmokeFixture.ps1 `
  -ConnectionStringsConfigPath $testConfig `
  -Action Remove `
  -FixtureId 'p19-local-001' `
  -Confirm:$false
```

The helper outputs only count-level facts, never connection strings, passwords, salts, hashes or application bodies. Remove deletes, in a single transaction and in dependency order, that fixture's P19 records, entity-role relations and both accounts; it cannot delete data by arbitrary username, role or prefix. It proves only that the test database and an isolated browser form a closed loop, and cannot substitute for real IIS/HTTPS, production or enterprise-scan evidence.

Default accounts and weak passwords must not be used in deployed or shared environments. For default credentials and legacy password governance see [deployment-default-credentials.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-default-credentials.md); before releasing run `dev/scripts/Test-PortalDefaultCredentialRisk.ps1`.

## Frontend assets

For the Web Forms Master, themes, module CSS, Gulp input/output, Visual Studio/VSCode boundaries and the untracked asset policy, see [frontend-asset-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/frontend-asset-guide.md).

`src/Portal/gulpfile.js` keeps the original Visual Studio Task Runner binding:

```js
/// <binding ProjectOpened='startWatch' />
```

It also adds a standalone `assets:build` task for VSCode and AI automation, without changing the original semantics of `startWatch` / `stopWatch`.

Common commands:

```powershell
cd src\Portal
npm run assets:build
npm run assets:watch
npm run assets:stop-watch
```

`npm run assets:build` has been verified to run successfully. A Browserslist data-outdated notice may appear; that is a frontend dependency maintenance reminder and does not affect the build result.

To review the formal frontend inputs read-only without running Node/Gulp:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalFrontendContracts.ps1
```

## Configuration notes

- `src/Portal/Config` contains `*.template.*` files as well as real environment files.
- Before changing real configuration, confirm whether you should be changing the template instead.
- Connection strings are no longer written to `UnityCfg*.xml`; they are read from the external `{ExternalCfgPath}\{env}\connectionStrings.config`.
- When `ExternalCfgPath` is empty, `{current process user directory}\Web\HIA-ASPNETPortal\` is used by default.
- The local `dev` default file path is `{current process user directory}\Web\HIA-ASPNETPortal\dev\connectionStrings.config`.
- The logical name currently used in `connectionStrings.config` is `Portal`, mapped after startup to Unity's `connectionString` named instance; `providerName` is required and the portal's main business database is fixed to `System.Data.SqlClient`.
- The environment variable `HIA_ASPNETPORTAL_CONNSTR_PORTAL` can override the concrete connection string value; the external file itself must still exist.
- Do not commit real production connection strings, accounts, passwords, tokens or certificates.
- `.gitignore` already covers some configuration, database and build-output ignore rules, but historical files still need manual review.

## File encoding

- New or modified text files use UTF-8 without BOM.
- The repository-root `.editorconfig` sets `charset = utf-8`.
- `src/Portal/.editorconfig` contains `root = true`, so `charset = utf-8` is set there separately.
- The VSCode workspace sets `"files.encoding": "utf8"`, i.e. UTF-8 without signature.
