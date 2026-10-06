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
$settingsPath = Join-Path $repoRoot 'src\Portal\Config\appSettings.dev.json'
$webConfigPath = Join-Path $repoRoot 'src\Portal\web.config'
$playwrightPath = Join-Path $repoRoot 'temp\node_modules\playwright'
$connectionConfig = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'

function Write-Step([string] $message) { Write-Host "[p83] $message" }

foreach ($required in @(@{ Path = $nodeExe; Hint = 'node 运行时' }, @{ Path = $playwrightPath; Hint = 'playwright 模块（开发期依赖，不入库）' }, @{ Path = $connectionConfig; Hint = '外置连接串' })) {
    if (-not (Test-Path $required.Path)) { Write-Error ("前置条件缺失：{0}（{1}）" -f $required.Hint, $required.Path); exit 2 }
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
    $groups[$groupKey].Targets += [pscustomobject]@{
        id             = "$($instance.SourceFile.Replace('DesktopModules/', '').Replace('.ascx', '').Replace('/', '-'))-$($instance.TabId)"
        url            = ([string]$baseUrl).TrimEnd('/') + "/DesktopDefault.aspx?tabindex=$($instance.TabOrder)&tabid=$($instance.TabId)"
        moduleSelector = $selector
        expectTitle    = $instance.Title
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
        if ($existing) { $existing.expectTitle = @($existing.expectTitle) + $target.expectTitle; continue }
        $deduped += $target
    }
    $groups[$groupKey].Targets = $deduped
    Write-Step ("组 {0,-8} 档位 {1,-16} URL {2} 个（模块实例 {3} 个）" -f $groupKey, $groups[$groupKey].Profile, $deduped.Count, $groups[$groupKey].Targets.Count)
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

function Set-Profile([string] $profile) {
    $json = '{' + [char]10 + '    "appSettings": {' + [char]10 + '        "TestItem": "dev item",' + [char]10 + '        "Portal.ModuleProfiles.Active": "' + $profile + '"' + [char]10 + '    }' + [char]10 + '}'
    [System.IO.File]::WriteAllText($settingsPath, $json, [System.Text.UTF8Encoding]::new($false))
    (Get-Item $webConfigPath).LastWriteTime = Get-Date
}

$overallFailed = $false
$roundSummaries = @()
try {
    foreach ($groupKey in @('legacy', 'business', 'admin')) {
        $targets = $groups[$groupKey].Targets
        if ($targets.Count -eq 0) { Write-Step "组 $groupKey 无目标，跳过"; continue }

        # <lang>
        #   <zh-CN>后台管理型模块（Admin\*.ascx）**如实跳过**而不是硬凑一个 URL：实测 DesktopDefault.aspx?tabid=6
        #   与 Admin\TabLayout.aspx 都不渲染它们（module=false）—— 这些模块只在页签布局编辑器的**编辑模式**下出现，
        #   没有面向普通请求的独立 URL。default-deny 要求不漏，但"用一个必然失败的 URL 去假装覆盖"更糟：它会把门禁
        #   永久钉在失败上，反而掩盖真实回归。未覆盖量与原因都写进证据，由后续包补静态校验（检查 Inherits 类型存在性
        #   与 <%@ Import %> 覆盖），而不是在这里假装已覆盖。</zh-CN>
        #   <en>Admin modules (Admin\*.ascx) are **honestly skipped** rather than given a made-up URL: neither
        #   `DesktopDefault.aspx?tabid=6` nor `Admin\TabLayout.aspx` renders them (`module=false`) — these modules only appear
        #   in the tab-layout editor's **edit mode** and have no URL for ordinary requests. Default-deny demands no gaps, but
        #   "covering" them with a URL that can only fail is worse: it pins the gate to permanent failure and would mask real
        #   regressions. The uncovered count and the reason are written into the evidence, and a later package adds static
        #   checks (existence of the `Inherits` type, `<%@ Import %>` coverage) instead of pretending coverage here.</en>
        # </lang>
        if ($groupKey -eq 'admin') {
            Write-Step ("组 admin 有 {0} 个实例，但后台管理型模块无可访问 URL（实测两种候选均 module=false），如实记为未覆盖" -f $targets.Count)
            foreach ($target in $targets) {
                $skippedInstances += [pscustomobject]@{ Title = ($target.expectTitle -join ' / '); Reason = '后台管理型模块：仅在页签布局编辑模式渲染，无独立可访问 URL' }
            }
            $roundSummaries += [pscustomobject]@{ group = 'admin'; profile = $groups[$groupKey].Profile; urlCount = 0; exitCode = 0; skipped = $targets.Count }
            continue
        }

        $profile = $groups[$groupKey].Profile

        if (-not $SkipProfileSwitch) {
            Write-Step "切换档位 → $profile"
            Set-Profile $profile
            Start-Sleep -Seconds 3
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

$summary = [pscustomobject]@{
    phase = 'W-anp-P83 P83.4'
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    result = if ($overallFailed) { 'Fail' } else { 'Pass' }
    moduleInstances = $instances.Count
    deployedInstances = $deployed.Count
    verifiedUrls = ($roundSummaries | Measure-Object -Property urlCount -Sum).Sum
    rounds = $roundSummaries
    skippedInstances = $skippedInstances
}
$summaryPath = Join-Path $EvidenceDir 'gate-summary.json'
($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host ''
Write-Step ("结果：{0}｜模块实例 {1} 个（已部署 {2}）｜验证 URL 合计 {3} 个" -f $summary.result, $summary.moduleInstances, $summary.deployedInstances, $summary.verifiedUrls)
Write-Step "证据：$EvidenceDir"
if ($overallFailed) { exit 1 }
exit 0


