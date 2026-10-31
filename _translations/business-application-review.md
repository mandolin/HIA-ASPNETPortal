---
layout: default
title: Reviewer path: how a business application gets reviewed
lang: en
parent: Learn
nav_order: 3
---

# Reviewer path: how a business application gets reviewed

You are a **reviewer** (holding the `BusinessApplicationReview` or `BusinessApplicationAdmin` permission). This page covers your side: seeing an application in the administration backend, approving / returning / rejecting it, and where to look afterwards.

The applicant's side — how they submit and what happens after — is in [business requests: from submission to closure]({{ site.baseurl }}/en/business-application-workflow/); this page is only about the review desk.

## Before you start

- The entry point is the administration page **`/Admin/BusinessApplications.aspx`** (its title is "Business applications"). If you lack the role for that page, you cannot open it — it is not a broken page.
- Viewing and handling share the same permission set: either `BusinessApplicationReview` or `BusinessApplicationAdmin` is enough.
- The list shows only applications under the **current status filter**, which defaults to "Submitted".

## Open the review page

1. Go to `/Admin/BusinessApplications.aspx`.
2. Use the **status filter** drop-down to choose a scope: All / Submitted (default) / In review / Returned / Approved / Rejected / Closed.
3. Click "Search" to reload the list under the current filter.

Each row shows: id, submission time (UTC), application code, applicant (user id + user name), application content (title / category / summary / body / latest review), current status, and the review area (a comment box plus three buttons).

## Review an application

For each row you may first enter a **review comment** (up to 1000 characters, may be left empty), then pick one of three:

- **Approve**: the application moves to "Approved" (a terminal state).
- **Return**: the application goes back to "Returned" and returns to the applicant for completion; once the applicant resubmits it returns to "Submitted".
- **Reject**: the application moves to "Rejected" (a terminal state).

Clicking the button submits. The backend hands the action to the data-layer state machine: it writes the application state and a workflow event; **the page does not infer state by itself**.

> Only these three actions work. The state machine also knows a "Close" action, but the current administration page has **no button for it**; passing any other action only yields an "unsupported action" message and writes nothing.

## After reviewing

- Every successful review is recorded in the **operations audit** (category `BusinessModuleCategory`, event `BusinessApplicationReviewed`), stating the application code and the action.
- At the same time the result is projected into a **work-item completion event**: Approve → Approved, Reject → Rejected, Return → Commented; the corresponding "My to-do items" entry is marked complete.
- The reviewer's (your) identity and time are written into the application record.

One boundary to keep in mind: **a failed work-item projection does not roll back the application state**. In other words, even if the to-do side fails to write, the application itself is still reviewed. To confirm the final result, look at the **operations audit page** (`/Admin/OperationAudits.aspx`) and the status in the application record, not only at the to-do.

## What you cannot see or do

- **No dedicated approval-trail screen**: after you click, the history lives only in the operations audit and the workflow events; there is no administration page showing "who approved what when".
- **No "withdraw / claim" buttons**: those two actions exist only as code constants with no wired-up UI — the reviewer side can neither withdraw nor claim.
- **"Close" is not on this page**: the state machine allows closing from "Submitted / In review" to "Closed", but the administration review page only exposes Approve / Return / Reject, with no close button.
- The applicant's **resubmit** UI is also not yet wired (a known gap), so how an applicant actually resubmits after a return has to wait until that gap is filled.

## What this page does not cover

- How the applicant submits and what happens afterwards → [business requests: from submission to closure]({{ site.baseurl }}/en/business-application-workflow/)
- The full status vocabulary and legal transitions (including the "Close" and "Resubmit" rules) → [business application state machine]({{ site.baseurl }}/en/business-application-state-machine/)
- How to-dos are presented and their boundaries → [my to-do items]({{ site.baseurl }}/en/modules/my-work-items/)
- Audit retention principles and their basis (not the audit UI itself) → [operations audit retention policy]({{ site.baseurl }}/en/audit-retention-policy/)
