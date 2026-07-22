# 发布与兼容检查清单

本清单用于 .NET Framework Web Forms 门户的测试和生产发布准备。它不替代真实 IIS 环境中的人工复测，也不授权自动修改 IIS、应用池、数据库或外置配置。

## 发布前边界

- [ ] 目标机器已安装 .NET Framework 4.8；缺少 4.8 时不得部署 P5.2+ 版本。
- [ ] 已确认本次发布 profile：Dev、Test、Prod、Scan 或 LegacyIe，并记录 profile 与 Web.config transform 的对应关系。
- [ ] 已运行 `dev/scripts/Test-PortalPublishReadiness.ps1`，项目 Content 清单、主题包和模块包检查通过。
- [ ] 如使用本地文件系统发布包，已运行 `dev/scripts/Publish-PortalFileSystem.ps1`，并对发布输出目录执行二次门禁。
- [ ] 已对 FileSystem 发布输出运行 `dev/scripts/New-PortalReleaseManifest.ps1 -PackagePath <发布目录>`，生成发布包 manifest。
- [ ] 发布包 manifest 确认没有 `work-zone/`、`temp/`、`dev/`、`src/`、上传目录业务文件、真实配置、Web.config transform 或证书私钥文件；`Uploads/web.config` 作为安全配置文件允许随包发布。
- [ ] 已运行 `dev/scripts/Get-PortalReleaseSummary.ps1` 汇总发布 manifest、运维证据和文档化证据；脚本不得自动打 tag、创建分支或推送。
- [ ] 已按 `docs/release-notes-template.md` 整理公开发布说明，并明确真实 IIS/TLS/ACL、SQL Server 2016/2017/2019、企业扫描和 HIA consumer 属于目标环境补证项。
- [ ] 使用独立的 `test` 或 `prod` 外置配置目录，不使用开发机 LocalDB 或开发连接串。
- [ ] 不提交或记录真实连接串、密码、Token、证书、Cookie 或生产数据库备份。
- [ ] 已运行 `dev/scripts/Test-PortalDefaultCredentialRisk.ps1`，并确认默认凭据风险只有可接受的历史样例或已处理项。
- [ ] 如本次发布需要合规留痕，已运行 `dev/scripts/New-PortalComplianceEvidencePackage.ps1` 并记录证据包目录。
- [ ] 如本次发布需要运维留痕，已运行 `dev/scripts/New-PortalOperationsEvidencePackage.ps1` 并记录 `Pending` 项。
- [ ] 目标数据库已完成备份、变更窗口和回滚责任人已明确。
- [ ] 全量初始化脚本仅在隔离测试库中按人工明确操作执行；历史脚本固定使用数据库名 `Portal` 并包含重建/清空行为。
- [ ] P2/P3/P5 增量迁移已先在独立测试库验证；应用启动不会自动执行迁移。
- [ ] P5.2 凭据迁移、P5.3 权限映射和 P5.4 部署策略均有回滚或延期记录。

## IIS 与应用池

- [ ] 服务器已安装 IIS、ASP.NET 4.x、所需静态内容和 IIS 管理功能。
- [ ] 应用池使用 CLR v4，优先以 Integrated pipeline 首测；若必须使用 Classic，记录兼容原因和复测结果。
- [ ] 站点根路径、虚拟目录、应用程序映射和物理目录均已确认。
- [ ] 应用池身份对站点目录、`App_Data/Logs`、上传目录和所需临时目录具备最小写入权限。
- [ ] `ExternalCfgPath` 已设置为部署明确的物理目录；不要依赖应用池 UserProfile 推导默认路径。
- [ ] `<env>` 与外置配置目录一致，例如 `test` 使用 `{ExternalCfgPath}\test\connectionStrings.config`。

## 数据库与配置

