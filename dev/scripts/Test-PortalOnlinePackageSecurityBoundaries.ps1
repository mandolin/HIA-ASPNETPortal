<#
.SYNOPSIS
<lang>
  <zh-CN>在线能力（在线安装/上传）B 级安全边界静态一致性门禁。</zh-CN>
  <en>Static consistency gate for the B-level security boundaries of online capabilities (online install/upload).</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>只做静态断言，覆盖六个维度，确认在线能力各安全边界在源码中**存在且契约一致**，不证明运行期判定正确、不执行安装、不连接数据库：
  1) 白名单/隔离：隔离暂存策略常量（`PortalOnlinePackagePolicy`）与隔离暂存器失败清理（`PortalOnlinePackageStaging.TryStage`）；
  2) 权限/双步确认：`PortalOnlinePackageConfirmationGate` 的签发票据与确认票据（`TryIssue`/`TryConfirm`）；
  3) 审计：在线能力审计事件词表（`PortalOnlinePackageAuditEvents`）与运行时统一写入入口（`PortalOnlinePackageAudit.TryRecord` 经 `PortalOperationAudit.Record`）；
  4) 工具不代写边界：Components 层只定义常量与纯函数，`PortalOnlinePackageAuditEvents`/`PortalOnlinePackageAuditFormatter` 不得出现运行时写入入口；
  5) 签名/完整性：`PortalOnlinePackageIntegrity` 的 `TryVerifySignature`/`TryVerifyIntegrity`（RSA-SHA256 真实验签 + 4.8 缺失的 CRC 完整性职责）；
  6) 扫描/fail-closed：`PortalOnlinePackageScanning` 的默认适配器 `PortalOnlinePackageUnavailableScanner` 与联合判定入口 `TryAllowInstall`。
  证明边界：只证明这些边界在源码中**存在且契约一致**；不证明运行期授权/验签/扫描判定正确（由运行期门禁与单测承载），不执行页面授权、不连接数据库、不调用安装流程。</zh-CN>
  <en>Static assertions only, across six dimensions, confirming each security boundary of online capabilities **exists and its contract agrees in source** — without proving runtime decisions, performing installs, or touching the database:
  1) Allowlist/isolation: isolation staging policy constants (`PortalOnlinePackagePolicy`) and staging failure cleanup (`PortalOnlinePackageStaging.TryStage`);
  2) Permission/two-step confirmation: `PortalOnlinePackageConfirmationGate` ticket issuance and confirmation (`TryIssue`/`TryConfirm`);
  3) Audit: online-capability audit event vocabulary (`PortalOnlinePackageAuditEvents`) and the unified runtime write entry (`PortalOnlinePackageAudit.TryRecord` via `PortalOperationAudit.Record`);
  4) Tools-never-write boundary: the Components layer defines constants and pure functions only; `PortalOnlinePackageAuditEvents`/`PortalOnlinePackageAuditFormatter` must not contain a runtime write entry;
  5) Signature/integrity: `PortalOnlinePackageIntegrity.TryVerifySignature`/`TryVerifyIntegrity` (RSA-SHA256 verification + the CRC integrity duty 4.8 lacks);
  6) Scanning/fail-closed: `PortalOnlinePackageScanning`'s default adapter `PortalOnlinePackageUnavailableScanner` and the joint gate `TryAllowInstall`.
  Proof boundary: proves only that these boundaries **exist and agree in source**; it does not prove runtime authorization/signature/scan decisions (runtime gates and unit tests do), performs no page authorization, connects to no database, and invokes no install flow.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>追加一条安全边界静态检查结果；结果只描述文本契约，不授予运行时权限、不执行安装。</zh-CN>
#   <en>Add one security-boundary static-check result; it describes text contracts only and grants no runtime permission or install.</en>
# </lang>
function Add-BoundaryCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][ValidateSet('Pass', 'Warning', 'Fail', 'Info')][string]$Status,
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject]@{
            Code     = $Code
            Status   = $Status
            Message  = $Message
            Evidence = $Evidence
        })
}

# <lang>
#   <zh-CN>按仓库相对路径读取必需文本；缺失立即失败，不连接数据库或读取真实配置。</zh-CN>
#   <en>Read required text by repository-relative path; fail immediately when missing without connecting to a database or reading real configuration.</en>
# </lang>
function Get-PortalText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>序数静态断言：文本必须同时包含全部片段，否则返回 false。</zh-CN>
#   <en>Ordinal static assertion: the text must contain every fragment at once, otherwise false.</en>
# </lang>
function Test-ContainsAll {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string[]]$Fragments
    )

    foreach ($fragment in $Fragments) {
        if ($Text.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
            return $false
        }
    }
    return $true
}

