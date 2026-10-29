---
layout: default
lang: zh-CN
parent: 开发
nav_order: 4
permalink: /theme-module-contract/
title: 模块外观契约（Theme → Module Contract）
---
# 模块外观契约（Theme → Module Contract）

本文件约定**业务模块外观**与**主题层**之间的分工：模块侧负责布局与结构，主题层负责颜色；并登记 `var(--portal-*, 回退值)` 中回退值的真实含义与已知不一致。

> 权威性同 `theme-semantic-variables.md`：本文件是代码实况的提取快照，机器可检查的权威来源是 CSS 与对应门禁。

## 一、分工原则

| 层 | 负责 | 不负责 |
| --- | --- | --- |
| 模块 `Styles/<Module>.css` | 布局、结构、间距、控件排布 | **不写死颜色** |
| 主题 `App_Themes/<Theme>/Default.css` | 颜色（经 `--portal-*` 语义变量） | 不管模块布局 |
| 门户基础层（`.Head` / `.SubHead` / `.CommandButton` / `.NormalTextBox` / `.portal-*`） | 通用控件与外壳形态 | 不管模块私有布局 |

模块私有类**只承担布局**；颜色一律 `var(--portal-*, 回退值)`。变量矩阵见 [`theme-semantic-variables.md`]({{ site.baseurl }}/theme-semantic-variables/)。

## 二、模块外观类族与所用变量

七个业务模块（`BusinessApplicationRequest` / `EmployeeProfileConfirm` / `EmployeeProfileCorrectionRequest` / `EnterpriseCapabilityWorkbench` / `ExpenseReimbursement` / `LeaveRequest` / `ModuleProbe`）的外观结构同构，按职责分为七族：

| 类族 | 典型类名 | 用途 | 应用变量 |
| --- | --- | --- | --- |
| 容器 | `<module>-request` / `-panel` | 模块卡片边界与底色 | `--portal-surface`、`--portal-border` |
| 标题 | `<module>-title` | 模块内主标题 | `--portal-text-strong` |
| 副标题 | `<module>-subtitle` | 表单/列表分区标题 | `--portal-text-strong` |
| 标签 | `<module>-label` | 字段标签（叠加 `.SubHead`） | 继承 `.SubHead`，不单独取色 |
| 输入 | `<module>-input` / `-body` | 输入控件（叠加 `.NormalTextBox`） | `--portal-field-bg`、`--portal-field-border`、`--portal-field-text` |
| 消息 | `<module>-message` | 保存/校验/失败反馈 | `--portal-danger-text` |
| 列表 | `<module>-list` / `-list-wrap` | 数据表与包裹层 | `--portal-border-subtle`（分隔线） |
| 空态 | `.portal-empty-state`（共享类） | 零条/筛选无结果提示 | 各皮肤中性色（复用 `.portal-admin-subtitle` 一族取值） |

## 三、回退值的真实含义（重点）

`var(--portal-danger-text, #8a1f1f)` 里的 `#8a1f1f` **不是设计值**，而是该模块**迁移前硬编码的原值**，保留它只为「不支持 CSS 自定义属性的旧内核仍得原色」。

**因此：读模块 CSS 时不得把回退值当作设计值。** 设计值在主题层，矩阵见 `theme-semantic-variables.md`。

### 已审计的回退值与主题取值差异

| 现象 | 涉及 | 说明 |
| --- | --- | --- |
| 记法差异（同色） | `#fff` ↔ `#ffffff` | 仅缩写记法不同，颜色相同，无影响 |
| **同一变量两套回退值** | `--portal-text-strong`：标题 `#2f475f` / 副标题 `#41556b` | **原设计标题与副标题不同色**；迁移后两者都取主题同一个值 → 颜色层次被抹平（见下） |
| 回退值偏离主题值 | `--portal-text-muted`：`#6b7c8f` ≠ 主题 `#5b6b7d` | 旧内核下会比主题略深 |
| 整模块自有调色板 | `ModuleProbe` 6 处回退值（`#52718a` / `#f4f8fb` / `#1f3546` / `#1b4f72` / `#496274` / `#31495c`） | 诊断模块自有 palette，与主题值无一相同；它本就是低优先诊断面 |
| 错误色 | `--portal-danger-text`：`#8a1f1f` ≠ 主题浅色 `#b42318` | 已把模块侧改取变量，此处差异只在旧内核回退时可见 |

### D3 裁定：副标题与标题的颜色层次（已结）

**问题**：迁移前标题 `#2f475f`、副标题 `#41556b`（副标题略浅）；迁移后两者都解析为主题 `--portal-text-strong`（浅色族 `#2f475f`、深色族 `#e6edf5`），**副标题变得和标题一样深**，颜色层次消失。

**D3 裁定采用（b）新增变量**：新增 `--portal-text-subtle`，取值介于 `--portal-text-strong` 与 `--portal-text-muted` 之间，7 套主题各定义一次（浅色族 `#41556b`、深色族 `#c2cadf`），恢复副标题的中间色。矩阵见 [`theme-semantic-variables.md`]({{ site.baseurl }}/theme-semantic-variables/)。

被否决的两个选项：

- **（a）接受**同色、靠字号字重区分层级 —— 未采纳：颜色层次是原设计意图，仅靠字号字重不足以承载。
- **（c）副标题改用 `--portal-text-muted`** —— 未采纳：会变得更浅，偏离原意。

## 四、新增模块外观的检查单

1. 布局与结构写在模块 `Styles/<Module>.css`；
2. 需要颜色处一律 `var(--portal-*, 回退值)`，回退值取该规则此前的原值；
3. 若需要**主题层没有的语义**，先读 `theme-semantic-variables.md` 的「新增变量分类推论」：表达明暗基调的加入浅/深两套，表达主题族固有属性的逐主题族取值并优先复用既有类；
4. **7 套主题的 `Default.css` 都要补该类的作用域规则**（`body.portal-theme-<名> .<模块类>`），漏补会导致该模块在多数皮肤下丢失主题化样式 —— 视为未完成；
5. 若界面形态发生变化，先出真实渲染原型（浅+深）并经确认，再动 CSS。

## 五、相关文档

- [`theme-semantic-variables.md`]({{ site.baseurl }}/theme-semantic-variables/)：`--portal-*` 变量矩阵与使用规则。
- [`theme-package-guide.md`]({{ site.baseurl }}/theme-package-guide/)：受信任主题包的目录、启用、回退和发布边界。
- [`frontend-asset-guide.md`]({{ site.baseurl }}/frontend-asset-guide/)：Web Forms 呈现、主题、模块 CSS 与前端资产边界。
- [`module-development-guide.md`]({{ site.baseurl }}/module-development/)：受信任部署模块包的开发、注册、启停和移除边界（含新增模块的隐藏陷阱）。

<!-- build-stamp: 148F695CC475 -->
