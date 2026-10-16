---
layout: default
lang: en
parent: 快速上手
permalink: /en/user-guide/
title: User guide
---
# User guide

> For positioning, intended audience, capability map and boundaries see
> [product-overview.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/product-overview.md). This page focuses on
> **how to get it running, how to use it, and what to do when something breaks**.

## What it is

HIA-ASPNETPortal is an ASP.NET Web Forms portal application sample and retrofit project, providing portal pages, modular
content, role permissions and back-office administration.

## Running it locally — overview

Running locally usually requires:

1. Preparing SQL Server or LocalDB.
2. Executing the database scripts under `src/Setup/`, which must at least include the base scripts, the P2/P3 incremental
   scripts and `Portal_UserCredentials.sql`.
3. Copying the template from `src/Portal/Config/Templates/connectionStrings.config` to the external configuration directory
   and configuring the `Portal` connection string.
4. Building and running `src/master.sln` with Visual Studio.
5. Visiting the started site.

The default external configuration directory is `{current user profile}\Web\HIA-ASPNETPortal\{env}\`. Local development usually
uses the `dev` environment, i.e. `...\HIA-ASPNETPortal\dev\connectionStrings.config`.

## Initial account

Historical notes mention that `admin/admin` can be used to sign in. That information is only for local verification on older
versions; **any** shared, test or production environment must change the default account and password policy. Since P5.2, an
old account is migrated to a strong-hash credential on its first successful sign-in; creating, registering and resetting
passwords no longer writes legacy MD5 digests.

## Common modules

Portal modules currently in the source tree include announcements, contacts, discussions, documents, events, HTML, images,
links, quick links and XML. See `src/Portal/DesktopModules/` for the actual set.

## Administration entry point

Back-office pages live under `src/Portal/Admin/`, covering users, roles, module definitions, module settings, site settings
and page layout management.

## Business modules: leave requests and expense reimbursements

These two modules (`HIA.LeaveRequest`, `HIA.ExpenseReimbursement`) are employee self-service (ESS) business modules delivered
. They are deployed as module packages and are **not enabled by default with the core profile**.

### Enabling prerequisites

1. Run the business-table migration scripts: `src/Setup/PortalBiz_LeaveRequests.sql` and
   `src/Setup/PortalBiz_ExpenseReimbursements.sql`. Both are re-runnable (an `OBJECT_ID` guard is checked before creating
   tables).
2. Allow-list the module packages: append `HIA.LeaveRequest,HIA.ExpenseReimbursement` to `Portal.ModulePackages.Enabled`
   (`appSettings.json` and environment override files). This key is unrelated to Profile and exists to allow deployment
   packages across profiles.

   > **Precondition for an override to take effect**: `appSettings.json` only overrides the same-named `<appSettings>` key in
   > `Web.config` when it is **deployed to the site root** — `AppSettingsLoader` silently skips the file when it does not exist,
   > so the runtime still reads the base value from `Web.config`. Changes to the uncommitted `appSettings.dev.json` on a
   > development machine never take effect. See "Hidden traps when adding a module" item 4 in
   > [module-development-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md).

3. An administrator mounts a module instance on a tab (a module definition only appears in the mountable list after its
   package is enabled).

### Leave requests

- Form fields: leave type (annual / personal / sick / compensatory), start date, end date, leave days, attachment, reason.
- After submission the status is fixed to `Pending`, and the most recent 20 requests of the current user appear in "My leave".
- Submitting also creates one work item, assigned to an approval role (currently fixed to `Admins`), with business type
  `LeaveRequest`.
- Validation: type, start/end dates and days are required; days must be positive; the end date must not be earlier than the
  start date (a `CHECK` constraint also enforces this at the database layer).

### Expense reimbursements

- Form fields: expense category (travel / meals / office / other), amount, invoice or attachment, reason.
- After submission the status is fixed to `Pending`, and the most recent 20 claims of the current user appear in "My
  reimbursements".
- Submitting also creates one work item, with business type `ExpenseReimbursement`, likewise assigned to `Admins`.
- Validation: amount is required and must be positive (a `CHECK` constraint also enforces this at the database layer).

### FAQ

- **The module does not appear on the page**: first confirm the package has been added to
  `Portal.ModulePackages.Enabled` and that a module instance has been mounted; modules that are not enabled or not mounted are
  skipped, with the reason recorded in the diagnostic log.
- **It always shows "No records yet" and submitting fails**: the business tables were not created, so run the migration
  scripts above; when the tables are unavailable the module degrades to an empty state instead of throwing.
- **Both modules show only the form and produce no work item after submitting**: a work item needs an approver (a user or a
  role), and both modules assign it to the `Admins` role; if that role does not exist, no work item is generated.

## Typical portal configuration flow (how-to)

The typical order from nothing to "a portal page with modules is visible":

1. **Prepare the database**: run the scripts under `src/Setup/` (base scripts, P2/P3 incremental scripts,
   `Portal_UserCredentials.sql`).
2. **Configure the connection string**: copy the template from `src/Portal/Config/Templates/connectionStrings.config` to the
   external configuration directory and fill in the `Portal` connection string (see "Running it locally — overview" above for
   the default external directory). Connection strings are **never committed**.
3. **Build and start**: build and run `src/master.sln` in Visual Studio, or build from the command line as described in
   `dev-guide.md`.
4. **Sign in**: use an administrator account (see "Initial account" for the security constraints on default accounts).
5. **Create a tab**: in the page layout management under `src/Portal/Admin/`, add a tab and set its **access role**.
6. **Mount modules**: mount module instances on that tab. A module must be **enabled** before it appears in the mountable list
   — trusted module packages are allowed through `Portal.ModulePackages.Enabled` (see "Business modules" below).
7. **Set the theme**: pick a skin in the site settings. For theme variables and the fallback convention, see
   `theme-semantic-variables.md`.

> **Changed a setting but it has no effect?** Check which configuration actually takes effect first: `<appSettings>` in
> `Web.config` holds the base values, and only `appSettings.json` **deployed to the site root** overrides the same-named key;
> changes to the uncommitted `appSettings.dev.json` on a development machine never take effect. See "Hidden traps when adding
> a module" item 4 in
> [module-development-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md).

## Common administrative operations (how-to)

Back-office pages live under `src/Portal/Admin/`. Common operations:

| What you want to do | Where | Notes |
| --- | --- | --- |
| Manage users | User management | Create / disable accounts; for the password system see below |
| Manage roles | Role management | Roles are the basic unit of authorisation; `Admins` is the legacy administrator role |
| Manage module definitions | Module definitions | A module **definition** and a module **instance** on a page are two layers; a new module needs a definition before it can be mounted |
| Module settings | Module settings | Configuration for a specific mounted instance |
| Site settings | Site settings | Site-level configuration, including the skin (theme) choice |
| Page layout | Page layout management | Add, edit, delete and reorder tabs, plus per-tab access roles |

**Passwords**: since P5.2, an old account is migrated to strong-hash (PBKDF2) credentials on its first successful sign-in;
creating, registering and resetting passwords no longer writes legacy MD5 digests. Legacy MD5 digests are kept only for
existing-data compatibility; for the disposal strategy see `deployment-default-credentials.md`.

## Troubleshooting

Start with three universal checks: **read the diagnostic log** (skipped modules and denied permissions both record their
reason), **confirm which configuration takes effect** (see above), and **confirm the database scripts have been run**.

| Symptom | Likely cause | Action |
| --- | --- | --- |
| A module is not visible on the page | The package is not in `Portal.ModulePackages.Enabled`, or no instance is mounted on the tab | Allow the package first, then mount the instance; skipped modules record their reason in the diagnostic log |
| The module renders but has no styling / shows light blocks in a dark skin | The module CSS bypasses `--portal-*` semantic variables and hard-codes raw colours | See `theme-module-contract.md`; colours must always be taken through semantic variables |
| It always shows "No records yet" and submitting fails | The business tables were not created | Run the corresponding migration script; when the tables are unavailable the module degrades to an empty state instead of throwing |
| Submission succeeds but no work item is generated | A work item needs an approver (a user or a role), and both business modules assign it to `Admins` | Confirm the `Admins` role exists |
| Sign-in fails / credential problems | Default account policy, legacy credential migration state | See `deployment-default-credentials.md` |
| A setting was changed but the runtime does not reflect it | The configuration file edited is not the one that takes effect | See "which configuration takes effect" above |
| Database connection fails | The external `connectionStrings.config` is unconfigured or the path is wrong | Confirm the external configuration directory and the `Portal` connection string |

## Not yet covered (registered honestly)

The following has **no documentation support at present** — consult the source or open an issue; do not assume
documentation exists:

- Runtime screenshots.
- Production deployment considerations (for deployment refer to `deployment-checklist.md` and `operations-runbook.md`; this
  page deliberately does not repeat deployment steps).
- The **approval flow UI** for leave requests and expense reimbursements — this cycle only persists records and generates work
  items; approval actions are out of scope for these modules.
- Item-by-item operating instructions for the existing content modules (announcements, contacts, discussions, documents,
  events, images, links, etc.).
