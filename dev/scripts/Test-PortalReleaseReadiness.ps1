<#
.SYNOPSIS
<lang>
  <zh-CN>封版前检查门禁（F1–F8）：把 `docs/versioning.md` 第四节的发布流程从"文档里写着、人照着做"变成**可复跑判据**。</zh-CN>
  <en>Release-readiness gate (F1-F8): turns the release process in section 4 of `docs/versioning.md` from "written down and
  followed by hand" into **repeatable assertions**.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    八项判据（`C-anp-P35` / `W-anp-P199`）：

      F1 `CHANGELOG.md` 的 `Unreleased` 段**有内容**——否则没东西可封（**两态判定**：日常自检下"段存在但为空"是合法状态，表示本版本已封、尚无新积累；封版前模式下空段即"没东西可封"，判 FAIL）；
      F2 版本锚点一致（程序集三字段 == CHANGELOG 最新已发布条目 == 最新 git 标签）——委托给版本一致性门禁；
      F3 **目标 tag 尚未存在**——防止重复打标签；
      F4 **L0/L1 最近一次运行无失败**，且每个 Skip 都必须带自报原因——读取**新鲜的套件统计产物**判定；
      F5 镜像新鲜度 Pass——委托给镜像新鲜度门禁；
      F6 发布后线上复核结论为 PASS 或**显式** INCOMPLETE——读取**新鲜的复核统计产物**判定；
      F7 备忘里没有"阻塞封版"级未决项，且**每条 D 条目都带显式判定**（阻塞 / 不阻塞 / 已关闭）；**两模式**：封版前模式下阻塞项判 FAIL，日常自检模式下判 PASS 但逐条点名待处理项（避免未决项天天把 L0 变红）；
      F8 `docs/versioning.md` 第六节"当前版本"与 CHANGELOG 最新已发布版本一致——没漂移。

    **F4 / F6 为什么读产物而不是自己跑（反递归设计）**：本门禁**不调用门禁套件**——套件里包含本门禁，
    自己调自己会无限递归。也不自己跑线上复核（它要访问外网，而本门禁也可能在离线环境里跑）。
    所以这两项改为消费**一次新鲜运行的统计产物**：拿不到、或超过时间窗，就报**未确认**，不假装通过。

    **两种模式**：
      · 封版前（默认，全部八项）：`-SuiteStatsPath` / `-LiveRecheckStatsPath` 由人显式传入新鲜产物；
      · 日常编排（`-DailySelfCheck`）：只跑**静态可判**的 F1 / F2 / F7 / F8。
        为什么这样切：日常跑全套时 F4/F6 永远拿不到"刚出炉"的产物，于是每次都报未确认、每次都跳过——
        而**持续跳过的告警等于没有告警**，那比没有更糟。产物类判据的正确执行时机是封版前由人喂入新鲜产物。

    退出码：`0` 全部通过；`1` 存在 FAIL（不就绪）；`2` 无 FAIL 但有未确认项（**不当作通过**）。
  </zh-CN>
  <en>
    Eight checks (`C-anp-P35` / `W-anp-P199`):

      F1 `CHANGELOG.md` has a **non-empty `Unreleased` section** — otherwise there is nothing to seal;
      F2 version anchors agree (assembly three fields == latest released CHANGELOG entry == latest git tag) — delegated
         to the version-consistency gate;
      F3 the **target tag does not exist yet** — prevents tagging twice;
      F4 the **latest L0/L1 run had no failures**, and every Skip carries a self-reported reason — judged from a **fresh
         suite statistics artifact**;
      F5 mirror freshness Pass — delegated to the mirror-freshness gate;
      F6 the post-publish live re-check verdict is PASS or an **explicit** INCOMPLETE — judged from a **fresh re-check
         statistics artifact**;
      F7 no "blocks the release" open item in the memo, and **every D entry carries an explicit verdict**
         (blocking / non-blocking / closed); **two modes**: before sealing a blocking entry fails, while the daily self-check
         passes but names each pending entry explicitly (so an open item does not turn L0 red every day);
      F8 the "current version" in section 6 of `docs/versioning.md` matches the latest released CHANGELOG version — no
         drift.

    **Why F4/F6 read artifacts instead of running things themselves (anti-recursion design)**: this gate **never invokes
    the gate suite** — the suite contains this gate, so invoking it would recurse forever. Nor does it run the live
    re-check itself (that needs the network, and this gate may run offline). Both therefore consume **the statistics
    artifact of a fresh run**: if none is available, or it is outside the time window, the answer is **unconfirmed** —
    never a silent pass.

    **Two modes**:
      - Release time (default, all eight): `-SuiteStatsPath` / `-LiveRecheckStatsPath` are supplied explicitly with fresh
        artifacts;
      - Daily orchestration (`-DailySelfCheck`): only the **statically decidable** F1 / F2 / F7 / F8.
        Why the split: in a daily run F4/F6 could never see a "just produced" artifact, so they would report unconfirmed
        and skip every single time — and **an alarm that skips every day is no alarm at all**, which is worse than not
        having it. The correct moment for the artifact-based checks is release time, with fresh artifacts fed in by a
        human.

    Exit codes: `0` all pass; `1` a FAIL exists (not ready); `2` no FAIL but something unconfirmed (**never treated as a
    pass**).
  </en>
