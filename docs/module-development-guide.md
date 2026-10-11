# 部署式模块包开发指南

## 适用范围

新的业务模块采用"受信任部署模块包"机制。它保持 ASP.NET Web Forms 与 `.ascx` 用户控件路线：模块源文件由受信任的开发、构建和部署流程写入站点，管理员只从已验证包中注册和启用模块。

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

`PackageFolder` 必须匹配 `^[A-Za-z][A-Za-z0-9_-]{0,63}$`。`moduleFolder` 下的 `module.json` 当前对只读参考包（如 `ModuleProbe`）使用 `schemaVersion: 1`：

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

> **`schemaVersion` 现状（C-anp-P20 实测）**：`ModuleProbe` 这类只读参考包用 `1`；但**所有业务模块（`HIA.*`）的 manifest 实际为 `2`**——`PortalModuleControl<T>` 加载器要求 `capability` 块，而该块由 `schemaVersion: 2` 引入。`Test-PortalBusinessModuleSmoke.ps1` 目前仍打印"期望 `schemaVersion: 1`"，属门禁口径陈旧（全部业务模块均为 2），**不要**为过门禁把业务模块降回 1。

规则如下：

1. `packageId` 必须匹配 `^[A-Za-z][A-Za-z0-9_.-]{0,99}$`，并在已部署包中保持唯一。
2. `desktopEntry` 必须是当前包目录内的现有 `.ascx` 文件，且必须通过门户既有的 `DesktopModules/` 路径校验。
3. `resources` 必须是包目录内的现有相对文件；允许 `.css`、`.png`、`.jpg`、`.jpeg`、`.gif`、`.webp`。只有已声明 CSS 会由门户宿主自动去重挂载。
4. 清单不得出现 `script`、`scripts`、`externalUrl`、`externalUrls`、`assembly`、`assemblies`、`packageUrl`。这些能力需要未来的可信部署机制另行设计并审核。
5. 新模块应继承 `PortalModuleControl<T>`，实现 `IPortalModuleControl`。涉及公开 API、配置、安全边界或复杂流程时，使用标准 XML 注释中的中英双语段落。

## 代码骨架

入口控件的指令行使用**全限定类名**（命名空间为 `ASPNET.StarterKit.Portal`）：

```aspx
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EmployeeProfileConfirm.ascx.cs" Inherits="ASPNET.StarterKit.Portal.EmployeeProfileConfirm" %>
```

> **必须写全限定名**。`Inherits` 只写类名时，标记层在运行期找不到类，会抛 `CS0103`；而 **msbuild 编译不出这个错误**（代码后置已单独编译进 `bin/Portal.dll`）。这类问题只能由运行期门禁发现，详见 [开发指南](dev-guide.md) 与 `Test-PortalAscxCompilationContract.ps1`。

标记层与代码后置中的注释使用双语块：

```aspx
<%--
    <lang>
        <zh-CN>说明：员工只确认自己当前绑定的低敏资料，不提供上传或外部资源。</zh-CN>
        <en>Note: employees only confirm their currently bound low-sensitivity profile data; no upload or external resource capability is provided.</en>
    </lang>
--%>
```

## 权限与数据范围

权限键集中定义在 `src/Portal.Components/PortalPermissions.cs`，采用点分命名（`领域.对象.动作`）：

```csharp
public const string SettingsView = "Settings.View";
public const string SettingsEdit = "Settings.Edit";
public const string OpsDiagnosticsDetail = "Ops.Diagnostics.Detail";
```

模块在代码后置中引用这些常量，**不要在模块内硬编码字符串字面量**：

```csharp
ReviewRoleKey = PortalPermissionKeys.BusinessApplicationReview
```

数据范围（可见行）由 Foundation 层统一控制：需要按组织子树过滤的模块，应使用员工目录查询的子树参数，而不是自行拼接过滤条件，避免绕过范围控制。

新增权限键时，还需同步数据库授权数据（`src/Setup/PortalCfg_RolePermissions.sql` 一类脚本），并在提交前运行 `Test-PortalBusinessModuleSmoke.ps1` 与相关授权门禁。

## 语义标记与可访问性约定

门户对语义标记有一致约定，**新模块必须遵守**（这些约定已有运行期门禁守护）：

