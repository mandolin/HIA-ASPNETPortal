<#
.SYNOPSIS
<lang>
  <zh-CN>公开文档内容适用性门禁：已上架的公开文档不得出现「内部资料放哪 / 不随本站发布」一类表述，也不得出现内部工作项编号（`W-anp-P*` / `C-anp-P*`）。</zh-CN>
  <en>Public documentation content applicability gate: published public documents must contain no "where internal material
  lives / not published here" statements, and no internal work-item numbers (`W-anp-P*` / `C-anp-P*`).</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    背景（用户 2026-10-21 指出，备忘 **D11**）：`docs/README.md` 首段末句写着「内部规划与协作资料不随本站发布。」
    ——公开页面向读者交代了项目内部的协作与治理安排，对读者没有信息价值。**原则：公开页面只讲产品与其
    使用/运维，不讲项目内部的计划、协作与治理安排。** 据此排查出 12 个已上架页面上的 30 处同类表述并移除；
    本门禁把它们变成硬断言以防复发，并覆盖同源的**内部编号**类（公开页里作为出处引用的 `W-anp-P*` /
    `C-anp-P*`，共 54 处，同批清理）。

    范围：**只扫已上架的公开文档**——`site/_data/published.yml` 声明的 `doc` 条目，中文取 `docs/<doc>`、
    英文取 `site/_translations/<doc>`。**未上架文档不在范围**：内部治理件（如 `documentation-registry.md`）
    本就是内部件，含内部编号是应该的，把它们纳入反而会让"内部件"与"公开页"的边界再次模糊。

    两类断言（命中即失败，列出文件行号与片段）：
      A1 内部向表述：
         中文：内部规划 / 协作资料 / 内部治理 / 内部计划 / 内部对标 / 私有仓库 / 私有记录 /
               不随公开文档发布 / 不随本站发布 / 内部计划，未公开
         英文：internal planning / internal governance / separate private repository / private records /
               not published with the public documentation / internal evidence directory by default
      A2 内部编号：`(W|C)-anp-P\d+`——内部工作项与周期编号是项目内部组织方式，读者无法凭它定位任何东西。

    刻意放行（写在门禁里，而不是靠自觉）：
      - `<internal evidence directory>`：它是**可操作命令里的占位符**（读者照着执行时需要一个路径位置），
        且不含「不发布」声明；删掉会让命令不可执行。
      - `dev/scripts/` 脚本路径：产品自带的工具路径，属于公开可用的操作入口（A1 不因它放宽，只是不把
        路径本身当作内部向表述）。
  </zh-CN>
  <en>
    Background (raised by the user on 2026-10-21, memo **D11**): the last sentence of `docs/README.md`'s first
    paragraph read "Internal planning and collaboration material are not published with this site." — a public page
    explaining the project's internal collaboration and governance arrangements to readers, which carries no
    information for them. **Principle: a public page talks about the product and its use/operation, not about the
    project's internal planning, collaboration or governance.** The review found 30 such statements across 12
    published pages and removed them; this gate turns them into hard assertions so they cannot return, and extends to
    the same-rooted **internal numbering** class (54 occurrences of `W-anp-P*` / `C-anp-P*` cited as provenance in
    public pages, cleaned in the same batch).

    Scope: **published public documents only** — the `doc` entries declared in `site/_data/published.yml`, with the
    Chinese text at `docs/<doc>` and the English render at `site/_translations/<doc>`. **Unpublished documents are out
    of scope**: internal governance artifacts (such as `documentation-registry.md`) are internal by design, and
    pulling them in would blur the boundary between "internal artifact" and "public page" all over again.

    Two assertion families (a hit fails the gate and lists file, line and snippet):
      A1 internal-facing wording:
         Chinese: 内部规划 / 协作资料 / 内部治理 / 内部计划 / 内部对标 / 私有仓库 / 私有记录 /
                  不随公开文档发布 / 不随本站发布 / 内部计划，未公开
         English: internal planning / internal governance / separate private repository / private records /
                  not published with the public documentation / internal evidence directory by default
      A2 internal numbering: `(W|C)-anp-P\d+` — internal work-item and cycle numbers are the project's internal
         organisation; a reader cannot locate anything by them.

    Deliberate allowances (encoded in the gate, not left to memory):
      - `<internal evidence directory>`: a **placeholder inside an executable command** (readers need a path position
        to run it), carrying no "not published" statement; removing it would make the command unrunnable.
      - `dev/scripts/` script paths: the product's own tool paths, publicly usable entry points (A1 is not relaxed
        because of them; the path itself simply is not treated as internal-facing).
  </en>
