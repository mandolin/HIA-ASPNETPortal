---
layout: default
lang: en
parent: Operate
permalink: /en/deployment-checklist/
title: Release and compatibility checklist
---
# Release and compatibility checklist

This checklist prepares the .NET Framework Web Forms portal for test and production releases. It does **not** replace manual
re-testing in a real IIS environment, and it does not authorise automatic changes to IIS, application pools, databases or
external configuration.

## Pre-release boundaries

- [ ] The target machine has .NET Framework 4.8 installed; do not deploy new builds when 4.8 is missing.
- [ ] The release profile is confirmed — Dev, Test, Prod, Scan or LegacyIe — and the mapping between profile and Web.config
      transform is recorded.
- [ ] `dev/scripts/Test-PortalPublishReadiness.ps1` has been run, and the project Content manifest, theme packages and
      module packages all pass.
- [ ] When using a local file-system release package, `dev/scripts/Publish-PortalFileSystem.ps1` has been run and a second
      gate has been executed against the release output directory.
- [ ] `dev/scripts/New-PortalReleaseManifest.ps1 -PackagePath <release dir>` has been run against the FileSystem release
      output, producing the package manifest.
- [ ] The package manifest confirms the absence of **internal repository directories**, `temp/`, `dev/`, `src/`, uploaded
      business files, real configuration, Web.config transforms and certificate private keys; `Uploads/web.config` is allowed
      to ship as a security configuration file.
- [ ] `dev/scripts/Get-PortalReleaseSummary.ps1` has been run to summarise the release manifest, operations evidence and
      documentation evidence; the script must not automatically create tags, branches or pushes.
- [ ] Public release notes have been prepared per `docs/release-notes-template.md`, stating explicitly that real IIS/TLS/ACL,
      SQL Server 2016/2017/2019, enterprise scanning and HIA consumers are target-environment evidence items still to be
      gathered.
- [ ] A separate `test` or `prod` external configuration directory is used, not a development machine's LocalDB or
      development connection string.
- [ ] No real connection strings, passwords, tokens, certificates, cookies or production database backups are committed or
      recorded.
- [ ] `dev/scripts/Test-PortalDefaultCredentialRisk.ps1` has been run, and default credential risks are confirmed to be
      either acceptable historical samples or already handled.
- [ ] If this release needs compliance traceability, `dev/scripts/New-PortalComplianceEvidencePackage.ps1` has been run and
      the evidence package directory recorded.
- [ ] If this release needs operations traceability, `dev/scripts/New-PortalOperationsEvidencePackage.ps1` has been run and
      the `Pending` items recorded.
- [ ] The target database has been backed up, the change window is set, and the rollback owner is named.
- [ ] Full initialisation scripts are executed only on an isolated test database under explicit manual operation;
      historical scripts hard-code the database name `Portal` and include rebuild/clear behaviour.
- [ ] Incremental migrations have first been verified on a separate test database; application startup does not run
      migrations automatically.
- [ ] credential migration, permission mapping and deployment policy all have rollback or deferral records.

## IIS and application pool

- [ ] IIS, ASP.NET 4.x, the required static content and IIS management features are installed on the server.
- [ ] The application pool uses CLR v4, with the Integrated pipeline preferred for the first test; if Classic must be used,
      the compatibility reason and re-test result are recorded.
- [ ] The site root path, virtual directories, application mappings and physical directories are all confirmed.
- [ ] The application pool identity has minimal write permission on the site directory, `App_Data/Logs`, the upload
      directory and the required temporary directories.
- [ ] `ExternalCfgPath` is set to a physical directory defined by the deployment; do not rely on deriving a default path
      from the application pool UserProfile.
- [ ] `<env>` matches the external configuration directory, e.g. `test` uses
      `{ExternalCfgPath}\test\connectionStrings.config`.

## Database and configuration

- [ ] SQL Server engine version, edition name, database name and `compatibility_level` are recorded.
- [ ] New test databases on SQL Server 2016 should use compatibility level `130`; for existing databases record the actual
      value first and do not force adjustment outside a release window.
- [ ] The application account has only the database permissions it needs; `sysadmin` is not used as the runtime account.
- [ ] The execution status of `PortalCfg_SystemSettings.sql`, `PortalCfg_UserRegistration.sql`,
      `PortalCfg_OperationAudits.sql`, `PortalCfg_TabThemeOverrides.sql`, `PortalCfg_ModulePackageStates.sql` and
      `Portal_UserCredentials.sql` is recorded.
