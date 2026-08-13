<#
.SYNOPSIS
.LANG en
Starts IIS Express for the Portal development site.

.LANG zh-CN
为 Portal 开发站点启动 IIS Express。

.DESCRIPTION
<lang>
  <en>Starts an IIS Express instance for the Portal site, usually on the fixed VSCode automation port 40001. When a non-root virtual path or non-localhost host name is requested, the script generates an isolated applicationhost.config under temp. It does not modify Visual Studio project files, global IIS settings, databases, or external configuration.</en>
  <zh-CN>为 Portal 开发站点启动 IIS Express，通常使用 VSCode 自动化固定端口 40001。当请求非根虚拟路径或非 localhost 主机名时，脚本会在 temp 下生成隔离的 applicationhost.config。它不修改 Visual Studio 项目文件、全局 IIS 设置、数据库或外置配置。</zh-CN>
</lang>

.PARAMETER Port
.LANG en
Local IIS Express port.

.LANG zh-CN
本地 IIS Express 端口。

.PARAMETER SitePath
.LANG en
Portal site physical path. Defaults to src/Portal.

.LANG zh-CN
Portal 站点物理路径，默认指向 src/Portal。

.PARAMETER VirtualPath
.LANG en
Optional virtual application path used to approximate IIS virtual-directory
deployment.

.LANG zh-CN
可选虚拟应用路径，用于近似验证 IIS 虚拟目录部署形态。

.PARAMETER HostName
.LANG en
Host name for the generated IIS Express binding.

.LANG zh-CN
生成 IIS Express 绑定时使用的主机名。
#>
[CmdletBinding()]
param(
    [int]$Port = 40001,

    [string]$SitePath,

    [string]$VirtualPath = '/',

    [string]$HostName = 'localhost'
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>以下状态只描述本次本地 IIS Express 启动请求；不代表共享 IIS、生产站点或外置配置。</zh-CN>
#   <en>The state below describes only this local IIS Express start request; it is not shared IIS, production-site, or external configuration state.</en>
# </lang>
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
if (-not $SitePath) {
    $SitePath = Join-Path $repoRoot 'src\Portal'
}
$SitePath = (Resolve-Path -LiteralPath $SitePath).Path
$VirtualPath = $VirtualPath.Trim()
if (-not $VirtualPath) {
    $VirtualPath = '/'
}
if (-not $VirtualPath.StartsWith('/')) {
    $VirtualPath = '/' + $VirtualPath
}
if ($VirtualPath.Length -gt 1) {
    $VirtualPath = $VirtualPath.TrimEnd('/')
}
$HostName = $HostName.Trim()
if (-not $HostName) {
    $HostName = 'localhost'
}
$useGeneratedConfig = $VirtualPath -ne '/' -or
    -not [string]::Equals($HostName, 'localhost', [System.StringComparison]::OrdinalIgnoreCase)

$iisCandidates = @(
    "${env:ProgramFiles(x86)}\IIS Express\iisexpress.exe",
    "${env:ProgramFiles}\IIS Express\iisexpress.exe"
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

$iisExpress = $iisCandidates | Select-Object -First 1
if (-not $iisExpress) {
    throw 'IIS Express was not found. Install IIS Express or Visual Studio web tooling.'
}

$configPath = $null
if ($useGeneratedConfig) {
    $configDir = Join-Path $repoRoot 'temp\iisexpress'
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null
    $configPath = Join-Path $configDir "applicationhost-$Port.config"
}
$escapedConfigPath = if ($configPath) { [regex]::Escape($configPath) } else { $null }

# <lang>
#   <zh-CN>端口或本次虚拟目录配置是唯一进程边界，不能以同一物理站点路径误匹配其他调试实例。</zh-CN>
#   <en>The port or this virtual-directory configuration is the only process boundary; do not match another debug instance merely because it uses the same physical site path.</en>
# </lang>
$existing = Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -match "/port:$Port(\s|$)" -or
        ($escapedConfigPath -and $_.CommandLine -match $escapedConfigPath)
    } |
    Select-Object -First 1

if ($existing) {
    Write-Host "IIS Express is already running for port $Port or site path $SitePath. PID: $($existing.ProcessId)"
    if ($useGeneratedConfig -and $existing.CommandLine -match "/port:$Port(\s|$)") {
        Write-Host "The current instance uses simple localhost mode. Stop it before starting HostName '$HostName'."
    }

    exit 0
}

$listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listening) {
    throw "Port $Port is already in use by PID $($listening.OwningProcess)."
}

