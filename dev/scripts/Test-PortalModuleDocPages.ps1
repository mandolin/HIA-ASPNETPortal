# <lang><zh-CN>
# `C-anp-P33` / `W-anp-P184`：模块独立文档页门禁（L0）。
#
# 为什么需要它：现有 `Test-PortalContentModuleDocs.ps1` 只扫 `docs/user-guide.md` 的**一节**，
# 即"11 个模块各一小节"。本周期（`C-anp-P33`，承接备忘 D9）要求**每个模块有独立页**，
# 字段表、必填与校验、操作步骤、常见问题、边界都要成篇。本门禁把页面纳入硬断言。
#
# 六类断言：
#   M1 页面存在性：登记为「已交付」的模块必须有页面文件。
#   M2 字段集 ⊇ 源码控件集：编辑页每个输入控件 id 必须在页面中出现（**词边界**匹配，
#      与 A3 同口径——`LinkTitleField` 这类"含原 id 的改名"不算命中）。
#   M3 八节齐全：用途与前置 / 操作步骤 / 字段表 / 必填与校验 / 存储与审计 / 权限 /
#      常见问题 / 边界与不做——缺一即失败（这是"详细使用说明"的可判定定义）。
#   M4 必填数量一致：编辑页 `RequiredFieldValidator` 数 == 页面中 `**必填**` 出现次数。
#   M5 部署边界必须写明：按登记表的 boundary 规则逐条检查（`Links` 仅测试页签、
#      `QuickLinks` 与 Links 共用编辑页、`HtmlModule` 不做净化、`Signin` 无运行期证据/无编辑页）。
#   M6 default-deny 自检：全部模块交付后 mode 必须切为 hard；pending 为 0 而 mode 仍是
#      soft-until-complete → 失败（防止"归零了却没人把软指标转成硬断言"，即 A6 同类问题）。
#
# 模式（soft-until-complete → hard）：批次未全部交付前，待交付模块按软指标列出，
# 避免 L0 在周期中途红；**最后一个批次交付后必须切换 hard**（由 `W-anp-P187` 执行）。
# 这条转换点写在登记表的 `mode` 字段里，由 M6 强制。
# </zh-CN>
# <en>
# `C-anp-P33` / `W-anp-P184`: the module document pages gate (L0).
#
# Why it exists: `Test-PortalContentModuleDocs.ps1` only scans one section of `docs/user-guide.md`
# (one small section per module). This cycle (`C-anp-P33`, following memo D9) requires a **dedicated page per
# module** with a field table, required/validation rules, operating steps, common questions and boundaries.
# This gate brings those pages under hard assertions.
#
# Six assertion families: M1 page existence; M2 field set ⊇ source control set (word-boundary matching, same
# convention as A3 — a rename such as `LinkTitleField` does not count); M3 all eight required sections present;
# M4 required-field count agrees with the source; M5 deployment boundaries stated; M6 default-deny self-check
# (once nothing is pending, the mode must be `hard`).
#
# Mode (soft-until-complete → hard): while batches are outstanding the remaining modules are reported as a soft
# metric so L0 does not go red mid-cycle; the **last batch must flip the mode to hard** (`W-anp-P187`).
# M6 enforces that transition.
# </en>
# <lang><zh-CN>
# 证明边界：只证明"已登记为已交付的模块页面**存在**、**八节齐全**、编辑页输入控件 id 以**词边界**出现在页面中、
# **必填项数量与源码一致**、登记表中要求的**部署边界语句已写明**"，以及"全部交付后模式必须切 hard"。
# **不证明**页面叙述的语义正确与操作步骤的可执行性（由人工复核承载）；**不检查**页面多写的字段或文字；
# **不核对**部署页签实测值（TabId/ModuleId 为交付时快照）；**不采集**运行期证据（如 `Signin` 在开发库 0 个实例，
# 只有源码级说明）。边界规则用**正则**匹配，故"写了类似的话"即算通过——它守住的是"关键事实不能被删掉"，
# 不是"话写得对不对"。
# </zh-CN>
# <en>
# Proof boundary: proves only that a module page registered as delivered **exists**, has **all eight sections**,
# contains every edit-page input control id under **word-boundary** matching, has a **required-field count equal to the
# source**, and **states the deployment boundaries** the registry requires — plus that the mode must be `hard` once
# everything is delivered. It does **not** prove the semantics of the prose or that the steps are executable (human review
# carries that); it does **not** check extra fields or extra prose; it does **not** re-verify measured deployment tab ids
# (TabId/ModuleId are delivery-time snapshots); it collects **no** runtime evidence (e.g. `Signin` has 0 instances in the
# dev database, so source-level description only). Boundary rules are matched by **regex**, so "something similar is
# written" passes — what this guards is that key facts cannot be deleted, not that the wording is right.
# </en>
param(
    [string] $RegistryPath = '',
    [string] $PagesRoot = ''
)

