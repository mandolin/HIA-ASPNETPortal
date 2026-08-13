<#
.SYNOPSIS
.LANG en
Runs the HIA DotNetDoc pilot for the portal codebase.

.LANG zh-CN
为门户代码库运行 HIA DotNetDoc 试点生成流程。

.DESCRIPTION
<lang>
  <zh-CN>可选构建 XML 文档、还原隔离的 DotNetDoc Node 工作区，并运行选定的 DotNetDoc npm 脚本。工具输出写入 temp/documentation，与 Web Forms 运行时、Visual Studio Task Runner 和旧 Gulp 流水线保持隔离。</zh-CN>
  <en>Optionally builds XML documentation, restores the isolated DotNetDoc Node workspace, and runs the selected DotNetDoc npm script. Tool output is written under temp/documentation and remains separate from the Web Forms runtime, Visual Studio Task Runner, and legacy Gulp pipeline.</en>
</lang>

.PARAMETER SkipRestore
.LANG en
Skips npm ci when node_modules already exists and the caller accepts the current local dependency state.

.LANG zh-CN
当 node_modules 已存在且调用方接受当前本地依赖状态时，跳过 npm ci。

.PARAMETER SkipXmlBuild
.LANG en
Skips the XML documentation build precheck and consumes existing XML outputs.

.LANG zh-CN
跳过 XML 文档构建预检，直接使用现有 XML 输出。

.PARAMETER ApiOnly
.LANG en
Runs the API-only DotNetDoc script instead of the full documentation build.

.LANG zh-CN
运行 API-only DotNetDoc 脚本，而不是完整文档构建。

.PARAMETER SourceProbe
.LANG en
Runs the source-probe DotNetDoc script for parser and source extraction checks.

.LANG zh-CN
运行 source-probe DotNetDoc 脚本，用于解析器和源码抽取检查。
#>
[CmdletBinding()]
param(
    [switch]$SkipRestore,
    [switch]$SkipXmlBuild,
    [switch]$ApiOnly,
    [switch]$SourceProbe
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>DotNetDoc pilot 与门户运行时、Gulp 和 Visual Studio Task Runner 隔离，只消费已存在或刚构建出的文档化输入。</zh-CN>
#   <en>The DotNetDoc pilot is isolated from the portal runtime, Gulp, and Visual Studio Task Runner; it only consumes existing or freshly built documentation inputs.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolDirectory = Join-Path $repositoryRoot 'dev\documentation\dotnetdoc'
$packageLockPath = Join-Path $toolDirectory 'package-lock.json'
$nodeModulesPath = Join-Path $toolDirectory 'node_modules'
$outputDirectoryName = if ($SourceProbe) { 'dotnetdoc-source-probe' } else { 'dotnetdoc' }
$outputDirectory = Join-Path $repositoryRoot "temp\documentation\$outputDirectoryName"

if (-not (Test-Path -LiteralPath $packageLockPath -PathType Leaf)) {
    # <lang>
    #   <zh-CN>锁文件是隔离工具链可重复还原的必要前提；缺失时立即停止，不能退化为未锁定安装。</zh-CN>
    #   <en>The lock file is required for reproducible restoration of the isolated toolchain; stop immediately instead of falling back to an unlocked install.</en>
    # </lang>
    throw "缺少锁定依赖文件：$packageLockPath"
}

if (-not $SkipXmlBuild) {
    # <lang>
    #   <zh-CN>默认先通过既有 XML 文档门禁生成输入；-SkipXmlBuild 只在调用方明确接受现有输出时绕过该前置步骤。</zh-CN>
    #   <en>By default, use the existing XML documentation gate to produce inputs first; -SkipXmlBuild bypasses that prerequisite only when the caller accepts existing outputs.</en>
    # </lang>
    $xmlDocumentationScript = Join-Path $PSScriptRoot 'Test-PortalXmlDocumentation.ps1'
    & $xmlDocumentationScript -Build
}

if (Test-Path -LiteralPath $outputDirectory -PathType Container) {
    # <lang>
    #   <zh-CN>只清理本次模式对应的临时文档输出目录，避免旧产物被误读且不触及源码或前端资产。</zh-CN>
    #   <en>Remove only the temporary output directory for the selected mode so stale artifacts are not consumed without touching source files or front-end assets.</en>
    # </lang>
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
}

# <lang>
#   <zh-CN>位置切换限定在隔离工具目录，并由 finally 保证异常或成功后都恢复调用方工作目录。</zh-CN>
#   <en>Change location only within the isolated tool directory, and let finally restore the caller's working directory after success or failure.</en>
# </lang>
Push-Location $toolDirectory
try {
    # <lang>
    #   <zh-CN>依赖还原限定在 dev/documentation/dotnetdoc，不修改 Portal 前端 package 或旧 Gulp 工作流。</zh-CN>
    #   <en>Dependency restoration is limited to dev/documentation/dotnetdoc and does not modify Portal front-end packages or the legacy Gulp workflow.</en>
    # </lang>
    if (-not $SkipRestore -or -not (Test-Path -LiteralPath $nodeModulesPath -PathType Container)) {
        & npm ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci 失败，退出代码：$LASTEXITCODE"
        }
    }

    if ($ApiOnly -and $SourceProbe) {
        # <lang>
        #   <zh-CN>两个试点模式互斥，避免调用方得到无法解释的混合输出。</zh-CN>
        #   <en>The two pilot modes are mutually exclusive so callers cannot receive ambiguous mixed output.</en>
        # </lang>
        throw "ApiOnly 与 SourceProbe 不能同时使用。"
    }

    # <lang>
    #   <zh-CN>按调用开关选择唯一的 npm 脚本；未指定时运行完整文档流程。</zh-CN>
    #   <en>Select exactly one npm script from the caller's switches; run the full documentation flow when no special mode is requested.</en>
    # </lang>
    $buildScriptName = if ($SourceProbe) {
        'docs:build:source-probe'
    } elseif ($ApiOnly) {
        'docs:api-only'
    } else {
        'docs'
    }
    # <lang>
    #   <zh-CN>执行隔离的文档构建并把非零退出码转换为明确异常，确保上层门禁不会误判成功。</zh-CN>
    #   <en>Run the isolated documentation build and turn a non-zero exit code into an explicit exception so higher-level gates cannot mistake failure for success.</en>
    # </lang>
    & npm run $buildScriptName
    if ($LASTEXITCODE -ne 0) {
        throw "HIA DotNetDoc pilot 失败，退出代码：$LASTEXITCODE"
    }
}
finally {
    # <lang>
    #   <zh-CN>无论 npm 或预检如何结束，都恢复进入脚本前的工作目录。</zh-CN>
    #   <en>Always restore the working directory that was active before the script entered the tool workspace.</en>
    # </lang>
    Pop-Location
}
