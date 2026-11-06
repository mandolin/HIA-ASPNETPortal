<#
.SYNOPSIS
<lang>
  <zh-CN>离线装配态校验与记录工具：只读校验台账与 WAP 形态事实，生成六要素审计载荷与可审阅的参数化 SQL 产物。</zh-CN>
  <en>Offline module-assembly check and recording tool: read-only validation of the state ledger and WAP shape facts, producing six-element audit payloads and a reviewable parameterized SQL artifact.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>本工具在仓库/部署副本上**离线**运行，不是 Portal 运行时、不联网、不连数据库、不被任何页面调用、不自动执行脚本。
  它按 `B + F + I` 裁定只做「校验 → 备份快照 → 六要素记录 → 回滚演练」这条机制，不改写 `.aspx`/`.ascx` 页头、
  不生成或删除 `.designer.cs`、不改 `.csproj`、不切换装配态、不进入 `CodeFile` 调试态、不把源码随站点部署。

  校验项：
    V1 台账文件存在且为合法 UTF-8 JSON，根节点只允许 schemaVersion/entries，版本必须为 1；
    V2 条目键为本仓受控命名空间（HIA./Legacy.）、非空白、长度 <= 200，条目对象只允许 assemblyMode 与四个追溯字段；
    V3 assemblyMode 属于封闭两值（CodeBehind/CodeFile，精确大小写），第三值/空值/非字符串一律整体失败；
    V4 台账里每个 packageId 都能在受信任部署目录的 module.json 中解析到（防止登记腐化）；
    V5 WAP 形态事实：App_Code 目录不存在、存在 .designer.cs、Portal.csproj 已登记设计器文件；
    V6 部署级开关 Portal.ModuleAssembly.AllowCodeFile 缺省关闭；开关关闭时台账含 CodeFile 条目即拒绝继续
       （事件驱动复核点「每次离线工具运行前」，与封版前复核同口径）；
    V7 六要素紧凑摘要 <= 500 字符，超长即失败并提示由后续工作包提出审计表扩展方案，绝不静默截断。

  产物（幂等：同一输入产生同一内容，时间戳由 -RunUtc 固定，不随挂钟变化）：
    <OutputDirectory>/module-assembly-check.json  —— 校验结论 + 哈希清单快照 + 六要素记录（证据）
    <OutputDirectory>/module-assembly-check.sql   —— 可审阅的参数化 INSERT，写入 PortalCfg_OperationAudits
  **本工具不执行该 SQL，也不连接任何数据库**：落库接线属于后续工作包（审计事件常量与权限闭环）。
  这是「离线、不联网、不被运行时调用」约束下六要素「写入审计表」的唯一自洽形态，
  与 dev/scripts/New-PortalP19BusinessApplicationScenarioSql.ps1 的既有范式一致。

  -WhatIf 只打印将生成的产物与校验结论，不写任何文件。任一步失败即停止、留下失败证据、退出码非 0，
  且不产生半改状态（工具从不修改产品文件，故回滚演练验证的是「快照可还原为零残留」而非源码回滚）。
</lang>
<en>
  This tool runs **offline** on a repository/deployment copy; it is not the Portal runtime, uses no network,
  connects to no database, is called by no page, and auto-executes no script. Under the `B + F + I` ruling it
  implements only the "check -> backup snapshot -> six-element record -> rollback drill" mechanism: it never
  rewrites .aspx/.ascx page directives, creates or deletes .designer.cs files, edits .csproj, switches assembly
  modes, enters the CodeFile debug mode, or deploys source alongside the site.

  Checks:
    V1 the ledger exists, is valid UTF-8 JSON, its root allows only schemaVersion/entries, and its version is 1;
    V2 entry keys use this repository's controlled namespaces (HIA./Legacy.), are nonblank and <= 200 chars, and
       entry objects allow only assemblyMode plus four traceability fields;
    V3 assemblyMode belongs to the closed two-value set (CodeBehind/CodeFile, exact case); a third value, blank,
       or non-string fails the whole parse;
    V4 every packageId in the ledger resolves to a module.json in the trusted deployment root (guards stale registration);
    V5 WAP shape facts: no App_Code directory, .designer.cs files exist, and Portal.csproj registers them;
    V6 the deployment-level switch Portal.ModuleAssembly.AllowCodeFile defaults to off; with the switch off, any
       CodeFile entry stops the run (the "before each offline tool run" event-driven review point, same basis as
       the pre-release review);
    V7 the compact six-element summary is <= 500 chars; overflow fails with a pointer to a later work package's
       audit-table extension proposal, and is never silently truncated.

  Artifacts (idempotent: identical input yields identical content; timestamps are pinned by -RunUtc, not wall clock):
    <OutputDirectory>/module-assembly-check.json  -- check results + hash snapshot + six-element records (evidence)
    <OutputDirectory>/module-assembly-check.sql   -- reviewable parameterized INSERT into PortalCfg_OperationAudits
  **This tool never executes that SQL and never connects to any database**: the persistence wiring belongs to a later
  work package (audit event constants and permission closure). This is the only self-consistent form of "recording
  into the audit table" under the offline/no-network/no-runtime-invocation constraints, and matches the existing
  dev/scripts/New-PortalP19BusinessApplicationScenarioSql.ps1 pattern.

  -WhatIf prints only the planned artifacts and check results and writes no file. Any failure stops immediately,
  leaves failure evidence and exits non-zero, without a half-changed state (the tool never edits product files, so
  the rollback drill verifies "the snapshot restores to zero residue" rather than a source rollback).
</en>
</lang>

