#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>看守"请假 / 报销不含审批"这条边界：公开文档必须声明，源码必须没有审批动作（L0）。</zh-CN>
  <en>Guards the "leave/expense has no approval" boundary: the public doc must declare it and the source must contain no approval action (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P153`）交付。验收判据 V3 要求"审批流转边界在公开文档中有明确声明"。
  本门禁把这条声明**双向看守**起来——否则它会静默过期：
    A1 公开文档 `docs/user-guide.md` 必须含「审批流转边界」小节，并写明关键事实：
       仅落库 + 生成待办、审批操作不在本模块内。
    A2 源码侧必须**没有**审批动作：`LeaveRequest.ascx.cs` 与 `ExpenseReimbursement.ascx.cs`
       不得出现 `Approve` / `Reject` 动作方法或按钮；`MyWorkItems` 必须保持只读
       （不得出现审批 / 完成类动作按钮）。
    A3 文档里点名要看的门禁必须存在（防止文档引用了不存在的机器看守）。
  任一侧变化而另一侧没同步即失败——将来真加了审批流程，本门禁会强制先把文档改对。

  证明边界：只证明"文档声明存在 + 源码无审批动作 + 文档点名的门禁存在"；
  **不证明审批流程在外部系统被正确执行**（本门户不含该流程，外部实现不在仓库内）、
  **不证明文档措辞的语义正确**（人工复核承载）、**不证明待办指派角色始终为 `Admins`**
  （那是运行期配置，由配置与运行期证据承载）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalApprovalBoundaryDocs.ps1'
  产物：temp\p153\approval-boundary-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 环境/文件前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P153`) deliverable. Acceptance criterion V3 requires an explicit declaration of the approval-flow boundary
  in the public docs. This gate guards that declaration **in both directions**, otherwise it silently goes stale:
    A1 the public doc `docs/user-guide.md` must contain the "approval flow boundary" section stating the key facts: persist + create a
       work item only, and approval actions are not part of the module.
    A2 the source must contain **no** approval action: `LeaveRequest.ascx.cs` and `ExpenseReimbursement.ascx.cs` must not define
       `Approve` / `Reject` action methods or buttons, and `MyWorkItems` must stay read-only (no approval or completion action buttons).
    A3 the gate the doc names must exist (so the doc cannot point at a machine guard that is not there).
  If either side changes without the other being updated, the gate fails — when approval is genuinely added later, this gate forces
  the doc to be corrected first.

  Proof boundary: proves only that "the declaration exists in the doc, the source has no approval action, and the named gate exists";
  does **not** prove that the external approval process is executed correctly (this portal does not contain it and the external
  implementation is outside the repository), does **not** prove the wording is semantically correct (human review), and does **not**
  prove the work item is always assigned to `Admins` (runtime configuration, covered by configuration and runtime evidence).

  Usage (repository root): & 'dev\scripts\Test-PortalApprovalBoundaryDocs.ps1'
  Output: temp\p153\approval-boundary-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing environment/file prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalApprovalBoundaryDocs.ps1'   # 看守审批边界声明与源码一致</zh-CN>
  <en>& 'dev\scripts\Test-PortalApprovalBoundaryDocs.ps1'   # guard the approval-boundary declaration against the source</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P153` 交付；边界声明写在 `docs/user-guide.md`「审批流转边界（必读）」小节。</zh-CN>
  <en>Provenance: delivered by `W-anp-P153`; the declaration lives in the "approval flow boundary (must read)" section of `docs/user-guide.md`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$docPath = Join-Path $repoRoot 'docs\user-guide.md'
$gatePath = Join-Path $repoRoot 'dev\scripts\Test-PortalApprovalBoundaryDocs.ps1'
$modulesDir = Join-Path $repoRoot 'src\Portal\DesktopModules'

$failures = @()

if (-not (Test-Path -LiteralPath $docPath)) {
    Write-Host '[PREREQ-MISSING] 文档不存在：' + $docPath + '（exit 2）'
    exit 2
}
$docText = [System.IO.File]::ReadAllText($docPath)

# <lang><zh-CN>A1：文档必须含边界小节与关键事实。标题必须**精确匹配**——注入法实测证明前缀匹配是弱断言
# （把标题改成「审批流转边界说明（旧）」仍能命中前缀，等于没断言）。</zh-CN>
# <en>A1: the doc must contain the boundary section and the key facts. The heading must match **exactly** — injection testing proved a
# prefix match is a weak assertion (renaming the heading to "审批流转边界说明（旧）" still matches the prefix, i.e. it asserts nothing).</en></lang>
if ($docText -notmatch [regex]::Escape('### 审批流转边界（必读）')) { $failures += 'A1 文档缺少「审批流转边界（必读）」小节（V3：边界必须在公开文档中明确声明）。' }
$requiredPhrases = @('仅落库', '审批操作不在本模块内')
foreach ($phrase in $requiredPhrases) {
    if ($docText -notmatch [regex]::Escape($phrase)) { $failures += "A1 文档缺少关键事实短语：$phrase。" }
}

