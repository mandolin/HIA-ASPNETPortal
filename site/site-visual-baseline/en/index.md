---
layout: default
lang: en
parent: Reference
permalink: /en/site-visual-baseline/
title: Site visual baseline
---
> This page is the site's **visual baseline specification**: it fixes the palette, typeface, type scale, and the colour
> sources for code blocks, tables and the navigation skeleton. It is the source of truth for the values in
> `site/_config.yml` and `site/_sass/` (if overridden) — **change it here, never hand-edit generated output**.
>
> **Single-source discipline**: the values below are **not a new design** — they are extracted from the
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

> `--portal-text-subtle` was added by the theme engineering decision, for colour layering between heading and subheading.
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
| One-to-one | Every page is available in both Chinese and English, switchable at any time |

## 8. Bilingual typography conventions

- The Chinese tree sits at the site root, the English tree under `/en/`; **paths and file names correspond one-to-one**
  (e.g. `product-overview/` ↔ `en/product-overview/`).
- Each page offers a cross-language entry at the **top** (already present on the home page) so any page can be switched in
  one click.
- Bilingual text must **not** be machine placeholder: a translation must be semantically equivalent to its source,
  confirmed by a human walkthrough (see `docs/documentation-review-checklist.md`).

## 10. HIA series main palette and landing styles (sourced wiring)

`color_scheme` in `site/_config.yml` is the custom scheme **`hia`** (defined in `site/_sass/color_schemes/hia.scss`).

- **Colour authority (user request)**: the HIA series' own design tokens —
  the `:root` of `HIA-Theme-Builder/HIA/web/src/style.css`
  (primary `#0047AB`, secondary `#00A8D3`, blue scale p50–p900, cyan scale s50–s900,
  `surface #F3F6F9` / `surface-2 #EAF0F5` / `panel #FFFFFF` / `line #DCE5EC`,
  `text #172633` / `muted #667782`, fonts Inter + Noto Sans CJK SC).
  The `--portal-*` matrix in section 2 of this file remains the value basis for the **product portal skins**
  (the site keeps product-semantic alignment), but the site's own brand colours follow the HIA series tokens.
- **Landing styles**: `site/_sass/custom/custom.scss` + `site/_layouts/home.html` (theme override point and custom layout).
  **The benchmark is orchardcore.net** (user request), following its "main site vs documentation, two distinct styles"
  approach: **the landing/marketing page** copies the main-site block order — own top nav, hero (big headline +
  copyable command block + dual CTA), stat row, capability and feature card grids with small-caps section eyebrows,
  CTA band, getting-started cards, FAQ accordion (native `<details>`, no JS) and a multi-column footer;
  **documentation pages** are rewritten in the style of docs.orchardcore.net (Material for MkDocs): dark top bar
  (brand + search box + repo link + language switch), left nav tree (upstream component, recoloured), **no breadcrumb
  bar** (as in Material), article + **sticky right "Table of contents" rail** (generated in the layout by splitting the
  rendered HTML on h2 ids), **previous/next pagination** within the same section, uppercase headings with anchors,
  admonition-style blockquotes (HIA p50 base + primary left bar), **dark code blocks** and a dark footer bar.
  The two styles are deliberately different (the OC main-site vs docs-subsites split). Block structure is copied;
  colours stay HIA tokens.
- **Variable names**: verified one by one against the local gem `just-the-docs v0.12.0`
  (`_sass/color_schemes/light.scss`), not guessed.
- **Code highlighting**: the scheme file keeps the upstream
  `@import "./vendor/accessible-pygments/github-light"` at the end (same as `light.scss`).
- **On theme upgrades**: re-verify the new version's `light.scss` variable names; if upstream renames or adds/removes
  palette variables, update the scheme file and custom.scss. The mechanism uses the theme-supported `color_scheme`
  and custom hooks, touching no theme-internal files.
