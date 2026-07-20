<#
.SYNOPSIS
检查 Portal SQL 脚本对 SQL Server 2016+ 版本矩阵的准备状态。
Checks Portal SQL scripts against the SQL Server 2016+ version-matrix preparation rules.

.DESCRIPTION
默认执行静态预检，不连接数据库、不创建数据库、不执行 SQL。提供外置连接串文件时，只读取目标实例的版本和兼容级别，不输出连接串、服务器名或密码。
By default this script performs static preflight only: it does not connect to a database, create a database, or execute SQL. When an external connection-string file is provided, it reads only version and compatibility metadata without printing the connection string, server name, or password.
#>
[CmdletBinding()]
param(
    [string]$SetupPath,

    [ValidateScript({ [string]::IsNullOrWhiteSpace($_) -or (Test-Path -LiteralPath $_ -PathType Leaf) })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [ValidateSet('SqlServer2016', 'SqlServer2017', 'SqlServer2019', 'SqlServer2022')]
    [string[]]$TargetVersions = @('SqlServer2016', 'SqlServer2017', 'SqlServer2019', 'SqlServer2022'),

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
$checks = New-Object 'System.Collections.Generic.List[object]'
$targetMetadata = @{
    SqlServer2016 = [pscustomobject]@{ ProductMajor = 13; CompatibilityLevel = 130; Label = 'SQL Server 2016' }
    SqlServer2017 = [pscustomobject]@{ ProductMajor = 14; CompatibilityLevel = 140; Label = 'SQL Server 2017' }
    SqlServer2019 = [pscustomobject]@{ ProductMajor = 15; CompatibilityLevel = 150; Label = 'SQL Server 2019' }
    SqlServer2022 = [pscustomobject]@{ ProductMajor = 16; CompatibilityLevel = 160; Label = 'SQL Server 2022' }
}

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

function Add-MatrixCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'Pending')]
        [string]$Status,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject]@{
            Status = $Status
            Code = $Code
            Message = $Message
            Evidence = $Evidence
        })

    Write-Host ('[{0}] {1}: {2}' -f $Status.ToUpperInvariant(), $Code, $Message)
    if (-not [string]::IsNullOrWhiteSpace($Evidence)) {
        Write-Host ('       {0}' -f $Evidence)
    }
}

function Get-RepoRelativePath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

function Get-SanitizedSqlEvidence {
    param([Microsoft.PowerShell.Commands.MatchInfo]$Match)

    $line = $Match.Line.Trim()
    $line = [regex]::Replace($line, "(?i)(sp_addlogin\s+'[^']+'\s*,\s*)'[^']+'", '$1''***''')
    $line = [regex]::Replace($line, '(?i)(password|pwd|passwd|secret|token)(\s*[:=]\s*)\S+', '$1$2***')
    if ($line.Length -gt 180) {
        $line = $line.Substring(0, 177) + '...'
    }

    return ('{0}:{1}: {2}' -f (Get-RepoRelativePath -Path $Match.Path), $Match.LineNumber, $line)
}

function Find-SqlMatches {
    param(
        [string]$Pattern,
        [string[]]$IncludeNames,
        [int]$Limit = 12
    )

    $matches = New-Object 'System.Collections.Generic.List[string]'
    $files = Get-ChildItem -LiteralPath $resolvedSetupPath -Recurse -File -Filter '*.sql'
    if ($IncludeNames -and $IncludeNames.Count -gt 0) {
        $fileSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($name in $IncludeNames) {
            [void]$fileSet.Add($name)
        }

        $files = @($files | Where-Object { $fileSet.Contains($_.Name) })
    }

    foreach ($file in $files) {
        foreach ($match in Select-String -LiteralPath $file.FullName -Pattern $Pattern -AllMatches -ErrorAction SilentlyContinue) {
            $matches.Add((Get-SanitizedSqlEvidence -Match $match))
            if ($matches.Count -ge $Limit) {
                return $matches
            }
        }
    }

    return $matches
}

function Get-ExternalConnectionString {
    param(
        [string]$Path,
        [string]$Name
    )

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $connectionStringsNode = if ($document.DocumentElement -and $document.DocumentElement.Name -eq 'connectionStrings') {
        $document.DocumentElement
    }
    elseif ($document.configuration -and $document.configuration.connectionStrings) {
        $document.configuration.connectionStrings
    }
    else {
        throw 'The external connection-string file must contain a <connectionStrings> section.'
    }

    $matches = @($connectionStringsNode.add | Where-Object { $_.name -eq $Name })
    if ($matches.Count -ne 1 -or [string]::IsNullOrWhiteSpace($matches[0].connectionString)) {
        throw "The external connection-string file does not contain one non-empty '$Name' entry."
    }

    return $matches[0].connectionString
}

