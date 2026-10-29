---
layout: default
lang: en
parent: Learn
nav_order: 3
permalink: /en/modules/announcements/
title: Announcements
---
# Announcements

> Module directory: `src/Portal/DesktopModules/`｜View: `Announcements.ascx`｜Edit page: `EditAnnouncements.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Publishes **in-site notices with an expiry date**. Each notice has a title, a description and an expiry time, plus two optional links (detail link, mobile link). **A notice is no longer displayed once its expiry is reached** — this is the module's core behaviour, not a display preference.

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked on every request).
2. The module must be mounted on a tab. Current deployment: **TabId=1 "新闻与特写" (News & Features), TabId=2 "人事福利" (HR & Benefits), TabId=3 "竞赛：TradeCraft / 监视 / 保护" (Competition: TradeCraft / Surveillance / Protection)**.
3. There is **no "never expires" switch**. To keep a notice visible you must adjust `ExpireField` before it lapses.

## Operating steps

**Create a notice**: open the tab → click **「Add New Announcement」** in the module title bar → fill in **title** and **description** (both required) → fill in the **expiry date** (required, checked by `VerifyExpireDate`, `Type="Date"` + `DataTypeCheck`) → add the two links only if needed → save.

**Edit a notice**: click the **edit entry** on the right of the item (visible **only to users with edit permission**; without permission it is not rendered at all, rather than erroring on click).

**Take a notice offline**: set `ExpireField` to a past date and save.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Title | `TitleField` | Single-line text | 100 | **Required** | No format check |
| Detail link | `MoreLinkField` | Single-line text | 100 | Optional | No check; a wrong URL is saved silently |
| Mobile link | `MobileMoreField` | Single-line text | 100 | Optional | No check; a wrong URL is saved silently |
| Description | `DescriptionField` | Multi-line text | None | **Required** | No length limit |
| Expiry date | `ExpireField` | Single-line text | No `MaxLength` in source | **Required** | Date comparison `VerifyExpireDate` (`Type="Date"`, `DataTypeCheck`) |

Control ids, types and lengths are taken from the `EditAnnouncements.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **Three required fields**: `TitleField`, `DescriptionField`, `ExpireField` — matching the three `RequiredFieldValidator` controls in source (`Req1`, `Req2`, `RequiredExpireDate`).
- **One comparison check**: `VerifyExpireDate` validates that `ExpireField` is a **date**. It checks "is this a date", **not** "is this in the future" — a past date passes validation and simply means the notice stops being displayed at once.
- **The two link fields have no validation at all**: an illegal URL produces no prompt. Check them by hand.

## Storage and audit

- **Storage**: `AnnouncementsDB` — add / update / delete / read go through that data-access class.
- **Audit**: **this module's save path writes no operational audit record**. The audit table will not show who changed which notice. **This is a fact of the current implementation, not an omission in this document**; traceability must be arranged outside the portal if needed.
- The only content path in this repository that **does** write operational audit is the [Signin]({{ site.baseurl }}/modules/signin/) module.

## Permissions

- The visibility of the edit entry and the save action are both governed by **module edit permission** (`HasEditPermissions(moduleId)`), **re-checked on each request** (both when opening the edit page and when submitting).
- Reading notices requires no edit permission; unauthorised users simply see a list without edit entries.

## Common questions

**"Title is required"** — `TitleField` is empty or whitespace-only. The title is the easiest field to miss during bulk entry.

**"Date is invalid"** — `ExpireField` cannot be parsed as a date; use the format matching the system's current locale.

**Saved but not visible** — most likely **already expired**: the module filters by `ExpireField`.

**Description missing** — `DescriptionField` is multi-line; if only the title shows, check whether the text went into another field, or whether the item has expired.

**Link does nothing** — `MoreLinkField` / `MobileMoreField` have no format check; illegal addresses are neither blocked nor reported.

## Boundaries and non-goals

- No "never expires", no scheduled publishing, no approval flow, no categories, no attachments, no withdrawal notice: visible until expiry and hidden afterwards is the whole lifecycle.
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Deployment tabs (TabId=1 / 2 / 3) are a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Compared with [Events]({{ site.baseurl }}/modules/events/): similar fields and expiry semantics, but Events has an extra "where and when" field (`WhereWhenField`) and **all four fields are required**, whereas here only the title, description and expiry date are required, plus two optional links.

<!-- build-stamp: A6718F10E37B -->