| 场景 | 正确写法 | 说明 |
| --- | --- | --- |
| 数据表头 | `<th scope="col">` | 不要留裸 `<th>` |
| 表单字段标签 | `<label class="…" for="<%= Xxx.ClientID %>">` | `class` 保持既有样式类，只加 `for` |
| **只读**字段 | `<dl>` + `<dt>` + `<dd>` | 标签后跟展示值而非表单控件时，**不要**用 `label for` |
| 模块标题 | 保持元素名与 class，只加 `role="heading"` 与 `aria-level` | 前台桌面 `aria-level="1"`；后台管理页 `= "2"` |
| 空态 | 走共享空态标记 | 不要自造"暂无数据"文案 |

两条容易做错的：

- **`for` 只能指向表单控件**。给只读展示字段加 `for` 是错误语义 —— 辅助技术会把标签关联到无关控件上，比不加更糟。只读字段用 `dl/dt/dd`。
- **不要为了语义改元素名**。`asp:Label` 渲染为 `span`，改成原生 `h1` 会改变视觉（除非另有 CSS 精确等价），且需新建自定义渲染控件。ARIA 标题是等价语义且视觉零变化。

CSS 应使用门户输出的稳定 scope（`portal-module`、`portal-module-{id}`、`portal-pane-{pane}`、`portal-package-{packageId}`），避免使用全局元素选择器污染其他模块；不要声明或随包分发专有字体，遵守项目的字体许可规则。

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

## 冒烟门禁与硬约束

每个新模块都应接入门禁编排，并单独通过冒烟检查：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalBusinessModuleSmoke.ps1 `
    -ModuleName HIA.YourModule `
    -SqlMigrationFile src\Setup\PortalBiz_YourEntities.sql
