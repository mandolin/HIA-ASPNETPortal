<#
.SYNOPSIS
  <lang>
    <zh-CN>门禁统一编排入口：按层（L0 静态 / L1 构建后 / L2 运行期）依次执行全部门禁，一次看到全部问题。</zh-CN>
    <en>Unified gate orchestration entry: runs every gate layer by layer (L0 static, L1 post-build, L2 runtime) so all issues are visible in a single pass.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>把 `dev/scripts/` 下的门禁从"跑哪几个、什么顺序全靠人记"改为统一编排。两个刻意设计：**失败不中断**（逐个跑完再汇总，一次暴露全部问题）；**退出码与输出双判定**（多数门禁只打印 PASS/FAIL 而不设退出码，只依赖 `$LASTEXITCODE` 会把失败误判为通过）。
    **前置条件**：本机 `C:\Program Files\PowerShell\7\pwsh.exe`；L2 层另需 IIS Express、LocalDB `Portal` 库、本地 `playwright` 与 `node`。
    **验证方式**：退出码 0 表示全部通过、1 表示有门禁失败、2 表示前置条件缺失；运行结束后汇总"门禁数 / 失败数 / 跳过数"并写入证据目录。
    **低敏边界**：脚本只执行 `dev/scripts/` 下的门禁并读取其输出，不读取、不回显连接串、凭据或业务数据。</zh-CN>
    <en>Turns the gates under `dev/scripts/` from "which to run and in what order depends on memory" into a single orchestration. Two deliberate choices: **no early exit on failure** (run all gates, then summarise, so every issue is exposed at once) and **dual exit-code/output detection** (most gates print PASS/FAIL without setting an exit code, so relying on `$LASTEXITCODE` alone would misjudge failures as passes).
    **Prerequisites**: a local `C:\Program Files\PowerShell\7\pwsh.exe`; the L2 layer additionally requires IIS Express, the LocalDB `Portal` database, local `playwright` and `node`.
    **Verification**: exit code 0 means all passed, 1 means some gate failed, 2 means missing prerequisites; a summary of "gates / failures / skips" is printed and evidence is written to the evidence directory.
    **Low-sensitivity boundary**: the script only executes gates under `dev/scripts/` and reads their output; it neither reads nor echoes connection strings, credentials or business data.</en>
  </lang>
.PARAMETER Layer
  <lang>
    <zh-CN>要执行的层：`L0` 静态、`L1` 构建后、`L2` 运行期，或 `All`（默认）。</zh-CN>
    <en>Layer to execute: `L0` static, `L1` post-build, `L2` runtime, or `All` (default).</en>
  </lang>
.PARAMETER EvidenceDir
  <lang>
    <zh-CN>证据输出目录；留空时使用带时间戳的默认目录。</zh-CN>
    <en>Evidence output directory; when empty, a timestamped default directory is used.</en>
  </lang>
.PARAMETER StopOnFirstFailure
  <lang>
    <zh-CN>遇到首个失败即停止，用于快速定位；默认失败不中断以一次暴露全部问题。</zh-CN>
    <en>Stops at the first failure for fast triage; by default execution continues so all issues are exposed at once.</en>
  </lang>
.PARAMETER IncludeSlow
  <lang>
    <zh-CN>包含耗时的门禁；默认跳过以便快速反馈。</zh-CN>
    <en>Includes slow gates; skipped by default for fast feedback.</en>
  </lang>
.PARAMETER SkipProfileSwitch
  <lang>
    <zh-CN>L2 层不自动切模块档位，供自行管理档位的 CI 使用。</zh-CN>
    <en>The L2 layer does not switch module profiles automatically, for CI that manages the profile itself.</en>
  </lang>
#>

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
    [switch] $SkipProfileSwitch,
    # <lang>
    #   <zh-CN>等待套件互斥体的秒数（`C-anp-P38` / `W-anp-P215`）。默认 600 秒：足够等另一个本地套件跑完
    #   一轮 L0；设 0 表示"另一个在跑就立即失败"，用于快速确认而不是干等。</zh-CN>
    #   <en>Seconds to wait for the suite mutex (`C-anp-P38` / `W-anp-P215`). Default 600 s: long enough to let another
    #     local suite finish an L0 round; 0 means "fail immediately if another is running", for a fast check rather than
    #     waiting.</en>
    # </lang>
    [int] $LockWaitSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptDir = Join-Path $repoRoot 'dev\scripts'
$pwsh = 'C:\Program Files\PowerShell\7\pwsh.exe'
$nodeExe = 'C:\Program Files\nodejs\node.exe'
# <lang>
#   <zh-CN>L2 切档位时的包集合由共享脚本按"基线显式启用包 ∪ 本层需要追加的包"求得，本脚本不再硬编码 `Enabled`。
#   硬编码 `HIA.ModuleProbe` 会替换掉基线里的 `HIA.LeaveRequest,HIA.ExpenseReimbursement`（只靠 `Enabled`
#   进入门户、不在任何 Profile 的 include 图里），使这些模块在 L2 期间不渲染 —— `W-anp-P137` 实测根因。</zh-CN>
#   <en>The package set for the L2 profile switch comes from the shared script as "baseline explicitly enabled packages ∪
#   packages this layer must append"; this script no longer hardcodes `Enabled`. Hardcoding `HIA.ModuleProbe` replaces the
#   baseline's `HIA.LeaveRequest,HIA.ExpenseReimbursement` (which reach the Portal only through `Enabled` and appear in no
#   Profile's include graph), leaving those modules unrendered during L2 — the root cause measured in `W-anp-P137`.</en>
# </lang>
. (Join-Path $scriptDir 'Resolve-PortalRuntimeGatePackages.ps1')

function Write-Step([string] $message) { Write-Host "[gate] $message" }

