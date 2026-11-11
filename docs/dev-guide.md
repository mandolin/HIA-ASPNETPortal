# 开发指南

## 环境要求

建议使用：

- Windows 开发环境。
- Visual Studio 2022 或可构建 .NET Framework 4.8 项目的 Visual Studio。
- .NET Framework 4.8 Developer Pack。
- NuGet 包还原能力。
- SQL Server 或 SQL Server LocalDB。
- IIS Express。
- Node.js 与 npm，仅在需要维护前端资源构建时使用。
- VSCode 建议使用 `Csharp_Webform` Profile，并启用 C#、.NET Runtime 及本项目推荐扩展。

## 获取依赖

本项目使用经典 NuGet `packages.config`。推荐在 Visual Studio 中打开 `src/master.sln` 并启用 NuGet Restore。

VSCode / 命令行方式：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Restore-NuGetPackages.ps1
```

当前普通 PowerShell PATH 未发现 `nuget.exe`。脚本会优先使用 NuGet CLI；如果未安装但 `src\packages` 已存在，则跳过显式还原，以保持旧项目当前可构建状态。后续如果要重新完整还原，建议安装 NuGet CLI 或使用 Visual Studio 的 NuGet Restore。

## 构建

推荐方式：

1. 使用 Visual Studio 打开 `src/master.sln`。
2. 还原 NuGet 包。
3. 选择 `Debug|Any CPU`。
4. 构建解决方案。

VSCode / 命令行方式：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File dev\scripts\Build-Solution.ps1 -Configuration Debug -Platform "Any CPU"
```

脚本会通过 `dev\scripts\Find-MsBuild.ps1` 查找 Visual Studio / Build Tools 中的 MSBuild。当前已验证脚本可自动定位已安装的 MSBuild 并完成 `Debug|Any CPU` 构建；实际安装路径因本机 Visual Studio / Build Tools 配置而异。

## 文档子站构建

文档子站里的页面（`site/` 下的镜像页）由构建脚本从 `docs/` 与 `site/_translations/` 生成：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev/scripts/Build-PortalSite.ps1
```

两个**只读**开关（都不改 `site/`，适合先看清再动手）：

- `-DryRun`：只报告"将生成什么"（新增 / 内容变更 / 无变化），**一个字节都不落盘**（含目录创建）——用来先预演一次再决定要不要真跑；
- `-OutputRoot <目录>`：把产物写到指定目录，输入仍然读真实 `site/`，用于在不改动工作区的前提下比较"现在应该生成什么"（镜像新鲜度门禁正是这么做的）。

## VSCode 任务

`.vscode/tasks.json` 提供以下常用任务：

- `nuget: restore packages`：还原或检查 NuGet 包。
- `build: solution Debug`：构建 `src/master.sln`，并作为默认 build task。
- `iisexpress: start Portal (40001)`：以固定端口 `40001` 启动 `src/Portal`。
- `iisexpress: stop Portal (40001)`：仅停止匹配端口或站点路径的 IIS Express 进程。
- `iisexpress: restart Portal (40001)`：顺序执行停止和启动。
- `gulp: assets:build`：执行一次前端资源构建。
- `gulp: startWatch` / `gulp: stopWatch`：保留原 Gulp 监视任务的 VSCode 入口。
- `portal: build assets and start`：构建解决方案、构建前端资源并启动 IIS Express。
- `portal: documentation baseline`：输出已追踪源码的文档化 inventory JSON。
- `portal: build JavaScript documentation pilot`：独立生成并验证 HIA JSDoc pilot。
- `portal: build .NET DotNetDoc pilot`：独立生成并验证 HIA DotNetDoc pilot。
- `portal: verify .NET XML documentation`：构建 Debug 后检查既有 C# XML 文档输出。
- `portal: verify frontend contracts`：只读检查已追踪的 Web Forms 呈现、主题、模块 CSS 和 Gulp 契约。
- `portal: verify public documentation`：只读检查公开 Markdown 的入口、相对文件链接、隐私边界和生成目录边界。

这些任务只调用仓库内的辅助脚本和 npm scripts，不修改 `.sln`、`.csproj`、`.csproj.user`，因此不会覆盖 Visual Studio 的既有调试设置。

## 文档化基线

公开文档门禁只读取根 README、`docs/` 与 JSDoc 工具 README；它不联网、不读取私有目录、不生成文档，
也不会访问配置或数据库：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalPublicDocumentation.ps1
```

门禁会验证公开入口、相对文件链接、文档索引、项目绝对路径/凭据形态和未确认生成目录的 Git 边界。它允许
`$env:USERPROFILE`、`{ExternalCfgPath}` 和 PowerShell 7 通用安装路径等可复现示例，但不会验证复杂 Markdown 锚点、
外部链接或网络可达性。

