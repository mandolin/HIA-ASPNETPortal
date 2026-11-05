<#
.SYNOPSIS
  <lang>
    <zh-CN>重算「门禁注入法双向证明覆盖索引」，把散落的证据汇总成唯一机器事实源。</zh-CN>
    <en>Recomputes the gate-injection-proof coverage index, consolidating scattered evidence into the single machine source of truth.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>起因：`W-anp-P267` 早期批次每次都用一段手敲的 PowerShell 重算 `dev/gate-injection-proof-index.json`。
    这段逻辑没有入库、口径只活在对话里，属于长期项目应避免的临时方案：换人接手就会重算错、漏算或改口径。
    本脚本把该逻辑固化为可重复执行的正式入口，并接上三类权威事实源：
      ① 证据记录：`work-zone/dev/evidence/gate-injection-proof/**/injection-proof-summary.json`（口径只认 verdict，不认夹具里出现过门禁名）；
      ② 已接入门禁集：`Invoke-PortalGateSuite.ps1` 全文件的 `File = '...'` 条目；
      ③ 已裁定不适用台账：`dev/gate-injection-proof-not-applicable.json`（与 neverTouched 区分，避免反复重新评估）。
    四类清单（proved / notDetected / other / neverTouched）互斥且并集等于已接入门禁集；
    notApplicable 单独列出、不计入 proved 也不计入 neverTouched，但其条目必须真实存在于 wired 集合中。
    索引写出后由 `Test-PortalGateCoverageMap.ps1` 的 M7 双向核对名字与磁盘文件，杜绝前缀错配型误判。</zh-CN>
    <en>Origin: early `W-anp-P267` batches recomputed `dev/gate-injection-proof-index.json` with a hand-typed PowerShell
    snippet each time. That logic was not committed and its semantics lived only in a chat transcript — exactly the kind
    of temporary workaround a long-lived project should avoid: the next maintainer would miscompute, omit or silently
    change the rules. This script makes it a repeatable, committed entry point wired to three authoritative sources:
      (1) evidence records `work-zone/dev/evidence/gate-injection-proof/**/injection-proof-summary.json` (verdict-driven;
          a gate merely appearing in a fixture counts for nothing);
      (2) the wired gate set: every `File = '...'` entry in `Invoke-PortalGateSuite.ps1`;
      (3) the adjudicated-not-applicable ledger `dev/gate-injection-proof-not-applicable.json` (kept distinct from
          neverTouched so the same gate is not re-litigated every batch).
    The four lists (proved / notDetected / other / neverTouched) are mutually exclusive and their union equals the wired
    set; notApplicable is listed separately, counted in neither proved nor neverTouched, but its entries must genuinely
    exist in the wired set. After writing, M7 in `Test-PortalGateCoverageMap.ps1` cross-checks names and files both ways,
    preventing the prefix-mismatch misjudgement that once produced a phantom-gate conclusion.</en>
  </lang>
.PARAMETER RepoRoot
  <lang><zh-CN>仓库根目录；默认按脚本位置（dev/scripts 的上两级）推导。</zh-CN><en>Repository root; defaults to two levels above this script's directory.</en></lang>
.PARAMETER WhatIf
  <lang><zh-CN>只计算并打印统计、不写索引文件，供核对用。</zh-CN><en>Compute and print statistics without writing the index, for review.</en></lang>
.EXAMPLE
  <lang><zh-CN>& 'dev\scripts\Update-PortalGateInjectionProofIndex.ps1'</zh-CN><en>& 'dev\scripts\Update-PortalGateInjectionProofIndex.ps1'</en></lang>
.NOTES
  <lang><zh-CN>退出码 0 成功；证据目录缺失、台账格式错误、或四类清单并集与 wired 集不一致时抛错而非静默写出。</zh-CN><en>Exit 0 on success; throws rather than silently writing when evidence is missing, the ledger is malformed, or the four lists do not cover the wired set.</en></lang>
