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
  <en>Build configuration forwarded to MSBuild WebPublish; it affects the generated filesystem package only and does not select a real target environment.</en>
  <zh-CN>转发给 MSBuild WebPublish 的构建配置；它只影响生成的文件系统发布包，不选择真实目标环境。</zh-CN>
</lang>

.PARAMETER Platform
<lang>
  <en>MSBuild platform value passed through to the Portal project, preserving the existing Visual Studio project contract.</en>
  <zh-CN>传递给 Portal 项目的 MSBuild 平台值，用于保持既有 Visual Studio 项目契约。</zh-CN>
</lang>

.PARAMETER PublishPath
<lang>
  <en>Target filesystem publish folder. Leave empty to create a timestamped folder under temp/publish; an existing folder is rejected to protect previous packages and manual targets.</en>
  <zh-CN>目标文件系统发布目录。留空时会在 temp/publish 下创建带时间戳的目录；若目录已存在则拒绝执行，以保护既有发布包和人工维护目标。</zh-CN>
</lang>
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Platform = 'AnyCPU',

    [string]$PublishPath
)

# <lang>
#   <zh-CN>严格模式和 fail-fast 策略让发布脚本在路径、helper 或 MSBuild 调用异常时立即停止，避免留下看似成功的半成品包。</zh-CN>
#   <en>Strict mode and fail-fast handling stop the publish script immediately on path, helper, or MSBuild-call errors so it cannot leave a half-created package that looks successful.</en>
# </lang>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>仓库根由脚本位置解析，确保 CI、VSCode 任务和人工 shell 从不同目录调用时仍使用同一个项目边界。</zh-CN>
#   <en>The repository root is resolved from the script location so CI, VSCode tasks, and manual shells use the same project boundary even when invoked from different directories.</en>
# </lang>
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')

# <lang>
#   <zh-CN>Portal 项目路径固定到仓库内 Web Forms `.csproj`，避免误发布其它解决方案或 ASP.NET Core 迁移试验产物。</zh-CN>
#   <en>The Portal project path is fixed to the repository Web Forms `.csproj` to avoid publishing another solution or an ASP.NET Core migration experiment by mistake.</en>
# </lang>
$portalProject = Join-Path $repoRoot 'src\Portal\Portal.csproj'

# <lang>
#   <zh-CN>MSBuild 查找 helper 由仓库脚本提供，保留 Visual Studio/MSBuild 解析策略的一处来源。</zh-CN>
#   <en>The MSBuild lookup helper comes from the repository scripts, keeping Visual Studio/MSBuild resolution policy in one place.</en>
# </lang>
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'

# <lang>
#   <zh-CN>发布 readiness helper 是 WebPublish 前后共用的只读门禁，避免发布流程绕过配置/产物边界检查。</zh-CN>
#   <en>The publish-readiness helper is the shared read-only gate before and after WebPublish, preventing the publish flow from bypassing configuration/package boundary checks.</en>
# </lang>
$publishReadiness = Join-Path $PSScriptRoot 'Test-PortalPublishReadiness.ps1'

# <lang>
#   <zh-CN>发布入口只接受仓库中的 Portal.csproj 和已绑定的 MSBuild/readiness helper，缺失即停止，不回退到未定义路径。</zh-CN>
#   <en>The publish entry accepts only the repository Portal.csproj and bound MSBuild/readiness helpers; missing inputs stop the flow without falling back to undefined paths.</en>
# </lang>
if (-not (Test-Path -LiteralPath $portalProject -PathType Leaf)) {
    throw "Portal project not found: $portalProject"
}

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    # <lang>
    #   <zh-CN>默认路径时间戳使每次本地发布拥有独立目录，降低重复演练覆盖先前证据的风险。</zh-CN>
    #   <en>The default-path timestamp gives each local publish a distinct directory, reducing the risk that repeated rehearsals overwrite earlier evidence.</en>
    # </lang>
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

    # <lang>
    #   <zh-CN>默认发布目录位于仓库临时区，表达这是可丢弃产物而非正式目标机器路径。</zh-CN>
    #   <en>The default publish directory lives under the repository temp area, signaling that it is disposable output rather than an official target-machine path.</en>
    # </lang>
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

# <lang>
#   <zh-CN>此处是脚本首次写入文件系统目标目录；由于前置 no-overwrite 检查已通过，`-Force` 仅用于创建中间目录。</zh-CN>
#   <en>This is the script's first write to the filesystem target; because the no-overwrite check already passed, `-Force` is used only to create intermediate directories.</en>
# </lang>
New-Item -ItemType Directory -Path $publishFullPath -Force | Out-Null

# <lang>
#   <zh-CN>解析一次 MSBuild 路径并复用于当前发布，避免前后日志和执行器来源不一致。</zh-CN>
#   <en>Resolve the MSBuild path once and reuse it for this publish so logs and the executor source stay consistent.</en>
# </lang>
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

# <lang>
#   <zh-CN>最终输出只声明文件系统包路径已通过静态发布门禁，不暗示 IIS、数据库、账号或真实流量已经验证。</zh-CN>
#   <en>The final message states only that the filesystem package path passed static publish gates; it does not imply IIS, database, account, or real-traffic validation.</en>
# </lang>
Write-Host "Publish output ready: $publishFullPath"