```

冒烟门禁强制以下约束（**均为 Fail 级，不可绕过**）：

| 约束 | 门禁检查名 |
| --- | --- |
| 包目录与 `module.json` 存在且可解析 | `Module directory` / `Module manifest` |
| `desktopEntry` 必须是包目录内的 `.ascx`，不得逃逸 | `Desktop entry safety` / `Desktop entry file` |
| 资源文件必须存在且不得逃逸包目录 | `Manifest resource safety` / `Manifest resource file` |
| 包内**不得有 DLL、ZIP、可执行文件或脚本文件** | `Module package static asset policy` |
| **默认禁止 JavaScript 文件**（除非显式 `-AllowModuleScripts`） | `Module script policy` |
| **默认禁止内联 `<script>` 块** | `Inline script policy` |
| 业务迁移脚本必须命名为 `PortalBiz_*.sql` | `SQL migration file` / `SQL migration naming` |
| 迁移脚本**不得含 `USE [database]`**（必须可移植到任意库名） | `SQL migration portability` |
| `packageId` 建议 `HIA.{ModuleName}` 风格 | `Package id convention`（Warning 级） |
| `version` 需为 SemVer 形态 | `Module version`（Warning 级） |

> **迁移文件按业务实体命名，不按模块命名**。例如 `HIA.MyWorkItems` 对应 `PortalBiz_WorkItems.sql`。找不到对应文件时需用 `-SkipSqlMigrationCheck` 显式豁免，并在提交信息里写明理由 —— 这是**如实豁免**，不是为过门禁而编造。

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

常见故障与定位：

| 现象 | 优先排查 |
| --- | --- |
| 前台 500 且提示找不到类 | `Inherits` 是否写了全限定名（见「代码骨架」） |
| 包在目录页不显示 | `module.json` 是否已部署、路径大小写、是否被状态表禁用 |
| 页面有模块但样式未生效 | `resources` 是否声明了 CSS、selector 是否用了稳定 scope |
| 门禁报 `Desktop entry safety` | `desktopEntry` 是否写了包目录外的路径或非 `.ascx` |
| 实例渲染但数据为空 | 是否绕过数据范围控制，或迁移脚本未执行 |
| 前台模块整页空白但构建通过 | 是否登记进 `Portal.csproj` 的 `<Compile>`；`.ascx` 是否用 `CodeBehind`；标记层类名与模块 CSS 是否一致；业务包是否加入 `Portal.ModulePackages.Enabled` |
| 模块在部分皮肤下样式丢失 | 对应皮肤的 `Default.css` 是否补了 `.<模块前缀类>` 主题作用域规则（仅模块自带 `Styles/*.css` 不够） |

## 新增模块的隐藏陷阱（C-anp-P20 实测）

以下四类缺陷**构建与单测全绿时不会出现，只在运行期以"页面空白 / 门禁误判"形式暴露**，是 `C-anp-P20` 交付两个业务模块时逐项踩出的。新增任何模块都先过一遍：

1. **代码后置必须登记进 `Portal.csproj`。** `src/Portal` 是 **Web Application 项目**（有 `Portal.csproj`），既有模块的 `.ascx.cs` 编译进 `bin/Portal.dll`。新增模块若只在磁盘上放文件、未在 csproj 登记 `<Compile Include="DesktopModules\{Module}\{Module}.ascx.cs">`（及 `.ascx.designer.cs`）与 `<Content Include="...{Module}.ascx / module.json / Styles/*.css">`，则 `CodeBehind` 在运行期报"未能加载类型 ASPNET.StarterKit.Portal.{Module}"；更隐蔽的是**未登记时 MSBuild 不编译这些文件**，于是 `out var`、`long→string` 一类编译错误只在运行期炸。
2. **6 套正式皮肤必须补齐主题作用域规则。** 只写模块自带 `Styles/{Module}.css` 而**不在 `src/Portal/App_Themes/<Theme>/Default.css` 补 `body.portal-theme-<规范化名> .<模块前缀类>` 规则**，模块在多数皮肤下丢失主题化样式（AGENTS.md 硬性要求）。取值取自各皮肤既有 `my-work-items` 同族调色板，并为不支持 `var()` 的 IE9+ 提供显式回退。
3. **标记层与 CSS 类名必须一致。** 标记层用 `form-grid` / `form-field` / `list` 而模块 CSS 写 `.grid` / `.field` / `.recent` 时，模块样式与主题规则会**全部落空却无任何报错**。以标记层（`.ascx` 的 `class=` 与 Web Forms 控件的 `CssClass=`）为权威，模块 CSS 与主题规则只承载布局与取色。
4. **本机配置生效源是 `Web.config` 的 `<appSettings>`。** 业务包需经 `Portal.ModulePackages.Enabled` 白名单放行；在开发机上改 `src/Portal/Config/appSettings.dev.json`（gitignore，不入库）的 `Set` 值**不落地**，运行期读的是 `Web.config` 的既有 `<appSettings>`。放行业务包时两处一并改，并确认 `appSettings.dev.json` 的档位（`Portal.ModuleProfiles.Active`）含该包所在档位。

> 上述 1–3 已由静态门禁 `Test-PortalNewModuleReadiness.ps1` 守护（L0 层，只依赖源码，无需 IIS/DB）：
> ```powershell
> & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalNewModuleReadiness.ps1 `
>     -ModuleName LeaveRequest -ModuleCssPrefix leave-request -ExpectedPackageId HIA.LeaveRequest
> ```
> 门禁检查：R1 csproj 登记、R2 `.ascx` 用 `CodeBehind`、R3 6 套皮肤均含 `<prefix>` 主题规则、R4 模块 CSS 类选择符均在 `.ascx` 出现（含 `CssClass`）。确有 JS/代码后置专属类时用 `-SkipClassConsistency` 据实豁免并在提交信息写明。

## 提交前检查清单

- [ ] `Inherits` 使用全限定类名
- [ ] `module.json` 字段合规，`desktopEntry` 与 `resources` 均在包目录内
- [ ] 权限键引用 `PortalPermissions.cs` 常量，无硬编码字面量
- [ ] 表头带 `scope="col"`；表单标签用 `label for`；只读字段用 `dl/dt/dd`
- [ ] 模块标题带 `role="heading"` 与 `aria-level`
- [ ] 无 JavaScript、无内联脚本、无 DLL/可执行文件
- [ ] 业务迁移脚本命名为 `PortalBiz_*.sql` 且不含 `USE [database]`
- [ ] `.ascx.cs` 与 `.ascx.designer.cs` 已登记进 `Portal.csproj` 的 `<Compile>`（Web Application 项目，遗漏会让 `CodeBehind` 运行期报"未能加载类型"）
- [ ] 6 套正式皮肤的 `Default.css` 均已补 `.<模块前缀类>` 主题作用域规则
- [ ] `Test-PortalNewModuleReadiness.ps1 -ModuleName <Module>` 通过（csproj / CodeBehind / 6 皮肤 / 标记与 CSS 类名一致性）
- [ ] `Test-PortalBusinessModuleSmoke.ps1 -ModuleName HIA.YourModule` 通过
- [ ] 新增接入编排：把该模块的冒烟检查与就绪检查加入 `dev/scripts/Invoke-PortalGateSuite.ps1`（L1 冒烟 / L0 就绪）
- [ ] 门禁套件全绿：`Invoke-PortalGateSuite.ps1`