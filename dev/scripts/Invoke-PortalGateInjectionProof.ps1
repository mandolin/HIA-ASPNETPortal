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

    # <lang>
    #   <zh-CN>node 不一定在 PATH 中（编排脚本本身也是硬编码全路径）。按"先 PATH、后常见安装目录"解析，
    #   与 Invoke-PortalGateSuite.ps1 的做法保持一致，避免脚手架因找不到 node 而把所有 mjs 门禁误判为失败。</zh-CN>
    #   <en>Node is not necessarily on PATH (the orchestration script itself hard-codes a full path). Resolve it as
    #   "PATH first, then common install directories", matching Invoke-PortalGateSuite.ps1, so the harness never
    #   misreports every mjs gate as failed merely because node could not be found.</en>
    # </lang>
    $nodeExe = 'node'
    if ($GateFile -like '*.mjs') {
        $nodeOnPath = Get-Command 'node' -ErrorAction SilentlyContinue
        if ($nodeOnPath) {
            $nodeExe = $nodeOnPath.Source
        }
        else {
            foreach ($candidate in @("$env:ProgramFiles\nodejs\node.exe", "${env:ProgramFiles(x86)}\nodejs\node.exe")) {
                if (Test-Path -LiteralPath $candidate) { $nodeExe = $candidate; break }
            }
        }
    }

    $runner = if ($GateFile -like '*.mjs') { $nodeExe } else { 'pwsh' }

    # <lang>
    #   <zh-CN>夹具参数支持 %ENV% 占位（如 %USERPROFILE%），由脚手架展开，避免把本机绝对路径写进版本库。</zh-CN>
    #   <en>Fixture arguments support %ENV% placeholders (for example %USERPROFILE%), expanded by the harness so machine-specific absolute paths never enter the repository.</en>
    # </lang>
    $expandedArgs = @()
    foreach ($argument in @($GateArgs)) {
        $expandedArgs += [System.Environment]::ExpandEnvironmentVariables([string]$argument)
    }

    $argumentList = if ($GateFile -like '*.mjs') {
        @($gatePath) + $expandedArgs
    }
    else {
        @('-NoProfile', '-NoLogo', '-File', $gatePath) + $expandedArgs
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

    # <lang>
    #   <zh-CN>create 模式用于"制造一个原本不存在的文件即可触发"的门禁（如日志目录中出现不受管文件）；此时没有原文可读。</zh-CN>
    #   <en>The create mode serves gates triggered by producing a file that did not exist (for example an unmanaged file appearing in the log directory); there is no original text to read.</en>
    # </lang>
    if ($Mode -eq 'create') {
        if (Test-Path -LiteralPath $fullPath) {
            throw ("create patch target already exists: $RelativePath")
        }
        $parent = Split-Path -Parent $fullPath
        if (-not (Test-Path -LiteralPath $parent)) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }
        $createContent = if ($null -eq $Content) { '' } else { ($Content -replace "`r`n", "`n") -replace "`n", "`r`n" }
        [System.IO.File]::WriteAllText($fullPath, $createContent, $utf8NoBom)
        return $null
    }

    # <lang>
    #   <zh-CN>remove 模式用于"受检文件缺失即失败"的门禁；原文已在调用方备份，还原时按备份回写。</zh-CN>
    #   <en>The remove mode serves gates that fail when a checked file is absent; the caller already backed up the original and restores it by writing the backup back.</en>
    # </lang>
    if ($Mode -eq 'remove') {
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw ("remove patch target not found: $RelativePath")
        }
        $existing = [System.IO.File]::ReadAllText($fullPath)
        Remove-Item -LiteralPath $fullPath -Force
        return $existing
    }

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

    # <lang>
    #   <zh-CN>运行期门禁（mjs 语义/证据类）要求特定环境变量（如 PORTAL_PLAYWRIGHT_MODULE、PORTAL_P82_PHASE）。
    #   由夹具声明，证明结束后按原值还原，避免把调用方环境永久改掉。</zh-CN>
    #   <en>Runtime gates (the mjs semantic/evidence family) require specific environment variables such as
    #   PORTAL_PLAYWRIGHT_MODULE and PORTAL_P82_PHASE. Fixtures declare them, and the harness restores the
    #   original values afterwards so the caller's environment is never permanently changed.</en>
    # </lang>
    $fixtureEnv = if ($fixture.PSObject.Properties.Name -contains 'env') { $fixture.env } else { $null }
    $savedEnv = @{}
    if ($fixtureEnv) {
        foreach ($property in $fixtureEnv.PSObject.Properties) {
            $savedEnv[$property.Name] = [System.Environment]::GetEnvironmentVariable($property.Name)
            $expandedValue = [System.Environment]::ExpandEnvironmentVariables([string]$property.Value)
            # <lang>
            #   <zh-CN>额外支持 %REPO% 占位符展开为仓库根，使夹具可以引用仓库内但不入库的开发期依赖
            #   （如 temp\node_modules\playwright），而不必把本机绝对路径写进版本库。</zh-CN>
            #   <en>Also support a %REPO% placeholder that expands to the repository root, so a fixture can reference
            #   in-repo but uncommitted development dependencies (such as temp\node_modules\playwright) without
            #   hard-coding a machine-specific absolute path in the repository.</en>
            # </lang>
            $expandedValue = $expandedValue -replace '%REPO%', $repositoryRoot
            [System.Environment]::SetEnvironmentVariable($property.Name, $expandedValue)
        }
    }

    try {
        foreach ($patch in $patches) {
            $targetFullPath = Join-Path $repositoryRoot $patch.file
            $backups[$patch.file] = if (Test-Path -LiteralPath $targetFullPath) {
                [System.IO.File]::ReadAllText($targetFullPath)
            }
            else {
                $null
            }
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
        foreach ($name in $savedEnv.Keys) {
            [System.Environment]::SetEnvironmentVariable($name, $savedEnv[$name])
        }

        foreach ($file in $backups.Keys) {
            $fullPath = Join-Path $repositoryRoot $file
            if ($null -eq $backups[$file]) {
                # <lang>
                #   <zh-CN>备份为空说明该文件是本次 create 出来的，还原即删除，避免把探针文件留在仓库里。</zh-CN>
                #   <en>An empty backup means the file was produced by this create patch, so restoring means deleting it and leaving no probe behind.</en>
                # </lang>
                if (Test-Path -LiteralPath $fullPath) {
                    Remove-Item -LiteralPath $fullPath -Force
                }
            }
            elseif ($null -ne $backups[$file]) {
                # <lang>
                #   <zh-CN>有原文就无条件回写，不能再以 Test-Path 为前提：remove 模式删除文件后目标已不存在，
                #   若沿用"存在才回写"的条件，被删的文件永远不会被还原（曾因此把 Web.config 留在删除状态）。</zh-CN>
                #   <en>Write the backup back unconditionally when it exists; existence must not be a precondition, because
                #   a file deleted by the remove mode no longer exists and an "only if present" guard would never restore it
                #   (this once left Web.config deleted).</en>
                # </lang>
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
