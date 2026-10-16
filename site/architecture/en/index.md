---
layout: default
lang: en
parent: Evaluate
permalink: /en/architecture/
title: Architecture overview
---
# Architecture overview

## Project positioning

HIA-ASPNETPortal is an ASP.NET Web Forms portal application derived from the original ASP.NET Portal Starter Kit. Its goal is
to keep providing a dynamic portal, modular content, role permissions and online administration in scenarios where legacy
systems, enterprise intranets and Web Forms still have to be maintained.

## Solution structure

- `src/master.sln`: the main solution.
- `src/Portal/Portal.csproj`: the WebForms site project, targeting .NET Framework 4.8.
- `src/Portal.Components/Portal.Components.csproj`: interfaces and shared components.
- `src/Portal.Components.Data/Portal.Components.Data.csproj`: one of the data-access projects.
- `src/Portal.Components.Data1/Portal.Components.Data1.csproj`: one of the data-access projects.

## Web project structure

- `Admin/`: back-office pages for users, roles, module definitions, page layout and site settings.
- `DesktopModules/`: portal modules such as announcements, contacts, discussions, documents, events, HTML, images, links and XML.
- `Components/`: runtime components for portal pages, module control, cached module control and containers.
- `Config/`: Unity configuration, environment configuration and `appSettings`.
- `App_GlobalResources/`: multilingual resources.
- `App_Themes/`: theme styles.
- `Sys/`, `Util/`: helper code for environment information and configuration loading.

## Data and dependencies

- Database initialisation scripts live in `src/Setup/`.
- Since P5.2, strong user password hashes live in `Portal_UserCredentials` and session security versions in
  `Portal_UserSecurityStates`; the legacy `Portal_Users.Password` column is kept only as a compatibility migration sample for
  existing MD5 data.
- Provider-specific scripts live in `src/Setup/Providers/{ProviderId}/`; the first SQLite proof is under `Providers/SQLite/`,
  and providers such as MySQL or PostgreSQL can be added alongside it later without migrating the existing SQL Server scripts.
- Local database files live in `db/MSSQLLocalDB/`.
- Data access uses Entity Framework 6.1.0.
- Dependency injection uses Unity 5.x.
- The web project still uses the classic `packages.config` and the `src/packages/` NuGet directory.
- `src/Portal.DataProviderProof/` is a .NET Framework 4.8 development/test proof project that is **not** part of the main
  solution; it verifies the ADO.NET provider factory and basic SQLite transaction capability and does not take part in normal
  portal deployment.
- `src/Portal.HiaBoundaryProof/` is a .NET Framework 4.8 contract proof project that is **not** part of the main solution; it
  uses fixtures to verify the version, fields and privacy boundaries of HIA peripheral capability descriptions, and adds no HIA
  runtime dependency or transport.

Since P11.2, data-access compatibility is recorded under four labels:

- `SqlServerOnly`: capabilities that can currently be treated as SQL Server only, e.g. `System.Data.SqlClient`, the EF SQL
  Server provider, `[dbo]`, `OBJECT_ID`, `SYSUTCDATETIME()`, `IDENTITY`, `ROWVERSION`.
- `NeedsDialect`: SQL that could be abstracted but must be rewritten per provider, e.g. `SELECT TOP`, lock hints,
  `OUTPUT INSERTED` and table variables.
- `PortableCandidate`: capabilities that can serve as a provider-abstraction entry point or are close to universal, e.g.
  `PortalDatabaseProfile`, `DbProviderFactories`, simple connection checks.
- `ProviderProof`: standalone proof scope, e.g. the current SQLite proof; it does **not** mean the portal's primary business
  database already supports that provider.

A read-only inventory can be produced with `dev/scripts/Get-PortalDataAccessInventory.ps1`. The script scans only Git-tracked
source and never reads real connection strings or configuration outside the repository.

## HIA peripheral collaboration baseline

`Portal.Components/PortalHiaBoundaryContracts.cs` defines the portal-owned `hia.portal.peripheral@0.1.0-draft` contract DTOs
and an offline validator. It currently covers only modules, themes, the settings registry metadata, health, and restricted
diagnostic references, and keeps the boundary clear through explicit versions, allow-listed fields, and path/sensitive-field
checks.

This is an evolvable collaboration baseline, **not** tight coupling to the HIA platform and **not** a general-purpose remote API.
A normal portal neither discovers nor loads an external adapter, and does not require an HIA repository, DLL or service to
exist. If real consumers, transport, identity mapping or write operations are needed later, they should come with a new design
decision, a versioned contract and independent regression runs.

Since P11.4, HIA peripheral integration is managed as layered candidates: user and HIA identities are only mapped by design and
do **not** replace the local `Portal_Users`; employees, organisations and user profiles are the first batch of read-only/import
candidates; module packages, theme packages and the settings registry are the second batch of metadata candidates; diagnostics,
audits and compliance evidence allow **summaries and references only**, never log bodies, physical paths, connection strings or
sensitive fields. The P11.4 samples under `src/Portal.HiaBoundaryProof/Fixtures/Draft/` exist for contract discussion only and
are **not** accepted by the current runtime validator.

The HIA-Documentation-Sys notification mechanism is pull-based: the target project reads notifications on its own initiative.
This project uses `dev/scripts/Get-HiaDocumentationNotifications.ps1` to view the **upstream notification directory** (kept in
this project's separate private repository, not published with the public docs), and will not restore the mechanism that
actively delivered notifications into this project's `dev/notify/`.

A read-only HIA integration-boundary inventory can be produced with `dev/scripts/Get-PortalHiaIntegrationInventory.ps1`. That
script does not connect to a database, load external HIA assemblies, issue network requests, or copy notification bodies.

## Current architectural risks

- Two closely related data-access projects exist, `Portal.Components.Data` and `Portal.Components.Data1`; their
  responsibility boundary needs further confirmation.
- Generated documentation is mixed with source in directories such as `src/Documentation`; a clear generation location and
  cleanup policy should be defined later.
- There is currently no automated test project; a minimal regression check is needed before architectural changes.
- The configuration directory holds both templates and real environment files; rules for managing sensitive configuration
  need to be established.
