---
layout: default
title: Site visual baseline
lang: en
parent: Site visual baseline
nav_order: 1
---

> `C-anp-P22` (project website) `W-anp-P131` deliverable. Principle 1 gate: **produce the site visual prototype
> before landing content**.
>
> This page is the site's **visual baseline specification**: it fixes the palette, typeface, type scale, and the colour
> sources for code blocks, tables and the navigation skeleton. It is the source of truth for the values in
> `site/_config.yml` and `site/_sass/` (if overridden) — **change it here, never hand-edit generated output**.
>
> **Single-source discipline (Principle 4)**: the values below are **not a new design** — they are extracted from the
> project's existing CSS so the website matches the product portal. Authoritative sources:
> `src/Portal/App_Themes/EnterpriseLight/Default.css` (the `--portal-*` variable block) and
> `src/Portal/App_Themes/Default/Default.css` (type stack and sizes). When a snapshot goes stale, the CSS wins.

# Site visual baseline

## 1. Tone: light

Adopts the **`EnterpriseLight` light tone** — the same origin as the product's default light skin, so website and product
look consistent.

Trade-off: the site is a **standalone static site** (GitHub Pages) and does **not** need IE9+ compatibility, so CSS
variables and modern layout are fine; however **palette and typeface are still taken from the project CSS**, avoiding a
"site vs product" split.

> Note: the project's theme layer is itself IE9+-constrained (must not rely on CSS variables / Grid / Flexbox / remote
> fonts / remote assets / theme scripts). That constraint does **not** apply to the website; the site inherits only the
> **values**, not the compatibility constraint.

## 2. Palette

Taken from `EnterpriseLight`'s `--portal-*` semantic variables (light family; `Default`/`EnterpriseLight`/`OaLight`/
`StateClassicLight` share the same values):

| Use | Variable | Value |
| --- | --- | --- |
| Page background / card container | `--portal-surface` | `#ffffff` |
| Secondary partition (table header, group block, code block) | `--portal-surface-2` | `#f5f8fb` |
| Container outer border | `--portal-border` | `#d8dee8` |
| Divider / subtle border | `--portal-border-subtle` | `#edf1f5` |
| Emphasised border (focus, selected) | `--portal-border-strong` | `#c2ccd8` |
| Heading / strong text | `--portal-text-strong` | `#2f475f` |
| Explanatory / muted text / placeholder | `--portal-text-muted` | `#5b6b7d` |
| Subheading level | `--portal-text-subtle` | `#41556b` |
| Input control background | `--portal-field-bg` | `#ffffff` |
| Input control border | `--portal-field-border` | `#cdd7e2` |
| Input control text | `--portal-field-text` | `#1f2d3d` |
| List header background | `--portal-list-header-bg` | `#eef2f7` |
| Zebra-stripe row | `--portal-list-row-alt` | `#f7f9fc` |
| Error / failure / validation feedback | `--portal-danger-text` | `#b42318` |

> `--portal-text-subtle` was added by the `C-anp-P23` D3 ruling, for colour layering between heading and subheading.
> `--portal-danger-text` is the only variable that differs per theme family (see `docs/theme-semantic-variables.md`);
> the site uses the Enterprise light value `#b42318`.

## 3. Typeface

The type stack is taken directly from the project CSS (open-source-first with a generic fallback); **no remote fonts**:

```
"Noto Sans SC", "Source Han Sans SC", "Sarasa Gothic SC", "Liberation Sans", sans-serif
```

Monospace (code blocks) uses the theme's monospace stack, falling back to the `monospace` generic fallback.

> The project portal is IE9+-constrained and must not depend on CSS variables; the website, being standalone, **may** use
> variables, but **the values still come from the table above** so the two do not drift apart.

## 4. Type scale

The project's existing size ramp (extracted from `Default.css`) is reused, with a unified line height on top:

| Level | Size | Note |
| --- | --- | --- |
| Page title (H1) | 20px | Matches the project heading size |
| Body / notes (H2 and below share body size) | 13px | Matches the project body size |
| Auxiliary notes / footnotes | 11px | Matches the project small size |

Body line height **1.6** (readability baseline for long-form reading); heading line height **1.3**.

## 5. Code blocks

- Background: `--portal-surface-2` (`#f5f8fb`), same as card partitions, so blocks do not appear to float on bare white;
- Border: `--portal-border-subtle` (`#edf1f5`);
- Font: monospace stack;
- Long lines **scroll horizontally** rather than wrap (a wrapped command copied by a reader would run incorrectly);
- Language tag must be given explicitly (readers may misread unhighlighted code).

## 6. Tables

- Header background: `--portal-list-header-bg` (`#eef2f7`);
- Zebra rows: `--portal-list-row-alt` (`#f7f9fc`);
- Cell border: `--portal-border-subtle`.

> Wide tables scroll horizontally on narrow screens; never shrink font size to squeeze them in.

## 7. Navigation skeleton

Two one-to-one trees; top-level entries follow the reader path in `docs/documentation-map.md`:

product entry → evaluate → quick start → learn → reference → develop → operate → changes

| Convention | Detail |
| --- | --- |
| Sidebar | Driven by `site/_data/navigation.yml` (Just the Docs) |
| Language switch | Each page's front matter `lang` selects its tree; Chinese pages link `/en/<same path>/`, English pages link `/<same path>/` |
| One-to-one | Any published page must have both language versions; a missing translation voids the publish declaration (`Build-PortalSite.ps1` gate) |

## 8. Bilingual typography conventions

- The Chinese tree sits at the site root, the English tree under `/en/`; **paths and file names correspond one-to-one**
  (e.g. `product-overview/` ↔ `en/product-overview/`).
- Each page offers a cross-language entry at the **top** (already present on the home page) so any page can be switched in
  one click.
- Bilingual text must **not** be machine placeholder: a translation must be semantically equivalent to its source,
  confirmed by a human walkthrough (see `docs/documentation-review-checklist.md`).
