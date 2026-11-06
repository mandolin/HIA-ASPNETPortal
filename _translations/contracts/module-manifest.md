---
layout: default
title: Module contract (module.json)
lang: en
parent: 参考
nav_order: 1
---

# Module contract (`module.json`)

> Authoritative source: `DesktopModules/<module>/module.json`; validation in
> `src/Portal/Components/PortalModulePackage.cs`. This page is an extracted snapshot — **when the two conflict, the source
> wins**.

## 1. File location and loading

The file name is fixed as `module.json`, located under the module directory. The loader reads the fixed manifest file as
**UTF-8** and parses it into a JSON object; **a parseable JSON is not by itself enough to establish trust** — after parsing it
must still pass schema, field, path and forbidden-capability validation.

## 2. Fields

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `schemaVersion` | integer | **Yes** | See below |
| `packageId` | string | Yes | Package identifier; conventionally shaped like `HIA.<ModuleName>` (existing content modules use `Legacy.*`) |
| `displayName` | string | Yes | Display name |
| `version` | string | Yes | Expected to be SemVer-shaped |
| `minimumPortalVersion` | string | Yes | Minimum portal version |
| `desktopEntry` | string | Yes | Relative path of the controlled ASCX entry, shaped like `DesktopModules/<module>/<module>.ascx` |
| `resources` | string array | Yes | In-site static resource paths; **validated item by item** — a missing item, a path escape or an extension outside the allow-list **rejects the whole package** |
| `capability` | object | **No** | See "Enterprise capability" below |

### `schemaVersion`

**1** and **2** are supported; future versions are **strictly rejected** (not auto-passed for backward compatibility):

- **1** — no `capability` section;
- **2** — an **optional** `capability` section.

> ⚠️ Common misconception: `schemaVersion: 2` does **not** require a `capability` section. Measured: `MyWorkItems` has
> `schemaVersion: 2` with no `capability` section and is a legal module. The gate `Test-PortalBusinessModuleSmoke.ps1` has been
> calibrated accordingly (2 = business-module compliant; 1 without capability = read-only reference package compliant).

### `resources`

The current static-resource allow-list is **`.css`** (exact extension comparison, no MIME guessing, no extension-less
acceptance). Resource paths must be in-site; new extensions are not auto-passed for backward compatibility.

## 3. Enterprise capability (capability)

The `capability` section is **optional** and declares the module's capability ownership and role:

```json
"capability": {
 "capabilityId": "BasicBusiness.LeaveRequest",
 "role": "Primary"
}
```

- When **not declared**, `capabilityId` / `capabilityRole` keep a stable empty text and the module is considered
 **Unclassified** — a legal state, not an error.
- Once **declared**:
 1. Neither `capabilityId` nor `role` may be empty;
 2. `capabilityId` must resolve in the **authoritative capability registry** (`PortalCapabilityRegistry`) — otherwise it is
 judged "unregistered" and rejected;
 3. `role` must belong to the **closed five-value vocabulary** (`PortalCapabilityRoles`): `Primary` / `Contributor` /
 `Projection` / `Adapter` / `Block`.

### Currently registered capability keys (measured)

| Module | `capabilityId` | `role` |
| --- | --- | --- |
| `BusinessApplicationRequest` | `BasicBusiness.ApplicationRequest` | `Primary` |
| `EmployeeProfileConfirm` | `BasicBusiness.EmployeeProfileConfirm` | `Primary` |
| `EmployeeProfileCorrectionRequest` | `BasicBusiness.EmployeeProfileCorrectionRequest` | `Primary` |
| `EnterpriseCapabilityWorkbench` | `BasicBusiness.Collaboration` | `Primary` |
| `ExpenseReimbursement` | `BasicBusiness.ExpenseReimbursement` | `Primary` |
| `LeaveRequest` | `BasicBusiness.LeaveRequest` | `Primary` |
| `MyWorkItems` | (not declared, Unclassified) | — |
| `ModuleProbe` | (not declared, read-only reference package) | — |

> The full capability-key vocabulary follows `PortalCapabilityRegistry.cs`; the table above is only a measured snapshot of the
> current `module.json` files.

## 4. Explicitly rejected declarations

The current deployment model **allows only controlled ASCX entry points and in-site static resources**. Any of the following
fields appearing in a manifest causes rejection — **a manifest declaration cannot change execution or download capability**:

- `script` / `scripts`
- `externalUrl` / `externalUrls`
- Assembly-related declarations
- Package-URL-related declarations

## 5. Related gates and documents

- New-module readiness check: `dev/scripts/Test-PortalNewModuleReadiness.ps1` (R1 csproj registration / R2 `CodeBehind` /
 R3 theme scope rules / R4 markup and CSS class consistency).
- Business module smoke: `dev/scripts/Test-PortalBusinessModuleSmoke.ps1`.
- Development flow and traps: [module-development-guide.md](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/docs/module-development-guide.md).
