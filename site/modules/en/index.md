---
layout: default
lang: en
parent: Reference
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

## Three cross-cutting facts to know before reading

1. **Audit surface**: of these eleven modules, **only Signin writes operational audit** (`SignInFailed` / `LegacyCredentialUpgraded` / `CredentialCostUpgraded`); the other ten content edit paths **write none**. If you need a traceable record of who changed what, it must be arranged outside the portal.
2. **Validation varies enormously**: Events requires all four fields, Discussion requires none, and Contacts performs **no format validation at all** beyond the name (e-mail and phone are stored exactly as typed). **Do not assume that silence means the value is correct.**
3. **Deployment differs**: Links is in use **on a test tab only** (no production tab yet); Signin has **0 instances in the development database** and therefore **source-level description only, with no runtime evidence**; HtmlModule has 12 instances and XmlModule 1. Deployment readings on each page are a **delivery-time snapshot** — re-check after adding or migrating tabs.

## Two things that look like defects but are established behaviour

- **HtmlModule performs no sanitisation**: rendering applies **no HTML allow-list san**itisation (it follows the historical "encode before persistence" convention). It may be maintained **only by trusted administrators** and **must not carry ordinary user input**; the real safeguard is the narrow `ContentRawHtmlEdit` permission, **not** `ValidateRequest="false"` (which is post-back compatibility only).
- **Editing QuickLinks also changes Links**: the two **share one link data set** and one edit page (`EditLinks.aspx`). That is by design, not data bleeding across modules.

## Boundaries and non-goals

- This directory is **operating documentation**, not source documentation: it does not change module behaviour and does not replace the [code documentation]({{ site.baseurl }}/code-documentation/) or the [documentation map]({{ site.baseurl }}/documentation-map/).
- The pages **do not rely on runtime screenshots**: every statement is traceable to source markup and verified deployment readings; where screenshot evidence would be needed (for example Signin) the page states the **re-entry condition** explicitly.
- Fields, required fields and validation on each page are guarded by a gate (`Test-PortalModuleDocPages.ps1`): the field set must cover the source control set, the required count must agree with the source, all eight sections must be present, and the **deployment boundary statements** required by the registry must be written out.

<!-- build-stamp: 96EF6AD172F9 -->
