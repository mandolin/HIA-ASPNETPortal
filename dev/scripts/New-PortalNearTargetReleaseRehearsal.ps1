<#
.SYNOPSIS
    <lang>
      <zh-CN>执行 P14.2 近真实发布演练，并写入证据包。</zh-CN>
      <en>Runs the P14.2 near-target release rehearsal and writes an evidence package.</en>
    </lang>

.DESCRIPTION
    <lang>
      <zh-CN>本脚本编排 P14.2 近真实发布演练：重新生成 FileSystem 发布包、生成 release manifest、启动或复用 IIS Express、执行 smoke、记录外置配置边界和回滚 dry-run，并可选捕获主题截图近似证据。它不修改真实 IIS、不连接生产数据库、不执行破坏性迁移、不写业务数据、不读取或输出真实连接串、密码、Token、Cookie 或证书私钥，也不把本机/IIS Express 结果宣称为生产通过。</zh-CN>
      <en>This script orchestrates the P14.2 near-target release rehearsal: it regenerates a filesystem publish package, creates a release manifest, starts or reuses IIS Express, runs smoke checks, records external-config boundaries and rollback dry-run evidence, and can optionally capture approximate theme screenshots. It does not modify real IIS, connect to production databases, run destructive migrations, write business data, read or output real connection strings, passwords, tokens, cookies, or certificate private keys, and it never claims that local or IIS Express evidence is production approval.</en>
    </lang>

.PARAMETER Configuration
    <lang>
      <zh-CN>传递给 FileSystem 发布脚本的构建配置；它只决定本地发布包构建方式，不代表目标环境已签收同一配置。</zh-CN>
      <en>Build configuration passed to the filesystem publish script; it controls local package generation only and does not prove target-environment approval of that configuration.</en>
    </lang>

.PARAMETER Profile
    <lang>
      <zh-CN>写入证据文件名和摘要的演练 profile 标签；它不加载真实环境配置，也不允许脚本读取私密连接串或凭据。</zh-CN>
      <en>Rehearsal profile label written to evidence file names and summaries; it does not load real environment configuration or allow secret connection strings or credentials to be read.</en>
    </lang>

.PARAMETER Port
    <lang>
      <zh-CN>IIS Express 本地 HTTP 端口；当未显式提供 BaseUrl 时，它也是默认 localhost URL 的端口来源。</zh-CN>
      <en>Local HTTP port for IIS Express; when BaseUrl is omitted, it also supplies the default localhost URL port.</en>
    </lang>

.PARAMETER BaseUrl
    <lang>
      <zh-CN>发布后 smoke 检查使用的本地 HTTP 基址；脚本只接受 localhost/loopback，不覆盖真实域名、生产 TLS 或反向代理链路。</zh-CN>
      <en>Local HTTP base URL used by post-publish smoke checks; the script accepts localhost/loopback only and does not cover real domains, production TLS, or reverse-proxy paths.</en>
    </lang>

.PARAMETER OutputRoot
    <lang>
      <zh-CN>证据包根目录；默认优先写入私有 WorkZone，缺失时落到仓库临时目录，且输出内容不得包含密钥或生产配置。</zh-CN>
      <en>Evidence-package root; by default it prefers the private WorkZone and falls back to the repository temp directory, and produced evidence must not contain secrets or production configuration.</en>
    </lang>

.PARAMETER PublishRoot
    <lang>
      <zh-CN>FileSystem 发布包父目录；脚本在其下创建带时间戳的子目录，避免把演练输出误写到人工维护的目标目录。</zh-CN>
      <en>Parent directory for the filesystem publish package; the script creates a timestamped child directory under it to avoid writing rehearsal output into a manually maintained target directory.</en>
    </lang>

.PARAMETER SkipThemeScreenshots
    <lang>
      <zh-CN>跳过 Playwright 主题截图近似证据；必需的发布包、manifest、配置边界、回滚和 smoke 步骤仍按原门禁记录。</zh-CN>
      <en>Skips approximate Playwright theme-screenshot evidence; required package, manifest, configuration-boundary, rollback, and smoke steps remain recorded by the same gate.</en>
    </lang>

