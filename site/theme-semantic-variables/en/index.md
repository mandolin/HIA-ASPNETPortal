---
layout: default
lang: en
parent: Reference
permalink: /en/theme-semantic-variables/
title: Theme semantic variables
---
# Theme semantic variable matrix (`--portal-*`)

This document is the human-readable baseline for the portal theme layer's **semantic variables**: it records which
`--portal-*` variables each of the 7 themes defines, what their values are, and what each is for.

> **Authority note**: this document is a **snapshot extracted from `src/Portal/App_Themes/*/Default.css`**, provided for
> reading and cross-checking. The machine-checkable authoritative source is the CSS itself plus the corresponding gate
>. Where the two disagree, **the CSS and the gate win**, and this document should be updated.
> This follows the project's standing principle: registry entries go stale, and the only reliable thing is a repeatable
> check.

## 1. Why semantic variables exist

If module CSS hard-codes colours (e.g. `color: #8a1f1f`), it becomes "dark on dark" under a dark skin, cut off from the
skin's palette, and new skins are easy to misconfigure. Switching to "the theme layer defines semantic variables → modules
only write `var(--portal-*, fallback)`" gives:

- module appearance switches with the skin automatically and stays consistent across skins;
- a new skin only needs the variable values filled in, with nothing easy to miss;
- fallback values guarantee that older engines without CSS custom properties still get the original colour.

## 2. Variable matrix (7 themes × 14 variables)

The 7 themes are the 6 official skins plus the `Default` fallback theme: `EnterpriseLight` / `EnterpriseDark` / `OaLight` /
`OaDark` / `StateClassicLight` / `StateClassicDark` / `Default`.

| Variable | Default | EnterpriseLight | EnterpriseDark | OaLight | OaDark | StateClassicLight | StateClassicDark |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `--portal-surface` | `#ffffff` | `#ffffff` | `#18222e` | `#ffffff` | `#18222e` | `#ffffff` | `#18222e` |
| `--portal-surface-2` | `#f5f8fb` | `#f5f8fb` | `#1f2b3a` | `#f5f8fb` | `#1f2b3a` | `#f5f8fb` | `#1f2b3a` |
| `--portal-border` | `#d8dee8` | `#d8dee8` | `#2b3a4d` | `#d8dee8` | `#2b3a4d` | `#d8dee8` | `#2b3a4d` |
| `--portal-border-subtle` | `#edf1f5` | `#edf1f5` | `#243140` | `#edf1f5` | `#243140` | `#edf1f5` | `#243140` |
| `--portal-border-strong` | `#c2ccd8` | `#c2ccd8` | `#3a4d63` | `#c2ccd8` | `#3a4d63` | `#c2ccd8` | `#3a4d63` |
| `--portal-text-strong` | `#2f475f` | `#2f475f` | `#e6edf5` | `#2f475f` | `#e6edf5` | `#2f475f` | `#e6edf5` |
| `--portal-text-muted` | `#5b6b7d` | `#5b6b7d` | `#9fb0c3` | `#5b6b7d` | `#9fb0c3` | `#5b6b7d` | `#9fb0c3` |
| `--portal-text-subtle` | `#41556b` | `#41556b` | `#c2cadf` | `#41556b` | `#c2cadf` | `#41556b` | `#c2cadf` |
| `--portal-field-bg` | `#ffffff` | `#ffffff` | `#121a24` | `#ffffff` | `#121a24` | `#ffffff` | `#121a24` |
| `--portal-field-border` | `#cdd7e2` | `#cdd7e2` | `#33425a` | `#cdd7e2` | `#33425a` | `#cdd7e2` | `#33425a` |
| `--portal-field-text` | `#1f2d3d` | `#1f2d3d` | `#dce6f0` | `#1f2d3d` | `#dce6f0` | `#1f2d3d` | `#dce6f0` |
| `--portal-list-header-bg` | `#eef2f7` | `#eef2f7` | `#1e2a38` | `#eef2f7` | `#1e2a38` | `#eef2f7` | `#1e2a38` |
| `--portal-list-row-alt` | `#f7f9fc` | `#f7f9fc` | `#161f2a` | `#f7f9fc` | `#161f2a` | `#f7f9fc` | `#161f2a` |
| `--portal-danger-text` | `red` | `#b42318` | `#ff8b8b` | `#b42318` | `#ff9b9b` | `#9e2f2e` | `#ff9c99` |

