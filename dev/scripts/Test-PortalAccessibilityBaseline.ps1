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
  A6 `<a>` 无 href 必须是命名锚（有 name/id）。

  软指标（上报不阻断）：各主题对比度明细、:focus 规则计数（Default/ThemeProbe 为 0 的观察如实列出）。

  证明边界：本门禁只证明 **CSS 声明值与标记属性** 的静态基线——不证明运行期真实渲染对比度
  （计算样式、渐变/叠加层）、真实键盘焦点顺序与可达路径、读屏器语义；这些需运行期（L2）检查，
  属后续工作，不做却宣称已验证视为未完成。命名锚（name/id 无 href）是跳转目标不是交互元素，豁免；
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
  A6 an `<a>` without href must be a named anchor (name/id present).

  Soft metrics (report only): per-theme contrast details and :focus rule counts (the 0-count observations of
  Default/ThemeProbe are listed truthfully).

  Proof boundary: this gate proves only the **static baseline of declared CSS values and markup attributes** — not rendered
  contrast (computed styles, gradients/overlays), real focus order or keyboard traversal, or screen-reader semantics; those
  need runtime (L2) checks and are future work. Named anchors (name/id, no href) are jump targets, exempt; server-hidden
  controls (DiscussDetails prevItem/nextItem, Visible=false at bind time) never render and are outside the static surface.

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
    foreach ($m in [regex]::Matches($text, '<img\b[^>]*>')) {
        if ($m.Value -notmatch '\balt=') { $imgNoAlt += $rel }
    }
    foreach ($m in [regex]::Matches($text, '<(div|span|td|tr|li|img)\b[^>]*\sonclick=')) {
        $onclickNonFocusable += ("{0} :: {1}" -f $rel, $m.Value.Substring(0, [Math]::Min(100, $m.Value.Length)))
    }
    foreach ($m in [regex]::Matches($text, '<a\b[^>]*>')) {
        # <lang><zh-CN>命名锚豁免：name/id 无 href 是跳转目标不是交互元素。</zh-CN>
        # <en>Named-anchor exemption: name/id without href is a jump target, not an interactive element.</en></lang>
        if ($m.Value -notmatch '\bhref=' -and $m.Value -notmatch '\b(name|id)=') { $anchorNoHref += ("{0} :: {1}" -f $rel, $m.Value) }
    }
}
foreach ($rel in $imgNoAlt) { $hardFailures += ("A4 img 缺 alt：{0}" -f $rel) }
foreach ($v in $onclickNonFocusable) { $hardFailures += ("A5 非可聚焦元素挂 onclick：{0}" -f $v) }
foreach ($v in $anchorNoHref) { $hardFailures += ("A6 a 无 href 且非命名锚：{0}" -f $v) }

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
    softMetrics = @(
        'Default/ThemeProbe 为回退与探针主题，无完整可判定正文/背景对，仅观察不断言',
        '运行期键盘焦点顺序与渲染级对比度不在本门禁证明边界内（后续 L2 工作）'
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
Write-Host '[PASS] 无障碍基线硬断言全过（A1–A6）。软指标见统计文件。'
exit 0
