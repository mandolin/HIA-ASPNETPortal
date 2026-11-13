<#
.SYNOPSIS
    R6 治理视图 L0 门禁：能力↔模块↔入口↔权限键 治理视图对账。

.DESCRIPTION
    单一事实源（只读对账，不改写任何产品文件）：
      - 能力：src/Portal.Components/PortalCapabilityRegistry.cs
              （public const string X = "BasicBusiness.XXX" 给出能力键；
               DefinitionArray 给出每条的 PrimaryModuleId 与 PermissionKeyPrefix）
      - 模块：src/Portal/DesktopModules/*/module.json
              （capability.capabilityId / capability.role / packageId）
      - 权限键：src/Portal.Components/PortalPermissions.cs
              （PortalPermissionKeys 的 public const string 键名）
      - 入口：src/Portal.Components/PortalNavigationRegistry.cs
              （new PortalNavigationEntry(...) 内 DesktopModules/<folder>/<file>.ascx 绑定）

    检查项：
      C1 模块声明的能力必须能在能力注册表解析（否则模块非法声明能力，P46 契约硬失败）
      C2 每个能力应有主责模块实现（孤儿能力 → L0 硬失败）
      C3 每个能力的 PermissionKeyPrefix 应在 PortalPermissionKeys 有对应键族（权限键漂移 → L0 硬失败）
      C4 导航 DesktopModule 入口引用的模块目录应存在（悬空入口 → L0 硬失败）
      C5 能力键 / 模块包标识 / 权限键 命名形态与层词表（命名漂移 → L0 硬失败）
      C6 入口双源交叉对账：SQL 导航种子（完整用户级页签/模块挂载源）与代码导航注册表（需运行期元数据的精选入口源）显式承认双源；代码声明的 DesktopModule 入口必须在数据库（种子 ∪ 部署增量）有对应挂载（含部署态增量白名单，FormBuilder 等由 PortalCfg_FormBuilder_Provision.sql 注入）→ L0 硬失败

    退出码：
      0  = 通过（无硬失败）
      1  = 硬失败（C1/C2/C3/C4/C5 任一不通过，或结构性自检失败）—— 自 W-anp-P325 起治理视图整体升 L0 常驻硬断言
      2  = -Strict 模式下存在告警（自 W-anp-P325 起 C2–C5 已升 FAIL，此分支通常不再触发，保留以兼容历史调用）
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [switch]$Strict
)

$ErrorActionPreference = 'Stop'

$capFile  = Join-Path $RepoRoot 'src/Portal.Components/PortalCapabilityRegistry.cs'
$permFile = Join-Path $RepoRoot 'src/Portal.Components/PortalPermissions.cs'
$navFile  = Join-Path $RepoRoot 'src/Portal.Components/PortalNavigationRegistry.cs'
$modRoot  = Join-Path $RepoRoot 'src/Portal/DesktopModules'

$fail = 0
$warn = 0

# 注：自 W-anp-P325 起，治理视图整体升为 L0 常驻硬断言——C2–C5 的告警（WARN）全部升级为硬失败（FAIL），
# 任何能力/模块/入口/权限键契约漂移都会让脚本以 exit 1 阻断；-Strict 的告警分支因此不再触发（保留以兼容历史调用）。
function Emit($sev, $msg) {
    if ($sev -eq 'FAIL') { Write-Host "  [FAIL] $msg" -ForegroundColor Red; $script:fail++ }
    elseif ($sev -eq 'WARN') { Write-Host "  [WARN] $msg" -ForegroundColor Yellow; $script:warn++ }
    else { Write-Host "  [ OK ] $msg" -ForegroundColor Green }
}

Write-Host '' -NoNewline
Write-Host '=== R6 治理视图 L0 门禁（能力↔模块↔入口↔权限键）===' -ForegroundColor Cyan

# ---- 解析能力注册表 ----
if (-not (Test-Path $capFile)) { Write-Host "能力注册表缺失: $capFile" -ForegroundColor Red; exit 1 }
$capText = Get-Content $capFile -Raw -Encoding UTF8

$capConst = @{}
foreach ($m in [regex]::Matches($capText, 'public const string (\w+) = "([^"]*)"')) {
    $capConst[$m.Groups[1].Value] = $m.Groups[2].Value
}
# 权限键常量表：能力前缀可引用其中的常量（如 PortalPermissions.BusinessCollaborationKeyPrefix），门禁须能解析其取值。
# 允许空串（keyless 能力 FormBuilderKeyPrefix = "" 是合法定义）。
$permConst = @{}
if (Test-Path $permFile) {
    $permText = Get-Content $permFile -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($permText, 'public const string (\w+) = "([^"]*)"')) {
        $permConst[$m.Groups[1].Value] = $m.Groups[2].Value
    }
}
$capIds = $capConst.Values | Where-Object { $_ -match '^BasicBusiness\.\w+$' } | Sort-Object -Unique

$caps = @()
# 能力权限前缀可写为带引号字面量，或引用 PortalPermissions 常量（如 PortalPermissions.BusinessCollaborationKeyPrefix）。
# 后者是 W-anp-P325 Q2-A 的「单一事实源」形态；门禁须解析常量引用，而非只认字面量，否则会误判为孤儿能力。
foreach ($m in [regex]::Matches($capText, 'new PortalCapabilityDefinition\(\s*(\w+),\s*"([^"]*)",\s*\w+\.[A-Za-z]+,\s*"([^"]*)",\s*("([^"]*)"|((?:[\w]+\.)?[\w]+)),')) {
    $id = $capConst[$m.Groups[1].Value]
    if (-not $id) { continue }
    $prefixRaw = if ($m.Groups[4].Success) { $m.Groups[4].Value } else { $m.Groups[5].Value }
    if ($prefixRaw -match '^".*"$') {
        $prefixVal = $prefixRaw.Trim('"')
    }
    else {
        # 常量引用：去掉 PortalPermissions. 前缀后在常量表中查值；查不到则保留原标识符（C3 会据此报漂移）
        $name = $prefixRaw -replace '^.*\.', ''
        if ($permConst.ContainsKey($name)) { $prefixVal = $permConst[$name] }
        elseif ($capConst.ContainsKey($name)) { $prefixVal = $capConst[$name] }
        else { $prefixVal = $prefixRaw }
    }
    $caps += [PSCustomObject]@{
        Id = $id
        Layer = $m.Groups[2].Value
        PrimaryModuleId = $m.Groups[3].Value
        PermissionPrefix = $prefixVal
    }
}
if ($caps.Count -eq 0) {
    foreach ($id in $capIds) {
        $caps += [PSCustomObject]@{ Id = $id; Layer = ''; PrimaryModuleId = ''; PermissionPrefix = '' }
    }
}
Write-Host ("能力注册表：{0} 条能力定义，{1} 个能力键" -f $caps.Count, $capIds.Count)

# ---- 解析模块 ----
$modules = @()
if (Test-Path $modRoot) {
    foreach ($mj in Get-ChildItem $modRoot -Recurse -Filter module.json) {
        try { $j = Get-Content $mj.FullName -Raw -Encoding UTF8 | ConvertFrom-Json }
        catch { Emit 'FAIL' ("模块清单解析失败: {0}" -f $mj.FullName); continue }
        $cap = if ($j.capability) { $j.capability.capabilityId } else { $null }
        $modules += [PSCustomObject]@{
            Folder = $mj.Directory.Name
            PackageId = $j.packageId
            CapabilityId = $cap
            Role = if ($j.capability) { $j.capability.role } else { $null }
        }
    }
}
Write-Host ("模块：{0} 个（其中声明能力的 {1} 个）" -f $modules.Count, ($modules | Where-Object { $_.CapabilityId }).Count)

# 结构性自检：避免「静默通过」
if ($caps.Count -eq 0 -or $modules.Count -eq 0) {
    Write-Host "结构性失败：解析到能力数=$($caps.Count) 或 模块数=$($modules.Count) 为 0，门禁无法可靠运行。" -ForegroundColor Red
    exit 1
}

# ---- 解析权限键 ----
$permKeys = @()
if (Test-Path $permFile) {
    $permText = Get-Content $permFile -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($permText, 'public const string \w+ = "([^"]+)"')) {
        $permKeys += $m.Groups[1].Value
    }
}

# ---- 解析导航入口 ----
$navFolders = @()
if (Test-Path $navFile) {
    $navText = Get-Content $navFile -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($navText, 'DesktopModules/([A-Za-z0-9_]+)/')) {
        $navFolders += $m.Groups[1].Value
    }
    $navFolders = $navFolders | Sort-Object -Unique
}

$modFolders = $modules.Folder

# ============ C1：模块声明的能力必须可解析 ============
Write-Host ''
Write-Host '[C1] 模块声明能力 ↔ 能力注册表解析' -ForegroundColor Cyan
foreach ($mod in $modules | Where-Object { $_.CapabilityId }) {
    if ($capIds -contains $mod.CapabilityId) {
        Emit 'OK' ("{0} 声明能力 {1} 可在注册表解析" -f $mod.PackageId, $mod.CapabilityId)
    }
    else {
        Emit 'FAIL' ("{0} 声明能力 {1} 不在能力注册表（非法能力声明，P46 契约违规）" -f $mod.PackageId, $mod.CapabilityId)
    }
}

# ============ C2：每个能力应有主责模块实现 ============
Write-Host ''
Write-Host '[C2] 能力 ↔ 主责模块（孤儿能力）' -ForegroundColor Cyan
foreach ($c in $caps) {
    $impl = $modules | Where-Object { $_.CapabilityId -eq $c.Id -and $_.PackageId -eq $c.PrimaryModuleId }
    if ($impl) {
        Emit 'OK' ("能力 {0} 由主责模块 {1} 实现" -f $c.Id, $c.PrimaryModuleId)
    }
    else {
        Emit 'FAIL' ("能力 {0} 未找到主责模块 {1} 的实现（孤儿能力或主责模块未登记 capability）" -f $c.Id, $c.PrimaryModuleId)
    }
}

# ============ C3：能力权限前缀 ↔ 权限键族 ============
Write-Host ''
Write-Host '[C3] 能力权限前缀 ↔ PortalPermissionKeys 键族' -ForegroundColor Cyan
foreach ($c in $caps) {
    if (-not $c.PermissionPrefix) { continue }
    $prefix = $c.PermissionPrefix + '.'
    $hit = $permKeys | Where-Object { $_.StartsWith($prefix) } | Select-Object -First 1
    if ($hit) {
        Emit 'OK' ("能力 {0} 权限前缀 {1} 在 PortalPermissionKeys 有键族（如 {2}）" -f $c.Id, $c.PermissionPrefix, $hit)
    }
    else {
        $alt = $c.PermissionPrefix -replace '^Business\.', ''
        $altHit = $permKeys | Where-Object { $_.StartsWith($alt + '.') } | Select-Object -First 1
        if ($altHit) {
            Emit 'FAIL' ("能力 {0} 注册表权限前缀 {1} 与实际键族前缀 {2} 不一致（注册表多写 Business. 前缀）" -f $c.Id, $c.PermissionPrefix, $alt)
        }
        else {
            Emit 'FAIL' ("能力 {0} 权限前缀 {1} 在 PortalPermissionKeys 无任何对应键族（可能缺失权限键定义）" -f $c.Id, $c.PermissionPrefix)
        }
    }
}

# ============ C4：导航 DesktopModule 入口 ↔ 模块目录 ============
Write-Host ''
Write-Host '[C4] 导航入口 ↔ 模块目录（悬空入口）' -ForegroundColor Cyan
if ($navFolders.Count -eq 0) {
    Emit 'FAIL' '未在导航注册表解析到 DesktopModules/<folder>/ 入口引用'
}
else {
    foreach ($f in $navFolders) {
        if ($modFolders -contains $f) {
            Emit 'OK' ("导航入口引用模块目录 {0} 存在" -f $f)
        }
        else {
            Emit 'FAIL' ("导航入口引用模块目录 {0} 不存在于 DesktopModules（悬空入口）" -f $f)
        }
    }
}

# ============ C5：命名纪律（能力键 / 模块包标识 / 权限键 形态）============
# 注：项目在源码刻意嵌入工作项可追溯性码（WorkItem sidecar 机制），不视为漂移，故不扫描。
Write-Host ''
Write-Host '[C5] 命名纪律（能力键 / 模块包标识 / 权限键 形态）' -ForegroundColor Cyan
$capLayerRe = '^BasicBusiness\.\w+$'
$modPkgRe    = '^HIA\.\w+$'
$permLayerOk = @('Admin','Ops','Settings','Theme','Module','Content','Business','EmployeeDirectory','EmployeeProfileConfirm','EmployeeProfileCorrectionRequest','LeaveRequest','ExpenseReimbursement','EnterpriseCapability','Audit','Portal')
foreach ($c in $caps) {
    if ($c.Id -match $capLayerRe) { Emit 'OK' ("能力键 {0} 符合 BasicBusiness.<类> 形态" -f $c.Id) }
    else { Emit 'FAIL' ("能力键 {0} 不符合 BasicBusiness.<类> 形态" -f $c.Id) }
}
foreach ($m in $modules) {
    if (-not $m.PackageId) { continue }
    if ($m.PackageId -match $modPkgRe) { Emit 'OK' ("模块包 {0} 符合 HIA.<名> 形态" -f $m.PackageId) }
    else { Emit 'FAIL' ("模块包 {0} 不符合 HIA.<名> 形态" -f $m.PackageId) }
}
$badPerm = 0
foreach ($k in $permKeys) {
    $seg = ($k.Split('.'))[0]
    if ($permLayerOk -notcontains $seg) {
        if ($badPerm -lt 20) { Emit 'FAIL' ("权限键 {0} 首段 {1} 不在已知层词表" -f $k, $seg) }
        $badPerm++
    }
}
if ($badPerm -eq 0) { Emit 'OK' ("{0} 个权限键首段均在已知层词表" -f $permKeys.Count) }

# ============ C6：入口双源交叉对账（SQL 种子 ↔ 代码注册表，含部署态增量白名单）============
# Q3-C（W-anp-P326）：显式承认入口存在两套并行源、交集须有人看守——
#   ① SQL 导航种子（src/Setup/Portal_NavigationSeed.sql）：用户级页签与模块挂载的【完整】事实源；
#   ② 代码导航注册表（PortalNavigationRegistry.cs）：需运行期权限/可见性元数据的【精选】入口集合
#      （含 AdminPage 与少数 DesktopModule），并非种子的完整镜像。
# 作用域不同，故只校收敛方向：代码声明了一个 DesktopModule 入口 ⇒ 数据库（种子 ∪ 部署增量）必须挂载它，
# 否则运行时该入口点进去无模块（P319-① 类事故）。反向（种子⊆代码）不成立——注册表是精选集、不枚举全部基础模块，属诚实边界。
# AdminPage 类入口为平台/后台页，本就不进用户级导航种子，显式免校；FormBuilder 等由部署增量脚本
# （PortalCfg_FormBuilder_Provision.sql）注入，纳入「部署态增量白名单」，与种子合并为有效数据库挂载集 D。
Write-Host ''
Write-Host '[C6] 入口双源交叉对账（SQL 种子 ↔ 代码注册表）' -ForegroundColor Cyan

$seedSqlPath      = Join-Path $RepoRoot 'src/Setup/Portal_NavigationSeed.sql'
$provisionSqlPath = Join-Path $RepoRoot 'src/Setup/PortalCfg_FormBuilder_Provision.sql'

# 数据库侧挂载集合 D = 种子模块挂载 ∪ 部署态增量挂载
$dbMounts = @{}
if (Test-Path $seedSqlPath) {
    $seedText = Get-Content $seedSqlPath -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($seedText, "\(N'[^']*',\s*N'[^']*',\s*N'[^']*',\s*N'[^']*',\s*\d+,\s*N'[^']*',\s*\d+,\s*N'[^']*',\s*N'(?<src>[^']*)',\s*N'[^']*',\s*N'[^']*'\)")) {
        $s = $m.Groups['src'].Value
        if ($s -ne '' -and -not $dbMounts.ContainsKey($s)) { $dbMounts[$s] = 'seed' }
    }
}
# 部署态增量白名单：部署期注入、不在基础种子内的模块挂载
$deployIncrements = @()
if (Test-Path $provisionSqlPath) {
    $provText = Get-Content $provisionSqlPath -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($provText, "\[?DesktopSourceFile\]?\s*=\s*N'(?<src>[^']+)'")) {
        $s = $m.Groups['src'].Value
        if ($s -ne '' -and -not $dbMounts.ContainsKey($s)) { $dbMounts[$s] = 'provision'; $deployIncrements += $s }
    }
}

# 代码侧 DesktopModule 入口集合 R（注册表非完整镜像，仅含精选桌面模块）
$regDmRe = '(?s)new PortalNavigationEntry\(\s*"(?<id>[^"]+)",\s*PortalNavigationEntryKind\.DesktopModule.*?(?<src>"(?:DesktopModules|Admin)/[^"]+\.ascx")'
$regDesktop = @()
if (Test-Path $navFile) {
    foreach ($m in [regex]::Matches($navText, $regDmRe)) {
        $regDesktop += [PSCustomObject]@{ Id = $m.Groups['id'].Value; Src = $m.Groups['src'].Value.Trim('"') }
    }
}

if ($regDesktop.Count -eq 0) {
    Emit 'FAIL' '未在代码导航注册表解析到任何 DesktopModule 入口（入口源缺失）'
}
else {
    foreach ($e in $regDesktop) {
        if ($dbMounts.ContainsKey($e.Src)) {
            Emit 'OK' ("代码入口 {0} 的模块挂载 {1} 在数据库（{2}）中存在" -f $e.Id, $e.Src, $dbMounts[$e.Src])
        }
        else {
            Emit 'FAIL' ("代码入口 {0} 声明的模块挂载 {1} 在数据库种子与部署态增量中均未找到（入口点进去无模块；若属合法部署增量，请加入白名单）" -f $e.Id, $e.Src)
        }
    }
}
if ($deployIncrements.Count -gt 0) {
    Emit 'OK' ("部署态增量白名单已纳入 {0} 项（由 PortalCfg_FormBuilder_Provision.sql 注入）：{1}" -f $deployIncrements.Count, ($deployIncrements -join ', '))
}

# ============ 汇总 ============
Write-Host ''
Write-Host '=== 汇总 ===' -ForegroundColor Cyan
Write-Host ("能力 {0} / 模块 {1} / 权限键 {2} / 导航入口目录 {3}" -f $caps.Count, $modules.Count, $permKeys.Count, $navFolders.Count)
Write-Host ("结果：FAIL={0}  WARN={1}" -f $fail, $warn) -ForegroundColor $(if ($fail -gt 0) { 'Red' } else { 'Green' })

if ($fail -gt 0) { exit 1 }
if ($Strict -and $warn -gt 0) { exit 2 }
exit 0
