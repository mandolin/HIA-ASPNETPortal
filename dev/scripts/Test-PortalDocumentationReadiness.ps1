<#
.SYNOPSIS
<lang>
  <zh-CN>检查门户文档工具链的可交接 readiness 契约。</zh-CN>
  <en>Checks the Portal documentation toolchain readiness contract.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>本脚本只读取仓库文件、Git 索引和可选的 HIA-Documentation-Sys 通知目录，检查公开文档、XML 文档、JSDoc/DotNetDoc pilot、生成目录边界、coverage 分层和通知读取机制是否处于可交接状态。它不改写源码注释、不执行 npm、不构建解决方案、不生成文档、不复制通知，也不访问数据库或网络。</zh-CN>
  <en>This script reads repository files, the Git index, and the optional HIA-Documentation-Sys notification directory to check whether public docs, XML docs, JSDoc/DotNetDoc pilots, generated-output boundaries, coverage tiers, and notification pull mechanics are ready for handoff. It never rewrites comments, runs npm, builds the solution, generates docs, copies notifications, or accesses databases or the network.</en>
</lang>

.PARAMETER HiaDocumentationRoot
<lang>
  <zh-CN>可选的 HIA-Documentation-Sys 仓库根目录；省略时使用同级仓库路径。</zh-CN>
  <en>Optional HIA-Documentation-Sys repository root; when omitted, use the sibling repository path.</en>
</lang>

.PARAMETER OutputJson
<lang>
  <zh-CN>可选的 readiness JSON 输出路径；指定后只写入调用方明确提供的文件。</zh-CN>
  <en>Optional readiness JSON output path; when supplied, write only to the caller-provided file.</en>
</lang>

.PARAMETER FailOnWarning
<lang>
  <zh-CN>将 Warning 结果升级为失败退出；不改变只读检查本身。</zh-CN>
  <en>Promote Warning results to a failing exit code without changing the read-only checks.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string]$HiaDocumentationRoot,

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>仓库根目录从脚本路径解析，Git 索引读取失败时立即停止，避免 readiness 结果缺少完整输入。</zh-CN>
#   <en>Resolve the repository root from the script path and stop when the Git index cannot be read, avoiding a readiness result with incomplete inputs.</en>
# </lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'
$trackedFiles = @(& git -C $repoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法检查文档化 readiness。'
}

