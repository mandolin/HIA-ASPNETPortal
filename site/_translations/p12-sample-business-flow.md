---
layout: default
title: Sample business flow
lang: en
parent: 学习
nav_order: 1
---

# Employee profile correction sample business flow

This page describes the minimal enterprise business path currently uses for demonstration and regression: an ordinary
employee signs in with an existing account and submits a profile correction request based on a valid employee binding; an
administrator reviews the request in the back office and tracks it through work items, audits and the employee directory.

## Boundaries

1. This path is for development, testing and demonstration — it is not a production initialisation process.
2. Sample data creates no user passwords and stores no credentials.
3. An employee number is only one of the login identifiers, not an independent password or independent authentication
   credential.
4. Only field-level text corrections are covered; high-sensitivity data such as attachments, phone numbers, national ID
   numbers, salary and performance is not handled.
5. The current work-item capability is lightweight: it includes no transfer, delegation, co-signing, notification or
   general-purpose workflow designer.

## Automated-side acceptance

Run the evidence package script:

```powershell
dev/scripts/New-PortalP12EvidencePackage.ps1
```

The script orchestrates the following read-only checks and, by default, runs one solution build:

1. Business identity gate.
2. Lightweight work-item gate.
3. Business permission and audit gate.
4. Solution build.

Evidence is written to an internal evidence directory by default; if that directory does not exist, it falls back to
`temp/evidence/p12.5/`.

## Sample data SQL

Generate development/test sample SQL:

```powershell
dev/scripts/New-PortalP12SampleScenarioSql.ps1 -PortalUserName P12EmployeeDemo
```

The default output is:

```text
temp/p12.5/PortalP12SampleScenario.sql
```

Before running it, prepare a portal test user named `P12EmployeeDemo` in a development or test database, and make sure the
business migration scripts have been applied. The generated SQL fills in the organisation, employee, user profile and
account-employee binding, but never creates users or modifies passwords.

## Manual regression path

1. Sign in with an ordinary employee test account.
2. Open the employee profile confirmation module and confirm the employee number, name, organisation and other profile
   data are visible.
3. Open the employee profile correction request module, pick a low-sensitivity field, enter a suggested value and submit.
4. Sign in to the back office with an administrator account and open
   `Admin/EmployeeProfileCorrectionRequests.aspx`.
5. Confirm the submitted request appears in the list.
6. Open `Admin/WorkItems.aspx` and confirm the lightweight work item for that request exists.
7. Perform one of approve, reject or close in the profile correction back office.
8. Open the operations audit page and confirm the submission and review actions can be traced.
9. Open the work-item back office again and confirm the corresponding work item status has been synced to completed or
   cancelled.

## Expected results

1. The employee number can serve as a login identifier, but the existing test user's password is still used.
2. An account with no employee binding cannot submit a profile correction request.
3. A successful profile correction submission produces an operations audit record and a lightweight work item.
4. A successful administrator review produces a review audit record and syncs the work item status.
5. Current permissions still retain `Admins` compatibility, but code entry points already use fine-grained business
   permissions.

## Follow-up deepening directions

1. Once a role-permission UI exists, assign the current fine-grained permissions to roles such as HR, business reviewers
   and read-only observers.
2. Bring read-type auditing, audit detail levels and the audit retention policy into system settings and release-time
   configuration.
3. In a later Workflow cycle, add transfer, delegation, co-signing, notification and more business objects.
