---
layout: default
title: Operator handbook: running the portal day to day
lang: en
parent: Learn
nav_order: 2
---

# Operator handbook: running the portal day to day

This handbook is for **site operators**: no code, just content, tabs and visibility. To change how a module behaves, or to read the source, go to the Reference section.

## You need permission first

- The administration pages all live under `/Admin/` in the portal (the paths below are **portal** paths, not documentation-site paths).
- Every request re-checks the permission; it is not a one-off grant at sign-in.
- Not seeing an administration page usually means **you lack the role that page requires**, not that the page is broken.

## Publish an announcement

Announcements are the "Portal announcements" block in the middle of the home page. The entry point is on the **tab that carries the announcements**: "Add New Announcement" on the right of the module title bar.

### Create one

1. Open the tab that carries the announcements and click **"Add New Announcement"** on the right of the title bar.
2. Fill in the **title** (required, at most 100 characters).
3. Fill in the **description** (required, no length limit).
4. Fill in the **expiry date** (required, in a format that parses as a date, for example `12/31/2026`).
5. Add a **read-more link** and a **mobile link** if you need them (both optional, at most 100 characters each).
6. Save. The new announcement appears in the list.

The description and the links are **not validated as you type**: a wrong link raises no error and only misbehaves when clicked.

### Edit one, or retire it

- Edit: find the entry and click the **edit entry** on its right (with no edit permission the entry **does not appear at all**, rather than failing when clicked).
- Retire: set the **expiry date** to a date in the past and save. From then on the announcement no longer shows.
- Delete: the edit page has a delete button.

There is **no "never expires" switch**. To keep an announcement up, set a far-future expiry date and come back to move it.

### Who can see an announcement

Each announcement carries an item-level **visible-to-guests** flag, and it is **off by default** — anonymous visitors see only the entries explicitly flagged for them.

That flag currently has **no check box in the administration UI**; it can only be set at the data layer by the deployment side. In other words: making an announcement visible to guests needs that deployment-side step, not a tick in the back office.

## Adjusting tabs

### Name, authorized roles, mobile visibility

The "Tab name and layout" page (`/Admin/TabLayout.aspx`) owns:

- The tab name and its layout
- **Authorized roles** (tick the roles that may see the tab)
- Mobile visibility

Changing the authorized roles is written to the database and audited (category `TabAdministration`, action `Update`). A newly created tab defaults to the "All Users" role.

### Tab ordering has no administration entry point

Ordering and deleting tabs used to live on the "legacy tab management" page, but that page has **no host page** and cannot be opened. So tab order currently has to be adjusted at the data layer by the deployment side.

### Making a tab visible to signed-in users only

Some tabs should show to signed-in users and not to anonymous visitors. That does not come from authorized roles (anonymous visitors are in no role at all) but from the **navigation-visibility settings**:

| Setting key | What it does |
| --- | --- |
| `Portal.Navigation.AnonymousHiddenNavigationKeys` | Comma-separated tab keys hidden from anonymous visitors only |
| `Portal.Navigation.HiddenTabIds` | Tabs hidden outright |
| `Portal.Navigation.HideTabsWithoutRenderableModules` | Hides tabs whose modules are all disabled by the active profile |
| `Portal.Navigation.ExcludedTabIdPrefixes` | Tabs excluded by key prefix |

## Enabling and configuring modules

- **Module package catalog** (`/Admin/ModuleCatalog.aspx`): which module packages are available.
- **Module type definitions** (`/Admin/ModuleDefinitions.aspx`): how module types relate to profiles.
- **Module settings** (`/Admin/ModuleSettings.aspx`): the metadata of a single **instance**, plus the **editor roles** for it — the announcement edit permission works from there.
- To make a module appear on a tab, or disappear from it, you change the **mounting relationship between tabs and modules**, not the module itself.

## Site settings

The **portal name** (the string in the sidebar) currently has **no reachable administration entry point** — the legacy site-settings control that owns it has no host page. Changing it has to happen at the data layer, by the deployment side.

Theme appearance lives on the "Theme settings" page (`/Admin/ThemeSettings.aspx`): the global theme plus per-tab overrides.

## Quick map of the administration pages

| Page | Path | What it is for |
| --- | --- | --- |
| Business applications | `/Admin/BusinessApplications.aspx` | Review business requests |
| Employee profile correction requests | `/Admin/EmployeeProfileCorrectionRequests.aspx` | Review profile corrections |
| Enterprise collaboration items | `/Admin/CollaborationItems.aspx` | Handle collaboration items |
| Business to-dos | `/Admin/WorkItems.aspx` | See to-dos and act on them |
| Employee directory | `/Admin/EmployeeDirectory.aspx` | Organizations, employees, account bindings |
| User-employee binding | `/Admin/UserEmployeeBindingEdit.aspx` | Bind an account to an employee |
| Role members | `/Admin/SecurityRoles.aspx` | Add or remove role members |
| Capability permissions | `/Admin/CapabilityPermissionMatrix.aspx` | The permission matrix for business capabilities |
| Operations audit | `/Admin/OperationAudits.aspx` | Who did what, and when |
| Monitoring logs | `/Admin/DiagnosticsLogs.aspx` | Runtime logs |
| System health | `/Admin/SystemHealth.aspx` | Health checks |

## What this page does not cover

- The full validation rules for announcement fields → [the announcements module page]({{ site.baseurl }}/en/modules/announcements/)
- The complete set of tab-visibility settings → [application settings contract]({{ site.baseurl }}/en/contracts-app-settings/)
- How module packages and profiles relate → [the module manifest contract]({{ site.baseurl }}/en/contracts-module-manifest/)
- How to add a module of your own → [the module development guide]({{ site.baseurl }}/en/module-development/)