</lang>

.PARAMETER TargetVersion
<lang>
  <zh-CN>目标版本号（形如 `v1.2.0`）。F3 用它判断 tag 是否已存在；日常自检模式下可省略。</zh-CN>
  <en>Target version (for example `v1.2.0`). F3 uses it to check whether the tag already exists; optional in daily
  self-check mode.</en>
</lang>

.PARAMETER SuiteStatsPath
<lang>
  <zh-CN>门禁套件统计产物（`gate-suite-summary.json`）路径。省略时取证据目录下最新的一份。</zh-CN>
  <en>Path to the gate-suite statistics artifact (`gate-suite-summary.json`). Defaults to the newest one in the evidence
  directory.</en>
</lang>

.PARAMETER LiveRecheckStatsPath
<lang>
  <zh-CN>线上复核统计产物路径。省略时取证据目录下最新的一份。</zh-CN>
  <en>Path to the live re-check statistics artifact. Defaults to the newest one in the evidence directory.</en>
</lang>

.PARAMETER SuiteMaxAgeMinutes
<lang>
  <zh-CN>套件统计产物的最大年龄（分钟，默认 1440 = 24 小时）。超时即未确认。</zh-CN>
  <en>Maximum age of the suite statistics artifact (minutes, default 1440 = 24 hours). Older means unconfirmed.</en>
</lang>

.PARAMETER LiveRecheckMaxAgeMinutes
<lang>
  <zh-CN>线上复核统计产物的最大年龄（分钟，默认 1440）。超时即未确认。</zh-CN>
  <en>Maximum age of the live re-check statistics artifact (minutes, default 1440). Older means unconfirmed.</en>
</lang>

.PARAMETER DailySelfCheck
<lang>
  <zh-CN>日常编排模式：只跑静态判据 F1 / F2 / F7 / F8（见上文"为什么这样切"）。</zh-CN>
  <en>Daily orchestration mode: only the static checks F1 / F2 / F7 / F8 (see "why the split" above).</en>
</lang>

.Proof boundary（证明边界）
<lang>
  <zh-CN>
    只证明"**封版前该核的八项锚点在机器可判的范围内成立**"。
    **不证明**：发布流程已被执行、产物已发布、版本号语义正确（是否该升主版本由人决定）、
    目标环境证据齐备（属 `v2.0.0` 与后续规划的范畴）。F2 与 F5 是**委托**既有门禁，本门禁只记录其结果、
    不复述其判据；F4 与 F6 只**消费**他人产的新鲜统计产物，不重新执行被检查的动作。
  </zh-CN>
  <en>
    Proves only that "**the eight anchors that must be checked before sealing hold, within what machines can decide**".
    It does **not** prove that the release process has been executed, that artifacts are published, that the version
    number is semantically right (whether to bump the major version is a human decision), or that target-environment
    evidence is complete (that belongs to `v2.0.0` and later planning). F2 and F5 are **delegations** to existing gates:
    this gate records their outcome without restating their criteria. F4 and F6 only **consume** fresh statistics
    artifacts produced elsewhere; they never re-execute the action being checked.
  </en>
