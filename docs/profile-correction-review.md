# 资料更正：审核侧操作路径

你是**处理人**（持有资料更正的审核/管理权限）。这篇讲后台这一侧：从看到一条更正申请，到处理掉它，以及**处理完并不等于资料被改了**这个关键边界。

申请人那一侧怎么提，见[员工资料：确认与更正]({{ site.baseurl }}/profile-confirm-and-correct/)。

## 开始之前

- 入口：`/Admin/EmployeeProfileCorrectionRequests.aspx`。
- 查看需要 `EmployeeProfileCorrectionRequestReview` 或 `EmployeeProfileCorrectionRequestAdmin` 任一权限。
- 列表只显示**当前状态筛选**下的请求，默认筛选是「已提交」。

## 打开处理页

1. 进入 `/Admin/EmployeeProfileCorrectionRequests.aspx`。
2. 用**状态筛选**下拉框选范围：全部 / 已提交（默认）/ 已处理 / 已关闭 / 已驳回。
3. 点「搜索」按当前筛选重新拉取。

每行展示：请求编号、提交时间（UTC）、员工（工号 / 姓名）、提交账号（用户 ID / 用户名）、**更正字段**、当前值快照、拟改值、员工说明、当前状态，以及处理信息（时间 / 处理人 / 备注）。

## 处理一条申请

对每一行，你可以先填一段**处理备注**（最多 1000 字，可留空），然后三选一：

- **已处理（Reviewed）**：标记为已处理。
- **已驳回（Rejected）**：驳回这条申请。
- **已关闭（Closed）**：关闭这条申请。

**权限在动作之间有差异**：设置「已处理」或「已驳回」需要 `EmployeeProfileCorrectionRequestReview` 或 `...Admin`；而设置「已关闭」需要 **`EmployeeProfileCorrectionRequestCancel`** 或 `...Admin`。也就是说，"关闭"是一个单独的权限，不能靠审核权限做。

> 本页只接受这三种目标状态。传其它值只会被判为"不支持的状态"并刷新列表，不写任何数据。

## 处理完之后

- 每一条成功处理都会记一条**运营审计**（分类 `BusinessModuleCategory`、事件 `EmployeeProfileCorrectionReviewed`），写清目标状态。
- 随后尝试把结果同步成**待办完成事件**：已驳回 → 驳回事件、已关闭 → 取消事件、其它 → 批准事件；对应待办被标为完成（关闭则标为取消）。
- 处理人与处理时间会写进请求记录。

**最重要的一条边界**：这个页面**只改请求本身的状态和备注，不直接修改员工主数据**。也就是说，你把一条更正申请标成"已处理"，**员工的资料并不会因此被改成拟改值** —— 真正的资料修改要走**员工目录维护**（后台 `/Admin/EmployeeDirectory.aspx` / `/Admin/EmployeeEdit.aspx`）。这两步是分开的，不要以为批完就改好了。

另一条边界：待办同步是审核写入**之后**的旁路调用，当前实现**不捕获**待办服务的异常。若待办服务出错，异常会向外传播（这一条已作为待办项登记，尚未改为失败隔离）。

## 看不到的

- 界面上**没有**"这条更正被谁在什么时候处理了"的独立时间线页面；能查的地方是**运营审计页**（`/Admin/OperationAudits.aspx`）。
- 申请人看到的「我的待办」**只呈现不办理**：它不审批、不改数据、也不回写业务表。

## 这页没讲的事

- 申请人怎么确认资料、怎么提更正 → [员工资料：确认与更正]({{ site.baseurl }}/profile-confirm-and-correct/)
- 更正申请的字段与校验 → [员工资料更正申请模块页]({{ site.baseurl }}/modules/employee-profile-correction-request/)
- 造样例数据与回归验证 → [员工资料更正样板业务路径]({{ site.baseurl }}/p12-sample-business-flow/)
- 待办的呈现与办理边界 → [我的待办]({{ site.baseurl }}/modules/my-work-items/)
- 审计保留期与依据 → [运营审计保留策略]({{ site.baseurl }}/audit-retention-policy/)
- 请假与报销**没有**后台审核入口这件事 → [请假与报销：审核侧现状]({{ site.baseurl }}/leave-and-expense-review/)
