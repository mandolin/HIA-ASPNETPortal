# 更新日志

本项目变更记录。版本规则见 [版本策略](docs/versioning.md)。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [SemVer](https://semver.org/lang/zh-CN/)。

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
