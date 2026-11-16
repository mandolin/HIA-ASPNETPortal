# 模块契约（`module.json`）

> 权威源：`DesktopModules/<模块>/module.json`；校验实现 `src/Portal/Components/PortalModulePackage.cs`。本页是提取快照，**冲突时以源码为准**。

## 一、文件位置与读取

固定文件名 `module.json`，位于模块目录下。加载器以 **UTF-8** 读取固定 manifest 文件并解析为 JSON 对象；**JSON 可解析本身不足以建立信任** —— 解析后仍必须通过 schema、字段、路径与禁止能力校验。

## 二、字段

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `schemaVersion` | 整数 | **是** | 见下节 |
| `packageId` | 字符串 | 是 | 包标识；约定形如 `HIA.<ModuleName>`（既有内容模块为 `Legacy.*`） |
| `displayName` | 字符串 | 是 | 显示名 |
| `version` | 字符串 | 是 | 期望为 SemVer 形态 |
| `minimumPortalVersion` | 字符串 | 是 | 最低门户版本 |
| `desktopEntry` | 字符串 | 是 | 受控 ASCX 入口的相对路径，形如 `DesktopModules/<模块>/<模块>.ascx` |
| `resources` | 字符串数组 | 是 | 站内静态资源路径；**逐项校验**，缺项、路径转义或扩展名不在允许表内会**拒绝整个包** |
| `capability` | 对象 | **否** | 见下节「企业能力」 |
| `moduleKind` | 字符串 | 否 | `Core`（缺省，兼容既有包）/ `Independent`（须显式列入部署 Profile 允许集）；其它值整包拒绝 |
| `hostFramework` | 字符串 | 否 | 宿主框架；封闭词表仅 `AspNetWebForms`（当前唯一已实现宿主）；出现即校验、未知值拒绝 |
| `entryKind` | 字符串 | 否 | 入口形态；封闭词表仅 `Ascx`（当前唯一已实现入口）；出现即校验、未知值拒绝 |
| `dependencies` | 字符串数组 | 否 | 依赖声明；逐项非空、长度受限、数组项数受限；**只做形态契约层校验，不解析依赖语义、不验证依赖对象存在性** |

> **形态字段的校验边界**：后三个字段（`hostFramework` / `entryKind` / `dependencies`）由**独立只读校验器** `PortalModuleFormContract.cs` 校验，**不接入装载链、不改变既有包解析结果**。字段缺失＝未声明（兼容既有包）；字段出现但值非法＝fail-closed。未知字段的通用拒绝**尚未实现**，作为开放点登记（见下节）。

### `schemaVersion`

支持 **1** 与 **2**，未来版本**严格拒绝**（不是向后兼容自动放行）：

- **1** —— 无 `capability` 段；
- **2** —— **可选** `capability` 段。

> ⚠️ 常见误解：`schemaVersion: 2` **不要求**必须带 `capability`。实测 `MyWorkItems` 为 `schemaVersion: 2` 且无 `capability` 段，是合法模块。门禁 `Test-PortalBusinessModuleSmoke.ps1` 已按此校准（2 = 业务模块合规；1 且无 capability = 只读参考包合规）。

### `resources`

当前静态资源允许表为 **`.css`、`.png`、`.jpg`、`.jpeg`、`.gif`、`.webp`**（扩展名精确比较、不按 MIME 猜测、不接受无扩展名）。资源路径须位于站内；新扩展名不会因向后兼容被自动放行。

> 本表于 2026-11-16 与权威源码 `PortalModulePackage.cs:1238-1243` 对齐（此前本页只列 `.css`，与源码及 `docs/dev-guide.md:358` 不一致）；此次仅收敛文档事实，**未放宽任何允许表、未改校验实现**。

## 三、企业能力（capability）

`capability` 段是**可选的**，用于声明模块的能力归属与角色：

```json
"capability": {
 "capabilityId": "BasicBusiness.LeaveRequest",
 "role": "Primary"
}
```

- **未声明**时 `capabilityId` / `capabilityRole` 保持稳定的空文本，该模块视为 **Unclassified**（未分类）—— 这是合法状态，不是错误。
- **一旦声明**，则：
 1. `capabilityId` 与 `role` 均不得为空；
 2. `capabilityId` 必须能在**权威能力注册表**（`PortalCapabilityRegistry`）中解析到 —— 否则判为"未注册"并拒绝；
 3. `role` 必须属于**封闭五值词表**（`PortalCapabilityRoles`）：`Primary` / `Contributor` / `Projection` / `Adapter` / `Block`。

