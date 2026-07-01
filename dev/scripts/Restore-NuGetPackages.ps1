[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$solutionPath = Join-Path $repoRoot 'src\master.sln'
$packagesDir = Join-Path $repoRoot 'src\packages'

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution file not found: $solutionPath"
}

$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
if ($nuget) {
    Write-Host "Restoring NuGet packages with $($nuget.Source)"
    & $nuget.Source restore $solutionPath -NonInteractive
    exit $LASTEXITCODE
}

if (Test-Path -LiteralPath $packagesDir) {
    Write-Host 'nuget.exe not found; src\packages already exists, so explicit restore is skipped.'
    exit 0
}

$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'
$msbuild = & $findMsBuild

Write-Host "nuget.exe not found; trying MSBuild restore with $msbuild"
& $msbuild $solutionPath /t:Restore /p:RestorePackagesConfig=true /v:minimal /m
exit $LASTEXITCODE
