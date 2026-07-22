<#
.SYNOPSIS
    Runs the P14.2 near-target release rehearsal and writes an evidence package.

.DESCRIPTION
    中文：本脚本编排 P14.2 近真实发布演练：重新生成 FileSystem 发布包、生成 release manifest、
    启动或复用 IIS Express、执行 smoke、记录外置配置边界和回滚 dry-run，并可选捕获主题截图近似证据。
    它不修改真实 IIS、不连接生产数据库、不执行破坏性迁移、不写业务数据、不读取或输出真实连接串、密码、
    Token、Cookie 或证书私钥，也不把本机/IIS Express 结果宣称为生产通过。
    English: This script orchestrates the P14.2 near-target release rehearsal: it regenerates a filesystem publish
    package, creates a release manifest, starts or reuses IIS Express, runs smoke checks, records external-config
    boundaries and rollback dry-run evidence, and can optionally capture approximate theme screenshots. It does not
    modify real IIS, connect to production databases, run destructive migrations, write business data, read or output
    real connection strings, passwords, tokens, cookies, or certificate private keys, and it never claims that local
    or IIS Express evidence is production approval.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [ValidateRange(1, 65535)]
    [int]$Port = 40001,

    [ValidatePattern('^https?://')]
    [string]$BaseUrl,

    [string]$OutputRoot,

    [string]$PublishRoot,

    [switch]$SkipThemeScreenshots,

    [switch]$KeepIISExpressRunning,

    [switch]$AllowFailures
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    $BaseUrl = ('http://localhost:{0}/' -f $Port)
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p14.2'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p14.2'
    }
}

if ([string]::IsNullOrWhiteSpace($PublishRoot)) {
    $PublishRoot = Join-Path $repoRoot 'temp/publish'
}

$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) ('{0}-{1}' -f $runId, $Profile)
$publishPath = Join-Path ([System.IO.Path]::GetFullPath($PublishRoot)) ('P14.2-{0}-{1}' -f $Configuration, $runId)
$releaseManifestRoot = Join-Path $runDirectory 'release-manifest'
$screenshotOutput = Join-Path $runDirectory 'theme-screenshots'
$steps = New-Object 'System.Collections.Generic.List[object]'
$startedIISExpress = $false

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

function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

function Get-PwshPath {
    $preferred = 'C:\Program Files\PowerShell\7\pwsh.exe'
    if (Test-Path -LiteralPath $preferred -PathType Leaf) {
        return $preferred
    }

    $command = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw 'PowerShell 7 (pwsh) was not found.'
}

function ConvertTo-RepoPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