.PARAMETER RepoRoot
<lang>
  <zh-CN>仓库根（含 src/、dev/、work-zone/）。默认取本脚本上两级目录。</zh-CN>
  <en>Repository root (containing src/, dev/, work-zone/). Defaults to two levels above this script.</en>
</lang>

.PARAMETER OutputDirectory
<lang>
  <zh-CN>产物输出目录，默认 temp/module-assembly-check（被 Git 忽略，不入库）。</zh-CN>
  <en>Artifact output directory; defaults to temp/module-assembly-check, which Git ignores and never commits.</en>
</lang>

.PARAMETER RunUtc
<lang>
  <zh-CN>本次运行的固定 UTC 时间戳（ISO 8601）。显式固定以保证产物幂等；缺省取当前 UTC。</zh-CN>
  <en>Fixed UTC timestamp (ISO 8601) for this run. Pin it explicitly for reproducible artifacts; defaults to current UTC.</en>
</lang>

.PARAMETER Actor
<lang>
  <zh-CN>审计载荷中的操作者名（部署/运维标识），不含凭据或个人数据。</zh-CN>
  <en>Actor name in the audit payload (a deployment/operations label); contains no credentials or personal data.</en>
</lang>

.PARAMETER WhatIf
<lang>
  <zh-CN>通用参数：预览模式，打印将生成的产物与校验结论，不写任何文件。</zh-CN>
  <en>Common parameter: preview mode; print planned artifacts and check results without writing any file.</en>
</lang>

.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Invoke-PortalModuleAssemblyCheck.ps1' -WhatIf</zh-CN>
  <en>& 'dev\scripts\Invoke-PortalModuleAssemblyCheck.ps1' -WhatIf</en>
</lang>

.NOTES
<lang>
  <zh-CN>证明边界：只证明「台账形状合法、受信任包可解析、WAP 形态事实成立、开关缺省关闭、六要素载荷完整且未截断」；
  不证明任何模块在运行期可加载、不证明审计已写入数据库、不证明装配态发生过切换、不证明 SQL 产物已被审阅或执行。
  本工具是离线校验与记录工具，不是门禁；注入式有效性与正式门禁由后续工作包承担。</zh-CN>
  <en>Proof boundary: proves only that "the ledger shape is valid, trusted packages resolve, WAP shape facts hold,
  the switch is off by default, and the six-element payload is complete and untruncated". It does not prove any module
  loads at runtime, that the audit reached a database, that any assembly mode was switched, or that the SQL artifact was
  reviewed or executed. This is an offline check/recording tool, not a gate; injection-based effectiveness and the
  formal gate belong to a later work package.</en>
</lang>
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $RepoRoot,

    [string] $OutputDirectory,

    [string] $RunUtc,

    [ValidateNotNullOrEmpty()]
    [string] $Actor = 'deployment-offline-check'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>统一 UTF-8 无 BOM 读写，避免产物在 Windows 与其它部署环境之间发生字节漂移。</zh-CN>
#   <en>Use UTF-8 without BOM consistently so artifacts do not drift byte-wise between Windows and other deployment environments.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)

# <lang>
#   <zh-CN>仓库根解析：显式参数优先，否则取脚本上两级；路径必须存在且含关键目录。</zh-CN>
#   <en>Resolve the repository root: an explicit parameter wins, otherwise two levels above this script; the path must exist and contain the key directories.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
}
else {
    $RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
}
$srcRoot = Join-Path $RepoRoot 'src'
$portalRoot = Join-Path $srcRoot 'Portal'
if (-not (Test-Path -LiteralPath $portalRoot -PathType Container)) {
    throw ('仓库根缺少 src/Portal：' + $RepoRoot)
}

# <lang>
#   <zh-CN>输出目录解析：默认置于被 Git 忽略的 temp/ 下，避免证据文件混入产品提交。</zh-CN>
#   <en>Resolve the output directory: default under the Git-ignored temp/ so evidence files never mix into product commits.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot 'temp\module-assembly-check'
}

# <lang>
#   <zh-CN>运行时间戳固定：显式 RunUtc 原样使用，缺省取当前 UTC；格式统一为秒级 ISO 8601，
#   使同一输入的两次运行产物逐字节一致（幂等的可验证定义）。</zh-CN>
#   <en>Pin the run timestamp: use an explicit RunUtc verbatim, otherwise current UTC; normalize to second-precision
#   ISO 8601 so two runs with identical input produce byte-identical artifacts (a verifiable definition of idempotence).</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($RunUtc)) {
    $runUtcText = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
}
else {
    $parsedUtc = [DateTime]::MinValue
    if (-not [DateTime]::TryParse($RunUtc, [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal,
            [ref]$parsedUtc)) {
        throw ('RunUtc 不是合法时间：' + $RunUtc)
    }
    $runUtcText = $parsedUtc.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
}

# <lang>
#   <zh-CN>固定键序与字段名构成六要素紧凑摘要契约；顺序稳定才可机检，也不得静默截断。</zh-CN>
#   <en>The fixed key order and field names form the compact six-element summary contract; a stable order is machine-checkable and no silent truncation is allowed.</en>
# </lang>
$ledgerRelative = 'src/Portal/Config/module-assembly-state.json'
$ledgerPath = Join-Path $RepoRoot ($ledgerRelative -replace '/', '\')
$moduleRoot = Join-Path $portalRoot 'DesktopModules'
$projectFilePath = Join-Path $portalRoot 'Portal.csproj'
$switchKey = 'Portal.ModuleAssembly.AllowCodeFile'
$maxSummaryLength = 500
$allowedRootFields = @('schemaVersion', 'entries')
$allowedEntryFields = @('assemblyMode', 'recordedUtc', 'recordedBy', 'sourceHash', 'note')
$registeredModes = @('CodeBehind', 'CodeFile')

# <lang>
#   <zh-CN>失败清单与校验计数：任一失败都进入清单并使退出码非 0；计数用于证明「未命中不视为成功」。</zh-CN>
#   <en>Failure list and check counters: any failure enters the list and yields a non-zero exit; counters prove that a miss is not treated as success.</en>
# </lang>
$failures = New-Object 'System.Collections.Generic.List[string]'
$checks = New-Object 'System.Collections.Generic.List[string]'
$warnings = New-Object 'System.Collections.Generic.List[string]'

# <lang>
#   <zh-CN>读取 UTF-8 文本的单一入口；文件不存在时显式抛错，绝不静默跳过（跳过等于假装通过）。</zh-CN>
#   <en>Single entry point for UTF-8 text reads; a missing file throws explicitly and is never silently skipped (skipping would be pretending to pass).</en>
# </lang>
function Read-Utf8Text {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('缺少必需文件：' + $Path)
    }
    return [System.IO.File]::ReadAllText($Path, $utf8)
}

# <lang>
#   <zh-CN>计算稳定 SHA256 小写十六进制；用于「哈希前后」与快照可还原性比对。</zh-CN>
#   <en>Compute a stable lowercase SHA256 hex digest for "before/after hashes" and snapshot restorability comparison.</en>
# </lang>
function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string] $Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

