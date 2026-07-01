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

## 数据库初始化

参考 `src/ReadMe.txt`，基础流程为：

1. 执行 `src/Setup/Portal_CreateDB.sql`。
2. 执行 `src/Setup/Portal_LoadConfig.sql`。
3. 执行 `src/Setup/Portal_LoadData.sql`。
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
