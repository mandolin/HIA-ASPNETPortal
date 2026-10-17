---
layout: default
lang: zh-CN
parent: 参考
permalink: /site-visual-baseline/
title: 官网视觉基线
---
> 本页是官网的**视觉基线规范**：规定配色、字体、排版层级、代码块与表格取色、以及导航骨架的呈现约定。
> 它是 `site/_config.yml` 与 `site/_sass/`（如有覆盖）的取值依据 —— **改这里，不要直接改生成产物**。
>
> **同源纪律**：下列取值**不是新设计**，而是从项目既有 CSS 提取，与产品门户保持一致。
> 权威源：`src/Portal/App_Themes/EnterpriseLight/Default.css`（`--portal-*` 变量块）与
> `src/Portal/App_Themes/Default/Default.css`（字体栈与字号）。快照过期时以 CSS 为准。

# 官网视觉基线

## 1. 基调：浅色

采用 **`EnterpriseLight` 的浅色基调** —— 与产品默认浅色皮肤同源，使官网与产品视觉一致。

取舍：官网是**独立静态站**（GitHub Pages），**不需要 IE9+ 兼容**，故可直接使用 CSS 变量与现代布局；
但**配色与字体仍取自项目 CSS**，避免"官网一套、产品一套"的割裂。

> 注意：项目主题层本身受 IE9+ 约束（不得依赖 CSS variables / Grid / Flexbox / 远程字体 / 主题脚本），
> 该约束**不适用于官网**；官网只继承其**取值**，不继承其兼容约束。

## 2. 配色

取自 `EnterpriseLight` 的 `--portal-*` 语义变量（浅色族；`Default`/`EnterpriseLight`/`OaLight`/`StateClassicLight` 取值相同）：

| 用途 | 变量 | 取值 |
| --- | --- | --- |
| 页面底色 / 卡片容器 | `--portal-surface` | `#ffffff` |
| 次级分区（表头、分组块、代码块底） | `--portal-surface-2` | `#f5f8fb` |
| 容器外边框 | `--portal-border` | `#d8dee8` |
| 分隔线 / 弱边框 | `--portal-border-subtle` | `#edf1f5` |
| 强调边框（焦点态、选中态） | `--portal-border-strong` | `#c2ccd8` |
| 标题 / 强文本 | `--portal-text-strong` | `#2f475f` |
| 说明文字 / 弱化文本 / 占位文案 | `--portal-text-muted` | `#5b6b7d` |
| 副标题层级 | `--portal-text-subtle` | `#41556b` |
| 输入控件背景 | `--portal-field-bg` | `#ffffff` |
| 输入控件边框 | `--portal-field-border` | `#cdd7e2` |
| 输入控件文字 | `--portal-field-text` | `#1f2d3d` |
| 列表表头背景 | `--portal-list-header-bg` | `#eef2f7` |
| 列表隔行底色 | `--portal-list-row-alt` | `#f7f9fc` |
| 错误 / 失败 / 校验反馈 | `--portal-danger-text` | `#b42318` |

> `--portal-text-subtle` 由 主题工程裁定新增，用于标题与副标题的色层次。
> `--portal-danger-text` 是唯一按主题族区分的变量（见 `docs/theme-semantic-variables.md`）；
> 官网取 Enterprise 族浅色值 `#b42318`。

## 3. 字体

字体栈直接沿用项目 CSS（开源字体优先 + generic fallback），**不引入远程字体**：

```
"Noto Sans SC", "Source Han Sans SC", "Sarasa Gothic SC", "Liberation Sans", sans-serif
```

等宽（代码块）使用站点主题的等宽栈，若无则退回 `monospace` generic fallback。

> 字体许可证边界见 `docs/font-policy-and-audit.md`；前端资源约束见 `docs/frontend-asset-guide.md`。

## 4. 排版层级

沿用项目 CSS 的字号梯度（从 `Default.css` 提取的既有层级），官网在此基础上统一行高：

| 层级 | 字号 | 说明 |
| --- | --- | --- |
| 页面标题（H1） | 20px | 与项目标题字号一致 |
| 正文 / 说明（H2 及以下同正文） | 13px | 与项目正文字号一致 |
| 辅助说明 / 脚注 | 11px | 与项目小字号一致 |

正文行高取 **1.6**（阅读长文的可读性基线），标题行高取 **1.3**。

> 项目门户内受 IE9+ 约束不依赖 CSS 变量；官网作为独立站**可以**用变量，但**取值仍取自上表**，以免两边漂移。

## 5. 代码块

- 底色：`--portal-surface-2`（`#f5f8fb`），与卡片分区同色，避免"贴白墙"无边界感；
- 边框：`--portal-border-subtle`（`#edf1f5`）；
- 字体：等宽栈；
- 长行**横向滚动**而非换行（避免命令被折行后复制出错）；
- 语言标注必须显式给出（防止高亮缺失时读者误判）。

## 6. 表格

- 表头背景：`--portal-list-header-bg`（`#eef2f7`）；
- 隔行底色：`--portal-list-row-alt`（`#f7f9fc`）；
- 单元格边框：`--portal-border-subtle`。

> 宽表在窄屏下横向滚动；不得用缩小字号的方式硬塞。

## 7. 导航骨架

两棵一一对应的树，**导航顶层条目沿用 `docs/documentation-map.md` 的读者路径**：

产品入口 → 评估 → 快速上手 → 学习 → 参考 → 开发 → 运维 → 变更

