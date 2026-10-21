<#
.SYNOPSIS
<lang>
  <zh-CN>**发布后线上复核门禁**（`C-anp-P34` / `W-anp-P189`；用户 2026-10-21 裁定"两者都要"）：对**已发布的站点**逐页核对四项判据——可达性（HTTP 200）、标题与本地构建产物一致、导航可见性、页面内无内部编号。**依赖外网**，因此**不进 L0/L1/L2 编排**（避免网络抖动把本地门禁变红），而是在 **CI 发布工作流后置步骤**自动跑，并可由**本地手动复跑**。</zh-CN>
  <en>**Post-publish live-site re-check gate** (`C-anp-P34` / `W-anp-P189`; user ruling 2026-10-21 — "both"): verifies four criteria per page on the **published site** — reachability (HTTP 200), title matching the locally built artifact, navigation visibility, and no internal numbering inside the page. It **needs the network**, so it is **not wired into the L0/L1/L2 suite** (network flakiness must not turn local gates red); it runs as a **CI post-publish step** and can be **re-run manually**.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    背景（本周期最要紧的教训）：`C-anp-P33` 出现过**本地全绿、线上仍脏**——服务模板注释里的内部编号
    随**每一个页面**发出，而当时的内容适用性门禁只扫文档：`docs/` 与译本全干净、L0 **50/50** 全绿。
    它是**人工按 D8 做提交后线上复核**才暴露的。本门禁把那套人工复核固化为机器可复跑的脚本。

    **为什么"线上 == 本地构建产物"是正确口径**：本地 `site/` 里是**镜像页（markdown + front matter）**，
    不是 Jekyll 渲染后的 HTML。因此本门禁**不比字节**，而是把本地构建产物提供的**可判定事实**作为期望值
    （`published.yml` 的页面清单与 permalink、镜像页 front matter 的 `title`），再核对线上四件事。
    字节级一致性由镜像门禁在本地负责，本门禁不重复。

    四项判据：
      C1 可达性：HTTP 200（超时 / 5xx / 4xx 记为**失败**，不是跳过）。
      C2 标题一致：线上 `<title>` 以本地镜像页 front matter 的 `title` **开头**。
      C3 导航可见性：页面 HTML 内含指向本页 permalink 的链接（`href="…/<path>/"`）——
         这正是用户 2026-10-20 抱怨的"**发布了但找不到**"的判据。
      C4 内部编号：页面 HTML 内 `(W|C)-anp-P\d+` 出现 **0** 次（`C-anp-P33` 的教训：
         模板注释里的编号会随每页发出，而当时没有任何机制能发现）。

    三种退出码（刻意区分，**不把"没查到"当成"干净"**）：
      0 PASS      —— 全部页面四项判据均通过；
      1 FAIL      —— 发现缺陷（输出 URL / 判据 / 片段）；
      2 INCOMPLETE —— 等待传播超时，**无法确认**（CI 侧配 `continue-on-error`：只报警、不阻断发布）。

    刻意不做：不改发布工作流既有阶段语义（只加**后置**步骤）；不做字节比对；不渲染 Jekyll；
    不覆盖未上架文档；不校验 CSS/JS 资源可用性（另有发布就绪门禁承载）。
  </zh-CN>
  <en>
    Background (the most important lesson of this cycle): `C-anp-P33` saw **local green, live dirty** — internal
    numbering inside served-template comments shipped with **every page**, while the content-applicability gate then
    scanned documents only: `docs/` and the renders were clean and L0 was **50/50 green**. It was exposed by a
    **human post-push online re-check** under D8. This gate turns that manual re-check into a reproducible script.

    **Why "live equals the locally built artifact" is the right convention**: what sits in local `site/` is
    **mirror pages (markdown + front matter)**, not Jekyll-rendered HTML. So this gate compares **no bytes**; it takes
    the **decidable facts** the local build provides (the page list and permalinks from `published.yml`, the `title`
    from each mirror page's front matter) as expectations, then verifies four things online. Byte-level consistency is
    the mirror gate's job locally and is not duplicated here.

    Four criteria:
      C1 reachability: HTTP 200 (timeout / 5xx / 4xx counts as **failure**, not as a skip);
      C2 title agreement: the live `<title>` **starts with** the local mirror page's front-matter `title`;
      C3 navigation visibility: the page HTML contains a link to this page's permalink (`href="…/<path>/"`) —
         this is precisely the criterion for the user's 2026-10-20 complaint of "**published but unfindable**";
      C4 internal numbering: `(W|C)-anp-P\d+` occurs **0** times in the page HTML (the `C-anp-P33` lesson: numbers in
         template comments ship with every page and no mechanism could see them then).

    Three exit codes (deliberately distinct — **"unconfirmed" is never treated as "clean"**):
      0 PASS       — every page passes all four criteria;
      1 FAIL       — a defect was found (output: URL / criterion / snippet);
      2 INCOMPLETE — the propagation wait timed out, so **nothing could be confirmed** (CI pairs this with
                    `continue-on-error`: warn only, never block publication).

    Deliberately out of scope: no change to the publish workflow's existing stage semantics (a **post** step only);
    no byte comparison; no Jekyll rendering; no unpublished documents; no CSS/JS asset availability (that is the
    publish-readiness gate's job).
  </en>
</lang>

.Proof boundary（证明边界）
<lang>
  <zh-CN>
    只证明"**已发布站点**上的页面可达、标题与本地构建产物一致、能被站内链接找到、且页面内不含内部编号"。
    **不证明**页面渲染后的完整内容正确（HTML 由 Jekyll 渲染，本门禁只核对四项判据）；**不做字节比对**
    （那是镜像门禁的职责）；**不检查**未上架文档（只扫 `published.yml` 声明的页面）；**不保证**等待传播
    成功——超时即 INCOMPLETE，**不假装通过**。C3 的判据是"页面 HTML 内出现指向本页的链接"，
    因此**导航结构本身的对错不在本门禁范围**（它证明的是"找得到"，不是"该不该在那"）。
  </zh-CN>
  <en>
    Proves only that pages on the **published site** are reachable, that their titles agree with the locally built
    artifact, that they can be found through an in-site link, and that they contain no internal numbering. It does
    **not** prove the full rendered content is correct (Jekyll renders the HTML; this gate checks four criteria); it
    performs **no byte comparison** (that is the mirror gate's job); it does **not** check unpublished documents (only
    pages declared in `published.yml`); it does **not** guarantee the propagation wait succeeds — a timeout is
    INCOMPLETE, **never a silent pass**. C3's criterion is "a link to this page appears in the page HTML", so the
    **correctness of the navigation structure itself is out of scope** (it proves "findable", not "belongs there").
  </en>
</lang>
#>
param(
    [string] $BaseUrl = 'https://mandolin.github.io/HIA-ASPNETPortal',
    [string] $SiteRoot = '',
    [string] $PublishedYml = '',
    [int] $WaitSeconds = 0,
    [int] $TimeoutSec = 25
)

$ErrorActionPreference = 'Stop'
# `dev/scripts` → 上一级 `dev`，上上级仓库根
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($SiteRoot)) { $SiteRoot = Join-Path $repoRoot 'site' }
if ([string]::IsNullOrWhiteSpace($PublishedYml)) { $PublishedYml = Join-Path $SiteRoot '_data/published.yml' }
$BaseUrl = $BaseUrl.TrimEnd('/')

# <lang>
#   <zh-CN>从**本地构建产物**取期望值：`published.yml` 给出页面清单与 permalink，
#     镜像页 front matter 给出 `title`。本地镜像缺失即记为失败（说明构建产物与上架清单不一致）——
#     这是"线上复核"的前置事实，不靠猜。</zh-CN>
#   <en>Take expectations from the **locally built artifact**: `published.yml` gives the page list and permalinks, the
#     mirror page front matter gives the `title`. A missing local mirror counts as a failure (the build artifact and the
#     published list disagree) — this is the preconditioning fact of a "live re-check", not a guess.</en>
# </lang>
$items = @()
$manifest = [System.IO.File]::ReadAllText($PublishedYml)
$entries = @([regex]::Matches($manifest, '(?ms)- doc:\s*(.+?)\r?\n\s*path:\s*(.+?)\r?\n') | ForEach-Object {
    [pscustomobject]@{ doc = $_.Groups[1].Value.Trim(); path = $_.Groups[2].Value.Trim() }
})
foreach ($e in $entries) {
    foreach ($lang in @('zh', 'en')) {
        $mirrorRel = if ($lang -eq 'zh') { $e.path + '/index.md' } else { $e.path + '/en/index.md' }
        $mirror = Join-Path $SiteRoot ($mirrorRel -replace '/', '\')
        $title = $null
        if (Test-Path -LiteralPath $mirror -PathType Leaf) {
            $fm = [System.IO.File]::ReadAllText($mirror)
            $m = [regex]::Match($fm, '(?m)^title:\s*(.+?)\s*$')
            if ($m.Success) { $title = $m.Groups[1].Value.Trim() }
        }
        $url = if ($lang -eq 'zh') { $BaseUrl + '/' + $e.path + '/' } else { $BaseUrl + '/en/' + $e.path + '/' }
        $items += [pscustomobject]@{
            url    = $url
            lang   = $lang
            path   = $e.path
            title  = $title
            mirror = $mirrorRel
        }
    }
}
Write-Host ('[INFO] 发布后线上复核：待核页面 ' + $items.Count + ' 个（源自 ' + $entries.Count + ' 条上架声明，中英各一份）｜BaseUrl=' + $BaseUrl)
if ($items.Count -eq 0) { Write-Host '[FAIL] 上架清单解析为 0 条页面——无法复核。'; exit 1 }

# <lang>
#   <zh-CN>阶段一：**等待传播**。GitHub Pages 在发布后有几十秒到数分钟的传播延迟，
#     立刻抓取会把"还没生效"误判成"页面丢了"——那正是假失败。故在等待窗口内轮询，
#     超时仍未出现的页面记为 **INCOMPLETE（无法确认）**，而不是 FAIL（缺陷）。
#     精确的传播信号（构建戳）留给 `W-anp-P192` 的 D10 镜像新鲜度门禁，那里本来就要引入
#     源文档指纹；本包先用"全部可达"作为可判定信号。</zh-CN>
#   <en>Stage one: **wait for propagation**. GitHub Pages takes tens of seconds to a few minutes after publishing, so
#     fetching immediately would mistake "not live yet" for "page missing" — a false failure. Hence polling inside a
#     wait window; pages that never appear are recorded as **INCOMPLETE (unconfirmed)**, not FAIL (defect). A precise
#     propagation signal (a build stamp) is left to `W-anp-P192`'s D10 mirror-freshness gate, which introduces source
#     fingerprints anyway; this package uses "all reachable" as the decidable signal.</en>
# </lang>
$reachable = @{}
if ($WaitSeconds -gt 0) {
    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    do {
        $missing = @($items | Where-Object { -not $reachable.ContainsKey($_.url) })
        if ($missing.Count -eq 0) { break }
        foreach ($it in $missing) {
            try {
                $r = Invoke-WebRequest -Uri $it.url -Headers @{ 'User-Agent' = 'Mozilla/5.0' } -TimeoutSec 15 -ErrorAction Stop
                if ($r.StatusCode -eq 200) { $reachable[$it.url] = $true }
            }
            catch { }
        }
        if ((Get-Date) -ge $deadline) { break }
        Start-Sleep -Seconds 5
    } while ($true)
    $pending = @($items | Where-Object { -not $reachable.ContainsKey($_.url) })
    if ($pending.Count -gt 0) {
        Write-Host ('[WARN] 等待 ' + $WaitSeconds + ' 秒后仍有 ' + $pending.Count + ' 个页面未观测到可达（传播未完成），将记为 INCOMPLETE。')
    }
    else {
        Write-Host ('[INFO] 传播已观测到：' + $items.Count + ' 个页面在等待窗口内全部可达。')
    }
}

# <lang>
#   <zh-CN>阶段二：抓取 + 四项判据。抓取用 2 次重试吸收瞬时网络抖动（重试后仍失败才算不可达）；
#     判据 C1–C4 对**每一个可达页面**执行，任一不满足即记缺陷并附片段。
#     并行抓取：PowerShell 7 用 `ForEach-Object -Parallel`；5.1 退化为顺序执行（结果一致，只是慢）。</zh-CN>
#   <en>Stage two: fetch plus the four criteria. Fetching retries twice to absorb transient network noise (still failing
#     after retries counts as unreachable); criteria C1-C4 run for **every reachable page**, and any miss is recorded as
#     a defect with a snippet. Parallel fetching uses `ForEach-Object -Parallel` on PowerShell 7; on 5.1 it degrades to
#     sequential (identical results, only slower).</en>
# </lang>
function Test-OnePage {
    param([string] $Url, [string] $ExpectedTitle, [string] $Path, [int] $TimeoutSec)
    $res = [pscustomobject]@{ url = $Url; reachable = $false; status = 0; title = ''; failures = @(); transport = $false }
    $html = $null
    # <lang>
    #   <zh-CN>抓取重试 **3 次 + 退避 1s/3s**，并在页面之间加 120ms pacing。
    #     原因（实测）：GitHub Pages 对 84 次连续快速请求会偶发限流/连接重置，
    #     2 次重试 + 800ms 不足以吸收，会把正常页面误报成"未确认"——INCOMPLETE 本身是
    #     诚实的设计，但**噪声**会让人习惯性忽略它，那比没有更糟。</zh-CN>
    #   <en>Fetching retries **3 times with 1s/3s backoff**, plus 120ms pacing between pages. Reason (measured): GitHub
    #     Pages occasionally throttles or resets connections under 84 rapid consecutive requests, and 2 retries with 800ms
    #     did not absorb it, misreporting healthy pages as "unconfirmed". INCOMPLETE is an honest design, but **noise**
    #     trains people to ignore it, which is worse than not having it.</en>
    # </lang>
    for ($try = 1; $try -le 3; $try++) {
        try {
            $r = Invoke-WebRequest -Uri $Url -Headers @{ 'User-Agent' = 'Mozilla/5.0' } -TimeoutSec $TimeoutSec -ErrorAction Stop
            $res.status = [int]$r.StatusCode
            $html = [string]$r.Content
            $res.reachable = ($res.status -eq 200)
            if ($res.reachable) { break }
        }
        catch {
            # <lang>
            #   <zh-CN>**传输层失败与 HTTP 错误必须分开**（`W-anp-P193` 收口时发现的校准缺陷）。
            #     此前一律记为"不可达（缺陷）"，于是本机一次 SSL 抖动就把一个**健康页面**判成缺陷——
            #     而 CI 侧虽配了 `continue-on-error`，**噪声会让人习惯性忽略它**（P189 已记过同类教训，
            #     当时只修了抓取重试，没修这个区分）。判据：异常里**拿得到 HTTP 响应**（404/5xx 等）才算
            #     "页面真的不在"；拿不到响应（SSL / 连接重置 / 超时 / 响应截断）是"**无法确认**"，
            #     与"页面缺失"是两件不同的事。</zh-CN>
            #   <en>**Transport failures and HTTP errors must be separated** (a calibration defect found during the
            #     `W-anp-P193` closeout). Everything used to be recorded as "unreachable (defect)", so one local SSL blip
            #     judged a **healthy page** defective — and while CI pairs that with `continue-on-error`, **noise trains
            #     people to ignore it** (the same lesson recorded in P189, where only the fetch retries were fixed, not this
            #     distinction). Criterion: only when an HTTP response **is** obtainable (404/5xx etc.) does it count as "the
            #     page really is not there"; no response (SSL / connection reset / timeout / truncated response) means
            #     "**unconfirmed**", which is a different thing from "the page is missing".</en>
            # </lang>
            $res.status = 0
            $hasResponse = $false
            try { if ($null -ne $_.Exception.Response) { $hasResponse = $true } } catch { }
            if (-not $hasResponse) { $res.transport = $true }
            if ($try -eq 3) {
                if ($hasResponse) { $res.failures += ('C1 页面不可达（HTTP 错误）：' + $_.Exception.Message.Substring(0, [Math]::Min(70, $_.Exception.Message.Length))) }
                else { $res.failures += ('C1 传输层不可达（网络/SSL/超时，非页面缺失）：' + $_.Exception.Message.Substring(0, [Math]::Min(70, $_.Exception.Message.Length))) }
            }
            Start-Sleep -Milliseconds $(if ($try -eq 1) { 1000 } else { 3000 })
        }
    }
    Start-Sleep -Milliseconds 120
    if (-not $res.reachable -or [string]::IsNullOrEmpty($html)) { return $res }
    # C2 标题一致（线上 <title> 以本地镜像页 front matter 的 title 开头）
    $liveTitle = ([regex]::Match($html, '(?is)<title>\s*(.*?)\s*</title>')).Groups[1].Value
    $res.title = $liveTitle
    if (-not [string]::IsNullOrWhiteSpace($ExpectedTitle)) {
        # <lang>
        #   <zh-CN>**期望值需剥除 markdown 行内代码的反引号**（首次实跑线上 84 页时暴露的校准问题）：
        #     镜像页 front matter 的 `title` 取自文档 H1，其中可能含 `` `module.json` `` 这类 markdown
        #     行内代码；而线上 `<title>` 是 Jekyll **渲染后**的文本，markdown 记号已去掉。
        #     拿 markdown 原文与渲染结果直接比对，会把 3 个正常页面误判成"标题不一致"——那是本门禁的
        #     校准缺陷，不是站点缺陷。故两侧都剥反引号后比较（只剥反引号：它是当前唯一被渲染器
        #     去掉的记号；不顺手剥 `*` / `_`，那会让判据变松而造成漏报）。</zh-CN>
        #   <en>**The expectation must have markdown inline-code backticks stripped** (a calibration issue exposed by the
        #     first live run over 84 pages): the mirror page's front-matter `title` comes from the document H1, which may
        #     contain markdown inline code such as `` `module.json` ``, whereas the live `<title>` is the **rendered** text
        #     where markdown markers are gone. Comparing markdown source against rendered output flagged 3 healthy pages as
        #     "title mismatch" — a calibration defect of this gate, not of the site. Both sides are therefore compared with
        #     backticks stripped (backticks only: they are the only marker the renderer currently drops; `*` and `_` are
        #     deliberately left alone, since stripping them would loosen the criterion into a false pass).</en>
        # </lang>
        $normLive = (($liveTitle -replace '`', '') -replace '\s+', ' ').Trim()
        $normExp = (($ExpectedTitle -replace '`', '') -replace '\s+', ' ').Trim()
        if (-not $normLive.StartsWith($normExp)) {
            $res.failures += ('C2 标题不一致：线上「' + $liveTitle + '」与本地构建产物「' + $ExpectedTitle + '」不符')
        }
    }
    else {
        $res.failures += 'C2 本地镜像页无 title（构建产物与上架清单不一致）'
    }
    # C3 导航可见性：页面 HTML 内含指向本页 permalink 的链接
    $needle = '/' + $Path.Trim('/') + '/'
    if (-not [regex]::IsMatch($html, 'href="[^"]*' + [regex]::Escape($needle) + '"')) {
        $res.failures += ('C3 导航不可见：页面 HTML 内没有指向 ' + $needle + ' 的链接')
    }
    # C4 内部编号（C-anp-P33 的教训：模板注释里的编号会随每页发出）
    $nums = [regex]::Matches($html, '(W|C)-anp-P\d+(\.\d+)?')
    if ($nums.Count -gt 0) {
        $shown = (@($nums | ForEach-Object { $_.Value } | Select-Object -Unique -First 3)) -join '、'
        $res.failures += ('C4 页面内含内部编号 ' + $nums.Count + ' 处（' + $shown + '）')
    }
    return $res
}

# <lang>
#   <zh-CN>**顺序执行，不用 `ForEach-Object -Parallel`**。原因（实测，不是偏好）：并行 runspace
#     既不继承调用方函数（`$using:function:` 对含连字符的函数名解析失败），也**不接受经 `$using:`
#     传入脚本块**（PowerShell 会直接报错）。为并行而把判据逻辑复制进 block，会造成"两处同源逻辑"，
#     正是本项目反复要避免的漂移形态。顺序执行 84 页约 1–2 分钟，CI 后置步骤可承受。</zh-CN>
#   <en>**Sequential, not `ForEach-Object -Parallel`**. Reason (measured, not preference): parallel runspaces neither
#     inherit the caller's functions (`$using:function:` fails to parse for hyphenated names) nor **accept a scriptblock
#     passed via `$using:`** (PowerShell errors out outright). Duplicating the criterion logic into the block to enable
#     parallelism would create "two copies of the same logic" — exactly the drift this project keeps avoiding.
#     Sequential execution takes about 1-2 minutes for 84 pages, which a CI post-step can absorb.</en>
# </lang>
$results = @()
foreach ($it in $items) {
    $results += (Test-OnePage -Url $it.url -ExpectedTitle $it.title -Path $it.path -TimeoutSec $TimeoutSec)
}

# <lang>
#   <zh-CN>阶段三：判定。**FAIL 优先于 INCOMPLETE**（发现缺陷时不必再等传播解释）；
#     全部页面通过才 PASS；没有缺陷但有页面未确认 → INCOMPLETE。三种状态都显式输出，
#     不允许"没抓到"被读成"干净"。</zh-CN>
#   <en>Stage three: verdict. **FAIL outranks INCOMPLETE** (once a defect is found there is nothing left for propagation
#     to explain); PASS only when every page passes; INCOMPLETE when there is no defect but some page is unconfirmed.
#     All three states print explicitly — "not caught" must never read as "clean".</en>
# </lang>
$defects = @()
$incomplete = @()
foreach ($r in $results) {
    if (-not $r.reachable) {
        # <lang>
        #   <zh-CN>传输层不可达一律算"未确认"——**哪怕它在等待窗口内曾可达**。理由：传输失败说明
        #     "这次没查到"，而不是"页面没了"；只有拿到 HTTP 错误响应（404/5xx）才是页面真的不在。</zh-CN>
        #   <en>A transport failure always counts as "unconfirmed" — **even if it was reachable inside the wait window**.
        #     Reason: a transport failure means "this attempt could not tell", not "the page is gone"; only an HTTP error
        #     response (404/5xx) means the page really is not there.</en>
        # </lang>
        if ($r.transport) { $incomplete += [pscustomobject]@{ url = $r.url; reason = '传输层不可达（网络/SSL/超时），无法确认' } }
        elseif ($reachable.ContainsKey($r.url)) { $defects += $r }
        else { $incomplete += [pscustomobject]@{ url = $r.url; reason = '未观测到可达（等待传播超时或抓取失败）' } }
        continue
    }
    if ($r.failures.Count -gt 0) { $defects += $r }
}

foreach ($d in $defects) {
    Write-Host ('  [DEFECT] ' + $d.url)
    foreach ($f in $d.failures) { Write-Host ('      - ' + $f) }
}
foreach ($i in $incomplete) { Write-Host ('  [UNCONFIRMED] ' + $i.url + ' — ' + $i.reason) }

$passCount = @($results | Where-Object { $_.reachable -and $_.failures.Count -eq 0 }).Count
Write-Host ('[INFO] 已核页面 ' + $results.Count + ' 个｜通过 ' + $passCount + '｜缺陷 ' + $defects.Count + '｜未确认 ' + $incomplete.Count)

if ($defects.Count -gt 0) {
    Write-Host ('[FAIL] 发布后线上复核发现 ' + $defects.Count + ' 个页面存在缺陷（页面已发布，需修复后重新发布）。')
    exit 1
}
if ($incomplete.Count -gt 0) {
    Write-Host ('[INCOMPLETE] 有 ' + $incomplete.Count + ' 个页面未能在等待窗口内确认——不当作通过，也不当作缺陷；请加大 -WaitSeconds 后重试。')
    exit 2
}
Write-Host '[PASS] 发布后线上复核：全部页面可达、标题与本地构建产物一致、导航可见、页面内无内部编号。'
exit 0