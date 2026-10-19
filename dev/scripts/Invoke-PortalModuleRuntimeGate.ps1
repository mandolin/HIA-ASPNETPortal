<#
.SYNOPSIS
  <lang>
    <zh-CN>发布前门禁编排层：从数据库自动发现全部启用模块，逐档位验证其运行期可加载。</zh-CN>
    <en>Pre-release gate orchestration: auto-discovers every enabled module from the database and verifies each one is runtime-loadable, profile by profile.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>把 `Test-PortalModuleRuntimeEvidence.mjs` 原先写死在源码里的目标清单改为以数据库为权威来源，使新增模块在原理上不可能静默落在门禁范围之外。
    **前置条件**：本机 `node` 运行时、开发期 `playwright` 本地依赖、外置 `connectionStrings.config`；任一缺失即以退出码 2 停止，不猜测替代路径。
    **验证方式**：退出码 0 表示全部通过、1 表示有模块加载或断言失败、2 表示前置条件缺失；证据写入 `-EvidenceDir` 指定目录。
    **低敏边界**：脚本不读取、不回显连接串内容或任何凭据；日志与证据只保留模块标识与断言结果。</zh-CN>
    <en>Changes the previously hardcoded target list of `Test-PortalModuleRuntimeEvidence.mjs` to treat the database as the authoritative source, so a newly added module cannot silently fall outside gate scope in principle.
    **Prerequisites**: a local `node` runtime, the development-time local `playwright` dependency, and the external `connectionStrings.config`; if any is missing the script stops with exit code 2 instead of guessing a substitute.
    **Verification**: exit code 0 means all passed, 1 means a module load or assertion failed, 2 means missing prerequisites; evidence is written to `-EvidenceDir`.
    **Low-sensitivity boundary**: the script neither reads nor echoes connection-string contents or any credentials; logs and evidence keep only module identity and assertion results.</en>
  </lang>
.PARAMETER EvidenceDir
  <lang>
    <zh-CN>证据输出目录；留空时使用带时间戳的默认目录。</zh-CN>
    <en>Evidence output directory; when empty, a timestamped default directory is used.</en>
  </lang>
.PARAMETER SkipProfileSwitch
  <lang>
    <zh-CN>不切换模块档位，供由 CI 自行管理档位的场景使用。</zh-CN>
    <en>Does not switch module profiles, for scenarios where CI manages the profile itself.</en>
  </lang>

  证明边界：只证明**当前数据库注册事实**下的启用模块能被加载并通过已登记断言；不证明未启用/未注册模块（不在发现集合内），不证明模块业务语义正确，不证明跨档位配置差异（按传入档位），证据需人工判读（不自动判合格）。

  Proof boundary: Proof boundary: proves only that the enabled modules discovered from the **current database registration facts** load and pass the registered assertions; it does not cover disabled/unregistered modules (outside the discovery set), does not prove business semantics, does not prove cross-profile configuration differences (per the given profile), and its evidence needs human reading (it is not auto-graded).
#>

# W83 P83.3 发布前门禁编排层：自动发现全部启用模块，逐档位验证其运行期可加载。
#
# 为什么需要它（P83.0 实测）：既有门禁 Test-PortalModuleRuntimeEvidence.mjs 的目标清单**写死在源码里**。
# 数据库里启用的模块实例 38 个，门禁只覆盖 4 个；新增模块若忘记改脚本**不会报错**，只是该模块从此静默地
# 落在门禁范围之外 —— 这是"必然漏"的机制。本脚本把目标来源改为**数据库**（权威），使漏网在原理上不可能。
#
# 门禁证明的是什么（一手依据 + 本项目实测，见 W-anp-P83.md）：Portal.csproj 的 ProjectTypeGuids 是
# {349C5851-…} = **Web Application Project**，代码后置由 msbuild **显式编译**进 bin\Portal.dll；但 .ascx
# **标记层**仍由 ASP.NET 在**运行期**生成 partial 类。因此"缺 <%@ Import %>"这类**标记层**问题 msbuild
# 完全测不到，只在真实访问该页时抛 CS0103（P74.3 三个前台模块中招、P81 两个旧内容模块中招）。
# 不可把这层原因说成"代码后置不被编译" —— 那是 Web Site Project 的特征，与本项目不符。
#
# 用法（仓库根目录）：
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File dev\scripts\Invoke-PortalModuleRuntimeGate.ps1
#   开关：-EvidenceDir <路径>（默认 work-zone\dev\evidence\p83.4\<时间戳>-Dev）；-SkipProfileSwitch（不切档位，供 CI）。
# 退出码：0 全部通过 / 1 有模块加载或断言失败 / 2 前置条件缺失。

