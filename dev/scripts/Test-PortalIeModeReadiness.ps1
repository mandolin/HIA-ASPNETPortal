[CmdletBinding()]
param(
    [switch]$FailWhenNotReady
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 中文：本脚本只读本机 Edge IE mode 自动化准备状态，不安装驱动、不修改注册表或企业策略。
# English: This script only reads local Edge IE mode automation readiness. It does not install drivers or modify registry/policies.
$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-PortalReadinessCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [bool]$Passed,

        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $checks.Add([pscustomobject][ordered]@{
            Name   = $Name
            Passed = $Passed
            Detail = $Detail
        })
}

function Get-FirstExistingFile {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Candidates
    )

    foreach ($candidate in $Candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return Get-Item -LiteralPath $candidate
        }
    }

    return $null
}

function Get-CommandDetail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        return $null
    }

    $versionText = ''
    try {
        $versionText = (& $command.Source --version) -join ' '
    }
    catch {
        $versionText = 'Version check failed: ' + $_.Exception.Message
    }

    return [pscustomobject][ordered]@{
        Path    = $command.Source
        Version = $versionText
    }
}

function Get-EdgePolicyValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ValueName
    )

    $policyRoots = @(
        'HKLM:\SOFTWARE\Policies\Microsoft\Edge',
        'HKCU:\SOFTWARE\Policies\Microsoft\Edge'
    )

    foreach ($policyRoot in $policyRoots) {
        if (-not (Test-Path -LiteralPath $policyRoot)) {
            continue
        }

        $properties = Get-ItemProperty -LiteralPath $policyRoot
        if ($properties.PSObject.Properties.Name -contains $ValueName) {
            return [pscustomobject][ordered]@{
                Root  = $policyRoot
                Name  = $ValueName
                Value = $properties.$ValueName
            }
        }
    }

    return $null
}

$edgeFile = Get-FirstExistingFile -Candidates @(
    'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Microsoft\Edge\Application\msedge.exe'
)

if ($null -eq $edgeFile) {
    Add-PortalReadinessCheck -Name 'Microsoft Edge' -Passed $false -Detail '未发现 msedge.exe。'
}
else {
    Add-PortalReadinessCheck -Name 'Microsoft Edge' -Passed $true -Detail ('{0}; version {1}' -f $edgeFile.FullName, $edgeFile.VersionInfo.ProductVersion)
}

$edgeDriver = Get-CommandDetail -Name 'msedgedriver.exe'
if ($null -eq $edgeDriver) {
    Add-PortalReadinessCheck -Name 'msedgedriver PATH' -Passed $false -Detail '未在 PATH 中发现 msedgedriver.exe；Chromium Edge 自动化需要单独准备。'
}
else {
    Add-PortalReadinessCheck -Name 'msedgedriver PATH' -Passed $true -Detail ('{0}; {1}' -f $edgeDriver.Path, $edgeDriver.Version)
}

$ieDriver = Get-CommandDetail -Name 'IEDriverServer.exe'
if ($null -eq $ieDriver) {
    Add-PortalReadinessCheck -Name 'IEDriverServer PATH' -Passed $false -Detail '未在 PATH 中发现 IEDriverServer.exe；Edge IE mode 自动化需要 Internet Explorer Driver。'
}
else {
    Add-PortalReadinessCheck -Name 'IEDriverServer PATH' -Passed $true -Detail ('{0}; {1}' -f $ieDriver.Path, $ieDriver.Version)
}

$integrationLevel = Get-EdgePolicyValue -ValueName 'InternetExplorerIntegrationLevel'
$siteList = Get-EdgePolicyValue -ValueName 'InternetExplorerIntegrationSiteList'

if ($null -eq $integrationLevel) {
    Add-PortalReadinessCheck -Name 'Edge IE mode policy' -Passed $false -Detail '未发现 InternetExplorerIntegrationLevel 策略；本机可能尚未启用企业 IE mode。'
}
else {
    Add-PortalReadinessCheck -Name 'Edge IE mode policy' -Passed $true -Detail ('{0} = {1}' -f $integrationLevel.Root, $integrationLevel.Value)
}

if ($null -eq $siteList) {
    Add-PortalReadinessCheck -Name 'Enterprise Mode Site List' -Passed $false -Detail '未发现 InternetExplorerIntegrationSiteList 策略；后续需提供 P9 本地站点清单。'
}
else {
    Add-PortalReadinessCheck -Name 'Enterprise Mode Site List' -Passed $true -Detail ('{0} = {1}' -f $siteList.Root, $siteList.Value)
}

$readyForIeModeAutomation = $null -ne $edgeFile -and $null -ne $ieDriver
$readyForEnterpriseSiteList = $null -ne $integrationLevel -and $null -ne $siteList

$checks | Format-Table -AutoSize

[pscustomobject][ordered]@{
    ReadyForIeModeAutomation = $readyForIeModeAutomation
    ReadyForEnterpriseSiteList = $readyForEnterpriseSiteList
    EdgeFound = $null -ne $edgeFile
    MsEdgeDriverFound = $null -ne $edgeDriver
    IeDriverFound = $null -ne $ieDriver
    IeModePolicyConfigured = $null -ne $integrationLevel
    EnterpriseSiteListConfigured = $null -ne $siteList
}

if ($FailWhenNotReady -and -not $readyForIeModeAutomation) {
    throw 'Portal Edge IE mode readiness check failed.'
}
