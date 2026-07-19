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
  2. Double-click run-smoke.cmd.
  3. Enter the admin password when prompted.
  4. Wait until the script closes Internet Explorer.
  5. Copy the generated results folder or PortalLegacyIeResult-*.zip back to the host.

Default settings:
  Task: $TaskName
  Base URL: $BaseUrl
  Admin user: $AdminUser

If the VM cannot access the default Base URL:
  Open a command prompt in this folder and run:
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File run-smoke.ps1 -BaseUrl http://HOST-IP:40001/

Notes:
  - The script uses InternetExplorer.Application COM automation.
  - No IEDriver, Java, Node.js, or browser plugin is required.
  - Screenshots are desktop screenshots, so keep the VM desktop unlocked and IE visible.
  - Passwords are requested at runtime and are not written into this package.
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
echo.
echo Finished. Press any key to close this window.
pause >nul
"@

$runSmoke = @'
param(
    [string]$BaseUrl = '__BASE_URL__',
    [string]$AdminUser = '__ADMIN_USER__',
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

function ConvertTo-JsonString {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return ($Value -replace '\\', '\\' -replace '"', '\"' -replace "`r", '\r' -replace "`n", '\n')
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

function Find-InputByIdSuffix {
    param(
        [object]$Document,
        [string]$Suffix
    )

    $inputs = $Document.getElementsByTagName('input')
    foreach ($input in $inputs) {
        try {
            $id = [string]$input.id
            if (-not [string]::IsNullOrEmpty($id) -and $id.EndsWith($Suffix, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $input
            }
        }
        catch {
        }
    }

    return $null
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
    $passed = Test-AnyKeyword -Text $bodyText -Keywords $Keywords
    $message = if ($passed) { 'Expected keyword found.' } else { 'Expected keyword not found; manual review required.' }
    Add-Result -Step $Step -Passed $passed -Message $message -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
}

function Invoke-PortalLogin {
    if ($SkipLogin) {
        Add-Result -Step 'login' -Passed $true -Message 'Skipped by parameter.' -Url ([string]$Ie.LocationURL) -Screenshot '' -Html ''
        return
    }

    $securePassword = Read-Host -Prompt ('Password for ' + $AdminUser + ' (not logged)') -AsSecureString
    $plainPassword = ConvertTo-PlainText -SecureText $securePassword

    Write-Log 'LOGIN finding fields.'
    $userInput = Find-InputByIdSuffix -Document $Ie.Document -Suffix 'EmailOrName'
    $passwordInput = Find-InputByIdSuffix -Document $Ie.Document -Suffix 'password'
    $button = Find-InputByIdSuffix -Document $Ie.Document -Suffix 'SigninBtn'

    if ($null -eq $userInput -or $null -eq $passwordInput -or $null -eq $button) {
        $htmlPath = Save-PageHtml -Browser $Ie -Step 'login-fields-missing'
        $screenshotPath = Save-Screenshot -Step 'login-fields-missing'
        Add-Result -Step 'login' -Passed $false -Message 'Login fields were not found on the current page.' -Url ([string]$Ie.LocationURL) -Screenshot $screenshotPath -Html $htmlPath
        return
    }

    $userInput.value = $AdminUser
    $passwordInput.value = $plainPassword
    $button.click()
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

    Invoke-PortalStep -Step 'home' -Url (Join-PortalUrl -Root $BaseUrl -Path 'Default.aspx') -Keywords @('ASP.NET Portal Starter Kit', 'Portal')
    Invoke-PortalLogin
    Invoke-PortalStep -Step 'admin-system-health' -Url (Join-PortalUrl -Root $BaseUrl -Path 'Admin/SystemHealth.aspx') -Keywords @('System Health')
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
