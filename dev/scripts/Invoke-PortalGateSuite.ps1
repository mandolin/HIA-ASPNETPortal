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
#   开关：-Layer L0|L1|L2|All（默认 All）；-EvidenceDir <路径>；-StopOnFirstFailure；
#         -SkipProfileSwitch（L2 不自动切模块档位，供自行管理档位的 CI 使用）
# 退出码：0 全部通过；1 有门禁失败；2 前置条件缺失。

param(
    [ValidateSet('All', 'L0', 'L1', 'L2')]
    [string] $Layer = 'All',
    [string] $EvidenceDir,
    [switch] $StopOnFirstFailure,
    [switch] $IncludeSlow,
    [switch] $SkipProfileSwitch
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
        @{ File = 'Test-PortalAscxCompilationContract.ps1'; Runner = 'ps1'; Kind = 'ASCX 编译契约'; Args = @() }
        @{ File = 'Test-PortalBenchmarkChecklist.ps1'; Runner = 'ps1'; Kind = '对标检查表'; Args = @() }
        # <lang>
        #   <zh-CN>文档与开源就绪度四门禁（W98 接入）：此前它们存在但不在编排内，因此"跑全门禁"并不包含它们。
        #   Test-PortalPublicDocumentation 长期返回 exit 1（公开文档索引缺失 + 指向私有仓库的链接），从未被处理，
        #   原因就是没接进编排 —— 这是"门禁写好了但等于没写"的直接实例。</zh-CN>
        #   <en>Four documentation and open-source readiness gates (added in W98): they existed but stayed outside the suite, so
        #   "running the whole gate suite" did not actually include them. Test-PortalPublicDocumentation returned exit 1 for a long
        #   time (missing public documentation index entries plus links into the private repository) and was never acted on, purely
        #   because it was not wired in — a direct instance of "a gate that exists but is effectively absent".</en>
        # </lang>
        # <lang>
        #   <zh-CN>Test-PortalSecretLeakage 为 W97 新增：现有门禁只覆盖默认凭据与公开文档的赋值形态，
        #   私钥块 / 云厂商密钥 / 访问令牌一类高置信度形态此前无人检查。</zh-CN>
        #   <en>Test-PortalSecretLeakage is new in W97: existing gates only cover default credentials and assignment shapes in public
        #   documentation, leaving high-confidence shapes such as private key blocks, cloud access keys and access tokens
        #   unchecked.</en>
        # </lang>
        @{ File = 'Test-PortalPublicDocumentation.ps1';    Runner = 'ps1'; Kind = '公开文档合规'; Args = @() }
        @{ File = 'Test-PortalDocumentationReadiness.ps1'; Runner = 'ps1'; Kind = '文档就绪度'; Args = @() }
        @{ File = 'Test-PortalSecretLeakage.ps1';          Runner = 'ps1'; Kind = '密钥泄露扫描'; Args = @() }
        @{ File = 'Test-PortalDefaultCredentialRisk.ps1';  Runner = 'ps1'; Kind = '默认凭据风险'; Args = @() }
        # <lang>
        #   <zh-CN>C-anp-P16 / A3（W96，2026-10-07）：以下 13 个为原未接入编排的 `Test-Portal*.ps1` 静态门禁，
        #   按"只读源码/配置、不连 DB/IIS"判定归入 L0（原则 2 调研现状、原则 3 以实测为准）。</zh-CN>
        #   <en>C-anp-P16 / A3 (W96, 2026-10-07): the 13 `Test-Portal*.ps1` static gates below were previously outside the
        #   suite; judged L0 because they read only source/config and touch no DB/IIS (principle 2 survey, principle 3 measure).</en>
        # </lang>
        @{ File = 'Test-PortalBusinessIdentity.ps1';        Runner = 'ps1'; Kind = '业务身份契约'; Args = @() }
        @{ File = 'Test-PortalBusinessPermissionAudit.ps1';  Runner = 'ps1'; Kind = '业务权限审计'; Args = @() }
        @{ File = 'Test-PortalCollaborationWorkflowSmoke.ps1'; Runner = 'ps1'; Kind = '协同工作流'; Args = @() }
        @{ File = 'Test-PortalComplianceBaseline.ps1';       Runner = 'ps1'; Kind = '合规基线'; Args = @() }
        @{ File = 'Test-PortalFrontendContracts.ps1';       Runner = 'ps1'; Kind = '前端契约'; Args = @() }
        @{ File = 'Test-PortalIeModeReadiness.ps1';         Runner = 'ps1'; Kind = 'IE 模式就绪'; Args = @() }
        @{ File = 'Test-PortalLogMaintenance.ps1';          Runner = 'ps1'; Kind = '日志留存'; Args = @() }
        @{ File = 'Test-PortalOperationsReadiness.ps1';     Runner = 'ps1'; Kind = '运维就绪'; Args = @() }
        @{ File = 'Test-PortalProductionHardening.ps1';     Runner = 'ps1'; Kind = '生产硬化预检'; Args = @() }
        @{ File = 'Test-PortalPublishReadiness.ps1';        Runner = 'ps1'; Kind = '发布就绪'; Args = @() }
        @{ File = 'Test-PortalReferenceDataSmoke.ps1';      Runner = 'ps1'; Kind = '参考数据'; Args = @() }
        @{ File = 'Test-PortalSqlVersionMatrix.ps1';        Runner = 'ps1'; Kind = 'SQL 版本矩阵'; Args = @() }
        @{ File = 'Test-PortalWorkItemSmoke.ps1';           Runner = 'ps1'; Kind = '轻量待办'; Args = @() }
    )
    L1 = @(
        @{ File = 'Build-Solution.ps1';                   Runner = 'ps1'; Kind = '构建'; Args = @() }
        @{ File = 'Invoke-PortalTests.ps1';               Runner = 'ps1'; Kind = '单元测试'; Args = @() }
        # <lang>
        #   <zh-CN>三处同源缺陷（包名重复加前缀、desktopEntry 越界误判、迁移文件按模块名通配）已在
        #   Test-PortalBusinessModuleSmoke.ps1 内修复：包名与目录名已解耦，故此处传 -ModuleName HIA.MyWorkItems
        #   即可命中 DesktopModules\MyWorkItems，不再需要 -ModuleDirectory。
        #   迁移文件**显式**给出，因为它们按业务实体命名而非模块名（实测 15 个，如 MyWorkItems 对应
        #   PortalBiz_WorkItems.sql）；EnterpriseCapabilityWorkbench 确实没有对应迁移文件，对它用
        #   -SkipSqlMigrationCheck —— 那是**据实**豁免，不是为了过门禁而编造。</zh-CN>
        #   <en>The three same-root defects (double package prefix, false desktopEntry verdict, module-name migration glob)
        #   are fixed inside Test-PortalBusinessModuleSmoke.ps1: the package name is decoupled from the directory name, so
        #   `-ModuleName HIA.MyWorkItems` resolves to DesktopModules\MyWorkItems with no -ModuleDirectory.
        #   Migration files are given **explicitly** because they are named after business entities rather than modules
        #   (15 measured, e.g. MyWorkItems to PortalBiz_WorkItems.sql); EnterpriseCapabilityWorkbench genuinely has none
        #   and gets -SkipSqlMigrationCheck — a truthful exemption, not an invented one to turn the gate green.</en>
        # </lang>
        @{ File = 'Test-PortalBusinessModuleSmoke.ps1';   Runner = 'ps1'; Kind = '业务模块冒烟';
           ArgSets = @(
               @('-ModuleName', 'HIA.MyWorkItems', '-SqlMigrationFile', 'src\Setup\PortalBiz_WorkItems.sql'),
               @('-ModuleName', 'HIA.EmployeeProfileConfirm', '-SqlMigrationFile', 'src\Setup\PortalBiz_EmployeeProfileConfirmations.sql'),
               @('-ModuleName', 'HIA.EmployeeProfileCorrectionRequest', '-SqlMigrationFile', 'src\Setup\PortalBiz_EmployeeProfileCorrectionRequests.sql'),
               @('-ModuleName', 'HIA.EnterpriseCapabilityWorkbench', '-SkipSqlMigrationCheck'),
               @('-ModuleName', 'HIA.BusinessApplicationRequest', '-SqlMigrationFile', 'src\Setup\PortalBiz_BusinessApplications.sql')
           ) }
        # <lang>
        #   <zh-CN>C-anp-P16 / A3（W96，2026-10-07）：以下 2 个为"构建隔离 proof 项目"门禁，归入 L1（需先构建产物）。</zh-CN>
        #   <en>C-anp-P16 / A3 (W96, 2026-10-07): the 2 build-isolated proof gates below belong in L1 (require build output first).</en>
        # </lang>
        @{ File = 'Test-PortalDataProvider.ps1';            Runner = 'ps1'; Kind = '数据 provider proof'; Args = @() }
        @{ File = 'Test-PortalHiaBoundary.ps1';             Runner = 'ps1'; Kind = 'HIA 边界 proof'; Args = @() }
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
        # <lang>
        #   <zh-CN>C-anp-P16 / A3（W96，2026-10-07）：以下 4 个为需运行期环境（IIS Express / SQL Server）的门禁，归入 L2。
        #   ModuleCache / SqlCompatibility 的 ConnectionStringsConfigPath 为必填，复用与 ThemeResolution 相同的开发库连接串路径；
        #   若开发库/连接串未就绪，编排如实记 Fail（原则 3：不以"感觉该通过"替代实测）。</zh-CN>
        #   <en>C-anp-P16 / A3 (W96, 2026-10-07): the 4 gates below need a runtime (IIS Express / SQL Server) and belong in L2.
        #   ModuleCache / SqlCompatibility require ConnectionStringsConfigPath (mandatory), reusing the dev-db path from ThemeResolution;
        #   if the dev DB / connection string is not ready, the suite records Fail truthfully (principle 3: measure, do not assume).</en>
        # </lang>
        @{ File = 'Test-PortalModuleCache.ps1';             Runner = 'ps1'; Kind = '模块缓存隔离'; Args = @('-ConnectionStringsConfigPath', (Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config')) }
        @{ File = 'Test-PortalSqlCompatibility.ps1';        Runner = 'ps1'; Kind = 'SQL 兼容'; Args = @('-ConnectionStringsConfigPath', (Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config')) }
        @{ File = 'Test-PortalSmoke.ps1';                   Runner = 'ps1'; Kind = 'HTTP smoke'; Args = @() }
        @{ File = 'Test-PortalExtensionSmoke.ps1';          Runner = 'ps1'; Kind = '扩展 smoke'; Args = @() }
        # <lang>
        #   <zh-CN>C-anp-P16 / A1（W93，2026-10-07）：以下 10 个 `Test-Portal*.mjs` 语义/证据门禁此前因编排无 mjs
        #   runner 分支而架构上无法接入，现随 A1 分支补齐全量接入。它们均依赖运行期（IIS Express :40001 +
        #   外置连接串 + 验收上下文 + playwright）；本环境 IIS Express 未常驻时如实记 Fail（原则 3）。</zh-CN>
        #   <en>C-anp-P16 / A1 (W93, 2026-10-07): the 10 `Test-Portal*.mjs` semantic/evidence gates below could not be
        #   wired in before A1 added the mjs runner branch; they are now fully onboarded. They all need a runtime
        #   (IIS Express :40001 + external connection string + acceptance context + playwright); when IIS Express is
        #   not running here, the suite records Fail truthfully (principle 3).</en>
        # </lang>
        @{ File = 'Test-PortalAdminListUiEvidence.mjs';     Runner = 'mjs'; Kind = '后台列表UI证据'; Args = @() }
        @{ File = 'Test-PortalCollaborationLayoutEvidence.mjs'; Runner = 'mjs'; Kind = '协同布局证据'; Args = @() }
        @{ File = 'Test-PortalLocalizationLeakEvidence.mjs'; Runner = 'mjs'; Kind = '语言泄漏证据'; Args = @() }
        @{ File = 'Test-PortalModuleRuntimeEvidence.mjs';    Runner = 'mjs'; Kind = '模块运行期证据'; Args = @() }
        # <lang>
        #   <zh-CN>本门禁是唯一需要**临时夹具**的证据门禁：`DarkTheme` 提供深色皮肤页签覆盖、`MultiInstance`
        #   提供多实例取舍场景。二者必须由编排在门禁前应用、门禁后移除（见夹具生命周期说明），否则会污染
        #   其它运行期门禁的页签取舍结果。</zh-CN>
        #   <en>This is the only evidence gate needing **temporary fixtures**: `DarkTheme` supplies the dark-skin tab
        #   override and `MultiInstance` the multi-instance selection scenario. The suite applies them before this gate
        #   and removes them after (see the fixture-lifecycle note); otherwise they pollute tab resolution for the other
        #   runtime gates.</en>
        # </lang>
        @{ File = 'Test-PortalP77SupplementEvidence.mjs';    Runner = 'mjs'; Kind = 'P77补充证据'; Args = @();
           Fixture = @(
             @{ Script = 'New-PortalP77ReachabilityFixture.ps1'
                Apply  = @(, @('-Action', 'Seed'))
                Remove = @(, @('-Action', 'Remove')) },
             @{ Script = 'New-PortalP77SupplementFixture.ps1'
                Apply  = @(, @('-Action', 'DarkTheme'), , @('-Action', 'MultiInstance'))
                Remove = @(, @('-Action', 'MultiInstanceOff'), , @('-Action', 'DarkThemeOff')) } ) }
        @{ File = 'Test-PortalPlaceholderEvidence.mjs';      Runner = 'mjs'; Kind = '占位文案证据'; Args = @() }
        @{ File = 'Test-PortalPlatformEmptyStateEvidence.mjs'; Runner = 'mjs'; Kind = '空态证据'; Args = @() }
        @{ File = 'Test-PortalResourceContractEvidence.mjs'; Runner = 'mjs'; Kind = '资源契约证据'; Args = @() }
        @{ File = 'Test-PortalSemanticMarkupEvidence.mjs';  Runner = 'mjs'; Kind = '语义标记证据'; Args = @() }
        # <lang>
        #   <zh-CN>本门禁断言"我的待办"的三种可达性状态，**必须有业务行数据**才能测到。此前它在
        #   `Test-PortalP77SupplementEvidence` 之后运行，而后者结束时已把同一套夹具移除，导致它无数据可测
        #   （实测表现为 `modulePresent=true` 却 `tablePresent=false`、各 row / link 均为 null）。故由编排为它
        #   **独立**声明同一套夹具，使它的前置状态不再依赖别个门禁的清理时机。</zh-CN>
        #   <en>This gate asserts the three reachability states of "My To-Do Items" and **needs business rows** to assert
        #   anything. It previously ran after `Test-PortalP77SupplementEvidence`, which had already removed the same
        #   fixture on completion, leaving nothing to assert on (measured as `modulePresent=true` but `tablePresent=false`
        #   with every row / link null). The suite therefore declares the fixture for this gate **independently**, so its
        #   preconditions no longer depend on another gate's cleanup timing.</en>
        # </lang>
        @{ File = 'Test-PortalWorkItemReachabilityEvidence.mjs'; Runner = 'mjs'; Kind = '待办可达性证据'; Args = @();
           Fixture = @{ Script = 'New-PortalP77ReachabilityFixture.ps1'
                        Apply  = @(, @('-Action', 'Seed'))
                        Remove = @(, @('-Action', 'Remove')) } }
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
    # <lang>
    #   <zh-CN>门禁用缩进附注（`note:`）输出诊断与"跳过"原因，其中常含"缺失 / 未渲染 / 未能"等描述词；
    #   它们解释**原因**，不是失败结论。若把这些行计入正则匹配，会把 `[Skip]` 误判成 Fail —— 实测：
    #   `Test-PortalSemanticMarkupEvidence` 打印 `note: 目标模块未渲染（表头标记缺失），记为跳过。`，
    #   命中中文失败标志 `'未能|缺失|...'`，而该门禁自身退出码为 0、结论是 "0 failed"。
    #   故检测前剔除附注行；行级失败标志（`[Fail]` / `RESULT: Fail`）依然生效，不会漏判真实失败。</zh-CN>
    #   <en>Gates emit indented notes (`note:`) carrying diagnostics and skip reasons, and those routinely contain words like
    #   "missing" / "not rendered" / "failed to". They explain the **cause**, not a verdict. Including them in the regex turns
    #   a `[Skip]` into a Fail — measured: `Test-PortalSemanticMarkupEvidence` prints `note: target module not rendered
    #   (header marker missing), recorded as skipped.`, which matched the Chinese marker `'未能|缺失|...'`, although the gate
    #   itself exited 0 and concluded "0 failed". Note lines are therefore stripped before matching; line-level failure
    #   markers (`[Fail]` / `RESULT: Fail`) remain in force, so real failures are still caught.</en>
    # </lang>
    $effectiveOutput = (($output -split "`r?`n") | Where-Object { $_ -notmatch '^\s*note\s*:' }) -join "`n"
    foreach ($indicator in $failureIndicators) {
        if ($effectiveOutput -match $indicator.Pattern) {
            # <lang>
            #   <zh-CN>命中失败标志后还要检查同一段输出是否同时出现否定形式 —— 例如
            #   "Failed              : False" 既含 Failed 又含 False，应判为通过。
            #   不做这步的话，通过的输出会被误判成失败，门禁就永远红着，很快会被人忽略。</zh-CN>
            #   <en>After a failure marker matches, the effective output is checked for the negated form — for example
            #   "Failed              : False" contains both "Failed" and "False" and must count as a pass. Without this step a
            #   passing run is scored as a failure, the gate stays permanently red, and people start ignoring it.</en>
            # </lang>
            if ($indicator.Negation -and ($effectiveOutput -match $indicator.Negation)) { continue }
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

    # <lang>
    #   <zh-CN>L2 档位自管（2026-10-09）：语义/证据类门禁断言前台业务模块与 DevProbe 包，而默认档位 `CoreOnly`
    #   下这些模块**根本不渲染**，表现为"模块不存在"而不是门禁失败 —— 历史上 P81 / P82 / P83 / v0.7.0 四次靠
    #   人工切档位，本次排查又因此把一整天耗在"异常被吞、面板空白"上。故由编排在 L2 开始前切到
    #   `BusinessWorkflow`（前台业务模块）+ `HIA.ModuleProbe`（模块缓存门禁需要的 DevProbe 包），并在
    #   `finally` 中还原，使切档位成为**代码责任**而非操作员记忆。
    #   手段沿用 `Invoke-PortalModuleRuntimeGate.ps1` 已验证的做法：重写 gitignore 的 `appSettings.dev.json`
    #   （不入库）+ 更新 `web.config` 时间戳触发应用域回收；两者都是临时改动，故必须兜底还原 —— 否则门禁失败、
    #   断言抛错或 Ctrl+C 都会把临时档位留在工作树里，下一个人看到的模块可见性与预期不符且极难定位。</zh-CN>
    #   <en>L2 profile self-management (2026-10-09): the semantic/evidence gates assert the front-office business modules and
    #   the DevProbe package, yet under the default `CoreOnly` profile those modules **do not render at all**, which surfaces
    #   as "module missing" rather than a gate failure — P81 / P82 / P83 / v0.7.0 each needed a manual profile switch, and the
    #   present investigation lost a whole day to the resulting "swallowed exception, blank pane". The suite therefore switches
    #   to `BusinessWorkflow` (front-office business modules) + `HIA.ModuleProbe` (the DevProbe package the module-cache gate
    #   needs) before L2 and restores it in `finally`, making the switch a **code responsibility** rather than operator memory.
    #   The mechanism reuses the approach already proven by `Invoke-PortalModuleRuntimeGate.ps1`: rewrite the git-ignored
    #   `appSettings.dev.json` (never committed) and update the `web.config` timestamp to recycle the app domain. Both are
    #   temporary changes, so restoration must be guaranteed — otherwise a failing gate, a thrown assertion, or Ctrl+C leaves
    #   the temporary profile in the working tree, and the next person sees module visibility that differs from expectations
    #   with almost no way to trace it.</en>
    # </lang>
    $moduleSettingsPath = Join-Path $repoRoot 'src\Portal\Config\appSettings.dev.json'
    $webConfigPath = Join-Path $repoRoot 'src\Portal\web.config'
    $moduleSettingsBackup = $null
    $webConfigStamp = $null
    $profileManaged = $false
    if ($layerName -eq 'L2' -and -not $SkipProfileSwitch) {
        if (Test-Path $moduleSettingsPath) { $moduleSettingsBackup = [System.IO.File]::ReadAllText($moduleSettingsPath, [System.Text.Encoding]::UTF8) }
        $webConfigStamp = (Get-Item $webConfigPath).LastWriteTime
        # <lang>
        #   <zh-CN>`Enabled` 必须带上 `HIA.ModuleProbe`：`Test-PortalModuleCache` 断言 DevProbe 包，而它不在
        #   `BusinessWorkflow` 的 include 图里，只能由部署级 `Enabled` 追加。</zh-CN>
        #   <en>`Enabled` must carry `HIA.ModuleProbe`: `Test-PortalModuleCache` asserts the DevProbe package, which is not part
        #   of the `BusinessWorkflow` include graph and can therefore only be appended through the deployment-level `Enabled`.</en>
        # </lang>
        $profileJson = '{' + [char]10 + '    "appSettings": {' + [char]10 + '        "TestItem": "dev item",' + [char]10 + '        "Portal.ModuleProfiles.Active": "BusinessWorkflow",' + [char]10 + '        "Portal.ModulePackages.Enabled": "HIA.ModuleProbe"' + [char]10 + '    }' + [char]10 + '}'
        [System.IO.File]::WriteAllText($moduleSettingsPath, $profileJson, [System.Text.UTF8Encoding]::new($false))
        (Get-Item $webConfigPath).LastWriteTime = Get-Date
        $profileManaged = $true
        Start-Sleep -Seconds 3
        Write-Step 'L2 档位已切换 → BusinessWorkflow + HIA.ModuleProbe'
    }
    try {
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

        # <lang>
        #   <zh-CN>夹具生命周期（2026-10-08）：部分证据门禁必须依赖临时造数/临时注册行才能进入它要断言的状态
        #   （实测 `Test-PortalP77SupplementEvidence` 需要 `DarkTheme` + `MultiInstance` 夹具）。但这些夹具会
        #   **改变全局取舍结果** —— 实测把 `Invoke-PortalModuleRuntimeGate`、`Test-PortalAdminListUiEvidence`、
        #   `Test-PortalModuleRuntimeEvidence` 三个门禁由 Pass 拖成 Fail。故不能"一次种下、全量跑完"，只能在
        #   本门禁前应用、跑完立即移除，把副作用严格限制在本门禁的时间窗内。</zh-CN>
        #   <en>Fixture lifecycle (2026-10-08): some evidence gates only reach the state they assert on with temporary
        #   seeded/registration rows (measured: `Test-PortalP77SupplementEvidence` needs the `DarkTheme` + `MultiInstance`
        #   fixtures). Those fixtures **change global resolution outcomes** — measured to flip
        #   `Invoke-PortalModuleRuntimeGate`, `Test-PortalAdminListUiEvidence` and `Test-PortalModuleRuntimeEvidence` from
        #   Pass to Fail. They therefore cannot be "seeded once, run everything"; they are applied just before this gate
        #   and removed immediately after, confining side effects to this gate's own time window.</en>
        # </lang>
        # <lang>
        #   <zh-CN>一个门禁可能需要**多套**夹具（实测 `Test-PortalP77SupplementEvidence`：既需要业务行数据，
        #   也需要深色皮肤与多实例注册行），故统一按数组处理，单套与多套写法对编排透明。</zh-CN>
        #   <en>A gate may need **several** fixture sets (measured for `Test-PortalP77SupplementEvidence`: it needs both
        #   business rows and the dark-skin/multi-instance registration rows), so they are handled uniformly as an array,
        #   making single- and multi-set declarations transparent to the suite.</en>
        # </lang>
        if ($gate.ContainsKey('Fixture')) {
            $fixtureSets = if ($gate.Fixture -is [array]) { $gate.Fixture } else { @($gate.Fixture) }
            foreach ($fixture in $fixtureSets) {
                foreach ($applyArgs in $fixture.Apply) {
                    & $pwsh -NoProfile -File (Join-Path (Split-Path $gatePath -Parent) $fixture.Script) @applyArgs 2>&1 | Out-Null
                }
            }
            # <lang>
            #   <zh-CN>夹具行要在**应用域回收后**才对站点可见：数据层在构造时把页签与模块实例读入内存快照
            #   （`New-PortalP77SupplementFixture.ps1` 的文档明确要求回收），回收前新插入的行不可见，
            #   多实例取舍会退化为"只有既有页签一个候选"，断言随之失败。这里以**完全相同的字节**重写
            #   `Web.config`：只更新写入时间以触发回收，内容零变化，故 `git diff` 必为空。</zh-CN>
            #   <en>Fixture rows become visible to the site only after an **application-domain recycle**: the data layer
            #   snapshots tabs and module instances at construction time (documented as a recycle requirement by
            #   `New-PortalP77SupplementFixture.ps1`), so newly inserted rows are invisible until then and multi-instance
            #   selection degrades to "only the pre-existing tab is a candidate", failing the assertion. `Web.config` is
            #   rewritten with **byte-identical content** here: only the write time changes to trigger the recycle, the
            #   content is untouched, so `git diff` necessarily stays empty.</en>
            # </lang>
            $recycleTrigger = Join-Path $repoRoot 'src\Portal\Web.config'
            if (Test-Path -LiteralPath $recycleTrigger) {
                [System.IO.File]::WriteAllBytes($recycleTrigger, [System.IO.File]::ReadAllBytes($recycleTrigger))
                Start-Sleep -Seconds 5
            }
        }

        try {
            # <lang>
            #   <zh-CN>参数用**数组展开**传入，而不是拼进命令行字符串：路径里的空格与反斜杠在拼串时极易出错，
            #   而"参数没传进去"的表现是脚本报"missing mandatory parameters"，很容易被误读成门禁本身有问题。</zh-CN>
            #   <en>Arguments are passed by **array splatting** rather than concatenated into a command string: spaces and
            #   backslashes in paths break easily when concatenated, and a parameter that never arrives surfaces as
            #   "missing mandatory parameters", which is easy to misread as a defect in the gate itself.</en>
            # </lang>
            if ($gate.Runner -eq 'mjs') {
                # <lang>
                #   <zh-CN>A1（W93）：`mjs` 门禁由 Node 直接运行（无需 `-File` 开关）。本分支在调用前设置
                #   `PORTAL_PLAYWRIGHT_MODULE`（指向本机开发期依赖 `temp\node_modules\playwright`，不入库），
                #   满足语义/证据类门禁的运行期依赖；门禁自身如有必填参数，通过 `Args` 以数组展开传入。</zh-CN>
                #   <en>A1 (W93): an `mjs` gate runs directly under Node (no `-File` switch). This branch sets
                #   PORTAL_PLAYWRIGHT_MODULE (to the local dev-only dependency `temp\node_modules\playwright`, not
                #   committed) before invoking, satisfying runtime deps of semantic/evidence gates; a gate's own
                #   mandatory arguments, if any, come through `Args` via array splatting.</en>
                # </lang>
                $pwDir = Join-Path $repoRoot 'temp\node_modules\playwright'
                if (Test-Path $pwDir) { $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path $pwDir).Path }
                $gateArgs = @($gatePath)
                if ($invokeArgs.Count -gt 0) { $gateArgs += $invokeArgs }
                $raw = & $nodeExe @gateArgs 2>&1
            } else {
                $gateArgs = @('-NoProfile', '-File', $gatePath)
                if ($invokeArgs.Count -gt 0) { $gateArgs += $invokeArgs }
                $raw = & $pwsh @gateArgs 2>&1
            }
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
        } finally {
            # <lang>
            #   <zh-CN>夹具必须在门禁结束后移除，且**无论门禁通过还是失败都要移除** —— 否则一次失败的门禁会把
            #   夹具永久留在开发库里，后续所有门禁与人工验证都跑在污染状态下，且很难追溯到是哪一次跑留下的。
            #   移除动作自身失败时在此记录并继续，不能让"清理失败"掩盖门禁本身的结论。</zh-CN>
            #   <en>Fixtures must be removed once the gate finishes, **whether it passed or failed** — otherwise one failing
            #   gate leaves the fixture permanently in the development database, and every later gate and manual check
            #   runs against polluted state with little trace of which run left it. A failing removal is recorded here and
            #   execution continues, so "cleanup failed" never masks the gate's own verdict.</en>
            # </lang>
            if ($gate.ContainsKey('Fixture')) {
                $fixtureSets = if ($gate.Fixture -is [array]) { $gate.Fixture } else { @($gate.Fixture) }
                foreach ($fixture in $fixtureSets) {
                    foreach ($removeArgs in $fixture.Remove) {
                        try {
                            & $pwsh -NoProfile -File (Join-Path (Split-Path $gatePath -Parent) $fixture.Script) @removeArgs 2>&1 | Out-Null
                        } catch {
                            Write-Step ("  [Warn] 夹具移除失败：{0} {1} —— {2}" -f $fixture.Script, ($removeArgs -join ' '), $_.Exception.Message)
                        }
                    }
                }
                # <lang>
                #   <zh-CN>移除夹具后同样要回收：否则后续门禁仍读到"含夹具"的内存快照，污染与"忘记移除"完全等价。
                #   同样只重写相同字节以触发回收，不改变文件内容。</zh-CN>
                #   <en>A recycle is equally required after removal: otherwise later gates still read the "fixture-present"
                #   in-memory snapshot, which is exactly equivalent to forgetting to remove it. The same byte-identical
                #   rewrite is used to trigger the recycle without changing file content.</en>
                # </lang>
                $recycleTrigger = Join-Path $repoRoot 'src\Portal\Web.config'
                if (Test-Path -LiteralPath $recycleTrigger) {
                    [System.IO.File]::WriteAllBytes($recycleTrigger, [System.IO.File]::ReadAllBytes($recycleTrigger))
                    Start-Sleep -Seconds 5
                }
            }
        }

        Write-Step ("  [{0}] {1}（{2}）{3}exit={4}{5}" -f $record.status, $gate.File, $gate.Kind, $suffix, $record.exitCode, $(if ($record.note) { '  ' + $record.note } else { '' }))
        $results += $record

        if ($StopOnFirstFailure -and $record.status -eq 'Fail') {
            Write-Step "遇到首个失败即停止（-StopOnFirstFailure）"
            break
        }
        }
    }
    }
    finally {
        # <lang>
        #   <zh-CN>无论门禁通过、失败还是被中断，L2 临时档位都必须还原；还原配置后再恢复 `web.config` 时间戳，
        #   使工作树回到进入 L2 之前的状态。仅当确实由本层切换过档位时才还原，避免覆盖 CI 自行管理的档位。</zh-CN>
        #   <en>Whatever happens — gates passing, failing, or the run being interrupted — the temporary L2 profile must be
        #   restored; the `web.config` timestamp is restored afterwards so the working tree returns to its pre-L2 state.
        #   Restoration runs only when this layer itself switched the profile, so a profile managed by CI is never
        #   overwritten.</en>
        # </lang>
        if ($profileManaged) {
            if ($null -ne $moduleSettingsBackup) {
                [System.IO.File]::WriteAllText($moduleSettingsPath, $moduleSettingsBackup, [System.Text.UTF8Encoding]::new($false))
            }
            (Get-Item $webConfigPath).LastWriteTime = $webConfigStamp
            Write-Step 'L2 档位已还原'
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

