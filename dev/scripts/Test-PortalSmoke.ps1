[CmdletBinding()]
param(
    [ValidatePattern('^https?://')]
    [string]$BaseUrl = 'http://localhost:40001/',

    [switch]$StartIISExpress,

    [switch]$StopWhenComplete,

    [string]$AdminUser,

    [SecureString]$AdminPassword,

    [switch]$SkipAuthenticated,

    [switch]$CheckGenericErrorPage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'
$startedByScript = $false

function Add-PortalCheck {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail
    )

    $checks.Add([pscustomobject]@{
            Name = $Name
            Passed = $Passed
            Detail = $Detail
        })

    $prefix = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ('[{0}] {1}: {2}' -f $prefix, $Name, $Detail)
}

function Test-LocalHttpUri {
    param([Uri]$Uri)

    return $Uri.Scheme -eq 'http' -and
        ($Uri.Host -ieq 'localhost' -or $Uri.Host -eq '127.0.0.1' -or $Uri.Host -eq '::1')
}

function Test-TcpPort {
    param(
        [string]$ServerHost,
        [int]$Port
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $client.Connect($ServerHost, $Port)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Get-PortalIISExpressProcess {
    $sitePath = (Resolve-Path -LiteralPath (Join-Path $repoRoot 'src/Portal')).Path
    $escapedSitePath = [regex]::Escape($sitePath)

    return Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match $escapedSitePath } |
        Select-Object -First 1
}

function Invoke-PortalRequest {
    param(
        [string]$Uri,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [ValidateSet('Get', 'Post')]
        [string]$Method = 'Get',
        [hashtable]$Body
    )

    $parameters = @{
        Uri = $Uri
        Method = $Method
        WebSession = $WebSession
        SkipHttpErrorCheck = $true
        ErrorAction = 'Stop'
    }

    if ($null -ne $Body) {
        $parameters.Body = $Body
        $parameters.ContentType = 'application/x-www-form-urlencoded'
    }

    return Invoke-WebRequest @parameters
}

function Get-PortalResponsePath {
    param($Response)

    try {
        return $Response.BaseResponse.RequestMessage.RequestUri.AbsolutePath
    }
    catch {
        return ''
    }
}

function Wait-PortalReady {
    param(
        [string]$Uri,
        [int]$RetryCount = 20
    )

    for ($attempt = 1; $attempt -le $RetryCount; $attempt++) {
        try {
            $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
            $response = Invoke-PortalRequest -Uri $Uri -WebSession $session
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            # IIS Express may still be compiling the first request; retry without exposing transport details.
        }

        Start-Sleep -Seconds 1
    }

    throw 'Portal did not become ready before the smoke-test timeout.'
}

function Get-HtmlAttribute {
    param(
        [string]$Tag,
        [string]$Name
    )

    $pattern = '\b' + [regex]::Escape($Name) + '\s*=\s*(?:"(?<value>[^"]*)"|''(?<value>[^'']*)''|(?<value>[^\s>]+))'
    $match = [regex]::Match($Tag, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        return $null
    }

    return [System.Net.WebUtility]::HtmlDecode($match.Groups['value'].Value)
}

function Get-InputTagByIdSuffix {
    param(
        [string]$Html,
        [string]$IdSuffix
    )

    foreach ($match in [regex]::Matches($Html, '<input\b[^>]*>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $tag = $match.Value
        $id = Get-HtmlAttribute -Tag $tag -Name 'id'
        if ($id -and $id.EndsWith($IdSuffix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $tag
        }
    }

    return $null
}

function Get-HiddenFormFields {
    param([string]$Html)

    $fields = @{}
    foreach ($match in [regex]::Matches($Html, '<input\b[^>]*>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $tag = $match.Value
        $type = Get-HtmlAttribute -Tag $tag -Name 'type'
        if (-not [string]::Equals($type, 'hidden', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $name = Get-HtmlAttribute -Tag $tag -Name 'name'
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        $fields[$name] = Get-HtmlAttribute -Tag $tag -Name 'value'
    }

    return $fields
}

function Invoke-PortalAdminLogin {
    param(
        [string]$LoginUri,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [string]$UserName,
        [SecureString]$Password
    )

    $loginPage = Invoke-PortalRequest -Uri $LoginUri -WebSession $WebSession
    $loginPagePath = Get-PortalResponsePath -Response $loginPage
    $resolvedLoginUri = if ([string]::IsNullOrWhiteSpace($loginPagePath)) {
        $LoginUri
    }
    else {
        [Uri]::new([Uri]$LoginUri, $loginPagePath).AbsoluteUri
    }
    $userTag = Get-InputTagByIdSuffix -Html $loginPage.Content -IdSuffix 'EmailOrName'
    $passwordTag = Get-InputTagByIdSuffix -Html $loginPage.Content -IdSuffix 'password'
    $buttonTag = Get-InputTagByIdSuffix -Html $loginPage.Content -IdSuffix 'SigninBtn'

    $userField = if ($userTag) { Get-HtmlAttribute -Tag $userTag -Name 'name' } else { $null }
    $passwordField = if ($passwordTag) { Get-HtmlAttribute -Tag $passwordTag -Name 'name' } else { $null }
    $buttonField = if ($buttonTag) { Get-HtmlAttribute -Tag $buttonTag -Name 'name' } else { $null }
    if ([string]::IsNullOrWhiteSpace($userField) -or
        [string]::IsNullOrWhiteSpace($passwordField) -or
        [string]::IsNullOrWhiteSpace($buttonField)) {
        throw 'The sign-in form no longer exposes the expected Web Forms fields.'
    }

    $form = Get-HiddenFormFields -Html $loginPage.Content
    $passwordBstr = [IntPtr]::Zero
    try {
        # 密码只在提交窗体前短暂还原为托管字符串，finally 中立即释放 BSTR。
        # The password is materialized only for form submission and its BSTR is released immediately.
        $passwordBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
        $form[$userField] = $UserName
        $form[$passwordField] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBstr)
        $form[$buttonField + '.x'] = '1'
        $form[$buttonField + '.y'] = '1'
        [void](Invoke-PortalRequest -Uri $resolvedLoginUri -WebSession $WebSession -Method Post -Body $form)
    }
    finally {
        if ($passwordBstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBstr)
        }
    }

    $cookies = $WebSession.Cookies.GetCookies([Uri]$resolvedLoginUri)
    return [bool]($cookies | Where-Object { $_.Name -eq '.ASPXAUTH' })
}

if ($StopWhenComplete -and -not $StartIISExpress) {
    throw 'StopWhenComplete is valid only when StartIISExpress is also specified.'
}

if (-not [string]::IsNullOrWhiteSpace($AdminUser) -and $SkipAuthenticated) {
    Write-Host '[INFO] Authenticated smoke checks were explicitly skipped.'
}

if ([string]::IsNullOrWhiteSpace($AdminUser) -and $null -ne $AdminPassword) {
    throw 'AdminPassword requires AdminUser.'
}

$baseUri = [Uri]$BaseUrl
if (-not $baseUri.IsAbsoluteUri) {
    throw 'BaseUrl must be an absolute HTTP or HTTPS URI.'
}

if ($StartIISExpress) {
    if (-not (Test-LocalHttpUri -Uri $baseUri)) {
        throw 'StartIISExpress only supports a local HTTP BaseUrl.'
    }

    $portAlreadyListening = Test-TcpPort -ServerHost $baseUri.Host -Port $baseUri.Port
    $existingPortalProcess = Get-PortalIISExpressProcess
    if (-not $portAlreadyListening -and $existingPortalProcess) {
        throw 'Portal IIS Express is already running on a different port. Stop that instance explicitly before requesting a new port.'
    }

    if (-not $portAlreadyListening) {
        & (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Port $baseUri.Port
        $startedByScript = $true
    }
}

try {
    Wait-PortalReady -Uri $baseUri.AbsoluteUri

    $anonymousSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $homeResponse = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, 'Default.aspx').AbsoluteUri) -WebSession $anonymousSession
    Add-PortalCheck -Name 'Home page' -Passed ($homeResponse.StatusCode -eq 200) -Detail ('HTTP ' + $homeResponse.StatusCode)

    $healthUri = [Uri]::new($baseUri, 'Admin/SystemHealth.aspx').AbsoluteUri
    $anonymousHealth = Invoke-PortalRequest -Uri $healthUri -WebSession $anonymousSession
    $anonymousHealthPath = Get-PortalResponsePath -Response $anonymousHealth
    $denied = $anonymousHealth.StatusCode -eq 200 -and
        $anonymousHealthPath -match '/Admin/(AccessDenied|EditAccessDenied)\.aspx$'
    Add-PortalCheck -Name 'Anonymous admin protection' -Passed $denied -Detail ('HTTP ' + $anonymousHealth.StatusCode + '; final path ' + $anonymousHealthPath)

    if ($CheckGenericErrorPage) {
        $missingUri = [Uri]::new($baseUri, ('P25SmokeMissing-' + [Guid]::NewGuid().ToString('N') + '.aspx')).AbsoluteUri
        $genericError = Invoke-PortalRequest -Uri $missingUri -WebSession $anonymousSession
        $genericErrorContent = [System.Net.WebUtility]::HtmlDecode($genericError.Content)
        $isGenericError = $genericError.StatusCode -eq 200 -and
            (Get-PortalResponsePath -Response $genericError) -eq '/GenericErrorPage.aspx' -and
            $genericErrorContent -match '应用程序暂时无法完成请求|系统已记录本次错误'
        Add-PortalCheck -Name 'Generic error page' -Passed $isGenericError -Detail ('HTTP ' + $genericError.StatusCode)
    }

    if (-not $SkipAuthenticated -and -not [string]::IsNullOrWhiteSpace($AdminUser)) {
        if ($null -eq $AdminPassword) {
            $AdminPassword = Read-Host -Prompt 'Admin password' -AsSecureString
        }

        $authenticatedSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        $loginSucceeded = Invoke-PortalAdminLogin -LoginUri ([Uri]::new($baseUri, 'Default.aspx').AbsoluteUri) -WebSession $authenticatedSession -UserName $AdminUser -Password $AdminPassword
        $loginDetail = if ($loginSucceeded) { 'Authentication cookie received.' } else { 'Authentication cookie was not received.' }
        Add-PortalCheck -Name 'Admin sign-in' -Passed $loginSucceeded -Detail $loginDetail

        if ($loginSucceeded) {
            $adminPages = @(
                @{ Name = 'System health'; Path = 'Admin/SystemHealth.aspx'; Marker = 'System Health' },
                @{ Name = 'Diagnostics logs'; Path = 'Admin/DiagnosticsLogs.aspx'; Marker = 'Diagnostics Logs' },
                @{ Name = 'Operation audits'; Path = 'Admin/OperationAudits.aspx'; Marker = 'Operation Audits' }
            )

            foreach ($page in $adminPages) {
                $response = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, $page.Path).AbsoluteUri) -WebSession $authenticatedSession
                $passed = $response.StatusCode -eq 200 -and $response.Content -match [regex]::Escape($page.Marker)
                Add-PortalCheck -Name $page.Name -Passed $passed -Detail ('HTTP ' + $response.StatusCode)
            }
        }
    }

    $failedChecks = @($checks | Where-Object { -not $_.Passed })
    [pscustomobject]@{
        BaseUrl = $baseUri.AbsoluteUri
        TotalChecks = $checks.Count
        FailedChecks = $failedChecks.Count
        StartedIISExpress = $startedByScript
    }

    if ($failedChecks.Count -gt 0) {
        throw ('Portal smoke test failed: ' + (($failedChecks | ForEach-Object { $_.Name }) -join ', '))
    }
}
finally {
    if ($startedByScript -and $StopWhenComplete) {
        & (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') -Port $baseUri.Port
    }
}
