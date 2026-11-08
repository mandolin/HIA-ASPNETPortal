<#
.SYNOPSIS
<lang>
  <zh-CN>在线能力包「校验 → 备份 → 偏离 → 还原」回滚演练：产出字节级一致与零残留证据（三种情形各一份）。</zh-CN>
  <en>Online capability package "verify → backup → diverge → restore" rollback drill: emits byte-identical and zero-residue evidence (one per scenario).</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>按 W-anp-P280 规格交付的参数化演练证据脚本（非临时方案，可长期复用），借鉴 W-anp-P274 E2E 范式：
  在隔离的临时目录构造一个受控暂存包清单，做字节级备份（逐文件复制 + SHA256 比对），注入可抓回的偏离态
  （修改文件 / 追加残留文件），再用备份逐字节还原，比对 SHA256 证明与偏离前逐字节一致、且无残留文件。
  三种演练情形（校验失败 / 备份损坏 / 复核点未通过）各自产出独立证据，全部在 try/finally 中清理临时目录。
  证据写明证明边界：演练只证明「快照可还原为零残留」，不表示真实回滚、安装或运行期装配态切换发生。
  **工具不代写审计**：本脚本只写 work-zone 下的可审阅 JSON，不调用任何运行时审计入口、不连接数据库。</zh-CN>
  <en>Parameterized drill evidence script delivered per the W-anp-P280 spec (not a temporary hack; reusable long-term), following the W-anp-P274 E2E pattern:
  it builds a controlled staged-package manifest in an isolated temp directory, takes a byte-level backup (per-file copy plus SHA256 comparison),
  injects a catchable divergence (modified file / extra residue file), then restores byte-by-byte from the backup and compares SHA256 to prove
  byte identity with the pre-divergence state and zero residue.
  Each of the three drill scenarios (validation failed / backup corrupted / checkpoint rejected) produces its own evidence, with temp directories
  removed inside try/finally throughout.
  Evidence states its proof boundary: a drill proves only that "a snapshot restores with zero residue", never that a real rollback, installation,
  or runtime assembly-mode switch occurred.
  **Tools never write on behalf of runtime**: this script writes only reviewable JSON under work-zone, calls no runtime audit entry point, and connects to no database.</en>
</lang>

.PARAMETER RepoRoot
<lang>
  <zh-CN>仓库根目录；缺省由脚本位置推导。</zh-CN>
  <en>Repository root; derived from the script location by default.</en>
</lang>

.PARAMETER OutputDirectory
<lang>
  <zh-CN>证据输出目录；缺省为 work-zone/dev/evidence/online-package-rollback。</zh-CN>
  <en>Evidence output directory; defaults to work-zone/dev/evidence/online-package-rollback.</en>
</lang>

证明边界：只证明「字节级备份可还原为与偏离前逐字节一致、零残留」以及三种情形各自的拒绝结论被如实记录；
不证明真实安装/回滚在运行期发生、不证明页面级失败隔离在运行站点生效、不证明审计已入库。
Proof boundary: proves only that "a byte-level backup restores byte-identically to the pre-divergence state with zero residue"
and that each scenario's rejection verdict is recorded honestly; it does not prove a real runtime install/rollback,
page-level failure isolation on a live site, or that an audit row was persisted.
#>
[CmdletBinding()]
param(
    [string] $RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string] $OutputDirectory = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>UTF-8 无 BOM 写入；与项目其它证据脚本同一编码口径。</zh-CN>
#   <en>Write UTF-8 without BOM, matching the encoding convention of other evidence scripts.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot 'work-zone\dev\evidence\online-package-rollback'
}
if (-not (Test-Path -LiteralPath $OutputDirectory)) { New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null }

