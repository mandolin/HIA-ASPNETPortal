---
layout: default
lang: en
parent: Learn
nav_order: 1
permalink: /en/first-use-portal/
title: First use of the portal
---
# First use of the portal

The first time you open this portal you will probably trip over three things: why the page looks empty, why the menu is so short, and where to click after signing in. This page takes them in order.

## Open the home page

The home page has three columns: the sign-in box on the left, announcements in the middle, help and notes on the right.

![The home page before signing in: account sign-in on the left, portal announcements in the middle, help and notes on the right; the left menu has only the Home and Help tabs]({{ site.baseurl }}/assets/img/first-use/home-anonymous.en.png)

Before signing in, the left side has just two tabs, "Home" and "Help". The business entries show up only after you sign in — that is by design, not a broken page.

## Sign in

In the box on the left, fill in your email address, user name or employee number and your password, then click Sign in. Tick "Remember me" if you want the session kept.

![The account sign-in card on the left of the home page: email, user name or employee number, password, remember me, and the sign-in button]({{ site.baseurl }}/assets/img/first-use/signin-card.en.png)

The identifier may be a user name, a legacy name, an active employee number, or an email address.

There is no registration link on the page. Whether self-registration is open is a deployment decision, and it is off by default — so not seeing the link is normal.

The password is encrypted with a one-time public key before it is sent, and the server decrypts it only for that one request, so it never travels in plain text.

When the password is wrong the page only says the sign-in failed; it does not tell you whether the account does not exist or the password is wrong — deliberately, so the form cannot be used to probe for accounts.

After signing in, the top of the page says "Welcome, your name", and the left menu grows from two tabs to six.

## What the left side shows after signing in

![The home page after signing in: the welcome line at the top, and the left menu expanded to Home, Help, My to-do items, Leave and expense, Business requests and My profile]({{ site.baseurl }}/assets/img/first-use/home-signed-in.en.png)

In order:

- Home — back to the sign-in box and the announcements
- Help — usage notes; the block on the right of the home page is the same page
- My to-do items — the things assigned to you that are not finished yet
- Leave and expense — leave requests and expense claims
- Business requests — submit and track business requests
- My profile — confirm and correct your employee profile

Administrators get two more: platform administration (site settings, tabs, modules, roles, users) and the enterprise workbench (submitting, resubmitting and commenting on collaboration items).

## View your to-do items

Click "My to-do items" and the page lists, as it loads, the unfinished items assigned to you. You do not have to press anything.

![The My to-do items page: the All and Overdue only filters at the top, and an empty list below saying there are no to-do items]({{ site.baseurl }}/assets/img/first-use/my-work-items.en.png)

There are two filters, "All" and "Overdue only". Click "Overdue only" to see just the late ones, and "All" to go back to the full list.

When you have nothing outstanding the list is empty and it says so.

Every entry names the module it came from.

You can look here but you cannot deal with anything here. This module does not approve anything, does not change data, and does not write back to the source module. Handling happens in the module that produced the to-do.

## Handle it in the source module

Find the to-do in the list, note the source it names, then click the matching tab in the left navigation — say "Leave and expense" — and finish the job in that tab.

Once it is handled, the to-do disappears from the list, because only unfinished items are listed.

## When something is not showing

- Only "Home" and "Help" on the left — you are not signed in. The business entries show for signed-in users only.
- Still no business entries after signing in — the sign-in did not succeed (check whether your name appears at the top), or the account has not been enabled; ask an administrator.
- "My to-do items" is empty — nothing unfinished is assigned to you right now. That is normal.
- A to-do you expected is gone — it was completed, it was assigned to someone else, or its source module is outside the aggregate. Check the source module.
- No action button in "My to-do items" — this module deliberately does not offer in-place handling. Handle it in the source module.
- The announcements area on the home page is empty — each announcement has to be marked visible to guests individually to show up here. Ask the site administrator.

## What this page does not cover

- What each module lets you fill in and what is required — the module pages under Learn.
- Publishing announcements and maintaining tabs and modules — that is site operations; there is a separate handbook.
- The trail of an approval — **there is no such page**. The approval flow is recorded inside the system and is not visible in the interface; what the "Business requests" and "Leave and expense" tabs show is the request itself and its current state.
- How the site is configured and how modules are extended — see Reference.

<!-- build-stamp: 3A45A7AE7A2B -->
