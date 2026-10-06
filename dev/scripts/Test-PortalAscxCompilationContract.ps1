# W87 ASCX 编译契约静态门禁：把"运行期才暴露的标记层问题"前移到静态检查。
#
# 覆盖 W86 遗留缺口：5 个后台管理型模块（tab=6）无可访问 URL，运行期门禁覆盖不到。
# 本脚本让它们（及全部 ASCX）进入门禁 —— 只依赖源码，属编排入口 L0 层。
#
# 四类检查（依据见 work-zone/dev/plans/W-anp-W87.md）：
#   C1 CodeBehind 文件存在；C2 Inherits 与代码后置 namespace+class 一致；
#   C3 标记层表达式引用的类型可解析（防 CS0103，即 P74.3 / P81 的故障）；
#   C4 module.json 的 desktopEntry 存在且能提取模块级唯一 class（供运行期门禁作 selector）。
#
# 关键设计：**宁可漏检，不可误报**。C3 只对"全仓 .cs 中确实存在同名类型"的标识符生效，
# 资源键（Contacts_LabelName）、表达式成员（Render）、属性名自然被排除 —— 它们不在类型表里。
# 误报会让门禁永远红着，红久了被当成背景噪音忽略，门禁就等于不存在。
#
# 用法：& pwsh -NoProfile -File dev\scripts\Test-PortalAscxCompilationContract.ps1
# 退出码：0 通过；1 有违规。

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = [System.Text.UTF8Encoding]::new($false)
$violations = @()
function Add-Violation([string] $f, [string] $rule, [string] $detail) {
    $script:violations += [pscustomobject]@{ file = $f; rule = $rule; detail = $detail }
}

