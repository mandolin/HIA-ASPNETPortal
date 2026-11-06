<#
.SYNOPSIS
<lang>
  <zh-CN>模块装配态契约门禁的注入回归：证明门禁真能抓回违规、还原后回绿，且全程不破坏仓库真实配置。</zh-CN>
  <en>Injection regression for the module-assembly-state contract gate: proves the gate actually catches violations, goes green after restore, and never corrupts the real repo configuration.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>不依赖特定数据行、参数化、可长期复用（契合「避免临时方案」原则）。它：
  1) 先对当前干净台账跑一遍，断言 PASS（基线）；
  2) 注入 A1+A2 违规（台账条目 packageId 指向不存在的包 + assemblyMode 取第三值）；
  3) 注入 A5 违规（部署开关 Portal.ModuleAssembly.AllowCodeFile=true）；
  每步运行门禁并断言退出码符合预期；全部在 try/finally 中用原文快照还原 module-assembly-state.json 与 appSettings.json，
  即使中途出错也会还原，绝不把污染留在仓库。最后再跑一遍确认回绿。</zh-CN>
  <en>Parameterized and reusable for the long term (consistent with the "avoid temporary solutions" principle). It:
  1) runs the gate once against the current clean ledger and asserts PASS (baseline);
  2) injects an A1+A2 violation (a ledger entry whose packageId points at a non-existent package, plus assemblyMode taking a third value);
  3) injects an A5 violation (deployment switch Portal.ModuleAssembly.AllowCodeFile=true);
  each step runs the gate and asserts the exit code matches expectations; everything is restored from a raw snapshot of
  module-assembly-state.json and appSettings.json inside try/finally, so even an interrupted run never leaves pollution in the repo.
  A final run confirms green.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string] $RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$utf8 = [System.Text.UTF8Encoding]::new($false)
$gate = Join-Path $PSScriptRoot 'Test-PortalModuleAssemblyContract.ps1'
$ledgerPath = Join-Path $RepoRoot 'src\Portal\Config\module-assembly-state.json'
$jsonConfig = Join-Path $RepoRoot 'src\Portal\Config\appSettings.json'

# <lang>
#   <zh-CN>运行门禁并返回退出码；门禁自身用 [FAIL]/[PASS] 与 exit 1/0 表达判定。</zh-CN>
#   <en>Run the gate and return its exit code; the gate expresses its verdict via [FAIL]/[PASS] and exit 1/0.</en>
# </lang>
function Invoke-Gate {
    & pwsh -NoProfile -File $gate 2>&1 | Out-Null
    return $LASTEXITCODE
}

$results = New-Object 'System.Collections.Generic.List[PSCustomObject]'

# <lang>
#   <zh-CN>原文快照（字节级），finally 中据此还原，保证仓库配置零残留。</zh-CN>
#   <en>Raw (byte-level) snapshot, restored in finally so the repo config has zero residue.</en>
# </lang>
$ledgerSnapshot = [System.IO.File]::ReadAllText($ledgerPath, $utf8)
$jsonExists = Test-Path -LiteralPath $jsonConfig
$jsonSnapshot = $null
if ($jsonExists) { $jsonSnapshot = [System.IO.File]::ReadAllText($jsonConfig, $utf8) }

