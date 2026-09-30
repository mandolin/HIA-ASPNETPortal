# 业务申请状态机（Business Application State Machine）

> 归属：`W-anp-P71` 状态机显式化（[W-anp-P71](../work-zone/dev/plans/W-anp-P71.md)）。代码事实源：`src/Portal.Components/PortalBusinessApplicationTransitions.cs`。
> 判据来源：`work-zone/dev/research/foundation-process-orchestration-reference-2026-09-26.md`（"初步完善"四条判据：显式迁移表、写入前 fail-closed、迁移必写事件、状态流转说明）。

## 一、状态词汇表（Status）

| 状态（常量） | 含义 |
| --- | --- |
| `Draft` | 草稿 |
| `Submitted` | 已提交，位于待审核窗口 |
| `InReview` | 审核中，同样位于待审核窗口 |
| `Returned` | 已退回申请人补充 |
| `Approved` | 已批准 |
| `Rejected` | 已驳回 |
| `Withdrawn` | 已撤回 |
| `Closed` | 已关闭/归档 |

状态集由 `PortalBusinessApplicationStatuses` 与数据库约束 `CK_PortalBiz_BusinessApplications_Status` 共同保证（两者须一致）。

**终态**：本状态机**没有**独立的终态常量或 `IsTerminalStatus` 判定；从迁移表可推出 `Approved` / `Rejected` / `Closed` / `Withdrawn` 在当前实现下**没有任何出发动作**，因此事实上的终态即这四者。这一结论由"迁移表中不存在以它们为来源的行"得出，而非来自某个终态函数。

## 二、动作（Action）

| 动作（常量） | 含义 | 是否纳入迁移表 |
| --- | --- | --- |
| `Approve` | 批准 | ✅ 审核路径 |
| `Return` | 退回 | ✅ 审核路径 |
| `Reject` | 驳回 | ✅ 审核路径 |
| `Close` | 关闭/归档 | ✅ 审核路径 |
| `Resubmit` | 申请人重新提交（`W71` 新增） | ✅ **自助路径**，不进入后台审核谓词 |
| `Submit` | 提交申请 | ❌ 由 `SubmitApplication` 处理（创建申请并写事件），不经过审核迁移路径 |
| `Claim` | 认领 | ❌ 当前未接线（仅有常量） |
| `Withdraw` | 撤回 | ❌ 当前未接线（仅有常量） |
| `CreateDraft` | 创建草稿 | ❌ 仅用于创建，不进入迁移路径 |

未纳入迁移表的动作如实标注，不以"存在常量"冒充"存在实现"。

## 三、合法迁移表（Single Source of Truth）

下列 9 条迁移是状态机的**唯一事实源**，定义于 `PortalBusinessApplicationTransitions.All`：

| 当前状态 | 动作 | 目标状态 | 路径 |
| --- | --- | --- | --- |
| `Submitted` | `Approve` | `Approved` | 审核 |
| `InReview` | `Approve` | `Approved` | 审核 |
| `Submitted` | `Return` | `Returned` | 审核 |
| `InReview` | `Return` | `Returned` | 审核 |
| `Submitted` | `Reject` | `Rejected` | 审核 |
| `InReview` | `Reject` | `Rejected` | 审核 |
| `Submitted` | `Close` | `Closed` | 审核 |
| `InReview` | `Close` | `Closed` | 审核 |
| `Returned` | `Resubmit` | `Submitted` | **申请人自助** |

- `MapActionToStatus(actionKey)` 由该表派生（同一动作目标唯一）。
- `ReviewApplication` 的 SQL 写入守卫谓词由 `BuildSqlStatusPredicate()` 从同一表生成（**仅含审核路径动作**），避免与表漂移；`Resubmit` 因 `ViaReviewPath = false` 不进入该谓词，防止自助动作被后台审核路径意外触发。
- 有单测断言生成谓词与显式化之前的手写窗口 `IN (N'Submitted', N'InReview')` **语义等价**。

**保留既有语义（重要）**：`Close` 仅允许从 `Submitted` / `InReview`，**不是**从 `Approved` / `Rejected`。这是显式化之前的既有行为，本轮原样保留；调整属独立决策（需迁移与回归），并由等价性单测守护现状。

## 四、强制校验（Enforcement）

1. **服务端显式门禁**：`ReviewApplication` 先由迁移表把动作映射为目标状态；未知动作返回"不支持动作"，不进入写入。
2. **数据库守卫**：SQL `UPDATE` 的 `WHERE` 谓词（由迁移表生成）在写入期再次验证当前状态；零行更新即不推进、不写孤立事件。
3. **自助动作独立路径**：`ResubmitApplication` 把**归属**（`ApplicantUserId`）与**状态**（`Returned`）条件写在同一个 `UPDATE` 语句中，消除"先查后改"的 TOCTOU 窗口；不写入任何审核人字段。
4. **约束驱动的字段处理**：`CK_PortalBiz_BusinessApplications_ReviewState` 要求 `Submitted` 状态下 `ReviewedUtc` 与 `ReviewedByUserId` **必须为 NULL**，因此 `ResubmitApplication` 在置状态的同时**必须清空这两个审核字段**，否则会被约束直接拒绝。`ReviewComment` 不受约束、予以保留，权威历史由迁移事件承载。
5. **授权**：审核动作仍由后台页面既有权限门禁（申请查看/审核/管理权限键）约束，迁移表只负责状态合法性，不替代授权。

## 五、迁移必写事件

每个**实际发生**的迁移都会写入一条 `PortalBiz_WorkflowEvents` 记录，含 `BusinessKind = BusinessApplication`、`ActionKey`、`FromStatus`、`ToStatus` 与操作人留痕。SQL 批处理仅在 `OUTPUT` 实际更新集合上写事件 —— **零行更新不写孤立事件**。`Resubmit` 同样写入该事件（`ActionKey = Resubmit`）。

## 六、与协同事项状态机的关系

两者**结构同构**（均有一个迁移表类 + 由表派生的映射与 SQL 守卫 + 迁移必写事件），但**语义独立**：协同事项有 8 状态 / 8 动作 / 15 条迁移，业务申请有 8 状态 / 5 条纳入表的动作 / 9 条迁移。两者不共享状态词汇，也不共享动作常量（`PortalCollaborationItemActions` 与 `PortalWorkflowActions` 是两套常量）。

## 七、不在范围 / 已知缺口

- **Resubmit 的界面入口**：数据层与契约已落地，前台按钮属界面改动，需先出原型并经确认（AGENTS.md）。
- **`Submit` / `Claim` / `Withdraw` 未接入迁移表**：见第二节，如实标注为未接线，不在本轮范围。
- **后台审核动作的合法来源调整**（如 `Close` 是否应允许从 `Approved`）：属独立决策，本轮保留现状。
- **旧业务申请样板**：本状态机即 `BusinessApplicationDb`（P19 旧样板）的显式化结果，不存在"第二套业务申请状态机"。
- 本轮**不新增/删除状态**，不改变业务语义与 UI。