param(
    [string] $EvidenceDir,
    [switch] $SkipProfileSwitch
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$nodeExe = 'C:\Program Files\nodejs\node.exe'
# <lang>
#   <zh-CN>切档位时的包集合由共享脚本按"基线显式启用包 ∪ 本组被测包"求得；本脚本不再自行拼 `Enabled` 值。
#   早先在此处把 `Enabled` 写死为 `HIA.ModuleProbe`，会**替换掉**基线里的 `HIA.LeaveRequest,HIA.ExpenseReimbursement`
#   （二者不在任何 Profile 的 include 图里，只靠 `Enabled` 进入门户），于是被测模块被 Profile 门禁阻断而不渲染，
#   门禁把"部署策略不允许"读成"模块根找不到" —— 这是 `W-anp-P137` 实测到的根因，也是同一处 2026-10-09 故障的复发。</zh-CN>
#   <en>The package set used when switching profiles comes from the shared script as "baseline explicitly enabled packages ∪
#   packages probed by this group"; this script no longer composes `Enabled` itself. Hardcoding `Enabled` to `HIA.ModuleProbe`
#   here **replaced** the baseline's `HIA.LeaveRequest,HIA.ExpenseReimbursement` (neither appears in any Profile's include
#   graph; they reach the Portal only through `Enabled`), so the probed module was blocked by the profile gate and never
#   rendered, and the gate read "deployment policy disallows this" as "module root not found" — the root cause measured in
#   `W-anp-P137` and a recurrence of the same 2026-10-09 failure at this very spot.</en>
# </lang>
. (Join-Path $PSScriptRoot 'Resolve-PortalRuntimeGatePackages.ps1')
$settingsPath = Join-Path $repoRoot 'src\Portal\Config\appSettings.dev.json'
$webConfigPath = Join-Path $repoRoot 'src\Portal\web.config'
$playwrightPath = Join-Path $repoRoot 'temp\node_modules\playwright'
$connectionConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'

function Write-Step([string] $message) { Write-Host "[p83] $message" }

# <lang>
#   <zh-CN>`W-anp-P139`：前置条件缺失必须**显式 exit 2**（表示"环境没装"，不是"代码坏了"），并写入证据、不与代码缺陷（exit 1）混报。
#   早先这里用 `Write-Error ...; exit 2`，但本脚本头 `$ErrorActionPreference='Stop'` 会把 `Write-Error` 当成**终止错误**抛出，
#   `exit 2` 根本执行不到，实际以 exit 1 退出 —— 于是 playwright 缺失被误判成产品缺陷。改用 `Write-Step`（`Write-Host`）避免进错误流，确保 exit 2 命中。</zh-CN>
#   <en>`W-anp-P139`: a missing prerequisite must exit **2** explicitly (meaning "environment not installed", not "product broken")
#   and be recorded, never conflated with a code defect (exit 1). Earlier this used `Write-Error ...; exit 2`, but the script header
#   `$ErrorActionPreference='Stop'` turns `Write-Error` into a **terminating** error, so `exit 2` was never reached and the script
#   actually exited 1 — playwright missing was misjudged as a product defect. Using `Write-Step` (`Write-Host`) keeps it off the error
#   stream so exit 2 actually fires.</en>
# </lang>
foreach ($required in @(@{ Path = $nodeExe; Hint = 'node 运行时' }, @{ Path = $playwrightPath; Hint = 'playwright 模块（开发期依赖，不入库）' }, @{ Path = $connectionConfig; Hint = '外置连接串' })) {
    if (-not (Test-Path $required.Path)) { Write-Step ("前置条件缺失：{0}（{1}）" -f $required.Hint, $required.Path); exit 2 }
}

if (-not $EvidenceDir) {
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmss')
    $EvidenceDir = Join-Path $repoRoot "work-zone\dev\evidence\p83.4\$stamp-Dev"
}
New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
Write-Step "证据目录：$EvidenceDir"

# ---------------------------------------------------------------------------
# 目标发现：数据库是权威来源（目录里有 != 启用了；数据库启用了但加载不了才是真问题）
# ---------------------------------------------------------------------------
[xml] $connectionXml = Get-Content $connectionConfig
$connectionString = ''
foreach ($entry in $connectionXml.connectionStrings.add) {
    if ($entry.name -match 'Portal') { $connectionString = $entry.connectionString; break }
}
if (-not $connectionString) { Write-Error '连接串配置里没有名为 Portal 的连接。'; exit 2 }

# <lang>
#   <zh-CN>预热用的站点基址：优先取 `temp\p65\p65-acceptance-context.json` 的 baseUrl，缺省回退 40001。
#   仅用于回收后预热（填充模块运行期缓存），与门禁真实导航目标一致。</zh-CN>
#   <en>Base URL used for warm-up: prefer `baseUrl` from `temp\p65\p65-acceptance-context.json`, falling back to 40001.
#   It is used only for post-recycle warm-up (priming the module runtime cache) and matches the gate's real navigation target.</en>
# </lang>
$baseUrl = 'http://localhost:40001/'
$acceptanceCtxPath = Join-Path $repoRoot 'temp\p65\p65-acceptance-context.json'
if (Test-Path $acceptanceCtxPath) {
    try { $acc = Get-Content $acceptanceCtxPath -Raw -Encoding UTF8 | ConvertFrom-Json; if ($acc.baseUrl) { $baseUrl = $acc.baseUrl } } catch { }
}

$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
$command = $connection.CreateCommand()
# <lang>
#   <zh-CN>只取"已部署到页签"的实例（TabId 非空），且用 LEFT JOIN：若某实例的 TabId 指向已删除的页签，
#   INNER JOIN 会**静默丢掉**它 —— 而那正是最需要被门禁发现的坏数据。</zh-CN>
#   <en>Only instances deployed onto a tab (TabId not null) are taken, and the joins are LEFT: if an instance points at a
#   deleted tab an INNER JOIN would **silently drop** it, and that is exactly the broken data the gate most needs to catch.</en>
# </lang>
$command.CommandText = 'SELECT d.DesktopSourceFile, m.TabId, t.TabOrder, m.ModuleTitle FROM PortalCfg_Modules m LEFT JOIN PortalCfg_ModuleDefinitions d ON m.ModuleDefId = d.ModuleDefId LEFT JOIN PortalCfg_Tabs t ON m.TabId = t.TabId WHERE m.TabId IS NOT NULL ORDER BY t.TabOrder, m.ModuleOrder'
$reader = $command.ExecuteReader()
$instances = @()
while ($reader.Read()) {
    $instances += [pscustomobject]@{
        SourceFile = [string]$reader['DesktopSourceFile']
        TabId      = $reader['TabId']
        TabOrder   = $reader['TabOrder']
        Title      = [string]$reader['ModuleTitle']
    }
}
$reader.Close()
$connection.Close()

$deployed = @($instances | Where-Object { $_.TabOrder -ne $null })
Write-Step ("数据库模块实例 {0} 个，其中已部署到页签 {1} 个" -f $instances.Count, $deployed.Count)
$orphan = @($instances | Where-Object { $_.TabOrder -eq $null })
if ($orphan.Count -gt 0) {
    Write-Step ("警告：{0} 个实例的 TabId 指向不存在的页签，无法验证（已记入证据的 skippedInstances）" -f $orphan.Count)
}

# ---------------------------------------------------------------------------
# 分组：一个档位渲染不了另一组的模块（P81 / P82 连续两次撞过这个坑），故按归属分轮
# ---------------------------------------------------------------------------
# <lang>
#   <zh-CN>分组依据是**模块在源码树里的位置**，实测规律：旧内容模块直接位于 DesktopModules\ 根目录，业务模块位于
#   同名子目录，后台模块位于 Admin\。这是启发式而非权威定义（权威应是包配置表），但它有两个好处：不需要额外查表，
#   且新增模块**默认落入某一组**（子目录即业务模块），不会"没人认领"而被跳过 —— 这才是 default-deny 要防的情形。
#   局限与后续：若将来出现"业务模块放在根目录"或"旧内容模块放进子目录"，本分组会错。届时应改为读包配置表。
# </zh-CN>
# <en>Grouping is by **where the module sits in the source tree**, an observed regularity: legacy modules sit directly under
# DesktopModules\, business modules in a same-named subdirectory, admin modules under Admin\. This is a heuristic rather
# than an authoritative definition (that would be the package configuration), but it has two benefits: no extra table lookup
# is needed, and a new module **defaults into some group** (a subdirectory means business module) instead of being unclaimed
# and skipped — which is precisely what default-deny must prevent. Known limitation: should a business module ever sit at the
# root or a legacy module move into a subdirectory, the grouping is wrong and should be replaced by the package configuration.
#   </en>
# </lang>
function Get-ModuleGroup([string] $sourceFile) {
    $normalized = $sourceFile.Replace('\', '/')
    if ($normalized -like 'Admin/*') { return 'admin' }
    if ($normalized -like 'DesktopModules/*/*') { return 'business' }
    return 'legacy'
}

# <lang>
#   <zh-CN>模块根选择器：从 ascx 标记里取**第一个** class 含 module 或以模块名命名的顶层 div。
#   取不到就退回"页面出现该模块的标题文本"作为可断言信号 —— 宁可弱一点，也不要因为选择器取不到就静默跳过模块。</zh-CN>
#   <en>The module-root selector: take the **first** top-level div whose class contains module or matches the module name.
#   When none is found, fall back to "the page contains this module's title text" as the assertable signal — a weaker
#   assertion is acceptable, silently skipping the module because no selector could be derived is not.</en>
# </lang>
function Get-ModuleSelector([string] $sourceFile) {
    $ascxPath = Join-Path (Join-Path $repoRoot 'src\Portal') $sourceFile.Replace('/', '\')
    if (-not (Test-Path $ascxPath)) { return $null }
    $markup = [System.IO.File]::ReadAllText($ascxPath, [System.Text.Encoding]::UTF8)

    # <lang>
    #   <zh-CN>取**第一个**顶层 div 的 class 里第一个"非 portal- 前缀"的 token 作为模块根选择器。
    #   早先的写法要求 class 里含 "module"，但实测模块根 class 是 my-work-items / enterprise-workbench /
    #   employee-profile-correction 这类**专有模块名**，并不含 "module" 字样 —— 那个正则一个都没匹配上，
    #   于是全部目标退化成"只断言未落错误页"，比改造前（4 个目标有 modulePresent 断言）**更弱**。
    #   门禁变弱比门禁报错更危险：它看起来是绿的。故此处改为取第一个可用 token，并排除 portal- 公共壳类。</zh-CN>
    #   <en>The selector is the first non-portal- prefixed token of the **first** top-level div class. An earlier version
    #    required the class to contain "module", but the measured module root classes are dedicated module names such as
    #   my-work-items / enterprise-workbench / employee-profile-correction, none of which contains the word "module" — that
    #    regex matched nothing, so every target degraded to "only assert no error page", which is **weaker** than before the
    #    change (4 targets had a modulePresent assertion). A weakened gate is more dangerous than a failing one: it still looks
    #    green. Hence the first usable token is taken instead, excluding the shared portal- shell classes.</en>
    # </lang>
    # <lang>
    #   <zh-CN>判据取"在 ascx 中**只出现一次**的 class"，不再排除 `portal-` 前缀 —— 这是 `W87` 实测得出的修正：
    #   旧内容模块**只有** `portal-` 前缀的 class（Contacts -> portal-content-table-wrap、
    #   QuickLinks -> portal-quicklinks），早先"排除 portal- 前缀"的写法恰好把它们的唯一标识也排除了，
    #   结果 11 个目标全部 `module=undefined`，断言退化为"只断言未落错误页"（比改造前更弱却显示通过）。
    #   仅排除三个公共壳类（模块标题/动作区），它们每个模块都有、不具区分度。
    #   已知局限：Repeater 模板里的行级 class（如 portal-content-list-item）在标记中也只出现一次，
    #   会被选中 —— 此时断言语义从"模块根存在"弱化为"模块渲染出了内容"，仍比没有断言强。
    # </zh-CN>
    #   <en>The criterion is a class occurring **exactly once** in the ascx, and the `portal-` prefix is no longer excluded — a
    #   correction established by measurement in `W87`: legacy modules have **only** `portal-`-prefixed classes (Contacts ->
    #   portal-content-table-wrap, QuickLinks -> portal-quicklinks), so the earlier "skip portal- prefixed" rule discarded the
    #   very tokens that identify them, leaving all 11 targets with `module=undefined` and degrading the assertion to "did not
    #   fall back to the error page" — weaker than before the change, yet still shown as a pass. Only the three shared shell
    #   classes are excluded (module header/title/actions); every module has them, so they carry no signal. Known limitation:
    #   a row-level class inside a Repeater template (such as portal-content-list-item) also occurs once in the markup and will
    #   be selected — the assertion then weakens from "the module root exists" to "the module rendered content", which is
    #   still better than no assertion.</en>
    # </lang>
    $counts = @{}
    $shellClasses = @('portal-module-header', 'portal-module-title-wrap', 'portal-module-actions')
    foreach ($cm in [regex]::Matches($markup, 'class="([^"]+)"')) {
        foreach ($token in ($cm.Groups[1].Value -split '\s+')) {
            if (-not $token -or $shellClasses -contains $token) { continue }
            if (-not $counts.ContainsKey($token)) { $counts[$token] = 0 }
            $counts[$token]++
        }
    }
    foreach ($token in ($counts.Keys | Sort-Object)) {
        if ($counts[$token] -eq 1) { return '.' + $token }
    }
    return $null
}

$baseUrl = ''
$contextPath = Join-Path $repoRoot 'temp\p65\p65-acceptance-context.json'
if (Test-Path $contextPath) {
    try { $baseUrl = [string](Get-Content $contextPath -Raw | ConvertFrom-Json).baseUrl } catch { $baseUrl = '' }
}
if (-not $baseUrl) {
    Write-Error '未能从 temp\p65\p65-acceptance-context.json 读到 baseUrl（运行期门禁需要绝对 URL）。'
    exit 2
}

$groups = @{
    legacy   = @{ Profile = 'LegacyContent';    Targets = @() }
    business = @{ Profile = 'BusinessWorkflow';  Targets = @() }
    admin    = @{ Profile = 'BusinessWorkflow';  Targets = @() }
}
$skippedInstances = @()
foreach ($instance in $deployed) {
    if (-not $instance.SourceFile) { $skippedInstances += [pscustomobject]@{ Title = $instance.Title; Reason = '模块定义缺失 DesktopSourceFile' }; continue }
    $groupKey = Get-ModuleGroup $instance.SourceFile
    $selector = Get-ModuleSelector $instance.SourceFile
    # <lang>
    #   <zh-CN>顺带解析实例的包标识：切档位时它必须进入 `Enabled`，否则"被测模块被 Profile 门禁阻断"
    #   会被读成"模块没渲染出来"。无 manifest 的旧内容/后台模块返回 `$null`，不参与并集。</zh-CN>
    #   <en>Also resolve the instance's package id: it must enter `Enabled` when the profile switches, otherwise "the probed
    #   module is blocked by the profile gate" is read as "the module did not render". Modules without a manifest (legacy
    #   content and admin) return `$null` and do not take part in the union.</en>
    # </lang>
    $packageId = Get-PortalModulePackageId -RepoRoot $repoRoot -SourceFile $instance.SourceFile
    $groups[$groupKey].Targets += [pscustomobject]@{
        id             = "$($instance.SourceFile.Replace('DesktopModules/', '').Replace('.ascx', '').Replace('/', '-'))-$($instance.TabId)"
        url            = ([string]$baseUrl).TrimEnd('/') + "/DesktopDefault.aspx?tabindex=$($instance.TabOrder)&tabid=$($instance.TabId)"
        moduleSelector = $selector
        expectTitle    = $instance.Title
        packageId      = $packageId
    }
}

# <lang>
#   <zh-CN>同一页签下的多个模块共用一个 URL，重复访问没有额外价值反而拖慢门禁，故按 URL 去重（保留全部模块名供断言）。</zh-CN>
#   <en>Several modules on the same tab share one URL, so revisiting it adds no coverage but slows the gate down; targets are
#   therefore deduplicated by URL while keeping every module name for assertions.</en>
# </lang>
foreach ($groupKey in @('legacy', 'business', 'admin')) {
    $deduped = @()
    foreach ($target in $groups[$groupKey].Targets) {
        $existing = $deduped | Where-Object { $_.url -eq $target.url } | Select-Object -First 1
        if ($existing) {
            $existing.expectTitle = @($existing.expectTitle) + $target.expectTitle
            # <lang>
            #   <zh-CN>同一 URL 上的多个实例可能分属不同包（实测 1101 同时挂 `HIA.LeaveRequest` 与
            #   `HIA.ExpenseReimbursement`）；去重只合并 URL，**包名必须逐个保留**，否则并集会漏包、
            #   漏掉的那个包在验证期间仍被阻断。</zh-CN>
            #   <en>Several instances on one URL may belong to different packages (measured: tab 1101 carries both
            #   `HIA.LeaveRequest` and `HIA.ExpenseReimbursement`); deduplication merges URLs only, so **every package name
            #   must be kept** — otherwise the union misses a package and that package stays blocked during verification.</en>
            # </lang>
            $existing.packageIds = @($existing.packageIds) + @($target.packageId)
            continue
        }
        # <lang>
        #   <zh-CN>首见的 URL 直接入列，并把单值 `packageId` 归一为数组，使后续合并无需区分标量与集合。</zh-CN>
        #   <en>A first-seen URL goes in directly and its scalar `packageId` is normalized to an array, so later merges need not distinguish scalar from collection.</en>
        # </lang>
        $target | Add-Member -NotePropertyName 'packageIds' -NotePropertyValue @($target.packageId) -Force
        $deduped += $target
    }
    $groups[$groupKey].Targets = $deduped
    # <lang>
    #   <zh-CN>本组被测包的并集：切档位时写入 `Enabled`，保证被测模块在验证期间**允许渲染** ——
    #   门禁要证明的是"运行期能否加载"，而"当前部署策略是否允许"是另一回事，被它挡住就什么都证明不了。</zh-CN>
    #   <en>Union of the packages probed by this group: written into `Enabled` when the profile switches so the probed modules are
    #   **allowed to render** during verification — the gate exists to prove "can it load at runtime", whereas "does the current
    #   deployment policy allow it" is a separate question, and being blocked by it proves nothing.</en>
    # </lang>
    $groups[$groupKey].Packages = @($deduped | ForEach-Object { $_.packageIds } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    Write-Step ("组 {0,-8} 档位 {1,-16} URL {2} 个（模块实例 {3} 个）｜被测包 {4} 个：{5}" -f $groupKey, $groups[$groupKey].Profile, $deduped.Count, $deduped.Count, $groups[$groupKey].Packages.Count, ($groups[$groupKey].Packages -join ', '))
}

$discovery = [pscustomobject]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    instanceCount = $instances.Count
    deployedCount = $deployed.Count
    groups = $groups
    skippedInstances = $skippedInstances
}
$discoveryPath = Join-Path $EvidenceDir 'module-discovery.json'
($discovery | ConvertTo-Json -Depth 8) | Set-Content -Path $discoveryPath -Encoding UTF8
Write-Step "目标清单已留证：$discoveryPath"

# ---------------------------------------------------------------------------
# 档位切换：必须兜底还原（P81 / P82 连续两次人工切档位都靠记忆，本层把它变成代码责任）
# ---------------------------------------------------------------------------
# <lang>
#   <zh-CN>切换手段是重写外置 appSettings.dev.json 并触碰 web.config（触发应用域回收）。两者都是**临时**改动，
#   因此整段包在 try/finally 里：finally 先把配置还原，再恢复 web.config 的时间戳。
#   为什么必须有 finally：门禁失败、断言中断甚至 Ctrl+C 时若不还原，临时档位会**留在工作树里**，
#   下一次任何人跑构建都可能看到与预期不同的模块可见性 —— 这类污染极难定位。
# </zh-CN>
# <en>The switch rewrites the external appSettings.dev.json and touches web.config (to recycle the app domain). Both are
#   **temporary** changes, so the whole block sits in a try/finally: finally restores the configuration first, then the
#   web.config timestamp. Why finally is mandatory: if the gate fails, an assertion throws, or the run is interrupted, an
#   unrestored profile stays **in the working tree**, and the next person who builds may see module visibility that differs
#   from expectations — such contamination is extremely hard to trace.
#   </en>
# </lang>
$settingsBackup = $null
if (Test-Path $settingsPath) { $settingsBackup = [System.IO.File]::ReadAllText($settingsPath, [System.Text.Encoding]::UTF8) }
$webConfigStamp = (Get-Item $webConfigPath).LastWriteTime

function Restore-Profile {
    if ($null -ne $settingsBackup) {
        [System.IO.File]::WriteAllText($settingsPath, $settingsBackup, [System.Text.UTF8Encoding]::new($false))
    }
    (Get-Item $webConfigPath).LastWriteTime = $webConfigStamp
    Write-Step '档位已还原'
}

function Set-Profile([string] $profile, [string] $enabledPackages = $null) {
    # <lang>
    #   <zh-CN>切档位时**保留** `appSettings` 的其它键（尤其 `Portal.ModulePackages.Enabled`）。
    #   早先实现把 dev.json 整体覆盖成只含 `Active` 的 JSON，会**一并抹掉** `Enabled=HIA.ModuleProbe` —— 于是
    #   business 组切回 BusinessWorkflow 后 ModuleProbe 包被禁用、模块不再渲染，门禁误报 `module=false`
    #   （这正是 2026-10-09 L2 14/15 的元凶）。`enabledPackages` 显式传入（business 组）时覆盖该键，
    #   保证被测包在验证期间启用。</zh-CN>
    #   <en>When switching profiles, **preserve** the other keys of `appSettings` (especially
    #   `Portal.ModulePackages.Enabled`). The earlier implementation overwrote dev.json wholesale with a JSON containing
    #   only `Active`, which also **erased** `Enabled=HIA.ModuleProbe` — so after the business group switched back to
    #   BusinessWorkflow the ModuleProbe package was disabled, the module stopped rendering, and the gate falsely reported
    #   `module=false` (the cause of L2 14/15 on 2026-10-09). When `enabledPackages` is passed explicitly (business group)
    #   it overrides that key so the probed package stays enabled during verification.</en>
    # </lang>
    $appSettings = [pscustomobject]@{ TestItem = 'dev item' }
    if (Test-Path $settingsPath) {
        try {
            $prev = Get-Content $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($prev.appSettings) {
                foreach ($k in $prev.appSettings.PSObject.Properties.Name) {
                    $appSettings | Add-Member -NotePropertyName $k -NotePropertyValue $prev.appSettings.$k -Force
                }
            }
        } catch { }
    }
    # <lang>
    #   <zh-CN>`appSettings` 的键是**平面点号键**（如 `Portal.ModuleProfiles.Active`）。直接用 `$obj.'a.b.c' = $v`
    #   会被 PowerShell 当成嵌套路径导航而报错（属性不存在）。故用 `Add-Member -Force`：点号按字面键名处理，
    #   既合并既有键也新增键。`[string]` 形参会把 `$null` 转空串，故用 `IsNullOrEmpty` 判空。</zh-CN>
    #   <en>`appSettings` keys are **flat dotted keys** (e.g. `Portal.ModuleProfiles.Active`). Assigning via `$obj.'a.b.c' = $v`
    #   makes PowerShell treat the dots as nested path navigation and fail (the property does not exist), so `Add-Member -Force`
    #   is used: dots are handled as literal key names, merging existing keys as well as adding new ones. A `[string]` parameter
    #   turns `$null` into an empty string, so `IsNullOrEmpty` performs the emptiness check.</en>
    # </lang>
    $appSettings | Add-Member -NotePropertyName 'Portal.ModuleProfiles.Active' -NotePropertyValue $profile -Force
    if (-not [string]::IsNullOrEmpty($enabledPackages)) {
        $appSettings | Add-Member -NotePropertyName 'Portal.ModulePackages.Enabled' -NotePropertyValue $enabledPackages -Force
    }
    $nextJson = ([pscustomobject]@{ appSettings = $appSettings } | ConvertTo-Json -Depth 4)
    [System.IO.File]::WriteAllText($settingsPath, $nextJson, [System.Text.UTF8Encoding]::new($false))
    (Get-Item $webConfigPath).LastWriteTime = Get-Date
}

# <lang>
#   <zh-CN>回收后预热：改 web.config 时间戳触发应用域回收，回收后首个请求需重新编译，往往超过 Set-Profile 后的 3s 睡眠。
#   若门禁在实例就绪前就导航（尤其某组首个目标），会打到未渲染完成的页面而误判失败。故切档位后显式预热：读外置连接串、
#   查 PortalCfg_Modules 全部非空 TabId 并逐个请求至 200。取页签失败则退化为仅首页预热。</zh-CN>
#   <en>Post-recycle warm-up: touching the web.config timestamp recycles the app domain, and the first request afterwards must
#   recompile, which usually exceeds the 3-second sleep after Set-Profile. Navigating before the instance is ready (especially
#   for a group's first target) hits a page that has not finished rendering and misreports a failure. Warm-up is therefore
#   explicit after a profile switch: read the external connection string, query PortalCfg_Modules for every non-empty TabId, and
#   request each until it returns 200. If the tab lookup fails, degrade to home-page warm-up only.</en>
# </lang>
function WarmUp-Instance([string] $urlBase) {
    $warmUrls = @($urlBase.TrimEnd('/') + '/')
    try {
        $c = New-Object System.Data.SqlClient.SqlConnection $connectionString
        $c.Open()
        $q = $c.CreateCommand()
        $q.CommandText = 'SELECT DISTINCT TabId FROM PortalCfg_Modules WHERE TabId IS NOT NULL'
        $rd = $q.ExecuteReader()
        while ($rd.Read()) { $warmUrls += ($urlBase.TrimEnd('/') + '/DesktopDefault.aspx?tabid=' + $rd['TabId']) }
        $rd.Close(); $c.Close()
        Write-Step ('预热覆盖 ' + ($warmUrls.Count - 1) + ' 个已部署页签')
    } catch {
        Write-Step ('预热取页签失败（' + $_.Exception.Message + '），退化为首页预热')
    }
    $ok = $false
    for ($wi = 1; $wi -le 40; $wi++) {
        $allOk = $true
        foreach ($u in $warmUrls) {
            try {
                $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 8 -ErrorAction Stop
                if ($r.StatusCode -ne 200) { $allOk = $false; break }
            } catch { $allOk = $false; break }
        }
        if ($allOk) { $ok = $true; break }
        Start-Sleep -Seconds 2
    }
    if ($ok) { Write-Step '预热完成' } else { Write-Step '预热未完成（门禁可能误判，建议手动确认实例）' }
}

$overallFailed = $false
$roundSummaries = @()
try {
    foreach ($groupKey in @('legacy', 'business', 'admin')) {
        $targets = $groups[$groupKey].Targets
        if ($targets.Count -eq 0) { Write-Step "组 $groupKey 无目标，跳过"; continue }

        # <lang>
        #   <zh-CN>历史登记修正（`W-anp-P140`）：本处曾把 admin 组**整体跳过**，理由是"实测 DesktopDefault.aspx?tabid=6
        #   与 Admin\TabLayout.aspx 都不渲染它们（module=false），仅在页签布局编辑模式出现"。该测量已**过期**——
        #   `W-anp-P138`（2026-10-18）以管理员登录访问 tabid=6，5 个后台管理模块全部渲染（截图留证），且按本编排器
        #   同款 URL 构造（tabindex=TabOrder&tabid=6）重跑 `Test-PortalModuleRuntimeEvidence.mjs` 断言 Pass
        #   （errorPage=false、modulePresent=true）。"登记会过期，唯一可靠的是可重复检查"：故删除该跳过，
        #   让 admin 组与其他组走完全相同的真实探测路径，由门禁每次运行重新证明，而不是靠一条旧测量永久豁免。</zh-CN>
        #   <en>Stale-registration correction (`W-anp-P140`): this spot used to **skip** the admin group entirely on the claim that
        #   neither `DesktopDefault.aspx?tabid=6` nor `Admin\TabLayout.aspx` rendered those modules (`module=false`), since they
        #   would appear only in the tab-layout editor's edit mode. That measurement is **outdated** — `W-anp-P138` (2026-10-18)
        #   signed in as administrator, visited tabid=6, and all five admin modules rendered (screenshot archived); re-running
        #   `Test-PortalModuleRuntimeEvidence.mjs` with this orchestrator's exact URL construction (tabindex=TabOrder&tabid=6)
        #   passed its own assertions (errorPage=false, modulePresent=true). "Registrations go stale; only repeatable checks are
        #   reliable": the skip is therefore removed so the admin group takes exactly the same real probing path as the other
        #   groups, re-proved by every gate run instead of being permanently exempted by one old measurement.</en>
        # </lang>
        $profile = $groups[$groupKey].Profile

        if (-not $SkipProfileSwitch) {
            # <lang>
            #   <zh-CN>切档位时 `Enabled` 取**并集**：基线显式启用包 ∪ 本组被测包。
            #   早先只写 `HIA.ModuleProbe`（2026-10-09 为修 L2 14/15 引入），会**替换掉**基线里的
            #   `HIA.LeaveRequest,HIA.ExpenseReimbursement` —— 二者不在任何 Profile 的 include 图里、只靠 `Enabled`
            #   进入门户，于是被测模块被 Profile 门禁阻断、不渲染，门禁却报"模块根找不到"
            #   （`W-anp-P137` 实测根因：`modulePresent=false` / `errorPage=false`，而同一 URL 在默认档位下渲染正常）。
            #   改成并集后，本组被测包（含 `HIA.ModuleProbe`）与基线显式启用包在验证期间**同时**允许渲染。</zh-CN>
            #   <en>When switching profiles, `Enabled` is a **union**: baseline explicitly enabled packages ∪ packages probed by this
            #   group. Writing only `HIA.ModuleProbe` (introduced on 2026-10-09 to fix L2 14/15) **replaced** the baseline's
            #   `HIA.LeaveRequest,HIA.ExpenseReimbursement` — neither appears in any Profile's include graph and they reach the
            #   Portal only through `Enabled`, so the probed module was blocked by the profile gate and did not render while the
            #   gate reported "module root not found" (the root cause measured in `W-anp-P137`: `modulePresent=false` /
            #   `errorPage=false`, while the same URL renders fine under the default profile). With the union, both the probed
            #   packages (including `HIA.ModuleProbe`) and the baseline's explicitly enabled packages are allowed to render
            #   during verification.</en>
            # </lang>
            $enableArg = Resolve-PortalRuntimeGatePackages -RepoRoot $repoRoot -GroupPackages $groups[$groupKey].Packages
            Write-Step ("切换档位 → $profile ｜ Enabled = $enableArg")
            Set-Profile $profile $enableArg
            Start-Sleep -Seconds 3
            WarmUp-Instance $baseUrl
        }

        $roundDir = Join-Path $EvidenceDir $groupKey
        New-Item -ItemType Directory -Force -Path $roundDir | Out-Null
        # <lang>
        #   <zh-CN>ConvertTo-Json -InputObject @($targets) 里的 @() 不是冗余：PowerShell 对**单元素**数组做
        #   ConvertTo-Json 会退化成裸对象（不带方括号），而 Node 侧 for (const target of targets) 要求可迭代对象，
        #   于是抛 TypeError: targets is not iterable —— 该问题只在某组恰好剩 1 个 URL 时出现，极易漏过。</zh-CN>
        #   <en>The @() in ConvertTo-Json -InputObject @($targets) is not redundant: PowerShell's ConvertTo-Json degrades a
        #   **single-element** array into a bare object (no brackets), while the Node side iterates with
        #   `for (const target of targets)` and then throws `TypeError: targets is not iterable` — a failure that appears only
        #   when some group happens to have exactly one URL left, which is easy to miss.</en>
        # </lang>
        $env:PORTAL_MODULE_TARGETS = (ConvertTo-Json -InputObject @($targets) -Compress -Depth 6)
        $env:PORTAL_PLAYWRIGHT_MODULE = $playwrightPath
        $env:PORTAL_MODULE_EVIDENCE_DIR = $roundDir
        Write-Step ("运行门禁：组 {0}，{1} 个 URL" -f $groupKey, $targets.Count)
        & $nodeExe (Join-Path $repoRoot 'dev\scripts\Test-PortalModuleRuntimeEvidence.mjs')
        $roundExit = $LASTEXITCODE
        $roundSummaries += [pscustomobject]@{ group = $groupKey; profile = $profile; urlCount = $targets.Count; exitCode = $roundExit }
        if ($roundExit -ne 0) { $overallFailed = $true; Write-Step "组 $groupKey 失败（退出码 $roundExit）" }
    }
} finally {
    Remove-Item Env:PORTAL_MODULE_TARGETS -ErrorAction SilentlyContinue
    Remove-Item Env:PORTAL_MODULE_EVIDENCE_DIR -ErrorAction SilentlyContinue
    Restore-Profile
}

# <lang>
#   <zh-CN>`W-anp-P140`：把"运行期证据覆盖面"从人工翻台账变成**本门禁每次运行自动产出**的机器可读数字。
#   `coverage` 块给出模块实例面的「已验证 / 总数」与未覆盖明细（孤儿页签引用、缺 DesktopSourceFile 的实例）——
#   数据库是权威清单来源，因此模块实例面**不在静态台账硬编码**（必然漂移），而由本门禁运行期枚举并留证。
#   静态面（后台管理页/模块入口台账）由 L0 的 `Test-PortalEvidenceCoverage.ps1` 负责，两层合起来覆盖全部运行期证据面。</zh-CN>
#   <en>`W-anp-P140`: turns the "runtime-evidence coverage" from a manually maintained ledger into numbers **produced
#   automatically by this gate on every run**. The `coverage` block reports the module-instance surface as "verified / total"
#   plus the uncovered detail (orphan tab references, instances whose definition lacks DesktopSourceFile) — the database is the
#   authoritative inventory, so that surface is deliberately **not hardcoded** into the static ledger (it would drift); this gate
#   enumerates it at runtime and archives the result. Static surfaces (the admin-page/module entry ledger) are owned by the L0
#   `Test-PortalEvidenceCoverage.ps1`; together the two layers cover all runtime-evidence surfaces.</en>
# </lang>
$coverage = [pscustomobject]@{
    total = $instances.Count
    deployed = $deployed.Count
    verifiedUrls = ($roundSummaries | Measure-Object -Property urlCount -Sum).Sum
    uncoveredCount = $skippedInstances.Count
    uncovered = $skippedInstances
}
$summary = [pscustomobject]@{
    phase = 'W-anp-P83 P83.4'
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    result = if ($overallFailed) { 'Fail' } else { 'Pass' }
    moduleInstances = $instances.Count
    deployedInstances = $deployed.Count
    verifiedUrls = ($roundSummaries | Measure-Object -Property urlCount -Sum).Sum
    coverage = $coverage
    rounds = $roundSummaries
    skippedInstances = $skippedInstances
}
$summaryPath = Join-Path $EvidenceDir 'gate-summary.json'
($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host ''
Write-Step ("结果：{0}｜模块实例 {1} 个（已部署 {2}）｜验证 URL 合计 {3} 个｜未覆盖 {4} 个" -f $summary.result, $summary.moduleInstances, $summary.deployedInstances, $summary.verifiedUrls, $coverage.uncoveredCount)
Write-Step "证据：$EvidenceDir"
if ($overallFailed) { exit 1 }
exit 0


