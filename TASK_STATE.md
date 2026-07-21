# HIA-ASPNETPortal Task State

> 本文件是项目级持久任务账本，用于在上下文压缩、会话中断或 AI 切换后恢复真实进度。
> 不记录密码、连接串、Token、Cookie、证书私钥、生产配置或敏感截图。

## 读取规则

1. 长任务开始、上下文恢复、阶段切换或继续推进前，先读取本文件。
2. 同时核对 `git status --short`、`git -C work-zone status --short`、最新相关计划和最近验证结果。
3. 不得重复执行已标记为 `completed`、`abandoned` 或 `deferred` 的动作，除非用户明确要求复核。
4. 每完成一个可验证里程碑、改变下一步动作、出现连续失败或准备长时间暂停时，更新本文件。

## Current Goal

| 字段 | 内容 |
| --- | --- |
| 当前大周期 | `W-anp-P13` 已启动 |
| 当前阶段 | `W-anp-P13.2` 待用户确认 |
| 当前唯一下一步 | 等用户批注 `work-zone/dev/plans/W-anp-P13.2-discussion-questions.md`；若确认，则推进运维可观测、例行任务和只读运维证据包。 |
| 当前完成条件 | P13.1 发布包 manifest、部署/回滚文档、自动侧验证和实施结果已完成。 |
| 最近状态更新时间 | 2026-07-22 |

## Recent Completed Items