# <lang><zh-CN>A2：源码不得出现审批动作。注入法实测证明 `\bApprove\b` 词边界匹配是弱断言——
# `ApproveRequest` / `ApproveLeave` 这类方法名会被漏掉，故改为子串匹配（PowerShell `-match`
# 默认大小写不敏感）。匹配"动作语义"而非字面「审批」二字：注释里出现「审批角色」是正常的
# （待办确实指派给审批角色），不能因此误报。</zh-CN>
# <en>A2: the source must not contain approval actions. Injection testing proved the word-boundary pattern `\bApprove\b` is a weak
# assertion — method names such as `ApproveRequest` / `ApproveLeave` slip through — so substring matching is used instead (PowerShell
# `-match` is case-insensitive by default). The match targets action semantics rather than the literal word "审批": comments mentioning
# "审批角色" (approval role) are legitimate, because the work item really is assigned to an approval role, and must not be misreported.</en></lang>
$sourceChecks = @(
    @{ file = 'LeaveRequest\LeaveRequest.ascx.cs'; label = '请假申请' },
    @{ file = 'ExpenseReimbursement\ExpenseReimbursement.ascx.cs'; label = '费用报销' }
)
foreach ($check in $sourceChecks) {
    $path = Join-Path $modulesDir $check.file
    if (-not (Test-Path -LiteralPath $path)) {
        $failures += "A2 源文件不存在：$($check.file)（前置缺失，请确认模块位置）。"
        continue
    }
    $text = [System.IO.File]::ReadAllText($path)
    foreach ($pattern in @('Approve', 'Reject')) {
        if ($text -match $pattern) {
            $failures += "A2 $($check.label)源码出现审批动作标识（$pattern）——边界声明已过期：要么移除审批动作，要么先更新公开文档的边界声明。"
        }
    }
}

# <lang><zh-CN>A2b：MyWorkItems 必须保持只读（除筛选外不得有状态变更类动作按钮）。</zh-CN>
# <en>A2b: MyWorkItems must stay read-only (no state-changing action buttons beyond filters).</en></lang>
$myWorkItemsAscx = Join-Path $modulesDir 'MyWorkItems\MyWorkItems.ascx'
if (Test-Path -LiteralPath $myWorkItemsAscx) {
    $ascx = [System.IO.File]::ReadAllText($myWorkItemsAscx)
    foreach ($pattern in @('Approve', 'Reject', 'Complete', '审批', '驳回')) {
        if ($ascx -match $pattern) {
            $failures += "A2b MyWorkItems 出现非只读动作标识（$pattern）——它应当只做待办的只读投影；若确需新增动作，先更新公开文档边界声明。"
        }
    }
}

