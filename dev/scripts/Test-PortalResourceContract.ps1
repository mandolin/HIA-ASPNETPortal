<#
<lang>
  <zh-CN>
    资源契约一致性门禁（W-anp-P80 `P80.4`）：把「三份 resx + 签入的强类型 designer + 标记层/代码层引用」
    这四处之间的契约做成**可重复运行、可门禁**的检查，而不是靠人记。

    为什么需要它（W80 勘察实测得出，不是推断）：
      1. `W76` 登记的「lang.designer.cs 113 行乱码」其实**已在 W78 期间被 2a2901e 闭合**，
         而台账没同步 —— 说明「登记项」会过期，**唯一可靠的是可重复运行的检查**。
      2. 勘察发现 `lang.designer.cs` 强类型属性 860 个 vs resx 键 876 个 → **16 个键缺属性**；
         这些键**全部在用**，且使用方式是**绕过编译期检查**的两种：
         标记层 `<%$ Resources:lang, X %>` 与代码层 `GetResource("字面量")`。
      3. 一手依据（Microsoft Learn，见 W-anp-P80.1-benchmark.md）：资源文件找到但键不存在时
         **返回 null 而不抛异常** —— 键名漂移是**静默失败**，界面显示空文案、日志无痕迹，
         所以「键存在性」必须由门禁兜底，不能指望异常机制。

    六项检查：
      C1 键集一致：zh-CN 与中性双向一致；en-us 是中性子集（不得有多余键）。
      C2 属性对应：designer 强类型属性与中性 resx 键**双向一一对应**。
      C3 双语块配对：每个属性前的 `<summary>` 必须含 `<lang>`（含 zh-CN 与 en 两侧）。
      C4 en-us 不漂移：en-us 显式条目的值**必须与中性值逐字相同**（W80 `D1` 裁定 (a) 的不变量）。
                      允许 `-AllowedEnUsOverride` 显式登记刻意的覆盖，默认无。
      C5 中性基准洁净：中性 resx 是英文基准，值**不得含 CJK**（污染会导致英文界面显示中文）。
      C6 引用存在性：标记层 `<%$ Resources:lang, X %>` 与代码层 `GetResource("X")` 引用的键必须在 resx 中存在。
                     （强类型 `lang.X` 由编译期保证，不重复检查。）

    用法（在仓库根目录执行）：
      & $pwsh 'dev\scripts\Test-PortalResourceContract.ps1'
      & $pwsh 'dev\scripts\Test-PortalResourceContract.ps1' -WhatIf     # 只报告不失败
    退出码：有违规为 1，否则 0。

    边界：本脚本只读解析 `.resx` / `.designer.cs` / 源码文本，**不连接数据库、不启动 IIS、不写生产配置**。
  </zh-CN>
  <en>
    Resource-contract consistency gate (W-anp-P80 `P80.4`): turns the contract between the three resx files,
    the checked-in strongly typed designer, and markup/code references into a **repeatable, gateable check**
    instead of something developers must remember.

    Why it exists (measured in the W80 survey, not inferred):
      1. the "113 garbled lines in lang.designer.cs" recorded by `W76` had **already been fixed during W78**
         by commit `2a2901e`, yet the ledger was never updated — registration items go stale, and
         **the only reliable source of truth is a repeatable check**.
      2. the survey found 860 strongly typed properties against 876 resx keys → **16 keys lack properties**;
         all of them are in use, through two forms that **bypass compile-time checking**:
         markup `<%$ Resources:lang, X %>` and code `GetResource("literal")`.
      3. first-hand source (Microsoft Learn, see W-anp-P80.1-benchmark.md): when the resource file is found but
         the key is absent, the request **returns null rather than throwing** — key drift is a **silent
         failure** (blank UI text, no log entry), so key existence must be enforced by a gate.

    Six checks:
      C1 key-set consistency: zh-CN and neutral match in both directions; en-us is a subset of neutral (no extra keys).
      C2 property mapping: designer strongly typed properties and neutral resx keys correspond **one-to-one, both ways**.
      C3 locale-block pairing: the `<summary>` before every property must contain `<lang>` (with both zh-CN and en).
      C4 no en-us drift: the value of every explicit en-us entry **must equal the neutral value verbatim**
          (the invariant chosen by W80 `D1` option (a)). Deliberate overrides can be registered explicitly
          through `-AllowedEnUsOverride`; the default is none.
      C5 clean neutral baseline: the neutral resx is the English baseline, so values **must not contain CJK**
          (contamination makes the English UI show Chinese).
      C6 reference existence: keys referenced by markup `<%$ Resources:lang, X %>` and by code `GetResource("X")`
          must exist in the resx. (Strongly typed `lang.X` is guaranteed by the compiler and is not re-checked.)

    Usage (run from the repository root):
      & $pwsh 'dev\scripts\Test-PortalResourceContract.ps1'
      & $pwsh 'dev\scripts\Test-PortalResourceContract.ps1' -WhatIf     # report without failing
    Exit code: 1 when there are violations, otherwise 0.

    Boundary: this script only parses `.resx` / `.designer.cs` / source text — it does not connect to a database,
    start IIS, or write production configuration.
  </en>