| 项 | 状态 | 证据 |
| --- | --- | --- |
| P10.1 合规输入与差距矩阵 | completed | `work-zone/dev/plans/W-anp-P10.1-closeout.md` |
| P10.2 安全响应头与发布环境治理 | completed | `work-zone/dev/plans/W-anp-P10.2-closeout.md` |
| P10.3 登录密码前端加密 | completed | `work-zone/dev/plans/W-anp-P10.3-login-encryption-result.md` |
| P10.3.2 注册/管理员重置口令加密与策略配置 | completed | `work-zone/dev/plans/W-anp-P10.3.2-password-entry-result.md` |
| P10.3.3 默认凭据与旧口令治理 | completed | `work-zone/dev/plans/W-anp-P10.3.3-default-credential-result.md` |
| P10.4 审计、日志、证据与例外机制 | completed | `work-zone/dev/plans/W-anp-P10.4-implementation-result.md`；证据包 `work-zone/dev/evidence/p10/20260721-032427-Dev/` |
| P10.5 周期验收与 P10 收口 | completed | `work-zone/dev/plans/W-anp-P10.5-acceptance-result.md`、`work-zone/dev/plans/W-anp-P10-closeout.md`；证据包 `work-zone/dev/evidence/p10.5/20260721-033459-Dev/` |
| 上下文恢复任务账本机制 | completed | `TASK_STATE.md`、`AGENTS.md`、`work-zone/docs/task-ledger-protocol.md` |
| P11.1 SQL Server 版本矩阵自动侧基线 | completed | `dev/scripts/Test-PortalSqlVersionMatrix.ps1`；`work-zone/dev/plans/W-anp-P11.1-static-preflight-result.md`；证据 `work-zone/dev/evidence/p11.1/` |
| P11.2 数据访问差异盘点 | completed | `dev/scripts/Get-PortalDataAccessInventory.ps1`；`work-zone/dev/plans/W-anp-P11.2-data-access-inventory.md`；证据 `work-zone/dev/evidence/p11.2/` |
| P11.3 迁移脚本与数据修复规范 | completed | `dev/scripts/Get-PortalMigrationManifest.ps1`；`work-zone/dev/plans/W-anp-P11.3-result.md`；证据 `work-zone/dev/evidence/p11.3/` |
| P11.4 HIA 外围集成契约 | completed | `dev/scripts/Get-PortalHiaIntegrationInventory.ps1`；`work-zone/dev/plans/W-anp-P11.4-result.md`；ADR `0023`；证据 `work-zone/dev/evidence/p11.4/` |
| P11.5 数据与集成验收 | completed | `work-zone/dev/plans/W-anp-P11.5-acceptance-result.md`；`work-zone/dev/plans/W-anp-P11-closeout.md`；证据 `work-zone/dev/evidence/p11.5/` |
| P12.0 入口确认 | completed | 用户确认 P12.0 推荐；`work-zone/dev/plans/W-anp-P12.md` |
| P12.1 参考项目业务盘点 | completed | `work-zone/dev/plans/W-anp-P12.1-reference-project-inventory.md`、`work-zone/dev/plans/W-anp-P12.1-business-candidate-map.md`、`work-zone/dev/plans/W-anp-P12.1-discussion-questions.md`；用户确认按推荐推进。 |
| P12.2 员工与组织资料深化当前切片 | completed | `work-zone/dev/plans/W-anp-P12.2-implementation-result.md`；业务身份静态门禁 `Pass=8; Warning=0; Fail=0; Info=0`。 |
| P12.3 轻量审批与待办基础问题清单 | completed | `work-zone/dev/plans/W-anp-P12.3-discussion-questions.md`；用户确认按推荐推进。 |
| P12.3 轻量待办当前切片 | completed | `work-zone/dev/plans/W-anp-P12.3-implementation-result.md`；静态门禁 `TotalChecks=9; FailedChecks=0; WarningChecks=0`。 |
| P12.4 业务权限与审计深化问题清单 | completed | `work-zone/dev/plans/W-anp-P12.4-discussion-questions.md`；用户确认全部按推荐推进。 |
| P12.4 业务权限与审计深化当前切片 | completed | `work-zone/dev/plans/W-anp-P12.4-implementation-result.md`；静态门禁 `TotalChecks=7; FailedChecks=0; WarningChecks=0`。 |
| P12.5 业务验收与样板场景问题清单 | completed | `work-zone/dev/plans/W-anp-P12.5-discussion-questions.md`；用户确认全部按推荐推进。 |
| P12.5 业务验收与样板场景当前切片 | completed | `work-zone/dev/plans/W-anp-P12.5-implementation-result.md`；证据包 `work-zone/dev/evidence/p12.5/20260721-202550/`，`Steps=4; Failed=0`。 |
| P12 周期收口 | completed | `work-zone/dev/plans/W-anp-P12-closeout.md` |
| P13.0 前置讨论问题 | completed | `work-zone/dev/plans/W-anp-P13.0-discussion-questions.md`；用户确认全部按推荐推进。 |
| P13 总规划 | completed | `work-zone/dev/plans/W-anp-P13.md` |
| P13.1 发布包与部署模板问题清单 | completed | `work-zone/dev/plans/W-anp-P13.1-discussion-questions.md`；用户确认全部按推荐推进。 |
| P13.1 发布包与部署模板当前切片 | completed | `work-zone/dev/plans/W-anp-P13.1-implementation-result.md`；manifest 证据 `work-zone/dev/evidence/p13.1/20260722-025435/`，`Failed=0; Warning=2`。 |
| P13.2 运维可观测与例行任务问题清单 | pending-user-review | `work-zone/dev/plans/W-anp-P13.2-discussion-questions.md` |

## Last Code State

| 仓库 | 最新已知提交 | 说明 |
| --- | --- | --- |
| 主仓库 | P13.1 发布 manifest 与公开部署文档已完成 | 已包含 `New-PortalReleaseManifest.ps1`、回滚指南、部署/测试清单和任务账本更新。 |
| WorkZone | P13.1 当前切片已完成并推进到 P13.2 讨论节点 | 已包含 P13.1 结果、P13.2 问题清单、发布 manifest 证据、P12 smoke 证据、状态和本轮日志。 |

## Last Validation Evidence

