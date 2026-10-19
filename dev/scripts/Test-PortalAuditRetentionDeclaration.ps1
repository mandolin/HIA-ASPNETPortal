#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>看守"审计留存：未实现 + 不得删除审计数据"这条登记，并核对实际删除点（L0）。</zh-CN>
  <en>Guards the "audit retention: unimplemented + audit data must not be deleted" declaration and audits actual delete points (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P154`）交付。验收判据 V4 要求审计留存二选一落地：实现自动归档 / 到期清理，
  **或**正式登记「未实现 + 不得删除审计数据」。本包选后者（实现需运维确认与备份策略，属环境触发型），
  但**把登记变成机器可核对的**——否则"不得删除"只是一句没人验证的散文。

  断言：
    A1 策略文档 `docs/audit-retention-policy.md` 必须保留状态声明（「尚未实现」+「不得删除」关键短语）。
    A2 产品源码 `src/` 内不得出现针对 `PortalCfg_OperationAudits` 的 DELETE / TRUNCATE
       ——生产路径不删审计数据，且**不允许任何豁免**。
    A3 仓库内（`dev/scripts` 与 SQL 脚本）其余审计表删除点必须在显式豁免清单中并附依据；
       未登记者即失败（default-deny：新增删除点必须先登记理由）；清单条目若已无对应命中
       （漂移）同样失败。
    A4 策略文档点名要看的门禁必须存在。

  证明边界：只证明"文档声明存在 + 产品源码无删除 + 删除点全部登记"；
  **不证明自动归档 / 到期清理已实现**（明确未实现，已登记）、**不证明保留期天数被自动执行**、
  **不证明归档数据的完整性或备份可用性**（属运维与备份策略范围）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalAuditRetentionDeclaration.ps1'
  产物：temp\p154\audit-retention-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 环境/文件前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P154`) deliverable. Acceptance criterion V4 requires audit retention to land one of two ways: implement
  automatic archiving / expiry cleanup, **or** formally register "not implemented + audit data must not be deleted". This work item
  chose the latter (implementation needs operational sign-off and a backup strategy, i.e. environment-triggered), but turns the
  registration into something machine-checkable — otherwise "must not delete" is just prose nobody verifies.

  Assertions:
    A1 the policy doc `docs/audit-retention-policy.md` must keep its status declaration (the key phrases "not yet implemented" and
       "must not delete").
    A2 product source `src/` must contain no DELETE / TRUNCATE against `PortalCfg_OperationAudits` — production paths never delete
       audit data, and **no exemption is allowed** there.
    A3 every other audit-table delete point in the repository (`dev/scripts` and SQL scripts) must appear in an explicit allowlist
       with a basis; an unregistered one fails (default-deny: a new delete point must be justified and registered first); an allowlist
       entry with no matching occurrence any more (drift) also fails.
    A4 the gate named by the policy doc must exist.

  Proof boundary: proves only that "the declaration exists, the product source has no delete, and every delete point is registered";
  does **not** prove that automatic archiving / expiry cleanup is implemented (explicitly not implemented, registered), does **not**
  prove that retention day counts are enforced automatically, and does **not** prove archived-data integrity or backup availability
  (those belong to operations and backup strategy).

  Usage (repository root): & 'dev\scripts\Test-PortalAuditRetentionDeclaration.ps1'
  Output: temp\p154\audit-retention-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing environment/file prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalAuditRetentionDeclaration.ps1'   # 看守审计留存登记与删除点</zh-CN>
  <en>& 'dev\scripts\Test-PortalAuditRetentionDeclaration.ps1'   # guard the audit-retention declaration and its delete points</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P154` 交付；登记写在 `docs/audit-retention-policy.md` 第三节（含 3.1 例外登记）。</zh-CN>
  <en>Provenance: delivered by `W-anp-P154`; the registration lives in section 3 of `docs/audit-retention-policy.md` (including 3.1 exceptions).</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$docPath = Join-Path $repoRoot 'docs\audit-retention-policy.md'
$gatePath = Join-Path $repoRoot 'dev\scripts\Test-PortalAuditRetentionDeclaration.ps1'
$auditTable = 'PortalCfg_OperationAudits'

$failures = @()
$softNotes = @()

if (-not (Test-Path -LiteralPath $docPath)) {
    Write-Host '[PREREQ-MISSING] 策略文档不存在：' + $docPath + '（exit 2）'
    exit 2
}
$docText = [System.IO.File]::ReadAllText($docPath)