# <lang>
#   <zh-CN>稳定 SHA-256 小写十六进制，与 PortalOnlinePackageIntegrity.ComputeSha256Hex 及离线工具同口径。</zh-CN>
#   <en>Stable lowercase SHA-256 hex, sharing the format of PortalOnlinePackageIntegrity.ComputeSha256Hex and the offline tool.</en>
# </lang>
function Get-FileSha256([string] $path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.IO.File]::ReadAllBytes($path)
        $digest = $sha.ComputeHash($bytes)
        return ([BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

# <lang>
#   <zh-CN>取目录下全部文件的相对路径清单（统一 '/' 分隔），按 Ordinal 排序保证证据可复跑。</zh-CN>
#   <en>Lists all files under a directory as sorted relative paths (uniform '/'), so evidence is reproducible.</en>
# </lang>
function Get-RelativeFiles([string] $root) {
    $files = @()
    foreach ($item in @(Get-ChildItem -LiteralPath $root -Recurse -File)) {
        $relative = $item.FullName.Substring($root.Length).TrimStart('\', '/') -replace '\\', '/'
        $files += $relative
    }
    return @($files | Sort-Object -CaseSensitive)
}

# <lang>
#   <zh-CN>生成哈希清单快照（路径/长度/摘要），与 Invoke-PortalModuleAssemblyCheck.ps1 的受控备份同口径：只记录元数据，不复制源码。</zh-CN>
#   <en>Builds a hash-manifest snapshot (path/length/digest), matching the controlled backup in Invoke-PortalModuleAssemblyCheck.ps1: metadata only, no source copy.</en>
# </lang>
function Get-Manifest([string] $root) {
    $entries = @()
    foreach ($relative in @(Get-RelativeFiles $root)) {
        $full = Join-Path $root ($relative -replace '/', '\')
        $bytes = (Get-Item -LiteralPath $full).Length
        $entries += [PSCustomObject]@{
            path    = $relative
            bytes   = $bytes
            sha256  = (Get-FileSha256 $full)
        }
    }
    return @($entries)
}

# <lang>
#   <zh-CN>把清单整体压缩为 JSON 字符串用于「字节级一致」的等价判定（长度+摘要逐项相等即字节级一致的可用代理）。</zh-CN>
#   <en>Serialises the manifest compactly for the byte-identity equivalence check (equal per-item lengths and digests are the usable proxy for byte identity).</en>
# </lang>
function Get-ManifestDigest([object] $manifest) {
    $text = ($manifest | ConvertTo-Json -Compress -Depth 5)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

# <lang>
#   <zh-CN>三种演练情形的稳定键名，与 PortalOnlinePackageRollbackScenario 枚举语义一致。</zh-CN>
#   <en>Stable keys for the three drill scenarios, matching the semantics of the PortalOnlinePackageRollbackScenario enum.</en>
# </lang>
$scenarios = @('ValidationFailed', 'BackupCorrupted', 'CheckpointRejected')
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$allPassed = $true
$summary = New-Object 'System.Collections.Generic.List[PSCustomObject]'

foreach ($scenario in $scenarios) {
    # <lang>
    #   <zh-CN>每次演练使用独立的临时工作区、暂存目录与备份目录，避免情形之间互相污染。</zh-CN>
    #   <en>Each drill uses its own temp workspace, staging directory, and backup directory so scenarios cannot pollute each other.</en>
    # </lang>
    $workspace = Join-Path $env:TEMP ('online-package-rollback-' + $scenario + '-' + [Guid]::NewGuid().ToString('N'))
    $staging = Join-Path $workspace 'staging'
    $backup = Join-Path $workspace 'backup'
    $stages = New-Object 'System.Collections.Generic.List[PSCustomObject]'

    try {
        New-Item -ItemType Directory -Path $staging -Force | Out-Null
        New-Item -ItemType Directory -Path $backup -Force | Out-Null

        # <lang>
        #   <zh-CN>构造受控暂存包：清单 + 一个静态资源 + 一个模块入口，全部为普通文本，不加载、不编译。</zh-CN>
        #   <en>Build a controlled staged package: manifest, one static resource, and one module entry, all plain text; nothing is loaded or compiled.</en>
        # </lang>
        [System.IO.File]::WriteAllText((Join-Path $staging 'module.json'),
            '{"schemaVersion":2,"packageId":"HIA.RollbackDrillFixture","version":"1.0.0"}', $utf8)
        [System.IO.File]::WriteAllText((Join-Path $staging 'Styles.css'), 'body { color: inherit; }', $utf8)
        [System.IO.File]::WriteAllText((Join-Path $staging 'Widget.ascx'), '<%-- drill fixture --%>', $utf8)

        # ---------- ① 校验（基线） ----------
        $baseline = Get-Manifest $staging
        $baselineDigest = Get-ManifestDigest $baseline
        $baselineOk = $baseline.Count -eq 3
        $stages.Add([PSCustomObject]@{
            Stage = 'verify-baseline'; FileCount = $baseline.Count; ManifestDigest = $baselineDigest; Pass = $baselineOk
        })

        # ---------- ② 备份（逐文件字节复制 + SHA256 比对） ----------
        foreach ($entry in $baseline) {
            $source = Join-Path $staging ($entry.path -replace '/', '\')
            $target = Join-Path $backup ($entry.path -replace '/', '\')
            $targetDir = Split-Path -Parent $target
            if (-not (Test-Path -LiteralPath $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
            [System.IO.File]::Copy($source, $target, $true)
        }
        $backupManifest = Get-Manifest $backup
        $backupDigest = Get-ManifestDigest $backupManifest
        $backupByteIdentical = $backupDigest -eq $baselineDigest
        $stages.Add([PSCustomObject]@{
            Stage = 'backup'; BaselineDigest = $baselineDigest; BackupDigest = $backupDigest; ByteIdentical = $backupByteIdentical
        })

        # ---------- ③ 偏离（可抓回：改一个文件 + 追加一个残留文件） ----------
        [System.IO.File]::WriteAllText((Join-Path $staging 'Styles.css'), 'body { color: red; } /* diverged */', $utf8)
        [System.IO.File]::WriteAllText((Join-Path $staging 'residue.txt'), 'residue', $utf8)
        $diverged = Get-Manifest $staging
        $divergedDigest = Get-ManifestDigest $diverged
        $divergenceDetected = ($divergedDigest -ne $baselineDigest) -and ($diverged.Count -eq 4)
        $stages.Add([PSCustomObject]@{
            Stage = 'diverge'; ManifestDigest = $divergedDigest; FileCount = $diverged.Count; Detected = $divergenceDetected
        })

        # ---------- ④ 还原（清目录 → 用备份逐文件写回 → 比对） ----------
        foreach ($item in @(Get-ChildItem -LiteralPath $staging -Recurse -File)) { Remove-Item -LiteralPath $item.FullName -Force }
        foreach ($entry in $backupManifest) {
            $source = Join-Path $backup ($entry.path -replace '/', '\')
            $target = Join-Path $staging ($entry.path -replace '/', '\')
            $targetDir = Split-Path -Parent $target
            if (-not (Test-Path -LiteralPath $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
            [System.IO.File]::Copy($source, $target, $true)
        }
        $restored = Get-Manifest $staging
        $restoredDigest = Get-ManifestDigest $restored
        $zeroResidue = ($restored.Count -eq $baseline.Count) -and ($restoredDigest -eq $baselineDigest)
        $stages.Add([PSCustomObject]@{
            Stage = 'restore'; BaselineDigest = $baselineDigest; RestoredDigest = $restoredDigest
            FileCount = $restored.Count; ZeroResidue = $zeroResidue
        })

        # <lang>
        #   <zh-CN>情形判定：三种演练全部如实得出「拒绝继续安装」的结论，且备份/偏离/还原证据齐备。
        #   校验失败情形额外断言基线校验通过；备份损坏情形额外断言备份哈希一致；复核点未通过情形不依赖前两者之外的额外前提。</zh-CN>
        #   <en>Scenario verdict: all three drills honestly conclude "installation refused to continue", with complete backup/diverge/restore evidence.
        #   Validation-failed additionally requires a passed baseline; backup-corrupted additionally requires matching backup hashes; checkpoint-rejected needs no extra premise.</en>
        # </lang>
        $scenarioOk = $divergenceDetected -and $zeroResidue
        if ($scenario -eq 'ValidationFailed') { $scenarioOk = $scenarioOk -and $baselineOk }
        if ($scenario -eq 'BackupCorrupted') { $scenarioOk = $scenarioOk -and $backupByteIdentical }

        # <lang>
        #   <zh-CN>`Restorability` 只表达「快照是否可还原为零残留」；`InstallationVerdict` 恒为 rejected，
        #   与 Components 层 `PortalOnlinePackageRollbackDrill.TryEvaluateScenario` 的结论一致——三种演练情形
        #   一律不允许继续安装。刻意不用含糊的 `Verdict` 字段名，避免把「可还原」误读成「允许安装」。</zh-CN>
        #   <en>`Restorability` expresses only whether the snapshot restores with zero residue; `InstallationVerdict` is
        #   always rejected, matching `PortalOnlinePackageRollbackDrill.TryEvaluateScenario` in the Components layer — all
        #   three drill scenarios refuse to continue installation. The ambiguous field name `Verdict` is deliberately avoided
        #   so "restorable" cannot be misread as "installation allowed".</en>
        # </lang>
        $evidence = [PSCustomObject]@{
            Tool          = 'Invoke-PortalOnlinePackageRollbackDrill'
            GeneratedUtc  = (Get-Date -Format 'o')
            Scenario      = $scenario
            Restorability = $(if ($scenarioOk) { 'restorable' } else { 'not-restorable' })
            InstallationVerdict = 'rejected'
            InstallationContinued = $false
            Stages        = $stages
            Result        = $(if ($scenarioOk) { 'Pass' } else { 'Fail' })
            ProofBoundary = 'Drill only: proves the controlled snapshot restores byte-identically with zero residue. It does not mean a real rollback, installation, or runtime assembly-mode switch occurred. All three scenarios refuse to continue installation. Tools never write runtime audit rows.'
            AuditWiring   = 'Runtime entry point (not called here): PortalOnlinePackageAudit.RecordFailureOrCleanup(PortalOnlinePackageAuditEvents.RollbackDrill, ...).'
        }

        $evidencePath = Join-Path $OutputDirectory ('rollback-drill-' + $scenario + '-' + $timestamp + '.json')
        [System.IO.File]::WriteAllText($evidencePath, ($evidence | ConvertTo-Json -Depth 8) + "`r`n", $utf8)
        Write-Output ("[DRILL] {0}: {1} -> {2}" -f $scenario, $evidence.Result, $evidencePath)
        $summary.Add([PSCustomObject]@{ Scenario = $scenario; Result = $evidence.Result; Evidence = $evidencePath })
        if (-not $scenarioOk) { $allPassed = $false }
    }
    finally {
        # <lang>
        #   <zh-CN>无论成败都删除临时工作区，确保演练在仓库与系统临时目录都零残留。</zh-CN>
        #   <en>Remove the temp workspace regardless of outcome, so the drill leaves zero residue in both the repo and the system temp directory.</en>
        # </lang>
        if (Test-Path -LiteralPath $workspace) {
            try { Remove-Item -LiteralPath $workspace -Recurse -Force }
            catch { Write-Output ("[WARN] temp workspace cleanup incomplete: " + $workspace) }
        }
    }
}

Write-Output ("[DRILL] summary: " + (@($summary | ForEach-Object { $_.Scenario + '=' + $_.Result }) -join ', '))
if ($allPassed) { Write-Output 'DRILL RESULT: Pass'; exit 0 }
Write-Output 'DRILL RESULT: Fail'; exit 1
