<#
.SYNOPSIS
.LANG en
Runs the isolated HIA JSDoc pilot workspace.

.LANG zh-CN
运行隔离的 HIA JSDoc 试点工作区。

.DESCRIPTION
<lang>
  <zh-CN>在需要时还原 JSDoc 试点依赖，并在 dev/documentation/jsdoc 中执行 docs npm 脚本。本脚本只是文档化试点入口，不改变 Portal 前端包、生成资产或 Visual Studio Task Runner 绑定。</zh-CN>
  <en>Restore JSDoc pilot dependencies when required and execute the docs npm script in dev/documentation/jsdoc. This is a documentation-only pilot entrypoint and does not change Portal front-end packages, generated assets, or Visual Studio Task Runner bindings.</en>
</lang>

.PARAMETER SkipRestore
.LANG en
Skips npm ci when the existing node_modules directory is acceptable for the caller.

.LANG zh-CN
当调用方接受现有 node_modules 目录状态时，跳过 npm ci。
#>
[CmdletBinding()]
param(
    [switch]$SkipRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>文档工具保持独立，不能改变 Portal 前端依赖或 Visual Studio Task Runner 的既有行为。</zh-CN>
#   <en>The documentation tool stays isolated and must not alter Portal front-end dependencies or Visual Studio Task Runner behavior.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolDirectory = Join-Path $repositoryRoot 'dev\documentation\jsdoc'
$packageLockPath = Join-Path $toolDirectory 'package-lock.json'
$nodeModulesPath = Join-Path $toolDirectory 'node_modules'

if (-not (Test-Path -LiteralPath $packageLockPath -PathType Leaf)) {
    # <lang>
    #   <zh-CN>锁文件是隔离 JSDoc 工作区可重复还原的必要前提，缺失时不执行未锁定安装。</zh-CN>
    #   <en>The lock file is required for reproducible restoration of the isolated JSDoc workspace; do not run an unlocked install when it is missing.</en>
    # </lang>
    throw "缺少锁定依赖文件：$packageLockPath"
}

# <lang>
#   <zh-CN>位置切换仅作用于 JSDoc 工具目录，并由 finally 保证恢复调用方工作目录。</zh-CN>
#   <en>Change location only within the JSDoc tool directory, and use finally to restore the caller's working directory.</en>
# </lang>
Push-Location $toolDirectory
try {
    # <lang>
    #   <zh-CN>依赖还原限定在隔离工作区，不修改 Portal 前端 package 或旧 Gulp 流程。</zh-CN>
    #   <en>Dependency restoration is limited to the isolated workspace and does not modify Portal front-end packages or the legacy Gulp flow.</en>
    # </lang>
    if (-not $SkipRestore -or -not (Test-Path -LiteralPath $nodeModulesPath -PathType Container)) {
        & npm ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci 失败，退出代码：$LASTEXITCODE"
        }
    }

    # <lang>
    #   <zh-CN>执行唯一的 JSDoc docs 入口并显式传播失败退出码，避免文档试点被报告为成功。</zh-CN>
    #   <en>Run the single JSDoc docs entrypoint and explicitly propagate failure exit codes so the documentation pilot cannot be reported as successful.</en>
    # </lang>
    & npm run docs
    if ($LASTEXITCODE -ne 0) {
        throw "HIA JSDoc pilot 失败，退出代码：$LASTEXITCODE"
    }
}
finally {
    # <lang>
    #   <zh-CN>无论依赖还原或文档生成如何结束，都恢复脚本调用前的工作目录。</zh-CN>
    #   <en>Always restore the working directory from before the script was invoked, regardless of restore or generation outcome.</en>
    # </lang>
    Pop-Location
}