# <lang><zh-CN>A4（Q6 / W-anp-P295）：把 W-anp-P153 的豁免清单固化为**白名单**，扫描【其余所有】用户模块文件不得含 Approve / Reject 动作标识。
# 与 A2 只盯两个提交模块不同，A4 扫描整个 DesktopModules，仅豁免「本就承载审批语义」的模块（后台审核页与修正审核）及待办只读投影里的注释字样。
# 凡白名单之外出现 Approve / Reject（子串、大小写不敏感）即失败——将来有人在**提交类**模块（LeaveRequest / ExpenseReimbursement 等）引入审批，门禁强制先把公开文档边界声明改对。
# 为何按「文件/目录」级豁免而非逐行：子串匹配会把注释里的 "reject submission" 也命中，若逐行豁免则易把真审批动作一并放掉，故以白名单文件/目录为粒度放行。</zh-CN>
# <en>A4 (Q6 / W-anp-P295): formalize the W-anp-P153 exemption list as a **whitelist**; scan ALL other user-module files for Approve / Reject action markers.
# Unlike A2 which only watches the two submission modules, A4 scans the entire DesktopModules and only exempts modules that legitimately carry approval semantics (the back-office review page, the correction review) and the read-only work-item projection's comment wording.
# Any non-whitelisted file containing Approve / Reject (substring, case-insensitive) fails — when approval is introduced into a *submission* module (LeaveRequest / ExpenseReimbursement etc.), the gate forces the public doc boundary declaration to be corrected first.
# Why file/directory-level exemption instead of per-line: substring matching also catches comment text like "reject submission"; per-line exemptions would easily let a real approval action slip through, so the whitelist is at file/directory granularity.</en></lang>
# <lang><zh-CN>豁免白名单 = 本就合法含 Approve/Reject 字样之处（来自 W-anp-P153 边界声明 + 全量扫描实测）：
# 两个「审核/审批」模块按目录级豁免（它们本就承载审批语义）；其余为散落于注释/JS/MIME 的无关字样，按文件级豁免。
# 白名单之外的任何用户模块文件不得含 Approve/Reject（子串、大小写不敏感）。</zh-CN>
# <en>Exemption whitelist = places where Approve/Reject is legitimately present (from the W-anp-P153 boundary declaration + a full-scan measurement):
# the two "review/approval" modules are exempt at directory level (they carry approval semantics by design); the rest are incidental occurrences in comments/JS/MIME, exempt at file level.
# No user-module file outside this whitelist may contain Approve/Reject (substring, case-insensitive).</en></lang>
$approvalBoundaryWhitelist = @(
    'BusinessApplicationRequest',               # <zh-CN>后台审核页：本就承载审批语义（整目录豁免）</zh-CN><en>back-office review page: carries approval semantics (whole dir exempt)</en>
    'EmployeeProfileCorrectionRequest',         # <zh-CN>修正审核：承载审核语义（整目录豁免）</zh-CN><en>correction review: carries review semantics (whole dir exempt)</en>
    'Contacts.ascx',                            # <zh-CN>联系人模块注释含该词</zh-CN><en>contacts module comment wording</en>
    'DiscussDetails.aspx.cs',                   # <zh-CN>讨论详情注释含该词</zh-CN><en>discussion detail comment wording</en>
    'Discussion.ascx.cs',                       # <zh-CN>讨论模块注释含该词</zh-CN><en>discussion module comment wording</en>
    'EditDocs.aspx.cs',                         # <zh-CN>富文本编辑器内嵌 JS/MIME 含 Reject 字样</zh-CN><en>rich-text editor embedded JS/MIME mentions Reject</en>
    'EditImage.aspx.cs',                         # <zh-CN>图片编辑器同</zh-CN><en>image editor, same</en>
    'EditLinks.aspx.cs',                         # <zh-CN>链接编辑器同</zh-CN><en>links editor, same</en>
    'EditXml.aspx.cs',                          # <zh-CN>XML 编辑器同</zh-CN><en>xml editor, same</en>
    'Links.ascx.cs',                            # <zh-CN>链接模块注释含该词</zh-CN><en>links module comment wording</en>
    'PortalStatusLocalizer.cs',                 # <zh-CN>状态本地化器为 W-anp-P153 豁免文件（注释含审批边界说明）</zh-CN><en>status localizer is a W-anp-P153 exempt file (comment mentions approval boundary)</en>
    'QuickLinks.ascx.cs',                       # <zh-CN>快捷链接模块注释含该词</zh-CN><en>quick links module comment wording</en>
    'MyWorkItems\MyWorkItems.ascx.cs'           # <zh-CN>只读投影，仅注释含 reject 字样</zh-CN><en>read-only projection, only a comment mentions reject</en>
)
$allModuleFiles = Get-ChildItem -Path $modulesDir -Recurse -Include '*.cs', '*.ascx' -ErrorAction SilentlyContinue
foreach ($file in $allModuleFiles) {
    $rel = $file.FullName.Substring($modulesDir.Length + 1).Replace('/', '\')
    $exempt = $false
    foreach ($w in $approvalBoundaryWhitelist) {
        if ($rel -eq $w -or $rel.StartsWith($w + '\')) { $exempt = $true; break }
    }
    if ($exempt) { continue }
    $text = [System.IO.File]::ReadAllText($file.FullName)
    foreach ($pattern in @('Approve', 'Reject')) {
        if ($text -match $pattern) {
            $failures += "A4 用户模块 $rel 出现审批动作标识（$pattern）——不在豁免白名单中。若确为合法审批，请把该模块加入白名单并同步更新 docs/user-guide.md 的审批边界声明；否则移除审批动作。"
        }
    }
}

# <lang><zh-CN>A3：文档点名的门禁必须真实存在。</zh-CN><en>A3: the gate named by the doc must exist.</en></lang>
if ($docText -match 'Test-PortalApprovalBoundaryDocs\.ps1') {
    if (-not (Test-Path -LiteralPath $gatePath)) {
        $failures += 'A3 文档点名了 Test-PortalApprovalBoundaryDocs.ps1，但该门禁文件不存在（文档与机器看守脱节）。'
    }
}
else {
    $failures += 'A3 文档未点名门禁 Test-PortalApprovalBoundaryDocs.ps1（边界声明应有机器看守，见 A1）。'
}

# <lang><zh-CN>统计落盘 + 输出。</zh-CN><en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p153'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'approval-boundary-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    docHasSection = ($docText -match '###\s*审批流转边界')
    docHasPhrases = @($requiredPhrases | Where-Object { $docText -match [regex]::Escape($_) })
    gateReferencedByDoc = ($docText -match 'Test-PortalApprovalBoundaryDocs\.ps1')
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 审批流转边界看守（W-anp-P153）==='
Write-Host ('文档含边界小节：' + $report.docHasSection + '｜关键短语齐备：' + ($report.docHasPhrases.Count) + '/' + $requiredPhrases.Count + '｜文档点名门禁：' + $report.gateReferencedByDoc)
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 审批边界看守失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 文档已明确声明审批边界，且源码侧无任何审批动作。' -ForegroundColor Green
exit 0
