<#
.SYNOPSIS
    Checks the Portal documentation toolchain readiness contract.

.DESCRIPTION
    中文：本脚本只读取仓库文件、Git 索引和可选的 HIA-Documentation-Sys 通知目录，检查公开文档、
    XML 文档、JSDoc/DotNetDoc pilot、生成目录边界、coverage 分层和通知读取机制是否处于可交接状态。
    它不改写源码注释、不执行 npm、不构建解决方案、不生成文档、不复制通知，也不访问数据库或网络。
    English: This script reads repository files, the Git index, and the optional HIA-Documentation-Sys notification
    directory to check whether public docs, XML docs, JSDoc/DotNetDoc pilots, generated-output boundaries, coverage tiers,
    and notification pull mechanics are ready for handoff. It never rewrites comments, runs npm, builds the solution,
    generates docs, copies notifications, or accesses databases or the network.
#>
[CmdletBinding()]
param(
    [string]$HiaDocumentationRoot,

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'
$trackedFiles = @(& git -C $repoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法检查文档化 readiness。'
}

if ([string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    $HiaDocumentationRoot = Join-Path (Split-Path -Parent $repoRoot) 'HIA-Documentation-Sys'
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Add-DocumentationCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'Pending')]
        [string]$Severity,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject][ordered]@{
            Severity = $Severity
            Code = $Code
            Message = $Message
            Evidence = $Evidence
        })

    Write-Host ('[{0}] {1}: {2}' -f $Severity.ToUpperInvariant(), $Code, $Message)
    if (-not [string]::IsNullOrWhiteSpace($Evidence)) {
        Write-Host ('       {0}' -f $Evidence)
    }
}

function Get-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    return [System.IO.File]::ReadAllText($LiteralPath, [System.Text.UTF8Encoding]::new($false))
}

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ($RelativePath -replace '/', '\')
}

function Test-TrackedPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return $trackedFiles -contains ($RelativePath -replace '\\', '/')
}

function Test-TextContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $path = Get-RepoPath -RelativePath $RelativePath
    return (Test-Path -LiteralPath $path -PathType Leaf) -and
        [regex]::IsMatch((Get-Utf8Text -LiteralPath $path), $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Get-TrackedCountUnder {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $normalized = ($RelativePath -replace '\\', '/').TrimEnd('/')
    $prefix = $normalized + '/'
    return @($trackedFiles | Where-Object {
            $_.Equals($normalized, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
        }).Count
}

Write-Host 'MODE: read-only documentation readiness check.'

$requiredScripts = @(
    'dev/scripts/Get-PortalDocumentationBaseline.ps1',
    'dev/scripts/Test-PortalXmlDocumentation.ps1',
    'dev/scripts/Build-PortalJsdocPilot.ps1',
    'dev/scripts/Build-PortalDotNetDocPilot.ps1',
    'dev/scripts/Get-HiaDocumentationNotifications.ps1',
    'dev/scripts/Test-PortalPublicDocumentation.ps1'
)
$missingScripts = @($requiredScripts | Where-Object {
        -not (Test-Path -LiteralPath (Get-RepoPath -RelativePath $_) -PathType Leaf) -or -not (Test-TrackedPath -RelativePath $_)
    })
if ($missingScripts.Count -eq 0) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-SCRIPTS' -Message 'Documentation baseline, XML, JSDoc, DotNetDoc, notification and public-doc scripts are present and tracked.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-SCRIPTS' -Message 'Required documentation scripts are missing or untracked.' -Evidence ($missingScripts -join '; ')
}

$docsGuideReady =
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Required') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Recommended') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Deferred') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Get-HiaDocumentationNotifications\.ps1') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'src/Documentation/') -and
    (Test-TextContains -RelativePath 'docs/README.md' -Pattern 'documentation-artifacts-guide\.md')
if ($docsGuideReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-PUBLIC-GUIDE' -Message 'Public documentation artifacts guide covers tiers, HIA notifications and generated-output boundaries.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-PUBLIC-GUIDE' -Message 'Public documentation artifacts guide needs P13.3 contract updates.'
}

