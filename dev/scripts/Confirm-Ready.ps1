<#
.SYNOPSIS
  <lang>
    <zh-CN>提交前确认：跑与 CI **完全相同**的三个门禁，并校验退出码。</zh-CN>
    <en>Pre-commit confirmation: runs the **same three gates** as CI and checks their exit codes.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>**要解决的问题（`W-anp-P140`）**：`C-anp-P22` 期间，我两次在门禁实际未过的情况下提交并推送 ——
    ① 单独跑门禁时新文件**尚未 `git add`**，门禁的 `git ls-files` 看不到它们，得到**假 Pass**
    （`W124` 就记过这个教训，又踩一次）；② L0 明确报 `Fail`，我**没有校验退出码**照样 `git commit && git push`，
    于是一次失败的 CI 运行被推送上去。本脚本把这三步固化成一步，不再依赖"我记得要检查"：

    1. **检查是否有未暂存的相关改动** —— `site/`、`docs/`、`.github/` 下有改动却未 `git add` 时，
       门禁看不到它们，结果不可信；此时直接提示并**拒绝继续**（退出码 2）。
    2. **按 CI 的顺序跑同样三个门禁**：`Build-PortalSite.ps1`（生成 + 双语 + N1 + N2）、
       `Test-PortalSitePublicSurface.ps1`（S1–S4）、`Test-PortalPublicDocumentation.ps1`（文档合规）。
    3. **逐个校验退出码**，任一非 0 即总体失败；全部为 0 才输出"可以提交"。

    **与 L0 的关系**：本脚本**不替代** L0。L0 是 29 个门禁的完整套件（含 .NET Framework 4.8 构建类门禁，
    需要 Visual Studio/MSBuild）；本脚本只跑 CI 会跑的那三个**静态**子集，
    目的是"提交前就知道 CI 会不会红"。完整 L0 仍应在本地另行运行。

    **前置条件**：需先 `git add`（脚本会检查并提示）；只读源码与文档，不联网、不构建 .NET。
    **验证方式**：退出码 0 = 三个门禁全过，可以提交；1 = 有门禁未过；2 = 有未暂存改动，请先 `git add`。</zh-CN>
    <en>**Problem addressed (`W-anp-P140`)**: during `C-anp-P22` I twice committed and pushed while the gates had not actually passed —
    ① when running a gate alone the new files were **not yet `git add`**ed, so the gate's `git ls-files` could not see them,
    yielding a **false Pass** (a lesson already recorded in `W124`, and I stepped on it again); ② L0 explicitly reported
    `Fail` and I **did not check the exit code**, committing and pushing anyway, so a failing CI run went out.
    This script turns those three steps into one, so it no longer depends on "remembering to check":

    1. **Check for unstaged relevant changes** — when `site/`, `docs/` or `.github/` have changes that are not `git add`ed, the
       gates cannot see them and the result is untrustworthy; the script then prompts and **refuses to continue**
       (exit code 2).
    2. **Run the same three gates in CI's order**: `Build-PortalSite.ps1` (generation + bilingual + N1 + N2),
       `Test-PortalSitePublicSurface.ps1` (S1–S4), `Test-PortalPublicDocumentation.ps1` (docs compliance).
    3. **Check each exit code**; any non-zero means overall failure; only when all are zero does it print "safe to commit".

    **Relationship to L0**: this script does **not** replace L0. L0 is the full 29-gate suite (including .NET Framework 4.8
    build gates that need Visual Studio/MSBuild); this script runs only the **static** subset that CI runs, so you know
    before committing whether CI will go red. The full L0 should still be run separately and locally.

    **Prerequisites**: requires a prior `git add` (the script checks and prompts); reads only source and docs — no network,
    no .NET build.
    **Verification**: exit code 0 = all three gates passed, safe to commit; 1 = a gate failed; 2 = unstaged changes exist,
    please `git add` first.</en>
  </lang>
#>
param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

$gates = @(
    'dev/scripts/Build-PortalSite.ps1'
    'dev/scripts/Test-PortalSitePublicSurface.ps1'
    'dev/scripts/Test-PortalPublicDocumentation.ps1'
)

