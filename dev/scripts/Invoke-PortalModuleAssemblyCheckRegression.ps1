<#
.SYNOPSIS
<lang>
  <zh-CN>装配态离线校验工具的四步注入回归：基线绿 → 注入后非 0 → 还原后绿，且仓库逐字节洁净。</zh-CN>
  <en>Four-step injection regression for the offline module-assembly check tool: green baseline -> non-zero after injection -> green after restore, with a byte-clean repository.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>按 `C-anp-P47.md` §三.3 的四步双向证明口径，对 `Invoke-PortalModuleAssemblyCheck.ps1` 做可复跑的有效性证明。
  注入方式：临时改写独立部署台账 `src/Portal/Config/module-assembly-state.json`，每次运行工具后用原始字节逐字节还原，
  并在 finally 中保证异常路径也还原（不留半改状态）。四类注入覆盖工具的关键判据：
    I1 未知装配态第三值（V3 封闭词表）；
    I2 开关关闭时的未收敛 CodeFile 条目（V6 事件驱动复核点）；
    I3 台账根节点未知字段（V1 白名单）；
    I4 台账包标识在受信任包中无法解析（V4 登记腐化）。
  任一步不符合期望即判证明不成立并退出非 0。工具本身不修改产品文件，本脚本也只在台账上做可逆注入。
  证明边界：只证明「工具能抓到这四类被注入的缺陷、且还原后仓库洁净」；不证明工具覆盖所有可能缺陷，
  不证明运行期加载、审计已入库或装配态发生过切换。
</lang>
<en>
  A repeatable effectiveness proof for Invoke-PortalModuleAssemblyCheck.ps1 following the four-step two-way proof
  discipline in C-anp-P47.md section 3.3. Injection temporarily rewrites the deployment-side state ledger
  src/Portal/Config/module-assembly-state.json; every run is followed by a byte-for-byte restore of the original bytes,
  and a finally block guarantees restoration even on error (no half-changed state). Four injections cover key predicates:
    I1 an unknown third assembly-mode value (V3 closed vocabulary);
    I2 an unconverged CodeFile entry while the switch is off (V6 event-driven review point);
    I3 an unknown root field in the ledger (V1 whitelist);
    I4 a package identifier that does not resolve to a trusted package (V4 stale registration).
  Any step that violates expectations fails the proof with a non-zero exit. The tool never edits product files, and this
  script performs only reversible injections on the ledger.
  Proof boundary: proves only that "the tool detects these four injected defect classes and the repository is clean
  after restore"; it does not prove coverage of all possible defects, runtime loading, audit persistence, or that any
  assembly mode was switched.
</en>
</lang>

.PARAMETER RepoRoot
<lang>
  <zh-CN>仓库根，默认取本脚本上两级目录。</zh-CN>
  <en>Repository root; defaults to two levels above this script.</en>
</lang>

.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Invoke-PortalModuleAssemblyCheckRegression.ps1'</zh-CN>
  <en>& 'dev\scripts\Invoke-PortalModuleAssemblyCheckRegression.ps1'</en>
</lang>

.NOTES
<lang>
  <zh-CN>证明边界：只证明本脚本 DESCRIPTION 声明的四类注入双向结论与仓库洁净；不证明工具覆盖了全部缺陷类别，
  不证明运行期行为，也不证明审计 SQL 产物已被审阅或执行。</zh-CN>
  <en>Proof boundary: proves only the four two-way injection conclusions and repository cleanliness declared above;
  it does not prove the tool covers every defect class, any runtime behaviour, or that the audit SQL artifact was
  reviewed or executed.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string] $RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>统一 UTF-8 无 BOM，保证注入文本与原始字节严格可比。</zh-CN>
#   <en>Use UTF-8 without BOM consistently so injected text and original bytes remain strictly comparable.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
}
else {
    $RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
}

