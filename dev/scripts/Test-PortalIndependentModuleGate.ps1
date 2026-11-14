<#
.SYNOPSIS
<lang>
  <zh-CN>W-anp-P287 独立模块契约一致性 L0 门禁：静态校验 manifest v2 契约、部署 Profile 允许集、Admins 边界、卸载一致性与 6 套皮肤主题作用域。</zh-CN>
  <en>W-anp-P287 independent-module contract L0 gate: statically validates manifest v2 contracts, deployment-profile allow-lists, the Admins boundary, uninstall symmetry and six-skin theme scope.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>本门禁防的是一类**构建与单测都发现不了、只能靠浏览器级 L2 才暴露**的缺陷：独立模块的 manifest 或部署配置与
  「可信目录 + Profile 形态感知」装载契约不一致，导致包永远进不了可信集合、模块静默不渲染，而静态检查却报绿。
  W-anp-P287 的 L2 运行期核验实测发现三处（schemaVersion 写成字符串 "2.0"、resources 写成全路径、capability 写成顶层字段），
  加上活动 Profile 由 Config/appSettings.{env}.json 覆盖而允许集只写在 Web.config，共四类根因；本门禁把它们固化为
  **从权威事实源推导**的机器校验，使同类偏差今后在 L0 即被拦住。

  契约口径与运行时 src/Portal/Components/PortalModulePackage.cs 的 TryReadPackage 严格对齐：
    C1 schemaVersion 必须是 JSON 整数 1 或 2（字符串 "2.0" 不被接受）；
    C2 capability 必须是含 capabilityId + role 的对象，能力键在 PortalCapabilityRegistry 词表、角色在封闭五值词表；
       顶层 capabilityId/capabilityRole 属旧写法，必须拒绝；
    C3 resources 必须是包内相对路径、逐段无逃逸、扩展名在 allowlist、物理文件存在；不得写成 DesktopModules/... 全路径；
    C4 desktopEntry 必须与包目录一致且入口文件存在；禁止脚本/外部 URL/程序集/包 URL 字段；moduleKind 只能是 Core/Independent；
    P1 每个活动 Profile（Web.config 基础值 + Config/appSettings.json + 每个环境模板各自的 Active 值）的 Packages 允许集
       必须**显式列出**所有 moduleKind=Independent 的包；通配符 '*' 不覆盖 Independent（与 IsPackageAllowed(PortalModulePackage) 同源）；
    A1 Admins 边界：PortalModuleProvisioner 必须含能力词表 fail-closed 校验与管理级前缀拒绝，能力词表中独立模块权限键前缀不得为管理级；
    U1 卸载一致性：Deprovision 脚本必须清理 Provision 脚本写入的每一张 PortalCfg_* 表（Tabs / ModuleDefinitions / Modules / ModulePackageStates）；
    T1 6 套正式皮肤 Default.css 必须含 body.portal-theme-<名> .form-builder 作用域规则。

  证明边界：只证明**静态契约自洽**——manifest 与运行时解析器同口径、Profile 允许集覆盖 Independent 包、Admins 边界断言存在、
  卸载脚本表覆盖对称、主题层规则存在；**不证明**模块在站点上真的渲染、不证明提交落库/审计、不证明运行期授权判定正确、
  不证明主题视觉正确（这些分别由 Test-PortalIndependentModuleRuntimeEvidence.mjs 的 L2 证据、Portal.Tests 与截图承载）。
  Proof boundary: proves only **static contract self-consistency** — manifests share the runtime parser's rules, profile allow-lists
  cover Independent packages, Admins-boundary assertions exist, uninstall table coverage is symmetric, theme-scope rules exist;
  it does **not** prove the module actually renders, that submissions persist or are audited, that runtime authorization is correct,
  or that themes look right (those belong to the L2 evidence script, Portal.Tests and screenshots respectively).

  归 L0：只读源码、配置与 CSS，不连数据库、不启动站点、不写任何文件。</zh-CN>
  <en>This gate prevents a defect class that neither builds nor unit tests can catch and that only browser-level L2 exposes:
  an independent module's manifest or deployment configuration disagreeing with the "trusted catalog + profile shape awareness"
  loading contract, so the package never enters the trusted set and silently never renders while static checks stay green.
  W-anp-P287 L2 runtime verification measured three such cases (schemaVersion as the string "2.0", resources as a full path,
  capability as top-level fields) plus a fourth root cause where the active profile is overridden by Config/appSettings.{env}.json
  while the allow-list was written only into Web.config. This gate turns them into machine checks **derived from authoritative
  sources** so the same drift is caught at L0 from now on.

  Contract rules mirror TryReadPackage in src/Portal/Components/PortalModulePackage.cs:
    C1 schemaVersion must be a JSON integer 1 or 2 (the string "2.0" is rejected);
    C2 capability must be an object with capabilityId + role, the capability key registered in PortalCapabilityRegistry and the
       role in the closed five-value vocabulary; top-level capabilityId/capabilityRole is the legacy shape and is rejected;
    C3 resources must be package-relative, escape-free per segment, allow-listed by extension and physically present;
       DesktopModules/... full paths are rejected;
    C4 desktopEntry must stay inside the package directory and its file must exist; script/external URL/assembly/package URL
       fields are prohibited; moduleKind may only be Core or Independent;
    P1 every active profile (Web.config base value plus Config/appSettings.json plus each environment template's own Active value)
       must **explicitly list** every moduleKind=Independent package; the '*' wildcard never covers Independent
       (same semantics as IsPackageAllowed(PortalModulePackage));
    A1 Admins boundary: PortalModuleProvisioner must carry the capability-registry fail-closed check and administrative-prefix
       rejection, and the independent module's capability permission-key prefix must not be administrative;
    U1 uninstall symmetry: the Deprovision script must clear every PortalCfg_* table written by the Provision script
       (Tabs / ModuleDefinitions / Modules / ModulePackageStates);
    T1 all six official skins' Default.css must contain a body.portal-theme-<name> .form-builder scope rule.

  Classified L0: reads only source, configuration and CSS — no database, no site start-up, no file writes.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>门禁自身元数据，供套件统计与注入证明索引按名定位。</zh-CN>
#   <en>Gate metadata used by suite statistics and the injection-proof index.</en>
# </lang>
$Gate = 'Test-PortalIndependentModuleGate.ps1'
$Layer = 'L0'

# <lang>
#   <zh-CN>统一无 BOM UTF-8，避免中文 manifest / 配置在默认编码下被读坏而误判。</zh-CN>
#   <en>Use UTF-8 without BOM so Chinese manifests/configuration are not corrupted into false verdicts.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)
function Read-Text([string] $path) { return [System.IO.File]::ReadAllText($path, $utf8) }

$repoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$failures = New-Object 'System.Collections.Generic.List[string]'
$passes = 0
function Check([string] $name, [bool] $ok, [string] $detail) {
    if ($ok) { $script:passes++; Write-Output "PASS  $name" }
    else { $failures.Add("FAIL  $name :: $detail"); Write-Output "FAIL  $name :: $detail" }
}

$modulesRoot = Join-Path $repoRoot 'src/Portal/DesktopModules'
$manifests = @(Get-ChildItem -LiteralPath $modulesRoot -Filter 'module.json' -Recurse -File | Sort-Object FullName)
Check 'G0.ManifestsDiscovered' ($manifests.Count -ge 1) "module.json count=$($manifests.Count)"

# <lang>
#   <zh-CN>能力词表与角色词表均从源码事实推导，不在门禁里复制一份硬编码清单（避免词表新增能力后门禁口径过期）。</zh-CN>
#   <en>Capability keys and roles are derived from source facts rather than duplicated as a hard-coded list in the gate,
#   so the gate's scope does not go stale when the registry gains a capability.</en>
# </lang>
$registryPath = Join-Path $repoRoot 'src/Portal.Components/PortalCapabilityRegistry.cs'
$registryText = Read-Text $registryPath
$capabilityKeys = [System.Text.RegularExpressions.Regex]::Matches(
    $registryText, '"(BasicBusiness\.[A-Za-z0-9_.]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$validRoles = @('Primary', 'Contributor', 'Projection', 'Adapter', 'Block')
$independentPackageIds = New-Object 'System.Collections.Generic.List[string]'

foreach ($manifestFile in $manifests) {
    $dir = $manifestFile.Directory.Name
    $prefix = "$dir/module.json"
    $raw = Read-Text $manifestFile.FullName
    try { $json = $raw | ConvertFrom-Json } catch { Check "$prefix.JsonParsable" $false 'invalid JSON'; continue }
    Check "$prefix.JsonParsable" $true ''

    # <lang>
    #   <zh-CN>C1：必须用正则判定 schemaVersion 是 JSON 数字字面量而非字符串；ConvertFrom-Json 会把 "2.0" 与 2 都变成数值，无法区分，故直接读原始文本。</zh-CN>
    #   <en>C1: a regex on the raw text decides whether schemaVersion is a JSON number literal rather than a string;
    #   ConvertFrom-Json coerces both "2.0" and 2 to a numeric value and cannot tell them apart.</en>
    # </lang>
    $schemaMatch = [regex]::Match($raw, '"schemaVersion"\s*:\s*("[^"]*"|[0-9]+)')
    $schemaOk = $schemaMatch.Success -and $schemaMatch.Groups[1].Value -notmatch '^"'
    $schemaOk = $schemaOk -and $json.schemaVersion -in @(1, 2)
    Check "$prefix.C1.SchemaVersionInteger" $schemaOk "schemaVersion must be JSON integer 1 or 2; raw=$($schemaMatch.Value)"

    # <lang>
    #   <zh-CN>C2：capability 必须是对象；顶层 capabilityId/capabilityRole 是导致首证失败的旧写法，显式拒绝以阻断回归。</zh-CN>
    #   <en>C2: capability must be an object; top-level capabilityId/capabilityRole was the legacy shape that broke the
    #   first-evidence package, so reject it explicitly to prevent regression.</en>
    # </lang>
    # <lang>
    #   <zh-CN>capability 段是可选的（未声明时运行时保持 Unclassified）；StrictMode 下先判属性存在性，且声明后必须完整合法。</zh-CN>
    #   <en>The capability section is optional (the runtime keeps Unclassified when absent); check property existence first
    #   under StrictMode, and require a fully valid declaration whenever present.</en>
    # </lang>
    $hasLegacyCapability = ($null -ne $json.PSObject.Properties['capabilityId']) -or
                           ($null -ne $json.PSObject.Properties['capabilityRole'])
    $capProperty = $json.PSObject.Properties['capability']
    $capObj = if ($null -eq $capProperty) { $null } else { $capProperty.Value }
    $capShapeOk = -not $hasLegacyCapability
    if ($null -ne $capObj) {
        # <lang>
        #   <zh-CN>声明了 capability 段就必须同时给出合法键与角色；同样先判属性存在性，避免 StrictMode 抛错被误读成门禁缺陷。</zh-CN>
        #   <en>Once a capability section is declared it must carry both a valid key and a valid role; check property
        #   existence first so a StrictMode error is not misread as a gate defect.</en>
        # </lang>
        $capIdProperty = $capObj.PSObject.Properties['capabilityId']
        $roleProperty = $capObj.PSObject.Properties['role']
        $capId = if ($null -eq $capIdProperty) { '' } else { [string]$capIdProperty.Value }
        $role = if ($null -eq $roleProperty) { '' } else { [string]$roleProperty.Value }
        $capShapeOk = $capShapeOk -and
                      (-not [string]::IsNullOrWhiteSpace($capId)) -and
                      ($capabilityKeys -contains $capId) -and
                      ($validRoles -contains $role)
    }
    Check "$prefix.C2.CapabilityObjectAndRegistry" $capShapeOk 'capability must be {capabilityId,role}, registered in PortalCapabilityRegistry with a valid role; top-level capabilityId/capabilityRole is prohibited'

    # <lang>
    #   <zh-CN>C3：运行时把 resources 当作包内相对路径拼到包根；写成 DesktopModules/<dir>/... 会拼出不存在的物理文件而整包被拒。</zh-CN>
    #   <en>C3: the runtime joins resources onto the package root as package-relative paths; writing DesktopModules/<dir>/...
    #   produces a non-existent physical file and rejects the whole package.</en>
    # </lang>
    $resources = @($json.resources)
    $resourceOk = $true
    $resourceDetail = ''
    foreach ($resource in $resources) {
        $r = [string]$resource
        $segments = $r.Replace('\', '/').Split('/')
        $relativeOk = -not [string]::IsNullOrWhiteSpace($r) -and
                      -not $r.StartsWith('/') -and
                      ($r -notmatch '://') -and
                      -not $r.StartsWith('//') -and
                      -not $r.StartsWith('DesktopModules/') -and
                      -not (@($segments | Where-Object { $_ -eq '' -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0)
        $extension = [System.IO.Path]::GetExtension($r)
        $extensionOk = $extension -in @('.css', '.png', '.jpg', '.jpeg', '.gif', '.webp')
        $existsOk = $relativeOk -and (Test-Path -LiteralPath (Join-Path $manifestFile.Directory.FullName ($r.Replace('/', [System.IO.Path]::DirectorySeparatorChar))))
        if (-not ($relativeOk -and $extensionOk -and $existsOk)) {
            $resourceOk = $false
            $resourceDetail = "resource '$r' relative=$relativeOk extension=$extensionOk exists=$existsOk"
        }
    }
    Check "$prefix.C3.ResourcesPackageRelative" $resourceOk $resourceDetail

    # <lang>
    #   <zh-CN>C4：入口必须留在本包目录内且文件真实存在；禁止字段与运行时黑名单一致；moduleKind 仅两值。</zh-CN>
    #   <en>C4: the entry must remain inside this package directory and physically exist; prohibited fields mirror the runtime
    #   blacklist; moduleKind accepts only two values.</en>
    # </lang>
    $entry = [string]$json.desktopEntry
    $entryOk = $entry.StartsWith("DesktopModules/$dir/", [System.StringComparison]::OrdinalIgnoreCase) -and
               (Test-Path -LiteralPath (Join-Path $repoRoot ('src/Portal/' + $entry.Replace('/', [System.IO.Path]::DirectorySeparatorChar))))
    Check "$prefix.C4.DesktopEntryInPackage" $entryOk "desktopEntry='$entry' must be under DesktopModules/$dir/ and exist"

    $prohibited = @('script', 'scripts', 'externalUrl', 'externalUrls', 'assembly', 'assemblies', 'packageUrl')
    $prohibitedHits = @($prohibited | Where-Object { $null -ne $json.PSObject.Properties[$_] })
    Check "$prefix.C4.NoProhibitedFields" ($prohibitedHits.Count -eq 0) "prohibited manifest fields: $($prohibitedHits -join ',')"

    # <lang>
    #   <zh-CN>moduleKind 是可选字段（缺省 Core）；StrictMode 下直接取属性会在未声明时抛错，故先判断属性是否存在。</zh-CN>
    #   <en>moduleKind is optional (defaults to Core); under StrictMode reading a missing property throws, so check existence first.</en>
    # </lang>
    $kindProperty = $json.PSObject.Properties['moduleKind']
    $kind = if ($null -eq $kindProperty) { '' } else { [string]$kindProperty.Value }
    $kindOk = [string]::IsNullOrWhiteSpace($kind) -or $kind -in @('Core', 'Independent')
    Check "$prefix.C4.ModuleKind" $kindOk "moduleKind='$kind' must be Core or Independent"
    if ($kind -eq 'Independent') { $independentPackageIds.Add([string]$json.packageId) }
}

# <lang>
#   <zh-CN>P1：活动 Profile 必须以「基础配置 + 每个环境覆盖」的事实源逐一推导；任一活动 Profile 未显式列出 Independent 包即失败。
#   这正是 W-anp-P287 L2 实测的第四类根因（只在 Web.config 的 CoreOnly 允许集登记，而 dev 活动 Profile 是 BusinessWorkflow）。</zh-CN>
#   <en>P1: derive every active profile from the "base config + per-environment override" fact sources; fail when any active
#   profile does not explicitly list an Independent package. This is exactly the fourth root cause measured by W-anp-P287 L2
#   (the allow-list lived only in Web.config's CoreOnly while dev's active profile was BusinessWorkflow).</en>
# </lang>
$configSources = New-Object 'System.Collections.Generic.List[object]'
$webConfigText = Read-Text (Join-Path $repoRoot 'src/Portal/Web.config')
$configSources.Add([ordered]@{
    name = 'Web.config'
    active = ([regex]::Match($webConfigText, 'Portal\.ModuleProfiles\.Active"\s+value="([^"]*)"')).Groups[1].Value
    text = $webConfigText
    kind = 'xml'
})
foreach ($configFile in @('appSettings.json', 'appSettings.dev.template.json', 'appSettings.test.template.json', 'appSettings.prod.template.json')) {
    $path = Join-Path $repoRoot "src/Portal/Config/$configFile"
    if (Test-Path -LiteralPath $path) {
        $text = Read-Text $path
        $configSources.Add([ordered]@{
            name = $configFile
            active = ([regex]::Match($text, '"Portal\.ModuleProfiles\.Active"\s*:\s*"([^"]*)"')).Groups[1].Value
            text = $text
            kind = 'json'
        })
    }
}

# <lang>
#   <zh-CN>合并允许集：环境模板未声明某 Profile 的 Packages 时，回退到基础 appSettings.json / Web.config 的同名键，这与启动时 AppSettingsLoader 的「基础 + 覆盖」语义一致。</zh-CN>
#   <en>Merge allow-lists: when an environment template does not declare a profile's Packages, fall back to the same key in
#   base appSettings.json / Web.config, matching AppSettingsLoader's "base plus override" semantics at start-up.</en>
# </lang>
$baseJsonText = (Read-Text (Join-Path $repoRoot 'src/Portal/Config/appSettings.json'))
function Get-PackagesValue([string] $text, [string] $kind, [string] $profileName) {
    if ($kind -eq 'xml') {
        $m = [regex]::Match($text, 'Portal\.ModuleProfiles\.' + [regex]::Escape($profileName) + '\.Packages"\s+value="([^"]*)"')
    }
    else {
        $m = [regex]::Match($text, '"Portal\.ModuleProfiles\.' + [regex]::Escape($profileName) + '\.Packages"\s*:\s*"([^"]*)"')
    }
    if ($m.Success) { return $m.Groups[1].Value }
    $fallback = [regex]::Match($baseJsonText, '"Portal\.ModuleProfiles\.' + [regex]::Escape($profileName) + '\.Packages"\s*:\s*"([^"]*)"')
    if ($fallback.Success) { return $fallback.Groups[1].Value }
    return ''
}

$activeProfiles = @($configSources | ForEach-Object { $_.active } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
Check 'P0.ActiveProfilesDiscovered' ($activeProfiles.Count -ge 1) "active profiles found=$($activeProfiles.Count)"

# <lang>
#   <zh-CN>逐源校验「任一可能成为活动态的 Profile 允许集声明」：运行时是「基础 + 环境覆盖」两层，
#   只检查最上层生效值会让基础配置里的漂移被遮蔽（注入证明实测的 not-detected 假绿）。因此每个配置源中
#   声明的、Profile 名属于活动集合的 Packages 都必须显式列出 Independent 包；随后再单独校验每个源自己
#   Active 的生效允许集（含回退），两层断言互补。</zh-CN>
#   <en>Validate every potentially-active profile allow-list declaration source by source: at run time the configuration is
#   "base plus environment override", so checking only the effective top-layer value hides drift in the base layer (the
#   not-detected false green measured by the injection proof). Hence any Packages declaration in any config source whose
#   profile name is in the active set must explicitly list Independent packages; each source's own effective Active
#   allow-list (including fallback) is then checked separately. The two assertion layers complement each other.</en>
# </lang>
foreach ($source in $configSources) {
    $pattern = if ($source.kind -eq 'xml') {
        'Portal\.ModuleProfiles\.([A-Za-z0-9_-]+)\.Packages"\s+value="([^"]*)"'
    }
    else {
        '"Portal\.ModuleProfiles\.([A-Za-z0-9_-]+)\.Packages"\s*:\s*"([^"]*)"'
    }
    foreach ($match in [regex]::Matches($source.text, $pattern)) {
        $declaredProfile = $match.Groups[1].Value
        $declaredValue = $match.Groups[2].Value
        if ($activeProfiles -notcontains $declaredProfile) { continue }
        $declaredExplicit = @($declaredValue.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' -and $_ -ne '*' })
        foreach ($packageId in $independentPackageIds) {
            Check "P1.$($source.name).$declaredProfile.Declares.$packageId" ($declaredExplicit -contains $packageId) "config source '$($source.name)' declares profile '$declaredProfile' packages='$declaredValue' without explicitly listing Independent package '$packageId'"
        }
    }

    # <lang>
    #   <zh-CN>每个源自身 Active 的生效允许集仍需显式列出（Independent 不被 '*' 通配覆盖，与 IsPackageAllowed 同源）。</zh-CN>
    #   <en>Each source's own effective Active allow-list must still list Independent packages explicitly
    #   (the '*' wildcard never covers Independent, same source as IsPackageAllowed).</en>
    # </lang>
    $profile = $source.active
    if ([string]::IsNullOrWhiteSpace($profile)) { continue }
    $packagesValue = Get-PackagesValue $source.text $source.kind $profile
    $explicit = @($packagesValue.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' -and $_ -ne '*' })
    foreach ($packageId in $independentPackageIds) {
        Check "P1.$($source.name).Allows.$packageId" ($explicit -contains $packageId) "active profile '$profile' packages='$packagesValue' must explicitly list Independent package '$packageId'"
    }
}

# <lang>
#   <zh-CN>A1：Admins 边界的静态断言存在性检查（不执行代码、不证明运行期判定）。</zh-CN>
#   <en>A1: existence checks for the Admins-boundary static assertions (does not execute code or prove runtime decisions).</en>
# </lang>
$provisionerText = Read-Text (Join-Path $repoRoot 'src/Portal.Components.Data1/PortalModuleProvisioner.cs')
$adminChecks = [ordered]@{
    'FailClosedRegistry' = $provisionerText.Contains('PortalCapabilityRegistry.TryGet') -and $provisionerText.Contains('return false;')
    'AdminPrefixReject' = $provisionerText.Contains('IsAdminCapability') -and $provisionerText.Contains('Business.Admin')
    'CapabilityIdContract' = $provisionerText.Contains('request.CapabilityId')
}
foreach ($key in $adminChecks.Keys) { Check "A1.$key" ([bool]$adminChecks[$key]) "PortalModuleProvisioner must carry the Admins-boundary assertion: $key" }
# <lang>
#   <zh-CN>A1（续）：FormBuilder 能力前缀的硬边界是「绝不为 Admins/Admin」，而非某个固定字面量。
#     注册表自 W-anp-P325 起引用 PortalPermissionKeys.FormBuilderKeyPrefix（单一事实源；keyless 空串是显式合法定义，
#     与 Test-PortalGovernanceView.ps1 的解析口径一致）。旧断言硬编码 "Business.FormBuilder" 字面量，
#     在该改造后已滞后于产品事实而恒判失败；此处改为解析常量取值，并同时校验单一事实源引用与管理级前缀拒绝，
#     断言名与「第三方独立模块不得获 Admins」的 A 级硬边界语义不变，且比原字面量检查更强。</zh-CN>
#   <en>A1 (cont.): the FormBuilder capability-prefix hard boundary is "never Admins/Admin", not a fixed literal.
#     Since W-anp-P325 the registry references PortalPermissionKeys.FormBuilderKeyPrefix (single source of truth;
#     a keyless empty string is an explicitly valid definition, matching Test-PortalGovernanceView.ps1 parsing).
#     The old assertion hard-coded the "Business.FormBuilder" literal and went stale after that change, failing forever.
#     It now resolves the constant value and checks both the single-source reference and admin-prefix rejection;
#     the assertion name and the A-level hard boundary ("independent modules never obtain Admins") are unchanged,
#     and this is strictly stronger than the literal check.</en>
# </lang>
$permissionKeysText = Read-Text (Join-Path $repoRoot 'src/Portal.Components/PortalPermissions.cs')
$formBuilderPrefixMatch = [regex]::Match($permissionKeysText, 'public\s+const\s+string\s+FormBuilderKeyPrefix\s*=\s*"([^"]*)"')
$formBuilderPrefixValue = if ($formBuilderPrefixMatch.Success) { $formBuilderPrefixMatch.Groups[1].Value } else { $null }
$formBuilderUsesSingleSource = $registryText -match '"HIA\.FormBuilder"\s*,\s*PortalPermissionKeys\.FormBuilderKeyPrefix'
$adminPrefixPattern = '(?i)(^|\.)Admins?(\.|$)'
$formBuilderPrefixOk = $formBuilderPrefixMatch.Success -and $formBuilderUsesSingleSource -and
    ($formBuilderPrefixValue -eq '' -or $formBuilderPrefixValue -notmatch $adminPrefixPattern)
Check 'A1.CapabilityPrefixNotAdmin' ([bool]$formBuilderPrefixOk) 'FormBuilder capability must reference PortalPermissionKeys.FormBuilderKeyPrefix (single source of truth) and its value must never be an Admins/Admin prefix; keyless empty string is valid'

# <lang>
#   <zh-CN>U1：卸载脚本必须清理安装脚本写入的每一张表；逐表对称，避免「安装留下孤儿行」。</zh-CN>
#   <en>U1: the deprovision script must clear every table written by the provision script, table by table, to avoid orphan rows.</en>
# </lang>
$provisionSql = Read-Text (Join-Path $repoRoot 'src/Setup/PortalCfg_FormBuilder_Provision.sql')
$deprovisionSql = Read-Text (Join-Path $repoRoot 'src/Setup/PortalCfg_FormBuilder_Deprovision.sql')
foreach ($table in @('PortalCfg_Tabs', 'PortalCfg_ModuleDefinitions', 'PortalCfg_Modules', 'PortalCfg_ModulePackageStates')) {
    $written = $provisionSql.Contains($table)
    $cleared = $deprovisionSql.Contains($table)
    Check "U1.$table.Symmetric" ($written -and $cleared) "provision writes=$written / deprovision clears=$cleared"
}

# <lang>
#   <zh-CN>T1：主题作用域按已登记的 6 套正式皮肤逐一核对，规则来自项目主题类体系。</zh-CN>
#   <en>T1: verify theme scope against each of the six registered official skins, using the project's theme class system.</en>
# </lang>
$skinThemes = [ordered]@{
    'enterpriselight' = 'EnterpriseLight'; 'enterprisedark' = 'EnterpriseDark'
    'oalight' = 'OaLight'; 'oadark' = 'OaDark'
    'stateclassiclight' = 'StateClassicLight'; 'stateclassicdark' = 'StateClassicDark'
}
foreach ($themeClass in $skinThemes.Keys) {
    $cssPath = Join-Path $repoRoot "src/Portal/App_Themes/$($skinThemes[$themeClass])/Default.css"
    $cssOk = (Test-Path -LiteralPath $cssPath) -and ((Read-Text $cssPath).Contains("body.portal-theme-$themeClass .form-builder {"))
    Check "T1.ThemeScope.$themeClass" $cssOk "skin $($skinThemes[$themeClass]) missing body.portal-theme-$themeClass .form-builder scope rule"
}

Write-Output ''
Write-Output ("SUMMARY gate=$Gate layer=$Layer pass=$passes fail=$($failures.Count) independentPackages=$($independentPackageIds.Count) activeProfiles=$($activeProfiles -join ',')")
if ($failures.Count -gt 0) { exit 1 }
exit 0
