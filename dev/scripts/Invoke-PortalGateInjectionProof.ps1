<#
.SYNOPSIS
    Prove that each gate can actually detect the defect class it claims to cover.
.DESCRIPTION
    双向证明（injection proof）对每个门禁执行四步：
        1. 基线运行   —— 期望 exit 0（当前代码是绿的，否则该门禁的证明无从建立）；
        2. 施加注入   —— 按夹具给出的补丁写入缺陷；
        3. 注入后运行 —— 期望 exit 非 0（门禁必须发现被注入的缺陷）；
        4. 还原后运行 —— 期望 exit 0（证明失败确由注入引起，而非环境或残留）。
    任一环节不符合期望即记为证明失败：第 3 步仍通过说明该门禁**探测不到这类缺陷**（假绿风险），
    第 1 或第 4 步失败说明基线不干净或还原不完整，都不能算完成证明。

    夹具为 JSON 数组，每项形如：
        {
          "gate":  "Test-PortalLegacyCssCompatibility.ps1",
          "note":  "注入 var() 用法",
          "args":  [],
          "patches": [ { "file": "src/.../Default.css", "mode": "append", "content": "\n.x{color:var(--z)}\n" } ]
        }
    mode 支持 append（追加）与 replace（old -> content，需同时给出 "old"）。

    还原一律以内存中的原始内容回写，**不使用 git checkout**，避免误改工作树中其它未提交变更。
    每次运行都把每个门禁的 stdout/stderr 落到证据目录，便于回溯"红"的真实原因。
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $FixturePath,

    [string[]] $GateFilter,

    [string] $EvidenceDir = (Join-Path 'work-zone/dev/evidence/gate-injection-proof' (Get-Date -Format 'yyyyMMddTHHmmss')),

    [switch] $SkipBaseline
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$gateDirectory = Join-Path $repositoryRoot 'dev/scripts'

# <lang>
#   <zh-CN>所有回写一律使用无 BOM 的 UTF8：.NET Framework 的 Encoding.UTF8 默认会写入 BOM，
#   而 ReadAllText 会剥离 BOM，于是一轮"备份-还原"就会把原本无 BOM 的仓库文件逐个污染成带 BOM。</zh-CN>
#   <en>Every write uses BOM-less UTF8: the .NET Framework Encoding.UTF8 emits a BOM by default while
#   ReadAllText strips it, so a single backup-restore round would pollute every originally BOM-less repository file.</en>
# </lang>
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

# <lang>
#   <zh-CN>门禁运行统一走 pwsh 并固定无 Profile，避免调用方环境变量改变门禁判定结果。</zh-CN>
#   <en>Run every gate through pwsh with no profile so the caller's environment cannot change a gate's verdict.</en>
# </lang>
function Invoke-GateRun {
    param(
        [Parameter(Mandatory = $true)][string] $GateFile,
        [string[]] $GateArgs,
        [Parameter(Mandatory = $true)][string] $LogPath
    )

    $gatePath = Join-Path $gateDirectory $GateFile
    if (-not (Test-Path -LiteralPath $gatePath)) {
        throw ("gate script not found: $GateFile")
    }

    $runner = if ($GateFile -like '*.mjs') { 'node' } else { 'pwsh' }
    $argumentList = if ($runner -eq 'node') {
        @($gatePath) + @($GateArgs)
    }
    else {
        @('-NoProfile', '-NoLogo', '-File', $gatePath) + @($GateArgs)
    }

    $output = & $runner @argumentList 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | Out-String).Trim()
    [System.IO.File]::WriteAllText($LogPath, $text, $utf8NoBom)
    return [PSCustomObject]@{
        ExitCode = $exitCode
        Output   = $text
    }
}