#>
[CmdletBinding()]
param(
    [string] $RepoRoot,
    [switch] $WhatIf
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
}

# <lang><zh-CN>所有路径都相对仓库根，避免脚本工作目录不同导致算到别的仓库去。</zh-CN><en>All paths are relative to the repo root so a different working directory cannot point at another repository.</en></lang>
$proofDir = Join-Path $RepoRoot 'work-zone\dev\evidence\gate-injection-proof'
$suitePath = Join-Path $RepoRoot 'dev\scripts\Invoke-PortalGateSuite.ps1'
$ledgerPath = Join-Path $RepoRoot 'dev\gate-injection-proof-not-applicable.json'
$indexPath = Join-Path $RepoRoot 'dev\gate-injection-proof-index.json'

if (-not (Test-Path -LiteralPath $proofDir)) { throw ('证据目录不存在：' + $proofDir) }
if (-not (Test-Path -LiteralPath $suitePath)) { throw ('门禁编排不存在：' + $suitePath) }

# <lang>
#   <zh-CN>已接入门禁集：全文件解析 `File = '...'`（含 L0/L1/L2 与 $slowGates），与覆盖地图门禁 M1 同口径，
#   不按层块分段——多行 Fixture/Args 会让分段解析脆弱。</zh-CN>
#   <en>Wired gate set: parse every `File = '...'` entry in the whole suite (L0/L1/L2 plus $slowGates), same convention
#   as coverage-map M1; per-layer block splitting is fragile because Fixture/Args blocks span multiple lines.</en>
# </lang>
$suiteText = [System.IO.File]::ReadAllText($suitePath)
$wired = @([regex]::Matches($suiteText, "File\s*=\s*'([^']+)'") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
if ($wired.Count -eq 0) { throw '未在编排器中解析到任何 File 条目。' }

# <lang><zh-CN>每个门禁取"最好"的一条 verdict：proved 优先于 not-detected，其余归 other。历史批次允许多次尝试，口径只认最佳记录。</zh-CN><en>Each gate takes its best verdict: proved outranks not-detected, everything else is other. Historical batches may retry; only the best record counts.</en></lang>
$stats = @{}
foreach ($summaryFile in (Get-ChildItem -LiteralPath $proofDir -Recurse -Filter 'injection-proof-summary.json' -ErrorAction Stop)) {
    $summary = Get-Content -LiteralPath $summaryFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($record in @($summary.results)) {
        $gateName = [string]$record.gate
        if ([string]::IsNullOrWhiteSpace($gateName)) { continue }
        if (-not $stats.ContainsKey($gateName)) {
            $stats[$gateName] = [pscustomobject]@{ Proved = 0; NotDetected = 0; Other = 0 }
        }
        switch ([string]$record.verdict) {
            'proved'       { $stats[$gateName].Proved++ }
            'not-detected' { $stats[$gateName].NotDetected++ }
            default        { $stats[$gateName].Other++ }
        }
    }
}

# <lang><zh-CN>已裁定不适用台账：条目必须是 wired 集合里的真实门禁，否则台账漂移立即报错。</zh-CN><en>Adjudicated-not-applicable ledger: every entry must be a real wired gate, so ledger drift fails immediately.</en></lang>
$notApplicable = @()
$notApplicableBasis = [ordered]@{}
if (Test-Path -LiteralPath $ledgerPath) {
    $ledger = Get-Content -LiteralPath $ledgerPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($entry in @($ledger.entries)) {
        $name = [string]$entry.gate
        if ($wired -notcontains $name) { throw ('notApplicable 台账条目不在已接入门禁集中：' + $name) }
        if ($notApplicable -notcontains $name) { $notApplicable += $name }
        $notApplicableBasis[$name] = [string]$entry.basis
    }
}
$notApplicable = @($notApplicable | Sort-Object)

# <lang><zh-CN>四类清单互斥；台账里已裁定不适用的门禁不进入任何一类，避免它既算"不适用"又算"未触及"。</zh-CN><en>The four lists are mutually exclusive; adjudicated-not-applicable gates enter none of them, so a gate is never both "not applicable" and "never touched".</en></lang>
$proven = @($stats.Keys | Where-Object { $stats[$_].Proved -gt 0 -and $notApplicable -notcontains $_ } | Sort-Object)
$notDetected = @($stats.Keys | Where-Object { $stats[$_].Proved -eq 0 -and $stats[$_].NotDetected -gt 0 -and $notApplicable -notcontains $_ } | Sort-Object)
$other = @($stats.Keys | Where-Object { $stats[$_].Proved -eq 0 -and $stats[$_].NotDetected -eq 0 -and $notApplicable -notcontains $_ } | Sort-Object)
$neverTouched = @($wired | Where-Object { -not $stats.ContainsKey($_) -and $notApplicable -notcontains $_ } | Sort-Object)

# <lang><zh-CN>并集校验：任何 wired 门禁都必须落入四类清单或 notApplicable 之一，且不得重复，防止索引静默漏项。</zh-CN><en>Union check: every wired gate must fall into exactly one of the four lists or notApplicable, with no duplicates, so the index cannot silently omit a gate.</en></lang>
$allNames = @($proven) + @($notDetected) + @($other) + @($neverTouched) + @($notApplicable)
if ($allNames.Count -ne $wired.Count) {
    throw ('覆盖清单并集 ' + $allNames.Count + ' 与已接入门禁 ' + $wired.Count + ' 不一致，拒绝写出索引。')
}
foreach ($name in $allNames) {
    if ($wired -notcontains $name) { throw ('覆盖清单出现未接入编排的门禁名：' + $name) }
}
if (@($allNames | Sort-Object -Unique).Count -ne $allNames.Count) {
    throw '覆盖清单存在重复门禁名，拒绝写出索引。'
}

$index = [pscustomobject][ordered]@{
    version                = 1
    generatedAt            = (Get-Date).ToString('s')
    note                   = '门禁注入法双向证明覆盖索引：proved = 至少有一条「注入即红、还原即绿」的真实记录；notDetected = 夹具存在但注入后门禁仍绿（假证明，需重做注入点）；other = 基线不绿 / 还原后仍红 / 夹具错误；neverTouched = 尚无任何注入夹具；notApplicable = 已评估并裁定不适用（依据见 dev/gate-injection-proof-not-applicable.json），不计入 proved 也不计入 neverTouched。统计口径只认证明记录，不认「夹具里出现过门禁名」。由 dev/scripts/Update-PortalGateInjectionProofIndex.ps1 生成，勿手改。'
    registeredCount        = $wired.Count
    provedCount            = $proven.Count
    notDetectedCount       = $notDetected.Count
    otherCount             = $other.Count
    neverTouchedCount      = $neverTouched.Count
    notApplicableCount     = $notApplicable.Count
    proved                 = $proven
    notDetected            = $notDetected
    other                  = $other
    neverTouched           = $neverTouched
    notApplicable          = $notApplicable
    notApplicableBasis     = $notApplicableBasis
}

Write-Host ('登记 {0} ｜ proved {1} ｜ notDetected {2} ｜ other {3} ｜ neverTouched {4} ｜ notApplicable {5}' -f `
    $index.registeredCount, $index.provedCount, $index.notDetectedCount, $index.otherCount, $index.neverTouchedCount, $index.notApplicableCount)
foreach ($name in $index.notApplicable) { Write-Host ('  notApplicable: ' + $name) }

if ($WhatIf) {
    Write-Host '[WhatIf] 未写出索引。'
    return
}
[System.IO.File]::WriteAllText($indexPath, ($index | ConvertTo-Json -Depth 6), $utf8NoBom)
Write-Host ('索引已写出：' + $indexPath)
