---
layout: default
lang: zh-CN
parent: 学习
nav_order: 24
permalink: /modules/leave-request/
title: 请假申请（LeaveRequest）
---
# 请假申请（LeaveRequest）

> 模块目录：`src/Portal/DesktopModules/LeaveRequest/`｜视图（含表单）：`LeaveRequest.ascx`｜**无独立编辑页**（表单与列表写在同一个 `ascx` 内）
> 本页是**详细使用说明**；一句话速查见[用户指南的“常见模块”一节]({{ site.baseurl }}/user-guide/)。

## 用途与前置

请假申请模块：员工提交**请假申请**（假别、起止日期、天数、事由，可附证明材料），提交后业务记录持久化到 **`PortalBiz_LeaveRequests`**，并写入一条工作项到**「我的待办」**。

模块下方同时列出**当前用户本人**的历史请假记录。

前置条件：

1. 你已登录门户；模块按当前用户取用户记录（`.GetSingleUser(`）。
2. 模块须挂载在某个页签（Tab）上才会出现。
3. 需要附件时准备好证明材料——附件**可选**。

## 操作步骤

**提交一条请假申请**

1. 打开承载该模块的页签，在**假别**下拉框（`ddlLeaveType`）中选择假别。
2. 填写**开始日期**（`txtStartDate`）与**结束日期**（`txtEndDate`）。
3. 填写**天数**（`txtDays`）。
4. 填写**事由**（`txtReason`）。
5. （可选）通过附件控件（`fileAttach`）上传证明材料。
6. 点击**提交**（`btnSubmit`）。
7. 提交后：记录落表 `PortalBiz_LeaveRequests`，并在「我的待办」生成一条工作项；页面下方列表出现这条记录。

**查看我提交过的请假**

无需操作：模块加载时按当前用户读取历史记录（`.GetRecentByUser(`）并列出。

## 字段表

| 字段 | 控件 id | 类型 | 长度上限 | 必填 | 校验 |
| --- | --- | --- | --- | --- | --- |
| 假别 | `ddlLeaveType` | 下拉选择 | — | 否（无标记层必填校验器） | 取值来自下拉项 |
| 开始日期 | `txtStartDate` | 单行文本 | 源码未设 `MaxLength` | 否（无标记层必填校验器） | **日期格式由代码侧判定**，填错会在提交后被拒 |
| 结束日期 | `txtEndDate` | 单行文本 | 源码未设 `MaxLength` | 否（无标记层必填校验器） | 同上；且与开始日期的先后关系由代码侧判定 |
| 天数 | `txtDays` | 单行文本 | 源码未设 `MaxLength` | 否（无标记层必填校验器） | **数值格式由代码侧判定** |
| 事由 | `txtReason` | 单行文本 | 源码未设 `MaxLength` | 否（无标记层必填校验器） | 由代码侧判定 |
| 证明材料 | `fileAttach` | 文件上传控件 | — | 可选 | 由上传路径的白名单/校验策略决定 |

字段的控件 id 与类型取自 `LeaveRequest.ascx` 的标记，**不是本文档的估计**。本模块**没有独立的 `Edit*.aspx`**。

## 必填与校验

- **标记层没有必填校验器**：`LeaveRequest.ascx` 中 `RequiredFieldValidator` 的数量为 **0**。必填由**提交时代码侧判定**。
- **日期与天数是风险最高的两项**：`txtStartDate` / `txtEndDate` / `txtDays` 在标记层都是普通文本框，没有日期比较校验器——**填一个结束早于开始的区间，标记层不会拦**，由服务端判定。
- **不要因为页面没标红就以为区间合法**。

## 存储与审计

- **存储**：提交后业务记录持久化到 **`PortalBiz_LeaveRequests`**（仓储调用 `.CreateRequest(`）。
- **工作项**：同时写入「我的待办」——与[业务申请]({{ site.baseurl }}/modules/business-application-request/)、[员工资料更正申请]({{ site.baseurl }}/modules/employee-profile-correction-request/)**同构**。
- **审计**：归类在 `BusinessModuleCategory` 下。
- **读取**：`.GetRecentByUser(` 取当前用户的历史记录。

## 权限

- 历史列表按**当前用户**过滤，只能看到自己的请假记录。
- 未取到用户记录时按无权限处理。
- 本模块不提供审批与“查看他人请假”的能力。

## 常见问题

**提交后没进「我的待办」** —— 先确认提交真的成功；被服务端拒绝时不落表、也不产生工作项。

**日期填了却提交失败** —— `txtStartDate` / `txtEndDate` 的格式不被接受，或区间不成立；判定发生在服务端。

**天数填小数或文字失败** —— `txtDays` 需要可解析的数值。

**看不到别人的请假** —— 设计如此：列表是本人视角。

## 边界与不做

- 本模块**不做审批流**，也不做日历视图、剩余额度计算、冲突检测。
- 本模块**不提供编辑与撤回**。
- 本模块**无独立编辑页**——表单与列表在同一个 `ascx` 内。
- 部署页签与实例数为交付时快照；**新增或迁移页签后需重新核对**本页描述。
- 与[费用报销]({{ site.baseurl }}/modules/expense-reimbursement/)同构（各自的业务表 + 同一个待办写入机制），只是字段不同。

<!-- build-stamp: F974D48E8D85 -->
