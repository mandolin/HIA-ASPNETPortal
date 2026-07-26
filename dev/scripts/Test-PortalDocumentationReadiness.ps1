<#
.SYNOPSIS
    Checks the Portal documentation toolchain readiness contract.

.DESCRIPTION
    中文：本脚本只读取仓库文件、Git 索引和可选的 HIA-Documentation-Sys 通知目录，检查公开文档、
    XML 文档、JSDoc/DotNetDoc pilot、生成目录边界、coverage 分层和通知读取机制是否处于可交接状态。
    它不改写源码注释、不执行 npm、不构建解决方案、不生成文档、不复制通知，也不访问数据库或网络。
    English: This script reads repository files, the Git index, and the optional HIA-Documentation-Sys notification
    directory to check whether public docs, XML docs, JSDoc/DotNetDoc pilots, generated-output boundaries, coverage tiers,
    and notification pull mechanics are ready for handoff. It never rewrites comments, runs npm, builds the solution,
    generates docs, copies notifications, or accesses databases or the network.
#>
[CmdletBinding()]
param(
    [string]$HiaDocumentationRoot,

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'
$trackedFiles = @(& git -C $repoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法检查文档化 readiness。'
}

if ([string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    $HiaDocumentationRoot = Join-Path (Split-Path -Parent $repoRoot) 'HIA-Documentation-Sys'
}

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

function Add-DocumentationCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'Pending')]
        [string]$Severity,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject][ordered]@{
            Severity = $Severity
            Code = $Code
            Message = $Message
            Evidence = $Evidence
        })

    Write-Host ('[{0}] {1}: {2}' -f $Severity.ToUpperInvariant(), $Code, $Message)
    if (-not [string]::IsNullOrWhiteSpace($Evidence)) {
        Write-Host ('       {0}' -f $Evidence)
    }
}

function Get-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    return [System.IO.File]::ReadAllText($LiteralPath, [System.Text.UTF8Encoding]::new($false))
}

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ($RelativePath -replace '/', '\')
}

function Test-TrackedPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return $trackedFiles -contains ($RelativePath -replace '\\', '/')
}

function Test-TextContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $path = Get-RepoPath -RelativePath $RelativePath
    return (Test-Path -LiteralPath $path -PathType Leaf) -and
        [regex]::IsMatch((Get-Utf8Text -LiteralPath $path), $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Get-TrackedCountUnder {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $normalized = ($RelativePath -replace '\\', '/').TrimEnd('/')
    $prefix = $normalized + '/'
    return @($trackedFiles | Where-Object {
            $_.Equals($normalized, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
        }).Count
}

Write-Host 'MODE: read-only documentation readiness check.'

$requiredScripts = @(
    'dev/scripts/Get-PortalDocumentationBaseline.ps1',
    'dev/scripts/Test-PortalXmlDocumentation.ps1',
    'dev/scripts/Build-PortalJsdocPilot.ps1',
    'dev/scripts/Build-PortalDotNetDocPilot.ps1',
    'dev/scripts/Get-HiaDocumentationNotifications.ps1',
    'dev/scripts/Test-PortalPublicDocumentation.ps1'
)
$missingScripts = @($requiredScripts | Where-Object {
        -not (Test-Path -LiteralPath (Get-RepoPath -RelativePath $_) -PathType Leaf) -or -not (Test-TrackedPath -RelativePath $_)
    })
if ($missingScripts.Count -eq 0) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-SCRIPTS' -Message 'Documentation baseline, XML, JSDoc, DotNetDoc, notification and public-doc scripts are present and tracked.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-SCRIPTS' -Message 'Required documentation scripts are missing or untracked.' -Evidence ($missingScripts -join '; ')
}

$docsGuideReady =
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Required') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Recommended') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Deferred') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Get-HiaDocumentationNotifications\.ps1') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'src/Documentation/') -and
    (Test-TextContains -RelativePath 'docs/README.md' -Pattern 'documentation-artifacts-guide\.md')
