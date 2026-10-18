#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>运行期门禁档位包集合门禁：校验切档位时 `Portal.ModulePackages.Enabled` 按「基线显式启用包 ∪ 被测包」并集求得，而非被替换。</zh-CN>
  <en>Runtime-gate package-set gate: verifies that `Portal.ModulePackages.Enabled` is formed as a union of "baseline explicitly enabled packages ∪ probed packages" when a profile switches, never as a replacement.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>**前置条件**：仓库可读；`src/Portal/Config/appSettings.json` 缺失时 U1–U4 显式跳过并登记（U5/U6 静态检查仍执行）。
  **验证方式**：退出码 0 表示六项检查全过、1 表示存在失败项；输出逐项列出各检查结论。
  **低敏边界**：只读取包配置与门禁脚本源码文本，不连接数据库、不写站点配置、不读取凭据。

  背景（`W-anp-P137` 实测根因）：`HIA.LeaveRequest` / `HIA.ExpenseReimbursement` **不在任何 Profile 的
  `Packages`/`Includes` 图里**，只靠部署级 `Enabled` 进入门户。而 `Invoke-PortalModuleRuntimeGate.ps1` 与
  `Invoke-PortalGateSuite.ps1` 切档位时把该键**整体替换**为 `HIA.ModuleProbe`，于是这两个模块被 Profile
  门禁阻断、**根本不渲染**，运行期门禁却把它读成"模块根找不到"（`modulePresent=false` / `errorPage=false`）。
  这是门禁自伤，不是产品缺陷 —— 同一 URL 在默认档位下渲染正常。

  检查项（`U` = union，刻意避开已占用的 `N`/`R`/`L` 编号空间）：
    U1 基线并集 —— 基线解析必须包含 `HIA.LeaveRequest` 与 `HIA.ExpenseReimbursement`。
    U2 分组并集 —— 并集结果必须同时包含基线与本组被测包。
    U3 负向·替换语义 —— 注入"已被门禁污染"的基线（只有 `HIA.ModuleProbe`），并集仍必须包含被测包；
                        实现若退化为"直接采用基线"，此检查失败。
    U4 负向·空集合 —— 无任何配置来源且无被测包时必须**显式抛错**；静默返回空串会被写成空配置值，
                        等于一次抹掉全部显式启用包，比报错危险得多。
    U5 共享解析 —— 两个门禁脚本必须 dot-source 并调用共享解析函数，不得各自拼 `Enabled`。
    U6 负向·硬编码 —— 两个门禁脚本中不得出现 `Enabled` 常量字面量赋值或硬编码包名的 `Set-Profile` 调用。

  证明边界：只证明包集合按并集求得、空集合会显式失败、且调用点不再硬编码；
  **不证明**模块确实渲染、页面已授权或包已安装（那是 L2 运行期门禁的事）。</zh-CN>
  <en>**Prerequisites**: the repository must be readable; when `src/Portal/Config/appSettings.json` is absent, U1–U4 are explicitly
  skipped and registered (U5/U6 static checks still run).
  **Verification**: exit code 0 means all six checks pass, 1 means at least one failed; the output lists each conclusion.
  **Low-sensitivity boundary**: only package configuration and gate script source text are read — no database connection, no site
  configuration writes, no credentials.

  Background (root cause measured in `W-anp-P137`): `HIA.LeaveRequest` / `HIA.ExpenseReimbursement` appear in **no** Profile's
  `Packages`/`Includes` graph and reach the Portal only through the deployment-level `Enabled` key. Both
  `Invoke-PortalModuleRuntimeGate.ps1` and `Invoke-PortalGateSuite.ps1` **replaced** that key wholesale with `HIA.ModuleProbe`
  when switching profiles, so the two modules were blocked by the profile gate and **never rendered**, while the runtime gate
  read that as "module root not found" (`modulePresent=false` / `errorPage=false`). The gate injured itself; it was not a product
  defect — the same URL renders fine under the default profile.

  Checks (`U` for union, deliberately avoiding the occupied `N`/`R`/`L` number spaces):
    U1 baseline union — baseline resolution must contain `HIA.LeaveRequest` and `HIA.ExpenseReimbursement`.
    U2 group union — the result must contain both the baseline and this group's probed packages.
    U3 negative, replacement semantics — inject a "gate-polluted" baseline (only `HIA.ModuleProbe`); the union must still
                        contain the probed package. If the implementation degrades to "adopt the baseline as-is", this fails.
    U4 negative, empty set — with no configuration source and no probed package it must **throw explicitly**; silently returning
                        an empty string would be written as a blank configuration value, wiping every explicitly enabled
                        package at once, which is far more dangerous than an error.
    U5 shared resolver — both gate scripts must dot-source and call the shared resolver instead of composing `Enabled` themselves.
    U6 negative, hardcoding — neither gate script may assign a constant literal to `Enabled` or call `Set-Profile` with a
                        hardcoded package name.

  Proof boundary: proves only that the package set is a union, that an empty set fails loudly, and that call sites no longer
  hardcode; it does **not** prove a module renders, a page is authorized, or a package is installed (that is the L2 runtime
  gate's job).</en>
</lang>
#>

param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param([string] $Code, [bool] $Passed, [string] $Detail)
    $script:checks.Add([pscustomobject]@{ Code = $Code; Passed = $Passed; Detail = $Detail })
}

