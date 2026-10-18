#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>页签名与模块标题多语言运行期证据门禁：对运行中的站点做 zh / en 双文化断言，证明多语言链路真的通。</zh-CN>
  <en>Runtime evidence gate for localized tab names and module titles: asserts against the running site in both zh and en cultures, proving the multilingual chain actually works.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>前置条件：IIS Express 已启动（默认 40001）；验收上下文 temp\p65\p65-acceptance-context.json 存在；
  PORTAL_PLAYWRIGHT_MODULE 已指向本机 temp\node_modules\playwright。前置缺失时显式跳过并登记（exit 2），
  不以 exit=1 冒充失败 —— 这是 C-anp-P26 W-anp-P139 要固化的口径，本门禁先行采用。

  与 Test-PortalLocalizedNameGate.ps1（L0 静态门禁）的分工：静态门禁证明链路齐备且互相引用正确；
  本门禁证明链路在真实站点上真的把中英文分开了。两者互补 —— 只做静态检查会漏掉
  「接线正确但档位不含该包」这类问题（C-anp-P29 收口时实测踩到）。

  断言项：RT0 登录态已建立；RT1/RT2/RT3 页签名 zh 取中文列、en 取英文列、zh 不泄漏英文；
  RT12/RT13 清洗后的种子页签名；RT7 稳定 tabid 不漂移；RT14 共享标题控件已渲染；
  RT15/RT16/RT17 模块标题 zh 取中文列、en 取英文列、en 不泄漏中文；
  RT18/RT19/RT20 自绘标题模块资源键改造后随语言切换；RT11 负向对照；
  RT21 第三语言 JSON 列端到端生效（fr-FR：首页渲染出 `Accueil`）。

  证明边界：证明页签名与模块标题在 zh / en / 第三语言（JSON 列）下分别取到对应语言值且门控键不漂移；
  不证明翻译内容正确、或页面其它部分本地化正确。</zh-CN>
  <en>Prerequisites: IIS Express running (default port 40001); temp\p65\p65-acceptance-context.json present;
  PORTAL_PLAYWRIGHT_MODULE pointing at the local temp\node_modules\playwright. When a prerequisite is missing the gate is
  explicitly skipped and registered (exit 2) rather than failing with exit=1 — the convention C-anp-P26 W-anp-P139 is meant to
  formalize, adopted here ahead of that work item.

  Division of labour with Test-PortalLocalizedNameGate.ps1 (the L0 static gate): the static gate proves the chain is complete and
  cross-references correctly; this gate proves the chain really separates Chinese from English on a live site. They are
  complementary — static checks alone would miss problems such as correctly wired but the profile does not include that package
  (measured during the C-anp-P29 closeout).

  Assertions: RT0 authenticated session established; RT1/RT2/RT3 tab names take the Chinese column under zh, the English column
  under en, and zh does not leak the English value; RT12/RT13 cleaned seed tab names; RT7 stable tabid does not drift;
  RT14 shared title control rendered; RT15/RT16/RT17 module titles take the Chinese column under zh, the English column under en,
  and en does not leak the Chinese value; RT18/RT19/RT20 self-drawn-title modules follow the language switch after the resource-key
  conversion; RT11 negative control; RT21 the third-language JSON column works end-to-end (fr-FR: the home tab renders `Accueil`).

  Proof boundary: proves that tab names and module titles take their language-specific column values under zh, en, and a third
  language (the JSON column), and that the gate key does not drift; it does not prove translation correctness, or that the rest of
  the page is localized correctly.</en>
</lang>
#>

