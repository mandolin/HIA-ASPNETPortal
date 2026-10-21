<#
.SYNOPSIS
<lang>
  <zh-CN>**镜像新鲜度门禁**（`C-anp-P34` / `W-anp-P192`，解备忘 **D10**）：仓库内 `site/` 镜像页必须与"按当前 `docs/` 重新构建的产物"**逐字节一致**。做法是把构建输出重定向到临时目录（`Build-PortalSite.ps1 -OutputRoot`），再与 `site/` 比对——**"什么算一致"由构建自身回答**，门禁不需要复述哪些变换被允许。</zh-CN>
  <en>**Mirror freshness gate** (`C-anp-P34` / `W-anp-P192`, resolves memo **D10**): the repository's `site/` mirror pages must be **byte-identical** to "what a rebuild from the current `docs/` produces". It redirects the build output into a temp directory (`Build-PortalSite.ps1 -OutputRoot`) and compares it with `site/` — **the build itself defines what counts as identical**, so the gate need not restate which transformations are allowed.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    背景（D10 的真实形态）：`W-anp-P181` 改了 `docs/` 却只提交 `docs/` 与 `_translations/`、**没提交构建产物** → 仓库内 `site/` 副本陈旧。当时靠**人工提交时 `git status` 发现**（"该守的位置没守住"）；**线上不受影响**（发布工作流会重跑构建），属仓库内副本陈旧。本门禁把这件事变成机器断言。

    **为什么与"真实构建输出"比字节，而不是与源文档比**：构建对文档有若干**合法变换**——`Convert-DocLink` 链接改写、`Convert-PublicText` 发布面文本过滤、front matter 注入、路径与文件名变化。与源文档比字节既不成立（必然不等），也会制造假失败。因此本门禁**不复述变换清单**，而是**跑一遍真实构建**（输出到临时目录）再逐字节比对。这也意味着：**将来构建新增合法变换时，本门禁无需改动**——它比对的是"构建说什么"，不是"我以为构建会说什么"。

    比对范围：`published.yml` 声明的每个 `path` 下的 `index.md` 与 `en/index.md`（中英各一份），外加**文件集合比对**（多出 / 缺失均报）。

    刻意不做：不渲染 Jekyll（镜像页是 markdown，渲染由 Pages 负责）；不检查手写页面（`site/index.md` 等不走生成流程，由镜像一致性门禁的形状检查承载）；不在临时目录上跑 N1/N2（构建的 `-OutputRoot` 模式本身就会跳过——否则会把"手写页面不在这里"误判成缺口）。
  </zh-CN>
  <en>
    Background (the real shape of D10): `W-anp-P181` changed `docs/` but committed only `docs/` and `_translations/`, **not the build output** → the repository's `site/` copy went stale. It was caught by a **human at commit time via `git status`** ("the spot that should have been guarded was not"); **the live site was unaffected** (the publish workflow rebuilds), so this was a stale in-repository copy. This gate turns it into a machine assertion.

    **Why compare against the real build output rather than the source documents**: the build applies several **legitimate transformations** — `Convert-DocLink` link rewriting, `Convert-PublicText` publication-surface filtering, front-matter injection, and path/filename changes. Comparing bytes against the source documents is both impossible (they can never match) and a false-failure generator. So this gate **does not restate the transformation list**; it **runs a real build** (into a temp directory) and compares byte-for-byte. This also means: **when the build gains new legitimate transformations, this gate needs no change** — it compares against "what the build says", not "what I assume the build would say".

    Comparison scope: for every `path` declared in `published.yml`, the `index.md` and `en/index.md` under it (one per language), plus a **file-set comparison** (extra or missing files are both reported).

    Deliberately out of scope: no Jekyll rendering (mirror pages are markdown; Pages renders them); no hand-written pages (`site/index.md` and friends bypass generation — their shape is covered by the mirror consistency gate); no N1/N2 over the temp directory (the build's `-OutputRoot` mode skips them itself — otherwise "hand-written pages are not here" would be misreported as a gap).
  </en>
</lang>

.Proof boundary（证明边界）
<lang>
  <zh-CN>
    只证明"仓库内 `site/` 的生成镜像与一次真实构建的输出**逐字节一致**（含文件集合）"。
    **不证明**渲染后的页面正确（那是发布后线上复核门禁的事）；**不检查**手写页面与 `_config.yml` 等非生成内容；
    **不验证** Jekyll 主题/插件的版本影响（本机未运行 Jekyll）；**不覆盖**未在 `published.yml` 声明的文档。
    本门禁**依赖构建脚本可运行**：构建本身失败（退出码非 0）时本门禁报 FAIL 并附构建输出，而不是猜。
  </zh-CN>
  <en>
    Proves only that "the generated mirrors in the repository's `site/` are **byte-identical** to one real build's output
    (including the file set)". It does **not** prove the rendered pages are correct (that is the post-publish live re-check
    gate's job); it does **not** check hand-written pages or non-generated content such as `_config.yml`; it does **not**
    verify Jekyll theme/plugin version effects (Jekyll is not run locally); it does **not** cover documents not declared in
    `published.yml`. This gate **depends on the build script being runnable**: if the build itself fails (non-zero exit) this
    gate reports FAIL with the build output rather than guessing.
  </en>
</lang>
#>
param()

$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$siteDir = Join-Path $repoRoot 'site'
$build = Join-Path $repoRoot 'dev/scripts/Build-PortalSite.ps1'
$publishedYml = Join-Path $siteDir '_data/published.yml'
$stamp = Get-Date -Format 'yyyyMMddHHmmss'
$tempOut = Join-Path $repoRoot "temp/mirror-freshness-$stamp"

$failures = New-Object 'System.Collections.Generic.List[string]'

function Get-FirstDifference {
    param([string] $A, [string] $B)
    $la = $A -split "`r?`n"
    $lb = $B -split "`r?`n"
    $n = [Math]::Min($la.Count, $lb.Count)
    for ($i = 0; $i -lt $n; $i++) {
        if ($la[$i] -ne $lb[$i]) { return ('第 ' + ($i + 1) + ' 行：仓库内「' + $la[$i].Trim() + '」 vs 构建产物「' + $lb[$i].Trim() + '」') }
    }
    if ($la.Count -ne $lb.Count) { return ('行数不同：仓库内 ' + $la.Count + ' 行 vs 构建产物 ' + $lb.Count + ' 行') }
    return '内容不同（未定位到首处差异）'
}

# ---------- 1) 跑一遍真实构建，输出到临时目录 ----------
if (Test-Path -LiteralPath $tempOut) { Remove-Item -Recurse -Force $tempOut }
$buildOut = & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File $build -OutputRoot $tempOut 2>&1
$buildCode = $LASTEXITCODE
if ($buildCode -ne 0) {
    Write-Host '[FAIL] 镜像新鲜度：构建本身失败（退出码 ' + $buildCode + '），无法比对。构建输出末尾：'
    ($buildOut | ForEach-Object { $_.ToString() } | Select-Object -Last 6) | ForEach-Object { Write-Host ('  ' + $_) }
    if (Test-Path -LiteralPath $tempOut) { Remove-Item -Recurse -Force $tempOut -ErrorAction SilentlyContinue }
    exit 1
}
Write-Host ('[INFO] 镜像新鲜度：已把构建输出导入临时目录 ' + ($tempOut.Substring($repoRoot.Length + 1)) + '，开始逐字节比对。')

# ---------- 2) 逐 path 比对 ----------
$y = [System.IO.File]::ReadAllText($publishedYml, $utf8)
$paths = @([regex]::Matches($y, '(?m)^\s*path:\s*(.+?)\s*$') | ForEach-Object { $_.Groups[1].Value.Trim() })
$compared = 0
foreach ($p in $paths) {
    $relDir = $p.Trim('/')
    $sitePathDir = Join-Path $siteDir ($relDir -replace '/', '\')
    $tempPathDir = Join-Path $tempOut ($relDir -replace '/', '\')

    # 2a 文件集合比对（多出 / 缺失）
    $siteFiles = @()
    if (Test-Path -LiteralPath $sitePathDir) {
        $siteFiles = @(Get-ChildItem -LiteralPath $sitePathDir -Recurse -File | ForEach-Object { $_.FullName.Substring($sitePathDir.Length + 1) })
    }
    $tempFiles = @()
    if (Test-Path -LiteralPath $tempPathDir) {
        $tempFiles = @(Get-ChildItem -LiteralPath $tempPathDir -Recurse -File | ForEach-Object { $_.FullName.Substring($tempPathDir.Length + 1) })
    }
    foreach ($f in $tempFiles) {
        if ($siteFiles -notcontains $f) { $failures.Add(('镜像缺失文件：site/' + $relDir + '/' + $f + '（构建会生成它，仓库内没有）')) }
    }
    foreach ($f in $siteFiles) {
        if ($tempFiles -notcontains $f) { $failures.Add(('仓库内多出文件：site/' + $relDir + '/' + $f + '（本次构建不生成它，属陈旧残留或手改）')) }
    }

    # 2b 逐字节比对（共同文件）
    foreach ($f in $tempFiles) {
        if ($siteFiles -notcontains $f) { continue }
        $siteFile = Join-Path $sitePathDir $f
        $tempFile = Join-Path $tempPathDir $f
        $a = [System.IO.File]::ReadAllText($siteFile, $utf8)
        $b = [System.IO.File]::ReadAllText($tempFile, $utf8)
        $compared++
        if ($a -ne $b) { $failures.Add(('镜像内容不一致：site/' + $relDir + '/' + $f + ' — ' + (Get-FirstDifference -A $a -B $b))) }
    }
}

if (Test-Path -LiteralPath $tempOut) { Remove-Item -Recurse -Force $tempOut -ErrorAction SilentlyContinue }

if ($failures.Count -gt 0) {
    Write-Host ('[FAIL] 镜像新鲜度门禁失败：' + $failures.Count + ' 处（仓库内 site/ 与一次真实构建不一致）。')
    foreach ($f in $failures | Select-Object -First 30) { Write-Host ('  - ' + $f) }
    if ($failures.Count -gt 30) { Write-Host ('  ……另有 ' + ($failures.Count - 30) + ' 处未列出') }
    Write-Host '  处置：在仓库根执行 `pwsh -File dev/scripts/Build-PortalSite.ps1` 重新生成，并把 site/ 的改动一并提交。'
    exit 1
}
Write-Host ('[PASS] 镜像新鲜度：已比对 ' + $compared + ' 个镜像文件（' + $paths.Count + ' 个已上架路径 × 中英），仓库内 site/ 与一次真实构建逐字节一致。')
exit 0
