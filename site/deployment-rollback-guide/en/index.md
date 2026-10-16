---
layout: default
lang: en
parent: Operate
permalink: /en/deployment-rollback-guide/
title: Release rollback guide
---
# Release rollback guide

This document prepares rollback for file-system release packages from P13.1 onward. It accompanies the release checklist and
does not replace the operating procedures of a real IIS, database, backup platform or enterprise change system.

## Basic principles

1. Before every release you must be able to locate "the previous release package, the current release package, the external
   configuration backup, the database migration record and the verification evidence".
2. Rollback must not depend on verbal memory; the rollback boundaries for the release package, configuration and database
   must be written down in advance.
3. Real passwords, connection strings, tokens, cookies, certificate private keys and production configuration must not enter
   Git, internal directories or the release evidence package.
4. If a database migration cannot be rolled back automatically, the owner and window for "restore the backup" or "manual
   data repair" must be defined before release.

## File rollback

- [ ] Keep the previous FileSystem release package, or the commit number that can regenerate it.
- [ ] Record the current release package path, the package manifest and the Git commit number.
- [ ] When publishing, do not put **internal repository directories**, `temp/`, uploaded business files, AI logs, real configuration or generated
      evidence into the site directory; `Uploads/web.config` may ship with the package as upload-directory security
      configuration.
- [ ] Back up the current site directory before rolling back, so on-site evidence is not lost by the overwrite.
- [ ] After rolling back, run smoke on the home page, sign-in, the back-office health page and core business pages.

## Configuration rollback

- [ ] Record the Web.config transform source and target profile, e.g. Dev/Test/Prod/Scan/LegacyIe.
- [ ] Keep a backup of the external configuration directory, but do not write real configuration content into the evidence
      package.
- [ ] Record `ExternalCfgPath`, the environment variable key names, the machine-level configuration location and how
      certificates are referenced.
- [ ] Cookie SSL, HSTS, customErrors, security headers and machineKey changes must be individually rollbackable.
- [ ] After a configuration rollback, check the generic error page, sign-in, logs and the system health page.

## Database rollback

- [ ] Confirm before release that a full database backup is available.
- [ ] Record the executed migration scripts, the manifest, execution order, executor and execution time.
- [ ] If a migration script includes a rollback script, verify the rollback path on an isolated test database first.
- [ ] If a migration is irreversible, confirm the backup-restore window and business downtime impact before release.
- [ ] After a database rollback, run read-only compatibility checks and key business smoke; do not try unverified scripts
      directly in a production window.

## Evidence retention

- [ ] Keep the pre-release gate output: release assets, public documentation, default credentials, the compliance baseline
      and the necessary business acceptance.
- [ ] Keep the release package manifest: the file list, SHA256, the exclusion check and version information.
- [ ] Keep the post-release smoke: entry access, sign-in, back-office permissions, log audits and error-page event IDs.
- [ ] If a rollback occurs, record the trigger reason, event ID, rollback start/end time and unresolved risks.

## Follow-up evidence

Real IIS, TLS, application pool ACLs, target SQL Server 2016/2017/2019, enterprise scanning tools and business-owner
sign-off are all environment-triggered evidence. Without the target environment, do not claim completion — record it only as
an item awaiting evidence.
