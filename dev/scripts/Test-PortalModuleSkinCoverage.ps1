<#
.SYNOPSIS
  <lang>
    <zh-CN>模块语义类皮肤覆盖门禁：校验业务模块颜色的主题化来源与主题层作用域规则覆盖。</zh-CN>
    <en>Module semantic-class skin coverage gate: verifies the theming source of business-module colors and the coverage of theme-layer scope rules.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>两层判定：**硬规则（阻断）** 模块自带 `Styles/<Module>.css` 中任何颜色相关裸字面量（`#hex` / `rgb()` / `hsl()`）不得出现在 `var(...)` 之外 —— 一旦裸写颜色，深色皮肤下必形成浅块；**软指标（上报不阻断）** 模块语义类在 7 套正式皮肤 `Default.css` 的覆盖缺口，逐模块列出"未在全部皮肤覆盖"的类名。
    **前置条件**：只需源码可读，属编排入口 L0 层；不连数据库、不启动 IIS、不执行浏览器。
    **验证方式**：退出码 0 无硬违规、1 存在裸色值硬违规、2 前置条件缺失；覆盖缺口以 `[GATE-NOTE]` 留档不阻断。
    **低敏边界**：只读取模块 CSS 与主题 CSS 文本，不读取连接串、凭据或业务数据，不改写任何文件。</zh-CN>
    <en>Two tiers: the **hard (blocking) rule** requires that no raw color literal (`#hex` / `rgb()` / `hsl()`) appears outside `var(...)` in module `Styles/<Module>.css`, because a hardcoded color inevitably produces light blocks on dark skins; the **soft (reported, non-blocking) metric** lists, per module, the classes not covered across all seven official skins' `Default.css`.
    **Prerequisites**: source only — an L0-layer gate with no database, IIS or browser access.
    **Verification**: exit code 0 means no hard violation, 1 means a raw-color hard violation, 2 means missing prerequisites; coverage gaps are recorded as `[GATE-NOTE]` without blocking.
    **Low-sensitivity boundary**: only module and theme CSS text is read — no connection strings, credentials or business data, and nothing is rewritten.</en>
  </lang>

  证明边界：只证明业务模块颜色的**主题化来源**与主题层作用域规则被覆盖（静态核对）；不证明视觉呈现正确（由视觉回归基线承载），不覆盖未登记为语义类的样式。

  Proof boundary: Proves only that business-module colours come from theming sources and that theme-layer scoping rules are covered (static checks); it does not prove visual rendering (the visual regression baseline does) and does not cover styles not registered as semantic classes.
#>

# W100 门禁：模块语义类皮肤覆盖（C-anp-P18）
#
# 依据 `C-anp-P18` 目标 4 与 `docs/theme-module-contract.md` §四.4：业务模块的语义类，其**颜色**
# 必须由主题层经 `--portal-*` 语义变量提供，且（理想上）7 套正式皮肤的 `Default.css` 都应补
# `body.portal-theme-<名> .<模块类>` 作用域规则。本门禁把"登记会过期、唯一可靠的是可重复检查"
# 落到机器可跑的检查上。
#
# 两层判定：
#   1. 硬规则（阻断）：模块自带 `Styles/<Module>.css` 中，任何颜色相关的裸字面量
#      （`#hex` / `rgb()` / `hsl()`）**不得出现在 `var(...)` 之外**。因为一旦裸写颜色，
#      在深色皮肤下就会形成浅色块（旧门户适配债的根因）。W-anp-P98 之后模块侧颜色一律
#      `var(--portal-*, 回退值)`，故当前应为 0 处；此规则用于阻止回归。
#   2. 软指标（上报，不阻断）：模块语义类在主题 `Default.css` 中的覆盖缺口。逐模块列出
#      "未在全部皮肤覆盖"的类名，作为目标 4「补齐缺的规则」的遗留债清单（已据实，不强行过门禁）。
#
# 用法（仓库根目录，由 `Invoke-PortalGateSuite.ps1` 编排调用）：
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File dev\scripts\Test-PortalModuleSkinCoverage.ps1
# 退出码：0 无硬违规；1 存在裸色值硬违规；2 前置条件缺失。

