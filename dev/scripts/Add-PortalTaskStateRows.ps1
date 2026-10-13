<#
.SYNOPSIS
  <lang>
    <zh-CN>把数据文件里的行插入任务账本：末行替换"当前唯一下一步"行，其余插入其前。</zh-CN>
    <en>Inserts rows from a data file into the task ledger: the last line replaces the "current single next step" row and the rest are inserted before it.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>用数据文件而非内联字符串传入行内容，是为避开 PowerShell 的引号转义陷阱（含单引号的表格行会让单引号字符串提前结束）。锚点正则必须精确匹配账本中的"当前唯一下一步"行，命中后才在其前插入正文、并用数据文件末行覆盖该行；锚点写错会导致插入点偏移，属本脚本唯一高危点。
    **前置条件**：`-LedgerPath` 指向的账本文件、`-RowsPath` 指向的数据文件必须可读；数据文件至少 1 行内容 + 1 行下一步（共 ≥2 行）。
    **验证方式**：成功时直接改写账本并以退出码 0 结束；行数不足或锚点未命中时抛异常（非零退出码），不留下半成品文件。
    **低敏边界**：脚本只读写调用方指定的账本与数据文件，不访问网络、数据库或凭据。</zh-CN>
    <en>Rows are supplied through a data file rather than inline strings to avoid PowerShell quote-escaping traps, where a table row containing an apostrophe terminates a single-quoted string early. The anchor regex must precisely match the ledger's "current single next step" row; only after a hit is the body inserted before it and that row overwritten with the data file's last line. A wrong anchor shifts the insertion point — this script's only high-risk point.
    **Prerequisites**: the `-LedgerPath` ledger and the `-RowsPath` data file must be readable; the data file needs at least one content line plus one next-step line (two lines minimum).
    **Verification**: on success the ledger is rewritten and the script exits 0; too few rows or an unmatched anchor throws (non-zero exit) rather than leaving a half-written file.
    **Low-sensitivity boundary**: only the caller-specified ledger and data files are read or written — no network, database or credential access.</en>
  </lang>
.PARAMETER LedgerPath
  <lang>
    <zh-CN>目标任务账本文件路径。</zh-CN>
    <en>Path to the target task ledger file.</en>
  </lang>
.PARAMETER RowsPath
  <lang>
    <zh-CN>待插入行内容的数据文件路径。</zh-CN>
    <en>Path to the data file holding the rows to insert.</en>
  </lang>
.PARAMETER AnchorPattern
  <lang>
    <zh-CN>用于定位"当前唯一下一步"行的正则；命中后在该行前插入正文。</zh-CN>
    <en>Regex locating the "current single next step" row; the body is inserted before that row on a hit.</en>
  </lang>
#>

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

# <lang>
#   <zh-CN>锚点正则必须精确匹配账本里的"当前唯一下一步"行；命中后才在其前插入正文、并把数据文件末行覆盖为该行。锚点写错会导致插入点偏移，属本脚本唯一高危点。</zh-CN>
#   <en>The anchor regex must precisely match the ledger's "current single next step" row; only after a hit do we insert the body before it and overwrite that row with the data file's last line. A wrong anchor shifts the insertion point — the script's only high-risk point.</en>
# </lang>
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
