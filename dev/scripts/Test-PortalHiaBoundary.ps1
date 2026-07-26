<#
.SYNOPSIS
.LANG en
Builds and runs the HIA boundary proof project.

.LANG zh-CN
构建并运行 HIA 边界 proof 项目。

.DESCRIPTION
.LANG en
Builds the isolated Portal.HiaBoundaryProof project and executes fixture-based
checks for the current HIA integration boundary. The proof validates contracts
without adding HIA runtime dependencies to the default portal startup path.

.LANG zh-CN
构建隔离的 Portal.HiaBoundaryProof 项目，并基于 fixture 执行当前 HIA 集成边界检查。
该 proof 验证契约，但不把 HIA 运行时依赖加入门户默认启动路径。

.PARAMETER Configuration
.LANG en
Build configuration for the proof project, normally Debug or Release.

.LANG zh-CN
proof 项目的构建配置，通常为 Debug 或 Release。
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$projectPath = Join-Path $repoRoot 'src\Portal.HiaBoundaryProof\Portal.HiaBoundaryProof.csproj'
$fixtureDirectory = Join-Path $repoRoot 'src\Portal.HiaBoundaryProof\Fixtures'
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "HIA boundary proof project not found: $projectPath"
}

if (-not (Test-Path -LiteralPath $fixtureDirectory)) {
    throw "HIA boundary proof fixtures not found: $fixtureDirectory"
}

$msbuild = & $findMsBuild

# <lang>
#   <zh-CN>proof 不加入主解决方案，单独构建以验证默认门户路径不需要 HIA 运行时依赖。</zh-CN>
#   <en>The proof stays outside the main solution and builds separately to confirm the default portal path does not require HIA runtime dependencies.</en>
# </lang>
Write-Host "Building HIA boundary proof project"
& $msbuild $projectPath /t:Build "/p:Configuration=$Configuration" '/p:Platform=AnyCPU' /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputDirectory = Join-Path $repoRoot ("temp\hia-boundary-proof\bin\{0}" -f $Configuration)
$proofExecutable = Join-Path $outputDirectory 'Portal.HiaBoundaryProof.exe'
if (-not (Test-Path -LiteralPath $proofExecutable)) {
    throw "HIA boundary proof executable not found: $proofExecutable"
}

Write-Host "Running HIA boundary contract fixture proof"
& $proofExecutable '--fixtures' $fixtureDirectory
exit $LASTEXITCODE
