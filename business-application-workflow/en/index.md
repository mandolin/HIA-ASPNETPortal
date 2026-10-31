---
layout: default
lang: en
parent: Learn
nav_order: 2
permalink: /en/business-application-workflow/
title: Business requests: from submission to closure
---
# Business requests: from submission to closure

Once you have handed in a request, three things matter: whether it really went through, what stage it is at now, and whose turn it is to act next. This page follows that order.

## Before you start

- You are signed in. The module takes one user record for the current user; when it cannot, it treats you as having no permission and shows nobody else's data.
- "Business requests" only appears once it is mounted on a tab. If you cannot find the tab, the deployment has not exposed it — ask an administrator.
- What this page covers is the **generic request skeleton**: title, category, summary, body. The fields of any particular business (research, well design, intelligence and so on) live in their own dedicated modules; do not look for them here.

## Submit a request

1. Open the tab that carries the module and fill in the **title**.
2. Pick a **category** in the category drop-down.
3. Fill in the **summary** and the **body**.
4. Click **submit**.

After a successful submission the list below the form shows the new request, a timeline event is written, and the audit trail records who submitted which request and when.

## After submitting

The status starts at `Submitted` and waits in the review window. From there it may become:

| Becomes | Moved by | What you do |
| --- | --- | --- |
| Approved | The reviewer | Close it off |
| Returned | The reviewer | Amend it and submit again |
| Rejected | The reviewer | Terminal, nothing further to do |
| Closed | The reviewer | Terminal, nothing further to do |

The list you see holds **only the requests you submitted yourself** — it is filtered by the current user, and there is no "see everyone's requests" entry point.

## After it comes back

When a request is `Returned`, amend it and submit again (`Resubmit`) and it goes back to `Submitted`. That step is the **applicant's own path** and does not go through review.

Two actions are defined but **not wired up**: `Withdraw` and `Claim`. Nothing in the current implementation triggers them, so a submitted request cannot be withdrawn.

## Why you cannot see the approval trail

Three things, because this is the easiest part to misread:

- **There is no approval-trail page.** The approval flow is a data-layer state machine recorded in the system; the interface shows no "who approved it when" timeline.
- **"My to-do items" presents, it does not process.** It lists unfinished to-dos and names the module each came from, but it does not approve anything, change data, or write back to the source module.
- To move a request forward, go back to the module that produced it.

## When the submission did not go through

- **Required fields are judged after you submit.** The form carries no red required-field hints (the markup layer has no required validator at all), so an empty title or an empty body is rejected by the server after you press submit.
- **A body over 4000 characters is truncated**, not rejected; the same applies to the 200-character title and the 500-character summary.
- **If the list shows no new entry after submitting**, first check whether the submission actually succeeded — a rejected submission adds no entry and raises no error in the list.

## What this page does not cover

- The field list and length limits → [the business request module page]({{ site.baseurl }}/en/modules/business-application-request/)
- The full status vocabulary and the nine legal transitions → [the business application state machine]({{ site.baseurl }}/en/business-application-state-machine/)
- How to-dos are presented and where handling happens → [my to-do items]({{ site.baseurl }}/en/modules/my-work-items/)

<!-- build-stamp: D9871D4E0856 -->
