<#
.SYNOPSIS
.LANG en
Reads notification documents from the HIA-Documentation-Sys WorkZone.

.LANG zh-CN
读取 HIA-Documentation-Sys WorkZone 中的通知文档。

.DESCRIPTION
<lang>
  <zh-CN>列出 HIA-Documentation-Sys/work-zone/notify 下近期 Markdown 通知，让本项目主动拉取上游文档工具链消息。本脚本为只读行为：不会把通知复制进本仓库，也不会自行更新本地规划文件。</zh-CN>
  <en>List recent Markdown notices from HIA-Documentation-Sys/work-zone/notify so this project can pull upstream documentation-tooling messages. The script is read-only: it does not copy notices into this repository or update local planning files.</en>
</lang>

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
# <lang>
#   <zh-CN>默认定位同级 HIA-Documentation-Sys 仓库；调用方可通过显式路径读取其他工作副本。</zh-CN>
#   <en>Default to the sibling HIA-Documentation-Sys repository, while allowing callers to read another worktree through an explicit path.</en>
# </lang>
$defaultHiaDocumentationRoot = Join-Path (Split-Path -Parent $repositoryRoot) 'HIA-Documentation-Sys'

if ([string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    $HiaDocumentationRoot = $defaultHiaDocumentationRoot
}

$notifyRoot = Join-Path $HiaDocumentationRoot 'work-zone\notify'
if (-not (Test-Path -LiteralPath $notifyRoot -PathType Container)) {
    # <lang>
    #   <zh-CN>通知目录是只读拉取的边界；目录缺失时明确失败，不回退到本项目或其他未指定位置。</zh-CN>
    #   <en>The notification directory is the read-only collection boundary; fail explicitly when it is missing instead of falling back to this project or another unspecified location.</en>
    # </lang>
    throw "未找到 HIA-Documentation-Sys 通知目录：$notifyRoot"
}

# <lang>
#   <zh-CN>优先提取首个 Markdown 一级标题；没有标题时使用文件名，保证摘要结果始终可识别。</zh-CN>
#   <en>Prefer the first Markdown level-one heading and fall back to the file name so every summary remains identifiable.</en>
# </lang>
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

# <lang>
#   <zh-CN>跳过标题、空行和围栏代码后取首段正文，并限制长度以保持列表输出可读。</zh-CN>
#   <en>Skip headings, blank lines, and fenced code, then take the first prose line with a length cap to keep list output readable.</en>
# </lang>
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

# <lang>
#   <zh-CN>递归枚举通知 Markdown 并排除 README；Since 过滤只使用文件最后写入时间，不修改任何上游文件。</zh-CN>
#   <en>Recursively enumerate notification Markdown files and exclude README; Since filters only by last-write time and never changes upstream files.</en>
# </lang>
$notificationFiles = Get-ChildItem -LiteralPath $notifyRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.Name -ne 'README.md' }

if ($PSBoundParameters.ContainsKey('Since')) {
    $notificationFiles = $notificationFiles | Where-Object { $_.LastWriteTime -ge $Since }
}

$pathTrimCharacters = [char[]]@('\', '/')
$notifyRootFullPath = [System.IO.Path]::GetFullPath($notifyRoot).TrimEnd($pathTrimCharacters) + [System.IO.Path]::DirectorySeparatorChar

# <lang>
#   <zh-CN>按更新时间截取通知，读取 UTF-8 正文并计算相对路径；输出对象保留完整路径供人工复核，但不复制内容到本仓库。</zh-CN>
#   <en>Limit notices by update time, read UTF-8 content, and calculate relative paths; retain full paths for manual review without copying content into this repository.</en>
# </lang>
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
    # <lang>
    #   <zh-CN>仅在调用方明确指定 -ShowContent 时输出完整通知正文，默认结果只暴露摘要字段和项目命中标记。</zh-CN>
    #   <en>Print full notice bodies only when -ShowContent is explicit; the default exposes summary fields and the current-project match flag.</en>
    # </lang>
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
    # <lang>
    #   <zh-CN>默认输出稳定的摘要列，避免无意把上游通知正文扩散到终端或后续日志。</zh-CN>
    #   <en>Use stable summary columns by default so upstream notice bodies are not unintentionally expanded into the terminal or later logs.</en>
    # </lang>
    $notifications | Select-Object LastWriteTime, Title, MentionsCurrentProject, RelativePath, Summary
}
