<#
.SYNOPSIS
    按计划表对标记/代码做精确字符串替换，并逐条核对出现次数。

.DESCRIPTION
    # <lang>
    #   <zh-CN>
    #     为什么需要本工具：同构页面（例如 8 个 EditXxx 旧编辑页）可以批量替换，但"盲替换"很危险——
    #     一旦对原文形态的假设错误，就会静默改错或静默漏改。本工具对每条替换**核对出现次数**：
    #     与预期不符则**跳过该条、计入失败、以退出码汇报**，把"假设错误"变成当场可见的失败，
    #     而不是留到编译期或运行期才暴露。
    #
    #     计划表为 JSON 数组，每项 { "file": "<相对仓库路径>", "old": "…", "new": "…", "expect": <次数> }。
    #     file 相对于**仓库根**（脚本按自身位置上溯两级推算），与口径/键工具的约定一致。
    #   </zh-CN>
    #   <en>
    #     Why this tool exists: isomorphic pages (for example the eight EditXxx legacy edit pages) can be updated in
    #     bulk, but blind replacement is dangerous — a wrong assumption about the source text either silently changes
    #     the wrong thing or silently misses the target. This tool verifies the occurrence count of every entry: on
    #     mismatch it skips that entry, counts a failure, and reports through the exit code, so a wrong assumption
    #     becomes an immediate visible failure instead of surfacing at compile time or run time.
    #
    #     The plan is a JSON array of { "file": "<path relative to the repository root>", "old": "...",
    #     "new": "...", "expect": <count> }. Paths are relative to the repository root, which the script derives two
    #     levels up from its own location, matching the inventory and key tools.
    #   </en>
    # </lang>

.PARAMETER PlanPath
    替换计划 JSON 路径。

.EXAMPLE
    pwsh -File dev/scripts/Apply-PortalLocalizationReplacements.ps1 -PlanPath temp/plan.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })][string]$PlanPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$plan = [IO.File]::ReadAllText((Resolve-Path $PlanPath).Path) | ConvertFrom-Json

$fail = 0
$done = 0
$touched = New-Object System.Collections.Generic.HashSet[string]

foreach ($row in $plan) {
    $path = Join-Path $repoRoot $row.file
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host ("  [失败] 文件不存在: {0}" -f $row.file)
        $fail++
        continue
    }
    $text = [IO.File]::ReadAllText($path)
    $count = ([regex]::Matches($text, [regex]::Escape($row.old))).Count
    if ($count -ne [int]$row.expect) {
        Write-Host ("  [失败] {0} : 期望 {1} 处、实际 {2} 处 -> [{3}]" -f $row.file, $row.expect, $count, $row.old)
        $fail++
        continue
    }
    [IO.File]::WriteAllText($path, $text.Replace($row.old, $row.new), (New-Object System.Text.UTF8Encoding $false))
    [void]$touched.Add($row.file)
    Write-Host ("  [完成] {0} : {1} 处" -f $row.file, $count)
    $done++
}

Write-Host ("  小结: 完成 {0} 条 / 失败 {1} 条 / 涉及文件 {2} 个" -f $done, $fail, $touched.Count)
if ($fail -gt 0) { exit 1 }