function Test-TcpPort {
    param(
        [string]$ServerHost,
        [int]$ServerPort
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $client.Connect($ServerHost, $ServerPort)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Add-StepResult {
    param(
        [string]$Name,
        [ValidateSet('Passed', 'Failed', 'Skipped')]
        [string]$Status,
        [int]$ExitCode,
        [string]$LogPath,
        [string]$Detail = '',
        [bool]$Required = $true,
        [datetime]$StartedAtUtc,
        [datetime]$FinishedAtUtc,
        [string]$Command = ''
    )

    $steps.Add([pscustomobject][ordered]@{
            Name = $Name
            Status = $Status
            ExitCode = $ExitCode
            Required = $Required
            LogPath = ConvertTo-RepoPath -Path $LogPath
            Detail = $Detail
            StartedUtc = $StartedAtUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
            FinishedUtc = $FinishedAtUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
            Command = $Command
        })
}

function Invoke-RehearsalStep {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments,
        [string]$LogPath,
        [bool]$Required = $true
    )

    $pwshPath = Get-PwshPath
    $argumentList = @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $ScriptPath) + $Arguments
    $displayCommand = @($pwshPath) + $argumentList | ForEach-Object { Format-EvidenceArgument -Value $_ }
    $startedAt = (Get-Date).ToUniversalTime()
    $capturedLines = New-Object 'System.Collections.Generic.List[string]'
    $exitCode = 0

    try {
        $output = & $pwshPath @argumentList 2>&1
        $exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
        foreach ($line in $output) {
            $capturedLines.Add([string]$line)
        }
    }
    catch {
        $exitCode = 1
        $capturedLines.Add($_.Exception.Message)
    }

    $finishedAt = (Get-Date).ToUniversalTime()
    $status = if ($exitCode -eq 0) { 'Passed' } else { 'Failed' }
    $logLines = @(
        ('# {0}' -f $Name),
        '',
        ('Started UTC: {0}' -f $startedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('Finished UTC: {0}' -f $finishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('ExitCode: {0}' -f $exitCode),
        ('Required: {0}' -f $Required),
        ('Command: {0}' -f ($displayCommand -join ' ')),
        '',
        '```text'
    ) + $capturedLines + @(
        '```',
        ''
    )

    Write-Utf8NoBomFile -Path $LogPath -Content (($logLines -join [Environment]::NewLine) + [Environment]::NewLine)

    Add-StepResult `
        -Name $Name `
        -Status $status `
        -ExitCode $exitCode `
        -LogPath $LogPath `
        -Required $Required `
        -StartedAtUtc $startedAt `
        -FinishedAtUtc $finishedAt `
        -Command ($displayCommand -join ' ')

    Write-Host ('[{0}] {1} -> {2}' -f $status.ToUpperInvariant(), $Name, (ConvertTo-RepoPath -Path $LogPath))

    if ($exitCode -ne 0 -and $Required) {
        throw ('Required rehearsal step failed: {0}' -f $Name)
    }
}

function Write-ConfigurationBoundaryEvidence {
    param([string]$OutputPath)

    $portalRoot = Join-Path $repoRoot 'src/Portal'
    $webConfigPath = Join-Path $portalRoot 'Web.config'
    $templatePath = Join-Path $portalRoot 'Config/Templates/connectionStrings.config'
    $defaultExternalPath = Join-Path $env:USERPROFILE 'Web/HIA-ASPNETPortal/dev/connectionStrings.config'
    $webConfigText = if (Test-Path -LiteralPath $webConfigPath -PathType Leaf) {
        [System.IO.File]::ReadAllText($webConfigPath, [System.Text.UTF8Encoding]::new($false))
    }
    else {
        ''
    }

    $result = [pscustomobject][ordered]@{
        SchemaVersion = 'p14.2.config-boundary-dry-run.v1'
        GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        ReadsSecretValues = $false
        WebConfigPath = ConvertTo-RepoPath -Path $webConfigPath
        TemplatePath = ConvertTo-RepoPath -Path $templatePath
        TemplateExists = Test-Path -LiteralPath $templatePath -PathType Leaf
        ExternalCfgPathSettingDeclared = $webConfigText -match 'ExternalCfgPath'
        EnvironmentSettingDeclared = $webConfigText -match 'Portal\.Environment'
        DefaultExternalConnectionStringsPath = $defaultExternalPath
        DefaultExternalConnectionStringsExists = Test-Path -LiteralPath $defaultExternalPath -PathType Leaf
        SensitiveValuesCaptured = $false
        Boundary = @(
            'Only path policy, template presence, and local external file existence are recorded.',
            'The external connectionStrings.config content is not read by this dry-run.',
            'Real IIS/TLS/ACL target configuration must be supplemented in the target environment.'
        )
    }

    Write-Utf8NoBomFile -Path $OutputPath -Content (($result | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
    return $result
}

function Write-RollbackDryRunEvidence {
    param(
        [string]$OutputPath,
        [string]$PackagePath
    )

    $rollbackGuide = Join-Path $repoRoot 'docs/deployment-rollback-guide.md'
    $deploymentGuide = Join-Path $repoRoot 'docs/deployment-guide.md'
    $packageExists = Test-Path -LiteralPath $PackagePath -PathType Container
    $packageFileCount = if ($packageExists) {
        @(Get-ChildItem -LiteralPath $PackagePath -File -Recurse).Count
    }
    else {
        0
    }

    $result = [pscustomobject][ordered]@{
        SchemaVersion = 'p14.2.rollback-dry-run.v1'
        GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        ExecutedRealRollback = $false
        PackagePath = ConvertTo-RepoPath -Path $PackagePath
        PackageExists = $packageExists
        PackageFileCount = $packageFileCount
        RollbackGuide = ConvertTo-RepoPath -Path $rollbackGuide
        RollbackGuideExists = Test-Path -LiteralPath $rollbackGuide -PathType Leaf
        DeploymentGuide = ConvertTo-RepoPath -Path $deploymentGuide
        DeploymentGuideExists = Test-Path -LiteralPath $deploymentGuide -PathType Leaf
        DryRunSteps = @(
            'Identify the published package directory and manifest.',
            'Confirm Web.config and external connectionStrings.config are backed up outside source control.',
            'Restore prior filesystem package or clean the target directory before copying the prior package.',
            'Restore previous external configuration file from protected backup.',
            'Run smoke checks and review diagnostics logs after rollback.'
        )
        PendingTargetEnvironment = @(
            'Real IIS site rollback window',
            'TLS certificate rollback',
            'App-pool identity and ACL rollback',
            'Target database backup and restore drill'
        )
    }

    Write-Utf8NoBomFile -Path $OutputPath -Content (($result | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
    return $result
}

New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $PublishRoot | Out-Null
Write-Host ('Near-target release rehearsal directory: {0}' -f $runDirectory)
Write-Host ('Publish path: {0}' -f $publishPath)

try {
    Invoke-RehearsalStep `
        -Name 'Filesystem publish package' `
        -ScriptPath (Join-Path $PSScriptRoot 'Publish-PortalFileSystem.ps1') `
        -Arguments @('-Configuration', $Configuration, '-PublishPath', $publishPath) `
        -LogPath (Join-Path $runDirectory 'filesystem-publish.log.md') `
        -Required $true

    Invoke-RehearsalStep `
        -Name 'Release manifest' `
        -ScriptPath (Join-Path $PSScriptRoot 'New-PortalReleaseManifest.ps1') `
        -Arguments @('-PackagePath', $publishPath, '-OutputRoot', $releaseManifestRoot, '-ReleaseId', ('P14.2-' + $runId)) `
        -LogPath (Join-Path $runDirectory 'release-manifest.log.md') `
        -Required $true

    $configStarted = (Get-Date).ToUniversalTime()
    $configEvidencePath = Join-Path $runDirectory 'configuration-boundary-dry-run.json'
    $configEvidence = Write-ConfigurationBoundaryEvidence -OutputPath $configEvidencePath
    Add-StepResult `
        -Name 'Configuration boundary dry run' `
        -Status 'Passed' `
        -ExitCode 0 `
        -LogPath $configEvidencePath `
        -Detail 'Recorded paths, template presence, and non-secret configuration boundary only.' `
        -Required $true `
        -StartedAtUtc $configStarted `
        -FinishedAtUtc (Get-Date).ToUniversalTime() `
        -Command 'internal'
    Write-Host ('[PASSED] Configuration boundary dry run -> {0}' -f (ConvertTo-RepoPath -Path $configEvidencePath))

    $rollbackStarted = (Get-Date).ToUniversalTime()
    $rollbackEvidencePath = Join-Path $runDirectory 'rollback-dry-run.json'
    $rollbackEvidence = Write-RollbackDryRunEvidence -OutputPath $rollbackEvidencePath -PackagePath $publishPath
    Add-StepResult `
        -Name 'Rollback dry run' `
        -Status 'Passed' `
        -ExitCode 0 `
        -LogPath $rollbackEvidencePath `
        -Detail 'Recorded package, rollback guide, and target-environment rollback gaps without executing rollback.' `
        -Required $true `
        -StartedAtUtc $rollbackStarted `
        -FinishedAtUtc (Get-Date).ToUniversalTime() `
        -Command 'internal'
    Write-Host ('[PASSED] Rollback dry run -> {0}' -f (ConvertTo-RepoPath -Path $rollbackEvidencePath))

    $baseUri = [Uri]$BaseUrl
    if ($baseUri.Scheme -ne 'http' -or $baseUri.Host -notin @('localhost', '127.0.0.1', '::1')) {
        throw 'P14.2 near-target rehearsal only starts IIS Express for local HTTP BaseUrl.'
    }

    if (-not (Test-TcpPort -ServerHost $baseUri.Host -ServerPort $baseUri.Port)) {
        Invoke-RehearsalStep `
            -Name 'Start IIS Express' `
            -ScriptPath (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') `
            -Arguments @('-Port', ([string]$baseUri.Port)) `
            -LogPath (Join-Path $runDirectory 'start-iisexpress.log.md') `
            -Required $true
        $startedIISExpress = $true
    }
    else {
        $skipStarted = (Get-Date).ToUniversalTime()
        $skipLogPath = Join-Path $runDirectory 'start-iisexpress.log.md'
        Write-Utf8NoBomFile -Path $skipLogPath -Content ("# Start IIS Express`r`n`r`nPort already listening; existing local server was reused.`r`n")
        Add-StepResult `
            -Name 'Start IIS Express' `
            -Status 'Skipped' `
            -ExitCode 0 `
            -LogPath $skipLogPath `
            -Detail 'Port already listening; existing local server was reused.' `
            -Required $true `
            -StartedAtUtc $skipStarted `
            -FinishedAtUtc (Get-Date).ToUniversalTime() `
            -Command 'internal'
        Write-Host ('[SKIPPED] Start IIS Express -> {0}' -f (ConvertTo-RepoPath -Path $skipLogPath))
    }

    Invoke-RehearsalStep `
        -Name 'Portal smoke after publish rehearsal' `
        -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalSmoke.ps1') `
        -Arguments @('-BaseUrl', $BaseUrl, '-SkipAuthenticated', '-CheckGenericErrorPage', '-CheckDocumentSafety', '-CheckEditorSafety') `
        -LogPath (Join-Path $runDirectory 'portal-smoke.log.md') `
        -Required $true

    if ($SkipThemeScreenshots) {
        $skipStarted = (Get-Date).ToUniversalTime()
        $skipLogPath = Join-Path $runDirectory 'theme-screenshots.log.md'
        Write-Utf8NoBomFile -Path $skipLogPath -Content "# Theme screenshots`r`n`r`nSkipped by parameter.`r`n"
        Add-StepResult `
            -Name 'Theme screenshot approximation' `
            -Status 'Skipped' `
            -ExitCode 0 `
            -LogPath $skipLogPath `
            -Detail 'Skipped by parameter.' `
            -Required $false `
            -StartedAtUtc $skipStarted `
            -FinishedAtUtc (Get-Date).ToUniversalTime() `
            -Command 'internal'
    }
    else {
        # 中文：`pwsh -File` 不适合直接展开 string[] 参数，这里按主题拆成独立步骤，避免第二个主题误绑定到其它参数。
        # English: `pwsh -File` does not safely expand string[] values here, so capture each theme in its own step.
        foreach ($themeName in @('EnterpriseLight', 'StateClassicLight')) {
            $themeOutput = Join-Path $screenshotOutput $themeName
            $screenshotArgs = @('-BaseUrl', $BaseUrl, '-OutputDirectory', $themeOutput, '-Themes', $themeName)
            Invoke-RehearsalStep `
                -Name ('Theme screenshot approximation - {0}' -f $themeName) `
                -ScriptPath (Join-Path $PSScriptRoot 'Capture-PortalThemeScreenshots.ps1') `
                -Arguments $screenshotArgs `
                -LogPath (Join-Path $runDirectory ('theme-screenshots-{0}.log.md' -f $themeName.ToLowerInvariant())) `
                -Required $false
        }
    }
}
finally {
    if ($startedIISExpress -and -not $KeepIISExpressRunning) {
        try {
            Invoke-RehearsalStep `
                -Name 'Stop IIS Express' `
                -ScriptPath (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') `
                -Arguments @('-Port', ([string]$Port)) `
                -LogPath (Join-Path $runDirectory 'stop-iisexpress.log.md') `
                -Required $false
        }
        catch {
            Write-Warning ('Unable to stop IIS Express cleanly: {0}' -f $_.Exception.Message)
        }
    }
}

$releaseManifestJson = @(Get-ChildItem -LiteralPath $releaseManifestRoot -Filter 'release-manifest.json' -File -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1)
$releaseManifest = if ($releaseManifestJson.Count -gt 0) {
    Get-Content -LiteralPath $releaseManifestJson[0].FullName -Raw -Encoding UTF8 | ConvertFrom-Json
}
else {
    $null
}

$requiredFailures = @($steps | Where-Object { $_.Required -and $_.Status -eq 'Failed' })
$optionalFailures = @($steps | Where-Object { -not $_.Required -and $_.Status -eq 'Failed' })
$summary = [pscustomobject][ordered]@{
    SchemaVersion = 'p14.2.near-target-release-rehearsal.v1'
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Profile = $Profile
    Configuration = $Configuration
    BaseUrl = $BaseUrl
    Port = $Port
    RunDirectory = ConvertTo-RepoPath -Path $runDirectory
    PublishPath = ConvertTo-RepoPath -Path $publishPath
    ReleaseManifestJson = if ($releaseManifestJson.Count -gt 0) { ConvertTo-RepoPath -Path $releaseManifestJson[0].FullName } else { '' }
    ReleaseManifestSummary = if ($null -ne $releaseManifest) {
        [pscustomobject][ordered]@{
            ReleaseId = $releaseManifest.ReleaseId
            FileCount = $releaseManifest.Package.FileCount
            FailedChecks = $releaseManifest.Summary.FailedChecks
            WarningChecks = $releaseManifest.Summary.WarningChecks
        }
    }
    else {
        $null
    }
    ConfigurationBoundary = $configEvidence
    RollbackDryRun = $rollbackEvidence
    Steps = $steps
    Summary = [pscustomobject][ordered]@{
        RequiredFailedStepCount = $requiredFailures.Count
        OptionalFailedStepCount = $optionalFailures.Count
        RequiredStepCount = @($steps | Where-Object { $_.Required }).Count
        OptionalStepCount = @($steps | Where-Object { -not $_.Required }).Count
        ReadyForInternalReleaseEntry = ($requiredFailures.Count -eq 0)
        RealProductionEvidenceClaimed = $false
    }
    PendingTargetEnvironment = @(
        'Real IIS site, TLS, app-pool identity, virtual directory and ACL validation',
        'SQL Server 2016/2017/2019 target instances and backup/restore drill',
        'Enterprise scanner report and re-scan window',
        'Real business-owner signoff for the employee-profile correction scenario'
    )
}

$summaryJsonPath = Join-Path $runDirectory 'near-target-release-rehearsal.json'
Write-Utf8NoBomFile -Path $summaryJsonPath -Content (($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine)

$markdownLines = @(
    '# Portal Near-Target Release Rehearsal',
    '',
    ('Profile: `{0}`' -f $Profile),
    ('Configuration: `{0}`' -f $Configuration),
    ('BaseUrl: `{0}`' -f $BaseUrl),
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    '',
    '## Conclusion',
    '',
    ('Ready for internal release entry: `{0}`' -f $summary.Summary.ReadyForInternalReleaseEntry),
    ('Required failed steps: `{0}`' -f $summary.Summary.RequiredFailedStepCount),
    ('Optional failed steps: `{0}`' -f $summary.Summary.OptionalFailedStepCount),
    ('Real production evidence claimed: `{0}`' -f $summary.Summary.RealProductionEvidenceClaimed),
    '',
    '## Package',
    '',
    ('Publish path: `{0}`' -f $summary.PublishPath),
    ('Release manifest: `{0}`' -f $summary.ReleaseManifestJson),
    '',
    '## Steps',
    '',
    '| Step | Required | Status | Evidence |',
    '| --- | --- | --- | --- |'
)

foreach ($step in $steps) {
    $markdownLines += ('| {0} | {1} | {2} | `{3}` |' -f $step.Name, $step.Required, $step.Status, $step.LogPath)
}

$markdownLines += @(
    '',
    '## Pending Target Environment',
    '',
    '| Item |',
    '| --- |'
)

foreach ($item in $summary.PendingTargetEnvironment) {
    $markdownLines += ('| {0} |' -f (($item -replace '\|', '/') -replace "`r?`n", ' '))
}

$markdownLines += @(
    '',
    '## Boundary',
    '',
    '1. This rehearsal uses local or near-target evidence and does not claim real production approval.',
    '2. External configuration evidence records only paths, template presence and existence flags; it does not read values.',
    '3. Rollback evidence is a dry-run and does not modify a target site or database.',
    '4. Theme screenshots, when captured, are approximate UI evidence and do not replace business signoff.',
    ''
)

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'README.md') -Content (($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine)

$releaseEntryPath = Join-Path $repoRoot 'work-zone/dev/releases/0.14.1-p14-near-target-release-rehearsal.md'
$releaseEntryLines = @(
    '# 0.14.1 P14 Near-Target Release Rehearsal',
    '',
    ('日期：{0}' -f (Get-Date).ToString('yyyy-MM-dd')),
    '',
    '## 定位',
    '',
    '`0.14.1` 是 P14.2 近真实发布演练的内部 release entry。它验证“重新生成 FileSystem 发布包 + release manifest + IIS Express smoke + 配置边界 + 回滚 dry-run + 可选截图证据”这一条近真实发布链路。',
    '',
    '它不是正式生产发布，不创建 Git tag，不创建 release 分支，不代表真实 IIS/TLS/ACL、SQL Server 2016/2017/2019、企业扫描或真实业务签收已经通过。',
    '',
    '## 版本信息',
    '',
    '| 项 | 内容 |',
    '| --- | --- |',
    '| Version | `0.14.1` |',
    '| Release name | `P14 near-target release rehearsal` |',
    '| Git tag | 暂不创建 |',
    '| Release branch | 暂不创建 |',
    ('| Evidence | `{0}` |' -f (ConvertTo-RepoPath -Path $runDirectory)),
    '',
    '## 摘要',
    '',
    '| 项 | 内容 |',
    '| --- | --- |',
    ('| 必需步骤失败数 | `{0}` |' -f $summary.Summary.RequiredFailedStepCount),
    ('| 可选步骤失败数 | `{0}` |' -f $summary.Summary.OptionalFailedStepCount),
    ('| 是否声明真实生产证据 | `{0}` |' -f $summary.Summary.RealProductionEvidenceClaimed),
    '',
    '## 目标环境补证',
    '',
    '| 项 | 状态 |',
    '| --- | --- |'
)

foreach ($item in $summary.PendingTargetEnvironment) {
    $releaseEntryLines += ('| {0} | `PendingTargetEnvironment` |' -f (($item -replace '\|', '/') -replace "`r?`n", ' '))
}

$releaseEntryLines += @(
    '',
    '## 结论',
    '',
    '`0.14.1` 可作为 P14.2 的近真实发布演练 baseline。真实 tag、release 分支、对外发布、真实 IIS/TLS/ACL、目标 SQL Server、企业扫描和业务签收仍需人工确认或目标环境补证。',
    ''
)

Write-Utf8NoBomFile -Path $releaseEntryPath -Content (($releaseEntryLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('Near-target release rehearsal JSON: {0}' -f (ConvertTo-RepoPath -Path $summaryJsonPath))
Write-Host ('Near-target release rehearsal README: {0}' -f (ConvertTo-RepoPath -Path (Join-Path $runDirectory 'README.md')))
Write-Host ('Internal release entry: {0}' -f (ConvertTo-RepoPath -Path $releaseEntryPath))

if ($requiredFailures.Count -gt 0 -and -not $AllowFailures) {
    throw ('P14.2 near-target release rehearsal contains required failed steps: {0}' -f $requiredFailures.Count)
}
