---
layout: default
lang: en
parent: 参考
permalink: /en/testing-checklist/
title: Testing checklist
---
# Testing checklist

## Build checks

- [ ] Open `src/master.sln` with Visual Studio.
- [ ] NuGet package restore succeeds.
- [ ] `Debug|Any CPU` builds successfully.
- [ ] `Release|Any CPU` builds successfully.
- [ ] No new errors in the build log.

## Automated regression layers

- [ ] `unit`: run `mise exec -- pwsh -NoLogo -NoProfile -File dev/scripts/Invoke-PortalTests.ps1` and confirm the MSTest
      runner passes.
- [ ] `static-contract`: run the read-only static gates, e.g. `Test-PortalFrontendContracts.ps1`,
      `Test-PortalWorkItemSmoke.ps1`, `Test-PortalBusinessPermissionAudit.ps1`,
      `Test-PortalCollaborationWorkflowSmoke.ps1`, `Test-PortalPublishReadiness.ps1` and
      `Test-PortalComplianceBaseline.ps1 -Profile Dev`.
- [ ] `sql-review`: only generate or review P19/P22/P23/P24 scenario SQL; do not connect to a database unless an external
      test configuration and `-Apply` are explicitly provided.
- [ ] `http-local`: when local IIS Express is needed use `Test-PortalSmoke.ps1 -StartIISExpress -StopWhenComplete`,
      strictly as a development-side HTTP smoke — not equivalent to real IIS/HTTPS evidence.
- [ ] `db-fixture`: run `Initialize-PortalTestDatabase.ps1` or the fixture helper only when a test
      `connectionStrings.config` outside the repository has been confirmed, and retain Inspect/Remove/zero-count
      evidence.
- [ ] `browser-smoke`: execute only when a cleanable short-lived account, an isolated test database and a browser
      environment are all in place; passwords, cookies and connection strings must not be written into commands, logs or
      configuration.
- [ ] `external-target`: real SQL Server versions, IIS/HTTPS/ACL/TLS and enterprise scanning are still handled as
      target-environment evidence or Pending/Gaps — local results must not be passed off as production passes.

## Database checks