if ($docsGuideReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-PUBLIC-GUIDE' -Message 'Public documentation artifacts guide covers tiers, HIA notifications and generated-output boundaries.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-PUBLIC-GUIDE' -Message 'Public documentation artifacts guide needs P13.3 contract updates.'
}

$jsdocPackagePath = Get-RepoPath -RelativePath 'dev/documentation/jsdoc/package.json'
$jsdocConfigPath = Get-RepoPath -RelativePath 'dev/documentation/jsdoc/jsdoc.conf.json'
$jsdocPackageLockPath = Get-RepoPath -RelativePath 'dev/documentation/jsdoc/package-lock.json'
$jsdocReady = $false
if ((Test-Path -LiteralPath $jsdocPackagePath -PathType Leaf) -and
    (Test-Path -LiteralPath $jsdocConfigPath -PathType Leaf) -and
    (Test-Path -LiteralPath $jsdocPackageLockPath -PathType Leaf)) {
    $package = Get-Utf8Text -LiteralPath $jsdocPackagePath | ConvertFrom-Json
    $config = Get-Utf8Text -LiteralPath $jsdocConfigPath | ConvertFrom-Json
    $sourceIncludes = @($config.source.include)
    $destination = [string]$config.opts.destination
    $integrationOutput = [string]$config.opts.hia.integration.outputFile
    $jsdocReady =
        ($package.private -eq $true) -and
        ($null -ne $package.devDependencies.'@mandolin/jsdoc-plugin-hia-sys') -and
        ($null -ne $package.devDependencies.'@mandolin/jsdoc-theme-hia') -and
        ($sourceIncludes.Count -eq 1) -and
        ($sourceIncludes[0] -eq '../../../src/Portal/gulpfile.js') -and
        ($destination -eq '../../../temp/documentation/jsdoc') -and
        ($integrationOutput -eq '../../../temp/documentation/jsdoc/hia-integration.json')
}

if ($jsdocReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-JSDOC-PILOT' -Message 'JSDoc pilot is isolated, locked and writes only to temp/documentation/jsdoc.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-JSDOC-PILOT' -Message 'JSDoc pilot isolation, input or output contract needs review.'
}

$xmlReady =
    (Test-TextContains -RelativePath 'dev/scripts/Test-PortalXmlDocumentation.ps1' -Pattern 'Portal\.Components\.xml') -and
    (Test-TextContains -RelativePath 'dev/scripts/Test-PortalXmlDocumentation.ps1' -Pattern '不改写.*MSBuild|must not rewrite') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Test-PortalXmlDocumentation.ps1')
if ($xmlReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-XML-CONTRACT' -Message '.NET XML documentation verification remains standard XML output and does not rewrite MSBuild settings.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-XML-CONTRACT' -Message '.NET XML documentation boundary needs review.'
}

$dotnetDocPackagePath = Get-RepoPath -RelativePath 'dev/documentation/dotnetdoc/package.json'
$dotnetDocPackageLockPath = Get-RepoPath -RelativePath 'dev/documentation/dotnetdoc/package-lock.json'
$dotnetDocCheckerPath = Get-RepoPath -RelativePath 'dev/documentation/dotnetdoc/check-dotnetdoc-output.cjs'
$dotnetDocConfigPath = Get-RepoPath -RelativePath 'dotnetdoc.config.json'
$dotnetDocApiOnlyConfigPath = Get-RepoPath -RelativePath 'dotnetdoc.api-only.config.json'
$dotnetDocSourceProbeConfigPath = Get-RepoPath -RelativePath 'dotnetdoc.source-probe.config.json'
$dotnetDocReady = $false
if ((Test-Path -LiteralPath $dotnetDocPackagePath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocPackageLockPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocCheckerPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocConfigPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocApiOnlyConfigPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocSourceProbeConfigPath -PathType Leaf)) {
    $package = Get-Utf8Text -LiteralPath $dotnetDocPackagePath | ConvertFrom-Json
    $config = Get-Utf8Text -LiteralPath $dotnetDocConfigPath | ConvertFrom-Json
    $dotnetDocReady =
        ($package.private -eq $true) -and
        ($package.devDependencies.'@hia-doc/dotnetdoc-runner' -eq '0.1.3') -and
        ($package.overrides.'fast-xml-parser' -eq '5.7.0') -and
        ([string]$config.outputDirectory -eq 'temp/documentation/dotnetdoc') -and
        (@($config.inputs).Count -gt 0) -and
        (Test-TextContains -RelativePath 'dev/scripts/Build-PortalDotNetDocPilot.ps1' -Pattern 'temp\\documentation') -and
        (Test-TextContains -RelativePath 'dev/documentation/dotnetdoc/check-dotnetdoc-output.cjs' -Pattern 'sourcesContent') -and
        (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'DotNetDoc pilot')
}