if ([string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    # <lang>
    #   <zh-CN>默认只定位同级 HIA-Documentation-Sys 通知源；调用方可显式指定其他只读工作副本。</zh-CN>
    #   <en>Default to the sibling HIA-Documentation-Sys notification source while allowing an explicit alternate read-only worktree.</en>
    # </lang>
    $HiaDocumentationRoot = Join-Path (Split-Path -Parent $repoRoot) 'HIA-Documentation-Sys'
}

# <lang>
#   <zh-CN>写 JSON 的 helper 只用于调用方指定的结果文件，并固定 UTF-8 无 BOM，避免工具版本差异改变证据编码。</zh-CN>
#   <en>The JSON helper writes only to the caller-specified result file and fixes UTF-8 without BOM so tool-version differences cannot change evidence encoding.</en>
# </lang>
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

# <lang>
#   <zh-CN>追加一条结构化 readiness 检查并输出低敏摘要；Evidence 只携带调用方提供的路径或说明。</zh-CN>
#   <en>Append one structured readiness check and print a low-sensitivity summary; Evidence carries only caller-supplied paths or descriptions.</en>
# </lang>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 读取文本输入，不在读取阶段改写文件或生成副本。</zh-CN>
#   <en>Read text inputs as UTF-8 without BOM and do not rewrite files or create copies while reading.</en>
# </lang>
function Get-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    return [System.IO.File]::ReadAllText($LiteralPath, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>把仓库相对路径规范化为当前仓库绝对路径，避免检查越过既定根目录。</zh-CN>
#   <en>Resolve a repository-relative path under the current repository root so checks cannot escape the declared boundary.</en>
# </lang>
function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ($RelativePath -replace '/', '\')
}

# <lang>
#   <zh-CN>只依据启动时读取的 Git 索引判断路径是否已追踪，不把未跟踪草稿或生成物纳入 readiness。</zh-CN>
#   <en>Use only the Git index captured at startup to decide whether a path is tracked, excluding untracked drafts and generated artifacts from readiness.</en>
# </lang>
function Test-TrackedPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return $trackedFiles -contains ($RelativePath -replace '\\', '/')
}

# <lang>
#   <zh-CN>在受控仓库文件中执行不区分大小写的文本契约检查；缺失文件直接产生失败结果。</zh-CN>
#   <en>Evaluate a case-insensitive text contract within a controlled repository file; a missing file yields a failed result.</en>
# </lang>
function Test-TextContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $path = Get-RepoPath -RelativePath $RelativePath
    return (Test-Path -LiteralPath $path -PathType Leaf) -and
        [regex]::IsMatch((Get-Utf8Text -LiteralPath $path), $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

# <lang>
#   <zh-CN>统计指定仓库相对目录下的 Git 已追踪路径，用于生成物公开边界门禁。</zh-CN>
#   <en>Count Git-tracked paths under a repository-relative directory for generated-output publication boundaries.</en>
# </lang>
function Get-TrackedCountUnder {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $normalized = ($RelativePath -replace '\\', '/').TrimEnd('/')
    $prefix = $normalized + '/'
    return @($trackedFiles | Where-Object {
            $_.Equals($normalized, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
        }).Count
}

# <lang>
#   <zh-CN>从此处开始执行只读 readiness 编排；后续检查不得运行 npm、构建、通知复制或源码改写。</zh-CN>
#   <en>Begin the read-only readiness orchestration; subsequent checks must not run npm, build, copy notifications, or rewrite source.</en>
# </lang>
Write-Host 'MODE: read-only documentation readiness check.'

# <lang>
#   <zh-CN>声明交接所需的文档脚本集合，缺失或未追踪任一项都会使 DOC-SCRIPTS 失败。</zh-CN>
#   <en>Declare the scripts required for handoff; any missing or untracked item fails DOC-SCRIPTS.</en>
# </lang>
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

# <lang>
#   <zh-CN>将历史或生成目录作为不可公开追踪的边界逐项检查，任何已追踪文件都会失败。</zh-CN>
#   <en>Check historical or generated directories as non-public tracking boundaries; any tracked file fails the gate.</en>
# </lang>
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

# <lang>
#   <zh-CN>通知源只做可选只读探测；缺失时记录 Pending，不复制通知也不把外部内容写入本仓库。</zh-CN>
#   <en>Probe the notification source as optional read-only input; record Pending when absent, without copying notices or writing external content into this repository.</en>
# </lang>
$notifyRoot = Join-Path $HiaDocumentationRoot 'work-zone\notify'
if (-not (Test-Path -LiteralPath $notifyRoot -PathType Container)) {
    Add-DocumentationCheck -Severity Pending -Code 'DOC-HIA-NOTIFY-SOURCE' -Message 'HIA-Documentation-Sys notify source is not available on this machine.' -Evidence $notifyRoot
}
else {
    $notifications = @(Get-ChildItem -LiteralPath $notifyRoot -Recurse -File -Filter '*.md' | Where-Object { $_.Name -ne 'README.md' })
    $severity = if ($notifications.Count -gt 0) { 'Pass' } else { 'Warning' }
    Add-DocumentationCheck -Severity $severity -Code 'DOC-HIA-NOTIFY-SOURCE' -Message ('HIA-Documentation-Sys notify source is readable. Notifications={0}; ContentCopied=False' -f $notifications.Count) -Evidence $notifyRoot
}

# <lang>
#   <zh-CN>显式记录本次 readiness 的无副作用契约，避免调用方把门禁误解为构建或生成流程。</zh-CN>
#   <en>Record the readiness no-side-effect contract explicitly so callers do not mistake the gate for a build or generation flow.</en>
# </lang>
Add-DocumentationCheck -Severity Pass -Code 'DOC-NO-CODE-REWRITE' -Message 'Readiness completed without rewriting source comments, generating docs, copying notifications, or changing dependencies.'

# <lang>
#   <zh-CN>汇总所有检查及失败/警告/Pending 计数；只有调用方显式指定 OutputJson 时才写文件。</zh-CN>
#   <en>Summarize all checks and failure/warning/pending counts; write a file only when OutputJson is explicit.</en>
# </lang>
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
    # <lang>
    #   <zh-CN>输出路径由调用方控制，写入保持 UTF-8 无 BOM，并不复制通知正文。</zh-CN>
    #   <en>The caller controls the output path; writes remain UTF-8 without BOM and never copy notice bodies.</en>
    # </lang>
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

# <lang>
#   <zh-CN>失败检查始终返回失败；只有 -FailOnWarning 才把警告升级为失败，不改变检查内容。</zh-CN>
#   <en>Failed checks always return failure; only -FailOnWarning promotes warnings, without changing the checks themselves.</en>
# </lang>
if ($summary.FailedChecks -gt 0 -or ($FailOnWarning -and $summary.WarningChecks -gt 0)) {
    exit 1
}

exit 0
