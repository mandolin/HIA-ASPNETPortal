---
layout: default
title: XmlModule
lang: en
parent: 参考
nav_order: 10
---

# XmlModule

> Module directory: `src/Portal/DesktopModules/`｜View: `XmlModule.ascx`｜Edit page: `EditXml.aspx`
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Renders **XML data deployed inside the site**, optionally transformed through XSL/T. It delegates "what to render" entirely to **files in a trusted deployment directory** and stores no data itself.

Prerequisites:

1. You are signed in and hold **edit permission on this module** (`HasEditPermissions(moduleId)`, re-checked per request). The narrow `ContentRawHtmlEdit` permission of the HTML module is **not** required.
2. The module must be mounted on a tab. Current deployment (measured in the development database): **1 instance** — home page "热门动态" (Trending news) (`ModuleId=6`, `xmlsrc=~/data/sales.xml`, `xslsrc=~/data/sales.xsl`).
3. The XML and XSL files must **already exist inside the application's trusted deployment directory** (for example `~/Data/`). **This module offers no upload and no online editing.**

## Operating steps

**Configure data source and transform**: open the tab → click the edit entry in the module title bar → fill in the **application-internal virtual path of the XML data file** (`XmlDataSrc`), for example `~/data/sales.xml` → optionally fill in the **virtual path of the XSL/T transform** (`XslTransformSrc`), for example `~/data/sales.xsl`; leaving it empty displays the XML as-is → save. On save both values pass through **the same path policy normalisation**, and **either being non-compliant rejects the save** (`EditXml_MessageInvalidResourcePath`, **without echoing the raw input**).

**What happens at runtime**:

- Both paths are individually subjected to **trusted deployment path normalisation** and a **physical file-existence check** before being handed to the `Xml` control (`DocumentSource` / `TransformSource`); **the markup layer accepts no client paths or remote resources**.
- If either path is non-compliant or the file is missing → a **neutral encoded message only** (`Legacy_XmlModule_MessageXmlUnavailable` / `Legacy_XmlModule_MessageXsltUnavailable`); **the original path is not echoed, and no physical path or exception stack is output**.
- **Configuring only XML without XSL works** (the two are validated independently).

## Field table

| Field | Control id | Type | Max length | Required | Validation |
| --- | --- | --- | --- | --- | --- |
| XML data file path | `XmlDataSrc` | Single-line text | 250 | Optional | Normalised by the path policy on save; non-compliant values reject the save |
| XSL/T transform file path | `XslTransformSrc` | Single-line text | 250 | Optional | Normalised by the path policy on save; non-compliant values reject the save |

Control ids, types and lengths are taken from the `EditXml.aspx` markup — **not estimated for this document**.

## Required fields and validation

- **No required fields**: the edit page has **no `RequiredFieldValidator`**; both fields may be saved empty.
- **Save-time validation is path compliance, not format validation**: both values are normalised by the same path policy and **either being non-compliant rejects the save** (`EditXml_MessageInvalidResourcePath`). This **differs** from other modules, which "store whatever is typed".
- Note: **leaving a field empty is allowed** (the module can carry XML only, or be cleared temporarily). "Non-compliant" and "empty" are different things.

## Storage and audit

- **Storage**: **module settings** (`ModulesConfig.GetModuleSettings` / `ModulesConfig.UpdateModuleSetting`), persisted in `PortalCfg_ModuleSettings` under the `xmlsrc` and `xslsrc` keys. **This module has no business table of its own.**
- **Audit**: **this save path writes no operational audit record**; the audit table will not show who changed the XML configuration.

## Permissions

- Editing requires a **positive-integer `Mid`** plus `HasEditPermissions(moduleId)`; the narrow `ContentRawHtmlEdit` permission is **not** required.
- On first load only the current module's existing settings are read, and **only after authorisation succeeds** (the source comment states this explicitly: check first, then read settings).
- Viewing the rendered result requires no edit permission.

## Common questions

**"Invalid resource path" on save** — you did not enter an application-internal virtual path (of the form `~/data/x.xml`), or you pointed at an off-site address or a physical path. **Only virtual paths inside the trusted deployment directory are accepted.**

**A neutral message appears instead of content** — the runtime check failed: the file does not exist, the path is non-compliant, or the XSL is missing. Note that the message **does not tell you the original path** (deliberately), so check the configured path against the actual file yourself.

**Only XML configured, yet a message shows** — the XML file itself is missing or its path is non-compliant; whether an XSL is configured is irrelevant (the two are validated independently).

**I want XML only** — leave `XslTransformSrc` **empty**; you do not need to delete the XSL file.

**I want to upload XML or XSL** — **not supported** (the source states: it stores only the virtual paths of already deployed resources and does not handle file content). Deploy the file inside the application first, then enter the path.

**Can I reference an external site or a remote stylesheet?** — **No**: the markup layer accepts no client paths or remote resources, and the module does not auto-load external XML, remote stylesheets or scripts.

## Boundaries and non-goals

- No upload, no online editing, no external URLs or arbitrary physical paths, no auto-loading of external XML / remote stylesheets / scripts, no content editing (paths only), no operation audit.
- **Writes no operational audit** (see above).
- At runtime it **does not echo the original path and does not output physical paths or exception stacks** — troubleshoot from the settings side rather than expecting detail from the page.
- Covers the **complete edit-side fields and validation**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- Instance count (1) and its `xmlsrc` / `xslsrc` values are a **delivery-time snapshot**; **re-check this section after adding or migrating tabs**.
- Against [ImageModule]({{ site.baseurl }}/modules/image-module/): both store configuration in **module settings rather than a table of their own**; the difference is that here the settings are **virtual paths of trusted deployment resources** with **save-time path compliance checks and runtime degradation**, whereas the image module's settings are plain text addresses and sizes with **no validation at all**.