# 部署式模块包开发指南

## 适用范围

从 W-anp-P3.2 起，新的业务模块采用“受信任部署模块包”机制。它保持 ASP.NET Web Forms 与 `.ascx` 用户控件路线：模块源文件由受信任的开发、构建和部署流程写入站点，管理员只从已验证包中注册和启用模块。

当前版本不支持后台 ZIP/DLL 上传、在线编译、在线编辑模块文件、外部 URL、远程下载、自动加载 JavaScript 或包自带数据库迁移。已有平铺的 `DesktopModules/*.ascx` 和 `Admin/*.ascx` 继续作为 Legacy 模块运行，不要求批量迁移。

## 目录与清单

一个新模块包位于 `src/Portal/DesktopModules/{PackageFolder}/`，例如：

```text
DesktopModules/
  ModuleProbe/
    ModuleProbe.ascx
    ModuleProbe.ascx.cs
    ModuleProbe.ascx.designer.cs
    module.json
    Styles/
      ModuleProbe.css
```

`PackageFolder` 必须匹配 `^[A-Za-z][A-Za-z0-9_-]{0,63}$`。`module.json` 当前使用 `schemaVersion: 1`：

```json
{
  "schemaVersion": 1,
  "packageId": "HIA.ModuleProbe",
  "displayName": "模块验证 / Module Probe",
  "version": "1.0.0",
  "minimumPortalVersion": "1.0",
  "desktopEntry": "DesktopModules/ModuleProbe/ModuleProbe.ascx",
  "resources": [
    "Styles/ModuleProbe.css"
  ]
}
```

规则如下：

1. `packageId` 必须匹配 `^[A-Za-z][A-Za-z0-9_.-]{0,99}$`，并在已部署包中保持唯一。
2. `desktopEntry` 必须是当前包目录内的现有 `.ascx` 文件，且必须通过门户既有的 `DesktopModules/` 路径校验。
3. `resources` 必须是包目录内的现有相对文件；允许 `.css`、`.png`、`.jpg`、`.jpeg`、`.gif`、`.webp`。只有已声明 CSS 会由门户宿主自动去重挂载。
4. 清单不得出现 `script`、`scripts`、`externalUrl`、`externalUrls`、`assembly`、`assemblies`、`packageUrl`。这些能力需要未来的可信部署机制另行设计并审核。
5. 新模块应继承 `PortalModuleControl<T>`，实现 `IPortalModuleControl`。涉及公开 API、配置、安全边界或复杂流程时，使用标准 XML 注释中的中英双语段落。

模块 CSS 应使用门户输出的稳定 scope，例如 `portal-module`、`portal-module-{id}`、`portal-pane-{pane}` 和 `portal-package-{packageId}`。不要声明或随包分发专有字体；遵守项目的字体许可规则。

## 安装与启用

1. 先在 Visual Studio 或 VSCode 构建解决方案，完成受信任部署流程后把包目录部署到 `DesktopModules/`。
2. 对目标数据库执行 `src/Setup/PortalCfg_ModulePackageStates.sql`，或使用 SQL 兼容性脚本：

   ```powershell
   & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalSqlCompatibility.ps1 `
       -ConnectionStringsConfigPath $testConfig -ApplyP3Migrations -RequireP3Migrations
   ```

3. 以 `Admins` 身份访问 `Admin/ModuleCatalog.aspx`。目录页只显示已部署且通过校验的包；点击 **Register** 会创建指向 manifest `desktopEntry` 的旧模块定义记录。
4. 在既有 Tab 布局管理页面从已注册定义中添加实例。新模块默认使用 `CacheTimeout=0`；缓存策略需要单独评估后再调整。
5. 已注册包可在目录页 **Enable** 或 **Disable**。状态写入 `PortalCfg_ModulePackageStates`，并记录运营审计。

包状态表中不存在记录时，已验证包按启用处理，兼容尚未配置状态行的部署。状态表缺失或不可读时，前台同样保持默认启用，但后台状态写入会提示先执行迁移。

## 生命周期与移除

模块包生命周期为：`Available` -> `Registered` -> `Enabled` 或 `Disabled` -> `UninstallReady`。

禁用只阻止该包的实例渲染和 CSS 挂载，不删除模块实例、模块业务数据或物理目录。目录页的 **Preflight** 会显示对应定义和实例数量。存在实例时，旧定义页会拒绝直接删除，必须先禁用、迁移或明确清理实例及其业务数据。

物理目录删除仍是受信任部署操作：先完成预检和实例清理，再在部署流程中移除包目录，最后重启应用或等待应用域刷新。后台不会删除模块物理目录。

## 验证与故障处理

`ModuleProbe` 是只读参考包，用于验证注册、启停、CSS、缓存身份和虚拟路径，不写入业务数据。排查新包时：

1. 先确认 `module.json`、入口控件和声明资源都已部署，且路径大小写与清单一致。
2. 在 `Admin/ModuleCatalog.aspx` 确认包被发现，并执行 **Preflight**。
3. 确认已注册定义和 Tab 实例存在；禁用状态下前台应跳过该包实例。
4. 查看 `Admin/DiagnosticsLogs.aspx` 中的 `ModulePackage.*` 事件。日志中不应依赖连接串、Cookie、Token 或密码。
