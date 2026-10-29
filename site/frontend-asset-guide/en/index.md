---
layout: default
lang: en
parent: Reference
nav_order: 16
permalink: /en/frontend-asset-guide/
title: Web Forms presentation and frontend assets
---
# Web Forms presentation and frontend asset maintenance

## Scope

This guide describes the Web Forms presentation and frontend build contracts currently in the main repository. It applies to
maintaining the default portal theme, trusted module styles and the existing Gulp automation; it is **not** a responsive
refactor plan, a frontend framework migration, or a dynamic asset management scheme.

## Formal inputs and load order

1. `src/Portal/Components/PortalPage.cs` resolves and applies the single native Web Forms Theme in `PreInit`.
2. `src/Portal/App_Themes/{ThemeName}/` provides the theme manifest and `Default.css`; for theme selection, tab overrides and
 the fallback rule, see [theme-package-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/theme-package-guide.md).
3. `src/Portal/Default.master` provides the default Banner, the single server form and the `MainContent` placeholder.
4. `src/Portal/Default.master.cs` writes stable theme/Tab CSS classes before rendering, and mounts only validated in-site CSS
 for the current tab's enabled, trusted module packages.
5. Module package styles such as `DesktopModules/ModuleProbe/Styles/ModuleProbe.css` must pass module catalog validation. Theme
 resources, external URLs and module JavaScript are **not** auto-loaded by the Master page.

The default theme continues to support IE9+ compatible CSS. Do not rely on CSS variables, Grid, Flexbox, remote fonts, remote
resources or theme scripts inside a theme. Body text uses the open-source-first font stack with a generic fallback; for
licensing boundaries see
[font-policy-and-audit.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/font-policy-and-audit.md).

## Gulp and dev-tool boundaries

The following binding in `src/Portal/gulpfile.js` belongs to the existing Visual Studio Task Runner behaviour and **cannot**
be replaced by a VSCode task:

```js
/// <binding ProjectOpened='startWatch' />
```

- `startWatch`: starts the watcher when Visual Studio opens the project; it does not run a first build.
- `stopWatch`: writes the watcher signal file so the existing watcher exits cleanly.
- `assets:build`: a one-off build aimed at VSCode and AI automation; it does not modify the Visual Studio bindings above.

Gulp only looks in the `src/Portal` working directory for `js/**/*.src.js`, `js/**/*.coffee`, `css/**/*.scss` and
`css/**/*.sass`, writing the generated `.js`, `.css` and source maps back to the corresponding directories. A frontend build
is **not** an implicit prerequisite of portal compilation or IIS Express startup; run it only when you are explicitly
maintaining these inputs:

```powershell
cd src\Portal
npm run assets:build
npm run assets:watch
npm run assets:stop-watch
```

## Untracked asset boundaries

`src/Portal/js/`, `src/Portal/css/` and several generated documentation directories have not yet had their provenance,
licensing, input/output relationships and commit policy confirmed. Until a separate asset-governance task settles this,
day-to-day maintenance must **not** read, move or commit these directories, nor treat them as a prerequisite for building or
starting the portal. When new formal frontend source or build output is needed, establish ownership, licensing, browser
compatibility scope, `.gitignore` rules and the verification method first.

## Verification

The following script only reads formally tracked Master, theme, module CSS, Gulp, `package.json` and this guide; it does not
invoke Node, npm, Gulp, IIS Express or a database:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalFrontendContracts.ps1
```

The script verifies the default Master structure, theme and module CSS load boundaries, the Visual Studio/VSCode Gulp tasks,
the IE9+ Browserslist, the theme manifest, the font-deny list and public documentation entry points. Real page loading should
still be re-checked with the IIS Express smoke described in
[testing-checklist.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/testing-checklist.md).

Legacy browser compatibility work should also run the static gate:

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalLegacyCssCompatibility.ps1
```

That script reads only Git-tracked formal theme and module CSS. Flex/Grid, CSS variables, modern units, modern filters and CSS
gradient dependencies are treated as blockers; IE8 visual degradations such as rounded corners, shadows and opacity default to
warnings only. Real IE8/IE9 or older domestic-engine behaviour still needs evidence from a cloud testing platform, an old VM
or manual verification in the target environment.

<!-- build-stamp: 8FDD44336C9D -->
