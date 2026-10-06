<#
.SYNOPSIS
<lang>
  <zh-CN>通用密钥与凭据泄露扫描门禁。只读扫描 Git 已追踪文件中的高置信度密钥形态。</zh-CN>
  <en>Generic secret and credential leakage gate. Read-only scan of Git-tracked files for high-confidence secret shapes.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>为什么需要本门禁（缺口实测）：现有 Test-PortalDefaultCredentialRisk.ps1 只覆盖"默认凭据风险"
  这一类语义，Test-PortalPublicDocumentation.ps1 只覆盖公开 Markdown 的凭据赋值形态。两者都不检查
  私钥块、云厂商密钥、访问令牌等高置信度密钥形态 —— 一旦有人把私钥或 AKIA 开头的访问键提交进仓库，
  现有门禁全部会通过。</zh-CN>
  <en>Why this gate exists (measured gap): the existing Test-PortalDefaultCredentialRisk.ps1 only covers the
  "default credential risk" category, and Test-PortalPublicDocumentation.ps1 only covers credential-assignment shapes in
  public Markdown. Neither inspects private key blocks, cloud access keys or access tokens — so if someone commits a private
  key or an AKIA-prefixed access key, every existing gate still passes.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>设计取舍：只扫**高置信度**形态，不做通用的"password=..." 匹配。通用匹配在本仓库必然误报
  （连接串模板、SQL 初始化脚本、测试固件、门禁脚本自身的正则定义都含有类似字面量），一个必然误报的
  门禁会被整体忽略，那比没有门禁更糟。通用凭据赋值形态由 Test-PortalPublicDocumentation.ps1 负责。</zh-CN>
  <en>Design trade-off: only **high-confidence** shapes are scanned; generic "password=..." matching is deliberately avoided. Such
  matching would inevitably produce false positives in this repository (connection string templates, SQL initialization scripts,
  test fixtures, and the gate scripts' own regular expressions all contain similar literals), and a gate that always fires gets
  ignored entirely, which is worse than having no gate. Generic credential-assignment shapes remain the responsibility of
  Test-PortalPublicDocumentation.ps1.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>只扫 Git 已追踪文件：不扫未追踪的本地临时文件、构建产物与依赖目录，避免开发期噪声。</zh-CN>
  <en>Only Git-tracked files are scanned: untracked local scratch files, build outputs and dependency directories are skipped to
  avoid development-time noise.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>报告只输出文件路径、模式名与行号，**不输出匹配正文** —— 否则门禁日志本身就会成为新的泄露源。</zh-CN>
  <en>The report outputs only the file path, the pattern name and the line number, **never the matched text** — otherwise the gate
  log itself becomes a new leakage source.</en>
</lang>

.PARAMETER RootPath
<lang>
  <en>Repository root. Defaults to the parent of the scripts directory.</en>
  <zh-CN>仓库根路径，默认取脚本目录的上级。</zh-CN>
</lang>

.PARAMETER OutputJson
<lang>
  <en>Optional JSON evidence output path.</en>
  <zh-CN>可选 JSON 证据输出路径。</zh-CN>
</lang>

.PARAMETER FailOnWarning
<lang>
  <en>Treats warnings as a failed gate for stricter security runs.</en>
  <zh-CN>在更严格的安全运行中将 Warning 视为门禁失败。</zh-CN>
</lang>
#>
[CmdletBinding()]
param(
    [string]$RootPath,

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = if ([string]::IsNullOrWhiteSpace($RootPath)) {
    Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
else {
    [System.IO.Path]::GetFullPath($RootPath)
}

# <lang>
#   <zh-CN>仅扫描这些扩展名的文本文件。二进制文件（图片、字体、程序集）不可能承载可读密钥，跳过它们
#   既避免误报也避免把二进制读成乱码后产生假匹配。</zh-CN>
#   <en>Only text files with these extensions are scanned. Binary files (images, fonts, assemblies) cannot carry a readable
#   secret, so skipping them avoids both false positives and garbled decoding producing fake matches.</en>
# </lang>
$textExtensions = @(
    '.cs', '.config', '.json', '.md', '.sql', '.xml', '.aspx', '.ascx', '.master',
    '.js', '.css', '.less', '.yml', '.yaml', '.ps1', '.txt', '.props', '.targets',
    '.sln', '.ini', '.cmd', '.bat', '.psd1', '.editorconfig', '.gitignore', '.resx'
)

# <lang>
#   <zh-CN>高置信度密钥形态。每个模式都要求有厂商专有前缀或固定结构，避免与普通标识符混淆。
#   顺序按"误报风险从低到高"排列，便于在报告中优先呈现更确定的发现。</zh-CN>
#   <en>High-confidence secret shapes. Every pattern requires a vendor-specific prefix or a fixed structure so it cannot be
#   confused with ordinary identifiers. Patterns are ordered from lowest to highest false-positive risk so the most certain
#   findings surface first in the report.</en>
# </lang>
$secretPatterns = @(
    @{ Name = 'PrivateKeyBlock';        Regex = '-----BEGIN\s+(?:RSA|DSA|EC|OPENSSH|PGP)?\s*PRIVATE\s+KEY-----' }
    @{ Name = 'AwsAccessKeyId';         Regex = '\bAKIA[0-9A-Z]{16}\b' }
    @{ Name = 'AwsSecretAccessKey';     Regex = '(?i)aws_secret_access_key\s*=\s*[A-Za-z0-9/+=]{40}' }
    @{ Name = 'GitHubToken';            Regex = '\bgh[pousr]_[A-Za-z0-9]{36,}\b|\bgithub_pat_[A-Za-z0-9_]{20,}\b' }
    @{ Name = 'SlackToken';             Regex = '\bxox[baprs]-[A-Za-z0-9-]{10,}\b' }
    @{ Name = 'GoogleApiKey';           Regex = '\bAIza[0-9A-Za-z_-]{35}\b' }
    @{ Name = 'AzureStorageAccountKey'; Regex = 'AccountKey=[A-Za-z0-9+/=]{60,}' }
    @{ Name = 'JsonWebToken';           Regex = '\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b' }
    @{ Name = 'NpmAuthToken';           Regex = '\bnpm_[A-Za-z0-9]{36}\b' }
)

# <lang>
#   <zh-CN>本门禁自身与编排入口必然包含上表的模式定义文本，若不排除就会自我命中。
#   排除采用"路径包含 scripts 目录"的粗粒度判定：本门禁的价值在于扫描源码与配置，不需要扫描门禁脚本
#   自身 —— 门禁脚本里的密钥形态都是正则字符类，不是真实凭据。</zh-CN>
#   <en>This gate and the orchestration entry necessarily contain the pattern definitions above, so without exclusion they would
#   self-match. Exclusion is a coarse "path contains the scripts directory" rule: this gate's value lies in scanning source and
#   configuration, not the gate scripts themselves, where secret-shaped text is regex character classes rather than real
#   credentials.</en>
# </lang>
$selfPathMarker = [System.IO.Path]::DirectorySeparatorChar + 'scripts' + [System.IO.Path]::DirectorySeparatorChar

# <lang>
#   <zh-CN>受控例外：**精确到文件**，不用通配符。通配符（例如排除所有 min.js）会让全部压缩产物
#   逃过扫描，那是重大削弱 —— 压缩文件同样可以是密钥的藏身处。例外必须逐个说明理由。</zh-CN>
#   <en>Controlled exceptions are **exact file paths**, never wildcards. A wildcard (excluding every min.js, say) would let all
#   minified artifacts escape the scan, which is a serious weakening since a minified file can just as well hide a secret. Every
#   exception must state its reason individually.</en>
# </lang>
#
# <lang>
#   <zh-CN>当前唯一例外：JSEncrypt v2.3.1 是第三方库（文件头自带版本与许可证声明），其源码内置了一个
#   用于自测的 demo RSA 私钥，形态上就是私钥块。该密钥属于公开库的公开内容，**不是本项目的凭据**，
#   因此不构成泄露。但它同时说明两件事：① 该文件仍需保留在仓库中（受信任发布契约未排除 Scripts 目录）；
#   ② 仓库内未发现任何页面引用它，是否仍需保留**待确认**，已登记为 C-anp-P15 的遗留项。</zh-CN>
#   <en>The only current exception: JSEncrypt v2.3.1 is a third-party library (its header carries the version and license notice) whose
#   source embeds a demo RSA private key used for self-test, which has exactly the shape of a private key block. That key is public
#   content of a public library and **not a credential of this project**, so it is not a leak. It also surfaces two facts: first, the
#   file still has to stay in the repository (the trusted publish contract does not exclude the Scripts directory); second, no page in
#   the repository references it, so whether it is still needed is **unconfirmed** and is registered as a C-anp-P15 leftover item.</en>
# </lang>
$knownThirdPartyExceptions = @(
    [pscustomobject]@{
        Path   = 'src' + [System.IO.Path]::DirectorySeparatorChar + 'Portal' + [System.IO.Path]::DirectorySeparatorChar + 'Scripts' + [System.IO.Path]::DirectorySeparatorChar + 'Security' + [System.IO.Path]::DirectorySeparatorChar + 'jsencrypt-ie6.min.js'
        Reason = 'JSEncrypt v2.3.1 third-party library; embeds the upstream demo RSA private key, which is public library content and not a project credential.'
    }
)

$exceptionPathSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($exception in $knownThirdPartyExceptions) {
    [void]$exceptionPathSet.Add($exception.Path)
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>取 Git 已追踪文件清单。用 -z 分隔以正确处理含空格与中文的路径。</zh-CN>
#   <en>Obtain the Git-tracked file list. The -z separator is used so paths containing spaces and non-ASCII characters are
#   handled correctly.</en>
# </lang>
$trackedFiles = @()
$gitError = $null
try {
    $raw = & git -C $repoRoot ls-files -z 2>&1
    if ($LASTEXITCODE -ne 0) {
        $gitError = ($raw | Out-String).Trim()
    }
    else {
        $trackedFiles = @(($raw -join '') -split "`0" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
}
catch {
    $gitError = $_.Exception.Message
}

$passCount = 0
$warningCount = 0
$failCount = 0
$infoCount = 0
$findings = New-Object 'System.Collections.Generic.List[object]'

function Add-GateCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Detail,
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')][string]$Severity = 'Pass'
    )

    $script:findings.Add([pscustomobject]@{
            Name   = $Name
            Status = $(if ($Passed) { 'Pass' } else { $Severity })
            Detail = $Detail
        })

    # <lang>
    #   <zh-CN>计数必须按**实际状态**（$Passed）而不是按传入的 Severity 递增。
    #   本门禁最初写成按 Severity 递增，导致一个 Severity='Fail' 但 Passed=$true 的检查（例如"未命中密钥"
    #   本就期望通过）也把 failCount 加一，于是四个检查全部显示 PASS 却仍然 exit 1 —— 这是**假失败**，
    #   与本项目反复遇到的"假通过"同源：信号与事实不一致时，门禁会被人当成噪声而整体忽略。
    #   Severity 只在**未通过**时决定记入哪一档。</zh-CN>
    #   <en>Counters must advance according to the **actual status** ($Passed), not the incoming Severity. This gate originally
    #   incremented by Severity, so a check with Severity='Fail' but Passed=$true (for example "no secret found", which is expected
    #   to pass) still added one to failCount and the gate exited 1 while all four checks displayed PASS — a **false failure**, the
    #   mirror image of the false passes this project keeps meeting: when signal and fact disagree, a gate that cries wolf gets
    #   ignored. Severity only decides the bucket when the check did not pass.</en>
    # </lang>
    if ($Passed) {
        $script:passCount++
    }
    else {
        switch ($Severity) {
            'Fail' { $script:failCount++ }
            'Warning' { $script:warningCount++ }
            'Info' { $script:infoCount++ }
            default { $script:failCount++ }
        }
    }

    Write-Host ('[{0}] {1}: {2}' -f $(if ($Passed) { 'PASS' } else { $Severity.ToUpperInvariant() }), $Name, $Detail)
}

# <lang>
#   <zh-CN>门禁自身的前置条件：必须能读到 Git 追踪清单。读不到就判 Fail 而不是静默通过 ——
#   一个"因为拿不到文件列表所以零发现"的门禁等于没有门禁，这正是本项目反复遇到的那类假通过。</zh-CN>
#   <en>Gate precondition: the Git-tracked list must be readable. Failing rather than passing silently is deliberate: a gate that
#   reports zero findings because it could not obtain the file list is equivalent to no gate, which is exactly the kind of false
#   pass this project keeps running into.</en>
# </lang>
if ($gitError) {
    Add-GateCheck -Name 'Git tracked file inventory' -Passed $false -Severity 'Fail' -Detail ('Unable to list tracked files: ' + $gitError)
}
else {
    Add-GateCheck -Name 'Git tracked file inventory' -Passed ($trackedFiles.Count -gt 0) -Detail ($trackedFiles.Count.ToString() + ' tracked file(s) listed.')
}

if (-not $gitError) {
    $scannable = 0
    $skippedSelf = 0
    $exceptionHits = New-Object 'System.Collections.Generic.List[string]'
    $hits = New-Object 'System.Collections.Generic.List[object]'

    foreach ($relative in $trackedFiles) {
        $normalized = $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar)

        if ($normalized.Contains($selfPathMarker)) {
            $skippedSelf++
            continue
        }

        $isKnownException = $exceptionPathSet.Contains($normalized)

        $extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
        if ($textExtensions -notcontains $extension) {
            continue
        }

        $absolute = Join-Path $repoRoot $normalized
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
            continue
        }

        $scannable++

        # <lang>
        #   <zh-CN>逐行读取而非整体读取：只需判断"第几行命中"，无需把整个文件留在内存里，
        #   同时便于精确定位而不必二次解析。</zh-CN>
        #   <en>Read line by line rather than as a whole: only "which line matched" matters, so there is no need to hold the entire
        #   file in memory, and locating the hit needs no second parse.</en>
        # </lang>
        $lineNumber = 0
        foreach ($line in [System.IO.File]::ReadLines($absolute, [System.Text.Encoding]::UTF8)) {
            $lineNumber++
            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }

            foreach ($pattern in $secretPatterns) {
                if ($line -match $pattern.Regex) {
                    if ($isKnownException) {
                        # <lang>
                        #   <zh-CN>受控例外文件命中时**不计入失败**，但必须留痕 —— 静默忽略会让后来者
                        #   无法判断"这个文件为什么没报"，从而可能引入真正的第二个例外时无人察觉。</zh-CN>
                        #   <en>A hit inside a controlled-exception file does **not** count as a failure, but it must leave a trace:
                        #   silently ignoring it means a later reader cannot tell why the file was not reported, so a genuine second
                        #   exception could be added unnoticed.</en>
                        # </lang>
                        $exceptionHits.Add($normalized + ':' + $lineNumber + ' [' + $pattern.Name + ']')
                        continue
                    }

                    # <lang>
                    #   <zh-CN>只记录路径、行号与模式名。匹配正文绝不进入日志或证据文件。</zh-CN>
                    #   <en>Record only the path, line number and pattern name. The matched text never enters the log or the evidence
                    #   file.</en>
                    # </lang>
                    $hits.Add([pscustomobject]@{
                            File     = $normalized
                            Line     = $lineNumber
                            Pattern  = $pattern.Name
                        })
                }
            }
        }
    }

    $secretDetail = if ($hits.Count -eq 0) {
        'No high-confidence secret shape found in ' + $scannable + ' scanned text file(s); ' + $skippedSelf + ' gate script file(s) excluded by design.'
    }
    else {
        ($hits | ForEach-Object { $_.File + ':' + $_.Line + ' [' + $_.Pattern + ']' }) -join '; '
    }

    Add-GateCheck -Name 'No high-confidence secret in tracked files' -Passed ($hits.Count -eq 0) -Severity 'Fail' -Detail $secretDetail

    # <lang>
    #   <zh-CN>例外命中单独记为 Info：既不失败也不隐藏，让"哪些文件命中了例外"始终可见。</zh-CN>
    #   <en>Exception hits are recorded as Info: neither failing nor hidden, so "which files hit an exception" stays visible at all
    #   times.</en>
    # </lang>
    if ($exceptionHits.Count -gt 0) {
        Add-GateCheck -Name 'Known third-party exceptions hit' -Passed $true -Severity 'Info' -Detail (($exceptionHits | Sort-Object -Unique) -join '; ')
    }

    Add-GateCheck -Name 'Scan coverage' -Passed ($scannable -gt 0) -Severity 'Warning' -Detail ($scannable.ToString() + ' text file(s) scanned; ' + $skippedSelf + ' gate script(s) excluded.')
}

# <lang>
#   <zh-CN>汇总计数与 findings。本门禁为纯静态形态检查，不构成"凭据有效性验证"的结论。</zh-CN>
#   <en>Summarize counts and findings. This gate is a purely static shape check and does not draw conclusions about credential
#   validity.</en>
# </lang>
$summary = [pscustomobject]@{
    GeneratedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Purpose     = 'Static high-confidence secret shape scan over Git-tracked files.'
    Limits      = 'Not a credential validity check. Generic password-assignment shapes are covered by Test-PortalPublicDocumentation.ps1; default credential governance by Test-PortalDefaultCredentialRisk.ps1.'
    Counts      = [pscustomobject]@{
        Pass    = $passCount
        Warning = $warningCount
        Fail    = $failCount
        Info    = $infoCount
    }
    Findings    = $findings
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

Write-Host ('SUMMARY: Pass={0}; Warning={1}; Fail={2}; Info={3}' -f $passCount, $warningCount, $failCount, $infoCount)

# <lang>
#   <zh-CN>Fail 或显式 FailOnWarning 下 Warning 返回非零；本门禁不联网、不触碰数据库、不读取真实配置。</zh-CN>
#   <en>Return non-zero for Fail or Warning under explicit FailOnWarning; this gate does not access the network, a database, or real
#   configuration.</en>
# </lang>
if ($failCount -gt 0 -or ($FailOnWarning -and $warningCount -gt 0)) {
    exit 1
}

exit 0