</lang>
#>
param(
    [string] $TargetVersion = '',
    [string] $SuiteStatsPath = '',
    [string] $LiveRecheckStatsPath = '',
    [int] $SuiteMaxAgeMinutes = 1440,
    [int] $LiveRecheckMaxAgeMinutes = 1440,
    [switch] $DailySelfCheck,
    [string] $RepoRoot = ''
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path }
$utf8 = New-Object System.Text.UTF8Encoding($false)
$pwsh = 'pwsh'
$onPath = Get-Command 'pwsh' -ErrorAction SilentlyContinue
if ($onPath) { $pwsh = $onPath.Source }
elseif (Test-Path -LiteralPath "$env:ProgramFiles\PowerShell\7\pwsh.exe") { $pwsh = "$env:ProgramFiles\PowerShell\7\pwsh.exe" }

$failures = New-Object 'System.Collections.Generic.List[string]'
$incomplete = New-Object 'System.Collections.Generic.List[string]'
$notes = New-Object 'System.Collections.Generic.List[string]'

function Add-Result {
    param([string] $Id, [string] $State, [string] $Message)
    switch ($State) {
        'PASS' { Write-Host ('  [PASS] ' + $Id + '：' + $Message) }
        'FAIL' { Write-Host ('  [FAIL] ' + $Id + '：' + $Message); $script:failures.Add($Id + '：' + $Message) }
        'INCOMPLETE' { Write-Host ('  [INCOMPLETE] ' + $Id + '：' + $Message); $script:incomplete.Add($Id + '：' + $Message) }
        'SKIP' { Write-Host ('  [SKIP] ' + $Id + '：' + $Message); $script:notes.Add($Id + '：' + $Message) }
    }
}

Write-Host ('[INFO] 封版前检查：模式=' + $(if ($DailySelfCheck) { '日常自检（F1/F2/F7/F8）' } else { '封版前（F1-F8）' }) + '｜目标版本=' + $(if ($TargetVersion) { $TargetVersion } else { '（未指定）' }))

# ---------- 公共：CHANGELOG 最新已发布版本 ----------
$changelogPath = Join-Path $RepoRoot 'CHANGELOG.md'
if (-not (Test-Path -LiteralPath $changelogPath)) { Write-Host '[FAIL] 找不到 CHANGELOG.md'; exit 1 }
$changelog = [System.IO.File]::ReadAllText($changelogPath, $utf8)
$latestReleased = $null
foreach ($m in [regex]::Matches($changelog, '(?m)^##\s+\[([^\]]+)\]')) {
    if ($m.Groups[1].Value -ne 'Unreleased') { $latestReleased = $m.Groups[1].Value; break }
}

# ---------- F1 Unreleased 段有内容 ----------
$unrelMatch = [regex]::Match($changelog, '(?ms)^##\s+\[Unreleased\](.*?)(?=^##\s+\[|\z)')
$unrelBody = if ($unrelMatch.Success) { $unrelMatch.Groups[1].Value } else { '' }
$unrelLines = @($unrelBody -split "`r?`n" | Where-Object { $_.Trim() -ne '' })
if (-not $unrelMatch.Success) { Add-Result 'F1' 'FAIL' 'CHANGELOG 没有 `## [Unreleased]` 段——按 Keep a Changelog 惯例该段应始终存在（封版后为**空段**是正常状态）' }
elseif ($unrelLines.Count -eq 0) {
    # <lang>
    #   <zh-CN>**空段在两模式下的判定不同**（刻意）：日常自检里"段存在但为空"是**合法状态**——
    #     表示本版本已封、尚无新积累，若此时判 FAIL，封版后每一天的 L0 都会红，那又是"告警天天响等于没有告警"；
    #     而**封版前**空段就是"没东西可封"，必须 FAIL。</zh-CN>
    #   <en>**An empty section is judged differently in the two modes** (deliberately): in a daily self-check "the section
    #     exists but is empty" is a **legal state** — this version is sealed and nothing has accumulated yet; judging that
    #     FAIL would turn every L0 red after each seal, which is again "an alarm that fires daily is no alarm". Before
    #     sealing, however, an empty section means "there is nothing to seal" and must FAIL.</en>
    # </lang>
    if ($DailySelfCheck) { Add-Result 'F1' 'PASS' '`Unreleased` 段存在且为空——本版本已封版、尚无新积累（日常自检的合法状态）' }
    else { Add-Result 'F1' 'FAIL' '`Unreleased` 段是空的——没东西可封' }
}
else { Add-Result 'F1' 'PASS' ('`Unreleased` 段有 ' + $unrelLines.Count + ' 行非空内容') }

