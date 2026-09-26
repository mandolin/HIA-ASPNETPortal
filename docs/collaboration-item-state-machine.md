# 协同事项状态机（Collaboration Item State Machine）

> 归属：`W-anp-P64` 状态机显式化（[W-anp-P64](../work-zone/dev/plans/W-anp-P64.md)）。代码事实源：`src/Portal.Components/PortalCollaborationItemTransitions.cs`。

## 一、状态词汇表（Status）

| 状态（常量） | 含义 |
| --- | --- |
| `Draft` | 草稿，尚未提交给处理人 |
| `Submitted` | 已提交，等待负责人或处理角色处理 |
| `InProgress` | 处理中 |
| `Returned` | 已退回发起人补充 |
| `Completed` | 已完成（终态） |
| `Rejected` | 已驳回（终态） |
| `Cancelled` | 已取消（终态） |
| `Closed` | 已关闭/归档（终态） |

状态集由 `PortalCollaborationItemStatuses` 与数据库约束 `CK_PortalBiz_CollaborationItems_Status` 共同保证（两者须一致）。终态 = `Completed` / `Rejected` / `Cancelled` / `Closed`，由 `IsTerminalStatus` 定义。

## 二、动作（Action）

| 动作（常量） | 含义 |
| --- | --- |
| `Submit` | 提交（Draft → Submitted） |
| `Start` | 开始处理（Submitted → InProgress） |
| `Complete` | 完成（Submitted/InProgress → Completed） |
| `Return` | 退回（Submitted/InProgress → Returned），需处理意见 |
| `Resubmit` | 重新提交（Returned → Submitted），发起人操作 |
| `Reject` | 驳回（Submitted/InProgress → Rejected），需处理意见 |
| `Cancel` | 取消（Draft/Submitted/Returned → Cancelled） |
| `Close` | 关闭/归档（Completed/Rejected/Cancelled → Closed） |
| `CreateDraft` | 仅用于创建草稿，不进入迁移路径 |

## 三、合法迁移表（Single Source of Truth）

下列 15 条迁移是状态机的**唯一事实源**，定义于 `PortalCollaborationItemTransitions.All`：

| 当前状态 | 动作 | 目标状态 |
| --- | --- | --- |
| `Draft` | `Submit` | `Submitted` |
| `Submitted` | `Start` | `InProgress` |
| `Submitted` | `Complete` | `Completed` |
| `InProgress` | `Complete` | `Completed` |
| `Submitted` | `Return` | `Returned` |
| `InProgress` | `Return` | `Returned` |
| `Returned` | `Resubmit` | `Submitted` |
| `Submitted` | `Reject` | `Rejected` |
| `InProgress` | `Reject` | `Rejected` |
| `Draft` | `Cancel` | `Cancelled` |
| `Submitted` | `Cancel` | `Cancelled` |
| `Returned` | `Cancel` | `Cancelled` |
| `Completed` | `Close` | `Closed` |
| `Rejected` | `Close` | `Closed` |
| `Cancelled` | `Close` | `Closed` |

- `MapActionToStatus(actionKey)` 由该表派生（同一动作目标唯一）。
- 数据库写入守卫的 SQL `WHERE` 谓词由 `BuildSqlStatusPredicate()` 从同一表生成，避免与表漂移。

## 四、强制校验（Enforcement）

1. **服务端显式门禁**：`ApplyAction` 在授权（`CanApplyAction`）之后、写入之前调用 `IsLegalTransition(currentStatus, actionKey)`；不合法即拒绝（fail-closed），不泄露内部原因。
2. **数据库守卫**：SQL `UPDATE` 的 `WHERE` 谓词（由迁移表生成）在写入期再次验证当前状态；零行更新即不推进、不写孤立事件。
3. **授权**：`CanApplyAction` 按状态/动作/角色判定处理权，与迁移合法性解耦（未改）。
4. **必填评论**：`Return`/`Reject` 强制处理意见（`ActionRequiresComment`）。
5. **父子约束**：进入终态前 `HasOpenDescendants` 阻止父项在子项未终态时被关闭。
6. **时间一致性**：`CompletedUtc`/`ClosedUtc` 由数据库约束 `CK_..._CompletionUtc` / `CK_..._ClosedUtc` 与写入逻辑（按 `TargetStatus` 同步）保证与终态一一对应。

## 五、迁移必写事件

每个**实际发生**的迁移都会写入一条 `WorkflowAction` 事件（`PortalBiz_CollaborationItemEvents`），含 `FromStatus` 与 `ToStatus` 留痕；SQL 批处理仅在 `OUTPUT` 实际更新集合上写事件，零行更新不写孤立事件。

## 六、不在范围

- 旧业务申请 `BusinessApplicationDb`（P19 旧样板）有独立状态机，不在本状态机范围内。
- 本轮不新增/删除状态或迁移，不改变业务语义与 UI。
