<#
.SYNOPSIS
    Performs a read-only dry run for Portal diagnostics log retention.

.DESCRIPTION
    中文：本脚本只读取结构化诊断日志目录，按 portal-yyyyMMdd-nnn.jsonl 命名规则列出当前会被保留或清理的文件。
    它不会删除、移动或压缩任何日志，也不会读取日志正文，适合发布前或例行运维时确认保留策略。
    English: This script only reads the structured diagnostics log directory and lists files that would be kept or
    cleaned according to the portal-yyyyMMdd-nnn.jsonl naming convention. It never deletes, moves, compresses, or
    reads log content, making it suitable for release and routine operations review.
#>
[CmdletBinding()]
param(
    [string]$LogDirectory,

    [ValidateRange(1, 3650)]
    [int]$RetentionDays = 90,

    [datetime]$NowUtc = (Get-Date).ToUniversalTime(),

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($LogDirectory)) {
    $LogDirectory = Join-Path (Join-Path $repoRoot 'src/Portal') 'App_Data/Logs'
}

$resolvedLogDirectory = [System.IO.Path]::GetFullPath($LogDirectory)
$checks = New-Object 'System.Collections.Generic.List[object]'
$managedFiles = New-Object 'System.Collections.Generic.List[object]'
$retentionCandidates = New-Object 'System.Collections.Generic.List[object]'
$unmanagedFiles = New-Object 'System.Collections.Generic.List[object]'
$managedPattern = [regex]::new(
    '^portal-(?<date>\d{8})-(?<sequence>\d{3})\.jsonl$',
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
    [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)

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

function Add-LogMaintenanceCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
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

function ConvertTo-DisplayPath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $repoPrefix = $repoRoot.TrimEnd('\') + '\'
    if ($fullPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($repoPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

function Try-ParseManagedLogDate {
    param(
        [string]$FileName,
        [ref]$FileDateUtc
    )

    $match = $managedPattern.Match($FileName ?? '')
    if (-not $match.Success) {
        return $false
    }

    $parsed = [datetime]::MinValue
    $ok = [datetime]::TryParseExact(
        $match.Groups['date'].Value,
        'yyyyMMdd',
        [System.Globalization.CultureInfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::AssumeUniversal -bor [System.Globalization.DateTimeStyles]::AdjustToUniversal,
        [ref]$parsed)
    if ($ok) {
        $FileDateUtc.Value = $parsed
    }

    return $ok
}

Write-Host ('MODE: read-only diagnostics log retention dry run.')
Write-Host ('LOG DIRECTORY: {0}' -f $resolvedLogDirectory)
Write-Host ('RETENTION DAYS: {0}' -f $RetentionDays)

$cutoffUtcDate = $NowUtc.Date.AddDays(-$RetentionDays)
if (-not (Test-Path -LiteralPath $resolvedLogDirectory -PathType Container)) {
    Add-LogMaintenanceCheck -Severity Warning -Code 'LOGDIR-001' -Message 'Diagnostics log directory does not exist yet.' -Evidence (ConvertTo-DisplayPath -Path $resolvedLogDirectory)
}
else {
    Add-LogMaintenanceCheck -Severity Pass -Code 'LOGDIR-001' -Message 'Diagnostics log directory exists.' -Evidence (ConvertTo-DisplayPath -Path $resolvedLogDirectory)

    foreach ($file in Get-ChildItem -LiteralPath $resolvedLogDirectory -File | Sort-Object Name) {
        $fileDateUtc = [datetime]::MinValue
        if (Try-ParseManagedLogDate -FileName $file.Name -FileDateUtc ([ref]$fileDateUtc)) {
            $entry = [pscustomobject][ordered]@{
                Name = $file.Name
                RelativePath = ConvertTo-DisplayPath -Path $file.FullName
                Bytes = $file.Length
                LogDateUtc = $fileDateUtc.ToString('yyyy-MM-dd')
                WouldBeDeleted = $fileDateUtc -lt $cutoffUtcDate
            }
            $managedFiles.Add($entry)
            if ($entry.WouldBeDeleted) {
                $retentionCandidates.Add($entry)
            }
        }
        else {
            $unmanagedFiles.Add([pscustomobject][ordered]@{
                    Name = $file.Name
                    RelativePath = ConvertTo-DisplayPath -Path $file.FullName
                    Bytes = $file.Length
                })
        }
    }

    Add-LogMaintenanceCheck -Severity Info -Code 'LOGFILE-COUNT' -Message ('Managed={0}; RetentionCandidates={1}; Unmanaged={2}' -f $managedFiles.Count, $retentionCandidates.Count, $unmanagedFiles.Count)
    if ($retentionCandidates.Count -gt 0) {
        Add-LogMaintenanceCheck -Severity Warning -Code 'RETENTION-DRYRUN' -Message 'Some managed log files are older than the retention cutoff; no files were deleted.' -Evidence (($retentionCandidates | Select-Object -First 8 -ExpandProperty Name) -join '; ')
    }
    else {
        Add-LogMaintenanceCheck -Severity Pass -Code 'RETENTION-DRYRUN' -Message 'No managed log file is older than the retention cutoff.'
    }

    if ($unmanagedFiles.Count -gt 0) {
        Add-LogMaintenanceCheck -Severity Warning -Code 'UNMANAGED-LOGFILES' -Message 'Unmanaged files are present in the diagnostics log directory; review before manual cleanup.' -Evidence (($unmanagedFiles | Select-Object -First 8 -ExpandProperty Name) -join '; ')
    }
}

Add-LogMaintenanceCheck -Severity Pass -Code 'DRYRUN-ONLY' -Message 'The script completed without deleting, moving, compressing, or reading log content.'

$summary = [pscustomobject][ordered]@{
    LogDirectory = $resolvedLogDirectory
    RetentionDays = $RetentionDays
    NowUtc = $NowUtc.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    CutoffUtcDate = $cutoffUtcDate.ToString('yyyy-MM-dd')
    ManagedFiles = $managedFiles
    RetentionCandidates = $retentionCandidates
    UnmanagedFiles = $unmanagedFiles
    Checks = $checks
    TotalChecks = $checks.Count
    FailedChecks = @($checks | Where-Object { $_.Severity -eq 'Fail' }).Count
    WarningChecks = @($checks | Where-Object { $_.Severity -eq 'Warning' }).Count
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