</lang>
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    # <lang><zh-CN>刻意的 en-us 覆盖白名单：这些键允许 en-us 值与中性不同（需写明理由）。</zh-CN><en>Allow-list of deliberate en-us overrides: these keys may differ from the neutral value (give a reason).</en></lang>
    [string[]]$AllowedEnUsOverride = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>脚本位于 `dev/scripts/`，仓库根在其上**两级**。用 `GetFullPath` 而不是链式 `Join-Path`：
#   `Join-Path` 的第二个参数带多个路径段时行为不直观，且首轮正是因此解析成了 `dev/` 而报 resx 找不到。</zh-CN>
#   <en>The script lives in `dev/scripts/`, so the repository root is **two levels up**. `GetFullPath` is used
#   instead of chained `Join-Path` because the latter behaves unintuitively when its second argument contains
#   multiple path segments — that is exactly why the first run resolved to `dev/` and reported a missing resx.</en>
# </lang>
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))   # repo root
$resDir = Join-Path $root 'src/Portal/App_GlobalResources'
$designerPath = Join-Path $resDir 'lang.designer.cs'
$portalRoot = Join-Path $root 'src/Portal'

# <lang>
#   <zh-CN>按 resx 原顺序读出键与值。`[ordered]` 是刻意的：部分检查（如 en-us 补齐写入位置）需要保持文件原始顺序，
#   用普通哈希表会丢序。</zh-CN>
#   <en>Read keys and values in the resx file's original order. `[ordered]` is deliberate: some checks (such as where
#   to insert missing en-us entries) need the file's original order, and a plain hash table would lose it.</en>
# </lang>
function Read-Resx([string]$fileName) {
    $path = Join-Path $resDir $fileName
    if (-not (Test-Path $path)) { throw "resx not found: $path" }
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $map = [ordered]@{}
    foreach ($m in [regex]::Matches($text, '<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)</value>\s*</data>')) {
        $map[$m.Groups[1].Value] = $m.Groups[2].Value
    }
    return $map
}

$neutral = Read-Resx 'lang.resx'
$zh = Read-Resx 'lang.zh-cn.resx'
$en = Read-Resx 'lang.en-us.resx'

# <lang>
#   <zh-CN>designer 强类型属性：只取 `internal static string X {` 形式，避免把 ResourceManager / Culture 等成员算进来。</zh-CN>
#   <en>Designer strongly typed properties: only the `internal static string X {` form, so members such as
#   ResourceManager or Culture are not counted.</en>
# </lang>
$designerLines = [System.IO.File]::ReadAllLines($designerPath, [System.Text.Encoding]::UTF8)
$properties = @()
for ($i = 0; $i -lt $designerLines.Count; $i++) {
    if ($designerLines[$i] -match '^\s*internal static string ([A-Za-z0-9_]+)\s*\{\s*$') {
        $properties += [pscustomobject]@{ Name = $Matches[1]; Line = $i + 1 }
    }
}
$propertyNames = @($properties | ForEach-Object { $_.Name })

$violations = New-Object System.Collections.Generic.List[object]

function Add-Violation([string]$check, [string]$detail) {
    $script:violations.Add([pscustomobject]@{ Check = $check; Detail = $detail })
}

# ---------- C1 键集一致 ----------
$zhMissing = @($neutral.Keys | Where-Object { -not $zh.Contains($_) })
$zhExtra = @($zh.Keys | Where-Object { -not $neutral.Contains($_) })
$enExtra = @($en.Keys | Where-Object { -not $neutral.Contains($_) })
$enMissingCount = @($neutral.Keys | Where-Object { -not $en.Contains($_) }).Count

foreach ($k in $zhMissing) { Add-Violation 'C1' "zh-cn 缺键: $k" }
foreach ($k in $zhExtra) { Add-Violation 'C1' "zh-cn 多出键（中性没有）: $k" }
foreach ($k in $enExtra) { Add-Violation 'C1' "en-us 多出键（中性没有）: $k" }
if ($enMissingCount -gt 0) { Add-Violation 'C1' "en-us 缺显式条目: $enMissingCount 个（按 W80 D1 裁定 (a) 应补齐）" }

# ---------- C2 属性对应 ----------
$resxOnly = @($neutral.Keys | Where-Object { -not ($propertyNames -contains $_) })
$propOnly = @($propertyNames | Where-Object { -not $neutral.Contains($_) })
foreach ($k in $resxOnly) { Add-Violation 'C2' "resx 有键但 designer 无属性: $k" }
foreach ($k in $propOnly) { Add-Violation 'C2' "designer 有属性但 resx 无键（孤儿属性）: $k" }

