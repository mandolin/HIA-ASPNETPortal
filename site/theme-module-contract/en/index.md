---
layout: default
lang: en
parent: 开发
permalink: /en/theme-module-contract/
title: Theme-module appearance contract
---
# Theme-module appearance contract

This document defines the division of labour between a **business module's appearance** and the **theme layer**: the module
side owns layout and structure, the theme layer owns colour. It also records the real meaning of the fallback values in
`var(--portal-*, fallback)` and the known inconsistencies.

> Authority is the same as `theme-semantic-variables.md`: this document is a snapshot extracted from the live code, while the
> machine-checkable authoritative source is the CSS and the `C-anp-P18` / `W-anp-P100` gates.

## 1. Division of labour

| Layer | Owns | Does not own |
| --- | --- | --- |
| Module `Styles/<Module>.css` | Layout, structure, spacing, control arrangement | **Hard-coded colours** |
| Theme `App_Themes/<Theme>/Default.css` | Colour (through `--portal-*` semantic variables) | Module layout |
| Portal base layer (`.Head` / `.SubHead` / `.CommandButton` / `.NormalTextBox` / `.portal-*`) | Common controls and shell form | Module-private layout |

Module-private classes carry **layout only**; colour is always `var(--portal-*, fallback)`. For the variable matrix see
[theme-semantic-variables.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/theme-semantic-variables.md).

## 2. Module appearance class families and the variables they use

The seven business modules (`BusinessApplicationRequest` / `EmployeeProfileConfirm` / `EmployeeProfileCorrectionRequest` /
`EnterpriseCapabilityWorkbench` / `ExpenseReimbursement` / `LeaveRequest` / `ModuleProbe`) share the same appearance
structure, divided by responsibility into seven families:

| Family | Typical class name | Purpose | Variables applied |
| --- | --- | --- | --- |
| Container | `<module>-request` / `-panel` | Module card border and background | `--portal-surface`, `--portal-border` |
| Title | `<module>-title` | Main heading inside the module | `--portal-text-strong` |
| Subtitle | `<module>-subtitle` | Section heading for a form/list | `--portal-text-subtle` |
| Label | `<module>-label` | Field label (on top of `.SubHead`) | Inherits `.SubHead`; not coloured separately |
| Input | `<module>-input` / `-body` | Input control (on top of `.NormalTextBox`) | `--portal-field-bg`, `--portal-field-border`, `--portal-field-text` |
| Message | `<module>-message` | Save / validation / failure feedback | `--portal-danger-text` |
| List | `<module>-list` / `-list-wrap` | Data table and its wrapper | `--portal-border-subtle` (dividers) |
| Empty state | `.portal-empty-state` (shared class) | Zero-rows / no-match hint | Per-skin neutral colour (reuses the `.portal-admin-subtitle` family) |

## 3. The real meaning of fallback values (important)

In `var(--portal-danger-text, #8a1f1f)`, the `#8a1f1f` is **not a design value** — it is the hard-coded original value the
module had **before migration**, kept only so that "older engines without CSS custom properties still get the original
colour".

**Therefore: when reading module CSS, never treat the fallback as the design value.** The design value lives in the theme
layer; see the matrix in `theme-semantic-variables.md`.

### Audited differences between fallback values and theme values

| Symptom | Involves | Note |
| --- | --- | --- |
| Notation difference (same colour) | `#fff` ↔ `#ffffff` | Only the shorthand differs; the colour is identical, no impact |
| **One variable, two fallback values** | `--portal-text-strong`: title `#2f475f` / subtitle `#41556b` | **The original design used different colours for title and subtitle**; after migration both take the same theme value → the colour layering was flattened (see below) |
| Fallback diverges from theme value | `--portal-text-muted`: `#6b7c8f` ≠ theme `#5b6b7d` | Slightly darker than the theme on older engines |
| A whole module's own palette | `ModuleProbe` has 6 fallback values (`#52718a` / `#f4f8fb` / `#1f3546` / `#1b4f72` / `#496274` / `#31495c`) | A diagnostic module's own palette; none matches a theme value, and it is a low-priority diagnostic surface by design |
| Error colour | `--portal-danger-text`: `#8a1f1f` ≠ theme light `#b42318` | `W-anp-P98` changed the module side to take the variable; the difference is visible only as a fallback on older engines |

### D3 ruling: the colour layering of subtitle vs title (closed)

**Problem**: before migration the title was `#2f475f` and the subtitle `#41556b` (the subtitle slightly lighter); after
migration both resolve to the theme's `--portal-text-strong` (light family `#2f475f`, dark family `#e6edf5`), so the
**subtitle became as dark as the title** and the colour layering disappeared.

**D3 ruled for (b), adding a variable**: add `--portal-text-subtle`, valued between `--portal-text-strong` and
`--portal-text-muted`, defined once in each of the 7 themes (light family `#41556b`, dark family `#c2cadf`), restoring the
subtitle's intermediate colour. See the matrix in
[theme-semantic-variables.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/theme-semantic-variables.md).

The two rejected options:

- **(a) accept** the same colour and separate levels by size/weight — rejected: colour layering was the original design
  intent, and size/weight alone do not carry it.
- **(c) switch the subtitle to `--portal-text-muted`** — rejected: it would become lighter and depart from the original
  intent.

## 4. Checklist for a new module's appearance

1. Layout and structure belong in the module's `Styles/<Module>.css`.
2. Where a colour is needed, always use `var(--portal-*, fallback)`, with the fallback being the value that rule had before.
3. If a **semantic meaning the theme layer lacks** is needed, first read the "corollary for adding variables" in
   `theme-semantic-variables.md`: one expressing light/dark tonality joins the light/dark pair, while one expressing an
   inherent theme-family property is valued per theme family, preferably reusing an existing class.
4. **All 7 themes' `Default.css` must add the scope rule for that class**
   (`body.portal-theme-<name> .<module class>`); missing one means the module loses themed styling in most skins — treat it
   as unfinished.
5. If the interface form changes, produce a real rendered prototype (light + dark) and get it confirmed before touching CSS.

## 5. Related documents

- [`theme-semantic-variables.md`](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/theme-semantic-variables.md): the `--portal-*` variable matrix and usage rules.
- [`theme-package-guide.md`](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/theme-package-guide.md): directory, enablement, fallback and release boundaries for trusted theme packages.
- [`frontend-asset-guide.md`](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/frontend-asset-guide.md): Web Forms presentation, themes, module CSS and frontend asset boundaries.
- [`module-development-guide.md`](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md): development, registration, enable/disable and removal boundaries for trusted deployed module packages (including hidden traps when adding a module).