# <lang>
# <zh-CN>验证隔离的 JSDoc pilot：仅允许经审查的源码白名单，并且输出必须保持在被忽略的临时目录。</zh-CN>
# <en>Validate the isolated JSDoc pilot: it permits only the reviewed source allowlist and keeps output in the ignored temporary directory.</en>
# </lang>
# <lang>
# <zh-CN>定位 JSDoc 工具项目的依赖清单；缺失时不得尝试解析或安装依赖。</zh-CN>
# <en>Locate the JSDoc tool project's dependency manifest; a missing manifest must not trigger parsing or dependency installation.</en>
# </lang>
$jsdocPackagePath = Get-RepoPath -RelativePath 'dev/documentation/jsdoc/package.json'
# <lang>
# <zh-CN>定位受版本控制的 JSDoc 输入与输出契约配置。</zh-CN>
# <en>Locate the version-controlled JSDoc input and output contract configuration.</en>
# </lang>
$jsdocConfigPath = Get-RepoPath -RelativePath 'dev/documentation/jsdoc/jsdoc.conf.json'
# <lang>
# <zh-CN>定位锁文件，以确认该 pilot 不依赖未锁定的瞬态依赖树。</zh-CN>
# <en>Locate the lockfile so the pilot does not rely on an unlocked transient dependency tree.</en>
# </lang>
$jsdocPackageLockPath = Get-RepoPath -RelativePath 'dev/documentation/jsdoc/package-lock.json'
# <lang>
# <zh-CN>声明当前经审查的两个 JSDoc 输入；任何增删均须同步更新门禁和公开文档。</zh-CN>
# <en>Declare the two currently reviewed JSDoc inputs; any addition or removal must update this gate and the public documentation together.</en>
# </lang>
$expectedJsdocInputs = @(
    '../../../src/Portal/gulpfile.js',
    '../../../src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js'
)
# <lang>
# <zh-CN>以失败为默认值，使缺失文件或不满足契约时产生明确的失败检查而非误报通过。</zh-CN>
# <en>Default to failure so missing files or an unmet contract produce an explicit failed check rather than a false pass.</en>
# </lang>
$jsdocReady = $false
# <lang>
# <zh-CN>只有清单、配置和锁文件均存在时才解析 JSON；这样损坏或不完整的工具项目不会被视为可交接。</zh-CN>
# <en>Parse JSON only when the manifest, configuration, and lockfile all exist, so a damaged or incomplete tool project is never considered handoff-ready.</en>
# </lang>
if ((Test-Path -LiteralPath $jsdocPackagePath -PathType Leaf) -and
    (Test-Path -LiteralPath $jsdocConfigPath -PathType Leaf) -and
    (Test-Path -LiteralPath $jsdocPackageLockPath -PathType Leaf)) {
    # <lang>
    # <zh-CN>读取 JSDoc 依赖声明，以验证 HIA 插件和主题仍在隔离工具项目内。</zh-CN>
    # <en>Read the JSDoc dependency manifest to verify that the HIA plugin and theme remain inside the isolated tool project.</en>
    # </lang>
    $jsdocPackage = Get-Utf8Text -LiteralPath $jsdocPackagePath | ConvertFrom-Json
    # <lang>
    # <zh-CN>读取 JSDoc 配置，以比较实际白名单和受限生成目录。</zh-CN>
    # <en>Read the JSDoc configuration to compare the actual allowlist and constrained generated directories.</en>
    # </lang>
    $jsdocConfig = Get-Utf8Text -LiteralPath $jsdocConfigPath | ConvertFrom-Json
    # <lang>
    # <zh-CN>收集配置的源码输入；顺序不构成安全边界，集合内容才构成边界。</zh-CN>
    # <en>Collect configured source inputs; their contents, rather than their order, form the security boundary.</en>
    # </lang>
    $sourceIncludes = @($jsdocConfig.source.include)
    # <lang>
    # <zh-CN>比较期望与实际输入集合，以发现遗漏、未经审查的新增输入或重复项。</zh-CN>
    # <en>Compare expected and actual input sets to detect omissions, unreviewed additions, or duplicate entries.</en>
    # </lang>
    $jsdocInputDifferences = @(Compare-Object -ReferenceObject $expectedJsdocInputs -DifferenceObject $sourceIncludes)
    # <lang>
    # <zh-CN>保留 HTML 文档输出目录，用于防止生成物漂移到可提交或运行时路径。</zh-CN>
    # <en>Keep the HTML documentation output directory to prevent generated artifacts from drifting into tracked or runtime paths.</en>
    # </lang>
    $destination = [string]$jsdocConfig.opts.destination
    # <lang>
    # <zh-CN>保留 integration JSON 输出目录；其边界必须与 HTML 输出同样受控。</zh-CN>
    # <en>Keep the integration JSON output directory; its boundary must be controlled just like the HTML output.</en>
    # </lang>
    $integrationOutput = [string]$jsdocConfig.opts.hia.integration.outputFile
    # <lang>
    # <zh-CN>仅当隔离依赖、精确输入白名单和两个临时输出边界同时成立时，才允许 JSDoc pilot 通过 readiness。</zh-CN>
    # <en>Allow the JSDoc pilot to pass readiness only when isolated dependencies, the exact input allowlist, and both temporary output boundaries all hold.</en>
    # </lang>
    $jsdocReady =
        ($jsdocPackage.private -eq $true) -and
        ($null -ne $jsdocPackage.devDependencies.'@mandolin/jsdoc-plugin-hia-sys') -and
        ($null -ne $jsdocPackage.devDependencies.'@mandolin/jsdoc-theme-hia') -and
        ($sourceIncludes.Count -eq $expectedJsdocInputs.Count) -and
        ($jsdocInputDifferences.Count -eq 0) -and
        ($destination -eq '../../../temp/documentation/jsdoc') -and
        ($integrationOutput -eq '../../../temp/documentation/jsdoc/hia-integration.json')
}