# <lang>
#   <zh-CN>稳定 SQL 单引号转义：只做标准双写，不拼接未净化的自由文本到 VALUES 里（参数以变量承载）。</zh-CN>
#   <en>Stable SQL single-quote escaping: standard doubling only; no unsanitized free text is concatenated into VALUES (values are carried by variables).</en>
# </lang>
function ConvertTo-SqlLiteral {
    param([AllowNull()][string] $Value)
    if ($null -eq $Value) { return 'NULL' }
    return ("N'" + $Value.Replace("'", "''") + "'")
}

# <lang>
#   <zh-CN>V1–V3：台账形状与封闭词表校验。任何结构或取值不合法都整体失败，不返回部分结果。
#   这里与产品侧 PortalModuleAssemblyState.TryParse 的白名单/两值/缺省口径保持一致，是它的离线镜像。</zh-CN>
#   <en>V1-V3: ledger shape and closed-vocabulary validation. Any invalid structure or value fails the whole parse with
#   no partial result. This mirrors the product-side PortalModuleAssemblyState.TryParse whitelist/two-value/default basis.</en>
# </lang>
$ledgerModes = @{}
$ledgerEntryCount = 0
try {
    # <lang>
    #   <zh-CN>台账文本读取后按 JSON 解析；解析失败由外层 catch 统一收敛为失败清单条目。</zh-CN>
    #   <en>Read and parse the ledger as JSON; a parse failure is converged into one failure-list entry by the outer catch.</en>
    # </lang>
    $ledgerText = Read-Utf8Text -Path $ledgerPath
    $ledger = $ledgerText | ConvertFrom-Json

    # <lang>
    #   <zh-CN>根节点只允许两个键；多余键拒绝，避免未知策略被静默忽略形成「声明了但不生效」。</zh-CN>
    #   <en>The root allows exactly two keys; extras are rejected so unknown policy cannot be silently ignored into a "declared but ineffective" state.</en>
    # </lang>
    $rootFields = @($ledger.PSObject.Properties.Name)
    foreach ($fieldName in $rootFields) {
        if ($allowedRootFields -notcontains $fieldName) {
            $failures.Add(('V1 台账根节点含未知字段：' + $fieldName))
        }
    }
    if ($rootFields -notcontains 'schemaVersion') { $failures.Add('V1 台账缺少 schemaVersion') }
    if ($rootFields -notcontains 'entries') { $failures.Add('V1 台账缺少 entries') }

    # <lang>
    #   <zh-CN>schemaVersion 必须为整数 1；字符串 "1" 也拒绝，避免类型宽松造成版本协商被绕过。
    #   产品侧 C# 的 JavaScriptSerializer 返回 Int32，而 PowerShell 的 ConvertFrom-Json 返回 Int64：
    #   两种整数类型都是同一 JSON 整数的合法表示，故按整数类型集合判定（不按单一 CLR 类型），
    #   但值必须精确等于 1、非整数类型一律拒绝。这样离线镜像与产品契约语义一致而不放宽校验。</zh-CN>
    #   <en>schemaVersion must be integer 1; even the string "1" is rejected so loose typing cannot bypass version negotiation.
    #   The product-side C# JavaScriptSerializer returns Int32 while PowerShell's ConvertFrom-Json returns Int64:
    #   both are legitimate representations of the same JSON integer, so check against the set of integer types rather
    #   than one CLR type, while requiring the exact value 1 and rejecting all non-integer types. This keeps the offline
    #   mirror semantically aligned with the product contract without loosening validation.</en>
    # </lang>
    $schemaToken = $null
    if ($rootFields -contains 'schemaVersion') { $schemaToken = $ledger.schemaVersion }
    $schemaIsInteger = $null -ne $schemaToken -and
        ($schemaToken -is [int] -or $schemaToken -is [long] -or
         $schemaToken -is [short] -or $schemaToken -is [byte])
    if (-not $schemaIsInteger -or [long]$schemaToken -ne 1) {
        $failures.Add('V1 台账 schemaVersion 必须为整数 1')
    }

    # <lang>
    #   <zh-CN>entries 必须存在且为对象；空对象是合法的全缺省基线，不是错误。</zh-CN>
    #   <en>entries must exist and be an object; an empty object is a valid all-default baseline, not an error.</en>
    # </lang>
    $entries = $null
    if ($rootFields -contains 'entries') { $entries = $ledger.entries }
    if ($null -eq $entries -or $entries -isnot [pscustomobject]) {
        $failures.Add('V1 台账 entries 必须为对象')
    }
    else {
        # <lang>
        #   <zh-CN>逐条校验包标识、条目字段白名单与装配态；包标识只认本仓受控命名空间，长度与审计列对齐。</zh-CN>
        #   <en>Validate each package identifier, entry field whitelist, and mode; identifiers use only this repository's controlled namespaces and the length aligns with audit columns.</en>
        # </lang>
        foreach ($entryProperty in @($entries.PSObject.Properties)) {
            $packageId = [string]$entryProperty.Name
            if ([string]::IsNullOrWhiteSpace($packageId) -or
                -not ($packageId.StartsWith('HIA.', [StringComparison]::Ordinal) -or
                      $packageId.StartsWith('Legacy.', [StringComparison]::Ordinal)) -or
                $packageId.Length -gt 200) {
                $failures.Add(('V2 台账包标识不合法：' + $packageId))
                continue
            }

            # <lang>
            #   <zh-CN>条目值必须是对象；字符串/数组无法承载具名字段。</zh-CN>
            #   <en>An entry value must be an object; a string or array cannot carry named fields.</en>
            # </lang>
            $entryValue = $entryProperty.Value
            if ($null -eq $entryValue -or $entryValue -isnot [pscustomobject]) {
                $failures.Add(('V2 台账条目必须为对象：' + $packageId))
                continue
            }

            # <lang>
            #   <zh-CN>条目字段白名单：装配态 + 四个追溯字段；多余字段拒绝。</zh-CN>
            #   <en>Entry field whitelist: mode plus four traceability fields; extras are rejected.</en>
            # </lang>
            $entryFields = @($entryValue.PSObject.Properties.Name)
            foreach ($entryField in $entryFields) {
                if ($allowedEntryFields -notcontains $entryField) {
                    $failures.Add(('V2 台账条目含未知字段：' + $packageId + '/' + $entryField))
                }
            }

            # <lang>
            #   <zh-CN>assemblyMode 必须存在、为字符串且属封闭两值；空值不被当作 CodeBehind 缺省。</zh-CN>
            #   <en>assemblyMode must exist, be a string, and belong to the closed two-value set; a blank value is not treated as the CodeBehind default.</en>
            # </lang>
            $modeValue = $null
            if ($entryFields -contains 'assemblyMode') { $modeValue = $entryValue.assemblyMode }
            if ($null -eq $modeValue -or $modeValue -isnot [string] -or $registeredModes -notcontains $modeValue) {
                $failures.Add(('V3 台账装配态不合法：' + $packageId))
                continue
            }

            # <lang>
            #   <zh-CN>四个追溯字段若出现则必须是字符串，避免结构化对象混入只供审计展示的文本列。</zh-CN>
            #   <en>The four traceability fields, when present, must be strings so structured objects cannot leak into audit-display text columns.</en>
            # </lang>
            foreach ($traceField in @('recordedUtc', 'recordedBy', 'sourceHash', 'note')) {
                if ($entryFields -contains $traceField) {
                    $traceValue = $entryValue.$traceField
                    if ($null -ne $traceValue -and $traceValue -isnot [string]) {
                        $failures.Add(('V2 台账追溯字段必须为字符串：' + $packageId + '/' + $traceField))
                    }
                }
            }

            # <lang>
            #   <zh-CN>全部校验通过后登记到映射；只有此处登记的条目才进入后续受信任包解析与审计载荷。</zh-CN>
            #   <en>Add to the map only after all checks pass; only entries registered here reach trusted-package resolution and the audit payload.</en>
            # </lang>
            $ledgerModes[$packageId] = [string]$modeValue
            $ledgerEntryCount++
        }
    }
    $checks.Add(('V1-V3 台账形状/词表校验：条目 ' + $ledgerEntryCount + ' 个'))
}
catch {
    $failures.Add(('V1 台账读取或 JSON 解析失败：' + $_.Exception.Message))
}

