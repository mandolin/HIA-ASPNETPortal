---
layout: default
title: Leave and expense: from filling in to appearing in to-dos
lang: en
parent: Learn
nav_order: 2
---

# Leave and expense: from filling in to appearing in to-dos

These two look almost the same: one form, one submission, a row in the database, and one more to-do on "My to-do items". This page covers both together.

## Before you start

- You are signed in. Both modules take one user record for the current user; when it cannot, they treat you as having no permission.
- Each module appears only once it is mounted on a tab; if you cannot find the tab, the deployment has not exposed it — ask an administrator.
- Attachments are **optional** in both cases — check whether you really need to attach supporting material or a receipt. Uploads themselves are constrained by the attachment type and path policy.

## Submit a leave request

1. Open the tab that carries the module and pick the **leave type** in the drop-down.
2. Fill in the **start date** and the **end date**.
3. Fill in the **number of days**.
4. Fill in the **reason**.
5. (Optional) Attach supporting material.
6. Click **submit**.

Afterwards: the request is stored, **a matching work item appears on "My to-do items"**, and the request shows up in the list below the form.

## Submit an expense claim

1. Open the tab that carries the module and pick the expense **category** in the drop-down.
2. Fill in the **amount**.
3. Fill in the **reason**.
4. (Optional) Attach a receipt.
5. Click **submit**.

Afterwards the claim is stored as well, and a work item is generated on "My to-do items".

## How the two differ

| | Leave | Expense |
| --- | --- | --- |
| Business table | `PortalBiz_LeaveRequests` | `PortalBiz_ExpenseReimbursements` |
| Fields | Leave type, start and end date, days, reason, attachment | Category, amount, reason, attachment |
| The field most likely to bite | The date range (end before start) | The amount format (not a number) |

Apart from that they are structurally the same: **the same to-do write path**, both lists are from your own point of view, and neither has a separate editing page.

## After submitting

- The list you see holds **only your own records** — it is filtered by the current user, and there is no "see other people's leave or claims" entry point.
- **Approval does not live in these two modules.** A submission is stored and a to-do is generated; approval happens on the to-do side or through a manual process.
- A submitted record **cannot be changed here**, and there is no withdrawal.
- To see the to-do, go to "My to-do items" — remembering that it **presents, it does not process**: it does not approve, change data, or write back to the business table.

## When the submission did not go through

- **Required fields are judged after you submit.** Neither form carries red required-field hints (the markup layer has zero required validators), so a mistake is rejected by the server after you press submit.
- **Dates filled in but the submission fails** — the format is not accepted, or the end date is earlier than the start date. There is no date comparison validator on the page; every check happens on the server.
- **Days as a decimal or as text fails** — a parsable number is required.
- **An amount filled in but the submission fails** — the value is not an acceptable number; the markup layer declares it as a plain text box and nothing more.
- **Nothing appears on "My to-do items" after submitting** — first check whether the submission actually succeeded: a rejected one stores nothing and produces no work item.
- **An attachment fails to upload** — that is the upload type and path policy at work, unrelated to required fields.

## What this page does not cover

- The leave field list and its validation details → [the leave request module page]({{ site.baseurl }}/en/modules/leave-request/)
- The expense field list and its validation details → [the expense reimbursement module page]({{ site.baseurl }}/en/modules/expense-reimbursement/)
- How to-dos are presented and where handling happens → [my to-do items]({{ site.baseurl }}/en/modules/my-work-items/)
- The similar generic request skeleton → [business requests: from submission to closure]({{ site.baseurl }}/en/business-application-workflow/)