# <lang>
#   <zh-CN>按 mode 施加补丁；replace 模式必须同时提供 old，且 old 必须在文件中唯一命中，否则宁可抛错也不要静默改错位置。</zh-CN>
#   <en>Apply a patch by mode; replace mode must also supply old, and old must match exactly once, otherwise fail loudly rather than silently patch the wrong location.</en>
# </lang>
function Apply-Patch {
    param(
        [Parameter(Mandatory = $true)][string] $RelativePath,
        [Parameter(Mandatory = $true)][string] $Mode,
        [string] $Content,
        [string] $Old
    )

    $fullPath = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ("patch target not found: $RelativePath")
    }

    $original = [System.IO.File]::ReadAllText($fullPath)

    # <lang>
    #   <zh-CN>夹具按 LF 书写，而仓库文本可能是 CRLF。匹配与回写都以文件自身的换行风格为准，否则"明明存在却匹配 0 次"。</zh-CN>
    #   <en>Fixtures are authored with LF while repository text may use CRLF. Match and write using the file's own newline style, otherwise an existing anchor reports zero matches.</en>
    # </lang>
    $newline = if ($original.Contains("`r`n")) { "`r`n" } else { "`n" }
    $normalizedContent = if ($null -eq $Content) { $Content } else { ($Content -replace "`r`n", "`n") -replace "`n", $newline }
    $normalizedOld = if ([string]::IsNullOrEmpty($Old)) { $Old } else { ($Old -replace "`r`n", "`n") -replace "`n", $newline }

    switch ($Mode) {
        'append' {
            [System.IO.File]::WriteAllText($fullPath, $original + $normalizedContent, $utf8NoBom)
        }
        'replace' {
            if ([string]::IsNullOrEmpty($normalizedOld)) { throw ("replace patch for $RelativePath is missing 'old'") }
            $occurrences = ([regex]::Matches($original, [regex]::Escape($normalizedOld))).Count
            if ($occurrences -ne 1) {
                throw ("replace patch for $RelativePath matched $occurrences times; exactly one match is required")
            }
            [System.IO.File]::WriteAllText($fullPath, $original.Replace($normalizedOld, $normalizedContent), $utf8NoBom)
        }
        default { throw ("unsupported patch mode: $Mode") }
    }
    return $original
}

$fixtures = @(Get-Content -LiteralPath $FixturePath -Raw -Encoding UTF8 | ConvertFrom-Json)
if ($GateFilter) {
    $fixtures = @($fixtures | Where-Object { $GateFilter -contains $_.gate })
}

if (-not (Test-Path -LiteralPath $EvidenceDir)) {
    New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
}

$results = New-Object System.Collections.Generic.List[object]
$proofFailures = 0