# 步骤 1：建立「类型名 → 命名空间」映射
# 只收 class/interface/struct/enum 的显式命名空间声明。同一类型名可能出现在多个命名空间
# （Portal 与 Portal.Components 都有同名类型），故保留全部候选，判定按"任一候选被覆盖即通过"
# —— 会漏掉真正的歧义，但不会误报，与"宁可漏检"一致。
$typeMap = @{}
$csFiles = Get-ChildItem $repoRoot -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\|\\temp\\|node_modules' }
foreach ($csFile in $csFiles) {
    $text = [System.IO.File]::ReadAllText($csFile.FullName, $utf8)
    $ns = [regex]::Match($text, '(?m)^namespace\s+([A-Za-z0-9_.]+)')
    if (-not $ns.Success) { continue }
    $nsName = $ns.Groups[1].Value
    foreach ($tm in [regex]::Matches($text, '(?m)^\s*(?:public|internal|private|protected|static|sealed|abstract|partial|\s)*\b(?:class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
        $tn = $tm.Groups[1].Value
        if (-not $typeMap.ContainsKey($tn)) { $typeMap[$tn] = New-Object System.Collections.Generic.List[string] }
        if (-not $typeMap[$tn].Contains($nsName)) { $typeMap[$tn].Add($nsName) }
    }
}
Write-Host ("[ascx] 类型表：{0} 个类型（来自 {1} 个 .cs）" -f $typeMap.Count, $csFiles.Count)
# 步骤 2：逐个 ASCX 做 C1 / C2 / C3
$ascxFiles = Get-ChildItem (Join-Path $repoRoot 'src') -Recurse -File -Filter '*.ascx' |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' }
$usableSelectors = @{}

foreach ($ascx in $ascxFiles) {
    $ascxRel = $ascx.FullName.Replace($repoRoot + '\', '')
    $markup = [System.IO.File]::ReadAllText($ascx.FullName, $utf8)

    # C1 CodeBehind 文件存在
    $codeBehind = [regex]::Match($markup, 'CodeBehind\s*=\s*"([^"]+)"')
    if (-not $codeBehind.Success) {
        Add-Violation $ascxRel 'C1' '标记层没有 CodeBehind 声明'
        continue
    }
    $codeBehindPath = Join-Path $ascx.DirectoryName $codeBehind.Groups[1].Value
    if (-not (Test-Path $codeBehindPath -PathType Leaf)) {
        Add-Violation $ascxRel 'C1' ('CodeBehind 指向的文件不存在: ' + $codeBehind.Groups[1].Value)
        continue
    }
    $codeText = [System.IO.File]::ReadAllText($codeBehindPath, $utf8)

    # C2 Inherits 与代码后置一致
    $inherits = [regex]::Match($markup, 'Inherits\s*=\s*"([^"]+)"')
    $codeNs = [regex]::Match($codeText, '(?m)^namespace\s+([A-Za-z0-9_.]+)')
    $codeClass = [regex]::Match($codeText, '(?m)^\s*(?:public|internal|partial|\s)*class\s+([A-Za-z_][A-Za-z0-9_]*)')
    if (-not $inherits.Success) {
        Add-Violation $ascxRel 'C2' '标记层没有 Inherits 声明'
    } elseif ($codeNs.Success -and $codeClass.Success) {
        $expected = $codeNs.Groups[1].Value + '.' + $codeClass.Groups[1].Value
        if ($inherits.Groups[1].Value -ne $expected) {
            Add-Violation $ascxRel 'C2' ('Inherits 与代码后置不一致：Inherits=' + $inherits.Groups[1].Value + '，代码后置=' + $expected)
        }
    }

    # C3 标记层表达式引用的类型可解析
    # 覆盖集合 = Import 声明 + Inherits 的命名空间 + 代码后置的 using
    $covered = New-Object System.Collections.Generic.List[string]
    foreach ($im in [regex]::Matches($markup, '<%@\s*Import\s+Namespace\s*=\s*"([^"]+)"')) { $covered.Add($im.Groups[1].Value) }
    if ($inherits.Success -and $inherits.Groups[1].Value.Contains('.')) {
        $covered.Add($inherits.Groups[1].Value.Substring(0, $inherits.Groups[1].Value.LastIndexOf('.')))
    }
    foreach ($um in [regex]::Matches($codeText, '(?m)^using\s+([A-Za-z0-9_.]+)\s*;')) { $covered.Add($um.Groups[1].Value) }

    # 标识符只在"命名空间/类型位置"才算引用：前面不是点号（排除了 Render、属性名等成员访问）
    $unresolved = @()
    foreach ($em in [regex]::Matches($markup, '<%=[^%]*%>')) {
        $expr = $em.Value
        $expr = [regex]::Replace($expr, '"[^"]*"', '""')          # 去字符串字面量
        foreach ($im in [regex]::Matches($expr, '(?<![\w.])[A-Z][A-Za-z0-9_]*')) {
            $id = $im.Value
            if (-not $typeMap.ContainsKey($id)) { continue }        # 资源键/局部变量：不在类型表 → 跳过
            $namespaces = $typeMap[$id]
            $hit = $false
            foreach ($candidate in $namespaces) {
                if ($covered.Contains($candidate)) { $hit = $true; break }
                # 也接受"被覆盖命名空间的前缀"（如 Portal 覆盖 Portal.Components 的用法）
                foreach ($c in $covered) { if ($candidate.StartsWith($c + '.')) { $hit = $true; break } }
                if ($hit) { break }
            }
            if (-not $hit) { $unresolved += ($id + '(' + ($namespaces -join '|') + ')') }
        }
    }
    if ($unresolved.Count -gt 0) {
        Add-Violation $ascxRel 'C3' ('标记层引用了无法解析的类型（运行期会 CS0103）: ' + (($unresolved | Sort-Object -Unique) -join ', '))
    }
}
Write-Host ("[ascx] 已检查 ASCX {0} 个" -f $ascxFiles.Count)
# 步骤 3：C4 —— module.json 的 desktopEntry 与模块级唯一 class
# A3 的根因（实测）：旧内容模块**只有** portal- 前缀的 class（Contacts -> portal-content-table-wrap、
# QuickLinks -> portal-quicklinks），故"排除 portal- 前缀"的旧提取逻辑恰好把它们唯一标识也排除了。
# 但也不能直接取首个 div：Announcements 的首个 div 是 portal-content-list-item（**行级**，每条公告一个），
# Links 的 portal-content-link-row 同样行级。故判据取"在 ascx 中**只出现一次**的 class" —— 唯一即模块级。
$moduleRoots = @{}
foreach ($manifestFile in (Get-ChildItem (Join-Path $repoRoot 'src\Portal\DesktopModules') -Recurse -File -Filter 'module.json')) {
    $moduleDir = $manifestFile.DirectoryName
    try { $manifest = [System.IO.File]::ReadAllText($manifestFile.FullName, $utf8) | ConvertFrom-Json } catch { continue }
    $entry = [string]$manifest.desktopEntry
    if ([string]::IsNullOrWhiteSpace($entry)) { continue }
    $entryPath = Join-Path (Join-Path $repoRoot 'src\Portal') ($entry.Replace('/', '\'))
    $moduleRel = $moduleDir.Replace($repoRoot + '\', '')
    if (-not (Test-Path $entryPath -PathType Leaf)) {
        Add-Violation $moduleRel 'C4' ('module.json 的 desktopEntry 指向的文件不存在: ' + $entry)
        continue
    }
    $entryMarkup = [System.IO.File]::ReadAllText($entryPath, $utf8)
    $counts = @{}
    foreach ($cm in [regex]::Matches($entryMarkup, 'class="([^"]+)"')) {
        foreach ($tok in ($cm.Groups[1].Value -split '\s+')) {
            if (-not $tok -or $tok -like 'portal-module-header*' -or $tok -like 'portal-module-title*' -or $tok -like 'portal-module-actions*') { continue }
            if (-not $counts.ContainsKey($tok)) { $counts[$tok] = 0 }
            $counts[$tok]++
        }
    }
    $unique = @($counts.Keys | Where-Object { $counts[$_] -eq 1 } | Sort-Object)
    $usable = @($unique | Where-Object { $_ -notlike 'portal-content-list-item' -and $_ -notlike 'portal-content-link-row' })
    if ($usable.Count -gt 0) {
        $moduleRoots[$moduleRel] = '.' + $usable[0]
    } else {
        $moduleRoots[$moduleRel] = $null
        Write-Host ("[ascx] 提示：{0} 无可用模块级唯一 class（运行期门禁只能靠'未落错误页'断言）" -f $moduleRel)
    }
}

# 步骤 4：汇总
Write-Host ''
if ($violations.Count -eq 0) {
    Write-Host ("RESULT: Pass  （ASCX 编译契约：{0} 个标记文件，{1} 个模块清单，0 违规）" -f $ascxFiles.Count, $moduleRoots.Count)
    exit 0
}
Write-Host ("RESULT: Fail  （{0} 条违规）" -f $violations.Count)
foreach ($v in $violations) { Write-Host ("  [" + $v.rule + "] " + $v.file + "  " + $v.detail) }
exit 1