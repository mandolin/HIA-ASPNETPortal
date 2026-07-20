[CmdletBinding()]
param(
    [string]$PortalPath,

    [string]$SourcePath,

    [string]$WebConfigPath,

    [string]$SetupPath,

    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [ValidatePattern('^https?://')]
    [string]$BaseUrl,

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

if ([string]::IsNullOrWhiteSpace($WebConfigPath)) {
    $WebConfigPath = Join-Path $resolvedPortalPath 'Web.config'
}

$resolvedWebConfigPath = [System.IO.Path]::GetFullPath($WebConfigPath)

function Test-StrictDeploymentProfile {
    return $Profile -in @('Test', 'Prod', 'Scan')
}

function Test-ProductionLikeProfile {
    return $Profile -in @('Prod', 'Scan')
}

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

function Get-PortalHttpHeader {
    param(
        [System.Net.WebHeaderCollection]$Headers,
        [string]$Name
    )

    foreach ($key in $Headers.AllKeys) {
        if ($key -ieq $Name) {
            return [string]$Headers[$key]
        }
    }

    return ''
}

function Resolve-PortalSmokeUri {
    param([string]$Url)

    $baseUri = [Uri]$Url
    if ($baseUri.AbsolutePath -eq '/' -or [string]::IsNullOrWhiteSpace($baseUri.AbsolutePath)) {
        return [Uri]::new($baseUri, 'Default.aspx')
    }

    return $baseUri
}

# 中文：先做只读基线检查，不主动扫描漏洞、不写数据库，也不改变 Web.config；Profile 只影响判定口径。
# English: This script is a read-only baseline check; Profile affects expectations only and never modifies Web.config.
Write-Host ('PROFILE: {0}' -f $Profile)
if (-not (Test-Path -LiteralPath $resolvedWebConfigPath)) {
    Add-ComplianceCheck -Severity Fail -Code 'CFG-001' -Message 'Web.config was not found.' -Evidence $resolvedWebConfigPath
}
else {
    Add-ComplianceCheck -Severity Pass -Code 'CFG-001' -Message 'Web.config exists.' -Evidence $resolvedWebConfigPath

    [xml]$webConfig = Get-Utf8Text -LiteralPath $resolvedWebConfigPath
    $headerNodes = $webConfig.SelectNodes('/configuration/system.webServer/httpProtocol/customHeaders/add')
    $headers = @{}
    foreach ($node in $headerNodes) {
        $headers[$node.GetAttribute('name')] = $node.GetAttribute('value')
    }

    $requiredHeaders = @(
        @{ Name = 'X-Content-Type-Options'; Pattern = '\bnosniff\b' },
        @{ Name = 'X-XSS-Protection'; Pattern = '^1;\s*mode=block$' },
        @{ Name = 'Content-Security-Policy'; Pattern = '\bdefault-src\b' },
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

    if ($headers.ContainsKey('Strict-Transport-Security')) {
        $hstsValue = [string]$headers['Strict-Transport-Security']
        if (Test-StrictDeploymentProfile) {
            if ($hstsValue -match '\bmax-age=') {
                Add-ComplianceCheck -Severity Pass -Code 'HDR-Strict-Transport-Security' -Message 'HSTS is present for the selected deployment profile.' -Evidence $hstsValue
            }
            else {
                Add-ComplianceCheck -Severity Fail -Code 'HDR-Strict-Transport-Security' -Message 'HSTS exists but does not contain max-age.' -Evidence $hstsValue
            }
        }
        else {
            Add-ComplianceCheck -Severity Warning -Code 'HDR-HSTS-DEV' -Message 'HSTS is present in a development/legacy profile; keep it in publish transforms instead of the base config.' -Evidence $hstsValue
        }

        if ($hstsValue -match 'includeSubDomains|preload') {
            Add-ComplianceCheck -Severity Warning -Code 'HDR-HSTS-SCOPE' -Message 'HSTS uses includeSubDomains or preload; confirm domain ownership and long-term HTTPS readiness.' -Evidence $hstsValue
        }
    }
    elseif (Test-StrictDeploymentProfile) {
        Add-ComplianceCheck -Severity Fail -Code 'HDR-Strict-Transport-Security' -Message ('HSTS is required for the {0} profile.' -f $Profile)
    }
    else {
        Add-ComplianceCheck -Severity Pass -Code 'HDR-HSTS-DEV' -Message 'HSTS is intentionally absent from the development/legacy base profile.'
    }

    if ($headers.ContainsKey('Content-Security-Policy') -and $headers['Content-Security-Policy'] -match "unsafe-inline|unsafe-eval") {
        $cspSeverity = if (Test-StrictDeploymentProfile) { 'Warning' } else { 'Info' }
        Add-ComplianceCheck -Severity $cspSeverity -Code 'HDR-CSP-COMPAT' -Message 'CSP currently keeps Web Forms compatibility exceptions.' -Evidence $headers['Content-Security-Policy']
    }

    if ($headers.ContainsKey('Referrer-Policy')) {
        $referrerPolicy = [string]$headers['Referrer-Policy']
        if (Test-ProductionLikeProfile) {
            if ($referrerPolicy -ieq 'strict-origin-when-cross-origin') {
                Add-ComplianceCheck -Severity Pass -Code 'HDR-REFERRER-PROD' -Message 'Production-like profile uses strict referrer policy.' -Evidence $referrerPolicy
            }
            else {
                Add-ComplianceCheck -Severity Warning -Code 'HDR-REFERRER-PROD' -Message 'Production-like profile should use strict-origin-when-cross-origin.' -Evidence $referrerPolicy
            }
        }
    }

    $httpCookiesNode = $webConfig.SelectSingleNode('/configuration/system.web/httpCookies')
    $httpCookiesRequireSsl = $httpCookiesNode -ne $null -and $httpCookiesNode.GetAttribute('requireSSL') -ieq 'true'
    if (Test-StrictDeploymentProfile) {
        if ($httpCookiesRequireSsl) {
            Add-ComplianceCheck -Severity Pass -Code 'COOKIE-SSL' -Message 'Cookies are configured to require SSL for the selected deployment profile.'
        }
        else {
            Add-ComplianceCheck -Severity Fail -Code 'COOKIE-SSL' -Message ('Cookies must require SSL for the {0} profile.' -f $Profile)
        }
    }
    else {
        if ($httpCookiesRequireSsl) {
            Add-ComplianceCheck -Severity Info -Code 'COOKIE-SSL' -Message 'Cookies require SSL in a development/legacy profile; HTTP local debugging may need a different config.'
        }
        else {
            Add-ComplianceCheck -Severity Pass -Code 'COOKIE-SSL' -Message 'Cookies do not require SSL in the development/legacy base profile; production transforms must enforce SSL.'
        }
    }

    $formsNode = $webConfig.SelectSingleNode('/configuration/system.web/authentication/forms')
    $formsRequireSsl = $formsNode -ne $null -and $formsNode.GetAttribute('requireSSL') -ieq 'true'
    if (Test-StrictDeploymentProfile) {
        if ($formsRequireSsl) {
            Add-ComplianceCheck -Severity Pass -Code 'COOKIE-FORMS-SSL' -Message 'Forms Authentication cookie is configured to require SSL.'
        }
        else {
            Add-ComplianceCheck -Severity Fail -Code 'COOKIE-FORMS-SSL' -Message ('Forms Authentication cookie must require SSL for the {0} profile.' -f $Profile)
        }
    }
    else {
        if ($formsRequireSsl) {
            Add-ComplianceCheck -Severity Info -Code 'COOKIE-FORMS-SSL' -Message 'Forms Authentication cookie requires SSL in a development/legacy profile; HTTP local debugging may need a different config.'
        }
        else {
            Add-ComplianceCheck -Severity Pass -Code 'COOKIE-FORMS-SSL' -Message 'Forms Authentication cookie does not require SSL in the development/legacy base profile.'
        }
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

$loginEncryptionFiles = @(
    'Portal/Scripts/Security/jsencrypt-ie6.min.js',
    'Portal/Scripts/Security/jsencrypt-ie6.LICENSE.txt',
    'Portal/Scripts/Security/PortalLoginPasswordEncryption.js',
    'Portal/Security/LoginPasswordKey.ashx',
    'Portal/Components/PortalLoginPasswordCrypto.cs'
)
$missingLoginEncryptionFiles = @(
    foreach ($relativeLoginEncryptionFile in $loginEncryptionFiles) {
        $loginEncryptionFilePath = Join-Path $resolvedSourcePath $relativeLoginEncryptionFile
        if (-not (Test-Path -LiteralPath $loginEncryptionFilePath)) {
            $relativeLoginEncryptionFile
        }
    }
)

$clientEncryptionMatches = @(Find-PortalSourceMatch -Pattern 'PortalLoginPasswordEncryption|LoginPasswordKey|TryDecryptSubmittedPassword|RequireEncryptedLoginPassword')
if ($missingLoginEncryptionFiles.Count -eq 0 -and $clientEncryptionMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Pass -Code 'TRANS-PWD-FIELD-ENCRYPT' -Message 'Login password field encryption assets and server hooks were found.' -Evidence (($clientEncryptionMatches | Select-Object -First 4) -join '; ')
}
else {
    $evidence = if ($missingLoginEncryptionFiles.Count -gt 0) { ($missingLoginEncryptionFiles -join '; ') } else { 'Expected source markers were not found.' }
    Add-ComplianceCheck -Severity Warning -Code 'TRANS-PWD-FIELD-ENCRYPT' -Message 'Login password field encryption implementation needs review.' -Evidence $evidence
}

$signInPath = Join-Path $resolvedPortalPath 'DesktopModules/SignIn.ascx'
if (Test-TextContains -LiteralPath $signInPath -Pattern 'TextMode\s*=\s*"?password"?' ) {
    Add-ComplianceCheck -Severity Info -Code 'LOGIN-PASSWORD-FIELD' -Message 'SignIn module contains a password input field.' -Evidence 'DesktopModules/SignIn.ascx'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'LOGIN-PASSWORD-FIELD' -Message 'SignIn password input field was not detected; review the login markup.'
}

if ((Test-TextContains -LiteralPath $signInPath -Pattern 'EncryptedPassword') -and
    (Test-TextContains -LiteralPath (Join-Path $resolvedSourcePath 'Portal/DesktopModules/Signin.ascx.cs') -Pattern 'PortalLoginPasswordCrypto\.TryDecryptSubmittedPassword')) {
    Add-ComplianceCheck -Severity Pass -Code 'LOGIN-PASSWORD-ENCRYPTED-FIELD' -Message 'SignIn posts an encrypted hidden password field and decrypts it server-side.'
}
else {
    Add-ComplianceCheck -Severity Warning -Code 'LOGIN-PASSWORD-ENCRYPTED-FIELD' -Message 'SignIn encrypted password field or server-side decrypt hook was not verified.'
}

$encryptedLoginSettingPattern = 'key\s*=\s*"Portal\.Security\.RequireEncryptedLoginPassword"\s+value\s*=\s*"true"'
if (Test-TextContains -LiteralPath $resolvedWebConfigPath -Pattern $encryptedLoginSettingPattern) {
    Add-ComplianceCheck -Severity Pass -Code 'LOGIN-PASSWORD-ENCRYPT-REQUIRED' -Message 'Encrypted login-password submission is required by default.' -Evidence 'Portal.Security.RequireEncryptedLoginPassword=true'
}
else {
    $settingSeverity = if (Test-StrictDeploymentProfile) { 'Fail' } else { 'Warning' }
    Add-ComplianceCheck -Severity $settingSeverity -Code 'LOGIN-PASSWORD-ENCRYPT-REQUIRED' -Message 'Encrypted login-password submission is not required by the selected Web.config.' -Evidence 'Portal.Security.RequireEncryptedLoginPassword should be true for production/scan profiles.'
}

$jsEncryptLicensePath = Join-Path $resolvedSourcePath 'Portal/Scripts/Security/jsencrypt-ie6.LICENSE.txt'
if (Test-TextContains -LiteralPath $jsEncryptLicensePath -Pattern 'The MIT License') {
    Add-ComplianceCheck -Severity Pass -Code '3P-JSENCRYPT-LICENSE' -Message 'JSEncrypt IE6-compatible asset has an archived MIT license.' -Evidence 'Scripts/Security/jsencrypt-ie6.LICENSE.txt'
}
else {
    Add-ComplianceCheck -Severity Warning -Code '3P-JSENCRYPT-LICENSE' -Message 'JSEncrypt license archive was not verified.'
}

$sensitiveQueryMatches = @(Find-PortalSourceMatch -Pattern '(?i)(Request\.QueryString\s*\[[^\]]*(password|passwd|pwd|token|密码)|(password|passwd|pwd|token|密码)[^\r\n]{0,80}Request\.QueryString)')
if ($sensitiveQueryMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Warning -Code 'TRANS-SENSITIVE-QUERYSTRING' -Message 'Potential sensitive QueryString usage was found; manual review required.' -Evidence (($sensitiveQueryMatches | Select-Object -First 5) -join '; ')
}
else {
    Add-ComplianceCheck -Severity Pass -Code 'TRANS-SENSITIVE-QUERYSTRING' -Message 'No obvious sensitive QueryString usage was found in portal source.'
}

$sensitiveLogMatches = @(Find-PortalSourceMatch -Pattern '(?i)((PortalDiagnostics|Trace\.|PortalOperationAudit|OperationAudit|\bLog(?:ger|ging)?\b)[^\r\n]*(password|passwd|pwd|token|密码)|(password|passwd|pwd|token|密码)[^\r\n]*(PortalDiagnostics|Trace\.|PortalOperationAudit|OperationAudit|\bLog(?:ger|ging)?\b))')
$sensitiveLogReviewMatches = @(
    $sensitiveLogMatches |
        Where-Object {
            $_ -notmatch '(?i)(do not log|不得记录|调用方不得记录|without logging|without the connection string|PortalOperationAuditEvents\.PasswordReset)'
        }
)
if ($sensitiveLogReviewMatches.Count -gt 0) {
    Add-ComplianceCheck -Severity Warning -Code 'LOG-SENSITIVE-FIELDS' -Message 'Potential sensitive logging/audit text was found; manual review required.' -Evidence (($sensitiveLogReviewMatches | Select-Object -First 5) -join '; ')
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

if (-not [string]::IsNullOrWhiteSpace($BaseUrl)) {
    $smokeUri = Resolve-PortalSmokeUri -Url $BaseUrl
    $response = $null

    try {
        $request = [System.Net.WebRequest]::Create($smokeUri)
        $request.Method = 'GET'
        $request.AllowAutoRedirect = $false
        $request.Timeout = 15000
        $response = $request.GetResponse()

        Add-ComplianceCheck -Severity Pass -Code 'HTTP-RESPONSE' -Message ('HTTP smoke returned status {0}.' -f [int]$response.StatusCode) -Evidence $smokeUri.AbsoluteUri

        $runtimeHeaders = $response.Headers
        $runtimeRequiredHeaders = @(
            'X-Content-Type-Options',
            'X-XSS-Protection',
            'Content-Security-Policy',
            'Referrer-Policy',
            'X-Permitted-Cross-Domain-Policies',
            'X-Download-Options',
            'X-Frame-Options'
        )

        foreach ($headerName in $runtimeRequiredHeaders) {
            $runtimeHeaderValue = Get-PortalHttpHeader -Headers $runtimeHeaders -Name $headerName
            if ([string]::IsNullOrWhiteSpace($runtimeHeaderValue)) {
                Add-ComplianceCheck -Severity Warning -Code ('HTTP-HDR-{0}' -f $headerName) -Message ('Runtime response header is missing: {0}' -f $headerName) -Evidence $smokeUri.AbsoluteUri
            }
            else {
                Add-ComplianceCheck -Severity Pass -Code ('HTTP-HDR-{0}' -f $headerName) -Message ('Runtime response header is present: {0}' -f $headerName) -Evidence $runtimeHeaderValue
            }
        }

        $runtimeHsts = Get-PortalHttpHeader -Headers $runtimeHeaders -Name 'Strict-Transport-Security'
        if (Test-StrictDeploymentProfile) {
            if ($smokeUri.Scheme -ine 'https') {
                Add-ComplianceCheck -Severity Warning -Code 'HTTP-HSTS-SCHEME' -Message ('Runtime {0} profile should be validated over HTTPS.' -f $Profile) -Evidence $smokeUri.AbsoluteUri
            }

            if ([string]::IsNullOrWhiteSpace($runtimeHsts)) {
                Add-ComplianceCheck -Severity Fail -Code 'HTTP-HDR-Strict-Transport-Security' -Message ('Runtime HSTS header is required for the {0} profile.' -f $Profile)
            }
            else {
                Add-ComplianceCheck -Severity Pass -Code 'HTTP-HDR-Strict-Transport-Security' -Message 'Runtime HSTS header is present.' -Evidence $runtimeHsts
            }
        }
        elseif ([string]::IsNullOrWhiteSpace($runtimeHsts)) {
            Add-ComplianceCheck -Severity Pass -Code 'HTTP-HSTS-DEV' -Message 'Runtime HSTS is absent for the development/legacy profile.'
        }
        else {
            Add-ComplianceCheck -Severity Warning -Code 'HTTP-HSTS-DEV' -Message 'Runtime HSTS is present in a development/legacy profile.' -Evidence $runtimeHsts
        }
    }
    catch {
        Add-ComplianceCheck -Severity Warning -Code 'HTTP-RESPONSE' -Message 'HTTP smoke could not reach the target URL.' -Evidence $_.Exception.Message
    }
    finally {
        if ($response -ne $null) {
            $response.Dispose()
        }
    }
}

$failCount = @($checks | Where-Object { $_.Severity -eq 'Fail' }).Count
$warningCount = @($checks | Where-Object { $_.Severity -eq 'Warning' }).Count
$infoCount = @($checks | Where-Object { $_.Severity -eq 'Info' }).Count
$passCount = @($checks | Where-Object { $_.Severity -eq 'Pass' }).Count

$summary = [pscustomobject]@{
    Profile = $Profile
    PortalPath = $resolvedPortalPath
    SourcePath = $resolvedSourcePath
    SetupPath  = $resolvedSetupPath
    WebConfigPath = $resolvedWebConfigPath
    BaseUrl = $BaseUrl
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
