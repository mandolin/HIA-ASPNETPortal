<#
.SYNOPSIS
    统计 Portal 站点中"未本地化的可见文案站点"数量，按区域分组。

.DESCRIPTION
    # <lang>
    #   <zh-CN>
    #     口径（本文件即口径的唯一权威定义，改动须同步本注释）：
    #     一个"站点"= 一处出现在 UI 文案位置、但写成字面量（而非资源表达式）的位置。
    #     计数单位为"处"，不做去重合并，便于与改造工作量对齐。
    #
    #     三类信号：
    #       M1 标记属性文案：Text/Title/ToolTip/HeaderText/AlternateText/ConfirmText/InfoMessage
    #                        的值以字母开头且不是 &lt;%...%&gt; 资源表达式。
    #                        刻意不含 OnClientClick：该属性的值是 JS 代码，其中真正的文案是属性内的字符串字面量，属另一类信号。
    #       M2 标记元素文本：&gt;文字&lt; 且内容由字母/数字/空格/常见标点组成、长度 &gt;= 4。
    #       C1 代码可见消息：Text/ErrorText/InfoMessage/Message/ConfirmText/ToolTip/HeaderText/AlternateText
    #                        赋值右侧（含 string.Format 前缀）为字面量；以及 InnerText/InnerHtml 赋值、
    #                        Show*("...") 与 new ListItem("...")。
    #                        刻意不含 Value：Value 既可能承载界面文案，也可能是隐藏字段的数据值（如 "edit"），
    #                        纳入会产生大量误报，其代价高于漏报。
    #
    #     排除项（这是本口径与早期交互式统计的关键差别）：
    #       - 代码注释：行注释 // 、块注释 /* */ 、XML 文档注释 /// 内的文本一律不计。
    #         早期统计未做此排除，把注释里的中文也算成了站点，导致虚高。
    #       - 标记注释 &lt;!-- --&gt; 与 &lt;%-- --%&gt;（本项目 &lt;lang&gt; 双语注释即位于其中）。
    #       - &lt;script&gt; 与 &lt;style&gt; 区域内的内容。
    #       - *.designer.cs、obj/、bin/。
    #       - 技术令牌（形如 Business.Collaboration.Handle 的点号标识符、module.json 之类的文件名）：
    #         它们是数据/文件名而非面向用户的文案，属性侧与元素侧一律剔除。
    #
    #     已知误报类（**须人工剔除，不得计入交付**）：
    #       - 作为数据载体的控件文本。典型例子：`SourceSystemTextBox.Text = "Portal";`
    #         写入的是"来源系统代码值"，属员工记录的数据而非界面文案；一旦本地化就会污染数据。
    #         本工具无法从语法上区分"赋给控件的文案"与"赋给控件的数据值"，故此类须由人工判定。
    #         实测：Admin 区曾出现的 1 处代码侧命中即属此类。
    #       - 已资源驱动消息里的 **HTML 包裹字面量**。典型例子：
    #         `Message.Text = string.Format("<br>{0}<br/>", lang.Signin_LoginFaild);`
    #         消息本身取自资源，字面量只是换行/标签包裹；本地化它没有意义且会破坏标签结构。
    #         实测：Signin 控件的 2 处命中即属此类。
    #     这两类必须由人工判定并**保留原样**，清单数字里会一直带着它们——这是刻意的：
    #     宁可让数字带可解释的噪音，也不要把它们"改掉"。
    #
    #     因此本口径得到的数字与早期记录的"124 处"**定义不同，不可相加**：早期清单偏向"硬编码中文"，
    #     本口径偏向"字面量文案（不分语种）"。
    #   </zh-CN>
    #   <en>
    #     Definition of the metric (this file is the single source of truth; keep this comment in sync):
    #     A "site" is one location where UI text is written as a literal instead of a resource expression.
    #     The unit is a location count with no deduplication, so it maps directly onto rework volume.
    #
    #     Three signals:
    #       M1 markup attribute text: Text/Title/ToolTip/HeaderText/AlternateText/ConfirmText/InfoMessage
    #                                 with a letter-leading literal value that is not a &lt;%...%&gt; expression.
    #                                 OnClientClick is deliberately excluded: its value is JavaScript, and the real
    #                                 text inside it is a string literal in that script, which is a different signal.
    #       M2 markup element text: &gt;text&lt; where the content is letters/digits/spaces/common punctuation, length &gt;= 4.
    #       C1 code visible messages: literal right-hand side of Text/ErrorText/InfoMessage/Message/ConfirmText/
    #                                 ToolTip/HeaderText/AlternateText assignments (including a string.Format prefix),
    #                                 plus InnerText/InnerHtml assignments, Show*("...") and new ListItem("...").
    #                                 Value is deliberately excluded: it may carry UI copy but just as often a hidden
    #                                 field's data value (e.g. "edit"), and the false positives would cost more than
    #                                 the misses.
    #
    #     Exclusions (this is the key difference from the earlier interactive count):
    #       - Code comments: text inside //, /* */ and /// is never counted. The earlier count lacked this exclusion
    #         and therefore counted Chinese text inside comments as sites, inflating the total.
    #       - Markup comments &lt;!-- --&gt; and &lt;%-- --%&gt; (this project's &lt;lang&gt; bilingual comments live there).
    #       - Content inside &lt;script&gt; and &lt;style&gt;.
    #       - *.designer.cs, obj/, bin/.
    #       - Technical tokens (dotted identifiers such as Business.Collaboration.Handle, file names such as
    #         module.json): data or file names rather than user-facing copy, removed on both the attribute and
    #         element sides.
    #
    #     v5 adds two more signals, again found by reconciling the inventory against a manual read-through:
