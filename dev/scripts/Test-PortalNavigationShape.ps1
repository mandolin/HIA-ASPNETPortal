<#
.SYNOPSIS
<lang>
  <zh-CN>导航形态一致性门禁：校验建库导航种子（`src/Setup/Portal_NavigationSeed.sql`）定义的一级导航，与公开文档《第一次使用本站》（`docs/first-use-portal.md`）描述的页签集合、顺序与可见性逐项一致。</zh-CN>
  <en>Navigation-shape consistency gate: verifies that the first-level navigation defined by the database seed (`src/Setup/Portal_NavigationSeed.sql`) matches, item by item, the tabs, order, and visibility described in the public "First time on this portal" page (`docs/first-use-portal.md`).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>**为什么需要它**：`C-anp-P17` 遗留的「快速上手真实可用」长期无法收敛，根因不是文档写错，而是**文档描述的产品形态与可复制最小安装（建库种子）从未被任何门禁同时约束**——文档改了没人验证，种子改了也没人验证，两者各自漂移。2026-11-03 的走查实测发现：文档写「匿名两个页签、登录后六个」，而种子给的是入门套件的六个演示页签，新装门户与文档完全对不上。
  **验证方式**：解析种子中的目标页签（按 `TabOrder` 排序并按页签名去重），与文档三处叙述比对——匿名可见项、登录后六项（含顺序）、管理员额外两项；任一不一致即 `RESULT: Fail` 并以退出码 1 结束。
  **前置条件**：仓库内存在上述两个文件；本脚本不连接数据库、不访问网络、不写任何文件。
  **低敏边界**：只读取页签名与角色串，不读取凭据、连接串或业务数据。</zh-CN>
  <en>**Why this gate exists**: the `C-anp-P17` leftover "quick start really works" could not converge because the root cause was never a wrong sentence — it was that **the product shape described by the document and the reproducible minimum install (the database seed) were never constrained by the same gate**. The document could change unverified and so could the seed, each drifting on its own. The 2026-11-03 walkthrough measured the result: the document promised "two tabs when anonymous, six after sign-in" while the seed shipped the starter kit's six demo tabs, so a freshly installed portal looked nothing like the document.
  **Verification**: parse the seed's target tabs (ordered by `TabOrder`, de-duplicated by tab name) and compare them against three passages in the document — the anonymous items, the six signed-in items (including order), and the two administrator-only items; any mismatch yields `RESULT: Fail` and exit code 1.
  **Prerequisites**: both files exist in the repository; this script does not touch a database, use the network, or write any file.
  **Low-sensitivity boundary**: only tab names and role strings are read — no credentials, connection strings, or business data.</en>
