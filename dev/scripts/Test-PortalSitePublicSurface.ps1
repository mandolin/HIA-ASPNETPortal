<#
.SYNOPSIS
  <lang>
    <zh-CN>校验 `site/`（官网源与生成产物）**公开面**不含私有内容外泄。</zh-CN>
    <en>Validates that the `site/` public surface (website source and generated output) leaks no private content.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>**为什么要单独一个门禁**：既有 `Test-PortalPublicDocumentation.ps1` 只扫描 `docs/*.md`，
    **不覆盖 `site/`**。而 `site/` 是**将被发布到公开官网**的目录，且它包含生成产物与翻译源 ——
    一旦把内部链接、内部计划路径或凭据形态发布出去，就是对外事故，且不像代码缺陷那样能被本地测试发现。
    故本门禁专守发布面。

    **四项检查**：

    | 项 | 内容 |
    | --- | --- |
    | **S1** | 无私有仓库链接（`work-zone` / `.serena`） |
    | **S2** | 无凭据赋值形态（`password` / `secret` / `token` / `apikey` / 连接串键 等后接 `:` 或 `=`） |
    | **S3** | 无意外绝对本地路径（盘符形式，如 `K:\`） |
    | **S4** | 无内部计划 / ADR 路径引用（`work-zone/dev/plans`、`work-zone/docs/adr`） |

    **报告而不暴露**：与既有文档门禁同一口径 —— 命中时**只报告文件与行号**，**不打印匹配内容**，
    避免"为了证明没有问题而把疑似敏感片段写进构建日志"。

    **证明边界（重要）**：本门禁**不证明**站点内容正确、译本等价或链接可达
    （这些分别由 `Build-PortalSite.ps1` 的 N1/N2 与人工走查负责）。它只守"公开面不得含私有内容"这一条。
    它也无法识别**语义上**敏感却不含上述模式的文字（如口头描述的内部架构决策），那仍需人工走查。

    **前置条件**：只读 `site/` 下 Markdown 文本，属编排入口 L0 层；不联网、不读凭据、不启动 IIS。
    **验证方式**：退出码 0 = 公开面无上述四类内容；1 = 存在命中，逐条列出"文件:行号"。
    **低敏边界**：只做模式匹配；日志不输出命中文本。</zh-CN>
    <en>**Why a separate gate**: the existing `Test-PortalPublicDocumentation.ps1` scans only `docs/*.md` and **does not cover
    `site/`**. Yet `site/` is the directory **published to the public website**, and it holds generated output plus
    translation sources — publishing an internal link, an internal plan path, or a credential shape is an external
    incident, and unlike a code defect it is not caught by local tests. This gate guards the publication surface.

    **Four checks**:

    | Item | Content |
    | --- | --- |
    | **S1** | No private-repository links (`work-zone` / `.serena`) |
    | **S2** | No credential assignment shape (`password` / `secret` / `token` / `apikey` / connection-string keys followed by `:` or `=`) |
    | **S3** | No unexpected absolute local paths (drive-letter form such as `K:\`) |
    | **S4** | No internal plan / ADR path references (`work-zone/dev/plans`, `work-zone/docs/adr`) |

    **Report, do not expose**: same convention as the existing documentation gate — a hit reports **file and line number
    only** and **never prints the matched text**, so a suspected sensitive fragment is not written into the build log just
    to prove nothing is wrong.

    **Proof boundary (important)**: this gate does **not** prove that site content is correct, that translations are
    equivalent, or that links resolve (those belong to `Build-PortalSite.ps1` N1/N2 and to the human walkthrough). It guards
    only one rule: "the public surface must not carry private content". It also cannot detect text that is **semantically**
    sensitive yet matches none of the patterns (e.g. a prose description of an internal architecture decision) — that still
    needs a human walkthrough.

    **Prerequisites**: reads only Markdown text under `site/` — an L0-layer entry point; no network, no credentials, no IIS.
    **Verification**: exit code 0 = none of the four categories found; 1 = hits exist, listed as "file:line".
    **Low-sensitivity boundary**: pattern matching only; the log never emits the matched text.</en>
  </lang>
.PARAMETER RepoRoot
  <lang>
    <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` → 仓库根）。</zh-CN>
    <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
  </lang>
#>
param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$siteDir = Join-Path $RepoRoot 'site'

if (-not (Test-Path -LiteralPath $siteDir)) {
    Write-Host ('[site-surface] site/ 不存在，无需检查。')
    Write-Host 'RESULT: Pass'
    exit 0
}

# <lang>
#   <zh-CN>四类模式。S2 覆盖常见凭据键名后接 `:`/`=`；S3 覆盖盘符绝对路径；
#     S1/S4 覆盖私有仓库与内部计划路径。刻意**不打印命中文本**。</zh-CN>
# </lang>
# <lang>
#   <zh-CN>**S1 只查链接形态**（`W-anp-P133` 口径修正）：初版用裸词 `(?i)work-zone` 匹配，会把
#     "内部资料留在私有仓库 `work-zone/`" 这类**纯文本提及**也判为违规。但既有
#     `Test-PortalPublicDocumentation.ps1` 只检查**链接目标**（`$rawTarget`），同一内容在 `docs/` 是被放行的 ——
#     若此处更严，就会出现"同一事实两个门禁给出矛盾结论"（正是 `W-anp-P126` 批评过的口径不一致）。
#     故 S1 收窄为**链接或 URL 中含私有仓库名**；纯文本提及由内容策略处理，已登记待办。
#     S4 保留路径形态（比仓库名更具体，指向内部计划/ADR 的实际路径，当前无命中）。</zh-CN>
# </lang>
$patterns = @(
    [pscustomobject]@{ Id = 'S1'; Name = '私有仓库链接'; Regex = '(?i)(\]\([^)]*(work-zone|\.serena)|https?://[^\s)]*(work-zone|\.serena))' },
    [pscustomobject]@{ Id = 'S2'; Name = '凭据赋值形态'; Regex = '(?i)(password|passwd|pwd|secret|token|api[_-]?key|connectionstring|conn[_-]?str)\s*[:=]' },
    [pscustomobject]@{ Id = 'S3'; Name = '绝对本地路径'; Regex = '[A-Za-z]:\\' },
    # <lang>
    #   <zh-CN>S4 扩大（`W-anp-P137`）：初版只匹配 `work-zone/dev/plans` 与 `work-zone/docs/adr` 两个子路径。
    #     系统扫描 `docs/` 后发现 **16 处** `work-zone/` 路径提及分布在 **9 个**文档里，形态远不止这两个
    #     （如 `work-zone/notify/`、`work-zone\dev\evidence\p13.3\*.json`、`work-zone/dev/plans/C-anp-P10.md`）。
    #     只要任何一个被上架，就会把私有仓库的内部路径发布到公开官网，而旧 S4 **抓不到**。
    #     现改为匹配 `work-zone` 后接分隔符的**任意路径形态**（链接或纯文本均是）——
    #     这样"待上架文档含内部路径"会在**上架时**被拦住，而不是等发布后靠人发现。
    #     注意：本检查只针对 `site/`（发布面），不动 `docs/`；`docs/` 里的同类提及按批次随上架清理。</zh-CN>
    # </lang>
    [pscustomobject]@{ Id = 'S4'; Name = '内部计划/证据/ADR 路径'; Regex = '(?i)work-zone\s*[\\/]' }
)

$files = @(git -C $RepoRoot ls-files 'site' | Where-Object { $_ -like '*.md' } | ForEach-Object { Join-Path $RepoRoot $_ })
$violations = New-Object 'System.Collections.Generic.List[string]'

foreach ($f in $files) {
    if (-not (Test-Path -LiteralPath $f)) { continue }
    $rel = $f.Substring($RepoRoot.Length + 1).Replace('\', '/')
    $lines = [System.IO.File]::ReadAllText($f, $utf8) -split "`r?`n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($p in $patterns) {
            if ($lines[$i] -match $p.Regex) {
                $violations.Add(('{0} {1} :: {2}:{3}' -f $p.Id, $p.Name, $rel, ($i + 1)))
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host ('[site-surface] 公开面校验失败（共 ' + $violations.Count + ' 处）：')
    foreach ($v in $violations) { Write-Host ('  - ' + $v) }
    Write-Host '[GATE-NOTE] 报告而不暴露：只列文件与行号，不打印命中文本。'
    Write-Host 'RESULT: Fail'
    exit 1
}

Write-Host ('[site-surface] 站点公开面校验通过：检查 ' + $files.Count + ' 个 Markdown 文件，无私有仓库链接 / 凭据形态 / 绝对本地路径 / 内部计划路径。')
Write-Host '[GATE-NOTE] 报告而不暴露：命中时只列文件与行号，不打印命中文本。'
Write-Host '[GATE-NOTE] 证明边界：只守"公开面不含私有内容"，不证明内容正确、译本等价或链接可达（后两者归 Build-PortalSite 的 N1/N2 与人工走查）。'
Write-Host 'RESULT: Pass'
exit 0
