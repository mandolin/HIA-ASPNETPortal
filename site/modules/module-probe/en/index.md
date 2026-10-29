---
layout: default
lang: en
parent: Learn
nav_order: 25
permalink: /en/modules/module-probe/
title: Module Probe (ModuleProbe)
---
# Module Probe (ModuleProbe)

> Module directory: `src/Portal/DesktopModules/ModuleProbe/`｜View: `ModuleProbe.ascx`｜**No standalone edit page, no input controls, no submit action**
> This page is the **detailed operating guide**; for a one-line summary see the ["Common modules"]({{ site.baseurl }}/user-guide/) section of the user guide.
>
> **First, what it is not**: this is a **read-only verification sample for a trusted-deployment module package**, **not a business handling entry**. It writes no business data and offers no script, upload or external-link entry. Treating it as a business module is a misreading.

## Purpose and prerequisites

The module probe **verifies the module package and the theme scope**: once placed on a tab it renders five **deployment facts about the module itself** as read-only labels —

- Module (`ModuleLabel`)
- Module package (`PackageLabel`)
- Placement (`PlacementLabel`)
- Rendered time (`RenderedUtcLabel`)
- Theme scope (`ThemeScopeLabel`)

Operators and developers use this to confirm **which trusted package the module came from**, **where it is placed**, **with which theme scope it renders**, and **when it was rendered**.

Prerequisites:

1. The module must be placed on a tab before it appears — **placing it is the whole operation**.
2. No sign-in and no permission are needed: it reads only portal settings (`.GetPortalSettings(`) and the current CSS class (`.GetCurrentCssClass(`), never business data.

## Operating steps

**Read the probe information**

1. Place the module probe on the target tab.
2. Open that tab and read the five read-only labels on the page.

**There is no second step** — this module has no buttons, no form and no submit action. Reading the information is the entire purpose.

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| (no input fields) | — | — | — | — | — |
| Module (read-only) | `ModuleLabel` | Read-only label | — | — | — |
| Module package (read-only) | `PackageLabel` | Read-only label | — | — | — |
| Placement (read-only) | `PlacementLabel` | Read-only label | — | — | — |
| Rendered time (read-only) | `RenderedUtcLabel` | Read-only label | — | — | — |
| Theme scope (read-only) | `ThemeScopeLabel` | Read-only label | — | — | — |

This module has **no input controls** (the count of `TextBox` / `DropDownList` / `CheckBox` / `HtmlTextArea` is **0**) and no buttons. All five values are read-only labels — that follows from its purpose: **it is meant to be read, not filled**.

## Required and validation

- **Nothing is required**: no input controls, and the `RequiredFieldValidator` count is **0**.
- **No format validation**: there is no field to fill.
- **No action to validate**: no submit, no upload.

## Storage and audit

- **Storage**: **nothing is written** — neither a business table nor module settings.
- **Audit**: **no operations audit is written** (this module has no auditable action).
- **Reads**: only `.GetPortalSettings(` (portal settings) and `.GetCurrentCssClass(` (current CSS class).
- This is the contrast with the other business modules: they all write audit events under `BusinessModuleCategory`, while this one writes none — **because it has no business action at all**.

## Permissions

- It renders deployment facts read-only and **depends on no business permission**.
- It reads no business data, so "seeing other people's data" cannot arise.
- It has no submit action, so write permission does not apply.

## Common questions

**Nothing on the page is clickable** — correct. This module has **no buttons**; the five values are read-only labels.

**Why does it write no audit?** — because it has **no business action**: no write, no submit, no settings change. Audit records actions, and this module has none to record.

**Can I use it to collect business data?** — no, and it should not be used that way. It is a **read-only verification sample** with no script, upload or external-link entry.

**The rendered time looks wrong** — that label shows the render time (UTC semantics, see `RenderedUtcLabel`); check the time zone rather than changing the module.

## Boundaries and what it does not do

- This module **is a read-only verification sample, not a business handling entry** — that is its only, and its most important, qualification.
- It **writes no business data**, **writes no audit**, and **offers no script, upload or external-link entry**.
- It has **no standalone edit page, no input controls and no submit action**.
- It **does not replace** monitoring or deployment-verification tooling: it is a **sample** for manually checking the module package and theme scope, not an automated inspection.
- Contrast with the other business modules: [Expense reimbursement]({{ site.baseurl }}/modules/expense-reimbursement/) and [Leave request]({{ site.baseurl }}/modules/leave-request/) persist on submit and write audit records; this module is read-only, zero writes, zero audit.

<!-- build-stamp: C083623FCB6D -->
