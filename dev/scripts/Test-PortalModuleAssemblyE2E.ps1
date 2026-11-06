<#
.SYNOPSIS
<lang>
  <zh-CN>模块装配态「校验 → 备份 → 回滚」端到端证据：证明字节级洁净与零残留。</zh-CN>
  <en>Module-assembly-state end-to-end "verify → backup → rollback" evidence: proves byte-level cleanliness and zero residue.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>按 W-anp-P274 规格交付的端到端证据脚本（非临时方案，可长期复用、参数化）：
  1) 校验：以正式契约门禁 Test-PortalModuleAssemblyContract.ps1 作为校验器，干净台账须 PASS；
  2) 备份：对受治理台账做字节级复制，并比对 SHA256 证明备份与原件逐字节一致；
  3) 偏离：注入一处契约门禁可抓回的偏离态（真实受信任包的 assemblyMode 取第三值），门禁须 FAIL；
  4) 回滚：用备份逐字节还原，比对 SHA256 证明回滚后与偏离前逐字节一致（零残留），门禁回绿；
  全程在 try/finally 中用备份还原真实台账，即使中断也不会把偏离态留在仓库。
  证据写入 work-zone/dev/evidence/module-assembly-e2e/，供封版前复核与里程碑验证引用。</zh-CN>
  <en>An end-to-end evidence script delivered per the W-anp-P274 spec (not a temporary hack; reusable and parameterized):
  1) verify: use the formal contract gate Test-PortalModuleAssemblyContract.ps1 as the verifier; a clean ledger must PASS;
  2) backup: byte-level copy of the governed ledger, with SHA256 compared to prove the backup is byte-identical to the original;
  3) diverge: inject a divergence state the contract gate can catch (a real trusted package with a third assemblyMode value); the gate must FAIL;
  4) rollback: restore byte-by-byte from the backup, compare SHA256 to prove the post-rollback state is byte-identical to the pre-divergence state (zero residue), and the gate goes green again;
  throughout, the real ledger is restored from the backup inside try/finally, so even an interruption leaves no divergence in the repo.
  Evidence is written to work-zone/dev/evidence/module-assembly-e2e/ for pre-seal review and milestone verification.</en>
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
$evidenceDir = Join-Path $RepoRoot 'work-zone\dev\evidence\module-assembly-e2e'
if (-not (Test-Path -LiteralPath $evidenceDir)) { New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null }
$evidencePath = Join-Path $evidenceDir ('e2e-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')

# <lang>
#   <zh-CN>运行契约门禁并返回退出码（门禁用 [FAIL]/[PASS] + exit 1/0 表达判定）。</zh-CN>
#   <en>Run the contract gate and return its exit code (the gate expresses its verdict via [FAIL]/[PASS] and exit 1/0).</en>
# </lang>
function Invoke-Gate {
    & pwsh -NoProfile -File $gate 2>&1 | Out-Null
    return $LASTEXITCODE
}

function Get-FileSha256([string] $path) {
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
}

$stages = New-Object 'System.Collections.Generic.List[PSCustomObject]'
$backupPath = Join-Path $env:TEMP ('module-assembly-state-backup-' + [System.Guid]::NewGuid().ToString('N') + '.json')

# <lang>
#   <zh-CN>校验基线：干净台账应 PASS。</zh-CN>
#   <en>Verify baseline: the clean ledger should PASS.</en>
# </lang>
$baselineExit = Invoke-Gate
$stages.Add([PSCustomObject]@{ Stage = 'verify-baseline'; Exit = $baselineExit; Expect = 0 })

try {
    # <lang>
    #   <zh-CN>备份：字节级复制并比对 SHA256。</zh-CN>
    #   <en>Backup: byte-level copy with SHA256 comparison.</en>
    # </lang>
    $preHash = Get-FileSha256 $ledgerPath
    [System.IO.File]::Copy($ledgerPath, $backupPath, $true)
    $backupHash = Get-FileSha256 $backupPath
    $stages.Add([PSCustomObject]@{ Stage = 'backup'; PreHash = $preHash; BackupHash = $backupHash; ByteIdentical = ($preHash -eq $backupHash) })

    # <lang>
    #   <zh-CN>偏离：注入契约门禁可抓回的偏离态——真实受信任包 HIA.Help 的 assemblyMode 取第三值（仅允许 CodeBehind/CodeFile）。</zh-CN>
    #   <en>Diverage: inject a divergence the contract gate can catch — a real trusted package HIA.Help with a third assemblyMode (only CodeBehind/CodeFile allowed).</en>
    # </lang>
    $diverged = '{"schemaVersion":1,"entries":{"HIA.Help":{"assemblyMode":"Dynamic"}}}'
    [System.IO.File]::WriteAllText($ledgerPath, $diverged + "`r`n", $utf8)
    $divergedHash = Get-FileSha256 $ledgerPath
    $divergedExit = Invoke-Gate
    $stages.Add([PSCustomObject]@{ Stage = 'diverge'; Hash = $divergedHash; GateExit = $divergedExit; ExpectNonZero = $true })

    # <lang>
    #   <zh-CN>回滚：用备份逐字节还原，并比对 SHA256 证明与偏离前逐字节一致（零残留）。</zh-CN>
    #   <en>Rollback: restore byte-by-byte from the backup and compare SHA256 to prove byte-identical to the pre-divergence state (zero residue).</en>
    # </lang>
    [System.IO.File]::Copy($backupPath, $ledgerPath, $true)
    $postHash = Get-FileSha256 $ledgerPath
    $postExit = Invoke-Gate
    $stages.Add([PSCustomObject]@{ Stage = 'rollback'; PostHash = $postHash; PreHash = $preHash; ByteIdenticalToPre = ($postHash -eq $preHash); GateExit = $postExit; Expect = 0 })
}
finally {
    # <lang>
    #   <zh-CN>无论如何，用备份还原真实台账并清理临时备份，确保仓库零残留。</zh-CN>
    #   <en>Regardless of outcome, restore the real ledger from the backup and clean up the temp backup, ensuring zero residue in the repo.</en>
    # </lang>
    if (Test-Path -LiteralPath $backupPath) {
        try { [System.IO.File]::Copy($backupPath, $ledgerPath, $true) } catch { }
        Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
    }
}

# <lang>
#   <zh-CN>判定：基线 PASS + 备份逐字节一致 + 偏离被门禁抓回 + 回滚逐字节还原 + 回绿，缺一不可。</zh-CN>
#   <en>Verdict: baseline PASS + backup byte-identical + divergence caught by the gate + rollback byte-identical + green again — all required.</en>
# </lang>
$ok = ($stages[0].Exit -eq 0) -and $stages[1].ByteIdentical -and ($stages[2].GateExit -ne 0) -and $stages[3].ByteIdenticalToPre -and ($stages[3].GateExit -eq 0)

$evidence = [PSCustomObject]@{
    Tool         = 'Test-PortalModuleAssemblyE2E'
    GeneratedUtc = (Get-Date -Format 'o')
    Ledger       = $ledgerPath
    Stages       = $stages
    Result       = $(if ($ok) { 'Pass' } else { 'Fail' })
}
$evidence | ConvertTo-Json -Depth 12 | Out-File -FilePath $evidencePath -Encoding utf8

foreach ($s in $stages) {
    Write-Output ('  ' + ($s | ConvertTo-Json -Compress))
}
if ($ok) {
    Write-Output ('E2E RESULT: Pass（校验→备份→回滚 字节级洁净、零残留）证据=' + $evidencePath)
    exit 0
}
Write-Output ('E2E RESULT: Fail（证据=' + $evidencePath + '）')
exit 1