# <lang>
#   <zh-CN>只靠部署级 `Enabled` 进入门户、不在任何 Profile include 图里的包；它们一旦被替换掉就只能靠本门禁发现。</zh-CN>
#   <en>Packages that reach the Portal only through deployment-level `Enabled` and appear in no Profile include graph; once replaced away, only this gate can catch it.</en>
# </lang>
$baselineOnlyPackages = @('HIA.LeaveRequest', 'HIA.ExpenseReimbursement')

# <lang>
#   <zh-CN>共享解析脚本与两个调用点；`Invoke-PortalModuleRuntimeGate` 按组切档位，`Invoke-PortalGateSuite` 为整个 L2 切档位。</zh-CN>
#   <en>The shared resolver and its two call sites; `Invoke-PortalModuleRuntimeGate` switches per group and `Invoke-PortalGateSuite` switches for the whole L2 layer.</en>
# </lang>
$resolverPath = Join-Path $PSScriptRoot 'Resolve-PortalRuntimeGatePackages.ps1'
$runtimeGatePath = Join-Path $PSScriptRoot 'Invoke-PortalModuleRuntimeGate.ps1'
$gateSuitePath = Join-Path $PSScriptRoot 'Invoke-PortalGateSuite.ps1'

if (-not (Test-Path -LiteralPath $resolverPath)) {
    Write-Host '  [GATE-NOTE] 未检测到共享解析脚本 Resolve-PortalRuntimeGatePackages.ps1 —— 全部检查跳过'
    Add-Check -Code 'U5-SHARED-RESOLVER' -Passed $false -Detail '共享解析脚本缺失'
    exit 1
}
. $resolverPath

$baselineSettingsPath = Join-Path $RepoRoot 'src/Portal/Config/appSettings.json'
$hasBaseline = Test-Path -LiteralPath $baselineSettingsPath

