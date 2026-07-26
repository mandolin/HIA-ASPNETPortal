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
