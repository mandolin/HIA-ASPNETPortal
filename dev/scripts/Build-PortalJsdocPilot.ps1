[CmdletBinding()]
param(
    [switch]$SkipRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 文档工具保持独立，不能改变 Portal 前端依赖或 Visual Studio Task Runner 的既有行为。
# The documentation tool stays isolated and must not alter Portal front-end dependencies or Visual Studio Task Runner behavior.
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