# ---------- F2 版本锚点一致（委托） ----------
$verGate = Join-Path $RepoRoot 'dev/scripts/Test-PortalVersionConsistency.ps1'
if (-not (Test-Path -LiteralPath $verGate)) { Add-Result 'F2' 'FAIL' '找不到版本一致性门禁，无法委托' }
else {
    & $pwsh -NoProfile -NoLogo -File $verGate 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) { Add-Result 'F2' 'PASS' '版本锚点一致（委托版本一致性门禁，exit 0）' }
    else { Add-Result 'F2' 'FAIL' ('版本一致性门禁未通过（exit ' + $LASTEXITCODE + '）：程序集 / CHANGELOG / git tag 三者不一致') }
}

# ---------- F3 目标 tag 尚未存在 ----------
if ($DailySelfCheck) { Add-Result 'F3' 'SKIP' '日常自检不判目标 tag（发布时须显式给出 -TargetVersion）' }
elseif (-not $TargetVersion) { Add-Result 'F3' 'INCOMPLETE' '未提供 -TargetVersion，无法判断 tag 是否已存在' }
else {
    $existing = @(& git -C $RepoRoot tag --list $TargetVersion 2>$null)
    if ($existing.Count -gt 0) { Add-Result 'F3' 'FAIL' ('目标 tag ' + $TargetVersion + ' 已存在——重复打标签') }
    else { Add-Result 'F3' 'PASS' ('目标 tag ' + $TargetVersion + ' 尚不存在') }
}

# ---------- F4 套件新鲜运行无失败（读产物，反递归） ----------
if ($DailySelfCheck) { Add-Result 'F4' 'SKIP' '日常自检不读套件产物（封版前须显式传入新鲜的 -SuiteStatsPath）' }
else {
    $suitePath = $SuiteStatsPath
    if (-not $suitePath) {
        $dir = Join-Path $RepoRoot 'work-zone/dev/evidence/gate-suite'
        if (Test-Path -LiteralPath $dir) {
            $newest = @(Get-ChildItem $dir -Recurse -File -Filter 'gate-suite-summary.json' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
            if ($newest.Count -gt 0) { $suitePath = $newest[0].FullName }
        }
    }
    if (-not $suitePath -or -not (Test-Path -LiteralPath $suitePath)) {
        Add-Result 'F4' 'INCOMPLETE' '找不到门禁套件统计产物——请先跑一次 L0/L1 并用 -SuiteStatsPath 传入（不假装通过）'
    }
    else {
        # 必须用 **UTC 的现在** 减 **UTC 的时间戳**：本地时间是 UTC+8，混用会把年龄多算 8 小时，窗口一收紧就会把新鲜产物误判成过期
        $age = ((Get-Date).ToUniversalTime() - (Get-Item -LiteralPath $suitePath).LastWriteTimeUtc).TotalMinutes
        if ($age -gt $SuiteMaxAgeMinutes) { Add-Result 'F4' 'INCOMPLETE' ('套件统计产物已过期（' + [int]$age + ' 分钟前，上限 ' + $SuiteMaxAgeMinutes + '）——请重跑套件') }
        else {
            $s = Get-Content -LiteralPath $suitePath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ([int]$s.failedCount -gt 0) { Add-Result 'F4' 'FAIL' ('套件统计产物显示失败 ' + $s.failedCount + ' 项（' + $suitePath + '）') }
            else {
                $skips = @($s.results | Where-Object { $_.status -eq 'Skip' })
                $noReason = @($skips | Where-Object { -not $_.note -or $_.note.Trim() -eq '' })
                if ($noReason.Count -gt 0) {
                    Add-Result 'F4' 'FAIL' ('套件有 ' + $noReason.Count + ' 个 Skip 未说明原因（' + (($noReason | ForEach-Object { $_.gate }) -join '、') + '）——跳过必须显式列出理由')
                }
                else {
                    $skipText = if ($skips.Count -gt 0) { '；Skip ' + $skips.Count + ' 项均带自报原因（' + (($skips | ForEach-Object { $_.gate }) -join '、') + '）' } else { '；无 Skip' }
                    Add-Result 'F4' 'PASS' ('套件新鲜运行（' + [int]$age + ' 分钟前）：' + $s.gateCount + ' 项、失败 0' + $skipText)
                }
            }
        }
    }
}

# ---------- F5 镜像新鲜度（委托） ----------
$mirrorGate = Join-Path $RepoRoot 'dev/scripts/Test-PortalSiteMirrorFreshness.ps1'
if ($DailySelfCheck) { Add-Result 'F5' 'SKIP' '日常自检不重复跑镜像新鲜度（L0 自身已含该门禁，避免同一份构建跑两遍）' }
elseif (-not (Test-Path -LiteralPath $mirrorGate)) { Add-Result 'F5' 'FAIL' '找不到镜像新鲜度门禁，无法委托' }
else {
    & $pwsh -NoProfile -NoLogo -File $mirrorGate 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) { Add-Result 'F5' 'PASS' '镜像新鲜度 Pass（委托镜像新鲜度门禁，exit 0）' }
    else { Add-Result 'F5' 'FAIL' ('镜像新鲜度门禁未通过（exit ' + $LASTEXITCODE + '）：仓库内 site/ 与当前 docs 的构建产物不一致') }
}

