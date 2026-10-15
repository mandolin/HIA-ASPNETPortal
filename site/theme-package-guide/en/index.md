---
layout: default
lang: en
parent: 开发
permalink: /en/theme-package-guide/
title: Theme package deployment guide
---
# Theme package deployment guide

## Scope

This project currently accepts only theme packages written into `src/Portal/App_Themes/` by the trusted deployment pipeline.
In the back office, an administrator can only choose a global theme from deployed, validated themes or set a per-tab override;
they cannot upload ZIPs, edit CSS, enter external URLs, or let a theme auto-load JavaScript.

This is a **current** security boundary, not a promise that richer theme operation capabilities are excluded forever. If
theme package sources or distribution are opened up later, a **standalone trusted-package mechanism** will be built first —
covering source attestation, review/signing, licensing, version compatibility, rollback and deployment ownership — rather
than simply widening the back office's file-write permission.

## Directory and manifest

Every selectable theme lives at `App_Themes/{ThemeName}/` and contains at least:

```text
App_Themes/
  {ThemeName}/
    Default.css
    theme.json
    Images/              # optional; relative resources inside the theme directory only
```

`ThemeName` must match `^[A-Za-z][A-Za-z0-9_-]{0,63}$` and be exactly identical to the manifest's `name`. The current
manifest version is `schemaVersion: 1`:

```json
{
  "schemaVersion": 1,
  "name": "ExampleTheme",
  "displayName": "示例主题 / Example Theme",
  "version": "1.0.0",
  "minimumPortalVersion": "1.0",
  "inheritsDefault": true,
  "resources": [
    "Default.css"
  ]
}
```

`resources` must declare `Default.css`, and every resource must be a relative file that actually exists inside the theme
directory. The manifest must not contain `script`, `scripts`, `externalUrl` or `externalUrls`; `.js` files do not pass
validation as theme resources.

## Scope and selection

The portal applies exactly **one** native Web Forms `Theme` per page. Effective values, highest priority first:

1. The current portal tab's theme override.
2. The database runtime-level global override.
3. The deployment-level `Portal.Theme.Name`.
4. The built-in `Default` safe fallback.

Admin pages use only the global theme, and the generic error page always uses the minimal Default styling. Each portal page's
`body` emits `portal-theme-{theme}` and `portal-tab-{id}`, and module containers emit `portal-module`, `portal-module-{id}`
and `portal-pane-{pane}`. Theme CSS should rely on these stable classes for local overrides and must not assume that
multiple native Themes can be stacked on one page.

The administrator entry point is `Admin/ThemeSettings.aspx`. It lists only themes that pass deployment validation, and every
select/clear action is written to the existing settings or to the operations audit record.

## Release and rollback

1. After reviewing the theme directory, licensing and compatibility in an isolated environment, deploy the complete
   directory as the release artefact.
2. Confirm that `theme.json`, `Default.css` and their declared resources all exist, then restart the application or wait for
   the app domain to refresh per the deployment.
3. Visit the home page, the target tab, Admin pages and the generic error page first; for virtual-directory deployments also
   check resource paths.
4. If a problem occurs, clear the tab override or select `Default` in `Admin/ThemeSettings.aspx`; if the package itself is
   faulty, roll back the deployed version of that theme directory.

Theme CSS must keep the IE9+ baseline: CSS Custom Properties, Grid and Flexbox must not be treated as required mechanisms.
