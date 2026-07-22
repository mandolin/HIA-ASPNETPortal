# 开发工具入口

根级 `dev/` 只保留可供项目开发者直接使用的公共脚本和说明。

## 当前内容

- `scripts/`：NuGet 还原、解决方案构建和 IIS Express 启停脚本。
- `documentation/jsdoc/`：独立的 HIA JSDoc pilot 配置、依赖锁定和输出验证器。
- `notify/`：历史通知快照。2026-07-19 起不再作为 HIA-Documentation-Sys 的新通知投递入口。

## 常用发布检查脚本

```powershell
dev/scripts/Test-PortalPublishReadiness.ps1
dev/scripts/Publish-PortalFileSystem.ps1
dev/scripts/New-PortalReleaseManifest.ps1
```

`Test-PortalPublishReadiness.ps1` 只读检查项目发布清单、主题包、模块包和可选发布输出目录。`Publish-PortalFileSystem.ps1` 发布到 `temp/publish/` 下的临时目录，并在发布前后执行门禁；它不修改 IIS、数据库或外置配置。`New-PortalReleaseManifest.ps1` 对 FileSystem 发布输出生成文件清单、SHA256、版本信息和排除项检查；它只记录路径和哈希，不写入真实敏感值。

## 运维证据与例行任务

```powershell
dev/scripts/Test-PortalOperationsReadiness.ps1
dev/scripts/Test-PortalLogMaintenance.ps1
dev/scripts/New-PortalOperationsEvidencePackage.ps1
```

`Test-PortalOperationsReadiness.ps1` 只读检查运维页面、诊断日志、运营审计、公开运行手册和目标环境补证边界。`Test-PortalLogMaintenance.ps1` 只做诊断日志保留策略 dry-run，不删除、移动、压缩或读取日志正文。`New-PortalOperationsEvidencePackage.ps1` 编排运维 readiness、日志 dry-run、发布、公开文档、合规和默认凭据风险门禁，证据默认写入 WorkZone。

## VM 任务代理

```powershell
dev/scripts/New-PortalVmTaskAgentPackage.ps1
dev/scripts/New-PortalVmAgentTask.ps1
```

`New-PortalVmTaskAgentPackage.ps1` 生成 Win7 VM 可运行的轮询型任务代理。`New-PortalVmAgentTask.ps1` 用于向代理目录投递 `tasks/*.task.cmd`，让 VM 内的代理自动执行并回写日志和结果。

内部开发状态、路线图、阶段计划、任务与修复记录已经迁移到独立私有仓库 `work-zone/dev/`，不进入公开项目仓库。

## HIA 文档化通知

HIA-Documentation-Sys 的目标项目通知机制已改为目标项目主动读取。查看通知时使用：

```powershell
dev/scripts/Get-HiaDocumentationNotifications.ps1
```

默认读取同级 `../HIA-Documentation-Sys/work-zone/notify/`。如本机目录不同，可传入 `-HiaDocumentationRoot`。

需要盘点门户自身 HIA 外围契约、proof、draft fixtures 与通知读取边界时使用：

```powershell
dev/scripts/Get-PortalHiaIntegrationInventory.ps1
```

该脚本只读，不连接数据库、不加载 HIA 外部程序集，也不复制通知正文。

## 文档化 readiness 与证据包

```powershell
dev/scripts/Test-PortalDocumentationReadiness.ps1
dev/scripts/New-PortalDocumentationEvidencePackage.ps1
```

`Test-PortalDocumentationReadiness.ps1` 只读检查公开文档化指南、XML 文档边界、JSDoc pilot、生成目录边界、coverage 分层和 HIA 通知读取机制。`New-PortalDocumentationEvidencePackage.ps1` 编排文档化 readiness、baseline、公开文档门禁、XML 文档验证、JSDoc pilot 和 HIA 通知读取，证据默认写入 WorkZone。

## 业务身份门禁

P12.2 起，员工号登录、用户资料、员工主数据和账号员工绑定的静态契约可用以下脚本检查：

```powershell
dev/scripts/Test-PortalBusinessIdentity.ps1
```

该脚本只读，不连接数据库、不读取密码、不修改外置配置。

## 轻量待办门禁

P12.3 起，轻量审批/待办基础的 SQL、契约、Unity 注册、后台入口和业务同步点可用以下脚本检查：

```powershell
dev/scripts/Test-PortalWorkItemSmoke.ps1
```

该脚本只读，不连接数据库、不执行迁移，用于避免待办骨架在后续业务模块接入时被拆断。

## 业务权限与审计门禁

P12.4 起，业务权限拆分、Admin 兼容 seed、页面授权入口、待办分派键和关键运营审计调用可用以下脚本检查：

```powershell
dev/scripts/Test-PortalBusinessPermissionAudit.ps1
```

该脚本只读，不连接数据库、不执行迁移，用于避免新增业务页绕过权限、审计或合规边界。

## P12 验收与样例数据

P12.5 起，业务身份、轻量待办、业务权限审计和解决方案构建可编排为一个验收证据包：

```powershell
dev/scripts/New-PortalP12EvidencePackage.ps1
```

需要准备员工资料更正样板路径的开发/测试数据时，可先生成 SQL 草案：

```powershell
dev/scripts/New-PortalP12SampleScenarioSql.ps1
```

该 SQL 生成脚本不连接数据库、不创建用户、不写密码；生成的 SQL 只能在开发库或测试库中经人工确认后执行。
