<#
.SYNOPSIS
    Generates a release manifest for a filesystem publish package.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本扫描已经生成的 FileSystem 发布目录，输出文件清单、SHA256、版本信息、排除项检查和门禁引用。它不连接 IIS、不连接数据库、不读取外置敏感配置，也不会把真实密码、连接串、Token、Cookie 或证书私钥写入证据。</zh-CN>
      <en>This script scans an existing filesystem publish directory and writes a file inventory, SHA256 hashes, version metadata, exclusion checks, and optional gate-result references. It does not connect to IIS or databases, read external secret configuration, or write real passwords, connection strings, tokens, cookies, or certificate private keys into evidence.</en>
    </lang>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [string]$OutputRoot,

    [string]$ReleaseId,

    [string[]]$GateResultPath = @(),

    [switch]$TreatWarningsAsErrors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p13.1'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p13.1'
    }
}

if ([string]::IsNullOrWhiteSpace($ReleaseId)) {
    $ReleaseId = 'P13.1-' + (Get-Date).ToString('yyyyMMdd-HHmmss')
}

$packageRoot = Resolve-Path -LiteralPath $PackagePath
if (-not (Test-Path -LiteralPath $packageRoot.Path -PathType Container)) {
    throw "PackagePath must be an existing directory: $PackagePath"
}

$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) $runId
$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入生成物；目录创建是输出准备动作，不代表发布包或真实环境已经存在。</zh-CN>
#   <en>Writes generated artifacts as UTF-8 without a BOM; directory creation prepares output only and does not imply that a release package or real environment exists.</en>
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
#   <zh-CN>追加结构化门禁检查并输出低敏状态行；Evidence 由调用方控制，不在此处采集秘密值。</zh-CN>
#   <en>Adds a structured gate check and emits a low-sensitivity status line; callers control Evidence and this helper does not collect secret values.</en>
# </lang>
function Add-ReleaseManifestCheck {
    param(
        [string]$Code,
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Status,
        [string]$Message,
        [object]$Evidence = $null
    )

    $checks.Add([pscustomobject]@{
            Code = $Code
            Status = $Status
            Message = $Message
            Evidence = $Evidence
        })

    Write-Host ('[{0}] {1}: {2}' -f $Status.ToUpperInvariant(), $Code, $Message)
}

# <lang>
#   <zh-CN>读取仓库元数据的兼容 helper；git 失败统一返回空值，不能把失败解释为仓库干净或发布已验证。</zh-CN>
#   <en>Reads repository metadata through a compatibility helper; git failures return null and must not be interpreted as a clean repository or validated release.</en>
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
#   <zh-CN>对已解析的本地文件流计算 SHA256；只返回哈希，不把文件内容写入清单或日志。</zh-CN>
#   <en>Computes SHA256 over a resolved local file stream; returns only the hash and never writes file contents to the manifest or log.</en>
# </lang>
function Get-FileSha256 {
    param([string]$Path)

    $hash = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            $bytes = $hash.ComputeHash($stream)
            return [System.BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $hash.Dispose()
    }
}

# <lang>
#   <zh-CN>按不区分大小写的正则检查发布相对路径；命中只表示路径策略信号，不执行删除或隔离。</zh-CN>
#   <en>Checks a release-relative path against case-insensitive regular expressions; a match is a policy signal and does not delete or quarantine anything.</en>
# </lang>
function Test-ReleasePathPattern {
    param(
        [string]$RelativePath,
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ($RelativePath -imatch $pattern) {
            return $true
        }
    }

    return $false
}

# <lang>
#   <zh-CN>按扩展名筛选可做低敏文本信号检查的候选文件；筛选不读取文件内容。</zh-CN>
#   <en>Selects text-file candidates for low-sensitivity signal checks by extension; selection alone does not read file contents.</en>
# </lang>
function Test-TextFileCandidate {
    param([string]$Path)

    $extensions = @(
        '.asax', '.ascx', '.aspx', '.ashx', '.config', '.css', '.htm', '.html',
        '.json', '.js', '.map', '.md', '.txt', '.xml', '.xsd', '.sql'
    )

    return $extensions -contains ([System.IO.Path]::GetExtension($Path).ToLowerInvariant())
}

