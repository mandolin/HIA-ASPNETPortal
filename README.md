# 站点源码（HIA-ASPNETPortal 官网）

[![Publish site](https://github.com/mandolin/HIA-ASPNETPortal/actions/workflows/publish-site.yml/badge.svg)](https://github.com/mandolin/HIA-ASPNETPortal/actions/workflows/publish-site.yml)

<!-- markdownlint-disable MD033 -->
<p><a href="./en/README.md">English →</a></p>
<!-- markdownlint-enable MD033 -->

> 本目录是**官网站点源码**的版本化位置（C-anp-P22 `W-anp-P130` 建立），**不是运行期输出**。
> 页面由 `dev/scripts/Build-PortalSite.ps1` 从仓库 `docs/`（中文权威源）与
> `site/_translations/`（英文译本）**生成** —— 不另起一套脱离代码的文档（原则4）。

## 目录约定

| 路径 | 作用 |
| --- | --- |
| `_config.yml` | Jekyll 配置：主题 `Just the Docs`（远程主题）、`lang: zh-CN`、`color_scheme: light` |
| `_data/navigation.yml` | 导航骨架：**8 个分区**，每条含中文与英文标题（纯分区，不带 `url`） |
| `_data/published.yml` | **上架清单**（`doc` / `path` / `parent`）；只有在此列出的页面才会被生成 |
| `_translations/` | 英文译本源（与 `docs/` 中文档**一一对应**） |
| `index.md` | 中文首页 |
| `en/index.md` | 英文首页 |
| `<path>/index.md`、`en/<path>/index.md` | **生成产物**（中/英各一份），请勿手工编辑 |

视觉取值（配色 / 字体 / 排版）的规范见 [`docs/site-visual-baseline.md`](../docs/site-visual-baseline.md)。

## 本地生成

```powershell
./dev/scripts/Build-PortalSite.ps1
```

该脚本同时执行三项校验，任一不满足即退出码 1：

- **双语并行**：已声明上架的页面必须中英两版齐备，**缺译即失败**；
- **N1**：页面的 `parent` 必须存在于 `_data/navigation.yml`；
- **N2**：站点页面内的站内链接必须可达（这是 `W-anp-P132` 为"导航/首页链接全部 404 而门禁未发现"补的检查）。

另有独立的公开面门禁：

```powershell
./dev/scripts/Test-PortalSitePublicSurface.ps1   # S1 私有仓库链接 / S2 凭据形态 / S3 绝对本地路径 / S4 内部计划路径
```

## 发布

推送到 `main` 后，`.github/workflows/publish-site.yml` 会重新生成站点、跑三项静态门禁，
并把 `site/` 内容发布到 **`gh-pages` 分支**。

> **为何是 `gh-pages` 而不是 `/docs` 目录**：`docs/` 已被 27 个公开维护文档占用，
> 若作为 GitHub Pages 的 `/docs` 发布源，维护文档会被当成站点页面发布，且站点配置要塞进维护文档目录。
> 详细的选型取舍记录在本项目的**内部计划仓库**中（不随站点发布）。
>
> ⚠️ **不要在 `site/` 下写内部计划路径**：本目录会整体发布到公开官网，
> `Test-PortalSitePublicSurface.ps1` 的 S4 检查会拦截此类引用（`W-anp-P134` 首次运行即抓到本页原稿的引用）。

**启用步骤**（尚未执行）：

1. 仓库 Settings → Pages → 发布源设为 **`gh-pages` 分支**；
2. 观察首次 workflow 运行结果。

## 与本地 L0 门禁套件的关系

- CI 只跑**不需要编译的静态子集**（生成 + 双语/N1/N2 + 公开面 + 文档合规）；
- **完整 L0 套件**（`Invoke-PortalGateSuite.ps1 -Layer L0`）仍需在**本地**运行 —— 其中含
  .NET Framework 4.8 的构建类门禁，需要 Windows + Visual Studio/MSBuild。

两者**不重复**：重叠部分是**同一个脚本**，不是各写一套。

## 双语并行（本周期硬约束）

- 中文树位于站点根，英文树位于 `/en/`，**一一对应**；
- 一个页面**只有中英两版都就绪**才允许写进 `_data/published.yml`；
- 当前已上架 2 页，其余 `docs/` 文档按批次翻译推进。
