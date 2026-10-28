<#
.SYNOPSIS
<lang>
  <zh-CN>校验模块包契约：`Enabled` 追加必须已登记，且每个 `module.json` 的包必须被覆盖或显式登记为“不挂载”。</zh-CN>
  <en>Validates the module-package contract: every `Enabled` append must be registered, and every `module.json` package must be covered or explicitly registered as "not mounted".</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>防的是一类“模块写了但永远不渲染、且没有任何机器能发现”的静默漏覆盖：
  `HIA.MyWorkItems` 曾在仓库里有 `module.json`，却不在任何 Profile 的 Packages 里，只有人工翻配置才能发现。
  同时给 `Portal.ModulePackages.Enabled` 这条“追加白名单”通道设闸，避免它变成架空档位分组意图的万能后门。

  五项检查（M1–M5）：
  M1 `Enabled` 的每一项都必须在 `appendAllowList` 里；
  M2 汇总所有 Profile 的 Packages 并集；
  M3 每个 `module.json` 的 packageId 必须被「Profile 并集 / Enabled / notMounted」之一覆盖；
  M4 `notMounted` 里的包不得再被 Profile 或 Enabled 覆盖（防止登记腐化）；
  M5 `appendAllowList` 与 `notMounted` 不得重叠。

  证明边界：本门禁只证明“包与配置的覆盖面自洽”，不证明包本身可运行、不证明档位已启用、也不替代运行期渲染证据。
  归 L0：只读 `Web.config`、`module.json` 与登记文件，不连库、不启动站点、不写配置。</zh-CN>
  <en>It guards a silent coverage gap where a module ships with a `module.json` but never renders and nothing detects it:
  `HIA.MyWorkItems` existed in the repository yet appeared in no Profile Packages list, discoverable only by manually reading configuration.
  It also puts a gate on the `Portal.ModulePackages.Enabled` append channel so it cannot become a catch-all that bypasses profile grouping.

  Five checks (M1–M5):
  M1 every `Enabled` entry must appear in `appendAllowList`;
  M2 union all Profile Packages lists;
  M3 every `module.json` packageId must be covered by "Profile union / Enabled / notMounted";
  M4 a `notMounted` package must not be covered by a Profile or `Enabled` (prevents registry rot);
  M5 `appendAllowList` and `notMounted` must not overlap.

  Proof boundary: this gate proves only that package-to-configuration coverage is self-consistent; it does not prove a package runs,
  that a Profile is active, or replace runtime rendering evidence. Classified L0: reads only `Web.config`, `module.json`, and the
  registry file — no database, no site start-up, no configuration writes.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string] $RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>统一使用无 BOM UTF-8 读取，避免 PowerShell 默认编码把中文配置读成乱码而误判。</zh-CN>
#   <en>Read with UTF-8 without BOM so the default PowerShell encoding cannot corrupt Chinese configuration and cause false verdicts.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)

# <lang>
#   <zh-CN>失败清单只保存稳定键与检查号，不保存无关配置片段，便于输出与后续门禁复用。</zh-CN>
#   <en>The failure list holds only stable keys and check numbers, not unrelated configuration excerpts, easing output and reuse.</en>
# </lang>
$failures = New-Object 'System.Collections.Generic.List[string]'

function Read-Text([string] $path) {
    return [System.IO.File]::ReadAllText($path, $utf8)
}

# <lang>
#   <zh-CN>逗号列表统一在此拆分：去空白、去空段、按首次出现去重，保证所有检查使用同一语义。</zh-CN>
#   <en>Comma lists are split here once: trim, drop empty segments, de-duplicate by first occurrence, so every check shares one semantic.</en>
# </lang>
function Split-Ids([string] $value) {
    $result = New-Object 'System.Collections.Generic.List[string]'
    # <lang>
    #   <zh-CN>提前返回同样必须用一元逗号：否则空输入会把空 List 展开成 `$null`，调用方 `.Contains()` 直接抛异常。</zh-CN>
    #   <en>The early return needs the same unary comma: otherwise an empty input expands the empty List to `$null` and the caller's `.Contains()` throws.</en>
    # </lang>
    if ([string]::IsNullOrWhiteSpace($value)) { return ,$result }
    foreach ($part in $value.Split(',')) {
        $item = ($part).Trim()
        if ($item.Length -gt 0 -and -not $result.Contains($item)) { $result.Add($item) }
    }

    # <lang>
    #   <zh-CN>用一元逗号抑制管道枚举：空 `List` 若直接 return 会被展开成 `$null`，
    #   调用方再 `.Contains()` 就抛“不能对值为 Null 的表达式调用方法”，使门禁以异常而非结论失败。
    #   这里必须稳定返回集合对象本身。</zh-CN>
    #   <en>Use a unary comma to suppress pipeline enumeration: returning an empty `List` directly is expanded to `$null`,
    #   and the caller's `.Contains()` then throws "cannot call a method on a null-valued expression", making the gate fail
    #   with an exception instead of a verdict. Return the collection object itself.</en>
    # </lang>
    return ,$result
}

