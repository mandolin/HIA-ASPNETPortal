---
layout: default
lang: en
parent: Operate (section)
permalink: /en/audit-retention-policy/
title: Audit retention policy
---
# Audit retention policy

Basis: the OWASP Logging Cheat Sheet (primary source) — log data "**must not be destroyed before the retention period
expires, nor kept beyond that period**"; legal, regulatory and contractual obligations may affect the concrete period.
Date: 2026-09-26

## 1. Scope

This policy covers the **operational audit table `PortalCfg_OperationAudits`** (including security events such as
authentication failures and authorisation failures).

It does **not** apply to runtime diagnostic logs (`Portal.Diagnostics.*`) — diagnostics serve troubleshooting and usually have
a shorter retention period, defined separately (see section 6).

## 2. Retention periods (defaults)

| Phase | Default period | Note |
| --- | --- | --- |
| Online queryable | **180 days** | Records inside the default query window of the audit admin page (`Admin/OperationAudits.aspx`) |
| Archive retention | **730 days (about 2 years)** | After leaving the online window, records move to read-only archive; the period can be adjusted for compliance or contractual requirements |
| Disposal at expiry | **secure deletion** once the period expires | The deletion action itself must be recorded (deleting audit data is a high-risk operation) |

> The defaults are engineering recommendations; where regulation or contract requires longer or shorter, the **written
> confirmed period** overrides these defaults and must be registered in this document.

## 3. Suggested configuration keys (automatic enforcement not yet implemented)

| Key | Suggested default | Meaning |
| --- | --- | --- |
| `Portal.Audit.RetentionDays` | `180` | Days retained as online queryable |
| `Portal.Audit.ArchiveRetentionDays` | `730` | Archive retention days |

**Current state**: this policy is a **definition**; automatic archiving and expiry cleanup are **not yet implemented**. Until
automatic cleanup exists and operations has confirmed it, **no audit data may be deleted** (so that compliance is not breached
by deleting data without a basis).

## 4. Handling

1. **Archive**: records past the online window are exported to a **read-only archive** (not modifiable); archive files are kept
   under the backup policy.
2. **Deletion at expiry**: secure deletion after the archive period ends; the deletion must be recorded in a high-level
   audit / operations record stating scope and time.
3. **Prohibited**: audit data must not be deleted during the retention period on "disk / database capacity" grounds; if
   capacity becomes a problem, shrink the online window and expand the archive instead of deleting outright.

## 5. Responsibility and access control

- **Read access to audit data is restricted** (authorised roles only), and **access to audit data should itself be recorded and
  controlled** (OWASP protection requirement).
- Archiving and deletion are high-risk operations: they require authorisation and must leave a trace.
- Any change to the retention period goes through change management and must be registered by updating this document.

## 6. Difference from diagnostic logs

| Dimension | Operational audit | Runtime diagnostics |
| --- | --- | --- |
| Purpose | Compliance traceability, security event detection | Troubleshooting, performance and error localisation |
| Retention tendency | **Longer** (compliance) | **Shorter** (time-sensitive) |
| Typical period | 180 days online + 730 days archived | Per `Portal.Diagnostics` configuration (per-file size limit, etc.) |

The two **must not substitute for each other**: the audit records high-value state changes and security events; diagnostics
record runtime detail.

## 7. Export and search

- Search: `Admin/OperationAudits.aspx` supports filtering and paging by time range, category, action and target identifier (the
  query window is bounded by `MaximumQueryDays = 31`).
- Export: listed as a later candidate (a common requirement for compliance evidence).
- Alerting / centralisation: listed as a later candidate (SIEM integration is not in the current phase).

## 8. Later candidates

1. Automatic archiving and expiry cleanup (requires operations confirmation and a backup policy first).
2. Audit export capability.
3. Tamper-resistance enhancement (hash chain / read-only replica).
4. Centralised logging and alerting.
