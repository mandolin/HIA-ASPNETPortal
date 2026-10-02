# 更新日志

本项目变更记录。版本规则见 [版本策略](docs/versioning.md)。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [SemVer](https://semver.org/lang/zh-CN/)。

## [v0.4.0] - 2026-10-02

**BasicBusiness（基础业务）能力初步完善**（`C-anp-P12`：W67–W75）。

### 已加入

- **W67 `WorkItems` 前台「我的待办」聚合入口 + 操作审计**：数据层 `PortalWorkItemQueryResult` 与 `IPortalWorkItemDb.GetWorkItemsForUser`（fail-closed、空/失败语义分离、角色键逐参数绑定）；前台模块 `HIA.MyWorkItems`；查看与筛选操作审计。
- **W68 `EnterpriseDirectory` 组织子树查询 + 异常语义修正**：`EmployeeDirectoryQuery` 增加 `OrganizationUnitId`（fail-closed）与 `IncludeDescendants`；`IEmployeeDirectoryDb.GetOrganizationUnitSubtreeIds`；纯函数 `PortalOrganizationTreeExpander`（数据范围深化的数据层前置）。
- **W69 `CorrectionRequest` 闭环（审核 → 回写主数据）**：审核通过按服务端白名单真正回写员工主数据（此前只改状态），含回写策略纯函数与事件/审计；按裁定 `D4` **不引入二次审批**。
- **W70 `EmployeeProfileConfirm` 前台权限 + 幂等 + 本地化**：渲染期权限门禁 + 提交前二次校验 + 授权失败审计；确认由"快照追加"改为幂等；清理未本地化硬编码。
- **W71 `ApplicationRequest` 状态机显式化 + Resubmit**：显式迁移表作为单一事实源（含 `ViaReviewPath` 隔离自助动作），映射与 SQL 守卫均由表派生，并附**等价性单测**；新增 `ResubmitApplication`。
- **W72 负责人角色键读取边界对齐**：修复 `CanParticipate`（写资格）与 `CanView`（读资格）不一致，持有负责人角色键须两者同时通过。
- **W73 治理**：`Business.Workflow` 归属合并到 `BusinessApplicationRequest`（`D1`）；`EmployeeProfileConfirm` / `EmployeeProfileCorrectionRequest` 补 `capabilityId`，`ModuleProbe` 明确归 Platform（`D2`）。

### 已改进

- **W74 UI 打磨（空态 / 资源键 / 角色名 / N+1）**：
  - **列表空态**：业务前后台共 **8 个列表**接入共享空态（表头保留 + 整行居中弱化提示），并区分"无数据"与"筛选无结果"；服务不可用路径**抑制空态**，不把故障说成"暂无"。
  - **资源键归属**：消除跨模块借用（借用方建自有键；角色词表等共享领域事实改用共享键），清理失效键。
  - **参与人角色名本地化**：界面不再暴露内部权限键（`Collaborator` / `Watcher`），改为本地化角色名，未知键回退原始键；空集合用本地化占位。
  - **N+1**：协同事项列表改为批量取数（逐行约 4N 次往返 → 常数次），**逐条可见性校验保留**。
  - **主题适配**：`MyWorkItems` 与工作台在 6 套正式皮肤补齐主题作用域规则，修复深色皮肤下"浅字压白底"（实测对比度由约 1:1 提升到 12.9:1）。
  - **模块注册**：通用场景脚本 `New-PortalModuleScenarioSql.ps1`；`HIA.MyWorkItems` 补齐模块定义/包状态/页签/实例，并归入新增 `BasicBusiness` 档位。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`）：R1 随本周期推进（BasicBusiness 七方向达"初步完善"）、R7 达成；R2/R3/R4/R5 部分达成；R6 未达成 |

### 说明

- 本版本为**内部基线锚点**，不对外宣传；`v1.0.0` 之前口径限于"可用 / 参考 / 研究基线"，不得宣称生产级可信。
- 本周期起将**运行期验证**（LocalDB + IIS Express + 浏览器断言）纳入常规门禁，并新增 `Test-PortalModuleRuntimeEvidence.mjs` 与 `Test-PortalAdminListUiEvidence.mjs` 两个证据脚本；它当场发现了静态门禁看不到的问题（ASCX 运行期编译错误、深色皮肤可读性）。

## [v0.3.0] - 2026-09-28

**Foundation（核心）能力初步完善**（`C-anp-P11`：W61–W66）。

### 已加入

- **W61 密码成本与版本语义**：PBKDF2 迭代下限 `210000 → 600000`（依据 OWASP Password Storage Cheat Sheet，目标环境实测 407 ms < 1 s）；`minimumPortalVersion` 语义在 `docs/versioning.md` 第八节定义（模块对宿主门户的兼容性声明，与产品版本号是两个独立维度）；上传白名单移除 `zip`（OWASP 不建议）。
- **W62 审计补齐**：审计门面支持 `Outcome` 参数化；**认证失败**入审计（`Signin.ascx.cs` 失败分支）；**授权失败**入审计（`PortalNavigationPolicy` 两个集中拒绝出口）——补齐 OWASP Logging Cheat Sheet 要求的必须事件类型；新增保留期策略 `docs/audit-retention-policy.md`。
- **W63 数据范围契约**：新增 `CollaborationItemDataScope` 与 `CollaborationItemDataScopePolicy.CanView`（判定顺序 管理员 → 归属 → 参与人 → 组织，**fail-closed**）；列表与详情共用同一判定且由服务端强制；既有 `CanParticipate` 语义未改变；**新增 10 例单测**。
- **W64 状态机显式化**：新增 `PortalCollaborationItemTransitions`（8 状态 / 8 动作 / 15 条迁移的单一事实源）；`MapActionToStatus` 与 SQL 写入守卫谓词均由该表派生，消除映射漂移；`ApplyAction` 新增 `IsLegalTransition` 服务端门禁；迁移必写 `WorkflowAction` 事件（`FromStatus`/`ToStatus` 留痕）；新增状态流转说明 `docs/collaboration-item-state-machine.md`；**新增 6 例单测**。
- **W65 Watcher 语义修正**：`Watcher` / `Collaborator` 描述与"待办即通知"定位对齐，消除"关注者仅接收通知"语义与实际无通知发送之间的落差；全仓确证协同事项无独立通知发送。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`）：**Foundation 侧已达初步完善**，BasicBusiness 侧待 `C-anp-P12` 执行（见 `work-zone/dev/milestones/M-ANP-RELEASE-READY-PORTAL.md`） |

### 说明

- 本版本为**内部基线锚点**，不对外宣传；`v1.0.0` 之前口径限于"可用 / 参考 / 研究基线"。
- 质量门禁：构建 `0 错 0 警`、单测 `86/86`、XML 文档门禁通过。
- `C-anp-P11` 的规划产出（`W66` closeout 与 `C-anp-P12` 候选蓝图）属内部计划，见 `work-zone/`。

## [v0.2.0] - 2026-09-28

**版本机制 + 两大能力层盘点与深度对标**（`C-anp-P10`：W56–W60）。

### 已加入

- **W56 版本号机制建立**：`docs/versioning.md` 版本策略（SemVer + 版本与程序集联动 + 发行流程 + 宣传口径约束 + `minimumPortalVersion` 语义）、`CHANGELOG.md`、`git tag v0.1.0`（已推送）、一致性门禁 `dev/scripts/Test-PortalVersionConsistency.ps1`，5 个程序集版本联动。
- **W57 Foundation 能力盘点与深度对标**：八个条目（身份账户、组织拓扑、授权与数据范围、流程编排、消息、文件、审计、集成标识）逐一给出现状 / 缺口 / 对标 / 建议，产出 6 份研究文档并登记来源强度与不适用清单；并产出 `C-anp-P11` 候选蓝图。
- **W58 BasicBusiness 能力盘点与对标**：七能力方向盘点（能力登记 2 条、孤儿模块 3 个、**业务单测覆盖 1/7**、Top 共性缺口 8 项）+ 外部对标（转引本项目已核实的一手来源）+ `C-anp-P12` 候选清单定稿为 P1/P2/P3。
- **W59 细节级对标规范**：`docs/detail-level-benchmark-spec.md`（八维度 + 来源强度标注 + 不适用清单 + 验收检查表），供 P11–P13 复用。
- **W60 P10 收口**：closeout、建立 L10 里程碑文档 `M-ANP-RELEASE-READY-PORTAL` 并登记七条判据与基线证据、本版本 tag。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`；**本 Cycle 新登记**，判据与基线证据见 `work-zone/dev/milestones/M-ANP-RELEASE-READY-PORTAL.md`） |

### 说明

- 本版本为**内部基线锚点**，不对外宣传；`v1.0.0` 之前口径限于"可用 / 参考 / 研究基线"，不得宣称生产级可信。
- `C-anp-P11`（Foundation 能力初步完善）的源码成果已合入，其版本锚点 `v0.3.0` 按推荐序列作为**后续发行动作**推进（不在本版本内）。

## [v0.1.0] - 2026-09-25

**机制闭环基线**（`C-anp-P9`：企业能力机制闭环深化与边界确证，W50–W54）。

### 已加入

- **W50 模块装配双模式原型验证**：本机确证 `CodeBehind`/`CodeFile` 精确运行时语义与 WAP 内用 `CodeFile` 的后果、C1-CMS 切换细节；裁定"实现另立项，不碰在线编译 / 在线上传"。
- **W51 第二能力样板复制验证**：`HIA.BusinessApplicationRequest` 补 `manifest v2` `capability` 段，能力词表注册 `BasicBusiness.ApplicationRequest`，确认 `BusinessWorkflow` Profile gate；验证能力归属机制可复制（构建 0 错 0 警、单测 70/70）。
- **W52 能力管理独立页复核**：维持不建独立页，保持 `Admin/CapabilityPermissionMatrix.aspx` 只读诊断视图（能力 Primary 样板 2 块 < 阈值 ≥5）。
- **W53 边界治理端到端验证**：`EnterpriseCapability.*` 分层键族、Profile gate、装配态迁移三块边界受控验证；零代码改动。
- **W54 W44/W45 残留收尾**：
  - `Admin/CollaborationItems.aspx` 参与人角色下拉两处硬编码显示名本地化（新增 `Admin_CollaborationItems_RoleType_Collaborator/Watcher` 三语资源键 + ROP 注释）；
  - 英文侧 gate 开态截图 46 张自动补采并归档（LocalDB + IIS Express + Chrome headless），UI 文本级验证 `ParticipantRoleList` = `Collaborator`/`Watcher`；关闭 W44.5/W45.5 长期登记的"英文侧 gate 开态未采"已知边界。

### 已修复

- 账目一致性：`TASK_STATE` 的 Current Goal 字段长期停留在旧周期（`C-anp-P6`/`W35`），已更新至 `C-anp-P9`/`W55` 并登记下一周期组 `C-anp-P10`。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`） |

### 说明

- 本版本为**内部基线锚点**，不对外宣传。
- 能力词表当前登记 2 条（`BasicBusiness.Collaboration`、`BasicBusiness.ApplicationRequest`），均属 `BasicBusiness` 层。
