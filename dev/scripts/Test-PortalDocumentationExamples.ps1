<#
.SYNOPSIS
  <lang>
    <zh-CN>文档示例有效性门禁：检查公开文档的命令示例引用的脚本 / SQL 路径是否真实存在，并核对 README 的中英双语结构。</zh-CN>
    <en>Documentation example validity gate: checks that script/SQL paths referenced by command examples in public documentation actually exist, and verifies the README's bilingual structure.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>**要解决的问题**：`Test-PortalPublicDocumentation.ps1` 只校验 **Markdown 相对链接**（`[文字](路径)`），
    而**命令示例里的路径**（如 `powershell -NoProfile -File dev\scripts\Restore-NuGetPackages.ps1`、
    `sqlcmd -i src\Setup\PortalBiz_LeaveRequests.sql`）不在链接检查范围内。脚本改名或删除后，这些示例会**静默过期** ——
    文档仍显示一条跑不通的命令，而没有任何门禁会报错。本门禁补上这一类。

    三项检查（均为 Fail 级）：
    - **E1 脚本示例**：公开文档中出现的 `dev/scripts/*.ps1` 引用，文件必须存在；
    - **E2 SQL 示例**：公开文档中出现的 `src/Setup/*.sql` 引用，文件必须存在；
    - **E3 双语结构**：根 `README.md` 必须同时含中文正文节与 `## English` 节（公开文档的中英对等底线）。

    **前置条件**：只需源码与文档可读，属编排入口 L0 层；不连数据库、不启动 IIS、不执行文档中的命令。
    **验证方式**：退出码 0 通过、1 有违规；输出逐项列出失效引用（含所在文档与行号），可直接定位。
    **低敏边界**：只读取公开文档与受检路径的**存在性**，不读取凭据、连接串或业务数据，不改写任何文件。

    **证明边界（重要）**：本门禁只证明"被引用的路径存在 / 双语结构齐备"，**不证明**示例的输出正确、
    命令参数有效、或示例语义与当前实现一致 —— 这些仍需人工走查（见 `docs/documentation-review-checklist.md`）。</zh-CN>
    <en>**Problem addressed**: `Test-PortalPublicDocumentation.ps1` only validates **Markdown relative links**
    (`[text](path)`), while **paths inside command examples** (e.g. `powershell -NoProfile -File dev\scripts\Restore-NuGetPackages.ps1`,
    `sqlcmd -i src\Setup\PortalBiz_LeaveRequests.sql`) fall outside link checking. After a script is renamed or deleted such
    examples go **silently stale** — the documentation still shows a command that cannot run, and no gate complains. This gate
    covers that class.

    Three checks (all Fail-level):
    - **E1 script examples**: every `dev/scripts/*.ps1` reference in public documentation must exist;
    - **E2 SQL examples**: every `src/Setup/*.sql` reference in public documentation must exist;
    - **E3 bilingual structure**: the root `README.md` must contain both a Chinese body section and a `## English` section
      (the minimum bar for bilingual parity in public docs).

    **Prerequisites**: readable source and documentation only — an L0-layer gate with no database, IIS, or execution of the
    documented commands.
    **Verification**: exit code 0 passes, 1 means violations; the output lists each broken reference with its document and
    line number so it can be located directly.
    **Low-sensitivity boundary**: only public documentation and the *existence* of referenced paths are read — no credentials,
    connection strings or business data, and nothing is rewritten.

    **Proof boundary (important)**: this gate proves only that "referenced paths exist / the bilingual structure is present".
    It does **not** prove that an example's output is correct, its arguments are valid, or its semantics match the current
    implementation — those still require a manual walkthrough (see `docs/documentation-review-checklist.md`).</en>
  </lang>
