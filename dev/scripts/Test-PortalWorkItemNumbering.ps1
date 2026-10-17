#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>工作项编号规范门禁：校验 `C-anp-P*` / `W-anp-P*` 编号唯一、号段无悬空，且内部编号未外泄到公开面。</zh-CN>
  <en>Work-item numbering gate: verifies that `C-anp-P*` / `W-anp-P*` numbers stay unique and dangling-free, and that internal numbers do not leak into public prose.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>**前置条件**：仓库可读；内部台账 `work-zone/dev/plans/` 不存在时，N1/N2/N3 自动跳过（公开仓不含内部计划文档）。
  **验证方式**：退出码 0 表示四项检查全过、1 表示存在失败项；输出逐项列出各检查结论。
  **低敏边界**：只读取计划/工作项文档标题与正文文本，不读取凭据、连接串或业务数据。</zh-CN>
  <en>**Prerequisites**: the repository must be readable; when the internal ledger `work-zone/dev/plans/` is absent, N1/N2/N3 are skipped (the public repo carries no internal plan docs).
  **Verification**: exit code 0 means all four checks pass, 1 means at least one failed; the output lists each conclusion.
  **Low-sensitivity boundary**: only plan/work-item document titles and prose text are read — no credentials, connection strings or business data.</en>
</lang>
#>

# <lang>
#   <zh-CN>工作项编号规范门禁：守住 `C-anp-P*` / `W-anp-P*` 编号体系的**唯一性**、**连续可追溯性**与**公开面洁净**。
#
#   背景（本项目已多次发生）：多个 `C-anp-P*` 周期**并行推进**时，工作包号段被后来的周期顺延或占用，
#   导致 ① 同一编号被两个周期声明（冲突）② 号段跳跃后无人认领（遗漏）③ 内部编号留在公开文档正文（外泄）。
#   三者都**不会**被编译或本地测试发现 —— 本门禁把这件事交给机器。
#
#   判定口径（关键）：只有周期计划文件（`C-anp-Pn.md`）里「## 二、W 工作包」一节列出的编号才算
#   **占用声明**；其余位置出现的编号一律视为**交叉引用**，不计入占用。否则历史计划的互相引用会全部误报冲突。
#
#   检查项：
#     N1 唯一性 —— 同一 W 编号不得被两个周期计划同时声明为本周期工作包。
#     N2 登记完整性 —— 1..max 每个编号必须有归属：被某周期声明 / 在 `works-index.md` 显式登记 / 已有独立工作项文档。
#     N3 工作包数量 —— 每个含工作包节的周期计划须声明 5–8 个 W 包（约定提醒第 1 条）。
#     N4 公开面洁净 —— 公开文档正文（代码块外）不得出现内部编号形态。
#
#   证明边界：只证明编号**不冲突、不悬空、未外泄**；**不证明**工作包内容已完成。
# </lang>
# <lang>
#   <en>Work-item numbering gate: keeps the `C-anp-P*` / `W-anp-P*` scheme **unique**, **continuously
#   traceable** and **clean on the public surface**.
#
#   Background (repeated here): several `C-anp-P*` cycles running **in parallel** defer or take over
#   work-package ranges, causing (1) one W number claimed by two cycles, (2) skipped ranges nobody owns,
#   and (3) internal numbers left in public prose. None are caught by compilation or local tests.
#
#   Decision rule (key): only the numbers listed under the `## 二、W 工作包` section of a cycle plan
#   (`C-anp-Pn.md`) count as a **claim**; numbers elsewhere are **cross-references** and are ignored
#   for conflict detection. Otherwise every historical back-reference would be a false conflict.
#
#   Checks: N1 uniqueness; N2 registry integrity; N3 package count 5–8; N4 public prose clean.
#
#   Proof boundary: proves numbers do not collide, dangle or leak — **not** that work is complete.
# </lang>

param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

$utf8        = [System.Text.UTF8Encoding]::new($false)
$idPattern   = '[CW]-anp-P\d+'
$wPkgPattern = 'W-anp-P(\d+)'

$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param([string] $Code, [bool] $Passed, [string] $Detail)
    $script:checks.Add([pscustomobject]@{ Code = $Code; Passed = $Passed; Detail = $Detail })
}

# <lang>
#   <zh-CN>剥离围栏代码块与行内代码后再判定：脚本名、路径、版本号里的形似文本不算编号。
# </lang>
# <lang>
#   <en>Strip fenced blocks and inline code spans: look-alike text in script names, paths or version
#   strings is not a work-item number.
# </lang>
function Get-ProseLines {
    param([string] $FilePath)
    $lines  = [System.IO.File]::ReadAllLines($FilePath, $utf8)
    $result = [System.Collections.Generic.List[object]]::new()
    $fence  = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $raw = [string]$lines[$i]
        if ($raw -match '^\s*```') { $fence = -not $fence; continue }
        if ($fence) { continue }
        $noCode = [regex]::Replace($raw, '`[^`]*`', '')
        $result.Add([pscustomobject]@{ Number = ($i + 1); Text = $noCode })
    }
    return ,$result
}