`Get-PortalDocumentationBaseline.ps1` 是只读基线工具。它只统计 Git 已追踪的 `src/` 源码，输出 C#、Web Forms、前端与配置的文件数量、C# XML 文档 inventory，以及已知生成/未跟踪目录的边界状态；这些数值不是文档质量或覆盖率百分比。

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDocumentationBaseline.ps1 -AsJson
```

如需把 baseline 写入证据目录，可使用（`<内部证据目录>` 按本项目的实际内部路径替换）：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDocumentationBaseline.ps1 -OutputJson <内部证据目录>\p13.3\documentation-baseline.json
```

脚本不会生成、发布、删除或修改文档。`src/Documentation/`、`src/DoxyGen/`、`src/Portal/Documentation/`、`src/Portal/js/`、`src/Portal/css/` 和 `temp/` 均不会被自动纳入输入；JSDoc 验证生成物写入被忽略的 `temp/documentation/`，公开发布策略见[文档产物与工具链边界](documentation-artifacts-guide.md)。

文档化 readiness 和证据包可以用以下脚本执行：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalDocumentationReadiness.ps1
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalDocumentationEvidencePackage.ps1
```

readiness 会检查 Required、Recommended、Deferred 三档覆盖率口径、JSDoc pilot 隔离、.NET XML 延期边界、生成物未入库和 HIA 通知读取。证据包默认写入证据目录；它不修改源码注释、不提交生成物、不读取敏感配置，也不连接数据库。

## JavaScript 文档化 Pilot

`dev/documentation/jsdoc/` 是独立的 HIA JSDoc 工具项目。它锁定 `@mandolin/jsdoc-plugin-hia-sys@0.1.0`、`@mandolin/jsdoc-theme-hia@0.1.0` 和 `jsdoc@4.0.5`，不改动 `src/Portal/package.json`，因此不会干扰既有 Gulp 或 Visual Studio Task Runner。

当前 pilot 只读取两个已追踪且经审查的输入：`src/Portal/gulpfile.js` 与
`src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js`。可在 VSCode 运行
`portal: build JavaScript documentation pilot`，或直接执行：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Build-PortalJsdocPilot.ps1
```

首次运行会在工具目录执行 `npm ci`；生成 HTML、HIA metadata 和 integration JSON 后验证双语内容、source-link 及本机路径泄漏。生成物只在 `temp/documentation/jsdoc/`，不得提交。

## .NET XML 文档验证

`Test-PortalXmlDocumentation.ps1` 只检查既有 `Debug|Any CPU` 的四份 C# XML 文档输出：`Portal`、`Portal.Components`、`Portal.Components.Data` 与 `Portal.Components.Data1`。它不修改 `.csproj`、不生成 HIA artifact，也不设置 `CS1591` 数量门禁。

默认仅验证已有构建产物：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalXmlDocumentation.ps1
```

需要脚本先执行既有解决方案构建时，显式传入 `-Build`；VSCode 的 `portal: verify .NET XML documentation` 任务采用此模式：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalXmlDocumentation.ps1 -Build
```

验证器只检查 XML 结构、程序集名和至少一个成员条目。XML 继续属于本机构建产物；待 HIA C# producer 和 source-linkage 契约稳定后，再考虑独立接入。

## .NET DotNetDoc Pilot

`dev/documentation/dotnetdoc/` 是独立的 HIA DotNetDoc 工具项目。其清单声明
`@hia-doc/dotnetdoc-runner@^0.1.8`，受版本控制的锁文件解析为 `0.1.8`；并通过工具目录局部 `.npmrc`
使用 npm 官方 registry。这只影响 DotNetDoc pilot，不改变 Portal 前端依赖或 Gulp 工作流。

默认 pilot 读取四份 Debug XML 文档、代表性 Web Forms 标记注释、一个 Web Forms surface 和 solution/project discovery，
输出到被忽略的 `temp/documentation/dotnetdoc/`。执行：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Build-PortalDotNetDocPilot.ps1
```

已有 Debug XML 文档时可跳过构建：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Build-PortalDotNetDocPilot.ps1 -SkipXmlBuild
```

`-ApiOnly` 使用 `dotnetdoc.api-only.config.json`，用于排查 XML 文档管线；`-SourceProbe` 使用
`dotnetdoc.source-probe.config.json`，用于研究 C# source relation。当前 source probe 可能因逐文件 Roslyn 解析缺少完整项目上下文而产生
`CS0246`/`CS0103` 诊断，因此不作为默认门禁。

## VSCode 调试

`.vscode/launch.json` 提供两个调试配置：

- `Portal: Start IIS Express (40001), then attach`：先启动 IIS Express，再通过进程选择器附加到 `iisexpress.exe`。
- `Portal: Attach to IIS Express`：用于 IIS Express 已经启动时手动附加。

