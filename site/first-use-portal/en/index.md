---
layout: default
lang: en
parent: Learn
nav_order: 1
permalink: /en/first-use-portal/
title: First use of the portal
---
# First use of the portal

> This page answers: **for someone logging in for the first time — where am I, what do I click, and what counts as normal?** Written for site users and colleagues new to the portal. Per-feature fields and validation rules live in the module pages under the Learn section; mechanics and configuration live under Reference.

## Before you begin

1. You have a usable login identifier and password. The identifier may be **a login name, a legacy name, an active employee number, or an email address**.
2. You open the site home page in a browser. The portal still works on **IE9** and later, and of course on modern browsers.
3. Reading this page **does not require signing in**; the "Menu after signing in" section below does.

## Recognize the three areas on the home page

Before signing in, the home page shows three areas:

| Position | Content | What to look for |
| --- | --- | --- |
| Left | **Account sign-in** | The sign-in form: identifier, password, remember me |
| Middle | **Portal announcements** | Only announcements **marked visible to guests, item by item**; unmarked entries never appear here |
| Right | **Help and notes** | Usage notes for the site; also the "Help" page in the left navigation |

While **not signed in**, the left navigation contains only "Home" and "Help". That is the normal state: **business capability entries appear only for signed-in users**, so an empty-looking menu before signing in is expected.

## Sign in

1. In the **Account sign-in** card on the left of the home page, fill in your **identifier** and **password**.
2. Tick **Remember me** if you want the session kept.
3. Click **Sign in**.

Expected result: the user area at the top shows your name, and the left navigation grows from two items to six.

> The page encrypts the password with a **one-time RSA public key** before submission; the server decrypts it only within the current request, so it never travels in plain text.
> A failed sign-in shows a **generic** message — it does not distinguish "no such account" from "wrong password".

## Menu after signing in

| Menu item | What it is for | Who sees it |
| --- | --- | --- |
| Home | Back to the sign-in card and announcements | Everyone |
| Help | Usage notes for the site | Everyone |
| My to-do items | An aggregate of **unfinished** to-dos (view only) | Signed-in users |
| Leave and expense | Leave requests and expense claims | Signed-in users |
| Business requests | Submit and track business requests | Signed-in users |
| My profile | Employee profile confirmation and correction requests | Signed-in users |
| Platform administration | Site settings, tabs, modules, roles and users | Administrators only |
| Enterprise workbench | Submit, resubmit and comment on collaboration items | Administrators only |

> Menu labels follow the interface language: the Chinese interface shows "我的待办", the English interface shows "My To-Do Items". The table above uses the Chinese wording.

## View your to-do items

1. Click **My to-do items** in the left navigation.
2. The page lists, on load, the **unfinished** to-dos **assigned to you or to a role key you hold** — you do not have to press anything.
3. Click **Overdue** to see only overdue entries; click **All** to return to the full list.

Expected result: every entry states the **source module** it comes from.

> **To-dos can be seen here but not handled here**: this module **presents and does not process** — it does not approve, does not change data, and does not write back to the source module. Handling happens in the module that produced the to-do (expense reimbursement, leave request, business request).

## Handle it in the source module

1. Find the to-do in the list and note the source it names.
2. Click the matching tab in the left navigation (for example "Leave and expense").
3. Complete the request or the handling action inside that tab.

Expected result: once handled, the to-do disappears from "My to-do items" — because the module lists unfinished items only.

## When something is not showing

| Symptom | Cause | What to do |
| --- | --- | --- |
| The left navigation shows only "Home" and "Help" | Not signed in (business entries are hidden from anonymous visitors) | Sign in first |
| "My to-do items" and the other entries are still missing after signing in | The sign-in did not succeed, or the entry is not visible to your account | Check whether your name appears at the top; ask an administrator to confirm the account is enabled |
| "My to-do items" is empty | Nothing unfinished is assigned to you | Normal state; the module lists unfinished items only |
| A particular to-do disappeared | It was completed, it was assigned to someone else, or its source module is outside the aggregate | Confirm in the source module |
| There is no action button in "My to-do items" | This module deliberately offers no in-place handling | Handle it in the source module |
| The announcements area on the home page is empty | Each announcement must be **marked visible to guests individually** to appear here | Contact the site administrator |

## Not covered on this page

- **Per-feature fields, required inputs and validation**: see the module pages under Learn.
- **Publishing and maintaining announcements**: that is a site operation; see the operator handbook (separate page).
- **A screen showing approval-trajectory details**: the approval flow of business requests and of leave/expense is a **data-layer state machine** and there is currently **no dedicated trajectory page** — this page therefore does not describe an interface that does not exist.
- **Site configuration and secondary development**: see Reference.

## Where to go next

- Module usage guides: "Sign-in", "My to-do items", "Leave and expense", "Business requests", "My profile", "Enterprise workbench" (Learn section)
- Mechanics and configuration: module package contract, module mechanics and secondary development, navigation visibility (Reference section)

<!-- build-stamp: 60BEF23949F0 -->
