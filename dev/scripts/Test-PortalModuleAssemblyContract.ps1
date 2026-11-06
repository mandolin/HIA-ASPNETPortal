<#
.SYNOPSIS
<lang>
  <zh-CN>模块装配态契约门禁（L0 静态）：把定稿 §二.2 的机器断言与部署开关缺省固化为可复跑判据。</zh-CN>
  <en>Module-assembly-state contract gate (L0 static): pins the §二.2 machine assertions and the deployment-switch default from the ruling into a rerunnable check.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>五项检查（A1–A5），全部为静态只读、不连库、不启动站点、不写配置：

  A1 台账里每个 `packageId` 都能在受信任包（`src/Portal/DesktopModules/*/module.json` 的 packageId）解析到 ——
      防「登记腐化 / 指向不存在的包」；空 entries 合法，不误判。
  A2 `assemblyMode` 只允许 `CodeBehind` / `CodeFile` 两值；出现第三值 / 空值 / 类型错误即失败（fail-closed）。
  A3 装配态与「包启用状态（PortalCfg_ModulePackageStates）/ 能力归属（PortalCapabilityRegistry）/
      Profile gate（Portal.ModuleProfiles.*）」**四者严格分离**：台账文件不得被这些系统的代码读取或写入。
      实现为「台账路径 token 仅允许出现在装配态子系统 + 测试 + 离线工具 + csproj 的代码行（注释提及不算）」，
      任何其它 .cs/.ps1 在真实代码行引用台账路径即失败。
  A4 台账不得被 `PortalModulePackage.cs` 的 manifest 解析链读取或写入（防与 F4 混流）。
  A5 部署级开关 `Portal.ModuleAssembly.AllowCodeFile` 必须显式缺省关闭：值为 true 即失败；false 或缺失即合规。

  证明边界：本门禁只证明「台账契约自洽、台账与其它系统物理隔离、开关缺省关闭」，
  不证明装配态被实际切换、不证明运行期加载、也不替代运行期渲染 / 审计证据。归 L0。</zh-CN>
  <en>Five static read-only checks (A1–A5), no database, no site start-up, no configuration writes:

  A1 every `packageId` in the ledger resolves to a trusted package (the packageId of `src/Portal/DesktopModules/*/module.json`)
      — guards against "registry rot / pointing at a non-existent package"; an empty entries object is legal.
  A2 `assemblyMode` allows only `CodeBehind` / `CodeFile`; a third value / empty / wrong type fails (fail-closed).
  A3 assembly state is strictly separated from "package-enable state (PortalCfg_ModulePackageStates) / capability
      ownership (PortalCapabilityRegistry) / profile gate (Portal.ModuleProfiles.*)": the ledger file must not be
      read or written by those systems' code. Implemented as "the ledger-path token may appear only in code lines
      (comments do not count) of the assembly-state subsystem + tests + offline tool + csproj"; any other .cs/.ps1
      referencing the ledger path in real code fails.
  A4 the ledger must not be read or written by `PortalModulePackage.cs`'s manifest-parse chain (avoids mixing with F4).
  A5 the deployment switch `Portal.ModuleAssembly.AllowCodeFile` must be explicitly off by default: a true value fails;
      false or absent is compliant.

  Proof boundary: this gate proves only that "the ledger contract is self-consistent, the ledger is physically isolated
  from other systems, and the switch defaults off" — it does not prove assembly mode is actually switched, that runtime
  loading occurs, nor replace runtime rendering / audit evidence. Classified L0.</en>
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

