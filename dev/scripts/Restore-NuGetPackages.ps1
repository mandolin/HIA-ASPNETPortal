<#
.SYNOPSIS
.LANG en
Restores NuGet packages for the portal solution.

.LANG zh-CN
还原门户解决方案的 NuGet 包。

.DESCRIPTION
<lang>
  <zh-CN>存在 nuget.exe 时优先使用它，否则通过共享 MSBuild 定位脚本执行 MSBuild Restore。本脚本只还原依赖包，不编辑 packages.config、项目引用或依赖版本。</zh-CN>
  <en>Prefer nuget.exe when available and fall back to MSBuild Restore through the shared MSBuild resolver. The script restores packages only; it does not edit packages.config, project references, or dependency versions.</en>
</lang>
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>仓库根目录和解决方案路径由脚本位置确定，避免当前目录把还原目标导向其他解决方案。</zh-CN>
#   <en>Derive the repository root and solution path from the script location so the current directory cannot redirect restore to another solution.</en>
# </lang>
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$solutionPath = Join-Path $repoRoot 'src\master.sln'

if (-not (Test-Path -LiteralPath $solutionPath)) {
    # <lang>
    #   <zh-CN>缺少受控解决方案时立即停止，不执行任何 NuGet 或 MSBuild 还原。</zh-CN>
    #   <en>Stop immediately when the controlled solution is missing; do not run NuGet or MSBuild restore.</en>
    # </lang>
    throw "Solution file not found: $solutionPath"
}

# <lang>
#   <zh-CN>优先使用调用环境中的 nuget.exe，保持非交互模式并把退出码直接传回调用方。</zh-CN>
#   <en>Prefer nuget.exe from the caller's environment, keep the operation non-interactive, and return its exit code directly.</en>
# </lang>
$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
if ($nuget) {
    Write-Host "Restoring NuGet packages with $($nuget.Source)"
    & $nuget.Source restore $solutionPath -NonInteractive
    exit $LASTEXITCODE
}

# <lang>
#   <zh-CN>没有 nuget.exe 时复用共享 MSBuild 解析器，再执行 Restore；不改变包配置、项目引用或版本声明。</zh-CN>
#   <en>When nuget.exe is unavailable, reuse the shared MSBuild resolver and run Restore without changing package configuration, project references, or version declarations.</en>
# </lang>
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'
$msbuild = & $findMsBuild

# <lang>
#   <zh-CN>即使 packages 目录已存在也执行 Restore，以便及早发现缺包和 NuGet 安全告警。</zh-CN>
#   <en>Restore still runs when packages already exist so missing packages and NuGet security warnings surface early.</en>
# </lang>
Write-Host "nuget.exe not found; restoring packages with MSBuild: $msbuild"
& $msbuild $solutionPath /t:Restore /p:RestorePackagesConfig=true /v:minimal /m
exit $LASTEXITCODE
