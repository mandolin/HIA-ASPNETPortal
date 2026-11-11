# My Work Items (MyWorkItems)

> Module directory: `src/Portal/DesktopModules/MyWorkItems/`｜View: `MyWorkItems.ascx`｜**No standalone edit page and no input controls at all**
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"](../user-guide.md) section of the user guide.

## Purpose and prerequisites

Front-end **"My Work Items" aggregation entry**: it gathers the **unfinished work items** scattered across the business modules into one place, showing only those assigned to **the current user** or to **role keys the user holds**.

**It renders, it does not handle** — this is the key to reading this module: work items are seen here but **no in-place handling is provided** (no approving here, no changing data here); handling happens back in the module that produced the item.

Prerequisites:

1. You are signed in; the module reads the current user record (`.GetSingleUser(`) and permission keys (`.GetPermissionKeysByUserName(`).
2. The module must be placed on a tab before it appears.
3. Work items come from **other modules** (for example [Expense reimbursement](./expense-reimbursement.md), [Leave request](./leave-request.md), [Business application](./business-application-request.md)); this module creates none itself.

## Operating steps

**Review my work items**

No action needed: the module reads the current user's work items (`.GetWorkItemsForUser(`) as it loads.

**Filtering**

- Click **all** (`AllFilterButton`) to see every unfinished work item.
- Click **overdue** (`OverdueFilterButton`) to see only items that are past due.

**Handling a work item**

This module **provides no in-place handling**. Follow the source marked on the item and handle it back in the module that produced it (a claim goes back to [Expense reimbursement](./expense-reimbursement.md), and so on).

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| (no input fields) | — | — | — | — | — |

This module has **no input controls at all** — `MyWorkItems.ascx` contains **0** input controls (`TextBox` / `DropDownList` / `CheckBox` / `HtmlTextArea`); the only interaction is two filter buttons (`AllFilterButton` / `OverdueFilterButton`). This is not a documentation omission — the module is a read-only aggregation by design.

## Required and validation

- **Nothing is required**: there are no input controls, and the `RequiredFieldValidator` count is **0**.
- **No format validation**: there is no field to fill.
- **The only filtering is the visible scope**: only **unfinished** items assigned to the current user or to role keys they hold — completed items do not appear, and other people's do not appear.

## Storage and audit

- **Audit**: **the view action itself** writes an operations audit record — event `WorkItemsViewed`, target type `WorkItemTargetType`, filed under `BusinessModuleCategory`. So "who looked at their own work items and when" is traceable (unlike most read-only modules).
- **Reads**: `.GetWorkItemsForUser(` for the items; `.GetPermissionKeysByUserName(` for permission keys; plus `.GetPortalSettings(` / `.GetModulesByModuleDefId(` / `.GetModuleDefinitions(` / `.GetTrustedPackages(` to resolve the source modules and trusted deployment packages.
- **This module writes no business data**: work items are produced by the business modules; here they are only aggregated.

## Permissions

- The visible scope is **the current user plus the role keys they hold**, unfinished items only.
- When no user record can be resolved the module takes the no-permission path.
- The module offers no administrative "see everyone's work items" view.

## Common questions

**A work item is missing** — three possibilities: it is already completed (only unfinished items are listed), it is assigned to someone else or to another role key, or its source module is outside the aggregation scope.

**I want to approve right here** — that is not possible, and **that is by design**: this module **provides no in-place handling**; handling happens in the source module.

**"Overdue" shows nothing** — there is nothing overdue right now; click "all" to confirm that aggregation itself works.

**Why is even viewing audited?** — work-item visibility involves assignment and role keys, which is a sensitive read; the `WorkItemsViewed` event is the trace left for it.

## Boundaries and what it does not do

- This module **provides no in-place handling**: no approving, no data changes, no write-back to the source module. **This is intended design, not a defect.**
- It **lists unfinished items only**: completed ones do not appear.
- It **offers no administrative view of all work items**.
- It has **no standalone edit page and no input controls**.
- The **semantics of a work item are defined by its source module**: this module only aggregates and renders, it does not interpret business meaning.
