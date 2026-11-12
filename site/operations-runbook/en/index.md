---
layout: default
lang: en
parent: Operate
nav_order: 2
permalink: /en/operations-runbook/
title: Operations runbook
---
# Operations runbook

This runbook is for deployment and maintenance staff. It describes the operations entry points currently available, routine
checks, evidence retention and deferred evidence items. It contains no real connection strings, passwords, tokens, cookies,
certificate private keys or production configuration.

## Operations entry points

| Entry point | Purpose | Permission boundary |
| --- | --- | --- |
| `Admin/SystemHealth.aspx` | View system health, the settings registry, log directory, upload directory and a lightweight database connection status. | Currently `Admins` only. |
| `Admin/DiagnosticsLogs.aspx` | Query the structured diagnostic log list. | Currently `Admins` only; detail view is controlled by a deployment-level switch. |
| `Admin/OperationAudits.aspx` | Query key operations audit records. | Currently `Admins` only; no export download in this phase. |
| `Admin/ThemeSettings.aspx` | View and change the trusted theme selection. | Currently `Admins` only. |
| `Admin/ModuleCatalog.aspx` | View, enable and disable trusted deployed module packages. | Currently `Admins` only. |

Admin pages are only runtime viewing and limited maintenance entry points; they do not replace the formal operations consoles
of the target IIS, SQL Server, backup systems or enterprise monitoring platforms.

## First-time deployment to IIS

This section chains "getting the source" through to "the site responds" for a target Windows + IIS + SQL Server
environment. Every step has a checkable result. The
[release and compatibility checklist](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-checklist.md)
complements it: that page lists *what to confirm*, this one lists *how to do it*. Nothing here automatically changes IIS,
application pools, databases or external configuration — the deployment owner performs every change in their own
environment.

1. **Get the source**: the project ships as source only, so clone the GitHub repository or download a source archive.
2. **Restore and build**: from the repository root run `dev/scripts/Restore-NuGetPackages.ps1`, then build `src/master.sln`
   with `dev/scripts/Build-Solution.ps1` (or select `Release | Any CPU` in Visual Studio). Continue only after a
   zero-error build.
3. **Initialise the database**: run the base and incremental scripts plus `src/Setup/Portal_UserCredentials.sql` in order
   against the target SQL Server. Initialisation scripts carry a **guard against accidental deletion** and run
   automatically only against isolated test databases; back up any existing database and confirm manually first — see the
   "Database and configuration" section of
   [deployment-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-checklist.md).
4. **Place the external connection strings**: copy `src/Portal/Config/Templates/connectionStrings.config` to
   `Web\HIA-ASPNETPortal\{env}\connectionStrings.config` under the profile directory of the process account on the target
   machine (`{env}` is usually `prod` and must match `<env value="..." />` in `web.config`), then fill in the `Portal`
   connection string for the database created in step 3. **Real connection strings never enter the repository.**
5. **Produce the publish directory**: use `dev/scripts/Publish-PortalFileSystem.ps1` to export `src/Portal` to the target
   publish directory. Confirm `bin/`, `DesktopModules/`, `Admin/`, `Default.aspx` and `web.config` are present, and that
   the publish directory contains **no real value** for `Config/Templates/connectionStrings.config`.
6. **Create the application pool and site**: in IIS Manager create an application pool (.NET CLR version `v4.0`, managed
   pipeline mode `Integrated`), create a site whose physical path is the publish directory from step 5, and configure
   bindings (HTTPS with a valid certificate is recommended on an intranet). Grant the application pool identity minimal
   write access to `App_Data/Logs` and the upload directory; keep everything else read-only.
7. **Confirm the external configuration is reachable**: verify the application pool identity can read the directory from
   step 4. For a custom location, set `ExternalCfgPath` in `web.config` explicitly and keep `<env>` consistent with the
   external directory.
8. **First visit and sign-in**: open the site root in a browser and confirm a 200 response, loaded theme styles and a
   visible sign-in card. Sign in with the deployment account and confirm the welcome text, the left-hand tabs and the
   lightweight database connection status on `Admin/SystemHealth.aspx`.
9. **Post-release regression**: work through the nine "Post-release regression" items in
   [deployment-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-checklist.md)
   (home, sign-in, permissions, error page, virtual directory, and so on) and retain the evidence.
10. **Record and roll back**: record the version, publish directory, configuration location and SQL batch; prepare a
    rollback package per "Rollback and records" in that checklist. Do not enable any debug-state switch on first deploy.

> **Common blockers**: "service not registered" or empty lists after sign-in usually mean the SQL batch was not fully
> applied; configuration errors usually mean `ExternalCfgPath` / `<env>` disagree with the external directory, or the
> application pool identity lacks read access; missing styles usually mean static assets were not copied into the publish
> directory. Start from the structured diagnostics logs and `Admin/DiagnosticsLogs.aspx`.