使用流程：

1. 在 VSCode 中打开 Debug 面板。
2. 选择 `Portal: Start IIS Express (40001), then attach`。
3. 进程选择器出现后选择 `iisexpress.exe`。
4. 浏览器访问 `http://localhost:40001/`。
5. 结束调试后会执行 `iisexpress: stop Portal (40001)`。

当前已验证 IIS Express 可在 `http://localhost:40001/` 返回 `HTTP 200 OK`。

Win7 VM 或其他局域网设备访问 IIS Express 时，不能使用默认 localhost 简易模式。先停止已有实例，再显式绑定主机 IP：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Stop-IISExpress.ps1 -Port 40001
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Start-IISExpress.ps1 -Port 40001 -HostName 192.168.199.21
```

脚本会同时保留 `localhost` 绑定并输出外部 URL。若 VM 仍无法访问，按脚本提示在主机管理员命令行中添加 URLACL 或防火墙规则。
默认不传 `-HostName` 时仍使用原有 localhost 启动方式，不影响 Visual Studio 和 VSCode 既有调试路径。

## 自动化测试、Smoke 与 SQL Server 兼容检查

新增正式 MSTest runner，默认只运行本机单元/契约测试，不连接数据库、不启动 IIS、不创建账号：

```powershell
mise exec -- pwsh -NoLogo -NoProfile -File dev/scripts/Invoke-PortalTests.ps1
```

需要快速复用已构建的测试程序集时，可显式跳过还原和构建：

```powershell
mise exec -- pwsh -NoLogo -NoProfile -File dev/scripts/Invoke-PortalTests.ps1 -NoRestore -NoBuild
```

新增的 HTTP smoke 默认只检查已经运行的站点，不写入数据库，也不要求管理员凭据：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSmoke.ps1
```

需要脚本自行启动并关闭本地 IIS Express 时，显式传入：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSmoke.ps1 -StartIISExpress -StopWhenComplete
```

管理员页面回归需要显式提供用户名；密码会使用交互式 `SecureString` 输入，不应写入命令、脚本或配置文件：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSmoke.ps1 -AdminUser admin
```

非 LocalDB SQL Server 兼容检查必须使用独立测试库的外置连接串文件。默认只读 preflight；传入 `-ApplyP2Migrations`、`-ApplyP3Migrations`、`-ApplyP5Migrations`、`-ApplyP6UserProfileMigration` 或 `-ApplyP6EmployeeOrganizationMigration` 并确认后，才会执行对应的幂等增量迁移：

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath $testConfig -RequireP2Migrations -RequireP3Migrations -RequireP5Migrations -RequireP6UserProfileMigration -RequireP6EmployeeOrganizationMigration
```

用户资料迁移会先检查旧用户名称和邮箱是否存在会破坏唯一性的历史数据；员工组织迁移按 `PortalBiz_OrganizationUnits.sql`、`PortalBiz_Employees.sql`、`PortalBiz_UserEmployeeBindings.sql` 的顺序执行。两者都只应指向隔离开发库或测试库，不要指向生产配置。

SQL Server 版本矩阵预检用于多版本补证。默认只做静态扫描，不连接数据库、不创建数据库、不执行 SQL：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlVersionMatrix.ps1
```

需要给某个隔离 SQL Server 测试实例补充版本、兼容级别和目标库元数据时，传入仓库外连接串文件。脚本只读取元数据，不输出服务器名、连接串或密码；SQL Server 2016/2017/2019 的通过结论仍必须由对应真实实例补证，不能用更新版本代替：

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlVersionMatrix.ps1 -ConnectionStringsConfigPath $testConfig
```

提供只读数据访问 inventory，用于扫描已追踪源码中的 SQL Server 专属依赖、需要 provider 方言处理的 SQL、可抽象入口和 provider proof 边界。它不连接数据库、不读取仓库外连接串，也不会扫描未跟踪生成目录：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDataAccessInventory.ps1
```

需要保留 JSON 证据时显式指定输出路径：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDataAccessInventory.ps1 -OutputJson <内部证据目录>\p11.2\data-access-inventory.json
```

提供只读迁移 manifest 检查，用于盘点 `src/Setup` 脚本的执行顺序、迁移类型、幂等性、provider 标签、回滚模式和 legacy 风险：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalMigrationManifest.ps1
```

