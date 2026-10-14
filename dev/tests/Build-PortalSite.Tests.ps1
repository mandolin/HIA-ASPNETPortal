# C-anp-P22 `W-anp-P132`：Build-PortalSite 的正向与负向验证。
# 正向：已声明上架、双语齐备、parent 有效、站内链接可达 → Pass。
# 负向 1（N2）：页面中插入不存在的站内链接 → Fail（证明坏链会被抓到，这是 W132 的核心回归防护）。
# 负向 2（N1）：上架清单的 parent 改为导航骨架中不存在的分区 → Fail。

param()

$repo = 'K:\Project\Github_mandolin\HIA-ASPNETPortal'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$indexPath = Join-Path $repo 'site\index.md'
$publishedPath = Join-Path $repo 'site\_data\published.yml'

$indexBackup = [System.IO.File]::ReadAllText($indexPath, $utf8)
$publishedBackup = [System.IO.File]::ReadAllText($publishedPath, $utf8)

$failures = 0

function Invoke-Gate {
    $out = & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoProfile -File (Join-Path $repo 'dev\scripts\Build-PortalSite.ps1') 2>&1 | Out-String
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Output = $out }
}

function Restore {
    [System.IO.File]::WriteAllText($indexPath, $indexBackup, $utf8)
    [System.IO.File]::WriteAllText($publishedPath, $publishedBackup, $utf8)
}

Write-Host '=== 正向：应 Pass / exit 0 ==='
$pos = Invoke-Gate
Write-Host ('  EXIT=' + $pos.Exit)
if ($pos.Exit -ne 0) { Write-Host '  ⚠️ 正向失败'; $failures++ } else { Write-Host '  正向通过 ✅' }

Write-Host ''
Write-Host '=== 负向 1（N2 坏链）：首页插入 /no-such-page/，应 Fail / exit 1 ==='
[System.IO.File]::WriteAllText($indexPath, ($indexBackup + "`r`n`r`n[坏链探针](/no-such-page/)`r`n"), $utf8)
$n1 = Invoke-Gate
Write-Host ('  EXIT=' + $n1.Exit)
foreach ($h in (($n1.Output -split "`r?`n" | Where-Object { $_ -match 'N2' }) | Select-Object -First 2)) { Write-Host ('    ' + $h.Trim()) }
if ($n1.Exit -ne 1) { Write-Host '  ⚠️ 负向 1 失败：坏链未被抓到'; $failures++ } else { Write-Host '  负向 1 通过 ✅（坏链被抓到）' }

Restore

Write-Host ''
Write-Host '=== 负向 2（N1 无效 parent）：改 parent 为不存在的分区，应 Fail / exit 1 ==='
[System.IO.File]::WriteAllText($publishedPath, ($publishedBackup -replace 'parent: 产品概览', 'parent: 不存在分区'), $utf8)
$n2 = Invoke-Gate
Write-Host ('  EXIT=' + $n2.Exit)
foreach ($h in (($n2.Output -split "`r?`n" | Where-Object { $_ -match 'N1' }) | Select-Object -First 2)) { Write-Host ('    ' + $h.Trim()) }
if ($n2.Exit -ne 1) { Write-Host '  ⚠️ 负向 2 失败：无效 parent 未被抓到'; $failures++ } else { Write-Host '  负向 2 通过 ✅（无效 parent 被抓到）' }

Restore

Write-Host ''
Write-Host '=== 恢复后复跑，应 Pass / exit 0 ==='
$back = Invoke-Gate
Write-Host ('  EXIT=' + $back.Exit)
if ($back.Exit -ne 0) { $failures++ }

Write-Host ''
Write-Host ('验证失败项: ' + $failures)
if ($failures -gt 0) { exit 1 }
Write-Host '正向 + 负向 1 + 负向 2 均通过 ✅'
exit 0
