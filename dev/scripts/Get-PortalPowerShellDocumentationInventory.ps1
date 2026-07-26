<#
.SYNOPSIS
Generates a read-only PowerShell documentation inventory for W-anp-P16.4.

.LANG en
Scans Git-tracked PowerShell scripts, classifies operational risk, and reports
whether each script has comment-based help and HIA bilingual language markers.
It does not execute scanned scripts, connect to services, rewrite files, or
read secrets.

.LANG zh-CN
扫描 Git 已追踪的 PowerShell 脚本，分类运行风险，并报告每个脚本是否具备
comment-based help 与 HIA 双语语言标记。它不执行被扫描脚本、不连接服务、
不改写文件，也不读取密钥。
#>
[CmdletBinding()]
param(
    [string]$OutputJson,

    [string]$OutputMarkdown,

    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptPaths = @(& git -C $repoRoot ls-files 'dev/scripts/*.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to read Git-tracked PowerShell scripts.'
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

function Get-RiskCategory {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($Name -match '(?i)(Sql|Database|Migration)') {
        return 'DataMigration'
    }

    if ($Name -match '(?i)(Compliance|Credential|Hardening|Security|EnterpriseScan)') {
        return 'SecurityCompliance'
    }

    if ($Name -match '(?i)(Publish|Release|TargetEnvironment|NearTarget)') {
        return 'ReleaseEnvironment'
    }

    if ($Name -match '(?i)(IIS|Smoke|IeMode|LegacyIe|VmAgent|VmTask)') {
        return 'RuntimeAutomation'
    }

    if ($Name -match '(?i)(Documentation|DotNetDoc|Jsdoc|Comment|Todo|SourceDocumentation)') {
        return 'Documentation'
    }

    if ($Name -match '(?i)(Operations|Log|Manifest|Evidence|Summary)') {
        return 'OperationsEvidence'
    }

    return 'General'
}

function Get-RiskLevel {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $highNamePattern = '(?i)(Publish-PortalFileSystem|Start-IISExpress|Stop-IISExpress|Initialize-PortalTestDatabase|Test-PortalSqlCompatibility|Test-PortalComplianceBaseline|Test-PortalDefaultCredentialRisk|Test-PortalProductionHardening|New-PortalNearTargetReleaseRehearsal|New-PortalLegacyIeTestPackage|New-PortalVmAgentTask|New-PortalVmTaskAgentPackage)'
    if ($Name -match $highNamePattern) {
        return 'High'
    }

    if ($Content -match '(?i)(Start-Process|Stop-Process|Invoke-WebRequest|Invoke-RestMethod|SqlConnection|ExecuteNonQuery|MSBuild|WebPublish|Set-Cookie|Password|SecureString)') {
        return 'Medium'
    }

    if ($Category -in @('Documentation', 'General')) {
        return 'Low'
    }

    return 'Medium'
}

function Get-ParameterNames {
    param([Parameter(Mandatory = $true)][string]$Content)

    $matches = [regex]::Matches($Content, '(?m)^\s*(?:\[[^\]]+\]\s*)*\$(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($match in $matches) {
        $name = $match.Groups['name'].Value
        if (-not $names.Contains($name)) {
            [void]$names.Add($name)
        }
    }

    return @($names)
}

$items = New-Object 'System.Collections.Generic.List[object]'
foreach ($relativePath in $scriptPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    $content = Get-Content -LiteralPath $fullPath -Encoding UTF8 -Raw
    $name = Split-Path -Leaf $relativePath
    $category = Get-RiskCategory -Name $name
    $hasHelp = $content -match '(?s)^\s*<#.*?\.(SYNOPSIS|DESCRIPTION|PARAMETER)'
    $hasHiaLang = $content -match '(?s)(\.LANG\s+en|\.LANG\s+zh-CN|<en>|<zh-CN>)'
    $hasSensitiveParameter = $content -match '(?i)\$(AdminPassword|Password|Token|Secret|ConnectionString|Cookie|Credential)'
    $riskLevel = Get-RiskLevel -Name $name -Category $category -Content $content
    $parameters = @(Get-ParameterNames -Content $content)

    $items.Add([pscustomobject]@{
            Path = $relativePath
            Name = $name
            RiskCategory = $category
            RiskLevel = $riskLevel
            HasCommentHelp = [bool]$hasHelp
            HasHiaLanguageMarkers = [bool]$hasHiaLang
            HasSensitiveParameter = [bool]$hasSensitiveParameter
            ParameterCount = $parameters.Count
            LineCount = ($content -split "`r?`n").Count
        })
}

$summaryByRiskLevel = @($items | Group-Object RiskLevel | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{ RiskLevel = $_.Name; Count = $_.Count }
    })
$summaryByCategory = @($items | Group-Object RiskCategory | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{ RiskCategory = $_.Name; Count = $_.Count }
    })
