<#
.SYNOPSIS
  <lang>
    <zh-CN>语义抽样**候选定位器**：按风险面定位"值得人工看一眼"的注释候选，供 C-anp-P25 语义抽样使用。</zh-CN>
    <en>Semantic sampling **candidate locator**: locates "worth a human look" comment candidates by risk surface, for C-anp-P25 semantic sampling.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>**它是什么**：可重复的"候选定位 + 人工判定分离"机制中的**前半段** —— 只负责把候选**找出来并列出**，
    **不做任何达标/缺陷判定**，因此**没有 Pass/Fail，也永远不接入 L0 门禁**。判定由人工按
    `work-zone/dev/plans/works/W-anp-P113-semantic-sampling-framework.md` 固化的五层级 rubric 完成。

    **为什么必须分离**：注释的"结构标记齐备"可由门禁证明（`Test-PortalRopCommentBaseline.ps1`），
    但"语义是否充分"只能由人判断。若把候选定位包装成门禁，会重演"门禁总是红 → 被当成背景噪音 → 等于没写"；
    若直接判定达标，又是把不可自动化的事情假装自动化。故本脚本刻意**只给候选、不给结论**。

    **四个风险面**（`-Surface`）：

    | 面 | 定位什么 | 窗口 |
    | --- | --- | --- |
    | `Branches` | C# 的 `if` / `return` / `throw` / `catch` 等分支 | 前 3 行 |
    | `DataAccess` | 数据访问的默认值 / 回退点（`??`、`IsNullOrWhiteSpace` 三元） | 前 3 行 |
    | `Markup` | 标记层 `<asp:HiddenField>`、`Visible="false"`、`<%#` 绑定、`CommandName` | 前 12 行 |
    | `DynamicSql` | SQL 文本拼接（注入边界候选） | 前 12 行 |

    **窗口为什么不一样**：C# 的行注释紧邻语句（3 行足够）；而**标记层的 `<%-- … --%>` 注释块通常放在一整组控件之前**
    （实测距离 5–14 行），用 3 行会大量误报 —— 这是 `W-anp-P115` 实测修正的结果，不要为了"统一"而改回 3 行。

    **已知误报源（人工判定时请先排除）**：
    - `DynamicSql` 面的 `FROM` / `WHERE` 会匹配普通英文句（如 `"Removed role id " + id + " from user."`）；
      初版曾因 `(?i)SELECT` 匹配 "Selected deployed …" 而误报，已加完整词约束，**普通英文句仍可能残留**。
    - 候选只表示"附近没找到注释"，**不代表该处一定缺说明** —— 说明可能写在方法级 XML 文档
      （`<summary>` / `<remarks>` / `<param>`）里，那同样是**有效覆盖**，不要重复补强。

    **前置条件**：只读源码与标记文件，属编排入口 L0 层；不连数据库、不启动 IIS、不写任何文件。
    **验证方式**：输出候选清单（文件:行号 + 代码行 + 是否含边界关键词线索），不设退出码语义。
    **低敏边界**：只读取受检文件的文本内容用于模式匹配，不读取连接串、凭据或业务数据。</zh-CN>
    <en>**What it is**: the **first half** of a repeatable "candidate location + human judgement" mechanism — it only *finds and lists*
    candidates and makes **no compliance or defect judgement**, so it has **no Pass/Fail and is never wired into the L0 gate**.
    Judgement belongs to a human, using the five-level rubric fixed in
    `work-zone/dev/plans/works/W-anp-P113-semantic-sampling-framework.md`.

    **Why the split is mandatory**: "structural markers present" is machine-provable (`Test-PortalRopCommentBaseline.ps1`), but
    "is the semantics adequate" is human-only. Wrapping candidate location as a gate would replay "a gate that is always red →
    dismissed as background noise → effectively absent"; judging compliance directly would pretend to automate what cannot be
    automated. So this script deliberately **gives candidates, never verdicts**.

    **Four risk surfaces** (`-Surface`):

    | Surface | What it locates | Window |
    | --- | --- | --- |
    | `Branches` | C# `if` / `return` / `throw` / `catch` branches | 3 lines |
    | `DataAccess` | data-access default/fallback points (`??`, `IsNullOrWhiteSpace` ternaries) | 3 lines |
    | `Markup` | markup `<asp:HiddenField>`, `Visible="false"`, `<%#` binding, `CommandName` | 12 lines |
    | `DynamicSql` | SQL text concatenation (injection-boundary candidates) | 12 lines |

    **Why windows differ**: C# line comments sit next to the statement (3 lines suffice), whereas **markup `<%-- … --%>` blocks
    usually precede an entire control group** (measured at 5–14 lines away); a 3-line window misreports heavily — this is the
    measured correction from `W-anp-P115`, do not "unify" it back to 3.

    **Known false-positive sources (rule these out first when judging)**:
    - In `DynamicSql`, `FROM` / `WHERE` also match ordinary English (e.g. `"Removed role id " + id + " from user."`);
      an earlier version mis-reported "Selected deployed …" because `(?i)SELECT` matched the substring — a whole-word constraint
      was added, but **ordinary prose can still slip through**.
    - A candidate only means "no comment found nearby", **not** "the point is unexplained" — the explanation may live in
      method-level XML docs (`<summary>` / `<remarks>` / `<param>`), which is equally **valid coverage**; do not duplicate it.

    **Prerequisites**: reads source and markup only — an L0-layer entry point with no database, IIS, or file writes.
    **Verification**: emits a candidate list (file:line + code line + whether a boundary-keyword hint was found); no exit-code
    semantics.
    **Low-sensitivity boundary**: reads only file text for pattern matching — no connection strings, credentials or business data.</en>
  </lang>
