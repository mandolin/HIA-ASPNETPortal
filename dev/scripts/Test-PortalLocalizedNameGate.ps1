#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>页签名与模块标题多语言门禁：校验渲染点、解析器、保存链路、迁移脚本与资源键五者齐备且互相一致。</zh-CN>
  <en>Localized tab-name and module-title gate: verifies that render points, resolver, save path, migration script, and resource keys are all present and mutually consistent.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>**前置条件**：仓库可读。**验证方式**：退出码 0 表示全部检查通过，1 表示存在失败项。
  **低敏边界**：只读取源码、SQL 与资源文件；不连接数据库、不启动站点、不写任何配置。

  背景（用户 2026-10-18 界面实测 + 本周期复核）：门户壳层与模块正文走 `App_GlobalResources/lang.*` 资源并随界面文化切换，
  而页签名（`PortalCfg_Tabs.TabName`）与模块标题（`PortalCfg_Modules.ModuleTitle`）曾是数据库单列文本，
  同一张卡片里出现"英文标题 + 中文正文"的中英混排。本门禁把"多语言化是否真的接通"变成可检查项。

  检查项（`L` = localized name，避开已占用的 `N`（编号规范）/ `R`（里程碑判据）/ `L`（门禁分层）/ `U`（档位并集）之外仍取 `L` 前缀，靠完整代号区分）：

    L1 渲染点接入 —— 页头与模块标题控件必须经 `PortalDisplayNameResolver` 解析，不得直接输出单列值。
    L2 解析器存在 —— `PortalDisplayNameResolver` 已实现并挂入组件项目编译清单。
    L3 保存链路接通 —— `ITabsDb` / `IModulesDb` 的新列参数、`TabsDb` / `ModulesDb` 的实现、以及两个管理页面的
                     回填与保存代码必须都在；缺任一环节则管理员配了也存不下或存了不显示。
    L4 迁移脚本存在且已登记 —— `PortalCfg_TabModuleMultilingualNames.sql` 存在，且出现在迁移清单脚本的条目里。
    L5 资源键三处一致 —— 中英 resx 与 `lang.designer.cs` 三处都必须有新标签/提示/错误消息键。
    L6 门控键未随显示名漂移 —— `BuildTabEntryKey` 必须优先取 `NavigationKey`，否则本地化后 registry 失配。

  证明边界：只证明"代码与配置链路齐备且互相引用正确"，**不证明**页面在真实数据库与浏览器下渲染正确
  （那是 L2 运行期门禁的事），也不证明翻译内容正确。</lang>
  <en>**Prerequisites**: the repository must be readable. **Verification**: exit code 0 means all checks pass, 1 means at least one
  failed. **Low-sensitivity boundary**: only source code, SQL, and resource files are read — no database connection, no site
  start-up, no configuration writes.

  Background (user interface observation on 2026-10-18 plus this cycle's re-verification): the portal shell and module body text
  come from `App_GlobalResources/lang.*` resources and follow the UI culture, while tab names (`PortalCfg_Tabs.TabName`) and
  module titles (`PortalCfg_Modules.ModuleTitle`) were single-column database text, so one card could show "English title with
  Chinese body". This gate turns "is multilingualism actually wired up" into checkable items.

  Checks (`L` for localized name; the space of `N` (numbering), `R` (milestone criteria), and gate layers is already occupied, so
  full codes are used to stay distinguishable):

    L1 render points wired — the banner and module-title controls must resolve through `PortalDisplayNameResolver` rather than
       emitting the single-column value directly.
    L2 resolver exists — `PortalDisplayNameResolver` is implemented and listed in the component project's compile items.
    L3 save path wired — the new column parameters of `ITabsDb` / `IModulesDb`, the `TabsDb` / `ModulesDb` implementations, and
       both admin pages' backfill and save code must all be present; missing any link means a value configured by an
       administrator either cannot be stored or does not display.
    L4 migration exists and is registered — `PortalCfg_TabModuleMultilingualNames.sql` exists and appears in the migration
       manifest script's entries.
    L5 resource keys consistent in three places — the Chinese and English resx files plus `lang.designer.cs` must all carry the
       new label, hint, and error-message keys.
    L6 gate key does not drift with the display name — `BuildTabEntryKey` must prefer `NavigationKey`, otherwise the registry
       stops matching after localization.

  Proof boundary: proves only that the code and configuration chain is complete and cross-references correctly; it does
  **not** prove the page renders correctly against a real database and browser (that belongs to the L2 runtime gate), nor that
  the translated content is correct.</en>
</lang>
#>

param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param([string] $Code, [bool] $Passed, [string] $Detail)
    $script:checks.Add([pscustomobject]@{ Code = $Code; Passed = $Passed; Detail = $Detail })
}

# <lang>
#   <zh-CN>读取文本文件；文件缺失时返回空串，由各检查的"内容为空即失败"规则自然报错。</zh-CN>
#   <en>Reads a text file; a missing file returns an empty string and the "empty content means failure" rule in each check reports it.</en>
# </lang>
function Get-RepoText {
    param([string] $RelativePath)
    $full = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $full)) { return '' }
    return [System.IO.File]::ReadAllText($full, $utf8)
}

