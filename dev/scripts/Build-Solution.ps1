<#
.SYNOPSIS
.LANG en
Builds the portal Visual Studio solution.

.LANG zh-CN
构建门户 Visual Studio 解决方案。

.DESCRIPTION
.LANG en
Locates the repository solution, resolves MSBuild through the local helper, and
runs a normal Build target. The script is intentionally thin so VS, VSCode tasks,
and CI-style local checks share the same command without changing project files.

.LANG zh-CN
定位仓库解决方案，通过本地辅助脚本解析 MSBuild，然后执行标准 Build 目标。本脚本刻意保持轻量，
让 Visual Studio、VSCode 任务和本地类 CI 检查共用同一入口，同时不改写项目文件。

.PARAMETER Configuration
.LANG en
Build configuration to pass to MSBuild, normally Debug or Release.

.LANG zh-CN
传给 MSBuild 的构建配置，通常为 Debug 或 Release。

.PARAMETER Platform
.LANG en
MSBuild platform value. The legacy solution normally uses Any CPU.

.LANG zh-CN
MSBuild 平台值。旧解决方案通常使用 Any CPU。
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$Platform = 'Any CPU'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$solutionPath = Join-Path $repoRoot 'src\master.sln'
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution file not found: $solutionPath"
}

$msbuild = & $findMsBuild

Write-Host "Building $solutionPath"
Write-Host "MSBuild: $msbuild"
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"

& $msbuild $solutionPath /m /t:Build "/p:Configuration=$Configuration" "/p:Platform=$Platform" /v:minimal /nologo
exit $LASTEXITCODE
