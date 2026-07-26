<#
.SYNOPSIS
.LANG en
Runs the isolated HIA JSDoc pilot workspace.

.LANG zh-CN
运行隔离的 HIA JSDoc 试点工作区。

.DESCRIPTION
.LANG en
Restores the JSDoc pilot dependencies when required and executes the docs npm
script in dev/documentation/jsdoc. This script is a documentation pilot only and
does not change Portal front-end packages, generated assets, or Visual Studio
Task Runner bindings.

.LANG zh-CN
在需要时还原 JSDoc 试点依赖，并在 dev/documentation/jsdoc 中执行 docs npm 脚本。
本脚本只是文档化试点入口，不改变 Portal 前端包、生成资产或 Visual Studio Task Runner 绑定。

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
    throw "缺少锁定依赖文件：$packageLockPath"
}

Push-Location $toolDirectory
try {
    if (-not $SkipRestore -or -not (Test-Path -LiteralPath $nodeModulesPath -PathType Container)) {
        & npm ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci 失败，退出代码：$LASTEXITCODE"
        }
    }

    & npm run docs
    if ($LASTEXITCODE -ne 0) {
        throw "HIA JSDoc pilot 失败，退出代码：$LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
