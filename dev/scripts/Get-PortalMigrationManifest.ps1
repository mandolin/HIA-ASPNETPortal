<#
.SYNOPSIS
生成 Portal 数据库迁移 manifest 草案与脚本风险盘点。
Generates the Portal database migration manifest draft and script risk inventory.

.DESCRIPTION
<lang>
  <zh-CN>本脚本只读取仓库内 Git 已追踪的 SQL 脚本，不连接数据库、不执行 SQL、不读取外置连接串；输出用于 P11.3 的迁移、seed、回滚和数据修复规范。</zh-CN>
  <en>This script reads only Git-tracked SQL scripts inside the repository. It does not connect to a database, execute SQL, or read external connection strings; output supports the P11.3 migration, seed, rollback, and data-repair rules.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string]$SetupPath,

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($SetupPath)) {
    $SetupPath = Join-Path $repoRoot 'src/Setup'
}

$resolvedSetupPath = (Resolve-Path -LiteralPath $SetupPath).Path

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入可选 manifest JSON，并按需创建父目录。</zh-CN>
#   <en>Writes optional manifest JSON as UTF-8 without BOM and creates the parent directory when needed.</en>
# </lang>
function Write-Utf8NoBomFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

# <lang>
#   <zh-CN>把 SQL 文件绝对路径规范化为仓库相对路径，保持 manifest 边界固定。</zh-CN>
#   <en>Normalizes an absolute SQL-file path to a repository-relative value so the manifest boundary stays fixed.</en>
# </lang>
function Get-RepoRelativePath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return ($fullPath -replace '\\', '/')
}

# <lang>
#   <zh-CN>净化 SQL 证据中的密码/秘密片段并限制长度，避免低敏输出泄露凭据。</zh-CN>
#   <en>Redacts password/secret fragments and bounds SQL evidence length so low-sensitivity output cannot leak credentials.</en>
# </lang>
function Protect-EvidenceText {
    param([string]$Text)

    $value = $Text.Trim()
    $value = [regex]::Replace($value, "(?i)(sp_addlogin\s+'[^']+'\s*,\s*)'[^']+'", '$1''***''')
    $value = [regex]::Replace($value, '(?i)(password|pwd|passwd|secret|token)(\s*[:=]\s*)\S+', '$1$2***')
    if ($value.Length -gt 200) {
        $value = $value.Substring(0, 197) + '...'
    }

    return $value
}

