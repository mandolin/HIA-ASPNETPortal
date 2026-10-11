# C-anp-P20 新增模块就绪门禁：把"新增业务模块时四类构建看不出、运行期才炸"的坑前移为静态检查。
#
# 依据见 docs/module-development-guide.md（C-anp-P20 补强的四道坑）。本门禁只依赖源码，属编排入口 L0 层。
#
# 四类检查（均为 Fail 级，不可绕过）：
#   R1 代码后置必须登记进 Portal.csproj。src/Portal 是 Web Application 项目，既有模块的 .ascx.cs 编译进
#       bin/Portal.dll；新增模块若只在磁盘上存在而未登记 Compile，CodeBehind 在运行期报"未能加载类型"，
#       且 MSBuild 不会编译这些文件，故 out var / long->string 一类错误只在运行期暴露。
#   R2 .ascx 必须使用 CodeBehind（非 CodeFile）。
#   R3 6 套正式皮肤 Default.css 均须含 .<prefix> 主题作用域规则（AGENTS.md 硬性要求；只写模块自带 Styles/*.css
#       而主题层不补规则，模块在多数皮肤下丢失主题化样式）。
#   R4 模块私有 CSS 的类选择符必须在 .ascx 标记层出现。标记层与 CSS 类名漂移（如标记用 form-grid / list、
#       CSS 写 .grid / .recent）会让模块样式与主题规则全部落空却无报错。
#
# 设计同既有门禁"宁可漏检不可误报"：R4 全量比对模块 CSS 与 ascx；确有 JS/代码后置专属类时可用
# -SkipClassConsistency 据实豁免，并在提交信息写明理由。
#
# 用法：
#   & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoProfile -File dev\scripts\Test-PortalNewModuleReadiness.ps1 `
#       -ModuleName LeaveRequest -ModuleCssPrefix leave-request -ExpectedPackageId HIA.LeaveRequest
# 退出码：0 通过；1 有违规。