function Get-Matches {
    param([string] $Text, [string] $Pattern)
    return ,([regex]::Matches($Text, $Pattern))
}

function New-IntSet {
    return ,([System.Collections.Generic.HashSet[int]]::new())
}

# <lang>
#   <zh-CN>只取「## 二、W 工作包」一节的编号作为本周期占用声明。
# </lang>
# <lang>
#   <en>Read only the `## 二、W 工作包` section when collecting a cycle's own claims.
# </lang>
function Test-CycleTemplate {
    param([string] $FilePath)
    $ls = [System.IO.File]::ReadAllLines($FilePath, $utf8)
    foreach ($l in $ls) {
        if ([string]$l -match '^##\s+二[、.]') { return $true }
    }
    return $false
}

function Get-ClaimedNumbers {
    param([string] $FilePath)
    $lines     = [System.IO.File]::ReadAllLines($FilePath, $utf8)
    $nums      = New-IntSet
    $inSection = $false
    foreach ($raw in $lines) {
        $l = [string]$raw
        if ($l -match '^##\s+二[、.]') { $inSection = $true; continue }
        if ($inSection -and $l -match '^##\s') { break }
        if (-not $inSection) { continue }
        # Do NOT strip inline code here: cycle tables write the IDs as `W-anp-P108`,
        # and those backticked IDs are exactly the claim we need to read.
        foreach ($m in (Get-Matches -Text $l -Pattern $wPkgPattern)) {
            [void]$nums.Add([int]$m.Groups[1].Value)
        }
    }
    return ,$nums
}

$plansDir     = Join-Path $RepoRoot 'work-zone/dev/plans'
$cyclesDir    = Join-Path $plansDir 'cycles'
$worksDir     = Join-Path $plansDir 'works'
$registryPath = Join-Path $plansDir 'works-index.md'
$hasLedger    = Test-Path -LiteralPath $plansDir

# ---------------------------------------------------------------- N4: 公开面 ---
$publicPaths = [System.Collections.Generic.List[string]]::new()
foreach ($rel in @('docs', 'site/_translations')) {
    $dir = Join-Path $RepoRoot $rel
    if (Test-Path -LiteralPath $dir) {
        foreach ($f in (Get-ChildItem -LiteralPath $dir -Recurse -Filter '*.md' -File)) {
            $publicPaths.Add($f.FullName)
        }
    }
}
$siteDir = Join-Path $RepoRoot 'site'
if (Test-Path -LiteralPath $siteDir) {
    foreach ($f in (Get-ChildItem -LiteralPath $siteDir -Recurse -Filter 'index.md' -File)) {
        $publicPaths.Add($f.FullName)
    }
}
$readmePath = Join-Path $RepoRoot 'README.md'
if (Test-Path -LiteralPath $readmePath) { $publicPaths.Add($readmePath) }

$leaks = [System.Collections.Generic.List[string]]::new()
foreach ($p in $publicPaths) {
    if (-not (Test-Path -LiteralPath $p)) { continue }
    foreach ($ln in (Get-ProseLines -FilePath $p)) {
        if ($ln.Text -match $idPattern) {
            # Report file and line only — never echo the matched text into logs.
            $rel = $p.Replace($RepoRoot + [IO.Path]::DirectorySeparatorChar, '')
            $leaks.Add(('{0}:{1}' -f $rel, $ln.Number))
        }
    }
}

Add-Check -Code 'N4-PUBLIC-SURFACE' -Passed ($leaks.Count -eq 0) -Detail ('公开面内部编号命中 ' + $leaks.Count + ' 处')
foreach ($l in ($leaks | Select-Object -First 10)) {
    Write-Host ('  [Fail] N4 内部编号外泄: ' + $l)
}