.PARAMETER KeepIISExpressRunning
    <lang>
      <zh-CN>保留本脚本启动的 IIS Express 进程以便人工复查；它只影响本地进程清理，不会管理真实 IIS 站点。</zh-CN>
      <en>Keeps the IIS Express process started by this script for manual inspection; it affects only local process cleanup and never manages real IIS sites.</en>
    </lang>

.PARAMETER AllowFailures
    <lang>
      <zh-CN>允许存在必需步骤失败时仍写出摘要并以成功退出交回调用方；用于收集证据，不得解读为发布批准。</zh-CN>
      <en>Allows the script to write a summary and return success even when required steps fail; it is for evidence collection and must not be interpreted as release approval.</en>
    </lang>
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

# <lang>
#   <zh-CN>启用严格模式和 fail-fast 错误策略，使发布演练在缺少变量、路径或子步骤失败时不会静默产出误导性证据。</zh-CN>
#   <en>Enable strict mode and fail-fast error handling so missing variables, paths, or failed child steps cannot silently produce misleading rehearsal evidence.</en>
# </lang>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>从脚本目录反推仓库根，保证后续相对路径只锚定当前 checkout，而不是调用者的工作目录。</zh-CN>
#   <en>Derive the repository root from the script location so later relative paths are anchored to this checkout rather than the caller's working directory.</en>
# </lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# <lang>
#   <zh-CN>未显式传入 BaseUrl 时只生成 localhost HTTP 地址，保持演练限定在 IIS Express/loopback 边界内。</zh-CN>
#   <en>When BaseUrl is omitted, generate only a localhost HTTP URL so the rehearsal remains inside the IIS Express/loopback boundary.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    $BaseUrl = ('http://localhost:{0}/' -f $Port)
}

# <lang>
#   <zh-CN>未显式传入 OutputRoot 时优先选择私有 WorkZone 证据目录；没有 WorkZone 的 checkout 使用仓库临时目录作为可丢弃 fallback。</zh-CN>
#   <en>When OutputRoot is omitted, prefer the private WorkZone evidence directory; checkouts without WorkZone use the repository temp directory as a disposable fallback.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    # <lang>
    #   <zh-CN>此分支只根据目录存在性选择证据落点，不读取 WorkZone 中任何环境配置或私密材料。</zh-CN>
    #   <en>This branch chooses the evidence destination only by directory presence and does not read environment configuration or private material from WorkZone.</en>
    # </lang>
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p14.2'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p14.2'
    }
}

# <lang>
#   <zh-CN>未显式传入 PublishRoot 时使用仓库临时发布根，避免把 rehearsal 包写到人工管理或真实目标目录。</zh-CN>
#   <en>When PublishRoot is omitted, use the repository temporary publish root to avoid writing rehearsal packages into manually managed or real target directories.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($PublishRoot)) {
    $PublishRoot = Join-Path $repoRoot 'temp/publish'
}

# <lang>
#   <zh-CN>运行 ID 使用本机时间戳作为一次演练的可读身份；它不是 release id 的唯一权威来源。</zh-CN>
#   <en>The run id uses a local timestamp as a readable rehearsal identity; it is not the sole authoritative release identifier.</en>
# </lang>
$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')

# <lang>
#   <zh-CN>运行目录绑定 OutputRoot、运行 ID 和 profile，承载本次演练所有证据并可在后续账本中整体引用。</zh-CN>
#   <en>The run directory combines OutputRoot, run id, and profile, carrying all evidence for this rehearsal as a single ledger-referenceable unit.</en>
# </lang>
$runDirectory = Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) ('{0}-{1}' -f $runId, $Profile)

# <lang>
#   <zh-CN>发布包路径绑定 PublishRoot、配置和运行 ID，使每次演练写入新的子目录而不是覆盖既有包。</zh-CN>
#   <en>The publish path combines PublishRoot, configuration, and run id so each rehearsal writes a new child directory instead of overwriting an existing package.</en>
# </lang>
$publishPath = Join-Path ([System.IO.Path]::GetFullPath($PublishRoot)) ('P14.2-{0}-{1}' -f $Configuration, $runId)

# <lang>
#   <zh-CN>release manifest 根目录位于本次运行目录内，用于区分清单证据与发布包物理文件。</zh-CN>
#   <en>The release-manifest root lives under the run directory to separate manifest evidence from physical package files.</en>
# </lang>
$releaseManifestRoot = Join-Path $runDirectory 'release-manifest'

