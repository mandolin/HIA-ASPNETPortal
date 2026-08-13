<#
.SYNOPSIS
.LANG en
Creates a portable legacy IE smoke-test package.

.LANG zh-CN
创建可移植的旧 IE smoke 测试包。

.DESCRIPTION
<lang>
  <zh-CN>生成用于 Win7/IE 虚拟机内运行的零依赖旧 IE smoke 测试包。该包通过 IE COM 自动化驱动浏览器，写入详细日志和压缩结果，并且不得内嵌原始密码；认证使用 VM 侧凭据文件或由生成脚本解析的占位符。</zh-CN>
  <en>Generates a zero-dependency legacy IE smoke package for a Win7/IE VM. The package drives Internet Explorer through COM, writes detailed logs and zipped results, and must not embed raw passwords; authentication uses VM-local secret files or placeholders resolved by the generated script.</en>
</lang>

.PARAMETER BaseUrl
.LANG en
Portal base URL reachable from the VM.

.LANG zh-CN
虚拟机可访问的 Portal 基础地址。

.PARAMETER TaskName
.LANG en
Task label used in generated package and result names.

.LANG zh-CN
用于生成包和结果名称的任务标签。

.PARAMETER AdminUser
.LANG en
Logical admin user name used by the generated smoke script.

.LANG zh-CN
生成的 smoke 脚本使用的逻辑管理员用户名。
#>
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

# <lang>
#   <zh-CN>生成可复制到 Win7/IE 虚拟机内运行的零依赖 IE COM smoke 测试包；不在包内保存原始密码。</zh-CN>
#   <en>Generates a zero-dependency IE COM smoke package that can be copied to a Win7/IE VM without storing raw passwords in the package.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$outputRootPath = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRoot
}
else {
    Join-Path $repositoryRoot $OutputRoot
}

# <lang>
#   <zh-CN>将任务标签规范化为可跨文件系统使用的包名片段，并为空值提供稳定回退。</zh-CN>
#   <en>Normalizes the task label into a file-system-safe package-name fragment and supplies a stable fallback for empty input.</en>
# </lang>
function ConvertTo-SafeFileName {
    param([string]$Value)

    $safe = [regex]::Replace($Value, '[^\w\.-]+', '-')
    $safe = $safe.Trim('-')
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'PortalLegacyIeSmoke'
    }

    return $safe
}

# <lang>
#   <zh-CN>转义单引号内容，使生成脚本中的单引号字符串保持字面值而不改变注入边界。</zh-CN>
#   <en>Escapes single-quoted content so generated-script literals preserve their value without changing the injection boundary.</en>
# </lang>
function ConvertTo-PowerShellSingleQuotedContent {
    param([string]$Value)

    return ($Value -replace "'", "''")
}

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 和 CRLF 写入生成物，保持旧 Windows PowerShell 与文本审计兼容。</zh-CN>
#   <en>Writes generated artifacts as UTF-8 without BOM and with CRLF for legacy Windows PowerShell and text-audit compatibility.</en>
# </lang>
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

# <lang>
#   <zh-CN>下面的 here-string 是复制到 VM 内执行的独立 smoke 代理体；本文件只生成它，不在宿主机启动 IE、HTTP 或压缩流程。</zh-CN>
#   <en>The following here-string is the standalone smoke agent copied into the VM; this file only generates it and does not launch IE, HTTP, or compression on the host.</en>
# </lang>
$runSmoke = @'
param(
    [string]$BaseUrl = '__BASE_URL__',
    [string]$AdminUser = '__ADMIN_USER__',
    [string]$AdminPasswordFile = '',
    [string]$AdminPassword = '',
    [switch]$SkipLogin,
    [switch]$DryRun
)

# <lang>
#   <zh-CN>生成代理的运行时状态集中在结果目录、日志路径和清理标志中，避免跨阶段隐式共享状态。</zh-CN>
#   <en>Keep generated-agent runtime state in the result directory, log paths, and cleanup flags so phase boundaries remain explicit.</en>
# </lang>
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

