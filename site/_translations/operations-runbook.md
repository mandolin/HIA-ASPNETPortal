---
layout: default
title: Operations runbook
lang: en
parent: 运维
nav_order: 1
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

> Why this step exists: an all-green local gate suite means only that the build artifact is fine, not that the published pages are. Template comments, mirror consistency and propagation delay can all make the live site differ from the local build — this step watches the real published output.

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

## Evidence package explained

`New-PortalOperationsEvidencePackage.ps1` orchestrates read-only script output into an evidence package. In an evidence
package, `Pending` means target-environment or manual material is still needed — it does **not** mean failure; `Fail` means
the current repository or public documentation has a problem that must be handled first.
