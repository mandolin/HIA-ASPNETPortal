<#
.SYNOPSIS
.LANG en
Validates the portal XML documentation build outputs.

.LANG zh-CN
验证门户 XML 文档构建输出。

.DESCRIPTION
.LANG en
Optionally builds Debug|Any CPU, then checks the expected XML documentation files
for existence, parseability, assembly names, and non-empty member lists. The
script verifies documentation artifacts only and does not rewrite MSBuild or
Visual Studio project settings.

.LANG zh-CN
可选构建 Debug|Any CPU，然后检查预期 XML 文档文件是否存在、能否解析、程序集名称是否匹配、
成员列表是否非空。本脚本只验证文档产物，不改写 MSBuild 或 Visual Studio 项目设置。

.PARAMETER Build
.LANG en
Builds the solution before checking the XML documentation artifacts.

.LANG zh-CN
检查 XML 文档产物前先构建解决方案。
#>
[CmdletBinding()]
param(
    [switch]$Build
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>P4.3 只验证既有 Debug XML 文档输出，不能改写旧项目的 MSBuild/Visual Studio 配置。</zh-CN>
#   <en>P4.3 validates existing Debug XML documentation outputs only and must not rewrite legacy MSBuild or Visual Studio settings.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$expectedDocuments = @(
    [pscustomobject]@{ Project = 'Portal'; RelativePath = 'src/Portal/bin/Portal.xml'; AssemblyName = 'Portal' },
    [pscustomobject]@{ Project = 'Portal.Components'; RelativePath = 'src/Portal.Components/bin/Debug/Portal.Components.xml'; AssemblyName = 'Portal.Components' },
    [pscustomobject]@{ Project = 'Portal.Components.Data'; RelativePath = 'src/Portal/bin/Portal.Components.Data.xml'; AssemblyName = 'Portal.Components.Data' },
    [pscustomobject]@{ Project = 'Portal.Components.Data1'; RelativePath = 'src/Portal/bin/Portal.Components.Data1.xml'; AssemblyName = 'Portal.Components.Data1' }
)

if ($Build) {
    $buildScript = Join-Path $PSScriptRoot 'Build-Solution.ps1'
    & $buildScript -Configuration Debug -Platform 'Any CPU'
    if ($LASTEXITCODE -ne 0) {
        throw "Debug|Any CPU 解决方案构建失败，退出代码：$LASTEXITCODE"
    }
}

$results = foreach ($expectedDocument in $expectedDocuments) {
    $absolutePath = Join-Path $repositoryRoot ($expectedDocument.RelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        throw "缺少 XML 文档输出：$($expectedDocument.RelativePath)。请使用 -Build 或先在 Visual Studio 中构建 Debug|Any CPU。"
    }

    try {
        [xml]$xmlDocument = [System.IO.File]::ReadAllText($absolutePath)
    }
    catch {
        throw "XML 文档无法解析：$($expectedDocument.RelativePath)。$($_.Exception.Message)"
    }

    if ($null -eq $xmlDocument.doc -or $null -eq $xmlDocument.doc.assembly -or $null -eq $xmlDocument.doc.members) {
        throw "XML 文档结构不完整：$($expectedDocument.RelativePath)。"
    }

    $actualAssemblyName = ([string]$xmlDocument.doc.assembly.name).Trim()
    if (-not $actualAssemblyName.Equals($expectedDocument.AssemblyName, [System.StringComparison]::Ordinal)) {
        throw "XML 程序集名称不匹配：$($expectedDocument.RelativePath)。期望 '$($expectedDocument.AssemblyName)'，实际 '$actualAssemblyName'。"
    }

    $members = @($xmlDocument.doc.members.member)
    if ($members.Count -eq 0) {
        throw "XML 文档不包含成员条目：$($expectedDocument.RelativePath)。"
    }

    [pscustomobject][ordered]@{
        Project = $expectedDocument.Project
        XmlDocument = $expectedDocument.RelativePath
        AssemblyName = $actualAssemblyName
        MemberCount = $members.Count
    }
}

$results
