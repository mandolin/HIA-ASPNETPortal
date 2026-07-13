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

脚本会通过 `dev\scripts\Find-MsBuild.ps1` 查找 Visual Studio / Build Tools 中的 MSBuild。当前已验证可使用 `d:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe` 完成 `Debug|Any CPU` 构建。

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

这些任务只调用仓库内的辅助脚本和 npm scripts，不修改 `.sln`、`.csproj`、`.csproj.user`，因此不会覆盖 Visual Studio 的既有调试设置。

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

## 数据库初始化

参考 `src/ReadMe.txt`，基础流程为：

1. 为隔离环境准备仓库外 `connectionStrings.config`，并确保目标数据库不存在。
2. 优先用 `Initialize-PortalTestDatabase.ps1` 显式执行基础初始化、P2 与 P3 迁移；历史 SQL 文件仍保留供 Visual Studio/SSMS 人工维护使用。
3. 如确需手工执行，请按 `Portal_CreateDB.sql`、`Portal_LoadConfig.sql`、`Portal_LoadData.sql`、三份 `PortalCfg_*.sql` 的顺序操作，并确认数据库上下文与连接串一致。
4. 复制 `src/Portal/Config/Templates/connectionStrings.config` 到外置配置目录，并修改其中的 `Portal` 连接串。
5. 构建并运行站点。

部署或共享环境中不得使用默认账号和弱密码。

## 前端资源

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

## 配置注意事项

- `src/Portal/Config` 下有 `*.template.*` 文件，也有实际环境文件。
- 修改真实配置前先确认是否应改模板。
- 连接串不再写入 `UnityCfg*.xml`，而是从外置 `{ExternalCfgPath}\{env}\connectionStrings.config` 读取。
- `ExternalCfgPath` 留空时默认使用 `{当前进程用户目录}\Web\HIA-ASPNETPortal\`。
- 本地 `dev` 默认文件路径为 `{当前进程用户目录}\Web\HIA-ASPNETPortal\dev\connectionStrings.config`。
- `connectionStrings.config` 中当前使用的逻辑名为 `Portal`，启动后会映射为 Unity 的 `connectionString` 命名实例。
- 环境变量 `HIA_ASPNETPORTAL_CONNSTR_PORTAL` 可覆盖具体连接串值；外置文件本身仍必须存在。
- 不要提交真实生产连接字符串、账号、密码、Token 或证书。
- `.gitignore` 已包含部分配置、数据库和构建产物忽略规则，但历史文件仍需人工核查。

## 文件编码

- 后续新增或修改的文本文件统一使用 UTF-8 无 BOM。
- 仓库根目录 `.editorconfig` 已设置 `charset = utf-8`。
- `src/Portal/.editorconfig` 因包含 `root = true`，也已单独设置 `charset = utf-8`。
- VSCode 工作区已设置 `"files.encoding": "utf8"`，对应 UTF-8 无签名。
