# 请假与报销：审核侧现状（目前没有后台审核入口）

这页讲**管理员/审批人**这一侧。结论先说在前面：**请假与报销目前没有后台审核页面，也没有可执行的批准/驳回入口。** 这页先把现状和"能做什么、做不到什么"讲清楚，免得你在后台里找一圈。

申请人那一侧怎么填、提交后落在哪，见[请假与报销：从填写到出现在待办里]({{ site.baseurl }}/leave-and-expense-workflow/)。

## 你能看到的：后台「业务待办」页

- 入口：`/Admin/WorkItems.aspx`。进入需要 `BusinessWorkItemsView` 或 `BusinessWorkItemsAdmin` 任一权限。
- 该页按状态筛选集中展示待办：全部 / 待处理（默认）/ 处理中 / 已完成 / 已取消 / 已过期。
- 请假与报销的待办确实会出现在这里，业务类型分别是 `LeaveRequest` 与 `ExpenseReimbursement`。

每一行展示：编号、状态、业务类型、标题、摘要、分派对象（用户或角色）、创建时间（UTC）、完成时间。

## 但这一页是只读的

后台待办页**只提供集中查看和状态筛选**，它：

- 不执行转办、加签、会签或通知发送；
- **不写入待办状态** —— 也就是说，你在这页上没有任何"批准 / 驳回"按钮可点。

## 点进去会落到占位页

待办列表里有一个"业务入口"链接，但它**只认三种业务类型**：

| 业务类型 | 跳到哪 |
| --- | --- |
| `EmployeeProfileCorrectionRequest`（资料更正） | `/Admin/EmployeeProfileCorrectionRequests.aspx` |
| `BusinessApplication`（业务申请） | `/Admin/BusinessApplications.aspx` |
| `CollaborationItem`（协同事项） | `/Admin/CollaborationItems.aspx` |
| **其它类型（含 `LeaveRequest`、`ExpenseReimbursement`）** | `/Admin/NotImplemented.aspx`（占位页） |

所以请假与报销的待办，点业务入口会落到**未实现占位页**，而不是一个可以审批的页面。

## 那现在怎么批

**没有任何界面路径可以批准或驳回请假/报销。** 目前只剩下两条路：

1. 由**部署侧在数据层**直接处理（请假落在 `PortalBiz_LeaveRequests`、报销落在 `PortalBiz_ExpenseReimbursements`）；
2. 等后续补上审核页面 —— 这需要新增后台页并纳入导航登记，属产品能力新增，不是文档能闭合的事。

这与"业务申请"不同：业务申请有完整的后台审核页（批准 / 退回 / 驳回），见[审核人操作路径：业务申请怎么批]({{ site.baseurl }}/business-application-review/)。资料更正也有，见[资料更正：审核侧操作路径]({{ site.baseurl }}/profile-correction-review/)。

## 为什么写成这样

- 后台待办页的定位是**集中查看**，不是办理入口；它明确不执行转办/加签/会签/通知。
- 业务入口的 URL 映射是一个**固定白名单**，未知类型统一走占位页，避免把不支持的类型解释成可导航地址。新增业务类型必须先完成导航注册，而不是隐式放行。

## 这页没讲的事

- 申请人怎么填、提交后落库与失败路径 → [请假与报销：从填写到出现在待办里]({{ site.baseurl }}/leave-and-expense-workflow/)
- 待办的呈现与"只呈现不办理"的边界 → [我的待办]({{ site.baseurl }}/modules/my-work-items/)
- 请假模块字段与校验 → [请假申请模块页]({{ site.baseurl }}/modules/leave-request/)
- 报销模块字段与落表 → [费用报销模块页]({{ site.baseurl }}/modules/expense-reimbursement/)
- 已经有的审核路径长什么样 → [审核人操作路径：业务申请怎么批]({{ site.baseurl }}/business-application-review/)
