<#
.SYNOPSIS
<lang>
  <zh-CN>机械提取 `src/Portal/DesktopModules/` 下**较新模块**（目录式、单 `.ascx`）的源码事实，供模块使用说明页撰写使用。</zh-CN>
  <en>Mechanically extracts source facts for the **newer modules** under `src/Portal/DesktopModules/` (directory-style, single
  `.ascx`) for use when writing module usage pages.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    **为什么需要**（`C-anp-P37` / `W-anp-P206`）：`C-anp-P33` 为 11 个经典模块（平铺 `.ascx`）建了独立使用说明页
    并登记进 `dev/documentation/module-doc-pages.json`。实测发现 `DesktopModules/` 下另有 **8 个目录式较新模块**
    （各含 1 个 `.ascx`），它们**既无文档页也未登记**——因为没登记，模块文档门禁的 M1「已交付模块必须有页」
    对它们不适用，**L0 全绿并不代表覆盖完整**。

    本脚本做的正是 `C-anp-P33` 那次盘点所确立的纪律：**字段、必填、校验、权限、存储一律机械取自源码**，
    不凭记忆写文档。提取结果落盘 JSON，可复跑、可 diff。

    **刻意的两条选择**：
    1. **不猜"模块是干什么的"**——只把标记层 `<lang>` 双语注释里的自我描述抽出来作为"模块自述"，
       判定是否用户面向留给人（本脚本只输出证据，不下结论）；
    2. **输出"有没有独立编辑页"这一事实**：这批模块是**单 ascx 自含表单与列表**，没有 `Edit*.aspx`。
       模块文档门禁对空 `edit` 的处理是 M2/M4 不适用、但页面必须写明「无编辑页」——
       所以这个事实直接决定页面该怎么写。
  </zh-CN>
  <en>
    **Why this exists** (`C-anp-P37` / `W-anp-P206`): `C-anp-P33` built standalone usage pages for the 11 classic modules
    (flat `.ascx`) and registered them in `dev/documentation/module-doc-pages.json`. Investigation found **8 directory-style
    newer modules** under `DesktopModules/` (one `.ascx` each) that have **neither a page nor a registry entry** — because
    they are unregistered, the module gate's M1 ("a delivered module must have a page") does not apply to them, so
    **a fully green L0 does not mean coverage is complete**.

    This script follows the discipline `C-anp-P33` established: **fields, required flags, validation, permissions and storage
    are all taken mechanically from source**, never written from memory. The result is written to JSON so it can be re-run
    and diffed.

    **Two deliberate choices**: (1) it never guesses "what the module is for" — it only lifts the self-description out of the
    markup's bilingual `<lang>` comment as the module's own words, leaving the user-facing judgement to a human (this script
    emits evidence, not verdicts); (2) it reports **whether a standalone edit page exists** — these modules are a **single
    ascx holding both the form and the list**, with no `Edit*.aspx`. The module gate treats an empty `edit` by skipping M2/M4
    but requiring the page to state "no edit page", so this fact directly determines how the page must be written.
  </en>
</lang>

.PARAMETER OutPath
<lang>
  <zh-CN>输出 JSON 路径（默认 `dev/documentation/new-module-facts.json`）。</zh-CN>
  <en>Output JSON path (defaults to `dev/documentation/new-module-facts.json`).</en>