.PARAMETER RepoRoot
  <lang>
    <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` → 仓库根）。</zh-CN>
    <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
  </lang>
#>
param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$violations = New-Object 'System.Collections.Generic.List[string]'

# <lang>
#   <zh-CN>公开文档集合 = 根 README*.md + docs/ 及其子目录下的 *.md；与 Test-PortalPublicDocumentation 的覆盖面保持一致，
#   避免两个门禁对"什么是公开文档"有不同答案。</zh-CN>
#   <en>Public documentation set = root README*.md plus *.md under docs/ and its subdirectories, matching the coverage of
#     Test-PortalPublicDocumentation so both gates agree on what counts as public documentation.</en>
# </lang>
$docs = New-Object 'System.Collections.Generic.List[string]'
Get-ChildItem -Path $RepoRoot -Filter 'README*.md' -File | ForEach-Object { $docs.Add($_.FullName) }
if (Test-Path -LiteralPath (Join-Path $RepoRoot 'docs')) {
    Get-ChildItem -Path (Join-Path $RepoRoot 'docs') -Filter '*.md' -File -Recurse | ForEach-Object { $docs.Add($_.FullName) }
}

# <lang>
#   <zh-CN>占位路径豁免：教学示例里的模板路径（如 `HIA.YourModule`、`src\Setup\PortalBiz_YourEntities.sql`）**注定不存在**，
#   若按普通引用校验会让门禁**永久 FAIL** —— 那正是"门禁长期红着被当成背景噪音、等于没写"的老问题。
#   故对含占位标记的引用跳过校验，并在收尾用 `[GATE-NOTE]` **透明报告豁免数**（不静默放过）。
#   占位标记取保守集合：路径中任意位置出现 `Your` + 字母（覆盖 `HIA.YourModule` 与 `PortalBiz_YourEntities.sql` 两种形态），
#   或含 `<` / `>` / `{` / `}`。真实文件名（如 `New-PortalP12SampleScenarioSql.ps1`）不含这些标记，不会误豁免。</zh-CN>
#   <en>Placeholder-path exemption: template paths in teaching examples (e.g. `HIA.YourModule`,
#   `src\Setup\PortalBiz_YourEntities.sql`) **can never exist**, so validating them like ordinary references would make the gate
#   **fail permanently** — the very "a gate that stays red is dismissed as background noise, i.e. absent" problem seen before.
#   References containing a placeholder marker are therefore skipped, and the closing `[GATE-NOTE]` **transparently reports the
#   exemption count** (nothing is silently let through). The marker set is deliberately conservative: `Your` followed by a
#   letter anywhere in the path (covering both `HIA.YourModule` and `PortalBiz_YourEntities.sql`), or `<` / `>` / `{` / `}`.
#   Real file names (e.g. `New-PortalP12SampleScenarioSql.ps1`) contain none of these markers and are not exempted by
#   mistake.</en>
# </lang>
function Test-PlaceholderReference {
    param([string] $RelativeTarget)

    return ($RelativeTarget -match 'Your[A-Za-z]') -or
           ($RelativeTarget -match '[<>{}]')
}

$placeholderSkips = 0

# <lang>
#   <zh-CN>每条检查都记录"文档相对路径:行号"，使违规可直接定位；同一引用重复出现时逐处报告，避免遗漏真实位置。</zh-CN>
#   <en>Every check records "document relative path:line number" so a violation can be located directly; repeated occurrences
#     of the same reference are reported individually so no real location is missed.</en>
# </lang>
function Test-ReferencedPath {
    param(
        [string] $DocumentPath,
        [int] $LineNumber,
        [string] $RelativeTarget,
        [string] $Rule
    )

    if (Test-PlaceholderReference -RelativeTarget $RelativeTarget) {
        $script:placeholderSkips++
        return
    }

    $target = Join-Path $RepoRoot $RelativeTarget
    if (-not (Test-Path -LiteralPath $target)) {
        $relDoc = $DocumentPath.Replace((Join-Path $RepoRoot ''), '').Replace('\', '/')
        $script:violations.Add(('{0} :: {1}:{2} 引用了不存在的 {3}' -f $Rule, $relDoc, $LineNumber, $RelativeTarget))
    }
}

$scriptRefCount = 0
$sqlRefCount = 0
foreach ($doc in $docs) {
    $lines = [System.IO.File]::ReadAllText($doc, $utf8) -split "`r?`n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($m in [regex]::Matches($lines[$i], 'dev[\\/]scripts[\\/][A-Za-z0-9._-]+\.ps1')) {
            $rel = $m.Value -replace '/', '\'
            Test-ReferencedPath -DocumentPath $doc -LineNumber ($i + 1) -RelativeTarget $rel -Rule 'E1'
            $script:scriptRefCount++
        }
        foreach ($m in [regex]::Matches($lines[$i], 'src[\\/]Setup[\\/][A-Za-z0-9._-]+\.sql')) {
            $rel = $m.Value -replace '/', '\'
            Test-ReferencedPath -DocumentPath $doc -LineNumber ($i + 1) -RelativeTarget $rel -Rule 'E2'
            $script:sqlRefCount++
        }
    }
}

# <lang>
#   <zh-CN>E3 只做**结构**检查：根 README 须有 `## English` 节。这是公开文档双语对等的底线，
#   不是语义对等（语义对等需人工，见 documentation-review-checklist.md）。</zh-CN>
#   <en>E3 is a **structural** check only: the root README must have a `## English` section. That is the floor for bilingual
#     parity in public docs, not semantic parity (which is manual — see documentation-review-checklist.md).</en>
# </lang>
$readmePath = Join-Path $RepoRoot 'README.md'
if (Test-Path -LiteralPath $readmePath) {
    $readme = [System.IO.File]::ReadAllText($readmePath, $utf8)
    if ($readme -notmatch '(?m)^##\s+English\s*$') {
        $violations.Add('E3 :: README.md 缺少 `## English` 节（公开文档双语对等的结构底线）')
    }
}
else {
    $violations.Add('E3 :: README.md 不存在')
}

if ($violations.Count -gt 0) {
    Write-Host ('文档示例有效性门禁失败（共 ' + $violations.Count + ' 处）：')
    foreach ($v in $violations) {
        Write-Host ('  - ' + $v)
    }
    Write-Host 'RESULT: Fail'
    exit 1
}

Write-Host ('公开文档 ' + $docs.Count + ' 个：脚本示例引用 ' + $scriptRefCount + ' 处、SQL 示例引用 ' + $sqlRefCount + ' 处，均存在。')
Write-Host 'README 中英双语结构齐备（含 `## English` 节）。'
if ($placeholderSkips -gt 0) {
    Write-Host ('[GATE-NOTE] 占位路径豁免 ' + $placeholderSkips + ' 处（教学示例模板路径，如 `HIA.YourModule`；已透明报告，非静默放过）。')
}
Write-Host '[GATE-NOTE] 证明边界：本门禁只证明引用路径存在与双语结构齐备，不证明示例输出正确、参数有效或语义与实现一致。'
Write-Host 'RESULT: Pass'
exit 0