function Get-LiveSqlServerInfo {
    param([string]$ConnectionString)

    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = @'
SELECT
    CONVERT(int, SERVERPROPERTY('ProductMajorVersion')) AS ProductMajorVersion,
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
    CONVERT(nvarchar(256), SERVERPROPERTY('Edition')) AS Edition,
    DB_NAME() AS DatabaseName,
    (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) AS CompatibilityLevel;
'@
            $reader = $command.ExecuteReader()
            try {
                if (-not $reader.Read()) {
                    throw 'SQL Server did not return version information.'
                }

                return [pscustomobject]@{
                    ProductMajorVersion = [System.Convert]::ToInt32($reader.GetValue(0), [System.Globalization.CultureInfo]::InvariantCulture)
                    ProductVersion = $reader.GetString(1)
                    Edition = $reader.GetString(2)
                    DatabaseName = $reader.GetString(3)
                    CompatibilityLevel = [System.Convert]::ToInt32($reader.GetValue(4), [System.Globalization.CultureInfo]::InvariantCulture)
                }
            }
            finally {
                $reader.Dispose()
            }
        }
        finally {
            $command.Dispose()
        }
    }
    finally {
        $connection.Dispose()
    }
}

# 中文：静态预检只检查脚本形态和已知版本边界；真实 SQL Server 版本结论必须由目标实例补证。
# English: Static preflight checks script shape and known version boundaries only; real version conclusions require target-instance evidence.
Add-MatrixCheck -Status 'Pass' -Code 'MATRIX-TARGETS' -Message 'SQL Server 2016+ target matrix is declared.' -Evidence (($TargetVersions | ForEach-Object { $targetMetadata[$_].Label }) -join '; ')

$allSqlFiles = @(Get-ChildItem -LiteralPath $resolvedSetupPath -Recurse -File -Filter '*.sql' | Sort-Object FullName)
Add-MatrixCheck -Status $(if ($allSqlFiles.Count -gt 0) { 'Pass' } else { 'Fail' }) -Code 'SETUP-SQL-FILES' -Message ('SQL setup scripts found: {0}.' -f $allSqlFiles.Count) -Evidence (Get-RepoRelativePath -Path $resolvedSetupPath)