### 当前登记的能力键（实测）

| 模块 | `capabilityId` | `role` |
| --- | --- | --- |
| `BusinessApplicationRequest` | `BasicBusiness.ApplicationRequest` | `Primary` |
| `EmployeeProfileConfirm` | `BasicBusiness.EmployeeProfileConfirm` | `Primary` |
| `EmployeeProfileCorrectionRequest` | `BasicBusiness.EmployeeProfileCorrectionRequest` | `Primary` |
| `EnterpriseCapabilityWorkbench` | `BasicBusiness.Collaboration` | `Primary` |
| `ExpenseReimbursement` | `BasicBusiness.ExpenseReimbursement` | `Primary` |
| `LeaveRequest` | `BasicBusiness.LeaveRequest` | `Primary` |
| `MyWorkItems` | （未声明，Unclassified） | — |
| `ModuleProbe` | （未声明，只读参考包） | — |

> 能力键的完整词表以 `PortalCapabilityRegistry.cs` 为准，上表只是当前 `module.json` 的实测快照。

## 四、被显式拒绝的声明

当前部署模型**只允许受控 ASCX 入口和站内静态资源**。manifest 中出现以下任一字段即被拒绝 —— **manifest 声明不能改变执行或下载能力**：

- `script` / `scripts`
- `externalUrl` / `externalUrls`
- 程序集相关声明
- 包 URL 相关声明

## 五、形态描述字段契约（2026-11-16 新增）

形态描述字段按 backlog「容纳机制设计方向」四类维度收口，权威校验实现为 `src/Portal.Components/PortalModuleFormContract.cs`（**只读、独立于装载链**）：

| 维度 | 字段 | 封闭词表 / 约束 | 缺失时 |
| --- | --- | --- | --- |
| 模块种类 | `moduleKind` | `Core`（默认）/ `Independent`，其它值拒绝（现有 `PortalModulePackage.cs:989-1004` 已校验） | 缺省 `Core` |
| 宿主框架 | `hostFramework` | 仅 `AspNetWebForms`（当前唯一已实现宿主） | 未声明（兼容） |
| 入口形态 | `entryKind` | 仅 `Ascx`（当前唯一已实现入口） | 未声明（兼容） |
| 资源与依赖声明 | `resources` / `dependencies` | 资源沿用 §`resources` 允许表；依赖数组逐项非空、长度 ≤ `MaxDependencyLength`、项数 ≤ `MaxDependencyCount` | `resources` 必填；`dependencies` 未声明（兼容） |

**校验纪律**：字段出现即校验、未知值 fail-closed、原因受控不回显输入；不修改 manifest、不读文件/配置、不接入 `TryReadPackage` 装载链（既有包解析结果零变化）。单测 `src/Portal.Tests/PortalModuleFormContractTests.cs` 覆盖封闭词表、缺省兼容、非法值/类型/数组 fail-closed 与重复实跑幂等。

**开放点（显式登记，未擅自实现）**：
1. **未知字段通用拒绝**：当前 `TryReadPackage` 为逐字段读取（`:911-1017`），对未列入黑名单的未知字段无通用拒绝。是否收紧为全字段白名单须用户裁定——收紧会使既有未登记字段的包失效，属兼容性决策，不在本包范围。
2. **依赖语义**：`dependencies` 只做形态契约层校验，**不验证依赖对象是否真实存在**、不参与授权；与 `PortalNavigationRegistry`「不验证依赖存在性」口径一致。语义解析留后续周期。
3. **宿主 / 入口扩展**：`AspNetMvc` / CMS 等形态未落地，不进入词表；未来扩展须显式修改封闭词表并经确认，不因向后兼容自动放行。
4. **A 级硬边界**：本契约不触碰三条硬边界（第三方禁获 `Admins`、来源与能力声明不作授权依据、第三方不得自创能力条目），相关判定仍由 `PortalCapabilityRegistry` / `PortalModuleProvisioner` / 门禁承担。

## 六、相关门禁与文档

- 新增模块就绪检查：`dev/scripts/Test-PortalNewModuleReadiness.ps1`（R1 csproj 登记 / R2 `CodeBehind` / R3 主题作用域规则 / R4 标记与 CSS 类名一致）。
- 业务模块冒烟：`dev/scripts/Test-PortalBusinessModuleSmoke.ps1`。
- 开发流程与陷阱：[../module-development-guide.md](../module-development-guide.md)。
