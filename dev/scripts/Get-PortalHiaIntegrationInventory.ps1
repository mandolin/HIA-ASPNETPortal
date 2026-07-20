<#
.SYNOPSIS
生成 Portal 与 HIA 外围集成边界的只读盘点。
Generates a read-only inventory of the Portal/HIA peripheral integration boundary.

.DESCRIPTION
本脚本只读取仓库内已追踪契约、proof、文档和可选的 HIA-Documentation-Sys 通知目录。
它不连接数据库、不加载外部程序集、不发起网络请求、不复制通知内容，也不读取连接串或凭据。
This script reads only tracked contract, proof, documentation, and the optional HIA-Documentation-Sys
notification directory. It does not connect to databases, load external assemblies, call networks,
copy notification content, or read connection strings or credentials.
#>
[CmdletBinding()]
param(
    [string]$DocumentationSysRoot,

    [string]$OutputJson,

    [ValidateRange(1, 50)]
    [int]$LatestNotifications = 10,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($DocumentationSysRoot)) {
    $DocumentationSysRoot = Join-Path (Split-Path -Parent $repoRoot) 'HIA-Documentation-Sys'
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-RepoRelativePath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return ($fullPath -replace '\\', '/')
}

function Get-ChildRelativePath {
    param(
        [string]$RootPath,
        [string]$ChildPath
    )

    $rootPrefix = [System.IO.Path]::GetFullPath($RootPath).TrimEnd('\') + '\'
    $childFullPath = [System.IO.Path]::GetFullPath($ChildPath)
    if ($childFullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($childFullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return [System.IO.Path]::GetFileName($ChildPath)
}

function New-Check {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'Pending')]
        [string]$Status,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    [pscustomobject]@{
        Status = $Status
        Code = $Code
        Message = $Message
        Evidence = $Evidence
    }
}

function Read-TextFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }

    return [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
}

function Get-NotificationTitle {
    param(
        [string]$Content,
        [string]$FallbackName
    )

    $titleLine = $Content -split "`r?`n" | Where-Object { $_ -match '^\s*#\s+' } | Select-Object -First 1
    if ($null -ne $titleLine) {
        return ($titleLine -replace '^\s*#\s+', '').Trim()
    }

    return [System.IO.Path]::GetFileNameWithoutExtension($FallbackName)
}

function Get-TrackedPathExists {
    param([string]$RelativePath)

    $tracked = @(git -C $repoRoot ls-files -- $RelativePath 2>$null)
    return $tracked -contains ($RelativePath -replace '\\', '/')
}

$checks = New-Object 'System.Collections.Generic.List[object]'
$contractPath = Join-Path $repoRoot 'src/Portal.Components/PortalHiaBoundaryContracts.cs'
$proofProjectPath = Join-Path $repoRoot 'src/Portal.HiaBoundaryProof/Portal.HiaBoundaryProof.csproj'
$proofScriptPath = Join-Path $repoRoot 'dev/scripts/Test-PortalHiaBoundary.ps1'
$fixtureDirectory = Join-Path $repoRoot 'src/Portal.HiaBoundaryProof/Fixtures'
$draftFixtureDirectory = Join-Path $fixtureDirectory 'Draft'
$notificationScriptPath = Join-Path $repoRoot 'dev/scripts/Get-HiaDocumentationNotifications.ps1'
$adrPath = Join-Path $repoRoot 'work-zone/docs/adr/0015-hia-peripheral-contract-and-independent-boundary.md'
$masterSolutionPath = Join-Path $repoRoot 'src/master.sln'
$notifyRoot = Join-Path $DocumentationSysRoot 'work-zone/notify'

$contractText = Read-TextFile -Path $contractPath
$masterSolutionText = Read-TextFile -Path $masterSolutionPath

$contractTracked = Get-TrackedPathExists -RelativePath 'src/Portal.Components/PortalHiaBoundaryContracts.cs'
$proofTracked = Get-TrackedPathExists -RelativePath 'src/Portal.HiaBoundaryProof/Portal.HiaBoundaryProof.csproj'
$proofScriptTracked = Get-TrackedPathExists -RelativePath 'dev/scripts/Test-PortalHiaBoundary.ps1'

$contractVersionMatched = $contractText -match 'CurrentContractVersion\s*=\s*"0\.1\.0-draft"' -and
    $contractText -match 'ContractName\s*=\s*"hia\.portal\.peripheral"'
$checks.Add((New-Check -Status $(if ((Test-Path -LiteralPath $contractPath -PathType Leaf) -and $contractTracked -and $contractVersionMatched) { 'Pass' } else { 'Fail' }) -Code 'HIA-CONTRACT-DRAFT' -Message 'HIA peripheral draft contract file and version marker are present.' -Evidence 'hia.portal.peripheral@0.1.0-draft'))

$allowedKinds = @(
    'portal.module-capability',
    'portal.theme-capability',
    'portal.setting-capability',
    'portal.health-capability',
    'portal.diagnostic-reference'
)

