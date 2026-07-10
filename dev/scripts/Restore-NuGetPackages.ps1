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

# 即使 packages 目录已存在也执行 Restore，以发现缺包和 NuGet 安全告警。
Write-Host "nuget.exe not found; restoring packages with MSBuild: $msbuild"
& $msbuild $solutionPath /t:Restore /p:RestorePackagesConfig=true /v:minimal /m
exit $LASTEXITCODE
