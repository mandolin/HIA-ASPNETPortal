---
layout: default
lang: zh-CN
parent: 参考
nav_order: 14
permalink: /modules/employee-profile-confirm/
title: 员工资料确认（EmployeeProfileConfirm）
---
# 员工资料确认（EmployeeProfileConfirm）

> 模块目录：`src/Portal/DesktopModules/EmployeeProfileConfirm/`｜视图：`EmployeeProfileConfirm.ascx`｜**无独立编辑页、也无任何输入控件**
> 本页是**详细使用说明**；一句话速查见[用户指南的“常见模块”一节]({{ site.baseurl }}/user-guide/)。

## 用途与前置

员工资料确认业务模块样板：员工**只确认自己当前绑定的低敏资料**，不做修改、不填表单。模块加载时读出当前用户的资料并以**只读标签**呈现，用户点击一次**确认**即完成。

**这是“确认”而不是“编辑”**——模块没有任何输入框；想改资料要走[员工资料更正申请]({{ site.baseurl }}/modules/employee-profile-correction-request/)，由那边提交申请。

前置条件：

1. 你已登录门户，且当前账号**绑定了员工资料**（模块读取 `.GetCurrentProfileForUser(`）。
2. 模块须挂载在某个页签（Tab）上才会出现。
3. 未取到资料/未取到用户时按无权限处理（不是“页面空白”，而是走无权限分支）。

## 操作步骤

**确认我的资料**

1. 打开承载该模块的页签，核对页面上列出的只读资料项（9 个只读标签）。
2. 确认无误后点击**确认**（`ConfirmButton`）。
3. 确认动作被记录为审计事件 `EmployeeProfileConfirmed`。

**发现资料有误**

不要在本模块里改——本模块没有输入框。请到[员工资料更正申请]({{ site.baseurl }}/modules/employee-profile-correction-request/)提交更正申请。

## 字段表

| 字段 | 控件 id | 类型 | 长度上限 | 必填 | 校验 |
| --- | --- | --- | --- | --- | --- |
| （无输入字段） | — | — | — | — | — |

本模块**没有任何输入控件**——源码 `EmployeeProfileConfirm.ascx` 中的输入控件（`TextBox` / `DropDownList` / `CheckBox` / `HtmlTextArea`）数量为 **0**，页面内容由 9 个**只读标签**与一个确认按钮（`ConfirmButton`）构成。这不是文档漏写字段，是模块本身只读。

## 必填与校验

- **没有必填项**：既没有输入控件，`RequiredFieldValidator` 的数量也是 **0**。
- **没有格式校验**：因为根本没有可填的字段。
- **唯一的动作校验是权限**：取不到当前用户的资料时走无权限分支，不会落到“确认了一个空资料”的状态。

## 存储与审计

- **审计**：确认动作写运营审计——事件 `EmployeeProfileConfirmed`，目标类型 `EmployeeProfileConfirmationTargetType`，归类在 `BusinessModuleCategory` 下。**“谁在什么时候确认过自己的资料”是可追溯的。**
- **无权限访问也会被记录**：事件 `AccessDenied` 同样写入审计。
- **读取**：`.GetCurrentProfileForUser(` 取当前用户资料；`.GetSingleUser(` 取用户记录。
- **本模块不写入业务数据**：确认是动作记录，不是资料变更。

## 权限

- 只呈现**当前用户自己**的资料；没有查看他人资料的入口。
- 取不到用户或资料时按无权限处理，并写入 `AccessDenied` 审计事件。

## 常见问题

**页面上看不到输入框，是不是坏了** —— 不是。**本模块设计上就是只读确认**，改资料要走[员工资料更正申请]({{ site.baseurl }}/modules/employee-profile-correction-request/)。

**点了确认但没反应** —— 确认只写审计、不改数据，页面上不会有“资料已更新”的变化；可核对审计侧是否记录了 `EmployeeProfileConfirmed`。

**提示无权限** —— 当前账号可能没有绑定员工资料，或取不到用户记录；该情况会写入 `AccessDenied`。

## 边界与不做

- 本模块**不提供任何编辑能力**：无输入控件、无编辑页。想改资料必须走更正申请流程。
- 本模块**不写入业务数据**，只写审计。
- 本模块**不提供查看他人资料**。
- 本模块是**业务模块样板**：它的形态被[员工资料更正申请]({{ site.baseurl }}/modules/employee-profile-correction-request/)等模块沿用，因此这里的“只读 + 审计”语义是整套业务模块的基线。
- 本页不依赖运行期截图；结论均回溯到源码标记与已核实的审计事件名。

<!-- build-stamp: 198D3B5A0EBD -->
