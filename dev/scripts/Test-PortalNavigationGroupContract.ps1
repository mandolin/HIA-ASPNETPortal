#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>主导航分组配置别名与主题设置入链的静态契约一致性门禁（L0）。不连数据库、不启动站点、不执行浏览器。</zh-CN>
  <en>Static contract-consistency gate (L0) for main-navigation group configuration aliases and the theme settings inbound link. No database, site, or browser is used.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>解决的问题（`C-anp-P51` / `W-anp-P290`）：R4 首子项按 Q1 裁定 ③ 以「配置别名分组」达成，若只靠人工看代码，
  以下四类漂移都不会被构建或单测发现：① 词表不再覆盖种子 `NavigationKey` 全集，某些页签静默失去分组；
  ② 别名配置解析从 fail-closed 被改成宽松解析，非法配置悄悄隐藏页签；③ 分组类型被塞入角色/权限字段，
  使 A 级硬边界「分组不参与授权」被破坏；④ 主题设置别名条目被删或改组，后台页重新变回 0 入链不可达。
  本门禁把四类约束变成机器可核对的**静态契约**，全部判定从权威事实源推导（种子 SQL / 源码 / 台账），不硬编码数据行。</zh-CN>
  <en>Problem solved (`C-anp-P51` / `W-anp-P290`): with Q1 ruling ③, R4's first sub-criterion is met through configuration
  aliases, but four drifts would escape builds and unit tests if checked only by eye: (1) the vocabulary stops covering the
  full seeded `NavigationKey` set, silently leaving tabs ungrouped; (2) alias parsing is loosened from fail-closed, so invalid
  configuration quietly hides tabs; (3) role/permission fields are added to group types, breaking the class-A boundary that
  groups never participate in authorization; (4) the theme settings alias entry is deleted or regrouped, making the admin page
  unreachable again with zero inbound links. This gate turns all four into machine-checkable **static contracts**, with every
  decision derived from authoritative sources (seed SQL / source code / ledgers) rather than hard-coded data rows.</en>
</lang>

  <zh-CN>断言分组（全部 fail-closed，任一异常即判红）：</zh-CN>
  <en>Assertion groups (all fail-closed; any exception fails the gate):</en>
  <zh-CN>
    G1 词表覆盖：`PortalNavigationGroupAlias.cs` 的四个 `GroupDefinition` 默认成员并集，必须恰好等于
       `Portal_NavigationSeed.sql` 取值行的 `NavigationKey` 去重集合（顺序无关、大小写不敏感）。
    G2 配置接线：`Portal.Navigation.GroupAliases` 必须在 `PortalSettingKeys` / `PortalSettingsRegistry`（含 All 清单）/
       `PortalCfg_SystemSettings.sql` 种子三处登记；解析方法必须含 fail-closed 的整份拒绝与「回退默认词表」语义。
    G3 A 级硬边界：分组定义与别名静态类中不得出现授权相关公开成员/字段，也不得引用 `IsInRoles` /
       `EnsurePermission` / `AccessRoles` 等授权入口；`PortalCfg_Tabs` 建表脚本不得新增父子列。
    G4 主题设置入链：注册表必须含 `Admin.Ops.ThemeSettingsEntry` 别名（Target / Active / AdminOnly / ThemeView 齐备），
       且其分组与运行期可达的 `Admin.SystemHealth` 相同；`evidence-coverage.json` 与 `gate-coverage-map.json`
       必须标记为已覆盖并登记 entry。
  </zh-CN>
  <en>
    G1 vocabulary coverage: the union of default members across the four `GroupDefinition` entries in
       `PortalNavigationGroupAlias.cs` must exactly equal the de-duplicated `NavigationKey` set from the value rows of
       `Portal_NavigationSeed.sql` (order-independent, case-insensitive).
    G2 configuration wiring: `Portal.Navigation.GroupAliases` must be registered in `PortalSettingKeys`,
       `PortalSettingsRegistry` (including the All list), and the `PortalCfg_SystemSettings.sql` seed; the parser must keep
       whole-configuration fail-closed rejection and the fallback-to-defaults semantics.
    G3 class-A boundary: group definition and alias static types must expose no authorization-related public member/field
       and must not reference `IsInRoles` / `EnsurePermission` / `AccessRoles`; the `PortalCfg_Tabs` create script must not
       gain a parent/child column.
    G4 theme settings inbound link: the registry must contain the `Admin.Ops.ThemeSettingsEntry` alias (with Target, Active,
       AdminOnly, ThemeView) in the same group as the reachable `Admin.SystemHealth`; both `evidence-coverage.json` and
       `gate-coverage-map.json` must mark the surface covered with an entry.
  </en>

  <zh-CN>证明边界：只证明**静态契约自洽**（源码、种子、台账三处一致且 fail-closed 语义存在）；
  不证明运行期真的渲染出入链 HTML、不证明浏览器点击可达（由 L2 运行期证据承载），
  不证明分组名翻译贴切，也不证明任何授权判定正确（由单测承载）。</zh-CN>
  <en>Proof boundary: proves only **static contract consistency** (source, seed and ledgers agree and fail-closed semantics
  exist); it does not prove the inbound link HTML is rendered at runtime, that a browser can click through (covered by L2
  runtime evidence), that group translations are apt, or that any authorization decision is correct (covered by unit tests).</en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalNavigationGroupContract.ps1'  # 仓库根目录执行；exit 0 全过，exit 1 任一失败</zh-CN>
  <en>& 'dev\scripts\Test-PortalNavigationGroupContract.ps1'     # run from the repository root; exit 0 pass, exit 1 any failure</en>
