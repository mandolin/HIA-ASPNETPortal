# 技术契约（HIA-ASPNETPortal）

> **P21 `W-anp-P125` 交付物**。本目录收纳本项目的**机器可读契约 + 人类可读说明**，对应文档族「API 与技术契约」。权威源一律是**代码与配置文件本身**，本目录的说明是提取快照；**两者冲突时以源为准**。

## 一、边界判定（先读这一节）

本项目是 **ASP.NET Web Forms 门户（.NET Framework 4.8）**，**没有对外 REST / GraphQL API**，也不提供 OpenAPI 一类的机器可读接口契约。

因此：

| 常见"API 文档"形态 | 本项目是否适用 | 理由 |
| --- | --- | --- |
| 公开 REST API 参考 | **不适用** | 架构无此类对外面 |
| GraphQL schema 参考 | **不适用** | 同上 |
| OpenAPI / Swagger | **不适用** | 同上 |
| **模块契约** | **适用** | 受信任模块包以 `module.json` 声明，有 schema 校验 |
| **配置契约** | **适用** | `appSettings` 平面点号键族 |
| **企业能力对象契约** | **适用** | 能力键与角色构成封闭词表 |

边界判定的完整登记见 [documentation-registry.md](../documentation-registry.md) 第 2 族。

## 二、契约清单

| 契约 | 权威源（机器可读） | 人类可读说明 |
| --- | --- | --- |
| **模块契约** | `DesktopModules/<模块>/module.json`；校验实现 `src/Portal/Components/PortalModulePackage.cs` | [module-manifest.md](module-manifest.md) |
| **配置契约** | `src/Portal/Web.config` 的 `<appSettings>`；键常量 `src/Portal/Components/PortalSettingKeys.cs`；环境覆盖 `src/Portal/Config/appSettings*.json` | [app-settings.md](app-settings.md) |
| **企业能力对象契约** | 能力注册表 `src/Portal.Components/PortalCapabilityRegistry.cs`；角色词表 `src/Portal.Components/PortalCapabilityRoles.cs` | [module-manifest.md](module-manifest.md)「企业能力（capability）」一节 |

## 三、共同约束

1. **受信任边界**：模块包是"受信任部署"模型 —— 只允许受控 ASCX 入口与站内静态资源；manifest 中声明 `script` / `scripts` / `externalUrl` / `externalUrls` / 程序集 / 包 URL 会被**显式拒绝**，manifest 声明不能改变执行或下载能力。
2. **默认值以源为准**：本目录不复制默认值（会过期）。查默认值请看 `src/Portal/Web.config` 的 `<appSettings>` 与各环境 `appSettings*.json`。
3. **配置生效源**：`Web.config` 是基础值；只有**部署到站点根目录**的 `appSettings.json` 才会覆盖同名键（不存在则静默跳过）。见 [../module-development-guide.md](../module-development-guide.md)「新增模块的隐藏陷阱」第 4 条。
4. **契约变更须同步**：改契约实现时，同步更新本目录对应说明与 `PortalSettingKeys.cs` 等常量源；验证方式是跑门禁套件（`dev/scripts/Invoke-PortalGateSuite.ps1`）。

## 四、验证

- 模块契约的机器校验：`dev/scripts/Test-PortalNewModuleReadiness.ps1`、`Test-PortalBusinessModuleSmoke.ps1`（L0）。
- 资源与主题契约：`Test-PortalModuleSkinCoverage.ps1`（L0）。
- 具体命令见 [../testing-checklist.md](../testing-checklist.md)。
