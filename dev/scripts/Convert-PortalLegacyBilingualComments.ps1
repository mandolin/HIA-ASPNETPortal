<#
.SYNOPSIS
<lang>
  <zh-CN>将明确的旧双语注释格式迁移为 DotNetDoc 支持的 locale 标记。</zh-CN>
  <en>Migrates explicit legacy bilingual comments to DotNetDoc-supported locale markup.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>本脚本只处理已经同时包含“中文：”与“English:”的注释节点或代码块注释，不推断缺失语言、不改业务语义。它用于 W-anp-P16.1 的分批迁移，可通过 -WhatIf 进行只读预演。</zh-CN>
  <en>This script only processes comments that already contain both "中文：" and "English:" text. It does not infer missing languages or change business semantics. It supports W-anp-P16.1 batched migration and can run in read-only preview mode with -WhatIf.</en>
</lang>

.PARAMETER Path
<lang>
  <zh-CN>要迁移的源码文件路径；建议按批次传入已人工选定的高风险文件。</zh-CN>
  <en>Source file paths to migrate; pass a reviewed high-risk batch rather than the whole repository by default.</en>
</lang>

.PARAMETER WhatIf
<lang>
  <zh-CN>只输出可迁移文件和节点数量，不写入文件。</zh-CN>
  <en>Outputs migratable files and node counts without writing files.</en>
</lang>
#>

param(
    [Parameter(Mandatory = $true)]
    [string[]]$Path,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-XmlText([string]$value) {
    if ($null -eq $value) {
        return ""
    }

    return (($value -replace "^\s+", "") -replace "\s+$", "")
}

function Join-CommentText([string[]]$lines) {
    $parts = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        $text = Convert-XmlText $line
        if ($text.Length -gt 0) {
            $parts.Add($text)
        }
    }

    return [string]::Join(" ", $parts)
}

function Add-LocaleBlock(
    [System.Collections.Generic.List[string]]$target,
    [string]$prefix,
    [string]$wrapperName,
    [string]$zhText,
    [string]$enText
) {
    $target.Add("$prefix<$wrapperName>")
    $target.Add("$prefix  <zh-CN>$zhText</zh-CN>")
    $target.Add("$prefix  <en>$enText</en>")
    $target.Add("$prefix</$wrapperName>")
}

function Try-SplitLegacyText(
    [string[]]$contentLines,
    [ref]$zhText,
    [ref]$enText
) {
    $zhLines = New-Object System.Collections.Generic.List[string]
    $enLines = New-Object System.Collections.Generic.List[string]
    $mode = ""

    foreach ($line in $contentLines) {
        $text = Convert-XmlText $line
        if ($text.Length -eq 0) {
            continue
        }

        if ($text -match "^中文[:：]\s*(.*)$") {
            $mode = "zh"
            $zhLines.Add($Matches[1])
            continue
        }

        if ($text -match "^English:\s*(.*)$") {
            $mode = "en"
            $enLines.Add($Matches[1])
            continue
        }

        if ($mode -eq "zh") {
            $zhLines.Add($text)
        }
        elseif ($mode -eq "en") {
            $enLines.Add($text)
        }
    }

    $zh = Join-CommentText $zhLines.ToArray()
    $en = Join-CommentText $enLines.ToArray()
    if ($zh.Length -eq 0 -or $en.Length -eq 0) {
        return $false
    }

    $zhText.Value = $zh
    $enText.Value = $en
    return $true
}

function Convert-InlineXmlDocLine(
    [string]$line,
    [System.Collections.Generic.List[string]]$target,
    [ref]$changed
) {
    if ($line -notmatch "^(?<indent>\s*)///\s*<(?<tag>\w+)(?<attrs>[^>]*)>中文[:：]\s*(?<zh>.*?)\s*English:\s*(?<en>.*?)</\k<tag>>\s*$") {
        return $false
    }

    $indent = $Matches["indent"]
    $tag = $Matches["tag"]
    $attrs = $Matches["attrs"]
    $zh = Convert-XmlText $Matches["zh"]
    $en = Convert-XmlText $Matches["en"]
    if ($zh.Length -eq 0 -or $en.Length -eq 0) {
        return $false
    }

    $prefix = "$indent/// "
    $wrapperName = if ($tag -eq "summary" -or $tag -eq "remarks") { "lang" } else { "l" }
    $target.Add("$prefix<$tag$attrs>")
    Add-LocaleBlock $target $prefix $wrapperName $zh $en
    $target.Add("$prefix</$tag>")
    $changed.Value = $true
    return $true
}