#       - element text that starts after a newline or indentation was never counted, because the leading
#         character class excluded whitespace; indented paragraphs and bare labels were missed as a result;
#       - ErrorMessage (validator prompts) was missing from the attribute list even though it is visible copy.
#     Known false-positive class (must be removed by hand and never counted as deliverable work):
#       - Control text used as a data carrier. Canonical example: `SourceSystemTextBox.Text = "Portal";`
#         writes a source-system code value, which is employee-record data rather than UI copy; localizing it
#         would corrupt data. The tool cannot distinguish by syntax between copy assigned to a control and a
#         data value assigned to a control, so this class requires human judgement. In practice one such
#         code-side hit occurred in the Admin area.
#       - HTML wrapper literals inside already resource-driven messages. Canonical example:
#         `Message.Text = string.Format("<br>{0}<br/>", lang.Signin_LoginFaild);` — the message itself comes
#         from resources and the literal is only line-break or tag wrapping; localizing it is meaningless and
#         would break the markup structure. Two such hits occurred in the Signin control.
#     Both classes must be judged by hand and left untouched. They deliberately remain in the reported number:
#     a number carrying explainable noise is better than silently changing copy that must not change.
    #
    #     The resulting number therefore has a different definition from the earlier "124 sites" record and the two
    #     must not be added together: the earlier list leaned toward hard-coded Chinese, this one toward literal
    #     text regardless of language.
    #   </en>
    # </lang>

.PARAMETER PortalRoot
    Portal 站点根目录，默认 src/Portal。

.PARAMETER OutputJson
    可选：把按文件的完整明细写入该 JSON 路径。

.PARAMETER DetailTop
    控制台展示的文件明细条数，默认 20。

.EXAMPLE
    pwsh -File dev/scripts/Get-PortalLocalizationInventory.ps1
