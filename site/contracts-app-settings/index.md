---
layout: default
lang: zh-CN
parent: 参考
nav_order: 31
permalink: /contracts-app-settings/
title: 配置契约（`appSettings`）
---
# 配置契约（`appSettings`）

> 权威源：`src/Portal/Web.config` 的 `<appSettings>`（基础值）；键常量分散定义，主键族见 `src/Portal/Components/PortalSettingKeys.cs` 与 `PortalModuleProfileResolver.cs`；环境覆盖见 `src/Portal/Config/appSettings*.json`。本页是提取快照，**冲突时以源为准**。

## 一、生效机制（先读这一节）

配置读取分两层，判断依据是**文件是否存在于站点根目录**：

1. **基础值**：`Web.config` 的 `<appSettings>` 始终先生效；
2. **覆盖值**：`AppSettingsLoader` 仅在其参数指向的 JSON 文件**确实存在于站点根目录**时，才把该文件的 `appSettings` 键写入 `ConfigurationManager.AppSettings`（覆盖同名键）；**文件不存在则静默跳过**。

由此产生两个实践结论：

- 开发机上改动**不入库**的 `src/Portal/Config/appSettings.dev.json` **不会落地**，本机运行期读的是 `Web.config` 的既有值；
- 部署环境把 `appSettings.json` 放进站点根目录后，该文件的覆盖**才会**生效。

键名是**平面点号键**（如 `Portal.ModuleProfiles.Active`）。直接对配置对象用 `$obj.'a.b.c'` 赋值会被当成嵌套路径导航而报错，须按键名字面处理。

完整机制与排错见 [../module-development-guide.md]({{ site.baseurl }}/module-development/)「新增模块的隐藏陷阱」第 4 条。

## 二、键族

默认值**不在此复制**（会过期）—— 查默认值请看 `src/Portal/Web.config` 的 `<appSettings>` 与各环境 `appSettings*.json`。

| 键族 | 代表键 | 作用 | 常量 / 读取位置 |
| --- | --- | --- | --- |
| 主题 | `Portal.Theme.Name` | 当前皮肤名 | `PortalSettingKeys.ThemeName` |
| 导航 | `Portal.Navigation.TabGateEnabled` | 页签可见性开关（仅显式真值启用） | `PortalNavigationVisibilityPolicy.IsTabGateEnabled`；读取处 `DesktopPortalBanner.ascx.cs` |
| 模块档位 | `Portal.ModuleProfiles.Active` | 当前激活档位名 | `PortalModuleProfileResolver`（`ActiveProfileKey`） |
| 模块档位 | `Portal.ModuleProfiles.<Profile>.Packages` | 该档位包含的包列表 | `PortalModuleProfileResolver`（前缀 `Portal.ModuleProfiles.`） |
| 模块档位 | `Portal.ModuleProfiles.<Profile>.Includes` | 该档位**包含**的其它档位（组合档位） | 同上 |
| 模块包放行 | `Portal.ModulePackages.Enabled` | 跨档位放行部署包（与档位无关） | `PortalModuleProfileResolver`（`EnabledPackagesKey`） |
| 诊断 | `Portal.Diagnostics.LogDirectory` / `EnableDetailedErrors` / `MaxFileBytes` / `RetentionDays` / `AllowAdminDetailView` | 日志目录、详细错误、单文件上限、保留天数、管理员明细视图 | `PortalSettingKeys` |
| 文档 | `Portal.Documents.AllowedExtensions` / `MaxUploadBytes` | 上传扩展名白名单与大小上限 | `PortalSettingKeys` |
| 安全 | `Portal.Security.AllowSelfRegistration` / `RequireRegistrationApproval` / `RequireEncryptedLoginPassword` | 自助注册、注册审批、登录口令加密提交 | `PortalSettingKeys` |
| 口令策略 | `Portal.Security.Password.MinimumLength` / `RequiredCategoryCount` / `WeakDictionaryEnabled` / `DisallowContextTerms` / `IterationCount` | 口令长度、字符类别数、弱字典、上下文词禁用、PBKDF2 迭代数 | `PortalSettingKeys` |
| Cookie | `Portal.Security.Cookies.Secure` / `SameSite` | Cookie 安全属性 | `PortalSettingKeys` |
| 注册 | `Portal.Registration.InviteDefaultExpiryDays` / `AllowPendingEmployeeBinding` | 邀请默认有效期、待绑定员工 | `PortalSettingKeys` |
| 实例 | `Portal.Hia.InstanceId` | 门户实例标识 | `PortalSettingKeys` |

## 三、模块档位与包放行（易混淆，单列）

两个机制**互相独立**，容易混为一谈：

| 机制 | 键 | 语义 |
| --- | --- | --- |
| **档位（Profile）** | `Portal.ModuleProfiles.Active` + `Portal.ModuleProfiles.<Profile>.Packages` | 决定"这一档包含哪些包"；组合档位可用 `.Includes` 引入其它档位 |
| **包放行（Enabled）** | `Portal.ModulePackages.Enabled` | **与档位无关**，用于跨档位放行部署包；业务模块（如请假 / 费用报销）默认不随核心档位启用，需在此追加 |

> 实践要点：新增业务模块时**两处都要改** —— 包要进 `Enabled` 放行，且其所在档位要被 `Active` 激活或经 `.Includes` 引入。改完不生效先查第一节的"生效机制"。

## 四、相关文档

- 启用业务模块的操作步骤：[../user-guide.md]({{ site.baseurl }}/user-guide/)「业务模块：请假申请与费用报销」。
- 部署配置：[../deployment-checklist.md]({{ site.baseurl }}/deployment-checklist/)。
- 外置连接串（**不入库**）：见 [../dev-guide.md]({{ site.baseurl }}/dev-guide/) 与 `src/Portal/Config/Templates/connectionStrings.config`。

<!-- build-stamp: 08931BEA1AE4 -->