[CmdletBinding()]
param(
    # <lang>
    #   <zh-CN>仓库根目录；默认由本脚本在 `dev/scripts` 下的位置反推两级得到，与门禁套件一致。</zh-CN>
    #   <en>Repository root; by default derived two levels up from this script's location in `dev/scripts`, matching the suite.</en>
    # </lang>
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>路径反推：本脚本位于 `dev/scripts`，上两级即仓库根。门禁套件统一用此方式定位源码。</zh-CN>
#   <en>Path resolution: this script lives in `dev/scripts`, so two levels up is the repo root; the suite locates source the same way.</en>
# </lang>
if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$portalDir = Join-Path $RepoRoot 'src\Portal'
$modulesDir = Join-Path $portalDir 'DesktopModules'
$themesRoot = Join-Path $portalDir 'App_Themes'

# <lang>
#   <zh-CN>`ThemeProbe` 是诊断用主题，不计入"正式皮肤"覆盖判定；其余 6 套正式 + `Default`
#   回退共 7 套，与 `theme-semantic-variables.md` 的变量矩阵口径一致。</zh-CN>
#   <en>`ThemeProbe` is a diagnostic theme and is excluded from the "official skin" coverage check; the remaining six formal
#     skins plus the `Default` fallback make seven, matching the variable-matrix scope in `theme-semantic-variables.md`.</en>
# </lang>
$excludeThemes = @('ThemeProbe')

function Write-GateNote([string] $message) {
    # <lang>
    #   <zh-CN>`[GATE-NOTE]` 是门禁自述通道：通过但值得注意的结论走这里。门禁套件会剔除这类行再做
    #   失败判定（避免"缺失/未覆盖"等描述词误判为失败），同时把它们附到汇总与证据里。</zh-CN>
    #   <en>`[GATE-NOTE]` is the gate's self-report channel for "passing but worth attention" conclusions. The suite strips
    #     these lines before failure matching (so words like "missing"/"uncovered" are not mistaken for a failure) yet keeps
    #     them in the summary and evidence.</en>
    # </lang>
    Write-Host ('[GATE-NOTE] ' + $message)
}

# <lang>
#   <zh-CN>提取一段 CSS 中出现的全部类选择器名（去重）。先剥多行 `/* */` 与 `//` 注释，避免把注释里
#   引用的色值示例当成形而上的类名。</zh-CN>
#   <en>Extract every class selector name (de-duplicated) from a CSS string. Multi-line `/* */` and `//` comments are
#     stripped first so color examples quoted inside comments are not mistaken for real class tokens.</en>
# </lang>
function Get-CssClassNames([string] $css) {
    $noComment = [regex]::Replace($css, '(?s)/\*.*?\*/', '')
    $noComment = [regex]::Replace($noComment, '(?m)//.*$', '')
    $matches = [regex]::Matches($noComment, '(?<=^|[\s,{}()>])\.([a-zA-Z][\w-]*)')
    $set = @{}
    foreach ($m in $matches) { $set[$m.Groups[1].Value] = $true }
    return $set.Keys
}

# <lang>
#   <zh-CN>剥除 `var(...)` 函数体（连同其回退值），再检测是否残留裸颜色字面量。
#   回退值里的 `#hex` 是"不支持 CSS 变量的旧内核"兜底，不算违规；只有 var 之外的裸色才算。</zh-CN>
#   <en>Strip the `var(...)` function bodies (including their fallbacks), then detect any leftover raw color literal.
#     A fallback `#hex` inside `var()` is the legacy-kernel safety net and is not a violation; only colors outside `var()` are.</en>
# </lang>
function Test-RawColorOutsideVar([string] $css) {
    $noComment = [regex]::Replace($css, '(?s)/\*.*?\*/', '')
    $noComment = [regex]::Replace($noComment, '(?m)//.*$', '')
    $stripped = [regex]::Replace($noComment, 'var\([^)]*\)', '')
    $hits = [regex]::Matches($stripped, '#[0-9a-fA-F]{3,8}\b|rgba?\s*\(|hsla?\s*\(')
    $found = @()
    foreach ($h in $hits) { $found += $h.Value }
    return $found
}

# <lang>
#   <zh-CN>前置条件：模块目录与主题目录必须存在，否则无法判定，按"前置缺失"退出 2。</zh-CN>
#   <en>Preconditions: the modules directory and the themes root must exist; otherwise judgement is impossible, exit 2.</en>
# </lang>
if (-not (Test-Path $modulesDir)) {
    Write-Host ('[错误] 模块目录不存在：' + $modulesDir)
    exit 2
}
if (-not (Test-Path $themesRoot)) {
    Write-Host ('[错误] 主题目录不存在：' + $themesRoot)
    exit 2
}

$themeDirs = Get-ChildItem $themesRoot -Directory | Where-Object { $excludeThemes -notcontains $_.Name }
if ($themeDirs.Count -eq 0) {
    Write-Host ('[错误] 未找到任何正式皮肤目录于：' + $themesRoot)
    exit 2
}

# <lang>
#   <zh-CN>预读各主题 `Default.css` 的类全集（一次读入，供所有模块比对复用）。</zh-CN>
#   <en>Pre-read each theme's `Default.css` class set once and reuse it across all module comparisons.</en>
# </lang>
$themeClassSets = @{}
foreach ($t in $themeDirs) {
    $cssPath = Join-Path $t.FullName 'Default.css'
    if (Test-Path $cssPath) {
        $themeClassSets[$t.Name] = [regex]::Replace((Get-Content $cssPath -Raw -Encoding UTF8), '(?s)/\*.*?\*/', '')
        $themeClassSets[$t.Name] = [regex]::Replace($themeClassSets[$t.Name], '(?m)//.*$', '')
    }
}

$hardViolations = @()
$softCoverage = @()

# <lang>
#   <zh-CN>遍历所有带 `module.json` 的业务模块目录（仅目录级 module.json，跳过独立 ascx 文件）。</zh-CN>
#   <en>Walk every business-module directory that carries a `module.json` (directory-scoped only, skipping standalone ascx files).</en>
# </lang>
$moduleDirs = Get-ChildItem $modulesDir -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'module.json') }

