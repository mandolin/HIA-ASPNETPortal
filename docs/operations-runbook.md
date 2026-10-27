# 运维运行手册

本手册面向部署和维护人员，说明当前门户可用的运维入口、例行检查、证据留存和延期补证项。它不包含真实连接串、密码、Token、Cookie、证书私钥或生产配置。

## 运维入口

| 入口 | 用途 | 权限边界 |
| --- | --- | --- |
| `Admin/SystemHealth.aspx` | 查看系统健康、设置 registry、日志目录、上传目录和数据库轻量连接状态。 | 当前仅 `Admins` 可访问。 |
| `Admin/DiagnosticsLogs.aspx` | 查询结构化诊断日志列表。 | 当前仅 `Admins` 可访问；详情受部署级开关控制。 |
| `Admin/OperationAudits.aspx` | 查询关键运营审计记录。 | 当前仅 `Admins` 可访问；本阶段不提供导出下载。 |
| `Admin/ThemeSettings.aspx` | 查看和调整受信任主题选择。 | 当前仅 `Admins` 可访问。 |
| `Admin/ModuleCatalog.aspx` | 查看和启停受信任部署模块包。 | 当前仅 `Admins` 可访问。 |

管理员页面只作为运行期查看和有限维护入口，不替代目标 IIS、SQL Server、备份系统和企业监控平台的正式运维控制台。

## 每日检查

1. 打开 `Admin/SystemHealth.aspx`，确认整体状态不是 `Error`，并记录需要目标环境补证的 `Pending` 项。
2. 打开 `Admin/DiagnosticsLogs.aspx`，查看最近错误和警告；遇到通用错误页事件编号时，用事件编号反查。
3. 打开 `Admin/OperationAudits.aspx`，抽查注册、审核、角色、资料更正、待办等关键业务审计记录。
4. 确认站点根目录、`App_Data/Logs`、上传目录和外置配置目录权限符合最小写入要求。

## 每周检查

1. 执行运维 readiness：

```powershell
dev/scripts/Test-PortalOperationsReadiness.ps1
```

2. 执行诊断日志保留策略 dry-run：

```powershell
dev/scripts/Test-PortalLogMaintenance.ps1
```

3. 查看 dry-run 输出中的 `RetentionCandidates` 和 `UnmanagedFiles`。脚本不会删除日志，生产清理前应先确认备份、保留天数、审计要求和变更窗口。
4. 检查上传目录是否存在异常扩展名、异常大小文件或越权访问迹象。

## 发布前和发布后

发布前至少执行：

```powershell
dev/scripts/Test-PortalPublishReadiness.ps1
dev/scripts/Test-PortalPublicDocumentation.ps1
dev/scripts/Test-PortalComplianceBaseline.ps1 -Profile Test
dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Test
```

需要留存 运维证据时执行：

```powershell
dev/scripts/New-PortalOperationsEvidencePackage.ps1 -Profile Test
```

公开交付文档只引用脱敏摘要，不引用真实配置或生产截图。
发布后线上复核由发布工作流**自动执行**，也可随时手动复跑：

```powershell
dev/scripts/Test-PortalPublishedSiteChecks.ps1
```

它逐页核对四项判据：页面可达（HTTP 200）、标题与本地构建产物一致、导航可见（页面 HTML 内能链到它）、页面内不含内部编号。CI 侧带 `-WaitSeconds 180` 等待 GitHub Pages 传播，并配 `continue-on-error`——**复核失败只报警，不阻断发布**。

退出码含义：`0` 全部通过；`1` 发现缺陷（页面已发布，需修复后重新发布）；`2` 未确认（等待传播超时，**不当作通过**，加大 `-WaitSeconds` 后重试）。

传播是否完成，看的是**构建戳**而不是"页面能打开"：构建会把源文档指纹写进每页末尾的 HTML 注释，复核时读同一个源文件复算同一个哈希，一致才算"线上是这一次构建的产物"。只看"能打开"是不够的——旧页面在传播完成前**一直打得开**（它承载上一次构建的内容），于是"旧产物还在"会被误读成"已传播"。戳对不上记为"未确认"而不是缺陷，且只在带等待窗口的复核中判定：随手复核时本地常有尚未发布的在改内容，判它就是制造噪声。

> 为什么需要这一步：本地门禁全绿只代表"构建产物没问题"，不代表"发布出去的页面没问题"。

> 想确认这套判据本身还有效，可以**本地一键复跑注入轨**（不动线上、不需要网络）：dev/scripts/Invoke-PortalPublishedSiteCheckInjection.ps1——它起一个本地静态服务承载模拟发布页，逐项注入缺陷并断言门禁真的会发现。模板注释、镜像一致性、传播延迟都可能让线上与本地不一致——这一步专门盯发布后的真实产物。

## 日志维护

当前结构化诊断日志由 `PortalDiagnostics` 写入 UTF-8 无 BOM NDJSON 文件，默认目录为 `App_Data/Logs`。默认保留天数为 90 天，默认单文件大小上限为 10 MiB；达到上限后按日期和序号滚动。

`Test-PortalLogMaintenance.ps1` 只做 dry-run：

