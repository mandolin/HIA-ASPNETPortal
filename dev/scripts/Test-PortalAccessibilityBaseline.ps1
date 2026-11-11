#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>无障碍基线门禁（L0，静态）：对比度 / 键盘可达 / 语义标记的硬断言 + 软指标分层。</zh-CN>
  <en>Accessibility baseline gate (L0, static): hard assertions + soft metrics for contrast, keyboard reachability, and semantic markup.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P27`（`W-anp-P143`）交付：补上"无障碍"这一完全无门禁的盲区面。阈值不是拍脑袋——
  全部硬断言先经探针实测当前读数（正文对比度 11.78–14.29、语义文本变量最低 4.89、三类标记违规均为 0、
  正式主题 :focus 规则各 5 处）再定 4.5（WCAG AA 正文）为硬线，保证门禁落地即绿且判据真实有据。

  硬断言（任一失败即 exit 1）：
  A1 六套正式主题正文（body color）对背景对比度 ≥ 4.5；
  A2 六套正式主题已定义的语义文本变量（--portal-text-* / --portal-field-text / --portal-danger-text）
     对背景对比度 ≥ 4.5（只断言已定义的，不强制变量必须存在）；
  A3 六套正式主题 Default.css 必须定义 :focus 可见样式（≥1 处）；
  A4 src/Portal 标记层（aspx/ascx/master，排除 obj/bin）img 必须有 alt；
  A5 非可聚焦元素（div/span/td/tr/li/img）不得内联 onclick（键盘不可达的操作）；
  A6 `<a>` 无 href 必须是命名锚（有 name/id）；
  A7 六套正式主题分组按钮（`.portal-nav-group-toggle`）文字对背景对比度 ≥ 4.5（W-anp-P292 扩到分组导航）；
  A8 七套主题（6 正式 + `Default` 回退，Q4 裁定）分组按钮 `:focus` 必须是可见 outline（非 none、宽度 ≥2px）；
  A9 分组导航标记层：banner 必须被 A4–A6 扫描集合覆盖（default-deny），分组标题是原生
     `<button type="button">` 且带 `aria-expanded` / `aria-controls`，不得用 menu/menubar role 或内联 onclick，
     组内链接必须是带 NavigateUrl 的 HyperLink（渲染后有 href，不是无 href 空锚）；
  A10 Q3 台账锁：`dev/gate-coverage-map.json` 的 `a11y-runtime` 必须保持 `coverage=uncovered`
     且带 owner/reEntry/basis；在没有真实运行期焦点顺序/读屏器门禁之前，把它改成 full 属假绿，判失败。

  软指标（上报不阻断）：各主题对比度明细、:focus 规则计数（Default/ThemeProbe 为 0 的观察如实列出）、
  Default 回退主题分组按钮用 `color: inherit` + `background-color: transparent`，静态无法判定对比度，
  与 A1 排除 Default 的既有口径一致，故列入观察而不硬断言（在门禁里显式记录原因，不静默跳过）。

  证明边界：本门禁只证明 **CSS 声明值与标记属性** 的静态基线——不证明运行期真实渲染对比度
  （计算样式、渐变/叠加层）、真实键盘焦点顺序与可达路径、读屏器语义；这些需运行期（L2）检查，
  属 `a11y-runtime` 未覆盖面，A10 机器化锁住该结论。命名锚（name/id 无 href）是跳转目标不是交互元素，豁免；
  服务端隐藏的控件（如 DiscussDetails 的 prevItem/nextItem，绑定阶段 Visible=false）不渲染，不在静态面内。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalAccessibilityBaseline.ps1'
  产物：temp\a11y-baseline\a11y-baseline-stats.json（每次运行刷新）。
  退出码：0 硬断言全过；1 任一硬断言失败。
  有效性验证（注入法，沿用 W83/W87/P112 惯例）：往主题追加低对比度变量重定义 → 应 Fail；
  注入无 alt 的 img / 带 onclick 的 div → 应 Fail；还原 → Pass。</zh-CN>
  <en>`C-anp-P27` (`W-anp-P143`) deliverable: fills the "accessibility" blind spot that had no gate at all. Thresholds are
  evidence-first — every hard assertion was measured against the current tree (body contrast 11.78–14.29, lowest semantic
  text variable 4.89, all three markup violation classes at 0, 5 :focus rules per official theme) before fixing 4.5
  (WCAG AA body text) as the hard line, so the gate lands green with genuinely grounded criteria.

  Hard assertions (any failure exits 1):
  A1 the six official themes' body text vs background contrast >= 4.5;
  A2 every semantic text variable defined by an official theme (--portal-text-* / --portal-field-text / --portal-danger-text)
     vs background >= 4.5 (only defined variables are asserted; existence is not forced);
  A3 each official theme's Default.css must define visible :focus styling (>= 1 rule);
  A4 every img in src/Portal markup (aspx/ascx/master, obj/bin excluded) must carry alt;
  A5 non-focusable elements (div/span/td/tr/li/img) must not carry inline onclick (keyboard-unreachable actions);
  A6 an `<a>` without href must be a named anchor (name/id present);
  A7 each of the six official themes' group toggle (`.portal-nav-group-toggle`) text vs background contrast >= 4.5
     (W-anp-P292 extends the baseline to grouped navigation);
  A8 all seven themes (six official plus the `Default` fallback, Q4 ruling) must give the group toggle a visible
     `:focus` outline (not none, width >= 2px);
  A9 grouped-navigation markup: the banner must be inside the A4-A6 scan set (default-deny), group headers must be
     native `<button type="button">` with `aria-expanded` / `aria-controls`, with no menu/menubar role or inline onclick,
     and in-group links must be HyperLinks carrying NavigateUrl (an href after rendering, not empty anchors);
  A10 Q3 ledger lock: `a11y-runtime` in `dev/gate-coverage-map.json` must stay `coverage=uncovered` with owner / reEntry /
     basis; marking it full without a real runtime focus-order / screen-reader gate is a false green and fails.

  Soft metrics (report only): per-theme contrast details and :focus rule counts (the 0-count observations of
  Default/ThemeProbe are listed truthfully). The `Default` fallback toggle uses `color: inherit` with
  `background-color: transparent`, so its contrast is statically undecidable; consistent with A1's existing exclusion of
  Default, it is observed rather than asserted, and the reason is recorded explicitly rather than silently skipped.

  Proof boundary: this gate proves only the **static baseline of declared CSS values and markup attributes** — not rendered
  contrast (computed styles, gradients/overlays), real focus order or keyboard traversal, or screen-reader semantics; those
  need runtime (L2) checks and belong to the uncovered `a11y-runtime` surface, which A10 locks mechanically. Named anchors
  (name/id, no href) are jump targets, exempt; server-hidden controls (DiscussDetails prevItem/nextItem, Visible=false at
  bind time) never render and are outside the static surface.

  Usage (repository root): & 'dev\scripts\Test-PortalAccessibilityBaseline.ps1'
  Output: temp\a11y-baseline\a11y-baseline-stats.json (refreshed per run).
  Exit codes: 0 all hard assertions pass; 1 any hard assertion fails.
  Effectiveness proof (injection, per the W83/W87/P112 convention): appending a low-contrast variable redefinition to a theme
  → must Fail; injecting an img without alt / a div with onclick → must Fail; restoring → Pass.</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P143` 交付并接入 L0（33→34）。运行期键盘遍历/渲染级对比度检查登记为后续 L2 工作。</zh-CN>
  <en>Provenance: delivered by `W-anp-P143` and wired into L0 (33→34). Runtime keyboard traversal and rendered-contrast checks are registered as future L2 work.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath = ''
)

