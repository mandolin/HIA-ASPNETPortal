<#
.SYNOPSIS
生成 Portal 数据访问与数据库方言只读盘点。
Generates a read-only inventory of Portal data-access and database-dialect usage.

.DESCRIPTION
本脚本只扫描 Git 已追踪源码和脚本，不连接数据库、不读取仓库外连接串、不修改文件。输出用于 P11.2 的数据库兼容性标签盘点。
This script scans only Git-tracked source and script files. It does not connect to a database, read external connection strings, or modify files. The output supports the P11.2 database compatibility labeling inventory.
#>
[CmdletBinding()]
param(
    [string]$SourceRoot,

    [string]$OutputJson,

    [ValidateRange(1, 200)]
    [int]$SampleLimit = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $repoRoot 'src'
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$pathComparer = [System.StringComparer]::OrdinalIgnoreCase

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

function Get-RepoRelativePath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return ($fullPath -replace '\\', '/')
}

function Protect-EvidenceText {
    param([string]$Text)

    $value = $Text.Trim()
    $value = [regex]::Replace($value, '(?i)(password|pwd|passwd|secret|token|connectionString)(\s*[:=]\s*)["''][^"'']+["'']', '$1$2"***"')
    $value = [regex]::Replace($value, '(?i)(password|pwd|passwd|secret|token)(\s*[:=]\s*)\S+', '$1$2***')
    if ($value.Length -gt 220) {
        $value = $value.Substring(0, 217) + '...'
    }

    return $value
}

function Get-TrackedInventoryFiles {
    $gitFiles = @()
    try {
        $gitFiles = @(git -C $repoRoot ls-files -- 'src' 2>$null)
    }
    catch {
        $gitFiles = @()
    }

    if ($LASTEXITCODE -ne 0 -or $gitFiles.Count -eq 0) {
        $extensions = @('.cs', '.sql', '.config', '.aspx', '.ascx', '.csproj', '.xml')
        return @(Get-ChildItem -LiteralPath $resolvedSourceRoot -Recurse -File |
            Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() } |
            ForEach-Object { $_.FullName })
    }

    $allowedExtensions = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($extension in @('.cs', '.sql', '.config', '.aspx', '.ascx', '.csproj', '.xml')) {
        [void]$allowedExtensions.Add($extension)
    }

    $files = New-Object 'System.Collections.Generic.List[string]'
    foreach ($relativePath in $gitFiles) {
        $fullPath = Join-Path $repoRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $extension = [System.IO.Path]::GetExtension($fullPath)
        if (-not $allowedExtensions.Contains($extension)) {
            continue
        }

        $resolvedPath = (Resolve-Path -LiteralPath $fullPath).Path
        if ($resolvedPath.StartsWith($resolvedSourceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $files.Add($resolvedPath)
        }
    }

    return @($files | Sort-Object)
}

function New-Rule {
    param(
        [string]$Tag,
        [string]$Code,
        [string]$Pattern,
        [string]$Description,
        [string[]]$Extensions
    )

    [pscustomobject]@{
        Tag = $Tag
        Code = $Code
        Pattern = $Pattern
        Description = $Description
        Extensions = $Extensions
    }
}

