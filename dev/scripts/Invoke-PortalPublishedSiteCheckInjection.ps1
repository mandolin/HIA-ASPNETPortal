<#
.SYNOPSIS
<lang>
  <zh-CN>发布后线上复核门禁的**本地注入轨**（按需工具）：起一个本地静态服务承载"模拟发布页"，用**同一个门禁脚本**指向它，逐项注入缺陷并断言门禁真的会发现；全程不动真实线上站点。</zh-CN>
  <en>Local injection track for the post-publish live re-check gate (on-demand tool): a local static server hosts
  "simulated published pages", the **same gate script** is pointed at it, defects are injected one by one and the gate
  must demonstrate that it finds them; the real live site is never touched.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    为什么需要它：线上复核门禁（`Test-PortalPublishedSiteChecks.ps1`）在**真实线上**跑，而"证明它真的能发现
    缺陷"不能靠改线上内容来做。本工具把这条证明**本地化**：由 `site/` 的镜像页合成 84 个模拟发布页
    （标题取自 front matter、正文取自镜像内容、导航块链向每个页面），起一个本地静态服务，然后让门禁
    把 `-BaseUrl` 指向它。于是"注入—观测—还原"全部在本地完成，可随时一键复跑，且**不需要网络**。

    覆盖的用例（每个都给出**期望退出码**与**期望输出模式**）：
      基线（无注入）                       -> exit 0
      I1 页面内注入内部编号                -> exit 1 + `C4-INTERNAL-ID`
      I2 改页面的 `<title>`                -> exit 1 + `C2-TITLE`
      I3 删掉指向某页的全部导航链接        -> exit 1 + `C3-NAV`
      I4 等待窗口内可达、随后被删          -> exit 1 + `C1-HTTP`
      I5 页面从一开始就不存在              -> exit 2 + `[INCOMPLETE]`
      I6 某页构建戳变旧（页面仍完全可达）  -> exit 2 + `构建戳未更新`
      I7 所有页面都没有构建戳              -> exit 2 + `没有任何页面带构建戳`
      还原后复跑                           -> exit 0

    **期望退出码是刻意的**：I4/I5 这一对证明"**曾是活的、后来没了**"（缺陷，exit 1）与"**从未确认可达**"
    （未确认，exit 2）是**两种不同状态**；I6 则证明"页面完全可达、四项判据全过"时，只有构建戳能把它
    从"通过"拉回"未确认"——那是"全部可达"这个粗判据漏掉的形态。

    **断言匹配的是判据编号，不是自然语言**：`C4-INTERNAL-ID` 而不是"页面内含内部编号"。编号是稳定接口，
    改一句消息措辞不会让本工具静默失效（那是上一周期真实发生过的一次返工）。

    每次注入后**字节级还原**并逐字节校验；被"移走/删除"的文件一律**无条件写回**（只按 `Test-Path` 判断会
    在文件被删时跳过还原，把仓库留在缺文件的状态——上一周期真实踩过一次）。

    本工具**按需手工执行**，不进日常门禁编排（它需要起本地端口，且用途是证明门禁而非守仓库）。
  </zh-CN>
  <en>
    Why it exists: the live re-check gate (`Test-PortalPublishedSiteChecks.ps1`) runs **against the real site**, and
    "prove it actually detects defects" cannot be done by modifying the live site. This tool **localises** that proof: it
    synthesises 84 simulated published pages from the `site/` mirrors (title from front matter, body from mirror content,
    a nav block linking every page), starts a local static server, and points the gate at it via `-BaseUrl`. Inject,
    observe and restore then all happen locally — one command, repeatable any time, **no network needed**.

    Cases covered (each with an **expected exit code** and an **expected output pattern**):
      baseline (no injection)                    -> exit 0
      I1 internal numbering injected into a page -> exit 1 + `C4-INTERNAL-ID`
      I2 the page's `<title>` is changed         -> exit 1 + `C2-TITLE`
      I3 every nav link to a page is removed     -> exit 1 + `C3-NAV`
      I4 reachable inside the wait window, then deleted -> exit 1 + `C1-HTTP`
      I5 the page never exists                   -> exit 2 + `[INCOMPLETE]`
      I6 one page's build stamp is stale (page still fully reachable) -> exit 2 + `构建戳未更新`
      I7 no page carries a build stamp           -> exit 2 + `没有任何页面带构建戳`
      restored re-run                            -> exit 0

    **The expected exit codes are deliberate**: the I4/I5 pair proves that "**was live, then gone**" (a defect, exit 1)
    and "**never confirmed reachable**" (unconfirmed, exit 2) are **two different states**; I6 proves that when a page is
    fully reachable and passes all four criteria, only the build stamp pulls it back from "pass" to "unconfirmed" — the
    shape that the coarse "all reachable" signal misses.

    **Assertions match criterion IDs, not prose**: `C4-INTERNAL-ID`, not "the page contains internal numbering". IDs are
    the stable interface, so rewording a message cannot silently invalidate this tool (a rework that really happened in
    the previous cycle).

    Every injection is followed by a **byte-level restore** with byte-by-byte verification; files that were "moved away"
    or deleted are **written back unconditionally** (deciding by `Test-Path` alone skips the restore when the file is
    gone, leaving the repository missing a file — really hit once in the previous cycle).

    This tool is **run manually on demand** and is not part of daily gate orchestration (it opens a local port, and its
    purpose is proving the gate rather than guarding the repository).
  </en>