foreach ($m in $moduleDirs) {
    $manifestPath = Join-Path $m.FullName 'module.json'
    $manifest = Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $cssResources = @($manifest.resources | Where-Object { $_ -like '*.css' })

    foreach ($rel in $cssResources) {
        $cssPath = Join-Path $m.FullName $rel
        if (-not (Test-Path $cssPath)) { continue }
        $css = Get-Content $cssPath -Raw -Encoding UTF8

        # <lang>
        #   <zh-CN>硬规则：模块 CSS 在 var() 之外的裸色值。</zh-CN>
        #   <en>Hard rule: raw color literals outside `var()` in the module CSS.</en>
        # </lang>
        $rawHits = Test-RawColorOutsideVar $css
        if ($rawHits.Count -gt 0) {
            $hardViolations += [pscustomobject]@{
                module  = $m.Name
                css     = $rel
                colors  = ($rawHits | Sort-Object -Unique)
            }
        }

        # <lang>
        #   <zh-CN>软指标：提取模块类，逐主题比对覆盖情况。</zh-CN>
        #   <en>Soft metric: extract module classes and compare coverage theme by theme.</en>
        # </lang>
        $moduleClasses = @(Get-CssClassNames $css) | Sort-Object
        $missingInAny = @()
        $missingInNone = @()
        $coveredInAll = 0
        $coveredInSome = 0
        foreach ($cls in $moduleClasses) {
            $presentCount = 0
            foreach ($t in $themeDirs) {
                $set = $themeClassSets[$t.Name]
                if ($set -and ($set -match ('\.{0}(?=[\s,.:{{>])' -f [regex]::Escape($cls)))) { $presentCount++ }
            }
            if ($presentCount -eq $themeDirs.Count) { $coveredInAll++ }
            elseif ($presentCount -gt 0) { $coveredInSome++; $missingInAny += $cls }
            else { $missingInNone += $cls; $missingInAny += $cls }
        }

        $softCoverage += [pscustomobject]@{
            module         = $m.Name
            css            = $rel
            totalClasses   = $moduleClasses.Count
            coveredInAll   = $coveredInAll
            coveredInSome  = $coveredInSome
            missingInAny   = $missingInAny
            missingInNone  = $missingInNone
        }
    }
}