</lang>
.Proof boundary（证明边界）
<lang>
  <zh-CN>
    只证明"已上架公开文档里**不存在**这两类模式"。**不证明**公开文档的叙述正确、完整或对读者真的有用
    （那些由人工复核承载）；**不检查**未上架文档（内部治理件不在范围内）；**不做语义判断**——模式命中
    即失败，所以"为了讲产品而不得不提到内部概念"的正常句子若命中模式也会被判失败。这是**有意的保守
    口径**：宁可让人看一眼再放行，也不让内部表述悄悄回到公开页。
  </zh-CN>
  <en>
    Proves only that these two families of patterns are **absent** from published public documents. It does **not**
    prove the prose is correct, complete or genuinely useful to readers (human review carries that); it does **not**
    check unpublished documents (internal governance artifacts are out of scope); it makes **no semantic judgement** —
    a pattern hit fails the gate, so even a legitimate sentence that must mention an internal concept is rejected if it
    matches. This is a **deliberately conservative convention**: better to make a human look once and allow it than to
    let internal wording creep back onto public pages.
  </en>
</lang>
#>
param(
    [string] $ManifestPath = '',
    [string] $DocsRoot = '',
    [string] $TranslationsRoot = ''
)

$ErrorActionPreference = 'Stop'
# `dev/scripts` → 上一级是 `dev`，上上级是仓库根
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($ManifestPath)) { $ManifestPath = Join-Path $repoRoot 'site/_data/published.yml' }
if ([string]::IsNullOrWhiteSpace($DocsRoot)) { $DocsRoot = Join-Path $repoRoot 'docs' }
if ([string]::IsNullOrWhiteSpace($TranslationsRoot)) { $TranslationsRoot = Join-Path $repoRoot 'site/_translations' }

$A1_ZH = @('内部规划', '协作资料', '内部治理', '内部计划', '内部对标', '私有仓库', '私有记录', '不随公开文档发布', '不随本站发布', '内部计划，未公开')
$A1_EN = @('internal planning', 'internal governance', 'separate private repository', 'private records', 'not published with the public documentation', 'internal evidence directory by default')
# 刻意放行：命令里的路径占位符（读者执行时需要一个位置），不含「不发布」声明
$ALLOW = @('<internal evidence directory>')

$manifest = [System.IO.File]::ReadAllText($ManifestPath)
$docs = @([regex]::Matches($manifest, '(?m)^\s*-\s*doc:\s*(.+?)\s*$') | ForEach-Object { $_.Groups[1].Value })
$all = @()
$scanned = 0

foreach ($doc in $docs) {
    foreach ($kv in @(@{ p = (Join-Path $DocsRoot $doc); lang = 'zh' }, @{ p = (Join-Path $TranslationsRoot $doc); lang = 'en' })) {
        if (-not (Test-Path -LiteralPath $kv.p -PathType Leaf)) { continue }
        $scanned++
        $rel = $kv.p.Substring($repoRoot.Length + 1)
        $lines = [System.IO.File]::ReadAllText($kv.p) -split "`r?`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $skip = $false
            foreach ($a in $ALLOW) { if ($line.Contains($a)) { $skip = $true; break } }
            if ($skip) { continue }
            # A1 内部向表述
            foreach ($pat in $(if ($kv.lang -eq 'zh') { $A1_ZH } else { $A1_EN })) {
                if ($line.Contains($pat)) {
                    $snippet = $line.Trim(); if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0, 120) + '…' }
                    $all += ([pscustomobject]@{ kind = 'A1'; file = $rel; line = $i + 1; text = $snippet })
                    break
                }
            }
            # A2 内部编号
            $m = [regex]::Match($line, '(W|C)-anp-P\d+(\.\d+)?')
            if ($m.Success) {
                $snippet = $line.Trim(); if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0, 120) + '…' }
                $all += ([pscustomobject]@{ kind = 'A2'; file = $rel; line = $i + 1; text = $snippet })
            }
        }
    }
}

Write-Host ('[INFO] 公开文档内容适用性：已扫描已上架文档文件 ' + $scanned + ' 份（源自 ' + $docs.Count + ' 条上架声明）')
if ($all.Count -eq 0) {
    Write-Host '[PASS] 公开文档内容适用性：已上架公开文档无内部向表述、无内部工作项编号。'
    exit 0
}
foreach ($f in $all) {
    Write-Host ('  [' + $f.kind + '] ' + $f.file + ':L' + $f.line + ' — ' + $f.text)
}
Write-Host ('[FAIL] 公开文档内容适用性门禁失败：' + $all.Count + ' 处（A1 ' + @($all | Where-Object { $_.kind -eq 'A1' }).Count + ' 处、A2 ' + @($all | Where-Object { $_.kind -eq 'A2' }).Count + ' 处）。')
exit 1