# <lang>
#   <zh-CN>以 UTF-8 读取仓库内 SQL 文本用于只读规则匹配，不执行内容。</zh-CN>
#   <en>Reads repository SQL text as UTF-8 for read-only rule matching without executing its contents.</en>
# </lang>
function Get-SqlFileText {
    param([string]$Path)

    return [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>创建迁移 manifest 条目，集中保存顺序、类型、方言、幂等和回滚事实。</zh-CN>
#   <en>Creates a migration manifest entry containing order, type, dialect, idempotence, and rollback facts.</en>
# </lang>
function New-MigrationEntry {
    param(
        [int]$Order,
        [string]$Path,
        [ValidateSet('Base', 'FeatureMigration', 'SeedRequired', 'SeedSample', 'DataRepair', 'LegacyUtility', 'ProviderProof')]
        [string]$Type,
        [string]$Area,
        [ValidateSet('SqlServerOnly', 'PortableCandidate', 'NeedsDialect', 'ProviderProof')]
        [string]$ProviderCompatibility,
        [bool]$Idempotent,
        [ValidateSet('None', 'CompensationOnly', 'ManualOnly', 'NotApplicable')]
        [string]$RollbackMode,
        [string]$Notes
    )

    [pscustomobject]@{
        Order = $Order
        Path = $Path
        Type = $Type
        Area = $Area
        ProviderCompatibility = $ProviderCompatibility
        Idempotent = $Idempotent
        RollbackMode = $RollbackMode
        Notes = $Notes
    }
}

# <lang>
#   <zh-CN>创建统一检查结果对象，保留状态、代码、消息和低敏证据字段。</zh-CN>
#   <en>Creates a uniform check result with status, code, message, and low-sensitivity evidence fields.</en>
# </lang>
function New-Check {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Status,
        [string]$Code,
        [string]$Message,
        [string]$Evidence = ''
    )

    [pscustomobject]@{
        Status = $Status
        Code = $Code
        Message = $Message
        Evidence = $Evidence
    }
}

# <lang>
#   <zh-CN>将检查命中的 manifest 路径限制为前十项，用于可读且受控的证据摘要。</zh-CN>
#   <en>Bounds matching manifest paths to the first ten entries for readable and controlled evidence summaries.</en>
# </lang>
function Join-ManifestPaths {
    param([object[]]$Entries)

    return (@($Entries | ForEach-Object { $_.Path } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 10) -join '; ')
}

# <lang>
#   <zh-CN>静态定义迁移、seed、修复、legacy utility 和 provider proof 顺序；不执行其中 SQL。</zh-CN>
#   <en>Defines migration, seed, repair, legacy-utility, and provider-proof order statically without executing SQL.</en>
# </lang>
$manifest = @(
    New-MigrationEntry 10 'src/Setup/Portal_CreateDB.sql' 'Base' 'LegacyBaseSchema' 'SqlServerOnly' $false 'ManualOnly' '历史基础建库脚本；由初始化 wrapper 改写目标库名后用于隔离空库。'
    New-MigrationEntry 20 'src/Setup/Portal_LoadConfig.sql' 'SeedRequired' 'LegacyBaseConfig' 'SqlServerOnly' $false 'CompensationOnly' '历史门户基础配置 seed，当前仍是运行所需基础数据。'
    New-MigrationEntry 30 'src/Setup/Portal_LoadData.sql' 'SeedSample' 'LegacySampleData' 'SqlServerOnly' $false 'CompensationOnly' '历史演示数据，包含默认账号/样例内容；生产导入前必须按安全规则替换或移除。'
    New-MigrationEntry 40 'src/Setup/PortalCfg_SystemSettings.sql' 'FeatureMigration' 'SystemSettings' 'SqlServerOnly' $true 'CompensationOnly' 'P2 系统设置与审计表。'
    New-MigrationEntry 50 'src/Setup/PortalCfg_UserRegistration.sql' 'FeatureMigration' 'UserRegistration' 'SqlServerOnly' $true 'CompensationOnly' 'P2 注册邀请和审核表。'
    New-MigrationEntry 60 'src/Setup/PortalCfg_OperationAudits.sql' 'FeatureMigration' 'OperationAudit' 'SqlServerOnly' $true 'CompensationOnly' 'P2 运营审计表。'
    New-MigrationEntry 70 'src/Setup/PortalCfg_TabThemeOverrides.sql' 'FeatureMigration' 'Theme' 'SqlServerOnly' $true 'CompensationOnly' 'P3 Tab 主题覆盖表。'
    New-MigrationEntry 80 'src/Setup/PortalCfg_ModulePackageStates.sql' 'FeatureMigration' 'ModuleCatalog' 'SqlServerOnly' $true 'CompensationOnly' 'P3 模块包状态表。'
    New-MigrationEntry 90 'src/Setup/Portal_UserCredentials.sql' 'FeatureMigration' 'SecurityLegacy' 'SqlServerOnly' $true 'CompensationOnly' 'P5 强哈希凭据与会话安全版本表；承接旧 MD5 退出路径。'
    New-MigrationEntry 100 'src/Setup/PortalCfg_RolePermissions.sql' 'FeatureMigration' 'RolePermission' 'SqlServerOnly' $true 'CompensationOnly' 'P5 细粒度权限表与基础权限 seed。'
    New-MigrationEntry 110 'src/Setup/PortalBiz_UserProfiles.sql' 'FeatureMigration' 'UserProfile' 'SqlServerOnly' $true 'CompensationOnly' 'P6 用户资料表和从旧用户表导入的必需 seed。'
    New-MigrationEntry 120 'src/Setup/PortalBiz_OrganizationUnits.sql' 'FeatureMigration' 'EmployeeOrganization' 'SqlServerOnly' $true 'CompensationOnly' 'P6.3 组织单元表。'
    New-MigrationEntry 130 'src/Setup/PortalBiz_Employees.sql' 'FeatureMigration' 'EmployeeOrganization' 'SqlServerOnly' $true 'CompensationOnly' 'P6.3 员工表。'
    New-MigrationEntry 140 'src/Setup/PortalBiz_UserEmployeeBindings.sql' 'FeatureMigration' 'EmployeeOrganization' 'SqlServerOnly' $true 'CompensationOnly' 'P6.3 用户员工绑定表。'
    New-MigrationEntry 150 'src/Setup/PortalBiz_EmployeeProfileConfirmations.sql' 'FeatureMigration' 'BusinessModule' 'SqlServerOnly' $true 'CompensationOnly' 'P6.4 员工资料确认表。'
    New-MigrationEntry 160 'src/Setup/PortalBiz_EmployeeProfileCorrectionRequests.sql' 'FeatureMigration' 'BusinessModule' 'SqlServerOnly' $true 'CompensationOnly' 'P6.4 员工资料更正请求表。'
    New-MigrationEntry 170 'src/Setup/PortalBiz_WorkItems.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P12.3 轻量业务待办表。'
    New-MigrationEntry 180 'src/Setup/PortalBiz_WorkItemEvents.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P12.3 轻量业务待办事件表。'
    New-MigrationEntry 190 'src/Setup/PortalBiz_BusinessApplications.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P19.4 抽象业务申请事实表。'
    New-MigrationEntry 200 'src/Setup/PortalBiz_WorkflowEvents.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P19.4 轻量 Workflow 事件事实表。'
    New-MigrationEntry 210 'src/Setup/PortalBiz_CollaborationItems.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P21.3 企业协同事项事实表。'
    New-MigrationEntry 220 'src/Setup/PortalBiz_CollaborationItemEvents.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P21.3 企业协同事项事件事实表。'
    New-MigrationEntry 230 'src/Setup/PortalBiz_ReferenceData.sql' 'FeatureMigration' 'BusinessReferenceData' 'SqlServerOnly' $true 'CompensationOnly' 'P23.2 受治理业务参考数据目录与协同事项类型、优先级种子。'
    New-MigrationEntry 240 'src/Setup/PortalBiz_CollaborationItemCommentWorkflow.sql' 'FeatureMigration' 'BusinessWorkflow' 'SqlServerOnly' $true 'CompensationOnly' 'P23.6 协同事项评论事件与状态规则扩展。'
    New-MigrationEntry 900 'src/Setup/Portal_CleanUp.sql' 'LegacyUtility' 'LegacyMaintenance' 'SqlServerOnly' $false 'ManualOnly' '历史清理脚本，不能进入自动迁移链。'
    New-MigrationEntry 910 'src/Setup/Portal_DropDB.sql' 'LegacyUtility' 'LegacyMaintenance' 'SqlServerOnly' $false 'ManualOnly' '历史删库脚本，仅限人工维护或隔离环境。'
    New-MigrationEntry 920 'src/Setup/Portal_GrantPermissions_ForLocal.sql' 'LegacyUtility' 'LegacySecurity' 'SqlServerOnly' $false 'ManualOnly' '历史授权脚本，含 legacy grant 写法，不进入现代矩阵路径。'
    New-MigrationEntry 930 'src/Setup/Portal_GrantPermissions_ForRemote.sql' 'LegacyUtility' 'LegacySecurity' 'SqlServerOnly' $false 'ManualOnly' '历史远程授权脚本，含 SQL login 字面量风险，不进入现代矩阵路径。'
    New-MigrationEntry 1000 'src/Setup/Providers/SQLite/PortalDataProviderProof.sql' 'ProviderProof' 'SQLiteProof' 'ProviderProof' $false 'NotApplicable' 'SQLite 独立 proof 脚本，不代表门户主库正式支持。'
)

# <lang>
#   <zh-CN>建立检查集合并只读盘点 setup 目录中的 SQL 文件覆盖、batch 和高风险标记。</zh-CN>
#   <en>Builds the check collection and read-only scans setup SQL coverage, batches, and high-risk markers.</en>
# </lang>
$checks = New-Object 'System.Collections.Generic.List[object]'
$existingSqlFiles = @(
    Get-ChildItem -LiteralPath $resolvedSetupPath -Recurse -File -Filter '*.sql' |
        ForEach-Object { Get-RepoRelativePath -Path $_.FullName } |
        Sort-Object
)
$manifestPaths = @($manifest | ForEach-Object { $_.Path } | Sort-Object)

$missingManifestFiles = @($manifest | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repoRoot ($_.Path -replace '/', [System.IO.Path]::DirectorySeparatorChar)) -PathType Leaf) })
$unmanifestedSqlFiles = @($existingSqlFiles | Where-Object { $manifestPaths -notcontains $_ })