# ---------- F6 线上复核结论（读产物） ----------
if ($DailySelfCheck) { Add-Result 'F6' 'SKIP' '日常自检不读线上复核产物（封版前须显式传入新鲜的 -LiveRecheckStatsPath）' }
else {
    $livePath = $LiveRecheckStatsPath
    if (-not $livePath) {
        $dir = Join-Path $RepoRoot 'work-zone/dev/evidence/published-site-recheck'
        if (Test-Path -LiteralPath $dir) {
            $newest = @(Get-ChildItem $dir -Recurse -File -Filter 'recheck-*.json' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
            if ($newest.Count -gt 0) { $livePath = $newest[0].FullName }
        }
    }
    if (-not $livePath -or -not (Test-Path -LiteralPath $livePath)) {
        Add-Result 'F6' 'INCOMPLETE' '找不到线上复核统计产物——请跑一次复核并用 -LiveRecheckStatsPath 传入（不假装通过）'
    }
    else {
        $age = ((Get-Date).ToUniversalTime() - (Get-Item -LiteralPath $livePath).LastWriteTimeUtc).TotalMinutes
        if ($age -gt $LiveRecheckMaxAgeMinutes) { Add-Result 'F6' 'INCOMPLETE' ('线上复核产物已过期（' + [int]$age + ' 分钟前，上限 ' + $LiveRecheckMaxAgeMinutes + '）——请重跑复核') }
        else {
            $r = Get-Content -LiteralPath $livePath -Raw -Encoding UTF8 | ConvertFrom-Json
            $reasons = @($r.unconfirmedReasons | Select-Object -First 3) -join '；'
            switch ($r.verdict) {
                'PASS' { Add-Result 'F6' 'PASS' ('线上复核 PASS（' + $r.total + ' 页，' + [int]$age + ' 分钟前）') }
                'INCOMPLETE' { Add-Result 'F6' 'PASS' ('线上复核为显式 INCOMPLETE（' + $r.incomplete + ' 页未确认：' + $reasons + '）——诚实状态，不阻塞封版') }
                default { Add-Result 'F6' 'FAIL' ('线上复核 verdict=' + $r.verdict + '（缺陷 ' + $r.defects + ' 项）——页面已发布且存在缺陷') }
            }
        }
    }
}

# ---------- F7 备忘无阻塞项 + 每条 D 带判定 ----------
$memoPath = Join-Path $RepoRoot 'work-zone/dev/plans/deferred-verification-memo.md'
if (-not (Test-Path -LiteralPath $memoPath)) { Add-Result 'F7' 'INCOMPLETE' '找不到备忘文件，无法核对 D 条目判定' }
else {
    $memo = [System.IO.File]::ReadAllText($memoPath, $utf8)
    $dRows = @([regex]::Matches($memo, '(?m)^\|\s*(D\d+)\s*\|.*$'))
    if ($dRows.Count -eq 0) { Add-Result 'F7' 'FAIL' '备忘里一条 D 条目都没有——要么真没有未决项（应写明），要么解析失败' }
    else {
        $noVerdict = @($dRows | Where-Object { $_.Value -notmatch '判定：' })
        $blocking = @($dRows | Where-Object { $_.Value -match '判定：[^*]*阻塞封版' -and $_.Value -notmatch '不阻塞封版' })
        if ($noVerdict.Count -gt 0) {
            Add-Result 'F7' 'FAIL' ('有 ' + $noVerdict.Count + ' 条 D 条目未标判定（' + (($noVerdict | ForEach-Object { [regex]::Match($_.Value, '^\|\s*(D\d+)').Groups[1].Value }) -join '、') + '）——每条都必须标 阻塞 / 不阻塞 / 已关闭')
        }
        elseif ($blocking.Count -gt 0) {
            # <lang>
            #   <zh-CN>**两模式判定**（沿用 F1 已确立的"同一判据在两模式下判定不同"范式，刻意）：
            #     · **封版前**：阻塞项必须 FAIL —— 它正是"封版前必须处理"的强制点，拦不住就等于没有这条约束；
            #     · **日常自检**：判 PASS 但**显式列出待处理条目** —— 否则一条"下次封版前清理"的未决项会天天把 L0 变红，
            #       而"持续告警等于没有告警"正是本脚本自己写下的原则。日常输出里逐条点名，不靠沉默掩盖。</zh-CN>
            #   <en>**Two-mode judgement** (deliberately following the pattern F1 already established, where one criterion is judged
            #   differently per mode):
            #     - **Before sealing**: a blocking item must FAIL — it *is* the enforcement point for "must be handled before
            #       sealing", and an unenforceable constraint is no constraint;
            #     - **Daily self-check**: PASS, but the pending entries are **listed explicitly** — otherwise an item like "clean
            #       up before the next seal" turns L0 red every single day, and "an alarm that fires daily is no alarm at all" is
            #       this script's own stated principle. The daily output names each entry; nothing is hidden by staying silent.</en>
            # </lang>
            $blockingIds = (($blocking | ForEach-Object { [regex]::Match($_.Value, '^\|\s*(D\d+)').Groups[1].Value }) -join '、')
            if ($DailySelfCheck) {
                Add-Result 'F7' 'PASS' ('有 ' + $blocking.Count + ' 条 D 条目待封版前处理（' + $blockingIds + '）——日常自检不判红，封版前检查会判红')
            } else {
                Add-Result 'F7' 'FAIL' ('有 ' + $blocking.Count + ' 条 D 条目被标为阻塞封版（' + $blockingIds + '）')
            }
        }
        else {
            $closed = @($dRows | Where-Object { $_.Value -match '判定：已关闭' }).Count
            Add-Result 'F7' 'PASS' ('D 条目 ' + $dRows.Count + ' 条全部带判定，无阻塞项（其中已关闭 ' + $closed + ' 条）')
        }
    }
}

# ---------- F8 versioning.md 当前版本未漂移 ----------
$verDoc = Join-Path $RepoRoot 'docs/versioning.md'
if (-not (Test-Path -LiteralPath $verDoc)) { Add-Result 'F8' 'FAIL' '找不到 docs/versioning.md' }
elseif (-not $latestReleased) { Add-Result 'F8' 'FAIL' 'CHANGELOG 里没有已发布版本条目，无法比对' }
else {
    $doc = [System.IO.File]::ReadAllText($verDoc, $utf8)
    $section = [regex]::Match($doc, '(?ms)^##\s+六、当前版本(.*?)(?=^##\s+|\z)')
    if (-not $section.Success) { Add-Result 'F8' 'FAIL' 'docs/versioning.md 缺少第六节「当前版本」' }
    else {
        $found = [regex]::Match($section.Groups[1].Value, 'v\d+\.\d+\.\d+')
        if (-not $found.Success) { Add-Result 'F8' 'FAIL' '第六节里找不到版本号' }
        elseif ($found.Value -ne $latestReleased) { Add-Result 'F8' 'FAIL' ('第六节写的是 ' + $found.Value + '，而 CHANGELOG 最新已发布版本是 ' + $latestReleased + '——锚点漂移') }
        else { Add-Result 'F8' 'PASS' ('第六节「当前版本」' + $found.Value + ' 与 CHANGELOG 最新已发布版本一致') }
    }
}

# ---------- 汇总 ----------
Write-Host ('[INFO] 判定汇总：PASS ' + (8 - $failures.Count - $incomplete.Count - $notes.Count) + '｜FAIL ' + $failures.Count + '｜INCOMPLETE ' + $incomplete.Count + '｜SKIP ' + $notes.Count)
if ($failures.Count -gt 0) { Write-Host ('[FAIL] 封版前检查未就绪：' + $failures.Count + ' 项不通过。'); exit 1 }
if ($incomplete.Count -gt 0) { Write-Host ('[INCOMPLETE] 封版前检查有 ' + $incomplete.Count + ' 项未确认——不当作通过；请补齐新鲜产物后重试。'); exit 2 }
Write-Host '[PASS] 封版前检查就绪。'
exit 0
