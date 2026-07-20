[CmdletBinding()]
param(
    [string]$RootPath,

    [string]$SourcePath,

    [string]$SetupPath,

    [string]$DocsPath,

    [ValidateSet('Dev', 'Test', 'Prod', 'Scan')]
    [string]$Profile = 'Dev',

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

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $repoRoot 'src'
}

if ([string]::IsNullOrWhiteSpace($SetupPath)) {
    $SetupPath = Join-Path $repoRoot 'src/Setup'
}

if ([string]::IsNullOrWhiteSpace($DocsPath)) {
    $DocsPath = Join-Path $repoRoot 'docs'
}

$resolvedSourcePath = (Resolve-Path -LiteralPath $SourcePath).Path
$resolvedSetupPath = (Resolve-Path -LiteralPath $SetupPath).Path
$resolvedDocsPath = (Resolve-Path -LiteralPath $DocsPath).Path
$findings = New-Object 'System.Collections.Generic.List[object]'

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

function Get-Utf8Text {
    param([string]$LiteralPath)

    return [System.IO.File]::ReadAllText($LiteralPath, [System.Text.Encoding]::UTF8)
}

function Get-RelativeDisplayPath {
    param([string]$LiteralPath)

    $fullPath = [System.IO.Path]::GetFullPath($LiteralPath)
    $rootPrefix = $repoRoot.TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($rootPrefix.Length)
    }

    return $fullPath
}

function Add-CredentialRiskFinding {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Severity,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    $findings.Add([pscustomobject]@{
            Severity = $Severity
            Code     = $Code
            Message  = $Message
            Evidence = $Evidence
        })

    $color = switch ($Severity) {
        'Pass' { 'Green' }
        'Warning' { 'Yellow' }
        'Fail' { 'Red' }
        default { 'Gray' }
    }

    Write-Host ('[{0}] {1}: {2}' -f $Severity.ToUpperInvariant(), $Code, $Message) -ForegroundColor $color
    if (-not [string]::IsNullOrWhiteSpace($Evidence)) {
        Write-Host ('       {0}' -f $Evidence) -ForegroundColor DarkGray
    }
}

function Get-ReviewFiles {
    param([string[]]$Roots)

    $extensions = @('.aspx', '.ascx', '.ashx', '.config', '.cs', '.json', '.master', '.md', '.ps1', '.sql', '.xml')

    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        Get-ChildItem -LiteralPath $root -Recurse -File |
            Where-Object {
                $extensions -contains $_.Extension.ToLowerInvariant() -and
                $_.FullName -notmatch '\\(bin|obj|Documentation|DoxyGen|Demo|node_modules)\\'
            }
    }
}

function Get-SanitizedEvidence {
    param([Microsoft.PowerShell.Commands.MatchInfo]$Match)

    $line = $Match.Line.Trim()
    $line = [regex]::Replace($line, '(?i)(admin)\s*/\s*(admin)', '$1/***')
    $line = [regex]::Replace($line, '(?i)(password|passwd|pwd|密码|口令)(\s*[:=]\s*)\S+', '$1$2***')
    $line = [regex]::Replace($line, "N'([0-9A-Fa-f]{2}-){8,}[0-9A-Fa-f]{2}'", "N'***'")
    if ($line.Length -gt 160) {
        $line = $line.Substring(0, 157) + '...'
    }

    return ('{0}:{1}: {2}' -f (Get-RelativeDisplayPath -LiteralPath $Match.Path), $Match.LineNumber, $line)
}

function Find-ReviewMatches {
    param(
        [string[]]$Roots,
        [string]$Pattern,
        [int]$Limit = 10
    )

    $matches = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in Get-ReviewFiles -Roots $Roots) {
        foreach ($match in Select-String -LiteralPath $file.FullName -Pattern $Pattern -AllMatches -ErrorAction SilentlyContinue) {
            $matches.Add((Get-SanitizedEvidence -Match $match))
            if ($matches.Count -ge $Limit) {
                return $matches
            }
        }
    }

    return $matches
}

