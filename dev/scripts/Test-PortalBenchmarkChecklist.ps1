# W88 对标检查表门禁：把规范 §七 的 7 条检查表固化为可重复运行的检查。
#
# 规范：work-zone/docs/standards/detail-level-benchmark-spec.md《细节级对标规范》（§三 八维度 /
# §四 每维度四要素 / §五 证据形式 / §七 可执行的检查表 7 条 / §九 后续候选 1）。
#
# 三条来自体检的设计决定（体检数据见 W-anp-W88.md）：
#   1. **范围必须限定**：含"来源强度"的文档里混入 C-anp-* 蓝图、W-anp-INDEX 索引、W-anp-W84
#      closeout，它们不是对标产出，纳入只会产出无意义违规（W82 教训：判据要能区分该改与不该改）。
#   2. **规范不溯及既往**：历史文档（P48–P59）声称"一手"却没有 URL —— 它们成文于规范
#      （W-anp-P59，2026-09-26）建立**之前**，按 §五 严格判会全部违规。故只*提示*不*阻断*。
#   3. **不适用声明是合法答案**：规范 §五 允许不适用项入清单。故判定为
#      "四要素 或 不适用声明，二者必有其一"，而不是机械地要求八个维度都填满
#      （P82.1-benchmark 只覆盖重点维度 7，其余走不适用声明，是合规写法）。
#
# 用法：& pwsh -NoProfile -File dev\scripts\Test-PortalBenchmarkChecklist.ps1
# 退出码：0 通过；1 有阻断级违规。

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$plansDir = Join-Path $repoRoot 'work-zone\dev\plans'
$utf8 = [System.Text.UTF8Encoding]::new($false)

$blocking = @()
$advisory = @()

# 受检范围：W-anp-P*.md 中承担对标职责的（含"来源强度"或"对标对象"），
# 排除蓝图/索引/closeout —— 它们引用规范而非产出于规范。
$candidates = Get-ChildItem $plansDir -File -Filter '*.md' | Where-Object {
    $_.Name -like 'W-anp-P*.md' -and $_.Name -notlike '*INDEX*'
}
foreach ($doc in $candidates) {
    $text = [System.IO.File]::ReadAllText($doc.FullName, $utf8)
    if ($text -notmatch '来源强度' -and $text -notmatch '对标对象') { continue }

    $name = $doc.Name
    # 严格模式的范围：规范建立后产出的对标文档（*.1-benchmark.md）
    $packageNumber = 0
    if ($name -match 'W-anp-P(\d+)') { $packageNumber = [int]$Matches[1] }
    # 按包号判定而非文件名：规范成文于 W-anp-P59，故 P60+ 属规范要求期。
    # 早先按"文件名是 .1-benchmark.md"判定，会把 P76/P77 这些规范之后的文档误判成历史文档。
    # 阻断范围再收一层：只限*.1-benchmark.md（明确承担对标产出的文档，6 份全部有 URL）。
    # 汇总/原型/盘点类文档（P76 盘点、P67.1 原型）虽提到"一手"，但对标产出在对应
    # benchmark 文档里，对它们阻断属于误报 —— 误报会让门禁永远红着进而被忽略。
    $isCurrent = ($packageNumber -ge 60) -and ($name -match '\.1-benchmark\.md$')

    $strength = ([regex]::Matches($text, '来源强度')).Count
    $notApplicable = ([regex]::Matches($text, '不适用')).Count
    $url = ([regex]::Matches($text, 'https?://')).Count
    $firstHand = ([regex]::Matches($text, '一手')).Count
    $decision = ([regex]::Matches($text, '决定|采纳|不采纳')).Count

    # R1（阻断）：既无来源强度也无不适用声明 —— 等于没对标
    if ($strength -eq 0 -and $notApplicable -eq 0) {
        $blocking += [pscustomobject]@{ doc = $name; rule = 'R1'; detail = '既无"来源强度"也无"不适用"声明 —— 未产出对标依据（规范 §四/§五）' }
    }

    # R2（仅对规范建立后的文档阻断）：声称一手却无 URL —— 违反 §五"一手来源须 URL + 要点"
    if ($isCurrent -and $firstHand -gt 0 -and $url -eq 0) {
        $blocking += [pscustomobject]@{ doc = $name; rule = 'R2'; detail = '声称"一手"来源但全文无 URL（规范 §五：一手须 URL + 要点）' }
    } elseif (-not $isCurrent -and $firstHand -gt 0 -and $url -eq 0) {
            $advisory += [pscustomobject]@{ doc = $name; rule = 'R2'; detail = ('声称"一手"但无 URL —— ' + $(if ($packageNumber -lt 60) { '成文于规范建立前（规范不溯及既往）' } else { '非 benchmark 文档（对标产出在对应 .1-benchmark.md，需人工核对）' }) + '；规范 §五 要求一手附 URL + 要点') }
    }

    # R3（提示）：缺"决定" —— 规范 §四 要求每个差异都有决定
    if ($decision -eq 0) {
        $advisory += [pscustomobject]@{ doc = $name; rule = 'R3'; detail = '未见"决定/采纳/不采纳"（规范 §四：每个差异都要有决定）' }
    }
}

Write-Host ("[bench] 受检对标文档 {0} 份（范围：W-anp-P*.md 中含来源强度或对标对象者）" -f @($candidates | Where-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName, $utf8)
    $t -match '来源强度' -or $t -match '对标对象'
}).Count)

foreach ($a in $advisory) { Write-Host ("  [提示] [" + $a.rule + "] " + $a.doc + "  " + $a.detail) }

Write-Host ''
if ($blocking.Count -eq 0) {
    Write-Host ("RESULT: Pass  （阻断级违规 0，提示 {0} 条）" -f $advisory.Count)
    exit 0
}
Write-Host ("RESULT: Fail  （阻断级违规 {0} 条）" -f $blocking.Count)
foreach ($b in $blocking) { Write-Host ("  [阻断] [" + $b.rule + "] " + $b.doc + "  " + $b.detail) }
exit 1