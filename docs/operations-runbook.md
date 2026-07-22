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

需要留存 P13.2 运维证据时执行：

```powershell
dev/scripts/New-PortalOperationsEvidencePackage.ps1 -Profile Test
```

证据包默认写入 `work-zone/dev/evidence/p13.2/`。公开交付文档只引用脱敏摘要，不把 WorkZone 证据、真实配置或生产截图放入根仓库。

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

## 证据包解释

`New-PortalOperationsEvidencePackage.ps1` 会把只读脚本输出编排成证据包。证据包中的 `Pending` 表示需要目标环境或人工材料补齐，不等于失败；`Fail` 表示当前仓库或公开文档存在需要先处理的问题。