需要保留 JSON 证据时显式指定输出路径：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalMigrationManifest.ps1 -OutputJson <内部证据目录>\p11.3\migration-manifest.json
```

既有测试库需要补齐对应 schema 时，必须显式使用外置 `test` 连接串和 `Test-PortalSqlCompatibility.ps1` 的 `-Apply*` 开关。该操作会写入目标测试库，只应指向隔离测试实例：

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath $testConfig -ApplyP5Migrations -ApplyP6UserProfileMigration -ApplyP6EmployeeOrganizationMigration -ApplyP6BusinessModuleMigration -RequireP2Migrations -RequireP3Migrations -RequireP5Migrations -RequireP6UserProfileMigration -RequireP6EmployeeOrganizationMigration -RequireP6BusinessModuleMigration
```

首次创建隔离的 SQL Server 2016+ 测试库时，可显式运行初始化脚本。它从外置连接串读取目标库名，要求目标库不存在，并在确认后导入历史基础数据与既有迁移；不会输出或保存连接串：

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Initialize-PortalTestDatabase.ps1 -ConnectionStringsConfigPath $testConfig
```

可先增加 `-WhatIf` 检查目标库和执行意图。该脚本绝不覆盖既有数据库，任一步失败后也不会自动删库；只可用于隔离测试实例。发布前请使用 [deployment-checklist.md](deployment-checklist.md)。

## 数据提供程序 Proof

提供一个不加入 `src/master.sln` 的 SQLite capability proof，用于验证新增 provider profile、ADO.NET factory、参数化读写、UTC、事务和唯一约束。它不切换门户主业务数据库，不需要修改外置 `Portal` 连接串，也不参与站点发布。

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalDataProvider.ps1 -Configuration Debug
```

脚本会单独还原 proof 项目的 packages.config、构建到 `temp\provider-proof\bin\`，并在 `temp\provider-proof\data\` 下重新生成 SQLite 文件。`-DatabasePath` 只允许该临时数据目录内的路径；默认会删除上次 proof 文件后重建，传入 `-KeepDatabase` 才保留文件。

新增 provider 专用 DDL 放在 `src\Setup\Providers\{ProviderId}\`。当前只有 `SQLite` proof；未来 MySQL、PostgreSQL 等 provider 应各自增加目录、依赖/许可证审计、方言实现、迁移与回归，不应覆盖既有 SQL Server 脚本。


## 扩展性验收

`Test-PortalExtensionSmoke.ps1` 汇总构建、前端资源、SQLite provider、根站点和虚拟目录回归。默认不写数据库、不要求管理员凭据，也不会停止正在使用的 `40001` 站点；虚拟目录使用临时 IIS Express 端口并在结束后停止。

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1
```

SQL Server schema、管理员交互和缓存隔离是显式能力。连接串仅从仓库外文件读取，脚本不会输出其中的值：

```powershell
$config = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1 -IncludeSqlCompatibility -ConnectionStringsConfigPath $config
```

主题与缓存 proof 会在指定的开发或测试库中短暂写入验证数据，并在 `finally` 中恢复原状态。主题 proof 验证数据库全局 `ThemeProbe`、Tab 覆盖优先级和非法主题回退；缓存 proof 会创建临时 ModuleProbe 定义、实例和包状态。两者必须显式开启，且不要指向生产配置：

```powershell
$config = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1 -IncludeThemeMutation -IncludeCacheMutation -ConnectionStringsConfigPath $config
```

