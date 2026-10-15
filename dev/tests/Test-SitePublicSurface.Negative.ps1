# C-anp-P22 `W-anp-P133`：站点公开面门禁的负向验证。
# 对 S1（私有仓库链接）/ S2（凭据形态）/ S3（绝对本地路径）/ S4（私有仓库名或路径）各插一处探针，
# 确认门禁能被触发；随后删除探针文件并复跑确认恢复 Pass。
#
# `W-anp-P141` 追加两项：
#   - **裸提名**：`WorkZone`（驼峰，后接中文"或"/"，"而非路径分隔符）—— 旧 S4 要求"后接分隔符"故漏网。
#   - **不误报**：英文正常词 "work zone"（工作区，带空格）**必须 Pass** —— 若它也被抓，
#     说明门禁是靠误报制造"看起来在检查"的假象，那才是真正的橡皮章反面。

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

# <lang>
#   <zh-CN>**轮 2：S4 裸提名形态**（`W-anp-P141` 新增）。旧 S4 要求仓库名后接**路径分隔符**，
#     而文档里的实际形态是「不得把共享开发密码写入仓库、WorkZone、日志或截图」——
#     后接中文"或"，一律漏网。译本批次 11、12 连续两批都由人工审查补上，故固化为机制。</zh-CN>
# </lang>
Write-Host ''
Write-Host '=== 轮 2：S4 裸提名 WorkZone（后接中文，非路径分隔符）应 Fail / exit 1 ==='
$bareName = "不得把共享开发密码写入仓库、WorkZone、日志或截图。"
$null = New-Item -ItemType Directory -Path $probeDir -Force
[System.IO.File]::WriteAllText($probeFile, $bareName, $utf8)
$null = & git -C $repo add -A 'site/_probe' 2>&1
$r2 = Invoke-Gate
Write-Host ('  EXIT=' + $r2.Exit)
if (($r2.Exit -ne 1) -or ($r2.Output -notmatch '- S4 ')) {
    Write-Host '  ⚠️ 裸提名未被抓到（S4 盲区复现）'
    $failures++
}
else { Write-Host '  裸提名被抓到 ✅' }
Remove-Item -Recurse -Force $probeDir
$null = & git -C $repo rm -r --cached --quiet 'site/_probe' 2>&1

# <lang>
#   <zh-CN>**轮 3：不得误报英文正常词**（`W-anp-P141` 新增，与轮 2 同等重要）。
#     若英文 "work zone"（工作区，带空格）也被判违规，门禁就变成了靠**噪音**冒充检查：
#     真问题会被淹没在一堆假阳性里，最终导致无人看报告 —— 那才是橡皮章的实质。
#     故 S4 的分隔符**刻意只允许 `-`/`_` 而不允许空格**，此处正是守护这条口径。</zh-CN>
# </lang>
Write-Host ''
Write-Host '=== 轮 3：英文正常词 "work zone"（带空格）应 Pass / exit 0（不误报）==='
$cjk = 'The work zone must remain clean during maintenance.'
$null = New-Item -ItemType Directory -Path $probeDir -Force
[System.IO.File]::WriteAllText($probeFile, $cjk, $utf8)
$null = & git -C $repo add -A 'site/_probe' 2>&1
$r3 = Invoke-Gate
Write-Host ('  EXIT=' + $r3.Exit)
if ($r3.Exit -ne 0) { Write-Host '  ⚠️ 误报：英文"work zone"被判违规'; $failures++ }
else { Write-Host '  未误报 ✅' }
Remove-Item -Recurse -Force $probeDir
$null = & git -C $repo rm -r --cached --quiet 'site/_probe' 2>&1

Write-Host ''
Write-Host '=== 最终清理后复跑，应 Pass / exit 0 且探针无残留 ==='
$final = Invoke-Gate
Write-Host ('  EXIT=' + $final.Exit)
if ($final.Exit -ne 0) { Write-Host '  ⚠️ 未恢复'; $failures++ }
if (Test-Path $probeDir) { Write-Host '  ⚠️ 探针残留'; $failures++ }

Write-Host ''
Write-Host ('验证失败项: ' + $failures)
if ($failures -gt 0) { exit 1 }
Write-Host 'S1/S2/S3/S4 四项负向验证 + S4 裸提名 + 不误报 均通过 ✅'
exit 0