- [ ] SQL Server 引擎版本、版本名称、数据库名和 `compatibility_level` 已记录。
- [ ] SQL Server 2016 的新测试库建议使用兼容级别 `130`；已有库先记录实际值，不在发布窗口外强制调整。
- [ ] 应用账号仅拥有所需数据库权限，不使用 `sysadmin` 作为运行账号。
- [ ] `PortalCfg_SystemSettings.sql`、`PortalCfg_UserRegistration.sql`、`PortalCfg_OperationAudits.sql`、`PortalCfg_TabThemeOverrides.sql`、`PortalCfg_ModulePackageStates.sql`、`Portal_UserCredentials.sql` 的执行状态已记录。
- [ ] 仅将已审查的主题目录部署到 `App_Themes`；每个可选主题均含通过校验的 `theme.json` 与 `Default.css`。
- [ ] 仅将已审查的模块目录部署到 `DesktopModules`；每个新业务模块均含通过校验的 `module.json`，且没有 ZIP、DLL、外链或自动脚本入口。
- [ ] 不通过后台上传 ZIP、在线编辑 CSS、外部 URL 或主题脚本改变主题资源；这些能力目前不属于发布契约。
- [ ] `Portal.Diagnostics.AllowAdminDetailView`、日志目录、上传目录和其他部署级设置符合环境要求。
- [ ] 生产默认账号已替换，且没有共享开发密码。
- [ ] 如使用 `Portal_LoadData.sql` 初始化数据库，默认 admin 已在开放访问前完成替换、禁用、删除或迁移，并记录处理结果。
- [ ] `Portal_UserCredentials`、`Portal_UserSecurityStates` 和 `PortalCfg_RolePermissions` 的执行状态已记录。
- [ ] `PortalCfg_RolePermissions` 已包含 `Admins` 兼容权限映射；后续非管理员权限映射需有审计或变更记录。
- [ ] `machineKey`、Cookie `Secure` / `SameSite` 和 HTTPS 策略已按目标环境确认；真实密钥不得进入仓库。

## Profile 分层

| Profile | 用途 | 发布注意事项 |
| --- | --- | --- |
| Dev | 本机开发和 IIS Express 验证。 | 可以使用 LocalDB/开发配置，但不得作为生产证据。 |
| Test | 测试库和测试 IIS/虚拟目录。 | 允许详细诊断，但真实测试连接串仍外置。 |
| Prod | 正式部署。 | 必须启用生产安全 header、Cookie SSL、外置敏感配置和最小权限账号。 |
| Scan | 企业扫描工具专用。 | 可为扫描调整 CSP、缓存或跳转，但需要记录例外和恢复策略。 |
| LegacyIe | 旧浏览器补证。 | 只证明可访问、可操作和可解释降级，不降低生产安全基线。 |

## 发布后回归

- [ ] 首页、登录、登出、Admin 主入口和核心模块可访问。
- [ ] 未登录访问后台资源、日志和审计页会被拒绝。
- [ ] 管理员访问 P5.3 首批权限入口正常；普通用户或缺失权限用户访问这些入口会被拒绝。
- [ ] 角色或权限收紧后，目标用户下一请求会重新判定或被要求重新登录。
- [ ] 管理员可查看系统健康、诊断日志和运营审计，且详情不含 Cookie、Token、密码或连接串。
- [ ] 已按 `docs/operations-runbook.md` 执行运维 readiness 和日志维护 dry-run；生产日志清理前已确认备份与保留要求。
- [ ] 通用错误页仅显示事件编号，不向普通用户暴露异常详情或物理路径。
- [ ] 上传、日志目录、数据库健康检查和主题资源均通过检查。
- [ ] 虚拟目录部署时，登录、模块路径和静态资源路径均已验证。

## 回滚与记录

- [ ] 已记录发布包/提交编号、外置配置版本、数据库迁移状态和验证时间。
- [ ] 已记录版本号；P13 阶段默认使用 `0.13.N` 预 1.0 口径，真实 tag 和 release 分支仅在人工确认后创建。
- [ ] 已按 `docs/deployment-rollback-guide.md` 准备文件、配置、数据库和证据回滚材料。
- [ ] 应用回滚、配置回滚和数据库恢复步骤均可执行。
- [ ] 真实 IIS 复测结果、已知限制和延期项已写入内部 WorkZone 记录。