# <lang>
#   <zh-CN>JSON 一律用 -AsHashtable 解析；但 PowerShell 7 的 -AsHashtable 实际返回内部 OrderedHashtable，
#   其 ContainsKey 在字符串拼接上下文与布尔 if 上下文会被 DLR 解析出不同结果（同一条调用一会儿 True 一会儿 False）。
#   因此递归塌缩为普通 [hashtable]，键访问（.Keys / .ContainsKey）才稳定无歧义。</zh-CN>
#   <en>Parse JSON always with -AsHashtable; but PowerShell 7's -AsHashtable actually returns an internal OrderedHashtable whose
#   ContainsKey is resolved differently by the DLR in string-concat vs boolean-if contexts (the same call returns True then False).
#   So recursively collapse to a plain [hashtable] so key access (.Keys / .ContainsKey) is stable and unambiguous.</en>
# </lang>
function ConvertToPlainHashtable($obj) {
    if ($obj -is [System.Collections.IDictionary]) {
        $h = @{}
        foreach ($k in $obj.Keys) { $h[$k] = ConvertToPlainHashtable $obj[$k] }
        return $h
    }
    if ($obj -is [System.Collections.IEnumerable] -and $obj -isnot [string]) {
        $a = @()
        foreach ($i in $obj) { $a += @(ConvertToPlainHashtable $i) }
        return $a
    }
    return $obj
}

function Read-Json([string] $path) {
    $raw = [System.IO.File]::ReadAllText($path, $utf8) | ConvertFrom-Json -AsHashtable
    return ConvertToPlainHashtable $raw
}

# <lang>
#   <zh-CN>判键是否存在一律用 PowerShell 原生的「键 -in 哈希表.Keys」，不用 .NET 的 ContainsKey 方法：
#   本环境 PowerShell 7 的 DLR 对 ContainsKey 在字符串拼接上下文与布尔 if 上下文会解析出不同结果（同一条调用一会儿 True 一会儿 False），
#   用 -in 集合运算符可彻底规避。</zh-CN>
#   <en>Key-existence is always checked via PowerShell-native `"key" -in $ht.Keys`, never the .NET ContainsKey method:
#   this PowerShell 7 build's DLR resolves ContainsKey differently in string-concat vs boolean-if contexts (the same call returns True then False);
#   the -in collection operator sidesteps it entirely.</en>
# </lang>
function Test-HasKey($ht, [string] $key) {
    if ($null -eq $ht -or $ht -isnot [System.Collections.IDictionary]) { return $false }
    return $key -in $ht.Keys
}

function Add-Fail([string] $id, [string] $message) {
    $failures.Add(('{0} {1}' -f $id, $message))
}

# <lang>
#   <zh-CN>判断一行是否为注释行或位于 <lang> 块内（注释提及台账路径不算代码引用，避免 A3 误报）。</zh-CN>
#   <en>Decide whether a line is a comment or sits inside a <lang> block (a comment mention of the ledger path is not a code reference, avoiding A3 false positives).</en>
# </lang>
function Test-IsNonCode([string] $line, [ref] $inLang) {
    $t = $line.Trim()
    if ($t -match '<lang>') { $inLang.Value = $true }
    if ($t -match '</lang>') { $inLang.Value = $false; return $true }
    if ($inLang.Value) { return $true }
    if ($t.StartsWith('///') -or $t.StartsWith('//') -or $t.StartsWith('*')) { return $true }
    return $false
}

# <lang>
#   <zh-CN>台账路径 token：真实文件名为 module-assembly-state.json（与 Web.config / csproj / 离线工具一致）。</zh-CN>
#   <en>Ledger-path token: the real file name is module-assembly-state.json (consistent with Web.config / csproj / the offline tool).</en>
# </lang>
$ledgerToken = 'module-assembly-state'
$ledgerPath = Join-Path $RepoRoot 'src\Portal\Config\module-assembly-state.json'

# ----------------------------------------------------------------------------
# A0 台账存在性与基本形状
# ----------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $ledgerPath)) {
    Add-Fail 'A0' ('找不到台账文件 ' + $ledgerPath)
    Write-Output '[FAIL] A0 找不到台账文件 src/Portal/Config/module-assembly-state.json'
    Write-Output ('RESULT: Fail（' + $failures.Count + ' 项）')
    exit 1
}

$ledger = $null
try {
    $ledger = Read-Json $ledgerPath
}
catch {
    Add-Fail 'A0' ('台账 JSON 解析失败：' + $_.Exception.Message)
}

