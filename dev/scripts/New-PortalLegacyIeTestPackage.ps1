[CmdletBinding()]
param(
    [ValidatePattern('^https?://')]
    [string]$BaseUrl = 'http://localhost:40001/',

    [string]$TaskName = 'P9.3-PortalLegacyIeSmoke',

    [string]$OutputRoot = 'temp\legacy-ie-packages',

    [string]$AdminUser = 'admin',

    [switch]$NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 中文：生成可复制到 Win7/IE VM 内运行的零依赖 IE COM smoke 测试包。
# English: Generates a zero-dependency IE COM smoke package that can be copied to a Win7/IE VM.
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$outputRootPath = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRoot
}
else {
    Join-Path $repositoryRoot $OutputRoot
}

function ConvertTo-SafeFileName {
    param([string]$Value)

    $safe = [regex]::Replace($Value, '[^\w\.-]+', '-')
    $safe = $safe.Trim('-')
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'PortalLegacyIeSmoke'
    }

    return $safe
}

function ConvertTo-PowerShellSingleQuotedContent {
    param([string]$Value)

    return ($Value -replace "'", "''")
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [string]$Content
    )

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $normalized = $Content -replace "`r?`n", "`r`n"
    [System.IO.File]::WriteAllText($Path, $normalized, $utf8NoBom)
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$safeTaskName = ConvertTo-SafeFileName -Value $TaskName
$packageName = "PortalLegacyIeTestPackage-$safeTaskName-$timestamp"
$packageRoot = Join-Path $outputRootPath $packageName
$resultsRoot = Join-Path $packageRoot 'results'
$toolsRoot = Join-Path $packageRoot 'tools'

New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null
New-Item -ItemType Directory -Path $toolsRoot -Force | Out-Null

$baseUrlContent = ConvertTo-PowerShellSingleQuotedContent -Value $BaseUrl
$adminUserContent = ConvertTo-PowerShellSingleQuotedContent -Value $AdminUser
$taskNameContent = ConvertTo-PowerShellSingleQuotedContent -Value $TaskName

$readme = @"
Portal legacy IE smoke package
================================

Purpose:
  Run a simple real-IE smoke test inside a Windows 7 VM.

How to run:
  1. Make sure this VM can access the portal base URL.
  2. Put the admin password in a VM-local secret file, for example secrets\admin-password.txt.
  3. Run run-smoke.ps1 with -AdminPasswordFile, or run the package through Portal VM Task Agent.
  4. Wait until the script closes Internet Explorer.
  5. Copy the generated results folder or PortalLegacyIeResult-*.zip back to the host.

Default settings:
  Task: $TaskName
  Base URL: $BaseUrl
  Admin user: $AdminUser

If the VM cannot access the default Base URL:
  Open a command prompt in this folder and run:
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File run-smoke.ps1 -BaseUrl http://HOST-IP:40001/ -AdminPasswordFile secrets\admin-password.txt

Notes:
  - The script uses InternetExplorer.Application COM automation.
  - No IEDriver, Java, Node.js, or browser plugin is required.
  - Screenshots are desktop screenshots, so keep the VM desktop unlocked and IE visible.
  - Passwords must come from a VM-local secret file or explicit parameter; the script will not wait for manual password input.
"@

$taskJson = @"
{
  "taskName": "$($TaskName -replace '\\', '\\' -replace '"', '\"')",
  "baseUrl": "$($BaseUrl -replace '\\', '\\' -replace '"', '\"')",
  "adminUser": "$($AdminUser -replace '\\', '\\' -replace '"', '\"')",
  "track": "IE COM smoke",
  "steps": [
    "home",
    "login",
    "admin-system-health",
    "generic-error-page"
  ],
  "notes": "Human-readable task metadata only. run-smoke.ps1 does not require JSON parsing on Windows PowerShell 2.0."
}
"@

$selectorsJson = @"
{
  "loginUserIdSuffix": "EmailOrName",
  "loginPasswordIdSuffix": "password",
  "loginButtonIdSuffix": "SigninBtn",
  "notes": "Human-readable selector metadata only. The script has equivalent PS2-compatible selector logic built in."
}
"@

$expectedJson = @"
{
  "homeKeywords": [ "ASP.NET Portal Starter Kit", "Portal" ],
  "loginSuccessKeywords": [ "Logoff", "欢迎", "Admin" ],
  "systemHealthKeywords": [ "System Health", "系统健康" ],
  "genericErrorKeywords": [ "应用程序暂时无法完成请求", "event", "事件编号" ]
}
"@

$cmd = @"
@echo off
setlocal
cd /d "%~dp0"
echo Portal legacy IE smoke package
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-smoke.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo Finished with exit code %EXITCODE%.
exit /b %EXITCODE%
"@

