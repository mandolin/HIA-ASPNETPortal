# C-anp-P22 `W-anp-P133`：站点公开面门禁的负向验证。
# 对 S1（私有仓库链接）/ S2（凭据形态）/ S3（绝对本地路径）/ S4（内部计划路径）各插一处探针，
# 确认门禁能被触发；随后删除探针文件并复跑确认恢复 Pass。

param()

$repo = 'K:\Project\Github_mandolin\HIA-ASPNETPortal'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$probeDir = Join-Path $repo 'site\_probe'
$probeFile = Join-Path $probeDir 'probe.md'

function Invoke-Gate {
    $out = & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoProfile -File (Join-Path $repo 'dev\scripts\Test-PortalSitePublicSurface.ps1') 2>&1 | Out-String
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Output = $out }
}

$failures = 0

Write-Host '=== 基线（无探针）应 Pass / exit 0 ==='
$base = Invoke-Gate
Write-Host ('  EXIT=' + $base.Exit)
if ($base.Exit -ne 0) { Write-Host '  ⚠️ 基线失败'; $failures++ } else { Write-Host '  基线通过 ✅' }

# 探针内容：四项各一
$probe = @(
    'S1 probe: [internal](https://example.invalid/work-zone/dev/plans/x.md)',
    '',
    'S2 probe: password: hunter2',
    '',
    'S3 probe: K:\Project\secret-notes.md',
    '',
    'S4 probe: work-zone/dev/plans/C-anp-P22.md'
) -join "`r`n"

$null = New-Item -ItemType Directory -Path $probeDir -Force
[System.IO.File]::WriteAllText($probeFile, $probe, $utf8)
$null = & git -C $repo add -A 'site/_probe' 2>&1

Write-Host ''
Write-Host '=== 插入四类探针后，应 Fail / exit 1 且四类全部命中 ==='
$neg = Invoke-Gate
Write-Host ('  EXIT=' + $neg.Exit)
$hitIds = @()
foreach ($line in ($neg.Output -split "`r?`n")) {
    if ($line -match '^\s*- (S[1-4]) ') { $hitIds += $Matches[1] }
}
$hitIds = @($hitIds | Sort-Object -Unique)
Write-Host ('  命中项: ' + ($hitIds -join ', '))
foreach ($id in @('S1', 'S2', 'S3', 'S4')) {
    if ($hitIds -contains $id) { Write-Host ('    ' + $id + ' ✅ 被抓到') } else { Write-Host ('    ' + $id + ' ⚠️ 未抓到'); $failures++ }
}
if ($neg.Exit -ne 1) { Write-Host '  ⚠️ 门禁未失败'; $failures++ }

# 清理探针
Remove-Item -Recurse -Force $probeDir
$null = & git -C $repo rm -r --cached --quiet 'site/_probe' 2>&1

Write-Host ''
Write-Host '=== 清理探针后复跑，应 Pass / exit 0 ==='
$back = Invoke-Gate
Write-Host ('  EXIT=' + $back.Exit)
if ($back.Exit -ne 0) { Write-Host '  ⚠️ 未恢复'; $failures++ }

Write-Host ''
Write-Host ('验证失败项: ' + $failures)
if ($failures -gt 0) { exit 1 }
Write-Host 'S1/S2/S3/S4 四项负向验证均通过 ✅'
exit 0