</lang>
.NOTES
<lang>
  <zh-CN>有效性按项目惯例由四步注入证明（基线绿 → 注入红 → 还原绿）验证，夹具见
  `dev/scripts/gate-injections/L0-navigation-group-contract.json`。</zh-CN>
  <en>Effectiveness is proven by the project's four-step injection (baseline green → injected red → restored green),
  with fixtures in `dev/scripts/gate-injections/L0-navigation-group-contract.json`.</en>
</lang>
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>显式 UTF-8 无 BOM 读取；仓库根由脚本目录上两级推导，所有路径相对仓库根，不依赖当前工作目录。</zh-CN>
#   <en>Read explicitly as UTF-8 without BOM; the repository root is derived two levels up from this script directory, so every path is root-relative and independent of the current working directory.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Read-RepoText([string] $relativePath) {
    $full = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $full)) {
        throw ('Missing authoritative source: ' + $relativePath)
    }
    return [System.IO.File]::ReadAllText($full, $utf8)
}

$failures = [System.Collections.Generic.List[string]]::new()
$checks = [System.Collections.Generic.List[string]]::new()

function Assert-Check([bool] $condition, [string] $code, [string] $message) {
    if ($condition) {
        $checks.Add('[Pass] ' + $code)
    }
    else {
        $failures.Add($code + ' — ' + $message)
        Write-Host ('  [Fail] ' + $code + ' — ' + $message) -ForegroundColor Red
    }
}