#>
[CmdletBinding()]
param(
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
    [string]$PortalRoot = (Join-Path (Join-Path $PSScriptRoot '..\..') 'src\Portal'),

    [string]$OutputJson,

    [int]$DetailTop = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>
#     剥离 C# 注释但保留字符串字面量：必须区分"注释里的中文"与"字符串里的 //"，
#     否则像 "http://..." 这样的字面量会被误当成注释起点，把后续真实代码整段吞掉。
#     注释内容以空格替换（保留换行），使行号与列位置在后续匹配中仍然可信。
#   </zh-CN>
#   <en>
#     Strips C# comments while preserving string literals: text inside comments must be distinguished from a
#     "//" that appears inside a string, otherwise a literal such as "http://..." would be treated as the start
#     of a comment and would swallow the real code that follows. Comment content is replaced with spaces
#     (newlines preserved) so line and column positions stay trustworthy for later matching.
#   </en>
# </lang>
function Remove-CodeComments {
    param([Parameter(Mandatory)][string]$Text)

    $sb = New-Object System.Text.StringBuilder ($Text.Length)
    $i = 0
    $n = $Text.Length
    $state = 'Code'

    while ($i -lt $n) {
        $c = $Text[$i]
        $c2 = if ($i + 1 -lt $n) { $Text[$i + 1] } else { [char]0 }

        if ($state -eq 'Code') {
            if ($c -eq '/' -and $c2 -eq '/') { $state = 'Line'; [void]$sb.Append('  '); $i += 2 }
            elseif ($c -eq '/' -and $c2 -eq '*') { $state = 'Block'; [void]$sb.Append('  '); $i += 2 }
            elseif ($c -eq '@' -and $c2 -eq '"') { $state = 'Verbatim'; [void]$sb.Append('@"'); $i += 2 }
            elseif ($c -eq '"') { $state = 'String'; [void]$sb.Append($c); $i++ }
            elseif ($c -eq "'") { $state = 'Char'; [void]$sb.Append($c); $i++ }
            else { [void]$sb.Append($c); $i++ }
        }
        elseif ($state -eq 'Line') {
            if ($c -eq "`n") { $state = 'Code'; [void]$sb.Append($c) } else { [void]$sb.Append(' ') }
            $i++
        }
        elseif ($state -eq 'Block') {
            if ($c -eq '*' -and $c2 -eq '/') { $state = 'Code'; [void]$sb.Append('  '); $i += 2 }
            else {
                [void]$sb.Append($(if ($c -eq "`n") { $c } else { ' ' }))
                $i++
            }
        }
        elseif ($state -eq 'String') {
            if ($c -eq '\') {
                [void]$sb.Append($c)
                if ($i + 1 -lt $n) { [void]$sb.Append($Text[$i + 1]) }
                $i += 2
            }
            else {
                if ($c -eq '"') { $state = 'Code' }
                [void]$sb.Append($c)
                $i++
            }
        }
        elseif ($state -eq 'Verbatim') {
            if ($c -eq '"' -and $c2 -eq '"') { [void]$sb.Append('""'); $i += 2 }
            else {
                if ($c -eq '"') { $state = 'Code' }
                [void]$sb.Append($c)
                $i++
            }
        }
        else {
            if ($c -eq '\') {
                [void]$sb.Append($c)
                if ($i + 1 -lt $n) { [void]$sb.Append($Text[$i + 1]) }
                $i += 2
            }
            else {
                if ($c -eq "'") { $state = 'Code' }
                [void]$sb.Append($c)
                $i++
            }
        }
    }

    return $sb.ToString()
}

# <lang>
#   <zh-CN>剔除标记中的注释与非 UI 区域，避免把注释文案与脚本内容算成可见文案。</zh-CN>
#   <en>Removes markup comments and non-UI regions so comment text and script content are not counted as visible text.</en>
# </lang>
function Remove-MarkupNonUi {
    param([Parameter(Mandatory)][string]$Text)

    $t = [regex]::Replace($Text, '(?s)<!--.*?-->', ' ')
    $t = [regex]::Replace($t, '(?s)<%--.*?--%>', ' ')
    $t = [regex]::Replace($t, '(?is)<script\b.*?</script\s*>', ' ')
    $t = [regex]::Replace($t, '(?is)<style\b.*?</style\s*>', ' ')
    return $t
}

$markupAttrPattern = [regex]("(?<![\w-])(?:Text|Title|ToolTip|HeaderText|AlternateText|ConfirmText|InfoMessage|ErrorMessage)\s*=\s*`"(?<v>[^`"<>]{2,})`"")
$markupElemPattern = [regex](">\s*(?<v>[^<>`"=]{2,})<")
$elemEntityPattern = [regex]'^&[A-Za-z]+;$'
$onClientClickPattern = [regex]("OnClientClick\s*=\s*`"[^`"]*[\u4e00-\u9fff][^`"]*`"")
$elemNoisePattern = [regex]'^\s*(?:&[A-Za-z]+;|&#\d+;)\s*$|</|^\s*$'
# <lang>
#   <zh-CN>技术令牌：点号标识符与常见文件名，属数据而非界面文案，属性侧与元素侧都剔除。</zh-CN>
#   <en>Technical tokens: dotted identifiers and common file names, which are data rather than UI copy and are removed on both the attribute and element sides.</en>
# </lang>
$techTokenPattern = [regex]'^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z0-9]+)+$'

# <lang>
#   <zh-CN>
#     统一的"是否界面文案"判定（口径 v4）。v4 修正两个漏计缺陷，两者都是通过"清单与人工通读对账"暴露的：
#       ① 大小写敏感：原信号只列 `Message`，因此 `message = "Organization unit id is invalid."` 这类
#          小写局部变量承载的校验消息**全部漏计**；
#       ② 首字符过窄：原要求字母/汉字开头，因此 `new ListItem("(none)")`、`"(root)"` 这类
#          括号开头的下拉哨兵项**漏计**。
#     放宽匹配后必须靠本函数收紧，否则会把纯数字、符号和无意义片段算成文案。
#   </zh-CN>
#   <en>
#     Shared "is this UI copy" test (metric v4). v4 fixes two under-counting defects, both exposed by reconciling
#     the inventory against a manual read-through:
#       ① case sensitivity: the signals only listed `Message`, so validation messages held in lowercase locals such
#          as `message = "Organization unit id is invalid."` were never counted;
#       ② leading character too narrow: a leading letter or Han character was required, so dropdown sentinel items
#          such as `new ListItem("(none)")` and `"(root)"` were never counted.
#     Widening the patterns requires this function to tighten them again, otherwise bare numbers, symbols, and
#     meaningless fragments would be counted as copy.
#   </en>
# </lang>
function Test-IsUiCopy {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    $v = $Value.Trim()
    if ($v.Length -lt 2) { return $false }
    if ($techTokenPattern.IsMatch($v)) { return $false }
    if (-not [regex]::IsMatch($v, '[A-Za-z\u4e00-\u9fff]')) { return $false }
    return $true
}
$codeAssignPattern = [regex]("(?i)(?:^|[^A-Za-z0-9_])(?:Text|ErrorText|InfoMessage|Message|ConfirmText|ToolTip|HeaderText|AlternateText|InnerText|InnerHtml)\s*=\s*(?:string\.Format\(\s*)?`"(?<v>[^`"]{2,})`"")
$codeCallPattern = [regex]("(?i)(?:Show[A-Za-z]*|new\s+ListItem)\(\s*`"(?<v>[^`"]{2,})`"")

# <lang>
#   <zh-CN>按相对路径归类到区域，用于把"已完成区"与"未覆盖区"分开呈现。</zh-CN>
#   <en>Classifies a relative path into an area so completed and uncovered areas are reported separately.</en>
# </lang>
function Get-PortalArea {
    param([Parameter(Mandatory)][string]$RelativePath)

    $p = $RelativePath -replace '\\', '/'
    if ($p -like 'Admin/*') { return 'Admin（已完成区）' }
    if ($p -like 'Components/*') { return 'Components' }
    if ($p -like 'DesktopModules/*') { return 'DesktopModules（内容模块套件）' }
    if ($p -like 'Util/*') { return 'Util' }
    return '站点根'
}

$root = (Resolve-Path -LiteralPath $PortalRoot).Path
$files = Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object {
        $_.Extension -in @('.aspx', '.ascx', '.master', '.cs') -and
        $_.Name -notlike '*.designer.cs' -and
        $_.FullName -notlike '*\obj\*' -and
        $_.FullName -notlike '*\bin\*'
    }

$rows = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length).TrimStart('\')
    $raw = [System.IO.File]::ReadAllText($file.FullName)

    $m1 = 0; $m2 = 0; $c1 = 0
    if ($file.Extension -eq '.cs') {
        $body = Remove-CodeComments -Text $raw
        foreach ($match in $codeAssignPattern.Matches($body)) { if (Test-IsUiCopy -Value $match.Groups['v'].Value) { $c1++ } }
        foreach ($match in $codeCallPattern.Matches($body)) { if (Test-IsUiCopy -Value $match.Groups['v'].Value) { $c1++ } }
    }
    else {
        $body = Remove-MarkupNonUi -Text $raw
        foreach ($match in $markupAttrPattern.Matches($body)) {
            if (-not (Test-IsUiCopy -Value $match.Groups['v'].Value)) { continue }
            $m1++
        }
        $m1 += $onClientClickPattern.Matches($body).Count
        foreach ($match in $markupElemPattern.Matches($body)) {
            $value = $match.Groups['v'].Value
            if ($elemNoisePattern.IsMatch($value)) { continue }
            # <lang>
            #   <zh-CN>长度判定必须语言感知：英文按"至少两个字母"，中文按"至少两个汉字"。
            #         此前统一要求 4 个字符，导致"昵称"这类两字中文标签被漏计（实测 ManageUsers 页漏了 3 处）。</zh-CN>
            #   <en>The length test must be language-aware: at least two letters for English, at least two Han
            #         characters for Chinese. The earlier uniform four-character threshold dropped two-character
            #         Chinese labels (three sites were missed on the ManageUsers page in practice).</en>
            # </lang>
            $hasAscii = [regex]::IsMatch($value, '[A-Za-z]{2,}')
            $hasCjk = ([regex]::Matches($value, '[\u4e00-\u9fff]')).Count -ge 2
            if (-not $hasAscii -and -not $hasCjk) { continue }
            if ($techTokenPattern.IsMatch($value.Trim())) { continue }
            $m2++
        }
    }

    $total = $m1 + $m2 + $c1
    if ($total -gt 0) {
        $rows.Add([pscustomobject]@{
                Area       = Get-PortalArea -RelativePath $relative
                File       = $relative
                MarkupAttr = $m1
                MarkupText = $m2
                CodeText   = $c1
                Total      = $total
            })
    }
}

$areaSummary = $rows | Group-Object Area | ForEach-Object {
    [pscustomobject]@{
        Area       = $_.Name
        Files      = $_.Count
        MarkupAttr = ($_.Group | Measure-Object -Property MarkupAttr -Sum).Sum
        MarkupText = ($_.Group | Measure-Object -Property MarkupText -Sum).Sum
        CodeText   = ($_.Group | Measure-Object -Property CodeText -Sum).Sum
        Total      = ($_.Group | Measure-Object -Property Total -Sum).Sum
    }
} | Sort-Object -Property Total -Descending

Write-Host ''
Write-Host '=== 本地化清单（口径见本脚本头部注释） ==='
Write-Host ("  扫描文件数 = {0}" -f $files.Count)
Write-Host ''
$areaSummary | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
Write-Host ("  全站合计 = {0} 处 / {1} 个文件" -f (($rows | Measure-Object -Property Total -Sum).Sum), $rows.Count)
Write-Host ''
Write-Host ("=== 站点最多的 {0} 个文件 ===" -f $DetailTop)
$rows | Sort-Object -Property Total -Descending | Select-Object -First $DetailTop |
    Format-Table -AutoSize -Property Total, MarkupAttr, MarkupText, CodeText, File | Out-String -Width 200 | Write-Host

if ($OutputJson) {
    $payload = [pscustomobject]@{
        PortalRoot  = $root
        ScannedFiles = $files.Count
        AreaSummary = $areaSummary
        Files       = $rows | Sort-Object -Property Total -Descending
    }
    [System.IO.File]::WriteAllText($OutputJson, ($payload | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
    Write-Host ("  明细已写入 {0}" -f $OutputJson)
}