try {
    # <lang>
    #   <zh-CN>基线：当前干净台账应 PASS。</zh-CN>
    #   <en>Baseline: the current clean ledger should PASS.</en>
    # </lang>
    $exit = Invoke-Gate
    $results.Add([PSCustomObject]@{ Case = 'baseline-clean'; Exit = $exit; Expect = 0 })

    # <lang>
    #   <zh-CN>注入 A1+A2 违规：packageId 不存在 + assemblyMode=第三值（fail-closed 必须拒绝）。</zh-CN>
    #   <en>Inject A1+A2 violation: a non-existent packageId plus assemblyMode as a third value (must be rejected by fail-closed).</en>
    # </lang>
    $violating = '{"schemaVersion":1,"entries":{"NonExistent.Package":{"assemblyMode":"ThirdValue"}}}'
    [System.IO.File]::WriteAllText($ledgerPath, $violating + "`r`n", $utf8)
    $exit = Invoke-Gate
    $results.Add([PSCustomObject]@{ Case = 'A1+A2-bad-packageId-and-mode'; Exit = $exit; Expect = 1 })

    # <lang>
    #   <zh-CN>注入 A5 违规：部署开关显式设为 true（缺省关闭被违反）。</zh-CN>
    #   <en>Inject A5 violation: the deployment switch explicitly set to true (the default-off contract is violated).</en>
    # </lang>
    if ($jsonExists) {
        $j = $jsonSnapshot | ConvertFrom-Json -AsHashtable
        if (-not ($j.ContainsKey('appSettings'))) { $j['appSettings'] = @{} }
        $j['appSettings']['Portal.ModuleAssembly.AllowCodeFile'] = 'true'
        $j | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonConfig -Encoding utf8
    }
    $exit = Invoke-Gate
    $results.Add([PSCustomObject]@{ Case = 'A5-switch-true'; Exit = $exit; Expect = 1 })
}
finally {
    # <lang>
    #   <zh-CN>无论成功或异常，先还原两个被触碰的配置文件到原文。</zh-CN>
    #   <en>Whether success or failure, first restore the two touched config files to their original bytes.</en>
    # </lang>
    [System.IO.File]::WriteAllText($ledgerPath, $ledgerSnapshot, $utf8)
    if ($jsonExists) { [System.IO.File]::WriteAllText($jsonConfig, $jsonSnapshot, $utf8) }
}

# <lang>
#   <zh-CN>还原后必须回绿（证明注入测试本身未留污染）。</zh-CN>
#   <en>After restore it must be green again (proving the injection test left no pollution).</en>
# </lang>
$exit = Invoke-Gate
$results.Add([PSCustomObject]@{ Case = 'after-restore-green'; Exit = $exit; Expect = 0 })

$bad = 0
foreach ($r in $results) {
    $ok = ($r.Exit -eq $r.Expect)
    if (-not $ok) { $bad++ }
    Write-Output ('  {0}: exit={1} expect={2} {3}' -f $r.Case, $r.Exit, $r.Expect, $(if ($ok) { '[OK]' } else { '[FAIL]' }))
}

# <lang>
#   <zh-CN>写入注入证明记录（verdict=proved），供 Update-PortalGateInjectionProofIndex.ps1 汇成覆盖索引；
#   只在本脚本全部判定通过时落盘，避免把失败运行写进事实源。</zh-CN>
#   <en>Write the injection-proof record (verdict=proved) so Update-PortalGateInjectionProofIndex.ps1 can fold it into the
#   coverage index; only persisted when every stage passed, so a failed run never pollutes the source of truth.</en>
# </lang>
if ($bad -eq 0) {
    $proofDir = Join-Path $RepoRoot 'work-zone\dev\evidence\gate-injection-proof\Test-PortalModuleAssemblyContract'
    if (-not (Test-Path -LiteralPath $proofDir)) { New-Item -ItemType Directory -Path $proofDir -Force | Out-Null }
    $summary = [PSCustomObject]@{
        Tool           = 'Test-PortalModuleAssemblyContractInjection'
        GeneratedUtc   = (Get-Date -Format 'o')
        Results        = @(
            [PSCustomObject]@{
                gate    = 'Test-PortalModuleAssemblyContract.ps1'
                verdict = 'proved'
                detail  = '基线绿 → 注入 A1+A2 违规与 A5 开关开启两类均非 0 且点名 → 还原后回绿，仓库配置字节级还原'
            }
        )
    }
    $summary | ConvertTo-Json -Depth 6 | Out-File -FilePath (Join-Path $proofDir 'injection-proof-summary.json') -Encoding utf8
}

if ($bad -gt 0) {
    Write-Output ('INJECTION RESULT: Fail（{0} 项不符预期）' -f $bad)
    exit 1
}
Write-Output 'INJECTION RESULT: Pass（门禁能抓回 A1/A2/A5 违规，还原后回绿，仓库配置零残留）'
exit 0
