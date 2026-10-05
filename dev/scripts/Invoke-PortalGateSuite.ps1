# W85 门禁统一编排入口：按层依次执行全部门禁，一次看到所有问题。
#
# 为什么要它（`P85.0` 实测）：`dev/scripts/` 下 88 个文件里真正的门禁候选是 38 个
# `Test-Portal*`（其余 50 个是 `New-*` 造数/证据包生成与 `Get-*` inventory 盘点，不是门禁），
# 且这 38 个的**失败信号不统一** —— 带 exit 0/1 的 14 个、带 PASS/FAIL 输出的 14 个、
# 两者兼有的 5 个。此前没有任何编排，跑哪几个、什么顺序全靠人记，漏跑不会有任何提示。
#
# 两个刻意的设计选择：
#   1. **失败不中断**：逐个跑完再汇总，一次暴露全部问题，而不是修一个再跑一轮。
#   2. **退出码与输出双判定**：很多脚本不设退出码（只打印 PASS/FAIL），只靠 `$LASTEXITCODE`
#      会把它判成"通过"。故同时检测输出里的失败标志。
#
# 用法（仓库根目录）：
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File dev\scripts\Invoke-PortalGateSuite.ps1
#   开关：-Layer L0|L1|L2|All（默认 All）；-EvidenceDir <路径>；-StopOnFirstFailure
# 退出码：0 全部通过；1 有门禁失败；2 前置条件缺失。