# <lang>
#   <zh-CN>台账路径与工具路径；两者都必须存在，缺失即显式失败，不静默跳过。</zh-CN>
#   <en>Ledger and tool paths; both must exist, and a missing one fails explicitly rather than being skipped.</en>
# </lang>
$ledgerPath = Join-Path $RepoRoot 'src\Portal\Config\module-assembly-state.json'
$toolPath = Join-Path $RepoRoot 'dev\scripts\Invoke-PortalModuleAssemblyCheck.ps1'
if (-not (Test-Path -LiteralPath $ledgerPath -PathType Leaf)) { throw ('台账缺失：' + $ledgerPath) }
if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) { throw ('工具缺失：' + $toolPath) }

# <lang>
#   <zh-CN>保存原始字节；全程不使用 ConvertTo-Json 重建，避免注入后还原产生字节漂移。</zh-CN>
#   <en>Save the original bytes; never rebuild via ConvertTo-Json so restoration cannot introduce byte drift.</en>
# </lang>
$originalBytes = [System.IO.File]::ReadAllBytes($ledgerPath)
$originalSha = ([BitConverter]::ToString(
    [System.Security.Cryptography.SHA256]::Create().ComputeHash($originalBytes))).Replace('-', '').ToLowerInvariant()

# <lang>
#   <zh-CN>运行工具并只取退出码与文本；固定 RunUtc 使输出可比对，-WhatIf 保证零副作用。</zh-CN>
#   <en>Run the tool and capture only the exit code and text; a fixed RunUtc makes output comparable and -WhatIf keeps it side-effect free.</en>
# </lang>
function Invoke-CheckTool {
    $output = & pwsh -NoProfile -File $toolPath -RepoRoot $RepoRoot -RunUtc '2026-11-07T00:00:00Z' -WhatIf 2>&1
    return [ordered]@{
        Exit = $LASTEXITCODE
        Text = ($output | Out-String)
    }
}

# <lang>
#   <zh-CN>四类注入文本：均使用已登记包标识或明确不可解析的标识，避免依赖外部状态。</zh-CN>
#   <en>Four injection texts: each uses a registered package id or an explicitly unresolvable one, avoiding external state.</en>
# </lang>
$injections = @(
    [ordered]@{
        Id = 'I1'
        Label = '未知装配态第三值'
        Text = '{"schemaVersion":1,"entries":{"HIA.Help":{"assemblyMode":"DebugMode"}}}'
        Expect = 'V3'
    },
    [ordered]@{
        Id = 'I2'
        Label = '开关关闭时未收敛 CodeFile'
        Text = '{"schemaVersion":1,"entries":{"HIA.Help":{"assemblyMode":"CodeFile"}}}'
        Expect = 'V6'
    },
    [ordered]@{
        Id = 'I3'
        Label = '台账根节点未知字段'
        Text = '{"schemaVersion":1,"entries":{},"unexpectedPolicy":true}'
        Expect = 'V1'
    },
    [ordered]@{
        Id = 'I4'
        Label = '台账包标识无法解析为受信任包'
        Text = '{"schemaVersion":1,"entries":{"HIA.NotDeployedPackage":{"assemblyMode":"CodeBehind"}}}'
        Expect = 'V4'
    }
)

$failures = New-Object 'System.Collections.Generic.List[string]'
$results = New-Object 'System.Collections.Generic.List[object]'

