<#
.SYNOPSIS
.LANG en
Builds and runs the SQLite data-provider proof project.

.LANG zh-CN
构建并运行 SQLite 数据 provider proof 项目。

.DESCRIPTION
<lang>
  <en>Restores and builds the isolated Portal.DataProviderProof project, then runs the proof executable against the SQLite schema fixture. The proof project stays outside the main solution so SQL Server production assumptions and Visual Studio debugging behavior remain unchanged.</en>
  <zh-CN>还原并构建隔离的 Portal.DataProviderProof 项目，然后使用 SQLite schema fixture 运行 proof 可执行文件。proof 项目不加入主解决方案路径，以免改变 SQL Server 生产假设和 Visual Studio 调试行为。</zh-CN>
</lang>

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

# <lang>
#   <zh-CN>项目和 schema 路径固定在仓库内的隔离 proof 目录；后续门禁先确认它们存在，再交给 MSBuild。</zh-CN>
#   <en>Project and schema paths are fixed to the repository's isolated proof directories; existence is guarded before either path reaches MSBuild.</en>
# </lang>
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
# <lang>
#   <zh-CN>restore 失败立即传播原始退出码，避免在不完整依赖上继续构建或运行 proof。</zh-CN>
#   <en>The restore exit code is propagated immediately so an incomplete dependency graph cannot continue to build or run the proof.</en>
# </lang>
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Building provider proof project"
& $msbuild $projectPath /t:Build "/p:Configuration=$Configuration" '/p:Platform=AnyCPU' /v:minimal /nologo
# <lang>
#   <zh-CN>独立构建仍使用显式配置和 AnyCPU，失败时保留 MSBuild 退出码，不改变主解决方案的构建路径。</zh-CN>
#   <en>The isolated build uses an explicit configuration and AnyCPU; failures preserve the MSBuild exit code without changing the main solution build path.</en>
# </lang>
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputDirectory = Join-Path $repoRoot ("temp\provider-proof\bin\{0}" -f $Configuration)
$proofExecutable = Join-Path $outputDirectory 'Portal.DataProviderProof.exe'
# <lang>
#   <zh-CN>只有构建产物存在时才允许进入运行阶段，避免把缺失文件误判为 provider 能力证明。</zh-CN>
#   <en>Execution is allowed only when the build artifact exists, preventing a missing executable from being reported as provider capability evidence.</en>
# </lang>
if (-not (Test-Path -LiteralPath $proofExecutable)) {
    throw "Provider proof executable not found: $proofExecutable"
}

if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    $DatabasePath = Join-Path $repoRoot 'temp\provider-proof\data\PortalDataProviderProof.sqlite'
}

# <lang>
#   <zh-CN>数据库路径先规范化并限制在 proof data 根目录下，防止参数把清理或 proof 写入带出隔离临时区。</zh-CN>
#   <en>The database path is normalized and confined to the proof data root before any cleanup or proof write can occur.</en>
# </lang>
$fullDatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
$proofDataRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'temp\provider-proof\data')).TrimEnd('\') + '\'
if (-not $fullDatabasePath.StartsWith($proofDataRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "DatabasePath must stay under the provider proof data directory: $proofDataRoot"
}

$databaseDirectory = Split-Path -Parent $fullDatabasePath
New-Item -ItemType Directory -Path $databaseDirectory -Force | Out-Null

# <lang>
#   <zh-CN>默认运行前删除旧数据库以保证 fixture 从已知状态开始；KeepDatabase 明确请求时保留既有文件。</zh-CN>
#   <en>By default the prior database is removed so the fixture starts from a known state; KeepDatabase explicitly opts into preserving it.</en>
# </lang>
if ((-not $KeepDatabase) -and (Test-Path -LiteralPath $fullDatabasePath)) {
    Remove-Item -LiteralPath $fullDatabasePath -Force
}

Write-Host "Running SQLite provider capability proof"
# <lang>
#   <zh-CN>proof 进程只接收已校验的数据库和仓库内 schema 路径，最终原样传播其退出码。</zh-CN>
#   <en>The proof process receives only the validated database and repository schema paths, and its exit code is propagated unchanged.</en>
# </lang>
& $proofExecutable '--database' $fullDatabasePath '--schema' $schemaPath
exit $LASTEXITCODE