# <lang>
#   <zh-CN>V4：离线枚举受信任包（与 Test-PortalModulePackageContract.ps1 的 M3 同法：只认固定文件名，
#   不递归猜测其它清单），并核对台账里每个 packageId 是否可解析；登记腐化即失败。</zh-CN>
#   <en>V4: enumerate trusted packages offline (same method as M3 in Test-PortalModulePackageContract.ps1: fixed file name
#   only, no recursive guessing), then verify every ledger packageId resolves; stale registration fails.</en>
# </lang>
$trustedPackages = @{}
if (Test-Path -LiteralPath $moduleRoot -PathType Container) {
    foreach ($dir in @(Get-ChildItem -LiteralPath $moduleRoot -Directory | Sort-Object Name)) {
        # <lang>
        #   <zh-CN>固定清单文件名；缺失即跳过该目录，不猜测其它清单来源。</zh-CN>
        #   <en>Fixed manifest file name; a missing one skips that directory without guessing another manifest source.</en>
        # </lang>
        $manifestPath = Join-Path $dir.FullName 'module.json'
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { continue }
        try {
            # <lang>
            #   <zh-CN>解析 packageId 与 version；两者都是审计载荷 TargetId 的组成部分。</zh-CN>
            #   <en>Parse packageId and version; both form the audit payload's TargetId.</en>
            # </lang>
            $manifest = (Read-Utf8Text -Path $manifestPath) | ConvertFrom-Json
            $manifestId = [string]$manifest.packageId
            $manifestVersion = [string]$manifest.version
            if ([string]::IsNullOrWhiteSpace($manifestId)) { continue }
            $trustedPackages[$manifestId] = @{
                Version = if ([string]::IsNullOrWhiteSpace($manifestVersion)) { 'unknown' } else { $manifestVersion }
                Directory = $dir.FullName
                RelativeDirectory = ('src/Portal/DesktopModules/' + $dir.Name)
            }
        }
        catch {
            $failures.Add(('V4 受信任包清单解析失败：' + $dir.Name))
        }
    }
}
else {
    $failures.Add('V4 受信任部署目录不存在')
}
foreach ($packageId in @($ledgerModes.Keys | Sort-Object)) {
    if (-not $trustedPackages.ContainsKey($packageId)) {
        $failures.Add(('V4 台账包标识在受信任包中无法解析：' + $packageId))
    }
}
$checks.Add(('V4 受信任包解析：可解析包 ' + $trustedPackages.Count + ' 个，台账条目 ' + $ledgerEntryCount + ' 个'))

