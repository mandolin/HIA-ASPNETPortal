---
layout: default
lang: en
parent: Learn
nav_order: 23
permalink: /en/modules/leave-request/
title: Leave Request (LeaveRequest)
---
# Leave Request (LeaveRequest)

> Module directory: `src/Portal/DesktopModules/LeaveRequest/`｜View (form included): `LeaveRequest.ascx`｜**No standalone edit page** (the form and the list live in the same `ascx`)
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.

## Purpose and prerequisites

Leave-request module: an employee submits a **leave request** (leave type, start and end dates, number of days, reason, optionally supporting evidence). On submit the business record is persisted to **`PortalBiz_LeaveRequests`** and a work item is written into **"My Work Items"**.

The module also lists the **current user's own** leave history below the form.

Prerequisites:

1. You are signed in; the module reads the current user record (`.GetSingleUser(`).
2. The module must be placed on a tab before it appears.
3. If you want to attach evidence, have the file ready — attachments are **optional**.

## Operating steps

**Submit a leave request**

1. Open the tab that hosts the module and pick a value from the **leave type** drop-down (`ddlLeaveType`).
2. Fill in the **start date** (`txtStartDate`) and the **end date** (`txtEndDate`).
3. Fill in the **number of days** (`txtDays`).
4. Fill in the **reason** (`txtReason`).
5. (Optional) attach supporting evidence through the attachment control (`fileAttach`).
6. Click **submit** (`btnSubmit`).
7. After submitting: the record lands in `PortalBiz_LeaveRequests`, a work item is created in "My Work Items", and the row appears in the list below the form.

**Review the leave I submitted**

No action needed: the module reads the current user's history (`.GetRecentByUser(`) as it loads.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Leave type | `ddlLeaveType` | Drop-down | — | No (no markup-level required validator) | Value comes from the drop-down items |
| Start date | `txtStartDate` | Single-line text | no `MaxLength` in source | No (no markup-level required validator) | **Date format judged in code**; a wrong format is rejected after submit |
| End date | `txtEndDate` | Single-line text | no `MaxLength` in source | No (no markup-level required validator) | Same as above; the start/end ordering is judged in code |
| Number of days | `txtDays` | Single-line text | no `MaxLength` in source | No (no markup-level required validator) | **Numeric format judged in code** |
| Reason | `txtReason` | Single-line text | no `MaxLength` in source | No (no markup-level required validator) | Judged in code |
| Supporting evidence | `fileAttach` | File upload control | — | Optional | Governed by the upload path policy |

Control ids and types come from the markup in `LeaveRequest.ascx`, **not from an estimate in this document**. This module has **no standalone `Edit*.aspx`**.

## Required and validation

- **No required validators in the markup**: `LeaveRequest.ascx` contains **0** `RequiredFieldValidator` controls. Required-ness is **decided in code at submit time**.
- **Dates and day count are the highest-risk fields**: `txtStartDate` / `txtEndDate` / `txtDays` are plain text boxes with no date-comparison validator — **an end date earlier than the start date is not stopped in markup**, only server-side.
- **Do not read "the page did not flag it" as "the range is valid".**

## Storage and audit

- **Storage**: on submit the business record is persisted to **`PortalBiz_LeaveRequests`** (repository call `.CreateRequest(`).
- **Work item**: also written into "My Work Items" — **isomorphic** with [Business application]({{ site.baseurl }}/modules/business-application-request/) and [Employee profile correction request]({{ site.baseurl }}/modules/employee-profile-correction-request/).
- **Audit**: filed under `BusinessModuleCategory`.
- **Reads**: `.GetRecentByUser(` for the current user's history.

## Permissions

- The history list is filtered by **the current user**; you see only your own leave records.
- When no user record can be resolved the module takes the no-permission path.
- This module offers no approval flow and no "see other people's leave".

## Common questions

**It did not reach "My Work Items" after submitting** — confirm the submission actually succeeded; a server-side rejection persists nothing and creates no work item.

**I filled in the dates but submission failed** — `txtStartDate` / `txtEndDate` are not in an accepted format, or the range does not hold; the decision happens server-side.

**A decimal or text day count fails** — `txtDays` needs a parseable number.

**I cannot see other people's leave** — by design: the list is a personal view.

## Boundaries and what it does not do

- This module **does not run an approval flow**, nor a calendar view, remaining-entitlement calculation or conflict detection.
- It **offers no editing or withdrawal**.
- It has **no standalone edit page** — form and list live in the same `ascx`.
- Placement tabs and instance counts are delivery-time snapshots; **re-check this page after adding or migrating a tab**.
- Isomorphic with [Expense reimbursement]({{ site.baseurl }}/modules/expense-reimbursement/) (each has its own business table plus the same work-item write), differing only in fields.

<!-- build-stamp: C7492B74E8C4 -->