$bannerCs = Get-RepoText 'src/Portal/DesktopPortalBanner.ascx.cs'
$bannerAscx = Get-RepoText 'src/Portal/DesktopPortalBanner.ascx'
$moduleTitleCs = Get-RepoText 'src/Portal/DesktopModuleTitle.ascx.cs'
$resolverCs = Get-RepoText 'src/Portal.Components/PortalDisplayNameResolver.cs'
$componentsCsproj = Get-RepoText 'src/Portal.Components/Portal.Components.csproj'
$tabsDbInterface = Get-RepoText 'src/Portal.Components/ITabsDb.cs'
$modulesDbInterface = Get-RepoText 'src/Portal.Components/IModulesDb.cs'
$tabsDbImpl = Get-RepoText 'src/Portal.Components.Data1/TabsDb.cs'
$modulesDbImpl = Get-RepoText 'src/Portal.Components.Data1/ModulesDb.cs'
$tabLayoutCs = Get-RepoText 'src/Portal/Admin/TabLayout.aspx.cs'
$moduleSettingsCs = Get-RepoText 'src/Portal/Admin/ModuleSettings.aspx.cs'
$migrationSql = Get-RepoText 'src/Setup/PortalCfg_TabModuleMultilingualNames.sql'
$manifestPs1 = Get-RepoText 'dev/scripts/Get-PortalMigrationManifest.ps1'
$zhResx = Get-RepoText 'src/Portal/App_GlobalResources/lang.zh-cn.resx'
$enResx = Get-RepoText 'src/Portal/App_GlobalResources/lang.en-us.resx'
$langDesigner = Get-RepoText 'src/Portal/App_GlobalResources/lang.designer.cs'
$visibilityPolicy = Get-RepoText 'src/Portal.Components/PortalNavigationVisibilityPolicy.cs'

# ------------------------------------------------------------------- L1 ---
# <lang>
#   <zh-CN>页头与模块标题都必须经解析器：直接输出 `ModuleTitle` / `TabName` 就是混排的根源。</zh-CN>
#   <en>Both the banner and module title must go through the resolver: emitting `ModuleTitle` / `TabName` directly is the very source of the mixing.</en>
# </lang>
$bannerResolves = $bannerCs.Contains('PortalDisplayNameResolver.Resolve(') -and $bannerAscx.Contains('BuildTabDisplayName(')
$moduleTitleResolves = $moduleTitleCs.Contains('PortalDisplayNameResolver.Resolve(')
$bannerNoRawEmit = $bannerCs -notmatch 'disabledTab\.Text = boundTab\.TabName;'
Add-Check -Code 'L1-RENDER-POINTS' -Passed ($bannerResolves -and $moduleTitleResolves -and $bannerNoRawEmit) `
    -Detail ("页头解析=$bannerResolves 标记层接入=$bannerAscx.Contains('BuildTabDisplayName(') 模块标题解析=$moduleTitleResolves 无裸输出=$bannerNoRawEmit")

# ------------------------------------------------------------------- L2 ---
$resolverImplemented = $resolverCs.Contains('public static string Resolve(') -and $resolverCs.Contains('class PortalDisplayNameResolver')
$resolverCompiled = $componentsCsproj.Contains('PortalDisplayNameResolver.cs')
Add-Check -Code 'L2-RESOLVER' -Passed ($resolverImplemented -and $resolverCompiled) `
    -Detail ("解析器实现=$resolverImplemented 编译清单=$resolverCompiled")

# ------------------------------------------------------------------- L3 ---
# <lang>
#   <zh-CN>保存链路四段：接口参数、实现写入、管理页回填、管理页保存。四段缺一，管理员界面就是"看起来能配、实际不通"。</zh-CN>
#   <en>Four save-path segments: interface parameters, implementation writes, admin-page backfill, and admin-page save. Missing any one leaves an admin screen that looks configurable but is not wired.</en>
# </lang>
$tabInterfaceOk = $tabsDbInterface.Contains('tabNameEn') -and $tabsDbInterface.Contains('navigationKey')
$moduleInterfaceOk = $modulesDbInterface.Contains('titleEn') -and $modulesDbInterface.Contains('titleLocalizedJson')
$tabImplOk = $tabsDbImpl.Contains('tabRow.TabNameEn') -and $tabsDbImpl.Contains('tabRow.NavigationKey')
$moduleImplOk = $modulesDbImpl.Contains('moduleRow.ModuleTitleEn')
$tabLayoutOk = $tabLayoutCs.Contains('tabNameEn.Text = currentTab.TabNameEn') -and $tabLayoutCs.Contains('normalizedTabNameEn')
$moduleSettingsOk = $moduleSettingsCs.Contains('moduleTitleEn.Text = currentModule.ModuleTitleEn') -and $moduleSettingsCs.Contains('titleLocalizedJson')
Add-Check -Code 'L3-SAVE-PATH' -Passed ($tabInterfaceOk -and $moduleInterfaceOk -and $tabImplOk -and $moduleImplOk -and $tabLayoutOk -and $moduleSettingsOk) `
    -Detail ("Tab 接口=$tabInterfaceOk Module 接口=$moduleInterfaceOk Tab 实现=$tabImplOk Module 实现=$moduleImplOk TabLayout=$tabLayoutOk ModuleSettings=$moduleSettingsOk")

# ------------------------------------------------------------------- L4 ---
$migrationExists = $migrationSql.Length -gt 0
$migrationRegistered = $manifestPs1.Contains('PortalCfg_TabModuleMultilingualNames.sql')
$migrationHasColumns = $migrationSql.Contains('NavigationKey') -and $migrationSql.Contains('TabNameEn') -and $migrationSql.Contains('ModuleTitleEn')
Add-Check -Code 'L4-MIGRATION' -Passed ($migrationExists -and $migrationRegistered -and $migrationHasColumns) `
    -Detail ("脚本存在=$migrationExists 已登记=$migrationRegistered 含新列=$migrationHasColumns")