# <lang>
#   <zh-CN>V5：WAP 形态事实。App_Code 不得存在；必须存在 .designer.cs；Portal.csproj 必须登记设计器文件。
#   三项都是「只读事实」，工具不生成、不删除、不改写任何设计器文件。</zh-CN>
#   <en>V5: WAP shape facts. App_Code must not exist; .designer.cs files must exist; Portal.csproj must register them.
#   All three are read-only facts; the tool creates, deletes, and rewrites no designer file.</en>
# </lang>
$appCodePath = Join-Path $portalRoot 'App_Code'
if (Test-Path -LiteralPath $appCodePath) {
    $failures.Add('V5 App_Code 目录存在（WAP 形态事实被破坏）')
}
$designerFiles = @(Get-ChildItem -LiteralPath $portalRoot -Recurse -Filter '*.designer.cs' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Sort-Object FullName)
if ($designerFiles.Count -eq 0) {
    $failures.Add('V5 未发现任何 .designer.cs 文件')
}
$projectText = ''
if (Test-Path -LiteralPath $projectFilePath -PathType Leaf) {
    $projectText = Read-Utf8Text -Path $projectFilePath
}
else {
    $failures.Add('V5 Portal.csproj 缺失')
}
if ($projectText -notmatch 'designer\.cs') {
    $failures.Add('V5 Portal.csproj 未登记任何 .designer.cs')
}
$checks.Add(('V5 WAP 形态：App_Code 不存在=' + (-not (Test-Path -LiteralPath $appCodePath)) +
             '；designer 文件=' + $designerFiles.Count + ' 个；csproj 已登记=' + ($projectText -match 'designer\.cs')))

# <lang>
#   <zh-CN>V6：部署级开关。读取 Web.config 与 Config/appSettings.json 的显式值，二者必须都是 false；
#   任何缺失、非法值或 true 都失败（fail-closed），不靠「键不存在」表达关闭。</zh-CN>
#   <en>V6: deployment-level switch. Read explicit values from Web.config and Config/appSettings.json; both must be false.
#   Any missing, invalid, or true value fails (fail-closed); absence of a key never expresses "off".</en>
# </lang>
$webConfigPath = Join-Path $portalRoot 'Web.config'
$appSettingsPath = Join-Path $portalRoot 'Config\appSettings.json'
$webConfigText = Read-Utf8Text -Path $webConfigPath
$appSettingsText = Read-Utf8Text -Path $appSettingsPath
$webSwitchMatch = [regex]::Match($webConfigText, '<add\s+key="Portal\.ModuleAssembly\.AllowCodeFile"\s+value="([^"]*)"\s*/>')
if (-not $webSwitchMatch.Success) {
    $failures.Add('V6 Web.config 未显式登记部署级开关')
}
elseif ($webSwitchMatch.Groups[1].Value -ne 'false') {
    $failures.Add('V6 Web.config 部署级开关非 false：' + $webSwitchMatch.Groups[1].Value)
}
$appSettings = $appSettingsText | ConvertFrom-Json
$appSwitchValue = $null
if ($appSettings.appSettings.PSObject.Properties.Name -contains $switchKey) {
    $appSwitchValue = [string]$appSettings.appSettings.$switchKey
}
if ($null -eq $appSwitchValue) {
    $failures.Add('V6 appSettings.json 未显式登记部署级开关')
}
elseif ($appSwitchValue -ne 'false') {
    $failures.Add('V6 appSettings.json 部署级开关非 false：' + $appSwitchValue)
}

# <lang>
#   <zh-CN>事件驱动复核点：开关关闭时，台账含 CodeFile 条目即拒绝继续（不自动改、不删条目）。
#   与 W-anp-P274 的封版前复核使用同一判据；此处是「每次离线工具运行前」那一个复核点。</zh-CN>
#   <en>Event-driven review point: with the switch off, any CodeFile entry stops the run (no automatic edit or deletion).
#   It shares the predicate with the pre-release review in a later work package; this is the "before each offline tool run" point.</en>
# </lang>
$switchEnabled = ($webSwitchMatch.Success -and $webSwitchMatch.Groups[1].Value -eq 'true') -or $appSwitchValue -eq 'true'
$codeFileEntries = @($ledgerModes.Keys | Where-Object { $ledgerModes[$_] -eq 'CodeFile' } | Sort-Object)
if (-not $switchEnabled -and $codeFileEntries.Count -gt 0) {
    $failures.Add('V6 部署级开关关闭但台账含受控调试态条目：' + ($codeFileEntries -join ', '))
}
$checks.Add(('V6 部署级开关：Web.config=' + $(if ($webSwitchMatch.Success) { $webSwitchMatch.Groups[1].Value } else { '<missing>' }) +
             '；appSettings=' + $(if ($null -eq $appSwitchValue) { '<missing>' } else { $appSwitchValue }) +
             '；未收敛 CodeFile 条目=' + $codeFileEntries.Count))

