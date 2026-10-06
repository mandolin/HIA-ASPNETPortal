# HIA-ASPNETPortal

企业内网 Web Forms 门户平台。fork 自 Codeplex 上的 ASP.NET Portal Starter Kit，在其基础上补齐了**权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁**。

当前版本：**v0.7.0**。成熟度定位为 **可用 / 参考 / 研究基线**，尚未取得真实生产环境证据，**不宣称生产级可信**（口径约束见 [版本策略](docs/versioning.md)）。

## English

Enterprise intranet portal platform on ASP.NET Web Forms, forked from the original ASP.NET Portal Starter Kit on Codeplex and extended with permission and data-scope enforcement, workflow state machines, auditing, module package governance, theme governance, and an automated gate suite.

Current version: **v0.7.0**. Maturity is stated as **usable / reference / research baseline** — no production-environment evidence yet, so production-grade reliability is **not** claimed.

## 功能特性

| 能力 | 说明 |
| --- | --- |
| 身份与授权 | 账户注册、角色权限实体授权、**按组织子树的数据范围控制**、认证角色 Cookie |
| 流程状态机 | 业务申请与协同事项的**显式迁移表**，写入前 fail-closed，迁移必写事件 |
| 审计 | 运营审计表、审计保留期策略、日志维护与归档 |
| 模块包治理 | 受信任模块包（`module.json`）的注册、启停、移除与边界校验 |
| 主题治理 | 主题包契约校验与回退 |
| 业务模块 | 6 个受信任模块包：员工目录、待办工作台、员工资料确认 / 更正、业务申请 |
| 国际化 | 中文 / 英文双语资源，强类型资源键，资源契约门禁 |
| 自动化门禁 | **14 个门禁统一编排**，按 L0/L1/L2 分层，退出码与输出双判定 |

## 快速上手（本地开发）

### 1. 前置环境

- Windows；Visual Studio 2022 或可构建 .NET Framework 4.8 的 Visual Studio / Build Tools
- **.NET Framework 4.8 Developer Pack**
- SQL Server 或 SQL Server LocalDB
- IIS Express
- NuGet 包还原能力（NuGet CLI，或使用 Visual Studio 的 NuGet Restore）

Node.js 与 npm **仅在**需要维护前端资源构建时需要。

### 2. 还原依赖

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Restore-NuGetPackages.ps1
```

### 3. 准备数据库连接串（必须在仓库外）

复制模板并修改 `Portal` 连接串：

```powershell
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\Web\HIA-ASPNETPortal\dev" | Out-Null
Copy-Item src\Portal\Config\Templates\connectionStrings.config "$env:USERPROFILE\Web\HIA-ASPNETPortal\dev\connectionStrings.config"
```

> **为什么必须在仓库外**：连接串含凭据，放在仓库内会被提交或误打包。仓库只提供模板。这是**安全要求**，不是配置负担。

### 4. 初始化数据库

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Initialize-PortalTestDatabase.ps1 -ConnectionStringsConfigPath "$env:USERPROFILE\Web\HIA-ASPNETPortal\dev\connectionStrings.config"
```

脚本按 `Portal_CreateDB.sql` → `Portal_LoadConfig.sql` → `Portal_LoadData.sql` → 后续幂等迁移的顺序执行。手工执行顺序与逐脚本说明见 [开发指南](docs/dev-guide.md)。

### 5. 构建

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Build-Solution.ps1 -Configuration Debug -Platform "Any CPU"
```

脚本通过 `dev\scripts\Find-MsBuild.ps1` 自动定位 MSBuild。

### 6. 启动

以 `src/Portal` 为站点、端口 `40001` 启动 IIS Express，然后访问 `http://localhost:40001/`。

VSCode 用户可直接运行任务 `iisexpress: start Portal (40001)`。

### 7. 验证

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Invoke-PortalGateSuite.ps1
```

分层执行（`All` / `L0` / `L1` / `L2`）：

| 层 | 依赖 | 内容 |
| --- | --- | --- |
| **L0** | 仅源码 | XML 文档、资源契约、版本一致性、旧版 CSS 兼容、ASCX 编译契约、对标检查表 |
| **L1** | 构建后 | 构建、单元测试、5 个业务模块冒烟 |
| **L2** | 需站点 | 模块运行期加载（按档位分组，自动发现模块目标） |

脚本会自动切换配置档位并在结束时还原，无需手工改配置。

## 文档

完整文档索引见 **[docs/README.md](docs/README.md)**。常用入口：

| 我想… | 看这份 |
| --- | --- |
| 了解系统结构与模块划分 | [架构说明](docs/architecture.md) |
| 本地开发、构建、配置、调试 | [开发指南](docs/dev-guide.md) |
| 开发一个模块包 | [模块开发指南](docs/module-development-guide.md) |
| 运行和使用门户 | [使用指南](docs/user-guide.md) |
| 部署上线 | [部署清单](docs/deployment-checklist.md) |
| 发布与回滚 | [回滚指南](docs/deployment-rollback-guide.md)、[版本策略](docs/versioning.md) |
| 跑测试与发布前检查 | [测试清单](docs/testing-checklist.md) |
| 默认账号与凭据治理 | [默认凭据治理](docs/deployment-default-credentials.md) |
| 新增第三方依赖 | [第三方依赖约定](docs/third-party-dependencies.md) |

## 贡献

请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。提交 Issue 或 PR 前请先确认门禁通过：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Invoke-PortalGateSuite.ps1 -Layer L0
```

## 许可证

[MIT](LICENSE)。fork 自 ASP.NET Portal Starter Kit，其许可与归属见 [README-old.md](README-old.md)。

## 项目沿革

本项目在 ASP.NET 领域起步于 MVC 之前的时代。随着前后端分离成为主流，纯 Web Forms 开发模式日渐式微，但由于运行环境老旧与既有项目体系的延续，它在**企业内网快速开发**场景中仍然适用 —— 大量同类项目如 [DNN](https://github.com/dnnsoftware/Dnn.Platform)、[mojoportal](https://github.com/i7MEDIA/mojoportal/)、[C1-CMS](https://github.com/Orckestra/C1-CMS-Foundation)、[CarrotCakeCMS](https://github.com/ninianne98/CarrotCakeCMS) 均延续了这一选择。