### Two patterns in the matrix (understand these before adding a variable)

1. **The first 13 variables come in one light-family set and one dark-family set**: the 4 light themes
   (Default / EnterpriseLight / OaLight / StateClassicLight) have identical values, and the 3 dark themes
   (EnterpriseDark / OaDark / StateClassicDark) also have identical values. They express **light/dark tonality**, not
   theme-family style (including `--portal-text-subtle`, added).
2. **`--portal-danger-text` is the only variable distinguished by theme family**: because it does not express light/dark
   but rather **each theme family's own error colour** (taken directly from that theme's existing `.NormalRed`). The
   Enterprise and Oa families share the same light error colour (`#b42318`) but differ in dark (`#ff8b8b` / `#ff9b9b`);
   StateClassic differs at both ends (`#9e2f2e` / `#ff9c99`).

**Corollary**: when adding a semantic variable, first decide which class it belongs to — one that expresses light/dark
tonality should join the "light/dark pair"; one that expresses an inherent theme-family property (such as an error or
accent colour) must be valued **per theme family**, preferably reusing that theme's existing class (such as `.NormalRed`)
rather than inventing a new colour.

## 3. Quick reference for variable purposes

| Variable | Purpose |
| --- | --- |
| `--portal-surface` | Module / card container background |
| `--portal-surface-2` | Secondary partition background inside a container (e.g. table header, group block) |
| `--portal-border` | Container outer border |
| `--portal-border-subtle` | Divider, subtle border |
| `--portal-border-strong` | Border needing emphasis (e.g. focus, selected) |
| `--portal-text-strong` | Strong text such as headings and subtitles |
| `--portal-text-muted` | De-emphasised text, explanatory text, placeholder wording |
| `--portal-text-subtle` | Subtitle level, valued between `--portal-text-strong` and `--portal-text-muted`, for colour layering between heading and subtitle |
| `--portal-field-bg` | Input control background |
| `--portal-field-border` | Input control border |
| `--portal-field-text` | Text inside an input control |
| `--portal-list-header-bg` | List header background |
| `--portal-list-row-alt` | Zebra-stripe row background |
| `--portal-danger-text` | Error / failure / validation feedback text |

## 4. Usage rules

1. **Module CSS must not hard-code colours.** Where a colour is needed, write `var(--portal-*, fallback)`, with the
   fallback being the value the rule had before, guaranteeing unchanged behaviour on older engines.
2. **All 7 themes must define it at the same time.** When adding a variable, not one of the 7 `Default.css` files may be
   missing it; a theme missing the definition falls back to the fallback value in browsers that support variables, causing
   "style drift in individual skins" that is hard to notice.
3. **Prefer reusing existing theme classes** rather than inventing new colours for a new variable. Reuse `.NormalRed` for
   the error colour, and existing values such as `.portal-admin-subtitle` for neutrals.
4. **Variables are defined only in the theme layer**; the module layer only references them. Defining another layer of
   semantic variables in the module layer creates two levels of indirection that are hard to trace.

## 5. How to reproduce this matrix

The following command extracts the matrix from the live CSS and can be used to check whether this document is stale:

```powershell
$themes = @('Default','EnterpriseLight','EnterpriseDark','OaLight','OaDark','StateClassicLight','StateClassicDark')
foreach ($t in $themes) {
    $text = Get-Content "src/Portal/App_Themes/$t/Default.css" -Raw -Encoding UTF8
    $vars = @{}
    foreach ($m in [regex]::Matches($text, '--portal-([a-z0-9-]+)\s*:\s*([^;]+);')) {
        $vars[$m.Groups[1].Value] = $m.Groups[2].Value.Trim()
    }
    Write-Host ("$t : " + (($vars.Keys | Sort-Object | ForEach-Object { "$_=$vars[$_]" }) -join '  '))
}
```

**Consistency expectation**: the set of variable names output by all 7 themes should be identical (currently 14); if a
theme lacks a variable, that is a missing definition and must be filled in.

## 6. Related documents

- `theme-package-guide.md`: directory, enablement, fallback and release boundaries for trusted theme packages.
- `frontend-asset-guide.md`: Web Forms presentation, themes, module CSS and frontend asset boundaries.
- For the module appearance contract (the class set for container / heading / partition / table / notice / muted / danger,
  plus per-skin palettes), see the output of 主题契约门禁.
