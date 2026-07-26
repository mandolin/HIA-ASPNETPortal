<#
.SYNOPSIS
.LANG en
Reads notification documents from the HIA-Documentation-Sys WorkZone.

.LANG zh-CN
读取 HIA-Documentation-Sys WorkZone 中的通知文档。

.DESCRIPTION
.LANG en
Lists recent Markdown notices from HIA-Documentation-Sys/work-zone/notify so this
project can actively pull upstream documentation-tooling messages. The script is
read-only: it does not copy notices into this repository and does not update any
local planning files by itself.

.LANG zh-CN
列出 HIA-Documentation-Sys/work-zone/notify 下近期 Markdown 通知，让本项目主动拉取上游
文档工具链消息。本脚本为只读行为：不会把通知复制进本仓库，也不会自行更新本地规划文件。

.PARAMETER HiaDocumentationRoot
.LANG en
Optional root path of the HIA-Documentation-Sys repository. When omitted, the script uses the sibling repository path.

.LANG zh-CN
可选的 HIA-Documentation-Sys 仓库根目录。省略时使用同级仓库路径。

.PARAMETER Since
.LANG en
Only returns notices whose last write time is greater than or equal to this value.

.LANG zh-CN
只返回最后写入时间大于或等于该值的通知。

.PARAMETER Latest
.LANG en
Maximum number of recent notices to return.

.LANG zh-CN
最多返回的近期通知数量。

.PARAMETER ShowContent
.LANG en
Prints full notice content instead of the summary table.

.LANG zh-CN
输出完整通知内容，而不是摘要表格。
#>
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

# <lang>
#   <zh-CN>本脚本只读取 HIA-Documentation-Sys 的 WorkZone 通知，不复制通知、不修改本项目状态。</zh-CN>
#   <en>This script only reads HIA-Documentation-Sys WorkZone notifications. It does not copy notices or modify this project.</en>
# </lang>
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