param(
    [string] $RepoRoot,
    [string] $BaseUrl = 'http://localhost:40001',
    [int] $BusinessTabId = 1101,
    [int] $SharedTitleTabId = 1011
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

$utf8 = [System.Text.UTF8Encoding]::new($false)

# <lang>
#   <zh-CN>前置检查：逐项给出明确结论，缺哪项就说哪项，不用一句环境不可用带过。</zh-CN>
#   <en>Prerequisite checks: report each item explicitly and name the missing one, rather than a blanket environment-unavailable.</en>
# </lang>
$contextPath = Join-Path $RepoRoot 'temp\p65\p65-acceptance-context.json'
$pwModule = $env:PORTAL_PLAYWRIGHT_MODULE

$siteUp = $false
try {
    $null = Invoke-WebRequest -Uri "$BaseUrl/DesktopDefault.aspx?tabid=1" -UseBasicParsing -TimeoutSec 10
    $siteUp = $true
}
catch { }

$missing = [System.Collections.Generic.List[string]]::new()
if (-not $siteUp) { $missing.Add('站点未响应 ' + $BaseUrl) }
if (-not (Test-Path -LiteralPath $contextPath)) { $missing.Add('验收上下文缺失 ' + $contextPath) }
if ([string]::IsNullOrWhiteSpace($pwModule)) { $missing.Add('PORTAL_PLAYWRIGHT_MODULE 未设置') }

if ($missing.Count -gt 0) {
    Write-Host '  [GATE-NOTE] 运行期多语言证据门禁跳过：'
    foreach ($m in $missing) { Write-Host ('    - ' + $m) }
    Write-Host '  [GATE-NOTE] 该门禁属 L2（需运行站点与 playwright）；跳过不等于通过，需在具备前置的环境中补跑。'

    $skipPolicy = 'exit 2 when prerequisites are missing'
    $skipProves = 'nothing on this run'
    $skipNotProves = 'zh and en resolve tab names and module titles to their own language columns'
    Write-Output ([pscustomobject]@{
        Gate           = 'Test-PortalLocalizedNameRuntimeEvidence.ps1'
        ExecutionMode  = 'runtime-evidence'
        ExitCodePolicy = $skipPolicy
        Skipped        = $true
        Missing        = ($missing -join '; ')
        Proves         = $skipProves
        DoesNotProve   = $skipNotProves
    })
    exit 2
}

$context = Get-Content -LiteralPath $contextPath -Raw -Encoding UTF8 | ConvertFrom-Json
$base = ([string] $context.baseUrl).TrimEnd('/')
$adminUser = [string] $context.adminUserName
$adminPassword = [string] $context.password

# <lang>
#   <zh-CN>登录表单内嵌在 DesktopDefault.aspx 左侧 signin 模块（实测输入框 id 为 ..._EmailOrName 与 ..._password，
#   按钮为 ..._SigninBtn）；站点没有独立登录页。
#   键名**必须全小写**：辅助 mjs 读 `spec.selectors.username`，而 PowerShell 哈希表的键原样进入 JSON ——
#   首版写成 `Username` 大写，mjs 取到 undefined、登录表单根本没填，脚本静默不产出结果（实测踩过）。</zh-CN>
#   <en>The sign-in form is embedded in DesktopDefault.aspx's left-hand signin module (measured input ids ..._EmailOrName and
#   ..._password, button ..._SigninBtn); the site has no standalone login page.
#   The keys **must be all lowercase**: the helper mjs reads `spec.selectors.username`, and PowerShell hashtable keys pass into JSON
#   verbatim — the first version used capital `Username`, so the mjs got undefined, the form was never filled, and the script
#   silently produced no output (hit in practice).</en>
# </lang>
$signInSelectors = @{
    username = 'input[id$="EmailOrName"]'
    password = 'input[id$="password"]'
    submit   = '#ctl00_MainContent_ctl01_SigninBtn, input[type="submit"]'
}

# <lang>
#   <zh-CN>断言用的探测串：全部取自本周期清洗后的真实数据，不写可能会出现的字符串。</zh-CN>
#   <en>Probe strings for assertions: all taken from real data cleaned in this cycle, never strings that might appear.</en>
# </lang>
$probes = @{
    ChineseTab            = '测试页'
    EnglishTab            = 'Test Page'
    SeededChineseTab      = '首页'
    SeededEnglishTab      = 'Home'
    SeededChineseTab2     = '员工信息'
    SeededEnglishTab2     = 'Employee Information'
    SharedTitleControl    = 'portal-module-title'
    ChineseModuleTitle    = '我的待办'
    EnglishModuleTitle    = 'My To-Do Items'
    SelfDrawnChineseTitle = '请假申请'
    SelfDrawnEnglishTitle = 'Leave Request'
    ExpenseEnglishTitle   = 'Expense Reimbursement'
    BusinessTabIdQuery    = 'tabid=' + $BusinessTabId
    SharedTitleTabIdQuery = 'tabid=' + $SharedTitleTabId
    # <lang>
    #   <zh-CN>第三语言 JSON 列端到端验证用的探测串。`fr-FR` 是种子数据里已经存在的第三语言（首页的
    #   `TabNameLocalizedJson` 含 `{"fr-FR":"Accueil",...}`），无需改库即可验证 JSON 列在真实站点上生效。</zh-CN>
    #   <en>Probe string for the end-to-end third-language JSON-column check. `fr-FR` is a third language already present in the seed
    #   data (the home tab's `TabNameLocalizedJson` contains `{"fr-FR":"Accueil",...}`), so the JSON column can be verified on the
    #   live site without mutating the database.</en>
    # </lang>
    ThirdLangCulture     = 'fr-FR'
    ThirdLangValue       = 'Accueil'
}

$checks = [System.Collections.Generic.List[object]]::new()

# <lang>
#   <zh-CN>记录一条断言。detail 一律用字符串拼接而非表达式，避免 PowerShell 把布尔表达式当位置参数解析。</zh-CN>
#   <en>Records one assertion. detail is always built by string concatenation rather than an expression, so PowerShell cannot
#   misparse a boolean expression as a positional argument.</en>
# </lang>
function Add-Check([string] $code, [bool] $passed, [string] $detail) {
    $script:checks.Add([pscustomobject]@{ Code = $code; Passed = $passed; Detail = $detail })
}

# <lang>
#   <zh-CN>把「是否包含」与「说明文字」一起生成，调用处只传一个字符串，彻底避开布尔表达式作为参数的解析歧义。</zh-CN>
#   <en>Builds the containment result and its description together so call sites pass a single string, entirely avoiding the parsing
#   ambiguity of a boolean expression used as an argument.</en>
# </lang>
function New-ContainsCheck([string] $code, [bool] $condition, [string] $label) {
    Add-Check -code $code -passed $condition -detail ($label + '=' + $condition)
}

# <lang>
#   <zh-CN>playwright 辅助脚本独立成文件而非嵌在 here-string 里：here-string 内的双斜杠注释与小于号会被 PowerShell
#   解析层误判（实测两次 ParserError），拆开后 PowerShell 与 Node 各自解析自己的语法。</zh-CN>
#   <en>The playwright helper is a separate file rather than embedded in a here-string: double-slash comments and less-than signs
#   inside a here-string are misjudged by the PowerShell parsing layer (two ParserErrors measured in practice); splitting them lets
#   PowerShell and Node each parse their own syntax.</en>
# </lang>
$probeScript = Join-Path $PSScriptRoot 'PortalLocalizedNameRuntimeProbe.mjs'
if (-not (Test-Path -LiteralPath $probeScript)) {
    throw ('缺少 playwright 辅助脚本 ' + $probeScript)
}

# <lang>
#   <zh-CN>本门禁**必须自带档位切换**才能验证模块标题链路。实测依据：走 `DesktopModuleTitle` 共享控件的模块
#   （`MyWorkItems`）所在页签在默认 `CoreOnly` 档位下**整页不渲染** —— `HIA.MyWorkItems` 不在该档位的 include 图里。
#   这与页签授权无关（1011 与 1101 的 `AccessRoles` 完全相同，都是 `All Users;`），实测切到 `BusinessWorkflow`
#   后共享标题控件立刻出现。早先版本不切档位，RT14/15/16 三条恒失败。
#
#   `Enabled` 取**并集**（基线显式启用包 ∪ 本门禁被测包），沿用 `W-anp-P137` 建立的共享解析：
#   早先直接整体替换成单个包名会抹掉只靠该键进入门户的 `HIA.LeaveRequest` / `HIA.ExpenseReimbursement`，
#   于是业务页模块不渲染、RT18/19/20 也会跟着失败 —— 那正是 `W-anp-P137` 修掉的故障形态。
#
#   切换在 `finally` 中**逐字节还原** `appSettings.dev.json` 并复原 `web.config` 时间戳，不留残留。</zh-CN>
#   <en>This gate **must switch the deployment profile itself** to verify the module-title path. Measured basis: modules using the
#   `DesktopModuleTitle` shared control (`MyWorkItems`) live on a tab that renders **nothing at all** under the default `CoreOnly`
#   profile — `HIA.MyWorkItems` is absent from that profile's include graph. This has nothing to do with tab authorization (1011 and
#   1101 carry identical `AccessRoles`, both `All Users;`); measured, switching to `BusinessWorkflow` makes the shared title control
#   appear immediately. An earlier version did not switch profiles, so RT14/15/16 failed permanently.
#
#   `Enabled` is a **union** (baseline explicitly enabled packages ∪ this gate's probed packages), reusing the shared resolution
#   established in `W-anp-P137`: replacing the key wholesale with a single package name erases `HIA.LeaveRequest` /
#   `HIA.ExpenseReimbursement`, which reach the Portal solely through that key, so the business-page modules stop rendering and
#   RT18/19/20 fail too — exactly the failure shape `W-anp-P137` fixed.
#
#   The switch restores `appSettings.dev.json` byte-for-byte and reverts the `web.config` timestamp in `finally`, leaving no residue.</en>
# </lang>
. (Join-Path $PSScriptRoot 'Resolve-PortalRuntimeGatePackages.ps1')

$settingsPath = Join-Path $RepoRoot 'src\Portal\Config\appSettings.dev.json'
$webConfigPath = Join-Path $RepoRoot 'src\Portal\web.config'
$settingsBackup = $null
$webConfigStamp = $null

$probedPackages = @(
    (Get-PortalModulePackageId -RepoRoot $RepoRoot -SourceFile 'DesktopModules/MyWorkItems/MyWorkItems.ascx')
    (Get-PortalModulePackageId -RepoRoot $RepoRoot -SourceFile 'DesktopModules/LeaveRequest/LeaveRequest.ascx')
    (Get-PortalModulePackageId -RepoRoot $RepoRoot -SourceFile 'DesktopModules/ExpenseReimbursement/ExpenseReimbursement.ascx')
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

$enabledPackages = Resolve-PortalRuntimeGatePackages -RepoRoot $RepoRoot -GroupPackages $probedPackages
Write-Host ('档位切至 BusinessWorkflow ｜ Enabled = ' + $enabledPackages)

$nodePath = Join-Path $env:TEMP ('portal-l10n-evidence-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $nodePath | Out-Null

try {
    # <lang>
    #   <zh-CN>切换档位并触发 AppDomain 回收（改 `web.config` 时间戳），否则站点仍在跑旧配置。
    #   预热重试到 200 为止：回收后第一次请求可能仍在编译。</zh-CN>
    #   <en>Switch the profile and trigger an AppDomain recycle (by touching the `web.config` timestamp), otherwise the site keeps
    #   running the old configuration. Warm up with retries until 200: the first request after a recycle may still be compiling.</en>
    # </lang>
    if (Test-Path -LiteralPath $settingsPath) {
        $settingsBackup = [System.IO.File]::ReadAllText($settingsPath, $utf8)
    }
    if (Test-Path -LiteralPath $webConfigPath) {
        $webConfigStamp = (Get-Item -LiteralPath $webConfigPath).LastWriteTime
    }

    $profileJson = '{' + [char]10 +
        '    "appSettings": {' + [char]10 +
        '        "TestItem": "dev item",' + [char]10 +
        '        "Portal.ModuleProfiles.Active": "BusinessWorkflow",' + [char]10 +
        '        "Portal.ModulePackages.Enabled": "' + $enabledPackages + '"' + [char]10 +
        '    }' + [char]10 + '}'
    [System.IO.File]::WriteAllText($settingsPath, $profileJson, $utf8)
    if ($null -ne $webConfigStamp) {
        (Get-Item -LiteralPath $webConfigPath).LastWriteTime = Get-Date
    }
    Start-Sleep -Seconds 10

    $warmed = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        try {
            $probe = Invoke-WebRequest -Uri ($base + '/DesktopDefault.aspx?tabid=1') -UseBasicParsing -TimeoutSec 10
            if ($probe.StatusCode -eq 200) { $warmed = $true; break }
        }
        catch { }
        Start-Sleep -Seconds 2
    }
    if (-not $warmed) { throw ('切换档位后站点未恢复 ' + $base) }

    $specPath = Join-Path $nodePath 'spec.json'
    $outPath = Join-Path $nodePath 'out.json'

    [System.IO.File]::WriteAllText(
        $specPath,
        ([pscustomobject]@{
            base          = $base
            adminUser     = $adminUser
            adminPassword = $adminPassword
            selectors     = $signInSelectors
            targets       = @(
                @{ key = 'businessZh'; culture = 'zh-CN'; tabId = $BusinessTabId }
                @{ key = 'businessEn'; culture = 'en-US'; tabId = $BusinessTabId }
                @{ key = 'sharedZh';   culture = 'zh-CN'; tabId = $SharedTitleTabId }
                @{ key = 'sharedEn';   culture = 'en-US'; tabId = $SharedTitleTabId }
                @{ key = 'jsonFr';     culture = $probes.ThirdLangCulture; tabId = $BusinessTabId }
            )
        } | ConvertTo-Json -Depth 6),
        $utf8)

    $nodeExe = 'C:\Program Files\nodejs\node.exe'
    if (-not (Test-Path -LiteralPath $nodeExe)) { $nodeExe = 'node' }
    & $nodeExe $probeScript $specPath $outPath 2>&1 | Out-Null
    if (-not (Test-Path -LiteralPath $outPath)) {
        throw ('playwright 脚本未产出结果 ' + $outPath)
    }

    $pages = Get-Content -LiteralPath $outPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $zh = [string] $pages.businessZh
    $en = [string] $pages.businessEn
    $zhShared = [string] $pages.sharedZh
    $enShared = [string] $pages.sharedEn
    $fr = [string] $pages.jsonFr

    Write-Host ('lengths businessZh=' + $zh.Length + ' businessEn=' + $en.Length + ' sharedZh=' + $zhShared.Length + ' sharedEn=' + $enShared.Length)

    New-ContainsCheck 'RT0-LOGGED-IN' $zh.Contains($adminUser) '登录态已建立（欢迎语含账号）'

    New-ContainsCheck 'RT1-ZH-TABNAME' $zh.Contains($probes.ChineseTab) 'zh 含中文页签名'
    New-ContainsCheck 'RT2-EN-TABNAME' $en.Contains($probes.EnglishTab) 'en 含英文列页签名'
    New-ContainsCheck 'RT3-ZH-NO-EN-LEAK' (-not $zh.Contains($probes.EnglishTab)) 'zh 不含英文列值'

    $seededZh = $zhShared.Contains($probes.SeededChineseTab) -and $zhShared.Contains($probes.SeededChineseTab2)
    $seededEn = $enShared.Contains($probes.SeededEnglishTab) -and $enShared.Contains($probes.SeededEnglishTab2)
    New-ContainsCheck 'RT12-SEEDED-ZH' $seededZh 'zh 含清洗后的种子中文页签名'
    New-ContainsCheck 'RT13-SEEDED-EN' $seededEn 'en 含种子英文页签名'

    $stableTabId = $en.Contains($probes.BusinessTabIdQuery) -and $enShared.Contains($probes.SharedTitleTabIdQuery)
    New-ContainsCheck 'RT7-STABLE-TABID' $stableTabId 'en 仍含稳定 tabid（门控键未漂移）'

    $sharedRendered = $zhShared.Contains($probes.SharedTitleControl) -or $enShared.Contains($probes.SharedTitleControl)
    New-ContainsCheck 'RT14-SHARED-TITLE-RENDERED' $sharedRendered '共享标题控件已渲染'

    New-ContainsCheck 'RT15-MODULE-TITLE-ZH' $zhShared.Contains($probes.ChineseModuleTitle) 'zh 含中文模块标题'
    New-ContainsCheck 'RT16-MODULE-TITLE-EN' $enShared.Contains($probes.EnglishModuleTitle) 'en 含英文模块标题'
    New-ContainsCheck 'RT17-MODULE-TITLE-NO-ZH-LEAK' (-not $enShared.Contains($probes.ChineseModuleTitle)) 'en 不含中文模块标题'

    New-ContainsCheck 'RT18-SELFDRAWN-TITLE-ZH' $zh.Contains($probes.SelfDrawnChineseTitle) 'zh 含自绘标题中文（资源键）'
    New-ContainsCheck 'RT19-SELFDRAWN-TITLE-EN' $en.Contains($probes.SelfDrawnEnglishTitle) 'en 含自绘标题英文（资源键）'
    New-ContainsCheck 'RT20-EXPENSE-TITLE-EN' $en.Contains($probes.ExpenseEnglishTitle) 'en 含报销模块英文标题'

    $noSharedOnBusiness = (-not $zh.Contains($probes.SharedTitleControl)) -and (-not $en.Contains($probes.SharedTitleControl))
    New-ContainsCheck 'RT11-NO-SHARED-TITLE-ON-BUSINESS' $noSharedOnBusiness '业务页两种文化下均不经共享标题控件（负向对照）'

    # <lang>
    #   <zh-CN>第三语言 JSON 列端到端验证：以 `fr-FR` 文化请求业务页，导航里首页（id=1）的 `TabNameLocalizedJson` 含
    #   `{"fr-FR":"Accueil"}`，解析器三级键匹配命中 `fr-FR`，因此首页名应渲染为 `Accueil`。这条证明 JSON 列在真实站点上生效，
    #   无需改动数据库（数据本就存在）。</zh-CN>
    #   <en>End-to-end third-language JSON-column check: request the business page under the `fr-FR` culture; the home tab (id=1) in the
    #   navigation carries `{"fr-FR":"Accueil"}` in `TabNameLocalizedJson`, the resolver's three-tier key lookup hits `fr-FR`, so the
    #   home tab name should render as `Accueil`. This proves the JSON column works on the live site without mutating the database
    #   (the data is already there).</en>
    # </lang>
    New-ContainsCheck 'RT21-JSON-THIRD-LANG' $fr.Contains($probes.ThirdLangValue) ('fr-FR 文化下首页渲染出 JSON 列值 ' + $probes.ThirdLangValue)
}
finally {
    # <lang>
    #   <zh-CN>还原顺序：先恢复档位配置并触发回收，再删临时目录。
    #   配置必须**逐字节**写回而非重新序列化 —— 重新序列化会改变键顺序与缩进，在用户本地留下看不出原因的差异。
    #   若原本不存在该文件（未覆盖部署），则删除我们新建的文件而不是写回内容。</zh-CN>
    #   <en>Restore order: write the profile configuration back and trigger a recycle first, then delete the temporary directory.
    #   The file must be written back **byte-for-byte** rather than re-serialized — re-serializing changes key order and indentation,
    #   leaving an inexplicable-looking difference in the user's local environment. If the file did not exist beforehand (an
    #   un-overridden deployment), delete the file we created instead of writing content back.</en>
    # </lang>
    if ($null -ne $settingsBackup) {
        [System.IO.File]::WriteAllText($settingsPath, $settingsBackup, $utf8)
    }
    elseif (Test-Path -LiteralPath $settingsPath) {
        Remove-Item -LiteralPath $settingsPath -Force -ErrorAction SilentlyContinue
    }
    if ($null -ne $webConfigStamp) {
        (Get-Item -LiteralPath $webConfigPath).LastWriteTime = $webConfigStamp
    }
    Remove-Item -LiteralPath $nodePath -Recurse -Force -ErrorAction SilentlyContinue
}

$failedChecks = @($checks | Where-Object { -not $_.Passed }).Count

Write-Host ''
foreach ($c in $checks) {
    $mark = if ($c.Passed) { '[Pass]' } else { '[Fail]' }
    Write-Host ('  ' + $mark + ' ' + $c.Code + ' — ' + $c.Detail)
}

$resultPolicy = 'exit 2 when prerequisites are missing / exit 1 when an assertion fails'
$resultProves = 'zh and en resolve tab names and module titles to their own language columns, gate key does not drift'
$resultNotProves = 'translation correctness or localization of the rest of the page'
Write-Output ([pscustomobject]@{
    Gate           = 'Test-PortalLocalizedNameRuntimeEvidence.ps1'
    ExecutionMode  = 'runtime-evidence'
    ExitCodePolicy = $resultPolicy
    Skipped        = $false
    FailedChecks   = $failedChecks
    CheckCount     = $checks.Count
    Proves         = $resultProves
    DoesNotProve   = $resultNotProves
})

if ($failedChecks -gt 0) { exit 1 }

exit 0
