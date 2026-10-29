---
layout: default
lang: en
parent: Learn
nav_order: 17
permalink: /en/modules/expense-reimbursement/
title: Expense Reimbursement (ExpenseReimbursement)
---
# Expense Reimbursement (ExpenseReimbursement)

> Module directory: `src/Portal/DesktopModules/ExpenseReimbursement/`｜View (form included): `ExpenseReimbursement.ascx`｜**No standalone edit page** (the form and the list live in the same `ascx`)
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.

## Purpose and prerequisites

Expense-reimbursement module: an employee submits an **expense claim** (category, amount, reason, optionally an attached document). On submit the business record is persisted to **`PortalBiz_ExpenseReimbursements`** and a work item is written into **"My Work Items"** — so after submitting you can see it in [My work items]({{ site.baseurl }}/modules/my-work-items/).

The module also lists the **current user's own** reimbursement history below the form.

Prerequisites:

1. You are signed in; the module reads the current user record (`.GetSingleUser(`).
2. The module must be placed on a tab before it appears.
3. If you want to attach a document, have the file ready — attachments are **optional**, not a precondition for submitting.

## Operating steps

**Submit a claim**

1. Open the tab that hosts the module and pick a value from the **category** drop-down (`ddlCategory`).
2. Fill in the **amount** (`txtAmount`).
3. Fill in the **reason** (`txtReason`).
4. (Optional) attach the document through the attachment control (`fileAttach`).
5. Click **submit** (`btnSubmit`).
6. After submitting: the record lands in `PortalBiz_ExpenseReimbursements`, a work item is created in "My Work Items", and the row appears in the list below the form.

**Review the claims I submitted**

No action needed: the module reads the current user's history (`.GetRecentByUser(`) as it loads.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Category | `ddlCategory` | Drop-down | — | No (no markup-level required validator) | Value comes from the drop-down items |
| Amount | `txtAmount` | Single-line text | no `MaxLength` in source | No (no markup-level required validator) | **Amount format is judged in code**; non-numeric values are rejected |
| Reason | `txtReason` | Single-line text | no `MaxLength` in source | No (no markup-level required validator) | Judged in code |
| Document attachment | `fileAttach` | File upload control | — | Optional | Governed by the upload path policy |

Control ids and types come from the markup in `ExpenseReimbursement.ascx`, **not from an estimate in this document**. This module has **no standalone `Edit*.aspx`**.

## Required and validation

- **No required validators in the markup**: `ExpenseReimbursement.ascx` contains **0** `RequiredFieldValidator` controls. Required-ness is **decided in code at submit time**, not by a red hint on the page.
- **The amount is the field most likely to go wrong**: `txtAmount` is declared as a plain text box in markup, and the numeric check happens server-side — a non-numeric value produces no on-page hint, just a rejection after submit.
- **Attachments are optional**: you can submit without one; if you do attach one, the upload path policy applies.

## Storage and audit

- **Storage**: on submit the business record is persisted to **`PortalBiz_ExpenseReimbursements`** (repository call `.CreateRequest(`).
- **Work item**: a work item is also written into "My Work Items" — **isomorphic** with [Business application]({{ site.baseurl }}/modules/business-application-request/) and [Employee profile correction request]({{ site.baseurl }}/modules/employee-profile-correction-request/).
- **Audit**: filed under `BusinessModuleCategory` (this module's audit category matches the other business modules).
- **Reads**: `.GetRecentByUser(` for the current user's history.

## Permissions

- The history list is filtered by **the current user**; you see only your own claims.
- When no user record can be resolved the module takes the no-permission path.
- This module offers no approval flow and no "see other people's claims".

## Common questions

**It did not reach "My Work Items" after submitting** — first confirm the submission actually succeeded (a server-side rejection persists nothing and creates no work item); the work item appears only after a successful submit.

**I filled in the amount but submission failed** — `txtAmount` does not hold an acceptable number. It has no format validator in markup; the decision happens server-side.

**I cannot see other people's claims** — by design: the list is a personal view.

**Attachment upload fails** — governed by the upload path policy (type / path), unrelated to required fields.

## Boundaries and what it does not do

- This module **does not run an approval flow**: submitting persists the record and creates the work item; approval happens on the work-item side or in a manual process.
- It **offers no editing or withdrawal**: submitted records are not modified inside this module.
- It has **no standalone edit page** — form and list live in the same `ascx`.
- Placement tabs and instance counts are delivery-time snapshots; **re-check this page after adding or migrating a tab**.
- Isomorphic with [Leave request]({{ site.baseurl }}/modules/leave-request/) (each has its own business table plus the same work-item write), differing only in fields.

<!-- build-stamp: ABD08987F6E1 -->