# <lang>
#   <zh-CN>哈希清单快照（受控备份）：对受信任包目录下的受控源文件与产物计算 SHA256，
#   只记录相对路径/长度/哈希，不复制源码内容，避免源码副本散落到 temp/ 或随站点部署。
#   「前后各一次」通过在同一不改动流程中两次采样并比对实现：相等即证明工具零写入。</zh-CN>
#   <en>Hash-manifest snapshot (controlled backup): compute SHA256 for controlled source/artifact files under trusted
#   package directories, recording only relative path/length/hash and copying no source content, so no source copy lands
#   in temp/ or ships with the site. "Before and after" is implemented by sampling twice within the same no-write flow;
#   equality proves the tool wrote nothing.</en>
# </lang>
function Get-PackageHashManifest {
    param([Parameter(Mandatory = $true)][string] $Root)
    $manifest = New-Object 'System.Collections.Generic.List[object]'
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return @() }
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Sort-Object FullName)) {
        # <lang>
        #   <zh-CN>相对路径按仓库根归一化，产物不携带本机绝对路径（公开面形状要求同样适用于证据）。</zh-CN>
        #   <en>Normalize relative paths against the repository root so artifacts carry no machine-specific absolute path (the public-shape rule applies to evidence too).</en>
        # </lang>
        $relative = $file.FullName.Substring($RepoRoot.Length + 1).Replace('\', '/')
        $manifest.Add([pscustomobject][ordered]@{
            path = $relative
            bytes = $file.Length
            sha256 = (Get-FileSha256 -Path $file.FullName)
        })
    }
    # <lang>
    #   <zh-CN>显式 ToArray 后再返回：PowerShell 7 对「泛型 List 内装字典」直接做数组枚举时，
    #   DLR 绑定器会抛 Argument types do not match；转成 PSCustomObject 数组可消除该类型歧义，
    #   同时保证产物形状稳定（相对路径/长度/哈希三字段固定）。</zh-CN>
    #   <en>Convert explicitly with ToArray before returning: PowerShell 7's DLR binder throws
    #   "Argument types do not match" when enumerating a generic List of dictionaries into an array;
    #   a PSCustomObject array removes that ambiguity and keeps the artifact shape stable.</en>
    # </lang>
    return $manifest.ToArray()
}

$hashBefore = @()
$hashAfter = @()
if ($moduleRoot -and (Test-Path -LiteralPath $moduleRoot -PathType Container)) {
    $hashBefore = @(Get-PackageHashManifest -Root $moduleRoot)
    $hashAfter = @(Get-PackageHashManifest -Root $moduleRoot)
}
# <lang>
#   <zh-CN>用 -InputObject 传入整个数组而非逐元素管道，保证空清单也序列化为 [] 而不是空字符串，
#   使「前后一致」的比对在零文件与多文件两种情况下都成立。</zh-CN>
#   <en>Pass the whole array via -InputObject instead of a per-element pipeline so an empty manifest serializes
#   to [] rather than an empty string, keeping the before/after comparison valid for both zero and many files.</en>
# </lang>
$beforeJson = (ConvertTo-Json -InputObject @($hashBefore) -Depth 4 -Compress)
$afterJson = (ConvertTo-Json -InputObject @($hashAfter) -Depth 4 -Compress)
if ($beforeJson -ne $afterJson) {
    $failures.Add('V7 哈希前后不一致：工具或外部流程改动了受信任包文件')
}
$checks.Add(('V7 哈希清单快照：文件 ' + $hashBefore.Count + ' 个，前后一致=' + ($beforeJson -eq $afterJson)))

# <lang>
#   <zh-CN>六要素紧凑摘要：模块标识与版本、源文件哈希（前后）、操作者、起止 UTC、验证证据、装配态迁移方向。
#   固定键序、分号分隔、不含源文件路径与敏感值；迁移方向按 F 裁定如实记为 CheckOnly（本工具不切换装配态）。
#   长度超过 500 即整体失败并提示由后续工作包提出审计表扩展方案，绝不静默截断。</zh-CN>
#   <en>Compact six-element summary: module identity+version, source hashes (before/after), actor, start/end UTC,
#   verification evidence, and assembly-mode migration direction. Fixed key order, semicolon separated, with no source
#   paths or sensitive values; under ruling F the direction is honestly recorded as CheckOnly (no mode switch occurs).
#   Exceeding 500 chars fails the whole run with a pointer to a later work package's audit-table extension proposal,
#   and is never silently truncated.</en>
# </lang>
$records = New-Object 'System.Collections.Generic.List[object]'
$beforeDigest = (Get-FileSha256 -Path $ledgerPath)
$afterDigest = $beforeDigest
if ($ledgerModes.Count -eq 0) {
    # <lang>
    #   <zh-CN>全缺省台账也产生一条记录，证明「本次确实校验过」而不是「没有输入所以跳过」。</zh-CN>
    #   <en>An all-default ledger still produces one record, proving "a check actually ran" rather than "no input, so skipped".</en>
    # </lang>
    $records.Add([pscustomobject][ordered]@{
        targetId = 'ALL@default'
        mode = 'CodeBehind'
        summary = ('pkg=ALL@default;hashBefore=' + $beforeDigest + ';hashAfter=' + $afterDigest +
                   ';actor=' + $Actor + ';startUtc=' + $runUtcText + ';endUtc=' + $runUtcText +
                   ';verification=' + $(if ($failures.Count -eq 0) { 'V1-V7 passed' } else { 'V1-V7 failed' }) +
                   ';direction=CheckOnly')
    })
}
else {
    foreach ($packageId in @($ledgerModes.Keys | Sort-Object)) {
        # <lang>
        #   <zh-CN>TargetId 采用 packageId@version；版本来自受信任包清单，无法解析时以 unknown 显式表达。</zh-CN>
        #   <en>TargetId uses packageId@version; the version comes from the trusted manifest and an unresolvable one is explicitly "unknown".</en>
        # </lang>
        $version = 'unknown'
        if ($trustedPackages.ContainsKey($packageId)) { $version = [string]$trustedPackages[$packageId].Version }
        $targetId = $packageId + '@' + $version
        $records.Add([pscustomobject][ordered]@{
            targetId = $targetId
            mode = $ledgerModes[$packageId]
            summary = ('pkg=' + $targetId + ';hashBefore=' + $beforeDigest + ';hashAfter=' + $afterDigest +
                       ';actor=' + $Actor + ';startUtc=' + $runUtcText + ';endUtc=' + $runUtcText +
                       ';verification=' + $(if ($failures.Count -eq 0) { 'V1-V7 passed' } else { 'V1-V7 failed' }) +
                       ';direction=CheckOnly')
        })
    }
}
foreach ($record in $records.ToArray()) {
    if ($record.summary.Length -gt $maxSummaryLength) {
        $failures.Add(('V7 六要素摘要超过 ' + $maxSummaryLength + ' 字符（' + $record.summary.Length +
                       '），不得静默截断；须由后续工作包提出审计表扩展方案'))
    }
}
$checks.Add(('V7 六要素记录：' + $records.Count + ' 条，最长摘要 ' +
             (($records | ForEach-Object { $_.summary.Length } | Measure-Object -Maximum).Maximum) + ' 字符'))

