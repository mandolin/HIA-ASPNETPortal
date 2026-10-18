#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>i18n 内容级一致性门禁（L0）：跨语言"值"的结构对齐（占位符 / 花括号），超越既有"成对存在"检查。</zh-CN>
  <en>i18n content-level parity gate (L0): structural alignment of cross-language "values" (placeholders / braces), going beyond the existing paired-existence checks.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P27`（`W-anp-P146`）交付：既有资源契约门禁（`Test-PortalResourceContract.ps1`）证明到"键存在 + en-us 逐字等于中性"，
  但**值的内部结构**从未被跨语言比对——翻译丢一个 `{0}` 就是运行期 FormatException 或错位输出，丢标签就是布局破坏，全部静默。
  本门禁把值级结构对齐做成硬断言（阈值先实测：939 键 × 三份 resx，占位符不一致 0、花括号不平衡 0、全角大括号 0、含标签键 0）。

  硬断言：
  A1 占位符一致：每个共享键的 `{n}` 占位符集合在 neutral / zh-cn（及 en-us，逐字不变量下应与中性相同，仍独立比对自证）之间完全一致；
  A2 花括号健全：三份 resx 的每个值，去除 `{{`/`}}` 转义后 `{` 与 `}` 数量必须相等（不平衡 = String.Format 运行期抛异常），
     且不得出现全角大括号 `｛｝`（翻译排版典型静默错误）。

  软指标（上报不阻断）：
  S1 长度比离群（>8x 且双长 ≥16——4x 已实测为中文信息密度噪声，121 处全是 "Submit/提交" 类正常现象，故收紧到 8x）；
  S2 zh 值与中性长串相同（≥12 字符，未翻译候选）；
  S3 占位符断档（{0},{2} 缺 {1}——String.Format 合法但可疑）；
  S4 含标签的键数（实测 0——今天为空覆盖，若未来资源值引入标签，应同时增设标签一致性硬断言，否则即"通过但什么都没断言"）。

  证明边界：只证明声明值的**结构**对齐；不证明语义正确性（翻译质量由人工双语抽样承载，见 P111 口径）、
  不证明运行期文化切换渲染（L2 的多语言运行期证据门禁已覆盖）、en-us 值内容由 C4 逐字不变量承载（本门禁独立比对以自证）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalI18nContentParity.ps1'
  产物：temp\i18n-parity\i18n-parity-stats.json（每次刷新）。
  退出码：0 硬断言全过；1 任一硬断言失败。
  有效性验证（注入法，沿用 W83/W87/P112 惯例）：zh-cn 值丢 {0} → A1 应 Fail；注入孤立 `{` 与全角 `｛` → A2 应 Fail（两处）；还原 → Pass。</zh-CN>
  <en>`C-anp-P27` (`W-anp-P146`) deliverable: the existing resource-contract gate (`Test-PortalResourceContract.ps1`) proves
  "keys exist + en-us verbatim equal to neutral", but the **internal structure of values** was never compared across languages —
  a translation dropping `{0}` is a runtime FormatException or misaligned output, a dropped tag breaks layout, all silently.
  This gate turns value-level structural alignment into hard assertions (thresholds measured first: 939 keys x three resx,
  placeholder mismatches 0, brace imbalance 0, full-width braces 0, tag-bearing keys 0).

  Hard assertions:
  A1 placeholder parity: each shared key's `{n}` set must be identical across neutral / zh-cn (and en-us — under the verbatim
     invariant it equals neutral, still compared independently as self-verification);
  A2 brace sanity: in every value of the three resx files, after removing `{{`/`}}` escapes, `{` and `}` counts must be equal
     (imbalance = String.Format throws at runtime), and full-width braces `｛｝` are banned (a classic silent translation typo).

  Soft metrics (report only):
  S1 length-ratio outliers (>8x with both lengths >=16 — 4x was measured to be Chinese information-density noise, 121 hits all
     "Submit/提交"-like normal cases, hence tightened to 8x);
  S2 zh values identical to long neutral strings (>=12 chars, untranslated candidates);
  S3 placeholder gaps ({0},{2} missing {1} — legal for String.Format but suspicious);
  S4 tag-bearing key count (measured 0 — vacuous today; if tags ever enter resource values, a tag-parity hard assertion must be
     added alongside, otherwise this becomes "passing while asserting nothing").

  Proof boundary: proves only **structural** alignment of declared values; not semantic correctness (translation quality is
  carried by human bilingual sampling, per the P111 convention), not runtime culture-switch rendering (the L2 localized runtime
  evidence gate covers it); en-us value content is carried by the C4 verbatim invariant (compared independently here as self-verification).

  Usage (repository root): & 'dev\scripts\Test-PortalI18nContentParity.ps1'
  Output: temp\i18n-parity\i18n-parity-stats.json (refreshed per run).
  Exit codes: 0 all hard assertions pass; 1 any hard assertion fails.
  Effectiveness proof (injection, per the W83/W87/P112 convention): dropping {0} from a zh-cn value → A1 must Fail; injecting a
  lone `{` plus full-width `｛` → A2 must Fail (two findings); restoring → Pass.</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P146` 交付并接入 L0（35→36）。语义正确性人工抽样与运行期文化切换归既有口径承载。</zh-CN>
  <en>Provenance: delivered by `W-anp-P146` and wired into L0 (35→36). Semantic sampling and runtime culture switching remain with the existing conventions.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath = ''
)

$ErrorActionPreference = 'Stop'

# <lang><zh-CN>仓库根 = 本脚本目录（dev\scripts）上两级。</zh-CN>
# <en>Repository root = two levels up from this script's directory (dev\scripts).</en></lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$resxDir = Join-Path $repoRoot 'src\Portal\App_GlobalResources'

function Get-ResxMap([string] $path) {
    $xml = [xml]([System.IO.File]::ReadAllText($path))
    $map = @{}
    foreach ($data in $xml.root.data) { if ($data.name) { $map[[string]$data.name] = [string]$data.value } }
    return $map
}

function Get-Placeholders([string] $value) {
    # <lang><zh-CN>格式占位符 {n} 集合（跳过 {{ 转义、含对齐/格式后缀如 {0:N1}）。</zh-CN>
    # <en>The {n} placeholder set (skipping {{ escapes, including alignment/format suffixes like {0:N1}).</en></lang>
    return @([regex]::Matches($value, '(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})') | ForEach-Object { [int]$_.Groups[1].Value } | Sort-Object -Unique)
}

$neutral = Get-ResxMap (Join-Path $resxDir 'lang.resx')
$zh = Get-ResxMap (Join-Path $resxDir 'lang.zh-cn.resx')
$en = Get-ResxMap (Join-Path $resxDir 'lang.en-us.resx')

$failures = @()
$soft = [ordered]@{
    lengthRatioOutliers = @()
    zhIdenticalToNeutral = @()
    placeholderGaps = @()
    tagBearingKeys = 0
}
$placeholderKeys = 0

# ---------------------------------------------------------------------------
# A1：占位符一致（neutral ↔ zh-cn ↔ en-us）
# ---------------------------------------------------------------------------
foreach ($key in $neutral.Keys) {
    $np = Get-Placeholders ([string]$neutral[$key])
    if ($np.Count -gt 0) { $placeholderKeys++ }
    foreach ($pair in @(@('zh-cn', $zh), @('en-us', $en))) {
        $label = $pair[0]; $map = $pair[1]
        if (-not $map.ContainsKey($key)) { continue }
        $lp = Get-Placeholders ([string]$map[$key])
        $a = $np -join ','; $b = $lp -join ','
        if ($a -ne $b) {
            # <lang><zh-CN>注意：`if` 是语句不是表达式，不能内联进参数表（运行期报 "'if' is not recognized"）——先赋值再插值。</zh-CN>
            # <en>Note: `if` is a statement, not an expression — it cannot be inlined into an argument list (runtime "'if' is not
            # recognized"); assign first, then interpolate.</en></lang>
            $localeLabel = if ($label -eq 'zh-cn') { '简体中文' } else { 'en-us' }
            $failures += ("A1 [{0}] {1} 占位符不一致：neutral[{2}] vs {3}[{4}]" -f $key, $localeLabel, $a, $label, $b)
        }
    }
    # <lang><zh-CN>S3 软指标：占位符断档（String.Format 合法但通常意味着笔误）。</zh-CN>
    # <en>S3 soft metric: placeholder gaps (legal for String.Format but usually a typo).</en></lang>
    for ($i = 0; $i -lt $np.Count; $i++) {
        if ($np[$i] -ne $i) { $soft.placeholderGaps += ("{0}: [{1}]" -f $key, ($np -join ',')); break }
    }
}

# ---------------------------------------------------------------------------
# A2：花括号健全（三份 resx 全体值）
# ---------------------------------------------------------------------------
foreach ($mapPair in @(@('neutral', $neutral), @('zh-cn', $zh), @('en-us', $en))) {
    $label = $mapPair[0]; $map = $mapPair[1]
    foreach ($key in $map.Keys) {
        $v = [string]$map[$key]
        if (-not $v) { continue }
        # <lang><zh-CN>去 {{ }} 转义后计数：不平衡 = String.Format 运行期抛 FormatException（每次取键即触发）。</zh-CN>
        # <en>Count after removing {{ }} escapes: imbalance = String.Format throws a FormatException at runtime (on every key fetch).</en></lang>
        $stripped = $v -replace '\{\{', '' -replace '\}\}', ''
        $open = ([regex]::Matches($stripped, '\{')).Count
        $close = ([regex]::Matches($stripped, '\}')).Count
        if ($open -ne $close) {
            $failures += ("A2 [{0}/{1}] 花括号不平衡（{{={2} }}={3}，String.Format 运行期将抛异常）：{4}" -f $label, $key, $open, $close, $v.Substring(0, [Math]::Min(60, $v.Length)))
        }
        if ($v -match '[｛｝]') {
            $failures += ("A2 [{0}/{1}] 出现全角大括号 ｛｝（String.Format 不识别，静默输出错误文案）" -f $label, $key)
        }
        if ($v -match '<[a-zA-Z/][^>]*>') { $soft.tagBearingKeys++ }
    }
}

# ---------------------------------------------------------------------------
# 软指标 S1 / S2
# ---------------------------------------------------------------------------
foreach ($key in $neutral.Keys) {
    $nv = [string]$neutral[$key]; $zv = [string]$zh[$key]
    if (-not $zv -or -not $nv) { continue }
    if ($nv.Length -ge 16 -and $zv.Length -ge 16) {
        $ratio = $nv.Length / [double]$zv.Length
        if ($ratio -gt 8 -or $ratio -lt 0.125) { $soft.lengthRatioOutliers += ("{0}: {1}/{2}={3:N1}" -f $key, $nv.Length, $zv.Length, $ratio) }
    }
    if ($nv -match '[\u4e00-\u9fff]' -and $zv -eq $nv -and $nv.Length -ge 12) { $soft.zhIdenticalToNeutral += $key }
}

# ---------------------------------------------------------------------------
# 统计落盘 + 输出
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $StatsPath = Join-Path (Join-Path $repoRoot 'temp') 'i18n-parity\i18n-parity-stats.json'
}
$statsDir = Split-Path -Parent $StatsPath
if (-not (Test-Path -LiteralPath $statsDir)) { New-Item -ItemType Directory -Force -Path $statsDir | Out-Null }
$report = [pscustomobject]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    keyCounts = [pscustomobject]@{ neutral = $neutral.Count; zhCn = $zh.Count; enUs = $en.Count; placeholderKeys = $placeholderKeys }
    soft = $soft
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== i18n 内容级一致性（硬断言 + 软指标） ==='
Write-Host ("键数 {0}（neutral/zh-cn/en-us {1}/{2}/{3}）｜含占位符键 {4}" -f $neutral.Count, $neutral.Count, $zh.Count, $en.Count, $placeholderKeys)
Write-Host ("软指标：长度比离群 {0} ｜zh 长串未翻译候选 {1} ｜占位符断档 {2} ｜含标签键 {3}" -f @($soft.lengthRatioOutliers).Count, @($soft.zhIdenticalToNeutral).Count, @($soft.placeholderGaps).Count, $soft.tagBearingKeys)
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host ("[FAIL] i18n 内容级硬断言失败 {0} 处：" -f $failures.Count)
    foreach ($f in $failures) { Write-Host ("  - " + $f) }
    exit 1
}

Write-Host ''
Write-Host '[PASS] 占位符一致、花括号健全（A1/A2）。软指标见统计文件。'
exit 0