管理员交互回归使用 `SecureString` 密码输入，不记录密码。它会验证已登录访问系统健康、日志、审计、主题设置和模块目录页面；主题/模块的实际业务操作仍应在隔离数据上按阶段验收清单执行：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalExtensionSmoke.ps1 -IncludeAdmin -AdminUser admin
```

## 独立模块（本体之外提供的能力）

门户支持把业务能力以**独立模块**的形式部署：模块包位于 `DesktopModules/<包名>/`，由容纳机制按可信目录、部署 Profile 与包启用状态装载，本体不需要为每个新能力修改内核代码。Form Builder（表单构建器）是这一机制的首个示例。

### 模块包清单（module.json）

每个模块包目录必须含 `module.json`，字段与运行时校验口径一致：

```json
{
  "schemaVersion": 2,
  "packageId": "HIA.FormBuilder",
  "displayName": "表单构建器（独立模块）",
  "version": "1.0.0",
  "minimumPortalVersion": "1.0",
  "moduleKind": "Independent",
  "desktopEntry": "DesktopModules/FormBuilder/FormBuilder.ascx",
  "resources": ["Styles/FormBuilder.css"],
  "capability": { "capabilityId": "BasicBusiness.FormBuilder", "role": "Primary" }
}
```

关键约束（写错会导致包**静默不渲染**，构建与单元测试都发现不了）：

- `schemaVersion` 必须是 JSON **整数** `1` 或 `2`，不能写成字符串。
- `desktopEntry` 必须位于本包目录内且入口文件真实存在。
- `resources` 必须是**包内相对路径**，扩展名限于 `.css`/`.png`/`.jpg`/`.jpeg`/`.gif`/`.webp`，且文件存在；不能写成 `DesktopModules/...` 全路径。
- `capability` 必须是含 `capabilityId` 与 `role` 的**对象**；能力键必须在权威能力词表 `PortalCapabilityRegistry` 中登记，角色取 `Primary`/`Contributor`/`Projection`/`Adapter`/`Block` 之一。顶层 `capabilityId`/`capabilityRole` 是无效写法。
- 禁止 `script`/`scripts`、`externalUrl`/`externalUrls`、`assembly`/`assemblies`、`packageUrl` 等字段：模块声明不能改变执行或下载能力。
- `moduleKind` 只能是 `Core`（缺省）或 `Independent`。

### 部署 Profile 允许集

活动 Profile 由 `Web.config` 的 `Portal.ModuleProfiles.Active` 给出，并在启动时按「基础配置 + 环境覆盖」顺序由 `Config/appSettings.json` 与 `Config/appSettings.<环境>.json` 叠加。每个活动 Profile 的 `Portal.ModuleProfiles.<Profile>.Packages` 允许集必须**显式列出** `Independent` 模块的 `packageId`；通配符 `*` 只覆盖 `Core` 形态，不能让独立模块被隐含放行。

修改允许集后需重启站点（或触发应用域回收）使新配置生效。`Config/appSettings.dev.json` 是本机忽略文件；正式登记应写入 `Config/appSettings.json` 与 `Config/appSettings.*.template.json`，避免只改本机文件导致其它环境缺包。

### 安装与卸载

安装/卸载的可复用实现为 `IPortalModuleProvisioner` / `PortalModuleProvisioner`，以幂等 SQL 写入或清理 `PortalCfg_Tabs`、`PortalCfg_ModuleDefinitions`、`PortalCfg_Modules` 与 `PortalCfg_ModulePackageStates`。部署期可直接执行：

```powershell
# 安装（幂等）
sqlcmd -S "(localdb)\MSSQLLocalDB" -d Portal -i src\Setup\PortalCfg_FormBuilder_Provision.sql
# 卸载（反向清理）
sqlcmd -S "(localdb)\MSSQLLocalDB" -d Portal -i src\Setup\PortalCfg_FormBuilder_Deprovision.sql
```

卸载脚本会清理模块实例、回收不再被引用的定义、删除包状态行，并在专用页签无模块时移除该页签。部署 Profile 允许集回滚与物理文件移除属部署动作，不在 SQL 脚本内。

### 安全边界

- 第三方模块**不得**获得 `Admins` 角色或管理级能力；安装期对能力标识做 fail-closed 校验：未登记的能力（拒绝能力自创）与管理级权限键前缀一律拒绝。
- 来源声明与能力声明永不作为授权依据；模块实例的角色设置（如 `EditRoles`）只决定谁可编辑该实例内容，不等于模块自身获得管理权限。
- 单模块安装失败不影响其它模块；运行期模块渲染失败由宿主隔离，不会导致整页失败。

### 主题皮肤与旧浏览器兼容

新增可见元素时，必须在 6 套正式皮肤的 `Default.css` 中补齐 `body.portal-theme-<主题名> .<模块类>` 作用域规则，并同时覆盖 `Default` 回退皮肤。颜色、边框与字体由主题层经 `--portal-*` 语义变量提供并保留旧内核回退值；模块私有 CSS 只承担布局，且不得使用 flex/grid、`rem`、`calc` 等 IE9 不支持的基础布局，也不得在 `var()` 之外写裸色值。

### 验证

- 静态契约门禁：`dev/scripts/Test-PortalIndependentModuleGate.ps1`（L0，校验 manifest 契约、Profile 允许集、Admins 边界、卸载对称性与 6 套皮肤作用域）。
- 浏览器级运行期证据：`dev/scripts/Invoke-PortalIndependentModuleRuntimeEvidence.ps1` 驱动 `Test-PortalIndependentModuleRuntimeEvidence.mjs`，需运行中的站点、LocalDB、`mise` 提供的 node 与开发期 playwright；按浅/深主题与中/英语言采集截图与断言，产物目录通过 `-OutputDirectory` 参数指定。

## 在线能力（在线安装/上传）

在线能力指通过受控通道把能力包（`.zip` 归档）安全安装或上传到门户宿主，而不破坏既有安全与审批边界。承载基座、隔离通道、双步确认闸、审计、签名与完整性校验、恶意扫描、回滚与故障隔离均已在 `Portal.Components` 与 `Portal` 项目中实现，并由质量门禁与单元测试验证。

### 承载基座与隔离通道

隔离暂存策略常量集中在 `src/Portal.Components/PortalOnlinePackagePolicy.cs`，含包扩展名、包大小上限、条目数与单条目/解压总字节上限，以及隔离暂存虚拟目录（落在 `~/App_Data/OnlinePackages/Staging`）。隔离暂存器 `src/Portal.Components/PortalOnlinePackageStaging.cs` 的 `TryStage` 在隔离子目录下解压；任何失败都在 `finally` 中清理本次上传的独立子目录，确保单次失败上传不会污染核心目录或彼此。

### 双步确认闸

`src/Portal.Components/PortalOnlinePackageConfirmation.cs` 的 `PortalOnlinePackageConfirmationGate` 提供无状态双步确认：`TryIssue` 签发票据（nonce 防重放、同步令牌防 CSRF、包哈希绑定、幂等），`TryConfirm` 在二次鉴权下确认票据。权限键为 `Module.OnlinePackage.Upload` 与 `Module.OnlinePackage.Confirm`，落在 Module 域。

### 审计

`src/Portal.Components/PortalOnlinePackageAuditEvents.cs` 定义在线能力审计事件词表（上传校验 / 确认安装 / 放弃清理 / 回滚演练）；`src/Portal/Components/PortalOnlinePackageAudit.cs` 的运行时包装经既有 `PortalOperationAudit.Record` 统一写入 `PortalCfg_OperationAudits`，`outcome` 如实记录失败、不伪造成功。

### 签名与完整性校验

`src/Portal.Components/PortalOnlinePackageIntegrity.cs` 提供 SHA-256 字节/流计算（与离线装配态校验同口径）与 RSA-SHA256 真实验签，并承担 .NET Framework 4.8 读取归档不自动校验 CRC-32 所缺的完整性职责。

### 扫描与安全闸门

`src/Portal.Components/PortalOnlinePackageScanning.cs` 提供 `IPortalOnlinePackageScanner` 唯一适配点（企业内网扫描引擎可在此接线）、默认 `PortalOnlinePackageUnavailableScanner` 显式「未接线」实现，以及联合判定门 `TryAllowInstall`（白名单 → 完整性 → 签名 → 扫描，任一不满足即拒、前置失败短路）。

### 回滚与故障隔离

`src/Portal.Components/PortalOnlinePackageRollback.cs` 提供回滚演练判定器（哈希清单快照、字节级一致与零残留判定、三种演练情形结论）与单模块失败隔离判定器 `PortalOnlinePackageFailureIsolation`。可复跑的回滚演练证据脚本见 `dev/scripts/Invoke-PortalOnlinePackageRollbackDrill.ps1`。

### 验证

- 单元测试：`src/Portal.Tests/PortalOnlinePackage*.cs` 覆盖承载、确认、审计、安全、回滚。
- 静态边界门禁：`dev/scripts/Test-PortalOnlinePackageSecurityBoundaries.ps1`（L0，覆盖白名单/隔离、权限、审计、签名、扫描、回滚六个维度一致性）。

## 数据库初始化

参考 `src/ReadMe.txt`，基础流程为：

1. 为隔离环境准备仓库外 `connectionStrings.config`，并确保目标数据库不存在。
2. 优先用 `Initialize-PortalTestDatabase.ps1` 显式执行基础初始化，再用 `Test-PortalSqlCompatibility.ps1` 的 `-Apply*` 开关补齐后续幂等迁移；历史 SQL 文件仍保留供 Visual Studio/SSMS 人工维护使用。
3. 如确需手工执行，请按 `Portal_CreateDB.sql`、`Portal_LoadConfig.sql`、`Portal_LoadData.sql`、`PortalCfg_SystemSettings.sql`、`PortalCfg_UserRegistration.sql`、`PortalCfg_OperationAudits.sql`、`PortalCfg_TabThemeOverrides.sql`、`PortalCfg_ModulePackageStates.sql`、`Portal_UserCredentials.sql`、`PortalCfg_RolePermissions.sql`、`PortalBiz_UserProfiles.sql`、`PortalBiz_OrganizationUnits.sql`、`PortalBiz_Employees.sql`、`PortalBiz_UserEmployeeBindings.sql`、`PortalBiz_EmployeeProfileConfirmations.sql`、`PortalBiz_EmployeeProfileCorrectionRequests.sql`、`PortalBiz_WorkItems.sql`、`PortalBiz_WorkItemEvents.sql`、`PortalBiz_BusinessApplications.sql`、`PortalBiz_WorkflowEvents.sql` 的顺序操作，并确认数据库上下文与连接串一致。
4. 复制 `src/Portal/Config/Templates/connectionStrings.config` 到外置配置目录，并修改其中的 `Portal` 连接串。
5. 构建并运行站点。

业务申请样板页可以先生成可审阅 SQL，不直接修改数据库：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BusinessApplicationScenarioSql.ps1
```