# ---------- C3 双语块配对 ----------
foreach ($p in $properties) {
    $summaryStart = -1
    for ($j = $p.Line - 2; $j -ge 0 -and $j -ge ($p.Line - 12); $j--) {
        if ($designerLines[$j] -match '^\s*///\s*<summary>\s*$') { $summaryStart = $j; break }
    }
    if ($summaryStart -lt 0) { Add-Violation 'C3' "属性 $($p.Name)（行 $($p.Line)）前未找到 <summary>"; continue }

    $hasLang = $false; $hasZh = $false; $hasEn = $false
    for ($j = $summaryStart; $j -lt $p.Line - 1; $j++) {
        if ($designerLines[$j] -match '<lang>') { $hasLang = $true }
        if ($designerLines[$j] -match '<zh-CN>') { $hasZh = $true }
        if ($designerLines[$j] -match '<en>') { $hasEn = $true }
    }
    if (-not ($hasLang -and $hasZh -and $hasEn)) {
        Add-Violation 'C3' "属性 $($p.Name) 的 <summary> 缺双语块（lang=$hasLang zh-CN=$hasZh en=$hasEn）"
    }
}

# ---------- C4 en-us 不漂移 ----------
foreach ($k in $en.Keys) {
    if (-not $neutral.Contains($k)) { continue }   # 已在 C1 报
    if (($AllowedEnUsOverride -contains $k)) { continue }
    if ($en[$k] -cne $neutral[$k]) {
        Add-Violation 'C4' "en-us 值与中性不一致（漂移）: $k`n       中性=[$($neutral[$k])]`n       en-us=[$($en[$k])]"
    }
}

# ---------- C5 中性基准洁净（不得含 CJK）----------
$cjkPattern = '[\u4e00-\u9fff]'
foreach ($k in $neutral.Keys) {
    if ($neutral[$k] -match $cjkPattern) {
        Add-Violation 'C5' "中性 resx（英文基准）的值含中文: $k = [$($neutral[$k])]"
    }
}

# ---------- C6 引用存在性 ----------
# <lang>
#   <zh-CN>只查两种**绕过编译期检查**的引用。排除 obj/bin 与 designer 文件，
#   避免把生成产物或其他资源基名（DesktopBanner 的 `Resources` 表达式基名不是 lang）算进来。</zh-CN>
#   <en>Only the two forms that **bypass compile-time checking** are scanned. obj/bin and designer files are
#   excluded so build output, or resource expressions with a base name other than `lang` (such as the
#   DesktopBanner designer), are not counted.</en>
# </lang>
$scanFiles = Get-ChildItem -Path $portalRoot -Recurse -File -Include '*.aspx', '*.ascx', '*.master', '*.cs' |
    Where-Object { $_.FullName -notlike '*\obj\*' -and $_.FullName -notlike '*\bin\*' -and $_.Name -notlike '*.designer.cs' }

$markupPattern = [regex]'<%\$\s*Resources:\s*lang\s*,\s*([A-Za-z0-9_]+)\s*%>'
$codePattern = [regex]'GetResource\(\s*"([A-Za-z0-9_]+)"\s*\)'

$checkedRefs = 0
foreach ($file in $scanFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($m in $markupPattern.Matches($text)) {
        $key = $m.Groups[1].Value
        $checkedRefs++
        if (-not $neutral.Contains($key)) { Add-Violation 'C6' "标记层引用了不存在的键: $key（$($file.Name)）" }
    }
    foreach ($m in $codePattern.Matches($text)) {
        $key = $m.Groups[1].Value
        $checkedRefs++
        if (-not $neutral.Contains($key)) { Add-Violation 'C6' "代码层 GetResource 引用了不存在的键: $key（$($file.Name)）" }
    }
}

# ---------- 报告 ----------
$byCheck = $violations | Group-Object -Property Check | Sort-Object Name
Write-Output '=== resource contract ==='
Write-Output ("neutral={0}  zh-cn={1}  en-us={2}  designerProperties={3}  scannedReferences={4}" -f $neutral.Count, $zh.Count, $en.Count, $propertyNames.Count, $checkedRefs)
Write-Output ("en-us missing={0}  resx-without-property={1}  orphan-properties={2}" -f $enMissingCount, $resxOnly.Count, $propOnly.Count)
foreach ($g in $byCheck) {
    Write-Output ("[{0}] {1} violation(s)" -f $g.Name, $g.Count)
    foreach ($v in ($g.Group | Select-Object -First 10)) { Write-Output ("    " + $v.Detail) }
    if ($g.Count -gt 10) { Write-Output ("    ... 另有 {0} 条" -f ($g.Count - 10)) }
}

if ($violations.Count -gt 0) {
    if ($WhatIfPreference) {
        Write-Output ("WhatIf: {0} violation(s) reported, exit code left at 0." -f $violations.Count)
        exit 0
    }
    Write-Output ("resource contract FAILED: {0} violation(s)" -f $violations.Count)
    exit 1
}

Write-Output 'resource contract OK: 0 violation(s)'
exit 0