$rules = @(
    New-Rule 'SqlServerOnly' 'SQLCLIENT-USING' 'System\.Data\.SqlClient|SqlConnection|SqlCommand|SqlParameter|SqlDbType|SqlDataReader|SqlTransaction' '直接依赖 SQL Server ADO.NET 类型。 Direct dependency on SQL Server ADO.NET types.' @('.cs')
    New-Rule 'SqlServerOnly' 'EF-SQLSERVER' 'EntityFramework\.SqlServer' 'EF6 SQL Server provider 引用。 EF6 SQL Server provider reference.' @('.csproj', '.config')
    New-Rule 'SqlServerOnly' 'SQLSERVER-SCHEMA' '\[dbo\]|\bdbo\.' 'SQL Server schema qualifier。 SQL Server schema qualifier.' @('.cs', '.sql')
    New-Rule 'SqlServerOnly' 'SQLSERVER-METADATA' 'OBJECT_ID|sys\.objects|sys\.indexes|SERVERPROPERTY|DB_NAME\(\)' 'SQL Server catalog / metadata API。 SQL Server catalog or metadata API.' @('.cs', '.sql', '.ps1')
    New-Rule 'SqlServerOnly' 'SQLSERVER-TYPES' 'IDENTITY\s*\(|ROWVERSION|UNIQUEIDENTIFIER|NVARCHAR|VARBINARY|DATETIME2|BIT\b' 'SQL Server DDL 类型或列属性。 SQL Server DDL type or column attribute.' @('.sql', '.cs')
    New-Rule 'SqlServerOnly' 'SQLSERVER-FUNCTIONS' 'SYSUTCDATETIME\(\)|GETUTCDATE\(\)|GETDATE\(\)|SCOPE_IDENTITY\(\)|NEWID\(\)' 'SQL Server scalar function。 SQL Server scalar function.' @('.sql', '.cs')
    New-Rule 'SqlServerOnly' 'SQLSERVER-BATCH-IDENTITY' 'GO\b|SET IDENTITY_INSERT|USE \[|USE master|sp_addlogin|sp_grantlogin|sp_grantdbaccess|sp_addrolemember' 'SQL Server 批处理、库上下文或历史授权语句。 SQL Server batch, database-context, or legacy grant statement.' @('.sql')
    New-Rule 'NeedsDialect' 'DIALECT-TOP' 'SELECT\s+TOP\s*(\(|\d|@)' '分页/限量语法需要 provider 方言。 Row limiting requires provider dialect handling.' @('.cs', '.sql')
    New-Rule 'NeedsDialect' 'DIALECT-LOCK-HINT' 'WITH\s*\((UPDLOCK|HOLDLOCK|NOLOCK)' 'SQL Server lock hint 需要替换或抽象。 SQL Server lock hint needs replacement or abstraction.' @('.cs', '.sql')
    New-Rule 'NeedsDialect' 'DIALECT-OUTPUT-TABLEVAR' 'OUTPUT\s+INSERTED|DECLARE\s+@Inserted\s+TABLE' 'SQL Server OUTPUT/table variable 返回键模式。 SQL Server OUTPUT/table-variable key-return pattern.' @('.cs', '.sql')
    New-Rule 'NeedsDialect' 'DIALECT-LIKE-CONCAT' 'LIKE\s+@|LIKE\s+N?''%' 'LIKE 与通配符拼接需按 provider 校验大小写、排序和转义。 LIKE patterns require provider-specific collation and escaping review.' @('.cs', '.sql')
    New-Rule 'PortableCandidate' 'ADO-FACTORY' 'DbProviderFactories|DbProviderFactory|DbConnection|PortalDatabaseProfile|IPortalDbConnectionFactory' '可用于 provider 抽象的 ADO.NET profile/factory。 ADO.NET profile/factory available for provider abstraction.' @('.cs')
    New-Rule 'PortableCandidate' 'SIMPLE-HEALTH-SQL' 'SELECT\s+1' '简单连接检查 SQL，有机会按 provider 保持通用。 Simple connection-check SQL may stay portable.' @('.cs', '.sql')
    New-Rule 'PortableCandidate' 'CONFIG-PROVIDERNAME' 'providerName|ProviderInvariantName|PortalDatabaseProviderNames' '配置已携带 provider 标识。 Configuration carries provider identity.' @('.cs', '.config')
    New-Rule 'ProviderProof' 'SQLITE-PROOF' 'System\.Data\.SQLite|PortalDataProviderProof|Providers[\\/]+SQLite|PortalDatabaseProviderNames\.Sqlite|ProviderProof' 'SQLite 独立 proof 边界。 SQLite standalone proof boundary.' @('.cs', '.sql', '.csproj', '.config')
)

$files = Get-TrackedInventoryFiles
$matches = New-Object 'System.Collections.Generic.List[object]'
$fileSummary = @{}