</lang>
.PARAMETER RepoRoot
<lang>
  <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` 的上两级即仓库根）。</zh-CN>
  <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
</lang>

  证明边界：只证明「种子里的目标导航」与「中文快速上手指南的三处叙述」在页签名、顺序与可见性上一致；不证明数据库里实际就是这样（由运行期走查与模块装载门禁承载），不证明英文版叙述一致（由站点译本新鲜度门禁承载），也不判断页签集合本身是否合理（产品决策）。

  Proof boundary: proves only that the seed's target navigation and the three passages of the Chinese quick-start page agree on tab names, order, and visibility; it does not prove the live database really looks like this (the runtime walkthrough and module mounting gates cover that), does not prove the English page agrees (the site translation freshness gate covers that), and does not judge whether the tab set itself is a good product decision.
#>

param(
    # <lang><zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（dev\scripts 的上两级即仓库根）。</zh-CN><en>Repository root; when empty, two levels above the script directory (dev\scripts -> repository root).</en></lang>
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

# <lang>
#   <zh-CN>按 UTF-8（无 BOM）读取文本的辅助函数，避免中文页签名在解析时被平台默认编码破坏。</zh-CN>
#   <en>Reads text as UTF-8 without BOM so Chinese tab names are not mangled by the platform default encoding.</en>
# </lang>
function Read-Utf8Text {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
}

$problems = @()

$seedPath = Join-Path $RepoRoot 'src/Setup/Portal_NavigationSeed.sql'
$docPath = Join-Path $RepoRoot 'docs/first-use-portal.md'

foreach ($required in @($seedPath, $docPath)) {
    if (-not (Test-Path -LiteralPath $required)) {
        Write-Output 'RESULT: Fail'
        Write-Output ('  - 缺少必需文件：' + $required)
        exit 1
    }
}

$seedText = Read-Utf8Text -Path $seedPath
$docText = Read-Utf8Text -Path $docPath

# <lang>
#   <zh-CN>从种子的目标表取值行中解析页签：每行形如 `(N'NavKey', N'页签名', N'English', N'移动名', 顺序, N'角色', ...)`。
#   只取前五个位置参数——后续列是模块信息，与本门禁无关。</zh-CN>
#   <en>Parse tabs from the seed's target-table value rows; each row looks like `(N'NavKey', N'tab', N'English', N'mobile', order, N'roles', ...)`.
#   Only the first five positional values are read — later columns describe modules and do not matter here.</en>
# </lang>
$rowPattern = "\(N'(?<nav>[^']*)',\s*N'(?<tab>[^']*)',\s*N'(?<en>[^']*)',\s*N'(?<mobile>[^']*)',\s*(?<order>\d+),\s*N'(?<roles>[^']*)'"
$seedRows = [regex]::Matches($seedText, $rowPattern)
if ($seedRows.Count -eq 0) {
    $problems += '导航种子中未解析到任何目标页签（@Target 取值行格式变化或文件被改坏）'
}

$seedTabs = foreach ($m in $seedRows) {
    [pscustomobject]@{
        Order = [int]$m.Groups['order'].Value
        Name  = $m.Groups['tab'].Value
        En    = $m.Groups['en'].Value
        Roles = $m.Groups['roles'].Value
    }
}

# <lang><zh-CN>按顺序排序并按页签名去重：同一页签承载多个模块时会出现多行，顺序取首次出现。</zh-CN><en>Sort by order and de-duplicate by tab name: a tab hosting several modules appears on several rows, and its order is the first occurrence.</en></lang>
$orderedNames = @()
$orderedRoles = @{}
foreach ($row in ($seedTabs | Sort-Object -Property Order)) {
    if ($orderedNames -notcontains $row.Name) {
        $orderedNames += $row.Name
        $orderedRoles[$row.Name] = $row.Roles
    }
}

# <lang><zh-CN>文档侧解析：登录后六项取自「登录后左侧有什么」与「看我的待办」之间的 `- 名称 —— 说明` 列表。</zh-CN><en>Document side: the six signed-in items come from the `- name -- description` list between the "after sign-in" and "my work items" sections.</en></lang>
$signedSection = [regex]::Match($docText, '(?s)##\s*登录后左侧有什么(.*?)##\s*看我的待办')
if (-not $signedSection.Success) {
    $problems += '文档中未找到「登录后左侧有什么」到「看我的待办」之间的叙述（小节标题被改动）'
    $docSigned = @()
}
else {
    $docSigned = @([regex]::Matches($signedSection.Groups[1].Value, '(?m)^-\s*([^\s—]+)\s*——') | ForEach-Object { $_.Groups[1].Value })
}

# <lang><zh-CN>匿名两项：文档原句「左侧只有「首页」和「帮助」两个页签」。</zh-CN><en>Anonymous pair: the document's sentence "only two tabs, Home and Help, on the left".</en></lang>
$anonMatch = [regex]::Match($docText, '左侧只有「(?<a>[^」]+)」和「(?<b>[^」]+)」两个页签')
$docAnonymous = @()
if ($anonMatch.Success) {
    $docAnonymous = @($anonMatch.Groups['a'].Value, $anonMatch.Groups['b'].Value)
}
else {
    $problems += '文档中未找到「左侧只有「A」和「B」两个页签」句式（匿名可见性叙述被改动）'
}

# <lang><zh-CN>管理员两项：文档原句「管理员还会多两项：平台管理（…）和企业能力工作台（…）」。</zh-CN><en>Administrator pair: the document's sentence "administrators get two more: Platform Admin (...) and Enterprise Capability Workbench (...)".</en></lang>
$adminMatch = [regex]::Match($docText, '管理员还会多两项：(?<a>[^（]+)（[^）]*）和(?<b>[^（]+)（')
$docAdmin = @()
if ($adminMatch.Success) {
    $docAdmin = @($adminMatch.Groups['a'].Value.Trim(), $adminMatch.Groups['b'].Value.Trim())
}
else {
    $problems += '文档中未找到「管理员还会多两项：…和…」句式（管理员项叙述被改动）'
}

# <lang><zh-CN>比对一：登录后六项的名称与顺序必须与种子完全一致（顺序即导航顺序，错序同样是缺陷）。</zh-CN><en>Check 1: the six signed-in items must match the seed by name and order — order is navigation order, and wrong order is a defect too.</en></lang>
$seedAllUsers = @($orderedNames | Where-Object { $orderedRoles[$_] -match 'All Users' })
if ($docSigned.Count -gt 0) {
    if ($seedAllUsers.Count -ne $docSigned.Count) {
        $problems += ('登录后页签数量不一致：种子 ' + $seedAllUsers.Count + ' 项（' + ($seedAllUsers -join '/') + '），文档 ' + $docSigned.Count + ' 项（' + ($docSigned -join '/') + '）')
    }
    for ($i = 0; $i -lt [Math]::Min($seedAllUsers.Count, $docSigned.Count); $i++) {
        if ($seedAllUsers[$i] -ne $docSigned[$i]) {
            $problems += ('登录后第 ' + ($i + 1) + ' 项不一致：种子「' + $seedAllUsers[$i] + '」，文档「' + $docSigned[$i] + '」')
        }
    }
}

# <lang><zh-CN>比对二：匿名可见项必须是种子中「All Users」组的前两项且顺序一致——文档承诺匿名只看得到这两项。</zh-CN><en>Check 2: the anonymous items must be the first two entries of the seed's All Users group in that order — the document promises anonymous visitors see exactly those two.</en></lang>
if ($docAnonymous.Count -eq 2) {
    $expectedAnon = @($seedAllUsers | Select-Object -First 2)
    for ($i = 0; $i -lt 2; $i++) {
        if ($expectedAnon[$i] -ne $docAnonymous[$i]) {
            $problems += ('匿名可见第 ' + ($i + 1) + ' 项不一致：种子「' + $expectedAnon[$i] + '」，文档「' + $docAnonymous[$i] + '」')
        }
    }
}

# <lang><zh-CN>比对三：管理员专属项（种子中角色为 Admins 的页签）必须与文档的两项一致，集合比较不比较顺序。</zh-CN><en>Check 3: administrator-only tabs (seed rows whose roles are Admins) must equal the document's two items; this is a set comparison, not an order one.</en></lang>
$seedAdmin = @($orderedNames | Where-Object { $orderedRoles[$_] -match 'Admins' })
if ($docAdmin.Count -eq 2) {
    $missing = @($docAdmin | Where-Object { $seedAdmin -notcontains $_ })
    $extra = @($seedAdmin | Where-Object { $docAdmin -notcontains $_ })
    if ($missing.Count -gt 0) {
        $problems += ('管理员项在种子中缺失：' + ($missing -join '、'))
    }
    if ($extra.Count -gt 0) {
        $problems += ('种子中多出文档未提及的管理员项：' + ($extra -join '、'))
    }
}

# <lang><zh-CN>比对四：文档里出现的每个页签名都必须能在种子里找到，防止文档加了入口而产品没有对应页签。</zh-CN><en>Check 4: every tab name mentioned by the document must exist in the seed, so the document cannot promise an entry the product does not have.</en></lang>
foreach ($name in @($docSigned + $docAnonymous)) {
    if ($name -and ($orderedNames -notcontains $name)) {
        $problems += ('文档提到的页签在种子中不存在：「' + $name + '」')
    }
}

if ($problems.Count -gt 0) {
    Write-Output 'RESULT: Fail'
    foreach ($problem in $problems) {
        Write-Output ('  - ' + $problem)
    }
    exit 1
}

Write-Output ('种子目标页签（' + $orderedNames.Count + ' 项）：' + ($orderedNames -join ' / '))
Write-Output ('文档匿名项：' + ($docAnonymous -join ' / ') + '；登录后六项：' + ($docSigned -join ' / ') + '；管理员项：' + ($docAdmin -join ' / '))
Write-Output 'RESULT: Pass'
exit 0
