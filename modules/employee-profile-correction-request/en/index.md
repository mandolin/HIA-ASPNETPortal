---
layout: default
lang: en
parent: Learn
nav_order: 23
permalink: /en/modules/employee-profile-correction-request/
title: Employee Profile Correction Request (EmployeeProfileCorrectionRequest)
---
# Employee Profile Correction Request (EmployeeProfileCorrectionRequest)

> Module directory: `src/Portal/DesktopModules/EmployeeProfileCorrectionRequest/`｜View (form included): `EmployeeProfileCorrectionRequest.ascx`｜**No standalone edit page** (the form and the list live in the same `ascx`)
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.

## Purpose and prerequisites

Business-module sample for employee-profile correction requests: when an employee finds an error in their own profile they **raise a correction request** (choose the field to correct, enter the proposed value, write the reason), which then goes on for further handling.

It pairs with [Employee profile confirmation]({{ site.baseurl }}/modules/employee-profile-confirm/): that one **confirms read-only**, this one **starts a correction**. A correction is **not a direct data change** — what is submitted is a request, not a write.

Prerequisites:

1. You are signed in and the current account has an employee profile bound (the module reads `.GetCurrentProfileForUser(`).
2. The module must be placed on a tab before it appears.
3. Know which field you want to correct — the correctable fields are limited by the drop-down (`FieldNameList`), not arbitrary.

## Operating steps

**Raise a correction request**

1. Open the tab that hosts the module and check the **current profile** rendered read-only at the top.
2. Choose the field to correct in the **correction field** drop-down (`FieldNameList`).
3. Enter the desired value in **proposed value** (`ProposedValueTextBox`).
4. Write why the change is needed in **request note** (`RequestNoteTextBox`) — this is what the handling side relies on.
5. Click **submit** (`SubmitButton`).
6. The request appears in the list below, together with the timeline event for this submission.

**Review the correction requests I raised**

No action needed: the module reads the current user's recent requests (`.GetRecentRequestsForUser(`) as it loads.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Correction field | `FieldNameList` | Drop-down | — | No (no markup-level required validator) | Only fields listed in the drop-down |
| Proposed value | `ProposedValueTextBox` | Single-line text | 512 | No (no markup-level required validator) | **Whether the value is valid is judged in code per target field** |
| Request note | `RequestNoteTextBox` | Multi-line text | 1000 | No (no markup-level required validator) | Judged in code |

Control ids, types and maximum lengths come from the markup in `EmployeeProfileCorrectionRequest.ascx`, **not from an estimate in this document**. This module has **no standalone `Edit*.aspx`**.

## Required and validation

- **No required validators in the markup**: `EmployeeProfileCorrectionRequest.ascx` contains **0** `RequiredFieldValidator` controls. Required-ness is **decided in code at submit time**.
- **The validity of the proposed value depends on the target field**: the same `ProposedValueTextBox` means different things for different fields (a date field versus a text field), so markup cannot validate it uniformly — **a wrong format is not flagged on the page, only rejected after submit**.
- **Write a real request note** (within 1000 characters): it is what the handling side relies on; an empty note leaves them nothing to judge by.

## Storage and audit

- **Audit**: submitting writes an operations audit record — event `EmployeeProfileCorrectionRequested`, target type `EmployeeProfileCorrectionRequestTargetType`, filed under `BusinessModuleCategory`.
- **Denied access is recorded too**: the `AccessDenied` event is written to the audit as well.
- **Reads**: `.GetCurrentProfileForUser(` for the current profile; `.GetRecentRequestsForUser(` for the current user's recent requests; `.GetSingleUser(` for the user record.
- **What is submitted is a request, not a profile change**: the actual profile change happens in the later handling step, not inside this module.

## Permissions

- Both the current profile and the request list are filtered by **the current user**.
- When no user or profile can be resolved the module takes the no-permission path and writes `AccessDenied`.
- The module offers no "see other people's correction requests" and no direct approval.

## Common questions

**The profile did not change after submitting** — expected. This module submits a **request**, not a data change; the profile change takes effect in the later handling step.

**The proposed value was rejected** — the value does not match the target field's format or allowed values (`ProposedValueTextBox` has no format validator in markup; the decision is server-side).

**I cannot see other people's requests** — by design: the list is a personal view.

**I want to change the profile directly** — this module cannot do that; every profile change is raised as a request.

## Boundaries and what it does not do

- This module **does not modify the profile directly**; it only raises a correction request.
- It **runs no approval flow**: submitting hands the request on for later handling.
- It has **no standalone edit page** — form and list live in the same `ascx`.
- Correctable fields are **limited by `FieldNameList`**; not every field can be corrected.
- Difference from [Employee profile confirmation]({{ site.baseurl }}/modules/employee-profile-confirm/): that one is read-only confirmation (no input controls), this one starts a correction (3 input controls plus submit).

<!-- build-stamp: 821AE102FD3B -->