# <lang>
#   <zh-CN>主题截图目录只保存近似视觉证据；它不构成浏览器兼容性、真实 TLS 或生产主题签收。</zh-CN>
#   <en>The theme-screenshot directory stores approximate visual evidence only; it is not browser-compatibility, real TLS, or production-theme approval.</en>
# </lang>
$screenshotOutput = Join-Path $runDirectory 'theme-screenshots'

# <lang>
#   <zh-CN>步骤列表是本次演练摘要的内存账本，记录必需/可选步骤、命令、日志和 UTC 时间。</zh-CN>
#   <en>The step list is the in-memory ledger for this rehearsal summary, recording required/optional steps, commands, logs, and UTC timestamps.</en>
# </lang>
$steps = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>该标记只跟踪本脚本是否启动了 IIS Express，确保 finally 只清理自身负责的本地进程。</zh-CN>
#   <en>This flag tracks only whether this script started IIS Express, ensuring finally cleans up only the local process it owns.</en>
# </lang>
$startedIISExpress = $false

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入演练证据，并只创建指定输出目录/文件。</zh-CN>
#   <en>Write rehearsal evidence as UTF-8 without a BOM, creating only the requested output directory/file.</en>
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
#   <zh-CN>为日志显示安全引用子进程参数；不改变实际传给 pwsh 的参数数组。</zh-CN>
#   <en>Safely quote child-process arguments for log display without changing the argument array passed to pwsh.</en>
# </lang>
function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

# <lang>
#   <zh-CN>优先解析固定 PowerShell 7 路径并回退到 PATH；缺失执行器时明确失败。</zh-CN>
#   <en>Prefer the fixed PowerShell 7 path and fall back to PATH; fail explicitly when no executor is available.</en>
# </lang>
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

# <lang>
#   <zh-CN>将仓库内路径转换为稳定相对路径，仓库外路径保持绝对形式以避免证据歧义。</zh-CN>
#   <en>Convert in-repository paths to stable relative paths while keeping external paths absolute to avoid evidence ambiguity.</en>
# </lang>
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

# <lang>
#   <zh-CN>只探测指定 TCP 端口是否已有监听，不创建 IIS、不发送业务请求。</zh-CN>
#   <en>Probe whether the specified TCP port is already listening without creating IIS or sending a business request.</en>
# </lang>
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

# <lang>
#   <zh-CN>追加低敏演练步骤结果并归一化日志路径/时间；不把成功记录扩展为生产批准。</zh-CN>
#   <en>Add a low-sensitivity rehearsal step result with normalized log path/time; success is not production approval.</en>
# </lang>
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

# <lang>
#   <zh-CN>以独立 PowerShell 7 子进程执行一个演练步骤并记录输出/退出码；必需步骤失败时抛出，禁止泄露秘密。</zh-CN>
#   <en>Run one rehearsal step in an isolated PowerShell 7 process and record output/exit code; throw for required failures without exposing secrets.</en>
# </lang>
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

# <lang>
#   <zh-CN>仅记录 Web.config/模板路径、存在性和目标环境边界，不读取外置 connectionStrings 内容。</zh-CN>
#   <en>Record Web.config/template paths, existence and target-environment boundaries without reading external connectionStrings content.</en>
# </lang>
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

# <lang>
#   <zh-CN>生成回滚 dry-run 证据和待补环境清单，不执行复制、恢复、IIS 或数据库回滚。</zh-CN>
#   <en>Generate rollback dry-run evidence and pending-environment items without copying, restoring, IIS, or database rollback.</en>
# </lang>
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

# <lang>
#   <zh-CN>先创建本次证据目录，后续日志、JSON 和截图都被限制在该运行边界内。</zh-CN>
#   <en>Create the run evidence directory first so later logs, JSON files, and screenshots stay within this run boundary.</en>
# </lang>
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

# <lang>
#   <zh-CN>只创建发布父目录；真正的发布脚本仍写入带运行 ID 的 `$publishPath` 子目录。</zh-CN>
#   <en>Create only the publish parent directory; the actual publish script still writes to the run-id-specific `$publishPath` child directory.</en>
# </lang>
New-Item -ItemType Directory -Force -Path $PublishRoot | Out-Null