- [ ] Only reviewed theme directories are deployed to `App_Themes`; each optional theme has a validated `theme.json` and
      `Default.css`.
- [ ] Only reviewed module directories are deployed to `DesktopModules`; each new business module has a validated
      `module.json` and contains no ZIP, DLL, external links or automatic script entry.
- [ ] Do not change theme assets through background ZIP upload, online CSS editing, external URLs or theme scripts; those
      capabilities are not part of the release contract at present.
- [ ] `Portal.Diagnostics.AllowAdminDetailView`, the log directory, the upload directory and other deployment-level
      settings match the environment's requirements.
- [ ] The production default account has been replaced, and no shared development password remains.
- [ ] If `Portal_LoadData.sql` is used to initialise the database, the default `admin` has been replaced, disabled, deleted
      or migrated before open access, with the result recorded.
- [ ] The execution status of `Portal_UserCredentials`, `Portal_UserSecurityStates` and `PortalCfg_RolePermissions` is
      recorded.
- [ ] `PortalCfg_RolePermissions` contains the `Admins` compatibility permission mapping; subsequent non-administrator
      permission mappings require an audit or change record.
- [ ] **Since W45**: the new `EnterpriseCapability.*` layered key seed mappings in `PortalCfg_RolePermissions.sql` **must be
      executed at deployment time** — that script is a deployment action and application startup does **not** execute it
      automatically; when unexecuted, the new keys are entirely unavailable to non-administrators (only `Admins` can use them
      via the administrator short-circuit).
- [ ] **Since W47**: `PortalBiz_CollaborationItems.sql` (idempotent column addition for the parent-item column
      `ParentItemId`) and `PortalBiz_CollaborationItemParticipants.sql` (new table) migration scripts **must be executed at
      deployment time**; both are idempotent (`IF NOT EXISTS` / `IF COL_LENGTH ... IS NULL`) and do not affect existing data.
- [ ] `machineKey`, Cookie `Secure` / `SameSite` and the HTTPS policy have been confirmed against the target environment;
      real keys must never enter the repository.

## Profile layering

| Profile | Purpose | Release notes |
| --- | --- | --- |
| Dev | Local development and IIS Express verification. | LocalDB / development configuration may be used, but must not serve as production evidence. |
| Test | Test database and test IIS / virtual directory. | Detailed diagnostics are allowed, but real test connection strings remain external. |
| Prod | Formal deployment. | Production security headers, Cookie SSL, external sensitive configuration and a least-privilege account must be enabled. |
| Scan | For enterprise scanning tools. | CSP, caching or redirects may be adjusted for scanning, but exceptions and the restoration strategy must be recorded. |
| LegacyIe | Legacy-browser evidence. | Proves only accessibility, operability and explainable degradation; does not lower the production security baseline. |

## Post-release regression

- [ ] The home page, sign-in, sign-out, the Admin main entry and core modules are accessible.
- [ ] Unauthenticated access to back-office resources, logs and audit pages is denied.
- [ ] Administrators can reach the first batch of permission entries normally; ordinary users or users lacking
      permissions are denied those entries.
- [ ] After a role or permission is tightened, the target user's next request is re-evaluated or they are required to sign
      in again.
- [ ] Administrators can view system health, diagnostic logs and operations audits, and details contain no cookies, tokens,
      passwords or connection strings.
- [ ] Operations readiness and log-maintenance dry-runs have been executed per `docs/operations-runbook.md`; backups and
      retention requirements are confirmed before production log cleanup.
- [ ] The generic error page shows only an event ID, exposing no exception details or physical paths to ordinary users.
- [ ] Uploads, the log directory, database health checks and theme resources all pass inspection.
- [ ] For virtual-directory deployment, sign-in, module paths and static resource paths have all been verified.

## Rollback and records

- [ ] The release package / commit number, external configuration version, database migration status and verification time
      are recorded.
- [ ] The version number is recorded; the current version is **`v1.0.0`** (first release), and real tags and release
      branches are created only after manual confirmation (see [versioning.md]({{ site.baseurl }}/versioning/) for the version rules).
- [ ] File, configuration, database and evidence rollback materials are prepared per
      `docs/deployment-rollback-guide.md`.
- [ ] Application rollback, configuration rollback and database recovery steps are all executable.
- [ ] Real IIS re-test results, known limitations and deferred items are written to internal records (not published with
      the public documentation).