$ErrorActionPreference = 'Stop'

# <lang><zh-CN>仓库根 = 本脚本目录（dev\scripts）上两级。</zh-CN>
# <en>Repository root = two levels up from this script's directory (dev\scripts).</en></lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# <lang>
#   <zh-CN>六套正式皮肤名单：与 AGENTS.md「App_Themes 下 6 套正式皮肤」口径一致，显式列出而非目录枚举——
#   `Default`（回退）与 `ThemeProbe`（探针）不具完整可判定的正文/背景对，纳入会让硬断言失真；
#   新增正式皮肤时把名字加进本表即被覆盖（default-deny：不在表里的主题只进软指标观察）。</zh-CN>
#   <en>The six official skin names, matching the AGENTS.md "6 official skins under App_Themes" convention, listed explicitly
#   rather than enumerated from directories — `Default` (fallback) and `ThemeProbe` (probe) lack a fully decidable body/background
#   pair and would distort hard assertions. Adding a new official skin's name here brings it under the gate (default-deny:
#   unlisted themes land in soft metrics only).</en>
# </lang>
$officialThemes = @('EnterpriseLight', 'EnterpriseDark', 'OaLight', 'OaDark', 'StateClassicLight', 'StateClassicDark')

# <lang><zh-CN>WCAG 相对亮度：sRGB → 线性化 → 加权求和；非法输入返回 null（由调用方按"无法判定"处理）。</zh-CN>
# <en>WCAG relative luminance: sRGB → linearization → weighted sum; invalid input yields null (caller treats as undecidable).</en></lang>
function Get-Luminance([string] $hex) {
    $hex = $hex.TrimStart('#')
    if ($hex.Length -eq 3) { $hex = -join ($hex.ToCharArray() | ForEach-Object { "$_$_" }) }
    if ($hex.Length -ne 6) { return $null }
    try {
        $r = [Convert]::ToInt32($hex.Substring(0, 2), 16) / 255.0
        $g = [Convert]::ToInt32($hex.Substring(2, 2), 16) / 255.0
        $b = [Convert]::ToInt32($hex.Substring(4, 2), 16) / 255.0
    } catch { return $null }
    foreach ($name in 'r', 'g', 'b') {
        $v = Get-Variable -Name $name -ValueOnly
        $lin = if ($v -le 0.04045) { $v / 12.92 } else { [Math]::Pow(($v + 0.055) / 1.055, 2.4) }
        Set-Variable -Name $name -Value $lin
    }
    return 0.2126 * $r + 0.7152 * $g + 0.0722 * $b
}

