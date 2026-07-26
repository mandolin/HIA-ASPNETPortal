<#
.SYNOPSIS
.LANG en
Builds and runs the SQLite data-provider proof project.

.LANG zh-CN
构建并运行 SQLite 数据 provider proof 项目。

.DESCRIPTION
.LANG en
Restores and builds the isolated Portal.DataProviderProof project, then runs the
proof executable against the SQLite schema fixture. The proof project is kept out
of the main solution path so SQL Server production assumptions and Visual Studio
debugging behavior remain unchanged.

.LANG zh-CN
还原并构建隔离的 Portal.DataProviderProof 项目，然后使用 SQLite schema fixture 运行 proof
可执行文件。proof 项目不加入主解决方案路径，以免改变 SQL Server 生产假设和 Visual Studio 调试行为。

.PARAMETER Configuration
.LANG en
Build configuration for the proof project, normally Debug or Release.

.LANG zh-CN
proof 项目的构建配置，通常为 Debug 或 Release。

.PARAMETER DatabasePath
.LANG en
Optional SQLite database path. The resolved path must stay under temp/provider-proof/data.

.LANG zh-CN
可选 SQLite 数据库路径。解析后的路径必须位于 temp/provider-proof/data 下。

.PARAMETER KeepDatabase
.LANG en
Keeps an existing proof database instead of deleting it before the run.

.LANG zh-CN
运行前保留已有 proof 数据库，而不是先删除它。
#>
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

# <lang>
#   <zh-CN>独立项目不加入主解决方案；这里单独 restore/build，避免影响 Visual Studio 的门户构建路径。</zh-CN>
#   <en>The isolated project is not added to the main solution; restore/build runs here so the Visual Studio portal build path stays unchanged.</en>
# </lang>
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