foreach ($file in $files) {
    $extension = [System.IO.Path]::GetExtension($file)
    $relativePath = Get-RepoRelativePath -Path $file

    foreach ($rule in $rules) {
        if ($rule.Extensions -and $rule.Extensions.Count -gt 0 -and -not ($rule.Extensions -contains $extension)) {
            continue
        }

        $hits = @(Select-String -LiteralPath $file -Pattern $rule.Pattern -AllMatches -ErrorAction SilentlyContinue)
        if ($hits.Count -eq 0) {
            continue
        }

        if (-not $fileSummary.ContainsKey($relativePath)) {
            $fileSummary[$relativePath] = [pscustomobject]@{
                Path = $relativePath
                Tags = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
                RuleCodes = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
                MatchCount = 0
            }
        }

        [void]$fileSummary[$relativePath].Tags.Add($rule.Tag)
        [void]$fileSummary[$relativePath].RuleCodes.Add($rule.Code)
        $fileSummary[$relativePath].MatchCount += $hits.Count

        foreach ($hit in $hits | Select-Object -First $SampleLimit) {
            $matches.Add([pscustomobject]@{
                    Tag = $rule.Tag
                    Code = $rule.Code
                    Path = $relativePath
                    Line = $hit.LineNumber
                    Evidence = Protect-EvidenceText -Text $hit.Line
                    Description = $rule.Description
                })
        }
    }
}

$tagSummary = @()
foreach ($tag in @('SqlServerOnly', 'NeedsDialect', 'PortableCandidate', 'ProviderProof')) {
    $tagFiles = @($fileSummary.Values | Where-Object { $_.Tags.Contains($tag) })
    $tagMatches = @($matches | Where-Object { $_.Tag -eq $tag })
    $tagSummary += [pscustomobject]@{
        Tag = $tag
        FileCount = $tagFiles.Count
        SampledMatchCount = $tagMatches.Count
    }
}

$ruleSummary = @()
foreach ($rule in $rules) {
    $ruleMatches = @($matches | Where-Object { $_.Code -eq $rule.Code })
    $ruleFiles = @($ruleMatches | Select-Object -ExpandProperty Path -Unique)
    $ruleSummary += [pscustomobject]@{
        Tag = $rule.Tag
        Code = $rule.Code
        FileCount = $ruleFiles.Count
        SampledMatchCount = $ruleMatches.Count
        Description = $rule.Description
    }
}

$topFiles = @(
    $fileSummary.Values |
        Sort-Object @{ Expression = 'MatchCount'; Descending = $true }, Path |
        Select-Object -First 30 |
        ForEach-Object {
            [pscustomobject]@{
                Path = $_.Path
                MatchCount = $_.MatchCount
                Tags = @($_.Tags | Sort-Object)
                RuleCodes = @($_.RuleCodes | Sort-Object)
            }
        }
)

$result = [pscustomobject]@{
    GeneratedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [System.Globalization.CultureInfo]::InvariantCulture)
    SourceRoot = Get-RepoRelativePath -Path $resolvedSourceRoot
    ScannedFileCount = $files.Count
    TagSummary = $tagSummary
    RuleSummary = $ruleSummary
    TopFiles = $topFiles
    Samples = @($matches | Sort-Object Tag, Code, Path, Line | Select-Object -First 300)
}

Write-Host ('Scanned files: {0}' -f $files.Count)
foreach ($summary in $tagSummary) {
    Write-Host ('{0}: files={1}; samples={2}' -f $summary.Tag, $summary.FileCount, $summary.SampledMatchCount)
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $json = $result | ConvertTo-Json -Depth 8
    Write-Utf8NoBomFile -Path $OutputJson -Content ($json + [Environment]::NewLine)
    Write-Host ('Wrote JSON: {0}' -f $OutputJson)
}

if ($tagSummary.Where({ $_.Tag -eq 'SqlServerOnly' }).FileCount -eq 0) {
    throw 'No SQL Server data-access evidence was found; the inventory rules may be misconfigured.'
}
