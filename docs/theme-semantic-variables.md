# 主题语义变量矩阵（`--portal-*`）

本文件是门户主题层**语义变量**的人类可读基线：登记 7 套主题各自定义了哪些 `--portal-*` 变量、取值是多少、各自适用于什么场景。

> **权威性说明**：本文件是**从 `src/Portal/App_Themes/*/Default.css` 提取的快照**，便于阅读与对照；
> 机器可检查的权威来源是 CSS 本身与对应门禁（`C-anp-P18` / `W-anp-P100`）。二者不一致时**以 CSS 与门禁为准**，
> 并应更新本文件。这遵循项目一贯原则：登记项会过期，唯一可靠的是可重复运行的检查。

## 一、为什么要有语义变量

模块自带 CSS 若直接写死颜色（如 `color: #8a1f1f`），在深色皮肤下会变成「深色压深底」，与皮肤调色板割裂；
新增皮肤时也容易漏配。改为「主题层定义语义变量 → 模块侧只写 `var(--portal-*, 回退值)`」后：

- 模块外观随皮肤自动切换，跨皮肤一致；
- 新增皮肤只需补齐变量值，不会漏；
- 回退值保证不支持 CSS 自定义属性的旧内核仍得原色。

## 二、变量矩阵（7 套主题 × 14 个变量）

7 套主题指 6 套正式皮肤 + `Default` 回退主题：`EnterpriseLight` / `EnterpriseDark` / `OaLight` / `OaDark` / `StateClassicLight` / `StateClassicDark` / `Default`。

| 变量 | Default | EnterpriseLight | EnterpriseDark | OaLight | OaDark | StateClassicLight | StateClassicDark |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `--portal-surface` | `#ffffff` | `#ffffff` | `#18222e` | `#ffffff` | `#18222e` | `#ffffff` | `#18222e` |
| `--portal-surface-2` | `#f5f8fb` | `#f5f8fb` | `#1f2b3a` | `#f5f8fb` | `#1f2b3a` | `#f5f8fb` | `#1f2b3a` |
| `--portal-border` | `#d8dee8` | `#d8dee8` | `#2b3a4d` | `#d8dee8` | `#2b3a4d` | `#d8dee8` | `#2b3a4d` |
| `--portal-border-subtle` | `#edf1f5` | `#edf1f5` | `#243140` | `#edf1f5` | `#243140` | `#edf1f5` | `#243140` |
| `--portal-border-strong` | `#c2ccd8` | `#c2ccd8` | `#3a4d63` | `#c2ccd8` | `#3a4d63` | `#c2ccd8` | `#3a4d63` |
| `--portal-text-strong` | `#2f475f` | `#2f475f` | `#e6edf5` | `#2f475f` | `#e6edf5` | `#2f475f` | `#e6edf5` |
| `--portal-text-muted` | `#5b6b7d` | `#5b6b7d` | `#9fb0c3` | `#5b6b7d` | `#9fb0c3` | `#5b6b7d` | `#9fb0c3` |
| `--portal-text-subtle` | `#41556b` | `#41556b` | `#c2cadf` | `#41556b` | `#c2cadf` | `#41556b` | `#c2cadf` |
| `--portal-field-bg` | `#ffffff` | `#ffffff` | `#121a24` | `#ffffff` | `#121a24` | `#ffffff` | `#121a24` |
| `--portal-field-border` | `#cdd7e2` | `#cdd7e2` | `#33425a` | `#cdd7e2` | `#33425a` | `#cdd7e2` | `#33425a` |
| `--portal-field-text` | `#1f2d3d` | `#1f2d3d` | `#dce6f0` | `#1f2d3d` | `#dce6f0` | `#1f2d3d` | `#dce6f0` |
| `--portal-list-header-bg` | `#eef2f7` | `#eef2f7` | `#1e2a38` | `#eef2f7` | `#1e2a38` | `#eef2f7` | `#1e2a38` |
| `--portal-list-row-alt` | `#f7f9fc` | `#f7f9fc` | `#161f2a` | `#f7f9fc` | `#161f2a` | `#f7f9fc` | `#161f2a` |
| `--portal-danger-text` | `red` | `#b42318` | `#ff8b8b` | `#b42318` | `#ff9b9b` | `#9e2f2e` | `#ff9c99` |