1. 它按 `portal-yyyyMMdd-nnn.jsonl` 识别受管理日志。
2. 它列出超过保留天数的候选文件。
3. 它列出目录中的非受管理文件，提醒人工复核。
4. 它不读取日志正文，不删除、移动、压缩或归档任何文件。

生产环境如需自动清理，应先建立企业级备份和审计策略，再考虑通过系统设置或部署级任务开启。

## 审计查询

`Admin/OperationAudits.aspx` 当前用于在线查询运营审计，不提供导出下载。审计记录应重点覆盖：

1. 注册、审核和账号状态变化。
2. 用户角色、权限和业务角色相关变化。
3. 员工资料、组织、员工绑定和资料更正。
4. 待办、审批动作和业务结果。

如后续增加导出能力，需要同步设计脱敏、下载权限、导出审计和保留期限。

## 数据库备份

本项目脚本不自动执行数据库备份。部署前、迁移前和重大配置调整前，运维人员应在目标 SQL Server 或企业备份平台完成：

1. 全库备份或符合企业策略的等效备份。
2. 备份文件保留位置和保留期限记录。
3. 恢复演练或至少恢复责任人确认。
4. 备份、迁移、验证和回滚窗口记录。

## 告警扩展点

当前阶段不接入邮件、IM 或 Webhook。建议先把以下事件作为未来告警扩展点：

1. `PortalDiagnostics` 中的 `Error` 和持续性 `Warning`。
2. `SystemHealth` 中数据库、日志目录、上传目录和配置 registry 的异常状态。
3. 默认凭据、旧 MD5 兼容路径和安全 header 门禁异常。
4. 运营审计写入失败或审计表缺失。

后续接入企业平台时，需要补告警去重、频率限制、脱敏、确认和恢复策略。

## Windows 计划任务建议

如目标机器采用 Windows Task Scheduler，只建议创建调用项目脚本的只读任务，并把任务配置纳入部署记录。当前仓库不会自动创建计划任务。

建议任务类型：

| 任务 | 频率 | 命令 |
| --- | --- | --- |
| 运维 readiness | 每日或每周 | `dev/scripts/Test-PortalOperationsReadiness.ps1` |
| 日志维护 dry-run | 每周 | `dev/scripts/Test-PortalLogMaintenance.ps1` |
| 合规基线 | 发布前或每月 | `dev/scripts/Test-PortalComplianceBaseline.ps1 -Profile Scan` |
| 默认凭据风险 | 发布前 | `dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Prod` |

计划任务运行身份应只具备读取仓库、读取目标日志目录和写入证据目录的最小权限。不要把真实密码写进任务命令、脚本文本或证据包。

## 目标环境补证

以下事项必须在目标环境补证，开发机或 IIS Express 结果不能替代：

1. 真实 IIS 站点、虚拟目录、应用池身份、管道模式和 .NET Framework 4.8。
2. TLS、证书、HSTS、Cookie Secure、SameSite 和企业扫描 profile。
3. 站点目录、`App_Data/Logs`、上传目录、外置配置目录和临时目录 ACL。
4. SQL Server 版本、兼容级别、备份任务、恢复演练和最小权限账号。
5. 磁盘空间、日志保留、监控、告警和企业审计平台对接。

## L7 目标环境证据（一键证据包）

安全可信门户里程碑 `M-ANP-TRUSTED-PORTAL`（L7）所需的环境触发型证据，已登记成机器可核对的清单，并可用**一条命令**出证：

```powershell
& 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'
```

- **清单**：`dev/target-environment-evidence.json`——逐项登记判据归属（T1–T7 与阻断项）、能否在本机出证、生成脚本、前置条件、预期产物与再入条件。
- **三种模式**：`local`（立即出证）、`targetEnv`（需目标环境，**显式跳过并说明缺失的前置条件**，绝不静默通过）、`manual`（人工产物，如业务签收）。
- **目标环境就绪后**：同一条命令加上 `-BaseUrl <目标终结点>` 即可把环境项一并出证；产物为带时间戳的证据目录（`manifest.json` + 各步骤日志 + `summary.md`）。
- **跳过不是通过**：摘要会逐条列出"尚未取得目标环境证据"的项；自 `v2.0.0` 起上述阻断项已全部出证并登记（真实 IIS/TLS/ACL、SQL Server 版本矩阵、企业扫描基线、IE9+ 真实复测、ADR 0021 目标环境实证、业务签收均已出证）。口径限制如实保留：企业扫描出证形态为项目授权的**离线只读基线**，实时 DAST / 网络扫描与绿盟、等保商用正式报告按 2026-07-22 调研递延为发布后项；SQL Server 2016 / 2017 未用各自独立隔离实例分别实测，而是按兼容级别 130 / 140 / 150 的裁定视为覆盖。
- 清单本身由 L0 门禁 `Test-PortalTargetEnvironmentEvidenceMap.ps1` 看守：条目缺字段、脚本不存在、或判据/阻断项漏登记都会失败。

## 证据包解释

`New-PortalOperationsEvidencePackage.ps1` 会把只读脚本输出编排成证据包。证据包中的 `Pending` 表示需要目标环境或人工材料补齐，不等于失败；`Fail` 表示当前仓库或公开文档存在需要先处理的问题。