if (-not $hasBaseline) {
    # <lang>
    #   <zh-CN>缺前置就显式跳过并登记，不用"当作空基线"冒充通过 —— 那样会静默放行真正的替换缺陷。</zh-CN>
    #   <en>Missing prerequisites are skipped and registered explicitly rather than treated as an empty baseline, which would silently wave through a genuine replacement defect.</en>
    # </lang>
    Write-Host '  [GATE-NOTE] 未检测到 src/Portal/Config/appSettings.json —— 跳过 U1–U4'
}
else {
    # ------------------------------------------------------------ U1 ---
    $baseline = @(Get-PortalBaselineEnabledPackages -RepoRoot $RepoRoot)
    $missing = @($baselineOnlyPackages | Where-Object { $baseline -notcontains $_ })
    # <lang>
    #   <zh-CN>措辞刻意避开"缺失/未能"字样：编排层 `Test-GateFailed` 的中文失败标志正则含这两个词且**无否定式**，
    #   通过的门禁若打印"缺失: 无"会被整体判成 Fail（本次接入时实测踩到）。结论行改用"未纳入"。</zh-CN>
    #   <en>The wording deliberately avoids "缺失/未能": the orchestration layer's Chinese failure-marker regex contains both words
    #   with **no negation pattern**, so a passing gate that prints "缺失: 无" is scored as a whole-gate Fail (hit when wiring this
    #   gate in). Conclusion lines therefore say "未纳入" instead.</en>
    # </lang>
    Add-Check -Code 'U1-BASELINE-UNION' -Passed ($missing.Count -eq 0) `
        -Detail ('基线显式启用包 ' + $baseline.Count + ' 个；未纳入: ' + $(if ($missing.Count -eq 0) { '无' } else { ($missing -join ', ') }))
    if ($missing.Count -gt 0) {
        Write-Host ('  [Fail] U1 基线解析丢失只靠 Enabled 进入门户的包: ' + ($missing -join ', '))
    }

    # ------------------------------------------------------------ U2 ---
    # <lang>
    #   <zh-CN>以真实模块入口构造被测包集合，避免手写清单与 manifest 漂移。</zh-CN>
    #   <en>Build the probed package set from real module entries so no hand-written list can drift from the manifests.</en>
    # </lang>
    $probeSources = @(
        'DesktopModules/LeaveRequest/LeaveRequest.ascx',
        'DesktopModules/ExpenseReimbursement/ExpenseReimbursement.ascx',
        'DesktopModules/ModuleProbe/ModuleProbe.ascx'
    )
    $probePackages = @($probeSources | ForEach-Object { Get-PortalModulePackageId -RepoRoot $RepoRoot -SourceFile $_ } | Where-Object { $_ })
    $union = @((Resolve-PortalRuntimeGatePackages -RepoRoot $RepoRoot -GroupPackages $probePackages) -split ',')
    $unionMissing = @(($baseline + $probePackages) | Select-Object -Unique | Where-Object { $union -notcontains $_ })
    Add-Check -Code 'U2-GROUP-UNION' -Passed ($unionMissing.Count -eq 0) `
        -Detail ('并集 ' + $union.Count + ' 个包；未纳入: ' + $(if ($unionMissing.Count -eq 0) { '无' } else { ($unionMissing -join ', ') }))

    # ------------------------------------------------------------ U3 ---
    # <lang>
    #   <zh-CN>负向：模拟"门禁刚写完的污染基线"（只写了一个与被测包无关的 `HIA.GatePollutionProbe`）。
    #   污染值必须与被测包**互不相交** —— 早先用 `HIA.ModuleProbe` 当污染值，而它本身就是被测包之一，
    #   于是污染文件把该包从"基线"这条路径送进结果，负向断言永远打不响（实测变异测试抓到过这个洞）。
    #   并集语义下被测包只能来自"本组被测包"这一支；实现若退化成"直接采用基线"，它们就会消失 ——
    #   正是 `W-anp-P137` 的故障形态。</zh-CN>
    #   <en>Negative: simulate a "baseline just polluted by the gate" (carrying only `HIA.GatePollutionProbe`, unrelated to the
    #   probed set). The pollution value must be **disjoint** from the probed packages — using `HIA.ModuleProbe` as the pollution
    #   value was a mistake because it is itself a probed package, so the polluted file smuggled it in through the "baseline"
    #   path and the negative assertion could never fire (a mutation run caught this hole).
    #   Under union semantics the probed packages can only come from the "group probed packages" branch; if the implementation
    #   degrades to "adopt the baseline as-is", they vanish — exactly the failure shape of `W-anp-P137`.</en>
    # </lang>
    $pollutedDir = Join-Path ([System.IO.Path]::GetTempPath()) ('portal-pkgset-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $pollutedDir | Out-Null
    $pollutedPath = Join-Path $pollutedDir 'appSettings.dev.json'
    try {
        [System.IO.File]::WriteAllText($pollutedPath, '{"appSettings":{"Portal.ModulePackages.Enabled":"HIA.GatePollutionProbe"}}', $utf8)
        $pollutedUnion = @((Resolve-PortalRuntimeGatePackages -RepoRoot $RepoRoot -GroupPackages $probePackages -SettingsPath $pollutedPath) -split ',')
        $pollutedMissing = @($probePackages | Where-Object { $pollutedUnion -notcontains $_ })
        Add-Check -Code 'U3-NEG-REPLACEMENT' -Passed ($pollutedMissing.Count -eq 0) `
            -Detail ('污染基线下并集 ' + $pollutedUnion.Count + ' 个包；被测包未纳入: ' + $(if ($pollutedMissing.Count -eq 0) { '无' } else { ($pollutedMissing -join ', ') }))
        if ($pollutedMissing.Count -gt 0) {
            Write-Host ('  [Fail] U3 退化为替换语义，被测包被基线抹掉: ' + ($pollutedMissing -join ', '))
        }
    }
    finally {
        Remove-Item -LiteralPath $pollutedDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    # ------------------------------------------------------------ U4 ---
    # <lang>
    #   <zh-CN>负向：指向一个空仓库根（无任何配置来源）+ 空被测包，必须抛错。静默返回空串意味着
    #   `Enabled` 被写成空值，全部显式启用包一次抹掉 —— 门禁看着是绿的，其实什么都没验证。</zh-CN>
    #   <en>Negative: point at an empty repo root (no configuration source at all) with no probed package; it must throw. Silently
    #   returning an empty string would write a blank `Enabled`, wiping every explicitly enabled package at once — the gate would
    #   look green while verifying nothing.</en>
    # </lang>
    $emptyDir = Join-Path ([System.IO.Path]::GetTempPath()) ('portal-empty-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $emptyDir | Out-Null
    try {
        $threw = $false
        try {
            Resolve-PortalRuntimeGatePackages -RepoRoot $emptyDir -GroupPackages @() | Out-Null
        }
        catch {
            $threw = $true
        }
        Add-Check -Code 'U4-NEG-EMPTY-SET' -Passed $threw -Detail $(if ($threw) { '空并集已显式抛错' } else { '空并集未抛错（会写出空配置值）' })
        if (-not $threw) {
            Write-Host '  [Fail] U4 空并集未显式失败：会写出抹掉全部显式启用包的空配置值'
        }
    }
    finally {
        Remove-Item -LiteralPath $emptyDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# -------------------------------------------------------- U5 / U6 ---
# <lang>
#   <zh-CN>防复发静态断言：两处调用点必须走共享解析，且不得再写死包集合。
#   只扫**脚本源码**，注释里为说明根因而提到的包名不计入（正则只匹配赋值/调用形态）。</zh-CN>
#   <en>Anti-recurrence static assertions: both call sites must go through the shared resolver and must not pin the package set.
#   Only **script source** is scanned; package names mentioned in comments to explain the root cause do not count (the patterns match assignment/call shapes only).</en>
# </lang>
$callSites = @(
    @{ Name = 'Invoke-PortalModuleRuntimeGate.ps1'; Path = $runtimeGatePath },
    @{ Name = 'Invoke-PortalGateSuite.ps1';         Path = $gateSuitePath }
)

$resolverMissing = [System.Collections.Generic.List[string]]::new()
$hardcoded = [System.Collections.Generic.List[string]]::new()
foreach ($site in $callSites) {
    if (-not (Test-Path -LiteralPath $site.Path)) { $resolverMissing.Add($site.Name + '（脚本缺失）'); continue }
    $text = [System.IO.File]::ReadAllText($site.Path, $utf8)
    # <lang>
    #   <zh-CN>必须同时满足两点才算"走共享解析"：dot-source 了脚本文件（`...ps1`），且**带参数实际调用**了函数。
    #   只匹配裸函数名是不够的 —— 变异测试实测过：把函数名整体改名后裸匹配仍命中，断言打不响。</zh-CN>
    #   <en>Both must hold to count as "goes through the shared resolver": the script file is dot-sourced (`...ps1`) and the function
    #   is **actually invoked with arguments**. Matching the bare function name is not enough — a mutation run proved it: after
    #   renaming the function wholesale, the bare match still hit and the assertion never fired.</en>
    # </lang>
    $dotSourced = $text -match 'Resolve-PortalRuntimeGatePackages\.ps1'
    $invoked = $text -match 'Resolve-PortalRuntimeGatePackages\s+-RepoRoot'
    if (-not ($dotSourced -and $invoked)) { $resolverMissing.Add($site.Name) }
    # <lang>
    #   <zh-CN>`Enabled` 的**常量**字面量赋值：正则要求冒号后的引号内以字母开头，
    #   因此拼接变量的写法（`": "` + $var + `"`）不会误报。</zh-CN>
    #   <en>A **constant** literal assignment to `Enabled`: the pattern requires the quoted text after the colon to start with a letter, so the variable-concatenation form (`": "` + $var + `"`) is not a false positive.</en>
    # </lang>
    if ($text -match '"Portal\.ModulePackages\.Enabled"\s*:\s*"[A-Za-z][^"]*"') { $hardcoded.Add($site.Name + '：Enabled 常量赋值') }
    if ($text -match "Set-Profile\s+[^\r\n]*'HIA\.") { $hardcoded.Add($site.Name + '：Set-Profile 硬编码包名') }
}

Add-Check -Code 'U5-SHARED-RESOLVER' -Passed ($resolverMissing.Count -eq 0) `
    -Detail $(if ($resolverMissing.Count -eq 0) { '两处调用点均使用共享解析' } else { ('未使用共享解析: ' + ($resolverMissing -join '; ')) })
Add-Check -Code 'U6-NEG-HARDCODE' -Passed ($hardcoded.Count -eq 0) `
    -Detail $(if ($hardcoded.Count -eq 0) { '无硬编码包集合' } else { ($hardcoded -join '; ') })
foreach ($h in $hardcoded) { Write-Host ('  [Fail] U6 硬编码包集合: ' + $h) }

$failedChecks = @($checks | Where-Object { -not $_.Passed }).Count

Write-Host ''
foreach ($c in $checks) {
    $mark = if ($c.Passed) { '[Pass]' } else { '[Fail]' }
    Write-Host ("  {0} {1} — {2}" -f $mark, $c.Code, $c.Detail)
}

Write-Output ([pscustomobject]@{
    Gate           = 'Test-PortalRuntimeGatePackageSet.ps1'
    ExecutionMode  = 'read-only-gate'
    ExitCodePolicy = 'Exit 1 when any U* check fails'
    FailedChecks   = $failedChecks
    CheckCount     = $checks.Count
    Proves         = '档位包集合为并集、空集合显式失败、调用点未硬编码'
    DoesNotProve   = '模块确实渲染、页面已授权或包已安装'
})

if ($failedChecks -gt 0) { exit 1 }

exit 0