if ($dotnetDocReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-DOTNETDOC-PILOT' -Message 'DotNetDoc pilot is isolated, locked, writes only to temp/documentation/dotnetdoc, and has a source-probe fallback boundary.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-DOTNETDOC-PILOT' -Message 'DotNetDoc pilot isolation, input, output, audit override or checker contract needs review.'
}

$generatedBoundaries = @(
    'src/Documentation',
    'src/DoxyGen',
    'src/Portal.Components.Data/Documentation',
    'src/Portal/Documentation',
    'src/Portal.shfbproj'
)
$trackedGenerated = New-Object 'System.Collections.Generic.List[string]'
foreach ($boundary in $generatedBoundaries) {
    if (Get-TrackedCountUnder -RelativePath $boundary) {
        $trackedGenerated.Add($boundary)
    }
}
if ($trackedGenerated.Count -eq 0) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-GENERATED-BOUNDARY' -Message 'Known generated or historical documentation output paths are not tracked.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-GENERATED-BOUNDARY' -Message 'Generated or historical documentation output paths are tracked.' -Evidence ($trackedGenerated -join '; ')
}

$notifyRoot = Join-Path $HiaDocumentationRoot 'work-zone\notify'
if (-not (Test-Path -LiteralPath $notifyRoot -PathType Container)) {
    Add-DocumentationCheck -Severity Pending -Code 'DOC-HIA-NOTIFY-SOURCE' -Message 'HIA-Documentation-Sys notify source is not available on this machine.' -Evidence $notifyRoot
}
else {
    $notifications = @(Get-ChildItem -LiteralPath $notifyRoot -Recurse -File -Filter '*.md' | Where-Object { $_.Name -ne 'README.md' })
    $severity = if ($notifications.Count -gt 0) { 'Pass' } else { 'Warning' }
    Add-DocumentationCheck -Severity $severity -Code 'DOC-HIA-NOTIFY-SOURCE' -Message ('HIA-Documentation-Sys notify source is readable. Notifications={0}; ContentCopied=False' -f $notifications.Count) -Evidence $notifyRoot
}

Add-DocumentationCheck -Severity Pass -Code 'DOC-NO-CODE-REWRITE' -Message 'Readiness completed without rewriting source comments, generating docs, copying notifications, or changing dependencies.'

$summary = [pscustomobject][ordered]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    HiaDocumentationRoot = $HiaDocumentationRoot
    Checks = $checks
    TotalChecks = $checks.Count
    FailedChecks = @($checks | Where-Object { $_.Severity -eq 'Fail' }).Count
    WarningChecks = @($checks | Where-Object { $_.Severity -eq 'Warning' }).Count
    PendingChecks = @($checks | Where-Object { $_.Severity -eq 'Pending' }).Count
}

$summary

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

if ($summary.FailedChecks -gt 0 -or ($FailOnWarning -and $summary.WarningChecks -gt 0)) {
    exit 1
}

exit 0
