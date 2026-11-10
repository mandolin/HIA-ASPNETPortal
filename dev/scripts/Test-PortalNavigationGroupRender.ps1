#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>主导航两级分组渲染的静态契约一致性门禁（L0）。不连数据库、不启动站点、不执行浏览器。</zh-CN>
  <en>Static contract-consistency gate (L0) for two-level main-navigation grouped rendering. No database, site, or browser is used.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>解决的问题（`C-anp-P51` / `W-anp-P291`）：Q1 裁定 ③ 把 R4 首子项落成「分组标题 disclosure + 组内平铺」两级展示。
  若只靠人工看代码，以下五类漂移都不会被构建或单测发现：① 分组视图被塞入授权判定，或把未分组页签静默丢弃；
  ② 分组标题退回内联 onclick / menu / menubar role，或选中态变成可点击链接，破坏 APG disclosure 与 a11y 契约；
  ③ 7 套皮肤的作用域规则被删或只补了 6 套，深色/回退主题丢失分组样式；④ 渐进增强脚本引入 IE9 不支持的 API，
  或服务端不再输出默认展开态，导致「无 JS 时页签可达」失效；⑤ 降级回滚点消失，非法配置不再能退回一级平铺。
  本门禁把五类约束变成机器可核对的**静态契约**，判定从权威事实源（源码 / 标记 / 7 套 CSS / csproj / 配置）推导，不硬编码数据行。</zh-CN>
  <en>Problem solved (`C-anp-P51` / `W-anp-P291`): Q1 ruling ③ implements R4's first sub-criterion as "group-header disclosure
  plus flat in-group links". Five drifts would escape builds and unit tests if checked only by eye: (1) grouping acquires
  authorization decisions or silently drops ungrouped tabs; (2) group headers regress to inline onclick / menu / menubar
  roles, or the selected state becomes a clickable link, breaking APG disclosure and a11y; (3) the seven theme scope rule
  blocks are deleted or only six are supplied, so dark/fallback themes lose group styling; (4) the progressive-enhancement
  script adopts IE9-unsupported APIs or the server stops emitting default-expanded markup, breaking "tabs reachable without
  JavaScript"; (5) the rollback point disappears so invalid configuration can no longer degrade to flat navigation.
  This gate turns the five into machine-checkable **static contracts**, derived from authoritative sources (source / markup /
  seven theme CSS files / csproj / config) rather than hard-coded data rows.</en>
