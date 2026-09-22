# <lang>
#   <zh-CN>
#     把数据文件里的行插入账本：最后一行替换"当前唯一下一步"行，其余插入其前。
#     用数据文件而非内联字符串，是为了避开 PowerShell 的引号转义陷阱（含单引号的表格行会让单引号字符串提前结束）。
#   </zh-CN>
#   <en>
#     Inserts rows from a data file into the ledger: the last line replaces the "current single next step" row and
#     the rest are inserted before it. A data file is used instead of inline strings to avoid PowerShell quote
#     escaping traps, where a table row containing an apostrophe terminates a single-quoted string early.
#   </en>
# </lang>
param(
    [Parameter(Mandatory)][string]$LedgerPath,
    [Parameter(Mandatory)][string]$RowsPath,
    [Parameter(Mandatory)][string]$AnchorPattern
)

$rows = [IO.File]::ReadAllLines($RowsPath) | Where-Object { $_.Trim().Length -gt 0 }
if ($rows.Count -lt 2) { throw '行数不足：至少需要 1 行内容 + 1 行下一步' }

$lines = New-Object System.Collections.Generic.List[string]
$lines.AddRange([IO.File]::ReadAllLines($LedgerPath))

$at = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match $AnchorPattern) { $at = $i; break }
}
if ($at -lt 0) { throw "未找到锚点: $AnchorPattern" }

$body = $rows[0..($rows.Count - 2)]
$next = $rows[$rows.Count - 1]
$lines.InsertRange($at, [string[]]$body)
$lines[$at + $body.Count] = $next

[IO.File]::WriteAllLines($LedgerPath, $lines, (New-Object System.Text.UTF8Encoding $false))

$chk = [IO.File]::ReadAllLines($LedgerPath)
$n = 0
for ($i = 0; $i -lt $chk.Count; $i++) { if ($chk[$i] -match '^\| \*\*当前唯一下一步\*\* \|') { $n++ } }
Write-Host "  插入内容行 = $($body.Count) ; 下一步行匹配数 = $n"
Write-Host "  锚点行 -> 新下一步行: $($next.Substring(0, [Math]::Min(38, $next.Length)))..."