# <lang><zh-CN>对比度 = (较亮 + 0.05) / (较暗 + 0.05)，保留两位；任一侧无法解析返回 null。</zh-CN>
# <en>Contrast = (lighter + 0.05) / (darker + 0.05), two decimals; null when either side is unparsable.</en></lang>
function Get-Contrast([string] $a, [string] $b) {
    $la = Get-Luminance $a; $lb = Get-Luminance $b
    if ($null -eq $la -or $null -eq $lb) { return $null }
    $hi = [Math]::Max($la, $lb); $lo = [Math]::Min($la, $lb)
    return [Math]::Round((($hi + 0.05) / ($lo + 0.05)), 2)
}

# <lang>
#   <zh-CN>颜色值解析：直接 hex / rgb(a) / var()（本文件内变量优先，支持 var(--x) 不带回退的常见形态）。
#   命名色只登记真实会出现的 white/black/transparent。解析不出返回 null —— A1/A2 对 null 的处理是
#   "无法判定即失败"，避免主题改成无法解析的写法后门禁静默变绿。</zh-CN>
#   <en>Color resolution: direct hex / rgb(a) / var() (file-scoped variables first, the common no-fallback form).
#   Named colors cover only the genuinely occurring white/black/transparent. Unresolvable → null — A1/A2 treat null as
#   failure ("undecidable fails"), so a theme rewritten into unparsable syntax can never silently turn the gate green.</en>
# </lang>
function Resolve-Color([string] $value, $vars) {
    $v = $value.Trim()
    if ($v -match '^var\(--([a-z0-9-]+)\)') {
        $key = '--' + $Matches[1]
        if ($vars.ContainsKey($key)) { return ([string]$vars[$key]) }
        return $null
    }
    if ($v -match '^#[0-9a-fA-F]{3,8}$') { return $v }
    if ($v -match '^rgba?\(') {
        $nums = [regex]::Matches($v, '[\d.]+') | ForEach-Object { [double]$_.Value }
        if ($nums.Count -ge 3) { return '#{0:x2}{1:x2}{2:x2}' -f [int]$nums[0], [int]$nums[1], [int]$nums[2] }
    }
    $named = @{ white = '#ffffff'; black = '#000000'; transparent = $null }
    if ($named.ContainsKey($v.ToLower())) { return $named[$v.ToLower()] }
    return $null
}

$hardFailures = @()
$themeStats = @()