$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
# `dev/scripts` → 上一级是 `dev`，上上级是仓库根（只退两级；首版退了三级，落到仓库的父目录）
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $RegistryPath) { $RegistryPath = Join-Path $repoRoot 'dev\documentation\module-doc-pages.json' }
if (-not $PagesRoot) { $PagesRoot = Join-Path $repoRoot 'docs\modules' }
$modulesDir = Join-Path $repoRoot 'src\Portal\DesktopModules'

$failures = @()
$softNotes = @()

if (-not (Test-Path -LiteralPath $RegistryPath)) {
    Write-Host "[FAIL] 模块文档页门禁：登记表不存在：$RegistryPath"
    exit 1
}
try {
    $registry = [System.IO.File]::ReadAllText($RegistryPath, $utf8) | ConvertFrom-Json
} catch {
    Write-Host ("[FAIL] 模块文档页门禁：登记表不是合法 JSON：" + $_.Exception.Message)
    exit 1
}

$requiredSections = @($registry.requiredSections)
if ($requiredSections.Count -lt 8) {
    Write-Host "[FAIL] 模块文档页门禁：登记表 requiredSections 少于 8 节，无法定义「详细使用说明」。"
    exit 1
}

function Get-InputControls([string] $editPath) {
    $text = [System.IO.File]::ReadAllText($editPath, $utf8)
    $ids = @()
    foreach ($tag in [regex]::Matches($text, '<asp:(TextBox|DropDownList|CheckBox|HtmlTextArea)\b[^>]*>')) {
        $m = [regex]::Match($tag.Value, '\bid\s*=\s*"([^"]+)"', 'IgnoreCase')
        if ($m.Success) { $ids += $m.Groups[1].Value }
    }
    return $ids
}

function Get-RequiredCount([string] $editPath) {
    $text = [System.IO.File]::ReadAllText($editPath, $utf8)
    $ids = @()
    foreach ($tag in [regex]::Matches($text, '<asp:RequiredFieldValidator\b[^>]*>')) {
        $m = [regex]::Match($tag.Value, '\bid\s*=\s*"([^"]+)"', 'IgnoreCase')
        if ($m.Success) { $ids += $m.Groups[1].Value } else { $ids += ('<idless:' + $ids.Count + '>') }
    }
    return @($ids | Select-Object -Unique).Count
}

$delivered = 0
$pending = @()
$checkedModules = 0

