> `C-anp-P22`（项目官网）`W-anp-P131` 交付。原则1 gate：**先出站点视觉原型，再落地内容填充**。
>
> 本页是官网的**视觉基线规范**：规定配色、字体、排版层级、代码块与表格取色、以及导航骨架的呈现约定。
> 它是 `site/_config.yml` 与 `site/_sass/`（如有覆盖）的取值依据 —— **改这里，不要直接改生成产物**。
>
> **同源纪律（原则4）**：下列取值**不是新设计**，而是从项目既有 CSS 提取，与产品门户保持一致。
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

> `--portal-text-subtle` 由 `C-anp-P23` D3 裁定新增，用于标题与副标题的色层次。
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
| 一一对应 | 任一上架页面必须中英两版齐备，缺译即上架声明无效（`Build-PortalSite.ps1` 门禁） |

## 8. 中英双语并行的排版约定

- 中文树位于站点根，英文树位于 `/en/`；**路径与文件名一一对应**（如 `product-overview/` ↔ `en/product-overview/`）。
- 每页在**顶部**提供对译语言入口（首页已含），保证任意页面都能一键切换。
- 双语**不得**只用机器占位：译本与原文须语义对等，由人工走查确认（见 `docs/documentation-review-checklist.md`）。
