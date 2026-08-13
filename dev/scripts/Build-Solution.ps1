<#
.SYNOPSIS
.LANG en
Builds the portal Visual Studio solution.

.LANG zh-CN
构建门户 Visual Studio 解决方案。

.DESCRIPTION
<lang>
  <zh-CN>定位仓库解决方案，通过本地辅助脚本解析 MSBuild，然后执行标准 Build 目标。本脚本刻意保持轻量，让 Visual Studio、VSCode 任务和本地类 CI 检查共用同一入口，同时不改写项目文件。</zh-CN>
  <en>Locate the repository solution, resolve MSBuild through the local helper, and run a normal Build target. The script stays intentionally thin so Visual Studio, VSCode tasks, and CI-style local checks share one command without changing project files.</en>
</lang>

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

# <lang>
#   <zh-CN>仓库根目录和解决方案路径由脚本位置确定，避免调用方当前目录改变构建目标。</zh-CN>
#   <en>Derive the repository root and solution path from the script location so the caller's current directory cannot change the build target.</en>
# </lang>
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$solutionPath = Join-Path $repoRoot 'src\master.sln'
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'

if (-not (Test-Path -LiteralPath $solutionPath)) {
    # <lang>
    #   <zh-CN>缺少受控解决方案时立即停止，不尝试从其他目录猜测构建输入。</zh-CN>
    #   <en>Stop immediately when the controlled solution is missing instead of guessing a build input from another directory.</en>
    # </lang>
    throw "Solution file not found: $solutionPath"
}

# <lang>
#   <zh-CN>MSBuild 解析委托给共享只读 helper；本入口不安装、修复或修改 Visual Studio 组件。</zh-CN>
#   <en>Delegate MSBuild discovery to the shared read-only helper; this entrypoint does not install, repair, or modify Visual Studio components.</en>
# </lang>
$msbuild = & $findMsBuild

# <lang>
#   <zh-CN>仅输出本次构建的目标、工具、配置和平台，便于诊断而不改变解决方案或项目文件。</zh-CN>
#   <en>Report the target, tool, configuration, and platform for this build without changing the solution or project files.</en>
# </lang>
Write-Host "Building $solutionPath"
Write-Host "MSBuild: $msbuild"
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"

# <lang>
#   <zh-CN>执行标准 Build 目标并原样返回 MSBuild 退出码，让 VS、VSCode 任务和本地门禁共享同一失败语义。</zh-CN>
#   <en>Run the standard Build target and return MSBuild's exit code unchanged so Visual Studio, VSCode tasks, and local gates share the same failure semantics.</en>
# </lang>
& $msbuild $solutionPath /m /t:Build "/p:Configuration=$Configuration" "/p:Platform=$Platform" /v:minimal /nologo
exit $LASTEXITCODE
