<#
.SYNOPSIS
<lang>
  <en>Publishes the Portal Web Forms project to a filesystem folder.</en>
  <zh-CN>将 Portal Web Forms 项目发布到文件系统目录。</zh-CN>
</lang>

<lang>
  <en>Builds a filesystem publish output for the Portal project and runs publish readiness checks before and after the MSBuild WebPublish step. The script writes only to the selected publish folder, does not modify IIS, databases, external configuration, credentials, or production machines, and fails if the target folder already exists.</en>
  <zh-CN>将 Portal Web Forms 项目发布到文件系统目录，并在 MSBuild WebPublish 前后执行发布就绪检查。本脚本只写入指定发布目录，不修改 IIS、数据库、外置配置、凭据或生产机器；如果目标目录已经存在，会直接失败。</zh-CN>
</lang>

.PARAMETER Configuration
<lang>
  <en>Build configuration, normally Debug or Release.</en>
  <zh-CN>构建配置，通常为 Debug 或 Release。</zh-CN>
</lang>

.PARAMETER Platform
<lang>
  <en>MSBuild platform value passed through to the Portal project.</en>
  <zh-CN>传递给 Portal 项目的 MSBuild 平台值。</zh-CN>
</lang>

.PARAMETER PublishPath
<lang>
  <en>Target filesystem publish folder. Leave empty to create a timestamped folder under temp/publish.</en>
  <zh-CN>目标文件系统发布目录。留空时会在 temp/publish 下创建带时间戳的目录。</zh-CN>
</lang>
#>
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

# <lang>
#   <zh-CN>发布入口只接受仓库中的 Portal.csproj 和已绑定的 MSBuild/readiness helper，缺失即停止，不回退到未定义路径。</zh-CN>
#   <en>The publish entry accepts only the repository Portal.csproj and bound MSBuild/readiness helpers; missing inputs stop the flow without falling back to undefined paths.</en>
# </lang>
if (-not (Test-Path -LiteralPath $portalProject -PathType Leaf)) {
    throw "Portal project not found: $portalProject"
}

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $PublishPath = Join-Path $repoRoot "temp\publish\Portal-$Configuration-$stamp"
}

# <lang>
#   <zh-CN>空发布路径只生成临时目录名；真实写入仍受“不覆盖既有目录”和后续发布工具边界约束。</zh-CN>
#   <en>An empty publish path receives a temporary directory name; writes remain constrained by the no-overwrite guard and the publish tool boundary.</en>
# </lang>
$publishFullPath = [System.IO.Path]::GetFullPath($PublishPath)
# <lang>
#   <zh-CN>目标目录必须不存在，避免覆盖既有发布物；后续写入严格限制在该目录。</zh-CN>
#   <en>The target directory must not already exist to avoid overwriting a package; subsequent writes are scoped to it.</en>
# </lang>
if (Test-Path -LiteralPath $publishFullPath) {
    throw "Publish path already exists. Choose a new empty folder: $publishFullPath"
}

New-Item -ItemType Directory -Path $publishFullPath -Force | Out-Null
$msbuild = & $findMsBuild

# <lang>
#   <zh-CN>先完成只读 readiness，再决定是否调用 WebPublish；前置门禁失败会原样传播退出码且保留已声明的目录边界。</zh-CN>
#   <en>Run read-only readiness before deciding whether to call WebPublish; preflight failures propagate their exit code while preserving the declared directory boundary.</en>
# </lang>
# <lang>
#   <zh-CN>发布前 readiness 失败即停止，不启动 WebPublish；门禁只读检查项目与配置边界。</zh-CN>
#   <en>Stop before WebPublish when preflight readiness fails; the gate is a read-only check of project/configuration boundaries.</en>
# </lang>
Write-Host "Running publish readiness check before filesystem publish."
& $publishReadiness -PortalProjectPath $portalProject
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# <lang>
#   <zh-CN>这里使用 MSBuild WebPublish 到临时文件夹，只验证包内容，不修改 IIS、数据库或外置配置。</zh-CN>
#   <en>This uses MSBuild WebPublish to a temporary folder to validate package contents only; it does not change IIS, databases, or external config.</en>
# </lang>
Write-Host "Publishing $portalProject"
Write-Host "MSBuild: $msbuild"
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host "PublishPath: $publishFullPath"

& $msbuild $portalProject /t:WebPublish "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/p:WebPublishMethod=FileSystem" "/p:PublishUrl=$publishFullPath" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# <lang>
#   <zh-CN>WebPublish 成功后仍必须通过针对产物目录的 readiness；该结果只说明文件系统包静态满足门禁，不代表真实 IIS 已部署。</zh-CN>
#   <en>After WebPublish succeeds, the artifact must still pass readiness; this proves only static package compliance, not real IIS deployment.</en>
# </lang>
# <lang>
#   <zh-CN>发布后再次检查文件系统产物；失败时返回门禁退出码，不触碰 IIS、数据库或外置配置。</zh-CN>
#   <en>Run readiness again against the filesystem artifact and return its exit code without touching IIS, databases, or external config.</en>
# </lang>
Write-Host "Running publish readiness check against filesystem output."
& $publishReadiness -PortalProjectPath $portalProject -PublishedPath $publishFullPath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Publish output ready: $publishFullPath"
