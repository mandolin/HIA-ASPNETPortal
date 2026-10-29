---
layout: default
lang: zh-CN
parent: 学习
nav_order: 21
permalink: /modules/employee-profile-correction-request/
title: 员工资料更正申请（EmployeeProfileCorrectionRequest）
---
# 员工资料更正申请（EmployeeProfileCorrectionRequest）

> 模块目录：`src/Portal/DesktopModules/EmployeeProfileCorrectionRequest/`｜视图（含表单）：`EmployeeProfileCorrectionRequest.ascx`｜**无独立编辑页**（表单与列表写在同一个 `ascx` 内）
> 本页是**详细使用说明**；一句话速查见[用户指南的“常见模块”一节]({{ site.baseurl }}/user-guide/)。

## 用途与前置

员工资料更正请求业务模块样板：员工发现自己的资料有误时，**提交一条更正申请**（选择要更正的字段、填写拟改值、写明申请说明），交由后续处理。

它与[员工资料确认]({{ site.baseurl }}/modules/employee-profile-confirm/)配套：那边**只读确认**，这边**发起更正**。更正**不是直接改数据**——提交的是申请，不是写库。

前置条件：

1. 你已登录门户，且当前账号绑定了员工资料（模块读取 `.GetCurrentProfileForUser(`）。
2. 模块须挂载在某个页签（Tab）上才会出现。
3. 明确知道要更正哪个字段——可更正的字段受下拉框（`FieldNameList`）限制，不是任意字段。

## 操作步骤

**提交一条更正申请**

1. 打开承载该模块的页签，先核对页面上部列出的**当前资料**（只读）。
2. 在**更正字段**下拉框（`FieldNameList`）中选择要更正的字段。
3. 在**拟改值**（`ProposedValueTextBox`）中填写希望改成的值。
4. 在**申请说明**（`RequestNoteTextBox`）中写明为什么改（这是后续处理的依据）。
5. 点击**提交**（`SubmitButton`）。
6. 提交后下方列表出现这条申请，并带上本次的时间线事件。

**查看我提交过的更正申请**

无需操作：模块加载时按当前用户读取近期申请（`.GetRecentRequestsForUser(`）并列出。

## 字段表

| 字段 | 控件 id | 类型 | 长度上限 | 必填 | 校验 |
| --- | --- | --- | --- | --- | --- |
| 更正字段 | `FieldNameList` | 下拉选择 | — | 否（无标记层必填校验器） | 只能选下拉项内的字段 |
| 拟改值 | `ProposedValueTextBox` | 单行文本 | 512 | 否（无标记层必填校验器） | **取值是否合法由代码侧按目标字段判定** |
| 申请说明 | `RequestNoteTextBox` | 多行文本 | 1000 | 否（无标记层必填校验器） | 由代码侧判定 |

字段的控件 id、类型与长度上限取自 `EmployeeProfileCorrectionRequest.ascx` 的标记，**不是本文档的估计**。本模块**没有独立的 `Edit*.aspx`**。

## 必填与校验

- **标记层没有必填校验器**：`EmployeeProfileCorrectionRequest.ascx` 中 `RequiredFieldValidator` 的数量为 **0**。必填由**提交时代码侧判定**。
- **拟改值的合法性按目标字段判定**：同一个 `ProposedValueTextBox` 对不同字段的含义不同（例如日期字段与文本字段），标记层无法统一校验——**填错格式不会当场标红，而是提交后被拒**。
- **申请说明建议写足 1000 字以内**：它是后续处理的依据，空说明会让处理方无从判断。

## 存储与审计

- **审计**：提交写运营审计——事件 `EmployeeProfileCorrectionRequested`，目标类型 `EmployeeProfileCorrectionRequestTargetType`，归类在 `BusinessModuleCategory` 下。
- **无权限访问也会被记录**：事件 `AccessDenied` 同样写入审计。
- **读取**：`.GetCurrentProfileForUser(` 取当前资料；`.GetRecentRequestsForUser(` 取当前用户的近期申请；`.GetSingleUser(` 取用户记录。
- **本模块提交的是申请、不直接改资料**：资料的实际变更发生在后续处理环节，不在本模块内。

## 权限

- 当前资料与申请列表都按**当前用户**过滤。
- 取不到用户或资料时按无权限处理，并写入 `AccessDenied`。
- 本模块不提供“查看他人更正申请”或“直接审批”的入口。

## 常见问题

**提交后资料没变** —— 正常。本模块提交的是**申请**，不是直接改数据；资料变更在后续处理环节生效。

**拟改值提交被拒** —— 该值不符合目标字段的格式/取值要求（`ProposedValueTextBox` 在标记层无格式校验器，判定在服务端）。

**看不到别人的申请** —— 设计如此：列表是本人视角。

**想直接改资料** —— 本模块没有这个能力；所有资料变更都以申请形式发起。

## 边界与不做

- 本模块**不直接修改资料**，只发起更正申请。
- 本模块**不做审批流**：提交即进入后续处理。
- 本模块**无独立编辑页**——表单与列表写在同一个 `ascx` 内。
- 可更正字段**受 `FieldNameList` 限制**，不是任意字段都能改。
- 与[员工资料确认]({{ site.baseurl }}/modules/employee-profile-confirm/)的差别：那边是只读确认（无输入控件），这边是发起更正（3 个输入控件 + 提交）。

<!-- build-stamp: 352E807AB177 -->