# <lang>
#   <zh-CN>schemaVersion 必须为整数 1（与产品侧 PortalModuleAssemblyState.TryParse 口径一致：C# Int32、PowerShell Int64 同属整数类型集合）。</zh-CN>
#   <en>schemaVersion must be integer 1 (consistent with the product-side PortalModuleAssemblyState.TryParse contract: C# Int32 and PowerShell Int64 are both the integer-type set).</en>
# </lang>
if ($null -ne $ledger) {
    $schemaIsInteger = $false
    $schemaValue = $null
    if (Test-HasKey $ledger 'schemaVersion') {
        $schemaValue = $ledger['schemaVersion']
        $schemaIsInteger = ($schemaValue -is [int]) -or ($schemaValue -is [long]) -or ($schemaValue -is [short]) -or ($schemaValue -is [byte])
    }
    if (-not $schemaIsInteger -or [long]$schemaValue -ne 1) {
        Add-Fail 'A0' ('台账 schemaVersion 必须为整数 1，实际=' + $schemaValue)
    }
    if (-not (Test-HasKey $ledger 'entries') -or $ledger['entries'] -isnot [System.Collections.IDictionary]) {
        Add-Fail 'A0' '台账缺少 entries 对象'
    }
}

# ----------------------------------------------------------------------------
# A1 每个 packageId 可解析为受信任包 + A2 两值 fail-closed
# ----------------------------------------------------------------------------
# <lang>
#   <zh-CN>受信任包来自 DesktopModules/*/module.json 的 packageId，与 Test-PortalModulePackageContract 的 M3 同源。</zh-CN>
#   <en>Trusted packages come from the packageId of DesktopModules/*/module.json, sharing the source with Test-PortalModulePackageContract's M3.</en>
# </lang>
$trustedIds = New-Object 'System.Collections.Generic.List[string]'
$moduleRoot = Join-Path $RepoRoot 'src\Portal\DesktopModules'
if (Test-Path -LiteralPath $moduleRoot) {
    foreach ($dir in (Get-ChildItem -LiteralPath $moduleRoot -Directory)) {
        $manifestPath = Join-Path $dir.FullName 'module.json'
        if (-not (Test-Path -LiteralPath $manifestPath)) { continue }
        try {
            $manifest = Read-Json $manifestPath
            if ($null -ne $manifest -and (Test-HasKey $manifest 'packageId')) {
                # <lang>
                #   <zh-CN>注意：不可用 $pid —— 它是 PowerShell 只读自动变量（当前进程 ID），对其赋值会抛出“无法覆盖变量 PID”并被外层 try 静默吞掉，导致可信包计数为 0。</zh-CN>
                #   <en>Note: never use $pid here — it is PowerShell's read-only automatic variable (current process id); assigning to it throws "cannot overwrite PID" which the outer try silently swallows, yielding a trusted-package count of 0.</en>
                # </lang>
                $pkgId = [string]$manifest['packageId']
                if ($pkgId.Trim().Length -gt 0 -and -not $trustedIds.Contains($pkgId.Trim())) {
                    $trustedIds.Add($pkgId.Trim())
                }
            }
        }
        catch {
            # <lang>
            #   <zh-CN>单个 manifest 损坏不应让门禁崩溃（崩溃与通过在 CI 里都易被当成绿），降级为可判定空集合。</zh-CN>
            #   <en>A single broken manifest must not crash the gate (a crash and a pass are both easily read as green in CI); degrade to an evaluable empty set.</en>
            # </lang>
        }
    }
}

if ($null -ne $ledger -and (Test-HasKey $ledger 'entries') -and $ledger['entries'] -is [System.Collections.IDictionary]) {
    $entryKeys = @($ledger['entries'].Keys)
    foreach ($key in $entryKeys) {
        if (-not $trustedIds.Contains($key)) {
            Add-Fail 'A1' ('台账条目 packageId 无法解析为受信任包：' + $key)
            continue
        }
        # <lang>
        #   <zh-CN>A2：该条目的 assemblyMode 只能为 CodeBehind / CodeFile。</zh-CN>
        #   <en>A2: this entry's assemblyMode may only be CodeBehind / CodeFile.</en>
        # </lang>
        $entry = $ledger['entries'][$key]
        $mode = $null
        if ($entry -is [System.Collections.IDictionary] -and (Test-HasKey $entry 'assemblyMode')) {
            $mode = $entry['assemblyMode']
        }
        if (($mode -isnot [string]) -or ($mode -ne 'CodeBehind' -and $mode -ne 'CodeFile')) {
            Add-Fail 'A2' ('条目 ' + $key + ' 的 assemblyMode 必须为 CodeBehind/CodeFile 两值之一，实际=' + $mode)
        }
    }
}

