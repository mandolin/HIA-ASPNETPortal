---
layout: default
lang: zh-CN
parent: 学习
nav_order: 28
permalink: /modules/my-work-items/
title: 我的待办（MyWorkItems）
---
# 我的待办（MyWorkItems）

> 模块目录：`src/Portal/DesktopModules/MyWorkItems/`｜视图：`MyWorkItems.ascx`｜**无独立编辑页、也无任何输入控件**
> 本页是**详细使用说明**；一句话速查见[用户指南的“常见模块”一节]({{ site.baseurl }}/user-guide/)。

## 用途与前置

前台**「我的待办」聚合入口**：把分散在各业务模块里的**未完成待办**聚到一处呈现，只呈现**当前用户本人**、或**其持有的角色键所指派**的未完成待办。

**它只呈现，不办理**——这是理解本模块的关键：待办在这里被看到，但**不提供就地办理**（不在此处审批、不在此处改数据）；办理要回到产生该待办的模块去。

前置条件：

1. 你已登录门户；模块按当前用户取用户记录（`.GetSingleUser(`）与权限键（`.GetPermissionKeysByUserName(`）。
2. 模块须挂载在某个页签（Tab）上才会出现。
3. 待办来自**别的模块**（如[费用报销]({{ site.baseurl }}/modules/expense-reimbursement/)、[请假申请]({{ site.baseurl }}/modules/leave-request/)、[业务申请]({{ site.baseurl }}/modules/business-application-request/)）；本模块自身不产生待办。

## 操作步骤

**查看我的待办**

无需操作：模块加载时即按当前用户取待办（`.GetWorkItemsForUser(`）并列出。

**筛选**

- 点击**全部**（`AllFilterButton`）查看全部未完成待办。
- 点击**逾期**（`OverdueFilterButton`）只看已逾期的待办。

**办理某条待办**

本模块**不提供就地办理**。请按待办上标出的来源，回到产生它的模块处理（例如报销单回[费用报销]({{ site.baseurl }}/modules/expense-reimbursement/)）。

## 字段表

| 字段 | 控件 id | 类型 | 长度上限 | 必填 | 校验 |
| --- | --- | --- | --- | --- | --- |
| （无输入字段） | — | — | — | — | — |

本模块**没有任何输入控件**——源码 `MyWorkItems.ascx` 中的输入控件（`TextBox` / `DropDownList` / `CheckBox` / `HtmlTextArea`）数量为 **0**；交互只有两个筛选按钮（`AllFilterButton` / `OverdueFilterButton`）。这不是文档漏写字段，是模块本身只读聚合。

## 必填与校验

- **没有必填项**：既没有输入控件，`RequiredFieldValidator` 的数量也是 **0**。
- **没有格式校验**：没有可填字段。
- **唯一的“过滤”是可见范围**：只呈现本人或本人持有角色键所指派的**未完成**待办——已完成的不出现，别人的不出现。

## 存储与审计

- **审计**：**查看动作本身**写运营审计——事件 `WorkItemsViewed`，目标类型 `WorkItemTargetType`，归类在 `BusinessModuleCategory` 下。即“谁什么时候看了自己的待办”是可追溯的（这与多数只读模块不同）。
- **读取**：`.GetWorkItemsForUser(` 取待办；`.GetPermissionKeysByUserName(` 取权限键；另有 `.GetPortalSettings(` / `.GetModulesByModuleDefId(` / `.GetModuleDefinitions(` / `.GetTrustedPackages(` 用于定位来源模块与受信任部署包。
- **本模块不写入业务数据**：待办的产生在各业务模块，这里只聚合呈现。

## 权限

- 可见范围是**当前用户本人 + 其持有角色键所指派**的未完成待办。
- 未取到用户记录时按无权限处理。
- 本模块不提供“查看全部人待办”的管理视角。

## 常见问题

**某个待办看不到** —— 三种可能：已完成（只列未完成）、指派给了别人或别的角色键、或该待办的来源模块不在聚合范围内。

**想在这里直接审批** —— 做不到，且**这是设计如此**：本模块**不提供就地办理**，办理要回来源模块。

**点了“逾期”但列表空** —— 当前没有逾期项；点“全部”可以确认聚合是否正常工作。

**为什么看个列表也要写审计** —— 待办可见性涉及指派与角色键，属于敏感读取；事件 `WorkItemsViewed` 就是为此留痕。

## 边界与不做

- 本模块**不提供就地办理**：不审批、不改数据、不回写来源模块。这是**既定设计**，不是缺陷。
- 本模块**只列未完成**待办：已完成的不出现。
- 本模块**不提供管理视角的全量待办**。
- 本模块**无独立编辑页、无输入控件**。
- 待办的**语义由来源模块定义**：本模块只聚合呈现，不解释业务含义。

<!-- build-stamp: 619EA1C3B409 -->