# ---------------------------------------------------------------------------
# A1–A3：主题层（对比度与 :focus）
# ---------------------------------------------------------------------------
foreach ($themeName in $officialThemes) {
    $cssPath = Join-Path (Join-Path $repoRoot 'src\Portal\App_Themes') (Join-Path $themeName 'Default.css')
    # <lang><zh-CN>正式主题缺文件 = 漂移（被删/改名），default-deny 判失败而非跳过。</zh-CN>
    # <en>A missing official theme file is drift (deleted/renamed) — default-deny fails it instead of skipping.</en></lang>
    if (-not (Test-Path -LiteralPath $cssPath)) {
        $hardFailures += ("A0 正式主题文件缺失：{0}（漂移：被删或改名）" -f $themeName)
        continue
    }
    $css = [System.IO.File]::ReadAllText($cssPath)

    $vars = @{}
    foreach ($m in [regex]::Matches($css, '(--[a-z0-9-]+)\s*:\s*([^;}]+)')) { $vars[$m.Groups[1].Value] = $m.Groups[2].Value.Trim() }

    # <lang><zh-CN>body 块取正文色与背景；词边界防止 background-color 的 "color:" 被误读为文本色。</zh-CN>
    # <en>Body block supplies text and background colors; the word boundary stops background-color from being misread as text color.</en></lang>
    $bodyColor = $null; $bodyBg = $null
    foreach ($m in [regex]::Matches($css, '(?s)body[^{]*\{([^}]*)\}')) {
        $block = $m.Groups[1].Value
        if ($block -match '(?<![a-z-])color\s*:\s*([^;}]+)') { $bodyColor = Resolve-Color $Matches[1] $vars }
        if ($block -match '(?<![a-z-])background(?:-color)?\s*:\s*([^;}]+)') { $bodyBg = Resolve-Color $Matches[1] $vars }
        if ($bodyColor -and $bodyBg) { break }
    }
    $bodyRatio = if ($bodyColor -and $bodyBg) { Get-Contrast $bodyColor $bodyBg } else { $null }
    if ($null -eq $bodyRatio) {
        $hardFailures += ("A1 [{0}] 正文/背景无法判定（color={1} bg={2}）——无法判定即失败" -f $themeName, $bodyColor, $bodyBg)
    } elseif ($bodyRatio -lt 4.5) {
        $hardFailures += ("A1 [{0}] 正文对比度 {1} < 4.5（color={2} bg={3}）" -f $themeName, $bodyRatio, $bodyColor, $bodyBg)
    }

    # <lang><zh-CN>A2：只断言主题**已定义**的语义文本变量；变量名口径 --portal-text-* / field-text / danger-text。</zh-CN>
    # <en>A2: only semantic text variables the theme actually defines are asserted; naming per --portal-text-* / field-text / danger-text.</en></lang>
    $varRatios = @()
    $textVarKeys = @($vars.Keys | Where-Object { $_ -match '^--portal-(text-[a-z0-9-]+|field-text|danger-text)$' } | Sort-Object)
    foreach ($k in $textVarKeys) {
        $c = Resolve-Color ([string]$vars[$k]) $vars
        $r = if ($c -and $bodyBg) { Get-Contrast $c $bodyBg } else { $null }
        if ($null -eq $r) {
            $hardFailures += ("A2 [{0}] 语义文本变量 {1} 对比度无法判定（值={2}）" -f $themeName, $k, $vars[$k])
        } elseif ($r -lt 4.5) {
            $hardFailures += ("A2 [{0}] 语义文本变量 {1} 对比度 {2} < 4.5（值={3}）" -f $themeName, $k, $r, $vars[$k])
        }
        $varRatios += [pscustomobject]@{ variable = $k; ratio = $r }
    }

    # <lang><zh-CN>A3：正式主题必须有 :focus 可见样式（键盘用户的焦点指示）；0 处即失败。</zh-CN>
    # <en>A3: official themes must define visible :focus styling (keyboard focus indicator); zero rules fails.</en></lang>
    $focusCount = [regex]::Matches($css, ':focus').Count
    if ($focusCount -lt 1) {
        $hardFailures += ("A3 [{0}] 未定义任何 :focus 样式（键盘焦点不可见）" -f $themeName)
    }

    $themeStats += [pscustomobject]@{ theme = $themeName; bodyRatio = $bodyRatio; variables = $varRatios; focusRules = $focusCount }
}

