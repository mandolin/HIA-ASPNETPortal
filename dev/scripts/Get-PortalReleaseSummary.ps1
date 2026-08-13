<#
.SYNOPSIS
    Builds a read-only release summary from release and evidence artifacts.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本汇总发布包 manifest、运维证据包、文档化证据包和当前 Git 状态，用于形成发布说明与内部 release ledger 的输入。它不打 tag、不创建分支、不发布文件、不连接 IIS、不连接数据库，也不读取外置敏感配置。</zh-CN>
      <en>This script summarizes the release manifest, operations evidence, documentation evidence, and current Git state for release notes and the internal release ledger. It does not tag, create branches, publish files, connect to IIS, connect to databases, or read external secret configuration.</en>
    </lang>
#>
[CmdletBinding()]
param(
    [string]$Version = '0.13.1',

    [string]$ReleaseName = 'P13 productization evidence baseline',

    [string]$ReleaseManifestPath,

    [string]$OperationsEvidencePath,

    [string]$DocumentationEvidencePath,

    [string]$OutputJson,

    [string]$OutputMarkdown,

    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入摘要文件，并只创建调用方指定的父目录。</zh-CN>
#   <en>Write summary files as UTF-8 without a BOM, creating only the parent directory selected by the caller.</en>
# </lang>
function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>在仓库根目录只读执行 Git 元数据命令；失败时返回空值，不把命令错误升级为敏感输出。</zh-CN>
#   <en>Run read-only Git metadata commands at the repository root; return null on failure without exposing command errors.</en>
# </lang>
function Get-GitValue {
    param([string[]]$Arguments)

    try {
        $output = & git -C $repoRoot @Arguments 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        return (($output -join "`n").Trim())
    }
    catch {
        return $null
    }
}

# <lang>
#   <zh-CN>解析显式或默认 evidence 文件，默认选择包含目标文件的最新目录，不把本地运行路径写死。</zh-CN>
#   <en>Resolve an explicit or default evidence file, selecting the newest directory containing the file without pinning one local run path.</en>
# </lang>
function Resolve-EvidenceFile {
    param(
        [string]$InputPath,
        [string]$DefaultRoot,
        [string]$FileName
    )

    if (-not [string]::IsNullOrWhiteSpace($InputPath)) {
        $candidate = [System.IO.Path]::GetFullPath($InputPath)
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            $candidate = Join-Path $candidate $FileName
        }

        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "Evidence file was not found: $candidate"
        }

        return (Resolve-Path -LiteralPath $candidate).Path
    }

    $rootPath = Join-Path $repoRoot $DefaultRoot
    if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) {
        throw "Evidence root was not found: $rootPath"
    }

    # <lang>
    #   <zh-CN>默认选取最新可用证据目录，避免脚本为发布节奏写死某一次本地运行路径。</zh-CN>
    #   <en>Select the latest available evidence directory by default so release summaries are not pinned to one local run.</en>
    # </lang>
    $matches = @(Get-ChildItem -LiteralPath $rootPath -Directory -Recurse |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName $FileName) -PathType Leaf } |
        Sort-Object LastWriteTimeUtc -Descending)

    if ($matches.Count -eq 0) {
        throw "No evidence file named $FileName was found under $rootPath"
    }

    return (Join-Path $matches[0].FullName $FileName)
}

# <lang>
#   <zh-CN>读取并解析低敏 JSON evidence；解析失败时抛出带路径的受控错误。</zh-CN>
#   <en>Read and parse low-sensitivity JSON evidence, throwing a controlled path-aware error when parsing fails.</en>
# </lang>
function Read-JsonFile {
    param([string]$Path)

    try {
        return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json)
    }
    catch {
        throw "Unable to read JSON evidence file: $Path. $($_.Exception.Message)"
    }
}

# <lang>
#   <zh-CN>把步骤摘要投影为名称、状态和退出码，不复制额外运行时对象或秘密字段。</zh-CN>
#   <en>Project step summaries to name, status, and exit code without copying extra runtime objects or secret fields.</en>
# </lang>
function Get-StepSummary {
    param([object]$RunSummary)

    if ($null -eq $RunSummary -or $null -eq $RunSummary.Steps) {
        return @()
    }

    return @($RunSummary.Steps | ForEach-Object {
            [pscustomobject][ordered]@{
                Name = $_.Name
                Status = $_.Status
                ExitCode = $_.ExitCode
            }
        })
}

$releaseManifestJsonPath = Resolve-EvidenceFile -InputPath $ReleaseManifestPath -DefaultRoot 'work-zone/dev/evidence/p13.1' -FileName 'release-manifest.json'
$operationsSummaryJsonPath = Resolve-EvidenceFile -InputPath $OperationsEvidencePath -DefaultRoot 'work-zone/dev/evidence/p13.2' -FileName 'run-summary.json'
$documentationSummaryJsonPath = Resolve-EvidenceFile -InputPath $DocumentationEvidencePath -DefaultRoot 'work-zone/dev/evidence/p13.3' -FileName 'run-summary.json'

