---
layout: default
title: Document
lang: en
parent: 参考
nav_order: 4
---

# Document

> Module directory: `src/Portal/DesktopModules/`｜View: `Document.ascx`｜Edit page: `EditDocs.aspx`｜View page: `ViewDocument.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Manages **document entries that can be downloaded or viewed**. An entry either points at an existing path (browse address) or is uploaded by the user; viewing always goes through `DesktopModules/ViewDocument.aspx` (**opens in a new window**).

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request).
2. The module must be mounted on a tab. Current deployment: **TabId=2 "新员工文档", TabId=4 "好书推荐"**.
3. For the **upload** path you must also satisfy the server's **shared upload policy** (file size and extension allow-list).

## Operating steps

**Create an entry pointing at an existing address**: open the tab → click **「Add New Document」** → fill in **document name** (required), **category** and **path** → leave **"upload"** and **"store in database"** unchecked → save.

**Create an entry by uploading**: open the edit page → fill in the name → check **"upload"** (`Upload`) and choose the file → if you also check **"store in database"** (`storeInDatabase`), note that **the switch only takes effect when the server allows database storage**, otherwise saving is rejected (`EditDocs_MessageDatabaseStorageDisabled`) → save; the server **generates a unique physical filename** to avoid overwriting same-named files.

**Edit an entry**: click the **edit entry** next to the document link (**visible only to users with edit permission**) and save.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Document name | `NameField` | Single-line text | 150 | **Required** | No format check |
| Category | `CategoryField` | Single-line text | 50 | Optional | None |
| Path | `PathField` | Single-line text | 250 | Optional | **Constrained by the path policy** (below) |
| Upload | `Upload` | Check box | — | Optional | Checked with no file selected → `EditDocs_MessageUploadSelected` |
| Store in database | `storeInDatabase` | Check box | — | Optional | Checked while database storage is disabled → `EditDocs_MessageDatabaseStorageDisabled` |

Control ids, types and lengths are taken from the `EditDocs.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **Exactly one required field**: `NameField` (document name), matching the single `RequiredFieldValidator` (`RequiredFieldValidator1`).
- **The two check boxes are not required** but each carries a **mutually exclusive precondition**: checking `Upload` with **no file selected** is rejected (`EditDocs_MessageUploadSelected`); checking `storeInDatabase` while **database storage is disabled** is rejected (`EditDocs_MessageDatabaseStorageDisabled`).
- **Path is policy-constrained (non-upload path)**: only **application-relative paths** or **http/https browse addresses** are accepted; arbitrary local paths (such as drive-letter paths) are rejected (`EditDocs_MessageInvalidBrowseUrl`). This is a security boundary against reading arbitrary local files — **do not try to work around it**.
- The upload path is additionally constrained by the **shared size and extension policy**.

## Storage and audit

- **Storage**: `DocumentDB` — update / delete / read go through that class. **The uploaded file itself is not stored in this table**: on save the server produces an **application virtual path** for persistence, and the physical path is used only for failure messages.
- **Audit**: **this module's save path writes no operational audit record**; the audit table will not show who uploaded or changed which document entry.

## Permissions

- The edit entry and the save action are governed by **module edit permission** (`HasEditPermissions(moduleId)`), **re-checked on each request**.
- The source notes explicitly that each request **re-validates both module edit permission and ownership of the existing item** — **a user without permission cannot manipulate another user's entry by changing parameters**.
- Reading the list and opening the view page require no edit permission; the **upload path** is additionally subject to the server upload policy.

## Common questions

**"Upload selected but no file"** — `Upload` is checked while the file box is empty; clear the check box and save as a plain path.

**"Database storage disabled"** — `storeInDatabase` is checked but this deployment does not enable it; clear the check box.

**"Invalid browse URL"** — you entered a drive-letter path or an unsupported protocol; **enter an application-relative path or an http/https address only**.

**404 after upload** — uploads use an application virtual path and the server assigns the physical location; view through `ViewDocument.aspx` rather than composing a physical path.

**Large file rejected** — above the shared upload policy's size limit; the policy varies by deployment and is not enumerated here.

**Duplicate document names** — the module performs **no uniqueness check**; same-named entries coexist.

## Boundaries and non-goals

- No uniqueness check, no versioning, no in-page preview (viewing opens a new window), no category tree or tags, no document-level permission (permissions stop at module level).
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Deployment tabs (TabId=2 / 4) are a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Against [Announcements]({{ site.baseurl }}/modules/announcements/): this is the **only module in this batch with file upload and a path security policy**, which is why its common questions cluster around upload and path issues.