# ---------------------------------------------------------------------------
# A4–A6：标记层
# ---------------------------------------------------------------------------
$imgNoAlt = @(); $onclickNonFocusable = @(); $anchorNoHref = @()
$markupFiles = Get-ChildItem (Join-Path $repoRoot 'src\Portal') -Recurse -File -Include *.aspx, *.ascx, *.master |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
foreach ($f in $markupFiles) {
    $text = [System.IO.File]::ReadAllText($f.FullName)
    $rel = $f.FullName.Substring($repoRoot.Length + 1)
    # <lang>
    #   <zh-CN>归一化 ASP.NET 内联表达式：`<%= %>` / `<%# %>` / `<% %>` / `<%-- --%>` 的内容里可能含 `>`，
    #   而标签扫描用的是 `<img\b[^>]*>` 这类正则 —— 内联表达式的 `%>` 会让标签被**提前截断**，
    #   于是 `alt="HIA"` 明明存在却被判成"缺 alt"（真实发生于门户 logo 的 `src="<%= ... %>"`）。
    #   这里把内联表达式整体替换为不含 `>` 与引号的占位符 `ASPXINLINE`：既让标签被完整扫描，
    #   又不会把 `alt="<%= 变量 %>"` 这类写法误判为缺 alt —— 断言强度只增不减。</zh-CN>
    #   <en>Normalize ASP.NET inline expressions: `<%= %>` / `<%# %>` / `<% %>` / `<%-- --%>` may contain `>`, while tag
    #   scanning uses patterns like `<img\b[^>]*>`. The `%>` of an inline expression truncates the tag early, so an
    #   existing `alt="HIA"` is reported as missing (observed on the portal logo's `src="<%= ... %>"`). Replacing the inline
    #   expression with a placeholder free of `>` and quotes — `ASPXINLINE` — lets the whole tag be scanned and never turns
    #   `alt="<%= variable %>"` into a false "missing alt": the assertion only gets stronger, never weaker.</en>
    # </lang>
    $scanText = $text -replace '<%[\s\S]*?%>', 'ASPXINLINE'
    foreach ($m in [regex]::Matches($scanText, '<img\b[^>]*>')) {
        if ($m.Value -notmatch '\balt=') { $imgNoAlt += $rel }
    }
    foreach ($m in [regex]::Matches($scanText, '<(div|span|td|tr|li|img)\b[^>]*\sonclick=')) {
        $onclickNonFocusable += ("{0} :: {1}" -f $rel, $m.Value.Substring(0, [Math]::Min(100, $m.Value.Length)))
    }
    foreach ($m in [regex]::Matches($scanText, '<a\b[^>]*>')) {
        # <lang><zh-CN>命名锚豁免：name/id 无 href 是跳转目标不是交互元素。</zh-CN>
        # <en>Named-anchor exemption: name/id without href is a jump target, not an interactive element.</en></lang>
        if ($m.Value -notmatch '\bhref=' -and $m.Value -notmatch '\b(name|id)=') { $anchorNoHref += ("{0} :: {1}" -f $rel, $m.Value) }
    }
}
foreach ($rel in $imgNoAlt) { $hardFailures += ("A4 img 缺 alt：{0}" -f $rel) }
foreach ($v in $onclickNonFocusable) { $hardFailures += ("A5 非可聚焦元素挂 onclick：{0}" -f $v) }
foreach ($v in $anchorNoHref) { $hardFailures += ("A6 a 无 href 且非命名锚：{0}" -f $v) }

# ---------------------------------------------------------------------------
# A7–A10：分组导航无障碍基线（`W-anp-P292`）
# <lang>
#   <zh-CN>把分组导航从「A4–A6 顺带扫到」提升为**有专属硬断言**的覆盖面：分组按钮对比度、
#   7 套焦点轮廓、disclosure 标记语义与 Q3 未覆盖台账锁。判定全部从权威源（7 套 CSS / banner 标记 /
#   覆盖地图）推导，不硬编码色值或按钮数量。</zh-CN>
#   <en>Promote grouped navigation from "incidentally scanned by A4-A6" to a surface with **dedicated hard
#   assertions**: toggle contrast, focus outlines across seven skins, disclosure markup semantics, and the Q3
#   uncovered-ledger lock. All decisions derive from authoritative sources (seven CSS files / banner markup /
#   coverage map) rather than hard-coded colours or button counts.</en>
# </lang>
# ---------------------------------------------------------------------------
$navThemes = @($officialThemes) + @('Default')
$navGroupStats = @()

