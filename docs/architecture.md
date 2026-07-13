# 架构概览

## 项目定位

HIA-ASPNETPortal 是一个 ASP.NET Web Forms 门户应用，来源于原 ASP.NET Portal Starter Kit。项目目标是在旧系统、企业内网和 WebForms 仍需维护的场景下，继续提供动态门户、模块化内容、角色权限和在线管理能力。

## 解决方案结构

- `src/master.sln`：主解决方案。
- `src/Portal/Portal.csproj`：WebForms 站点项目，目标框架 .NET Framework 4.7。
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
- 新增 provider 专用脚本位于 `src/Setup/Providers/{ProviderId}/`；首轮 SQLite proof 位于 `Providers/SQLite/`，未来可并列增加 MySQL、PostgreSQL 等 provider，而不迁移既有 SQL Server 脚本。
- 本地数据库文件位于 `db/MSSQLLocalDB/`。
- 数据访问使用 Entity Framework 6.1.0。
- 依赖注入使用 Unity 5.x。
- Web 项目仍使用经典 `packages.config` 和 `src/packages/` NuGet 包目录。
- `src/Portal.DataProviderProof/` 是未加入主解决方案的 .NET Framework 4.7 开发/测试 proof 项目；它验证 ADO.NET provider factory 与 SQLite 基础事务能力，不参与正常门户部署。

## 当前架构风险

- 数据访问存在 `Portal.Components.Data` 与 `Portal.Components.Data1` 两个相近项目，职责边界需要进一步确认。
- 生成文档与源码混放在 `src/Documentation` 等目录，后续可考虑明确生成位置和清理策略。
- 当前缺少自动化测试项目，架构调整前需要先补最小回归验证。
- 配置目录中同时存在模板和实际环境文件，后续需要建立敏感配置管理规则。
