# Site source (HIA-ASPNETPortal website)

[![Publish site](https://github.com/mandolin/HIA-ASPNETPortal/actions/workflows/publish-site.yml/badge.svg)](https://github.com/mandolin/HIA-ASPNETPortal/actions/workflows/publish-site.yml)

<!-- markdownlint-disable MD033 -->
<p><a href="../README.md">← 中文</a></p>
<!-- markdownlint-enable MD033 -->

> This directory is the **versioned source of the website** (established in C-anp-P22 `W-anp-P130`) and is **not runtime
> output**. Pages are **generated** by `dev/scripts/Build-PortalSite.ps1` from the repository's `docs/` (Chinese
> authoritative source) and `site/_translations/` (English renders) — no separate, code-detached documentation set
> (Principle 4).

## Layout

| Path | Purpose |
| --- | --- |
| `_config.yml` | Jekyll configuration: `Just the Docs` theme (remote theme), `lang: zh-CN`, `color_scheme: light` |
| `_data/navigation.yml` | Navigation skeleton: **8 sections**, each with Chinese and English titles (pure sections, no `url`) |
| `_data/published.yml` | **Published list** (`doc` / `path` / `parent`); only pages listed here are generated |
| `_translations/` | English render sources (**one-to-one** with documents under `docs/`) |
| `index.md` | Chinese home page |
| `en/index.md` | English home page |
| `<path>/index.md`, `en/<path>/index.md` | **Generated output** (Chinese/English pair) — do not hand-edit |

Visual values (palette / typeface / typography) are specified in
[`docs/site-visual-baseline.md`](../../docs/site-visual-baseline.md).

## Generate locally

```powershell
./dev/scripts/Build-PortalSite.ps1
```

The script also enforces three checks and exits 1 if any fails:

- **Bilingual parity**: a declared-published page must have both language versions — **a missing translation fails**;
- **N1**: a page's `parent` must exist in `_data/navigation.yml`;
- **N2**: in-site links within site pages must resolve (added in `W-anp-P132` after navigation and home-page links all
  resolved to 404 with no gate noticing).

An independent public-surface gate also exists:

```powershell
./dev/scripts/Test-PortalSitePublicSurface.ps1   # S1 private-repo links / S2 credential shapes / S3 absolute local paths / S4 internal plan paths
```

## Publishing

After a push to `main`, `.github/workflows/publish-site.yml` regenerates the site, runs three static gates, and publishes
the `site/` content to the **`gh-pages` branch**.

> **Why `gh-pages` and not the `/docs` folder**: `docs/` is already occupied by 27 maintained public documents; using it
> as the GitHub Pages `/docs` source would publish those maintenance documents as site pages and force site configuration
> into the documentation directory. The detailed trade-off record lives in this project's **internal planning repository**
> (not published with the site).
>
> ⚠️ **Do not write internal planning paths under `site/`**: this directory is published wholesale to the public website,
> and check S4 of `Test-PortalSitePublicSurface.ps1` blocks such references (it caught the original draft of this very page
> on its first run, in `W-anp-P134`).

**Enablement steps** (not yet performed):

1. Repository Settings → Pages → set the publishing source to the **`gh-pages` branch**;
2. Watch the first workflow run.

## Relationship to the local L0 gate suite

- CI runs only the **compile-free static subset** (generation + bilingual/N1/N2 + public surface + docs compliance);
- The **full L0 suite** (`Invoke-PortalGateSuite.ps1 -Layer L0`) still runs **locally** — it contains .NET Framework 4.8
  build gates that require Windows + Visual Studio/MSBuild.

They do **not** duplicate each other: the overlapping parts are **the same script**, not two implementations.

## Bilingual parity (hard constraint of this cycle)

- The Chinese tree sits at the site root, the English tree under `/en/`, **one-to-one**;
- A page may be added to `_data/published.yml` **only when both its Chinese and English versions are ready**;
- Two pages are published so far; the remaining `docs/` documents are translated in batches.