foreach ($themeName in $navThemes) {
    $navCssPath = Join-Path (Join-Path $repoRoot 'src\Portal\App_Themes') (Join-Path $themeName 'Default.css')
    if (-not (Test-Path -LiteralPath $navCssPath)) {
        $hardFailures += ("A7/A8 [{0}] 主题文件缺失（漂移：被删或改名）" -f $themeName)
        continue
    }
    $navCss = [System.IO.File]::ReadAllText($navCssPath)
    $navVars = @{}
    foreach ($m in [regex]::Matches($navCss, '(--[a-z0-9-]+)\s*:\s*([^;}]+)')) { $navVars[$m.Groups[1].Value] = $m.Groups[2].Value.Trim() }

    $scope = 'body.portal-theme-' + $themeName.ToLowerInvariant() + ' '
    $toggleBlock = [regex]::Match($navCss, '(?s)' + [regex]::Escape($scope + '.portal-nav-group-toggle') + '\s*\{([^}]*)\}')
    $focusBlock = [regex]::Match($navCss, '(?s)' + [regex]::Escape($scope + '.portal-nav-group-toggle:focus') + '\s*\{([^}]*)\}')
    $hiddenBlock = [regex]::Match($navCss, '(?s)' + [regex]::Escape($scope + '.portal-nav-group-hidden') + '\s*\{([^}]*)\}')

    # <lang>
    #   <zh-CN>A8：7 套（含 Default 回退）焦点轮廓必须可见：outline 非 none 且宽度 ≥2px。
    #   用 `solid 2px` 的既有写法推导宽度，不硬编码颜色（各皮肤调色板不同）。</zh-CN>
    #   <en>A8: all seven skins (including the Default fallback) need a visible focus outline: not none and
    #   width >= 2px. Width is derived from the existing `solid 2px` form; no colour is hard-coded because each
    #   skin uses its own palette.</en>
    # </lang>
    if (-not $focusBlock.Success) {
        $hardFailures += ("A8 [{0}] 分组按钮缺 `:focus` 规则（键盘焦点不可见）" -f $themeName)
    }
    else {
        $focusBody = $focusBlock.Groups[1].Value
        $outline = [regex]::Match($focusBody, '(?i)\boutline\s*:\s*([^;}]+)')
        $widthMatch = [regex]::Match($outline.Groups[1].Value, '(?i)(\d+(?:\.\d+)?)\s*px')
        if (-not $outline.Success -or $outline.Groups[1].Value -match '(?i)\bnone\b' -or -not $widthMatch.Success -or [double]$widthMatch.Groups[1].Value -lt 2) {
            $hardFailures += ("A8 [{0}] 分组按钮 `:focus` 轮廓不可见或宽度 <2px（{1}）" -f $themeName, ($outline.Groups[1].Value.Trim()))
        }
    }

    # <lang>
    #   <zh-CN>折叠后组内链接必须移出 Tab 序列；`display:none` 是键盘可达性的静态前提。</zh-CN>
    #   <en>Collapsed in-group links must leave the Tab sequence; `display:none` is the static prerequisite for keyboard reachability.</en>
    # </lang>
    if (-not $hiddenBlock.Success -or $hiddenBlock.Groups[1].Value -notmatch '(?is)\bdisplay\s*:\s*none') {
        $hardFailures += ("A8 [{0}] `.portal-nav-group-hidden` 缺 display:none（折叠后链接仍在键盘 Tab 序列）" -f $themeName)
    }

    # <lang>
    #   <zh-CN>A7：6 套正式主题分组按钮文字/背景对比度 ≥4.5（WCAG AA 正文）。
    #   `Default` 用 inherit/transparent，静态不可判定，按 A1 既有口径列软指标观察而非硬断言，
    #   并在统计里显式记录原因；「无法判定即失败」只针对有完整色值的正式主题。</zh-CN>
    #   <en>A7: the six official themes' toggle text/background contrast must be >= 4.5 (WCAG AA body text).
    #   `Default` uses inherit/transparent, so it is statically undecidable; consistent with A1's existing
    #   convention it is a soft observation rather than a hard assertion, with the reason recorded in the stats.
    #   "Undecidable fails" applies only to official themes that declare a complete colour pair.</en>
    # </lang>
    if (-not $toggleBlock.Success) {
        if ($themeName -ne 'Default') { $hardFailures += ("A7 [{0}] 缺分组按钮规则块（无法证明对比度）" -f $themeName) }
        continue
    }
    $toggleBody = $toggleBlock.Groups[1].Value
    $toggleColor = [regex]::Match($toggleBody, '(?i)(?<![a-z-])color\s*:\s*([^;}]+)')
    $toggleBg = [regex]::Match($toggleBody, '(?i)background(?:-color)?\s*:\s*([^;}]+)')
    $navRatio = $null
    $observation = ''
    if ($themeName -eq 'Default') {
        # <lang>
        #   <zh-CN>回退主题继承 body 色，无法从本规则块静态判定；只记录事实，不做假绿硬断言。</zh-CN>
        #   <en>The fallback inherits the body colour, which cannot be decided statically from this block; record the fact only, never a false-green hard assertion.</en>
        # </lang>
        $observation = 'color: inherit + background-color: transparent → 静态不可判定，按 A1 口径观察'
    }
    elseif (-not $toggleColor.Success -or -not $toggleBg.Success) {
        $hardFailures += ("A7 [{0}] 分组按钮缺 color 或 background-color（无法判定即失败）" -f $themeName)
    }
    else {
        $fgHex = Resolve-Color $toggleColor.Groups[1].Value $navVars
        $bgHex = Resolve-Color $toggleBg.Groups[1].Value $navVars
        if ($null -eq $fgHex -or $null -eq $bgHex) {
            $hardFailures += ("A7 [{0}] 分组按钮色值无法解析（fg={1} bg={2}）——无法判定即失败" -f $themeName, $toggleColor.Groups[1].Value.Trim(), $toggleBg.Groups[1].Value.Trim())
        }
        else {
            $navRatio = Get-Contrast $fgHex $bgHex
            if ($null -eq $navRatio) {
                $hardFailures += ("A7 [{0}] 分组按钮对比度无法判定（fg={1} bg={2}）" -f $themeName, $fgHex, $bgHex)
            }
            elseif ($navRatio -lt 4.5) {
                $hardFailures += ("A7 [{0}] 分组按钮对比度 {1} < 4.5（fg={2} bg={3}）" -f $themeName, $navRatio, $fgHex, $bgHex)
            }
        }
    }
    $navGroupStats += [pscustomobject]@{
        theme = $themeName; toggleContrast = $navRatio; focusOutline = if ($focusBlock.Success) { $focusBlock.Groups[1].Value.Trim() } else { $null }; observation = $observation
    }
}

