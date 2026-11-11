---
layout: default
lang: zh-CN
parent: 学习
nav_order: 24
permalink: /modules/enterprise-capability-workbench/
title: 企业能力工作台（EnterpriseCapabilityWorkbench）
---
# 企业能力工作台（EnterpriseCapabilityWorkbench）

> 模块目录：`src/Portal/DesktopModules/EnterpriseCapabilityWorkbench/`｜视图（含表单）：`EnterpriseCapabilityWorkbench.ascx`｜**无独立编辑页**（表单、列表、评论写在同一个 `ascx` 内）
> 本页是**详细使用说明**；一句话速查见[用户指南的“常见模块”一节]({{ site.baseurl }}/user-guide/)。

## 用途与前置

企业能力工作台前台模块：围绕**协作条目**工作——提交条目、被退回后**重新提交**、参与者**追加评论**，并按优先级与截止时间跟踪。

它是本批模块里**输入控件最多**的一个（7 个），也是唯一同时具备“提交 / 重新提交 / 评论”三种动作的模块。

前置条件：

1. 你已登录门户；模块按当前用户取用户记录（`.GetSingleUser(`）。
2. 模块须挂载在某个页签（Tab）上才会出现。
3. 想对**已有条目**做重新提交或评论，需要先在列表里选中该条目——这两个动作是**针对某条已有条目**的，不是新建。

## 操作步骤

**提交一条新条目**

1. 打开承载该模块的页签，在表单填写**标题**（`TitleTextBox`）。
2. 选择**条目类型**（`ItemTypeList`）与**优先级**（`PriorityList`）。
3. 填写**摘要**（`SummaryTextBox`）与**截止 UTC 时间**（`DueUtcTextBox`）。
4. 需要时填写**详细说明**（`DescriptionTextBox`）。
5. 点击**提交**（`SubmitButton`）。条目进入时间线，事件 `CollaborationItemSubmitted`。

**被退回后重新提交**

1. 在列表中找到被退回的条目并选中。
2. 修正内容后点击**重新提交**（`ResubmitButton`）。事件 `CollaborationItemResubmitted`。

**追加参与者评论**

1. 选中目标条目。
2. 在**评论**（`ParticipantCommentTextBox`）中填写内容。
3. 点击**追加评论**（`AddParticipantCommentButton`）。事件 `CollaborationItemCommentAdded`。

**查看我参与的条目**

无需操作：模块加载时按当前用户读取近期条目（`.GetRecentItemsForUser(`）并列出，同时带出参与者与可见事件。

## 字段表

| 字段 | 控件 id | 类型 | 长度上限 | 必填 | 校验 |
| --- | --- | --- | --- | --- | --- |
| 标题 | `TitleTextBox` | 单行文本 | 200 | 否（无标记层必填校验器） | 由代码侧判定 |
| 条目类型 | `ItemTypeList` | 下拉选择 | — | 否（无标记层必填校验器） | 取值来自下拉项 |
| 优先级 | `PriorityList` | 下拉选择 | — | 否（无标记层必填校验器） | 取值来自下拉项 |
| 摘要 | `SummaryTextBox` | 单行文本 | 500 | 否（无标记层必填校验器） | 由代码侧判定 |
| 截止 UTC 时间 | `DueUtcTextBox` | 单行文本 | 19 | 否（无标记层必填校验器） | **UTC 时间格式由代码侧判定**；长度上限 19 位（对应 `yyyy-MM-ddTHH:mm:ss` 形态） |
| 详细说明 | `DescriptionTextBox` | 多行文本 | 4000 | 否（无标记层必填校验器） | 由代码侧判定 |
| 参与者评论 | `ParticipantCommentTextBox` | 多行文本 | 1000 | 否（无标记层必填校验器） | 由代码侧判定 |

字段的控件 id、类型与长度上限取自 `EnterpriseCapabilityWorkbench.ascx` 的标记，**不是本文档的估计**。本模块**没有独立的 `Edit*.aspx`**。

## 必填与校验

- **标记层没有必填校验器**：`EnterpriseCapabilityWorkbench.ascx` 中 `RequiredFieldValidator` 的数量为 **0**。必填由**提交时代码侧判定**。
- **截止时间是风险最高的字段**：`DueUtcTextBox` 长度上限 19 位，填的是 **UTC** 时间——填成本地时间或与 19 位形态不符，标记层不会拦，由服务端判定。
- **三种动作共用同一批字段**：重新提交与追加评论复用同一表单区域，注意动作按钮不要点错（`SubmitButton` / `ResubmitButton` / `AddParticipantCommentButton`）。

## 存储与审计

- **审计**：三种动作各有审计事件——`CollaborationItemSubmitted`（提交）、`CollaborationItemResubmitted`（重新提交）、`CollaborationItemCommentAdded`（追加评论），目标类型 `CollaborationItemTargetType`，归类在 `BusinessModuleCategory` 下。
- **写入**：`.CreateSubmittedItem(` 落条目。
- **读取**：`.GetRecentItemsForUser(` 取当前用户的近期条目；`.GetParticipantsForItems(` 取参与者；`.GetVisibleEventsForItems(` 取可见事件。
- **降级路径**：`.GetFallbackItems(` ——当条目读取不可用时会走这条**降级**路径。**这是既定行为，不是缺陷**：它保证工作台在依赖不可用时仍能呈现，而不是整页报错；但降级得到的内容可能与主路径不同，需以主路径为准核对。

## 权限

- 列表按**当前用户**取近期条目；参与者与可见事件随条目带出。
- 未取到用户记录时按无权限处理。
- 本模块不提供“查看全部人条目”的管理视角。

## 常见问题

**提交后条目没出现** —— 先确认提交真的成功；被服务端拒绝时不落条目。

**截止时间填了却失败** —— `DueUtcTextBox` 需要 **UTC** 时间且不超过 19 位；判定在服务端。

**重新提交/评论按钮点了没效果** —— 这两个动作针对**已选中的条目**；未选中条目时不会生效。

**看到的内容与预期不一致** —— 可能走了 `.GetFallbackItems(` 的降级路径（依赖不可用时的既定行为）；以主路径核对。

## 边界与不做

- 本模块**不做审批流与状态机裁决**：提交 / 重新提交 / 评论是它全部的动作语义。
- 本模块**不提供管理视角的全量列表**。
- 本模块**无独立编辑页**——表单、列表、评论写在同一个 `ascx` 内。
- **降级路径 `.GetFallbackItems(` 属既定行为**：依赖不可用时仍呈现内容，但不保证与主路径一致。
- 与[费用报销]({{ site.baseurl }}/modules/expense-reimbursement/)、[请假申请]({{ site.baseurl }}/modules/leave-request/)的差别：那两个是**单向提交**（提交后只能看），本模块支持**重新提交与评论**，是双向协作语义。

<!-- build-stamp: 5BD2A4ECDA11 -->
