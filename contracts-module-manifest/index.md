---
layout: default
lang: zh-CN
parent: 参考
permalink: /contracts-module-manifest/
title: 模块契约（`module.json`）
---
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

### `schemaVersion`

支持 **1** 与 **2**，未来版本**严格拒绝**（不是向后兼容自动放行）：

- **1** —— 无 `capability` 段；
- **2** —— **可选** `capability` 段。

> ⚠️ 常见误解：`schemaVersion: 2` **不要求**必须带 `capability`。实测 `MyWorkItems` 为 `schemaVersion: 2` 且无 `capability` 段，是合法模块。门禁 `Test-PortalBusinessModuleSmoke.ps1` 已按此校准（2 = 业务模块合规；1 且无 capability = 只读参考包合规）。

### `resources`

当前静态资源允许表为 **`.css`**（扩展名精确比较、不按 MIME 猜测、不接受无扩展名）。资源路径须位于站内；新扩展名不会因向后兼容被自动放行。

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

## 五、相关门禁与文档

- 新增模块就绪检查：`dev/scripts/Test-PortalNewModuleReadiness.ps1`（R1 csproj 登记 / R2 `CodeBehind` / R3 主题作用域规则 / R4 标记与 CSS 类名一致）。
- 业务模块冒烟：`dev/scripts/Test-PortalBusinessModuleSmoke.ps1`。
- 开发流程与陷阱：[../module-development-guide.md]({{ site.baseurl }}/module-development/)。