> **Boundary**: for local development with IIS Express you can use `dev/scripts/Start-IISExpress.ps1` (default port
> `40001`), which is a much shorter path. This section targets a formal IIS site and **does not cover** load balancing,
> distributing a shared `machineKey` across a web farm, SQL Server Always On, or similar topologies — those belong to the
> target environment's operations plan.

## Daily checks

1. Open `Admin/SystemHealth.aspx`, confirm the overall status is not `Error`, and record `Pending` items that need
 target-environment evidence.
2. Open `Admin/DiagnosticsLogs.aspx` and review recent errors and warnings; when a generic error-page event ID appears,
 look it up by that ID.
3. Open `Admin/OperationAudits.aspx` and spot-check key business audit records such as registration, review, roles, profile
 corrections and work items.
4. Confirm that the site root directory, `App_Data/Logs`, the upload directory and the external configuration directory
 permissions meet the minimal-write requirement.

## Weekly checks

1. Run operations readiness:

 ```powershell
 dev/scripts/Test-PortalOperationsReadiness.ps1
 ```

2. Run the diagnostic log retention policy dry-run:

 ```powershell
 dev/scripts/Test-PortalLogMaintenance.ps1
 ```

3. Review `RetentionCandidates` and `UnmanagedFiles` in the dry-run output. The script never deletes logs; before production
 cleanup, confirm backups, retention days, audit requirements and the change window first.
4. Check the upload directory for abnormal extensions, abnormally sized files or signs of unauthorised access.

## Before and after release

At minimum, run the following before release:

```powershell
dev/scripts/Test-PortalPublishReadiness.ps1
dev/scripts/Test-PortalPublicDocumentation.ps1
dev/scripts/Test-PortalComplianceBaseline.ps1 -Profile Test
dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Test
```

When operations evidence must be retained:

```powershell
dev/scripts/New-PortalOperationsEvidencePackage.ps1 -Profile Test
```

Public delivery documentation references only sanitised summaries, and real configuration or production
screenshots are never placed in the root repository.
A post-publish live re-check runs **automatically** in the publish workflow and can be re-run at any time:

```powershell
dev/scripts/Test-PortalPublishedSiteChecks.ps1
```

It verifies four criteria per page: reachability (HTTP 200), title agreement with the locally built artifact, navigation visibility (a link to the page exists in the page HTML), and no internal numbering inside the page. CI runs it with `-WaitSeconds 180` to allow GitHub Pages propagation and with `continue-on-error` — **a failed re-check warns only and never blocks publication**.

Exit codes: `0` all pages pass; `1` a defect was found (the page is already published; fix and republish); `2` unconfirmed (the propagation wait timed out — **not treated as a pass**; retry with a larger `-WaitSeconds`).

