---
layout: default
lang: en
parent: Reference
nav_order: 8
permalink: /en/modules/links/
title: Links
---
# Links

> Module directory: `src/Portal/DesktopModules/`｜View: `Links.ascx`｜Edit page: `EditLinks.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Maintains a **list of links** (site navigation or resource entry points). The view shows clickable link titles with an edit entry on the right of each row.

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request).
2. The module must be mounted on a tab.
3. **Current deployment status (must be known)**: this module is deployed **on a test tab only** — **TabId=1004 "测试链接模块" (test links module)** — i.e. **only** a test tab uses it and **no production tab uses it yet**. Enabling it on a production tab is a new deployment, after which this section must be re-checked.

## Operating steps

**Add a link**: open the tab → click **「Add Link」** in the module title bar → fill in the **title** (`TitleField`) and **address** (`UrlField`), both required → fill in the **display order** (`ViewOrderField`), required and **must be an integer** → optionally fill in the **mobile address** (`MobileUrlField`) and **description** (`DescriptionField`) → save.

**Edit a link**: click the edit entry on that row and save.

**Reorder**: change the number in `ViewOrderField`. Smaller numbers come first (the exact ordering follows the view implementation).

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Title | `TitleField` | Single-line text | 150 | **Required** | No format check |
| Address | `UrlField` | Single-line text | 150 | **Required** | No format check |
| Mobile address | `MobileUrlField` | Single-line text | 150 | Optional | No format check |
| Description | `DescriptionField` | Single-line text | 150 | Optional | No format check |
| Display order | `ViewOrderField` | Single-line text | 3 | **Required** | Integer comparison `VerifyViewOrder` (`Type="Integer"`, `DataTypeCheck`) |

Control ids, types and lengths are taken from the `EditLinks.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **Three required fields**: `TitleField`, `UrlField`, `ViewOrderField` — matching the three `RequiredFieldValidator` controls (`Req1`, `Req2`, `RequiredViewOrder`).
- **One comparison check**: `VerifyViewOrder` checks that `ViewOrderField` is an **integer** (`Operator="DataTypeCheck"`, `Type="Integer"`). It checks "is this an integer" only — **not** a range and **not** uniqueness: negative numbers, over-long values and several rows sharing the same number all pass.
- **The address field has no format check**: a non-existent address is neither blocked nor reported.
- `ViewOrderField` is limited to **3 characters**, which together with the integer check constrains its usable range.

## Storage and audit

- **Storage**: `LinkDB` — add / update / delete / read go through that data-access class.
- **Audit**: **this module's save path writes no operational audit record**; the audit table will not show who changed which link.

## Permissions

- Whether the edit entry is rendered is decided by the **server permission context** (the markup layer grants no editing capability by itself).
- Saving is checked per request against `HasEditPermissions(moduleId)` and additionally carries a **link-ownership check**: **another module's links cannot be changed or deleted by supplying an `ItemID`**.
- Reading the list requires no edit permission.

## Common questions

**Title or address reported as required** — `TitleField` or `UrlField` is empty.

**"Display order is not an integer"** — `ViewOrderField` contains a non-integer (for example `first`, `1.5`, or empty). The check accepts integers only, not decimals.

**Order changed but nothing moved** — confirm the value is an **integer**; also, when several rows share **the same number** the relative order is undefined.

**The link does not open** — the address field has **no format check**, so a wrong address is stored as typed; check it by hand.

**I want link lists on several tabs** — link rows **belong to a module instance** (`ModuleId`); separate tabs host separate instances with separate data, and the ownership check blocks cross-module operations.

**I cannot find this module on a production tab** — because it is deployed **on a test tab only** (TabId=1004); production tabs do not use it yet.

## Boundaries and non-goals

- No address format check, no ordering-uniqueness check, no grouping or categories, no click statistics, no scheduled publishing, no operation audit.
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- **Deployment tab (TabId=1004 test tab) is a delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Compared with [QuickLinks]({{ site.baseurl }}/modules/quick-links/): the two **share one data set and one edit page** (see that page); they differ in view presentation and permission context, not in fields or validation.

<!-- build-stamp: F0A8471C06BC -->