如需在已确认的开发/测试库中执行，应先补齐相应迁移，再显式传入外置连接串并使用 `-Apply`：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath '<仓库外>\connectionStrings.config' -ApplyP5Migrations -ApplyP12WorkItemMigration -ApplyP19BusinessApplicationMigration -RequireP5Migrations -RequireP12WorkItemMigration -RequireP19BusinessApplicationMigration -Confirm:$false

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BusinessApplicationScenarioSql.ps1 -ConnectionStringsConfigPath '<仓库外>\connectionStrings.config' -Apply -Confirm:$false
```

需要补齐认证浏览器回归时，可使用受限的 fixture helper。它只接受直接父目录名为 `test` 的外置 `connectionStrings.config`，且只创建由 `FixtureId` 确定的一个普通参与者和一个仅具 `Admins` 实体角色的管理员。Create 的密码必须由短生命周期进程以 `SecureString` 交互输入；不得把密码写入命令行、脚本、日志或配置。完成提交和管理员审核后，必须先 Inspect 再 Remove，并确认最终计数均为零：

```powershell
$testConfig = '<仓库外>\test\connectionStrings.config'
$participantPassword = Read-Host -AsSecureString 'P19 fixture participant password'
$administratorPassword = Read-Host -AsSecureString 'P19 fixture administrator password'

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BrowserSmokeFixture.ps1 `
 -ConnectionStringsConfigPath $testConfig `
 -Action Create `
 -FixtureId 'p19-local-001' `
 -ParticipantPassword $participantPassword `
 -AdministratorPassword $administratorPassword `
 -Confirm:$false

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BrowserSmokeFixture.ps1 `
 -ConnectionStringsConfigPath $testConfig `
 -Action Inspect `
 -FixtureId 'p19-local-001'

& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\New-PortalP19BrowserSmokeFixture.ps1 `
 -ConnectionStringsConfigPath $testConfig `
 -Action Remove `
 -FixtureId 'p19-local-001' `
 -Confirm:$false
```

