# 架构概览

## 项目定位

HIA-ASPNETPortal 是一个 ASP.NET Web Forms 门户应用，来源于原 ASP.NET Portal Starter Kit。项目目标是在旧系统、企业内网和 WebForms 仍需维护的场景下，继续提供动态门户、模块化内容、角色权限和在线管理能力。

## 解决方案结构

- `src/master.sln`：主解决方案。
- `src/Portal/Portal.csproj`：WebForms 站点项目，目标框架 .NET Framework 4.8。
- `src/Portal.Components/Portal.Components.csproj`：接口和公共组件项目。
- `src/Portal.Components.Data/Portal.Components.Data.csproj`：数据访问项目之一。
- `src/Portal.Components.Data1/Portal.Components.Data1.csproj`：数据访问项目之一。

## Web 项目结构

- `Admin/`：用户、角色、模块定义、页面布局、站点设置等后台管理页面。
- `DesktopModules/`：公告、联系人、讨论、文档、事件、HTML、图片、链接、XML 等门户模块。
- `Components/`：门户页面、模块控制、缓存模块控制、容器相关运行时组件。
- `Config/`：Unity 配置、环境配置和 appSettings 配置。
- `App_GlobalResources/`：多语言资源。
- `App_Themes/`：主题样式。
- `Sys/`、`Util/`：环境信息、配置加载等辅助代码。

## 数据和依赖

- 数据库初始化脚本位于 `src/Setup/`。
- P5.2 起，用户强哈希凭据位于 `Portal_UserCredentials`，会话安全版本位于 `Portal_UserSecurityStates`；旧 `Portal_Users.Password` 只保留既有 MD5 数据的兼容迁移样本。
- 新增 provider 专用脚本位于 `src/Setup/Providers/{ProviderId}/`；首轮 SQLite proof 位于 `Providers/SQLite/`，未来可并列增加 MySQL、PostgreSQL 等 provider，而不迁移既有 SQL Server 脚本。
- 本地数据库文件位于 `db/MSSQLLocalDB/`。
- 数据访问使用 Entity Framework 6.1.0。
- 依赖注入使用 Unity 5.x。
- Web 项目仍使用经典 `packages.config` 和 `src/packages/` NuGet 包目录。
- `src/Portal.DataProviderProof/` 是未加入主解决方案的 .NET Framework 4.8 开发/测试 proof 项目；它验证 ADO.NET provider factory 与 SQLite 基础事务能力，不参与正常门户部署。
- `src/Portal.HiaBoundaryProof/` 是未加入主解决方案的 .NET Framework 4.8 契约 proof 项目；它通过 fixtures 验证 HIA 外围能力描述的版本、字段与隐私边界，不添加 HIA 运行时依赖或 transport。

数据访问兼容性标签从 P11.2 起按四类记录：

- `SqlServerOnly`：当前只能视为 SQL Server 能力，例如 `System.Data.SqlClient`、EF SQL Server provider、`[dbo]`、`OBJECT_ID`、`SYSUTCDATETIME()`、`IDENTITY`、`ROWVERSION`。
- `NeedsDialect`：具备抽象可能但必须按 provider 改写的 SQL，例如 `SELECT TOP`、锁提示、`OUTPUT INSERTED` 和 table variable。
- `PortableCandidate`：可作为 provider 抽象入口或接近通用的能力，例如 `PortalDatabaseProfile`、`DbProviderFactories`、简单连接检查。
- `ProviderProof`：独立 proof 范围，例如当前 SQLite proof；不表示门户主业务数据库已支持该 provider。

可用 `dev/scripts/Get-PortalDataAccessInventory.ps1` 生成只读 inventory。脚本只扫描 Git 已追踪源码，不读取真实连接串或仓库外配置。

## HIA 外围协作基线

`Portal.Components/PortalHiaBoundaryContracts.cs` 定义门户拥有的 `hia.portal.peripheral@0.1.0-draft` 契约 DTO 和离线验证器。当前仅覆盖模块、主题、设置 registry 元数据、健康和受限诊断引用，且通过显式版本、白名单字段和路径/敏感字段检查保持边界清晰。

这是一项可演进的协作基线，不是 HIA 平台强耦合或通用远程 API。正常门户不会发现或加载外部 adapter，也不需要 HIA 仓库、DLL 或服务存在。未来如需真实 consumer、传输、身份映射或写操作，应新增设计决策、版本化契约与独立运行回归。

## 当前架构风险

- 数据访问存在 `Portal.Components.Data` 与 `Portal.Components.Data1` 两个相近项目，职责边界需要进一步确认。
- 生成文档与源码混放在 `src/Documentation` 等目录，后续可考虑明确生成位置和清理策略。
- 当前缺少自动化测试项目，架构调整前需要先补最小回归验证。
- 配置目录中同时存在模板和实际环境文件，后续需要建立敏感配置管理规则。