| 验证 | 结果 |
| --- | --- |
| `dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Dev` | `Pass=5; Warning=3; Fail=0; Info=1`；Warning 为历史 admin seed、本地旧默认说明、旧 MD5 兼容路径。 |
| `dev/scripts/Test-PortalComplianceBaseline.ps1 -Profile Dev` | `Pass=26; Warning=1; Fail=0; Info=2`；唯一 Warning 为旧 MD5 兼容路径。 |
| `dev/scripts/New-PortalComplianceEvidencePackage.ps1 -Profile Dev` | 通过；证据包 `work-zone/dev/evidence/p10/20260721-032427-Dev/`，3 步骤全部 `Passed`，失败数 `0`。 |
| `dev/scripts/New-PortalComplianceEvidencePackage.ps1 -Profile Dev -OutputRoot work-zone/dev/evidence/p10.5` | 通过；证据包 `work-zone/dev/evidence/p10.5/20260721-033459-Dev/`，3 步骤全部 `Passed`，失败数 `0`。 |
| `dev/scripts/Test-PortalSmoke.ps1 -StartIISExpress -StopWhenComplete -SkipAuthenticated -CheckGenericErrorPage -CheckDocumentSafety -CheckEditorSafety` | 通过；15 项检查，失败数 `0`；40001 已有 IIS Express 实例，脚本未启动也未关闭。 |
| `dev/scripts/Test-PortalPublishReadiness.ps1` | 通过；10 项检查，失败数 `0`，警告数 `0`。 |
| `dev/scripts/Test-PortalLegacyCssCompatibility.ps1` | 通过当前门禁；阻断项 `0`，IE8 视觉降级 Warning `224`。 |
| `dev/scripts/Test-PortalComplianceBaseline.ps1 -Profile Dev -BaseUrl http://localhost:40001/` | `Pass=35; Warning=1; Fail=0; Info=2`；唯一 Warning 为旧 MD5 兼容路径。 |
| 证据包敏感值复查 | 未发现实际密码、Token、Cookie、连接串或证书私钥值；快速正则命中均为 `Pass=` 摘要或安全配置键名误报。 |
| UTF-8 无 BOM 检查 | P10.3.3 相关文件均为 UTF-8 无 BOM。 |
| `dev/scripts/Test-PortalPublicDocumentation.ps1` | 通过；12 个公开文档均已登记到 `docs/README.md`。 |
| P10 阶段关键短语复查 | 无 `P10.4=多角色`、`P10 主轴待讨论`、`当前进入 P10.3`、`当前 P4.1` 等误导性残留命中。 |
| `dev/scripts/Test-PortalSqlVersionMatrix.ps1` 静态预检 | `Pass=11; Warning=1; Fail=0; Info=1; Pending=4`；Warning 为历史 SQL 登录授权脚本。 |
| `dev/scripts/Test-PortalSqlVersionMatrix.ps1 -ConnectionStringsConfigPath {test config}` | `Pass=14; Warning=1; Fail=0; Info=1; Pending=3`；本机仅补得 SQL Server 2022 元数据证据。 |
| `dev/scripts/Test-PortalSqlCompatibility.ps1 -RequireP2...P6...` 只读检查 | SQL Server 2022 元数据通过；当前 test 库缺少 P5/P6 后续表，不能作为“最新迁移完成库”证据。 |
| `dev/scripts/Get-PortalDataAccessInventory.ps1 -OutputJson work-zone/dev/evidence/p11.2/data-access-inventory-20260721-043622.json` | 扫描 384 个已追踪源文件；`SqlServerOnly=48 files`、`NeedsDialect=11 files`、`PortableCandidate=16 files`、`ProviderProof=6 files`。 |
| `dev/scripts/Get-PortalMigrationManifest.ps1 -OutputJson work-zone/dev/evidence/p11.3/migration-manifest-20260721-045026.json` | `Pass=4; Warning=2; Fail=0; Info=1`；21 个已追踪 SQL 文件全部纳入 manifest。 |
| `dev/scripts/Test-PortalSqlCompatibility.ps1 -ApplyP5... -ApplyP6... -Require...` | 对外置 `test` 配置指向的隔离测试库执行，P5/P6 schema 补齐成功；21 项检查，失败数 `0`。 |
| `dev/scripts/Test-PortalSqlCompatibility.ps1 -Require...` | P11.3 写入后只读复核通过；15 项检查，失败数 `0`。 |
| `dev/scripts/Get-PortalHiaIntegrationInventory.ps1 -OutputJson work-zone/dev/evidence/p11.4/hia-integration-inventory-20260721-052833.json` | `Pass=9; Warning=0; Fail=0; Info=0; Pending=0`；确认 HIA 契约、proof、draft fixture、通知读取和隐私边界。 |
| `dev/scripts/Test-PortalHiaBoundary.ps1 -Configuration Debug` | 通过；10 项 HIA boundary fixture proof 全部 `PASS`，P11.4 draft fixtures 未进入当前运行时验证器接受清单。 |
| `dev/scripts/Test-PortalSqlVersionMatrix.ps1 -OutputJson work-zone/dev/evidence/p11.5/sql-version-matrix-20260721-115503.json` | `Pass=11; Warning=1; Fail=0; Info=1; Pending=4`；Warning 为 legacy grant 脚本，Pending 为本轮未提供真实 SQL Server 实例。 |
| `dev/scripts/Get-PortalDataAccessInventory.ps1 -OutputJson work-zone/dev/evidence/p11.5/data-access-inventory-20260721-115503.json` | 扫描 384 个已追踪源文件；`SqlServerOnly=48 files`、`NeedsDialect=11 files`、`PortableCandidate=16 files`、`ProviderProof=6 files`。 |
| `dev/scripts/Get-PortalMigrationManifest.ps1 -OutputJson work-zone/dev/evidence/p11.5/migration-manifest-20260721-115503.json` | `Pass=4; Warning=2; Fail=0; Info=1`；21 个已追踪 SQL 文件全部纳入 manifest。 |
| `dev/scripts/Get-PortalHiaIntegrationInventory.ps1 -OutputJson work-zone/dev/evidence/p11.5/hia-integration-inventory-20260721-115503.json` | `Pass=9; Warning=0; Fail=0; Info=0; Pending=0`。 |
| WSF 参考项目只读盘点 | 已形成脱敏结构证据 `work-zone/dev/evidence/p12.1/wsf-reference-inventory-20260721-122902.json`；确认 `petroleum-sys`、`petroleum-scientificresearch` 为 P12 主要参考源。 |
| `dev/scripts/Test-PortalBusinessIdentity.ps1 -OutputJson work-zone/dev/evidence/p12.2/business-identity-static-20260721-133737.json` | `Pass=8; Warning=0; Fail=0; Info=0`；确认工号登录标识、资料字段、员工主数据和账号员工绑定关键契约。 |
| `dev/scripts/Build-Solution.ps1` | 通过；存在既有 `CS1591` XML 注释警告，无编译错误。 |
| `dev/scripts/Test-PortalWorkItemSmoke.ps1 -OutputJson work-zone/dev/evidence/p12.3/work-item-static-20260721-144000.json` | `TotalChecks=9; FailedChecks=0; WarningChecks=0`；确认 P12.3 SQL、契约、Unity、项目文件、权限、后台页、业务同步点和迁移工具。 |
| `dev/scripts/Get-PortalMigrationManifest.ps1 -OutputJson work-zone/dev/evidence/p12.3/migration-manifest-20260721-145100.json` | `Pass=4; Warning=2; Fail=0; Info=1`；Warning 为既有 legacy grant/security seed review。 |
| `dev/scripts/Test-PortalSqlVersionMatrix.ps1 -OutputJson work-zone/dev/evidence/p12.3/sql-version-matrix-20260721-145100.json` | `Pass=11; Warning=1; Fail=0; Info=1; Pending=4`；Warning 为 legacy grant 脚本，Pending 为本轮未提供真实 SQL Server 目标实例。 |
| `dev/scripts/Build-Solution.ps1` | P12.3 后复跑通过；仅保留既有 XML 注释警告和 `Roles.ModulesConfig` 隐藏警告。 |
| `dev/scripts/Test-PortalBusinessPermissionAudit.ps1 -OutputJson work-zone/dev/evidence/p12.4/business-permission-audit-static-20260721-1610.json` | `TotalChecks=7; FailedChecks=0; WarningChecks=0`；确认 P12.4 业务权限、Admin seed、页面门禁、待办分派和审计事件目录。 |
| `dev/scripts/Test-PortalWorkItemSmoke.ps1 -OutputJson work-zone/dev/evidence/p12.4/work-item-static-20260721-1610.json` | `TotalChecks=9; FailedChecks=0; WarningChecks=0`；确认 P12.3 待办静态门禁在 P12.4 权限拆分后仍通过。 |
| `dev/scripts/Build-Solution.ps1` | P12.4 后复跑通过；仅保留既有 XML 注释警告，无编译错误。 |
| `dev/scripts/New-PortalP12SampleScenarioSql.ps1 -OutputPath temp/p12.5/PortalP12SampleScenario.sql` | 通过；仅生成开发/测试 SQL 文件，不连接数据库、不创建用户、不写密码。 |
| `dev/scripts/New-PortalP12EvidencePackage.ps1 -OutputRoot work-zone/dev/evidence/p12.5` | 通过；证据包 `work-zone/dev/evidence/p12.5/20260721-202550/`，P12.2、P12.3、P12.4 门禁和解决方案构建全部 `Passed`，失败数 `0`。 |
| `dev/scripts/Publish-PortalFileSystem.ps1 -Configuration Release -PublishPath temp/publish/P13.1-Release-20260722-025022` | 通过；发布前门禁 `Failed=0; Warning=0`，发布后二次门禁 `Failed=0; Warning=0`；构建保留既有 `Roles.ModulesConfig` 隐藏警告。 |
| `dev/scripts/New-PortalReleaseManifest.ps1 -PackagePath temp/publish/P13.1-Release-20260722-025022 -OutputRoot work-zone/dev/evidence/p13.1` | 通过；manifest 证据 `work-zone/dev/evidence/p13.1/20260722-025435/`，`Files=155; Failed=0; Warning=2`。 |
| `dev/scripts/Test-PortalPublicDocumentation.ps1` | 通过；13 个公开文档已登记，无私有链接和敏感赋值。 |
| `dev/scripts/New-PortalP12EvidencePackage.ps1 -OutputRoot work-zone/dev/evidence/p13.1/p12-acceptance-smoke -SkipBuild` | 通过；`Steps=3; Failed=0`。 |

## Known Residual Working Tree Items

这些项在多轮任务中已作为既有残留保留，除非用户明确要求，不纳入普通阶段提交：

1. 主仓库：`.vscode/settings.json`、生成文档目录、`src/Portal/Uploads/sample-under-10mb.json`、`src/Portal/css/`、`src/Portal/js/`、`temp/` 等。
2. WorkZone：历史 2026-07-13/14/15 日志、P7 截图研究目录和一份旧日志修改。

## Failed Or Risky Attempts

| 动作 | 状态 | 处理 |
| --- | --- | --- |
| 用双引号包裹 `pwsh -Command` 且内部包含 `$p`、`$null` | failed | 外层 PowerShell 会提前展开变量；后续统一用单引号包裹 `-Command` 或避免内部 `$` 变量。 |
| 全量 `rg` 默认口令扫描一次输出过大 | adjusted | 改为分阶段、限量、聚焦文件范围的扫描。 |

## Anti-Loop Guard

| 指标 | 当前值 |
| --- | --- |
| 连续无新证据次数 | 0 |
| 最近重复失败动作 | 无 |
| 熔断规则 | 同一命令/方案重复失败 2 次，或连续 2 轮没有新代码、测试结果、文档证据时，暂停并报告。 |
