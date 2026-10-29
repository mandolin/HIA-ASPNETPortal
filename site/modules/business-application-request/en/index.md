---
layout: default
lang: en
parent: Learn
nav_order: 19
permalink: /en/modules/business-application-request/
title: Business Application (BusinessApplicationRequest)
---
# Business Application (BusinessApplicationRequest)

> Module directory: `src/Portal/DesktopModules/BusinessApplicationRequest/`｜View (form included): `BusinessApplicationRequest.ascx`｜**No standalone edit page** (the form and the list live in the same `ascx`)
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.

## Purpose and prerequisites

Submission module for **abstract business applications**: raise one **business application** (title, category, summary, body); once submitted it moves on for further handling, and the module lists the applications **the current user** submitted recently below the form.

Why "abstract": the module carries only a **generic application skeleton** and no concrete business fields (no research, well-design or intelligence fields are part of its field set). Concrete business fields belong to their own dedicated modules.

Prerequisites:

1. You are signed in. The module reads the current user record once (`.GetSingleUser(`); when no user can be resolved it takes the no-permission path.
2. The module must be placed on a tab before it appears.
3. To see "applications I submitted", you must be signed in as **the same account** — the list returns only the current user's records, never everyone's.

## Operating steps

**Submit an application**

1. Open the tab that hosts the module and fill in the **title** (`TitleTextBox`).
2. Pick a value from the **category** drop-down (`CategoryList`).
3. Fill in the **summary** (`SummaryTextBox`) and the **body** (`BodyTextBox`).
4. Click **submit** (`SubmitButton`).
5. The new application appears in the list below the form, together with the timeline event this submission produced.

**Review the applications you submitted**

No action needed: the module reads the current user's recent applications as it loads. The list is a **personal view** — you cannot see other people's applications.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Title | `TitleTextBox` | Single-line text | 200 | No (no markup-level required validator) | Judged in code; an empty value is rejected |
| Category | `CategoryList` | Drop-down | — | No (no markup-level required validator) | Value comes from the drop-down items |
| Summary | `SummaryTextBox` | Single-line text | 500 | No (no markup-level required validator) | Judged in code |
| Body | `BodyTextBox` | Multi-line text | 4000 | No (no markup-level required validator) | Judged in code |

Control ids, types and maximum lengths come from the markup in `BusinessApplicationRequest.ascx`, **not from an estimate in this document**. This module has **no standalone `Edit*.aspx`** — the form lives in the view control.

## Required and validation

- **No required validators in the markup**: `BusinessApplicationRequest.ascx` contains **0** `RequiredFieldValidator` controls. Required-ness is therefore **decided in code at submit time**, not by a red hint on the page — an empty title or body is rejected server-side, with a different shape of feedback than a markup validator would give.
- **Maximum lengths are hard markup constraints**: `TitleTextBox` 200, `SummaryTextBox` 500, `BodyTextBox` 4000; anything longer is stopped by `MaxLength`.
- **Do not read "the page did not object" as "the value is fine"**: feedback for this module arrives after submission.

## Storage and audit

- **Audit**: submitting writes an operations audit record — event `BusinessApplicationSubmitted`, target type `BusinessApplicationTargetType`, filed under `BusinessModuleCategory`. So "who submitted which business application and when" **is traceable**.
- **Reads**: `.GetRecentApplicationsForUser(` for the current user's recent applications; `.GetSingleUser(` for the user record.
- **Where the business record is persisted is carried by the business repository**; this page does not restate table names — to verify, follow the repository calls in `BusinessApplicationRequest.ascx.cs` (this document describes only source facts it can trace, and does not guess storage details it has not checked).

## Permissions

- The list is filtered by **the current user**: only your own applications are visible.
- When no user record can be resolved the module takes the no-permission path.
- The module offers no "see everyone's applications" entry — that is an administrative capability and does not live here.

## Common questions

**My submission does not appear in the list** — check that you are looking as the same account (the list returns only the current user's records), then check whether the submission actually succeeded (a server-side rejection produces no new row).

**The body is cut off / will not save** — `BodyTextBox` is capped at 4000; the excess is stopped by `MaxLength` rather than reported.

**I cannot see other people's applications — is the permission broken?** — No. The personal view is this module's designed semantics.

**I need to submit an application with concrete business fields** — this module is an **abstract skeleton** and holds no concrete business fields; use the dedicated module for that business.

## Boundaries and what it does not do

- This module **carries no concrete business fields** (research, well design, intelligence, …); it offers only the generic application skeleton.
- It **provides no administrative full list**, no approval flow, no withdrawal, no attachments.
- It has **no standalone edit page** — form and list live in the same `ascx`, so editing and viewing are one page state rather than two pages.
- This page depends on no runtime screenshots; every statement traces back to source markup and to audit event names that have been verified.
- Difference from [Expense reimbursement]({{ site.baseurl }}/modules/expense-reimbursement/) and [Leave request]({{ site.baseurl }}/modules/leave-request/): those two are **concrete businesses** with their own business tables and attachments; this module is the **abstract skeleton**.

<!-- build-stamp: D82FAB0B02A9 -->
