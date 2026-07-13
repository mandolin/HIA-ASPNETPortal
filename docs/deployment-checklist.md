# 发布与兼容检查清单

本清单用于 .NET Framework Web Forms 门户的测试和生产发布准备。它不替代真实 IIS 环境中的人工复测，也不授权自动修改 IIS、应用池、数据库或外置配置。

## 发布前边界

- [ ] 使用独立的 `test` 或 `prod` 外置配置目录，不使用开发机 LocalDB 或开发连接串。
- [ ] 不提交或记录真实连接串、密码、Token、证书、Cookie 或生产数据库备份。
- [ ] 目标数据库已完成备份、变更窗口和回滚责任人已明确。
- [ ] 全量初始化脚本仅在隔离测试库中按人工明确操作执行；历史脚本固定使用数据库名 `Portal` 并包含重建/清空行为。
- [ ] P2 增量迁移已先在独立测试库验证；应用启动不会自动执行迁移。

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
- [ ] `PortalCfg_SystemSettings.sql`、`PortalCfg_UserRegistration.sql`、`PortalCfg_OperationAudits.sql`、`PortalCfg_TabThemeOverrides.sql`、`PortalCfg_ModulePackageStates.sql` 的执行状态已记录。
- [ ] 仅将已审查的主题目录部署到 `App_Themes`；每个可选主题均含通过校验的 `theme.json` 与 `Default.css`。
- [ ] 仅将已审查的模块目录部署到 `DesktopModules`；每个新业务模块均含通过校验的 `module.json`，且没有 ZIP、DLL、外链或自动脚本入口。
- [ ] 不通过后台上传 ZIP、在线编辑 CSS、外部 URL 或主题脚本改变主题资源；这些能力目前不属于发布契约。
- [ ] `Portal.Diagnostics.AllowAdminDetailView`、日志目录、上传目录和其他部署级设置符合环境要求。
- [ ] 生产默认账号已替换，且没有共享开发密码。

## 发布后回归

- [ ] 首页、登录、登出、Admin 主入口和核心模块可访问。
- [ ] 未登录访问后台资源、日志和审计页会被拒绝。
- [ ] 管理员可查看系统健康、诊断日志和运营审计，且详情不含 Cookie、Token、密码或连接串。
- [ ] 通用错误页仅显示事件编号，不向普通用户暴露异常详情或物理路径。
- [ ] 上传、日志目录、数据库健康检查和主题资源均通过检查。
- [ ] 虚拟目录部署时，登录、模块路径和静态资源路径均已验证。

## 回滚与记录

- [ ] 已记录发布包/提交编号、外置配置版本、数据库迁移状态和验证时间。
- [ ] 应用回滚、配置回滚和数据库恢复步骤均可执行。
- [ ] 真实 IIS 复测结果、已知限制和延期项已写入内部 WorkZone 记录。