$webConfig = Join-Path $RepoRoot 'src\Portal\Web.config'
if (-not (Test-Path -LiteralPath $webConfig)) {
    Write-Output '[FAIL] M0 找不到 src/Portal/Web.config'
    exit 1
}

# <lang>
#   <zh-CN>只解析 appSettings 的 add 键值；不执行配置转换，也不读取部署侧环境文件。</zh-CN>
#   <en>Parse only appSettings add entries; no configuration transform is executed and no deployment environment file is read.</en>
# </lang>
$configText = Read-Text $webConfig
$settings = @{}
foreach ($match in [regex]::Matches($configText, '<add\s+key="([^"]+)"\s+value="([^"]*)"\s*/?>')) {
    $settings[$match.Groups[1].Value] = $match.Groups[2].Value
}

# <lang>
#   <zh-CN>**必须再叠加 JSON 配置层**：运行时实际取值来自 `Config/appSettings.json`（并可被环境覆盖文件进一步覆盖），
#   Web.config 中的同名键在部署后会被它覆盖。只读 Web.config 会让本门禁校验一个**不生效的值**——
#   2026-10-28 就真实发生过：Web.config 已含 `HIA.Help`，而运行时读到的 JSON 仍是旧值，
#   模块被 `CoreOnly` 拒绝、帮助页空白；门禁却报 PASS（因为它看的是 Web.config）。这是"门禁绿而产品坏"的典型。
#   此处以 JSON 覆盖 Web.config 同名键，与运行时优先级保持一致。</zh-CN>
#   <en>**The JSON configuration layer must be layered on top**: runtime values actually come from `Config/appSettings.json`
#   (which environment override files may further override), and it wins over identically named Web.config keys after deployment.
#   Reading Web.config alone makes this gate validate a value that has no effect — exactly what happened on 2026-10-28: Web.config
#   already listed `HIA.Help` while the runtime JSON still held the old value, so `CoreOnly` rejected the module and the help page
#   rendered empty, yet the gate reported PASS because it inspected Web.config. JSON entries therefore override Web.config entries
#   here, matching runtime precedence.</en>
# </lang>
$jsonConfig = Join-Path $RepoRoot 'src\Portal\Config\appSettings.json'
if (Test-Path -LiteralPath $jsonConfig) {
    $jsonSettings = Read-Text $jsonConfig | ConvertFrom-Json
    foreach ($property in $jsonSettings.PSObject.Properties) {
        if ($property.Name -eq 'Portal.ModulePackages.Enabled' -or $property.Name -like 'Portal.ModuleProfiles.*') {
            $settings[$property.Name] = [string]$property.Value
        }
    }
}

$profilePackages = @{}
foreach ($key in $settings.Keys) {
    if ($key -match '^Portal\.ModuleProfiles\.(.+)\.Packages$') {
        $profilePackages[$Matches[1]] = $settings[$key]
    }
}

$enabledValue = ''
if ($settings.ContainsKey('Portal.ModulePackages.Enabled')) { $enabledValue = $settings['Portal.ModulePackages.Enabled'] }
$enabledIds = Split-Ids $enabledValue

$registryPath = Join-Path $RepoRoot 'dev\module-package-registry.json'
if (-not (Test-Path -LiteralPath $registryPath)) {
    Write-Output '[FAIL] M0 缺少登记文件 dev/module-package-registry.json'
    exit 1
}
$registry = Read-Text $registryPath | ConvertFrom-Json

# <lang>
#   <zh-CN>防御式读取两个清单：属性缺失或为空时视为空集合，而不是让 Set-StrictMode 抛异常。
#   “门禁崩溃”与“门禁通过”在 CI 里容易都被当成绿，因此这里必须把缺失降级为可判定的空集合，
#   由后续 M3/M4/M5 给出明确结论。</zh-CN>
#   <en>Read both lists defensively: a missing or empty property becomes an empty set instead of throwing under
#   Set-StrictMode. A crashed gate and a passing gate are easily both treated as green in CI, so absence must degrade
#   to an evaluable empty set and let M3/M4/M5 reach an explicit conclusion.</en>
# </lang>
$appendAllowRaw = @()
if ($registry.PSObject.Properties.Name -contains 'appendAllowList') { $appendAllowRaw = @($registry.appendAllowList) }

