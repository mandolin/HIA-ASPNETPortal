# 测试清单

## 构建检查

- [ ] 使用 Visual Studio 打开 `src/master.sln`。
- [ ] NuGet 包还原成功。
- [ ] `Debug|Any CPU` 构建成功。
- [ ] `Release|Any CPU` 构建成功。
- [ ] 构建日志中无新增错误。

## 数据库检查

- [ ] `Portal_CreateDB.sql` 可执行。
- [ ] `Portal_LoadConfig.sql` 可执行。
- [ ] `Portal_LoadData.sql` 可执行。
- [ ] 外置 `connectionStrings.config` 存在于 `{ExternalCfgPath}\{env}\`。
- [ ] `Portal` 连接串指向预期本地或测试数据库。
- [ ] 不使用生产数据库执行开发验证。
- [ ] 如需 SQL Server 2016+ 版本补证，先运行 `dev/scripts/Test-PortalSqlVersionMatrix.ps1`；没有对应真实实例时只记录 Pending，不宣称通过。
- [ ] 如需验证最新 schema，`dev/scripts/Test-PortalSqlCompatibility.ps1` 指向已完成对应迁移的隔离测试库。
- [ ] 新增或调整数据访问代码前，运行 `dev/scripts/Get-PortalDataAccessInventory.ps1`，确认 SQL/provider 标签和方言风险已记录。
- [ ] 新增或调整数据库脚本前，运行 `dev/scripts/Get-PortalMigrationManifest.ps1`，确认脚本已纳入 manifest、类型、幂等性、provider 标签和回滚边界。
- [ ] 任何 `-Apply*` 数据库迁移演练均只允许指向隔离测试库，并保留 apply 输出和随后的只读 require 输出。
- [ ] 新增或调整 HIA 外围集成契约、draft fixture、文档化通知读取或证据引用规则前，运行 `dev/scripts/Get-PortalHiaIntegrationInventory.ps1`，确认契约版本、独立 proof、通知读取和隐私边界仍可复核。

## 运行检查

- [ ] 站点可启动。
- [ ] 首页可访问。
- [ ] `dev/scripts/Test-PortalSmoke.ps1` 对已运行站点通过。
- [ ] 登录流程可用。
- [ ] 默认账号已仅限本地验证，非本地环境已修改密码。
- [ ] `dev/scripts/Test-PortalDefaultCredentialRisk.ps1` 已运行；默认 admin、旧 MD5 和初始化脚本风险已记录。
- [ ] 管理入口可访问。

## 核心模块手工回归

- [ ] 公告模块列表和编辑流程。
- [ ] 联系人模块列表和编辑流程。
- [ ] 讨论模块列表、详情和编辑流程。
- [ ] 文档模块上传、查看和编辑流程。
- [ ] 事件模块列表和编辑流程。
- [ ] HTML、图片、链接、XML 模块展示流程。
- [ ] 用户、角色、权限配置流程。
- [ ] 页面布局和模块设置流程。

## 前端资源检查

- [ ] `dev/scripts/Test-PortalFrontendContracts.ps1` 对已追踪的前端契约通过。
- [ ] `dev/scripts/Test-PortalLegacyCssCompatibility.ps1` 对旧浏览器 CSS 基础阻断项通过。
- [ ] 如需 Edge IE mode 近似回归，先执行 `dev/scripts/Test-PortalIeModeReadiness.ps1` 并记录驱动/策略状态。
- [ ] 如需 Win7/IE 真浏览器补证，使用 `dev/scripts/New-PortalLegacyIeTestPackage.ps1` 生成 VM 内可运行测试包，并回收结果 zip。
- [ ] 确认是否需要运行 Gulp 或 Grunt。
- [ ] 如需构建前端资源，记录实际命令。
- [ ] CSS、JS、图片资源路径正确。
- [ ] 未将来源未确认的 `src/Portal/js/`、`src/Portal/css/` 或生成文档目录作为正式输入提交。
- [ ] 浏览器控制台无新增关键错误。

## 文档化检查

- [ ] `dev/scripts/Test-PortalDocumentationReadiness.ps1` 对公开文档化指南、coverage 分层、JSDoc pilot、XML 边界和 HIA 通知读取机制通过。
- [ ] `dev/scripts/Get-PortalDocumentationBaseline.ps1 -OutputJson <证据路径>` 已记录已追踪源码的文档化 inventory。
- [ ] 如需 P13.3 文档化证据，执行 `dev/scripts/New-PortalDocumentationEvidencePackage.ps1` 并确认 `Failed=0`。
- [ ] `src/Documentation/`、`src/DoxyGen/`、`src/Portal.Components.Data/Documentation/`、`src/Portal/Documentation/` 和 `src/Portal.shfbproj` 未作为正式生成物提交。

## 发布前检查

- [ ] `dev/scripts/Test-PortalPublicDocumentation.ps1` 对公开入口、相对文件链接和隐私边界通过。
- [ ] 如需保留合规证据，执行 `dev/scripts/New-PortalComplianceEvidencePackage.ps1` 并将输出留存在 WorkZone 证据目录。
- [ ] 如需保留运维证据，执行 `dev/scripts/New-PortalOperationsEvidencePackage.ps1` 并确认 `Fail=0`；`Pending` 项应记录为目标环境补证。
- [ ] `dev/scripts/Test-PortalPublishReadiness.ps1` 对项目 Content 清单、主题包、模块包和发布输出通过。
- [ ] 如需生成本地文件系统发布包，执行 `dev/scripts/Publish-PortalFileSystem.ps1` 并记录输出目录。
- [ ] 如需生成交付证据，执行 `dev/scripts/New-PortalReleaseManifest.ps1 -PackagePath <发布目录>` 并记录 manifest 输出目录。
- [ ] 如需形成版本节奏或发布说明材料，执行 `dev/scripts/Get-PortalReleaseSummary.ps1`，确认发布 manifest、运维证据和文档化证据均可汇总。
- [ ] 如需对外或跨团队传阅，按 `docs/release-notes-template.md` 整理公开发布说明，并将内部证据路径留在私有记录中。
- [ ] 未提交真实连接字符串、密码、Token、证书。
- [ ] 未提交本地数据库文件、`node_modules/`、`bin/`、`obj/`。
- [ ] `Web.Debug.config`、`Web.Release.config`、`Web.Test.config` 的处理方式符合仓库规则。
- [ ] README、`docs/`、`dev/` 已按变更更新。
- [ ] 已记录验证结果和残留风险。
- [ ] 已按 `docs/deployment-checklist.md` 完成 SQL Server、IIS、外置配置和回滚检查。
- [ ] 已按 `docs/deployment-rollback-guide.md` 准备文件、配置、数据库和证据回滚材料。
- [ ] 已按 `docs/operations-runbook.md` 完成运维入口、日志 dry-run、审计查询、备份提醒和计划任务建议复核。
- [ ] 新模块包已通过 `module.json`、入口 `.ascx` 和本地资源校验；启用时模块/CSS 可用，禁用时二者均不加载。
- [ ] 有实例引用的模块定义不能通过 Legacy 删除页直接级联删除；先完成禁用、迁移或显式实例清理。