try {
    # <lang>
    #   <zh-CN>权威事实源：种子取值行的首列是 NavigationKey；只解析形如 `(N'Key',` 的取值行，跳过注释与建表语句。</zh-CN>
    #   <en>Authoritative source: the first column of a seed value row is the NavigationKey; only rows shaped like `(N'Key',` are parsed, skipping comments and DDL.</en>
    # </lang>
    $seedText = Read-RepoText 'src/Setup/Portal_NavigationSeed.sql'
    $seedKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($seedText, '(?m)^\s*\(N''([^'']+)''\s*,')) {
        [void]$seedKeys.Add($m.Groups[1].Value.Trim())
    }

    $aliasText = Read-RepoText 'src/Portal.Components/PortalNavigationGroupAlias.cs'

    # <lang>
    #   <zh-CN>先剥离 XML 文档注释、`/* */` 与整行 `//` 注释：分组机制的正当说明必须能引用
    #   「可见性仍由 AccessRoles 判定」等既有链路，但不能真的调用它们；按项目「注释提及不计」口径只校验代码文本。
    #   剥离结果同时供 G2 计数与 G3 授权符号判定使用，故在此统一计算一次。</zh-CN>
    #   <en>Strip XML doc comments, `/* */` blocks and whole-line `//` comments first: legitimate documentation must be
    #   able to reference existing chains such as "visibility is still decided by AccessRoles", but the code must not call
    #   them. Following the project's "comment mentions do not count" convention, only code text is checked. The stripped
    #   text is computed once here and reused by both the G2 count and the G3 authorization-symbol assertions.</en>
    # </lang>
    $aliasCode = [regex]::Replace($aliasText, '(?s)///.*?$', '', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $aliasCode = [regex]::Replace($aliasCode, '(?s)/\*.*?\*/', '')
    $aliasCode = [regex]::Replace($aliasCode, '(?m)^\s*//.*$', '')

    # <lang>
    #   <zh-CN>G1：从每个 `new GroupDefinition(` 的第五实参 `new[] { ... }` 抽取默认成员键，与种子集合双向比对。</zh-CN>
    #   <en>G1: extract default member keys from the fifth `new[] { ... }` argument of each `new GroupDefinition(` and compare both directions against the seed set.</en>
    # </lang>
    $groupBlocks = [regex]::Matches($aliasText, '(?s)new GroupDefinition\((.*?)\)\s*(?=,\s*new GroupDefinition|\s*\};)')
    $memberKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($block in $groupBlocks) {
        $memberArray = [regex]::Match($block.Groups[1].Value, 'new\[\]\s*\{([^}]*)\}')
        if ($memberArray.Success) {
            foreach ($m in [regex]::Matches($memberArray.Groups[1].Value, '"([^"]+)"')) {
                [void]$memberKeys.Add($m.Groups[1].Value.Trim())
            }
        }
    }
    Assert-Check ($groupBlocks.Count -eq 4) 'G1.GroupCount' 'Closed vocabulary must declare exactly four functional-family groups.'
    Assert-Check ($seedKeys.Count -eq 8) 'G1.SeedKeyCount' 'Seed value rows must yield the eight NavigationKey values.'
    $missing = @($seedKeys | Where-Object { -not $memberKeys.Contains($_) })
    $extra = @($memberKeys | Where-Object { -not $seedKeys.Contains($_) })
    Assert-Check ($missing.Count -eq 0) 'G1.SeedCovered' ('Seed NavigationKey values missing from group vocabulary: ' + ($missing -join ', '))
    Assert-Check ($extra.Count -eq 0) 'G1.NoUnknownMember' ('Group vocabulary contains non-seeded keys: ' + ($extra -join ', '))

    # <lang>
    #   <zh-CN>G2：配置键必须在常量类、设置 registry、All 清单与 SQL 种子四处登记；解析层须保留整份拒绝与回退语义。</zh-CN>
    #   <en>G2: the setting key must be registered in the constants class, settings registry, All list and SQL seed; the parser must retain whole-configuration rejection and fallback semantics.</en>
    # </lang>
    $keyText = Read-RepoText 'src/Portal/Components/PortalSettingKeys.cs'
    $registryText = Read-RepoText 'src/Portal/Components/PortalSettingsRegistry.cs'
    $settingsSeedText = Read-RepoText 'src/Setup/PortalCfg_SystemSettings.sql'
    $navigationSettingsText = Read-RepoText 'src/Portal/Components/PortalNavigationSettings.cs'

    Assert-Check ($keyText -match 'public const string NavigationGroupAliases\s*=\s*PortalNavigationGroupAlias\.ConfigurationKey') 'G2.SettingKeyConstant' 'PortalSettingKeys must expose NavigationGroupAliases bound to the alias vocabulary constant.'
    Assert-Check ($registryText -match 'PortalSettingDefinition NavigationGroupAliases\s*=') 'G2.SettingDefinition' 'PortalSettingsRegistry must declare the NavigationGroupAliases definition.'
    Assert-Check ($registryText -match 'NavigationAnonymousHiddenNavigationKeys,\s*\r?\n\s*NavigationGroupAliases') 'G2.SettingInAllList' 'The new setting definition must be present in the AllDefinitions list.'
    Assert-Check ($settingsSeedText -match "N'Portal\.Navigation\.GroupAliases'") 'G2.SettingSeed' 'PortalCfg_SystemSettings.sql must seed the default empty GroupAliases value idempotently.'
    Assert-Check ($navigationSettingsText -match 'public static string GroupAliases\(') 'G2.RuntimeReader' 'PortalNavigationSettings must expose the effective-value reader consumed by the renderer.'
    Assert-Check ($aliasText -match 'reason = "UnknownGroupKey"') 'G2.RejectUnknownGroup' 'The parser must reject unknown group keys fail-closed.'
    Assert-Check ($aliasText -match 'reason = "DuplicateNavigationKey"') 'G2.RejectDuplicateTab' 'The parser must reject duplicate tab keys fail-closed.'
    # <lang>
    #   <zh-CN>四个拒绝分支（畸形/空白/未知组/重复键）都必须把 aliases 置空；只检查"至少出现过一次"
    #   会让删掉任一分支的注入仍报绿（假绿），故按**次数**断言。</zh-CN>
    #   <en>All four rejection branches (malformed / blank / unknown group / duplicate key) must null out aliases;
    #   checking only "appeared at least once" would let an injection removing one branch stay green (false green), so the count is asserted.</en>
    # </lang>
    $nullAssignments = [regex]::Matches($aliasCode, 'aliases\s*=\s*null\s*;').Count
    Assert-Check ($nullAssignments -ge 4) 'G2.WholeConfigRejection' ('Every invalid-entry branch must reject the whole configuration; found ' + $nullAssignments + ' null assignments, expected at least 4.')
    Assert-Check ($aliasText -match 'degraded = !TryParseAliases' -and $aliasText -match '回退默认|fallback|defaults') 'G2.FallbackToDefaults' 'A failed parse must degrade back to the default vocabulary without hiding tabs.'

    # <lang>
    #   <zh-CN>G3：分组机制不得引用授权入口、不得暴露授权公开成员；`PortalCfg_Tabs` 建表脚本不得新增父子列。</zh-CN>
    #   <en>G3: the group mechanism must not reference authorization entry points or expose authorization members; the `PortalCfg_Tabs` create script must gain no parent/child column.</en>
    # </lang>
    $forbiddenAuth = @('IsInRoles', 'EnsurePermission', 'AccessRoles', 'PortalRoleNames', 'PortalPermissionKeys', 'HasPermission')
    $authHits = @($forbiddenAuth | Where-Object { $aliasCode -match [regex]::Escape($_) })
    Assert-Check ($authHits.Count -eq 0) 'G3.NoAuthorizationSymbols' ('Group alias code references authorization symbols: ' + ($authHits -join ', '))
    $publicMembers = [regex]::Matches($aliasText, '(?m)^\s*public\s+(?:static\s+)?(?:readonly\s+)?[A-Za-z0-9_<>,\[\]\. ]+\s+([A-Za-z0-9_]+)\s*(?:\(|\{|=>|=)')
    $authMemberHits = @($publicMembers | Where-Object {
        $n = $_.Groups[1].Value
        ($n -match 'Role|Permission|Authoriz|Access|Admin') -and ($n -notmatch 'Administration')
    } | ForEach-Object { $_.Groups[1].Value })
    Assert-Check ($authMemberHits.Count -eq 0) 'G3.NoAuthorizationMembers' ('Group alias source exposes authorization-like public members: ' + ($authMemberHits -join ', '))
    $createDbText = Read-RepoText 'src/Setup/Portal_CreateDB.sql'
    $tabsDdl = [regex]::Match($createDbText, '(?s)CREATE TABLE \[dbo\]\.\[PortalCfg_Tabs\](.*?)(?:GO|CREATE TABLE)')
    $tabsDdlText = if ($tabsDdl.Success) { $tabsDdl.Groups[1].Value } else { '' }
    Assert-Check ($tabsDdlText.Length -gt 0) 'G3.TabsDdlFound' 'PortalCfg_Tabs DDL must be readable to assert the no-parent-column boundary.'
    Assert-Check ($tabsDdlText -notmatch '(?i)ParentTab|ParentItemId|ParentTabId|ParentId') 'G3.NoParentColumn' 'PortalCfg_Tabs must not gain a parent/child column; grouping stays a configuration alias.'

    # <lang>
    #   <zh-CN>G4：别名条目必须存在、与可达页同组、可导航，且两份台账都标记为已覆盖并登记 entry/basis。</zh-CN>
    #   <en>G4: the alias entry must exist, share the group of a reachable page, be navigable, and both ledgers must mark the surface covered with an entry/basis.</en>
    # </lang>
    $navRegistryText = Read-RepoText 'src/Portal.Components/PortalNavigationRegistry.cs'
    $aliasEntry = [regex]::Match($navRegistryText, '(?s)"Admin\.Ops\.ThemeSettingsEntry",(.*?)\)\s*,')
    Assert-Check $aliasEntry.Success 'G4.AliasEntryExists' 'The theme settings grouped alias entry must be present in the navigation registry.'
    if ($aliasEntry.Success) {
        $body = $aliasEntry.Groups[1].Value
        Assert-Check ($body -match '"Admin/ThemeSettings\.aspx"') 'G4.AliasTarget' 'The alias must target Admin/ThemeSettings.aspx.'
        Assert-Check ($body -match 'PortalNavigationLifecycleState\.Active') 'G4.AliasActive' 'The alias must be Active.'
        Assert-Check ($body -match 'PortalNavigationVisibilityMode\.AdminOnly') 'G4.AliasAdminOnly' 'The alias must stay AdminOnly.'
        Assert-Check ($body -match 'PortalPermissionKeys\.ThemeView') 'G4.AliasThemeViewPermission' 'The alias must keep the ThemeView permission requirement.'
        Assert-Check ($body -match 'PortalRoleNames\.Administrators') 'G4.AliasAdministratorsRole' 'The alias must keep the Administrators role requirement.'
    }
    $policyText = Read-RepoText 'src/Portal.Components/PortalNavigationVisibilityPolicy.cs'
    Assert-Check ($policyText -match '"Admin\.SystemHealth",\s*GroupAdminOps') 'G4.ReachableHostGroup' 'Admin.SystemHealth must resolve to the reachable Admin.Ops group (prefix derivation makes the alias a same-group candidate).'
    Assert-Check ($navRegistryText -match 'PortalNavigationEntryRenderer\.RenderActions|RenderActions') 'G4.RendererReference' 'Registry comments must reference the existing RenderActions path.'

    # <lang>
    #   <zh-CN>台账按行内 JSON 对象文本取 ThemeSettings 一条；不依赖 JSON 层级结构，避免台账形状调整时门禁静默失效。</zh-CN>
    #   <en>Read the ThemeSettings row as an inline JSON object text rather than depending on JSON nesting, so a ledger shape change cannot silently disable the gate.</en>
    # </lang>
    $evidenceRaw = Read-RepoText 'dev/evidence-coverage.json'
    $themeLine = [regex]::Match($evidenceRaw, '\{[^{}]*"id"\s*:\s*"Admin/ThemeSettings\.aspx"[^{}]*\}')
    Assert-Check $themeLine.Success 'G4.EvidenceLedgerRow' 'dev/evidence-coverage.json must carry the ThemeSettings row.'
    if ($themeLine.Success) {
        Assert-Check ($themeLine.Value -match '"covered"\s*:\s*true') 'G4.EvidenceCovered' 'ThemeSettings must be marked covered=true after the alias lands.'
        Assert-Check ($themeLine.Value -match '"entry"\s*:') 'G4.EvidenceEntry' 'A covered row must carry an entry describing the inbound link.'
    }
    $mapRaw = Read-RepoText 'dev/gate-coverage-map.json'
    $mapLine = [regex]::Match($mapRaw, '(?s)\{\s*"id"\s*:\s*"theme-settings-entry".*?\}')
    Assert-Check $mapLine.Success 'G4.MapRow' 'dev/gate-coverage-map.json must carry the theme-settings-entry surface.'
    if ($mapLine.Success) {
        Assert-Check ($mapLine.Value -match '"coverage"\s*:\s*"(full|partial)"') 'G4.MapCovered' 'The theme-settings-entry surface must no longer be uncovered.'
        Assert-Check ($mapLine.Value -match 'Admin\.Ops\.ThemeSettingsEntry') 'G4.MapBasis' 'The map basis must name the concrete alias entry that produces the inbound link.'
    }
}
catch {
    # <lang>
    #   <zh-CN>任何读取/解析异常都视为契约无法证明，一律判红，不做静默降级。</zh-CN>
    #   <en>Any read/parse exception means the contract cannot be proven; fail instead of silently degrading.</en>
    # </lang>
    $failures.Add('G0.Precondition — ' + $_.Exception.Message)
    Write-Host ('  [Fail] G0.Precondition — ' + $_.Exception.Message) -ForegroundColor Red
}

Write-Host ''
Write-Host '=== 主导航分组配置别名契约门禁 ==='
foreach ($check in $checks) { Write-Host ('  ' + $check) }
Write-Host ("断言 {0} 项｜失败 {1} 项" -f ($checks.Count + $failures.Count), $failures.Count)

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    Write-Host 'RESULT: Fail' -ForegroundColor Red
    exit 1
}

Write-Host 'RESULT: Pass' -ForegroundColor Green
exit 0
