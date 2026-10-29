---
layout: default
lang: en
parent: Learn
nav_order: 17
permalink: /en/modules/enterprise-capability-workbench/
title: Enterprise Capability Workbench (EnterpriseCapabilityWorkbench)
---
# Enterprise Capability Workbench (EnterpriseCapabilityWorkbench)

> Module directory: `src/Portal/DesktopModules/EnterpriseCapabilityWorkbench/`｜View (form included): `EnterpriseCapabilityWorkbench.ascx`｜**No standalone edit page** (form, list and comments live in the same `ascx`)
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.

## Purpose and prerequisites

Front-end module for the enterprise-capability workbench: it works around **collaboration items** — submit an item, **resubmit** after it is returned, **add a participant comment**, and track by priority and due time.

It has the **most input controls** of this batch (7) and is the only module with three distinct actions: submit / resubmit / comment.

Prerequisites:

1. You are signed in; the module reads the current user record (`.GetSingleUser(`).
2. The module must be placed on a tab before it appears.
3. To **resubmit or comment** you must first select an existing item in the list — those two actions target **an existing item**, they do not create one.

## Operating steps

**Submit a new item**

1. Open the tab that hosts the module and fill in the **title** (`TitleTextBox`).
2. Choose the **item type** (`ItemTypeList`) and the **priority** (`PriorityList`).
3. Fill in the **summary** (`SummaryTextBox`) and the **due UTC time** (`DueUtcTextBox`).
4. Optionally fill in the **detailed description** (`DescriptionTextBox`).
5. Click **submit** (`SubmitButton`). The item enters the timeline with event `CollaborationItemSubmitted`.

**Resubmit after being returned**

1. Find the returned item in the list and select it.
2. Correct the content and click **resubmit** (`ResubmitButton`). Event `CollaborationItemResubmitted`.

**Add a participant comment**

1. Select the target item.
2. Write the content in **comment** (`ParticipantCommentTextBox`).
3. Click **add comment** (`AddParticipantCommentButton`). Event `CollaborationItemCommentAdded`.

**Review the items I am involved in**

No action needed: the module reads the current user's recent items (`.GetRecentItemsForUser(`) as it loads, together with participants and visible events.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Title | `TitleTextBox` | Single-line text | 200 | No (no markup-level required validator) | Judged in code |
| Item type | `ItemTypeList` | Drop-down | — | No (no markup-level required validator) | Value comes from the drop-down items |
| Priority | `PriorityList` | Drop-down | — | No (no markup-level required validator) | Value comes from the drop-down items |
| Summary | `SummaryTextBox` | Single-line text | 500 | No (no markup-level required validator) | Judged in code |
| Due UTC time | `DueUtcTextBox` | Single-line text | 19 | No (no markup-level required validator) | **UTC time format judged in code**; capped at 19 characters (the `yyyy-MM-ddTHH:mm:ss` shape) |
| Detailed description | `DescriptionTextBox` | Multi-line text | 4000 | No (no markup-level required validator) | Judged in code |
| Participant comment | `ParticipantCommentTextBox` | Multi-line text | 1000 | No (no markup-level required validator) | Judged in code |

Control ids, types and maximum lengths come from the markup in `EnterpriseCapabilityWorkbench.ascx`, **not from an estimate in this document**. This module has **no standalone `Edit*.aspx`**.

## Required and validation

- **No required validators in the markup**: `EnterpriseCapabilityWorkbench.ascx` contains **0** `RequiredFieldValidator` controls. Required-ness is **decided in code at submit time**.
- **The due time is the highest-risk field**: `DueUtcTextBox` is capped at 19 characters and holds **UTC** — a local time, or a value of a different shape, is not stopped in markup, only server-side.
- **All three actions share the same fields**: resubmitting and commenting reuse the same form area, so take care which button you press (`SubmitButton` / `ResubmitButton` / `AddParticipantCommentButton`).

## Storage and audit

- **Audit**: each of the three actions has its own audit event — `CollaborationItemSubmitted` (submit), `CollaborationItemResubmitted` (resubmit), `CollaborationItemCommentAdded` (comment), with target type `CollaborationItemTargetType`, filed under `BusinessModuleCategory`.
- **Writes**: `.CreateSubmittedItem(` persists the item.
- **Reads**: `.GetRecentItemsForUser(` for the current user's recent items; `.GetParticipantsForItems(` for participants; `.GetVisibleEventsForItems(` for visible events.
- **Fallback path**: `.GetFallbackItems(` — used when item reads are unavailable. **This is intended behaviour, not a defect**: it keeps the workbench rendering instead of failing the whole page when a dependency is down; content served by the fallback may differ from the primary path, so verify against the primary path.

## Permissions

- The list is read **for the current user**; participants and visible events come along with the items.
- When no user record can be resolved the module takes the no-permission path.
- The module offers no administrative "see everyone's items" view.

## Common questions

**My item does not appear after submitting** — first confirm the submission actually succeeded; a server-side rejection persists no item.

**I filled in the due time but it failed** — `DueUtcTextBox` needs **UTC** and no more than 19 characters; the decision is server-side.

**Resubmit / comment had no effect** — both actions target **the selected item**; without a selection they do nothing.

**What I see does not match what I expect** — the `.GetFallbackItems(` fallback path may have been used (intended behaviour when a dependency is unavailable); verify against the primary path.

## Boundaries and what it does not do

- This module **runs no approval flow and no state-machine decision**: submit / resubmit / comment are its entire action semantics.
- It **offers no administrative full list**.
- It has **no standalone edit page** — form, list and comments live in the same `ascx`.
- **The `.GetFallbackItems(` fallback path is intended behaviour**: content is still rendered when a dependency is unavailable, but it is not guaranteed to match the primary path.
- Difference from [Expense reimbursement]({{ site.baseurl }}/modules/expense-reimbursement/) and [Leave request]({{ site.baseurl }}/modules/leave-request/): those are **one-way submissions** (submit, then only view); this module supports **resubmitting and commenting** — two-way collaboration semantics.

<!-- build-stamp: A61F759520B8 -->
