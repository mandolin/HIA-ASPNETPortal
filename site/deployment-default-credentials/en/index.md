---
layout: default
lang: en
parent: Operate (section)
permalink: /en/deployment-default-credentials/
title: Default credentials and legacy password governance
---
# Default credentials and legacy password governance

This note defines how default accounts, sample passwords in initialisation scripts and the legacy MD5 compatibility path
should be handled **before a release**. It records no real passwords and does not ask anyone to try signing in with a default
password automatically.

## Current boundaries

1. `src/Setup/Portal_LoadData.sql` still keeps the old Starter Kit local sample `admin` user and the old
   `Portal_Users.Password` digest.
2. `src/Setup/Portal_UserCredentials.sql` only creates the strong-hash credential table and the security-state table; it
   writes no new strong-hash password seed.
3. Since P5.2, creating a user, registering and an administrator resetting a password all write to `Portal_UserCredentials`;
   the legacy MD5 is allowed only as a first-sign-in compatibility upgrade path for historical accounts.
4. Production, shared test and customer demo environments must not keep the default admin password, and must not write shared
   development passwords into the repository, internal directories, logs or screenshots.

## Pre-release requirements

1. Run `dev/scripts/Test-PortalDefaultCredentialRisk.ps1` and record the output summary.
2. If the database is initialised with `Portal_LoadData.sql`, the default admin must have its password replaced, its account
   disabled, or be explicitly deleted **before open access**.
3. The result of handling the default admin should be recorded in the release record; the record must state only
   "replaced / disabled / deleted / migrated" — **never a plaintext password**.
4. The legacy `Portal_Users.Password` exists only as a compatibility field; newly created or reset passwords must not be
   written back into the old MD5 digest.
5. First-sign-in forced password change, default-account detection, password history and lockout policy belong to a separate
   security design and are not improvised in this note.

## Recommended commands

```powershell
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoLogo -NoProfile -File dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Dev
```

Before a test or production release, use the stricter scope and keep JSON evidence:

```powershell
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoLogo -NoProfile -File dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Prod -OutputJson temp/compliance/default-credential-risk.json
```