# <lang>
#   <zh-CN>把包内绝对路径转换为使用正斜杠的相对路径，作为清单和证据的路径标识。</zh-CN>
#   <en>Converts an in-package absolute path to a forward-slash relative path for manifest and evidence identifiers.</en>
# </lang>
function Get-RelativePathForManifest {
    param(
        [string]$Root,
        [string]$Path
    )

    return ([System.IO.Path]::GetRelativePath($Root, $Path) -replace '\\', '/')
}

# <lang>
#   <zh-CN>以下阶段只读取已存在的发布包并生成清单；输出目录可以创建，但不会修改发布包内容。</zh-CN>
#   <en>The following stage reads an existing publish package and generates a manifest; the output directory may be created, but package contents are not modified.</en>
# </lang>
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

$packageRootPath = $packageRoot.Path
$repositoryCommit = Get-GitValue -Arguments @('rev-parse', '--short', 'HEAD')
$repositoryBranch = Get-GitValue -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')
$repositoryStatusOutput = Get-GitValue -Arguments @('status', '--short')
$repositoryStatusLines = @()
if (-not [string]::IsNullOrWhiteSpace($repositoryStatusOutput)) {
    $repositoryStatusLines = @($repositoryStatusOutput -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$packageFiles = @(Get-ChildItem -LiteralPath $packageRootPath -File -Recurse | Sort-Object FullName)
$fileEntries = New-Object 'System.Collections.Generic.List[object]'
$totalBytes = [int64]0
foreach ($file in $packageFiles) {
    $relativePath = Get-RelativePathForManifest -Root $packageRootPath -Path $file.FullName
    $totalBytes += $file.Length
    $fileEntries.Add([pscustomobject]@{
            Path = $relativePath
            Bytes = $file.Length
            Sha256 = Get-FileSha256 -Path $file.FullName
        })
}

Add-ReleaseManifestCheck -Code 'PACKAGE-EXISTS' -Status 'Pass' -Message $packageRootPath
Add-ReleaseManifestCheck -Code 'PACKAGE-FILE-COUNT' -Status 'Info' -Message ("Files={0}; Bytes={1}" -f $fileEntries.Count, $totalBytes)

$pathSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $fileEntries) {
    [void]$pathSet.Add($entry.Path)
}

$requiredFiles = @(
    'Web.config',
    'Global.asax',
    'Default.aspx',
    'DesktopDefault.aspx',
    'GenericErrorPage.aspx',
    'Config/Web.config',
    'Config/Templates/connectionStrings.config',
    'Admin/SystemHealth.aspx',
    'Admin/ThemeSettings.aspx',
    'Admin/ModuleCatalog.aspx',
    'DesktopModules/Signin.ascx',
    'App_Themes/EnterpriseLight/Default.css',
    'App_Themes/StateClassicLight/Default.css',
    'App_Themes/OaDark/theme.json',
    'bin/Portal.dll'
)

$missingRequired = @($requiredFiles | Where-Object { -not $pathSet.Contains($_) })
if ($missingRequired.Count -eq 0) {
    Add-ReleaseManifestCheck -Code 'REQUIRED-FILES' -Status 'Pass' -Message 'Core runtime files, templates, themes and Portal.dll are present.'
}
else {
    Add-ReleaseManifestCheck -Code 'REQUIRED-FILES' -Status 'Fail' -Message 'Required files missing.' -Evidence ($missingRequired | Select-Object -First 20)
}

$forbiddenPatterns = @(
    '^\.git(/|$)',
    '^\.vs(/|$)',
    '^\.vscode(/|$)',
    '^work-zone(/|$)',
    '^ai(/|$)',
    '^temp(/|$)',
    '^dev(/|$)',
    '^src(/|$)',
    '^node_modules(/|$)',
    '^obj(/|$)',
    '^Documentation(/|$)',
    '^DoxyGen(/|$)',
    '^Demo(/|$)',
    '^Uploads/(?!web\.config$)',
    '^Web\.(Debug|Release|Test)\.config$',
    '^Config/(appSettings\.(dev|test|prod)\.json|UnityCfg\.(dev|test|prod)\.xml)$',
    '\.(pfx|p12|pem|key)$'
)

$forbiddenFiles = New-Object 'System.Collections.Generic.List[string]'
foreach ($entry in $fileEntries) {
    $relativePath = [string]$entry.Path
    $isExternalConnectionFile = $relativePath -imatch '(^|/)connectionStrings\.config$' -and $relativePath -ine 'Config/Templates/connectionStrings.config'
    if ($isExternalConnectionFile -or (Test-ReleasePathPattern -RelativePath $relativePath -Patterns $forbiddenPatterns)) {
        $forbiddenFiles.Add($relativePath)
    }
}

if ($forbiddenFiles.Count -eq 0) {
    Add-ReleaseManifestCheck -Code 'FORBIDDEN-PATHS' -Status 'Pass' -Message 'No WorkZone, temp, dev, source, upload, environment config, transform or private-key paths are present.'
}
else {
    Add-ReleaseManifestCheck -Code 'FORBIDDEN-PATHS' -Status 'Fail' -Message 'Forbidden package paths were found.' -Evidence ($forbiddenFiles | Select-Object -First 30)
}

$warningPatterns = @(
    '\.pdb$',
    '^bin/.*\.(xml)$',
    '^App_Data/Logs(/|$)'
)
$warningFiles = @($fileEntries |
    ForEach-Object { [string]$_.Path } |
    Where-Object { Test-ReleasePathPattern -RelativePath $_ -Patterns $warningPatterns })

if ($warningFiles.Count -eq 0) {
    Add-ReleaseManifestCheck -Code 'REVIEW-PATHS' -Status 'Pass' -Message 'No debug symbol, XML doc or runtime log paths require release review.'
}
else {
    Add-ReleaseManifestCheck -Code 'REVIEW-PATHS' -Status 'Warning' -Message 'Review optional release paths.' -Evidence ($warningFiles | Select-Object -First 30)
}

# <lang>
#   <zh-CN>敏感信号扫描只收集相对路径，不收集匹配行或值；它是发布前提示，不是秘密检测的完整保证。</zh-CN>
#   <en>The sensitive-signal scan collects relative paths only, never matching lines or values; it is a release warning and not a complete secret-detection guarantee.</en>
# </lang>
$secretPattern = '(?i)(password|passwd|pwd|secret|token|privateKey|connectionString)\s*[=:]'
$secretSignalFiles = New-Object 'System.Collections.Generic.List[string]'
foreach ($file in $packageFiles) {
    if (-not (Test-TextFileCandidate -Path $file.FullName)) {
        continue
    }

    try {
        $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
        if ($text -match $secretPattern) {
            $secretSignalFiles.Add((Get-RelativePathForManifest -Root $packageRootPath -Path $file.FullName))
        }
    }
    catch {
        # <lang>
        #   <zh-CN>无法按 UTF-8 读取的文本候选不记录内容，避免把二进制或未知编码误写入证据。</zh-CN>
        #   <en>If a text candidate cannot be read as UTF-8, do not record its content in evidence.</en>
        # </lang>
    }
}

if ($secretSignalFiles.Count -eq 0) {
    Add-ReleaseManifestCheck -Code 'SENSITIVE-CONTENT-SIGNALS' -Status 'Pass' -Message 'No simple key=value secret signals were found in text candidates.'
}
else {
    Add-ReleaseManifestCheck -Code 'SENSITIVE-CONTENT-SIGNALS' -Status 'Warning' -Message 'Potential secret-key text signals found; paths only are recorded and values are not captured.' -Evidence ($secretSignalFiles | Select-Object -First 30)
}

# <lang>
#   <zh-CN>门禁引用只保存已解析文件的路径、大小和哈希；不存在的引用产生 Warning，不会伪造通过结果。</zh-CN>
#   <en>Gate references retain resolved file paths, sizes, and hashes only; missing references produce a warning and never fabricate a pass result.</en>
# </lang>
$gateResults = New-Object 'System.Collections.Generic.List[object]'
foreach ($gatePath in $GateResultPath) {
    if ([string]::IsNullOrWhiteSpace($gatePath)) {
        continue
    }

    $resolvedGate = Resolve-Path -LiteralPath $gatePath -ErrorAction SilentlyContinue
    if ($null -eq $resolvedGate) {
        Add-ReleaseManifestCheck -Code 'GATE-RESULT-REFERENCE' -Status 'Warning' -Message "Gate result not found: $gatePath"
        continue
    }

    foreach ($item in $resolvedGate) {
        if (-not (Test-Path -LiteralPath $item.Path -PathType Leaf)) {
            continue
        }

        $gateFile = Get-Item -LiteralPath $item.Path
        $gateResults.Add([pscustomobject]@{
                Path = $gateFile.FullName
                Bytes = $gateFile.Length
                Sha256 = Get-FileSha256 -Path $gateFile.FullName
            })
    }
}

if ($gateResults.Count -gt 0) {
    Add-ReleaseManifestCheck -Code 'GATE-RESULTS' -Status 'Info' -Message ("Gate result references recorded: {0}" -f $gateResults.Count)
}

$failedCount = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
$warningCount = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count

# <lang>
#   <zh-CN>清单输出包含路径、大小、哈希、门禁引用和状态，不包含敏感值；失败检查或 TreatWarningsAsErrors 会通过退出码阻止成功。</zh-CN>
#   <en>The manifest contains paths, sizes, hashes, gate references, and statuses but no secret values; failed checks or TreatWarningsAsErrors prevent success through the exit code.</en>
# </lang>
$manifest = [pscustomobject][ordered]@{
    SchemaVersion = 'p13.1.release-manifest.v1'
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    ReleaseId = $ReleaseId
    Repository = [pscustomobject][ordered]@{
        Root = $repoRoot
        Commit = $repositoryCommit
        Branch = $repositoryBranch
        Status = [pscustomobject][ordered]@{
            IsDirty = $repositoryStatusLines.Count -gt 0
            EntryCount = $repositoryStatusLines.Count
        }
    }
    Package = [pscustomobject][ordered]@{
        Root = $packageRootPath
        FileCount = $fileEntries.Count
        TotalBytes = $totalBytes
        Files = $fileEntries
    }
    GateResults = $gateResults
    Checks = $checks
    Summary = [pscustomobject][ordered]@{
        FailedChecks = $failedCount
        WarningChecks = $warningCount
        TreatWarningsAsErrors = [bool]$TreatWarningsAsErrors
    }
}

$jsonPath = Join-Path $runDirectory 'release-manifest.json'
$markdownPath = Join-Path $runDirectory 'release-manifest.md'
Write-Utf8NoBomFile -Path $jsonPath -Content (($manifest | ConvertTo-Json -Depth 8) + [Environment]::NewLine)

$markdownLines = @(
    '# Portal Release Manifest',
    '',
    ('ReleaseId: `{0}`' -f $ReleaseId),
    ('Generated UTC: `{0}`' -f $manifest.GeneratedAtUtc),
    ('Package: `{0}`' -f $packageRootPath),
    ('Repository commit: `{0}`' -f $repositoryCommit),
    '',
    '## Summary',
    '',
    ('Files: `{0}`' -f $fileEntries.Count),
    ('Bytes: `{0}`' -f $totalBytes),
    ('Failed checks: `{0}`' -f $failedCount),
    ('Warning checks: `{0}`' -f $warningCount),
    '',
    '## Checks',
    '',
    '| Code | Status | Message |',
    '| --- | --- | --- |'
)

foreach ($check in $checks) {
    $message = ([string]$check.Message) -replace '\|', '/'
    $markdownLines += ('| {0} | {1} | {2} |' -f $check.Code, $check.Status, $message)
}

$markdownLines += @(
    '',
    '## Evidence Boundary',
    '',
    '1. The JSON manifest records file paths, byte counts, hashes, gate-result references and check outcomes.',
    '2. Secret-like text checks only record path-level signals; values are never captured in this manifest.',
    '3. Real IIS, database migration, TLS, ACL and production configuration evidence must be recorded separately after target-environment validation.',
    '',
    ('JSON: `{0}`' -f (Split-Path -Leaf $jsonPath))
)

Write-Utf8NoBomFile -Path $markdownPath -Content (($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('MANIFEST JSON: {0}' -f $jsonPath)
Write-Host ('MANIFEST MD: {0}' -f $markdownPath)
Write-Host ('SUMMARY: Files={0}; Failed={1}; Warning={2}' -f $fileEntries.Count, $failedCount, $warningCount)

if ($failedCount -gt 0 -or ($TreatWarningsAsErrors -and $warningCount -gt 0)) {
    exit 1
}

exit 0
