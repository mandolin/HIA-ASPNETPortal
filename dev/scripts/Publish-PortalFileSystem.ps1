[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Platform = 'AnyCPU',

    [string]$PublishPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$portalProject = Join-Path $repoRoot 'src\Portal\Portal.csproj'
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'
$publishReadiness = Join-Path $PSScriptRoot 'Test-PortalPublishReadiness.ps1'

if (-not (Test-Path -LiteralPath $portalProject -PathType Leaf)) {
    throw "Portal project not found: $portalProject"
}

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $PublishPath = Join-Path $repoRoot "temp\publish\Portal-$Configuration-$stamp"
}

$publishFullPath = [System.IO.Path]::GetFullPath($PublishPath)
if (Test-Path -LiteralPath $publishFullPath) {
    throw "Publish path already exists. Choose a new empty folder: $publishFullPath"
}

New-Item -ItemType Directory -Path $publishFullPath -Force | Out-Null
$msbuild = & $findMsBuild

Write-Host "Running publish readiness check before filesystem publish."
& $publishReadiness -PortalProjectPath $portalProject
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# 中文：这里使用 MSBuild WebPublish 到临时文件夹，只验证包内容，不修改 IIS、数据库或外置配置。
# English: This uses MSBuild WebPublish to a temporary folder to validate package contents only; it does not change IIS, databases, or external config.
Write-Host "Publishing $portalProject"
Write-Host "MSBuild: $msbuild"
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host "PublishPath: $publishFullPath"

& $msbuild $portalProject /t:WebPublish "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/p:WebPublishMethod=FileSystem" "/p:PublishUrl=$publishFullPath" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Running publish readiness check against filesystem output."
& $publishReadiness -PortalProjectPath $portalProject -PublishedPath $publishFullPath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Publish output ready: $publishFullPath"
