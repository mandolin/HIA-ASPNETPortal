<#
.SYNOPSIS
<lang>
  <en>Builds the portal Visual Studio solution.</en>
  <zh-CN>构建门户 Visual Studio 解决方案。</zh-CN>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>定位仓库解决方案，通过本地辅助脚本解析 MSBuild，然后执行标准 Build 目标。本脚本刻意保持轻量，让 Visual Studio、VSCode 任务和本地类 CI 检查共用同一入口，同时不改写项目文件。</zh-CN>
  <en>Locate the repository solution, resolve MSBuild through the local helper, and run a normal Build target. The script stays intentionally thin so Visual Studio, VSCode tasks, and CI-style local checks share one command without changing project files.</en>
</lang>

.PARAMETER Configuration
<lang>
  <en>Build configuration to pass to MSBuild, normally Debug or Release.</en>
  <zh-CN>传给 MSBuild 的构建配置，通常为 Debug 或 Release。</zh-CN>
</lang>

.PARAMETER Platform
<lang>
  <en>MSBuild platform value. The legacy solution normally uses Any CPU.</en>
  <zh-CN>MSBuild 平台值。旧解决方案通常使用 Any CPU。</zh-CN>
</lang>

  证明边界：只证明解决方案能被 msbuild 成功构建（按传入配置/平台）；不证明构建产物可运行或行为正确（由 L2 运行期门禁与冒烟承载），不校验 ASCX 标记（Web Site 项目不进编译），不发布、不部署、不改 IIS。

  Proof boundary: Proof boundary: proves only that the solution builds with msbuild (per the given configuration/platform); it does not prove the output runs or behaves correctly (L2 runtime gates and smoke cover that), does not validate ASCX markup (a Web Site project does not compile it), and does not publish, deploy or touch IIS.
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
#   <zh-CN>**构建前先还原 NuGet 包**（`C-anp-P38` / `W-anp-P213` 修的真缺陷）。
#     这个解决方案用 `packages.config` 风格引用（EntityFramework / MSTest.TestFramework / EntityFramework.SqlServer /
#     Microsoft.VisualStudio.QualityTools.UnitTestFramework），而 `src/packages/` **被 .gitignore 忽略、零文件入库**。
#     于是本脚本此前在本地永远绿——本地工作树里恰好有 19 个未跟踪的包目录；而在 CI 的干净检出上
#     引用无法解析，构建直接失败（`MSB3245: Could not locate the assembly "EntityFramework"` +
#     一连串 `CS0234`）。
#     **这正是备忘 D8 那一类**："本地门禁跑在未跟踪文件上会给假通过"——只是这里未跟踪的不是文档，是依赖包。
#     修在门禁自身而不是只修工作流：门禁必须在"干净树"与"本地树"两种环境下都自足，
#     否则同一份代码在两种环境给出相反结论，比没有门禁更糟。</zh-CN>
#   <en>**Restore NuGet packages before building** (a real defect fixed by `C-anp-P38` / `W-anp-P213`).
#     This solution uses `packages.config`-style references (EntityFramework / MSTest.TestFramework /
#     EntityFramework.SqlServer / Microsoft.VisualStudio.QualityTools.UnitTestFramework), while `src/packages/`
#     **is gitignored with zero files committed**. This script therefore always passed locally — the local working tree
#     happens to hold 19 untracked package directories; on a clean CI checkout the references cannot resolve and the build
#     fails outright (`MSB3245: Could not locate the assembly "EntityFramework"` followed by a chain of `CS0234`).
#     **This is exactly the class of memo D8**: "local gates run on untracked files give false passes" — except here the
#     untracked thing is not documentation but dependency packages. The fix belongs in the gate itself rather than only in
#     the workflow: a gate must be self-sufficient on both a clean tree and a local tree, otherwise the same code yields
#     opposite verdicts in the two environments, which is worse than having no gate.</en>
# </lang>
$packagesRoot = Join-Path $repoRoot 'src\packages'
if (-not (Test-Path -LiteralPath $packagesRoot)) {
    Write-Host "Restoring NuGet packages (src/packages absent on a clean checkout)"
    $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
    if (-not $nuget) {
        # <lang>
        #   <zh-CN>找不到 `nuget.exe` 时**不静默跳过**：跳过就等于回到"本地绿、CI 红"的老路。
        #     明确失败并给出可执行提示（windows-latest 运行器自带 nuget CLI；本地可用
        #     `dotnet tool install --global NuGet.CommandLine` 或从 VS 开发者命令行取）。</zh-CN>
        #   <en>When `nuget.exe` cannot be found the script **does not silently skip**: skipping would walk straight back
        #     into the old "green locally, red in CI" path. It fails explicitly with an actionable hint (the windows-latest
        #     runner ships the nuget CLI; locally use `dotnet tool install --global NuGet.CommandLine` or take it from a VS
        #     developer prompt).</en>
        # </lang>
        throw '找不到 nuget.exe，无法还原 packages.config 依赖（src/packages 不存在）。请安装 NuGet CLI 后重跑；不跳过——跳过会重现"本地绿、CI 红"。'
    }
    & $nuget.Source restore $solutionPath -Verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw ('nuget restore 失败，退出码 ' + $LASTEXITCODE) }
    Write-Host "NuGet restore complete."
}
else {
    Write-Host "NuGet packages already present (src/packages exists); skipping restore."
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
