---
layout: default
lang: zh-CN
parent: 参考
nav_order: 2
permalink: /module-mechanics-and-extension/
title: 模块运作与二次开发：模块是怎么装上门户的
---
# 模块运作与二次开发：模块是怎么装上门户的

这页是**总纲**：给你一张地图，知道模块从代码到出现在页面上要经过哪几层、每层的配置在哪个文件、哪条约束会让你的改动被门禁打回。每一层的细节都有专页，这里只给骨架。

## 五个层次

| 层 | 是什么 | 存在哪 | 通常谁改 |
| --- | --- | --- | --- |
| 模块包 | 一个可部署的模块单元，带清单文件 `module.json` | `src/Portal/DesktopModules/<名字>/module.json` | 开发者 |
| 档位（Profile） | 一组"放行哪些包"的清单 | `src/Portal/Config/appSettings*.json` 里的 `Portal.ModuleProfiles.*` | 部署侧 |
| 模块类型定义 | 模块的**类型**：标识、来源文件、标题 | 库表 `PortalCfg_ModuleDefinitions` | 管理员 / 部署 |
| 模块实例 | 某个定义在某个页签上的一次**挂载**，带标题、顺序、编辑角色 | 库表 `PortalCfg_Modules` | 管理员（后台「模块设置」） |
| 页签 | 导航与内容的容器 | 库表 `PortalCfg_Tabs` | 管理员（后台「页签名称与布局」） |

清单文件长这样（字段名固定）：

```json
{
  "schemaVersion": 2,
  "packageId": "HIA.Help",
  "displayName": "帮助与说明 / Help",
  "version": "1.0.0",
  "minimumPortalVersion": "2.0.0",
  "desktopEntry": "DesktopModules/Help/Help.ascx",
  "resources": []
}
```

`packageId` 是包的唯一身份；档位、门禁、登记全都按它对账。

## 档位：决定哪些模块能用

- `Portal.ModuleProfiles.Active` 选当前生效的档位（如 `CoreOnly`、`BusinessWorkflow`）。
- `Portal.ModuleProfiles.<档位>.Includes` 写这个档位**包含**哪些其它档位。
- `Portal.ModuleProfiles.<档位>.Packages` 写这个档位**放行**哪些包。

没被任何生效档位放行的包，其模块**不渲染**。档位是"整组开关"，不是逐个模块勾选。

## 加一个自己的模块

1. **写模块**：控件（`*.ascx`）加清单（`module.json`）。控件里不要读原始配置键、不要在标记里判定权限。
2. **登记类型定义**：在模块类型定义里登记它的标识与来源文件（后台「模块类型定义」，或部署时写库）。
3. **挂实例**：在目标页签上挂一个实例，并给标题、顺序、编辑角色（后台「模块设置」）。
4. **确认档位放行**：把 `packageId` 加进生效档位的 `Packages`；**不打算挂载**的包，要登记进 `notMounted`，否则包契约门禁会判它"无档位覆盖"。

## 改外观

门户有 **8 套主题**（Enterprise / StateClassic / Oa 的 Light + Dark，加 Default 与 ThemeProbe）。主题包用这些作用域 class 定位：

| class | 含义 |
| --- | --- |
| `portal-theme-{名字}` | 当前主题 |
| `portal-tab-{页签 Id}` | 当前页签 |
| `portal-module-{实例 Id}` | 当前模块实例 |
| `portal-pane-{窗格名}` | 所在窗格（left / content / right） |

主题包的硬约束（写在 `Default/Default.css` 的注释里）：**不得依赖远程资源、主题脚本、CSS 变量、Grid 或 Flexbox** —— 本项目支持 IE9，颜色还要按"显式 hex 在前、变量在后"的顺序写双声明。

## 四道门禁会盯着你

| 门禁 | 盯什么 |
| --- | --- |
| 模块包契约（M1–M5） | 包的登记与档位覆盖是否自洽：`Enabled` 必须先登记；所有档位 `Packages` 取并集；每个 `module.json` 的 `packageId` 必须被"档位并集 / Enabled / notMounted"之一覆盖；`notMounted` 不得又被档位覆盖；`appendAllowList` 与 `notMounted` 不得重叠 |
| 资源契约（C1–C6） | 中性 / 中文 / 英文三份 resx 的键集一致、强类型属性与键**双向一一对应**、`<summary>` 必须含双语块、英文不得漂移、中性值不得含中文、标记与代码引用的键必须存在 |
| 无障碍 | 标记里的 `alt` / `label` / 标题语义 |
| Legacy CSS | IE9 基础路径（禁 Grid / Flexbox / `var()` 兜底）与颜色双声明 |

资源契约最容易踩的一条：**键名漂移是静默失败** —— 资源文件里找不到键时 `GetString` 返回 `null` 而不是抛异常，界面显示空白、日志无痕。所以键的存在性由门禁兜底，不靠异常机制。

## API 文档在哪

- `/api/dotnetdoc/`：**源码级** API 索引（类型、成员、源码行），从 XML 文档注释生成。
- `/api/jsdoc/`：前端脚本的 API 文档。

两者都是站点的静态产物，由发布脚本重新生成并提交快照；改了源码注释却没刷新快照，代码文档门禁会判"源指纹不一致"。

## 这页没讲的事

- 模块包契约的完整判据 → [模块清单契约]({{ site.baseurl }}/contracts-module-manifest/)
- 模块开发的具体边界与注册流程 → [模块开发指南]({{ site.baseurl }}/module-development/)
- 主题包的写法与外观族 → [主题包指南]({{ site.baseurl }}/theme-package-guide/) 与 [主题模块契约]({{ site.baseurl }}/theme-module-contract/)
- 页签可见性怎么配 → [系统设置契约]({{ site.baseurl }}/contracts-app-settings/)

<!-- build-stamp: F7DCD15CFBA4 -->
