---
layout: default
lang: en
parent: Evaluate
nav_order: 1
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
- Strong user password hashes live in `Portal_UserCredentials` and session security versions in
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
 runtime dependency or transport.

Data-access compatibility is recorded under four labels:

- `SqlServerOnly`: capabilities that can currently be treated as SQL Server only, e.g. `System.Data.SqlClient`, the EF SQL
 Server provider, `[dbo]`, `OBJECT_ID`, `SYSUTCDATETIME`, `IDENTITY`, `ROWVERSION`.
- `NeedsDialect`: SQL that could be abstracted but must be rewritten per provider, e.g. `SELECT TOP`, lock hints,
 `OUTPUT INSERTED` and table variables.
- `PortableCandidate`: capabilities that can serve as a provider-abstraction entry point or are close to universal, e.g.
 `PortalDatabaseProfile`, `DbProviderFactories`, simple connection checks.
- `ProviderProof`: standalone proof scope, e.g. the current SQLite proof; it does **not** mean the portal's primary business
 database already supports that provider.

A read-only inventory can be produced with `dev/scripts/Get-PortalDataAccessInventory.ps1`. The script scans only Git-tracked
source and never reads real connection strings or configuration outside the repository.


## Current architectural risks

- Two closely related data-access projects exist, `Portal.Components.Data` and `Portal.Components.Data1`; their
 responsibility boundary needs further confirmation.
- Generated documentation is mixed with source in directories such as `src/Documentation`; a clear generation location and
 cleanup policy should be defined later.
- There is currently no automated test project; a minimal regression check is needed before architectural changes.
- The configuration directory holds both templates and real environment files; rules for managing sensitive configuration
 need to be established.

<!-- build-stamp: ECDDA62EBA57 -->