# <lang>
#   <zh-CN>确保代理结果目录存在且只创建缺失目录。</zh-CN>
#   <en>Ensures the agent's result directories exist, creating only missing directories.</en>
# </lang>
function New-DirectoryIfMissing {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

# <lang>
#   <zh-CN>以时间戳写入日志并同步输出，保持诊断信息集中且不打印密码。</zh-CN>
#   <en>Writes timestamped diagnostics to the log and console without printing passwords.</en>
# </lang>
function Write-Log {
    param([string]$Message)

    $line = ('{0} {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message)
    Add-Content -LiteralPath $LogPath -Value $line -Encoding UTF8
    Write-Host $line
}

# <lang>
#   <zh-CN>在最小作用域内解包 SecureString，并在 finally 中清零 BSTR。</zh-CN>
#   <en>Unwraps a SecureString only within the smallest scope and zeroes the BSTR in finally.</en>
# </lang>
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

# <lang>
#   <zh-CN>把用户名约束为安全的秘密文件名片段，避免路径穿越和不稳定命名。</zh-CN>
#   <en>Constrains a user name to a safe secret-file fragment, preventing traversal and unstable names.</en>
# </lang>
function ConvertTo-SecretFileName {
    param([string]$Value)

    $safe = $Value -replace '[^A-Za-z0-9_.@-]+', '-'
    $safe = $safe.Trim('-')
    if ([string]::IsNullOrEmpty($safe)) {
        return 'admin'
    }

    return $safe
}

# <lang>
#   <zh-CN>按显式参数、显式文件和 VM 秘密目录的优先级读取密码，不回显秘密。</zh-CN>
#   <en>Reads the password from explicit parameters, an explicit file, or the VM secret directory without echoing it.</en>
# </lang>
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

# <lang>
#   <zh-CN>转义结果 JSON 中的字符串值，不引入新的序列化依赖。</zh-CN>
#   <en>Escapes result JSON string values without adding a serialization dependency.</en>
# </lang>
function ConvertTo-JsonString {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return ($Value -replace '\\', '\\' -replace '"', '\"' -replace "`r", '\r' -replace "`n", '\n')
}

# <lang>
#   <zh-CN>读取并关闭 HTTP 响应流，确保 fallback 不遗留网络资源。</zh-CN>
#   <en>Reads and closes an HTTP response stream so the fallback does not leak network resources.</en>
# </lang>
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

# <lang>
#   <zh-CN>从 HTML 标签提取并解码属性值，兼容单双引号和未加引号形式。</zh-CN>
#   <en>Extracts and decodes an HTML attribute while accepting quoted and unquoted legacy forms.</en>
# </lang>
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

# <lang>
#   <zh-CN>按 UTF-8 对表单字段编码，并为旧运行时提供 URI 回退。</zh-CN>
#   <en>URL-encodes a form component as UTF-8 with a URI fallback for older runtimes.</en>
# </lang>
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

# <lang>
#   <zh-CN>按稳定字段顺序拼接表单正文，保持 HTTP 登录提交契约。</zh-CN>
#   <en>Builds the form body from fields while preserving the HTTP login submission contract.</en>
# </lang>
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

# <lang>
#   <zh-CN>替换指定表单字段而不重复键，保持隐藏字段与凭据字段边界。</zh-CN>
#   <en>Replaces one form field without duplicate keys, preserving hidden-field and credential boundaries.</en>
# </lang>
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

# <lang>
#   <zh-CN>收集登录表单的可提交 input 字段，并保留已勾选控件的语义。</zh-CN>
#   <en>Collects submit-ready login inputs while preserving checked-control semantics.</en>
# </lang>
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

# <lang>
#   <zh-CN>把表单 action 解析为相对页面地址，异常时回退到当前页面。</zh-CN>
#   <en>Resolves form action against the page URI and falls back to the current page on malformed input.</en>
# </lang>
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

# <lang>
#   <zh-CN>仅提取 Set-Cookie 名称用于诊断日志，不记录 Cookie 值。</zh-CN>
#   <en>Extracts only Set-Cookie names for diagnostics and never records cookie values.</en>
# </lang>
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

# <lang>
#   <zh-CN>从响应头受限提取认证 Cookie 值，仅供后续受控写入 IE。</zh-CN>
#   <en>Extracts the authentication cookie value from a response header only for controlled IE injection.</en>
# </lang>
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

# <lang>
#   <zh-CN>惰性注册 WinInet Cookie P/Invoke 类型，并避免重复 Add-Type。</zh-CN>
#   <en>Lazily registers the WinInet cookie P/Invoke type and avoids duplicate Add-Type calls.</en>
# </lang>
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

# <lang>
#   <zh-CN>把受控 Cookie 写入 IE 的 WinInet 容器，并将失败降级为可审计结果。</zh-CN>
#   <en>Copies a controlled cookie into IE WinInet and degrades failures to an auditable result.</en>
# </lang>
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

# <lang>
#   <zh-CN>写入原始认证 Cookie 值的兼容路径，并保持失败不泄露值。</zh-CN>
#   <en>Provides the raw authentication-cookie compatibility path while never logging its value.</en>
# </lang>
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

# <lang>
#   <zh-CN>惰性注册窗口和鼠标自动化类型，为键盘 fallback 复用一次性状态。</zh-CN>
#   <en>Lazily registers window and mouse automation types for the keyboard fallback.</en>
# </lang>
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

# <lang>
#   <zh-CN>通过 clip.exe 临时写入剪贴板，并在进程结束后释放句柄。</zh-CN>
#   <en>Temporarily writes clipboard text through clip.exe and releases the process handle.</en>
# </lang>
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

# <lang>
#   <zh-CN>尽力清空剪贴板，避免键盘登录后的密码残留。</zh-CN>
#   <en>Best-effort clears the clipboard so keyboard login does not leave a password behind.</en>
# </lang>
function Clear-ClipboardByClipExe {
    try {
        [void](Set-ClipboardTextByClipExe -Text '')
    }
    catch {
    }
}

# <lang>
#   <zh-CN>调整 IE 窗口并置前，为坐标式键盘 fallback 建立可重复前提。</zh-CN>
#   <en>Positions and foregrounds IE to establish repeatable preconditions for coordinate-based keyboard fallback.</en>
# </lang>
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

# <lang>
#   <zh-CN>在已定位的 IE 窗口内执行受控鼠标点击并保留必要节拍。</zh-CN>
#   <en>Performs a controlled mouse click within the positioned IE window with required pacing.</en>
# </lang>
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

# <lang>
#   <zh-CN>通过临时剪贴板把文本粘贴到当前控件，避免模拟逐字符输入。</zh-CN>
#   <en>Pastes text through the temporary clipboard into the focused control instead of simulating keystrokes.</en>
# </lang>
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

# <lang>
#   <zh-CN>使用坐标和剪贴板完成最后一级登录 fallback，并确保清空剪贴板。</zh-CN>
#   <en>Performs the final coordinate-and-clipboard login fallback and always clears the clipboard.</en>
# </lang>
function Invoke-PortalLoginByKeyboard {
    param(
        [object]$Browser,
        [string]$UserName,
        [string]$Password
    )

    try {
        Write-Log 'LOGIN keyboard fallback positioning IE window.'
        Set-BrowserWindowForKeyboard -Browser $Browser

        # <lang>
        #   <zh-CN>坐标相对于本包已定位的 IE 窗口，窗口布局变化时必须先复核这些 fallback 前提。</zh-CN>
        #   <en>Coordinates are relative to the IE window positioned by this package; review the fallback precondition if the layout changes.</en>
        # </lang>
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

# <lang>
#   <zh-CN>执行 HTTP 表单登录、Cookie 复制与受控结果判定，不把凭据写入日志。</zh-CN>
#   <en>Performs HTTP form login, cookie copying, and controlled result classification without logging credentials.</en>
# </lang>
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

# <lang>
#   <zh-CN>追加步骤结果并统一输出 PASS/FAIL 日志，保持结果字段契约。</zh-CN>
#   <en>Adds a step result and emits the canonical PASS/FAIL log while preserving result fields.</en>
# </lang>
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

# <lang>
#   <zh-CN>以固定字段和稳定顺序写入结果 JSON，供宿主归档和人工复核。</zh-CN>
#   <en>Writes result JSON with stable fields and ordering for host-side archiving and review.</en>
# </lang>
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

# <lang>
#   <zh-CN>拼接门户根地址与相对路径，同时避免重复或缺失斜杠。</zh-CN>
#   <en>Joins the portal root and relative path without duplicate or missing slashes.</en>
# </lang>
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

# <lang>
#   <zh-CN>等待 IE 完成导航并在固定期限后失败，避免 smoke 无限挂起。</zh-CN>
#   <en>Waits for IE navigation with a fixed deadline so the smoke test cannot hang indefinitely.</en>
# </lang>
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

# <lang>
#   <zh-CN>保存当前页面 HTML 供失败复核，并将读取异常转为内嵌诊断标记。</zh-CN>
#   <en>Saves current page HTML for review and turns read failures into an embedded diagnostic marker.</en>
# </lang>
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

# <lang>
#   <zh-CN>捕获桌面截图用于人工复核，并在图形资源失败时安全降级。</zh-CN>
#   <en>Captures a desktop screenshot for review and degrades safely when graphics resources fail.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取 IE 文档正文文本，异常时返回空值而不阻断后续诊断。</zh-CN>
#   <en>Reads IE document body text and returns empty text on failure without blocking diagnostics.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取 IE 文档 HTML，异常时返回空值以保持结果生成。</zh-CN>
#   <en>Reads IE document HTML and returns empty text on failure so results can still be generated.</en>
# </lang>
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

# <lang>
#   <zh-CN>按不区分大小写的序列检查页面标记，保持 smoke 断言简单可审计。</zh-CN>
#   <en>Checks page markers with ordinal case-insensitive matching for simple auditable smoke assertions.</en>
# </lang>
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

# <lang>
#   <zh-CN>在 IE DOM 旧接口与标准接口之间提供标签集合兼容读取。</zh-CN>
#   <en>Reads tag collections through both legacy IE DOM and standard interfaces.</en>
# </lang>
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

# <lang>
#   <zh-CN>兼容 length、Length 和 count 形态读取 COM 集合数量。</zh-CN>
#   <en>Reads COM collection counts across length, Length, and count shapes.</en>
# </lang>
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

# <lang>
#   <zh-CN>兼容多种 item/Item 索引调用方式读取 COM 集合项。</zh-CN>
#   <en>Reads COM collection items across legacy item/Item index call shapes.</en>
# </lang>
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

# <lang>
#   <zh-CN>用最小属性探测判断 COM 元素是否可继续使用。</zh-CN>
#   <en>Uses minimal property probes to decide whether a COM element is usable.</en>
# </lang>
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

# <lang>
#   <zh-CN>兼容 getAttribute 与属性访问读取 DOM 属性，失败返回空值。</zh-CN>
#   <en>Reads a DOM attribute through getAttribute or property access, returning empty on failure.</en>
# </lang>
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

# <lang>
#   <zh-CN>在标准 DOM 与 IE all 集合之间按兼容顺序查找元素。</zh-CN>
#   <en>Finds an element through standard DOM and IE all-collection fallbacks.</en>
# </lang>
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

# <lang>
#   <zh-CN>按标准 name 查询并回退到 IE all 集合，返回可用元素集合。</zh-CN>
#   <en>Finds named elements through standard APIs and IE all-collection fallbacks.</en>
# </lang>
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

# <lang>
#   <zh-CN>执行旧运行时兼容的大小写不敏感后缀比较。</zh-CN>
#   <en>Performs an older-runtime-compatible case-insensitive suffix comparison.</en>
# </lang>
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

# <lang>
#   <zh-CN>记录登录页 input 的非敏感结构诊断，不输出控件值。</zh-CN>
#   <en>Logs non-sensitive login-input structure without outputting control values.</en>
# </lang>
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

# <lang>
#   <zh-CN>按 ID 或 name 后缀寻找兼容登录控件，避免依赖单一命名容器。</zh-CN>
#   <en>Finds a login control by ID or name suffix without relying on one naming container.</en>
# </lang>
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

# <lang>
#   <zh-CN>优先按已知 ID/name 白名单寻找登录控件，保持选择器边界固定。</zh-CN>
#   <en>Finds login controls from an allowlisted ID/name set before using broader fallbacks.</en>
# </lang>
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

# <lang>
#   <zh-CN>转义注入 JavaScript 的字面值，避免用户名或密码改变脚本结构。</zh-CN>
#   <en>Escapes JavaScript literal values so user names or passwords cannot change script structure.</en>
# </lang>
function ConvertTo-JavascriptString {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    return $Value.Replace('\', '\\').Replace("'", "\'").Replace("`r", '\r').Replace("`n", '\n')
}

# <lang>
#   <zh-CN>通过 IE 文档脚本提交登录控件，并将失败交给下一 fallback。</zh-CN>
#   <en>Submits the login controls through IE document script and delegates failure to the next fallback.</en>
# </lang>
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

# <lang>
#   <zh-CN>导航到一个 smoke 步骤，采集页面、截图并按关键词写入结果。</zh-CN>
#   <en>Navigates one smoke step, captures page artifacts, and records a keyword-based result.</en>
# </lang>
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

# <lang>
#   <zh-CN>按参数、DOM、脚本、键盘和 HTTP 顺序完成登录并记录结果。</zh-CN>
#   <en>Runs login through parameter, DOM, script, keyboard, and HTTP fallbacks in order.</en>
# </lang>
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

# <lang>
#   <zh-CN>使用 Shell COM 将本次结果目录压缩归档，失败时保留目录作为回退。</zh-CN>
#   <en>Uses Shell COM to archive the run directory and retains the directory when zipping fails.</en>
# </lang>
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

# <lang>
#   <zh-CN>先建立本次运行的结果目录和日志边界，再记录任务启动信息；此处不创建外部资源。</zh-CN>
#   <en>Establishes the run result and log boundaries before recording startup; no external resource is created here.</en>
# </lang>
New-DirectoryIfMissing -Path $ResultsRoot
New-DirectoryIfMissing -Path $RunRoot
Write-Log ('START task=' + $TaskName + ' baseUrl=' + $BaseUrl)

# <lang>
#   <zh-CN>DryRun 只生成结果 JSON 并退出，明确不启动 IE、读取秘密或访问门户。</zh-CN>
#   <en>DryRun writes only result JSON and exits without launching IE, reading secrets, or accessing the portal.</en>
# </lang>
if ($DryRun) {
    Add-Result -Step 'dry-run' -Passed $true -Message 'Package script dry run completed without launching IE.' -Url $BaseUrl -Screenshot '' -Html ''
    Write-ResultJson
    Write-Log ('RESULT JSON ' + $ResultJsonPath)
    exit 0
}

# <lang>
#   <zh-CN>真实 smoke 流程在受控 try/catch/finally 内启动 IE、执行导航和登录 fallback，并始终进入结果与资源清理。</zh-CN>
#   <en>The real smoke flow starts IE, navigation, and login fallbacks inside controlled try/catch/finally blocks and always reaches result and resource cleanup.</en>
# </lang>
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
# <lang>
#   <zh-CN>把未处理的导航或认证异常转换为非泄露的失败结果，同时尽力保存页面证据。</zh-CN>
#   <en>Converts unhandled navigation or authentication errors into a non-leaking failure result while best-effort evidence is saved.</en>
# </lang>
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
# <lang>
#   <zh-CN>无论成功或失败都写入结果、尝试压缩并退出 IE；清理失败不覆盖主结果。</zh-CN>
#   <en>Always writes results, attempts compression, and quits IE; cleanup failure does not replace the primary result.</en>
# </lang>
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

# <lang>
#   <zh-CN>汇总每个步骤的通过状态并以非零退出码向 VM 任务代理报告失败。</zh-CN>
#   <en>Aggregates step outcomes and reports failure to the VM task agent through a non-zero exit code.</en>
# </lang>
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
