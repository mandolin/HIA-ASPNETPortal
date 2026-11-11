---
layout: default
lang: en
parent: Learn
nav_order: 7
permalink: /en/leave-and-expense-review/
title: Leave and expense: the review side today (no administration entry point yet)
---
# Leave and expense: the review side today (no administration entry point yet)

This page is about the **administrator / approver** side. The conclusion first: **leave and expense currently have no administration review page and no working approve/reject entry point.** This page sets out what exists, so you do not spend a long time hunting through the backend.

The applicant's side — how they fill the form in and where it lands — is in [leave and expense: from filling in to appearing in to-dos]({{ site.baseurl }}/en/leave-and-expense-workflow/).

## What you can see: the administration "business to-dos" page

- Entry point: `/Admin/WorkItems.aspx`. Opening it requires either `BusinessWorkItemsView` or `BusinessWorkItemsAdmin`.
- The page shows to-dos centrally, filtered by status: All / Open (default) / In progress / Completed / Cancelled / Expired.
- Leave and expense to-dos do appear here, with business kinds `LeaveRequest` and `ExpenseReimbursement`.

Each row shows: id, status, business kind, title, summary, assignment (user or role), creation time (UTC), and completion time.

## But that page is read-only

The administration to-do page **provides centralized viewing and status filtering only**. It:

- does not perform delegation, countersigning, co-signing, or notification delivery;
- **does not write work-item state** — in other words, there is no "approve / reject" button for you to click on this page.

## Clicking through lands on a placeholder page

The list has a "business entry" link, but it **recognises only three business kinds**:

| Business kind | Where it goes |
| --- | --- |
| `EmployeeProfileCorrectionRequest` (profile correction) | `/Admin/EmployeeProfileCorrectionRequests.aspx` |
| `BusinessApplication` (business request) | `/Admin/BusinessApplications.aspx` |
| `CollaborationItem` (collaboration item) | `/Admin/CollaborationItems.aspx` |
| **Any other kind (including `LeaveRequest` and `ExpenseReimbursement`)** | `/Admin/NotImplemented.aspx` (placeholder) |

So for a leave or expense to-do, the business entry leads to a **not-implemented placeholder page**, not to a page where you can approve anything.

## How to approve one today

**There is no interface path that approves or rejects a leave request or an expense claim.** Two routes remain:

1. Handle it **at the data layer, by the deployment side** (leave rows live in `PortalBiz_LeaveRequests`, expense rows in `PortalBiz_ExpenseReimbursements`);
2. wait for a review page to be added — that means a new administration page registered in navigation, which is a product-capability addition that documentation alone cannot close.

This differs from business requests, which do have a full administration review page (approve / return / reject) — see [reviewer path: how a business application gets reviewed]({{ site.baseurl }}/en/business-application-review/). Profile corrections also have one — see [profile correction: the reviewer path]({{ site.baseurl }}/en/profile-correction-review/).

## Why it is built this way

- The administration to-do page is positioned as **centralized viewing**, not as a processing entry point; it deliberately performs no delegation, countersigning, co-signing, or notification.
- The business-entry URL mapping is a **fixed allowlist**; unknown kinds consistently fall back to a placeholder page so an unsupported kind cannot be read as a navigable address. A new business kind must complete navigation registration first rather than being implicitly allowed.

## What this page does not cover

- How the applicant fills it in, what lands where, and the failure paths → [leave and expense: from filling in to appearing in to-dos]({{ site.baseurl }}/en/leave-and-expense-workflow/)
- How to-dos are presented and the "presents, does not process" boundary → [my to-do items]({{ site.baseurl }}/en/modules/my-work-items/)
- Leave request fields and validation → [the leave request module page]({{ site.baseurl }}/en/modules/leave-request/)
- Expense claim fields and storage → [the expense reimbursement module page]({{ site.baseurl }}/en/modules/expense-reimbursement/)
- What a working review path looks like → [reviewer path: how a business application gets reviewed]({{ site.baseurl }}/en/business-application-review/)

<!-- build-stamp: 3C99736B1427 -->
