---
layout: default
lang: en
parent: Learn
nav_order: 9
permalink: /en/modules/
title: Module guides
---
# Module guides (index)

This directory holds a dedicated operating page for **every content module** of the portal. For one-line summaries see the [user guide]({{ site.baseurl }}/user-guide/); here each module gets **its own page** covering: purpose and prerequisites, operating steps, field table, required fields and validation, storage and audit, permissions, common questions, and boundaries and non-goals.

## Pick a page by purpose

| Page | Module | In one line | Controls | Required | Storage |
| --- | --- | --- | --- | --- | --- |
| [Announcements]({{ site.baseurl }}/modules/announcements/) | Announcements | In-site notices with an expiry date; hidden once expired | 5 | 3 | `AnnouncementsDB` |
| [Contacts]({{ site.baseurl }}/modules/contacts/) | Contacts | A people directory; the **most lightly validated** module here | 5 | 1 | `ContactsDB` |
| [Discussion]({{ site.baseurl }}/modules/discussion/) | Discussion | Threaded discussion area; **no required field** | 2 | 0 | `DiscussionDB` |
| [Document]({{ site.baseurl }}/modules/document/) | Document | Document entries with an **upload and path security policy** | 5 | 1 | `DocumentDB` |
| [Events]({{ site.baseurl }}/modules/events/) | Events | Activity information; **all four fields required** | 4 | 4 | `EventsDB` |
| [ImageModule]({{ site.baseurl }}/modules/image-module/) | ImageModule | A single image; text settings only, **no upload** | 3 | 0 | module settings (`src`, …) |
| [Links]({{ site.baseurl }}/modules/links/) | Links | Link list; **in use on a test tab only** | 5 | 3 | `LinkDB` |
| [QuickLinks]({{ site.baseurl }}/modules/quick-links/) | QuickLinks | **Shares one data set and one edit page with Links** | 5 (same edit page) | 3 | `LinkDB` (same data) |
| [HtmlModule]({{ site.baseurl }}/modules/html-module/) | HtmlModule | Raw HTML maintained by trusted administrators; **no sanitisation** | 3 | 0 | `HtmlTexts` (`HtmlTextDB`) |
| [XmlModule]({{ site.baseurl }}/modules/xml-module/) | XmlModule | Renders in-site XML; path compliance checks and neutral degradation | 2 | 0 | module settings (`xmlsrc` / `xslsrc`) |
| [Signin]({{ site.baseurl }}/modules/signin/) | Signin | Sign-in form; **the only module writing operational audit**, **no edit page**, **0 instances** | 6 view controls | — | `UsersDB.SignIn` |

## Pages for the newer (directory-style) modules

These modules live at `src/Portal/DesktopModules/<module>/<module>.ascx` and have **no standalone `Edit*.aspx`** — the form and the list sit in the same `ascx`. The field tables on these pages are therefore taken straight from that `ascx` markup, and **the gate points `edit` at the same `ascx`** so that "the field set covers the source controls" and "the required count matches the source" are real assertions rather than skipped ones (leaving it empty would skip both, and then nobody checks the field table).