$checks.Add((New-Check -Status $(if ($missingManifestFiles.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'MANIFEST-FILES' -Message ('Manifest file entries checked: {0}.' -f $manifest.Count) -Evidence $(if ($missingManifestFiles.Count -eq 0) { 'All manifest files exist.' } else { ($missingManifestFiles.Path -join '; ') })))
$checks.Add((New-Check -Status $(if ($unmanifestedSqlFiles.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'MANIFEST-COVERAGE' -Message ('Tracked SQL files checked: {0}.' -f $existingSqlFiles.Count) -Evidence $(if ($unmanifestedSqlFiles.Count -eq 0) { 'All tracked SQL files are represented in the manifest draft.' } else { ($unmanifestedSqlFiles -join '; ') })))

$scriptAnalyses = New-Object 'System.Collections.Generic.List[object]'
foreach ($entry in $manifest) {
    $fullPath = Join-Path $repoRoot ($entry.Path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $text = Get-SqlFileText -Path $fullPath
    $batches = @([regex]::Split($text, '(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?\n') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $scriptAnalyses.Add([pscustomobject]@{
            Path = $entry.Path
            Type = $entry.Type
            Area = $entry.Area
            ProviderCompatibility = $entry.ProviderCompatibility
            Idempotent = $entry.Idempotent
            RollbackMode = $entry.RollbackMode
            HasSqlCmdDirective = [regex]::IsMatch($text, '(?im)^\s*:')
            HasGoRepeatCount = [regex]::IsMatch($text, '(?im)^\s*GO\s+\d+')
            HasFixedDatabaseContext = [regex]::IsMatch($text, '(?im)^\s*USE\s+\[?Portal\]?|^\s*USE\s+master')
            HasCreateOrDropDatabase = [regex]::IsMatch($text, '(?im)\bCREATE\s+DATABASE\b|\bDROP\s+DATABASE\b')
            HasDropTable = [regex]::IsMatch($text, '(?im)\bDROP\s+TABLE\b')
            HasLegacyGrant = [regex]::IsMatch($text, '(?i)sp_addlogin|sp_grantlogin|sp_grantdbaccess|sp_addrolemember')
            HasIdentityInsert = [regex]::IsMatch($text, '(?i)SET\s+IDENTITY_INSERT')
            HasDefaultAdminOrLegacyPassword = [regex]::IsMatch($text, '(?i)\badmin\b|password|md5|PortalUser')
            BatchCount = $batches.Count
            Length = $text.Length
        })
}

# <lang>
#   <zh-CN>将 SQLCMD、GO 重复、legacy grant、删库/删表和安全 seed 命中归类为检查证据，不执行修复。</zh-CN>
#   <en>Classifies SQLCMD, GO-repeat, legacy-grant, drop, and security-seed matches as evidence without applying fixes.</en>
# </lang>
$sqlCmdScripts = @($scriptAnalyses | Where-Object { $_.HasSqlCmdDirective })
$goRepeatScripts = @($scriptAnalyses | Where-Object { $_.HasGoRepeatCount })
$legacyGrantScripts = @($scriptAnalyses | Where-Object { $_.HasLegacyGrant })
$utilityDropScripts = @($scriptAnalyses | Where-Object { $_.HasCreateOrDropDatabase -or $_.HasDropTable })
$securitySeedScripts = @($scriptAnalyses | Where-Object { $_.HasDefaultAdminOrLegacyPassword })

$checks.Add((New-Check -Status $(if ($sqlCmdScripts.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'SQLCMD-DIRECTIVES' -Message 'SQLCMD directives are not allowed in automated migration chain.' -Evidence (Join-ManifestPaths -Entries $sqlCmdScripts)))
$checks.Add((New-Check -Status $(if ($goRepeatScripts.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'GO-REPEAT' -Message 'GO repeat-count batches are not allowed in automated migration chain.' -Evidence (Join-ManifestPaths -Entries $goRepeatScripts)))
$checks.Add((New-Check -Status 'Warning' -Code 'LEGACY-GRANT-SCRIPTS' -Message ('Legacy grant/login scripts identified: {0}.' -f $legacyGrantScripts.Count) -Evidence (Join-ManifestPaths -Entries $legacyGrantScripts)))
$checks.Add((New-Check -Status 'Info' -Code 'UTILITY-DROP-SCRIPTS' -Message ('Drop/create database or drop-table utility scripts identified: {0}.' -f $utilityDropScripts.Count) -Evidence (Join-ManifestPaths -Entries $utilityDropScripts)))
$checks.Add((New-Check -Status 'Warning' -Code 'SECURITY-SEED-REVIEW' -Message ('Scripts needing default credential or security-legacy review: {0}.' -f $securitySeedScripts.Count) -Evidence (Join-ManifestPaths -Entries $securitySeedScripts)))

$typeSummary = @(
    $manifest |
        Group-Object Type |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                Type = $_.Name
                Count = $_.Count
            }
        }
)

$areaSummary = @(
    $manifest |
        Group-Object Area |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                Area = $_.Name
                Count = $_.Count
            }
        }
)

$statusSummary = [ordered]@{
    Pass = @($checks | Where-Object { $_.Status -eq 'Pass' }).Count
    Warning = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count
    Fail = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
    Info = @($checks | Where-Object { $_.Status -eq 'Info' }).Count
}

# <lang>
#   <zh-CN>组装稳定的 manifest、统计、分析和检查结果对象，供控制台或可选 JSON 输出使用。</zh-CN>
#   <en>Assembles stable manifest, summary, analysis, and check objects for console or optional JSON output.</en>
# </lang>
$result = [pscustomobject]@{
    GeneratedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [System.Globalization.CultureInfo]::InvariantCulture)
    SetupPath = Get-RepoRelativePath -Path $resolvedSetupPath
    Counts = $statusSummary
    TypeSummary = $typeSummary
    AreaSummary = $areaSummary
    Manifest = $manifest
    ScriptAnalyses = @($scriptAnalyses | Sort-Object Path)
    Checks = $checks
}

# <lang>
#   <zh-CN>输出低敏检查结果，并在可选路径写入 JSON；FailOnWarning 仅改变最终失败门禁。</zh-CN>
#   <en>Emits low-sensitivity check results and optionally writes JSON; FailOnWarning changes only the final failure gate.</en>
# </lang>
foreach ($check in $checks) {
    Write-Host ('[{0}] {1}: {2}' -f $check.Status.ToUpperInvariant(), $check.Code, $check.Message)
    if (-not [string]::IsNullOrWhiteSpace($check.Evidence)) {
        Write-Host ('       {0}' -f (Protect-EvidenceText -Text $check.Evidence))
    }
}

Write-Host ('SUMMARY: Pass={0}; Warning={1}; Fail={2}; Info={3}' -f $statusSummary.Pass, $statusSummary.Warning, $statusSummary.Fail, $statusSummary.Info)

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $json = $result | ConvertTo-Json -Depth 8
    Write-Utf8NoBomFile -Path $OutputJson -Content ($json + [Environment]::NewLine)
    Write-Host ('Wrote JSON: {0}' -f $OutputJson)
}

if ($statusSummary.Fail -gt 0 -or ($FailOnWarning -and $statusSummary.Warning -gt 0)) {
    throw ('Portal migration manifest check failed: Fail={0}; Warning={1}.' -f $statusSummary.Fail, $statusSummary.Warning)
}
