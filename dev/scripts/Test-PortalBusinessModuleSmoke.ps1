[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ModuleName,

    [string]$ModuleDirectory,

    [string]$ExpectedPackageId,

    [string]$ExpectedDesktopEntry,

    [string]$SqlMigrationFile,

    [switch]$AllowModuleScripts,

    [switch]$SkipSqlMigrationCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$portalRoot = Join-Path $repoRoot 'src\Portal'
$setupRoot = Join-Path $repoRoot 'src\Setup'
$hasFailures = $false
$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-BusinessModuleCheck {
    param(
        [string]$Name,
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Status,
        [string]$Detail
    )

    $script:checks.Add([pscustomobject]@{
            Name = $Name
            Status = $Status
            Detail = $Detail
        })

    if ($Status -eq 'Fail') {
        $script:hasFailures = $true
    }

    Write-Host ('[{0}] {1}: {2}' -f $Status.ToUpperInvariant(), $Name, $Detail)
}

function Get-FullPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Test-ChildPath {
    param(
        [string]$ParentPath,
        [string]$ChildPath
    )

    $parent = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $child = [System.IO.Path]::GetFullPath($ChildPath)
    return $child.StartsWith($parent + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-SafeRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    if ($Path -match '^[a-z]+:' -or $Path -match '^(?i:https?:)?//' -or [System.IO.Path]::IsPathRooted($Path)) {
        return $false
    }

    $segments = $Path -split '[\\/]'
    return -not ($segments | Where-Object { $_ -eq '..' })
}

$moduleRoot = if ([string]::IsNullOrWhiteSpace($ModuleDirectory)) {
    Join-Path $portalRoot ('DesktopModules\' + $ModuleName)
}
else {
    Get-FullPath -Path $ModuleDirectory
}

if (-not (Test-Path -LiteralPath $moduleRoot -PathType Container)) {
    Add-BusinessModuleCheck -Name 'Module directory' -Status 'Fail' -Detail ('Directory not found: ' + $moduleRoot)
}
else {
    Add-BusinessModuleCheck -Name 'Module directory' -Status 'Pass' -Detail ('Found ' + $moduleRoot)
}

$manifestPath = Join-Path $moduleRoot 'module.json'
$manifest = $null
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    Add-BusinessModuleCheck -Name 'Module manifest' -Status 'Fail' -Detail 'module.json was not found.'
}
else {
    try {
        $manifestText = [System.IO.File]::ReadAllText($manifestPath, [System.Text.UTF8Encoding]::new($false))
        $manifest = $manifestText | ConvertFrom-Json
        Add-BusinessModuleCheck -Name 'Module manifest' -Status 'Pass' -Detail 'module.json is valid JSON.'
    }
    catch {
        Add-BusinessModuleCheck -Name 'Module manifest' -Status 'Fail' -Detail ('module.json parse failed: ' + $_.Exception.Message)
    }
}

if ($null -ne $manifest) {
    foreach ($propertyName in @('schemaVersion', 'packageId', 'displayName', 'version', 'desktopEntry')) {
        if (-not ($manifest.PSObject.Properties.Name -contains $propertyName) -or [string]::IsNullOrWhiteSpace([string]$manifest.$propertyName)) {
            Add-BusinessModuleCheck -Name ('Manifest property ' + $propertyName) -Status 'Fail' -Detail 'Required manifest property is missing or empty.'
        }
        else {
            Add-BusinessModuleCheck -Name ('Manifest property ' + $propertyName) -Status 'Pass' -Detail ([string]$manifest.$propertyName)
        }
    }

    if ($manifest.schemaVersion -ne 1) {
        Add-BusinessModuleCheck -Name 'Manifest schema version' -Status 'Warning' -Detail 'Current standard expects schemaVersion 1.'
    }

    $expectedPackage = if ([string]::IsNullOrWhiteSpace($ExpectedPackageId)) { 'HIA.' + $ModuleName } else { $ExpectedPackageId }
    if ($manifest.packageId -ne $expectedPackage) {
        Add-BusinessModuleCheck -Name 'Package id convention' -Status 'Warning' -Detail ('Expected ' + $expectedPackage + ', actual ' + $manifest.packageId + '.')
    }
    elseif ($manifest.packageId -notmatch '^HIA\.[A-Za-z][A-Za-z0-9.]*$') {
        Add-BusinessModuleCheck -Name 'Package id convention' -Status 'Warning' -Detail 'Package id does not follow HIA.{ModuleName}-style naming.'
    }
    else {
        Add-BusinessModuleCheck -Name 'Package id convention' -Status 'Pass' -Detail ([string]$manifest.packageId)
    }

    if ($manifest.version -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') {
        Add-BusinessModuleCheck -Name 'Module version' -Status 'Warning' -Detail 'Version is not a SemVer-like value.'
    }

    $expectedEntry = if ([string]::IsNullOrWhiteSpace($ExpectedDesktopEntry)) {
        'DesktopModules/' + $ModuleName + '/' + $ModuleName + '.ascx'
    }
    else {
        $ExpectedDesktopEntry
    }

    if ($manifest.desktopEntry -ne $expectedEntry) {
        Add-BusinessModuleCheck -Name 'Desktop entry convention' -Status 'Warning' -Detail ('Expected ' + $expectedEntry + ', actual ' + $manifest.desktopEntry + '.')
    }

    if (-not (Test-SafeRelativePath -Path ([string]$manifest.desktopEntry))) {
        Add-BusinessModuleCheck -Name 'Desktop entry safety' -Status 'Fail' -Detail 'desktopEntry must be a safe in-site relative path.'
    }
    elseif ($manifest.desktopEntry -notlike ('DesktopModules/' + $ModuleName + '/*')) {
        Add-BusinessModuleCheck -Name 'Desktop entry safety' -Status 'Fail' -Detail 'desktopEntry must stay inside the module directory.'
    }
    elseif ($manifest.desktopEntry -notlike '*.ascx') {
        Add-BusinessModuleCheck -Name 'Desktop entry safety' -Status 'Fail' -Detail 'desktopEntry must point to an .ascx control.'
    }
    else {
        $entryPath = Join-Path $portalRoot (($manifest.desktopEntry -replace '/', '\'))
        if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
            Add-BusinessModuleCheck -Name 'Desktop entry file' -Status 'Fail' -Detail ('Entry file not found: ' + $entryPath)
        }
        elseif (-not (Test-ChildPath -ParentPath $moduleRoot -ChildPath $entryPath)) {
            Add-BusinessModuleCheck -Name 'Desktop entry file' -Status 'Fail' -Detail 'Entry file is outside the module directory.'
        }
        else {
            Add-BusinessModuleCheck -Name 'Desktop entry file' -Status 'Pass' -Detail ([string]$manifest.desktopEntry)
        }
    }

    $resources = @()
    if ($manifest.PSObject.Properties.Name -contains 'resources' -and $null -ne $manifest.resources) {
        $resources = @($manifest.resources)
    }

    foreach ($resource in $resources) {
        $resourceText = [string]$resource
        if (-not (Test-SafeRelativePath -Path $resourceText)) {
            Add-BusinessModuleCheck -Name 'Manifest resource safety' -Status 'Fail' -Detail ('Unsafe resource path: ' + $resourceText)
            continue
        }

        if (-not $AllowModuleScripts -and [System.IO.Path]::GetExtension($resourceText).Equals('.js', [System.StringComparison]::OrdinalIgnoreCase)) {
            Add-BusinessModuleCheck -Name 'Manifest resource script policy' -Status 'Fail' -Detail ('Module script is not allowed by default: ' + $resourceText)
            continue
        }

        $resourcePath = Join-Path $moduleRoot (($resourceText -replace '/', '\'))
        if (-not (Test-Path -LiteralPath $resourcePath -PathType Leaf)) {
            Add-BusinessModuleCheck -Name 'Manifest resource file' -Status 'Fail' -Detail ('Resource not found: ' + $resourceText)
        }
        elseif (-not (Test-ChildPath -ParentPath $moduleRoot -ChildPath $resourcePath)) {
            Add-BusinessModuleCheck -Name 'Manifest resource file' -Status 'Fail' -Detail ('Resource escapes module directory: ' + $resourceText)
        }
        else {
            Add-BusinessModuleCheck -Name 'Manifest resource file' -Status 'Pass' -Detail $resourceText
        }
    }
}

if (Test-Path -LiteralPath $moduleRoot -PathType Container) {
    $dangerousFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File | Where-Object {
            $_.Extension -in @('.dll', '.exe', '.zip', '.cmd', '.bat', '.ps1', '.psm1')
        })

    if ($dangerousFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Module package static asset policy' -Status 'Fail' -Detail ('Disallowed files: ' + (($dangerousFiles | ForEach-Object { $_.FullName.Substring($moduleRoot.Length).TrimStart('\') }) -join ', '))
    }
    else {
        Add-BusinessModuleCheck -Name 'Module package static asset policy' -Status 'Pass' -Detail 'No DLL, ZIP, executable or script package files were found.'
    }

    $scriptFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File -Filter '*.js')
    if (-not $AllowModuleScripts -and $scriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Module script policy' -Status 'Fail' -Detail ('JavaScript files are not allowed by default: ' + (($scriptFiles | ForEach-Object { $_.FullName.Substring($moduleRoot.Length).TrimStart('\') }) -join ', '))
    }
    elseif ($scriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Module script policy' -Status 'Warning' -Detail 'Module scripts are allowed only because AllowModuleScripts was specified.'
    }
    else {
        Add-BusinessModuleCheck -Name 'Module script policy' -Status 'Pass' -Detail 'No module JavaScript files were found.'
    }

    $ascxFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File -Filter '*.ascx')
    $inlineScriptFiles = @()
    foreach ($ascxFile in $ascxFiles) {
        $ascxText = [System.IO.File]::ReadAllText($ascxFile.FullName, [System.Text.UTF8Encoding]::new($false))
        if ($ascxText -match '(?is)<script\b') {
            $inlineScriptFiles += $ascxFile
        }
    }

    if (-not $AllowModuleScripts -and $inlineScriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Inline script policy' -Status 'Fail' -Detail ('Inline script block found in: ' + (($inlineScriptFiles | ForEach-Object { $_.FullName.Substring($moduleRoot.Length).TrimStart('\') }) -join ', '))
    }
    elseif ($inlineScriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Inline script policy' -Status 'Warning' -Detail 'Inline script blocks are allowed only because AllowModuleScripts was specified.'
    }
    else {
        Add-BusinessModuleCheck -Name 'Inline script policy' -Status 'Pass' -Detail 'No inline script blocks were found.'
    }
}

if (-not $SkipSqlMigrationCheck) {
    $migrationFiles = @()
    if (-not [string]::IsNullOrWhiteSpace($SqlMigrationFile)) {
        $migrationFiles = @(Get-FullPath -Path $SqlMigrationFile)
    }
    else {
        $migrationFiles = @(Get-ChildItem -LiteralPath $setupRoot -File -Filter ('PortalBiz_' + $ModuleName + '*.sql') | ForEach-Object { $_.FullName })
    }

    if ($migrationFiles.Count -eq 0) {
        Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Fail' -Detail ('No PortalBiz_' + $ModuleName + '*.sql file was found.')
    }
    else {
        foreach ($migrationFile in $migrationFiles) {
            if (-not (Test-Path -LiteralPath $migrationFile -PathType Leaf)) {
                Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Fail' -Detail ('Migration file not found: ' + $migrationFile)
                continue
            }

            $fileName = Split-Path -Leaf $migrationFile
            if ($fileName -notlike 'PortalBiz_*.sql') {
                Add-BusinessModuleCheck -Name 'SQL migration naming' -Status 'Warning' -Detail ('Business migration should use PortalBiz_*.sql: ' + $fileName)
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration naming' -Status 'Pass' -Detail $fileName
            }

            $sqlText = [System.IO.File]::ReadAllText($migrationFile, [System.Text.UTF8Encoding]::new($false))
            if ($sqlText -match '(?im)^\s*USE\s+\[') {
                Add-BusinessModuleCheck -Name 'SQL migration portability' -Status 'Fail' -Detail ($fileName + ' contains USE [database].')
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration portability' -Status 'Pass' -Detail ($fileName + ' does not force a database context.')
            }

            if ($sqlText -match '(?im)^\s*(DROP\s+TABLE|TRUNCATE\s+TABLE|ALTER\s+DATABASE)\b') {
                Add-BusinessModuleCheck -Name 'SQL migration destructive statement' -Status 'Fail' -Detail ($fileName + ' contains destructive database statements.')
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration destructive statement' -Status 'Pass' -Detail ($fileName + ' has no obvious destructive database statement.')
            }

            if ($sqlText -notmatch '(?i)OBJECT_ID\s*\(') {
                Add-BusinessModuleCheck -Name 'SQL migration idempotency hint' -Status 'Warning' -Detail ($fileName + ' does not contain OBJECT_ID guard; verify idempotency manually.')
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration idempotency hint' -Status 'Pass' -Detail ($fileName + ' contains OBJECT_ID guard.')
            }
        }
    }

    $sqlCompatibilityScript = Join-Path $PSScriptRoot 'Test-PortalSqlCompatibility.ps1'
    $compatibilityText = [System.IO.File]::ReadAllText($sqlCompatibilityScript, [System.Text.UTF8Encoding]::new($false))
    if ($compatibilityText -match 'ApplyP6BusinessModuleMigration' -and $compatibilityText -match 'RequireP6BusinessModuleMigration') {
        Add-BusinessModuleCheck -Name 'SQL compatibility entry' -Status 'Pass' -Detail 'P6 business-module apply/require switches exist.'
    }
    else {
        Add-BusinessModuleCheck -Name 'SQL compatibility entry' -Status 'Fail' -Detail 'P6 business-module apply/require switches were not found.'
    }
}
else {
    Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Info' -Detail 'Skipped by SkipSqlMigrationCheck.'
}

$failed = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
$warnings = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count

$summary = [pscustomobject]@{
    ModuleName = $ModuleName
    ModuleDirectory = $moduleRoot
    TotalChecks = $checks.Count
    FailedChecks = $failed
    WarningChecks = $warnings
    Checks = $checks
}

$summary

if ($hasFailures) {
    exit 1
}
