<#
.SYNOPSIS
.LANG en
Finds an installed MSBuild executable for local portal automation.

.LANG zh-CN
为门户本地自动化查找已安装的 MSBuild 可执行文件。

.DESCRIPTION
.LANG en
Uses Visual Studio's vswhere first, then falls back to known Visual Studio and
Build Tools installation paths. The script only reports a usable path and does
not install, repair, or modify Visual Studio components.

.LANG zh-CN
优先使用 Visual Studio 的 vswhere，再回退到已知的 Visual Studio 与 Build Tools 安装路径。
本脚本只返回可用路径，不安装、修复或修改任何 Visual Studio 组件。
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$vswhereCandidates = @(
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

foreach ($vswhere in $vswhereCandidates) {
    $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1
    if ($found -and (Test-Path -LiteralPath $found)) {
        Write-Output $found
        exit 0
    }
}

$knownCandidates = @(
    'd:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)

foreach ($path in $knownCandidates) {
    if ($path -and (Test-Path -LiteralPath $path)) {
        Write-Output $path
        exit 0
    }
}

throw 'MSBuild.exe not found. Install Visual Studio Build Tools or run this task from a Visual Studio developer environment.'