$highRiskMissingHia = @($items | Where-Object { $_.RiskLevel -eq 'High' -and -not $_.HasHiaLanguageMarkers } | Sort-Object Name)
$missingHelp = @($items | Where-Object { -not $_.HasCommentHelp } | Sort-Object RiskLevel, Name)

$inventory = [pscustomobject]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    Scope = 'Git-tracked dev/scripts/*.ps1; scanned as text only.'
    TotalScripts = $items.Count
    ScriptsWithCommentHelp = @($items | Where-Object HasCommentHelp).Count
    ScriptsWithHiaLanguageMarkers = @($items | Where-Object HasHiaLanguageMarkers).Count
    HighRiskScripts = @($items | Where-Object { $_.RiskLevel -eq 'High' }).Count
    HighRiskMissingHiaLanguageMarkers = $highRiskMissingHia.Count
    MissingCommentHelp = $missingHelp.Count
    SummaryByRiskLevel = $summaryByRiskLevel
    SummaryByRiskCategory = $summaryByCategory
    HighRiskMissingHia = $highRiskMissingHia
    MissingHelp = $missingHelp
    Items = @($items | Sort-Object RiskLevel, RiskCategory, Name)
}

if ($OutputJson) {
    $json = $inventory | ConvertTo-Json -Depth 8
    Write-Utf8NoBomFile -Path $OutputJson -Content ($json + "`r`n")
}

if ($OutputMarkdown) {
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add('# P16.4 PowerShell Documentation Inventory')
    $lines.Add('')
    $lines.Add("Generated UTC: $($inventory.GeneratedAtUtc)")
    $lines.Add('')
    $lines.Add('| Metric | Value |')
    $lines.Add('| --- | ---: |')
    $lines.Add("| Total scripts | $($inventory.TotalScripts) |")
    $lines.Add("| Scripts with comment help | $($inventory.ScriptsWithCommentHelp) |")
    $lines.Add("| Scripts with HIA language markers | $($inventory.ScriptsWithHiaLanguageMarkers) |")
    $lines.Add("| High-risk scripts | $($inventory.HighRiskScripts) |")
    $lines.Add("| High-risk scripts missing HIA markers | $($inventory.HighRiskMissingHiaLanguageMarkers) |")
    $lines.Add("| Scripts missing comment help | $($inventory.MissingCommentHelp) |")
    $lines.Add('')
    $lines.Add('## High-Risk Scripts Missing HIA Markers')
    $lines.Add('')
    $lines.Add('| Script | Category | Has Help | Sensitive Parameter |')
    $lines.Add('| --- | --- | --- | --- |')
    foreach ($item in $highRiskMissingHia) {
        $lines.Add("| `$($item.Name)` | $($item.RiskCategory) | $($item.HasCommentHelp) | $($item.HasSensitiveParameter) |")
    }
    $lines.Add('')
    $lines.Add('## Missing Comment Help')
    $lines.Add('')
    $lines.Add('| Script | Risk | Category |')
    $lines.Add('| --- | --- | --- |')
    foreach ($item in $missingHelp) {
        $lines.Add("| `$($item.Name)` | $($item.RiskLevel) | $($item.RiskCategory) |")
    }

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content (($lines -join "`r`n") + "`r`n")
}

if ($AsJson) {
    $inventory | ConvertTo-Json -Depth 8
}
else {
    $inventory
}