try {
    # <lang>
    #   <zh-CN>步骤 1：基线必须绿（exit 0）。基线不绿则后续证明无从建立。</zh-CN>
    #   <en>Step 1: the baseline must be green (exit 0); a red baseline makes any later proof meaningless.</en>
    # </lang>
    $baseline = Invoke-CheckTool
    if ($baseline.Exit -ne 0) {
        $failures.Add(('基线不绿：exit=' + $baseline.Exit))
    }
    if ($baseline.Text -notmatch 'verdict=pass') {
        $failures.Add('基线未产出 verdict=pass')
    }
    $results.Add([pscustomobject][ordered]@{ Step = 'baseline'; Exit = $baseline.Exit; Expected = 0; Proved = ($baseline.Exit -eq 0) })
    Write-Host ('  [1/4] 基线：exit=' + $baseline.Exit + '（期望 0）')

    # <lang>
    #   <zh-CN>步骤 2–3：逐类注入，工具必须非 0 且点名对应判据；仍通过即说明该判据探测不到缺陷（假绿风险）。</zh-CN>
    #   <en>Steps 2-3: for each injection the tool must exit non-zero and name the matching predicate; a pass would mean that predicate cannot detect the defect (false-green risk).</en>
    # </lang>
    foreach ($injection in $injections) {
        # <lang>
        #   <zh-CN>注入文本写入台账；固定 CRLF 与无 BOM 编码，保证与原文件可逐字节比较。</zh-CN>
        #   <en>Write the injection text with fixed CRLF and no BOM so it remains byte-comparable with the original.</en>
        # </lang>
        [System.IO.File]::WriteAllText($ledgerPath, $injection.Text + "`r`n", $utf8)
        $injected = Invoke-CheckTool
        # <lang>
        #   <zh-CN>每次注入后立即还原，避免任一注入残留影响后续用例。</zh-CN>
        #   <en>Restore immediately after each injection so no residue can affect later cases.</en>
        # </lang>
        [System.IO.File]::WriteAllBytes($ledgerPath, $originalBytes)

        $named = $injected.Text.Contains($injection.Expect)
        $proved = ($injected.Exit -ne 0 -and $named)
        if (-not $proved) {
            $failures.Add(($injection.Id + ' 未证明：exit=' + $injected.Exit + ' 点名 ' + $injection.Expect + '=' + $named))
        }
        $results.Add([pscustomobject][ordered]@{
            Step = ($injection.Id + ' ' + $injection.Label)
            Exit = $injected.Exit
            Expected = 'non-zero + ' + $injection.Expect
            Proved = $proved
        })
        Write-Host ('  [2/4] 注入 ' + $injection.Id + '（' + $injection.Label + '）：exit=' + $injected.Exit +
                    '；点名 ' + $injection.Expect + '=' + $named)
    }

    # <lang>
    #   <zh-CN>步骤 4：还原后必须回到绿，且台账字节与原始哈希完全一致。</zh-CN>
    #   <en>Step 4: after restoration the run must be green again and the ledger bytes must match the original hash exactly.</en>
    # </lang>
    $restored = Invoke-CheckTool
    if ($restored.Exit -ne 0) {
        $failures.Add(('还原后未回绿：exit=' + $restored.Exit))
    }
    $finalBytes = [System.IO.File]::ReadAllBytes($ledgerPath)
    $finalSha = ([BitConverter]::ToString(
        [System.Security.Cryptography.SHA256]::Create().ComputeHash($finalBytes))).Replace('-', '').ToLowerInvariant()
    if ($finalSha -ne $originalSha) {
        $failures.Add('台账未逐字节还原（哈希不一致）')
    }
    $results.Add([pscustomobject][ordered]@{ Step = 'restored'; Exit = $restored.Exit; Expected = 0; Proved = ($restored.Exit -eq 0 -and $finalSha -eq $originalSha) })
    Write-Host ('  [4/4] 还原：exit=' + $restored.Exit + '；台账字节洁净=' + ($finalSha -eq $originalSha))
}
finally {
    # <lang>
    #   <zh-CN>finally 兜底：无论正常结束还是异常，都把台账恢复为原始字节，杜绝半改状态。</zh-CN>
    #   <en>Finally guard: restore the original bytes on both normal and exceptional exits, eliminating any half-changed state.</en>
    # </lang>
    [System.IO.File]::WriteAllBytes($ledgerPath, $originalBytes)
}

# <lang>
#   <zh-CN>结论：五步（基线 + 四注入 + 还原）全成立且字节洁净才算证明成立。</zh-CN>
#   <en>Conclusion: the proof holds only when all five steps (baseline, four injections, restore) pass and bytes are clean.</en>
# </lang>
$allProved = ($failures.Count -eq 0)
Write-Host ('  verdict=' + $(if ($allProved) { 'proved' } else { 'not-proved' }) + '；步骤 ' + $results.Count + ' 个；失败 ' + $failures.Count + ' 项')
foreach ($failure in $failures) { Write-Host ('  [FAIL] ' + $failure) }

if (-not $allProved) { exit 1 }
exit 0
