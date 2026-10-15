---
layout: default
lang: en
parent: 参考
permalink: /en/font-policy-and-audit/
title: Font policy and audit
---
# Font policy and audit

## Usage principles

- The project does not ship body-text font files with the site, avoiding unnecessary licensing, package size and cache
  maintenance costs.
- Page styles actively reference only open-source fonts with clear licences, and keep a `sans-serif` or `monospace` generic
  fallback.
- Verdana, Arial, Helvetica, Times New Roman, Courier New, Consolas, Segoe UI, Microsoft YaHei and other proprietary fonts or
  fonts with unclear licensing boundaries are not actively referenced.
- If font files ever need to be shipped with the project, the original licence, copyright notice, source, version and release
  audit record must be submitted at the same time.

## Current font stacks

Body interface text:

```css
font-family: "Noto Sans SC", "Source Han Sans SC", "Sarasa Gothic SC", "Liberation Sans", sans-serif;
```

Monospaced content:

```css
font-family: "Sarasa Mono SC", "Noto Sans Mono CJK SC", "Source Han Mono SC", "Liberation Mono", monospace;
```

These names are only local font candidates and trigger no network download. When the client does not have them installed,
the browser falls back to the generic family, so site startup is unaffected.

## Licensing basis

| Font family | Licence | Official reference |
| --- | --- | --- |
| Noto | Open Font License | [Noto official usage notes](https://notofonts.github.io/noto-docs/website/use/) |
| Source Han Sans / Mono | SIL Open Font License 1.1 | [Source Han Sans LICENSE](https://github.com/adobe-fonts/source-han-sans/blob/master/LICENSE.txt) |
| Sarasa Gothic / Mono | SIL Open Font License 1.1 | [Sarasa Gothic LICENSE](https://github.com/be5invis/Sarasa-Gothic/blob/main/LICENSE) |
| Liberation Sans / Mono | SIL Open Font License 1.1 | [Liberation Fonts LICENSE](https://github.com/liberationfonts/liberation-fonts/blob/main/LICENSE) |

## Audit result, 2026-07-10

- The main project's Git currently tracks no `.eot`, `.otf`, `.ttf`, `.woff` or `.woff2` font binaries.
- The default theme has had its Verdana, Helvetica and Lucida Console references replaced with the open-source stacks above.
- Back-office role controls and the document upload control have had their inline Verdana/Arial bindings removed and now
  inherit the theme font.
- The old `src/Setup/SystemReqs.rtf` was not referenced by the project, and its font table contained several
  Office/Windows proprietary fonts; the existing `SystemReqs.md` keeps the same historical requirements content, so the
  redundant RTF was deleted.
- Untracked generated documentation contains the Font Awesome Free 6.1.1 icon font, whose CSS header declares the font under
  SIL OFL 1.1, the code under MIT and the icons under CC BY 4.0; that directory is still treated as generated output and is
  not part of this repository's font distribution.

## Audit result, 2026-07-19 (P7.5)

- The six P7 official themes ship no `.eot`, `.otf`, `.ttf`, `.woff` or `.woff2` font binaries.
- No `@font-face`, remote font URL or `data:font` was found under `src/Portal/App_Themes/`.
- The P7 themes continue to use the open-source-first stacks above and keep the `sans-serif` / `monospace` generic fallbacks.
- `Default`, `EnterpriseLight/Dark`, `OaLight/Dark`, `StateClassicLight/Dark` and `ThemeProbe` do not actively reference
  Verdana, Arial, Helvetica, Times New Roman, Courier New, Consolas, Segoe UI, Microsoft YaHei or 微软雅黑.
- The full P7.5 asset audit record is kept in this project's **internal planning document** (not published with the public
  documentation).