Whether propagation has finished is decided by the **build stamp**, not by "the page opens": the build writes the source document fingerprint into an HTML comment at the end of each page, and the re-check reads the same source file and recomputes the same hash — agreement is what makes the live page "this build's output". "It opens" is not enough: the old page stays **openable** until propagation finishes (it carries the previous build's content), so "the old artifact is still there" would be misread as "propagated". A mismatched stamp is recorded as unconfirmed rather than a defect, and it is judged only in a re-check with a wait window: during a casual re-check the local tree routinely holds unpublished work in progress, and flagging that is noise.

> Why this step exists: an all-green local gate suite means only that the build artifact is fine, not that the published pages are.

> To confirm that these criteria still work, **re-run the local injection track with one command** (no network, nothing online is touched): dev/scripts/Invoke-PortalPublishedSiteCheckInjection.ps1 — it hosts simulated published pages on a local static server, injects defects one by one and asserts that the gate really finds them. Template comments, mirror consistency and propagation delay can all make the live site differ from the local build — this step watches the real published output.

## Log maintenance

Current structured diagnostic logs are written by `PortalDiagnostics` as UTF-8 no-BOM NDJSON files, with `App_Data/Logs` as
the default directory. The default retention is 90 days and the default per-file size cap is 10 MiB; files roll over by date
and sequence number once the cap is reached.

`Test-PortalLogMaintenance.ps1` is dry-run only:

1. It identifies managed logs by the pattern `portal-yyyyMMdd-nnn.jsonl`.
2. It lists candidate files past the retention days.
3. It lists unmanaged files in the directory, prompting manual review.
4. It does not read log bodies, and does not delete, move, compress or archive any file.

If automatic cleanup is needed in production, establish an enterprise-grade backup and audit policy first, then consider
enabling it through system settings or a deployment-level task.

## Audit queries

`Admin/OperationAudits.aspx` currently supports online audit queries and offers no export download. Audit records should
focus on:

1. Registration, review and account status changes.
2. User role, permission and business-role related changes.
3. Employee profiles, organisations, employee bindings and profile corrections.
4. Work items, approval actions and business results.

If export capability is added later, sanitisation, download permissions, export auditing and retention periods must be
designed alongside it.

## Database backup

This project's scripts never perform database backups automatically. Before deployment, migration and major configuration
changes, operations staff should complete the following on the target SQL Server or enterprise backup platform:

1. A full database backup or an equivalent backup consistent with enterprise policy.
2. Recording of the backup file's retention location and retention period.
3. A restore rehearsal, or at least confirmation of the named restore owner.
4. Recording of the backup, migration, verification and rollback windows.

## Alerting extension points

The current phase does not integrate email, IM or Webhooks. The following events are suggested as future alerting extension
points:

1. `Error` and persistent `Warning` in `PortalDiagnostics`.
2. Abnormal states for database, log directory, upload directory and configuration registry in `SystemHealth`.
3. Default credential, legacy MD5 compatibility path and security header gate anomalies.
4. Operations audit write failures or a missing audit table.

When integrating with an enterprise platform later, alert de-duplication, rate limiting, sanitisation, acknowledgement and
recovery strategies must be added.

## Windows scheduled task recommendations

If the target machine uses Windows Task Scheduler, only create read-only tasks that invoke project scripts, and record the
task configuration in the deployment record. The current repository never creates scheduled tasks automatically.

Recommended task types:

| Task | Frequency | Command |
| --- | --- | --- |
| Operations readiness | Daily or weekly | `dev/scripts/Test-PortalOperationsReadiness.ps1` |
| Log maintenance dry-run | Weekly | `dev/scripts/Test-PortalLogMaintenance.ps1` |
| Compliance baseline | Pre-release or monthly | `dev/scripts/Test-PortalComplianceBaseline.ps1 -Profile Scan` |
| Default credential risk | Pre-release | `dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Prod` |

A scheduled task's run identity should have only the minimal permissions to read the repository, read the target log
directory and write the evidence directory. Never write real passwords into a task command, script text or evidence package.

## Target-environment evidence

The following must be evidenced in the target environment; results from a development machine or IIS Express cannot
substitute:

1. Real IIS site, virtual directory, application pool identity, pipeline mode and .NET Framework 4.8.
2. TLS, certificates, HSTS, Cookie Secure, SameSite and the enterprise scanning profile.
3. ACLs on the site directory, `App_Data/Logs`, upload directory, external configuration directory and temporary directory.
4. SQL Server version, compatibility level, backup tasks, restore rehearsals and least-privilege accounts.
5. Disk space, log retention, monitoring, alerting and enterprise audit platform integration.

## L7 target-environment evidence (one-command evidence pack)

The environment-triggered evidence required by the trusted-portal milestone `M-ANP-TRUSTED-PORTAL` (L7) is registered
as a machine-checkable checklist and can be produced with **one command**:

```powershell
& 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'
```

- **Checklist**: `dev/target-environment-evidence.json` — registers per item which criterion it belongs to (T1–T7 and
  the blocking items), whether it can be evidenced on this machine, the generating script, prerequisites, expected
  artifacts and the re-entry condition.
- **Three modes**: `local` (evidence immediately), `targetEnv` (needs the target environment — **skipped explicitly with
  the missing prerequisites stated**, never passed silently), `manual` (human artifact, e.g. business sign-off).
- **Once the target environment is ready**: the same command plus `-BaseUrl <target endpoint>` also evidences the
  environment items; the artifact is a timestamped evidence directory (`manifest.json` + per-step logs + `summary.md`).
- **Skipped is not passed**: the summary lists every item "still without target-environment evidence"; the currently
  unmet items match the blocking items of `M-ANP-TRUSTED-PORTAL` (real IIS/TLS/ACL, SQL Server 2016/2017/2019 target
  instance, enterprise scan report, IE9+ re-test, ADR 0021 target-environment proof, business sign-off).
- The checklist itself is guarded by the L0 gate `Test-PortalTargetEnvironmentEvidenceMap.ps1`: a missing field, a
  non-existent script, or a criterion / blocking item left unregistered all fail the gate.

## Evidence package explained

`New-PortalOperationsEvidencePackage.ps1` orchestrates read-only script output into an evidence package. In an evidence
package, `Pending` means target-environment or manual material is still needed — it does **not** mean failure; `Fail` means
the current repository or public documentation has a problem that must be handled first.

<!-- build-stamp: A1D923DF5742 -->