该 helper 只输出计数级事实，不输出连接串、密码、盐、哈希或申请正文。Remove 在单一事务中按依赖顺序删除该 fixture 的记录、实体角色关系和两个账号；它不能按任意用户名、角色或前缀删除数据。它只证明 test 数据库与隔离浏览器闭环，不能替代真实 IIS/HTTPS、生产或企业扫描证明。

部署或共享环境中不得使用默认账号和弱密码。
默认凭据和旧口令治理见 `docs/deployment-default-credentials.md`；发布前应运行 `dev/scripts/Test-PortalDefaultCredentialRisk.ps1`。

## 前端资源

Web Forms Master、主题、模块 CSS、Gulp 输入输出、Visual Studio/VSCode 边界和未跟踪资产策略见
[frontend-asset-guide.md](frontend-asset-guide.md)。

`src/Portal/gulpfile.js` 保留 Visual Studio Task Runner 原绑定：

```js
/// <binding ProjectOpened='startWatch' />
```

同时新增独立的 `assets:build` 任务，用于 VSCode 和 AI 自动化调用，不改变 `startWatch` / `stopWatch` 原语义。

常用命令：

```powershell
cd src\Portal
npm run assets:build
npm run assets:watch
npm run assets:stop-watch
```

当前已验证 `npm run assets:build` 可以成功执行。执行时出现 Browserslist 数据过期提示，属于前端依赖维护提醒，不影响本次构建结果。

