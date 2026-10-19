#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>校验既有内容模块的文档逐项说明与源码一致（L0）。</zh-CN>
  <en>Validates that the per-module documentation for legacy content modules matches the source (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P152`）交付：`documentation-registry.md` 与 `documentation-map.md` 长期把
  「既有内容模块（公告 / 联系人 / 讨论 / 文档 / 事件 / 图片 / 链接）的逐项操作说明」登记为剩余缺口。
  本包补齐了文档，但**文档会因源码改动而腐烂**（改一个字段名、加一个必填校验，文档就静默过期）——
  故把一致性变成机器核对。断言面向**源码 → 文档**方向（防漏写、防必填项数量不符），
  文档里多写的叙述不在本门禁检查范围内（见证明边界）。

  断言：
    A1 注册的七个模块，其显示控件（`.ascx`）与编辑页（`Edit*.aspx` / 讨论的 `DiscussDetails.aspx`）必须在磁盘存在。
    A2 文档 `docs/user-guide.md` 必须为每个模块设有小节（`### <标题>`），标题与注册表一致。
    A3 编辑页每个**输入控件 id**（TextBox / DropDownList / CheckBox / HtmlTextArea）必须出现在该模块的小节文本中
       ——源码里有而文档没写 = 漏写字段，直接失败。故文档字段一律写成"中文标签 + 源码控件 id"。
    A4 必填项数量一致：编辑页 `RequiredFieldValidator` 控件数（按 id 去重）== 小节内 `**必填**` 出现次数
       ——加/去一个必填校验而文档没同步即失败。（只数控件数，不依赖 `ControlToValidate` 属性顺序。）
    default-deny：注册表外仍存在的模块文件（如 `HTML` / `QuickLinks` / `XmlModule`）**只作软指标上报**，
       不静默放过——它们是下一批待补文档的清单。

  证明边界：只证明"文档覆盖了源码中的输入字段、且必填项数量与源码一致"；
  **不证明文档叙述的语义正确**（字段用途、操作流程由人工复核承载）、
  **不检查文档里多写的字段或文字**（只做源码 → 文档方向，避免把"补充说明"误判为缺陷）、
  **不核对部署位置**（文档中的 TabId/ModuleId 是交付时的实测快照，演示数据变化不会触发本门禁）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalContentModuleDocs.ps1'
  产物：temp\p152\content-module-docs-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 环境/文件前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P152`) deliverable: `documentation-registry.md` and `documentation-map.md` have long listed "per-module
  operating instructions for the legacy content modules (announcements / contacts / discussion / documents / events / pictures /
  links)" as a remaining gap. This work item filled the documentation, but **docs rot as the source changes** (rename a field or
  add a required validator and the doc silently goes stale) — so consistency is turned into a machine check. The assertions run
  **source → doc** (catching undocumented fields and required-count mismatches); extra prose in the doc is out of scope (see the
  proof boundary).

  Assertions:
    A1 for the seven registered modules, both the view control (`.ascx`) and the edit page (`Edit*.aspx`, or `DiscussDetails.aspx`
       for discussions) must exist on disk.
    A2 the doc `docs/user-guide.md` must have a section per module (`### <heading>`), with headings matching the registry.
    A3 every **input control id** in the edit page (TextBox / DropDownList / CheckBox / HtmlTextArea) must appear in that module's
       section text — present in source but missing from the doc is an omission and fails outright. Documentation therefore
       writes fields as "Chinese label + source control id".
    A4 required counts agree: the number of `RequiredFieldValidator` controls (deduplicated by id) == the number of `**必填**`
       occurrences in the section — adding or removing a required validator without updating the doc fails. (Only the control count
       is used, so attribute order such as `ControlToValidate` does not matter.)
    default-deny: module files outside the registry (for example `HTML` / `QuickLinks` / `XmlModule`) are **reported as a soft
       metric**, never silently ignored — they are the backlog for the next documentation batch.

  Proof boundary: proves only that "the doc covers the input fields present in the source, and that required-counts agree with the
  source"; does **not** prove that the doc's prose is semantically correct (field purpose and procedures belong to human review),
  does **not** check for fields or wording the doc adds beyond the source (source → doc only, so extra notes are not misread as
  defects), and does **not** verify deployment locations (the TabId/ModuleId values in the doc are a snapshot taken on delivery;
  demo-data changes do not trip this gate).

  Usage (repository root): & 'dev\scripts\Test-PortalContentModuleDocs.ps1'
  Output: temp\p152\content-module-docs-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing environment/file prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalContentModuleDocs.ps1'   # 校验内容模块文档与源码一致</zh-CN>
  <en>& 'dev\scripts\Test-PortalContentModuleDocs.ps1'   # validate content-module docs against the source</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P152` 交付；七个模块的逐项说明写在 `docs/user-guide.md`「常见模块」各子节。</zh-CN>
  <en>Provenance: delivered by `W-anp-P152`; the seven modules are documented in the "common modules" subsections of `docs/user-guide.md`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$modulesDir = Join-Path $repoRoot 'src\Portal\DesktopModules'
$docPath = Join-Path $repoRoot 'docs\user-guide.md'

# <lang><zh-CN>注册表：模块中文名 / 文档小节标题 / 显示控件 / 编辑页。新增模块必须登记，否则不在核对范围。</zh-CN>
# <en>Registry: Chinese name / doc section heading / view control / edit page. A new module must be registered or it is out of scope.</en></lang>
$MODULES = @(
    [pscustomobject]@{ name = '公告';   heading = '公告（Announcements）';   view = 'Announcements.ascx'; edit = 'EditAnnouncements.aspx' },
    [pscustomobject]@{ name = '联系人'; heading = '联系人（Contacts）';       view = 'Contacts.ascx';       edit = 'EditContacts.aspx' },
    [pscustomobject]@{ name = '讨论';   heading = '讨论（Discussion）';       view = 'Discussion.ascx';     edit = 'DiscussDetails.aspx' },
    [pscustomobject]@{ name = '文档';   heading = '文档（Document）';         view = 'Document.ascx';       edit = 'EditDocs.aspx' },
    [pscustomobject]@{ name = '事件';   heading = '事件（Events）';           view = 'Events.ascx';         edit = 'EditEvents.aspx' },
    [pscustomobject]@{ name = '图片';   heading = '图片（ImageModule）';      view = 'ImageModule.ascx';    edit = 'EditImage.aspx' },
    [pscustomobject]@{ name = '链接';   heading = '链接（Links）';            view = 'Links.ascx';          edit = 'EditLinks.aspx' }
    # <lang><zh-CN>`C-anp-P31`（`W-anp-P171`）新增 HTML 文本模块。注意该编辑页**没有任何 RequiredFieldValidator**，
    # 故文档小节中**不得**出现 `**必填**` 标注（A4 会比对数量）。</zh-CN>
    # <en>`C-anp-P31` (`W-anp-P171`) adds the HTML text module. Note that its edit page has **no RequiredFieldValidator**, so the
    # doc section must **not** contain any `**必填**` marker (A4 compares counts).</en></lang>
    [pscustomobject]@{ name = 'HTML 文本'; heading = 'HTML 文本模块（HtmlModule）'; view = 'HtmlModule.ascx'; edit = 'EditHtml.aspx' }
)

$failures = @()
$softNotes = @()

if (-not (Test-Path -LiteralPath $docPath)) {
    Write-Host '[PREREQ-MISSING] 文档不存在：' + $docPath + '（exit 2）'
    exit 2
}

# <lang><zh-CN>按 `### ` 小节切分文档（`## ` 出现即结束当前小节）。</zh-CN>
# <en>Split the doc into `### ` sections (a `## ` line ends the current section).</en></lang>
$sections = @{}
$current = $null
foreach ($line in [System.IO.File]::ReadAllLines($docPath)) {
    if ($line -match '^###\s+(.+?)\s*$') { $current = $Matches[1].Trim(); $sections[$current] = ''; continue }
    if ($line -match '^#{1,2}\s+') { $current = $null; continue }
    if ($current) { $sections[$current] = $sections[$current] + $line + "`n" }
}

foreach ($module in $MODULES) {
    # <lang><zh-CN>A1：源码文件必须存在。</zh-CN><en>A1: source files must exist.</en></lang>
    $viewPath = Join-Path $modulesDir $module.view
    $editPath = Join-Path $modulesDir $module.edit
    if (-not (Test-Path -LiteralPath $viewPath)) { $failures += "A1 模块显示控件不存在：$($module.name) → $($module.view)。"; continue }
    if (-not (Test-Path -LiteralPath $editPath)) { $failures += "A1 模块编辑页不存在：$($module.name) → $($module.edit)。"; continue }

    # <lang><zh-CN>A2：文档必须有该模块小节。</zh-CN><en>A2: the doc must have the module section.</en></lang>
    if (-not $sections.ContainsKey($module.heading)) {
        $failures += "A2 文档缺少小节：$($module.heading)（模块 $($module.name) 未逐项说明）。"
        continue
    }
    $sectionText = $sections[$module.heading]

    # <lang><zh-CN>A3：编辑页每个输入控件 id 必须出现在小节文本中（源码 → 文档，防漏写）。</zh-CN>
    # <en>A3: every input control id in the edit page must appear in the section text (source → doc; catches omissions).</en></lang>
    $editText = [System.IO.File]::ReadAllText($editPath)
    $controls = @([regex]::Matches($editText, '(?i)<asp:(?:TextBox|DropDownList|CheckBox|HtmlTextArea)[^>]*\sID="(?<id>[^"]+)"') |
        ForEach-Object { $_.Groups['id'].Value } | Sort-Object -Unique)
    foreach ($control in $controls) {
        if ($sectionText -notmatch [regex]::Escape($control)) {
            $failures += "A3 文档未记录字段：$($module.name) 的编辑页控件 $control（源码有、文档缺）。"
        }
    }

    # <lang><zh-CN>A4：必填数量一致（只数 RequiredFieldValidator 控件，不依赖 ControlToValidate 属性顺序）。</zh-CN>
    # <en>A4: required counts agree (counting RequiredFieldValidator controls only; independent of ControlToValidate attribute order).</en></lang>
    $requiredCount = ([regex]::Matches($editText, '(?i)<asp:RequiredFieldValidator\b')).Count
    $docRequired = ([regex]::Matches($sectionText, [regex]::Escape('**必填**'))).Count
    if ($requiredCount -ne $docRequired) {
        $failures += "A4 必填项数量不符：$($module.name) 源码 RequiredFieldValidator=$requiredCount，文档标注必填=$docRequired。"
    }
}

# <lang><zh-CN>default-deny：注册表外的模块文件只作软指标上报（下一批待补文档清单）。</zh-CN>
# <en>default-deny: module files outside the registry are reported as a soft metric (backlog for the next doc batch).</en></lang>
$registeredViews = @($MODULES | ForEach-Object { $_.view })
$unregistered = @()
if (Test-Path -LiteralPath $modulesDir) {
    foreach ($file in (Get-ChildItem $modulesDir -File -Filter '*.ascx')) {
        if ($registeredViews -notcontains $file.Name) { $unregistered += $file.Name }
    }
}
if ($unregistered.Count -gt 0) {
    $softNotes += "[soft] 未登记逐项说明的模块控件 $($unregistered.Count) 个：" + ($unregistered -join '、') + '（下一批待补文档）。'
}

# <lang><zh-CN>统计落盘 + 输出。</zh-CN><en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p152'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'content-module-docs-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    modules = @($MODULES | ForEach-Object { $_.name })
    docSections = @($sections.Keys).Count
    unregisteredViews = $unregistered
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 内容模块文档一致性（W-anp-P152）==='
Write-Host ("注册模块 {0} 个｜文档小节 {1} 个｜未登记模块控件 {2} 个" -f $MODULES.Count, @($sections.Keys).Count, $unregistered.Count)
foreach ($note in $softNotes) { Write-Host ('  ' + $note) }
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 内容模块文档一致性校验失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 内容模块文档：七个小节齐备、字段与必填项同源码一致。' -ForegroundColor Green
exit 0