</lang>
#>
[CmdletBinding()]
param(
    [string] $OutPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = [System.Text.UTF8Encoding]::new($false)
$modulesDir = Join-Path $repoRoot 'src\Portal\DesktopModules'
if (-not $OutPath) { $OutPath = Join-Path $repoRoot 'dev\documentation\new-module-facts.json' }

$controlPattern = '<asp:(TextBox|DropDownList|CheckBox|HtmlTextArea|FileUpload|GridView|Button|LinkButton|Literal|Label|Panel|RequiredFieldValidator|RegularExpressionValidator|RangeValidator|CompareValidator|CustomValidator)\b[^>]*>'
$idPattern = '\bID="([^"]+)"'
$validatorPattern = '<asp:(RequiredFieldValidator|RegularExpressionValidator|RangeValidator|CompareValidator)\b'

$results = @()
foreach ($dir in (Get-ChildItem -LiteralPath $modulesDir -Directory | Sort-Object Name)) {
    $ascx = @(Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Filter '*.ascx')
    if ($ascx.Count -eq 0) { continue }
    $view = $ascx[0]
    $viewRel = $view.FullName.Substring($repoRoot.Length + 1) -replace '\\', '/'
    $markup = [System.IO.File]::ReadAllText($view.FullName, $utf8)

    $controls = @()
    foreach ($m in [regex]::Matches($markup, $controlPattern)) {
        $tag = $m.Value
        $kind = ([regex]::Match($tag, '<asp:(\w+)')).Groups[1].Value
        $id = ([regex]::Match($tag, $idPattern)).Groups[1].Value
        if ($id) { $controls += [pscustomobject]@{ id = $id; kind = $kind } }
    }
    $validatorCount = ([regex]::Matches($markup, $validatorPattern)).Count

    # 模块自述：抽标记层 <lang> 注释里的 zh-CN / en 首段（模块自己怎么说，不猜）
    $commentBlock = ([regex]::Match($markup, '(?s)<%--(.*?)--%>')).Groups[1].Value
    $selfZh = ''
    $selfEn = ''
    if ($commentBlock) {
        $zh = ([regex]::Match($commentBlock, '(?s)<zh-CN>(.*?)</zh-CN>'))
        if ($zh.Success) { $selfZh = (($zh.Groups[1].Value -replace '\s+', ' ').Trim()) }
        $en = ([regex]::Match($commentBlock, '(?s)<en>(.*?)</en>'))
        if ($en.Success) { $selfEn = (($en.Groups[1].Value -replace '\s+', ' ').Trim()) }
    }

    # 编辑页：目录式模块没有 Edit*.aspx，表单与列表都在同一个 ascx 里
    $editPage = @(Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Filter 'Edit*.aspx')
    $hasEditPage = ($editPage.Count -gt 0)

    # code-behind 里的事实线索：权限判定 / 数据库访问 / 审计
    $codeBehind = @(Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Filter '*.ascx.cs')
    $permHits = @(); $dbHits = @(); $auditHits = @()
    foreach ($cb in $codeBehind) {
        $code = [System.IO.File]::ReadAllText($cb.FullName, $utf8)
        foreach ($m in [regex]::Matches($code, '(?m)^\s*.*?(HasEditPermissions|HasViewPermissions|IsInRole|PortalSecurity\.\w+).*?$')) { $permHits += $m.Value.Trim() }
        foreach ($m in [regex]::Matches($code, '(?m)^\s*.*?(SqlHelper|ConfigurationManager|ConnectionString|Execute\w+|存储过程|StoredProcedure).*?$')) { $dbHits += $m.Value.Trim() }
        foreach ($m in [regex]::Matches($code, '(?m)^\s*.*?(OperationAudit|审计|Audit).*?$')) { $auditHits += $m.Value.Trim() }
    }

    $results += [pscustomobject]@{
        directory      = $dir.Name
        view           = ($viewRel -replace '^src/Portal/DesktopModules/', '')
        hasEditPage    = $hasEditPage
        editPages      = @($editPage | ForEach-Object { $_.Name })
        controlCount   = @($controls).Count
        controls       = @($controls | Sort-Object id -Unique)
        validatorCount = $validatorCount
        selfDescriptionZh = $(if ($selfZh.Length -gt 400) { $selfZh.Substring(0, 400) } else { $selfZh })
        selfDescriptionEn = $(if ($selfEn.Length -gt 400) { $selfEn.Substring(0, 400) } else { $selfEn })
        permissionHints = @($permHits | Sort-Object -Unique | Select-Object -First 8)
        dataAccessHints = @($dbHits | Sort-Object -Unique | Select-Object -First 8)
        auditHints     = @($auditHits | Sort-Object -Unique | Select-Object -First 8)
    }
}

$payload = [ordered]@{
    schemaVersion   = 1
    generatedUtc    = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    producedBy      = 'C-anp-P37 / W-anp-P206（dev/scripts/Get-PortalNewModuleFacts.ps1）'
    sourceRoot      = 'src/Portal/DesktopModules'
    note            = '字段/必填/权限/存储一律机械取自源码；「是否用户面向」不下结论，只给证据。'
    moduleCount     = @($results).Count
    modules         = @($results)
}
[System.IO.File]::WriteAllText($OutPath, ($payload | ConvertTo-Json -Depth 6), $utf8)
Write-Host ('[OK] 已提取 ' + @($results).Count + ' 个较新模块的源码事实 → ' + $OutPath)
foreach ($r in $results) {
    Write-Host ('     ' + $r.directory.PadRight(36) + ' 控件 ' + $r.controlCount.ToString().PadLeft(3) +
        '｜校验器 ' + $r.validatorCount.ToString().PadLeft(2) +
        '｜独立编辑页 ' + $(if ($r.hasEditPage) { '有' } else { '无' }))
}