需要只读复核正式前端输入而不运行 Node/Gulp 时，执行：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalFrontendContracts.ps1
```

## 主导航两级分组

主导航把「分组标题 + 组内页签链接」作为两级展示结构。它**不是**父子页签：`PortalCfg_Tabs` 表结构没有新增父子列，分组只是展示层的功能族归属，不参与授权判定，也不改变页签的可见性链路。

分组词表是 `src/Portal.Components/PortalNavigationGroupAlias.cs` 中的封闭集合，按 `SortOrder` 固定为四组：

| 稳定键 | 中文名 | 英文名 | 默认成员（`NavigationKey`） |
| --- | --- | --- | --- |
| `WorkAndApproval` | 工作与审批 | Work & Approvals | `MyWorkItems`、`LeaveAndExpense` |
| `Business` | 业务办理 | Business | `BusinessRequests`、`MyProfile` |
| `PortalAndHelp` | 门户与帮助 | Portal & Help | `Home`、`Help` |
| `SystemAdministration` | 系统管理 | System Administration | `Admin`、`CapabilityWorkbench` |

渲染链路分为三层，各层职责不可混用：

- `PortalNavigationGroupView` 是纯逻辑视图模型，消费已完成角色过滤与入口判定的页签集合，产出分组、未分组平铺页签、降级标记与空组计数；它保留每个页签在最终集合中的显示下标，因此 `tabindex` 语义与改造前一致。
- `DesktopPortalBanner.ascx` 用原生 `<button type="button">` 渲染分组标题，带 `aria-expanded` 与 `aria-controls`；组内仍是平铺的 `.portal-tab` 链接，选中态为非链接 span。不采用 `menu` / `menubar` role，不写内联 `onclick`。服务端默认输出全部展开，无 JavaScript 时页签依然可达。
- `Scripts/Navigation/PortalNavigationGroups.js` 是 ES5 渐进增强脚本，绑定点击折叠/展开、`Escape` 关闭并回焦、同步 `aria-expanded`；它只改变展示状态，不重新判定权限。

别名覆盖用配置键 `Portal.Navigation.GroupAliases`（`Web.config` appSettings，可被数据库运行设置覆盖），值为逗号分隔的 `NavigationKey=GroupKey`。解析严格校验：未知分组键、畸形项、重复键都会**整份拒绝并回退默认词表**，渲染层读到降级标记后退回保留的平铺 `DataList`。任何非法配置都不会隐藏页签。

主题层按 **7 套**（6 套正式皮肤 + `Default` 回退，`ThemeProbe` 诊断探针排除）各自定义 `body.portal-theme-<名> .portal-nav-group*` 作用域规则，类名族为 `portal-nav-group`、`portal-nav-group-toggle`、`portal-nav-group-tabs`、`portal-nav-group-caret`、`portal-nav-group-count`、`portal-nav-group-hidden`。`:focus` 必须是可见轮廓，折叠类必须是 `display:none`（保证组内链接离开键盘 Tab 序列）。新增可见元素时按同一口径补齐，不要只在模块自带样式里写规则。

相关门禁与证据：

| 门禁 / 证据 | 证明范围 |
| --- | --- |
| `dev/scripts/Test-PortalNavigationGroupContract.ps1`（L0） | 词表覆盖种子 8 个 `NavigationKey`、配置键四处登记、fail-closed 解析、分组不含授权符号、主题设置入链 |
| `dev/scripts/Test-PortalNavigationGroupRender.ps1`（L0） | disclosure 标记语义、7 套皮肤作用域规则、ES5 脚本约束、降级回滚接线 |
| `dev/scripts/Test-PortalAccessibilityBaseline.ps1`（L0） | A1–A10：对比度 ≥4.5、`:focus`、`img alt`、禁内联 `onclick`、命名锚，含分组导航专属断言与 `a11y-runtime` 未覆盖台账锁 |
| `dev/scripts/Test-PortalNavigationGroupRenderEvidence.mjs`（L2） | 浏览器级：中英各自渲染截图、点击折叠/展开、`Escape` 回焦、7 套皮肤计算样式、禁用 JavaScript 时页签仍可达 |
| `dev/scripts/Test-PortalCollaborationRuntimeEvidence.mjs`（L2） | 协作工作台四象限（浅/深主题 × 中/英语言）各自出图，禁止共用同一张 |

运行期键盘焦点顺序与读屏器语义仍如实登记为未覆盖面 `a11y-runtime`，静态门禁与浏览器截图都不替代它。

## 配置注意事项

- `src/Portal/Config` 下有 `*.template.*` 文件，也有实际环境文件。
- 修改真实配置前先确认是否应改模板。
- 连接串不再写入 `UnityCfg*.xml`，而是从外置 `{ExternalCfgPath}\{env}\connectionStrings.config` 读取。
- `ExternalCfgPath` 留空时默认使用 `{当前进程用户目录}\Web\HIA-ASPNETPortal\`。
- 本地 `dev` 默认文件路径为 `{当前进程用户目录}\Web\HIA-ASPNETPortal\dev\connectionStrings.config`。
- `connectionStrings.config` 中当前使用的逻辑名为 `Portal`，启动后会映射为 Unity 的 `connectionString` 命名实例；`providerName` 为必填项，当前门户主业务数据库固定为 `System.Data.SqlClient`。
- 环境变量 `HIA_ASPNETPORTAL_CONNSTR_PORTAL` 可覆盖具体连接串值；外置文件本身仍必须存在。
- 不要提交真实生产连接字符串、账号、密码、Token 或证书。
- `.gitignore` 已包含部分配置、数据库和构建产物忽略规则，但历史文件仍需人工核查。

## 文件编码

- 后续新增或修改的文本文件统一使用 UTF-8 无 BOM。
- 仓库根目录 `.editorconfig` 已设置 `charset = utf-8`。
- `src/Portal/.editorconfig` 因包含 `root = true`，也已单独设置 `charset = utf-8`。
- VSCode 工作区已设置 `"files.encoding": "utf8"`，对应 UTF-8 无签名。
