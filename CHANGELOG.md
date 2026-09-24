# 更新日志

本项目变更记录。版本规则见 [版本策略](docs/versioning.md)。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [SemVer](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### 进行中

- `W-anp-P55`（C-anp-P9 closeout 与能力机制成熟度证据更新）：`P55.0` 盘点与战略头脑风暴备忘、`P55.3` 下一阶段建议定稿已完成；`P55.1` 实治汇总、`P55.2` 里程碑证据更新待完成。
- `W-anp-P56`（版本号机制建立）：版本策略文档与 `CHANGELOG.md` 建立中。

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
