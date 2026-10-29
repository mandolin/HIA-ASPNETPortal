---
layout: default
lang: en
parent: Learn
nav_order: 18
permalink: /en/modules/signin/
title: Signin
---
# Signin

> Module directory: `src/Portal/DesktopModules/`｜View: `Signin.ascx`｜**No edit page**
> This is the **detailed operating guide**; for a one-line summary see the [user guide]({{ site.baseurl }}/user-guide/).

## Purpose and prerequisites

Provides a **sign-in form** on a tab: identifier, password and "remember me". It is the **only content path that writes operational audit** and it has **no edit page** — sign-in behaviour is determined by runtime settings and the authentication service, **not configured on a module instance**.

Prerequisites:

1. **For users**: open the tab hosting the module and enter the identifier and password; no permission is needed to read this page.
2. **For maintainers**: there is **no edit page** (no `EditSignin` or similar), so there is **no procedure for "configuring this module"**. What can be configured are runtime settings (for example whether self-registration is allowed) and the authentication service itself.
3. **Evidence boundary (must be known first)**: this module has **0 instances in the development database** (no `Signin.ascx` record in `PortalCfg_Modules`), so there is **no runtime evidence** for it; everything on this page is **source-level description**. Re-entry condition: once an instance is mounted in the target environment, collect runtime evidence and add the measured readings here.

## Operating steps

**Sign in**: open the tab → enter the **identifier** (`EmailOrName`) and **password** (`password`) → tick **remember me** (`RememberCheckbox`) if the session should persist → click **sign in** (`SigninBtn`).

**Register a new account (normally unavailable)**: the registration entry (**`RegisterLink`**, pointing at `~/Admin/Register.aspx`) is visible **only when `PortalRegistrationOptions.AllowSelfRegistration` is explicitly enabled** — **disabled by default**. **Not seeing a registration link is the normal state**, not a broken page.

**How the password is protected on submit**:

1. Before submitting, the page encrypts the password with a **one-time RSA public key** into the hidden field `EncryptedPassword` (the key is served by `~/Security/LoginPasswordKey.ashx`).
2. The server decrypts it and the password travels **only within the current request**.
3. Whether encrypted submission is **mandatory** is decided by `PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired`; plain-text fallback is accepted only when it is not required.

**Server-side validation flow**:

- Password verification is delegated to `IUsersDb.SignIn`: strong hash + **legacy MD5 compatibility upgrade** + **multi-identifier resolution** (login name / legacy name / active employee number / e-mail).
- On success → `PortalAuthenticationService.SignIn` issues a ticket **carrying a security version** (the roles cookie is created on demand during the authentication request).
- On failure → a **generic** failure message.

## Field table

This module has **no edit page**, so there is no "edit-page control" field table. The table below lists the **view's input controls** (source markup):

| Control | Control id | Type | Notes |
| --- | --- | --- | --- |
| Identifier | `EmailOrName` | Text input | Accepts login name / legacy name / active employee number / e-mail (resolved by `IUsersDb.SignIn`) |
| Password | `password` | Password input | Encrypted with the one-time RSA public key into `EncryptedPassword` before submit |
| Remember me | `RememberCheckbox` | Check box | Keeps the session signed in |
| Sign-in button | `SigninBtn` | Submit button | Submits the form |
| Registration entry | `RegisterLink` | Link | Points at `~/Admin/Register.aspx`; **hidden by default** (self-registration must be enabled explicitly) |
| Encrypted password | `EncryptedPassword` | Hidden field | Carries the encrypted password; the server decrypts it for the current request only |

## Required fields and validation

- With **no edit page**, there are **no edit-side `RequiredFieldValidator` controls** — the table above is **not** an "edit-page control set"; that is the key difference from the other module pages.
- Required-ness and validation happen in the **authentication pipeline**: whether an identifier and password are present is handled by the form and `IUsersDb.SignIn`, and **failure messages are generic** — they **do not reveal whether the user exists, the approval state, or digest comparison details** (deliberate, to prevent account enumeration).
- The password is **never stored in the clear**: the data layer owns strong-hash verification and the legacy MD5 upgrade, and **the page layer produces no digest** (stated in the source comments).
## Storage and audit

- **Storage**: password verification goes through `UsersDB.SignIn` (`IUsersDb.SignIn`); the ticket is issued by `PortalAuthenticationService.SignIn`. **The sign-in module itself stores no business data.**
- **Audit (the only content path in this repository that writes operational audit — must be known)**:
 - **sign-in failure** writes one record: `SignInFailed`, `Outcome=Failure`, **carrying only the identifier**, **never the password, salt or hash material**;
 - **legacy credential upgrade** writes one record: `LegacyCredentialUpgraded`;
 - **password iteration-cost upgrade** writes one record: `CredentialCostUpgraded`.
- By contrast, announcements, contacts, discussion, document, events, images, links, quick links, HTML text and XML/XSL — **all ten content edit paths write no operational audit**.

## Permissions

- **Using the sign-in form requires no permission** (it is the entry point for obtaining permissions).
- There is **no permission concept for "editing the sign-in module"**: no edit page; behaviour is determined by runtime settings and the authentication service.
- Whether self-registration is visible is controlled by `PortalRegistrationOptions.AllowSelfRegistration` (disabled by default) — a **runtime setting**, not a permission grant.

## Common questions

**No registration link** — normal: `RegisterLink` is visible only when **self-registration is explicitly enabled**; it is disabled by default.

**The failure message is very vague** — deliberate: it **does not reveal whether the user exists, the approval state, or digest details**, to prevent account enumeration. Recheck the identifier and password rather than expecting the message to say which one was wrong.

**I have to sign in again on another browser or device** — without "remember me" the session ends with the ticket lifetime; with it, the remember-me validity applies.

**Can I sign in with my employee number?** — Yes: identifier resolution accepts login name / legacy name / active employee number / e-mail.

**A legacy account is slow the first time / reports an upgrade** — legacy MD5 credentials are **upgraded to the strong hash** once verification succeeds (`LegacyCredentialUpgraded`); this is one-off and leaves an audit record.

**I want to change the look or wording of the sign-in box** — there is **no edit page**, so no per-instance configuration; such changes belong to runtime settings and front-end resources.

**Why are there no screenshots on this page?** — because there are **0 instances in the development database** and therefore **no runtime evidence**; this page is source-level description (see the next section).

## Boundaries and non-goals

- **No edit page** and **no runtime evidence** (0 instances in the development database); **everything here is source-level description**. Once an instance is mounted in the target environment, collect runtime evidence and add the measured readings.
- No CAPTCHA, no failure-lockout counter, no multi-factor authentication, no third-party identity providers, no self-service password recovery (these are **honestly registered as not implemented**, not "missing from this page").
- **Stores no business data**; it only reads and writes credentials and tickets within the authentication pipeline.
- Covers **view controls, submission protection, validation flow, audit and permissions**; **does not rely on runtime screenshots** — every statement is traceable to source markup and verified deployment readings.
- **Instance count (0) is a delivery-time snapshot**; re-check this section after adding or migrating tabs, and collect runtime evidence at that point.
- Against the other ten modules, this one is the **only** module that **writes operational audit**, the **only** one with **no edit page** and the **only** one with **0 instances** — three facts that the other module pages refer back to and that are essential to understanding the audit surface of this repository.

<!-- build-stamp: 834A2CB3C36E -->