# <lang>
#   <zh-CN>分层的原因是各门禁对**前置条件**的依赖不同，不能一锅端：
#     L0 静态 —— 只依赖源码，随时可跑；
#     L1 构建后 —— 须先构建出产物（单测与冒烟依赖 bin/）；
#     L2 运行期 —— 须有 IIS Express、数据库、playwright，且部分还要临时切模块档位。
#   把 L2 与 L0 混在一起，会让"环境没起"和"代码有问题"两种失败互相掩盖。</zh-CN>
#   <en>Layering exists because the gates depend on **different prerequisites**, so they cannot be run as one lump:
#     L0 static — depends only on source and can run any time;
#     L1 post-build — requires build output first (unit tests and smoke tests depend on bin/);
#     L2 runtime — requires IIS Express, the database, playwright, and for some gates a temporary module-profile switch.
#     Mixing L2 with L0 would let two different failures — "the environment is down" and "the code is broken" — mask each other.</en>
# </lang>
$gateLayers = [ordered]@{
    L0 = @(
        @{ File = 'Test-PortalXmlDocumentation.ps1';      Runner = 'ps1'; Kind = 'XML 文档'; Args = @() }
        @{ File = 'Test-PortalResourceContract.ps1';      Runner = 'ps1'; Kind = '资源契约'; Args = @() }
        @{ File = 'Test-PortalVersionConsistency.ps1';    Runner = 'ps1'; Kind = '版本一致性'; Args = @() }
        @{ File = 'Test-PortalLegacyCssCompatibility.ps1'; Runner = 'ps1'; Kind = '旧版 CSS 兼容'; Args = @() }
        @{ File = 'Test-PortalAscxCompilationContract.ps1'; Runner = 'ps1'; Kind = 'ASCX 编译契约'; Args = @() }
        # <lang>
        #   <zh-CN>C-anp-P20 新增模块就绪门禁（L0 静态）：把"新增业务模块时四类构建看不出、运行期才炸"的坑
        #   （csproj 登记 / CodeBehind 标记 / 6 皮肤主题规则 / 标记与 CSS 类名一致性）前移为静态检查。
        #   每模块一组参数，逐组各跑一次（与业务模块冒烟同源的 ArgSets 写法）。</zh-CN>
        #   <en>C-anp-P20 new-module readiness gate (L0 static): moves the four "builds fine but fails at runtime" pitfalls of
        #   adding a business module (csproj registration / CodeBehind marker / six-skin theme rules / markup-CSS class-name
        #   consistency) into a static check. One argument set per module, run once each (same ArgSets shape as the smoke gate).</en>
        # </lang>
        @{ File = 'Test-PortalNewModuleReadiness.ps1';    Runner = 'ps1'; Kind = '新增模块就绪';
           ArgSets = @(
               @('-ModuleName', 'LeaveRequest', '-ModuleCssPrefix', 'leave-request', '-ExpectedPackageId', 'HIA.LeaveRequest'),
               @('-ModuleName', 'ExpenseReimbursement', '-ModuleCssPrefix', 'expense-reimbursement', '-ExpectedPackageId', 'HIA.ExpenseReimbursement')
           ) }
        @{ File = 'Test-PortalBenchmarkChecklist.ps1'; Runner = 'ps1'; Kind = '对标检查表'; Args = @() }
        # <lang>
        #   <zh-CN>C-anp-P19（ROP 注释治理独立周期）W112 新增：ROP 注释基线门禁。把 W108–W111 达成的基线固化为
        #   机器可跑断言 —— 硬断言（阻断）：legacy `.LANG` 表面 0、legacy 标记 0、混合语言表面 0、高风险脚本缺 HIA 标记 0、
        #   缺 comment-based help 0；软指标（上报不阻断）：`HighRiskScriptCandidate`（按文件名生成的候选集）与
        #   `TodoOrDeferredMarker`（按四分法登记的延期项）—— 二者都不是缺陷计数，阻断只会制造无意义的历史搅动。
        #   门禁注释中明示证明边界：只证明结构标记存在或缺失，不证明注释语义充分性。</zh-CN>
        #   <en>C-anp-P19 (independent ROP comment-governance cycle) W112 new: the ROP comment-baseline gate. It pins the baseline
        #   reached in W108–W111 into machine-runnable assertions — hard (blocking): legacy `.LANG` surface 0, legacy markers 0,
        #   mixed language surface 0, high-risk scripts missing HIA markers 0, missing comment-based help 0; soft (reported,
        #   non-blocking): `HighRiskScriptCandidate` (a file-name keyword candidate set) and `TodoOrDeferredMarker` (items
        #   registered under the four-way split) — neither is a defect count, and blocking on them would only create pointless
        #   historical churn. The gate's own documentation states the proof boundary: it proves only that structural markers exist
        #   or are absent, never that comments are semantically sufficient.</en>
        # </lang>
        @{ File = 'Test-PortalRopCommentBaseline.ps1'; Runner = 'ps1'; Kind = 'ROP 注释基线'; Args = @() }
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
        # <lang>
        #   <zh-CN>C-anp-P21（`W-anp-P127`）新增：文档示例有效性门禁。补的是 `Test-PortalPublicDocumentation` 的盲区 ——
        #   后者只校验 Markdown **相对链接**，而**命令示例里的路径**（`dev\scripts\*.ps1`、`src\Setup\*.sql`）不在其范围；
        #   脚本改名或删除后这些示例会静默过期，文档仍显示跑不通的命令却无人报错。三项检查：E1 脚本示例存在、
        #   E2 SQL 示例存在、E3 README 中英双语结构齐备。
        #   教学示例的**占位路径**（如 `HIA.YourModule`、`PortalBiz_YourEntities.sql`）注定不存在，已按占位标记豁免并
        #   在 `[GATE-NOTE]` 透明报告豁免数 —— 否则门禁会永久 FAIL，沦为"长期红着被当成背景噪音"的老问题。
        #   门禁自带证明边界声明：只证明引用存在与结构齐备，**不证明**示例输出正确、参数有效或语义与实现一致。</zh-CN>
        #   <en>C-anp-P21 (`W-anp-P127`) new: the documentation-example validity gate. It closes a blind spot of
        #   `Test-PortalPublicDocumentation`, which validates only Markdown **relative links** — **paths inside command
        #   examples** (`dev\scripts\*.ps1`, `src\Setup\*.sql`) fall outside its scope, so after a script is renamed or deleted
        #   those examples go silently stale while the docs still show a command that cannot run. Three checks: E1 script
        #   examples exist, E2 SQL examples exist, E3 the README's bilingual structure is present.
        #   **Placeholder paths** in teaching examples (e.g. `HIA.YourModule`, `PortalBiz_YourEntities.sql`) can never exist, so
        #   they are exempted by placeholder marker and the exemption count is transparently reported via `[GATE-NOTE]` —
        #   otherwise the gate would fail permanently and fall into the old "stays red, dismissed as background noise" trap.
        #   The gate declares its own proof boundary: it proves only that references exist and the structure is present, **not**
        #   that an example's output is correct, its arguments are valid, or its semantics match the implementation.</en>
        # </lang>
        @{ File = 'Test-PortalDocumentationExamples.ps1'; Runner = 'ps1'; Kind = '文档示例有效性'; Args = @() }
        # <lang>
        #   <zh-CN>C-anp-P22（`W-anp-P133`）新增：**站点公开面**门禁。补的是一条**发布面**盲区 ——
        #     既有 `Test-PortalPublicDocumentation.ps1` 只扫描 `docs/*.md`，**不覆盖 `site/`**；
        #     而 `site/` 是**将被发布到公开官网**的目录（含生成产物与翻译源），一旦把内部链接、
        #     内部计划路径或凭据形态发布出去就是对外事故，且不像代码缺陷那样能被本地测试发现。
        #     四项检查：S1 私有仓库链接、S2 凭据赋值形态、S3 绝对本地路径、S4 内部计划/ADR 路径。
        #     **口径对齐**：S1 只查**链接/URL 形态**，与既有文档门禁只查链接目标一致 ——
        #     初版用裸词匹配会把文档里"内部资料留在 `work-zone/`"这类**纯文本提及**也判违规，
        #     从而与 docs/ 门禁对同一内容给出矛盾结论（`W-anp-P126` 批评过的口径不一致）。
        #     口径约定：**命中只报告文件与行号，不打印命中文本**（避免为证无问题而把疑似敏感片段写进日志）。
        #     证明边界：只守"公开面不含私有内容"，不证明内容正确、译本等价或链接可达
        #     （后两者分别归 `Build-PortalSite.ps1` 的 N1/N2 与人工走查）。</zh-CN>
        #   <en>C-anp-P22 (`W-anp-P133`) new: the **site public surface** gate. It closes a **publication-surface** blind spot — the
        #     existing `Test-PortalPublicDocumentation.ps1` scans only `docs/*.md` and **does not cover `site/`**, yet `site/` is the
        #     directory **published to the public website** (including generated output and translation sources); publishing an
        #     internal link, an internal plan path or a credential shape is an external incident, and unlike a code defect it is not
        #     caught by local tests. Four checks: S1 private-repository links, S2 credential assignment shapes, S3 absolute local
        #     paths, S4 internal plan / ADR paths.
        #     **Aligned scope**: S1 matches **link/URL forms only**, matching the existing documentation gate's link-target-only
        #     check — the first version used a bare word and flagged *prose mentions* such as "internal material stays in
        #     `work-zone/`", which would contradict the docs gate on the same content (the scope mismatch criticised in
        #     `W-anp-P126`). Convention: **a hit reports file and line number only, never the matched text** (so a suspected
        #     sensitive fragment is not written into the log just to prove nothing is wrong).
        #     Proof boundary: it guards only "the public surface carries no private content"; it does not prove content is
        #     correct, that translations are equivalent, or that links resolve (those belong to `Build-PortalSite.ps1` N1/N2 and
        #     to the human walkthrough).</en>
        # </lang>
        @{ File = 'Test-PortalSitePublicSurface.ps1';    Runner = 'ps1'; Kind = '站点公开面'; Args = @() }
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
        # <lang>
        #   <zh-CN>W100 / C-anp-P18 新增：模块语义类皮肤覆盖门禁（L0 静态）。扫描 `DesktopModules/**/module.json`
        #   的 CSS resources，提取模块类选择器，比对 7 套正式皮肤 `Default.css` 的覆盖；两层判定 ——
        #   硬规则（阻断）：模块 CSS 在 `var()` 之外的裸色值不得出现（否则深色皮肤必裂，W98 后应恒为 0）；
        #   软指标（上报）：模块类在主题层的覆盖缺口（目标 4「补齐缺的规则」遗留债），经 `[GATE-NOTE]` 留档不阻断。
        #   把"登记会过期、唯一可靠的是可重复检查"落到机器可跑的检查上。</zh-CN>
        #   <en>W100 / C-anp-P18 new: module semantic-class skin-coverage gate (L0 static). It scans the CSS resources
        #   declared in `DesktopModules/**/module.json`, extracts module class selectors, and compares coverage against the
        #   seven official skins' `Default.css`. Two tiers: a hard (blocking) rule that no raw color literal may appear outside
        #   `var()` in module CSS (would break dark skins; must stay 0 after W98), and a soft (reported) metric for the theme
        #   coverage gap of module classes — the C-anp-P18 goal-4 debt — surfaced via `[GATE-NOTE]` without blocking.</en>
        # </lang>
        @{ File = 'Test-PortalModuleSkinCoverage.ps1';      Runner = 'ps1'; Kind = '模块语义类皮肤覆盖'; Args = @() }
        # 编号规范：防并行冲突（N1）、防号段遗漏（N2）、包数约定（N3）、防内部编号外泄（N4）。
        # 台账 work-zone/ 不在公开仓，缺失时本门禁跳过而非误报绿。
        @{ File = 'Test-PortalWorkItemNumbering.ps1';       Runner = 'ps1'; Kind = '工作项编号规范'; Args = @() }
        # <lang>
        #   <zh-CN>C-anp-P26（`W-anp-P137`）新增：**运行期门禁档位包集合**门禁。
        #   防的是一条已实测发生、且编译与本地测试都发现不了的门禁自伤：`HIA.LeaveRequest` /
        #   `HIA.ExpenseReimbursement` 不在任何 Profile 的 include 图里，只靠部署级 `Enabled` 进入门户；
        #   两处切档位代码把该键整体替换为 `HIA.ModuleProbe`，于是这两个模块**不渲染**，运行期门禁却报
        #   "模块根找不到"（`modulePresent=false` / `errorPage=false`）——产品侧正常，是门禁自己把证据打没了。
        #   六项检查（U1–U6）中含三项负向：替换语义回归、空并集必须显式抛错、调用点不得硬编码包集合。
        #   归 L0：只读取包配置与门禁脚本源码，不连库、不启动站点、不写配置。</zh-CN>
        #   <en>C-anp-P26 (`W-anp-P137`) new: the **runtime-gate package-set** gate. It guards against a self-inflicted gate
        #   injury that has already been measured and that neither compilation nor local tests can see:
        #   `HIA.LeaveRequest` / `HIA.ExpenseReimbursement` appear in no Profile's include graph and reach the Portal only
        #   through deployment-level `Enabled`; two profile-switch code paths replaced that key wholesale with
        #   `HIA.ModuleProbe`, so both modules **did not render** while the runtime gate reported "module root not found"
        #   (`modulePresent=false` / `errorPage=false`) — the product was fine, the gate had destroyed its own evidence.
        #   Of the six checks (U1–U6), three are negative: replacement-semantics regression, an empty union must throw loudly,
        #   and call sites must not hardcode the package set. Classified L0 because it only reads package configuration and
        #   gate script source — no database, no site start-up, no configuration writes.</en>
        # </lang>
        @{ File = 'Test-PortalRuntimeGatePackageSet.ps1';    Runner = 'ps1'; Kind = '档位包集合并集'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P42` 新增：**模块包契约**门禁（ADR 0026 决策一）。与上一条“档位包集合并集”职责不同：
        #   上一条管“并集计算与替换语义”，本条管“覆盖面与登记自洽”。五项检查（M1–M5）：
        #   M1 `Enabled` 追加必须先在 `dev/module-package-registry.json` 的 appendAllowList 登记；
        #   M2 汇总所有 Profile 的 Packages 并集；M3 每个 `module.json` 的 packageId 必须被 Profile 并集 / Enabled /
        #   notMounted 之一覆盖（治住 `HIA.MyWorkItems` 这类“有 manifest 但永远不渲染、无人发现”的漏覆盖）；
        #   M4 notMounted 不得再被覆盖（防登记腐化）；M5 两个清单不得重叠。
        #   证明边界：只证明“包与配置覆盖面自洽”，不证明包可运行或档位已启用。归 L0：只读配置/清单，不连库、不启动站点。</zh-CN>
        #   <en>`C-anp-P42` new: the **module-package contract** gate (ADR 0026 decision 1). It differs from the package-set-union
        #   gate above: that one owns union computation and replacement semantics, while this one owns coverage and registry
        #   self-consistency. Five checks (M1–M5): M1 every `Enabled` append must be registered in
        #   `dev/module-package-registry.json`; M2 union all Profile Packages; M3 every `module.json` packageId must be covered by
        #   the Profile union / Enabled / notMounted (catching silent gaps such as `HIA.MyWorkItems`, which shipped a manifest but
        #   never rendered and was invisible to every gate); M4 notMounted entries must not become covered (registry rot);
        #   M5 the two lists must not overlap. Proof boundary: only package-to-configuration consistency, not that a package runs
        #   or a Profile is active. Classified L0: reads configuration and manifests only.</en>
        # </lang>
        @{ File = 'Test-PortalModulePackageContract.ps1';     Runner = 'ps1'; Kind = '模块包契约覆盖'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P29`（`W-anp-P164`）新增：**页签名与模块标题多语言链路**门禁。
        #   防的是一条编译期完全无声、直到界面上出现半中半英才被发现的链路断裂：渲染点没接解析器、保存链路断一环、
        #   迁移脚本没登记、资源键三处不同步、门控键跟着显示名漂移。六项检查（L1–L6）均为只读取证。
        #   归 L0：只读源码/SQL/资源文件，不连库、不启动站点、不写配置。</zh-CN>
        #   <en>`C-anp-P29` (`W-anp-P164`) new: the **localized tab-name and module-title chain** gate. It guards a class of
        #   breakage that is completely silent at compile time and only surfaces as half-Chinese, half-English UI: a render point
        #   not wired to the resolver, a broken save-path link, an unregistered migration script, resource keys out of sync across
        #   three files, or a gate key drifting with the display name. All six checks (L1–L6) are read-only.
        #   Classified L0: only source, SQL, and resource files are read — no database, no site start-up, no configuration writes.</en>
        # </lang>
        @{ File = 'Test-PortalLocalizedNameGate.ps1';         Runner = 'ps1'; Kind = '多语言链路齐备'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P26`（`W-anp-P140`）新增：**运行期证据覆盖台账**门禁（L0 静态）。
        #   把"覆盖面说不清、人工翻台账"变成每次运行自动产出「已覆盖/总数」与未覆盖清单：台账
        #   `dev\evidence-coverage.json` 与源码双向 default-deny 核对（新页面未分类 → Fail；台账漂移 → Fail），
        #   并机器强制 V3 —— 未覆盖项必须带所有者/再入条件/判定依据。数据库面（模块实例）不静态枚举，
        #   由 L2 `Invoke-PortalModuleRuntimeGate.ps1` 运行期产出 coverage 块，两层合成完整覆盖陈述。</zh-CN>
        #   <en>`C-anp-P26` (`W-anp-P140`) addition: the **runtime-evidence coverage ledger** gate (L0, static). Replaces
        #   "unstateable coverage, manually dug from ledgers" with automatically produced "covered / total" numbers and the
        #   uncovered list on every run: the ledger `dev\evidence-coverage.json` is cross-checked bidirectionally against source
        #   under default-deny (an unclassified new page → Fail; ledger drift → Fail), and V3 is machine-enforced — uncovered
        #   items must carry owner / re-entry condition / basis. The database surface (module instances) is not statically
        #   enumerated; the L2 `Invoke-PortalModuleRuntimeGate.ps1` produces its coverage block at runtime, and the two layers
        #   together form the complete coverage statement.</en>
        # </lang>
        @{ File = 'Test-PortalEvidenceCoverage.ps1';          Runner = 'ps1'; Kind = '证据覆盖台账'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P143`）新增：**无障碍基线**门禁（L0 静态）——补上完全无门禁的盲区面。
        #   硬断言（阈值先经探针实测当前读数再定）：六套正式主题正文与语义文本变量对背景 ≥ 4.5（WCAG AA 正文）、
        #   必须定义 :focus 可见样式、img 必须有 alt、非可聚焦元素禁挂内联 onclick、`<a>` 无 href 必须是命名锚；
        #   软指标上报明细与 Default/ThemeProbe 观察。证明边界：只证明 CSS 声明值与标记属性，
        #   不证明渲染级对比度/真实键盘焦点顺序（后续 L2 工作）。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P143`) addition: the **accessibility baseline** gate (L0, static) — filling a blind spot with
        #   no gate at all. Hard assertions (thresholds measured against the current tree before fixing): the six official themes'
        #   body text and semantic text variables vs background >= 4.5 (WCAG AA body), visible :focus styling required, img must
        #   carry alt, non-focusable elements must not carry inline onclick, and an `<a>` without href must be a named anchor;
        #   soft metrics report details and the Default/ThemeProbe observations. Proof boundary: proves declared CSS values and
        #   markup attributes only — rendered contrast / real keyboard focus order are future L2 work.</en>
        # </lang>
        @{ File = 'Test-PortalAccessibilityBaseline.ps1';     Runner = 'ps1'; Kind = '无障碍基线'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P145`）新增：**依赖漏洞基线**门禁（L0）。三层：A1 基线↔packages.config↔
        #   公开登记表三份事实双向 default-deny（依赖任何增删/版本变更缺登记即 Fail）；A2 离线高危版本地板
        #   （Newtonsoft.Json >= 13.0.0，只登记有公开依据的地板）；B 按 NuGet VulnerabilityInfo 协议实扫漏洞库
        #   （服务索引 → base+update 合并 → 区间语义匹配），命中即 Fail（附 GHSA 链接）。数据源不可达 → 显式
        #   exit 2（前置缺失不伪装通过）。证明边界：不证传递依赖（packages.config 无 lock 语义）与许可证合规。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P145`) addition: the **dependency vulnerability baseline** gate (L0). Three layers:
        #   A1 bidirectional default-deny across baseline ↔ packages.config ↔ the public register (any dependency
        #   add/remove/version change without registration fails); A2 offline known-bad version floors
        #   (Newtonsoft.Json >= 13.0.0 — only publicly justified floors); B a real scan of the vulnerability database per the
        #   NuGet VulnerabilityInfo protocol (service index → base+update merge → range-semantics matching), a hit fails with
        #   the GHSA link. Unreachable data source → explicit exit 2 (a missing prerequisite is never faked as a pass).
        #   Proof boundary: does not prove transitive dependencies (packages.config has no lock semantics) nor license compliance.</en>
        # </lang>
        @{ File = 'Test-PortalDependencyVulnerabilityBaseline.ps1'; Runner = 'ps1'; Kind = '依赖漏洞基线'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P146`）新增：**i18n 内容级一致性**门禁（L0）——超越既有资源契约门禁的
        #   "键存在 + en-us 逐字"层面，比对**值的内部结构**：A1 占位符集合跨语言一致（939 键、51 个含占位符键，
        #   丢 {0} = 运行期 FormatException/错位输出）；A2 花括号健全（去 {{}} 转义后 {==}，不平衡 = String.Format
        #   运行期抛异常；禁全角 ｛｝ 静默错误）。阈值先实测（三项违规现状全 0）；含标签键实测 0 → 标签一致性
        #   今天是空覆盖，降为软指标并注明未来引入标签时必须增设硬断言。软指标另含长度比离群（8x，实测 4x 为
        #   中文信息密度噪声）、未翻译候选、占位符断档。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P146`) addition: the **i18n content-level parity** gate (L0) — going beyond the existing
        #   resource-contract gate's "keys exist + en-us verbatim" level to compare the **internal structure of values**:
        #   A1 placeholder sets identical across locales (939 keys, 51 bearing placeholders; dropping {0} = a runtime
        #   FormatException or misaligned output); A2 brace sanity (after removing {{}} escapes, { must equal }, imbalance =
        #   String.Format throwing at runtime; full-width ｛｝ banned as a silent typo). Thresholds measured first (all three
        #   violation classes currently 0); tag-bearing keys measured 0 → tag parity is vacuous today, demoted to a soft metric
        #   with the note that a hard tag assertion must be added if tags ever appear. Soft metrics also include length-ratio
        #   outliers (8x; 4x measured to be Chinese information-density noise), untranslated candidates, and placeholder gaps.</en>
        # </lang>
        @{ File = 'Test-PortalI18nContentParity.ps1';        Runner = 'ps1'; Kind = 'i18n 内容级一致性'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P147`）新增：**门禁覆盖地图**门禁（L0）。地图文件 `dev\gate-coverage-map.json`
        #   登记"哪些面有门禁、哪些没有"，本门禁让它不漂移、不漏登记：M1 编排门禁必须被某面登记（新增门禁未登记即 Fail，
        #   default-deny）；M2 地图引用的门禁必须在编排中且文件存在；M3 未覆盖面必须带 owner/reEntry/basis（V3 机器强制）；
        #   M4 部分覆盖面必须声明 boundary（不证明什么）；M5 门禁类脚本不得成孤儿（未接入编排须在 tooling 显式声明）。
        #   本门禁**先接入编排再运行**——地图自指它自身，否则会被 M2 判为幽灵。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P147`) addition: the **gate coverage map** gate (L0). The map file
        #   `dev\gate-coverage-map.json` records "which surfaces have gates and which do not"; this gate keeps it drift-free
        #   and complete: M1 every wired gate must be registered by a surface (unregistered new gate → Fail, default-deny);
        #   M2 gates referenced by the map must be wired and present on disk; M3 uncovered surfaces must carry
        #   owner/reEntry/basis (V3 machine-enforced); M4 partial surfaces must declare a boundary (what is not proven);
        #   M5 no orphan gate scripts (unwired ones must be declared in tooling). This gate is **wired first and run
        #   afterwards** — the map references itself, so a missing entry would trip M2 as a phantom.</en>
        # </lang>
        @{ File = 'Test-PortalGateCoverageMap.ps1';          Runner = 'ps1'; Kind = '门禁覆盖地图'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P148`）新增：**landing 统计数字**门禁（L0）。landing（`site/index.md` 与
        #   `site/en/index.md`）上的统计数字此前手工维护并已实测漂移（本轮手工把"篇文档"从 31 改到 29，
        #   而真值=已上架文档篇数 27）。本门禁把五个数字全部改为机器取数：已上架文档篇数、
        #   皮肤数（主题目录排除 ThemeProbe 探针）、UI 语言数（lang.*.resx）、版本（docs/versioning.md）、
        #   许可证（根 LICENSE）；并 default-deny —— landing 上任何**未识别**的统计块即失败，
        #   防止新增一个没人核对的数字。交付时已把漂移的 29 修正为真值 27。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P148`) addition: the **landing statistics** gate (L0). The statistics on the landing pages
        #   (`site/index.md` and `site/en/index.md`) were hand-maintained and had measurably drifted (this cycle corrected
        #   "Documents" from 31 to 29 by hand, while the truth — published document count — is 27). This gate makes all five
        #   numbers machine-derived: published document count, skin count (theme directories excluding the `ThemeProbe` probe
        #   theme), UI languages (`lang.*.resx`), version (`docs/versioning.md`) and licence (root `LICENSE`); plus default-deny
        #   — any **unrecognised** statistic block on the landing fails, so no new number can appear without a check. The
        #   drifted 29 was corrected to the true 27 on delivery.</en>
        # </lang>
        @{ File = 'Test-PortalLandingStats.ps1';             Runner = 'ps1'; Kind = 'landing 统计数字'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P28`（`W-anp-P151`）新增：**运行截图基线**门禁（L0）。`docs/documentation-registry.md` 与
        #   `docs/documentation-map.md` 长期把「运行截图」登记为剩余缺口（`user-guide.md` 更列入"没有文档支持"）——
        #   手工贴一次必然腐坏且与源码脱节，故必须**可复现**。本门禁把验收判据 V2（有脚本/有规范/进证据包）
        #   变成机器核对：A1 采集工具固化规范（视口 1440×1000 / DPR 1 / zh-CN / 只截视口）；
        #   A2 目标清单 default-deny（≥10 个、业务页与后台页两类都覆盖、area+id 唯一）；
        #   A3 证据清单与目标清单一致（缺条目/未登记条目/命名不合规范即失败；清单缺失 → exit 2 前置缺失）；
        #   A4 本地截图非空（哈希差异仅软指标——跨机器字体/渲染差异是天然现象，强一致会造成假阳性）。
        #   截图本身**不入库**（P18 收口"按需生成 + 摘要入库"、operations-runbook 不把截图放入根仓库），
        #   入库的是清单。采集由 `Capture-PortalRuntimeScreenshots.mjs` 按需执行（非门禁）。</zh-CN>
        #   <en>`C-anp-P28` (`W-anp-P151`) addition: the **runtime screenshot baseline** gate (L0). `docs/documentation-registry.md`
        #   and `docs/documentation-map.md` have long listed "runtime screenshots" as a remaining gap (`user-guide.md` even lists
        #   it among items with no documentation support) — screenshots pasted by hand inevitably rot and drift from the source,
        #   so they must be **reproducible**. This gate turns acceptance criterion V2 (script / spec / evidence package) into a
        #   machine check: A1 the capture tool pins the spec (viewport 1440x1000 / DPR 1 / zh-CN / viewport-only shots);
        #   A2 default-deny target list (>= 10 targets, both business and admin areas, unique area+id);
        #   A3 the manifest agrees with the target list (missing/unregistered entries or non-conforming names fail; a missing
        #   manifest exits 2 as a prerequisite gap); A4 local screenshots are non-empty (hash differences are soft only —
        #   cross-machine font/rendering differences are inherent and requiring equality would be a false positive).
        #   Screenshots themselves are **not committed** (the P18 closeout rule of "generate on demand + commit the summary",
        #   and operations-runbook keeps screenshots out of the root repository); the manifest is what gets committed. Capture
        #   runs on demand via `Capture-PortalRuntimeScreenshots.mjs` (not a gate).</en>
        # </lang>
        @{ File = 'Test-PortalRuntimeScreenshotBaseline.ps1'; Runner = 'ps1'; Kind = '运行截图基线'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P28`（`W-anp-P152`）新增：**既有内容模块文档一致性**门禁（L0）。
        #   `documentation-registry.md` 与 `documentation-map.md` 长期把「既有内容模块（公告 / 联系人 / 讨论 /
        #   文档 / 事件 / 图片 / 链接）的逐项操作说明」登记为剩余缺口，本包已补齐 `docs/user-guide.md`
        #   「常见模块」各子节（字段写成"中文标签 + 源码控件 id"）。但**文档会因源码改动而腐烂**——改一个字段名、
        #   加一个必填校验，文档就静默过期，故把一致性变成机器核对：
        #   A1 七个模块的显示控件与编辑页必须存在；A2 文档必须为每个模块设有小节；
        #   A3 编辑页每个输入控件 id 必须出现在对应小节（源码有、文档缺即失败）；
        #   A4 源码 `RequiredFieldValidator` 数 == 小节内 `**必填**` 标注数。
        #   注册表外模块（HtmlModule / QuickLinks / Signin / XmlModule）只作软指标上报，是下一批待补文档清单。
        #   证明边界显式：只做"源码 → 文档"方向，不证语义正确（人工复核承载）、不查文档多写的文字、
        #   不核对部署位置（TabId/ModuleId 为交付时实测快照）。</zh-CN>
        #   <en>`C-anp-P28` (`W-anp-P152`) addition: the **legacy content-module doc consistency** gate (L0).
        #   `documentation-registry.md` and `documentation-map.md` have long listed "per-module operating instructions for the
        #   legacy content modules (announcements / contacts / discussion / documents / events / pictures / links)" as a remaining
        #   gap; this work item filled them in `docs/user-guide.md` under "common modules" (fields written as "Chinese label +
        #   source control id"). But **docs rot as the source changes** — rename a field or add a required validator and the doc
        #   silently goes stale, so consistency is turned into a machine check: A1 the seven modules' view controls and edit pages
        #   must exist; A2 the doc must have a section per module; A3 every input control id in the edit page must appear in the
        #   matching section (present in source, missing from the doc → fail); A4 the number of `RequiredFieldValidator` controls
        #   equals the number of `**必填**` markers in the section. Modules outside the registry (HtmlModule / QuickLinks / Signin /
        #   XmlModule) are reported as a soft metric — the backlog for the next doc batch.
        #   Proof boundary: source → doc only; it does not prove semantic correctness (human review), does not check prose the doc
        #   adds, and does not verify deployment locations (TabId/ModuleId are a delivery-time snapshot).</en>
        # </lang>
        @{ File = 'Test-PortalContentModuleDocs.ps1';         Runner = 'ps1'; Kind = '内容模块文档一致性'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P32`（`W-anp-P178`）新增：**官网译本新鲜度**门禁（L0）。`W-anp-P177` 机器化审计发现
        #   `site/_translations/user-guide.md` 的 `## Common modules` **只有一句散文**（"include announcements,
        #   contacts, ..."），而中文版有 16 个 `###` 模块小节——`W-anp-P152` 的 7 个 + `C-anp-P31` 的 4 个
        #   **英文版一个都没有**。现有 `Build-PortalSite.ps1` 的"缺译即 Fail"只校验**翻译文件存在**，
        #   不校验**内容新鲜度**，所以这类滞后一路绿灯。
        #   口径（三处教训固化）：①只认**标题行**——首版把"标题行或反引号代码"都算覆盖，注入把 `### HtmlModule`
        #   改名成 `### HTML text module` 时锚点仍靠别处代码命中，门禁放行；②**大小写敏感**——
        #   PowerShell `-notmatch` 默认忽略大小写，`W-anp-P177` 首版审计曾把 6 个模块蒙成"已覆盖"；
        #   ③中文侧只取**小节标题括号内的 ASCII 标识符**作锚点（中英标题不可直接比对，必须语言无关）。
        #   另含 A2：导航分区必须中英标题齐备（**按行解析**——`W-anp-P177` 用负向预查找正则，因回溯把
        #   「产品概览」截成「产品概」而误报 8 个）。</zh-CN>
        #   <en>`C-anp-P32` (`W-anp-P178`) addition: the **site translation freshness** gate (L0). The `W-anp-P177`
        #   machine-driven audit found that `## Common modules` in `site/_translations/user-guide.md` was **a single prose
        #   sentence** ("include announcements, contacts, ...") while the Chinese source had 16 `###` module sections — the 7
        #   from `W-anp-P152` plus the 4 from `C-anp-P31` were **entirely absent** from English. The existing "missing
        #   translation fails" check in `Build-PortalSite.ps1` only verifies that a translation **file exists**, never its
        #   **freshness**, so this lag stayed green.
        #   Contract (three lessons hardened): 1) **heading lines only** — the first version also accepted backticked code,
        #   so an injection renaming `### HtmlModule` to `### HTML text module` still passed via a code reference
        #   elsewhere; 2) **case-sensitive** — PowerShell `-notmatch` is case-insensitive by default, and the first
        #   `W-anp-P177` audit marked 6 modules as covered by one lowercase English prose list; 3) the Chinese side takes
        #   **ASCII identifiers inside section-heading parentheses** as anchors (Chinese and English headings cannot be
        #   compared directly, so anchors must be language-independent).
        #   Also A2: navigation sections must carry both titles (**parsed line by line** — the `W-anp-P177` negative-lookahead
        #   regex backtracked and truncated 「产品概览」 to 「产品概」, falsely reporting 8).</en>
        # </lang>
        @{ File = 'Test-PortalSiteTranslationFreshness.ps1';   Runner = 'ps1'; Kind = '官网译本新鲜度'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P32`（`W-anp-P180`）新增：**代码文档发布**门禁（L0）。官网 `/api/jsdoc/` 是
        #   HIA-Documentation-Sys JSDoc 半边产物的**快照**（官网由签入静态文件发布）——源码改了而快照没刷，
        #   官网就会展示过期代码文档且无人发现。本门禁四查：A1 快照完整（清单声明产物齐备、HTML ≥ 3）；
        #   A2 **源指纹一致**（对 JSDoc 的 2 个输入文件算 SHA256 组合指纹，与清单比对——这正是"源码变了快照没刷"的形态）；
        #   A3 公开面形状（无内部编号、无本地绝对路径）；A4 说明页已上架且链接到 `/api/jsdoc/`。
        #   首跑即抓到一处真问题：我手算指纹时用了反斜杠路径、门禁按 `/` 规范化，两者不一致 → A2 报出，
        #   按门禁算法重算后通过（**指纹算法以门禁为准，文档只是记录者**）。</zh-CN>
        #   <en>`C-anp-P32` (`W-anp-P180`) addition: the **code documentation publishing** gate (L0). `/api/jsdoc/` on the
        #   site is a **snapshot** of the JSDoc half of HIA-Documentation-Sys output (the site is published from committed
        #   static files) — if the sources change but the snapshot is not refreshed, the site serves stale code documentation
        #   unnoticed. Four checks: A1 snapshot completeness (manifest-declared artifacts present, HTML ≥ 3); A2 **source
        #   fingerprint agreement** (combined SHA-256 over the two JSDoc input files vs the manifest — exactly the shape of
        #   "sources changed, snapshot not refreshed"); A3 public-surface shape (no internal IDs, no local absolute paths);
        #   A4 the explainer page is published and links to `/api/jsdoc/`.
        #   The first run caught a real problem: my hand-computed fingerprint used backslash paths while the gate normalises
        #   to `/`, so they disagreed and A2 fired; recomputing with the gate's algorithm passed (**the gate's algorithm is
        #   authoritative; the manifest is only a record**).</en>
        # </lang>
        @{ File = 'Test-PortalCodeDocs.ps1';                   Runner = 'ps1'; Kind = '代码文档发布'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P33`（`W-anp-P184`）新增：**模块独立文档页**门禁（L0）。现有
        #   `Test-PortalContentModuleDocs.ps1` 只扫 `docs/user-guide.md` 的**一节**（11 个模块各一小节）；
        #   本周期要求**每个模块有独立页**（字段表 / 必填与校验 / 操作步骤 / 常见问题 / 边界成篇）。
        #   登记表 `dev/documentation/module-doc-pages.json` 记录每个模块的页面 slug、批次、交付状态与
        #   **必须写明的部署边界**（`Links` 仅测试页签、`QuickLinks` 与 Links 共用编辑页、`HtmlModule`
        #   不做净化、`Signin` 无运行期证据/无编辑页）。六类断言：M1 页面存在 / M2 字段集 ⊇ 源码控件集
        #   （**词边界**口径，与 A3 同源）/ M3 八节齐全 / M4 必填数量与源码一致 / M5 部署边界必须写明 /
        #   M6 **default-deny 自检**（已无待交付时 mode 必须切 hard）。
        #   模式 soft-until-complete → hard：批次未交付完时待交付模块只作软指标（避免 L0 周期中途红），
        #   **最后一个批次交付后由 M6 强制切换**——这正是 `C-anp-P31` 的 A6 教训（"归零 ≠ 有约束"）在此处的落点。
        #   注入验证 7/7（基线 + 六类断言各触发一次），全部在 `temp/` 下进行、不触碰仓库文件。</zh-CN>
        #   <en>`C-anp-P33` (`W-anp-P184`) addition: the **module document pages** gate (L0). The existing
        #   `Test-PortalContentModuleDocs.ps1` only scans one section of `docs/user-guide.md`; this cycle requires a
        #   **dedicated page per module**. The registry `dev/documentation/module-doc-pages.json` records each module's
        #   page slug, batch, delivery status and the **deployment boundaries that must be stated** (`Links` is on the test
        #   tab only, `QuickLinks` shares the edit page with Links, `HtmlModule` performs no sanitisation, `Signin` has no
        #   runtime evidence / no edit page). Six assertion families: M1 page exists / M2 field set ⊇ source controls
        #   (**word-boundary** convention, same source as A3) / M3 all eight sections / M4 required count agrees /
        #   M5 boundaries stated / M6 **default-deny self-check** (mode must be `hard` once nothing is pending).
        #   Mode soft-until-complete → hard: while batches are outstanding the remainder is a soft metric so L0 does not go
        #   red mid-cycle; **M6 forces the switch after the last batch** — the landing point of the `C-anp-P31` A6 lesson
        #   ("zero is not the same as constrained"). Injection proof 7/7 (baseline plus one trigger per assertion family),
        #   entirely under `temp/`, touching no repository file.</en>
        # </lang>
        @{ File = 'Test-PortalModuleDocPages.ps1';             Runner = 'ps1'; Kind = '模块独立文档页'; Args = @() }

        # <lang>
        #   <zh-CN>公开文档**内容适用性**门禁（L0；用户 2026-10-21 指出、备忘 D11）：已上架的公开文档不得出现
        #     「内部资料放哪 / 不随本站发布」一类表述，也不得出现内部工作项编号（W-anp-P* / C-anp-P*）。
        #     只扫 published.yml 声明的已上架文档（中文 docs/<doc> + 英文 site/_translations/<doc>），
        #     **未上架的内部治理件不在范围**（它们本就是内部件）。原则：公开页面只讲产品与其使用/运维，
        #     不讲项目内部的计划、协作与治理安排。刻意放行 <internal evidence directory> 命令占位符。</zh-CN>
        #   <en>Public documentation **content applicability** gate (L0; raised by the user 2026-10-21, memo D11):
        #     published public documents must not contain "where internal material lives / not published here"
        #     statements, nor internal work-item numbers (W-anp-P* / C-anp-P*). It scans only the published
        #     documents declared in published.yml (Chinese docs/<doc> + English site/_translations/<doc>);
        #     **unpublished internal governance artifacts are out of scope** (they are internal by design).
        #     Principle: a public page talks about the product and its use/operation, not about the project's internal
        #     planning, collaboration or governance. The <internal evidence directory> command placeholder is
        #     deliberately allowed.</en>
        # </lang>
        @{ File = 'Test-PortalPublicDocContentApplicability.ps1'; Runner = 'ps1'; Kind = '公开文档内容适用性'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P28`（`W-anp-P153`）新增：**审批流转边界看守**门禁（L0）。验收判据 V3 要求
        #   「审批流转边界在公开文档中有明确声明」。源码实测（2026-10-19）：请假 / 报销两个模块的提交动作链
        #   仅为「字段校验 → 落库（状态固定 `Pending`）→ 生成待办（指派 `Admins`）→ 写审计」，**没有任何审批动作**；
        #   `MyWorkItems` 也只做待办的只读投影（全部 / 仅超期筛选），不处理业务、不拥有流程语义。
        #   本门禁把这条边界**双向看守**：A1 公开文档必须含「审批流转边界（必读）」小节与关键短语；
        #   A2 两个模块的 code-behind 不得出现审批动作标识（`Approve` / `Reject`），`MyWorkItems` 必须保持只读；
        #   A3 文档点名要看的门禁必须存在。任一侧变化而另一侧未同步即失败——将来真加审批流程，门禁会强制先改文档。
        #   注入法实测曾暴露本门禁自身两处弱断言（标题前缀匹配、`\bApprove\b` 词边界匹配不到 `ApproveRequest`），
        #   已分别改为精确标题匹配与子串匹配。</zh-CN>
        #   <en>`C-anp-P28` (`W-anp-P153`) addition: the **approval-flow boundary guard** gate (L0). Acceptance criterion V3 requires an
        #   explicit declaration of the approval-flow boundary in the public docs. Measured from source (2026-10-19): the submit chain
        #   of the leave and expense modules is only "field validation → persist (status fixed to `Pending`) → create a work item
        #   (assigned to `Admins`) → write an audit" with **no approval action at all**, and `MyWorkItems` is only a read-only
        #   projection of work items (all / overdue filters) that neither processes business nor owns process semantics. This gate
        #   guards the boundary **in both directions**: A1 the public doc must contain the "approval flow boundary (must read)" section
        #   and its key phrases; A2 the two modules' code-behind must not contain approval action identifiers (`Approve` / `Reject`) and
        #   `MyWorkItems` must stay read-only; A3 the gate the doc names must exist. If either side changes without the other, the gate
        #   fails — when approval is genuinely added later, the doc must be corrected first.
        #   Injection testing exposed two weak assertions in this gate itself (a heading prefix match and `\bApprove\b` word-boundary
        #   matching that missed `ApproveRequest`), now fixed to an exact heading match and substring matching.</en>
        # </lang>
        @{ File = 'Test-PortalApprovalBoundaryDocs.ps1';      Runner = 'ps1'; Kind = '审批流转边界'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P28`（`W-anp-P154`）新增：**审计留存处置登记**门禁（L0）。验收判据 V4 要求审计留存二选一：
        #   实现自动归档 / 到期清理，**或**正式登记「未实现 + 不得删除审计数据」。本包选后者（实现需运维确认与
        #   备份策略，属环境触发型），但把登记变成**机器可核对**的——否则"不得删除"只是一句没人验证的散文。
        #   断言：A1 策略文档必须保留「尚未实现」+「不得删除」状态声明与「3.1 已登记的例外」小节；
        #   A2 产品源码 `src/` 不得出现针对 `PortalCfg_OperationAudits` 的 DELETE / TRUNCATE（**无豁免**）；
        #   A3 仓库内其余删除点必须在显式豁免清单中并附依据（default-deny），清单漂移同样失败；
        #   A4 策略文档点名要看的门禁必须存在。
        #   实测发现仓库内确有一处审计表删除（`New-PortalP19BrowserSmokeFixture.ps1` 的 fixture 自产自销清理：
        #   只删该 fixture 自己创建的审计行、跑在可丢弃开发库），已正式登记为唯一例外。
        #   注入法首轮曾三路"全部通过"，诊断后确认是**注入脚本自身三处缺陷**（PowerShell 参数模式丢弃拼接表达式、
        #   rogue 文件写在扫描范围外、短语有两处只改一处），修正后 3/3 抓获。</zh-CN>
        #   <en>`C-anp-P28` (`W-anp-P154`) addition: the **audit-retention declaration** gate (L0). Acceptance criterion V4 requires
        #   audit retention to land one of two ways: implement automatic archiving / expiry cleanup, **or** formally register
        #   "not implemented + audit data must not be deleted". This work item chose the latter (implementation needs operational
        #   sign-off and a backup strategy, i.e. environment-triggered) but makes the registration machine-checkable — otherwise
        #   "must not delete" is prose nobody verifies. Assertions: A1 the policy doc must keep its "not yet implemented" +
        #   "must not delete" declaration and the "3.1 registered exceptions" section; A2 product source `src/` must contain no
        #   DELETE / TRUNCATE against `PortalCfg_OperationAudits` (**no exemptions**); A3 every other delete point must be in the
        #   explicit allowlist with a basis (default-deny), and allowlist drift also fails; A4 the gate the policy doc names must
        #   exist. Measurement found exactly one audit-table delete in the repository (the self-cleaning fixture
        #   `New-PortalP19BrowserSmokeFixture.ps1`: it deletes only the audit rows the fixture itself created and runs on a
        #   disposable dev database), now formally registered as the sole exception. A first injection round "passed" all three
        #   cases; diagnosis showed three defects in the injection script itself (PowerShell argument mode dropping the
        #   concatenation expression, the rogue file written outside the scan scope, and only one of two phrase occurrences
        #   replaced); after fixing them, all 3 were caught.</en>
        # </lang>
        @{ File = 'Test-PortalAuditRetentionDeclaration.ps1';  Runner = 'ps1'; Kind = '审计留存登记'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P28`（`W-anp-P155`）新增：**L7 目标环境证据清单**门禁（L0）。验收判据 V5 要求
        #   「拿到目标环境后，一条命令产出 L7 所需全部证据」。本包交付两件东西：
        #   ① 台账 `dev/target-environment-evidence.json`——逐项登记 `M-ANP-TRUSTED-PORTAL`（L7）判据与阻断项
        #   对应的环境触发型证据：mode=local（现在就能出证）/ targetEnv（需目标环境）/ manual（人工产物）；
        #   ② 一键证据包 `Invoke-PortalTargetEnvironmentEvidencePack.ps1`——按台账逐项出证，环境项**显式跳过并
        #   说明缺失前置**（P139 口径，绝不静默通过），目标就绪后同一条命令（加 -BaseUrl）即自动补齐。
        #   本门禁保证台账不漏、不假、不腐：A1 字段齐备且 id 唯一、mode 合法；A2 生成器脚本必须存在
        #   （不得登记跑不起来的命令）；A3 T1/T4/T5/blocker-1/2/3/L8-handoff 全覆盖（漏登记=环境到了也出不全证）；
        #   A4 模式语义自洽（local 必须有 generator、targetEnv 必须写 prereqs）；A5 公开运维文档必须点名该工具
        #   （有工具没人知道 = 没交付）。实测：证据包当前 4 项出证 / 6 项显式跳过 / 1 项待人工，exit 0。</zh-CN>
        #   <en>`C-anp-P28` (`W-anp-P155`) addition: the **L7 target-environment evidence registry** gate (L0). Acceptance
        #   criterion V5 requires that "once the target environment is available, one command produces all evidence required by
        #   L7". This work item delivers two things: ① the registry `dev/target-environment-evidence.json`, which registers the
        #   environment-triggered evidence for each `M-ANP-TRUSTED-PORTAL` (L7) criterion and blocker with mode=local (collectable
        #   now) / targetEnv (needs the target environment) / manual (human artefact); ② the one-command pack
        #   `Invoke-PortalTargetEnvironmentEvidencePack.ps1`, which produces evidence item by item, **skipping environment items
        #   explicitly with the missing prerequisite stated** (P139 convention — never a silent pass); once the environment exists
        #   the same command (plus -BaseUrl) fills them in. This gate keeps the registry complete, truthful and unrotted: A1 fields
        #   present, ids unique, mode valid; A2 generator scripts must exist (a command that cannot run must not be registered);
        #   A3 full coverage of T1/T4/T5/blocker-1/2/3/L8-handoff (an unregistered item means "the environment arrives but the
        #   evidence is incomplete"); A4 mode semantics self-consistent (local requires a generator, targetEnv must state prereqs);
        #   A5 the public operations doc must name the tool (a tool nobody knows about is not delivered). Measured: the pack currently
        #   collects 4 items, skips 6 explicitly, has 1 manual item, exit 0.</en>
        # </lang>
        @{ File = 'Test-PortalTargetEnvironmentEvidenceMap.ps1'; Runner = 'ps1'; Kind = 'L7 证据清单'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P28`（`W-anp-P156`）新增：**L7 判据表**门禁（L0）。里程碑 `M-ANP-TRUSTED-PORTAL` 的
        #   T1–T7 判据原先只以叙述形式存在于文档里，无法逐条复核。本包把它条目化为台账
        #   `dev/l7-criteria-registry.json`（每条：判定标准、当前状态、证据指针、复核方式、失效条件、所有者、再入条件），
        #   并由本门禁看守：A1 字段齐备/id 唯一/status 合法；A2 复核方式可用（script 必须存在、manual 必须写得具体 ≥20 字，
        #   **不得登记跑不起来的复核命令或空话程序**）；A3 T1–T7 全覆盖（漏一条 = 有判据没人复核）；
        #   A4 状态自洽（needs-target-env 必须在本仓 `dev/target-environment-evidence.json` 有对应证据条目，
        #   met-dev-side 必须有证据指针）；A5 与 work-zone 里程碑文档交叉核对（软，私有仓缺失不阻断）。
        #   实测：7 条判据（met-dev-side 6 / needs-target-env 1），脚本复核 6、人工复核 1。</zh-CN>
        #   <en>`C-anp-P28` (`W-anp-P156`) addition: the **L7 criteria table** gate (L0). The T1–T7 criteria of the milestone
        #   `M-ANP-TRUSTED-PORTAL` previously existed only as prose in documents and could not be reviewed criterion by criterion.
        #   This work item itemises them into the registry `dev/l7-criteria-registry.json` (per criterion: requirement, current
        #   status, evidence pointers, verification method, invalidation condition, owner, re-entry), guarded by this gate:
        #   A1 fields present / ids unique / status valid; A2 usable verification (script must exist; manual must be concrete,
        #   >= 20 characters — **a review command that cannot run or a vacuous procedure must not be registered**);
        #   A3 full coverage of T1–T7 (a missing one means a criterion nobody reviews); A4 status coherence (needs-target-env must
        #   have a matching evidence entry in `dev/target-environment-evidence.json`; met-dev-side must have evidence pointers);
        #   A5 cross-check against the work-zone milestone doc (soft; a missing private repo does not block).
        #   Measured: 7 criteria (6 met-dev-side / 1 needs-target-env), 6 script-verified and 1 manually verified.</en>
        # </lang>
        @{ File = 'Test-PortalL7CriteriaRegistry.ps1';       Runner = 'ps1'; Kind = 'L7 判据表'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P30`（`W-anp-P165`）新增：**计划账本一致性**门禁（L0）。起因是 2026-10-20 跨周期盘点发现
        #   **账本漂移 3 处、且没有任何门禁能发现**：`cycle-status.md` 第五节的"当前线 / 下一步"长期停在
        #   `C-anp-P26` / `W-anp-P139`（而 P26–P29 均已收口）、第三节跳跃记录停在 2026-10-15、
        #   `works-index.md` 把 P26/P27/P28 标"待开始"、P29 标"进行中"。编号门禁一路通过，因为它只核对
        #   "编号不冲突 / 不悬空 / 不外泄"，**不核对周期状态与实际是否一致**。
        #   修法：在 `cycle-status.md` 引入机器可读状态块（`LEDGER-STATE-BEGIN/END` 包 JSON）作唯一事实源，
        #   本门禁交叉核对三处账本：A1 状态块存在且可解析、与状态总表两边集合一致；A2 状态总表标记 == 状态块；
        #   A3 编号登记册标记 == 状态块；A4 closed 必须有收口文档、disposition 必须有裁定文档；
        #   A5 「下一步唯一动作」引用的 W 编号不得属于已收口周期（这正是原来那处漂移的形态）。
        #   A5 只扫「下一步唯一动作」这一行——首跑时整节扫描误报了我说明文字里的旧编号，已收窄。</zh-CN>
        #   <en>`C-anp-P30` (`W-anp-P165`) addition: the **plan ledger consistency** gate (L0). It originates from the 2026-10-20
        #   cross-cycle audit, which found **three ledger drifts that no gate could catch**: section 5 of `cycle-status.md` had
        #   long been stuck on `C-anp-P26` / `W-anp-P139` while P26–P29 were all closed, the jump log stopped at 2026-10-15, and
        #   `works-index.md` marked P26/P27/P28 as "not started" and P29 as "in progress". The numbering gate passed throughout,
        #   because it only checks "no conflicts / no dangling / no public leak" and **never checks whether cycle status matches
        #   reality**.
        #   Fix: a machine-readable state block (`LEDGER-STATE-BEGIN/END` wrapping JSON) in `cycle-status.md` becomes the single
        #   source of truth, and this gate cross-checks the three ledgers: A1 the block exists, parses, and its cycle set matches
        #   the status table; A2 status-table markers equal the block; A3 numbering-register markers equal the block; A4 a `closed`
        #   cycle must have a closeout document and a `disposition` cycle a disposition document; A5 any W number referenced by the
        #   "next single action" must not belong to a closed cycle (exactly the shape of the original drift).
        #   A5 scans only the "next single action" line — a first-run whole-section scan false-positived on an old number inside my
        #   own explanatory note, so it was narrowed.</en>
        # </lang>
        @{ File = 'Test-PortalPlanLedgerConsistency.ps1';    Runner = 'ps1'; Kind = '计划账本一致性'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P30`（`W-anp-P166`）新增：**`docs` ↔ `site` 镜像一致性**门禁（L0）。`C-anp-P28` 期间每改一次公开文档都要
        #   **人工**同步 `site/` 镜像页，再用临时脚本人工校验——**没有任何门禁校验这件事**，改了 docs 忘同步则 L0 全绿。
        #   本门禁以 `site/_data/published.yml` 上架清单为事实源：A1 每个条目的源文件与镜像页都必须存在；
        #   A2 镜像必须是**构建产物形状**——A2a 无裸相对 `.md` 链接（站点上必然 404，须由构建重写为 `{{ site.baseurl }}/...`）、
        #   A2b 无 `W-anp-P*` / `C-anp-P*` 内部编号外泄（公开面过滤契约）、A2c 首标题与 docs 一致（兜底"整页没重新生成"）；
        #   A3 front matter 契约（`lang: zh-CN` / `permalink: /<path>/` / `parent` 与清单一致）；A4 未上架 docs 仅作软指标列出。
        #   **关键口径修正（首跑实测推翻原设计）**：初版要求"镜像正文 == docs 原文"，结果 29 页中 16 页失败——
        #   但差异全部来自构建的两项合法变换（链接重写 + 公开面文本过滤），行数一致，即"不等"是设计如此；
        #   真问题在另一侧：`C-anp-P28` 我手工同步的若干页是"docs 原文直抄"，**缺了这两项变换**
        #   （裸相对链接 404 + 内部编号外泄）。故改为核对形状不变量，并跑一次 `Build-PortalSite.ps1`
        #   把这几页修回构建产物（实测 29 页中英生成、82 页检查 Pass，仅 4 个文件变更）。</zh-CN>
        #   <en>`C-anp-P30` (`W-anp-P166`) addition: the **`docs` ↔ `site` mirror consistency** gate (L0). During `C-anp-P28`, every
        #   public-doc change required **manually** syncing the `site/` mirror page plus a scratch-script check — **no gate checked
        #   it**, so a forgotten sync left L0 green. This gate takes the `site/_data/published.yml` manifest as its source of truth:
        #   A1 source file and mirror page must exist per entry; A2 the mirror must be **build-output shaped** — A2a no raw
        #   relative `.md` links (they 404 on the site; the build rewrites them to `{{ site.baseurl }}/...`), A2b no leaked
        #   `W-anp-P*` / `C-anp-P*` internal IDs (public-surface filtering contract), A2c the first heading matches the docs
        #   (backstop for "the page was never regenerated"); A3 the front-matter contract (`lang: zh-CN`, `permalink: /<path>/`,
        #   and `parent` matching the manifest); A4 unpublished docs are listed as a soft metric only.
        #   **Key calibration fix (the first run overturned the original design)**: the first version required "mirror body == docs
        #   source" and 16 of 29 pages failed — yet every difference came from the build's two legitimate transformations (link
        #   rewriting + public-surface text filtering) with identical line counts, i.e. the inequality is by design; the real
        #   problem was on the other side: several pages I hand-synced during `C-anp-P28` were verbatim docs copies **missing both
        #   transformations** (raw relative links 404, internal IDs leak). So the gate now checks shape invariants, and one run of
        #   `Build-PortalSite.ps1` restored those pages to build output (measured: 29 zh/en pages generated, 82 pages checked with
        #   Pass, only 4 files changed).</en>
        # </lang>
        @{ File = 'Test-PortalSiteMirrorConsistency.ps1'; Runner = 'ps1'; Kind = '站点镜像一致性'; Args = @() }
        # <lang>
        #   <zh-CN>**镜像新鲜度**（`W-anp-P192`，解备忘 D10）。与上一条（一致性）分工明确：一致性查"形状"
        #     （无裸相对链接、无内部编号外泄、首标题一致、front matter 契约）；新鲜度查"**是不是当前 `docs/`
        #     的构建产物**"——把构建输出重定向到临时目录后逐字节比对。之所以能逐字节：**"合法变换"由构建自身定义**，
        #     门禁不需要复述哪些变换被允许（链接重写、发布面文本过滤、front matter 注入、路径变化），
        #     因此也不会因新增变换而漏判。D10 的真实形态：曾有周期改了 `docs/` 却没提交构建产物，仓库内 `site/`
        #     副本陈旧，靠人工提交时 `git status` 发现——本门禁把这件事变成机器断言。</zh-CN>
        #   <en>**Mirror freshness** (`W-anp-P192`, resolves memo D10). Clear division from the entry above (consistency):
        #     consistency checks "shape" (no raw relative links, no internal-ID leakage, first heading agrees, front-matter
        #     contract); freshness checks "**is this the build output of the current `docs/`**" — redirecting the build
        #     output to a temp directory and comparing byte-for-byte. Byte comparison is possible because **the build itself
        #     defines the legitimate transformations** (link rewriting, publication-surface filtering, front-matter injection,
        #     path changes), so the gate need not restate which are allowed and cannot miss newly added ones. The real shape of
        #     D10: a cycle once changed `docs/` without committing the build output, leaving the repository's `site/` copy
        #     stale, caught only by a human at commit time via `git status` — this gate turns that into a machine assertion.</en>
        # </lang>
        @{ File = 'Test-PortalSiteMirrorFreshness.ps1';  Runner = 'ps1'; Kind = '站点镜像新鲜度'; Args = @() }
        # <lang>
        #   <zh-CN>手写页面新鲜度门禁（L0，静态）：不走生成流程的页面（首页 / 营销三页 / 八个分区页）
        #     必须与 `_data/navigation.yml` 一致且中英成对。核心判据是"**分区页 title 必须等于导航分区名**"——
        #     已上架页面的 `parent` 是按这个字符串去匹配父页 title 的，两侧漂移时页面还在、链接不坏、
        #     parent 也仍"有效"（构建 N1 只校验 parent 是否属于已知分区名），**但子页不会嵌套到分区下**，
        #     导航结构静默失效。首页统计数字归 landing 门禁、站内链接与 parent 有效性归构建 N1/N2，
        #     本门禁不重复。</zh-CN>
        #   <en>Handwritten page freshness gate (L0, static): pages that skip generation (landing / three
        #     marketing pages / eight section pages) must agree with `_data/navigation.yml` and be paired across
        #     languages. The core criterion is "**a section page's title must equal the navigation section name**"
        #     — published pages match their `parent` string against the parent page's title, so when the two sides
        #     drift the page still exists, no link breaks and `parent` remains "valid" (build N1 only checks that
        #     a parent is a known section name), **yet children no longer nest under the section** and navigation
        #     fails silently. Landing statistics belong to the landing gate; in-site links and `parent` validity
        #     belong to the build's N1/N2 — this gate does not duplicate them.</en>
        # </lang>
        @{ File = 'Test-PortalHandwrittenPageFreshness.ps1'; Runner = 'ps1'; Kind = '手写页面新鲜度'; Args = @() }
        # <lang>
        #   <zh-CN>封版前检查门禁（`W-anp-P199`）。**日常编排只跑静态判据**（`-DailySelfCheck`：F1 Unreleased 有内容 /
        #     F2 版本锚点一致 / F7 备忘无阻塞项 / F8 当前版本未漂移）。产物类判据（F4 套件新鲜运行、F6 线上复核结论）
        #     **不在日常跑**：它们需要"刚出炉"的统计产物，日常每次都会因拿不到而报未确认——而**持续跳过的告警等于
        #     没有告警**。封版前由人显式传入新鲜产物后跑全八项。F4/F6 读产物而不自己跑，是为了**反递归**
        #     （套件里包含本门禁，自己调自己会无限递归）。</zh-CN>
        #   <en>Release-readiness gate (`W-anp-P199`). **Daily orchestration runs only the static checks**
        #     (`-DailySelfCheck`: F1 non-empty Unreleased / F2 version anchors agree / F7 no blocking memo item /
        #     F8 current version not drifted). The artifact-based checks (F4 a fresh suite run, F6 the live re-check
        #     verdict) are **not run daily**: they need a "just produced" statistics artifact, so a daily run would
        #     report unconfirmed every single time — and **an alarm that skips every day is no alarm at all**. Before
        #     sealing, a human runs all eight with fresh artifacts supplied explicitly. F4/F6 read artifacts rather
        #     than executing anything themselves, for **anti-recursion** (the suite contains this gate, so invoking
        #     it would recurse forever).</en>
        # </lang>
        @{ File = 'Test-PortalReleaseReadiness.ps1';  Runner = 'ps1'; Kind = '封版前检查'; Args = @('-DailySelfCheck') }
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
        #   <zh-CN>`C-anp-P29` 收口三件事之「固化」：页签名与模块标题多语言的**运行期**证据门禁。
        #   与 L0 的 `Test-PortalLocalizedNameGate` 分工：静态门禁证明链路齐备且互相引用正确，本门禁证明链路在
        #   真实站点上真的把中英文分开了。只做静态检查会漏掉「接线正确但档位不含该包」—— 实测 `MyWorkItems`
        #   在默认 `CoreOnly` 档位下整页不渲染，静态检查对此完全无感。
        #   归 L2：需运行站点 + playwright（开发期依赖、不入库）+ 验收上下文；前置缺失时**显式跳过并 exit 2**，
        #   不以 exit=1 冒充失败，这正是 `W-anp-P139` 要固化的口径，本门禁先行采用。
        #   门禁自带档位切换并在 `finally` 逐字节还原 `appSettings.dev.json`。</zh-CN>
        #   <en>`C-anp-P29` closeout item "formalize": the **runtime** evidence gate for localized tab names and module titles.
        #   Division of labour with the L0 `Test-PortalLocalizedNameGate`: the static gate proves the chain is complete and
        #   cross-references correctly, while this gate proves the chain really separates Chinese from English on a live site.
        #   Static checks alone would miss "correctly wired but the profile does not include that package" — measured, `MyWorkItems`
        #   renders nothing at all under the default `CoreOnly` profile, which static checks are entirely blind to.
        #   Classified L2: needs a running site, playwright (a development-only dependency, not committed), and the acceptance
        #   context; when a prerequisite is missing it is **explicitly skipped with exit 2** rather than failing with exit=1, which
        #   is precisely the convention `W-anp-P139` is meant to formalize, adopted here ahead of that work item.
        #   The gate switches the deployment profile itself and restores `appSettings.dev.json` byte-for-byte in `finally`.</en>
        # </lang>
        @{ File = 'Test-PortalLocalizedNameRuntimeEvidence.ps1'; Runner = 'ps1'; Kind = '多语言运行期证据'; Args = @() }
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
        # <lang>
        #   <zh-CN>`Env` 声明该门禁**必须**以哪个档位运行。实测依据（2026-10-09 注入法）：本脚本
        #   `phase = PORTAL_P82_PHASE || 'baseline'`，而语义断言（`<th>` 全带 `scope`、无孤儿 span 标签、
        #   标题 `role=heading` + `aria-level`）**只在 `after` 档生效**；`baseline` 档只断言"模块已渲染"。
        #   注入一个缺 `scope` 的 `<th>` 后实测：`baseline` 档 exit 0 / "0 failed"（**抓不到**），
        #   `after` 档 exit 1 / "1 failed"（**抓得到**）。故编排若沿用默认档，该门禁就是"通过但什么都没断言"。
        # </zh-CN>
        #   <en>`Env` declares the phase this gate **must** run in. Measured basis (2026-10-09, injection method): the script
        #   sets `phase = PORTAL_P82_PHASE || 'baseline'`, while the semantic assertions (every `<th>` carries `scope`, no
        #   orphan span labels, title has `role=heading` + `aria-level`) apply **only in the `after` phase**; `baseline` asserts
        #   merely that the module rendered. After injecting one `<th>` without `scope`: `baseline` gave exit 0 / "0 failed"
        #   (**did not catch it**), `after` gave exit 1 / "1 failed" (**caught it**). Letting the suite keep the default would
        #   therefore leave this gate passing while asserting nothing.</en>
        # </lang>
        @{ File = 'Test-PortalSemanticMarkupEvidence.mjs';  Runner = 'mjs'; Kind = '语义标记证据'; Args = @();
           Env = @{ PORTAL_P82_PHASE = 'after' } }
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
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P144`）新增：**性能基线**门禁（L2）。性能没有"对错"只有"快慢"，
        #   编译与功能门禁对 N+1 查询 / 无界列表完全失明。断言：五个已知列表面必须声明行数上限常量且 ≤100
        #   （本项目"列表分页"的正确形态是服务端行数上限）；全仓 Limit/PageSize 常量 ≤100；
        #   五个代表页登录后预热 1 次再测 5 轮取中位数——服务端 TTFB（含其背后全部 DB 工作，即慢查询经 UI 的
        #   表现）≤ 2000ms、浏览器 load ≤ 10000ms。阈值实测定线（服务端中位 11–43ms），env 可覆盖供负向测试。
        #   证明边界：不证明直接 SQL 层耗时（需 DB 驱动，后续工作）、不发现未知新列表面（静态规则按已知清单）。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P144`) addition: the **performance baseline** gate (L2). Performance has no
        #   "correct/incorrect", only "fast/slow", and build/functional gates are blind to N+1 queries and unbounded lists.
        #   Assertions: the five known list surfaces must declare a row-cap constant <= 100 (this project's shape of list
        #   pagination is a server-side row cap); every Limit/PageSize constant in the repo <= 100; and five representative
        #   pages, signed in and warmed up once then timed 5 rounds taking the median — server TTFB (which includes all DB
        #   work behind the page, i.e. how slow queries surface through the UI) <= 2000ms and browser load <= 10000ms.
        #   Thresholds are measured-first (server medians 11–43ms); env-overridable for negative testing. Proof boundary:
        #   does not prove direct SQL-level timings (needs a DB driver, future work) nor discover unknown new list surfaces
        #   (the static rule follows the known list).</en>
        # </lang>
        @{ File = 'Test-PortalPerformanceBaseline.mjs';      Runner = 'mjs'; Kind = '性能基线'; Args = @() }
        # <lang>
        #   <zh-CN>`C-anp-P27`（`W-anp-P149`，`C-anp-P24` 残余项）新增：**默认主题视觉回归基线**门禁（L2）。
        #   P24 裁定残余项为"`Default` 回退主题纳入视觉回归（补 Default 规则的运行期证据）"；既有 C-anp-P18/W101
        #   的 258 张截图矩阵是人工复核证据而非机器判据，且收口建议"截图转为按需生成 + 摘要入库"。
        #   故本门禁把视觉回归固化为**结构化指标**：每页采集关键元素的计算样式（CSS 决定，跨机器确定）与几何（容差），
        #   与基线图 dev\default-theme-visual-baseline.json 比对。主题只能由「Tab 覆盖 → 数据库运行设置 → appSettings →
        #   Default」决定（解析器不接受 URL 覆盖），故依赖 fixture `New-PortalDefaultThemeFixture.ps1` 临时切换到 Default
        #   并在结束后恢复；A0 断言每页 body 必须带 portal-theme-default class——fixture 未生效即失败，不假装通过。
        #   采样页第 5 页（tabid=1101）来自实测：前四页对 95 条 Default 作用域规则命中 0 条（指标空转），该页命中 23/95。</zh-CN>
        #   <en>`C-anp-P27` (`W-anp-P149`, the `C-anp-P24` residual) addition: the **default-theme visual regression baseline**
        #   gate (L2). The residual ruled by P24 is "bring the `Default` fallback theme into visual regression (add runtime
        #   evidence for the Default rules)"; the existing C-anp-P18/W101 258-shot matrix is human-reviewed evidence, not a machine
        #   criterion, and its closeout recommended moving screenshots to "generate on demand + commit a summary". This gate
        #   therefore turns visual regression into **structured metrics**: per page it captures key elements' computed styles
        #   (CSS-determined, stable across machines) and geometry (tolerance), compared against
        #   dev\default-theme-visual-baseline.json. A theme is selected only by "tab override → database setting → appSettings →
        #   Default" (the resolver rejects URL overrides), so the fixture `New-PortalDefaultThemeFixture.ps1` switches to Default
        #   temporarily and restores afterwards; A0 asserts every page's body carries the portal-theme-default class — if the
        #   fixture did not take effect the gate fails rather than pretending to pass. The fifth sampled page (tabid=1101) comes
        #   from measurement: the first four pages matched 0 of the 95 Default-scoped rules (a vacuous metric); this one hits 23/95.</en>
        # </lang>
        @{ File = 'Test-PortalDefaultThemeVisualBaseline.mjs'; Runner = 'mjs'; Kind = '默认主题视觉基线'; Args = @();
           Fixture = @{ Script = 'New-PortalDefaultThemeFixture.ps1'
                        Apply  = @(, @('-Action', 'Apply'))
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
#   </en>
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
    # <lang>
    #   <zh-CN>`[GATE-NOTE]` 是门禁自述的**注释通道**（见下方提取逻辑）：它用于带出"通过但值得注意"的结论，
    #   例如某门禁 exit 0 但其内部就绪指标为 False。注释文本可能含"缺失/未就绪"一类字样，
    #   若参与失败匹配就会把通过的门禁判成失败，故在此剔除 —— 与既有 `note:` 行同一处理原则。</zh-CN>
    #   <en>`[GATE-NOTE]` is a gate's self-reported **annotation channel** (extracted below): it surfaces "passing but worth
    #   attention" conclusions, such as a gate that exits 0 while its own readiness indicators are False. Annotation text may
    #   contain words like "missing" or "not ready", so including it in failure matching would score a passing gate as
    #   failing; it is therefore stripped here, applying the same principle already used for `note:` lines.</en>
    # </lang>
    # <lang>
    #   <zh-CN>`[soft]` 是门禁自述的**软指标 / 环境事实通道**：它登记"通过但受环境制约"的事实，
    #   例如 `Test-PortalRuntimeScreenshotBaseline` 打印"截图按需生成，**缺失**属环境事实，已登记"，
    #   而该门禁自身退出码 0、结论为 `[PASS]`。中文失败标志 `未能|缺失|不存在.*文件` 没有否定式，
    #   会把这句软提示里的"缺失"判成失败 —— 即"红色其实是误报"。这比漏判更隐蔽：它让套件长期假红，
    #   最终被人忽略（与 `W83` "门禁变弱比门禁报错更危险"同源）。
    #   故此处把 `[soft]` 行一并剔除，与 `note:` / `[GATE-NOTE]` 同理：剔除的是**原因说明**，不是结论；
    #   行级失败标志（`[Fail]` / `RESULT: Fail`）依然生效，真实失败不会被漏判。</zh-CN>
    #   <en>`[soft]` is a gate's self-reported **soft-metric / environment-fact channel**: it records facts that hold
    #   "passed but constrained by the environment", for example `Test-PortalRuntimeScreenshotBaseline` printing
    #   "screenshots are generated on demand; **absence** is an environment fact and is recorded", while the gate itself
    #   exits 0 and concludes `[PASS]`. The Chinese failure marker `未能|缺失|不存在.*文件` carries no negation, so the word
    #   for "absence" inside that soft note is scored as a failure — the red is a false positive. That is more insidious
    #   than a miss: it keeps the suite falsely red until people ignore it (the same root as `W83` "a weakened gate is
    #   more dangerous than a failing one").
    #   `[soft]` lines are therefore stripped here, exactly like `note:` / `[GATE-NOTE]`: what is removed is the **cause
    #   explanation**, not the verdict; line-level markers (`[Fail]` / `RESULT: Fail`) remain in force, so real failures
    #   are still caught.</en>
    # </lang>
    $effectiveOutput = (($output -split "`r?`n") | Where-Object { $_ -notmatch '^\s*note\s*:' -and $_ -notmatch '\[GATE-NOTE\]' -and $_ -notmatch '\[soft\]' }) -join "`n"
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
# <lang>
#   <zh-CN>`$prereqMissing` 标记本层是否有门禁因**前置条件缺失**而被跳过（exit 2）：这类跳过不是代码缺陷，
#   编排必须把它与真实失败（exit 1）分开，并以整体退出码 2 明示"前置缺失"，否则 CI 会把"环境没装 playwright"
#   误判成"产品坏了"。对应 `W-anp-P139`（playwright 前置固化）。</zh-CN>
#   <en>`$prereqMissing` marks whether any gate in this layer was **skipped for a missing prerequisite** (exit 2): such a skip is
#   not a code defect, and the suite must keep it separate from a real failure (exit 1) and surface it with an overall exit code of
#   2 meaning "prerequisite missing", so CI never misreads "playwright not installed" as "the product is broken". Belongs to
#   `W-anp-P139` (formalizing the playwright prerequisite).</en>
# </lang>
$prereqMissing = $false

# <lang>
#   <zh-CN>**运行级互斥：一次只允许一个门禁套件**（`C-anp-P38` / `W-anp-P215`，据此关闭备忘 D2）。
#     D2 的原文是"后续一次只启动一个套件，超时后先确认进程状态再重试"——那是**靠人守的约定**。
#     本周期把它变成机器断言：用**命名互斥体**而不是锁文件，理由有两条：
#     ① 进程崩溃或被 kill 时，操作系统会自动释放互斥体，**不存在残留锁**（锁文件会，且要靠时间戳猜新鲜度）；
#     ② 不需要在每条退出路径上写清理逻辑。
#     **为什么必须做**：套件里有改写工作树的步骤（`Build-Solution.ps1` 写 `bin/obj`、
#     `Build-PortalSite.ps1` 改写已签入的 `site/`）。两个套件同时跑不是"假失败"，
#     是**真的会把对方的产物改掉**——这正是 D2 当初观察到的现象，只是源头从 L2 切库变成了构建产物。
#     等待而不是立即失败：给人"另一个正在跑、我等它"的机会，而不是一上来就报错。</zh-CN>
#   <en>**Run-level mutual exclusion: only one gate suite at a time** (`C-anp-P38` / `W-anp-P215`, which closes memo D2).
#     D2's original wording was "from now on start only one suite at a time; after a timeout, confirm the process state
#     before retrying" — that was a **convention kept by people**. This cycle turns it into a machine assertion, using a
#     **named mutex** rather than a lock file, for two reasons: ① when a process crashes or is killed the OS releases the
#     mutex automatically, so **no stale lock can remain** (a lock file can, and staleness has to be guessed from
#     timestamps); ② no cleanup logic is needed on every exit path.
#     **Why it is necessary**: the suite contains steps that rewrite the working tree (`Build-Solution.ps1` writes
#     `bin/obj`, `Build-PortalSite.ps1` rewrites the committed `site/`). Two suites running at once is not a "false
#     failure" — they **really overwrite each other's artifacts**, which is exactly the phenomenon D2 observed, with the
#     source moved from L2 database switching to build artifacts. Waiting rather than failing immediately gives a person
#     the chance to say "another one is running, I'll wait for it" instead of an instant error.</en>
# </lang>
$mutexName = 'Global\HIA-ASPNETPortal-GateSuite'
$suiteMutex = New-Object System.Threading.Mutex($false, $mutexName)
$mutexHeld = $false
$mutexWaitSeconds = if ($LockWaitSeconds -gt 0) { $LockWaitSeconds } else { 600 }
$mutexWait = [TimeSpan]::FromSeconds($mutexWaitSeconds)
try {
    try {
        $mutexHeld = $suiteMutex.WaitOne($mutexWait)
    }
    catch [System.Threading.AbandonedMutexException] {
        # <lang>
        #   <zh-CN>上一个持有者**死了但没释放**（崩溃 / kill）。操作系统把互斥体交给我们——
        #     这正是选互斥体而不是锁文件的理由：不会有永远打不开的残留锁。记一笔再继续。</zh-CN>
        #   <en>The previous holder **died without releasing** (crash / kill). The OS hands the mutex to us —
        #     exactly why a mutex was chosen over a lock file: there can be no permanently stuck stale lock. Note it and
        #     continue.</en>
        # </lang>
        Write-Step '[WARN] 上一个门禁套件异常退出（互斥体被放弃），已接管——这不会造成产物冲突，因为对方已不在运行。'
        $mutexHeld = $true
    }
    if (-not $mutexHeld) {
        $msg = ('另一个门禁套件正在运行（互斥体 ' + $mutexName + '）。已等待 ' + $mutexWait.TotalSeconds + ' 秒仍未取得锁。请等它结束，或用 -LockWaitSeconds 0 立即失败以快速确认。（同时运行会互相覆盖构建产物：Build-Solution 写 bin/obj、Build-PortalSite 改写已签入的 site/。）')
        throw $msg
    }
    Write-Step ('已取得门禁套件互斥体（一次只运行一个；锁由操作系统在进程退出时自动释放）。')

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
        #   `BusinessWorkflow` 的 include 图里，只能由部署级 `Enabled` 追加。
        #   但**不能只有**它：基线里显式启用的 `HIA.LeaveRequest,HIA.ExpenseReimbursement` 同样只靠 `Enabled`
        #   进入门户，早先把该键整体写成 `HIA.ModuleProbe` 会把它们一起抹掉，于是整个 L2 期间这两个模块
        #   被 Profile 门禁阻断、不渲染 —— 运行期门禁因此报"模块根找不到"（`W-anp-P137` 实测根因）。
        #   故取**并集**：基线显式启用包 ∪ `HIA.ModuleProbe`，由共享脚本统一求解。</zh-CN>
        #   <en>`Enabled` must carry `HIA.ModuleProbe`: `Test-PortalModuleCache` asserts the DevProbe package, which is not part
        #   of the `BusinessWorkflow` include graph and can therefore only be appended through the deployment-level `Enabled`.
        #   It must **not carry only** that value, though: the baseline's explicitly enabled `HIA.LeaveRequest,HIA.ExpenseReimbursement`
        #   also reach the Portal solely through `Enabled`, so writing the key wholesale as `HIA.ModuleProbe` erased them too and
        #   left both modules blocked by the profile gate and unrendered throughout L2 — which is why the runtime gate reported
        #   "module root not found" (the root cause measured in `W-anp-P137`). Hence a **union**: baseline explicitly enabled
        #   packages ∪ `HIA.ModuleProbe`, resolved by the shared script.</en>
        # </lang>
        $l2EnabledPackages = Resolve-PortalRuntimeGatePackages -RepoRoot $repoRoot -GroupPackages @('HIA.ModuleProbe')
        Write-Step ("L2 档位 Enabled = $l2EnabledPackages")
        $profileJson = '{' + [char]10 + '    "appSettings": {' + [char]10 + '        "TestItem": "dev item",' + [char]10 + '        "Portal.ModuleProfiles.Active": "BusinessWorkflow",' + [char]10 + '        "Portal.ModulePackages.Enabled": "' + $l2EnabledPackages + '"' + [char]10 + '    }' + [char]10 + '}'
        [System.IO.File]::WriteAllText($moduleSettingsPath, $profileJson, [System.Text.UTF8Encoding]::new($false))
        (Get-Item $webConfigPath).LastWriteTime = Get-Date
        $profileManaged = $true
        # <lang>
        #   <zh-CN>改 web.config 时间戳会触发应用域回收，回收后首个请求需重新编译，耗时往往超过 3s。
        #   若不等就绪就跑门禁，首个门禁（如 Invoke-PortalModuleRuntimeGate）会打到未就绪实例而**误判失败**——
        #   这正是 2026-10-09 首次冷启动 L2 时出现过的竞态。故显式预热：轮询 40001 直到返回 HTTP 200
        #   （最多约 60s）；预热失败只告警、不终止，因为门禁本身也会重试/超时。</zh-CN>
        #   <en>Changing the web.config timestamp triggers an app-domain recycle; the first request afterwards recompiles and
        #   often takes longer than 3s. Running gates before the instance is ready makes the first gate (e.g.
        #   Invoke-PortalModuleRuntimeGate) hit an unready instance and **fail spuriously** — exactly the cold-start race seen
        #   on the first L2 run of 2026-10-09. We therefore warm up explicitly: poll 40001 until it returns HTTP 200 (up to
        #   ~60s); a failed warm-up only warns rather than stopping, because the gates themselves retry and time out.</en>
        # </lang>
        # <lang>
        #   <zh-CN>模块运行期缓存按"首次访问对应页签"填充；只打首页或少数页签，`Invoke-PortalModuleRuntimeGate`
        #   遍历 DB 中**全部**已部署模块实例时会打到未就绪模块而误判失败。故预热要覆盖所有已部署页签：
        #   读外置连接串 → 查 `PortalCfg_Modules` 中全部非空 TabId → 逐个请求直到返回 200（取页签失败则退化为首页预热）。</zh-CN>
        #   <en>The module runtime cache is populated per "first visit to the tab"; hitting only the homepage or a few tabs makes
        #   `Invoke-PortalModuleRuntimeGate` (which walks **every** deployed instance from the DB) hit not-yet-ready modules and
        #   fail spuriously. Warm-up must therefore cover every deployed tab: read the external connection string, query all
        #   non-null TabIds from `PortalCfg_Modules`, and request each until it returns 200 (falls back to homepage-only warm-up
        #   if the tab list cannot be read).</en>
        # </lang>
        $warmupUrls = @('http://localhost:40001/')
        try {
            $ccPath = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'
            [xml]$cc = Get-Content $ccPath
            $cs = ''
            foreach ($a in $cc.connectionStrings.add) { if ($a.name -match 'Portal') { $cs = $a.connectionString; break } }
            if ($cs) {
                $conn = New-Object System.Data.SqlClient.SqlConnection $cs
                $conn.Open()
                $cmd = $conn.CreateCommand()
                $cmd.CommandText = 'SELECT DISTINCT TabId FROM PortalCfg_Modules WHERE TabId IS NOT NULL'
                $rd = $cmd.ExecuteReader()
                while ($rd.Read()) { $warmupUrls += ('http://localhost:40001/DesktopDefault.aspx?tabid=' + $rd['TabId']) }
                $rd.Close(); $conn.Close()
                Write-Step ('L2 预热覆盖 ' + ($warmupUrls.Count - 1) + ' 个已部署页签')
            }
        } catch {
            Write-Step ('L2 预热取页签失败（' + $_.Exception.Message + '），退化为首页预热')
        }
        $warmupOk = $false
        for ($wi = 1; $wi -le 60; $wi++) {
            $allOk = $true
            foreach ($u in $warmupUrls) {
                try {
                    $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 8 -ErrorAction Stop
                    if ($r.StatusCode -ne 200) { $allOk = $false; break }
                } catch { $allOk = $false; break }
            }
            if ($allOk) { $warmupOk = $true; break }
            Start-Sleep -Seconds 2
        }
        if ($warmupOk) {
            Write-Step 'L2 档位已切换并预热 → BusinessWorkflow + HIA.ModuleProbe'
        } else {
            Write-Step 'L2 档位已切换，但 40001 预热未完成（门禁可能误判，建议手动确认实例）'
        }
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
            # <lang>
            #   <zh-CN>门禁专属环境变量（清单 `Env` 声明）：有的门禁默认档位只"记录现状"而不下断言，编排沿用默认档
            #   就会让它**通过却什么都没断言**（见该门禁清单处的注入法实测）。这里在调用前设置并在 `finally` 还原。</zh-CN>
            #   <en>Gate-specific environment variables (declared by the manifest's `Env`): some gates default to a phase that
            #   only records the current state and asserts nothing, so letting the suite use the default leaves them **passing
            #   while asserting nothing** (see the injection measurement recorded at that gate's manifest entry). They are set
            #   here before invocation and restored in `finally`.</en>
            # </lang>
            $envBackup = @{}
            if ($gate.ContainsKey('Env')) {
                foreach ($envKey in $gate.Env.Keys) {
                    $envBackup[$envKey] = [Environment]::GetEnvironmentVariable($envKey)
                    [Environment]::SetEnvironmentVariable($envKey, [string]$gate.Env[$envKey])
                }
            }

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
                # <lang>
                #   <zh-CN>`W-anp-P139`：playwright 是开发期依赖、不入库。所有 `mjs` 门禁都通过 `loadChromium()` 依赖它，
                #   缺失时门禁会抛 `Unable to resolve "playwright"`（exit 1）并被误判为**代码缺陷** —— 这正是本工作包要消的混报。
                #   故在此**统一**判定：playwright 可用 ⇔ 外部已设 `PORTAL_PLAYWRIGHT_MODULE` 且路径存在，或仓库
                #   `temp\node_modules\playwright` 存在。两者皆否则**显式跳过**（记 `Skip`、exit 2、写明原因），
                #   不调用 Node、不记失败。这样无论将来新增多少 `mjs` 门禁，前置缺失口径自动覆盖，无需逐门禁改。</zh-CN>
                #   <en>`W-anp-P139`: playwright is a dev-only dependency and is not committed. Every `mjs` gate depends on it through
                #   `loadChromium()`; when absent the gate throws `Unable to resolve "playwright"` (exit 1) and is misjudged as a
                #   **code defect** — exactly the misreport this work item removes. So the check is made **uniformly here**: playwright
                #   is available iff an external `PORTAL_PLAYWRIGHT_MODULE` is set to an existing path, or the repo's
                #   `temp\node_modules\playwright` exists. If neither holds, the gate is **explicitly skipped** (recorded as `Skip`,
                #   exit 2, with the reason stated), Node is not invoked and no failure is recorded. Thus however many `mjs` gates are
                #   added later, the missing-prerequisite convention covers them automatically without per-gate edits.</en>
                # </lang>
                $pwDir = Join-Path $repoRoot 'temp\node_modules\playwright'
                $pwAvailable = $false
                if ($env:PORTAL_PLAYWRIGHT_MODULE -and (Test-Path -LiteralPath $env:PORTAL_PLAYWRIGHT_MODULE)) {
                    $pwAvailable = $true
                } elseif (Test-Path -LiteralPath $pwDir) {
                    $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path $pwDir).Path
                    $pwAvailable = $true
                }
                if (-not $pwAvailable) {
                    $record.status = 'Skip'
                    $record.exitCode = 2
                    $record.note = 'playwright 前置缺失（未设置 PORTAL_PLAYWRIGHT_MODULE 且 temp\node_modules\playwright 不存在）；按 W-anp-P139 跳过，非代码缺陷'
                    Write-Step ("  [Skip] {0}（playwright 前置缺失）" -f $gate.File)
                    $results += $record
                    continue
                }
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
            if ($exit -eq 2) {
                # <lang>
                #   <zh-CN>门禁自报 `exit 2` = 前置条件缺失（不是代码缺陷）。`W-anp-P139` 之前这里被 `Test-GateFailed`
                #   的"非零退出码即失败"判定误记成 `Fail` —— 正是"环境没装"与"产品坏了"混报的根因。此处显式识别为 `Skip`。</zh-CN>
                #   <en>A gate's own `exit 2` means a missing prerequisite, not a code defect. Before `W-anp-P139` this was
                #   mis-scored as `Fail` by `Test-GateFailed`'s "non-zero exit means failure" rule — the very root of conflating
                #   "environment not installed" with "product broken". Here it is explicitly recognised as `Skip`.</en>
                # </lang>
                $record.status = 'Skip'
                if (-not $record.note) { $record.note = '门禁自报前置条件缺失（exit 2）' }
                $prereqMissing = $true
            } elseif (Test-GateFailed $text $exit) {
                $record.status = 'Fail'
                $overallFailed = $true
                $record.note = '退出码或输出含失败标志'
            }
            # <lang>
            #   <zh-CN>提取门禁自述的 `[GATE-NOTE]` 注释并附到汇总行：这类门禁**通过但带有值得注意的结论**
            #   （实测典型：`Test-PortalIeModeReadiness` exit 0 而其就绪指标全为 False）。只看退出码的话，
            #   绿色会把"该能力实际不可用"这一事实掩盖掉 —— 即 `W83` 所说"**门禁变弱比门禁报错更危险**"。
            #   注释只进汇总与证据，**不改变通过/失败判定**（是否失败仍由退出码与失败标志决定）。</zh-CN>
            #   <en>Extract a gate's self-reported `[GATE-NOTE]` annotations and attach them to the summary line: such gates
            #   **pass but carry a conclusion worth attention** (the measured example: `Test-PortalIeModeReadiness` exits 0
            #   while all of its readiness indicators are False). Judging by exit code alone lets the green result hide the
            #   fact that the capability is actually unavailable — precisely what `W83` called "**a weakened gate is more
            #   dangerous than a failing one**". Annotations reach the summary and the evidence only; they **never change the
            #   pass/fail verdict**, which still rests on the exit code and the failure markers.</en>
            # </lang>
            $gateNotes = @()
            foreach ($noteLine in ($text -split "`r?`n")) {
                if ($noteLine -match '\[GATE-NOTE\]\s*(.+)') { $gateNotes += $Matches[1].Trim() }
            }
            if ($gateNotes.Count -gt 0) {
                $record.note = if ($record.note) { $record.note + '; ' + ($gateNotes -join '; ') } else { ($gateNotes -join '; ') }
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
            #   <zh-CN>门禁专属环境变量**必须**还原，理由与夹具相同：一次失败或中断若把 `PORTAL_P82_PHASE=after`
            #   之类的档位留在进程环境里，后续门禁乃至下一次编排运行会在错误档位下得出看似正确的结论。
            #   `$envBackup` 未初始化时（该门禁无 `Env` 声明或在设置前就抛错）循环自然为空，无需额外判空。</zh-CN>
            #   <en>Gate-specific environment variables **must** be restored, for the same reason as fixtures: a failure or
            #   interruption that leaves something like `PORTAL_P82_PHASE=after` in the process environment would make later
            #   gates — or the very next suite run — reach conclusions under the wrong phase while still looking correct.
            #   When `$envBackup` was never initialised (the gate declares no `Env`, or it threw before the values were set)
            #   the loop is naturally empty, so no extra null check is needed.</en>
            # </lang>
            foreach ($envKey in $envBackup.Keys) {
                [Environment]::SetEnvironmentVariable($envKey, $envBackup[$envKey])
            }
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

# <lang>
#   <zh-CN>互斥体在这里释放。用 `try/finally` 包住整个执行段，而不是在每条 `exit` 前写一遍：
#   PowerShell 会保证 `finally` 在 `exit` 之前执行，所以三条退出路径（失败 / 前置缺失 / 成功）
#   以及任何异常都走同一处清理——**清理逻辑只有一份，就不会漏**。</zh-CN>
#   <en>The mutex is released here. The whole execution segment is wrapped in `try/finally` rather than writing cleanup
#     before each `exit`: PowerShell guarantees `finally` runs before `exit`, so all three exit paths (failure /
#     prerequisite missing / success) and any exception go through one cleanup — **there is only one copy of the cleanup
#     logic, so it cannot be missed**.</en>
# </lang>
$summary = [pscustomobject]@{
    phase = 'W-anp-P85 gate suite'
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    layer = $Layer
    result = if ($overallFailed) { 'Fail' } elseif ($prereqMissing) { 'PassWithSkips' } else { 'Pass' }
    gateCount = $results.Count
    failedCount = ($results | Where-Object { $_.status -eq 'Fail' }).Count
    skippedCount = ($results | Where-Object { $_.status -eq 'Skip' }).Count
    prereqMissing = $prereqMissing
    results = $results
}
$summaryPath = Join-Path $EvidenceDir 'gate-suite-summary.json'
($summary | ConvertTo-Json -Depth 6) | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host ''
Write-Step ("结果：{0}｜门禁 {1} 个，失败 {2}，跳过 {3}" -f $summary.result, $summary.gateCount, $summary.failedCount, $summary.skippedCount)
Write-Step "证据：$EvidenceDir"
if ($overallFailed) { exit 1 }
if ($prereqMissing) { exit 2 }
exit 0
}
finally {
    # <lang>
    #   <zh-CN>无论成功、失败还是异常，都在这里释放互斥体。**先判 `$mutexHeld` 再释放**——
    #     没拿到锁就释放会抛 `ApplicationException`（"Object synchronization method was called from an
    #     unsynchronized block of code"），把一次"另一个套件在跑"的清晰失败糊成难懂的异常。</zh-CN>
    #   <en>The mutex is released here on success, failure or exception. **Check `$mutexHeld` before releasing** —
    #     releasing without holding throws `ApplicationException` ("Object synchronization method was called from an
    #     unsynchronized block of code"), which would smear a clear "another suite is running" failure into an
    #     unintelligible exception.</en>
    # </lang>
    if ($mutexHeld) {
        try { $suiteMutex.ReleaseMutex() } catch { Write-Step ('[WARN] 释放互斥体失败：' + $_.Exception.Message) }
    }
    try { $suiteMutex.Dispose() } catch { }
}