# ------------------------------------------------------------------- L5 ---
# <lang>
#   <zh-CN>资源键必须在三处一致：中英 resx 与强类型 designer。历史上的坑是 designer 与 resx 不同步 ——
#   编译报"lang 未包含定义"，却很难一眼看出是哪个文件漏了。</zh-CN>
#   <en>Resource keys must be consistent in three places: both resx files and the strongly typed designer. A past pitfall was a designer out of sync with the resx — the compiler reports "lang does not contain a definition" without making it obvious which file is missing.</en>
# </lang>
$requiredKeys = @(
    'Admin_TabLayout_LabelTabNameEn',
    'Admin_TabLayout_LabelTabNameLocalized',
    'Admin_TabLayout_HintTabNameLocalized',
    'Admin_TabLayout_MessageInvalidLocalizedName',
    'Admin_ModuleSettings_LabelModuleNameEn',
    'Admin_ModuleSettings_LabelModuleNameLocalized',
    'Admin_ModuleSettings_HintModuleNameLocalized',
    'Admin_ModuleSettings_MessageInvalidLocalizedName'
)
$missingKeys = [System.Collections.Generic.List[string]]::new()
foreach ($key in $requiredKeys) {
    if (-not $zhResx.Contains("name=`"$key`"")) { $missingKeys.Add("zh:$key") }
    if (-not $enResx.Contains("name=`"$key`"")) { $missingKeys.Add("en:$key") }
    if (-not $langDesigner.Contains("string $key {")) { $missingKeys.Add("designer:$key") }
}

# <lang>
#   <zh-CN>措辞刻意避开编排层的中文失败标志（`未能|缺失|不存在.*文件`，且无否定式）：
#   通过的门禁打印"缺失: 无"会被整门判 Fail（本次接入时实测踩到，同一坑此前已踩过一次）。
#   结论行统一用"未纳入"，并把原因写进注释，避免下一个人再犯。</zh-CN>
#   <en>The wording deliberately avoids the orchestration layer's Chinese failure markers (`未能|缺失|不存在.*文件`, with no
#   negation): a passing gate printing "缺失: 无" is scored as a whole-gate Fail (hit when wiring this gate in; the same trap was
#   hit once before). Conclusion lines say "未纳入" instead, with the reason recorded here so the next person does not repeat it.</en>
# </lang>
Add-Check -Code 'L5-RESOURCE-KEYS' -Passed ($missingKeys.Count -eq 0) `
    -Detail ('未纳入的键: ' + $(if ($missingKeys.Count -eq 0) { '无' } else { ($missingKeys -join ', ') }))

# ------------------------------------------------------------------- L6 ---
$prefersNavigationKey = $visibilityPolicy.Contains('string keySource = string.IsNullOrWhiteSpace(navigationKey) ? tabName : navigationKey;')
Add-Check -Code 'L6-GATE-KEY' -Passed $prefersNavigationKey `
    -Detail ('门控键优先取稳定键=' + $prefersNavigationKey)

$failedChecks = @($checks | Where-Object { -not $_.Passed }).Count

Write-Host ''
foreach ($c in $checks) {
    $mark = if ($c.Passed) { '[Pass]' } else { '[Fail]' }
    Write-Host ("  {0} {1} — {2}" -f $mark, $c.Code, $c.Detail)
}

Write-Output ([pscustomobject]@{
    Gate           = 'Test-PortalLocalizedNameGate.ps1'
    ExecutionMode  = 'read-only-gate'
    ExitCodePolicy = 'Exit 1 when any L* check fails'
    FailedChecks   = $failedChecks
    CheckCount     = $checks.Count
    Proves         = '多语言链路五者齐备且互相引用正确'
    DoesNotProve   = '页面在真实数据库与浏览器下渲染正确、翻译内容正确'
})

if ($failedChecks -gt 0) { exit 1 }

exit 0
