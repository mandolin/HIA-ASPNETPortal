# C-anp-P22 `W-anp-P130`：Build-PortalSite 的正向与负向验证。
# 正向：已声明上架且双语齐备 → Pass。
# 负向：已声明上架但缺英文译本 → Fail（证明门禁真的在校验，而非恒真）。

param()

$repo = 'K:\Project\Github_mandolin\HIA-ASPNETPortal'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$publishedPath = Join-Path $repo 'site\_data\published.yml'
$original = [System.IO.File]::ReadAllText($publishedPath, $utf8)

$failures = 0

function Invoke-Gate {
    $out = & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoProfile -File (Join-Path $repo 'dev\scripts\Build-PortalSite.ps1') 2>&1 | Out-String
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Output = $out }
}

Write-Host '=== 正向：已声明上架且双语齐备，应 Pass / exit 0 ==='
$pos = Invoke-Gate
Write-Host ('  EXIT=' + $pos.Exit)
if ($pos.Exit -ne 0) { Write-Host '  ⚠️ 正向失败'; $failures++ }
else { Write-Host '  正向通过 ✅' }

Write-Host ''
Write-Host '=== 负向：声明一个缺英文译本的页面，应 Fail / exit 1 ==='
[System.IO.File]::WriteAllText($publishedPath, ($original + "`r`n- architecture.md`r`n"), $utf8)
$neg = Invoke-Gate
Write-Host ('  EXIT=' + $neg.Exit)
$hit = ($neg.Output -split "`r?`n" | Where-Object { $_ -match 'architecture' })
foreach ($h in ($hit | Select-Object -First 2)) { Write-Host ('    ' + $h.Trim()) }
if ($neg.Exit -ne 1) { Write-Host '  ⚠️ 负向失败：门禁未报错（可能是橡皮章）'; $failures++ }
else { Write-Host '  负向通过 ✅（门禁确实会因缺译而失败）' }

# 恢复
[System.IO.File]::WriteAllText($publishedPath, $original, $utf8)
Write-Host ''
Write-Host '=== 恢复后复跑，应 Pass / exit 0 ==='
$back = Invoke-Gate
Write-Host ('  EXIT=' + $back.Exit)
$clean = & git -C $repo diff --quiet -- 'site/_data/published.yml'
Write-Host ('  工作树与 HEAD 一致: ' + $(if ($LASTEXITCODE -eq 0) { '是' } else { '否' }))
if ($back.Exit -ne 0) { $failures++ }

Write-Host ''
Write-Host ('验证失败项: ' + $failures)
if ($failures -gt 0) { exit 1 }
Write-Host '正向 + 负向均通过 ✅'
exit 0
