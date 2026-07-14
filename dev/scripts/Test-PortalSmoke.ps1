[CmdletBinding()]
param(
    [ValidatePattern('^https?://')]
    [string]$BaseUrl = 'http://localhost:40001/',

    [switch]$StartIISExpress,

    [switch]$StopWhenComplete,

    [string]$AdminUser,

    [SecureString]$AdminPassword,

    [switch]$SkipAuthenticated,

    [switch]$CheckGenericErrorPage,

    [switch]$CheckDocumentSafety,

    [switch]$CheckEditorSafety
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
        $genericErrorPath = Get-PortalResponsePath -Response $genericError
        # 根站点与虚拟目录会有不同的应用路径前缀，只断言目标错误页而不硬编码站点根路径。
        # Root and virtual-directory hosting use different application prefixes, so assert the target error page rather than a root-only path.
        $isGenericError = $genericError.StatusCode -eq 200 -and
            $genericErrorPath -match '/GenericErrorPage\.aspx$' -and
            $genericErrorContent -match '应用程序暂时无法完成请求|系统已记录本次错误'
        Add-PortalCheck -Name 'Generic error page' -Passed $isGenericError -Detail ('HTTP ' + $genericError.StatusCode)
    }

    if ($CheckDocumentSafety) {
        # 中文：使用仓库已有 sample.doc 验证允许扩展名的静态服务与全局 nosniff 响应头，不创建或删除上传文件。
        # English: Use the repository's existing sample.doc to verify allowed-extension static serving and the global nosniff header without creating or deleting uploads.
        $allowedUpload = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, 'uploads/sample.doc').AbsoluteUri) -WebSession $anonymousSession
        $allowedUploadPassed = $allowedUpload.StatusCode -eq 200 -and
            [string]::Equals(
                [string]$allowedUpload.Headers['X-Content-Type-Options'],
                'nosniff',
                [System.StringComparison]::OrdinalIgnoreCase)
        Add-PortalCheck -Name 'Upload allowed-extension serving' -Passed $allowedUploadPassed -Detail ('HTTP ' + $allowedUpload.StatusCode)

        # 中文：不存在的 .aspx 仍应在目录级 requestFiltering 阶段被拒绝为 IIS 404.7，不能落到页面处理器。
        # English: A non-existing .aspx must still be rejected by directory-level requestFiltering as IIS 404.7, never reaching a page handler.
        $blockedUpload = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, ('uploads/P44Blocked-' + [Guid]::NewGuid().ToString('N') + '.aspx')).AbsoluteUri) -WebSession $anonymousSession
        $blockedUploadPassed = $blockedUpload.StatusCode -eq 404 -and $blockedUpload.Content -match '404\.7'
        Add-PortalCheck -Name 'Upload blocked-extension filtering' -Passed $blockedUploadPassed -Detail ('HTTP ' + $blockedUpload.StatusCode)

        # 中文：伪造事件编号只可显示“未提供”，不能成为管理员在日志页中无法查找的表面编号。
        # English: A forged event id may display only the fallback text, never an apparent id that administrators cannot find in logs.
        $forgedError = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, 'GenericErrorPage.aspx?id=P44-forged').AbsoluteUri) -WebSession $anonymousSession
        $forgedErrorContent = [System.Net.WebUtility]::HtmlDecode($forgedError.Content)
        $forgedErrorPassed = $forgedError.StatusCode -eq 200 -and $forgedErrorContent -match '事件编号：\s*未提供'
        Add-PortalCheck -Name 'Forged diagnostics event id' -Passed $forgedErrorPassed -Detail ('HTTP ' + $forgedError.StatusCode)
    }

    if ($CheckEditorSafety) {
        # 中文：正数但不存在的 Mid 不能穿透到严格数据查询并触发通用错误页，所有已迁移编辑器都应拒绝匿名访问。
        # English: A positive but non-existing Mid must not reach strict data access and trigger the generic error page; every migrated editor must deny anonymous access.
        $missingModuleId = '2147483647'
        $editorPages = @(
            @{ Name = 'Announcements editor missing module'; Path = ('DesktopModules/EditAnnouncements.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Contacts editor missing module'; Path = ('DesktopModules/EditContacts.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Events editor missing module'; Path = ('DesktopModules/EditEvents.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Links editor missing module'; Path = ('DesktopModules/EditLinks.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Image editor missing module'; Path = ('DesktopModules/EditImage.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'XML editor missing module'; Path = ('DesktopModules/EditXml.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'HTML editor missing module'; Path = ('DesktopModules/EditHtml.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Documents editor missing module'; Path = ('DesktopModules/EditDocs.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Discussion editor missing module'; Path = ('DesktopModules/DiscussDetails.aspx?Mid=' + $missingModuleId) }
        )

        foreach ($editorPage in $editorPages) {
            $response = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, $editorPage.Path).AbsoluteUri) -WebSession $anonymousSession
            $responsePath = Get-PortalResponsePath -Response $response
            $passed = $response.StatusCode -eq 200 -and $responsePath -match '/Admin/EditAccessDenied\.aspx$'
            Add-PortalCheck -Name $editorPage.Name -Passed $passed -Detail ('HTTP ' + $response.StatusCode + ' -> ' + $responsePath)
        }
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
                @{ Name = 'Operation audits'; Path = 'Admin/OperationAudits.aspx'; Marker = 'Operation Audits' },
                @{ Name = 'Theme settings'; Path = 'Admin/ThemeSettings.aspx'; Marker = 'Theme Settings' },
                @{ Name = 'Module catalog'; Path = 'Admin/ModuleCatalog.aspx'; Marker = 'Module Catalog' }
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
