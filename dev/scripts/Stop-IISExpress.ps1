[CmdletBinding()]
param(
    [int]$Port = 40001,

    [string]$SitePath,

    [string]$VirtualPath = '/'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
if (-not $SitePath) {
    $SitePath = Join-Path $repoRoot 'src\Portal'
}
$SitePath = (Resolve-Path -LiteralPath $SitePath).Path
$escapedSitePath = [regex]::Escape($SitePath)
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

$configPath = Join-Path $repoRoot "temp\iisexpress\applicationhost-$Port.config"
$escapedConfigPath = [regex]::Escape($configPath)

$targets = Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -match "/port:$Port(\s|$)" -or
        ($VirtualPath -eq '/' -and $_.CommandLine -match $escapedSitePath) -or
        $_.CommandLine -match $escapedConfigPath
    }

if (-not $targets) {
    Write-Host "No matching IIS Express process found for port $Port or site path $SitePath."
    exit 0
}

foreach ($processInfo in $targets) {
    Write-Host "Stopping IIS Express PID $($processInfo.ProcessId)"
    Stop-Process -Id $processInfo.ProcessId -Force
}