if ($jsdocReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-JSDOC-PILOT' -Message 'JSDoc pilot is isolated, locked, uses two reviewed source inputs, and writes only to temp/documentation/jsdoc.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-JSDOC-PILOT' -Message 'JSDoc pilot isolation, input or output contract needs review.'
}

$xmlReady =
    (Test-TextContains -RelativePath 'dev/scripts/Test-PortalXmlDocumentation.ps1' -Pattern 'Portal\.Components\.xml') -and
    (Test-TextContains -RelativePath 'dev/scripts/Test-PortalXmlDocumentation.ps1' -Pattern '不改写.*MSBuild|must not rewrite') -and
    (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'Test-PortalXmlDocumentation.ps1')
if ($xmlReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-XML-CONTRACT' -Message '.NET XML documentation verification remains standard XML output and does not rewrite MSBuild settings.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-XML-CONTRACT' -Message '.NET XML documentation boundary needs review.'
}

# <lang>
# <zh-CN>验证隔离的 DotNetDoc pilot：清单的允许版本范围和锁文件的解析版本必须一起反映当前已审查的工具链。</zh-CN>
# <en>Validate the isolated DotNetDoc pilot: both the manifest's accepted range and the lockfile's resolved version must reflect the current reviewed toolchain.</en>
# </lang>
# <lang>
# <zh-CN>定位 DotNetDoc 工具项目清单，以验证其不与 Portal 前端依赖混用。</zh-CN>
# <en>Locate the DotNetDoc tool-project manifest to verify it remains separate from Portal frontend dependencies.</en>
# </lang>
$dotnetDocPackagePath = Get-RepoPath -RelativePath 'dev/documentation/dotnetdoc/package.json'
# <lang>
# <zh-CN>定位 DotNetDoc 锁文件，以验证本机 `npm ci` 可复现当前 runner 版本。</zh-CN>
# <en>Locate the DotNetDoc lockfile to verify that local `npm ci` can reproduce the current runner version.</en>
# </lang>
$dotnetDocPackageLockPath = Get-RepoPath -RelativePath 'dev/documentation/dotnetdoc/package-lock.json'
# <lang>
# <zh-CN>定位输出检查器；它防止生成内容丢失 source-content 审计信息。</zh-CN>
# <en>Locate the output checker; it prevents generated content from losing source-content audit information.</en>
# </lang>
$dotnetDocCheckerPath = Get-RepoPath -RelativePath 'dev/documentation/dotnetdoc/check-dotnetdoc-output.cjs'
# <lang>
# <zh-CN>定位默认配置；它定义常规 pilot 的输入和受限输出目录。</zh-CN>
# <en>Locate the default configuration; it defines the regular pilot's inputs and constrained output directory.</en>
# </lang>
$dotnetDocConfigPath = Get-RepoPath -RelativePath 'dotnetdoc.config.json'
# <lang>
# <zh-CN>定位仅 API 回退配置，以确保排障路径不会意外丢失。</zh-CN>
# <en>Locate the API-only fallback configuration so the troubleshooting path is not accidentally lost.</en>
# </lang>
$dotnetDocApiOnlyConfigPath = Get-RepoPath -RelativePath 'dotnetdoc.api-only.config.json'
# <lang>
# <zh-CN>定位 source-probe 配置，以确保研究性路径与默认门禁保持显式分离。</zh-CN>
# <en>Locate the source-probe configuration so the exploratory path remains explicitly separate from the default gate.</en>
# </lang>
$dotnetDocSourceProbeConfigPath = Get-RepoPath -RelativePath 'dotnetdoc.source-probe.config.json'
# <lang>
# <zh-CN>声明清单中允许的 runner 版本范围；升级必须先审查，再同步此契约。</zh-CN>
# <en>Declare the runner version range allowed by the manifest; an upgrade must be reviewed before this contract is updated.</en>
# </lang>
$expectedDotnetDocRunnerRange = '^0.1.8'
# <lang>
# <zh-CN>声明锁文件中当前可复现的 runner 版本；它不能仅由 semver 范围隐式决定。</zh-CN>
# <en>Declare the runner version currently reproducible from the lockfile; it must not be implied by the semver range alone.</en>
# </lang>
$expectedDotnetDocRunnerLockedVersion = '0.1.8'
# <lang>
# <zh-CN>以失败为默认值，使缺失或不一致的 pilot 契约明确失败。</zh-CN>
# <en>Default to failure so a missing or inconsistent pilot contract fails explicitly.</en>
# </lang>
$dotnetDocReady = $false
# <lang>
# <zh-CN>仅在所有配置和检查器均存在时解析 JSON，避免把不完整工具项目视作可交接。</zh-CN>
# <en>Parse JSON only when every configuration and checker exists, avoiding treating an incomplete tool project as handoff-ready.</en>
# </lang>
if ((Test-Path -LiteralPath $dotnetDocPackagePath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocPackageLockPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocCheckerPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocConfigPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocApiOnlyConfigPath -PathType Leaf) -and
    (Test-Path -LiteralPath $dotnetDocSourceProbeConfigPath -PathType Leaf)) {
    # <lang>
    # <zh-CN>读取依赖清单，以校验隔离性、runner 允许范围和安全解析器覆盖项。</zh-CN>
    # <en>Read the dependency manifest to validate isolation, the allowed runner range, and the secure parser override.</en>
    # </lang>
    $dotnetDocPackage = Get-Utf8Text -LiteralPath $dotnetDocPackagePath | ConvertFrom-Json
    # <lang>
    # <zh-CN>以哈希表读取锁文件，以兼容 npm 锁文件的空根键并校验实际解析的 runner 版本。</zh-CN>
    # <en>Read the lockfile as a hash table to support npm's empty root key and validate the actually resolved runner version.</en>
    # </lang>
    $dotnetDocPackageLock = Get-Utf8Text -LiteralPath $dotnetDocPackageLockPath | ConvertFrom-Json -AsHashtable
    # <lang>
    # <zh-CN>通过哈希键读取含斜杠的 npm 路径，避免把包路径误当作普通 PowerShell 属性。</zh-CN>
    # <en>Read the npm path containing slashes through a hash key, avoiding treatment of the package path as an ordinary PowerShell property.</en>
    # </lang>
    $dotnetDocRunnerLockEntry = $dotnetDocPackageLock['packages']['node_modules/@hia-doc/dotnetdoc-runner']
    # <lang>
    # <zh-CN>将缺失锁条目规范化为空字符串，使版本比较产生 readiness 失败而不产生不透明的空引用异常。</zh-CN>
    # <en>Normalize a missing lock entry to an empty string so version comparison produces a readiness failure instead of an opaque null-reference exception.</en>
    # </lang>
    $dotnetDocRunnerLockedVersion = if ($null -eq $dotnetDocRunnerLockEntry) { '' } else { [string]$dotnetDocRunnerLockEntry['version'] }
    # <lang>
    # <zh-CN>读取默认 pilot 配置，以校验输出目录和至少一个输入仍被定义。</zh-CN>
    # <en>Read the default pilot configuration to validate that its output directory and at least one input remain defined.</en>
    # </lang>
    $dotnetDocConfig = Get-Utf8Text -LiteralPath $dotnetDocConfigPath | ConvertFrom-Json
    # <lang>
    # <zh-CN>缓存输入数量，避免在完成条件中重复展开配置数组，并明确“至少一个输入”的门禁语义。</zh-CN>
    # <en>Cache the input count to avoid repeatedly expanding the configuration array in the completion condition and to make the "at least one input" gate explicit.</en>
    # </lang>
    $dotnetDocInputCount = @($dotnetDocConfig.inputs).Count
    # <lang>
    # <zh-CN>仅当依赖清单、锁定 runner、解析器覆盖、默认输入输出及两个回退边界均完整时，才允许 DotNetDoc pilot 通过 readiness。</zh-CN>
    # <en>Allow the DotNetDoc pilot to pass readiness only when its manifest, locked runner, parser override, default inputs and outputs, and both fallback boundaries are complete.</en>
    # </lang>
    $dotnetDocReady =
        ($dotnetDocPackage.private -eq $true) -and
        ($dotnetDocPackage.devDependencies.'@hia-doc/dotnetdoc-runner' -eq $expectedDotnetDocRunnerRange) -and
        ($dotnetDocRunnerLockedVersion -eq $expectedDotnetDocRunnerLockedVersion) -and
        ($dotnetDocPackage.overrides.'fast-xml-parser' -eq '5.7.0') -and
        ([string]$dotnetDocConfig.outputDirectory -eq 'temp/documentation/dotnetdoc') -and
        ($dotnetDocInputCount -gt 0) -and
        (Test-TextContains -RelativePath 'dev/scripts/Build-PortalDotNetDocPilot.ps1' -Pattern 'temp\\documentation') -and
        (Test-TextContains -RelativePath 'dev/documentation/dotnetdoc/check-dotnetdoc-output.cjs' -Pattern 'sourcesContent') -and
        (Test-TextContains -RelativePath 'docs/documentation-artifacts-guide.md' -Pattern 'DotNetDoc pilot')
}