</lang>

  <zh-CN>断言分组（全部 fail-closed，任一异常即判红）：</zh-CN>
  <en>Assertion groups (all fail-closed; any exception fails the gate):</en>
  <zh-CN>
    R1 视图层 A 级边界：`PortalNavigationGroupView.cs` 代码文本不得引用授权符号；必须保留未分组平铺集合、
       显示下标不变量、`Degraded` 降级标记与空组计数；不得出现父子表结构字段。
    R2 标记层 disclosure：`DesktopPortalBanner.ascx` 必须含 `button[type=button]` + `aria-expanded` + `aria-controls`，
       不得含 `role="menu"` / `menubar` 或内联 onclick；选中态仍为非链接 span/选中类；降级用 DataList 仍保留。
    R3 七套皮肤作用域：6 正式 + `Default` 回退共 7 套 `Default.css` 各自必须含
       `body.portal-theme-<名> .portal-nav-group-toggle` / `.portal-nav-group-hidden` / `:focus` 等关键规则。
    R4 渐进增强脚本：`PortalNavigationGroups.js` 必须已登记 csproj、使用 ES5 兼容 API、含 Escape 回焦与默认展开说明，
       且不使用 classList / 箭头函数 / const / let / addEventListener 单通道。
    R5 接线与回滚：csproj 登记视图类、单测、脚本；codebehind 必须同时存在分组绑定与降级分支；
       `Portal.Navigation.GroupAliases` 必须在 Web.config appSettings 登记。
  </zh-CN>
  <en>
    R1 view-layer class-A boundary: `PortalNavigationGroupView.cs` code text must not reference authorization symbols;
       it must keep the ungrouped flat collection, the display-index invariant, the `Degraded` flag and the empty-group
       count, and must not introduce parent/child schema fields.
    R2 markup disclosure: `DesktopPortalBanner.ascx` must contain `button[type=button]` plus `aria-expanded` and
       `aria-controls`, with no `role="menu"` / `menubar` or inline onclick; the selected state stays a non-link span /
       selected class; the flat DataList rollback path remains.
    R3 seven theme scopes: all seven `Default.css` files (six official plus `Default` fallback) must carry
       `body.portal-theme-<name> .portal-nav-group-toggle`, `.portal-nav-group-hidden`, `:focus` and related rules.
    R4 progressive-enhancement script: `PortalNavigationGroups.js` must be registered in csproj, use ES5 APIs, include
       Escape focus return and default-expanded documentation, and avoid classList / arrow functions / const / let /
       an addEventListener-only binding path.
    R5 wiring and rollback: csproj registers the view class, unit tests and script; codebehind must contain both the grouped
       binding and the degradation branch; `Portal.Navigation.GroupAliases` must be registered in Web.config appSettings.
  </en>

  <zh-CN>证明边界：只证明**静态契约自洽**（源码、标记、7 套 CSS、脚本与台账一致，且关键不变量存在）；
  不证明运行期真的渲染出分组 HTML、不证明浏览器键盘交互与 Escape 回焦生效、不证明浅/深主题视觉正确、
  不证明中英截图各自渲染（由 L2 运行期证据 `Test-PortalNavigationGroupRenderEvidence.mjs` 承载），
  也不证明任何授权判定正确（由 `PortalNavigationGroupViewTests` 单测与既有 gate 门禁承载）。</zh-CN>
  <en>Proof boundary: proves only **static contract consistency** (source, markup, seven theme CSS files, script and
  ledgers agree and key invariants exist); it does not prove grouped HTML is rendered at runtime, that keyboard interaction
  and Escape focus return work in a browser, that light/dark themes look correct, or that Chinese/English screenshots are
  rendered separately (covered by L2 runtime evidence `Test-PortalNavigationGroupRenderEvidence.mjs`), nor that any
  authorization decision is correct (covered by `PortalNavigationGroupViewTests` and existing gate gates).</en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalNavigationGroupRender.ps1'  # 仓库根目录执行；exit 0 全过，exit 1 任一失败</zh-CN>
  <en>& 'dev\scripts\Test-PortalNavigationGroupRender.ps1'    # run from the repository root; exit 0 pass, exit 1 any failure</en>
</lang>
.NOTES
<lang>
  <zh-CN>有效性按项目惯例由四步注入证明（基线绿 → 注入红 → 还原绿）验证，夹具见
  `dev/scripts/gate-injections/L0-navigation-group-render.json`。</zh-CN>
  <en>Effectiveness is proven by the project's four-step injection (baseline green → injected red → restored green),
  with fixtures in `dev/scripts/gate-injections/L0-navigation-group-render.json`.</en>
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

