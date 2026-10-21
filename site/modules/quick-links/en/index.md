---
layout: default
lang: en
parent: Reference
permalink: /en/modules/quick-links/
title: QuickLinks
---
# QuickLinks

> Module directory: `src/Portal/DesktopModules/`｜View: `QuickLinks.ascx`｜Edit page: **shared with Links — `EditLinks.aspx`**
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Presents frequently used entry points as a **compact link group**. It **shares one link data set and one edit page with the Links module** — it is not a separate link store, but another view of the same link data.

Prerequisites:

1. You are signed in and belong to the module's **`AuthorizedEditRoles`** — only those users see the edit entries.
2. The module must be mounted on a tab. Current deployment (measured in the development database): **3 instances** — home page "快速链接", product information "值得关注的竞争者", about the portal "快速链接".

## Operating steps

**Add a link**: open the tab → users in `AuthorizedEditRoles` see an **add-link entry**, which opens `EditLinks.aspx` (**the same edit page shared with Links**) → fill in the fields and save: title and address are required and the display order must be an integer (see the [Links field table]({{ site.baseurl }}/modules/links/)).

**Edit a link**: click the per-row **edit** entry (again the shared `EditLinks.aspx`).

**As a reader**: you only see links that **pass the navigation policy**; non-compliant legacy addresses are **suppressed into encoded text** (no longer a link) and **the original address is not echoed**.

## Field table

The edit page is **identical to the Links module** (the same `EditLinks.aspx`):

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Title | `TitleField` | Single-line text | 150 | **Required** | No format check |
| Address | `UrlField` | Single-line text | 150 | **Required** | No format check |
| Mobile address | `MobileUrlField` | Single-line text | 150 | Optional | No format check |
| Description | `DescriptionField` | Single-line text | 150 | Optional | No format check |
| Display order | `ViewOrderField` | Single-line text | 3 | **Required** | Integer comparison `VerifyViewOrder` (`Type="Integer"`, `DataTypeCheck`) |

Control ids, types and lengths are taken from the `EditLinks.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **Three required fields** and **one integer comparison check** — **exactly the same as the Links module** (same edit page, same markup); they are not repeated here.
- In other words: **QuickLinks has no field rules of its own**; all of its validation is inherited from the shared `EditLinks.aspx`.

## Storage and audit

- **Storage**: `LinkDB` — **the same data set as the Links module** (the view re-binds `ILinksDb.GetLinks(ModuleId)` on every request with ViewState disabled).
- **Audit**: **this save / delete path writes no operational audit record** (consistent with the other legacy content edit pages); the audit table will not show who changed which link.

## Permissions

- The view **re-binds data on every request** (ViewState disabled); whether edit entries appear is decided by the **server permission context**, and only users in `AuthorizedEditRoles` see the add and per-row edit entries.
- The permission model is **module edit permission plus a link-ownership check** — it does **not** require the narrow "raw HTML" permission (`ContentRawHtmlEdit`) that the HTML module requires, and it **cannot** change or delete another module's links by supplying an `ItemID`.
- Readers need no edit permission.

## Common questions

**A link I changed here also changed in the Links module** — **expected**: the two share one link data set. Which links appear on which tab depends on the module instance deployed on each tab.

**I see no edit entry** — edit entries are rendered according to `AuthorizedEditRoles`; outside that role they are **not rendered at all** (not "visible but failing on click").

**A legacy link shows as encoded text and is not clickable** — that is the **navigation policy** at work: non-compliant legacy addresses are suppressed into encoded text and **the original address is not echoed**. To make it clickable again, change the address in the edit page to a compliant one.

**The page did not change after my edit** — the view re-binds on every request, so changes take effect immediately; if nothing changed, check the browser cache and whether the save succeeded.

**I want a separate data set for QuickLinks** — not possible: QuickLinks has **no data of its own**, it is another presentation of the same data.

## Boundaries and non-goals

- QuickLinks has **no edit page, field rules or data of its own** — it is another view over the shared edit page and data.
- No address format check, no ordering-uniqueness check, no grouping, no click statistics, no operation audit.
- The view's **navigation policy** suppresses non-compliant addresses (shown as encoded text) — the most visible behavioural difference from the Links view.
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Instance count (3) is a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.

<!-- build-stamp: 312EBD1DFBF3 -->
