[CmdletBinding()]
param(
    [int]$Port = 40001,

    [string]$SitePath,

    [string]$VirtualPath = '/',

    [string]$HostName = 'localhost'
)

$ErrorActionPreference = 'Stop'

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

# 端口或本次虚拟目录配置是唯一进程边界，不能以同一物理站点路径误匹配其他调试实例。
# The port or this virtual-directory configuration is the only process boundary; do not match another debug
# instance merely because it uses the same physical site path.
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

$displayPath = if ($VirtualPath -eq '/') { '/' } else { "$VirtualPath/" }
Write-Host "IIS Express started. PID: $($started.ProcessId); URL: http://localhost:$Port$displayPath"
if (-not [string]::Equals($HostName, 'localhost', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "External URL: http://$HostName`:$Port$displayPath"
    Write-Host "If the VM still cannot access the site, run these once from an elevated PowerShell/cmd on the host:"
    Write-Host "  netsh http add urlacl url=http://$HostName`:$Port/ user=Everyone"
    Write-Host "  netsh advfirewall firewall add rule name=`"HIA-ASPNETPortal $Port`" dir=in action=allow protocol=TCP localport=$Port"
}
