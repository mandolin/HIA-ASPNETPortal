---
layout: default
lang: en
parent: 变更
permalink: /en/release-notes-template/
title: Release notes template
---
# Release notes template

> This template is for manually preparing each HIA-ASPNETPortal delivery note. Release notes are addressed to maintenance,
> test or deployment staff who may circulate them publicly; they must not record real connection strings, passwords, tokens,
> cookies, certificate private keys, production paths or **internal evidence paths**.

## Basic information

| Item | Content |
| --- | --- |
| Version | `1.0.0` |
| Release name | `<release name>` |
| Release date | `<yyyy-MM-dd>` |
| Release type | `<internal validation / test delivery / production candidate / formal release>` |
| Corresponding commit | `<short commit hash>` |
| Release package ID | `<ReleaseId from the release manifest>` |
| Applicable environments | `<Dev / Test / Prod / Scan / LegacyIe>` |

## Version positioning

State briefly what problem this release solves and which validation scenario it is aimed at; maturity and publicity framing
follow the external constraints of [versioning.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/versioning.md).

Example:

```text
This release focuses on validating the release package, deployment templates, operations evidence, documentation readiness
and release-note material, targeting the validation scenario <internal validation / test delivery / production candidate /
formal release>; its maturity and publicity framing follow [versioning.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/versioning.md).
```

## Main changes

| Category | Description |
| --- | --- |
| Features | `<user-visible features added or adjusted in this release>` |
| Operations | `<changes to the operations runbook, logging, audit, health checks or evidence packages>` |
| Documentation | `<changes to public documentation, deployment notes, test checklists or the documentation toolchain>` |
| Security | `<changes to security headers, credentials, permissions, audit or compliance boundaries>` |
| Database | `<changes to migration scripts, the compatibility matrix, schema or data fixes>` |

## Verification summary

| Verification item | Result | Note |
| --- | --- | --- |
| Build | `<pass / conditional pass / not run>` | `<summary>` |
| Release package manifest | `<pass / conditional pass / not run>` | `<failure count, warning count>` |
| Operations evidence | `<pass / conditional pass / not run>` | `<failure count, Pending count>` |
| Documentation evidence | `<pass / conditional pass / not run>` | `<failure count, Pending count>` |
| Public documentation gate | `<pass / not run>` | `<summary>` |
| Manual regression | `<pass / partial pass / not run>` | `<summary>` |

## Release and deployment notes

1. Confirm the target environment, external configuration, database and rollback owner per
   [deployment-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-checklist.md).
2. Prepare file, configuration, database and evidence rollback material per
   [deployment-rollback-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/deployment-rollback-guide.md).
3. Complete post-release health checks, the logging maintenance dry-run, audit queries and the backup reminder per
   [operations-runbook.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/operations-runbook.md).
4. Before releasing, do not put development connection strings, shared default passwords, real certificate private keys,
   production backups or internal evidence directories into the release package.

## Known limitations and deferred evidence

| Item | Status | Handling |
| --- | --- | --- |
| Real IIS / TLS / application pool ACL | `<evidence pending / evidence provided>` | Needs re-testing in the target environment. |
| SQL Server 2016 / 2017 / 2019 | `<evidence pending / evidence provided>` | Needs verification against a corresponding real instance. |
| Enterprise scanning tools | `<evidence pending / evidence provided>` | Needs the scanning tool, rule version and exceptions recorded. |
| HIA consumer / transport | `<evidence pending / evidence provided>` | Needs to wait for the HIA runtime integration. |
| Real business sign-off | `<evidence pending / evidence provided>` | Needs a business owner to confirm the sample flow. |

## Rollback entry points

| Rollback scope | Entry point |
| --- | --- |
| File release package | Use the previous release package or a confirmed file backup. |
| External configuration | Restore the pre-release configuration backup. |
| Database | Use the pre-release database backup or a reviewed rollback script. |
| Runtime state | Re-check health checks, logs and audit per the operations runbook. |

## Release conclusion

```text
<State whether this release may proceed to the next validation environment, whether evidence is still needed, and whether
to roll back or postpone.>
```