</lang>

.PARAMETER Port
<lang>
  <zh-CN>本地静态服务端口（默认 8975）。与其它本地服务冲突时可改。</zh-CN>
  <en>Local static server port (default 8975). Change it if another local service conflicts.</en>
</lang>

.PARAMETER StampWaitSeconds
<lang>
  <zh-CN>构建戳相关用例（I6/I7）使用的等待窗口秒数。必须 **> 0**：门禁只在带等待窗口的复核中判定构建戳。</zh-CN>
  <en>Wait window in seconds used by the build-stamp cases (I6/I7). Must be **> 0**: the gate judges the build stamp only
  in a re-check with a wait window.</en>
</lang>

.PARAMETER KeepArtifacts
<lang>
  <zh-CN>保留本次合成的模拟发布页（默认在结束后清理），便于人工复查。</zh-CN>
  <en>Keep the synthesised simulated pages (they are cleaned up by default), for manual inspection.</en>
</lang>

.Proof boundary（证明边界）
<lang>
  <zh-CN>
    只证明"**在模拟发布页上，门禁能发现各类注入**"。**不证明**真实线上站点的状态（那由门禁本身在线上跑
    来证明，本工具不访问外网）；**不替代**镜像新鲜度门禁（那是"仓库内 `site/` 是否等于构建产物"）；
    **不覆盖**门禁未声明的判据。模拟页由镜像页合成，与 Jekyll 真实渲染**不等价**（本工具自己拼 HTML，
    不跑 Jekyll）——它证明的是门禁的**判定逻辑**，不是渲染结果。
  </zh-CN>
  <en>
    Proves only that "**on simulated published pages, the gate detects each class of injection**". It does **not** prove
    the state of the real live site (that is what the gate itself proves when run online; this tool never reaches the
    network); it does **not** replace the mirror-freshness gate ("does the repository's `site/` equal the build output");
    it does **not** cover criteria the gate does not declare. The simulated pages are synthesised from mirror pages and
    are **not equivalent to real Jekyll rendering** (this tool assembles HTML itself and does not run Jekyll) — what it
    proves is the gate's **decision logic**, not rendering output.
  </en>
</lang>
#>
param(
    [int] $Port = 8975,
    [int] $StampWaitSeconds = 8,
    [switch] $KeepArtifacts
)

$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$gate = Join-Path $repoRoot 'dev/scripts/Test-PortalPublishedSiteChecks.ps1'
$siteRoot = Join-Path $repoRoot 'site'
$stage = Join-Path $repoRoot ('temp/published-site-injection-' + (Get-Date -Format 'yyyyMMddTHHmmss'))

if (-not (Test-Path -LiteralPath $gate)) { Write-Host ('[FAIL] 找不到门禁脚本：' + $gate); exit 1 }
# pwsh：先 PATH，再常见安装目录（与门禁编排脚本的做法一致，避免本机差异导致"门禁跑不起来"被误读成"注入失败"）
$pwsh = 'pwsh'
$onPath = Get-Command 'pwsh' -ErrorAction SilentlyContinue
if ($onPath) { $pwsh = $onPath.Source }
elseif (Test-Path -LiteralPath "$env:ProgramFiles\PowerShell\7\pwsh.exe") { $pwsh = "$env:ProgramFiles\PowerShell\7\pwsh.exe" }

$failed = 0