# <lang>
#   <zh-CN>回滚演练：工具从不修改产品文件，故「可回滚」的可验证定义是「备份快照可还原为零残留」——
#   校验快照中每条路径仍存在、长度与哈希均未变，且没有额外文件；任何不一致即失败。
#   这是演练，不是真实回滚动作，符合 F 裁定「不切换装配态」。</zh-CN>
#   <en>Rollback drill: since the tool never edits product files, "restorable" is verifiably defined as "the backup
#   snapshot restores to zero residue" -- every snapshot path still exists with unchanged length and hash, and no extra
#   file appeared; any mismatch fails. This is a drill, not an actual rollback, consistent with ruling F.</en>
# </lang>
$rollbackResidue = 0
foreach ($item in $hashAfter) {
    $full = Join-Path $RepoRoot ($item.path -replace '/', '\')
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        $rollbackResidue++
        continue
    }
    $fileInfo = Get-Item -LiteralPath $full
    if ($fileInfo.Length -ne $item.bytes -or (Get-FileSha256 -Path $full) -ne $item.sha256) {
        $rollbackResidue++
    }
}
$currentFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
if ($currentFiles.Count -ne $hashAfter.Count) { $rollbackResidue += [Math]::Abs($currentFiles.Count - $hashAfter.Count) }
if ($rollbackResidue -gt 0) {
    $failures.Add(('V7 回滚演练发现残留 ' + $rollbackResidue + ' 处'))
}
$checks.Add(('V7 回滚演练：零残留=' + ($rollbackResidue -eq 0)))

# <lang>
#   <zh-CN>结论汇总：JSON 证据与参数化 SQL 产物都是纯文本，写入前先算好内容，保证幂等（同输入同字节）。
#   失败时仍写证据（含失败清单），但不写 SQL（避免失败状态产生可执行的入库语句）。</zh-CN>
#   <en>Result assembly: both the JSON evidence and the parameterized SQL artifact are pure text computed before any
#   write, guaranteeing idempotence (identical input yields identical bytes). Failures still write evidence (with the
#   failure list) but no SQL, so a failed state never produces an executable persistence statement.</en>
# </lang>
$result = [ordered]@{
    tool = 'Invoke-PortalModuleAssemblyCheck.ps1'
    runUtc = $runUtcText
    actor = $Actor
    ledgerRelativePath = $ledgerRelative
    ledgerEntryCount = $ledgerEntryCount
    trustedPackageCount = $trustedPackages.Count
    designerFileCount = $designerFiles.Count
    switchEnabled = $switchEnabled
    codeFileEntryCount = $codeFileEntries.Count
    hashFileCount = $hashBefore.Count
    hashesMatch = ($beforeJson -eq $afterJson)
    rollbackResidue = $rollbackResidue
    verdict = $(if ($failures.Count -eq 0) { 'pass' } else { 'fail' })
    checks = $checks.ToArray()
    failures = $failures.ToArray()
    warnings = $warnings.ToArray()
    records = $records.ToArray()
    hashManifestBefore = @($hashBefore)
    hashManifestAfter = @($hashAfter)
    persistence = [ordered]@{
        status = 'not-executed'
        target = 'PortalCfg_OperationAudits'
        note = 'SQL artifact is reviewable output only; the tool never connects to a database or executes it.'
    }
}
$jsonArtifact = ($result | ConvertTo-Json -Depth 8)