- [ ] `Portal_CreateDB.sql` is executable.
- [ ] `Portal_LoadConfig.sql` is executable.
- [ ] `Portal_LoadData.sql` is executable.
- [ ] The external `connectionStrings.config` exists at `{ExternalCfgPath}\{env}\`.
- [ ] The `Portal` connection string points at the expected local or test database.
- [ ] Development verification is not run against a production database.
- [ ] For SQL Server 2016+ version evidence, first run `dev/scripts/Test-PortalSqlVersionMatrix.ps1`; with no real instance
      available, record `Pending` only and do not claim a pass.
- [ ] To verify the latest schema, point `dev/scripts/Test-PortalSqlCompatibility.ps1` at an isolated test database that has
      had the corresponding migrations applied.
- [ ] Before adding or changing data-access code, run `dev/scripts/Get-PortalDataAccessInventory.ps1` and confirm that
      SQL/provider labels and dialect risks are recorded.
- [ ] Before adding or changing database scripts, run `dev/scripts/Get-PortalMigrationManifest.ps1` and confirm the script
      is covered by the manifest, along with its type, idempotency, provider labels and rollback boundary.
- [ ] Any `-Apply*` database migration rehearsal may target only an isolated test database, and must retain the apply
      output plus the subsequent read-only require output.
- [ ] Before adding or changing HIA peripheral integration contracts, draft fixtures, documentation-notification reading or
      evidence-reference rules, run `dev/scripts/Get-PortalHiaIntegrationInventory.ps1` and confirm the contract version,
      standalone proof, notification reading and privacy boundary remain reviewable.

## Runtime checks

- [ ] The site starts.
- [ ] The home page is accessible.
- [ ] `dev/scripts/Test-PortalSmoke.ps1` passes against the running site.
- [ ] The sign-in flow works.
- [ ] The default account is limited to local verification, and non-local environments have changed the password.
- [ ] `dev/scripts/Test-PortalDefaultCredentialRisk.ps1` has been run; default admin, legacy MD5 and initialisation-script
      risks are recorded.
- [ ] The administration entry point is accessible.

## Core module manual regression

- [ ] Announcements module list and edit flow.
- [ ] Contacts module list and edit flow.
- [ ] Discussions module list, detail and edit flow.
- [ ] Documents module upload, view and edit flow.
- [ ] Events module list and edit flow.
- [ ] HTML, image, link and XML module display flows.
- [ ] User, role and permission configuration flow.
- [ ] Page layout and module settings flow.

## Frontend asset checks

- [ ] `dev/scripts/Test-PortalFrontendContracts.ps1` passes for the tracked frontend contracts.
- [ ] `dev/scripts/Test-PortalLegacyCssCompatibility.ps1` passes the legacy-browser CSS baseline blockers.
- [ ] For an approximate Edge IE mode regression, first run `dev/scripts/Test-PortalIeModeReadiness.ps1` and record the
      driver/policy status.
- [ ] For real Win7/IE browser evidence, use `dev/scripts/New-PortalLegacyIeTestPackage.ps1` to generate a VM-runnable test
      package and collect the result zip.
- [ ] Confirm whether Gulp or Grunt needs to run.
- [ ] If frontend assets must be built, record the actual command.
- [ ] CSS, JS and image resource paths are correct.
- [ ] Do not commit `src/Portal/js/`, `src/Portal/css/` or generated documentation directories of unconfirmed provenance as
      formal inputs.
- [ ] No new critical errors in the browser console.

## Documentation checks

- [ ] `dev/scripts/Test-PortalDocumentationReadiness.ps1` passes for the public documentation guide, coverage layering, the
      JSDoc pilot, XML boundaries and the HIA notification reading mechanism.
- [ ] `dev/scripts/Get-PortalDocumentationBaseline.ps1 -OutputJson <evidence path>` has recorded the documentation
      inventory of tracked source.
- [ ] For P13.3 documentation evidence, run `dev/scripts/New-PortalDocumentationEvidencePackage.ps1` and confirm
      `Failed=0`.
- [ ] `src/Documentation/`, `src/DoxyGen/`, `src/Portal.Components.Data/Documentation/`, `src/Portal/Documentation/` and
      `src/Portal.shfbproj` are not committed as formal generated output.

## Pre-release checks

- [ ] `dev/scripts/Test-PortalPublicDocumentation.ps1` passes for the public entry points, relative file links and privacy
      boundaries.
- [ ] To retain compliance evidence, run `dev/scripts/New-PortalComplianceEvidencePackage.ps1` and keep the output in an
      internal evidence directory (not published with the public documentation).
- [ ] To retain operations evidence, run `dev/scripts/New-PortalOperationsEvidencePackage.ps1` and confirm `Fail=0`;
      `Pending` items should be recorded as target-environment evidence to gather.
- [ ] `dev/scripts/Test-PortalPublishReadiness.ps1` passes for the project Content manifest, theme packages, module
      packages and release output.
- [ ] To produce a local file-system release package, run `dev/scripts/Publish-PortalFileSystem.ps1` and record the output
      directory.
- [ ] To produce delivery evidence, run `dev/scripts/New-PortalReleaseManifest.ps1 -PackagePath <release dir>` and record
      the manifest output directory.
- [ ] To shape version cadence or release-note material, run `dev/scripts/Get-PortalReleaseSummary.ps1` and confirm the
      release manifest, operations evidence and documentation evidence can all be summarised.
- [ ] For external or cross-team circulation, prepare public release notes per `docs/release-notes-template.md` and keep
      internal evidence paths in private records.
- [ ] No real connection strings, passwords, tokens or certificates are committed.
- [ ] No local database files, `node_modules/`, `bin/` or `obj/` are committed.
- [ ] The handling of `Web.Debug.config`, `Web.Release.config` and `Web.Test.config` follows the repository rules.
- [ ] README, `docs/` and `dev/` have been updated for the change.
- [ ] Verification results and residual risks are recorded.
- [ ] SQL Server, IIS, external configuration and rollback checks are complete per `docs/deployment-checklist.md`.
- [ ] File, configuration, database and evidence rollback materials are prepared per
      `docs/deployment-rollback-guide.md`.
- [ ] Operations entry points, log dry-runs, audit queries, backup reminders and scheduled-task recommendations have been
      reviewed per `docs/operations-runbook.md`.
- [ ] The new module package passes `module.json`, entry `.ascx` and local resource validation; when enabled both the
      module and its CSS load, and when disabled neither loads.
- [ ] A module definition still referenced by instances cannot be cascade-deleted directly through the legacy deletion
      page; disable, migrate or explicitly clean up instances first.
