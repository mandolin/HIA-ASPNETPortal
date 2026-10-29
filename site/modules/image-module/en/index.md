---
layout: default
lang: en
parent: Learn
nav_order: 15
permalink: /en/modules/image-module/
title: ImageModule
---
# ImageModule

> Module directory: `src/Portal/DesktopModules/`｜View: `ImageModule.ascx`｜Edit page: `EditImage.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Displays **a single image** on a tab. It has no table of its own and stores three text settings only: image address, width and height.

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request).
2. The module must be mounted on a tab. Current deployment: **TabId=3 "夜视镜" (Night vision)**.
3. **The image file itself is not managed by this module**: the edit page only maintains text settings and **does not handle binary uploads** (stated in the source comments). Put the image somewhere reachable first; this module only stores the address that points at it.

## Operating steps

**Set an image**: open the tab → click the **Edit** entry in the module title bar (this module edits *settings*, it does not "create an item" — there is only ever one image) → fill in the **image address** (`Src`) and, if needed, **width** (`Width`) and **height** (`Height`) → save.

**Adjust the size**: change `Width` / `Height` and save. Both are **text boxes** and the module **does not check whether they are numbers** — non-numeric input is accepted but produces no meaningful size.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Image address | `Src` | Single-line text | 250 | Optional | No format check |
| Width | `Width` | Single-line text | 8 | Optional | No check (non-numeric is accepted) |
| Height | `Height` | Single-line text | 8 | Optional | No check (non-numeric is accepted) |

Control ids, types and lengths are taken from the `EditImage.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **No required fields**: the edit page has **no `RequiredFieldValidator`**; all three fields may be saved empty.
- **No format validation**: no comparison, regular-expression or custom validation. Whether the address is valid, or width and height are numbers, **is not checked at all**.
- The only real constraint is **input length**: `Src` up to 250 characters, `Width` and `Height` up to 8 each.
- In practice **you must check these yourself** — mistakes do not surface at save time.

## Storage and audit

- **Storage**: **module settings** (`ModulesConfig.GetModuleSettings` / `ModulesConfig.UpdateModuleSetting`), persisted in the portal module-settings table (`PortalCfg_ModuleSettings`). The address is stored under the `src` setting key; width and height have their own keys. **This module has no business table of its own.**
- **Audit**: **this module's save path writes no operational audit record**; the audit table will not show who changed the image settings.

## Permissions

- Whether the edit entry is rendered is decided by the **server permission context** (source comment: the markup layer grants no editing capability by itself) — without permission it is **not rendered at all**, rather than erroring on click.
- Saving is checked per request against `HasEditPermissions(moduleId)`.
- Viewing the image requires no edit permission.

## Common questions

**Saved but no image appears** — check whether `Src` is empty or the address is reachable. The module **does not validate the address**.

**Wrong size / image too large** — `Width` and `Height` are free text: `800` and `eight hundred` are both accepted, only the former has meaning. If left empty, the page and the image's own dimensions decide.

**I want to upload a new image** — **not supported**. Place the image somewhere reachable first, then put its address into `Src`.

**The address does not fit** — `Src` is limited to 250 characters; use a shorter or relative address.

**Changed the settings but the page did not change** — apart from permissions, check the **browser cache**; settings are read per request, so a hard refresh rules caching out.

## Boundaries and non-goals

- No file upload, no galleries or multiple images, no size/format validation, no cropping or compression, no image versioning, no audit of image operations.
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Deployment tab (TabId=3) is a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Like [XmlModule]({{ site.baseurl }}/modules/xml-module/) this module stores its configuration in **module settings rather than a table of its own**; the difference is that here the settings are **plain text** (address and size), whereas the XML module's settings are **virtual paths of trusted deployment resources** and are checked for path compliance at save time.

<!-- build-stamp: 4FD387A9A5A4 -->