### 矩阵的两个规律（新增变量前先理解）

1. **前 13 个变量按「浅色族 / 深色族」各一套**：4 套浅色主题（Default / EnterpriseLight / OaLight / StateClassicLight）取值完全相同，3 套深色主题（EnterpriseDark / OaDark / StateClassicDark）取值也完全相同。它们表达的是**明暗基调**，不区分主题族风格（含 C-anp-新增的 `--portal-text-subtle`）。
2. **`--portal-danger-text` 是唯一按主题族区分的变量**：因为它不表达明暗，而表达**各主题族自己的错误色**（直接取该主题既有的 `.NormalRed`）。Enterprise 族与 Oa 族的浅色错误色相同（`#b42318`）但深色不同（`#ff8b8b` / `#ff9b9b`）；StateClassic 族则两端都不同（`#9e2f2e` / `#ff9c99`）。

**推论**：新增语义变量时，先判断它属于哪一类——表达明暗基调的应加入「浅/深两套」；表达主题族固有属性（如错误色、强调色）的必须**逐主题族**取值，且优先复用该主题既有类（如 `.NormalRed`）而不要新造颜色。

## 三、变量用途速查

| 变量 | 用途 |
| --- | --- |
| `--portal-surface` | 模块/卡片容器底色 |
| `--portal-surface-2` | 容器内的次级分区底色（如表头、分组块） |
| `--portal-border` | 容器外边框 |
| `--portal-border-subtle` | 分隔线、弱边框 |
| `--portal-border-strong` | 需要强调的边框（如焦点态、选中态） |
| `--portal-text-strong` | 标题、副标题等强文本 |
| `--portal-text-muted` | 弱化文本、说明文字、占位文案 |
| `--portal-text-subtle` | 副标题层级，取值介于 `--portal-text-strong` 与 `--portal-text-muted` 之间，用于标题与副标题的色层次 |
| `--portal-field-bg` | 输入控件背景 |
| `--portal-field-border` | 输入控件边框 |
| `--portal-field-text` | 输入控件内文字 |
| `--portal-list-header-bg` | 列表表头背景 |
| `--portal-list-row-alt` | 列表隔行底色 |
| `--portal-danger-text` | 错误/失败/校验反馈文本 |

## 四、使用规则

1. **模块 CSS 不得写死颜色**。需要颜色时写 `var(--portal-*, 回退值)`，回退值取该规则此前的原值，保证旧内核行为不变。
2. **7 套主题必须同时定义**。新增变量时 7 套 `Default.css` 一个都不能少；漏定义的主题在支持变量的浏览器中该处会退到回退值，造成「个别皮肤样式漂移」且难以察觉。
3. **优先复用既有主题类**，不为新变量新造颜色。错误色复用 `.NormalRed`、中性色复用 `.portal-admin-subtitle` 一类的既有取值。
4. **变量只在主题层定义**，模块层只引用。模块层再定义一层语义变量会造成两层间接、难以追溯。

## 五、如何复现本矩阵

以下命令从 CSS 实况提取矩阵，可用于核对本文件是否过期：

```powershell
$themes = @('Default','EnterpriseLight','EnterpriseDark','OaLight','OaDark','StateClassicLight','StateClassicDark')
foreach ($t in $themes) {
    $text = Get-Content "src/Portal/App_Themes/$t/Default.css" -Raw -Encoding UTF8
    $vars = @{}
    foreach ($m in [regex]::Matches($text, '--portal-([a-z0-9-]+)\s*:\s*([^;]+);')) {
        $vars[$m.Groups[1].Value] = $m.Groups[2].Value.Trim()
    }
    Write-Host ("$t : " + (($vars.Keys | Sort-Object | ForEach-Object { "$_=$vars[$_]" }) -join '  '))
}
```

**一致性期望**：7 套主题输出的变量名集合应完全相同（当前为 14 个）；若某主题缺少某个变量，即为漏定义，须补齐。

## 六、相关文档

- `theme-package-guide.md`：受信任主题包的目录、启用、回退和发布边界。
- `frontend-asset-guide.md`：Web Forms 呈现、主题、模块 CSS 与前端资产边界。
- 模块外观契约（容器/标题/分区/表格/提示/弱化/危险的类集与逐皮肤色板）见 `C-anp-P18` / `W-anp-P99` 产出。