# <lang>
#   <zh-CN>W106（C-anp-P23）二段升级：在 51 零覆盖类补齐、48 部分覆盖类残留清零后（覆盖率达 100%），
#   把"任何覆盖缺口"升为硬失败 —— 只要某模块类未在全部 7 套皮肤覆盖即 Fail，完整守护 §四.4 合规；
#   合计缺口为 0 时通过。</zh-CN>
#   <en>W106 (C-anp-P23) second promotion: after the 51 zero-coverage classes were filled and the 48 partial-coverage
#     residue cleared (coverage now 100%), any coverage gap becomes a hard failure — a module class missing from any of
#     the 7 skins fails the gate, fully guarding §四.4 compliance; passes when the total gap is 0.</en>
# </lang>
foreach ($c in $softCoverage) {
    if ($c.missingInAny.Count -gt 0) {
        $hardViolations += [pscustomobject]@{
            module        = $c.module
            css           = $c.css
            missingInAny  = $c.missingInAny
            zeroCoverage  = $c.missingInNone
        }
    }
}

# <lang>
#   <zh-CN>输出：先打软指标（覆盖缺口）到 `[GATE-NOTE]`，再据硬违规给最终结论。</zh-CN>
#   <en>Output: emit the soft coverage gaps as `[GATE-NOTE]` first, then the final verdict based on hard violations.</en>
# </lang>
Write-Host ('== 模块语义类皮肤覆盖门禁（W100 / C-anp-P18，W106 已升级零覆盖为硬失败）==')
Write-Host ('正式皮肤数：' + $themeDirs.Count + '（排除 ' + ($excludeThemes -join ', ') + '）')
Write-Host ('模块数：' + $moduleDirs.Count)

$totalGap = 0
$totalNone = 0
foreach ($c in $softCoverage) {
    $gap = $c.missingInAny.Count
    $totalGap += $gap
    $totalNone += $c.missingInNone.Count
    $verdict = if ($c.coveredInAll -eq $c.totalClasses) { '全部皮肤覆盖' } else { ('全部覆盖 ' + $c.coveredInAll + '/' + $c.totalClasses + '，部分覆盖 ' + $c.coveredInSome + '，零覆盖 ' + $c.missingInNone.Count) }
    Write-GateNote (('{0}（{1}）：{2}；未在所有皮肤覆盖的类 {3} 个' -f $c.module, $c.css, $verdict, $gap))
    if ($c.missingInNone.Count -gt 0) {
        Write-GateNote (('    -> 零覆盖（7 套皮肤均无主题规则）类：' + ($c.missingInNone -join ', ')))
    }
    elseif ($c.missingInAny.Count -gt 0) {
        Write-GateNote (('    -> 仅部分皮肤覆盖类：' + ($c.missingInAny -join ', ')))
    }
}

if ($hardViolations.Count -gt 0) {
    Write-Host ('硬违规（门禁失败）：')
    foreach ($v in $hardViolations) {
        if ($v.PSObject.Properties.Name -contains 'colors') {
            # <lang>
            #   <zh-CN>模块 CSS 在 var() 之外出现裸色值，深色皮肤下必然形成浅块。</zh-CN>
            #   <en>Module CSS has raw color literal outside `var()`, which would break dark skins.</en>
            # </lang>
            Write-Host ('  - 裸色值违规 {0}/{1}：{2}' -f $v.module, $v.css, ($v.colors -join ', '))
        }
        if ($v.PSObject.Properties.Name -contains 'missingInAny') {
            # <lang>
            #   <zh-CN>存在未在全部 7 套皮肤覆盖的类（含零覆盖与部分覆盖），§四.4 合规被回归。</zh-CN>
            #   <en>Classes are not covered in all 7 skins (zero or partial coverage); §四.4 compliance was regressed.</en>
            # </lang>
            $tail = if ($v.PSObject.Properties.Name -contains 'zeroCoverage' -and $v.zeroCoverage.Count -gt 0) { '（其中零覆盖：' + ($v.zeroCoverage -join ', ') + '）' } else { '' }
            Write-Host ('  - 覆盖缺口违规 {0}/{1}：{2}{3}' -f $v.module, $v.css, ($v.missingInAny -join ', '), $tail)
        }
    }
    Write-Host 'RESULT: Fail'
    exit 1
}

if ($totalGap -eq 0) {
    Write-GateNote ('覆盖完整：全部模块语义类在 7 套正式皮肤均有主题层作用域规则（§四.4 合规达成，C-anp-P23 成果）。')
} else {
    Write-GateNote ('覆盖缺口汇总：共 ' + $totalGap + ' 个"未在所有皮肤覆盖"的类（其中零覆盖 ' + $totalNone + ' 个）——已升为硬失败，需补齐后放行。')
}
Write-Host 'RESULT: Pass'
exit 0