$missingKinds = @($allowedKinds | Where-Object { $contractText -notmatch [regex]::Escape($_) })
$checks.Add((New-Check -Status $(if ($missingKinds.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'HIA-CAPABILITY-KINDS' -Message 'Current runtime contract keeps the approved read-only capability kinds.' -Evidence $(if ($missingKinds.Count -eq 0) { ($allowedKinds -join '; ') } else { ($missingKinds -join '; ') })))

$prohibitedFragments = @('password', 'secret', 'token', 'cookie', 'connectionstring', 'physicalpath', 'absolutepath', 'filepath', 'clientip', 'useragent', 'username', 'userid', 'email', 'role', 'audit', 'stacktrace', 'exceptiondetail')
$missingProhibitedFragments = @($prohibitedFragments | Where-Object { $contractText -notmatch [regex]::Escape($_) })
$checks.Add((New-Check -Status $(if ($missingProhibitedFragments.Count -eq 0) { 'Pass' } else { 'Warning' }) -Code 'HIA-SENSITIVE-FIELD-GUARD' -Message 'Contract validator keeps the current prohibited sensitive field fragments.' -Evidence ('Guarded fragments: {0}; Missing from guard: {1}' -f ($prohibitedFragments.Count - $missingProhibitedFragments.Count), $missingProhibitedFragments.Count)))

$fixtureFiles = @()
if (Test-Path -LiteralPath $fixtureDirectory -PathType Container) {
    $fixtureFiles = @(Get-ChildItem -LiteralPath $fixtureDirectory -File -Filter '*.json' | Sort-Object Name)
}

$validFixtureCount = @($fixtureFiles | Where-Object { $_.Name.StartsWith('valid-', [System.StringComparison]::OrdinalIgnoreCase) }).Count
$invalidFixtureCount = @($fixtureFiles | Where-Object { $_.Name.StartsWith('invalid-', [System.StringComparison]::OrdinalIgnoreCase) }).Count
$checks.Add((New-Check -Status $(if ($fixtureFiles.Count -ge 9 -and $validFixtureCount -ge 5 -and $invalidFixtureCount -ge 4) { 'Pass' } else { 'Fail' }) -Code 'HIA-FIXTURE-COVERAGE' -Message 'HIA proof fixtures cover valid descriptors and rejected privacy/version cases.' -Evidence ('Fixtures={0}; Valid={1}; Invalid={2}' -f $fixtureFiles.Count, $validFixtureCount, $invalidFixtureCount)))

$draftFixtureFiles = @()
if (Test-Path -LiteralPath $draftFixtureDirectory -PathType Container) {
    $draftFixtureFiles = @(Get-ChildItem -LiteralPath $draftFixtureDirectory -File -Filter '*.sample.json' | Sort-Object Name)
}

$checks.Add((New-Check -Status $(if ($draftFixtureFiles.Count -ge 4) { 'Pass' } else { 'Warning' }) -Code 'HIA-DRAFT-FIXTURES' -Message 'P11.4 candidate integration layers have draft sample fixtures outside the runtime validator set.' -Evidence ('DraftFixtures={0}; RuntimeAccepted=False' -f $draftFixtureFiles.Count)))

$proofIsIsolated = (Test-Path -LiteralPath $proofProjectPath -PathType Leaf) -and
    $proofTracked -and
    $proofScriptTracked -and
    ($masterSolutionText -notmatch 'Portal\.HiaBoundaryProof')
$checks.Add((New-Check -Status $(if ($proofIsIsolated) { 'Pass' } else { 'Fail' }) -Code 'HIA-PROOF-ISOLATED' -Message 'HIA proof project exists, is tracked, and remains outside the main solution.' -Evidence 'No HIA runtime dependency is introduced into src/master.sln.'))

$notificationScriptExists = (Test-Path -LiteralPath $notificationScriptPath -PathType Leaf) -and (Get-TrackedPathExists -RelativePath 'dev/scripts/Get-HiaDocumentationNotifications.ps1')
$checks.Add((New-Check -Status $(if ($notificationScriptExists) { 'Pass' } else { 'Fail' }) -Code 'HIA-DOC-NOTIFY-READER' -Message 'Target-side HIA-Documentation-Sys notification reader exists.' -Evidence 'dev/scripts/Get-HiaDocumentationNotifications.ps1'))

$notificationFiles = @()
if (Test-Path -LiteralPath $notifyRoot -PathType Container) {
    $notificationFiles = @(Get-ChildItem -LiteralPath $notifyRoot -Recurse -File -Filter '*.md' | Where-Object { $_.Name -ne 'README.md' } | Sort-Object LastWriteTime -Descending)
}

$checks.Add((New-Check -Status $(if ($notificationFiles.Count -gt 0) { 'Pass' } elseif (Test-Path -LiteralPath $notifyRoot -PathType Container) { 'Warning' } else { 'Pending' }) -Code 'HIA-DOC-NOTIFY-SOURCE' -Message 'HIA-Documentation-Sys WorkZone notify source is readable when available on this machine.' -Evidence ('Notifications={0}; ContentCopied=False' -f $notificationFiles.Count)))

$latestNotificationSummaries = @(
    $notificationFiles |
        Select-Object -First $LatestNotifications |
        ForEach-Object {
            $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.UTF8Encoding]::new($false))
            [pscustomobject]@{
                LastWriteTime = $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture)
                Title = Get-NotificationTitle -Content $content -FallbackName $_.Name
                MentionsCurrentProject = $content -match 'HIA-ASPNETPortal'
                RelativePath = Get-ChildRelativePath -RootPath $notifyRoot -ChildPath $_.FullName
            }
        }
)