# <lang>
#   <zh-CN>根路径和 localhost 使用 IIS Express 简化参数；其它虚拟路径或主机名转入隔离 XML 配置，避免修改用户级 IIS 全局文件。</zh-CN>
#   <en>Root-path localhost uses the simple IIS Express arguments; other virtual paths or host names use an isolated XML config instead of changing the user-level global IIS file.</en>
# </lang>
if (-not $useGeneratedConfig) {
    $arguments = @(
        "/path:`"$SitePath`"",
        "/port:$Port"
    ) -join ' '
}
else {
    $siteName = "HIA-ASPNETPortal-$Port"
    $siteId = [string](400000 + $Port)
    $rootSitePath = Join-Path $configDir "root-$Port"
    New-Item -ItemType Directory -Force -Path $rootSitePath | Out-Null
    $baseConfigPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'IISExpress\config\applicationhost.config'
    if (-not (Test-Path -LiteralPath $baseConfigPath)) {
        throw "IIS Express base applicationhost.config was not found: $baseConfigPath"
    }

    [xml]$config = Get-Content -Encoding UTF8 -Raw -Path $baseConfigPath
    $sites = $config.configuration.'system.applicationHost'.sites
    foreach ($existingSite in @($sites.site | Where-Object { $_.name -eq $siteName -or $_.id -eq $siteId })) {
        [void]$sites.RemoveChild($existingSite)
    }

# <lang>
#   <zh-CN>创建最小 application/virtualDirectory 节点，保持生成配置只映射本次站点物理路径。</zh-CN>
#   <en>Creates the minimal application/virtualDirectory nodes so the generated config maps only this site's physical paths.</en>
# </lang>
    function New-IISExpressApplicationElement([xml]$Document, [string]$Path, [string]$PhysicalPath) {
        $application = $Document.CreateElement('application')
        $application.SetAttribute('path', $Path)
        $application.SetAttribute('applicationPool', 'Clr4IntegratedAppPool')

        $virtualDirectory = $Document.CreateElement('virtualDirectory')
        $virtualDirectory.SetAttribute('path', '/')
        $virtualDirectory.SetAttribute('physicalPath', $PhysicalPath)
        [void]$application.AppendChild($virtualDirectory)

        return $application
    }

    $site = $config.CreateElement('site')
    $site.SetAttribute('name', $siteName)
    $site.SetAttribute('id', $siteId)
    if ($VirtualPath -eq '/') {
        [void]$site.AppendChild((New-IISExpressApplicationElement -Document $config -Path '/' -PhysicalPath $SitePath))
    }
    else {
        [void]$site.AppendChild((New-IISExpressApplicationElement -Document $config -Path '/' -PhysicalPath $rootSitePath))
        [void]$site.AppendChild((New-IISExpressApplicationElement -Document $config -Path $VirtualPath -PhysicalPath $SitePath))
    }

    $bindings = $config.CreateElement('bindings')
    $bindingHosts = New-Object 'System.Collections.Generic.List[string]'
    [void]$bindingHosts.Add('localhost')
    if (-not [string]::Equals($HostName, 'localhost', [System.StringComparison]::OrdinalIgnoreCase)) {
        [void]$bindingHosts.Add($HostName)
    }

    foreach ($bindingHost in ($bindingHosts | Select-Object -Unique)) {
        $binding = $config.CreateElement('binding')
        $binding.SetAttribute('protocol', 'http')
        $binding.SetAttribute('bindingInformation', "*:$($Port):$bindingHost")
        [void]$bindings.AppendChild($binding)
    }

    [void]$site.AppendChild($bindings)
    [void]$sites.AppendChild($site)

    $xmlSettings = [System.Xml.XmlWriterSettings]::new()
    $xmlSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $xmlSettings.Indent = $true
    $xmlWriter = [System.Xml.XmlWriter]::Create($configPath, $xmlSettings)
    try {
        $config.Save($xmlWriter)
    }
    finally {
        $xmlWriter.Close()
    }

    $arguments = @(
        "/config:`"$configPath`"",
        "/site:`"$siteName`""
    ) -join ' '
}

# <lang>
#   <zh-CN>启动隐藏的 IIS Express 子进程后按相同端口/配置边界确认归属；未发现目标进程即失败。</zh-CN>
#   <en>Starts the hidden IIS Express child process and verifies ownership using the same port/config boundary; absence of a matching process fails.</en>
# </lang>
Write-Host "Starting IIS Express: $iisExpress $arguments"
Start-Process -FilePath $iisExpress -ArgumentList $arguments -WorkingDirectory $SitePath -WindowStyle Hidden

Start-Sleep -Seconds 2

$started = Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -match "/port:$Port(\s|$)" -or
        ($escapedConfigPath -and $_.CommandLine -match $escapedConfigPath)
    } |
    Select-Object -First 1

if (-not $started) {
    throw 'IIS Express did not appear to start. Check the IIS Express logs for details.'
}

# <lang>
#   <zh-CN>非 localhost 主机名只输出需要人工配置的 URL ACL/防火墙提示，不自动修改主机策略。</zh-CN>
#   <en>For non-localhost host names, emits manual URL ACL/firewall guidance only and never changes host policies automatically.</en>
# </lang>
$displayPath = if ($VirtualPath -eq '/') { '/' } else { "$VirtualPath/" }
Write-Host "IIS Express started. PID: $($started.ProcessId); URL: http://localhost:$Port$displayPath"
if (-not [string]::Equals($HostName, 'localhost', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "External URL: http://$HostName`:$Port$displayPath"
    Write-Host "If the VM still cannot access the site, run these once from an elevated PowerShell/cmd on the host:"
    Write-Host "  netsh http add urlacl url=http://$HostName`:$Port/ user=Everyone"
    Write-Host "  netsh advfirewall firewall add rule name=`"HIA-ASPNETPortal $Port`" dir=in action=allow protocol=TCP localport=$Port"
}