# ---------- 1) 合成"模拟发布页" ----------
# <lang>
#   <zh-CN>**英文页必须放在 `/en/<path>/` 下**（与线上 URL 命名空间一致），不是 `<path>/en/`——后者是
#     **镜像目录**的布局。首版按镜像布局放，导致 42 个英文页在本轨全部 404：那是合成脚本的缺陷，
#     不是门禁的（门禁的 URL 构造已被线上 84/84 证明是对的）。</zh-CN>
#   <en>**English pages must live under `/en/<path>/`** (the live URL namespace), not `<path>/en/` — the latter is the
#     **mirror directory** layout. The first version used the mirror layout, so all 42 English pages 404'd in this
#     track: a defect of the synthesis script, not of the gate (whose URL construction is already proven correct by the
#     84/84 live run).</en>
# </lang>
if (Test-Path -LiteralPath $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null
$manifest = [System.IO.File]::ReadAllText((Join-Path $siteRoot '_data/published.yml'), $utf8)
$entries = @([regex]::Matches($manifest, '(?ms)- doc:\s*(.+?)\r?\n\s*path:\s*(.+?)\r?\n') | ForEach-Object {
    [pscustomobject]@{ doc = $_.Groups[1].Value.Trim(); path = $_.Groups[2].Value.Trim() }
})
$navZh = @(); $navEn = @()
foreach ($e in $entries) {
    $navZh += ('<a href="/HIA-ASPNETPortal/' + $e.path + '/">zh-' + $e.path + '</a>')
    $navEn += ('<a href="/HIA-ASPNETPortal/en/' + $e.path + '/">en-' + $e.path + '</a>')
}
$navZhHtml = ($navZh -join ''); $navEnHtml = ($navEn -join '')
foreach ($e in $entries) {
    foreach ($lang in @('zh', 'en')) {
        $mirrorRel = if ($lang -eq 'zh') { $e.path + '/index.md' } else { $e.path + '/en/index.md' }
        $mirror = Join-Path $siteRoot ($mirrorRel -replace '/', '\')
        $title = $e.path
        if (Test-Path -LiteralPath $mirror) {
            $fm = [System.IO.File]::ReadAllText($mirror, $utf8)
            $m = [regex]::Match($fm, '(?m)^title:\s*(.+?)\s*$')
            if ($m.Success) { $title = $m.Groups[1].Value.Trim() }
        }
        $nav = if ($lang -eq 'zh') { $navZhHtml } else { $navEnHtml }
        $body = if (Test-Path -LiteralPath $mirror) { [System.IO.File]::ReadAllText($mirror, $utf8) } else { '(no mirror)' }
        $html = @"
<!DOCTYPE html>
<html lang="$lang"><head><meta charset="utf-8"><title>$title | HIA-ASPNETPortal</title></head>
<body><nav>$nav</nav><main>$body</main></body></html>
"@
        $subDir = if ($lang -eq 'zh') { $e.path } else { 'en/' + $e.path }
        $outDir = Join-Path $stage ($subDir -replace '/', '\')
        New-Item -ItemType Directory -Force $outDir | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $outDir 'index.html'), $html, $utf8)
    }
}
Write-Host ('[INFO] 模拟发布页已合成：' + @(Get-ChildItem $stage -Recurse -File -Filter 'index.html').Count + ' 页（' + $entries.Count + ' 条上架声明 × 中英）')

# ---------- 2) 起本地静态服务 ----------
$job = Start-Job -ScriptBlock {
    param($Root, $Port)
    $listener = New-Object System.Net.HttpListener
    $listener.Prefixes.Add("http://localhost:$Port/")
    $listener.Start()
    try {
        while ($true) {
            $ctx = $listener.GetContext()
            $rel = $ctx.Request.Url.AbsolutePath.TrimStart('/')
            if ([string]::IsNullOrWhiteSpace($rel)) { $rel = 'index.html' }
            if ($rel.EndsWith('/')) { $rel += 'index.html' }
            $file = Join-Path $Root ($rel -replace '/', '\')
            if (Test-Path -LiteralPath $file -PathType Leaf) {
                $bytes = [System.IO.File]::ReadAllBytes($file)
                $ctx.Response.StatusCode = 200
                $ctx.Response.ContentType = 'text/html; charset=utf-8'
                $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
            }
            else {
                $ctx.Response.StatusCode = 404
                $nb = (New-Object System.Text.UTF8Encoding($false)).GetBytes('not found')
                $ctx.Response.OutputStream.Write($nb, 0, $nb.Length)
            }
            $ctx.Response.Close()
        }
    }
    finally { $listener.Stop(); $listener.Close() }
} -ArgumentList $stage, $Port
Start-Sleep -Seconds 2
$base = "http://localhost:$Port"
try {
    $probe = Invoke-WebRequest -Uri ($base + '/modules/announcements/') -TimeoutSec 10 -ErrorAction Stop
    Write-Host ('[INFO] 本地静态服务已就绪（HTTP ' + $probe.StatusCode + '，端口 ' + $Port + '）')
}
catch { Write-Host ('[WARN] 本地服务探测失败：' + $_.Exception.Message) }

# ---------- 3) 用例框架 ----------
function Invoke-Gate {
    param([int] $Wait)
    $args = @('-NoProfile', '-NoLogo', '-File', $gate, '-BaseUrl', $base)
    if ($Wait -gt 0) { $args += @('-WaitSeconds', [string]$Wait) }
    $out = & $pwsh @args 2>&1
    return @{ code = $LASTEXITCODE; text = (($out | ForEach-Object { $_.ToString() }) -join "`n") }
}
function Assert-Case {
    param([string] $Label, [int] $ExpectExit, [string] $ExpectPattern, [int] $Wait)
    $r = Invoke-Gate -Wait $Wait
    $hit = if ($ExpectPattern) { $r.text -match $ExpectPattern } else { $true }
    if ($r.code -eq $ExpectExit -and $hit) {
        Write-Host ('  [OK]   ' + $Label + ' -> exit ' + $r.code + $(if ($ExpectPattern) { '｜命中「' + $ExpectPattern + '」' } else { '' }))
    }
    else {
        Write-Host ('  [FAIL] ' + $Label + ' -> exit ' + $r.code + '（期望 ' + $ExpectExit + '）｜命中=' + $hit)
        ($r.text -split "`n" | Where-Object { $_ -match '\[FAIL\]|\[INCOMPLETE\]|\[PASS\]|DEFECT|UNCONFIRMED' } | Select-Object -First 4) | ForEach-Object { '        ' + $_.Trim() }
        $script:failed++
    }
}
# <lang>
#   <zh-CN>还原一律**无条件写回**并逐字节校验：只按 `Test-Path` 判断会在文件被删时跳过还原，
#     把仓库留在缺文件的状态（上一周期真实踩过一次，若当时收工，仓库会带着缺失的镜像被提交）。</zh-CN>
#   <en>Restore is always an **unconditional write-back** with byte-by-byte verification: deciding by `Test-Path` alone
#     skips the restore when the file has been deleted, leaving the repository missing a file (really hit once in the
#     previous cycle; had we stopped then, the repository would have been committed with a missing mirror).</en>
# </lang>
function Restore-Bytes {
    param([string] $Path, [byte[]] $Bytes)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllBytes($Path, $Bytes)
    $now = [System.IO.File]::ReadAllBytes($Path)
    if ($now.Length -ne $Bytes.Length) { Write-Host ('  [FAIL] 还原失败（长度不同）：' + $Path); $script:failed++; return }
    for ($i = 0; $i -lt $Bytes.Length; $i++) {
        if ($now[$i] -ne $Bytes[$i]) { Write-Host ('  [FAIL] 还原失败（第 ' + $i + ' 字节）：' + $Path); $script:failed++; return }
    }
}

$zhAnn = Join-Path $stage 'modules\announcements\index.html'
$ctHtml = Join-Path $stage 'modules\contacts\index.html'
$origAnnBytes = [System.IO.File]::ReadAllBytes($zhAnn)
$origAnn = [System.IO.File]::ReadAllText($zhAnn, $utf8)
$allPages = @(Get-ChildItem $stage -Recurse -File -Filter 'index.html')
$snapshot = @{}
foreach ($f in $allPages) { $snapshot[$f.FullName] = [System.IO.File]::ReadAllBytes($f.FullName) }

try {
    Assert-Case '基线（无注入）' 0 '' 0

    # I1 C4
    [System.IO.File]::WriteAllText($zhAnn, ($origAnn -replace '</main>', '<!-- W-anp-P999 injection --></main>'), $utf8)
    Assert-Case 'I1 页面内注入内部编号（C4）' 1 'C4-INTERNAL-ID' 0
    Restore-Bytes $zhAnn $origAnnBytes

    # I2 C2
    [System.IO.File]::WriteAllText($zhAnn, ($origAnn -replace '<title>[^<]*</title>', '<title>篡改后的标题 | HIA-ASPNETPortal</title>'), $utf8)
    Assert-Case 'I2 改页面标题（C2）' 1 'C2-TITLE' 0
    Restore-Bytes $zhAnn $origAnnBytes

    # I3 C3：删掉指向 events 的全部导航链接
    foreach ($f in $allPages) {
        $t2 = [regex]::Replace([System.IO.File]::ReadAllText($f.FullName, $utf8), '<a href="[^"]*/modules/events/">[^<]*</a>', '')
        [System.IO.File]::WriteAllText($f.FullName, $t2, $utf8)
    }
    Assert-Case 'I3 删除指向 events 的全部导航链接（C3）' 1 'C3-NAV' 0
    foreach ($k in $snapshot.Keys) { Restore-Bytes $k $snapshot[$k] }

    # I4 C1：等待窗口内可达、随后被删（证明"曾可达又消失"是缺陷，与 I5 的"从未可达"不同）
    $gateJob = Start-Job -ScriptBlock {
        param($Pwsh, $Gate, $Base, $Wait)
        & $Pwsh -NoProfile -NoLogo -File $Gate -BaseUrl $Base -WaitSeconds $Wait 2>&1
    } -ArgumentList $pwsh, $gate, $base, 12
    Start-Sleep -Seconds 4
    Remove-Item -LiteralPath $ctHtml -Force
    Wait-Job $gateJob | Out-Null
    $i4Text = ((Receive-Job $gateJob | ForEach-Object { $_.ToString() }) -join "`n")
    Remove-Job $gateJob -Force
    if ($i4Text -match 'C1-HTTP') { Write-Host '  [OK]   I4 等待窗口内可达、随后被删（C1）-> 判为缺陷｜命中「C1-HTTP」' }
    else {
        Write-Host '  [FAIL] I4 未命中 C1 缺陷'
        ($i4Text -split "`n" | Where-Object { $_ -match '\[FAIL\]|\[INCOMPLETE\]|\[PASS\]|DEFECT' } | Select-Object -First 4) | ForEach-Object { '        ' + $_.Trim() }
        $failed++
    }
    Restore-Bytes $ctHtml $snapshot[$ctHtml]

    # I5 从未存在 -> INCOMPLETE（不当作通过）
    Remove-Item -LiteralPath $ctHtml -Force
    Assert-Case 'I5 页面从一开始就不存在（应为 INCOMPLETE）' 2 '\[INCOMPLETE\]' 0
    Restore-Bytes $ctHtml $snapshot[$ctHtml]

    # I6 构建戳变旧（页面仍完全可达、四项判据仍全过——只有戳能把它拉回"未确认"）
    $stale = [System.IO.File]::ReadAllText($zhAnn, $utf8) -replace 'build-stamp:\s*[0-9a-fA-F]+', 'build-stamp: 000000000000'
    [System.IO.File]::WriteAllText($zhAnn, $stale, $utf8)
    Assert-Case 'I6 构建戳变旧（可达且四项全过，仍应判未确认）' 2 '构建戳未更新' $StampWaitSeconds
    Restore-Bytes $zhAnn $origAnnBytes

    # I7 所有页面都没有构建戳（模拟早于本机制的站点）
    $noStampBackup = @{}
    foreach ($f in $allPages) {
        $noStampBackup[$f.FullName] = [System.IO.File]::ReadAllBytes($f.FullName)
        [System.IO.File]::WriteAllText($f.FullName, ([System.IO.File]::ReadAllText($f.FullName, $utf8) -replace '<!-- build-stamp:[^>]*-->', ''), $utf8)
    }
    $r7 = Invoke-Gate -Wait $StampWaitSeconds
    $hit7 = $r7.text.Contains('没有任何页面带构建戳')
    if ($r7.code -eq 2 -and $hit7) { Write-Host '  [OK]   I7 所有页面都没有构建戳 -> exit 2｜命中「没有任何页面带构建戳」' }
    else { Write-Host ('  [FAIL] I7 -> exit ' + $r7.code + '（期望 2）｜命中=' + $hit7); $failed++ }
    if ($r7.text.Contains('[DEFECT]')) { Write-Host '  [FAIL] I7 把"无戳"误报成了缺陷'; $failed++ }
    else { Write-Host '  [OK]   I7 未把"无戳"误报为缺陷' }
    foreach ($k in $noStampBackup.Keys) { Restore-Bytes $k $noStampBackup[$k] }

    Assert-Case '还原后复跑' 0 '' 0
}
finally {
    if ($job) { Stop-Job $job -ErrorAction SilentlyContinue; Remove-Job $job -Force -ErrorAction SilentlyContinue }
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $stage)) { Remove-Item -Recurse -Force $stage }
    else { Write-Host ('[INFO] 已按 -KeepArtifacts 保留：' + $stage) }
}

if ($failed -eq 0) {
    Write-Host '[PASS] 发布后线上复核本地注入轨：全部用例通过（基线 + I1-I7 + 还原后，均字节级还原）。'
    exit 0
}
Write-Host ('[FAIL] 发布后线上复核本地注入轨失败：' + $failed + ' 项。')
exit 1