$requiredScripts = @(
    'Portal_CreateDB.sql',
    'Portal_LoadConfig.sql',
    'Portal_LoadData.sql',
    'PortalCfg_SystemSettings.sql',
    'PortalCfg_UserRegistration.sql',
    'PortalCfg_OperationAudits.sql',
    'PortalCfg_TabThemeOverrides.sql',
    'PortalCfg_ModulePackageStates.sql',
    'Portal_UserCredentials.sql',
    'PortalCfg_RolePermissions.sql',
    'PortalBiz_UserProfiles.sql',
    'PortalBiz_OrganizationUnits.sql',
    'PortalBiz_Employees.sql',
    'PortalBiz_UserEmployeeBindings.sql',
    'PortalBiz_EmployeeProfileConfirmations.sql',
    'PortalBiz_EmployeeProfileCorrectionRequests.sql'
)
$existingScriptNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($file in $allSqlFiles) {
    [void]$existingScriptNames.Add($file.Name)
}
$missingRequiredScripts = @($requiredScripts | Where-Object { -not $existingScriptNames.Contains($_) })
Add-MatrixCheck -Status $(if ($missingRequiredScripts.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'SETUP-REQUIRED-SCRIPTS' -Message 'Required base and migration scripts are present.' -Evidence $(if ($missingRequiredScripts.Count -eq 0) { 'All required scripts found.' } else { $missingRequiredScripts -join '; ' })

$sqlcmdMatches = @(Find-SqlMatches -Pattern '(?im)^\s*:' -IncludeNames @())
Add-MatrixCheck -Status $(if ($sqlcmdMatches.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'SQLCMD-DIRECTIVES' -Message 'No SQLCMD directives are used in setup scripts.' -Evidence ($sqlcmdMatches -join '; ')

$goRepeatMatches = @(Find-SqlMatches -Pattern '(?im)^\s*GO\s+\d+' -IncludeNames @())
Add-MatrixCheck -Status $(if ($goRepeatMatches.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'GO-REPEAT' -Message 'No GO repeat-count batches are used.' -Evidence ($goRepeatMatches -join '; ')

$sql2017PlusMatches = @(Find-SqlMatches -Pattern '(?i)\b(STRING_AGG|TRANSLATE|CONCAT_WS)\s*\(|\bTRIM\s*\(' -IncludeNames @())
Add-MatrixCheck -Status $(if ($sql2017PlusMatches.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'SQL2016-SYNTAX' -Message 'No SQL Server 2017+ only syntax was found in SQL Server setup scripts.' -Evidence ($sql2017PlusMatches -join '; ')

$createOrAlterMatches = @(Find-SqlMatches -Pattern '(?i)\bCREATE\s+OR\s+ALTER\b' -IncludeNames @())
Add-MatrixCheck -Status $(if ($createOrAlterMatches.Count -eq 0) { 'Pass' } else { 'Warning' }) -Code 'SQL2016-SP1-SYNTAX' -Message 'No CREATE OR ALTER dependency was found; SQL Server 2016 RTM/SP boundary stays simple.' -Evidence ($createOrAlterMatches -join '; ')

$jsonOrSplitMatches = @(Find-SqlMatches -Pattern '(?i)\b(OPENJSON|STRING_SPLIT)\s*\(' -IncludeNames @())
Add-MatrixCheck -Status $(if ($jsonOrSplitMatches.Count -eq 0) { 'Pass' } else { 'Info' }) -Code 'SQL2016-COMPAT130-FUNCTIONS' -Message 'SQL Server 2016 compatibility-level 130 functions are not required by setup scripts.' -Evidence ($jsonOrSplitMatches -join '; ')

$extensionScripts = @(
    'PortalCfg_SystemSettings.sql',
    'PortalCfg_UserRegistration.sql',
    'PortalCfg_OperationAudits.sql',
    'PortalCfg_TabThemeOverrides.sql',
    'PortalCfg_ModulePackageStates.sql',
    'Portal_UserCredentials.sql',
    'PortalCfg_RolePermissions.sql',
    'PortalBiz_UserProfiles.sql',
    'PortalBiz_OrganizationUnits.sql',
    'PortalBiz_Employees.sql',
    'PortalBiz_UserEmployeeBindings.sql',
    'PortalBiz_EmployeeProfileConfirmations.sql',
    'PortalBiz_EmployeeProfileCorrectionRequests.sql'
)
$extensionUseMatches = @(Find-SqlMatches -Pattern '(?im)^\s*USE\s+\[[^\]]+\]' -IncludeNames $extensionScripts)
Add-MatrixCheck -Status $(if ($extensionUseMatches.Count -eq 0) { 'Pass' } else { 'Fail' }) -Code 'MIGRATION-NO-HARDCODED-DB' -Message 'Modern extension migrations do not hard-code database context.' -Evidence ($extensionUseMatches -join '; ')

$legacyAuthMatches = @(Find-SqlMatches -Pattern '(?i)\b(sp_addlogin|sp_grantlogin|sp_grantdbaccess|sp_addrolemember)\b' -IncludeNames @('Portal_GrantPermissions_ForLocal.sql', 'Portal_GrantPermissions_ForRemote.sql'))
Add-MatrixCheck -Status $(if ($legacyAuthMatches.Count -eq 0) { 'Pass' } else { 'Warning' }) -Code 'LEGACY-GRANT-SCRIPTS' -Message 'Legacy SQL login/grant scripts remain and should not be used as the modern matrix path.' -Evidence ($legacyAuthMatches -join '; ')

$legacyDatabaseContextMatches = @(Find-SqlMatches -Pattern '(?im)^\s*(CREATE\s+DATABASE|USE)\s+\[(Portal|master)\]' -IncludeNames @('Portal_CreateDB.sql', 'Portal_LoadConfig.sql', 'Portal_LoadData.sql', 'Portal_DropDB.sql', 'Portal_CleanUp.sql', 'Portal_GrantPermissions_ForLocal.sql', 'Portal_GrantPermissions_ForRemote.sql'))
Add-MatrixCheck -Status $(if ($legacyDatabaseContextMatches.Count -gt 0) { 'Info' } else { 'Warning' }) -Code 'LEGACY-DB-CONTEXT' -Message 'Legacy install scripts keep fixed database context and require initialization-wrapper rewriting or manual isolation.' -Evidence (($legacyDatabaseContextMatches | Select-Object -First 10) -join '; ')

$initializeScriptPath = Join-Path $repoRoot 'dev/scripts/Initialize-PortalTestDatabase.ps1'
$initializeScriptText = if (Test-Path -LiteralPath $initializeScriptPath -PathType Leaf) { [System.IO.File]::ReadAllText($initializeScriptPath, [System.Text.UTF8Encoding]::new($false)) } else { '' }
$initializationWrapperOk = $initializeScriptText.Contains('Set-LegacyDatabaseContext') -and
    $initializeScriptText.Contains('Get-SqlServerMajorVersion') -and
    $initializeScriptText.Contains('serverMajorVersion -lt 13')
Add-MatrixCheck -Status $(if ($initializationWrapperOk) { 'Pass' } else { 'Fail' }) -Code 'INIT-WRAPPER-SQL2016' -Message 'Isolated database initialization wrapper enforces SQL Server 2016+ and rewrites legacy database context.' -Evidence 'dev/scripts/Initialize-PortalTestDatabase.ps1'

$compatibilityScriptPath = Join-Path $repoRoot 'dev/scripts/Test-PortalSqlCompatibility.ps1'
Add-MatrixCheck -Status $(if (Test-Path -LiteralPath $compatibilityScriptPath -PathType Leaf) { 'Pass' } else { 'Fail' }) -Code 'LIVE-SCHEMA-SMOKE-SCRIPT' -Message 'Existing live SQL compatibility smoke script is available for target databases.' -Evidence 'dev/scripts/Test-PortalSqlCompatibility.ps1'

$liveInfo = $null
if ([string]::IsNullOrWhiteSpace($ConnectionStringsConfigPath)) {
    foreach ($target in $TargetVersions) {
        Add-MatrixCheck -Status 'Pending' -Code ('LIVE-{0}' -f $target) -Message ('{0} target-instance evidence is not provided in this run.' -f $targetMetadata[$target].Label) -Evidence 'Run again with -ConnectionStringsConfigPath against an isolated target database.'
    }
}
else {
    try {
        $connectionString = Get-ExternalConnectionString -Path $ConnectionStringsConfigPath -Name $ConnectionStringName
        $liveInfo = Get-LiveSqlServerInfo -ConnectionString $connectionString
        Add-MatrixCheck -Status $(if ($liveInfo.ProductMajorVersion -ge 13) { 'Pass' } else { 'Fail' }) -Code 'LIVE-SQL2016-BASELINE' -Message ('Connected SQL Server major version is {0}.' -f $liveInfo.ProductMajorVersion) -Evidence ('Database=' + $liveInfo.DatabaseName + '; CompatibilityLevel=' + $liveInfo.CompatibilityLevel)
        Add-MatrixCheck -Status $(if ($liveInfo.CompatibilityLevel -ge 130) { 'Pass' } else { 'Warning' }) -Code 'LIVE-COMPAT-LEVEL' -Message 'Target database compatibility level is recorded without automatic upgrade.' -Evidence ('CompatibilityLevel=' + $liveInfo.CompatibilityLevel)

        foreach ($target in $TargetVersions) {
            $targetInfo = $targetMetadata[$target]
            if ($liveInfo.ProductMajorVersion -eq $targetInfo.ProductMajor) {
                Add-MatrixCheck -Status 'Pass' -Code ('LIVE-{0}' -f $target) -Message ('This run provides direct evidence for {0}.' -f $targetInfo.Label) -Evidence ('ProductVersion=' + $liveInfo.ProductVersion)
            }
            else {
                Add-MatrixCheck -Status 'Pending' -Code ('LIVE-{0}' -f $target) -Message ('This run does not provide direct evidence for {0}.' -f $targetInfo.Label) -Evidence ('ObservedMajor=' + $liveInfo.ProductMajorVersion)
            }
        }
    }
    catch {
        Add-MatrixCheck -Status 'Fail' -Code 'LIVE-CONNECTION' -Message 'Live SQL Server metadata check failed.' -Evidence $_.Exception.Message
    }
}

$counts = [ordered]@{}
foreach ($statusName in @('Pass', 'Warning', 'Fail', 'Info', 'Pending')) {
    $counts[$statusName] = @($checks | Where-Object { $_.Status -eq $statusName }).Count
}

$summary = [pscustomobject]@{
    SetupPath = $resolvedSetupPath
    TargetVersions = $TargetVersions
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    LiveServer = $liveInfo
    Counts = $counts
    Checks = $checks
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

Write-Host ('SUMMARY: Pass={0}; Warning={1}; Fail={2}; Info={3}; Pending={4}' -f $counts.Pass, $counts.Warning, $counts.Fail, $counts.Info, $counts.Pending)

if ($counts.Fail -gt 0) {
    exit 1
}

if ($FailOnWarning -and $counts.Warning -gt 0) {
    exit 1
}

exit 0
