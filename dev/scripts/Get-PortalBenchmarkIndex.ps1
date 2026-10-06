# W89 对标证据索引：把"哪份一手来源支撑了哪个包的哪个决定"变成可查询的视图。
#
# 规范 §九 后续候选 2："建立对标证据索引，按功能维度检索"。
#
# 为什么做成**脚本生成**而不是手写一份索引文档：手写索引会过期 —— 这与本 Cycle
# 反复出现的"登记项会过期"是同一类问题（W80 的 113 行乱码、W83 的运行期门禁都已
# 建成却被记成待办）。可靠的是可重复运行的检查，不是一次性登记。
#
# 属性：**inventory 工具**（Get-*），不是门禁（Test-*）—— 它只报告现状，不做阻断，
# 故不接入 Invoke-PortalGateSuite.ps1。
#
#
# 已知局限（实测，如实登记）：
#   1. 来源强度靠 URL 所在行或其上下 3 行内是否出现强度声明来推断。规范模板把强度写在
#      独立一行，但历史文档格式不一，故部分引用标注为未标注（首次运行 24 条中 7 条判为
#      一手、17 条未标注）。索引属 inventory 工具，不影响门禁判定。
#   2. 维度靠向前 40 行内最近的维度名推断，属关键词匹配，存在噪声（安全事实条目偏多）。
#   3. 彻底结构化需各文档统一采用规范的输出模板，属独立项。
#
# 用法：& pwsh -NoProfile -File dev\scripts\Get-PortalBenchmarkIndex.ps1
#       [-OutputJson <路径>] [-OutputMarkdown <路径>]
# 产物：默认写入 work-zone/dev/evidence/benchmark-index/（JSON + Markdown）