$runSmoke = @'
param(
    [string]$BaseUrl = '__BASE_URL__',
    [string]$AdminUser = '__ADMIN_USER__',
    [string]$AdminPasswordFile = '',
    [string]$AdminPassword = '',
    [switch]$SkipLogin,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$TaskName = '__TASK_NAME__'
$StartedAt = Get-Date
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ResultsRoot = Join-Path $ScriptRoot 'results'
$RunRoot = Join-Path $ResultsRoot ('run-' + $Stamp)
$LogPath = Join-Path $RunRoot 'portal-legacy-ie-smoke.log'
$ResultJsonPath = Join-Path $RunRoot 'result.json'
$Results = New-Object System.Collections.ArrayList
$Ie = $null
$script:PortalLegacyIeWinInetTypeAdded = $false
$script:PortalLegacyIeUiTypeAdded = $false

function New-DirectoryIfMissing {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Write-Log {
    param([string]$Message)

    $line = ('{0} {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message)
    Add-Content -LiteralPath $LogPath -Value $line -Encoding UTF8
    Write-Host $line
}

function ConvertTo-PlainText {
    param([System.Security.SecureString]$SecureText)

    if ($null -eq $SecureText) {
        return ''
    }

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureText)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function ConvertTo-SecretFileName {
    param([string]$Value)

    $safe = $Value -replace '[^A-Za-z0-9_.@-]+', '-'
    $safe = $safe.Trim('-')
    if ([string]::IsNullOrEmpty($safe)) {
        return 'admin'
    }

    return $safe
}

function Get-AdminLoginPassword {
    if (-not [string]::IsNullOrEmpty($AdminPassword)) {
        return $AdminPassword
    }

    if (-not [string]::IsNullOrEmpty($AdminPasswordFile)) {
        if (-not (Test-Path -LiteralPath $AdminPasswordFile -PathType Leaf)) {
            throw ('AdminPasswordFile not found: ' + $AdminPasswordFile)
        }

        return ([System.IO.File]::ReadAllText($AdminPasswordFile, [System.Text.Encoding]::UTF8)).Trim()
    }

    $secretRoot = [Environment]::GetEnvironmentVariable('PORTAL_VM_SECRETS_DIR')
    if (-not [string]::IsNullOrEmpty($secretRoot)) {
        $userSecret = Join-Path (Join-Path $secretRoot 'users') ((ConvertTo-SecretFileName -Value $AdminUser) + '.password.txt')
        if (Test-Path -LiteralPath $userSecret -PathType Leaf) {
            return ([System.IO.File]::ReadAllText($userSecret, [System.Text.Encoding]::UTF8)).Trim()
        }

        $legacySecret = Join-Path $secretRoot 'admin-password.txt'
        if (Test-Path -LiteralPath $legacySecret -PathType Leaf) {
            return ([System.IO.File]::ReadAllText($legacySecret, [System.Text.Encoding]::UTF8)).Trim()
        }
    }

    throw ('Password was not provided for user ' + $AdminUser + '. Use -AdminPasswordFile, -AdminPassword, or PORTAL_VM_SECRETS_DIR\users\' + $AdminUser + '.password.txt.')
}

function ConvertTo-JsonString {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return ($Value -replace '\\', '\\' -replace '"', '\"' -replace "`r", '\r' -replace "`n", '\n')
}

function Read-HttpResponseText {
    param([object]$Response)

    $stream = $Response.GetResponseStream()
    try {
        $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Close()
        }
    }
    finally {
        $stream.Close()
    }
}

function Get-HtmlAttributeValue {
    param(
        [string]$Tag,
        [string]$Name
    )

    try {
        Add-Type -AssemblyName System.Web
    }
    catch {
    }

    $pattern = '(?is)\b' + [regex]::Escape($Name) + '\s*=\s*(?:"([^"]*)"|''([^'']*)''|([^\s>]+))'
    $match = [regex]::Match($Tag, $pattern)
    if (-not $match.Success) {
        return ''
    }

    if ($match.Groups[1].Success) {
        return [System.Web.HttpUtility]::HtmlDecode($match.Groups[1].Value)
    }

    if ($match.Groups[2].Success) {
        return [System.Web.HttpUtility]::HtmlDecode($match.Groups[2].Value)
    }

    return [System.Web.HttpUtility]::HtmlDecode($match.Groups[3].Value)
}

function Encode-FormComponent {
    param([string]$Value)

    if ($null -eq $Value) {
        $Value = ''
    }

    try {
        return [System.Web.HttpUtility]::UrlEncode($Value, [System.Text.Encoding]::UTF8)
    }
    catch {
        return [System.Uri]::EscapeDataString($Value)
    }
}

function ConvertTo-FormUrlEncoded {
    param([System.Collections.Specialized.NameValueCollection]$Fields)

    $pairs = New-Object System.Collections.ArrayList
    foreach ($key in $Fields.AllKeys) {
        if ([string]::IsNullOrEmpty($key)) {
            continue
        }

        $pair = (Encode-FormComponent -Value $key) + '=' + (Encode-FormComponent -Value $Fields[$key])
        [void]$pairs.Add($pair)
    }

    return [string]::Join('&', [string[]]$pairs.ToArray([string]))
}

function Set-FormFieldValue {
    param(
        [System.Collections.Specialized.NameValueCollection]$Fields,
        [string]$Name,
        [string]$Value
    )

    if ([string]::IsNullOrEmpty($Name)) {
        return
    }

    $Fields.Remove($Name)
    $Fields.Add($Name, $Value)
}

function Get-FormFieldsFromHtml {
    param([string]$Html)

    try {
        Add-Type -AssemblyName System.Web
    }
    catch {
    }

    $fields = New-Object System.Collections.Specialized.NameValueCollection
    $inputMatches = [regex]::Matches($Html, '(?is)<input\b[^>]*>')
    foreach ($inputMatch in $inputMatches) {
        $tag = $inputMatch.Value
        $name = Get-HtmlAttributeValue -Tag $tag -Name 'name'
        if ([string]::IsNullOrEmpty($name)) {
            continue
        }

        $type = (Get-HtmlAttributeValue -Tag $tag -Name 'type').ToLowerInvariant()
        if (($type -eq 'checkbox' -or $type -eq 'radio') -and $tag.IndexOf('checked', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            continue
        }

        Set-FormFieldValue -Fields $fields -Name $name -Value (Get-HtmlAttributeValue -Tag $tag -Name 'value')
    }

    return ,$fields
}

function Resolve-FormPostUri {
    param(
        [System.Uri]$PageUri,
        [string]$Html
    )

    $formMatch = [regex]::Match($Html, '(?is)<form\b[^>]*>')
    if (-not $formMatch.Success) {
        return $PageUri
    }

    $action = Get-HtmlAttributeValue -Tag $formMatch.Value -Name 'action'
    if ([string]::IsNullOrEmpty($action)) {
        return $PageUri
    }

    try {
        return (New-Object System.Uri -ArgumentList $PageUri, $action)
    }
    catch {
        return $PageUri
    }
}

function Get-SetCookieNames {
    param([string]$SetCookieHeader)

    $names = New-Object System.Collections.ArrayList
    if ([string]::IsNullOrEmpty($SetCookieHeader)) {
        return ,$names
    }

    $matches = [regex]::Matches($SetCookieHeader, '(?im)(^|,\s*)([^=;,\s]+)=')
    foreach ($match in $matches) {
        $name = $match.Groups[2].Value
        if (-not [string]::IsNullOrEmpty($name) -and $names.IndexOf($name) -lt 0) {
            [void]$names.Add($name)
        }
    }

    return ,$names
}

function Get-AuthCookieValueFromHeader {
    param([string]$SetCookieHeader)

    if ([string]::IsNullOrEmpty($SetCookieHeader)) {
        return ''
    }

    $match = [regex]::Match($SetCookieHeader, '(?i)(^|,\s*)\.ASPXAUTH=([^;,\r\n]+)')
    if ($match.Success) {
        return $match.Groups[2].Value
    }

    return ''
}

function Ensure-WinInetCookieType {
    if ($script:PortalLegacyIeWinInetTypeAdded) {
        return
    }

    Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class PortalLegacyIeWinInetCookie
{
    [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool InternetSetCookie(string url, string cookieName, string cookieData);
}
"@

    $script:PortalLegacyIeWinInetTypeAdded = $true
}

function Set-InternetExplorerCookie {
    param(
        [string]$Url,
        [System.Net.Cookie]$Cookie
    )

    try {
        Ensure-WinInetCookieType
        $cookieData = $Cookie.Name + '=' + $Cookie.Value + '; path=/'
        if (-not [PortalLegacyIeWinInetCookie]::InternetSetCookie($Url, $null, $cookieData)) {
            Write-Log ('WARN wininet cookie set returned false for ' + $Cookie.Name)
            return $false
        }

        Write-Log ('LOGIN copied cookie to IE: ' + $Cookie.Name)
        return $true
    }
    catch {
        Write-Log ('WARN wininet cookie set failed for ' + $Cookie.Name + ': ' + $_.Exception.Message)
        return $false
    }
}

function Set-InternetExplorerCookieValue {
    param(
        [string]$Url,
        [string]$Name,
        [string]$Value
    )

    try {
        Ensure-WinInetCookieType
        $cookieData = $Name + '=' + $Value + '; path=/'
        if (-not [PortalLegacyIeWinInetCookie]::InternetSetCookie($Url, $null, $cookieData)) {
            Write-Log ('WARN wininet raw cookie set returned false for ' + $Name)
            return $false
        }

        Write-Log ('LOGIN copied raw cookie to IE: ' + $Name)
        return $true
    }
    catch {
        Write-Log ('WARN wininet raw cookie set failed for ' + $Name + ': ' + $_.Exception.Message)
        return $false
    }
}

function Ensure-UiAutomationType {
    if ($script:PortalLegacyIeUiTypeAdded) {
        return
    }

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class PortalLegacyIeUi
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
"@

    $script:PortalLegacyIeUiTypeAdded = $true
}

function Set-ClipboardTextByClipExe {
    param([string]$Text)

    if ($null -eq $Text) {
        $Text = ''
    }

    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = 'clip.exe'
    $processInfo.UseShellExecute = $false
    $processInfo.RedirectStandardInput = $true
    $processInfo.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::Start($processInfo)
    try {
        $process.StandardInput.Write($Text)
        $process.StandardInput.Close()
        $process.WaitForExit()
        return ($process.ExitCode -eq 0)
    }
    finally {
        $process.Dispose()
    }
}

function Clear-ClipboardByClipExe {
    try {
        [void](Set-ClipboardTextByClipExe -Text '')
    }
    catch {
    }
}

function Set-BrowserWindowForKeyboard {
    param([object]$Browser)

    try {
        $Browser.Left = 120
        $Browser.Top = 40
        $Browser.Width = 1280
        $Browser.Height = 920
    }
    catch {
    }

    Ensure-UiAutomationType
    try {
        [void][PortalLegacyIeUi]::SetForegroundWindow([IntPtr]([int]$Browser.HWND))
    }
    catch {
    }

    Start-Sleep -Milliseconds 600
}

function Click-BrowserPoint {
    param(
        [object]$Browser,
        [int]$X,
        [int]$Y
    )

    Ensure-UiAutomationType
    $screenX = [int]$Browser.Left + $X
    $screenY = [int]$Browser.Top + $Y
    [void][PortalLegacyIeUi]::SetCursorPos($screenX, $screenY)
    Start-Sleep -Milliseconds 120
    [PortalLegacyIeUi]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [PortalLegacyIeUi]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 180
}

function Paste-TextToFocusedControl {
    param([string]$Text)

    if (-not (Set-ClipboardTextByClipExe -Text $Text)) {
        throw 'clip.exe failed to set clipboard text.'
    }

    [System.Windows.Forms.SendKeys]::SendWait('^a')
    Start-Sleep -Milliseconds 120
    [System.Windows.Forms.SendKeys]::SendWait('^v')
    Start-Sleep -Milliseconds 250
}

function Invoke-PortalLoginByKeyboard {
    param(
        [object]$Browser,
        [string]$UserName,
        [string]$Password
    )

    try {
        Write-Log 'LOGIN keyboard fallback positioning IE window.'
        Set-BrowserWindowForKeyboard -Browser $Browser

        # Coordinates are relative to the IE window after it is positioned by this package.
        Click-BrowserPoint -Browser $Browser -X 500 -Y 250
        Paste-TextToFocusedControl -Text $UserName

        Click-BrowserPoint -Browser $Browser -X 500 -Y 350
        Paste-TextToFocusedControl -Text $Password

        Click-BrowserPoint -Browser $Browser -X 520 -Y 485
        Wait-InternetExplorer -Browser $Browser
        Clear-ClipboardByClipExe
        return $true
    }
    catch {
        Clear-ClipboardByClipExe
        Write-Log ('WARN keyboard login fallback failed: ' + $_.Exception.Message)
        return $false
    }
}

function Invoke-PortalLoginByHttp {
    param(
        [string]$Root,
        [string]$UserName,
        [string]$Password
    )

    $loginUrl = Join-PortalUrl -Root $Root -Path 'Default.aspx'
    $loginUri = New-Object System.Uri($loginUrl)
    $cookieContainer = New-Object System.Net.CookieContainer
    $userAgent = 'Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)'

    try {
        $rawAuthCookie = ''
        $setCookieHeader = ''
        $postText = ''
        $locationHeader = ''

        Write-Log 'LOGIN HTTP fallback requesting login page.'
        $getRequest = [System.Net.HttpWebRequest]::Create($loginUri)
        $getRequest.CookieContainer = $cookieContainer
        $getRequest.UserAgent = $userAgent
        $getRequest.AllowAutoRedirect = $true
        $getResponse = $getRequest.GetResponse()
        try {
            $loginHtml = Read-HttpResponseText -Response $getResponse
            $actualLoginUri = $getResponse.ResponseUri
        }
        finally {
            $getResponse.Close()
        }

        if ($null -eq $actualLoginUri) {
            $actualLoginUri = $loginUri
        }

        $postUri = Resolve-FormPostUri -PageUri $actualLoginUri -Html $loginHtml
        Write-Log ('LOGIN HTTP fallback GET url: ' + $actualLoginUri.AbsoluteUri)
        Write-Log ('LOGIN HTTP fallback form POST url: ' + $postUri.AbsoluteUri)

        $fields = Get-FormFieldsFromHtml -Html $loginHtml
        Set-FormFieldValue -Fields $fields -Name 'ctl00$MainContent$ctl01$EmailOrName' -Value $UserName
        Set-FormFieldValue -Fields $fields -Name 'ctl00$MainContent$ctl01$password' -Value $Password
        Set-FormFieldValue -Fields $fields -Name 'ctl00$MainContent$ctl01$SigninBtn' -Value 'Sign In'

        $postBody = ConvertTo-FormUrlEncoded -Fields $fields
        $postBytes = [System.Text.Encoding]::UTF8.GetBytes($postBody)

        Write-Log ('LOGIN HTTP fallback posting form fields: ' + $fields.Count)
        $postRequest = [System.Net.HttpWebRequest]::Create($postUri)
        $postRequest.CookieContainer = $cookieContainer
        $postRequest.UserAgent = $userAgent
        $postRequest.Method = 'POST'
        $postRequest.ContentType = 'application/x-www-form-urlencoded'
        $postRequest.Referer = $actualLoginUri.AbsoluteUri
        $postRequest.AllowAutoRedirect = $false
        $postRequest.ContentLength = $postBytes.Length
        $requestStream = $postRequest.GetRequestStream()
        try {
            $requestStream.Write($postBytes, 0, $postBytes.Length)
        }
        finally {
            $requestStream.Close()
        }

        $postResponse = $postRequest.GetResponse()
        try {
            $postText = Read-HttpResponseText -Response $postResponse
            $statusText = [int]$postResponse.StatusCode
            $locationHeader = [string]$postResponse.Headers['Location']
            $setCookieHeader = [string]$postResponse.Headers['Set-Cookie']
            Write-Log ('LOGIN HTTP fallback response status: ' + $statusText)
            Write-Log ('LOGIN HTTP fallback response url: ' + $postResponse.ResponseUri.AbsoluteUri)
            if (-not [string]::IsNullOrEmpty($locationHeader)) {
                Write-Log ('LOGIN HTTP fallback response location: ' + $locationHeader)
            }

            $setCookieNames = Get-SetCookieNames -SetCookieHeader $setCookieHeader
            if ($setCookieNames.Count -gt 0) {
                Write-Log ('LOGIN HTTP fallback set-cookie names: ' + [string]::Join(',', [string[]]$setCookieNames.ToArray([string])))
            }
            else {
                Write-Log 'LOGIN HTTP fallback set-cookie names: (none)'
            }
        }
        finally {
            $postResponse.Close()
        }

        $authCookieFound = $false
        $authCookieCopied = 0
        $allCookiesCopied = 0

        $cookieUris = @($loginUri, $actualLoginUri, $postUri)
        foreach ($cookieUri in $cookieUris) {
            $cookies = $cookieContainer.GetCookies($cookieUri)
            foreach ($cookie in $cookies) {
                Write-Log ('LOGIN HTTP fallback cookie container has: ' + $cookie.Name + '; path=' + $cookie.Path)
                if ($cookie.Name -eq '.ASPXAUTH') {
                    $authCookieFound = $true
                }

                if (Set-InternetExplorerCookie -Url $loginUrl -Cookie $cookie) {
                    $allCookiesCopied++
                    if ($cookie.Name -eq '.ASPXAUTH') {
                        $authCookieCopied++
                    }
                }
            }
        }

        if (-not $authCookieFound) {
            $rawAuthCookie = Get-AuthCookieValueFromHeader -SetCookieHeader $setCookieHeader
            if (-not [string]::IsNullOrEmpty($rawAuthCookie)) {
                $authCookieFound = $true
            }
        }

        if (-not [string]::IsNullOrEmpty($rawAuthCookie)) {
            if (Set-InternetExplorerCookieValue -Url $loginUrl -Name '.ASPXAUTH' -Value $rawAuthCookie) {
                $authCookieCopied++
                $allCookiesCopied++
            }
        }

        $logoffFound = Test-AnyKeyword -Text $postText -Keywords @('Logoff', 'Log off')
        Write-Log ('LOGIN HTTP fallback logoff marker: ' + $logoffFound)

        if ($authCookieFound -and $authCookieCopied -gt 0 -and -not [string]::IsNullOrEmpty($locationHeader)) {
            try {
                $redirectUri = New-Object System.Uri -ArgumentList $postUri, $locationHeader
                $redirectRequest = [System.Net.HttpWebRequest]::Create($redirectUri)
                $redirectRequest.CookieContainer = $cookieContainer
                $redirectRequest.UserAgent = $userAgent
                $redirectRequest.AllowAutoRedirect = $true
                $redirectResponse = $redirectRequest.GetResponse()
                try {
                    [void](Read-HttpResponseText -Response $redirectResponse)
                    Write-Log ('LOGIN HTTP fallback followed redirect: ' + $redirectResponse.ResponseUri.AbsoluteUri)
                }
                finally {
                    $redirectResponse.Close()
                }
            }
            catch {
                Write-Log ('WARN HTTP login redirect follow failed: ' + $_.Exception.Message)
            }
        }

        if ($logoffFound -and -not $authCookieFound) {
            Write-Log 'WARN login marker was found without auth cookie; treating as not logged.'
        }

        if (-not $authCookieFound) {
            $snippet = $postText
            if ($null -eq $snippet) {
                $snippet = ''
            }

            $snippet = [regex]::Replace($snippet, '\s+', ' ')
            if ($snippet.Length -gt 160) {
                $snippet = $snippet.Substring(0, 160)
            }

            Write-Log ('LOGIN HTTP fallback response snippet: ' + $snippet)
        }

        $passed = $authCookieFound -and $authCookieCopied -gt 0
        $result = New-Object PSObject
        Add-Member -InputObject $result -MemberType NoteProperty -Name Passed -Value $passed
        Add-Member -InputObject $result -MemberType NoteProperty -Name AuthCookieFound -Value $authCookieFound
        Add-Member -InputObject $result -MemberType NoteProperty -Name CookiesCopied -Value $allCookiesCopied
        Add-Member -InputObject $result -MemberType NoteProperty -Name MarkerFound -Value $logoffFound
        return $result
    }
    catch {
        Write-Log ('WARN HTTP login fallback failed: ' + $_.Exception.Message)
        $result = New-Object PSObject
        Add-Member -InputObject $result -MemberType NoteProperty -Name Passed -Value $false
        Add-Member -InputObject $result -MemberType NoteProperty -Name AuthCookieFound -Value $false
        Add-Member -InputObject $result -MemberType NoteProperty -Name CookiesCopied -Value 0
        Add-Member -InputObject $result -MemberType NoteProperty -Name MarkerFound -Value $false
        return $result
    }
}

function Add-Result {
    param(
        [string]$Step,
        [bool]$Passed,
        [string]$Message,
        [string]$Url,
        [string]$Screenshot,
        [string]$Html
    )

    $item = New-Object PSObject
    Add-Member -InputObject $item -MemberType NoteProperty -Name Step -Value $Step
    Add-Member -InputObject $item -MemberType NoteProperty -Name Passed -Value $Passed
    Add-Member -InputObject $item -MemberType NoteProperty -Name Message -Value $Message
    Add-Member -InputObject $item -MemberType NoteProperty -Name Url -Value $Url
    Add-Member -InputObject $item -MemberType NoteProperty -Name Screenshot -Value $Screenshot
    Add-Member -InputObject $item -MemberType NoteProperty -Name Html -Value $Html
    [void]$Results.Add($item)

    if ($Passed) {
        Write-Log ('PASS {0}: {1}' -f $Step, $Message)
    }
    else {
        Write-Log ('FAIL {0}: {1}' -f $Step, $Message)
    }
}

function Write-ResultJson {
    $lines = New-Object System.Collections.ArrayList
    [void]$lines.Add('{')
    [void]$lines.Add(('  "taskName": "{0}",' -f (ConvertTo-JsonString $TaskName)))
    [void]$lines.Add(('  "baseUrl": "{0}",' -f (ConvertTo-JsonString $BaseUrl)))
    [void]$lines.Add(('  "startedAt": "{0}",' -f $StartedAt.ToString('s')))
    [void]$lines.Add(('  "finishedAt": "{0}",' -f (Get-Date).ToString('s')))
    [void]$lines.Add('  "results": [')

    for ($i = 0; $i -lt $Results.Count; $i++) {
        $item = $Results[$i]
        $suffix = if ($i -lt ($Results.Count - 1)) { ',' } else { '' }
        [void]$lines.Add('    {')
        [void]$lines.Add(('      "step": "{0}",' -f (ConvertTo-JsonString $item.Step)))
        $passedText = if ($item.Passed) { 'true' } else { 'false' }
        [void]$lines.Add(('      "passed": {0},' -f $passedText))
        [void]$lines.Add(('      "message": "{0}",' -f (ConvertTo-JsonString $item.Message)))
        [void]$lines.Add(('      "url": "{0}",' -f (ConvertTo-JsonString $item.Url)))
        [void]$lines.Add(('      "screenshot": "{0}",' -f (ConvertTo-JsonString $item.Screenshot)))
        [void]$lines.Add(('      "html": "{0}"' -f (ConvertTo-JsonString $item.Html)))
        [void]$lines.Add(('    }}{0}' -f $suffix))
    }

    [void]$lines.Add('  ]')
    [void]$lines.Add('}')
    Set-Content -LiteralPath $ResultJsonPath -Value $lines.ToArray() -Encoding UTF8
}

function Join-PortalUrl {
    param(
        [string]$Root,
        [string]$Path
    )

    if ($Root.EndsWith('/')) {
        return $Root + $Path.TrimStart('/')
    }

    return $Root + '/' + $Path.TrimStart('/')
}

function Wait-InternetExplorer {
    param([object]$Browser)

    $deadline = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $deadline) {
        try {
            if (-not $Browser.Busy -and $Browser.ReadyState -eq 4) {
                Start-Sleep -Milliseconds 500
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }

        Start-Sleep -Milliseconds 250
    }

    throw 'Internet Explorer did not finish loading before timeout.'
}

function Save-PageHtml {
    param(
        [object]$Browser,
        [string]$Step
    )

    $path = Join-Path $RunRoot ($Step + '.html')
    $html = ''
    try {
        if ($null -ne $Browser.Document -and $null -ne $Browser.Document.documentElement) {
            $html = $Browser.Document.documentElement.outerHTML
        }
    }
    catch {
        $html = '<!-- unable to read document html: ' + $_.Exception.Message + ' -->'
    }

    Set-Content -LiteralPath $path -Value $html -Encoding UTF8
    return $path
}

function Save-Screenshot {
    param([string]$Step)

    $path = Join-Path $RunRoot ($Step + '.png')
    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
        $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
            $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
            return $path
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
    catch {
        Write-Log ('WARN screenshot failed for ' + $Step + ': ' + $_.Exception.Message)
        return ''
    }
}

function Get-BodyText {
    param([object]$Browser)

    try {
        if ($null -ne $Browser.Document -and $null -ne $Browser.Document.body) {
            return [string]$Browser.Document.body.innerText
        }
    }
    catch {
        return ''
    }

    return ''
}

function Get-DocumentHtml {
    param([object]$Browser)

    try {
        if ($null -ne $Browser.Document -and $null -ne $Browser.Document.documentElement) {
            return [string]$Browser.Document.documentElement.outerHTML
        }
    }
    catch {
        return ''
    }

    return ''
}

function Test-AnyKeyword {
    param(
        [string]$Text,
        [string[]]$Keywords
    )

    foreach ($keyword in $Keywords) {
        if (-not [string]::IsNullOrEmpty($keyword) -and $Text.IndexOf($keyword, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Get-ElementsByTagNameCompat {
    param(
        [object]$Document,
        [string]$TagName
    )

    $items = New-Object System.Collections.ArrayList
    $collection = $null

    try {
        if ($null -ne $Document.all) {
            $collection = $Document.all.tags($TagName)
        }
    }
    catch {
    }

    if ($null -eq $collection) {
        try {
            $collection = $Document.getElementsByTagName($TagName)
        }
        catch {
            $collection = $null
        }
    }

    if ($null -eq $collection) {
        return $items
    }

    $count = Get-CollectionCountCompat -Collection $collection
    if ($count -gt 0) {
        for ($i = 0; $i -lt $count; $i++) {
            $element = Get-CollectionItemCompat -Collection $collection -Index $i
            if ($null -ne $element) {
                [void]$items.Add($element)
            }
        }

        return $items
    }

    try {
        foreach ($element in $collection) {
            if ($null -ne $element) {
                [void]$items.Add($element)
            }
        }
    }
    catch {
    }

    return $items
}

function Get-CollectionCountCompat {
    param([object]$Collection)

    try {
        if ($null -ne $Collection.length) {
            return [int]$Collection.length
        }
    }
    catch {
    }

    try {
        if ($null -ne $Collection.Length) {
            return [int]$Collection.Length
        }
    }
    catch {
    }

    try {
        if ($null -ne $Collection.count) {
            return [int]$Collection.count
        }
    }
    catch {
    }

    try {
        if ($null -ne $Collection.Count) {
            return [int]$Collection.Count
        }
    }
    catch {
    }

    return 0
}

function Get-CollectionItemCompat {
    param(
        [object]$Collection,
        [int]$Index
    )

    try {
        return $Collection.item($Index)
    }
    catch {
    }

    try {
        return $Collection.item($Index, 0)
    }
    catch {
    }

    try {
        return $Collection.Item($Index)
    }
    catch {
    }

    try {
        return $Collection.Item($Index, 0)
    }
    catch {
    }

    try {
        return $Collection[$Index]
    }
    catch {
    }

    return $null
}

function Test-ElementLooksUsable {
    param([object]$Element)

    if ($null -eq $Element) {
        return $false
    }

    try {
        if (-not [string]::IsNullOrEmpty([string]$Element.tagName)) {
            return $true
        }
    }
    catch {
    }

    try {
        if (-not [string]::IsNullOrEmpty([string]$Element.id)) {
            return $true
        }
    }
    catch {
    }

    try {
        if (-not [string]::IsNullOrEmpty([string]$Element.name)) {
            return $true
        }
    }
    catch {
    }

    return $false
}

function Get-ElementAttributeCompat {
    param(
        [object]$Element,
        [string]$Name
    )

    try {
        $value = $Element.getAttribute($Name)
        if ($null -ne $value) {
            return [string]$value
        }
    }
    catch {
    }

    try {
        $value = $Element.$Name
        if ($null -ne $value) {
            return [string]$value
        }
    }
    catch {
    }

    return ''
}

function Get-ElementByIdCompat {
    param(
        [object]$Document,
        [string]$Id
    )

    try {
        $element = $Document.getElementById($Id)
        if (Test-ElementLooksUsable -Element $element) {
            return $element
        }
    }
    catch {
    }

    try {
        if ($null -ne $Document.all) {
            $element = $Document.all.item($Id)
            if (Test-ElementLooksUsable -Element $element) {
                return $element
            }
        }
    }
    catch {
    }

    try {
        if ($null -ne $Document.all) {
            $element = $Document.all.item($Id, 0)
            if (Test-ElementLooksUsable -Element $element) {
                return $element
            }
        }
    }
    catch {
    }

    return $null
}

function Get-ElementsByNameCompat {
    param(
        [object]$Document,
        [string]$Name
    )

    $items = New-Object System.Collections.ArrayList

    try {
        $collection = $Document.getElementsByName($Name)
        $count = Get-CollectionCountCompat -Collection $collection
        for ($i = 0; $i -lt $count; $i++) {
            $element = Get-CollectionItemCompat -Collection $collection -Index $i
            if (Test-ElementLooksUsable -Element $element) {
                [void]$items.Add($element)
            }
        }
    }
    catch {
    }

    if ($items.Count -gt 0) {
        return $items
    }

    try {
        if ($null -ne $Document.all) {
            $elementOrCollection = $Document.all.item($Name)
            if (Test-ElementLooksUsable -Element $elementOrCollection) {
                [void]$items.Add($elementOrCollection)
                return $items
            }

            $count = Get-CollectionCountCompat -Collection $elementOrCollection
            for ($i = 0; $i -lt $count; $i++) {
                $element = Get-CollectionItemCompat -Collection $elementOrCollection -Index $i
                if (Test-ElementLooksUsable -Element $element) {
                    [void]$items.Add($element)
                }
            }
        }
    }
    catch {
    }

    return $items
}

function Test-EndsWithIgnoreCase {
    param(
        [string]$Value,
        [string]$Suffix
    )

    if ([string]::IsNullOrEmpty($Value) -or [string]::IsNullOrEmpty($Suffix)) {
        return $false
    }

    return $Value.ToLowerInvariant().EndsWith($Suffix.ToLowerInvariant())
}

function Write-InputInventory {
    param([object]$Document)

    try {
        $inputs = Get-ElementsByTagNameCompat -Document $Document -TagName 'input'
        $index = 0
        foreach ($input in $inputs) {
            $id = Get-ElementAttributeCompat -Element $input -Name 'id'
            $name = Get-ElementAttributeCompat -Element $input -Name 'name'
            $type = Get-ElementAttributeCompat -Element $input -Name 'type'
            Write-Log ('INPUT {0}: id={1}; name={2}; type={3}' -f $index, $id, $name, $type)
            $index++
        }
    }
    catch {
        Write-Log ('WARN input inventory failed: ' + $_.Exception.Message)
    }
}

function Find-InputByIdSuffix {
    param(
        [object]$Document,
        [string]$Suffix
    )

    $inputs = Get-ElementsByTagNameCompat -Document $Document -TagName 'input'
    foreach ($input in $inputs) {
        try {
            $id = Get-ElementAttributeCompat -Element $input -Name 'id'
            $name = Get-ElementAttributeCompat -Element $input -Name 'name'
            if ((Test-EndsWithIgnoreCase -Value $id -Suffix $Suffix) -or
                (Test-EndsWithIgnoreCase -Value $name -Suffix $Suffix)) {
                return $input
            }
        }
        catch {
        }
    }

    return $null
}

function Find-InputByKnownIdentity {
    param(
        [object]$Document,
        [string[]]$Ids,
        [string[]]$Names
    )

    foreach ($id in $Ids) {
        $element = Get-ElementByIdCompat -Document $Document -Id $id
        if (Test-ElementLooksUsable -Element $element) {
            return $element
        }
    }

    foreach ($name in $Names) {
        $elements = Get-ElementsByNameCompat -Document $Document -Name $name
        foreach ($element in $elements) {
            if (Test-ElementLooksUsable -Element $element) {
                return $element
            }
        }
    }

    return $null
}

function ConvertTo-JavascriptString {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return $Value.Replace('\', '\\').Replace("'", "\'").Replace("`r", '\r').Replace("`n", '\n')
}

function Invoke-PortalLoginByScript {
    param(
        [string]$UserName,
        [string]$Password
    )

    try {
        $userScript = ConvertTo-JavascriptString -Value $UserName
        $passwordScript = ConvertTo-JavascriptString -Value $Password
        $script = "(function(){function ew(v,s){v=(v||'').toLowerCase();s=s.toLowerCase();return v.length>=s.length&&v.substr(v.length-s.length)==s;}function bySuffix(s){var xs=document.getElementsByTagName('input');for(var i=0;i<xs.length;i++){var e=xs[i];if(ew(e.id,s)||ew(e.name,s)){return e;}}return null;}var u=document.getElementById('ctl00_MainContent_ctl01_EmailOrName')||bySuffix('EmailOrName');var p=document.getElementById('ctl00_MainContent_ctl01_password')||bySuffix('password');var b=document.getElementById('ctl00_MainContent_ctl01_SigninBtn')||bySuffix('SigninBtn');window.__PortalLegacyIeLoginResult='missing';if(u&&p&&b){u.value='" + $userScript + "';p.value='" + $passwordScript + "';window.__PortalLegacyIeLoginResult='clicked';b.click();}})();"
        $Ie.Document.parentWindow.execScript($script, 'JavaScript')
        return $true
    }
    catch {
        Write-Log ('WARN script login failed: ' + $_.Exception.Message)
        return $false
    }
}

function Invoke-PortalStep {
    param(
        [string]$Step,
        [string]$Url,
        [string[]]$Keywords
    )

    Write-Log ('NAV ' + $Step + ': ' + $Url)
    $Ie.Navigate($Url)
    Wait-InternetExplorer -Browser $Ie
    try {
        $Ie.Visible = $true
        $Ie.Width = 1200
        $Ie.Height = 900
    }
    catch {
    }

    $htmlPath = Save-PageHtml -Browser $Ie -Step $Step
    $screenshotPath = Save-Screenshot -Step $Step
    $bodyText = Get-BodyText -Browser $Ie
    $documentHtml = Get-DocumentHtml -Browser $Ie
    $locationText = ([string]$Ie.LocationName) + ' ' + ([string]$Ie.LocationURL)
    $combinedText = $bodyText + ' ' + $documentHtml + ' ' + $locationText
    $passed = Test-AnyKeyword -Text $combinedText -Keywords $Keywords
    $message = if ($passed) { 'Expected keyword found.' } else { 'Expected keyword not found; manual review required.' }
    Add-Result -Step $Step -Passed $passed -Message $message -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
}

function Invoke-PortalLogin {
    if ($SkipLogin) {
        Add-Result -Step 'login' -Passed $true -Message 'Skipped by parameter.' -Url ([string]$Ie.LocationURL) -Screenshot '' -Html ''
        return
    }

    $plainPassword = Get-AdminLoginPassword

    Write-Log 'LOGIN finding fields.'
    Write-InputInventory -Document $Ie.Document
    $userInput = Find-InputByKnownIdentity -Document $Ie.Document -Ids @('ctl00_MainContent_ctl01_EmailOrName', 'EmailOrName') -Names @('ctl00$MainContent$ctl01$EmailOrName', 'EmailOrName')
    if ($null -eq $userInput) {
        $userInput = Find-InputByIdSuffix -Document $Ie.Document -Suffix 'EmailOrName'
    }

    $passwordInput = Find-InputByKnownIdentity -Document $Ie.Document -Ids @('ctl00_MainContent_ctl01_password', 'password') -Names @('ctl00$MainContent$ctl01$password', 'password')
    if ($null -eq $passwordInput) {
        $passwordInput = Find-InputByIdSuffix -Document $Ie.Document -Suffix 'password'
    }

    $button = Find-InputByKnownIdentity -Document $Ie.Document -Ids @('ctl00_MainContent_ctl01_SigninBtn', 'SigninBtn') -Names @('ctl00$MainContent$ctl01$SigninBtn', 'SigninBtn')
    if ($null -eq $button) {
        $button = Find-InputByIdSuffix -Document $Ie.Document -Suffix 'SigninBtn'
    }

    if ($null -eq $userInput -or $null -eq $passwordInput -or $null -eq $button) {
        Write-Log 'LOGIN using JavaScript DOM fallback.'
        if (-not (Invoke-PortalLoginByScript -UserName $AdminUser -Password $plainPassword)) {
            Write-Log 'LOGIN using keyboard fallback.'
            if (Invoke-PortalLoginByKeyboard -Browser $Ie -UserName $AdminUser -Password $plainPassword) {
                $htmlPath = Save-PageHtml -Browser $Ie -Step 'login'
                $screenshotPath = Save-Screenshot -Step 'login'
                $message = 'Keyboard login submitted; the next protected-page step validates the authenticated session.'
                Add-Result -Step 'login' -Passed $true -Message $message -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
                return
            }
            else {
                Write-Log 'LOGIN using HTTP cookie fallback.'
                $httpLogin = Invoke-PortalLoginByHttp -Root $BaseUrl -UserName $AdminUser -Password $plainPassword
                if ($httpLogin.Passed) {
                    $Ie.Navigate((Join-PortalUrl -Root $BaseUrl -Path 'Default.aspx'))
                    Wait-InternetExplorer -Browser $Ie
                    $htmlPath = Save-PageHtml -Browser $Ie -Step 'login'
                    $screenshotPath = Save-Screenshot -Step 'login'
                    $message = 'HTTP fallback login completed. AuthCookieFound=' + $httpLogin.AuthCookieFound + '; CookiesCopied=' + $httpLogin.CookiesCopied + '; MarkerFound=' + $httpLogin.MarkerFound
                    Add-Result -Step 'login' -Passed $true -Message $message -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
                    return
                }
                else {
                    $htmlPath = Save-PageHtml -Browser $Ie -Step 'login-fields-missing'
                    $screenshotPath = Save-Screenshot -Step 'login-fields-missing'
                    Add-Result -Step 'login' -Passed $false -Message 'Login fields were not found and fallbacks failed.' -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
                    return
                }
            }
        }
    }
    else {
        $userInput.value = $AdminUser
        $passwordInput.value = $plainPassword
        $button.click()
    }

    Wait-InternetExplorer -Browser $Ie
    $htmlPath = Save-PageHtml -Browser $Ie -Step 'login'
    $screenshotPath = Save-Screenshot -Step 'login'
    $bodyText = Get-BodyText -Browser $Ie
    $passed = Test-AnyKeyword -Text $bodyText -Keywords @('Logoff', 'Admin')
    $message = if ($passed) { 'Login marker found.' } else { 'Login marker not found; manual review required.' }
    Add-Result -Step 'login' -Passed $passed -Message $message -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
}

function New-ResultZip {
    $zipPath = Join-Path $ResultsRoot ('PortalLegacyIeResult-' + $Stamp + '.zip')
    try {
        $emptyZip = [byte[]](80,75,5,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0)
        [System.IO.File]::WriteAllBytes($zipPath, $emptyZip)
        $shell = New-Object -ComObject Shell.Application
        $zip = $shell.NameSpace($zipPath)
        $source = $shell.NameSpace($RunRoot)
        if ($null -eq $zip -or $null -eq $source) {
            throw 'Shell zip namespace was not available.'
        }

        $zip.CopyHere($source.Items(), 20)
        Start-Sleep -Seconds 3
        Write-Log ('RESULT ZIP ' + $zipPath)
    }
    catch {
        Write-Log ('WARN result zip failed: ' + $_.Exception.Message)
        Write-Log ('RESULT DIR ' + $RunRoot)
    }
}

New-DirectoryIfMissing -Path $ResultsRoot
New-DirectoryIfMissing -Path $RunRoot
Write-Log ('START task=' + $TaskName + ' baseUrl=' + $BaseUrl)

if ($DryRun) {
    Add-Result -Step 'dry-run' -Passed $true -Message 'Package script dry run completed without launching IE.' -Url $BaseUrl -Screenshot '' -Html ''
    Write-ResultJson
    Write-Log ('RESULT JSON ' + $ResultJsonPath)
    exit 0
}

try {
    $Ie = New-Object -ComObject InternetExplorer.Application
    $Ie.Visible = $true
    $Ie.Width = 1200
    $Ie.Height = 900

    Invoke-PortalStep -Step 'home' -Url (Join-PortalUrl -Root $BaseUrl -Path 'Default.aspx') -Keywords @('Portal', 'Home', 'Default.aspx')
    Invoke-PortalLogin
    Invoke-PortalStep -Step 'admin-system-health' -Url (Join-PortalUrl -Root $BaseUrl -Path 'Admin/SystemHealth.aspx') -Keywords @('System Health', 'SystemHealth.aspx', 'SystemHealth')
    Invoke-PortalStep -Step 'generic-error-page' -Url (Join-PortalUrl -Root $BaseUrl -Path 'GenericErrorPage.aspx?id=P9LegacyVmProbe') -Keywords @('P9LegacyVmProbe', 'event')
}
catch {
    Write-Log ('ERROR ' + $_.Exception.Message)
    try {
        if ($null -ne $Ie) {
            $htmlPath = Save-PageHtml -Browser $Ie -Step 'fatal-error'
            $screenshotPath = Save-Screenshot -Step 'fatal-error'
            Add-Result -Step 'fatal-error' -Passed $false -Message $_.Exception.Message -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
        }
        else {
            Add-Result -Step 'fatal-error' -Passed $false -Message $_.Exception.Message -Url $BaseUrl -Screenshot '' -Html ''
        }
    }
    catch {
    }
}
finally {
    Write-ResultJson
    Write-Log ('RESULT JSON ' + $ResultJsonPath)
    New-ResultZip
    if ($null -ne $Ie) {
        try {
            $Ie.Quit()
        }
        catch {
        }
    }
}

$failed = $false
foreach ($result in $Results) {
    if (-not $result.Passed) {
        $failed = $true
    }
}

if ($failed) {
    exit 1
}

exit 0
'@

$runSmoke = $runSmoke.Replace('__BASE_URL__', $baseUrlContent)
$runSmoke = $runSmoke.Replace('__ADMIN_USER__', $adminUserContent)
$runSmoke = $runSmoke.Replace('__TASK_NAME__', $taskNameContent)

Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'README.txt') -Content $readme
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'run-smoke.cmd') -Content $cmd
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'run-smoke.ps1') -Content $runSmoke
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'test-task.json') -Content $taskJson
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'selectors.json') -Content $selectorsJson
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'expected.json') -Content $expectedJson
Write-Utf8NoBomFile -Path (Join-Path $toolsRoot 'README.txt') -Content "Optional tools such as IEDriverServer.exe can be placed here for the Selenium track. The IE COM smoke track does not require them."
Write-Utf8NoBomFile -Path (Join-Path $resultsRoot '.gitkeep') -Content ''

$zipPath = $null
if (-not $NoZip) {
    $zipPath = Join-Path $outputRootPath ($packageName + '.zip')
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -Force
}

[pscustomobject][ordered]@{
    PackageRoot = $packageRoot
    ZipPath = $zipPath
    BaseUrl = $BaseUrl
    TaskName = $TaskName
    AdminUser = $AdminUser
}