# <lang><zh-CN>A1：策略文档必须保留「尚未实现」+「不得删除」的状态声明（防文档漂移成"已实现"）。</zh-CN>
# <en>A1: the policy doc must keep its status declaration ("not yet implemented" + "must not delete"), so it cannot drift into "implemented".</en></lang>
foreach ($phrase in @('尚未实现', '不得删除')) {
    if ($docText -notmatch [regex]::Escape($phrase)) { $failures += "A1 策略文档缺少状态声明短语：$phrase。" }
}
if ($docText -notmatch '3\.1\s*已登记的例外') { $failures += 'A1 策略文档缺少「3.1 已登记的例外」小节（删除点必须显式登记）。' }

# <lang><zh-CN>A2：产品源码不得出现审计表删除语句（无豁免）。</zh-CN>
# <en>A2: the product source must not contain audit-table delete statements (no exemptions).</en></lang>
$deletePattern = "(?i)(DELETE\s+FROM|TRUNCATE\s+TABLE)\s+\[?dbo\]?\.?\[?$auditTable"
$srcHits = @()
if (Test-Path -LiteralPath (Join-Path $repoRoot 'src')) {
    foreach ($file in (Get-ChildItem (Join-Path $repoRoot 'src') -Recurse -File -Include '*.cs', '*.sql' -ErrorAction SilentlyContinue)) {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        if ($text -match $deletePattern) { $srcHits += $file.FullName.Substring($repoRoot.Length + 1) }
    }
}
foreach ($hit in $srcHits) {
    $failures += "A2 产品源码出现审计表删除语句：$hit（生产路径不得删除审计数据，且不允许豁免）。"
}

# <lang><zh-CN>A3：仓库内其余删除点必须登记在豁免清单中（附依据）；未登记 → 失败；清单漂移 → 失败。</zh-CN>
# <en>A3: all other delete points must be registered in the allowlist with a basis; unregistered → fail; allowlist drift → fail.</en></lang>
$ALLOWED_DELETES = @{
    'dev\scripts\New-PortalP19BrowserSmokeFixture.ps1' = 'P19 浏览器冒烟 fixture 的自产自销清理：只删除该 fixture 自己创建的审计行（TargetType=BusinessApplication、TargetId 属于 fixture 业务单、ActorUserName 属于 fixture 用户），运行在可丢弃开发库上，不触碰既有审计数据。'
}

$scanRoots = @(
    (Join-Path $repoRoot 'dev\scripts'),
    (Join-Path $repoRoot 'src\Setup')
)
$otherHits = @{}
foreach ($root in $scanRoots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    foreach ($file in (Get-ChildItem $root -Recurse -File -Include '*.ps1', '*.sql', '*.cs' -ErrorAction SilentlyContinue)) {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        if ($text -match $deletePattern) {
            $rel = $file.FullName.Substring($repoRoot.Length + 1)
            $otherHits[$rel] = $true
        }
    }
}
foreach ($rel in @($otherHits.Keys)) {
    if (-not $ALLOWED_DELETES.ContainsKey($rel)) {
        $failures += "A3 出现未登记的审计表删除点：$rel（新增删除点必须先在本策略文档登记依据）。"
    }
}
foreach ($rel in @($ALLOWED_DELETES.Keys)) {
    if (-not $otherHits.ContainsKey($rel)) {
        $failures += "A3 豁免清单条目已无对应命中（漂移，请清理）：$rel。"
    }
}

# <lang><zh-CN>A4：策略文档点名要看的门禁必须存在。</zh-CN><en>A4: the gate named by the policy doc must exist.</en></lang>
if ($docText -match 'Test-PortalAuditRetentionDeclaration\.ps1') {
    if (-not (Test-Path -LiteralPath $gatePath)) {
        $failures += 'A4 策略文档点名了 Test-PortalAuditRetentionDeclaration.ps1，但该门禁文件不存在（文档与机器看守脱节）。'
    }
}
else {
    $failures += 'A4 策略文档未点名门禁 Test-PortalAuditRetentionDeclaration.ps1（登记应有机器看守）。'
}

# <lang><zh-CN>统计落盘 + 输出。</zh-CN><en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p154'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'audit-retention-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    declarationPhrases = @('尚未实现', '不得删除') | Where-Object { $docText -match [regex]::Escape($_) }
    productSourceDeleteHits = $srcHits
    registeredDeletePoints = @($ALLOWED_DELETES.Keys)
    otherDeleteHits = @($otherHits.Keys)
    gateReferencedByDoc = ($docText -match 'Test-PortalAuditRetentionDeclaration\.ps1')
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 审计留存登记看守（W-anp-P154）==='
Write-Host ('状态声明短语齐备：' + $report.declarationPhrases.Count + '/2｜产品源码删除命中：' + $srcHits.Count + '｜已登记删除点：' + $ALLOWED_DELETES.Count + '｜实际命中：' + $otherHits.Count)
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 审计留存登记看守失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 留存状态已登记：自动归档/清理未实现、不得删除审计数据；删除点全部登记且产品源码无删除。' -ForegroundColor Green
exit 0
