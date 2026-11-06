---
layout: default
title: Employee profile: confirm and correct
lang: en
parent: Learn
nav_order: 2
---

# Employee profile: confirm and correct

The first thing to do after joining is to check whether your own profile is right; when it is not, you cannot change it — you file a correction request. Those two steps live in two different modules, and this page joins them up.

## Before you start

- You are signed in and your account **is bound to an employee profile**. When no profile can be read, the module takes the no-permission branch — it is not a blank page.
- Each module appears only once it is mounted on a tab; if you cannot find the tab, the deployment has not exposed it — ask an administrator.

## Confirm my profile

1. Open the tab that carries the module and check the read-only items listed on the page (employee number, name, organization and so on — nine read-only labels in all).
2. If nothing is wrong, click **confirm**.

That action is recorded as an audit event (`EmployeeProfileConfirmed`).

The module has **no input fields at all** — it confirms, it does not edit. Nothing on that page that looks like a form can be typed into.

## When something is wrong

Do not try to change it on the confirmation page (you cannot). Corrections go through the other module: **the employee profile correction request**.

The fields you can correct are **the ones the drop-down offers** — not any field you like. Work out beforehand which field, what value, and why: all three become the basis for the handling that follows.

## Submit a correction request

1. Open the tab that carries the correction module and first check the **current profile** listed at the top of the page (read-only).
2. Pick the field to correct in the **correction field** drop-down.
3. Fill in the **proposed value**.
4. Write **why** you are asking for the change in the request note.
5. Click **submit**.

The request then appears in the list below the form, together with the timeline event of this submission.

Note that this step **submits a request; it does not change data**. Your profile stays as it is until the request is handled.

## After submitting

- The list holds **only the requests you submitted yourself**, filtered by the current user.
- A submitted request **cannot be changed here**, and there is no withdrawal.
- Like leave and expense, a correction request generates a work item on **"My to-do items"**.
- Handling happens on the **administrator's side**: the request shows up in the administration list of correction requests, its work item on the administration to-do page, and both the action taken (approve / reject / close) and your submission are traceable in the operations audit.

## When you cannot see the progress

- The to-do on "My to-do items" **presents, it does not process**: it does not approve, change data, or write back to the business table. To move it forward you go back to the administrator side.
- There is **no** page showing "who handled this correction and when"; where you can look that up is the **operations audit page**.

## What this page does not cover

- Only **field-level text corrections**. Attachments, phone numbers, identity numbers, salary and performance are **out of scope** and are handled elsewhere.
- The employee number is one of several login identifiers — it is not a password or a separate credential.

## What this page does not cover in detail

- The read-only items and the audit event of the confirmation module → [the employee profile confirmation module page]({{ site.baseurl }}/en/modules/employee-profile-confirm/)
- The fields and validation of the correction request → [the employee profile correction request module page]({{ site.baseurl }}/en/modules/employee-profile-correction-request/)
- How to-dos are presented and where handling happens → [my to-do items]({{ site.baseurl }}/en/modules/my-work-items/)
- Producing sample data and regression checks (implementation view) → [the employee profile correction sample business flow]({{ site.baseurl }}/en/p12-sample-business-flow/)