param(
    [Parameter(Mandatory = $true)]
    [string] $ModuleName,
    [string] $ModuleCssPrefix,
    [string] $ExpectedPackageId,
    [switch] $SkipClassConsistency
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = [System.Text.UTF8Encoding]::new($false)

# <lang>
#   <zh-CN>前缀默认取模块名小写（LeaveRequest -> leave-request）；若标记层类名另有约定，用 -ModuleCssPrefix 显式覆盖。</zh-CN>
#   <en>The prefix defaults to the lower-cased module name (LeaveRequest -> leave-request); when the markup uses a
#       different class convention, pass -ModuleCssPrefix explicitly.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($ModuleCssPrefix)) { $ModuleCssPrefix = $ModuleName.ToLowerInvariant() }

$moduleDir = Join-Path $repoRoot ('src\Portal\DesktopModules\' + $ModuleName)
$csprojPath = Join-Path $repoRoot 'src\Portal\Portal.csproj'
$themes = @('EnterpriseLight', 'EnterpriseDark', 'OaLight', 'OaDark', 'StateClassicLight', 'StateClassicDark')

$violations = @()
function Add-Violation([string] $rule, [string] $detail) {
    $script:violations += [pscustomobject]@{ rule = $rule; detail = $detail }
    Write-Host ('  [FAIL] ' + $rule + ' :: ' + $detail)
}

Write-Host ('[readiness] 模块=' + $ModuleName + ' 前缀=' + $ModuleCssPrefix + ' 目录=' + $moduleDir)

# R1：代码后置登记进 Portal.csproj
Write-Host '[readiness] R1 csproj 登记'
if (-not (Test-Path $csprojPath)) {
    Add-Violation 'R1' ('未找到 Portal.csproj：' + $csprojPath)
} else {
    $csproj = [System.IO.File]::ReadAllText($csprojPath, $utf8)
    $csEntry = ('DesktopModules\' + $ModuleName + '\' + $ModuleName + '.ascx.cs')
    $designerEntry = ('DesktopModules\' + $ModuleName + '\' + $ModuleName + '.ascx.designer.cs')
    if ($csproj -notmatch [regex]::Escape($csEntry)) {
        Add-Violation 'R1' ('Portal.csproj 缺少 Compile 登记：' + $csEntry + '（未登记则 CodeBehind 运行期报"未能加载类型"）')
    }
    if ($csproj -notmatch [regex]::Escape($designerEntry)) {
        Add-Violation 'R1' ('Portal.csproj 缺少 designer 登记：' + $designerEntry)
    }
    $ascxContentEntry = ('DesktopModules\' + $ModuleName + '\' + $ModuleName + '.ascx')
    if ($csproj -notmatch [regex]::Escape($ascxContentEntry)) {
        Add-Violation 'R1' ('Portal.csproj 缺少 Content 登记：' + $ascxContentEntry)
    }
}

# R2：.ascx 使用 CodeBehind
Write-Host '[readiness] R2 ascx CodeBehind 标记'
$ascxPath = Join-Path $moduleDir ($ModuleName + '.ascx')
if (-not (Test-Path $ascxPath)) {
    Add-Violation 'R2' ('未找到 .ascx：' + $ascxPath)
} else {
    $ascx = [System.IO.File]::ReadAllText($ascxPath, $utf8)
    if ($ascx -match 'CodeFile\s*=') {
        Add-Violation 'R2' ($ModuleName + '.ascx 使用了 CodeFile；本仓库既有模块用 CodeBehind（ASCX 编译契约 C1）')
    } elseif ($ascx -notmatch 'CodeBehind\s*=') {
        Add-Violation 'R2' ($ModuleName + '.ascx 缺少 CodeBehind 声明')
    }
}

# R2.5：packageId 与预期一致（可选，提供 -ExpectedPackageId 时校验）
if (-not [string]::IsNullOrWhiteSpace($ExpectedPackageId)) {
    $manifestPath = Join-Path $moduleDir 'module.json'
    if (Test-Path $manifestPath) {
        try {
            $manifest = Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($manifest.packageId -ne $ExpectedPackageId) {
                Add-Violation 'R2.5' ('module.json packageId=' + $manifest.packageId + ' 与预期 ' + $ExpectedPackageId + ' 不一致')
            }
        } catch {
            Add-Violation 'R2.5' ('module.json 解析失败：' + $_.Exception.Message)
        }
    } else {
        Add-Violation 'R2.5' ('未找到 module.json：' + $manifestPath)
    }
}

# R3：6 套正式皮肤均含 .<prefix> 主题作用域规则
Write-Host '[readiness] R3 6 套皮肤主题规则'
foreach ($theme in $themes) {
    $themeCss = Join-Path $repoRoot ('src\Portal\App_Themes\' + $theme + '\Default.css')
    if (-not (Test-Path $themeCss)) {
        Add-Violation 'R3' ('缺少皮肤 CSS：' + $themeCss)
        continue
    }
    $css = [System.IO.File]::ReadAllText($themeCss, $utf8)
    # <lang>
    #   <zh-CN>只查前缀类（如 .leave-request）是否出现在该皮肤规则中；具体由 body.portal-theme-* 作用域承载，
    #   不必强求是 body 作用域内的第一行，故用"含该选择器词"判定。</zh-CN>
    #   <en>Only checks whether the prefix class (e.g. .leave-request) appears in that skin's rules; the body.portal-theme-*
    #       scope carries it, so a substring match suffices rather than demanding it be the first token in a scoped rule.</en>
    # </lang>
    if ($css -notmatch [regex]::Escape(('.' + $ModuleCssPrefix))) {
        Add-Violation 'R3' ($theme + ' 的 Default.css 未含 .' + $ModuleCssPrefix + ' 主题作用域规则（AGENTS.md 硬性要求）')
    }
}

# R4：模块私有 CSS 的类选择符必须在 .ascx 标记层出现
if (-not $SkipClassConsistency) {
    Write-Host '[readiness] R4 标记层与 CSS 类名一致性'
    $cssPaths = Get-ChildItem $moduleDir -Recurse -File -Filter '*.css' | Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' }
    if ($cssPaths.Count -eq 0) {
        Add-Violation 'R4' ($ModuleName + ' 目录下未找到模块 CSS')
    } else {
        $cssText = ($cssPaths | ForEach-Object { [System.IO.File]::ReadAllText($_.FullName, $utf8) }) -join "`n"
        # <lang>
        #   <zh-CN>剥掉注释与 @media/@keyframes 块，仅取普通类选择符的词（去掉伪类/伪元素与组合符）。</zh-CN>
        #   <en>Strip comments and @media/@keyframes blocks, keeping only plain class-selector identifiers (drop pseudo /
        #       combinator tokens).</en>
        # </lang>
        $cssStripped = [regex]::Replace($cssText, '(?s)/\*.*?\*/', '')
        $cssStripped = [regex]::Replace($cssStripped, '(?s)@\w+[^{]*\{.*?\}', '')
        $cssClasses = @{}
        foreach ($m in [regex]::Matches($cssStripped, '\.([A-Za-z_][A-Za-z0-9_-]*)')) {
            $cssClasses[$m.Groups[1].Value] = $true
        }

        # <lang>
        #   <zh-CN>从 .ascx 收集所有 class="..." 与 CssClass="..." 词（Web Forms 服务端控件用 CssClass，
        #   纯标记 div 用 class），作为标记层实际使用的类名集合。</zh-CN>
        #   <en>Collect every class="..." and CssClass="..." token from the .ascx (server controls use CssClass,
        #       plain markup divs use class) as the set of class names actually used in markup.</en>
        # </lang>
        $ascxClasses = @{}
        foreach ($m in [regex]::Matches($ascx, '(?:class|CssClass)\s*=\s*"([^"]*)"')) {
            foreach ($tok in ($m.Groups[1].Value -split '\s+')) {
                if ($tok) { $ascxClasses[$tok] = $true }
            }
        }
        foreach ($cls in $cssClasses.Keys) {
            if (-not $ascxClasses.ContainsKey($cls)) {
                Add-Violation 'R4' ('模块 CSS 类 .' + $cls + ' 在 .ascx 标记层未出现（类名漂移会导致该样式落空）')
            }
        }
        if (-not $ascxClasses.ContainsKey($ModuleCssPrefix)) {
            Add-Violation 'R4' ('容器类 .' + $ModuleCssPrefix + ' 未在 .ascx 出现（模块根容器缺失）')
        }
    }
} else {
    Write-Host '[readiness] R4 已用 -SkipClassConsistency 豁免'
}

$count = $violations.Count
Write-Host ''
if ($count -eq 0) {
    Write-Host ('[readiness] PASS：' + $ModuleName + ' 新增模块就绪检查通过（' + $count + ' violation(s)）')
    exit 0
} else {
    Write-Host ('[readiness] FAIL：' + $ModuleName + ' 存在 ' + $count + ' 个违规')
    exit 1
}
