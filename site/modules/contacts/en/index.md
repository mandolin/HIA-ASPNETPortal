---
layout: default
lang: en
parent: Reference
nav_order: 3
permalink: /en/modules/contacts/
title: Contacts
---
# Contacts

> Module directory: `src/Portal/DesktopModules/`｜View: `Contacts.ascx`｜Edit page: `EditContacts.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Maintains a **directory of people**: name, role, e-mail and two contact details. The view renders the e-mail as a clickable mail link and shows name / role / e-mail / contact details in order, with an edit entry on the right of each row.

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request).
2. The module must be mounted on a tab. Current deployment: **TabId=2 "员工联系信息" (Employee contact information)**.
3. This module only manages a **directory** — it does not manage accounts, permissions or the organisation structure.

## Operating steps

**Create a contact**: open the tab → click **「Add New Contact」** in the module title bar → fill in the **name** (the only required field) → fill in role, e-mail and the two contact details as needed (all four may be left empty and **the module supplies no defaults**) → save.

**Edit a contact**: click the **edit entry** on that row (visible **only to users with edit permission**) and save.

**Make the e-mail clickable**: once an e-mail is stored, the view renders it as a mail link. There is **no e-mail format check** — a non-existent address displays normally and the error only surfaces when someone actually tries to send.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| Name | `NameField` | Single-line text | 50 | **Required** | No format check |
| Role | `RoleField` | Single-line text | 100 | Optional | None |
| E-mail | `EmailField` | Single-line text | 100 | Optional | **No e-mail format check** |
| Contact detail 1 | `Contact1Field` | Single-line text | 250 | Optional | None |
| Contact detail 2 | `Contact2Field` | Single-line text | 250 | Optional | None |

Control ids, types and lengths are taken from the `EditContacts.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **Exactly one required field**: `NameField` (name), matching the single `RequiredFieldValidator` in source (`RequiredFieldValidator1`).
- **No second validator**: this module registers **no comparison, regular-expression or custom validation at all** — role, e-mail and both contact details are stored exactly as typed.
- This is the main practical risk: **you must check formats by hand**. The module will not complain about `abc` in the e-mail field or an implausible phone number.
- The 50-character limit on the name is an input constraint of the edit box.

## Storage and audit

- **Storage**: `ContactsDB` — add / update / delete / read go through that data-access class.
- **Audit**: **this module's save path writes no operational audit record**; the audit table will not show who changed which contact. This is a fact of the current implementation; arrange traceability outside the portal if needed.
- The only content path that writes operational audit is [Signin]({{ site.baseurl }}/modules/signin/).

## Permissions

- The visibility of the edit entry and the save action are governed by **module edit permission** (`HasEditPermissions(moduleId)`), **re-checked on each request**.
- Reading the list requires no edit permission.
- There is **no distinction between "may edit only my own entries" and "may edit all"** — edit permission means **all** contact rows are editable.

## Common questions

**"Name is required"** — `NameField` is empty or whitespace-only.

**Clicking the e-mail does nothing** — most likely the **address is malformed and nothing validated it**. Check spelling and domain by hand.

**A field displays as empty** — empty values are allowed and the module inserts no placeholder.

**I need more than two phone numbers** — only two contact fields exist (`Contact1Field`, `Contact2Field`).

**The name is truncated** — name is limited to 50 characters and contact details to 250.

## Boundaries and non-goals

- No format validation (e-mail, phone, role — none), no de-duplication (the same name may appear several times), no link to accounts or the organisation structure, no sort field (list order is not controlled here).
- **Writes no operational audit** (see above).
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Deployment tab (TabId=2) is a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Compared with [Announcements]({{ site.baseurl }}/modules/announcements/) this module carries **no business rule beyond the required check**, making it the **most lightly validated module in this repository** — which is exactly why **manual review is the only safeguard**.

<!-- build-stamp: 4AC799FF7E9B -->