$releaseManifest = Read-JsonFile -Path $releaseManifestJsonPath
$operationsSummary = Read-JsonFile -Path $operationsSummaryJsonPath
$documentationSummary = Read-JsonFile -Path $documentationSummaryJsonPath

# <lang>
#   <zh-CN>只读取当前仓库 Git 摘要并记录脏状态计数；不会提交、回滚或修改索引。</zh-CN>
#   <en>Read the current repository Git summary and dirty-state count without committing, reverting, or changing the index.</en>
# </lang>
$repositoryCommit = Get-GitValue -Arguments @('rev-parse', '--short', 'HEAD')
$repositoryBranch = Get-GitValue -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')
$repositoryStatusOutput = Get-GitValue -Arguments @('status', '--short')
$repositoryStatusLines = @()
if (-not [string]::IsNullOrWhiteSpace($repositoryStatusOutput)) {
    $repositoryStatusLines = @($repositoryStatusOutput -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$targetSupplementItems = @(
    [pscustomobject][ordered]@{ Code = 'REAL-IIS-TLS-ACL'; Status = 'PendingTargetEnvironment'; Detail = '真实 IIS 站点、TLS、应用池 ACL 和虚拟目录发布需要目标环境复测。' },
    [pscustomobject][ordered]@{ Code = 'SQLSERVER-2016-2017-2019'; Status = 'PendingTargetEnvironment'; Detail = 'SQL Server 2016/2017/2019 真实实例矩阵仍需目标环境补证。' },
    [pscustomobject][ordered]@{ Code = 'ENTERPRISE-SCAN'; Status = 'PendingTargetEnvironment'; Detail = '绿盟等企业扫描工具结果需由对应扫描环境补证。' },
    [pscustomobject][ordered]@{ Code = 'HIA-CONSUMER-TRANSPORT'; Status = 'PendingTargetEnvironment'; Detail = '真实 HIA consumer、transport 和运行时接入仍在后续集成周期补证。' },
    [pscustomobject][ordered]@{ Code = 'BUSINESS-SIGNOFF'; Status = 'PendingTargetEnvironment'; Detail = '员工资料更正样板路径仍需真实业务负责人签收。' }
)

$releaseFailedChecks = [int]$releaseManifest.Summary.FailedChecks
$releaseWarningChecks = [int]$releaseManifest.Summary.WarningChecks
$operationsFailedSteps = [int]$operationsSummary.FailedStepCount
$documentationFailedSteps = [int]$documentationSummary.FailedStepCount
$documentationPendingSteps = if ($null -ne $documentationSummary.PendingStepCount) { [int]$documentationSummary.PendingStepCount } else { 0 }

# <lang>
#   <zh-CN>汇总发布包、证据步骤和目标环境 Pending 项；ReadyForInternalReleaseLedger 不等于真实生产发布通过。</zh-CN>
#   <en>Summarize the release package, evidence steps, and target-environment pending items; ReadyForInternalReleaseLedger is not a real production-release pass.</en>
# </lang>
$summary = [pscustomobject][ordered]@{
    SchemaVersion = 'p13.4.release-summary.v1'
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Version = $Version
    ReleaseName = $ReleaseName
    VersionPolicy = [pscustomobject][ordered]@{
        Scheme = '0.13.N'
        IsPreOneDotZero = $true
        CreatesGitTag = $false
        CreatesReleaseBranch = $false
        RequiresManualReleaseConfirmation = $true
    }
    Repository = [pscustomobject][ordered]@{
        Root = $repoRoot
        Commit = $repositoryCommit
        Branch = $repositoryBranch
        Status = [pscustomobject][ordered]@{
            IsDirty = $repositoryStatusLines.Count -gt 0
            EntryCount = $repositoryStatusLines.Count
        }
    }
    Inputs = [pscustomobject][ordered]@{
        ReleaseManifestJson = $releaseManifestJsonPath
        OperationsRunSummaryJson = $operationsSummaryJsonPath
        DocumentationRunSummaryJson = $documentationSummaryJsonPath
        PublicReleaseNotesTemplate = 'docs/release-notes-template.md'
    }
    ReleasePackage = [pscustomobject][ordered]@{
        ReleaseId = $releaseManifest.ReleaseId
        PackageRoot = $releaseManifest.Package.Root
        FileCount = $releaseManifest.Package.FileCount
        TotalBytes = $releaseManifest.Package.TotalBytes
        FailedChecks = $releaseFailedChecks
        WarningChecks = $releaseWarningChecks
    }
    Evidence = [pscustomobject][ordered]@{
        Operations = [pscustomobject][ordered]@{
            Profile = $operationsSummary.Profile
            RunDirectory = $operationsSummary.RunDirectory
            FailedStepCount = $operationsFailedSteps
            Steps = Get-StepSummary -RunSummary $operationsSummary
        }
        Documentation = [pscustomobject][ordered]@{
            RunDirectory = $documentationSummary.RunDirectory
            FailedStepCount = $documentationFailedSteps
            PendingStepCount = $documentationPendingSteps
            Steps = Get-StepSummary -RunSummary $documentationSummary
        }
    }
    TargetEnvironmentSupplement = $targetSupplementItems
    Summary = [pscustomobject][ordered]@{
        FailedReleaseChecks = $releaseFailedChecks
        WarningReleaseChecks = $releaseWarningChecks
        FailedOperationsSteps = $operationsFailedSteps
        FailedDocumentationSteps = $documentationFailedSteps
        PendingTargetEnvironmentItems = $targetSupplementItems.Count
        ReadyForInternalReleaseLedger = ($releaseFailedChecks -eq 0 -and $operationsFailedSteps -eq 0 -and $documentationFailedSteps -eq 0)
    }
}

# <lang>
#   <zh-CN>生成低敏 Markdown 摘要，保留目标环境待补证和只读边界，不写入连接串或凭据。</zh-CN>
#   <en>Generate a low-sensitivity Markdown summary that preserves target-environment gaps and read-only boundaries without writing connection strings or credentials.</en>
# </lang>
$markdownLines = @(
    '# Portal Release Summary',
    '',
    ('Version: `{0}`' -f $Version),
    ('Release name: `{0}`' -f $ReleaseName),
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    ('Repository commit: `{0}`' -f $repositoryCommit),
    '',
    '## Release Package',
    '',
    ('ReleaseId: `{0}`' -f $summary.ReleasePackage.ReleaseId),
    ('Files: `{0}`' -f $summary.ReleasePackage.FileCount),
    ('Failed checks: `{0}`' -f $summary.ReleasePackage.FailedChecks),
    ('Warning checks: `{0}`' -f $summary.ReleasePackage.WarningChecks),
    '',
    '## Evidence',
    '',
    '| Area | Result | Evidence |',
    '| --- | --- | --- |',
    ('| Release manifest | Failed={0}; Warning={1} | `{2}` |' -f $releaseFailedChecks, $releaseWarningChecks, $releaseManifestJsonPath),
    ('| Operations | Failed={0} | `{1}` |' -f $operationsFailedSteps, $operationsSummary.RunDirectory),
    ('| Documentation | Failed={0}; Pending={1} | `{2}` |' -f $documentationFailedSteps, $documentationPendingSteps, $documentationSummary.RunDirectory),
    '',
    '## Target Environment Supplements',
    '',
    '| Code | Status | Detail |',
    '| --- | --- | --- |'
)

foreach ($item in $targetSupplementItems) {
    $markdownLines += ('| {0} | {1} | {2} |' -f $item.Code, $item.Status, (($item.Detail -replace '\|', '/') -replace "`r?`n", ' '))
}

$markdownLines += @(
    '',
    '## Boundary',
    '',
    '1. This summary is read-only and does not create tags, branches, release packages, database changes, IIS changes, or external notifications.',
    '2. Warnings remain visible and must be reviewed before any real external release.',
    '3. Target-environment supplement items are not failures; they mark evidence that cannot be produced on the local development machine.'
)

# <lang>
#   <zh-CN>按显式输出参数写入 JSON/Markdown；未指定时只输出到控制台，不创建默认证据目录。</zh-CN>
#   <en>Write JSON/Markdown only when explicit output parameters are provided; otherwise print to the console without creating a default evidence directory.</en>
# </lang>
if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('RELEASE SUMMARY JSON: {0}' -f ([System.IO.Path]::GetFullPath($OutputJson)))
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    Write-Utf8NoBomFile -Path $OutputMarkdown -Content (($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine)
    Write-Host ('RELEASE SUMMARY MD: {0}' -f ([System.IO.Path]::GetFullPath($OutputMarkdown)))
}

if ($AsJson) {
    $summary | ConvertTo-Json -Depth 8
}
else {
    $markdownLines | ForEach-Object { Write-Host $_ }
}

# <lang>
#   <zh-CN>发布摘要未达到内部 ledger 条件时返回非零；目标环境 Pending 不被伪装为通过。</zh-CN>
#   <en>Return non-zero when the release summary is not ready for the internal ledger; target-environment pending items are never disguised as passes.</en>
# </lang>
if (-not $summary.Summary.ReadyForInternalReleaseLedger) {
    exit 1
}

exit 0
