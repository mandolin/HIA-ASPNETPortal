[CmdletBinding()]
param(
    [string]$HiaDocumentationRoot,

    [datetime]$Since,

    [ValidateRange(1, 200)]
    [int]$Latest = 20,

    [switch]$ShowContent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 中文：本脚本只读取 HIA-Documentation-Sys 的 WorkZone 通知，不复制通知、不修改本项目状态。
# English: This script only reads HIA-Documentation-Sys WorkZone notifications. It does not copy notices or modify this project.
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$defaultHiaDocumentationRoot = Join-Path (Split-Path -Parent $repositoryRoot) 'HIA-Documentation-Sys'

if ([string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    $HiaDocumentationRoot = $defaultHiaDocumentationRoot
}

$notifyRoot = Join-Path $HiaDocumentationRoot 'work-zone\notify'
if (-not (Test-Path -LiteralPath $notifyRoot -PathType Container)) {
    throw "未找到 HIA-Documentation-Sys 通知目录：$notifyRoot"
}

function Get-NotificationTitle {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$FallbackName
    )

    $titleLine = $Content -split "`r?`n" | Where-Object { $_ -match '^\s*#\s+' } | Select-Object -First 1
    if ($null -ne $titleLine) {
        return ($titleLine -replace '^\s*#\s+', '').Trim()
    }

    return [System.IO.Path]::GetFileNameWithoutExtension($FallbackName)
}

function Get-NotificationSummary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $lines = $Content -split "`r?`n"
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#') -or $trimmed.StartsWith('```')) {
            continue
        }

        if ($trimmed.Length -gt 120) {
            return $trimmed.Substring(0, 120) + '...'
        }

        return $trimmed
    }

    return ''
}

$notificationFiles = Get-ChildItem -LiteralPath $notifyRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.Name -ne 'README.md' }

if ($PSBoundParameters.ContainsKey('Since')) {
    $notificationFiles = $notificationFiles | Where-Object { $_.LastWriteTime -ge $Since }
}

$pathTrimCharacters = [char[]]@('\', '/')
$notifyRootFullPath = [System.IO.Path]::GetFullPath($notifyRoot).TrimEnd($pathTrimCharacters) + [System.IO.Path]::DirectorySeparatorChar

$notifications = @($notificationFiles |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First $Latest |
    ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
        $fileFullPath = [System.IO.Path]::GetFullPath($_.FullName)
        $relativePath = if ($fileFullPath.StartsWith($notifyRootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            $fileFullPath.Substring($notifyRootFullPath.Length)
        }
        else {
            $_.Name
        }

        [pscustomobject][ordered]@{
            LastWriteTime = $_.LastWriteTime
            Title = Get-NotificationTitle -Content $content -FallbackName $_.Name
            Summary = Get-NotificationSummary -Content $content
            MentionsCurrentProject = $content -match 'HIA-ASPNETPortal'
            RelativePath = $relativePath
            FullPath = $_.FullName
            Content = if ($ShowContent) { $content } else { $null }
        }
    })

if ($ShowContent) {
    foreach ($notification in $notifications) {
        Write-Output ('Title: ' + $notification.Title)
        Write-Output ('Path: ' + $notification.FullPath)
        Write-Output ''
        Write-Output $notification.Content
        Write-Output ''
        Write-Output '---'
    }
}
else {
    $notifications | Select-Object LastWriteTime, Title, MentionsCurrentProject, RelativePath, Summary
}
