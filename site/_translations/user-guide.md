---
layout: default
title: User guide
lang: en
parent: 快速上手
nav_order: 1
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
2. Executing the database scripts under `src/Setup/`, which must at least include the base scripts, incremental
   scripts and `Portal_UserCredentials.sql`.
3. Copying the template from `src/Portal/Config/Templates/connectionStrings.config` to the external configuration directory
   and configuring the `Portal` connection string.
4. Building and running `src/master.sln` with Visual Studio.
5. Visiting the started site.

The default external configuration directory is `{current user profile}\Web\HIA-ASPNETPortal\{env}\`. Local development usually
uses the `dev` environment, i.e. `...\HIA-ASPNETPortal\dev\connectionStrings.config`.

## Initial account

Historical notes mention that `admin/admin` can be used to sign in. That information is only for local verification on older
versions; **any** shared, test or production environment must change the default account and password policy. An
old account is migrated to a strong-hash credential on its first successful sign-in; creating, registering and resetting
passwords no longer writes legacy MD5 digests.

## Common modules

The following modules are itemised from the source tree (`src/Portal/DesktopModules/`); deployment locations are
measured values from the development database, and tab names are data values kept verbatim.

### Announcements

- Purpose: publish in-site announcements with an expiry date; expired items are no longer displayed.
- Display: title + description + expiry time; each row carries an edit entry (visible only with edit permission).
- Edit page `DesktopModules/EditAnnouncements.aspx`:
  - `TitleField` title (**required**)
  - `MoreLinkField` details link (optional)
  - `MobileMoreField` mobile link (optional)
  - `DescriptionField` description (**required**)
  - `ExpireField` expiry date (**required**, with a date-format comparison validator `VerifyExpireDate`)
- New entry: "Add New Announcement" in the module title bar.
- Current deployment: TabId=1「新闻与特写」、TabId=2「人事福利」、TabId=3「竞赛：TradeCraft / 监视 / 保护」。

### Contacts

- Purpose: maintain a contact directory (name, role, e-mail, two contact numbers).
- Display: name / role / e-mail (clickable to compose) / contact numbers; each row carries an edit entry.
- Edit page `DesktopModules/EditContacts.aspx`:
  - `NameField` name (**required**)
  - `RoleField` role
  - `EmailField` e-mail
  - `Contact1Field` contact number 1
  - `Contact2Field` contact number 2
- **Note: only the name is required** — role, e-mail and contact numbers have neither required nor format validation and
  may be left empty, so verify entries yourself.
- New entry: "Add New Contact". Current deployment: TabId=2「员工联系信息」。

### Discussion

- Purpose: topic-based discussion area supporting reply hierarchies under a topic.
- Display: topic list (child replies can be expanded/collapsed, with the expand state showing the child-reply count);
  each row shows author and post time, and the title links to the detail page (new window).
- Detail page `DesktopModules/DiscussDetails.aspx?ItemID=<id>&mid=<module id>`: shows title / author / date / body;
  provides a reply form (`TitleField` max 100 characters, `BodyField` multi-line, `ReplyBtn` submit) and paging between
  the previous / next item.
- New entry: "Add New Thread" in the module title bar (opens the detail page in a new window).
- Current deployment: TabId=4「TradeCraft 技巧与装备」「实战手册」。

### Document

- Purpose: document entries (upload a file or point at an existing path) for download or viewing in a new window.
- Display: document link (opens in a new window) + edit entry.
- Edit page `DesktopModules/EditDocs.aspx`:
  - `NameField` document name (**required**)
  - `CategoryField` category
  - `PathField` path
  - `Upload` whether to upload (checkbox)
  - `storeInDatabase` whether to store in the database (checkbox)
- Viewing goes through `DesktopModules/ViewDocument.aspx`. New entry: "Add New Document".
- Current deployment: TabId=2「新员工文档」、TabId=4「好书推荐」。

### Events

- Purpose: publish activities (with time and place); expired items are no longer displayed.
- Display: title + description + time and place.
- Edit page `DesktopModules/EditEvents.aspx`:
  - `TitleField` title (**required**)
  - `DescriptionField` description (**required**)
  - `WhereWhenField` time and place (**required**)
  - `ExpireField` expiry date (**required**, with a date-format comparison validator `VerifyExpireDate`)
- New entry: "Add New Event". Current deployment: TabId=1「近期活动」。

### ImageModule

- Purpose: display a single image on a tab.
- Edit page `DesktopModules/EditImage.aspx`: `Src` image path, `Width` width, `Height` height. **None are required.**
- New / edit entry: "Edit". Current deployment: TabId=3「夜视镜」。

### Links

- Purpose: maintain a list of links (site navigation or resource entries).
- Display: link title (clickable) + edit entry.
- Edit page `DesktopModules/EditLinks.aspx`:
  - `TitleField` title (**required**)
  - `UrlField` URL (**required**)
  - `MobileUrlField` mobile URL (optional)
  - `DescriptionField` description (optional)
  - `ViewOrderField` display order (**required**, with a numeric comparison validator `VerifyViewOrder`)
- New entry: "Add Link". Current deployment: **TabId=1004 test tab「测试链接模块」** — i.e. currently deployed on the test
  tab only and not yet used on production tabs.

### HtmlModule

- **Purpose**: render a fragment of **raw HTML** maintained by a trusted administrator on a tab (welcome text,
  explanatory blocks, etc.).
- **Display**: `DesktopModules/HtmlModule.ascx` — reads the stored content by `ModuleId`, `HtmlDecode`s it and emits it
  verbatim through a `LiteralControl`; **a missing record or empty content emits nothing at all** (the ordinary page does
  not disclose configuration state).
- **Edit page** `DesktopModules/EditHtml.aspx` (markup-level `ValidateRequest="false"`):
  - `DesktopText` desktop raw HTML (optional)
  - `MobileSummary` mobile summary (optional)
  - `MobileDetails` mobile details (optional)
- **Required fields**: this edit page has **no `RequiredFieldValidator` at all** — all three fields may be saved empty.
- **Storage**: `HtmlTexts` located uniquely by `ModuleId` (`UpdateHtmlText`): the first save creates the record, later
  saves replace only the three content fields; values are HTML-encoded before persistence and decoded on display (the
  historical compatibility contract).
- **Permissions**: editing requires **both** module-edit permission and the narrower "raw HTML" permission
  (`PortalPermissionKeys.ContentRawHtmlEdit`); `Mid` comes from the query string or the unvalidated postback collection
  and must be a positive integer, otherwise the request is redirected to the edit-denied page.
- **Audit**: this save path **does not write an operation audit** (neither do the other legacy content edit pages). Audit
  coverage concentrates on business requests and key operations — see "Audit and retention".
- **Security boundary (stated honestly)**: this is an **explicit exception** to the "HTML-encode at display time" rule —
  rendering performs **no HTML allow-list sanitisation**, so content must be maintained by trusted administrators and
  **must not carry general-user input**; `ValidateRequest="false"` only satisfies legacy Web Forms postback compatibility
  and **is not a security boundary**.
- **Current deployment** (measured in the development database): **12 instances** — 2 on the home tab, plus employee
  information, product information, discussion, 7 on the about tab, and the `G1P2-Test-A` test tab.

### QuickLinks

- **Purpose**: present common entries as a compact link group; **shares the same link data and the same edit page** as
  Links.
- **Display**: `DesktopModules/QuickLinks.ascx` — rebinds `ILinksDb.GetLinks(ModuleId)` on every request (ViewState
  disabled):
  - **Editors** (members of the module's `AuthorizedEditRoles`) additionally see the "add link" entry and a per-row "edit"
    entry (both pointing at `EditLinks.aspx`);
  - **Viewers** only see links that **pass the navigation policy** — legacy addresses that fail the policy are suppressed
    as **encoded text** (no longer links) and the raw address is not echoed.
- **Edit page**: **shared** with Links — `DesktopModules/EditLinks.aspx`:
  - `TitleField` title (**required**)
  - `UrlField` URL (**required**)
  - `MobileUrlField` mobile URL (optional)
  - `DescriptionField` description (optional)
  - `ViewOrderField` display order (**required**, with a numeric comparison validator `VerifyViewOrder`)
- **Permissions**: module-edit permission (**does not require** the narrower "raw HTML" permission that the HTML module
  needs) plus **link ownership validation** (a link of another module cannot be edited or deleted by `ItemID`).
- **Audit**: consistent with the other legacy content edit pages, this save / delete path **does not write an operation
  audit**.
- **Current deployment** (measured in the development database): **3 instances** — "快速链接" on the home tab,
  "值得关注的竞争者" on the product-information tab, and "快速链接" on the about tab.

### XmlModule

- **Purpose**: render XML data **deployed inside the site**, optionally transformed with XSL/T before display (current
  sample: "热门动态" on the home tab).
- **Data source**: both the XML and the XSL are **files inside the trusted deployment directory** (samples
  `~/Data/sales.xml` and `~/Data/sales.xsl`), with their paths stored as the module settings `xmlsrc` / `xslsrc` in
  `PortalCfg_ModuleSettings` — **this module has no business data table of its own**.
- **Display**: `DesktopModules/XmlModule.ascx` — each path must pass
  `TryNormalizeTrustedDeploymentResourcePath` and a physical file-existence check before it is handed to the `Xml`
  control (`DocumentSource` / `TransformSource`):
  - if either path fails the policy or the file does not exist, only a **neutral, encoded notice** is shown
    (`Legacy_XmlModule_MessageXmlUnavailable` / `Legacy_XmlModule_MessageXsltUnavailable`) — **the raw path is not echoed,
    and no physical path or exception stack is output**;
  - XML-only configuration (no XSL) also displays normally (the two are validated independently).
- **Edit page** `DesktopModules/EditXml.aspx`:
  - `XmlDataSrc` in-application virtual path of the XML data file (optional, max 250 characters)
  - `XslTransformSrc` in-application virtual path of the XSL/T transform file (optional, max 250 characters)
- **Required fields**: this edit page has **no `RequiredFieldValidator` at all** — both fields may be saved empty.
- **Save validation**: both values are normalised through the same path policy; **if either fails, the save is rejected**
  with the notice `EditXml_MessageInvalidResourcePath` (which does not include the raw input).
- **Permissions**: positive-integer `Mid` + `HasEditPermissions(moduleId)` (**does not require** the narrower
  `ContentRawHtmlEdit` permission that the HTML module needs).
- **What it does not do (stated honestly)**: no upload, online editing, external URL or arbitrary physical-path
  capability; external XML, remote stylesheets and scripts are never auto-loaded.
- **Audit**: this save path **does not write an operation audit**.
- **Current deployment** (measured in the development database): **1 instance** — "热门动态" on the home tab
  (`ModuleId=6`: `xmlsrc=~/data/sales.xml`, `xslsrc=~/data/sales.xsl`).

### Signin

- **Purpose**: provide a sign-in form on a tab (login identifier + password + remember me).
- **Display**: `DesktopModules/Signin.ascx` — `EmailOrName` (login identifier), `password` (password),
  `RememberCheckbox` (remember me), `SigninBtn` (sign in), `RegisterLink` (self-registration entry pointing at
  `~/Admin/Register.aspx`, **visible only when `PortalRegistrationOptions.AllowSelfRegistration` is explicitly enabled —
  off by default**).
- **Submission protection**: before submit, the password is written into the hidden field `EncryptedPassword` using a
  **one-time RSA public key** (provided by `~/Security/LoginPasswordKey.ashx`); the server decrypts it and keeps the plain
  value **only within the current request**. Whether encrypted submission is mandatory is decided by
  `PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired()`, and a plain fallback is accepted only when it is not
  required.
- **Validation and ticket**: password verification is delegated to `IUsersDb.SignIn` (strong hashing + legacy MD5
  compatibility upgrade + multi-identifier resolution: login name / legacy name / active employee code / e-mail); on
  success `PortalAuthenticationService.SignIn` issues a ticket **carrying the security version** (the role cookie is
  built on demand during the authentication-request phase); the **failure message is generic** and discloses neither
  whether the user exists, nor review status, nor digest-comparison details.
- **Audit (unlike the content modules above, stated honestly)**: **a failed sign-in writes an operation audit**
  (`SignInFailed`, `Outcome=Failure`, carrying only the login identifier and **never the password, salt or hash
  material**); a legacy-credential upgrade and a password iteration-count upgrade each write one audit record as well.
- **Edit page**: **no edit page** — this module has no settings page such as `EditSignin`; sign-in behaviour is decided by
  runtime settings and the authentication service and **is not configured per module instance**.
- **Current deployment and evidence boundary (registered honestly)**: **0 instances** in the development database (no
  `Signin.ascx` row in `PortalCfg_Modules`), so **this module has no runtime evidence** — everything above is
  **source-level description**; re-entry condition: after the module instance is mounted in the target environment,
  runtime evidence can be collected and this section's measured readings filled in.

## Administration entry point

Back-office pages live under `src/Portal/Admin/`, covering users, roles, module definitions, module settings, site settings
and page layout management.

## Business modules: leave requests and expense reimbursements

These two modules (`HIA.LeaveRequest`, `HIA.ExpenseReimbursement`) are employee self-service (ESS) business modules delivered
by `C-anp-P20`. They are deployed as module packages and are **not enabled by default with the core profile**.

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

1. **Prepare the database**: run the scripts under `src/Setup/` (base scripts, incremental scripts,
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

**Passwords**: An old account is migrated to strong-hash (PBKDF2) credentials on its first successful sign-in;
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

The following currently has **no documentation support** — consult the source or open an issue; do not assume
documentation exists:

- ~~Runtime screenshots~~ — **delivered**: `W-anp-P151` established the **runtime screenshot baseline** (reproducible
  capture at a fixed 1440×1000 viewport, unified naming, artifacts into the internal evidence package, manifest kept in
  the repository); see `documentation-artifacts-guide.md` "Runtime screenshot baseline".
- Production deployment considerations (for deployment refer to `deployment-checklist.md` and `operations-runbook.md`;
  this page deliberately does not repeat deployment steps).
- ~~Item-by-item operating instructions for the approval-flow UI of leave requests and expense reimbursements~~ —
  **delivered**: the approval-flow boundary is declared in the public documentation (see "Approval flow boundary
  (required reading)" above: these modules only persist data and generate to-dos; approval actions are not part of them,
  and the boundary is machine-guarded by a gate, `W-anp-P153`).
- ~~Item-by-item operating instructions for the existing content modules (announcements, contacts, discussions, documents,
  events, images, links, etc.)~~ — **delivered**: see the "Common modules" subsections above (`W-anp-P152`).
- ~~Item-by-item operating instructions for the four modules `HTML`, `QuickLinks`, `XML` and `Signin`~~ — **delivered**:
  see the "Common modules" subsections above (`W-anp-P171` – `W-anp-P174`; note that `Signin` has **0 instances** in the
  development database, so it is documented at source level only and **has no runtime evidence** — see that section for
  the re-entry condition).
