---
layout: default
lang: en
parent: Learn
nav_order: 4
permalink: /en/modules/discussion/
title: Discussion
---
# Discussion

> Module directory: `src/Portal/DesktopModules/`｜View: `Discussion.ascx`｜Detail page: `DiscussDetails.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

A **threaded discussion area**: each thread can carry replies, and replies can carry child replies. The view lists threads with author and posting time; the thread title links to the detail page (**opens in a new window**).

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request).
2. The module must be mounted on a tab. Current deployment: **TabId=4 "TradeCraft 技巧与装备" (TradeCraft tips and gear) and "实战手册" (Field manual)**.
3. The detail page locates an item through **query parameters**: `DiscussDetails.aspx?ItemID=<id>&mid=<moduleId>`. **Both parameters are required** — opening the page without them targets no item.

## Operating steps

**Start a thread**: open the tab → click **「Add New Thread」** in the module title bar (**opens the detail page in a new window**) → fill in **title** (`TitleField`) and **body** (`BodyField`) → submit.

**Reply to a thread**: click the thread title to open the detail page → use the reply form at the bottom → click **`ReplyBtn`** → the reply is nested under the current thread; the expand/collapse state is driven by the **number of child replies**.

**Move between threads**: the detail page offers **previous / next** paging within the same module.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Thread / reply title | `TitleField` | Single-line text | 100 | Optional | None |
| Body | `BodyField` | Multi-line text | None | Optional | None |

| Submit control | Control id | Purpose |
| --- | --- | --- |
| Submit button | `ReplyBtn` | Submits title and body (a button, not an input control) |

Control ids, types and lengths are taken from the `DiscussDetails.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **No required validation**: the edit page registers **no `RequiredFieldValidator`** — neither the title nor the body is required, and no comparison, regular-expression or custom validation is registered either.
- On submit the server **HTML-encodes** title and body (`Server.HtmlEncode`) before storing, so submitted content is never parsed as markup. **That is not content validation**: an empty title and an empty body can still be submitted.
- The source recognises the **`Re: ` prefix** (`ReTitle`) to decide whether a reply follows the previous one and adjusts the displayed title. **That is display behaviour and does not affect required fields.**
- Practical note: the module will not block empty values, but an empty title makes the thread list unreadable — **treat an empty title as an entry mistake**.

## Storage and audit

- **Storage**: `DiscussionDB` — adding goes through `AddMessage` (module id, parent item id, author, title, body); reading splits into thread list (`GetTopLevelMessages`), single item (`GetSingleMessage`) and thread messages (`GetThreadMessages`).
- **Audit**: **this module's save path writes no operational audit record** — discussion content is retained, but there is **no separate audit trail of the operation**. The only content path that writes operational audit is [Signin]({{ site.baseurl }}/modules/signin/).

## Permissions

- Submitting a reply and the edit entry are governed by **module edit permission** (`HasEditPermissions(moduleId)`), **re-checked on each request**.
- Reading threads and the detail page requires no edit permission.
- Replies are **not scoped by author** — anyone with edit permission may reply to any thread, including other people's.

## Common questions

**The page opened by 「Add New Thread」 looks empty** — the detail page depends on `ItemID` and `mid`; typing the address without parameters locates nothing. Enter through the module title bar.

**My reply does not appear** — check that you clicked `ReplyBtn`; also, the list shows the expand state **by number of child replies**, so the reply may be collapsed.

**The reply title became "Re: …"** — that is the `Re:` prefix logic marking a response; normal display behaviour.

**Line breaks disappeared** — `BodyField` is multi-line and is HTML-encoded on storage. **This module offers no rich-text editing**, so presentation follows its own rules.

**I want to send an empty message; will it be blocked?** — No. **An empty title and body submit successfully**; check yourself.

## Boundaries and non-goals

- No required or format validation, no moderation or sensitive-word filtering, no rich-text editing, no attachments, no likes / follows / notifications.
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Deployment tab (TabId=4) is a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Against [Announcements]({{ site.baseurl }}/modules/announcements/) and [Events]({{ site.baseurl }}/modules/events/): Discussion is the **only module in this batch with no required field** and the **only one with no expiry** — it organises content by hierarchy and paging, not by filtering on a validity date.

<!-- build-stamp: 2F0AFD163785 -->
