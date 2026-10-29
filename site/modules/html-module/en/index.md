---
layout: default
lang: en
parent: Learn
nav_order: 11
permalink: /en/modules/html-module/
title: HtmlModule
---
# HtmlModule

> Module directory: `src/Portal/DesktopModules/`｜View: `HtmlModule.ascx`｜Edit page: `EditHtml.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Renders **a block of raw HTML maintained by a trusted administrator** (welcome text, explanatory blocks). It keeps three content slots: desktop body, mobile summary, mobile details.

Prerequisites:

1. You are signed in and hold **both**:
 - **module edit permission** (`HasEditPermissions(moduleId)`), and
 - the narrower **"raw HTML" permission** (`PortalPermissionKeys.ContentRawHtmlEdit`).
2. The module must be mounted on a tab. Current deployment (measured in the development database): **12 instances** — home page 2, employee information, product information, discussion area, about the portal 7, and the `G1P2-Test-A` test page.
3. The edit page's `Mid` comes from the unverified query-string / post-back collection and **must be a positive integer**, otherwise the page redirects to the edit-denied page.

## Operating steps

**Write HTML content**: open the tab → click the edit entry in the module title bar (visibility decided by the server permission context) → fill in **desktop raw HTML** (`DesktopText`), **mobile summary** (`MobileSummary`) and **mobile details** (`MobileDetails`) — **all three may be left empty** → save. Content is **HTML-encoded before storage** and decoded for display (the historical compatibility contract).

**Clear a slot**: empty the field and save. **When the record is missing or the content is empty, the view outputs nothing** (ordinary pages do not expose configuration state).

**Maintain desktop and mobile separately**: the desktop slot usually carries the full text, the mobile slots shorter summary and details; they do not affect each other.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Desktop raw HTML | `DesktopText` | Multi-line text | No `MaxLength` in source | Optional | No format check |
| Mobile summary | `MobileSummary` | Multi-line text | No `MaxLength` in source | Optional | No format check |
| Mobile details | `MobileDetails` | Multi-line text | No `MaxLength` in source | Optional | No format check |

Control ids and types are taken from the `EditHtml.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **No required fields**: the edit page has **no `RequiredFieldValidator`**; all three fields may be saved empty.
- **No format validation**: no comparison, regular-expression or custom validation; **none of the three fields has a length limit**.
- The edit page markup carries `ValidateRequest="false"` — it exists **only for legacy Web Forms post-back compatibility** and **is not a security boundary** (see below).
- On load, `Server.HtmlDecode` decodes the stored encoded content into the edit controls (decoding happens only within the trusted edit context).

## Storage and audit

- **Storage**: `HtmlTextDB` — `GetHtmlText` reads, `UpdateHtmlText` saves; the `HtmlTexts` table is located uniquely by `ModuleId`: **the first save creates the record, later saves only replace the three content fields**.
- **Storage contract (historical compatibility)**: **HTML-encode before storage, decode for display**.
- **Audit**: **this save path writes no operational audit record** (the other legacy content edit pages do not either); the audit table will not show who changed which HTML block.

## Permissions

- Editing requires **both** module edit permission and the narrow `ContentRawHtmlEdit` permission — **the only module in this repository that requires the narrow raw-HTML permission**.
- The request gate runs **before every lifecycle branch** (opening, cancelling, saving), so neither an unauthorised request nor an invalid module id can get through.
- Reading and rendering require no edit permission.

## Common questions

**Encoded text appears on the page after saving** — content is stored **encoded** and decoded for rendering; if the content contains `&`, `<` or `>`, what you see while editing may be the decoded form and what you see after saving the encoded form. **Do not copy the encoded content back in** (that would double-encode it).

**Nothing is displayed** — **when the record is missing or the content is empty the view outputs nothing** (ordinary pages do not expose configuration state). Check that the field really has content.

**Save rejected / redirected to the denied page** — most likely `Mid` is not a positive integer, or the `ContentRawHtmlEdit` permission is missing.

**I want to include a script** — **do not**. This module performs **no HTML allow-list sanitisation on render (source: it follows the historical "encode before persistence" convention **rather than sanitising**), so the content enters the page as written. It may be maintained **only by trusted administrators** and **must not carry ordinary user input**.

**Can I paste an image?** — this module stores text only and **does no file upload**.

## Boundaries and non-goals

- No content review, no HTML sanitisation, no template or variable substitution, no version history, no publishing approval, no operation audit.
- **Security boundary (must be known)**: this is an **explicit exception** to the "HTML-encode on display" rule — rendering performs **no sanitisation, so it **must not carry ordinary user input**; `ValidateRequest="false"` is post-back compatibility only and **is not a security boundary**. What actually contains the risk is the **permission** (narrow `ContentRawHtmlEdit` plus trusted administrators).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Instance count (12) is a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Against [ImageModule]({{ site.baseurl }}/modules/image-module/) and [XmlModule]({{ site.baseurl }}/modules/xml-module/): this module **has a business table of its own** (`HtmlTexts`) and **additionally requires the narrow permission**; it is the **only module in this batch that renders un-sanitised HTML.

<!-- build-stamp: DD612F0829E4 -->
