---
layout: default
lang: zh-CN
parent: 学习
nav_order: 22
permalink: /modules/expense-reimbursement/
title: 费用报销（ExpenseReimbursement）
---
# 费用报销（ExpenseReimbursement）

> 模块目录：`src/Portal/DesktopModules/ExpenseReimbursement/`｜视图（含表单）：`ExpenseReimbursement.ascx`｜**无独立编辑页**（表单与列表写在同一个 `ascx` 内）
> 本页是**详细使用说明**；一句话速查见[用户指南的“常见模块”一节]({{ site.baseurl }}/user-guide/)。

## 用途与前置

费用报销模块：员工提交**费用报销单**（类别、金额、事由，可附单据），提交后业务记录持久化到 **`PortalBiz_ExpenseReimbursements`**，并写入一条工作项到**「我的待办」**——因此提交后可以在[我的待办]({{ site.baseurl }}/modules/my-work-items/)里看到它。

模块下方同时列出**当前用户本人**的历史报销记录。

前置条件：

1. 你已登录门户；模块按当前用户取用户记录（`.GetSingleUser(`）。
2. 模块须挂载在某个页签（Tab）上才会出现。
3. 需要附件时准备好单据文件——附件是**可选**的，不是提交的前提。

## 操作步骤

**提交一张报销单**

1. 打开承载该模块的页签，在**类别**下拉框（`ddlCategory`）中选择费用类别。
2. 填写**金额**（`txtAmount`）。
3. 填写**事由**（`txtReason`）。
4. （可选）通过附件控件（`fileAttach`）上传单据。
5. 点击**提交**（`btnSubmit`）。
6. 提交后：报销记录落表 `PortalBiz_ExpenseReimbursements`，并在「我的待办」生成一条工作项；页面下方列表出现这条记录。

**查看我提交过的报销单**

无需操作：模块加载时按当前用户读取历史记录（`.GetRecentByUser(`）并列出。

## 字段表

| 字段 | 控件 id | 类型 | 长度上限 | 必填 | 校验 |
| --- | --- | --- | --- | --- | --- |
| 类别 | `ddlCategory` | 下拉选择 | — | 否（无标记层必填校验器） | 取值来自下拉项 |
| 金额 | `txtAmount` | 单行文本 | 源码未设 `MaxLength` | 否（无标记层必填校验器） | **金额格式由代码侧判定**，非数字会被拒 |
| 事由 | `txtReason` | 单行文本 | 源码未设 `MaxLength` | 否（无标记层必填校验器） | 由代码侧判定 |
| 单据附件 | `fileAttach` | 文件上传控件 | — | 可选 | 由上传路径的白名单/校验策略决定 |

字段的控件 id 与类型取自 `ExpenseReimbursement.ascx` 的标记，**不是本文档的估计**。本模块**没有独立的 `Edit*.aspx`**。

## 必填与校验

- **标记层没有必填校验器**：`ExpenseReimbursement.ascx` 中 `RequiredFieldValidator` 的数量为 **0**。必填由**提交时代码侧判定**，不是页面上的红色提示。
- **金额是最容易出问题的字段**：`txtAmount` 在标记层只声明为普通文本框，非数字的判定发生在服务端——填了非数字不会当场提示，而是提交后被拒。
- **附件可选**：不上传也能提交；上传则受附件路径策略约束。

## 存储与审计

- **存储**：提交后业务记录持久化到 **`PortalBiz_ExpenseReimbursements`**（仓储调用 `.CreateRequest(`）。
- **工作项**：同时写入一条工作项到「我的待办」——这与[业务申请]({{ site.baseurl }}/modules/business-application-request/)、[员工资料更正申请]({{ site.baseurl }}/modules/employee-profile-correction-request/)**同构**。
- **审计**：归类在 `BusinessModuleCategory` 下（本模块的审计归类与其余业务模块一致）。
- **读取**：`.GetRecentByUser(` 取当前用户的历史记录。

## 权限

- 历史列表按**当前用户**过滤，只能看到自己的报销单。
- 未取到用户记录时按无权限处理。
- 本模块不提供审批与“查看他人报销单”的能力。

## 常见问题

**提交后没进「我的待办」** —— 先确认提交是否真的成功（被服务端拒绝时不落表、也不产生工作项）；成功提交后工作项才会出现。

**金额填了却提交失败** —— `txtAmount` 的值不是可接受的数字。它在标记层没有格式校验器，判定发生在服务端。

**看不到别人的报销单** —— 设计如此：列表是本人视角。

**附件上传失败** —— 受上传路径策略约束（类型/路径），与必填无关。

## 边界与不做

- 本模块**不做审批流**：提交即落表 + 生成待办，审批在待办侧或人工流程里。
- 本模块**不提供编辑与撤回**：提交后的记录不在本模块内修改。
- 本模块**无独立编辑页**——表单与列表在同一个 `ascx` 内。
- 部署页签与实例数为交付时快照；**新增或迁移页签后需重新核对**本页描述。
- 与[请假申请]({{ site.baseurl }}/modules/leave-request/)同构（各自的业务表 + 同一个待办写入机制），只是字段不同。

<!-- build-stamp: A87DC1EA2A9C -->