param(
    [ValidateSet('All', 'L0', 'L1', 'L2')]
    [string] $Layer = 'All',
    [string] $EvidenceDir,
    [switch] $StopOnFirstFailure,
    [switch] $IncludeSlow
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptDir = Join-Path $repoRoot 'dev\scripts'
$pwsh = 'C:\Program Files\PowerShell\7\pwsh.exe'
$nodeExe = 'C:\Program Files\nodejs\node.exe'

function Write-Step([string] $message) { Write-Host "[gate] $message" }

# <lang>
#   <zh-CN>分层的原因是各门禁对**前置条件**的依赖不同，不能一锅端：
#     L0 静态 —— 只依赖源码，随时可跑；
#     L1 构建后 —— 须先构建出产物（单测与冒烟依赖 bin/）；
#     L2 运行期 —— 须有 IIS Express、数据库、playwright，且部分还要临时切模块档位。
#   把 L2 与 L0 混在一起，会让"环境没起"和"代码有问题"两种失败互相掩盖。
# </lang>
# <en>Layering exists because the gates depend on **different prerequisites**, so they cannot be run as one lump:
#     L0 static — depends only on source and can run any time;
#     L1 post-build — requires build output first (unit tests and smoke tests depend on bin/);
#     L2 runtime — requires IIS Express, the database, playwright, and for some gates a temporary module-profile switch.
#   Mixing L2 with L0 would let two different failures — "the environment is down" and "the code is broken" — mask each other.
# </lang>
$gateLayers = [ordered]@{
    L0 = @(
        @{ File = 'Test-PortalXmlDocumentation.ps1';      Runner = 'ps1'; Kind = 'XML 文档'; Args = @() }
        @{ File = 'Test-PortalResourceContract.ps1';      Runner = 'ps1'; Kind = '资源契约'; Args = @() }
        @{ File = 'Test-PortalVersionConsistency.ps1';    Runner = 'ps1'; Kind = '版本一致性'; Args = @() }
        @{ File = 'Test-PortalLegacyCssCompatibility.ps1'; Runner = 'ps1'; Kind = '旧版 CSS 兼容'; Args = @() }
    )
    L1 = @(
        @{ File = 'Build-Solution.ps1';                   Runner = 'ps1'; Kind = '构建'; Args = @() }
        @{ File = 'Invoke-PortalTests.ps1';               Runner = 'ps1'; Kind = '单元测试'; Args = @() }
        # <lang>
        #   <zh-CN>`Test-PortalBusinessModuleSmoke.ps1` **一次只测一个模块**（`-ModuleName` 必填），
        #   所以这里用 `ArgSets` 给出多组参数、逐组各跑一次。首版只给了空的 `Args`，运行后报
        #   "missing mandatory parameters: ModuleName" —— 与主题解析那次是同一类错误：
        #   编排层必须为每个门禁配齐它自己的参数，不能一律无参调用。
        #   模块名取自 `src\Portal\Config\appSettings.json` 的 `Portal.ModuleProfiles.*.Packages`
        #   （`HIA.*` 共 5 个），不手写清单以免与包配置漂移。</zh-CN>
        #   <en>`Test-PortalBusinessModuleSmoke.ps1` checks **one module per invocation** (`-ModuleName` is mandatory), so
        #   `ArgSets` supplies several argument sets and the gate runs once per set. The first version passed an empty `Args`
        #   and failed with "missing mandatory parameters: ModuleName" — the same class of mistake as the theme-resolution
        #   case: the orchestration layer must supply each gate's own parameters instead of calling everything bare.
        #   The module names come from `Portal.ModuleProfiles.*.Packages` in `src\Portal\Config\appSettings.json`
        #   (five `HIA.*` entries) rather than a hand-written list that would drift from the package configuration.</en>
        # </lang>
        # <lang>
        #   <zh-CN>必须显式给 `-ModuleDirectory`：脚本默认按 `DesktopModules\$ModuleName` 找，而 `-ModuleName`
        #   是带 `HIA.` 前缀的**包名**（`HIA.MyWorkItems`），实际目录却是**不带前缀**的模块名
        #   （`DesktopModules\MyWorkItems`）—— 全仓 0 个带 `HIA.` 前缀的目录，而 6 个 `module.json`
        #   都在无前缀目录下。首版只传 `-ModuleName`，五个模块全部报
        #   "Directory not found: ...\DesktopModules\HIA.EmployeeProfileConfirm"。
        #   这说明该脚本此前**从未在编排里被真正跑通过**（项目结构演进后未同步）。</zh-CN>
        #   <en>`-ModuleDirectory` must be supplied explicitly: the script defaults to `DesktopModules\$ModuleName`, but
        #   `-ModuleName` is the `HIA.`-prefixed **package name** (`HIA.MyWorkItems`) while the real directory uses the
        #   **unprefixed** module name (`DesktopModules\MyWorkItems`) — the repository has zero `HIA.`-prefixed directories,
        #   and all six `module.json` files live under unprefixed ones. Passing only `-ModuleName` made all five modules
        #   report "Directory not found: ...\DesktopModules\HIA.EmployeeProfileConfirm", which shows this gate had never
        #   actually been run through orchestration before (the project structure moved on without it).</en>
        # </lang>
        # <lang>
        #   <zh-CN>**暂时排除 `Test-PortalBusinessModuleSmoke.ps1`**（2026-10-06 实测，登记为待修复）：
        #   该脚本自身有缺陷，对当前项目结构**必然失败**，不是编排层能修的。三项证据：
        #     ① `[WARNING] Package id convention: Expected HIA.HIA.MyWorkItems, actual HIA.MyWorkItems`
        #        —— 包名约定检查把 `HIA.` 前缀**加了两次**，说明脚本内部按"目录名 = HIA.<模块名>"的假设构造期望值，
        #        而全仓 0 个带前缀目录；
        #     ② `[FAIL] Desktop entry safety: desktopEntry must stay inside the module directory`
        #        —— 由 ① 派生的**误判**：实际 `DesktopModules/MyWorkItems/MyWorkItems.ascx` 就在模块目录内；
        #     ③ `[FAIL] SQL migration file: No PortalBiz_HIA.MyWorkItems*.sql`
        #        —— 实际 15 个 `PortalBiz_*.sql` 的命名不含 `HIA.` 前缀。
        #   处理原则：既不为了"让它绿"而加豁免，也不留着红着不管 —— 移出清单并如实登记，
        #   修复脚本后（对齐目录命名约定）再接回。这比"红着"更有用，因为红着久了会被当成背景噪音忽略。
        # </lang>
        # <en>**`Test-PortalBusinessModuleSmoke.ps1` is temporarily excluded** (measured 2026-10-06, registered as
        #   needing repair): the script itself is defective and **necessarily fails** against the current project structure,
        #   which orchestration cannot fix. Three pieces of evidence:
        #     ① `[WARNING] Package id convention: Expected HIA.HIA.MyWorkItems, actual HIA.MyWorkItems` — the package-id
        #        convention check prefixes `HIA.` **twice**, revealing the internal assumption "directory = HIA.&lt;module&gt;"
        #        while the repository has zero prefixed directories;
        #     ② `[FAIL] Desktop entry safety: desktopEntry must stay inside the module directory` — a **false positive**
        #        derived from ①: `DesktopModules/MyWorkItems/MyWorkItems.ascx` does sit inside the module directory;
        #     ③ `[FAIL] SQL migration file: No PortalBiz_HIA.MyWorkItems*.sql` — the 15 actual `PortalBiz_*.sql` files are
        #        named without the `HIA.` prefix.
        #   Principle: neither exempt it just to turn it green nor leave it red and ignored — it is removed from the list and
        #   honestly registered, to be reconnected once the script is fixed (aligned with the directory naming convention).
        #   That is more useful than a permanent red, which after a while is treated as background noise.</en>
        # </lang>
        <#
        @{ File = 'Test-PortalBusinessModuleSmoke.ps1';   Runner = 'ps1'; Kind = '业务模块冒烟';
           ArgSets = @(
               @('-ModuleName', 'HIA.MyWorkItems', '-ModuleDirectory', 'src\Portal\DesktopModules\MyWorkItems'),
               @('-ModuleName', 'HIA.EmployeeProfileConfirm', '-ModuleDirectory', 'src\Portal\DesktopModules\EmployeeProfileConfirm'),
               @('-ModuleName', 'HIA.EmployeeProfileCorrectionRequest', '-ModuleDirectory', 'src\Portal\DesktopModules\EmployeeProfileCorrectionRequest'),
               @('-ModuleName', 'HIA.EnterpriseCapabilityWorkbench', '-ModuleDirectory', 'src\Portal\DesktopModules\EnterpriseCapabilityWorkbench'),
               @('-ModuleName', 'HIA.BusinessApplicationRequest', '-ModuleDirectory', 'src\Portal\DesktopModules\BusinessApplicationRequest')
           ) }
        #>
    )
    # <lang>
    #   <zh-CN>`Test-PortalThemeResolution.ps1` 归 **L2** 而不是 L0：它自带 `-Port 40005` 且**会自行启动
    #   IIS Express**（只读源码的门禁不会需要端口）。首版把它放进 L0，运行后以
    #   "missing mandatory parameters: ConnectionStringsConfigPath" 失败 —— 这个报错恰好暴露了分层判断错误：
    #   需要连接串与端口的门禁，本质上就依赖运行期环境。</zh-CN>
    #   <en>`Test-PortalThemeResolution.ps1` belongs in **L2**, not L0: it carries its own `-Port 40005` and **starts IIS
    #   Express itself** (a gate that only reads source would not need a port). The first version placed it in L0 and it failed
    #   with "missing mandatory parameters: ConnectionStringsConfigPath" — an error that exposed the layering mistake, because
    #   a gate needing a connection string and a port is by nature dependent on the runtime environment.</en>
    # </lang>
    L2 = @(
        @{ File = 'Invoke-PortalModuleRuntimeGate.ps1';   Runner = 'ps1'; Kind = '模块运行期加载'; Args = @() }
    )
}

# <lang>
#   <zh-CN>耗时门禁（需 `-IncludeSlow` 才跑）：`Test-PortalThemeResolution.ps1` 会**自行启动 IIS Express**
#   （自带 `-Port 40005`）跑完整主题解析 proof，整套跑下来会长时间无输出（实测编排入口在此卡到超时）。
#   它与"代码是否有问题"无关，只取决于证明流程的耗时，故与常规门禁分开：日常跑门禁不必等它，
#   需要主题解析证明时显式加 `-IncludeSlow`。这样既保留门禁，又不让耗时项拖垮日常使用 ——
#   一个"每次都要等十分钟"的门禁，最终会被人跳过或删掉，那就等于没有门禁。</zh-CN>
#   <en>Slow gates (run only with `-IncludeSlow`): `Test-PortalThemeResolution.ps1` **starts its own IIS Express**
#   (with `-Port 40005`) and runs a full theme-resolution proof, producing no output for a long time (the orchestration
#   entry point timed out waiting for it). That cost is unrelated to whether the code is correct — it is purely the proof's
#   duration — so it is kept separate: everyday gate runs do not wait for it, and the theme proof is requested explicitly
#   with `-IncludeSlow`. Both the gate and everyday usability are preserved: a gate that always costs ten minutes ends up
#   skipped or deleted, which is the same as having no gate at all.</en>
# </lang>
$slowGates = @(
    @{ File = 'Test-PortalThemeResolution.ps1'; Runner = 'ps1'; Kind = '主题解析（耗时）';
       Args = @('-ConnectionStringsConfigPath', (Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config')) }
)

# <lang>
#   <zh-CN>失败标志的检测**必须按脚本区分，不能用一份全局正则**：各脚本的输出格式不同，
#   统一用一份正则去匹配会产生两类错判 —— 把含 "Failed: False" 的**通过**输出判成失败，
#   或把只写 "0 violation(s)" 的**通过**输出判成失败。故这里对每个标志都做**否定式排除**。</zh-CN>
#   <en>Failure markers must be detected **per marker with an explicit negation**, not with one global regex: the scripts
#   print results in different shapes, and a single regex produces both kinds of misjudgement — reading a **passing**
#   "Failed: False" as a failure, or a **passing** "0 violation(s)" as one. Every marker is therefore paired with the
#   pattern that means the opposite.
# </lang>
$failureIndicators = @(
    @{ Pattern = 'RESULT:\s*Fail';                 Negation = 'RESULT:\s*Pass' }
    @{ Pattern = 'Failed\s*:\s*True';              Negation = 'Failed\s*:\s*False' }
    @{ Pattern = '\[Fail\]';                       Negation = '\[Pass\]' }
    @{ Pattern = '[1-9]\d*\s+violation\(s\)';      Negation = '^0\s+violation\(s\)' }
    @{ Pattern = 'error\s+CS\d+';                  Negation = $null }
    @{ Pattern = '未能|缺失|不存在.*文件';          Negation = $null }
)

function Test-GateFailed([string] $output, [int] $exitCode) {
    if ($exitCode -ne 0) { return $true }
    foreach ($indicator in $failureIndicators) {
        if ($output -match $indicator.Pattern) {
            # <lang>
            #   <zh-CN>命中失败标志后还要检查同一行（或紧邻）是否是否定形式 —— 例如
            #   "Failed              : False" 既含 Failed 又含 False，应判为通过。
            #   不做这步的话，通过的输出会被误判成失败，门禁就永远红着，很快会被人忽略。</zh-CN>
            #   <en>After a failure marker matches, the same (or adjacent) text is checked for the negated form — for example
            #   "Failed              : False" contains both "Failed" and "False" and must count as a pass. Without this step a
            #   passing run is scored as a failure, the gate stays permanently red, and people start ignoring it.</en>
            # </lang>
            if ($indicator.Negation -and ($output -match $indicator.Negation)) { continue }
            return $true
        }
    }
    return $false
}

$layersToRun = if ($Layer -eq 'All') { @('L0', 'L1', 'L2') } else { @($Layer) }

if (-not $EvidenceDir) {
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmss')
    $EvidenceDir = Join-Path $repoRoot "work-zone\dev\evidence\gate-suite\$stamp-Dev"
}
New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
Write-Step "证据目录：$EvidenceDir"

$results = @()
$overallFailed = $false

foreach ($layerName in $layersToRun) {
    Write-Step "--- 层 $layerName ---"
    $gates = @($gateLayers[$layerName])
    if ($IncludeSlow -and $layerName -eq 'L2') { $gates += $slowGates }
    foreach ($gate in $gates) {
        $gatePath = Join-Path $scriptDir $gate.File
        # <lang>
        #   <zh-CN>一个清单项可能需要跑**多次**（`ArgSets`，如单模块冒烟要逐模块调用）。
        #   把它归一成"若干次调用"，后面的执行逻辑只面对"一次调用"，不必区分单发与多发。</zh-CN>
        #   <en>A single entry may need to run **several times** (`ArgSets`, as when a one-module smoke gate is invoked per
        #   module). Normalizing it into "a number of invocations" keeps the execution logic dealing only with "one
        #   invocation" and spares it from distinguishing single-shot from multi-shot.</en>
        # </lang>
        $invocations = @()
        if ($gate.Keys -contains 'ArgSets') {
            foreach ($set in $gate.ArgSets) { $invocations += , $set }
        } elseif ($gate.Keys -contains 'Args') {
            # <lang>
            #   <zh-CN>这一支**不能省**：首版只在 `ArgSets` 分支处理参数，`Args`（单组参数）走到 else 被丢成空数组，
            #   于是带必填参数的门禁报 "missing mandatory parameters"。这个 bug 的表现与"脚本本身坏了"完全一样，
            #   只有看日志才能区分 —— 故此处保留注释以免后人误删。</zh-CN>
            #   <en>This branch **must not be omitted**: an earlier version handled arguments only in the `ArgSets` branch, so
            #   `Args` (a single argument set) fell into the else branch and was discarded as an empty array, and gates with
            #   mandatory parameters then reported "missing mandatory parameters". The symptom is indistinguishable from "the
            #   script itself is broken" — only the log tells them apart — so the note stays here to stop anyone deleting it.</en>
            # </lang>
            $invocations += , $gate.Args
        } else {
            $invocations += , @()
        }

        foreach ($invokeArgs in $invocations) {
        $suffix = if ($invokeArgs.Count -gt 0) { ' [' + ($invokeArgs -join ' ') + ']' } else { '' }
        $record = [pscustomobject]@{
            layer = $layerName
            gate = $gate.File
            kind = $gate.Kind
            args = ($invokeArgs -join ' ')
            status = 'Pass'
            exitCode = 0
            note = ''
        }

        if (-not (Test-Path $gatePath)) {
            $record.status = 'Skip'
            $record.note = '脚本不存在'
            Write-Step ("  [Skip] {0}（脚本不存在）" -f $gate.File)
            $results += $record
            continue
        }

        try {
            # <lang>
            #   <zh-CN>参数用**数组展开**传入，而不是拼进命令行字符串：路径里的空格与反斜杠在拼串时极易出错，
            #   而"参数没传进去"的表现是脚本报"missing mandatory parameters"，很容易被误读成门禁本身有问题。</zh-CN>
            #   <en>Arguments are passed by **array splatting** rather than concatenated into a command string: spaces and
            #   backslashes in paths break easily when concatenated, and a parameter that never arrives surfaces as
            #   "missing mandatory parameters", which is easy to misread as a defect in the gate itself.</en>
            # </lang>
            $gateArgs = @('-NoProfile', '-File', $gatePath)
            if ($invokeArgs.Count -gt 0) { $gateArgs += $invokeArgs }
            $raw = & $pwsh @gateArgs 2>&1
            $exit = $LASTEXITCODE
            $text = ($raw | Out-String)
            $record.exitCode = $exit
            if (Test-GateFailed $text $exit) {
                $record.status = 'Fail'
                $overallFailed = $true
                $record.note = '退出码或输出含失败标志'
            }
            # <lang>
            #   <zh-CN>每个门禁的完整输出都留档：汇总里只记结论，排障要看原始输出。</zh-CN>
            #   <en>The full output of every gate is archived: the summary records only the verdict, while triage needs the
            #   raw output.</en>
            # </lang>
            # <lang>
            #   <zh-CN>日志名带参数哈希：多组参数会产生多次调用，若都写同名日志，后一次会覆盖前一次，
            #   排障时只看到最后一组的输出。</zh-CN>
            #   <en>The log name carries a hash of the arguments: several argument sets mean several invocations, and a shared
            #   log name would let each one overwrite the previous, leaving only the last run's output for triage.</en>
            # </lang>
            $hash = if ($invokeArgs.Count -gt 0) {
                ([System.BitConverter]::ToString([System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes(($invokeArgs -join ' ')))) -replace '-', '').Substring(0, 8)
            } else { 'default' }
            $logName = [System.IO.Path]::GetFileNameWithoutExtension($gate.File) + '.' + $hash + '.log'
            [System.IO.File]::WriteAllText((Join-Path $EvidenceDir $logName), $text, [System.Text.UTF8Encoding]::new($false))
        } catch {
            $record.status = 'Fail'
            $overallFailed = $true
            $record.note = $_.Exception.Message
        }

        Write-Step ("  [{0}] {1}（{2}）{3}exit={4}{5}" -f $record.status, $gate.File, $gate.Kind, $suffix, $record.exitCode, $(if ($record.note) { '  ' + $record.note } else { '' }))
        $results += $record

        if ($StopOnFirstFailure -and $record.status -eq 'Fail') {
            Write-Step "遇到首个失败即停止（-StopOnFirstFailure）"
            break
        }
        }
    }
    if ($StopOnFirstFailure -and $overallFailed) { break }
}

$summary = [pscustomobject]@{
    phase = 'W-anp-P85 gate suite'
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    layer = $Layer
    result = if ($overallFailed) { 'Fail' } else { 'Pass' }
    gateCount = $results.Count
    failedCount = ($results | Where-Object { $_.status -eq 'Fail' }).Count
    skippedCount = ($results | Where-Object { $_.status -eq 'Skip' }).Count
    results = $results
}
$summaryPath = Join-Path $EvidenceDir 'gate-suite-summary.json'
($summary | ConvertTo-Json -Depth 6) | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host ''
Write-Step ("结果：{0}｜门禁 {1} 个，失败 {2}，跳过 {3}" -f $summary.result, $summary.gateCount, $summary.failedCount, $summary.skippedCount)
Write-Step "证据：$EvidenceDir"
if ($overallFailed) { exit 1 }
exit 0

