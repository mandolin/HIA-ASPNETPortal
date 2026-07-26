<#
.SYNOPSIS
.LANG en
Runs the HIA DotNetDoc pilot for the portal codebase.

.LANG zh-CN
为门户代码库运行 HIA DotNetDoc 试点生成流程。

.DESCRIPTION
.LANG en
Optionally builds XML documentation, restores the isolated DotNetDoc Node
workspace, and runs the selected DotNetDoc npm script. The tool output is written
under temp/documentation and remains separate from the Web Forms runtime,
Visual Studio Task Runner, and legacy Gulp pipeline.

.LANG zh-CN
可选构建 XML 文档、还原隔离的 DotNetDoc Node 工作区，并运行选定的 DotNetDoc npm 脚本。
工具输出写入 temp/documentation，与 Web Forms 运行时、Visual Studio Task Runner 和旧 Gulp
流水线保持隔离。

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
    throw "缺少锁定依赖文件：$packageLockPath"
}

if (-not $SkipXmlBuild) {
    $xmlDocumentationScript = Join-Path $PSScriptRoot 'Test-PortalXmlDocumentation.ps1'
    & $xmlDocumentationScript -Build
}

if (Test-Path -LiteralPath $outputDirectory -PathType Container) {
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
}

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
        throw "ApiOnly 与 SourceProbe 不能同时使用。"
    }

    $buildScriptName = if ($SourceProbe) {
        'docs:build:source-probe'
    } elseif ($ApiOnly) {
        'docs:api-only'
    } else {
        'docs'
    }
    & npm run $buildScriptName
    if ($LASTEXITCODE -ne 0) {
        throw "HIA DotNetDoc pilot 失败，退出代码：$LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