.PARAMETER Surface
  <lang>
    <zh-CN>风险面：`Branches` / `DataAccess` / `Markup` / `DynamicSql` / `All`（默认 `All`）。</zh-CN>
    <en>Risk surface: `Branches` / `DataAccess` / `Markup` / `DynamicSql` / `All` (default `All`).</en>
  </lang>
.PARAMETER Path
  <lang>
    <zh-CN>限定扫描路径（相对仓库根，逗号分隔）；留空时按各面的默认范围扫描。</zh-CN>
    <en>Limits the scan path (relative to the repository root, comma separated); empty uses each surface's default scope.</en>
  </lang>
#>
param(
    [ValidateSet('Branches', 'DataAccess', 'Markup', 'DynamicSql', 'All')]
    [string] $Surface = 'All',
    [string] $Path
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Get-CommentLine {
    param([string] $Line)

    return ($Line -match '^\s*//') -or
           ($Line -match '<lang>') -or ($Line -match '</lang>') -or
           ($Line -match '<zh-CN>') -or ($Line -match '</zh-CN>') -or
           ($Line -match '<en>') -or ($Line -match '</en>') -or
           ($Line -match '<%--') -or ($Line -match '--%>')
}

function Test-HasNearbyComment {
    param([string[]] $Lines, [int] $Index, [int] $Window)

    for ($k = [Math]::Max(0, $Index - $Window); $k -lt $Index; $k++) {
        if (Get-CommentLine -Line $Lines[$k]) { return $true }
    }
    return $false
}

$boundaryHint = '(?i)(编译期常量|常量|参数化|SqlParameter|不参与|拼接|constant|compile-time|parameteriz|concatenat|injection|只允许|may only)'

function Test-BoundaryHint {
    param([string[]] $Lines, [int] $Index, [int] $Window)

    for ($k = [Math]::Max(0, $Index - $Window); $k -lt $Index; $k++) {
        if ($Lines[$k] -match $boundaryHint) { return $true }
    }
    return $false
}

# 各面：正则 + 窗口（窗口差异见 .DESCRIPTION，不要统一）
$surfaceRules = @{
    'Branches'   = @{ Pattern = '(?m)^\s*(if\s*\(|else\s+if\s*\(|return\s|throw\s|catch\s*\()'; Window = 3 }
    'DataAccess' = @{ Pattern = '(?i)(\?\?|IsNullOrWhiteSpace\s*\(|IsNullOrEmpty\s*\()'; Window = 3 }
    'Markup'     = @{ Pattern = '(<asp:HiddenField|Visible="false"|Visible="False"|<%#|CommandName=|CommandArgument=)'; Window = 12 }
    'DynamicSql' = @{ Pattern = '(?i)("\s*(SELECT\s|INSERT\s+INTO\s|UPDATE\s|DELETE\s+FROM\s|FROM\s|WHERE\s|ORDER\s+BY\s|COL_LENGTH\s*\()[^"]*"\s*\+|\+\s*"\s*(SELECT\s|INSERT\s+INTO\s|UPDATE\s|DELETE\s+FROM\s|FROM\s|WHERE\s|ORDER\s+BY\s)[^"]*")'; Window = 12 }
}

# 各面默认扫描范围（相对仓库根的相对路径过滤）
$surfaceScope = @{
    'Branches'   = { param($p) $p -like 'src/*.cs' -and $p -notlike '*.designer.cs' }
    'DataAccess' = { param($p) $p -like 'src/*Db.cs' -or $p -like 'src/**/*Db.cs' }
    'Markup'     = { param($p) $p -like 'src/**/*.aspx' -or $p -like 'src/**/*.ascx' -or $p -like 'src/**/*.master' }
    'DynamicSql' = { param($p) $p -like 'src/*.cs' -and $p -notlike '*.designer.cs' }
}

$surfaces = if ($Surface -eq 'All') { @('Branches', 'DataAccess', 'Markup', 'DynamicSql') } else { @($Surface) }

$tracked = @(git -C $repoRoot ls-files | Where-Object { $_ })
if ($Path) {
    $filters = @($Path -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $tracked = @($tracked | Where-Object { $f = $_; ($filters | Where-Object { $f -like "*$_*" }).Count -gt 0 })
}

$total = 0
foreach ($s in $surfaces) {
    $rule = $surfaceRules[$s]
    $inScope = $surfaceScope[$s]
    $files = @($tracked | Where-Object { & $inScope $_ })

    Write-Host ('=== 面: ' + $s + ' （文件 ' + $files.Count + ' 个，窗口 ' + $rule.Window + ' 行）===')
    $surfaceCount = 0

    foreach ($rel in $files) {
        $full = Join-Path $repoRoot $rel
        if (-not (Test-Path -LiteralPath $full)) { continue }

        $text = [System.IO.File]::ReadAllText($full, $utf8)
        if ($text -notmatch $rule.Pattern) { continue }

        $lines = $text -split "`r?`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -notmatch $rule.Pattern) { continue }
            if (Test-HasNearbyComment -Lines $lines -Index $i -Window $rule.Window) { continue }

            $surfaceCount++
            $total++
            $hint = if (Test-BoundaryHint -Lines $lines -Index $i -Window $rule.Window) { '含边界关键词' } else { '无边界关键词' }
            Write-Host ('  ' + $rel + ':' + ($i + 1) + '  [' + $hint + ']  ' + $lines[$i].Trim())
        }
    }

    if ($surfaceCount -eq 0) { Write-Host '  （无候选）' }
    Write-Host ''
}

Write-Host ('候选合计: ' + $total + ' 处')
Write-Host '[GATE-NOTE] 本脚本只做候选定位，不做达标/缺陷判定 —— 候选 ≠ 缺陷：说明可能已写在方法级 XML 文档里（有效覆盖，不重复补强）；普通英文句也可能被 DynamicSql 的 FROM/WHERE 误匹配。'
Write-Host '[GATE-NOTE] 判定请用人工五层级 rubric（见 W-anp-P113-semantic-sampling-framework.md），本脚本不设 Pass/Fail、不接入 L0。'
