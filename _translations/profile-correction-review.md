---
layout: default
title: Profile correction: the reviewer path
lang: en
parent: Learn
nav_order: 5
---

# Profile correction: the reviewer path

You are a **handler** (holding a profile-correction review or administration permission). This page covers the administration side: seeing a correction request, handling it, and the crucial boundary that **handling it does not mean the profile has been changed**.

The applicant's side — how they file it — is in [employee profile: confirm and correct]({{ site.baseurl }}/en/profile-confirm-and-correct/).

## Before you start

- Entry point: `/Admin/EmployeeProfileCorrectionRequests.aspx`.
- Viewing requires either `EmployeeProfileCorrectionRequestReview` or `EmployeeProfileCorrectionRequestAdmin`.
- The list shows only requests under the **current status filter**, which defaults to "Submitted".

## Open the handling page

1. Go to `/Admin/EmployeeProfileCorrectionRequests.aspx`.
2. Use the **status filter** drop-down: All / Submitted (default) / Reviewed / Closed / Rejected.
3. Click "Search" to reload the list under the current filter.

Each row shows: request id, submission time (UTC), employee (code / display name), submitting account (user id / user name), the **field to correct**, the current-value snapshot, the proposed value, the employee's note, the current status, and the handling information (time / handler / note).

## Handle a request

For each row you may first enter a **handling note** (up to 1000 characters, may be left empty), then pick one of three:

- **Reviewed**: mark it as handled.
- **Rejected**: reject the request.
- **Closed**: close the request.

**The permission differs between actions**: setting Reviewed or Rejected needs `EmployeeProfileCorrectionRequestReview` or `...Admin`, while setting **Closed** needs **`EmployeeProfileCorrectionRequestCancel`** or `...Admin`. Closing is a separate permission; the review permission does not cover it.

> This page accepts only those three target statuses. Any other value is treated as an unsupported status, the list refreshes, and nothing is written.

## After handling

- Every successful handling is recorded in the **operations audit** (category `BusinessModuleCategory`, event `EmployeeProfileCorrectionReviewed`), stating the target status.
- The result is then mirrored into a **work-item completion event**: Rejected → rejected event, Closed → cancelled event, anything else → approved event; the corresponding to-do is marked completed (cancelled for Closed).
- The handler and handling time are written into the request record.

**The most important boundary**: this page **updates only the request's own status and note; it does not modify employee master data**. Marking a correction request as "Reviewed" does **not** change the employee's profile to the proposed value — the actual data change goes through **employee-directory maintenance** (`/Admin/EmployeeDirectory.aspx` / `/Admin/EmployeeEdit.aspx`). Those are two separate steps; do not assume approving has fixed the data.

Another boundary: the work-item sync is a **sidecar call after** the review write, and the current implementation does **not catch** work-item service exceptions. If the work-item service fails, the exception propagates (this is registered as a deferred item and has not yet been changed to failure isolation).

## What you cannot see

- There is **no** dedicated screen showing "who handled this correction and when"; the place to look is the **operations audit page** (`/Admin/OperationAudits.aspx`).
- The applicant's "My to-do items" **presents, it does not process**: it does not approve, change data, or write back to the business table.

## What this page does not cover

- How the applicant confirms a profile and files a correction → [employee profile: confirm and correct]({{ site.baseurl }}/en/profile-confirm-and-correct/)
- Correction-request fields and validation → [the employee profile correction request module page]({{ site.baseurl }}/en/modules/employee-profile-correction-request/)
- Producing sample data and regression checks → [the employee profile correction sample business flow]({{ site.baseurl }}/en/p12-sample-business-flow/)
- How to-dos are presented and where processing happens → [my to-do items]({{ site.baseurl }}/en/modules/my-work-items/)
- Audit retention principles and their basis → [operations audit retention policy]({{ site.baseurl }}/en/audit-retention-policy/)
- The fact that leave and expense have **no** administration review entry → [leave and expense: the review side today]({{ site.baseurl }}/en/leave-and-expense-review/)