function Test-TextContains {
    param(
        [string]$LiteralPath,
        [string]$Pattern
    )

    if (-not (Test-Path -LiteralPath $LiteralPath)) {
        return $false
    }

    return [regex]::IsMatch((Get-Utf8Text -LiteralPath $LiteralPath), $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

Write-Host ('PROFILE: {0}' -f $Profile)
Write-Host 'MODE: static source/setup/document scan only; no HTTP login attempt and no database connection.'

$loadDataPath = Join-Path $resolvedSetupPath 'Portal_LoadData.sql'
$credentialScriptPath = Join-Path $resolvedSetupPath 'Portal_UserCredentials.sql'
$deploymentCredentialDocPath = Join-Path $resolvedDocsPath 'deployment-default-credentials.md'
$deploymentChecklistPath = Join-Path $resolvedDocsPath 'deployment-checklist.md'
$testingChecklistPath = Join-Path $resolvedDocsPath 'testing-checklist.md'

if (Test-TextContains -LiteralPath $loadDataPath -Pattern "INSERT\s+\[Portal_Users\].*\[Password\].*N'admin'") {
    $seedLine = Select-String -LiteralPath $loadDataPath -Pattern "INSERT\s+\[Portal_Users\].*\[Password\].*N'admin'" | Select-Object -First 1
    Add-CredentialRiskFinding -Severity Warning -Code 'SEED-ADMIN-LEGACY' -Message 'Legacy sample admin seed remains in Portal_LoadData.sql; shared, test, and production deployments must replace or disable it before use.' -Evidence (Get-SanitizedEvidence -Match $seedLine)
}
else {
    Add-CredentialRiskFinding -Severity Pass -Code 'SEED-ADMIN-LEGACY' -Message 'No legacy sample admin seed was found in Portal_LoadData.sql.'
}

$credentialSeedMatches = @(Find-ReviewMatches -Roots @($resolvedSetupPath) -Pattern 'INSERT\s+(INTO\s+)?(\[dbo\]\.)?\[Portal_UserCredentials\]' -Limit 8)
if ($credentialSeedMatches.Count -gt 0) {
    Add-CredentialRiskFinding -Severity Fail -Code 'SEED-STRONG-CREDENTIALS' -Message 'Credential table seed statements were found; seeded strong hashes must be reviewed and usually removed.' -Evidence ($credentialSeedMatches -join '; ')
}
else {
    Add-CredentialRiskFinding -Severity Pass -Code 'SEED-STRONG-CREDENTIALS' -Message 'No Portal_UserCredentials password seed statements were found in Setup scripts.'
}

$plainDefaultPatterns = '(?i)(admin\s*/\s*admin|admin:admin|password\s*[:=]\s*(admin|admin123|password|123456|12345678)|pwd\s*[:=]\s*(admin|admin123|password|123456|12345678)|默认(密码|口令).{0,60}(admin|123456)|N''(admin|admin123|password|123456|12345678)''\s*,\s*N''(admin|admin123|password|123456|12345678)'')'
$plainDefaultMatches = @(Find-ReviewMatches -Roots @($resolvedSetupPath, $resolvedDocsPath) -Pattern $plainDefaultPatterns -Limit 12)
if ($plainDefaultMatches.Count -gt 0) {
    Add-CredentialRiskFinding -Severity Warning -Code 'DOC-DEFAULT-CREDENTIALS' -Message 'Default credential wording was found in Setup/docs and must be treated as local-only legacy guidance.' -Evidence ($plainDefaultMatches -join '; ')
}
else {
    Add-CredentialRiskFinding -Severity Pass -Code 'DOC-DEFAULT-CREDENTIALS' -Message 'No obvious default credential wording was found in Setup/docs.'
}

$hardcodedSourceMatches = @(Find-ReviewMatches -Roots @($resolvedSourcePath) -Pattern '(?i)(DefaultPassword|AdminPassword|SeedPassword|Password)\s*=\s*["''](admin|admin123|password|123456|12345678)["'']' -Limit 12)
if ($hardcodedSourceMatches.Count -gt 0) {
    Add-CredentialRiskFinding -Severity Fail -Code 'SRC-HARDCODED-PASSWORD' -Message 'Potential hard-coded default password assignments were found in source.' -Evidence ($hardcodedSourceMatches -join '; ')
}
else {
    Add-CredentialRiskFinding -Severity Pass -Code 'SRC-HARDCODED-PASSWORD' -Message 'No obvious hard-coded default password assignments were found in source.'
}

$legacyEncryptMatches = @(Find-ReviewMatches -Roots @($resolvedSourcePath) -Pattern 'PortalSecurity\.Encrypt\s*\(' -Limit 8)
if ($legacyEncryptMatches.Count -gt 0) {
    Add-CredentialRiskFinding -Severity Warning -Code 'PWD-LEGACY-MD5-COMPAT' -Message 'Legacy MD5 encrypt references remain; current acceptance is compatibility-only and must stay out of new/reset credential paths.' -Evidence ($legacyEncryptMatches -join '; ')
}
else {
    Add-CredentialRiskFinding -Severity Pass -Code 'PWD-LEGACY-MD5-COMPAT' -Message 'No legacy PortalSecurity.Encrypt callers were found in source scope.'
}

$passwordPolicyPath = Join-Path $resolvedSourcePath 'Portal.Components/PortalPasswordPolicy.cs'
if ((Test-TextContains -LiteralPath $passwordPolicyPath -Pattern '"admin"') -and
    (Test-TextContains -LiteralPath $passwordPolicyPath -Pattern '"admin123"')) {
    Add-CredentialRiskFinding -Severity Pass -Code 'PWD-WEAK-DICTIONARY' -Message 'Weak password dictionary includes common administrator defaults.' -Evidence 'PortalPasswordPolicy.cs'
}
else {
    Add-CredentialRiskFinding -Severity Warning -Code 'PWD-WEAK-DICTIONARY' -Message 'Weak password dictionary does not show expected administrator default markers.'
}

if ((Test-TextContains -LiteralPath $deploymentCredentialDocPath -Pattern '默认凭据|default credential') -and
    (Test-TextContains -LiteralPath $deploymentChecklistPath -Pattern 'Test-PortalDefaultCredentialRisk\.ps1') -and
    (Test-TextContains -LiteralPath $testingChecklistPath -Pattern 'Test-PortalDefaultCredentialRisk\.ps1')) {
    Add-CredentialRiskFinding -Severity Pass -Code 'DOC-DEPLOYMENT-GOVERNANCE' -Message 'Deployment and testing documents reference default credential governance and the read-only scan script.'
}
else {
    Add-CredentialRiskFinding -Severity Warning -Code 'DOC-DEPLOYMENT-GOVERNANCE' -Message 'Default credential governance documentation or checklist links need review.'
}

if (Test-TextContains -LiteralPath $credentialScriptPath -Pattern 'RequiresReset') {
    Add-CredentialRiskFinding -Severity Pass -Code 'PWD-RESET-MARKER' -Message 'Credential schema has a RequiresReset marker for later forced-reset flow design.' -Evidence 'src/Setup/Portal_UserCredentials.sql'
}
else {
    Add-CredentialRiskFinding -Severity Warning -Code 'PWD-RESET-MARKER' -Message 'Credential schema does not expose a RequiresReset marker.'
}

Add-CredentialRiskFinding -Severity Info -Code 'SCAN-NO-AUTH-PROBE' -Message 'This script intentionally does not try default passwords over HTTP; interactive or VM login checks must use explicit secret files.'

$failCount = @($findings | Where-Object { $_.Severity -eq 'Fail' }).Count
$warningCount = @($findings | Where-Object { $_.Severity -eq 'Warning' }).Count
$infoCount = @($findings | Where-Object { $_.Severity -eq 'Info' }).Count
$passCount = @($findings | Where-Object { $_.Severity -eq 'Pass' }).Count

$summary = [pscustomobject]@{
    Profile = $Profile
    SourcePath = $resolvedSourcePath
    SetupPath = $resolvedSetupPath
    DocsPath = $resolvedDocsPath
    GeneratedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Counts = [pscustomobject]@{
        Pass = $passCount
        Warning = $warningCount
        Fail = $failCount
        Info = $infoCount
    }
    Findings = $findings
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

Write-Host ('SUMMARY: Pass={0}; Warning={1}; Fail={2}; Info={3}' -f $passCount, $warningCount, $failCount, $infoCount)

if ($failCount -gt 0 -or ($FailOnWarning -and $warningCount -gt 0)) {
    exit 1
}

exit 0
