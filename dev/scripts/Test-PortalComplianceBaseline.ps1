[CmdletBinding()]
param(
    [string]$PortalPath,

    [string]$SourcePath,

    [string]$SetupPath,

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

if ([string]::IsNullOrWhiteSpace($PortalPath)) {
    $PortalPath = Join-Path $repoRoot 'src/Portal'
}

if ([string]::IsNullOrWhiteSpace($SetupPath)) {
    $SetupPath = Join-Path $repoRoot 'src/Setup'
}

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $repoRoot 'src'
}

$resolvedPortalPath = (Resolve-Path -LiteralPath $PortalPath).Path
$resolvedSourcePath = (Resolve-Path -LiteralPath $SourcePath).Path
$resolvedSetupPath = (Resolve-Path -LiteralPath $SetupPath).Path
$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-ComplianceCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Severity,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject]@{
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

function Get-Utf8Text {
    param([string]$LiteralPath)

    return Get-Content -LiteralPath $LiteralPath -Encoding UTF8 -Raw
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

function Get-PortalSourceFiles {
    $extensions = @('.aspx', '.ascx', '.ashx', '.cs', '.config', '.master')

    Get-ChildItem -LiteralPath $resolvedSourcePath -Recurse -File |
        Where-Object {
            $extensions -contains $_.Extension.ToLowerInvariant() -and
            $_.FullName -notmatch '\\(bin|obj|Documentation|DoxyGen|Demo)\\'
        }
}

function Find-PortalSourceMatch {
    param(
        [string]$Pattern,
        [int]$Limit = 8
    )

    $matches = New-Object 'System.Collections.Generic.List[string]'

    foreach ($file in Get-PortalSourceFiles) {
        $relativePath = $file.FullName.Substring($repoRoot.Length + 1)

        foreach ($match in Select-String -LiteralPath $file.FullName -Pattern $Pattern -AllMatches -ErrorAction SilentlyContinue) {
            $matches.Add(('{0}:{1}: {2}' -f $relativePath, $match.LineNumber, $match.Line.Trim()))
            if ($matches.Count -ge $Limit) {
                return $matches
            }
        }
    }

    return $matches
}

# 中文：先做只读基线检查，不主动扫描漏洞、不写数据库，也不改变 Web.config。
# English: This script is a read-only baseline check; it does not attack, write the database, or modify Web.config.
$webConfigPath = Join-Path $resolvedPortalPath 'Web.config'
if (-not (Test-Path -LiteralPath $webConfigPath)) {
    Add-ComplianceCheck -Severity Fail -Code 'CFG-001' -Message 'Web.config was not found.' -Evidence $webConfigPath
}
else {
    Add-ComplianceCheck -Severity Pass -Code 'CFG-001' -Message 'Web.config exists.' -Evidence $webConfigPath

    [xml]$webConfig = Get-Utf8Text -LiteralPath $webConfigPath
    $headerNodes = $webConfig.SelectNodes('/configuration/system.webServer/httpProtocol/customHeaders/add')
    $headers = @{}
    foreach ($node in $headerNodes) {
        $headers[$node.GetAttribute('name')] = $node.GetAttribute('value')
    }

    $requiredHeaders = @(
        @{ Name = 'X-Content-Type-Options'; Pattern = '\bnosniff\b' },
        @{ Name = 'X-XSS-Protection'; Pattern = '^1;\s*mode=block$' },
        @{ Name = 'Content-Security-Policy'; Pattern = '\bdefault-src\b' },
        @{ Name = 'Strict-Transport-Security'; Pattern = '\bmax-age=' },
        @{ Name = 'Referrer-Policy'; Pattern = '.+' },
        @{ Name = 'X-Permitted-Cross-Domain-Policies'; Pattern = '.+' },
        @{ Name = 'X-Download-Options'; Pattern = '\bnoopen\b' },
        @{ Name = 'X-Frame-Options'; Pattern = '^(SAMEORIGIN|DENY)$' }
    )

    foreach ($requiredHeader in $requiredHeaders) {
        $name = $requiredHeader.Name
        if (-not $headers.ContainsKey($name)) {
            Add-ComplianceCheck -Severity Fail -Code ('HDR-{0}' -f $name) -Message ('Required security header is missing: {0}' -f $name)
            continue
        }

        $value = [string]$headers[$name]
        if ($value -notmatch $requiredHeader.Pattern) {
            Add-ComplianceCheck -Severity Warning -Code ('HDR-{0}' -f $name) -Message ('Security header value needs review: {0}' -f $name) -Evidence $value
        }
        else {
            Add-ComplianceCheck -Severity Pass -Code ('HDR-{0}' -f $name) -Message ('Security header is present: {0}' -f $name) -Evidence $value
        }
    }

    if ($headers.ContainsKey('Content-Security-Policy') -and $headers['Content-Security-Policy'] -match "unsafe-inline|unsafe-eval") {
        Add-ComplianceCheck -Severity Warning -Code 'HDR-CSP-COMPAT' -Message 'CSP currently keeps Web Forms compatibility exceptions.' -Evidence $headers['Content-Security-Policy']
    }

    $envNode = $webConfig.SelectSingleNode('/configuration/env')
    $envValue = if ($envNode -ne $null) { $envNode.GetAttribute('value') } else { '' }
    if ($envValue -ieq 'dev' -and $headers.ContainsKey('Strict-Transport-Security')) {
        Add-ComplianceCheck -Severity Warning -Code 'HDR-HSTS-ENV' -Message 'HSTS is present while the current config env is dev; P10.2 should split environment policy.' -Evidence $headers['Strict-Transport-Security']
    }

    $httpCookiesNode = $webConfig.SelectSingleNode('/configuration/system.web/httpCookies')
    if ($httpCookiesNode -ne $null -and $httpCookiesNode.GetAttribute('requireSSL') -ieq 'true') {
        Add-ComplianceCheck -Severity Pass -Code 'COOKIE-SSL' -Message 'Cookies are configured to require SSL.'
    }
    else {
        Add-ComplianceCheck -Severity Warning -Code 'COOKIE-SSL' -Message 'Cookies do not require SSL in the base config; production transform or deployment config must enforce it.'
    }

    $customErrorsNode = $webConfig.SelectSingleNode('/configuration/system.web/customErrors')
    if ($customErrorsNode -ne $null -and $customErrorsNode.GetAttribute('mode') -in @('RemoteOnly', 'On')) {
        Add-ComplianceCheck -Severity Pass -Code 'ERROR-CUSTOM' -Message 'Custom error handling is enabled for non-local users.' -Evidence $customErrorsNode.GetAttribute('mode')
    }
    else {
        Add-ComplianceCheck -Severity Warning -Code 'ERROR-CUSTOM' -Message 'Custom error handling mode should be reviewed for production.'
    }
}

$passwordPolicyPath = Join-Path $resolvedSourcePath 'Portal.Components/PortalPasswordPolicy.cs'
if ((Test-TextContains -LiteralPath $passwordPolicyPath -Pattern 'MinimumLength\s*=\s*8') -and
    (Test-TextContains -LiteralPath $passwordPolicyPath -Pattern 'RequiredCategoryCount\s*=\s*3')) {
    Add-ComplianceCheck -Severity Pass -Code 'PWD-POLICY-MIN' -Message 'Password policy currently meets the legacy-compatible 8 length / 3 category baseline.' -Evidence 'PortalPasswordPolicy.cs'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'PWD-POLICY-MIN' -Message 'Password policy baseline could not be verified from source.' -Evidence 'Expected MinimumLength = 8 and RequiredCategoryCount = 3.'
}

$passwordHasherPath = Join-Path $resolvedSourcePath 'Portal.Components.Data1/PortalPasswordHasher.cs'
if ((Test-TextContains -LiteralPath $passwordHasherPath -Pattern 'Rfc2898DeriveBytes') -and
    (Test-TextContains -LiteralPath $passwordHasherPath -Pattern 'HashAlgorithmName\.SHA256') -and
    (Test-TextContains -LiteralPath $passwordHasherPath -Pattern 'DefaultIterationCount\s*=\s*210000')) {
    Add-ComplianceCheck -Severity Pass -Code 'PWD-HASH-PBKDF2' -Message 'New credential hashing uses PBKDF2-HMAC-SHA256 with the current iteration baseline.' -Evidence 'PortalPasswordHasher.cs'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'PWD-HASH-PBKDF2' -Message 'PBKDF2-HMAC-SHA256 baseline could not be verified from source.'
}

$credentialScriptPath = Join-Path $resolvedSetupPath 'Portal_UserCredentials.sql'
if ((Test-TextContains -LiteralPath $credentialScriptPath -Pattern 'Portal_UserCredentials') -and
    (Test-TextContains -LiteralPath $credentialScriptPath -Pattern 'PasswordHash') -and
    (Test-TextContains -LiteralPath $credentialScriptPath -Pattern 'PasswordSalt')) {
    Add-ComplianceCheck -Severity Pass -Code 'PWD-SQL-CREDENTIALS' -Message 'Credential table migration script is present.' -Evidence 'src/Setup/Portal_UserCredentials.sql'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'PWD-SQL-CREDENTIALS' -Message 'Credential table migration script needs review.'
}

$operationAuditScriptPath = Join-Path $resolvedSetupPath 'PortalCfg_OperationAudits.sql'
if (Test-TextContains -LiteralPath $operationAuditScriptPath -Pattern 'PortalCfg_OperationAudits') {
    Add-ComplianceCheck -Severity Pass -Code 'AUDIT-SQL' -Message 'Operation audit table migration script is present.' -Evidence 'src/Setup/PortalCfg_OperationAudits.sql'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'AUDIT-SQL' -Message 'Operation audit migration script needs review.'
}

$clientEncryptionMatches = @(Find-PortalSourceMatch -Pattern 'JSEncrypt|LoginSecurityKey|RSAEncrypt|RSADecrypt')
if ($clientEncryptionMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Pass -Code 'TRANS-PWD-FIELD-ENCRYPT' -Message 'Client-side password field encryption markers were found.' -Evidence (($clientEncryptionMatches | Select-Object -First 3) -join '; ')
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'TRANS-PWD-FIELD-ENCRYPT' -Message 'Client-side login password field encryption is not implemented yet; P10.3 must cover it.' -Evidence 'Reference approach: IE6-compatible JSEncrypt + session-bound RSA private key.'
}

$signInPath = Join-Path $resolvedPortalPath 'DesktopModules/SignIn.ascx'
if (Test-TextContains -LiteralPath $signInPath -Pattern 'TextMode\s*=\s*"?password"?' ) {
    Add-ComplianceCheck -Severity Info -Code 'LOGIN-PASSWORD-FIELD' -Message 'SignIn module contains a password input field.' -Evidence 'DesktopModules/SignIn.ascx'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'LOGIN-PASSWORD-FIELD' -Message 'SignIn password input field was not detected; review the login markup.'
}

$sensitiveQueryMatches = @(Find-PortalSourceMatch -Pattern '(?i)(Request\.QueryString\s*\[[^\]]*(password|passwd|pwd|token|密码)|(password|passwd|pwd|token|密码)[^\r\n]{0,80}Request\.QueryString)')
if ($sensitiveQueryMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Warning -Code 'TRANS-SENSITIVE-QUERYSTRING' -Message 'Potential sensitive QueryString usage was found; manual review required.' -Evidence (($sensitiveQueryMatches | Select-Object -First 5) -join '; ')
}
else {
    Add-ComplianceCheck -Severity Pass -Code 'TRANS-SENSITIVE-QUERYSTRING' -Message 'No obvious sensitive QueryString usage was found in portal source.'
}

$sensitiveLogMatches = @(Find-PortalSourceMatch -Pattern '(?i)((PortalDiagnostics|Trace\.|PortalOperationAudit|OperationAudit|Log)[^\r\n]*(password|passwd|pwd|token|密码)|(password|passwd|pwd|token|密码)[^\r\n]*(PortalDiagnostics|Trace\.|PortalOperationAudit|OperationAudit|Log))')
if ($sensitiveLogMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Warning -Code 'LOG-SENSITIVE-FIELDS' -Message 'Potential sensitive logging/audit text was found; manual review required.' -Evidence (($sensitiveLogMatches | Select-Object -First 5) -join '; ')
}
else {
    Add-ComplianceCheck -Severity Pass -Code 'LOG-SENSITIVE-FIELDS' -Message 'No obvious password/token logging pattern was found in portal source.'
}

$legacyEncryptMatches = @(Find-PortalSourceMatch -Pattern 'PortalSecurity\.Encrypt\s*\(')
if ($legacyEncryptMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Warning -Code 'PWD-LEGACY-MD5' -Message 'Legacy PortalSecurity.Encrypt references remain; verify they are only compatibility paths.' -Evidence (($legacyEncryptMatches | Select-Object -First 6) -join '; ')
}
else {
    Add-ComplianceCheck -Severity Pass -Code 'PWD-LEGACY-MD5' -Message 'No PortalSecurity.Encrypt callers were found outside the scanner scope.'
}

$failCount = @($checks | Where-Object { $_.Severity -eq 'Fail' }).Count
$warningCount = @($checks | Where-Object { $_.Severity -eq 'Warning' }).Count
$infoCount = @($checks | Where-Object { $_.Severity -eq 'Info' }).Count
$passCount = @($checks | Where-Object { $_.Severity -eq 'Pass' }).Count

$summary = [pscustomobject]@{
    PortalPath = $resolvedPortalPath
    SourcePath = $resolvedSourcePath
    SetupPath  = $resolvedSetupPath
    GeneratedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Counts = [pscustomobject]@{
        Pass = $passCount
        Warning = $warningCount
        Fail = $failCount
        Info = $infoCount
    }
    Checks = $checks
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $outputDirectory = Split-Path -Parent $OutputJson
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory | Out-Null
    }

    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputJson -Encoding UTF8
    Write-Host ('JSON: {0}' -f $OutputJson)
}

Write-Host ('SUMMARY: Pass={0}; Warning={1}; Fail={2}; Info={3}' -f $passCount, $warningCount, $failCount, $infoCount)

if ($failCount -gt 0 -or ($FailOnWarning -and $warningCount -gt 0)) {
    exit 1
}

exit 0