# ----------------------------------------------------------------------------
# A3 + A4 台账路径隔离（四分离 / 不被 manifest 链读写）
# ----------------------------------------------------------------------------
# <lang>
#   <zh-CN>允许引用台账路径 token 的文件白名单（按文件名判定，注释提及不计入）：
#   装配态子系统实现、测试、离线校验工具、本契约门禁及其注入回归、以及把文件列为 Content 的 csproj。</zh-CN>
#   <en>Allowlist of files permitted to reference the ledger-path token (judged by file name; comment mentions excluded):
#   the assembly-state subsystem implementation, its tests, the offline check tool, this contract gate and its injection
#   regression, and the csproj that lists the file as Content.</en>
# </lang>
$allowPatterns = @(
    'PortalModuleAssemblyState\.cs$',
    'PortalModuleAssemblyStateTests?\.cs$',
    'PortalModuleAssemblyAudit.*\.cs$',
    'Invoke-PortalModuleAssemblyCheck.*\.ps1$',
    'Test-PortalModuleAssemblyContract.*\.ps1$',
    'Test-PortalModuleAssemblyE2E\.ps1$',
    # <lang>
    #   <zh-CN>发布就绪门禁是定稿 §四.1 指定的封版前装配态复核点（F9），被授权读取台账以核对 CodeFile 收敛态；
    #   这是构建期校验工具，不属于运行时"包启用/能力/Profile"三系统，故作为四分离的显式例外放行。</zh-CN>
    #   <en>The release-readiness gate is the designated pre-seal assembly-state checkpoint (F9) per ruling §4.1; it is authorized to
    #   read the ledger to check CodeFile convergence. As a build-time verification tool it is not one of the runtime
    #   "package-enable / capability / profile" systems, so it is an explicit exception to the four-separation whitelist.</en>
    # </lang>
    'Test-PortalReleaseReadiness\.ps1$',
    '\.csproj$'
)

# <lang>
#   <zh-CN>A4 单独核对 PortalModulePackage.cs（manifest 解析链主文件）：不得引用台账路径或装配态类。</zh-CN>
#   <en>A4 separately checks PortalModulePackage.cs (the main manifest-parse file): it must not reference the ledger path or the assembly-state class.</en>
# </lang>
$pmPath = Join-Path $RepoRoot 'src\Portal\Components\PortalModulePackage.cs'
if (Test-Path -LiteralPath $pmPath) {
    $pmText = [System.IO.File]::ReadAllText($pmPath, $utf8)
    if ($pmText -match [regex]::Escape($ledgerToken) -or $pmText -match 'PortalModuleAssemblyState') {
        Add-Fail 'A4' 'PortalModulePackage.cs 引用了台账路径或 PortalModuleAssemblyState 类（违反台账不被 manifest 链读写）'
    }
}

# <lang>
#   <zh-CN>A3 全仓扫描：找到任何在「真实代码行」引用台账路径 token 的文件，若不在白名单即失败（点名文件）。
#   注释行与 <lang> 块内提及一律忽略，避免审计/权限/设置键里的中文说明性注释被误判。</zh-CN>
#   <en>A3 full-repo scan: any file that references the ledger-path token in a real code line and is not on the allowlist fails (naming the file).
#   Comment lines and text inside <lang> blocks are ignored so explanatory Chinese comments in audit/permission/setting keys are not misjudged.</en>
# </lang>
$scanDirs = @(
    (Join-Path $RepoRoot 'src'),
    (Join-Path $RepoRoot 'dev\scripts')
)
foreach ($scanDir in $scanDirs) {
    if (-not (Test-Path -LiteralPath $scanDir)) { continue }
    $files = Get-ChildItem -LiteralPath $scanDir -Recurse -Include '*.cs', '*.ps1' -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($RepoRoot.Length).TrimStart('\')
        $inLang = $false
        $codeHits = 0
        foreach ($line in [System.IO.File]::ReadAllLines($file.FullName, $utf8)) {
            if (Test-IsNonCode $line ([ref]$inLang)) { continue }
            if ($line -match [regex]::Escape($ledgerToken)) { $codeHits++ }
        }
        if ($codeHits -eq 0) { continue }

        $allowed = $false
        foreach ($pat in $allowPatterns) {
            if ($rel -match $pat) { $allowed = $true; break }
        }
        if (-not $allowed) {
            Add-Fail 'A3' ('台账路径被非装配态子系统代码引用（违反四分离）：' + $rel)
        }
    }
}