foreach ($fixture in $fixtures) {
    $gate = $fixture.gate
    $note = if ($fixture.PSObject.Properties.Name -contains 'note') { $fixture.note } else { '' }
    $gateArgs = if ($fixture.PSObject.Properties.Name -contains 'args') { @($fixture.args) } else { @() }
    $patches = @($fixture.patches)

    $safeName = ($gate -replace '\.ps1$', '') -replace '[^A-Za-z0-9_.-]', '_'
    Write-Host ('--- ' + $gate + ' :: ' + $note)

    # <lang>
    #   <zh-CN>无论中途抛出与否，都必须用内存中的原文回写，保证工作树不会残留注入内容。</zh-CN>
    #   <en>No matter what is thrown midway, restore from the in-memory originals so no injected content survives in the working tree.</en>
    # </lang>
    $backups = @{}
    $verdict = 'proved'
    $detail = ''

    try {
        foreach ($patch in $patches) {
            $backups[$patch.file] = [System.IO.File]::ReadAllText((Join-Path $repositoryRoot $patch.file))
        }

        if (-not $SkipBaseline) {
            $baseline = Invoke-GateRun -GateFile $gate -GateArgs $gateArgs -LogPath (Join-Path $EvidenceDir ($safeName + '.1-baseline.log'))
            if ($baseline.ExitCode -ne 0) {
                $verdict = 'baseline-failed'
                $detail = '基线门禁未通过，无法建立注入证明；见 1-baseline.log'
                throw $detail
            }
        }

        foreach ($patch in $patches) {
            $patchOld = $null
            $patchContent = $null
            if ($patch.PSObject.Properties.Name -contains 'old') { $patchOld = $patch.old }
            if ($patch.PSObject.Properties.Name -contains 'content') { $patchContent = $patch.content }
            Apply-Patch -RelativePath $patch.file -Mode $patch.mode -Content $patchContent -Old $patchOld
        }

        $injected = Invoke-GateRun -GateFile $gate -GateArgs $gateArgs -LogPath (Join-Path $EvidenceDir ($safeName + '.2-injected.log'))
        if ($injected.ExitCode -eq 0) {
            $verdict = 'not-detected'
            $detail = '注入缺陷后门禁仍通过（exit 0）——该门禁探测不到此类缺陷，属假绿风险'
            throw $detail
        }
    }
    catch {
        if (-not $detail) { $detail = $_.Exception.Message }
        if ($verdict -eq 'proved') { $verdict = 'harness-error' }
    }
    finally {
        foreach ($file in $backups.Keys) {
            $fullPath = Join-Path $repositoryRoot $file
            if (Test-Path -LiteralPath $fullPath) {
                [System.IO.File]::WriteAllText($fullPath, $backups[$file], $utf8NoBom)
            }
        }

        if ($verdict -ne 'baseline-failed' -and $verdict -ne 'harness-error') {
            $restored = Invoke-GateRun -GateFile $gate -GateArgs $gateArgs -LogPath (Join-Path $EvidenceDir ($safeName + '.3-restored.log'))
            if ($restored.ExitCode -ne 0) {
                $verdict = 'restore-failed'
                $detail = '还原后门禁仍未通过，说明失败并非仅由注入引起（环境残留或还原不完整）'
            }
        }
    }

    if ($verdict -ne 'proved') { $proofFailures++ }
    Write-Host ('    => ' + $verdict + $(if ($detail) { ' :: ' + $detail } else { '' }))
    $results.Add([PSCustomObject]@{
            gate    = $gate
            note    = $note
            verdict = $verdict
            detail  = $detail
        })
}

$summary = [PSCustomObject]@{
    generatedAt  = (Get-Date).ToString('s')
    totalGates   = $results.Count
    proved       = @($results | Where-Object { $_.verdict -eq 'proved' }).Count
    notDetected  = @($results | Where-Object { $_.verdict -eq 'not-detected' }).Count
    otherIssues  = @($results | Where-Object { $_.verdict -notin @('proved', 'not-detected') }).Count
    results      = $results
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $EvidenceDir 'injection-proof-summary.json') -Encoding UTF8

$lines = @()
$lines += '# 门禁注入法双向证明'
$lines += ''
$lines += ('- 生成时间：' + $summary.generatedAt)
$lines += ('- 门禁数：' + $summary.totalGates + '；证明成立：' + $summary.proved + '；未探测到注入（假绿风险）：' + $summary.notDetected + '；其它问题：' + $summary.otherIssues)
$lines += ''
$lines += '| 门禁 | 注入内容 | 结论 |'
$lines += '| --- | --- | --- |'
foreach ($r in $results) {
    $status = switch ($r.verdict) {
        'proved' { '✅ 双向证明成立（注入即红、还原即绿）' }
        'not-detected' { '⚠️ 假绿风险：注入后仍通过' }
        'baseline-failed' { '❌ 基线不绿' }
        'restore-failed' { '❌ 还原后仍红' }
        default { '❌ 夹具或脚手架错误' }
    }
    $lines += ('| ' + $r.gate + ' | ' + $r.note + ' | ' + $status + $(if ($r.detail) { ' — ' + $r.detail } else { '' }) + ' |')
}
$lines | Set-Content -LiteralPath (Join-Path $EvidenceDir 'injection-proof-report.md') -Encoding UTF8

Write-Host ''
Write-Host ('注入证明完成：门禁 ' + $summary.totalGates + '，成立 ' + $summary.proved + '，未探测到 ' + $summary.notDetected + '，其它 ' + $summary.otherIssues)
Write-Host ('证据目录：' + $EvidenceDir)

if ($summary.notDetected -gt 0 -or $summary.otherIssues -gt 0) { exit 1 }
exit 0