$notMountedRaw = @()
if ($registry.PSObject.Properties.Name -contains 'notMounted') { $notMountedRaw = @($registry.notMounted) }

$appendAllow = Split-Ids ($appendAllowRaw -join ',')
$notMounted = Split-Ids ($notMountedRaw -join ',')

# <lang>
#   <zh-CN>M1：Enabled 是“追加白名单”通道，每一项都必须先登记；未登记的追加等同绕过档位分组意图。</zh-CN>
#   <en>M1: `Enabled` is an append channel, so every entry must be registered first; an unregistered append bypasses profile grouping intent.</en>
# </lang>
foreach ($id in $enabledIds) {
    if (-not $appendAllow.Contains($id)) { $failures.Add("M1 Enabled 追加未登记：$id") }
}

# <lang>
#   <zh-CN>M2：汇总全部 Profile 的包并集（不区分是否启用），用来回答“这个包有没有档位覆盖”。</zh-CN>
#   <en>M2: union every Profile Packages list regardless of activation, answering "does any profile cover this package".</en>
# </lang>
$profileUnion = New-Object 'System.Collections.Generic.List[string]'
foreach ($entry in $profilePackages.GetEnumerator()) {
    foreach ($id in (Split-Ids $entry.Value)) {
        if (-not $profileUnion.Contains($id)) { $profileUnion.Add($id) }
    }
}

# <lang>
#   <zh-CN>M3：扫描受信任部署目录下的 module.json，只认固定文件名，不递归猜测其它清单。</zh-CN>
#   <en>M3: scan module.json under the trusted deployment root, accepting only the fixed file name and never guessing other manifests.</en>
# </lang>
$moduleRoot = Join-Path $RepoRoot 'src\Portal\DesktopModules'
$manifestIds = New-Object 'System.Collections.Generic.List[string]'
if (Test-Path -LiteralPath $moduleRoot) {
    foreach ($dir in (Get-ChildItem -LiteralPath $moduleRoot -Directory)) {
        $manifestPath = Join-Path $dir.FullName 'module.json'
        if (-not (Test-Path -LiteralPath $manifestPath)) { continue }
        $manifest = Read-Text $manifestPath | ConvertFrom-Json
        if ($manifest.PSObject.Properties.Name -contains 'packageId') {
            $packageId = [string]$manifest.packageId
            if ($packageId.Trim().Length -gt 0 -and -not $manifestIds.Contains($packageId.Trim())) {
                $manifestIds.Add($packageId.Trim())
            }
        }
    }
}

foreach ($id in $manifestIds) {
    $covered = $profileUnion.Contains($id) -or $enabledIds.Contains($id) -or $notMounted.Contains($id)
    if (-not $covered) { $failures.Add("M3 模块包无档位覆盖且未登记为不挂载：$id") }
}

# <lang>
#   <zh-CN>M4：反向核对，防止“已登记不挂载”的包后来被某个档位启用却没人清理登记，导致登记腐化。</zh-CN>
#   <en>M4: reverse check, preventing a package registered as not-mounted from later being covered by a Profile while the stale entry stays behind.</en>
# </lang>
foreach ($id in $notMounted) {
    if ($profileUnion.Contains($id) -or $enabledIds.Contains($id)) {
        $failures.Add("M4 已登记为不挂载但实际已被覆盖：$id")
    }
}

# <lang>
#   <zh-CN>M5：两个清单语义互斥，重叠意味着配置意图自相矛盾。</zh-CN>
#   <en>M5: the two lists are mutually exclusive by meaning; an overlap means the configuration intent contradicts itself.</en>
# </lang>
foreach ($id in $notMounted) {
    if ($appendAllow.Contains($id)) { $failures.Add("M5 appendAllowList 与 notMounted 重叠：$id") }
}

Write-Output ('Profile 数：' + $profilePackages.Count + '；Profile 包并集：' + $profileUnion.Count + '；Enabled：' + $enabledIds.Count + '；module.json：' + $manifestIds.Count)

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Output ('[FAIL] ' + $failure) }
    Write-Output ('RESULT: Fail（' + $failures.Count + ' 项）')
    exit 1
}

Write-Output '[PASS] 模块包契约：Enabled 追加均已登记，全部 module.json 均被覆盖或显式登记为不挂载。'
Write-Output 'RESULT: Pass'
exit 0