# <lang>
#   <zh-CN>向控制台显示低敏路径，方便人工定位本次证据和发布包而不打印配置内容。</zh-CN>
#   <en>Print low-sensitivity paths to the console so humans can locate this run's evidence and package without exposing configuration contents.</en>
# </lang>
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

    # <lang>
    #   <zh-CN>配置边界步骤使用 UTC 起点，便于与其它子步骤和 CI 日志按同一时区排序。</zh-CN>
    #   <en>The configuration-boundary step uses a UTC start time so it can be ordered with other child steps and CI logs in one time zone.</en>
    # </lang>
    $configStarted = (Get-Date).ToUniversalTime()

    # <lang>
    #   <zh-CN>配置边界证据固定写入运行目录，内容仅描述模板/路径存在性，不包含真实连接串或密钥值。</zh-CN>
    #   <en>Configuration-boundary evidence is fixed under the run directory and describes only template/path presence, not real connection-string or secret values.</en>
    # </lang>
    $configEvidencePath = Join-Path $runDirectory 'configuration-boundary-dry-run.json'

    # <lang>
    #   <zh-CN>生成 dry-run 配置证据对象，供摘要复用同一份内存结果而不是再次扫描路径。</zh-CN>
    #   <en>Generate the dry-run configuration evidence object so the summary reuses the same in-memory result instead of scanning paths again.</en>
    # </lang>
    $configEvidence = Write-ConfigurationBoundaryEvidence -OutputPath $configEvidencePath

    # <lang>
    #   <zh-CN>把配置边界 dry-run 作为必需步骤入账；若该证据缺失，近目标演练不能被视为完整。</zh-CN>
    #   <en>Record the configuration-boundary dry run as a required step; without this evidence the near-target rehearsal is incomplete.</en>
    # </lang>
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

    # <lang>
    #   <zh-CN>只输出仓库相对证据路径，避免控制台日志泄露机器绝对路径以外的敏感上下文。</zh-CN>
    #   <en>Print only the repository-relative evidence path to avoid leaking sensitive context beyond machine path shape in console logs.</en>
    # </lang>
    Write-Host ('[PASSED] Configuration boundary dry run -> {0}' -f (ConvertTo-RepoPath -Path $configEvidencePath))

    # <lang>
    #   <zh-CN>回滚 dry-run 使用独立 UTC 起点，避免与配置边界步骤混淆同一个 evidence span。</zh-CN>
    #   <en>The rollback dry run uses its own UTC start time so it is not confused with the configuration-boundary evidence span.</en>
    # </lang>
    $rollbackStarted = (Get-Date).ToUniversalTime()

    # <lang>
    #   <zh-CN>回滚证据固定写入运行目录，仅记录包路径、指南和待目标环境补证项，不执行真实回滚。</zh-CN>
    #   <en>Rollback evidence is fixed under the run directory and records only package path, guide, and pending target-environment evidence; it performs no real rollback.</en>
    # </lang>
    $rollbackEvidencePath = Join-Path $runDirectory 'rollback-dry-run.json'

    # <lang>
    #   <zh-CN>生成回滚 dry-run 证据对象，供最终摘要列出仍需真实 IIS/TLS/ACL/数据库验证的缺口。</zh-CN>
    #   <en>Generate the rollback dry-run evidence object so the final summary can list gaps that still require real IIS/TLS/ACL/database validation.</en>
    # </lang>
    $rollbackEvidence = Write-RollbackDryRunEvidence -OutputPath $rollbackEvidencePath -PackagePath $publishPath

    # <lang>
    #   <zh-CN>把回滚 dry-run 作为必需步骤入账；它证明回滚边界被记录，不证明目标环境可回滚。</zh-CN>
    #   <en>Record the rollback dry run as a required step; it proves rollback boundaries were documented, not that the target environment is recoverable.</en>
    # </lang>
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

    # <lang>
    #   <zh-CN>只输出回滚证据的仓库相对路径，保持控制台日志可引用但不扩散环境细节。</zh-CN>
    #   <en>Print only the repository-relative rollback evidence path so console logs remain referenceable without spreading environment details.</en>
    # </lang>
    Write-Host ('[PASSED] Rollback dry run -> {0}' -f (ConvertTo-RepoPath -Path $rollbackEvidencePath))

    # <lang>
    #   <zh-CN>BaseUrl 在进入网络探测前统一转换为 Uri，后续 scheme/host/port 判断都基于同一解析结果。</zh-CN>
    #   <en>Convert BaseUrl to a Uri before network probing so later scheme/host/port checks share one parsed representation.</en>
    # </lang>
    $baseUri = [Uri]$BaseUrl

    # <lang>
    #   <zh-CN>强制限定本地 HTTP，防止演练脚本被误用去探测真实站点、TLS 入口或生产代理链。</zh-CN>
    #   <en>Force local HTTP only so the rehearsal script cannot be misused to probe real sites, TLS endpoints, or production proxy paths.</en>
    # </lang>
    if ($baseUri.Scheme -ne 'http' -or $baseUri.Host -notin @('localhost', '127.0.0.1', '::1')) {
        throw 'P14.2 near-target rehearsal only starts IIS Express for local HTTP BaseUrl.'
    }

    # <lang>
    #   <zh-CN>端口探测决定是启动本脚本负责的 IIS Express，还是复用已经监听的本地服务。</zh-CN>
    #   <en>The port probe decides whether this script starts its own IIS Express process or reuses an already-listening local service.</en>
    # </lang>
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
        # <lang>
        #   <zh-CN>复用已有监听端口时仍记录跳过步骤，使摘要能解释没有启动新进程的原因。</zh-CN>
        #   <en>When reusing an existing listener, still record a skipped step so the summary explains why no new process was started.</en>
        # </lang>
        $skipStarted = (Get-Date).ToUniversalTime()

        # <lang>
        #   <zh-CN>跳过日志位于运行目录内，作为复用本地服务这个决策的可审计证据。</zh-CN>
        #   <en>The skipped-step log lives in the run directory as auditable evidence for the local-service reuse decision.</en>
        # </lang>
        $skipLogPath = Join-Path $runDirectory 'start-iisexpress.log.md'
        Write-Utf8NoBomFile -Path $skipLogPath -Content ("# Start IIS Express`r`n`r`nPort already listening; existing local server was reused.`r`n")

        # <lang>
        #   <zh-CN>复用本地服务仍是必需路径的一种通过形态；失败判断交给后续 smoke 步骤确认。</zh-CN>
        #   <en>Reusing a local service is still a valid required-path outcome; later smoke checks confirm whether it actually serves the portal.</en>
        # </lang>
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

        # <lang>
        #   <zh-CN>控制台只报告跳过证据路径，避免把监听进程详情误当成生产服务信息。</zh-CN>
        #   <en>The console reports only the skipped-step evidence path, avoiding confusion between local listener details and production service information.</en>
        # </lang>
        Write-Host ('[SKIPPED] Start IIS Express -> {0}' -f (ConvertTo-RepoPath -Path $skipLogPath))
    }

    Invoke-RehearsalStep `
        -Name 'Portal smoke after publish rehearsal' `
        -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalSmoke.ps1') `
        -Arguments @('-BaseUrl', $BaseUrl, '-SkipAuthenticated', '-CheckGenericErrorPage', '-CheckDocumentSafety', '-CheckEditorSafety') `
        -LogPath (Join-Path $runDirectory 'portal-smoke.log.md') `
        -Required $true

    if ($SkipThemeScreenshots) {
        # <lang>
        #   <zh-CN>主题截图被参数跳过时也记录 UTC 起点，使可选步骤的省略原因保留在摘要中。</zh-CN>
        #   <en>When theme screenshots are skipped by parameter, still record a UTC start time so the optional omission remains visible in the summary.</en>
        # </lang>
        $skipStarted = (Get-Date).ToUniversalTime()

        # <lang>
        #   <zh-CN>截图跳过日志写入运行目录，明确这是人工选择而不是 Playwright 或主题流程失败。</zh-CN>
        #   <en>The screenshot-skip log is written under the run directory to show this was an operator choice, not a Playwright or theme-flow failure.</en>
        # </lang>
        $skipLogPath = Join-Path $runDirectory 'theme-screenshots.log.md'
        Write-Utf8NoBomFile -Path $skipLogPath -Content "# Theme screenshots`r`n`r`nSkipped by parameter.`r`n"

        # <lang>
        #   <zh-CN>截图近似证据是可选步骤；跳过时不降低发布包、manifest 和 smoke 这些必需门禁。</zh-CN>
        #   <en>Approximate screenshot evidence is optional; skipping it does not weaken required package, manifest, and smoke gates.</en>
        # </lang>
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
        # <lang>
        #   <zh-CN>`pwsh -File` 不适合直接展开 string[] 参数，这里按主题拆成独立步骤，避免第二个主题误绑定到其它参数。</zh-CN>
        #   <en>`pwsh -File` does not safely expand string[] values here, so capture each theme in its own step.</en>
        # </lang>
        foreach ($themeName in @('EnterpriseLight', 'StateClassicLight')) {
            # <lang>
            #   <zh-CN>每个主题使用独立输出目录，避免截图文件互相覆盖并让失败可定位到具体主题。</zh-CN>
            #   <en>Each theme uses its own output directory to avoid screenshot overwrites and to make failures attributable to a specific theme.</en>
            # </lang>
            $themeOutput = Join-Path $screenshotOutput $themeName

            # <lang>
            #   <zh-CN>截图参数显式传递单个主题，保持子脚本接收的 `-Themes` 绑定与日志中的步骤名一致。</zh-CN>
            #   <en>Screenshot arguments pass one explicit theme so the child script's `-Themes` binding matches the logged step name.</en>
            # </lang>
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

# <lang>
#   <zh-CN>从最新 release manifest JSON 回读摘要字段；若 manifest 步骤失败或未产出文件，后续摘要保留空值而不是伪造清单。</zh-CN>
#   <en>Read summary fields back from the newest release-manifest JSON; if the manifest step failed or produced no file, the later summary keeps null/empty values instead of fabricating a manifest.</en>
# </lang>
$releaseManifestJson = @(Get-ChildItem -LiteralPath $releaseManifestRoot -Filter 'release-manifest.json' -File -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1)

# <lang>
#   <zh-CN>只在实际找到 manifest JSON 时解析对象；缺失时显式使用 null 传达证据缺口。</zh-CN>
#   <en>Parse a manifest object only when a JSON file is actually found; otherwise use null to communicate the evidence gap explicitly.</en>
# </lang>
$releaseManifest = if ($releaseManifestJson.Count -gt 0) {
    Get-Content -LiteralPath $releaseManifestJson[0].FullName -Raw -Encoding UTF8 | ConvertFrom-Json
}
else {
    $null
}

# <lang>
#   <zh-CN>必需失败集合决定默认退出门禁；可选失败集合仅保留诊断上下文，不应阻塞发布演练摘要生成。</zh-CN>
#   <en>The required-failure set drives the default exit gate; optional failures retain diagnostic context without blocking summary creation.</en>
# </lang>
$requiredFailures = @($steps | Where-Object { $_.Required -and $_.Status -eq 'Failed' })

# <lang>
#   <zh-CN>可选失败通常来自截图等近似证据，摘要保留它们但不把它们等同于核心发布失败。</zh-CN>
#   <en>Optional failures usually come from approximate evidence such as screenshots; the summary preserves them without equating them to core release failure.</en>
# </lang>
$optionalFailures = @($steps | Where-Object { -not $_.Required -and $_.Status -eq 'Failed' })
# <lang>
#   <zh-CN>汇总必需/可选步骤、发布清单和 PendingTargetEnvironment；本机演练不声明真实生产证据。</zh-CN>
#   <en>Summarize required/optional steps, release-manifest facts, and PendingTargetEnvironment; local rehearsal never claims real production evidence.</en>
# </lang>
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
# <lang>
#   <zh-CN>只向演练目录写入 JSON/Markdown 和内部 release entry；路径由脚本参数和仓库边界决定。</zh-CN>
#   <en>Write JSON/Markdown and the internal release entry only under rehearsal paths selected by parameters and repository boundaries.</en>
# </lang>
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

# <lang>
#   <zh-CN>必需步骤失败且未显式 AllowFailures 时返回失败；AllowFailures 只放宽演练退出，不改变证据含义。</zh-CN>
#   <en>Fail when required steps fail without explicit AllowFailures; AllowFailures relaxes rehearsal exit only, not evidence meaning.</en>
# </lang>
if ($requiredFailures.Count -gt 0 -and -not $AllowFailures) {
    throw ('P14.2 near-target release rehearsal contains required failed steps: {0}' -f $requiredFailures.Count)
}