foreach ($mod in $registry.modules) {
    $status = $mod.status
    if ($status -ne 'delivered') {
        $pending += ('{0}（批次 {1}）' -f $mod.name, $mod.batch)
        continue
    }
    $delivered++
    $pagePath = Join-Path $PagesRoot ($mod.slug + '.md')
    $pageName = $mod.name

    # ---------- M1 页面存在性 ----------
    if (-not (Test-Path -LiteralPath $pagePath)) {
        $failures += "M1 模块文档页不存在：$pageName（登记为已交付，预期 $pagePath）。"
        continue
    }
    $checkedModules++
    $page = [System.IO.File]::ReadAllText($pagePath, $utf8)

    # ---------- M3 八节齐全 ----------
    foreach ($section in $requiredSections) {
        if ($page -notmatch [regex]::Escape($section)) {
            $failures += "M3 模块文档页缺小节：$pageName 的页面必须含「$section」（八节模板，缺一即不合格）。"
        }
    }

    # ---------- 无编辑页模块 ----------
    if ([string]::IsNullOrWhiteSpace($mod.edit)) {
        # 该模块没有编辑页 → M2 / M4 不适用；但 M5 仍要求写明「无编辑页」。
        if ($page -notmatch '无编辑页') {
            $failures += "M3 无编辑页模块未声明：$pageName 的页面必须写明「无编辑页」（否则读者会以为漏写了字段）。"
        }
        continue
    }

    $editPath = Join-Path $modulesDir $mod.edit
    if (-not (Test-Path -LiteralPath $editPath)) {
        $failures += "M1 模块编辑页不存在：$pageName → $mod.edit（登记表登记的编辑页在源码中找不到）。"
        continue
    }

    # ---------- M2 字段集 ⊇ 源码控件集（词边界） ----------
    foreach ($control in (Get-InputControls $editPath)) {
        if ($page -notmatch ('(?<![\w])' + [regex]::Escape($control) + '(?![\w])')) {
            $failures += "M2 文档页未记录字段：$pageName 的编辑页控件 $control（源码有、文档缺；须以完整 id 出现，不能只作子串）。"
        }
    }

    # ---------- M4 必填数量一致 ----------
    $requiredInSource = Get-RequiredCount $editPath
    $requiredInPage = ([regex]::Matches($page, '\*\*必填\*\*')).Count
    if ($requiredInSource -ne $requiredInPage) {
        $failures += "M4 必填项数量不一致：$pageName 源码 $requiredInSource 个 vs 页面 $requiredInPage 个（源码 RequiredFieldValidator 数 == 页面 **必填** 次数）。"
    }
}

# ---------- M5 部署边界必须写明（对已交付页面） ----------
foreach ($mod in $registry.modules) {
    if ($mod.status -ne 'delivered') { continue }
    $pagePath = Join-Path $PagesRoot ($mod.slug + '.md')
    if (-not (Test-Path -LiteralPath $pagePath)) { continue }
    $page = [System.IO.File]::ReadAllText($pagePath, $utf8)
    foreach ($rule in @($mod.boundary)) {
        if ($rule.pattern -and $page -notmatch $rule.pattern) {
            $failures += ("M5 部署边界未写明：$($mod.name) 的页面必须写明「$($rule.note)」（规则 $($rule.id)）。")
        }
    }
}

# ---------- M6 default-deny 自检 ----------
$pendingCount = $pending.Count
$mode = $registry.mode
if ($pendingCount -eq 0 -and $mode -ne 'hard') {
    $failures += "M6 default-deny 未落实：已无待交付模块，但登记表 mode 仍为 '$mode'——必须切换为 'hard'（否则「已交付」这个状态没有任何约束力）。"
}

Write-Host ("[INFO] 模块文档页：已交付 " + $delivered + " 个｜已校验 " + $checkedModules + " 个｜待交付 " + $pendingCount + " 个｜模式 " + $mode)
if ($pendingCount -gt 0) {
    $softNotes += ("[soft] 待交付模块文档页 " + $pendingCount + " 个：" + ($pending -join '、') + '（批次完成后由 M6 强制切换 hard）。')
}
foreach ($n in $softNotes) { Write-Host $n }

if ($failures.Count -gt 0) {
    foreach ($f in $failures) { Write-Host ("[FAIL] " + $f) }
    Write-Host ("[FAIL] 模块文档页门禁失败：" + $failures.Count + " 项。")
    exit 1
}

Write-Host '[PASS] 模块文档页：八节齐全、字段与必填项同源码一致、部署边界已写明。'
exit 0
