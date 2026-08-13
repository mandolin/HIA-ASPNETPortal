<#
.SYNOPSIS
.LANG en
Finds an installed MSBuild executable for local portal automation.

.LANG zh-CN
为门户本地自动化查找已安装的 MSBuild 可执行文件。

.DESCRIPTION
<lang>
  <zh-CN>优先使用 Visual Studio 的 vswhere，再回退到已知的 Visual Studio 与 Build Tools 安装路径。本脚本只返回可用路径，不安装、修复或修改任何 Visual Studio 组件。</zh-CN>
  <en>Use Visual Studio's vswhere first, then fall back to known Visual Studio and Build Tools installation paths. The script only reports a usable path and does not install, repair, or modify Visual Studio components.</en>
</lang>
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>候选路径只覆盖受控的本机安装位置；不存在的路径在枚举前被过滤，不触发安装或修复。</zh-CN>
#   <en>Candidate paths are limited to controlled local installation locations; missing paths are filtered before enumeration without installing or repairing anything.</en>
# </lang>
$vswhereCandidates = @(
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

foreach ($vswhere in $vswhereCandidates) {
    # <lang>
    #   <zh-CN>优先让 vswhere 按最新实例和 MSBuild 组件要求解析真实路径；只接受存在的首个结果。</zh-CN>
    #   <en>Prefer vswhere to resolve the newest instance requiring the MSBuild component, and accept only the first existing result.</en>
    # </lang>
    $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1
    if ($found -and (Test-Path -LiteralPath $found)) {
        Write-Output $found
        exit 0
    }
}

# <lang>
#   <zh-CN>只有在 vswhere 无结果时才检查固定版本路径，顺序保持兼容既有开发机环境。</zh-CN>
#   <en>Check fixed-version paths only when vswhere yields no result, preserving the order expected by existing developer environments.</en>
# </lang>
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
    # <lang>
    #   <zh-CN>回退路径仍是只读存在性检查，找到第一个可用文件后立即返回，避免产生多重工具选择。</zh-CN>
    #   <en>The fallback remains a read-only existence check and returns the first usable file to avoid multiple tool selections.</en>
    # </lang>
    if ($path -and (Test-Path -LiteralPath $path)) {
        Write-Output $path
        exit 0
    }
}

# <lang>
#   <zh-CN>所有受控候选均不可用时明确失败，并把安装建议留给调用方处理。</zh-CN>
#   <en>Fail explicitly when every controlled candidate is unavailable and leave installation decisions to the caller.</en>
# </lang>
throw 'MSBuild.exe not found. Install Visual Studio Build Tools or run this task from a Visual Studio developer environment.'