if ($dotnetDocReady) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-DOTNETDOC-PILOT' -Message 'DotNetDoc pilot is isolated, locked, writes only to temp/documentation/dotnetdoc, and has a source-probe fallback boundary.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-DOTNETDOC-PILOT' -Message 'DotNetDoc pilot isolation, input, output, audit override or checker contract needs review.'
}

$generatedBoundaries = @(
    'src/Documentation',
    'src/DoxyGen',
    'src/Portal.Components.Data/Documentation',
    'src/Portal/Documentation',
    'src/Portal.shfbproj'
)
$trackedGenerated = New-Object 'System.Collections.Generic.List[string]'
foreach ($boundary in $generatedBoundaries) {
    if (Get-TrackedCountUnder -RelativePath $boundary) {
        $trackedGenerated.Add($boundary)
    }
}
if ($trackedGenerated.Count -eq 0) {
    Add-DocumentationCheck -Severity Pass -Code 'DOC-GENERATED-BOUNDARY' -Message 'Known generated or historical documentation output paths are not tracked.'
}
else {
    Add-DocumentationCheck -Severity Fail -Code 'DOC-GENERATED-BOUNDARY' -Message 'Generated or historical documentation output paths are tracked.' -Evidence ($trackedGenerated -join '; ')
}

$notifyRoot = Join-Path $HiaDocumentationRoot 'work-zone\notify'
if (-not (Test-Path -LiteralPath $notifyRoot -PathType Container)) {
    Add-DocumentationCheck -Severity Pending -Code 'DOC-HIA-NOTIFY-SOURCE' -Message 'HIA-Documentation-Sys notify source is not available on this machine.' -Evidence $notifyRoot
}
else {
    $notifications = @(Get-ChildItem -LiteralPath $notifyRoot -Recurse -File -Filter '*.md' | Where-Object { $_.Name -ne 'README.md' })
    $severity = if ($notifications.Count -gt 0) { 'Pass' } else { 'Warning' }
    Add-DocumentationCheck -Severity $severity -Code 'DOC-HIA-NOTIFY-SOURCE' -Message ('HIA-Documentation-Sys notify source is readable. Notifications={0}; ContentCopied=False' -f $notifications.Count) -Evidence $notifyRoot
}

Add-DocumentationCheck -Severity Pass -Code 'DOC-NO-CODE-REWRITE' -Message 'Readiness completed without rewriting source comments, generating docs, copying notifications, or changing dependencies.'

$summary = [pscustomobject][ordered]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    HiaDocumentationRoot = $HiaDocumentationRoot
    Checks = $checks
    TotalChecks = $checks.Count
    FailedChecks = @($checks | Where-Object { $_.Severity -eq 'Fail' }).Count
    WarningChecks = @($checks | Where-Object { $_.Severity -eq 'Warning' }).Count
    PendingChecks = @($checks | Where-Object { $_.Severity -eq 'Pending' }).Count
}

$summary

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

if ($summary.FailedChecks -gt 0 -or ($FailOnWarning -and $summary.WarningChecks -gt 0)) {
    exit 1
}

exit 0
