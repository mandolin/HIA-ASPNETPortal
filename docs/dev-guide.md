# 开发指南

## 环境要求

建议使用：

- Windows 开发环境。
- Visual Studio 2022 或可构建 .NET Framework 4.7 项目的 Visual Studio。
- .NET Framework 4.7 Developer Pack。
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
- `portal: verify .NET XML documentation`：构建 Debug 后检查既有 C# XML 文档输出。
- `portal: verify frontend contracts`：只读检查已追踪的 Web Forms 呈现、主题、模块 CSS 和 Gulp 契约。
- `portal: verify public documentation`：只读检查公开 Markdown 的入口、相对文件链接、隐私边界和生成目录边界。

这些任务只调用仓库内的辅助脚本和 npm scripts，不修改 `.sln`、`.csproj`、`.csproj.user`，因此不会覆盖 Visual Studio 的既有调试设置。

## 文档化基线

公开文档门禁只读取根 README、`docs/` 与 JSDoc 工具 README；它不联网、不读取 WorkZone、不生成文档，
也不会访问配置或数据库：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalPublicDocumentation.ps1
```

门禁会验证公开入口、相对文件链接、文档索引、项目绝对路径/凭据形态和未确认生成目录的 Git 边界。它允许
`$env:USERPROFILE`、`{ExternalCfgPath}` 和 PowerShell 7 通用安装路径等可复现示例，但不会验证复杂 Markdown 锚点、
外部链接或网络可达性。

`Get-PortalDocumentationBaseline.ps1` 是 `W-anp-P4.1` 的只读基线工具。它只统计 Git 已追踪的 `src/` 源码，输出 C#、Web Forms、前端与配置的文件数量、C# XML 文档 inventory，以及已知生成/未跟踪目录的边界状态；这些数值不是文档质量或覆盖率百分比。

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Get-PortalDocumentationBaseline.ps1 -AsJson
```

脚本不会生成、发布、删除或修改文档。`src/Documentation/`、`src/DoxyGen/`、`src/Portal/Documentation/`、`src/Portal/js/`、`src/Portal/css/` 和 `temp/` 均不会被自动纳入输入；JSDoc 验证生成物写入被忽略的 `temp/documentation/`，公开发布策略见[文档产物与工具链边界](documentation-artifacts-guide.md)。

## JavaScript 文档化 Pilot

`dev/documentation/jsdoc/` 是独立的 HIA JSDoc 工具项目。它锁定 `@mandolin/jsdoc-plugin-hia-sys@0.1.0`、`@mandolin/jsdoc-theme-hia@0.1.0` 和 `jsdoc@4.0.5`，不改动 `src/Portal/package.json`，因此不会干扰既有 Gulp 或 Visual Studio Task Runner。

首个 pilot 只读取已追踪的 `src/Portal/gulpfile.js`。可在 VSCode 运行 `portal: build JavaScript documentation pilot`，或直接执行：

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

## 自动 Smoke 与 SQL Server 兼容检查

P2.5 新增的 HTTP smoke 默认只检查已经运行的站点，不写入数据库，也不要求管理员凭据：

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

非 LocalDB SQL Server 兼容检查必须使用独立测试库的外置连接串文件。默认只读 preflight；传入 `-ApplyP2Migrations` 或 `-ApplyP3Migrations` 并确认后，才会执行对应的幂等增量迁移：

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 -ConnectionStringsConfigPath $testConfig -RequireP2Migrations -RequireP3Migrations
```

首次创建隔离的 SQL Server 2016+ 测试库时，可显式运行初始化脚本。它从外置连接串读取目标库名，要求目标库不存在，并在确认后导入历史基础数据、P2 与 P3 迁移；不会输出或保存连接串：

```powershell
$testConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\test\connectionStrings.config'
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Initialize-PortalTestDatabase.ps1 -ConnectionStringsConfigPath $testConfig
```

可先增加 `-WhatIf` 检查目标库和执行意图。该脚本绝不覆盖既有数据库，任一步失败后也不会自动删库；只可用于隔离测试实例。发布前请使用 [deployment-checklist.md](deployment-checklist.md)。

## 数据提供程序 Proof

P3.3 提供一个不加入 `src/master.sln` 的 SQLite capability proof，用于验证新增 provider profile、ADO.NET factory、参数化读写、UTC、事务和唯一约束。它不切换门户主业务数据库，不需要修改外置 `Portal` 连接串，也不参与站点发布。

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalDataProvider.ps1 -Configuration Debug
```

脚本会单独还原 proof 项目的 packages.config、构建到 `temp\provider-proof\bin\`，并在 `temp\provider-proof\data\` 下重新生成 SQLite 文件。`-DatabasePath` 只允许该临时数据目录内的路径；默认会删除上次 proof 文件后重建，传入 `-KeepDatabase` 才保留文件。

新增 provider 专用 DDL 放在 `src\Setup\Providers\{ProviderId}\`。当前只有 `SQLite` proof；未来 MySQL、PostgreSQL 等 provider 应各自增加目录、依赖/许可证审计、方言实现、迁移与回归，不应覆盖既有 SQL Server 脚本。

## HIA 外围契约 Proof

P3.4 提供独立的 HIA 外围能力描述 proof。它仅验证门户拥有的 JSON envelope、版本、字段范围和隐私边界；不加载 HIA 外部程序集、不开放 HTTP/消息/文件 transport，也不影响门户正常启动与发布。

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalHiaBoundary.ps1 -Configuration Debug
```

Proof 项目位于 `src\Portal.HiaBoundaryProof\`，未加入 `src\master.sln`，输出仅写入被忽略的 `temp\hia-boundary-proof\`。当前草案允许模块、主题、设置 registry 元数据、健康状态和受限诊断引用；不会接受用户/角色/认证、设置实际值、日志详情、审计正文、路径、连接串或任何凭据。

`Portal.Hia.InstanceId` 是可选的部署级、非敏感稳定标识。默认留空，留空不会启用任何对外适配器。需要为未来受控协作准备时，可设置为 GUID 或小写字母、数字、`.`、`_`、`-` 组成的稳定标识；不得由机器名、数据库名、域名、用户名或其他个人/环境信息推导。后续若接入真实 HIA consumer 或 transport，应先更新 ADR、契约版本和独立运行回归。

## P3 扩展性验收

`Test-PortalExtensionSmoke.ps1` 汇总 P3 的构建、前端资源、SQLite provider、HIA 契约、根站点和虚拟目录回归。默认不写数据库、不要求管理员凭据，也不会停止正在使用的 `40001` 站点；虚拟目录使用临时 IIS Express 端口并在结束后停止。

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

## 数据库初始化

参考 `src/ReadMe.txt`，基础流程为：

1. 为隔离环境准备仓库外 `connectionStrings.config`，并确保目标数据库不存在。
2. 优先用 `Initialize-PortalTestDatabase.ps1` 显式执行基础初始化、P2 与 P3 迁移；历史 SQL 文件仍保留供 Visual Studio/SSMS 人工维护使用。
3. 如确需手工执行，请按 `Portal_CreateDB.sql`、`Portal_LoadConfig.sql`、`Portal_LoadData.sql`、三份 `PortalCfg_*.sql` 的顺序操作，并确认数据库上下文与连接串一致。
4. 复制 `src/Portal/Config/Templates/connectionStrings.config` 到外置配置目录，并修改其中的 `Portal` 连接串。
5. 构建并运行站点。

部署或共享环境中不得使用默认账号和弱密码。

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