# ----------------------------------------------------------------------------
# A5 部署开关显式缺省关闭
# ----------------------------------------------------------------------------
# <lang>
#   <zh-CN>配置分层：先读 Web.config 的 appSettings，再用 Config/appSettings.json 覆盖同名键，与运行时优先级一致
#   （避免「门禁看 Web.config、运行时看 JSON」导致的绿而产品坏，见 Test-PortalModulePackageContract 的同类教训）。</zh-CN>
#   <en>Config layering: read Web.config appSettings first, then override same-named keys with Config/appSettings.json, matching runtime precedence
#   (avoids the "gate inspects Web.config while runtime reads JSON" green-but-broken trap, per the same lesson in Test-PortalModulePackageContract).</en>
# </lang>
$settings = @{}
$webConfig = Join-Path $RepoRoot 'src\Portal\Web.config'
if (Test-Path -LiteralPath $webConfig) {
    $cfgText = [System.IO.File]::ReadAllText($webConfig, $utf8)
    foreach ($m in [regex]::Matches($cfgText, '<add\s+key="([^"]+)"\s+value="([^"]*)"\s*/?>')) {
        $settings[$m.Groups[1].Value] = $m.Groups[2].Value
    }
}
$jsonConfig = Join-Path $RepoRoot 'src\Portal\Config\appSettings.json'
if (Test-Path -LiteralPath $jsonConfig) {
    $jsonRoot = Read-Json $jsonConfig
    $jsonSettings = $jsonRoot
    if (Test-HasKey $jsonRoot 'appSettings') { $jsonSettings = $jsonRoot['appSettings'] }
    if ($jsonSettings -is [System.Collections.IDictionary] -and (Test-HasKey $jsonSettings 'Portal.ModuleAssembly.AllowCodeFile')) {
        $settings['Portal.ModuleAssembly.AllowCodeFile'] = [string]$jsonSettings['Portal.ModuleAssembly.AllowCodeFile']
    }
}

$switchKey = 'Portal.ModuleAssembly.AllowCodeFile'
$switchValue = ''
if (Test-HasKey $settings $switchKey) { $switchValue = [string]$settings[$switchKey] }
$switchOn = $switchValue.Trim().Length -gt 0 -and $switchValue.Trim().ToLowerInvariant() -eq 'true'
if ($switchOn) {
    Add-Fail 'A5' ('部署开关 Portal.ModuleAssembly.AllowCodeFile 必须为显式缺省关闭（false 或缺失），实际=' + $switchValue)
}

# ----------------------------------------------------------------------------
# 汇总
# ----------------------------------------------------------------------------
$entryCount = 0
if ($null -ne $ledger -and (Test-HasKey $ledger 'entries') -and $ledger['entries'] -is [System.Collections.IDictionary]) {
    $entryCount = $ledger['entries'].Keys.Count
}
Write-Output ('受信任包：' + $trustedIds.Count + '；台账条目：' + $entryCount + '；开关 AllowCodeFile=' + $(if ($switchValue.Length -eq 0) { '缺失(合规)' } else { $switchValue }))

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Output ('[FAIL] ' + $failure) }
    Write-Output ('RESULT: Fail（' + $failures.Count + ' 项）')
    exit 1
}

Write-Output '[PASS] 模块装配态契约：台账自洽、四分离、台账不被 manifest 链读写、开关缺省关闭。'
Write-Output 'RESULT: Pass'
exit 0