$adrExists = Test-Path -LiteralPath $adrPath -PathType Leaf
$checks.Add((New-Check -Status $(if ($adrExists) { 'Pass' } else { 'Warning' }) -Code 'HIA-ADR-BASELINE' -Message 'Existing P3.4 HIA independent-boundary ADR is available as the P11.4 baseline.' -Evidence 'work-zone/docs/adr/0015-hia-peripheral-contract-and-independent-boundary.md'))

$contractLayers = @(
    [pscustomobject]@{ Layer = 'Identity'; Candidate = 'Portal user to HIA SysUser/BizUser mapping'; Direction = 'DesignOnly'; P11_4Boundary = 'No replacement of Portal users; no transport; no credential exchange.' }
    [pscustomobject]@{ Layer = 'EmployeeOrganization'; Candidate = 'Employees, organizations, and user profiles'; Direction = 'ImportOrReferenceDraft'; P11_4Boundary = 'First candidate for read/import mapping; no external write contract.' }
    [pscustomobject]@{ Layer = 'ExtensionMetadata'; Candidate = 'Module packages, theme packages, and settings registry'; Direction = 'ReadOnlyDescriptorDraft'; P11_4Boundary = 'Second candidate; trusted deployment remains local.' }
    [pscustomobject]@{ Layer = 'DiagnosticsEvidence'; Candidate = 'Diagnostics, audits, and compliance evidence summaries'; Direction = 'ReferenceOnlyDraft'; P11_4Boundary = 'Expose summaries and references only; no bodies, paths, connection strings, or sensitive fields.' }
    [pscustomobject]@{ Layer = 'DocumentationNotify'; Candidate = 'HIA-Documentation-Sys WorkZone notifications'; Direction = 'TargetPull'; P11_4Boundary = 'Target project reads upstream notify directory; no dev/notify push revival.' }
)

$statusSummary = [ordered]@{
    Pass = @($checks | Where-Object { $_.Status -eq 'Pass' }).Count
    Warning = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count
    Fail = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
    Info = @($checks | Where-Object { $_.Status -eq 'Info' }).Count
    Pending = @($checks | Where-Object { $_.Status -eq 'Pending' }).Count
}

$result = [pscustomobject]@{
    GeneratedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [System.Globalization.CultureInfo]::InvariantCulture)
    Contract = 'hia.portal.peripheral@0.1.0-draft'
    Counts = $statusSummary
    ContractFile = Get-RepoRelativePath -Path $contractPath
    ProofProject = Get-RepoRelativePath -Path $proofProjectPath
    ProofScript = Get-RepoRelativePath -Path $proofScriptPath
    DraftFixtureDirectory = Get-RepoRelativePath -Path $draftFixtureDirectory
    NotificationReader = Get-RepoRelativePath -Path $notificationScriptPath
    DocumentationNotifyAvailable = Test-Path -LiteralPath $notifyRoot -PathType Container
    NotificationContentCopied = $false
    ContractLayers = $contractLayers
    LatestNotifications = $latestNotificationSummaries
    Checks = $checks
}

foreach ($check in $checks) {
    Write-Host ('[{0}] {1}: {2}' -f $check.Status.ToUpperInvariant(), $check.Code, $check.Message)
    if (-not [string]::IsNullOrWhiteSpace($check.Evidence)) {
        Write-Host ('       {0}' -f $check.Evidence)
    }
}

Write-Host ('SUMMARY: Pass={0}; Warning={1}; Fail={2}; Info={3}; Pending={4}' -f $statusSummary.Pass, $statusSummary.Warning, $statusSummary.Fail, $statusSummary.Info, $statusSummary.Pending)

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $json = $result | ConvertTo-Json -Depth 8
    Write-Utf8NoBomFile -Path $OutputJson -Content ($json + [Environment]::NewLine)
    Write-Host ('Wrote JSON: {0}' -f $OutputJson)
}

if ($statusSummary.Fail -gt 0 -or ($FailOnWarning -and ($statusSummary.Warning -gt 0 -or $statusSummary.Pending -gt 0))) {
    exit 1
}