| 约定 | 说明 |
| --- | --- |
| 侧栏导航 | 由 `site/_data/navigation.yml` 驱动（Just the Docs） |
| 语言切换 | 每个页面的 front matter `lang` 决定所属树；中文页链 `/en/<同路径>/`，英文页链 `/<同路径>/` |
| 一一对应 | 任一页面都同时提供中英两版，可随时切换 |

## 8. 中英双语并行的排版约定

- 中文树位于站点根，英文树位于 `/en/`；**路径与文件名一一对应**（如 `product-overview/` ↔ `en/product-overview/`）。
- 每页在**顶部**提供对译语言入口（首页已含），保证任意页面都能一键切换。
- 双语**不得**只用机器占位：译本与原文须语义对等，由人工走查确认（见 `docs/documentation-review-checklist.md`）。

## 9. `<html lang>` 覆盖与主题版本耦合

**背景**：上游 `just-the-docs` 的 `_layouts/default.html` 第 7 行只读**站点级** `site.lang`，**不看 `page.lang`**，
于是英文页也输出 `<html lang="zh-CN">`，影响可访问性（屏幕阅读器用中文发音读英文内容）与 SEO。

**已采取的处置**：

1. `site/_layouts/default.html` 是上游 layout 的**受控覆盖**，与上游的**唯一差异**是 `<html lang>` 改为
   `{{ page.lang | default: site.lang | default: 'en-US' }}`。其余行逐字复制，**不做顺手优化**（否则将来无法与上游 diff）。
2. `_config.yml` 的 `remote_theme` **固定到 `v0.12.0`**，与本地 gem 一致 —— 否则 Pages 拉到更新版主题、其 include/Sass
   已变而覆盖文件仍是旧版，会造成"部分新部分旧"的错配。

**升级主题时的动作**：取新版 `_layouts/default.html`，只把 `<html lang>` 一行替换为上述表达式，其余照抄；
若上游在别处也改了 `<html>` 或 `head.html` 的 lang 逻辑，须一并复核。

**两条实测得到的教训（写在这里以免重犯）**：

- **不要在 layout 注释里写字面的 html-lang 标记样例**。覆盖文件初版的注释里写了一处这样的样例，
  结果任何基于该标记的检查都**先命中注释里的假标记**，把"修复已生效"误判成"修复失败"，
  并引出三轮无效排查。**注释中的示例文本不得与真实标记形成可匹配的同形文本。**
- **不要在该 layout 里使用 Liquid 注释 `{#- ... -#}`**。曾用 Liquid 注释写双语说明，结果整个 layout 被静默旁路
  （Jekyll 回退到主题 layout），页面看起来正常、但 lang 修复完全失效。说明文字一律用 HTML 注释或独立文档承载。

## 10. HIA 系列主配色与 landing 样式（同源接线）

`site/_config.yml` 的 `color_scheme` 为自定义方案 **`hia`**（定义在 `site/_sass/color_schemes/hia.scss`）。

- **配色权威源（用户指定）**：HIA 系列自己的设计令牌 ——
  `HIA-Theme-Builder/HIA/web/src/style.css` 的 `:root`
  （primary `#0047AB`、secondary `#00A8D3`、蓝色阶 p50–p900、cyan 阶 s50–s900、
  `surface #F3F6F9` / `surface-2 #EAF0F5` / `panel #FFFFFF` / `line #DCE5EC`、
  `text #172633` / `muted #667782`、字体 Inter + Noto Sans CJK SC）。
  本文件第 2 节的 `--portal-*` 矩阵仍是**产品门户皮肤**的取值依据（站点与其保持同源的产品语义，
  但站点自身品牌色以 HIA 系列令牌为准）。
- **landing 样式**：`site/_sass/custom/custom.scss` + `site/_layouts/home.html`（主题官方覆盖点与自定义 layout）。
  **对标基准为 orchardcore.net**（用户指定），且照它"主站与文档两套风格"的做法：**首页/营销页**照搬
  主站板块顺序——自带顶部导航、hero（大字标题 + 可复制命令块 + 双 CTA）、统计行、全大写小标题的能力卡与功能卡栅格、
  CTA 带、入手方式、FAQ 折叠（原生 `<details>`，无 JS 依赖）、多列页脚；
  **文档页**重写为 docs.orchardcore.net（Material for MkDocs）的风格：深色顶栏（品牌 + 搜索框 +
  仓库链接 + 语言切换）、左侧导航树（沿用主题组件重新着色）、**无面包屑**（与 Material 一致）、
  正文 + **右栏 sticky「本页目录」**（由 layout 对渲染 HTML 按 h2 拆分生成）、同一分区内**上一篇/下一篇**分页、
  大写标题带锚点、admonition 式引用块（HIA p50 底 + 主色左条）、**深色代码块**、深色页脚条。
  两套风格刻意不同（对应 OC 主站与文档子站的划分）。板块结构照搬，配色保持 HIA 令牌。
- **变量名**：对照本地 gem `just-the-docs v0.12.0` 的 `_sass/color_schemes/light.scss`
  逐一核实后使用，非猜测。
- **代码高亮**：方案文件末尾保留上游的
  `@import "./vendor/accessible-pygments/github-light"`（与 `light.scss` 一致）。
- **升级主题时**：重新核对新版 `light.scss` 的变量名；若上游改名或增删调色板变量，
  需同步方案文件与 custom.scss。本机制走主题支持的 `color_scheme` 与 custom 挂钩，不触碰主题内部文件。