# <lang>
#   <zh-CN>参数化 SQL 产物：以 DECLARE 变量承载全部值、INSERT 使用变量而非拼接自由文本，
#   与产品侧 PortalOperationAudit.Record 的列集合与参数化口径一致；SQL 只写入，不含 DELETE/TRUNCATE。
#   注释明确「生成 ≠ 执行」，避免读者误以为审计已入库。</zh-CN>
#   <en>Parameterized SQL artifact: all values are carried by DECLARE variables and INSERT uses variables rather than
#   concatenated free text, matching the column set and parameterization of the product-side PortalOperationAudit.Record;
#   the SQL only inserts and contains no DELETE/TRUNCATE. A comment states "generated != executed" so readers do not
#   mistake it for an audit already persisted.</en>
# </lang>
$sqlLines = New-Object 'System.Collections.Generic.List[string]'
$sqlLines.Add('-- Generated by Invoke-PortalModuleAssemblyCheck.ps1; REVIEWABLE OUTPUT ONLY.')
$sqlLines.Add('-- This file is NOT executed by the tool. The tool never connects to any database.')
$sqlLines.Add('-- Persistence wiring and audit event constants belong to a later work package.')
$sqlLines.Add('-- Generated (not executed) at ' + $runUtcText + '; verdict=' + $result.verdict)
$sqlLines.Add('')
# <lang>
#   <zh-CN>仅在整个校验通过时才生成 INSERT；失败状态绝不产出可执行的入库语句。</zh-CN>
#   <en>Generate INSERT statements only when the whole check passed; a failed state never yields executable persistence SQL.</en>
# </lang>
if ($failures.Count -eq 0) {
    foreach ($record in $records.ToArray()) {
        $sqlLines.Add('DECLARE @Category nvarchar(80) = ' + (ConvertTo-SqlLiteral 'ModuleAssembly') + ';')
        $sqlLines.Add('DECLARE @Action nvarchar(80) = ' + (ConvertTo-SqlLiteral 'AssemblyStateCheck') + ';')
        $sqlLines.Add('DECLARE @Outcome nvarchar(20) = ' + (ConvertTo-SqlLiteral 'Success') + ';')
        $sqlLines.Add('DECLARE @ActorUserName nvarchar(100) = ' + (ConvertTo-SqlLiteral $Actor) + ';')
        $sqlLines.Add('DECLARE @TargetType nvarchar(80) = ' + (ConvertTo-SqlLiteral 'ModuleAssemblyState') + ';')
        $sqlLines.Add('DECLARE @TargetId nvarchar(200) = ' + (ConvertTo-SqlLiteral $record.targetId) + ';')
        $sqlLines.Add('DECLARE @Summary nvarchar(500) = ' + (ConvertTo-SqlLiteral $record.summary) + ';')
        $sqlLines.Add('DECLARE @OccurredUtc datetime2(0) = ' + (ConvertTo-SqlLiteral $runUtcText) + ';')
        $sqlLines.Add('INSERT INTO [dbo].[PortalCfg_OperationAudits]')
        $sqlLines.Add('    ([OccurredUtc], [Category], [Action], [Outcome], [ActorUserName], [TargetType], [TargetId], [Summary])')
        $sqlLines.Add('VALUES')
        $sqlLines.Add('    (@OccurredUtc, @Category, @Action, @Outcome, @ActorUserName, @TargetType, @TargetId, @Summary);')
        $sqlLines.Add('GO')
        $sqlLines.Add('')
    }
}
else {
    $sqlLines.Add('-- Verdict is fail: no INSERT was generated. See module-assembly-check.json for the failure list.')
    $sqlLines.Add('')
}
$sqlArtifact = ($sqlLines -join "`r`n")

# <lang>
#   <zh-CN>产物落盘：先打印结论，-WhatIf 到此为止不写任何文件；非预览模式用 ShouldProcess 取得显式确认语义，
#   再以 UTF-8 无 BOM 写入两个产物。目录仅在非预览模式下创建，保证预览零副作用。</zh-CN>
#   <en>Artifact persistence: print the verdict first; -WhatIf stops here and writes nothing. In non-preview mode use
#   ShouldProcess for explicit confirmation semantics, then write both artifacts as UTF-8 without BOM. The directory is
#   created only in non-preview mode, guaranteeing preview has zero side effects.</en>
# </lang>
$jsonPath = Join-Path $OutputDirectory 'module-assembly-check.json'
$sqlPath = Join-Path $OutputDirectory 'module-assembly-check.sql'
Write-Host ('装配态离线校验：verdict=' + $result.verdict + '；runUtc=' + $runUtcText +
            '；条目=' + $ledgerEntryCount + '；受信任包=' + $trustedPackages.Count +
            '；失败=' + $failures.Count + ($(if ($WhatIfPreference) { '；预览模式' } else { '' })))
foreach ($checkText in $checks.ToArray()) { Write-Host ('  [CHECK] ' + $checkText) }
foreach ($failureText in $failures.ToArray()) { Write-Host ('  [FAIL] ' + $failureText) }

if ($WhatIfPreference) {
    Write-Host ('  [DIFF] 将生成 ' + $jsonPath)
    Write-Host ('  [DIFF] 将生成 ' + $sqlPath + '（INSERT ' + $records.Count + ' 条；生成≠执行）')
    Write-Host '  [DIFF] 预览模式未写入任何文件；产品文件全程零改动。'
}
elseif ($PSCmdlet.ShouldProcess($OutputDirectory, '写入装配态校验证据与审计 SQL 产物')) {
    if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
        New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($jsonPath, $jsonArtifact, $utf8)
    [System.IO.File]::WriteAllText($sqlPath, $sqlArtifact, $utf8)
    Write-Host ('  [WRITE] ' + $jsonPath)
    Write-Host ('  [WRITE] ' + $sqlPath)
}

# <lang>
#   <zh-CN>退出码：任一校验失败即非 0，供调用方/门禁判定；-WhatIf 不改变该语义。
#   证明边界：只证明本脚本头部 NOTES 声明的静态事实，不证明运行期加载、审计已入库或装配态已切换。</zh-CN>
#   <en>Exit code: any failed check yields non-zero for callers/gates; -WhatIf does not change that. Proof boundary:
#   only the static facts declared in the NOTES header, not runtime loading, audit persistence, or mode switching.</en>
# </lang>
if ($failures.Count -gt 0) { exit 1 }
exit 0
