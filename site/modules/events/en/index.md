---
layout: default
lang: en
parent: Learn
nav_order: 12
permalink: /en/modules/events/
title: Events
---
# Events

> Module directory: `src/Portal/DesktopModules/`｜View: `Events.ascx`｜Edit page: `EditEvents.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Publishes **activity information** including time and place. Like Announcements, events carry an **expiry date**: after it they are no longer displayed.

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request).
2. The module must be mounted on a tab. Current deployment: **TabId=1 "近期活动" (Upcoming events)**.
3. There is **no "never expires" and no scheduled-publishing switch** — long-term display is only possible by adjusting `ExpireField`.

## Operating steps

**Create an event**: open the tab → click **「Add New Event」** in the module title bar → fill in **title**, **description**, **where and when** and **expiry date** — **all four are required**; omitting any one is blocked → save. The view shows title, description, and where-and-when in order.

**Edit an event**: click the **edit entry** on the row (visible **only to users with edit permission**) and save.

**Take an event offline**: set `ExpireField` to a past date and save.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Title | `TitleField` | Single-line text | 150 | **Required** | No format check |
| Description | `DescriptionField` | Multi-line text | None | **Required** | No length limit |
| Where and when | `WhereWhenField` | Single-line text | 150 | **Required** | No format check |
| Expiry date | `ExpireField` | Single-line text | No `MaxLength` in source | **Required** | Date comparison `VerifyExpireDate` (`Type="Date"`, `DataTypeCheck`) |

Control ids, types and lengths are taken from the `EditEvents.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **Four required fields**: `TitleField`, `DescriptionField`, `WhereWhenField`, `ExpireField` — matching the four `RequiredFieldValidator` controls in source (**three of them have no `ID` attribute at all**; only `RequiredExpireDate` has one).
- **One comparison check**: `VerifyExpireDate` validates that `ExpireField` is a **date**. It checks "is this a date", **not** "is this in the future" — a past date passes and the event simply stops being displayed at once.
- Events have the **most required fields in this batch**: none of the four may be left empty.

## Storage and audit

- **Storage**: `EventsDB` — add / update / delete / read go through that data-access class.
- **Audit**: **this module's save path writes no operational audit record**; the audit table will not show who changed which event. The only content path that writes operational audit is [Signin]({{ site.baseurl }}/modules/signin/).

## Permissions

- The edit entry and the save action are governed by **module edit permission** (`HasEditPermissions(moduleId)`), **re-checked on each request**.
- Reading the list requires no edit permission.
- The module **does not distinguish by author** — edit permission means every event entry is editable.

## Common questions

**A field is reported as required** — all four are required; the most often missed are the **description** and **where and when**.

**"Date is invalid"** — `ExpireField` cannot be parsed as a date; use the format matching the system's current locale.

**Saved but not visible** — most likely **already expired**: the module filters by `ExpireField`.

**I want an event to stay up indefinitely** — set `ExpireField` to a distant future date; there is **no "never expires" switch**.

**The description is not fully shown** — the description is multi-line with no length limit; if you need full layout control, put the essentials into the title or the where-and-when field.

## Boundaries and non-goals

- No approval flow, no scheduled publishing, no categories, no attachments, no sign-up or reminders, no "never expires".
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Deployment tab (TabId=1) is a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Against [Announcements]({{ site.baseurl }}/modules/announcements/): both filter by expiry and share the date comparison check, but Events has **one extra where-and-when field** and **all four fields are required**, whereas Announcements requires three and carries two optional links.

<!-- build-stamp: 4768FECCD4EC -->