# <lang>
#   <zh-CN>步骤 1：未暂存改动检查。这三个目录正是门禁扫描的范围 —— 有改动却没 add，门禁就看不到，
#     结果不可信（本次要防的核心错误）。
#
#     **用 `status --porcelain` 而不是 `diff --name-only 2>&1`**（`W-anp-P140` 自修）：
#     初版把 stderr 合并进来，结果 git 的 `warning: ... LF will be replaced by CRLF` 也被当成文件名，
#     在**明明没有未暂存改动**时误报 exit 2。porcelain 输出到 stdout，且状态位可精确判断：
#       - `??` 未跟踪（门禁的 `git ls-files` 看不到）→ 需 add
#       - ` M` 已跟踪但工作区改了未暂存（门禁读到的是旧内容）→ 需 add
#       - `M ` / `A ` 已暂存 → 门禁能看到，放行</zh-CN>
# </lang>
$unstaged = New-Object 'System.Collections.Generic.List[string]'
foreach ($dir in @('site', 'docs', '.github')) {
    $full = Join-Path $RepoRoot $dir
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $status = & git -C $RepoRoot status --porcelain -- $dir
    foreach ($entry in $status) {
        $line = ('{0}' -f $entry)
        if ($line.Trim() -eq '') { continue }
        $x = $line.Substring(0, 1)
        $y = if ($line.Length -ge 2) { $line.Substring(1, 1) } else { ' ' }
        $path = if ($line.Length -gt 3) { $line.Substring(3).Trim() } else { '' }
        if ($path -eq '') { continue }
        # 未跟踪，或工作区有改动但未暂存 → 门禁结果不可信
        if ($x -eq '?' -or $y -ne ' ') {
            $unstaged.Add(($dir + '/') + $path)
        }
    }
}

if ($unstaged.Count -gt 0) {
    Write-Host '[ready] 检测到已修改但未暂存的文件（门禁用 git 追踪列表，看不到它们，结果不可信）：'
    foreach ($u in ($unstaged | Select-Object -Unique | Select-Object -First 15)) { Write-Host ('  - ' + $u) }
    Write-Host '[ready] 请先执行 git add 再运行本脚本。'
    Write-Host 'RESULT: NotReady'
    exit 2
}

# <lang>
#   <zh-CN>步骤 2+3：按 CI 顺序跑三个门禁，**逐个校验退出码**（原生命令不会因 ErrorActionPreference 抛异常）。</zh-CN>
# </lang>
$failures = 0
foreach ($g in $gates) {
    $path = Join-Path $RepoRoot $g
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host ('[ready] 门禁脚本不存在: ' + $g)
        $failures++
        continue
    }

    # <lang>
    #   <zh-CN>**生成产物必须先入库，再让门禁扫描**（`W-anp-P142` 修正，由译本批次 13 实测暴露）。
    #     门禁（含 `Test-PortalSitePublicSurface.ps1`）用 git 追踪列表枚举文件。
    #     `Build-PortalSite.ps1` 会**新生成** `site/<path>/index.md`，而这些文件在生成时**尚未入库** ——
    #     若不在生成后补一次 `git add`，门禁看到的就不含新页面，**新上架页面的生成产物要延迟一个批次才被检查**。
    #     实测证据：批次 13 故意保留两处私有仓提及上架，门禁只报了译本（`_translations/`，手写即入库）
    #     **未报生成页**（`site/deployment-rollback-guide/`，当时未入库）。
    #     本脚本本就要求"先 add 再跑"，此处补 add 与流程语义一致（不是代用户决定提交内容）。
    #     只 add 站点目录 `site/`，不触碰其他路径。</zh-CN>
    #   <en>**Generated output must be staged before the gates scan it** (`W-anp-P142` fix, exposed by translation batch 13).
    #     The gates (including `Test-PortalSitePublicSurface.ps1`) enumerate files via the git tracked list.
    #     `Build-PortalSite.ps1` **newly generates** `site/<path>/index.md`, and those files are **not yet staged** when it
    #     finishes — without an extra `git add` after generation, the gates do not see the new pages, so **the generated output
    #     of a newly published page is only checked one batch later**.
    #     Evidence: in batch 13 two private-repository mentions were published deliberately; the gate reported only the
    #     translation source (`_translations/`, hand-written and therefore staged) and **not the generated page**
    #     (`site/deployment-rollback-guide/`, unstaged at the time).
    #     This script already requires "add first, then run", so adding here is consistent with the flow (it does not decide
    #     commit contents for the user). Only the site directory `site/` is added; no other path is touched.</en>
    # </lang>
    if ((Split-Path $g -Leaf) -ne 'Build-PortalSite.ps1') {
        $null = & git -C $RepoRoot add -A 'site' 2>&1
    }

    Write-Host ''
    Write-Host ('[ready] === ' + (Split-Path $g -Leaf) + ' ===')
    & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoProfile -File $path 2>&1 | Out-String | Write-Host
    $code = $LASTEXITCODE
    Write-Host ('[ready]   -> 退出码 ' + $code + $(if ($code -eq 0) { '（通过）' } else { '（未通过）' }))
    if ($code -ne 0) { $failures++ }
}

Write-Host ''
if ($failures -gt 0) {
    Write-Host ('[ready] ' + $failures + ' 个门禁未通过 —— **不要提交**（否则 CI 会红）。')
    Write-Host 'RESULT: NotReady'
    exit 1
}

Write-Host '[ready] 三个门禁全部通过，与 CI 同口径 —— 可以提交。'
Write-Host '[ready] 注意：本脚本只覆盖 CI 会跑的静态子集；完整 L0 套件（含 .NET Framework 4.8 构建类门禁）仍需另行本地运行。'
Write-Host 'RESULT: Ready'
exit 0