function Convert-XmlDocBlock(
    [string[]]$lines,
    [int]$index,
    [System.Collections.Generic.List[string]]$target,
    [ref]$changed,
    [ref]$nextIndex
) {
    $line = $lines[$index]
    if ($line -notmatch "^(?<indent>\s*)///\s*<(?<tag>summary|remarks|param|returns|exception)(?<attrs>[^>]*)>\s*$") {
        return $false
    }

    $indent = $Matches["indent"]
    $tag = $Matches["tag"]
    $attrs = $Matches["attrs"]
    $content = New-Object System.Collections.Generic.List[string]
    $scan = $index + 1
    while ($scan -lt $lines.Length) {
        if ($lines[$scan] -match "^\s*///\s*</$tag>\s*$") {
            break
        }

        if ($lines[$scan] -match "^\s*///\s?(?<text>.*)$") {
            $content.Add($Matches["text"])
        }
        else {
            return $false
        }

        $scan++
    }

    if ($scan -ge $lines.Length) {
        return $false
    }

    $zh = ""
    $en = ""
    if (-not (Try-SplitLegacyText $content.ToArray() ([ref]$zh) ([ref]$en))) {
        return $false
    }

    $prefix = "$indent/// "
    $wrapperName = if ($tag -eq "summary" -or $tag -eq "remarks") { "lang" } else { "l" }
    $target.Add("$prefix<$tag$attrs>")
    Add-LocaleBlock $target $prefix $wrapperName $zh $en
    $target.Add("$prefix</$tag>")
    $nextIndex.Value = $scan
    $changed.Value = $true
    return $true
}

function Convert-LineCommentPair(
    [string[]]$lines,
    [int]$index,
    [System.Collections.Generic.List[string]]$target,
    [ref]$changed,
    [ref]$nextIndex
) {
    if ($index + 1 -ge $lines.Length) {
        return $false
    }

    $first = $lines[$index]
    $second = $lines[$index + 1]
    if ($first -notmatch "^(?<indent>\s*)//\s*中文[:：]\s*(?<zh>.+?)\s*$") {
        return $false
    }

    $indent = $Matches["indent"]
    $zh = Convert-XmlText $Matches["zh"]
    if ($second -notmatch "^\s*//\s*English:\s*(?<en>.+?)\s*$") {
        return $false
    }

    $en = Convert-XmlText $Matches["en"]
    $target.Add("$indent// <lang>")
    $target.Add("$indent//   <zh-CN>$zh</zh-CN>")
    $target.Add("$indent//   <en>$en</en>")
    $target.Add("$indent// </lang>")
    $nextIndex.Value = $index + 1
    $changed.Value = $true
    return $true
}

$convertedFiles = New-Object System.Collections.Generic.List[object]

foreach ($inputPath in $Path) {
    $resolved = Resolve-Path -LiteralPath $inputPath
    $filePath = $resolved.Path
    $originalText = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
    $lineEnding = if ($originalText.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = [System.Text.RegularExpressions.Regex]::Split($originalText, "\r?\n")

    if ($lines.Length -gt 0 -and $lines[$lines.Length - 1] -eq "") {
        $lines = $lines[0..($lines.Length - 2)]
    }

    $output = New-Object System.Collections.Generic.List[string]
    $changed = $false
    $convertedNodes = 0

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $next = $i
        $lineChanged = $false
        if (Convert-InlineXmlDocLine $lines[$i] $output ([ref]$lineChanged)) {
            $changed = $true
            $convertedNodes++
            continue
        }

        if (Convert-XmlDocBlock $lines $i $output ([ref]$lineChanged) ([ref]$next)) {
            $changed = $true
            $convertedNodes++
            $i = $next
            continue
        }

        if (Convert-LineCommentPair $lines $i $output ([ref]$lineChanged) ([ref]$next)) {
            $changed = $true
            $convertedNodes++
            $i = $next
            continue
        }

        $output.Add($lines[$i])
    }

    if ($changed) {
        if (-not $WhatIf) {
            # <lang>
            #   <zh-CN>实际写入统一使用 UTF-8 无 BOM，避免 PowerShell 版本差异重新引入 BOM。</zh-CN>
            #   <en>Real writes always use UTF-8 without BOM so PowerShell version differences do not reintroduce a BOM.</en>
            # </lang>
            $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
            [System.IO.File]::WriteAllText($filePath, [string]::Join($lineEnding, $output) + $lineEnding, $utf8NoBom)
        }

        $convertedFiles.Add([pscustomobject]@{
            Path = $filePath
            ConvertedNodes = $convertedNodes
        })
    }
}

$convertedFiles