# <lang>
#   <zh-CN>A9：banner 分组导航标记层语义。先剥离 ASPX 说明注释（按项目「注释提及不计」口径），
#   再把 `<%...%>` 归一化为 ASPXINLINE（与 A4–A6 同法），避免绑定表达式里的 `>` 截断标签扫描。</zh-CN>
#   <en>A9: banner grouped-navigation markup semantics. Strip ASPX explanatory comments first (per the
#   project's "comment mentions do not count" convention), then normalize `<%...%>` to ASPXINLINE (same as
#   A4-A6) so `>` inside binding expressions cannot truncate tag scanning.</en>
# </lang>
$bannerPath = Join-Path $repoRoot 'src\Portal\DesktopPortalBanner.ascx'
if (-not (Test-Path -LiteralPath $bannerPath)) {
    $hardFailures += 'A9 banner 标记文件缺失（分组导航无可判定来源）'
}
else {
    $bannerText = [System.IO.File]::ReadAllText($bannerPath)
    $bannerMarkup = [regex]::Replace($bannerText, '(?s)<%--.*?--%>', '')
    $bannerScan = $bannerMarkup -replace '<%[\s\S]*?%>', 'ASPXINLINE'

    # <lang>
    #   <zh-CN>default-deny：banner 必须在 A4–A6 的扫描集合里，否则「分组导航被顺带扫到」的前提不成立。</zh-CN>
    #   <en>Default-deny: the banner must be inside the A4-A6 scan set, otherwise the premise that grouped navigation is covered by them does not hold.</en>
    # </lang>
    $bannerScanned = @($markupFiles | Where-Object { $_.Name -ieq 'DesktopPortalBanner.ascx' }).Count -gt 0
    if (-not $bannerScanned) { $hardFailures += 'A9 DesktopPortalBanner.ascx 未被 A4–A6 扫描集合覆盖' }

    $toggleTags = [regex]::Matches($bannerScan, '<button\b[^>]*portal-nav-group-toggle[^>]*>')
    if ($toggleTags.Count -eq 0) { $hardFailures += 'A9 缺原生 button 分组标题（disclosure 契约不成立）' }
    foreach ($tag in $toggleTags) {
        if ($tag.Value -notmatch '\btype\s*=\s*"button"') { $hardFailures += ('A9 分组标题非 type=button：' + $tag.Value) }
        if ($tag.Value -notmatch '\baria-expanded\s*=') { $hardFailures += ('A9 分组标题缺 aria-expanded：' + $tag.Value) }
        if ($tag.Value -notmatch '\baria-controls\s*=') { $hardFailures += ('A9 分组标题缺 aria-controls：' + $tag.Value) }
        if ($tag.Value -match '(?i)\brole\s*=\s*"(menu|menubar)"') { $hardFailures += ('A9 分组标题误用 menu/menubar role：' + $tag.Value) }
        if ($tag.Value -match '(?i)\bonclick\s*=') { $hardFailures += ('A9 分组标题使用内联 onclick：' + $tag.Value) }
    }

    # <lang>
    #   <zh-CN>组内链接必须有 NavigateUrl（渲染后成为 href）；若退化成无 href 的 `<a>`，键盘与读屏器都失去目标。</zh-CN>
    #   <en>In-group links must carry NavigateUrl (an href after rendering); if they degrade into href-less `<a>` tags, both keyboard and screen-reader targets are lost.</en>
    # </lang>
    $groupLinks = [regex]::Matches($bannerScan, '(?s)<asp:HyperLink\b[^>]*ID="GroupTabLink"[^>]*>')
    if ($groupLinks.Count -eq 0) { $hardFailures += 'A9 组内缺 GroupTabLink HyperLink' }
    foreach ($link in $groupLinks) {
        if ($link.Value -notmatch '\bNavigateUrl\s*=') { $hardFailures += ('A9 组内链接缺 NavigateUrl（将渲染成无 href 空锚）：' + $link.Value) }
    }
    $navGroupStats += [pscustomobject]@{
        theme = 'markup'; toggleTags = $toggleTags.Count; groupLinks = $groupLinks.Count; bannerInScanSet = $bannerScanned; observation = ''
    }
}