# <lang>
#   <zh-CN>剥离 C# 整行注释（含 `///` 文档注释）后再做静态断言，沿用项目「注释提及不计」口径
#   （如 `PortalOnlinePackageAuditEvents.cs` 的 XML 注释会合法地说明写入经 `PortalOperationAudit.Record`，
#   若连同注释一并判定会把设计说明误判为代码调用）。仅剔除整行注释，可执行语句一律保留，断言强度不降低。</zh-CN>
#   <en>Strips whole-line C# comments (including `///` doc comments) before static assertions, following the project's
#   "comment mentions do not count" rule (e.g. `PortalOnlinePackageAuditEvents.cs` legitimately documents that writes go
#   through `PortalOperationAudit.Record`; judging with comments included would mistake design notes for code calls).
#   Only whole-line comments are removed; every executable statement stays, so assertion strength is unchanged.</en>
# </lang>
function Strip-PortalComments {
    param([Parameter(Mandatory = $true)][string]$Source)

    $builder = New-Object System.Text.StringBuilder
    foreach ($line in $Source.Split(@("`r`n", "`n"), [System.StringSplitOptions]::None)) {
        $trimmed = $line.TrimStart()
        if ($trimmed.StartsWith('//', [System.StringComparison]::Ordinal)) {
            continue
        }
        [void]$builder.AppendLine($line)
    }
    return $builder.ToString()
}

# <lang>
#   <zh-CN>单维度封装：读取源码并断言关键符号存在；任何异常（文件缺失/符号缺失）即记为 Fail，不影响其它维度继续。</zh-CN>
#   <en>Per-dimension wrapper: reads source and asserts key symbols exist; any anomaly (missing file/symbol) is recorded as Fail without blocking other dimensions.</en>
# </lang>
function Test-BoundaryDimension {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string[]]$Fragments,
        [Parameter(Mandatory = $true)][string]$Label
    )

    try {
        $text = Get-PortalText -RelativePath $RelativePath
        if (Test-ContainsAll -Text $text -Fragments $Fragments) {
            Add-BoundaryCheck -Code $Code -Status 'Pass' -Message "$Label 边界在源码中一致存在" -Evidence $RelativePath
        }
        else {
            Add-BoundaryCheck -Code $Code -Status 'Fail' -Message "$Label 边界缺失关键符号（$(($Fragments | Select-Object -First 3) -join ', ')'）" -Evidence $RelativePath
        }
    }
    catch {
        Add-BoundaryCheck -Code $Code -Status 'Fail' -Message "$Label 检查失败：$(($_.Exception.Message -replace '[\r\n]+', ' '))" -Evidence $RelativePath
    }
}

# 维度 1：白名单/隔离（隔离暂存策略常量 + 隔离暂存器失败清理）
Test-BoundaryDimension -Code 'OP-ISOLATION' -RelativePath 'src/Portal.Components/PortalOnlinePackagePolicy.cs' `
    -Fragments @('PortalOnlinePackagePolicy', 'StagingVirtualDirectory', 'App_Data/OnlinePackages/Staging', 'MaximumPackageBytes') `
    -Label '隔离暂存策略常量'
Test-BoundaryDimension -Code 'OP-ISOLATION' -RelativePath 'src/Portal.Components/PortalOnlinePackageStaging.cs' `
    -Fragments @('PortalOnlinePackageStaging', 'TryStage', 'Directory.Delete', 'finally') `
    -Label '隔离暂存器失败清理'

# 维度 2：权限/双步确认
Test-BoundaryDimension -Code 'OP-PERMISSION' -RelativePath 'src/Portal.Components/PortalOnlinePackageConfirmation.cs' `
    -Fragments @('PortalOnlinePackageConfirmationGate', 'TryIssue', 'TryConfirm', 'Module.OnlinePackage') `
    -Label '双步确认闸'

# 维度 3：审计（事件词表 + 统一写入入口）
Test-BoundaryDimension -Code 'OP-AUDIT' -RelativePath 'src/Portal.Components/PortalOnlinePackageAuditEvents.cs' `
    -Fragments @('PortalOnlinePackageAuditEvents', 'OnlinePackageUploadValidated', 'OnlinePackageInstallConfirmed') `
    -Label '在线能力审计事件词表'
Test-BoundaryDimension -Code 'OP-AUDIT' -RelativePath 'src/Portal/Components/PortalOnlinePackageAudit.cs' `
    -Fragments @('PortalOnlinePackageAudit', 'TryRecord', 'PortalOperationAudit.Record', 'outcome') `
    -Label '运行时统一审计写入入口'