# ------------------------------------------------- N1/N2/N3: 内部台账 -----
if (-not $hasLedger) {
    Write-Host '  [GATE-NOTE] 未检测到内部台账 work-zone/dev/plans/ —— 跳过 N1/N2/N3'
}
else {
    $owner     = @{}
    $conflicts = [System.Collections.Generic.List[string]]::new()
    $cycleFiles = @(Get-ChildItem -LiteralPath $cyclesDir -Filter 'C-anp-P*.md' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^C-anp-P\d+\.md$' })

    foreach ($cf in $cycleFiles) {
        $cycleNumber = 0
        if ($cf.Name -match '^C-anp-P(\d+)\.md$') { $cycleNumber = [int]$Matches[1] }
        $claimed = Get-ClaimedNumbers -FilePath $cf.FullName

        # The 5–8 package convention is enforced from P26 onwards; historical cycles
        # (P1–P25) are registered but not retro-checked (see works-index.md §1).
        if ($cycleNumber -ge 26 -and ($claimed.Count -lt 5 -or $claimed.Count -gt 8)) {
            Add-Check -Code ('N3-PKG-COUNT/' + $cf.Name) -Passed $false -Detail ('声明 ' + $claimed.Count + ' 个 W 包，约定为 5–8 个')
        }

        foreach ($wn in $claimed) {
            if ($owner.ContainsKey($wn)) {
                if ($owner[$wn] -ne $cycleNumber) {
                    $conflicts.Add(('W-anp-P' + $wn + ' 同时被 C-anp-P' + $owner[$wn] + ' 与 ' + $cf.Name + ' 声明'))
                }
            }
            else {
                $owner[$wn] = $cycleNumber
            }
        }
    }

    if ($conflicts.Count -eq 0) {
        Add-Check -Code 'N1-UNIQUENESS' -Passed $true -Detail '跨周期无编号冲突'
    }
    else {
        foreach ($c in ($conflicts | Select-Object -First 10)) {
            Write-Host ('  [Fail] N1 编号冲突: ' + $c)
        }
        Add-Check -Code 'N1-UNIQUENESS' -Passed $false -Detail ('共 ' + $conflicts.Count + ' 处冲突')
    }

    # --- N2: 每个编号必须有归属（声明 / 登记册 / 独立工作项文档）---
    $declared = New-IntSet
    if (Test-Path -LiteralPath $registryPath) {
        $regText = [System.IO.File]::ReadAllText($registryPath, $utf8)
        foreach ($m in (Get-Matches -Text $regText -Pattern $wPkgPattern)) {
            [void]$declared.Add([int]$m.Groups[1].Value)
        }
    }

    $documented = New-IntSet
    if (Test-Path -LiteralPath $worksDir) {
        foreach ($f in (Get-ChildItem -LiteralPath $worksDir -File -ErrorAction SilentlyContinue)) {
            if ($f.Name -match '^W-anp-P(\d+)') {
                [void]$documented.Add([int]$Matches[1])
            }
        }
    }

    $maxNumber = 0
    foreach ($k in $owner.Keys)     { if ($k -gt $maxNumber) { $maxNumber = $k } }
    foreach ($d in $declared)        { if ($d -gt $maxNumber) { $maxNumber = $d } }
    foreach ($d in $documented)      { if ($d -gt $maxNumber) { $maxNumber = $d } }

    $undeclared = [System.Collections.Generic.List[int]]::new()
    for ($i = 1; $i -le $maxNumber; $i++) {
        if (-not $owner.ContainsKey($i) -and -not $declared.Contains($i) -and -not $documented.Contains($i)) {
            $undeclared.Add($i)
        }
    }

    $ranges = @()
    if ($undeclared.Count -gt 0) {
        $sorted = @($undeclared | Sort-Object)
        $start  = $sorted[0]
        $prev   = $sorted[0]
        foreach ($v in ($sorted | Select-Object -Skip 1)) {
            if ($v -ne ($prev + 1)) {
                if ($start -eq $prev) { $ranges += [string]$start } else { $ranges += ($start.ToString() + '-' + $prev.ToString()) }
                $start = $v
            }
            $prev = $v
        }
        if ($start -eq $prev) { $ranges += [string]$start } else { $ranges += ($start.ToString() + '-' + $prev.ToString()) }
    }

    if ($undeclared.Count -eq 0) {
        Add-Check -Code 'N2-RANGE-REGISTRY' -Passed $true -Detail ('号段 1..' + $maxNumber + ' 无悬空编号')
    }
    else {
        Add-Check -Code 'N2-RANGE-REGISTRY' -Passed $false -Detail ('未登记编号: ' + ($ranges -join ', '))
    }
}

$failedChecks = @($checks | Where-Object { -not $_.Passed }).Count

Write-Host ''
foreach ($c in $checks) {
    $mark = if ($c.Passed) { '[Pass]' } else { '[Fail]' }
    Write-Host ("  {0} {1} — {2}" -f $mark, $c.Code, $c.Detail)
}

Write-Output ([pscustomobject]@{
    Gate           = 'Test-PortalWorkItemNumbering.ps1'
    ExecutionMode  = 'read-only-gate'
    ExitCodePolicy = 'Exit 1 when any N* check fails'
    FailedChecks   = $failedChecks
    CheckCount     = $checks.Count
    Proves         = '工作项编号不冲突、不悬空、未外泄到公开面'
    DoesNotProve = '被引用的工作包确实已完成'
})

if ($failedChecks -gt 0) { exit 1 }

exit 0