# <lang>
#   <zh-CN>A10：Q3 台账锁。`a11y-runtime` 在没有真实运行期焦点顺序/读屏器门禁之前必须保持 uncovered
#   且带 owner/reEntry/basis；把它改成 full 会把「未做」伪装成「已验证」，属假绿。</zh-CN>
#   <en>A10: Q3 ledger lock. Until a real runtime focus-order / screen-reader gate exists, `a11y-runtime` must
#   remain uncovered with owner/reEntry/basis. Marking it full would disguise "not done" as "verified" — a false green.</en>
# </lang>
$coverageMapPath = Join-Path $repoRoot 'dev\gate-coverage-map.json'
if (-not (Test-Path -LiteralPath $coverageMapPath)) {
    $hardFailures += 'A10 覆盖地图缺失（无法核对 Q3 未覆盖台账）'
}
else {
    $coverageMapRaw = [System.IO.File]::ReadAllText($coverageMapPath)
    $a11yRuntime = [regex]::Match($coverageMapRaw, '(?s)\{\s*"id"\s*:\s*"a11y-runtime".*?\}')
    if (-not $a11yRuntime.Success) {
        $hardFailures += 'A10 覆盖地图缺 a11y-runtime 条目（未覆盖面失去 owner）'
    }
    else {
        $row = $a11yRuntime.Value
        if ($row -notmatch '"coverage"\s*:\s*"uncovered"') {
            $hardFailures += 'A10 a11y-runtime 不再标记 uncovered（无运行期门禁支撑的假绿）'
        }
        foreach ($required in @('owner', 'reEntry', 'basis')) {
            if ($row -notmatch ('"' + $required + '"\s*:\s*"[^"]+"')) {
                $hardFailures += ('A10 a11y-runtime 缺必填字段 ' + $required)
            }
        }
    }
}

# ---------------------------------------------------------------------------
# 统计落盘 + 输出
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $StatsPath = Join-Path (Join-Path $repoRoot 'temp') 'a11y-baseline\a11y-baseline-stats.json'
}
$statsDir = Split-Path -Parent $StatsPath
if (-not (Test-Path -LiteralPath $statsDir)) { New-Item -ItemType Directory -Force -Path $statsDir | Out-Null }
$report = [pscustomobject]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    officialThemes = $officialThemes
    themes = $themeStats
    markup = [pscustomobject]@{
        scannedFiles = @($markupFiles).Count
        imgMissingAlt = $imgNoAlt.Count
        onclickNonFocusable = $onclickNonFocusable.Count
        anchorNoHref = $anchorNoHref.Count
    }
    groupedNavigation = $navGroupStats
    softMetrics = @(
        'Default/ThemeProbe 为回退与探针主题，无完整可判定正文/背景对，仅观察不断言',
        'Default 分组按钮 color:inherit + background-color:transparent，静态不可判定，按 A1 口径观察不硬断言',
        '运行期键盘焦点顺序与渲染级对比度不在本门禁证明边界内（a11y-runtime，A10 台账锁）'
    )
    failures = $hardFailures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 无障碍基线（硬断言 + 软指标） ==='
foreach ($t in $themeStats) {
    Write-Host ("主题 {0}: 正文对比度 {1}｜:focus 规则 {2} 处｜语义文本变量 {3} 个" -f $t.theme, $t.bodyRatio, $t.focusRules, @($t.variables).Count)
}
Write-Host ("标记层：扫描 {0} 个文件｜img 缺 alt {1}｜onclick 非可聚焦 {2}｜a 无 href 非命名锚 {3}" -f @($markupFiles).Count, $imgNoAlt.Count, $onclickNonFocusable.Count, $anchorNoHref.Count)
Write-Host ("统计已写入：$StatsPath")

if ($hardFailures.Count -gt 0) {
    Write-Host ''
    Write-Host ("[FAIL] 无障碍基线硬断言失败 {0} 处：" -f $hardFailures.Count)
    foreach ($f in $hardFailures) { Write-Host ("  - " + $f) }
    exit 1
}

Write-Host ''
Write-Host ("[PASS] 无障碍基线硬断言全过（A1–A10，含分组导航 7 套主题与 Q3 台账锁）。软指标见统计文件。")
exit 0