# <lang>
#   <zh-CN>剥离 XML 文档注释、`/* */` 块与整行 `//` 注释：按项目「注释提及不计」口径，
#   只校验真正的代码文本，避免把说明性引用误判成调用。</zh-CN>
#   <en>Strip XML doc comments, `/* */` blocks and whole-line `//` comments: per the project's "comment mentions do not
#   count" convention, only real code text is checked so explanatory references are not mistaken for calls.</en>
# </lang>
function Get-CodeText([string] $text) {
    $code = [regex]::Replace($text, '(?s)///.*?$', '', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $code = [regex]::Replace($code, '(?s)/\*.*?\*/', '')
    $code = [regex]::Replace($code, '(?m)^\s*//.*$', '')
    return $code
}

try {
    # ------------------------------------------------------------------
    # R1：视图层 A 级边界与关键不变量
    # ------------------------------------------------------------------
    $viewText = Read-RepoText 'src/Portal.Components/PortalNavigationGroupView.cs'
    $viewCode = Get-CodeText $viewText

    # <lang>
    #   <zh-CN>授权符号清单与 P290 契约门禁同一口径；分组视图若调用任一符号，即等于把分组变成授权依据。</zh-CN>
    #   <en>The authorization symbol list matches the P290 contract gate; if the view calls any of them, grouping has become an authorization basis.</en>
    # </lang>
    $forbiddenAuth = @('IsInRoles', 'EnsurePermission', 'AccessRoles', 'PortalRoleNames', 'PortalPermissionKeys', 'HasPermission')
    $authHits = @($forbiddenAuth | Where-Object { $viewCode -match [regex]::Escape($_) })
    Assert-Check ($authHits.Count -eq 0) 'R1.NoAuthorizationSymbols' ('Group view code references authorization symbols: ' + ($authHits -join ', '))

    Assert-Check ($viewCode -match 'UngroupedTabs') 'R1.UngroupedFlatCollection' 'The view must expose an ungrouped flat collection so no tab can be hidden.'
    Assert-Check ($viewCode -match 'DisplayIndex') 'R1.DisplayIndexInvariant' 'The view must preserve the final-collection display index used by tabindex.'
    Assert-Check ($viewCode -match 'Degraded') 'R1.DegradedFlag' 'The view must expose a degradation flag for invalid configuration.'
    Assert-Check ($viewCode -match 'EmptySectionCount') 'R1.EmptyGroupEvidence' 'Empty groups must be counted as evidence rather than silently discarded.'
    Assert-Check ($viewCode -match 'BuildGroupedView') 'R1.ConsumesVocabularyApi' 'The view must consume the closed-vocabulary API instead of re-parsing configuration.'

    # <lang>
    #   <zh-CN>父子表结构字段是 Q1 裁定 ③ 明确排除的；视图层出现即说明有人偷偷改了数据模型。</zh-CN>
    #   <en>Parent/child schema fields are explicitly excluded by Q1 ruling ③; their presence means the data model was quietly changed.</en>
    # </lang>
    $schemaHits = @('ParentTabId', 'ParentTabKey', 'ChildTabs', 'DepthLevel') | Where-Object { $viewCode -match [regex]::Escape($_) }
    Assert-Check (@($schemaHits).Count -eq 0) 'R1.NoParentChildSchema' ('Group view must not introduce parent/child tab schema fields: ' + (@($schemaHits) -join ', '))

    # ------------------------------------------------------------------
    # R2：标记层 disclosure 契约
    # ------------------------------------------------------------------
    $bannerText = Read-RepoText 'src/Portal/DesktopPortalBanner.ascx'

    # <lang>
    #   <zh-CN>先剥离 `<%-- ... --%>` ASPX 说明注释：注释会正当引用 `aria-expanded="true"`、
    #   `menu` role 与 `onclick` 等词，若用它判定会产生「注释提及即通过」的假绿。
    #   按项目「注释提及不计」口径，R2 的结构断言一律只基于真实标记文本。</zh-CN>
    #   <en>Strip `<%-- ... --%>` ASPX explanatory comments first: legitimate documentation mentions
    #   `aria-expanded="true"`, `menu` roles and `onclick`, and counting those would create a false green where a
    #   comment mention alone passes a check. Per the project's "comment mentions do not count" convention, all R2
    #   structural assertions use real markup text only.</en>
    # </lang>
    $bannerMarkup = [regex]::Replace($bannerText, '(?s)<%--.*?--%>', '')

    Assert-Check ($bannerMarkup -match '(?s)<button[^>]*type="button"[^>]*class="portal-nav-group-toggle"') 'R2.NativeDisclosureButton' 'Group headers must be native button elements, not links or div/span with inline handlers.'
    Assert-Check ($bannerMarkup -match '(?s)<button[^>]*aria-expanded="true"[^>]*aria-controls=') 'R2.DefaultExpanded' 'Groups must render expanded by default so all tabs stay reachable without JavaScript.'
    Assert-Check ($bannerMarkup -match 'aria-controls=') 'R2.AriaControls' 'Each disclosure button must target its group container with aria-controls.'
    Assert-Check ($bannerMarkup -notmatch 'role\s*=\s*"(menu|menubar)"') 'R2.NoMenuRole' 'Site navigation must not use menu/menubar roles (W3C WAI-ARIA APG).'
    Assert-Check ($bannerMarkup -notmatch '(?i)onclick\s*=') 'R2.NoInlineOnclick' 'No inline onclick handlers are allowed; interaction is bound by the external script.'
    Assert-Check ($bannerText -match 'portal-tab-selected') 'R2.SelectedNonLink' 'The selected tab must keep the non-link selected class.'
    Assert-Check ($bannerText -match 'portal-tab-disabled') 'R2.DisabledState' 'The gate-blocked disabled state must remain available.'
    Assert-Check ($bannerText -match 'ID="Tabs"') 'R2.FlatRollbackDataList' 'The original flat DataList must remain as the degradation rollback point.'
    Assert-Check ($bannerText -match 'ID="UngroupedTabs"') 'R2.UngroupedRepeater' 'Ungrouped tabs must have a dedicated flat repeater.'
    Assert-Check ($bannerText -match 'PortalNavigationGroups\.js') 'R2.ExternalScriptReference' 'The navigation enhancement script must be referenced externally.'

    # <lang>
    #   <zh-CN>脚本必须以 `src=` 外链而非内联 `<script>` 代码块引入；内联脚本会扩大注入面并违反 a11y/模块脚本策略。</zh-CN>
    #   <en>The script must be referenced with `src=` rather than an inline `<script>` body; inline script enlarges the injection surface and violates a11y / module-script policy.</en>
    # </lang>
    $scriptTag = [regex]::Match($bannerText, '(?s)<script[^>]*>.*?</script>')
    Assert-Check ($scriptTag.Success -and $scriptTag.Value -match 'src=') 'R2.ScriptIsExternalOnly' 'Only an external script reference is allowed; no inline script body.'

    # ------------------------------------------------------------------
    # R3：7 套皮肤作用域规则
    # ------------------------------------------------------------------
    # <lang>
    #   <zh-CN>Q4 裁定统一 7 套：6 正式 + Default 回退；ThemeProbe 为诊断探针明确排除。
    #   主题名规范化由 `PortalThemeResolver.NormalizeCssSegment` 决定，这里按小写主题名推导作用域前缀，不硬编码规则内容。</zh-CN>
    #   <en>Q4 ruling standardizes on seven themes: six official plus the Default fallback; ThemeProbe is explicitly excluded
    #   as a diagnostic probe. Theme names are normalized by `PortalThemeResolver.NormalizeCssSegment`, so scope prefixes are
    #   derived from lowercased theme names here rather than hard-coding rule bodies.</en>
    # </lang>
    $themes = @('Default', 'EnterpriseLight', 'EnterpriseDark', 'OaLight', 'OaDark', 'StateClassicLight', 'StateClassicDark')
    $requiredSelectors = @('.portal-nav-group-toggle', '.portal-nav-group-hidden', '.portal-nav-group-tabs', '.portal-nav-group-caret', '.portal-nav-group-count', '.portal-nav-group {')
    foreach ($theme in $themes) {
        $cssPath = 'src/Portal/App_Themes/' + $theme + '/Default.css'
        $css = Read-RepoText $cssPath
        $scope = 'body.portal-theme-' + $theme.ToLowerInvariant()

        $scopedRuleCount = ([regex]::Matches($css, [regex]::Escape($scope) + ' \.portal-nav-group')).Count
        Assert-Check ($scopedRuleCount -ge 8) ('R3.' + $theme + '.RuleCount') ('Expected at least 8 scoped group rules, found ' + $scopedRuleCount + '.')

        $missing = @($requiredSelectors | Where-Object { $css -notmatch [regex]::Escape($scope + ' ' + $_) })
        Assert-Check ($missing.Count -eq 0) ('R3.' + $theme + '.KeySelectors') ('Missing scoped selectors: ' + ($missing -join ', '))

        # <lang>
        #   <zh-CN>焦点样式属于 a11y 静态基线 A3；分组按钮新增交互后，每套皮肤都必须给出可见焦点。</zh-CN>
        #   <en>Focus styling belongs to a11y static baseline A3; after adding the disclosure interaction every theme must provide a visible focus indicator.</en>
        # </lang>
        Assert-Check ($css -match [regex]::Escape($scope + ' .portal-nav-group-toggle:focus')) ('R3.' + $theme + '.FocusStyle') 'The group toggle needs a scoped :focus rule in this theme.'
        Assert-Check ($css -match '(?s)' + [regex]::Escape($scope + ' .portal-nav-group-hidden') + '[^}]*display:\s*none') ('R3.' + $theme + '.HiddenDisplayNone') 'Collapsed groups must use display:none so links leave the keyboard Tab sequence.'
    }

    # ------------------------------------------------------------------
    # R4：渐进增强脚本
    # ------------------------------------------------------------------
    $jsText = Read-RepoText 'src/Portal/Scripts/Navigation/PortalNavigationGroups.js'
    $jsCode = Get-CodeText $jsText

    Assert-Check ($jsCode -notmatch 'classList') 'R4.NoClassList' 'IE9 has no classList; className whole-word helpers must be used.'
    Assert-Check ($jsCode -notmatch '=>') 'R4.NoArrowFunctions' 'ES6 arrow functions are not supported by IE9.'
    Assert-Check ($jsCode -notmatch '(?m)^\s*(const|let)\s+') 'R4.NoConstLet' 'ES6 const/let are not supported by IE9; use var.'
    Assert-Check ($jsCode -match 'attachEvent') 'R4.IeEventFallback' 'A legacy attachEvent path must exist alongside addEventListener.'
    Assert-Check ($jsCode -match 'keyCode === 27') 'R4.EscapeKey' 'Escape must be handled for close-and-return-focus.'
    Assert-Check ($jsCode -match '\.focus\(\)') 'R4.FocusReturn' 'Closing with Escape must return focus to the disclosure button.'
    Assert-Check ($jsCode -match 'setAttribute\(.aria-expanded.') 'R4.AriaSync' 'aria-expanded must be synchronized on toggle.'
    Assert-Check ($jsText -match '(?i)progressive.?enhancement|渐进增强') 'R4.ProgressiveEnhancementDoc' 'The script must document that tabs stay reachable without it.'
    Assert-Check ($jsCode -match 'data-nav-group-bound') 'R4.IdempotentBinding' 'Repeated initialization must not double-bind click handlers.'

    # ------------------------------------------------------------------
    # R5：接线、降级回滚与配置登记
    # ------------------------------------------------------------------
    $componentsProj = Read-RepoText 'src/Portal.Components/Portal.Components.csproj'
    $portalProj = Read-RepoText 'src/Portal/Portal.csproj'
    $testsProj = Read-RepoText 'src/Portal.Tests/Portal.Tests.csproj'
    $bannerCode = Read-RepoText 'src/Portal/DesktopPortalBanner.ascx.cs'
    $bannerCodeOnly = Get-CodeText $bannerCode
    $webConfig = Read-RepoText 'src/Portal/Web.config'

    Assert-Check ($componentsProj -match 'PortalNavigationGroupView\.cs') 'R5.ViewClassRegistered' 'The group view class must be compiled into Portal.Components.'
    Assert-Check ($testsProj -match 'PortalNavigationGroupViewTests\.cs') 'R5.ViewTestsRegistered' 'The group view unit tests must be compiled into Portal.Tests.'
    Assert-Check ($portalProj -match 'Scripts\\Navigation\\PortalNavigationGroups\.js') 'R5.ScriptRegistered' 'The enhancement script must be a Web project Content item.'
    Assert-Check ($portalProj -match 'DesktopPortalBanner\.ascx\.cs') 'R5.BannerCodeBehindRegistered' 'The banner codebehind must remain compiled.'

    Assert-Check ($bannerCodeOnly -match 'PortalNavigationGroupView\.Build') 'R5.BuildCall' 'Codebehind must call the grouped-view builder.'
    Assert-Check ($bannerCodeOnly -match 'navigationView\.Degraded') 'R5.DegradedBranch' 'Codebehind must branch on the degradation flag.'
    Assert-Check ($bannerCodeOnly -match 'Tabs\.Visible = true') 'R5.FlatRollbackVisible' 'The degradation branch must reveal the flat DataList.'
    Assert-Check ($bannerCodeOnly -match 'GroupedNavigation\.Visible = false') 'R5.GroupedHiddenOnDegrade' 'The degradation branch must hide the grouped container.'
    Assert-Check ($bannerCodeOnly -match 'UngroupedTabs\.DataSource') 'R5.UngroupedBound' 'Ungrouped tabs must be bound so none is hidden.'
    Assert-Check ($bannerCodeOnly -match 'PortalNavigationSettings\.GroupAliases') 'R5.ConfigurationRead' 'Codebehind must read the registered alias configuration.'
    Assert-Check ($bannerCodeOnly -notmatch 'PortalNavigationGroupView[^\r\n]*IsInRoles') 'R5.NoAuthInRenderLayer' 'The render layer must not combine grouping with a role check.'

    Assert-Check ($webConfig -match 'Portal\.Navigation\.GroupAliases') 'R5.WebConfigKey' 'The alias override key must be registered in Web.config appSettings.'

    # <lang>
    #   <zh-CN>分组显示名走 P290 的中英列，不应新增 `Portal.Navigation.*` resx 资源键；
    #   这是按调研先行对原型设计稿「资源键」口径的据实纠正，门禁把它固定下来。</zh-CN>
    #   <en>Group labels use P290's Chinese/English columns; no new `Portal.Navigation.*` resx resource keys should be
    #   introduced. This is the evidence-based correction of the prototype's "resource key" wording, fixed by this gate.</en>
    # </lang>
    $langResx = Read-RepoText 'src/Portal/App_GlobalResources/lang.resx'
    Assert-Check ($langResx -notmatch 'name="Portal\.Navigation\.Group') 'R5.NoNewResxKeys' 'Group labels must come from the vocabulary columns, not new resx keys.'
}
catch {
    # <lang>
    #   <zh-CN>任何读取/解析异常都视为契约无法证明，一律判红，不做静默降级。</zh-CN>
    #   <en>Any read/parse exception means the contract cannot be proven; fail instead of silently degrading.</en>
    # </lang>
    $failures.Add('R0.Precondition — ' + $_.Exception.Message)
    Write-Host ('  [Fail] R0.Precondition — ' + $_.Exception.Message) -ForegroundColor Red
}

Write-Host ''
Write-Host '=== 主导航两级分组渲染契约门禁 ==='
foreach ($check in $checks) { Write-Host ('  ' + $check) }
Write-Host ("断言 {0} 项｜失败 {1} 项" -f ($checks.Count + $failures.Count), $failures.Count)

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    Write-Host 'RESULT: Fail' -ForegroundColor Red
    exit 1
}

Write-Host 'RESULT: Pass' -ForegroundColor Green
exit 0