| Page | Module | One line | Input controls | Writes | Audit event |
| --- | --- | --- | --- | --- | --- |
| [Business Application]({{ site.baseurl }}/modules/business-application-request/) | BusinessApplicationRequest | An **abstract** application skeleton with no concrete business fields | 4 | business record + work item | `BusinessApplicationSubmitted` |
| [Employee Profile Confirmation]({{ site.baseurl }}/modules/employee-profile-confirm/) | EmployeeProfileConfirm | **Read-only confirmation** of your own bound low-sensitivity profile; nothing editable | 0 | no business data | `EmployeeProfileConfirmed` |
| [Employee Profile Correction Request]({{ site.baseurl }}/modules/employee-profile-correction-request/) | EmployeeProfileCorrectionRequest | Raises a **correction request**; does not change the profile directly | 3 | the request (profile change happens later) | `EmployeeProfileCorrectionRequested` |
| [Enterprise Capability Workbench]({{ site.baseurl }}/modules/enterprise-capability-workbench/) | EnterpriseCapabilityWorkbench | Collaboration items: submit / resubmit / comment | 7 | collaboration items | `CollaborationItemSubmitted` / `Resubmitted` / `CommentAdded` |
| [Expense Reimbursement]({{ site.baseurl }}/modules/expense-reimbursement/) | ExpenseReimbursement | Expense claim, document attachable | 3 (+attachment) | `PortalBiz_ExpenseReimbursements` + work item | `BusinessModuleCategory` |
| [Leave Request]({{ site.baseurl }}/modules/leave-request/) | LeaveRequest | Leave request, evidence attachable | 5 (+attachment) | `PortalBiz_LeaveRequests` + work item | `BusinessModuleCategory` |
| [Module Probe]({{ site.baseurl }}/modules/module-probe/) | ModuleProbe | A **read-only verification sample**, not a business entry | 0 | **writes nothing** | **writes no audit** |
| [My Work Items]({{ site.baseurl }}/modules/my-work-items/) | MyWorkItems | Work-item aggregation entry, **no in-place handling** | 0 | nothing; read-only aggregation | `WorkItemsViewed` |

**The single most important difference from the classic modules**: these have **no required validators in markup** (the `RequiredFieldValidator` count is 0 everywhere) — required-ness is **decided in code at submit time**, so **the page never turns red and a wrong value is rejected only after you submit**. The date range in `LeaveRequest` and the amount in `ExpenseReimbursement` are the sharpest cases: markup looks like "anything goes", and only the server says no.

## Three cross-cutting facts to know before reading

1. **Audit surface**: **the classic content modules and the newer business modules are opposites** — of the eleven classic content modules, **only Signin writes operational audit** (`SignInFailed` / `LegacyCredentialUpgraded` / `CredentialCostUpgraded`) and the other ten content edit paths **write none**; whereas the newer business modules (business application / employee-profile confirmation / employee-profile correction request / enterprise-capability workbench / my work items) **all write operational audit**, with events listed on their own pages. **The only one that writes neither business data nor audit is the module probe** (a read-only verification sample with no business action). For the classic content modules, a traceable record of who changed what must be arranged outside the portal.
2. **Validation varies enormously**: Events requires all four fields, Discussion requires none, and Contacts performs **no format validation at all** beyond the name (e-mail and phone are stored exactly as typed). **Do not assume that silence means the value is correct.**
3. **Deployment differs**: Links is in use **on a test tab only** (no production tab yet); Signin has **0 instances in the development database** and therefore **source-level description only, with no runtime evidence**; HtmlModule has 12 instances and XmlModule 1. Deployment readings on each page are a **delivery-time snapshot taken on 2026-10-21** — re-check after adding or migrating tabs.

## Two things that look like defects but are established behaviour

- **HtmlModule performs no sanitisation**: rendering applies **no HTML allow-list san**itisation (it follows the historical "encode before persistence" convention). It may be maintained **only by trusted administrators** and **must not carry ordinary user input**; the real safeguard is the narrow `ContentRawHtmlEdit` permission, **not** `ValidateRequest="false"` (which is post-back compatibility only).
- **Editing QuickLinks also changes Links**: the two **share one link data set** and one edit page (`EditLinks.aspx`). That is by design, not data bleeding across modules.

## Boundaries and non-goals

- This directory is **operating documentation**, not source documentation: it does not change module behaviour and does not replace the [code documentation]({{ site.baseurl }}/code-documentation/) or the [documentation map]({{ site.baseurl }}/documentation-map/).
- The pages **do not rely on runtime screenshots**: every statement is traceable to source markup and verified deployment readings; where screenshot evidence would be needed (for example Signin) the page states the **re-entry condition** explicitly.
- Fields, required fields and validation on each page are guarded by a gate (`Test-PortalModuleDocPages.ps1`): the field set must cover the source control set, the required count must agree with the source, all eight sections must be present, and the **deployment boundary statements** required by the registry must be written out.

<!-- build-stamp: 400C740A7C16 -->
