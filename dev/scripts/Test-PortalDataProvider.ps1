[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$DatabasePath,

    [switch]$KeepDatabase
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$projectPath = Join-Path $repoRoot 'src\Portal.DataProviderProof\Portal.DataProviderProof.csproj'
$schemaPath = Join-Path $repoRoot 'src\Setup\Providers\SQLite\PortalDataProviderProof.sql'
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Provider proof project not found: $projectPath"
}

if (-not (Test-Path -LiteralPath $schemaPath)) {
    throw "SQLite provider proof schema not found: $schemaPath"
}

$msbuild = & $findMsBuild

# 独立项目不加入主解决方案；这里单独 restore/build，避免影响 Visual Studio 的门户构建路径。
Write-Host "Restoring provider proof packages with $msbuild"
& $msbuild $projectPath /t:Restore /p:RestorePackagesConfig=true "/p:SolutionDir=$repoRoot\src\" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Building provider proof project"
& $msbuild $projectPath /t:Build "/p:Configuration=$Configuration" '/p:Platform=AnyCPU' /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputDirectory = Join-Path $repoRoot ("temp\provider-proof\bin\{0}" -f $Configuration)
$proofExecutable = Join-Path $outputDirectory 'Portal.DataProviderProof.exe'
if (-not (Test-Path -LiteralPath $proofExecutable)) {
    throw "Provider proof executable not found: $proofExecutable"
}

if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    $DatabasePath = Join-Path $repoRoot 'temp\provider-proof\data\PortalDataProviderProof.sqlite'
}

$fullDatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
$proofDataRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'temp\provider-proof\data')).TrimEnd('\') + '\'
if (-not $fullDatabasePath.StartsWith($proofDataRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "DatabasePath must stay under the provider proof data directory: $proofDataRoot"
}

$databaseDirectory = Split-Path -Parent $fullDatabasePath
New-Item -ItemType Directory -Path $databaseDirectory -Force | Out-Null

if ((-not $KeepDatabase) -and (Test-Path -LiteralPath $fullDatabasePath)) {
    Remove-Item -LiteralPath $fullDatabasePath -Force
}

Write-Host "Running SQLite provider capability proof"
& $proofExecutable '--database' $fullDatabasePath '--schema' $schemaPath
exit $LASTEXITCODE
