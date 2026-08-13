<#
.SYNOPSIS
    Performs a read-only dry run for Portal diagnostics log retention.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本只读取结构化诊断日志目录，按 portal-yyyyMMdd-nnn.jsonl 命名规则列出当前会被保留或清理的文件。它不会删除、移动或压缩任何日志，也不会读取日志正文，适合发布前或例行运维时确认保留策略。</zh-CN>
      <en>This script only reads the structured diagnostics log directory and lists files that would be kept or cleaned according to the portal-yyyyMMdd-nnn.jsonl naming convention. It never deletes, moves, compresses, or reads log content, making it suitable for release and routine operations review.</en>
    </lang>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 输出 dry-run JSON，并只创建调用方指定的输出父目录。</zh-CN>
#   <en>Write dry-run JSON as UTF-8 without a BOM, creating only the output parent directory selected by the caller.</en>
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
#   <zh-CN>追加日志保留静态 finding 并输出低敏证据；Severity 不触发文件系统写操作。</zh-CN>
#   <en>Add a static log-retention finding and display low-sensitivity evidence; Severity never triggers filesystem writes.</en>
# </lang>
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

# <lang>
#   <zh-CN>把仓库内日志路径转为稳定相对显示路径，仓库外路径保持绝对形式。</zh-CN>
#   <en>Convert repository log paths to stable relative display paths while keeping external paths absolute.</en>
# </lang>
function ConvertTo-DisplayPath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $repoPrefix = $repoRoot.TrimEnd('\') + '\'
    if ($fullPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($repoPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

# <lang>
#   <zh-CN>按固定文件名规则解析日志日期；解析失败只归入 unmanaged，不删除或读取正文。</zh-CN>
#   <en>Parse the log date using the fixed filename rule; failures become unmanaged entries without deleting or reading content.</en>
# </lang>
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

# <lang>
#   <zh-CN>输出本次 dry-run 参数；只展示目录、保留天数和计算基准，不泄露日志正文。</zh-CN>
#   <en>Display dry-run parameters using only directory, retention days, and calculation baseline without exposing log content.</en>
# </lang>
Write-Host ('MODE: read-only diagnostics log retention dry run.')
Write-Host ('LOG DIRECTORY: {0}' -f $resolvedLogDirectory)
Write-Host ('RETENTION DAYS: {0}' -f $RetentionDays)

# <lang>
#   <zh-CN>计算 UTC 保留截止日期并只读取目录元数据；不存在目录时记录 Warning，不创建日志目录。</zh-CN>
#   <en>Calculate the UTC retention cutoff and read directory metadata only; record Warning for a missing directory without creating it.</en>
# </lang>
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

# <lang>
#   <zh-CN>汇总 managed/unmanaged 文件和保留候选；WouldBeDeleted 只是预测字段，不执行删除。</zh-CN>
#   <en>Summarize managed/unmanaged files and retention candidates; WouldBeDeleted is predictive only and never deletes files.</en>
# </lang>
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

# <lang>
#   <zh-CN>仅在显式指定 OutputJson 时写出摘要；不会写回日志目录或读取日志正文。</zh-CN>
#   <en>Write the summary only when OutputJson is explicit; do not write back to the log directory or read log content.</en>
# </lang>
if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

# <lang>
#   <zh-CN>存在 Fail 或显式 FailOnWarning 的 Warning 时返回非零；不会把 dry-run 变成清理动作。</zh-CN>
#   <en>Return non-zero for Fail or Warning when FailOnWarning is explicit; never turn the dry run into a cleanup action.</en>
# </lang>
if ($summary.FailedChecks -gt 0 -or ($FailOnWarning -and $summary.WarningChecks -gt 0)) {
    exit 1
}

exit 0