param(
    [string] $OutputJson,
    [string] $OutputMarkdown
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$plansDir = Join-Path $repoRoot 'work-zone\dev\plans'
$utf8 = [System.Text.UTF8Encoding]::new($false)

# 维度名取自规范 §三（八项）
$dimensions = @(
    @{ Index = 1; Name = '设计意图' }
    @{ Index = 2; Name = '预期效果' }
    @{ Index = 3; Name = '流程' }
    @{ Index = 4; Name = '交互' }
    @{ Index = 5; Name = '操作体验' }
    @{ Index = 6; Name = '异常与反馈' }
    @{ Index = 7; Name = '可达性' }
    @{ Index = 8; Name = '双语一致性' }
    @{ Index = 9; Name = '安全事实' }
)

# 受检范围与 Test-PortalBenchmarkChecklist.ps1 保持一致：W-anp-P*.md 中承担对标职责者
$documents = Get-ChildItem $plansDir -File -Filter '*.md' | Where-Object {
    $_.Name -like 'W-anp-P*.md' -and $_.Name -notlike '*INDEX*'
}

$entries = @()
foreach ($doc in $documents) {
    $text = [System.IO.File]::ReadAllText($doc.FullName, $utf8)
    if ($text -notmatch '来源强度' -and $text -notmatch '对标对象') { continue }

    $package = ''
    if ($doc.Name -match 'W-anp-P(\d+)') { $package = 'W' + $Matches[1] }

    $lines = $text -split "`r?`n"
    foreach ($line in $lines) {
        foreach ($um in [regex]::Matches($line, 'https?://[^\s）)>\]]+')) {
            $url = $um.Value.TrimEnd('.', ',', ';')
            # 来源强度：先在本行找，找不到就在上下 3 行内找 —— 引用常写成
            # "来源强度：一手" 与 "引用：<URL>" 两行
            $strength = ''
            $sm = if ($line -match '来源强度') { [regex]::Match($line, '(一手|二手|社区|自有)') } else { $null }
            if ($sm -and $sm.Success) { $strength = $sm.Groups[1].Value }
            if (-not $strength) {
                $idx = [array]::IndexOf($lines, $line)
                for ($k = [Math]::Max(0, $idx - 3); $k -le [Math]::Min($lines.Count - 1, $idx + 3); $k++) {
                    $sm2 = if ($lines[$k] -match '来源强度') { [regex]::Match($lines[$k], '(一手|二手|社区|自有)') } else { $null }
                    if ($sm2 -and $sm2.Success) { $strength = $sm2.Groups[1].Value; break }
                }
            }
            # 维度：在该行及其前文最近的维度标题里找
            $dimension = ''
            $idx2 = [array]::IndexOf($lines, $line)
            for ($k = $idx2; $k -ge [Math]::Max(0, $idx2 - 40); $k--) {
                foreach ($d in $dimensions) {
                    if ($lines[$k] -match ('维度\s*' + $d.Index) -or $lines[$k] -match $d.Name) {
                        $dimension = $d.Name
                        break
                    }
                }
                if ($dimension) { break }
            }

            $entries += [pscustomobject]@{
                package = $package
                document = $doc.Name
                dimension = $(if ($dimension) { $dimension } else { '(未标注)' })
                strength = $(if ($strength) { $strength } else { '(未标注)' })
                url = $url
            }
        }
    }
}

# 汇总
$byStrength = $entries | Group-Object -Property strength |
    ForEach-Object { [pscustomobject]@{ strength = $_.Name; count = $_.Count } } | Sort-Object count -Descending
$byDimension = $entries | Group-Object -Property dimension |
    ForEach-Object { [pscustomobject]@{ dimension = $_.Name; count = $_.Count } } | Sort-Object count -Descending

Write-Host ("[bench-index] 对标引用 {0} 条，来自 {1} 份文档" -f $entries.Count, @($entries | Group-Object document).Count)
Write-Host '  按来源强度：'
foreach ($s in $byStrength) { Write-Host ("    {0,-12} {1}" -f $s.strength, $s.count) }
Write-Host '  按维度：'
foreach ($d in $byDimension) { Write-Host ("    {0,-14} {1}" -f $d.dimension, $d.count) }

# 输出目录
if (-not $OutputJson -and -not $OutputMarkdown) {
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmss')
    $dir = Join-Path $repoRoot "work-zone\dev\evidence\benchmark-index\$stamp-Dev"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $OutputJson = Join-Path $dir 'benchmark-index.json'
    $OutputMarkdown = Join-Path $dir 'benchmark-index.md'
} elseif ($OutputJson) {
    $dir = Split-Path -Parent $OutputJson
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
}

if ($OutputJson) {
    ($entries | ConvertTo-Json -Depth 4) | Set-Content -Path $OutputJson -Encoding UTF8
    Write-Host ("[bench-index] JSON  -> " + $OutputJson)
}
if ($OutputMarkdown) {
    $md = @()
    $md += '# 对标证据索引'
    $md += ''
    $md += ('> 由 `dev/scripts/Get-PortalBenchmarkIndex.ps1` 自动生成于 ' + (Get-Date).ToUniversalTime().ToString('o'))
    $md += '> 规范：`work-zone/docs/standards/detail-level-benchmark-spec.md`（§三 八维度 / §四 四要素 / §五 证据形式）'
    $md += ''
    $md += ('共 **{0}** 条对标引用，来自 {1} 份文档。' -f $entries.Count, @($entries | Group-Object document).Count)
    $md += ''
    $md += '## 按维度'
    $md += ''
    $md += '| 维度 | 引用数 |'
    $md += '| --- | --- |'
    foreach ($d in $byDimension) { $md += ('| {0} | {1} |' -f $d.dimension, $d.count) }
    $md += ''
    $md += '## 按来源强度'
    $md += ''
    $md += '| 来源强度 | 引用数 |'
    $md += '| --- | --- |'
    foreach ($s in $byStrength) { $md += ('| {0} | {1} |' -f $s.strength, $s.count) }
    $md += ''
    $md += '## 明细'
    $md += ''
    $md += '| 包 | 文档 | 维度 | 来源强度 | URL |'
    $md += '| --- | --- | --- | --- | --- |'
    foreach ($e in ($entries | Sort-Object package, dimension)) {
        $md += ('| {0} | {1} | {2} | {3} | {4} |' -f $e.package, $e.document, $e.dimension, $e.strength, $e.url)
    }
    [System.IO.File]::WriteAllText($OutputMarkdown, ($md -join "`r`n"), $utf8)
    Write-Host ("[bench-index] Markdown -> " + $OutputMarkdown)
}