<#
.SYNOPSIS
.LANG en
Restores NuGet packages for the portal solution.

.LANG zh-CN
还原门户解决方案的 NuGet 包。

.DESCRIPTION
.LANG en
Prefers nuget.exe when it is available and falls back to MSBuild Restore through
the shared MSBuild resolver. The script restores packages only; it does not edit
packages.config, project references, or dependency versions.

.LANG zh-CN
存在 nuget.exe 时优先使用它，否则通过共享 MSBuild 定位脚本执行 MSBuild Restore。
本脚本只还原依赖包，不编辑 packages.config、项目引用或依赖版本。
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$solutionPath = Join-Path $repoRoot 'src\master.sln'

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution file not found: $solutionPath"
}

$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
if ($nuget) {
    Write-Host "Restoring NuGet packages with $($nuget.Source)"
    & $nuget.Source restore $solutionPath -NonInteractive
    exit $LASTEXITCODE
}

$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'
$msbuild = & $findMsBuild

# <lang>
#   <zh-CN>即使 packages 目录已存在也执行 Restore，以便及早发现缺包和 NuGet 安全告警。</zh-CN>
#   <en>Restore still runs when packages already exist so missing packages and NuGet security warnings surface early.</en>
# </lang>
Write-Host "nuget.exe not found; restoring packages with MSBuild: $msbuild"
& $msbuild $solutionPath /t:Restore /p:RestorePackagesConfig=true /v:minimal /m
exit $LASTEXITCODE
