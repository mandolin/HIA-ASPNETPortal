---
layout: default
lang: en
parent: Learn
nav_order: 14
permalink: /en/modules/employee-profile-confirm/
title: Employee Profile Confirmation (EmployeeProfileConfirm)
---
# Employee Profile Confirmation (EmployeeProfileConfirm)

> Module directory: `src/Portal/DesktopModules/EmployeeProfileConfirm/`｜View: `EmployeeProfileConfirm.ascx`｜**No standalone edit page and no input controls at all**
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.

## Purpose and prerequisites

Business-module sample for employee-profile confirmation: an employee **only confirms the low-sensitivity profile currently bound to them** — nothing is modified and no form is filled. On load the module reads the current user's profile and renders it as **read-only labels**; the user clicks **confirm** once and is done.

**This is "confirmation", not "editing"** — the module has no input boxes. To change the profile, raise a correction request in [Employee profile correction request]({{ site.baseurl }}/modules/employee-profile-correction-request/).

Prerequisites:

1. You are signed in and the current account **has an employee profile bound** (the module reads `.GetCurrentProfileForUser(`).
2. The module must be placed on a tab before it appears.
3. When no profile or no user can be resolved the module takes the no-permission path (not "a blank page" — it takes the no-permission branch).

## Operating steps

**Confirm my profile**

1. Open the tab that hosts the module and check the read-only profile items listed on the page (9 read-only labels).
2. If everything is correct, click **confirm** (`ConfirmButton`).
3. The confirmation is recorded as the audit event `EmployeeProfileConfirmed`.

**Something in the profile is wrong**

Do not try to fix it here — there are no input boxes. Raise a correction request in [Employee profile correction request]({{ site.baseurl }}/modules/employee-profile-correction-request/).

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| (no input fields) | — | — | — | — | — |

This module has **no input controls at all** — `EmployeeProfileConfirm.ascx` contains **0** input controls (`TextBox` / `DropDownList` / `CheckBox` / `HtmlTextArea`); the page is built from 9 **read-only labels** plus one confirm button (`ConfirmButton`). This is not a documentation omission — the module is read-only by design.

## Required and validation

- **Nothing is required**: there are no input controls, and the `RequiredFieldValidator` count is **0**.
- **No format validation**: there is no field to fill.
- **The only check is a permission check**: when the current user's profile cannot be resolved the module takes the no-permission branch, so it never ends up "confirming an empty profile".

## Storage and audit

- **Audit**: confirming writes an operations audit record — event `EmployeeProfileConfirmed`, target type `EmployeeProfileConfirmationTargetType`, filed under `BusinessModuleCategory`. **"Who confirmed their own profile and when" is traceable.**
- **Denied access is recorded too**: the `AccessDenied` event is written to the audit as well.
- **Reads**: `.GetCurrentProfileForUser(` for the current profile; `.GetSingleUser(` for the user record.
- **This module writes no business data**: confirmation is an action record, not a profile change.

## Permissions

- Only **the current user's own** profile is rendered; there is no entry for viewing other people's profiles.
- When no user or profile can be resolved the module takes the no-permission path and writes an `AccessDenied` audit event.

## Common questions

**There are no input boxes — is it broken?** — No. **This module is read-only confirmation by design**; changing the profile goes through [Employee profile correction request]({{ site.baseurl }}/modules/employee-profile-correction-request/).

**I clicked confirm and nothing changed** — confirmation only writes an audit record and changes no data, so the page shows no "profile updated" change; check the audit side for `EmployeeProfileConfirmed`.

**It says I have no permission** — the current account may have no employee profile bound, or the user record cannot be resolved; that case writes `AccessDenied`.

## Boundaries and what it does not do

- This module **provides no editing capability**: no input controls, no edit page. Changing the profile must go through the correction-request flow.
- It **writes no business data** — audit only.
- It **offers no way to view other people's profiles**.
- This module is a **business-module sample**: its shape is reused by modules such as [Employee profile correction request]({{ site.baseurl }}/modules/employee-profile-correction-request/), so the "read-only + audit" semantics here are the baseline for the whole business-module family.
- This page depends on no runtime screenshots; every statement traces back to source markup and to audit event names that have been verified.

<!-- build-stamp: 7A3758339740 -->