# 维度 4：工具不代写边界（Components 层不得出现运行时写入入口）
try {
    $eventsText = Strip-PortalComments -Source (Get-PortalText -RelativePath 'src/Portal.Components/PortalOnlinePackageAuditEvents.cs')
    $formatterText = Strip-PortalComments -Source (Get-PortalText -RelativePath 'src/Portal.Components/PortalOnlinePackageAuditFormatter.cs')
    $leak = ($eventsText.IndexOf('PortalOperationAudit.Record(', [System.StringComparison]::Ordinal) -ge 0) -or `
            ($formatterText.IndexOf('PortalOperationAudit.Record(', [System.StringComparison]::Ordinal) -ge 0)
    if ($leak) {
        Add-BoundaryCheck -Code 'OP-NOWRITE' -Status 'Fail' -Message 'Components 层出现运行时审计写入入口，违反工具不代写边界' `
            -Evidence 'src/Portal.Components/PortalOnlinePackageAuditEvents.cs / PortalOnlinePackageAuditFormatter.cs'
    }
    else {
        Add-BoundaryCheck -Code 'OP-NOWRITE' -Status 'Pass' -Message 'Components 层只定义常量与纯函数，未出现运行时写入入口' `
            -Evidence 'src/Portal.Components/PortalOnlinePackageAuditEvents.cs / PortalOnlinePackageAuditFormatter.cs'
    }
}
catch {
    Add-BoundaryCheck -Code 'OP-NOWRITE' -Status 'Fail' -Message "工具不代写边界检查失败：$(($_.Exception.Message -replace '[\r\n]+', ' '))" `
        -Evidence 'src/Portal.Components/PortalOnlinePackageAuditEvents.cs'
}

# 维度 5：签名/完整性
Test-BoundaryDimension -Code 'OP-SIGNATURE' -RelativePath 'src/Portal.Components/PortalOnlinePackageIntegrity.cs' `
    -Fragments @('PortalOnlinePackageIntegrity', 'TryVerifySignature', 'TryVerifyIntegrity', 'ComputeSha256Hex', 'RSACryptoServiceProvider') `
    -Label '签名与完整性校验'

# 维度 6：扫描/fail-closed
Test-BoundaryDimension -Code 'OP-SCAN' -RelativePath 'src/Portal.Components/PortalOnlinePackageScanning.cs' `
    -Fragments @('PortalOnlinePackageUnavailableScanner', 'PortalOnlinePackageSecurityGate', 'TryAllowInstall', 'Unavailable') `
    -Label '恶意扫描适配点与 fail-closed 联合判定'

# 维度 7：回滚一致性（判定器 + 演练证据脚本）
Test-BoundaryDimension -Code 'OP-ROLLBACK' -RelativePath 'src/Portal.Components/PortalOnlinePackageRollback.cs' `
    -Fragments @('PortalOnlinePackageRollbackDrill', 'TryVerifyZeroResidue', 'PortalOnlinePackageFailureIsolation', 'TryAggregate') `
    -Label '回滚与故障隔离判定器'
try {
    $drillPath = Join-Path $RepoRoot 'dev/scripts/Invoke-PortalOnlinePackageRollbackDrill.ps1'
    if (Test-Path -LiteralPath $drillPath -PathType Leaf) {
        Add-BoundaryCheck -Code 'OP-ROLLBACK' -Status 'Pass' -Message '回滚演练证据脚本存在（隔离临时目录 + 逐字节还原 + 零残留证明）' `
            -Evidence 'dev/scripts/Invoke-PortalOnlinePackageRollbackDrill.ps1'
    }
    else {
        Add-BoundaryCheck -Code 'OP-ROLLBACK' -Status 'Fail' -Message '回滚演练证据脚本缺失' `
            -Evidence 'dev/scripts/Invoke-PortalOnlinePackageRollbackDrill.ps1'
    }
}
catch {
    Add-BoundaryCheck -Code 'OP-ROLLBACK' -Status 'Fail' -Message "回滚证据脚本检查失败：$(($_.Exception.Message -replace '[\r\n]+', ' '))" `
        -Evidence 'dev/scripts/Invoke-PortalOnlinePackageRollbackDrill.ps1'
}

# <lang>
#   <zh-CN>汇总结果；存在 Fail 时抛出非零失败，避免静态安全边界缺失被误报为通过。</zh-CN>
#   <en>Summarise results; throw a non-zero failure when any check is Fail so a missing static security boundary cannot be reported as passed.</en>
# </lang>
$failedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' })
$warningChecks = @($checks | Where-Object { $_.Status -eq 'Warning' })

$result = [pscustomobject]@{
    Gate            = 'Test-PortalOnlinePackageSecurityBoundaries.ps1'
    Layer           = 'L0'
    Repository      = 'HIA-ASPNETPortal'
    GeneratedUtc    = (Get-Date -Format 'o')
    TotalChecks     = $checks.Count
    FailedChecks    = $failedChecks.Count
    WarningChecks   = $warningChecks.Count
    Checks          = @($checks.ToArray())
}

if ($OutputJson) {
    $jsonPath = if ([System.IO.Path]::IsPathRooted($OutputJson)) { $OutputJson }
    else { Join-Path $RepoRoot $OutputJson }
    $jsonDirectory = Split-Path -Parent $jsonPath
    if (-not [string]::IsNullOrWhiteSpace($jsonDirectory)) {
        New-Item -ItemType Directory -Force -Path $jsonDirectory | Out-Null
    }
    [System.IO.File]::WriteAllText(
        $jsonPath,
        ($result | ConvertTo-Json -Depth 5),
        [System.Text.UTF8Encoding]::new($false))
}

$result

if ($failedChecks.Count -gt 0) {
    throw ('Online-capability security boundary gate failed: ' + (($failedChecks | ForEach-Object { $_.Code }) -join ', '))
}
