<#
.SYNOPSIS
.LANG en
Checks local Edge IE mode automation readiness.

.LANG zh-CN
检查本机 Edge IE mode 自动化准备状态。

.DESCRIPTION
<lang>
  <en>Inspects Microsoft Edge, msedgedriver, IEDriverServer, and Edge IE mode policy markers required by the legacy-browser automation strategy. The script is read-only and does not install drivers, change registry policy, or start a browser session.</en>
  <zh-CN>检查旧浏览器自动化策略所需的 Microsoft Edge、msedgedriver、IEDriverServer 和 Edge IE mode 策略标记。本脚本为只读检查，不安装驱动、不修改注册表策略，也不启动浏览器会话。</zh-CN>
</lang>

.PARAMETER FailWhenNotReady
.LANG en
Returns a failing exit code when one or more readiness checks fail.

.LANG zh-CN
当一个或多个准备项失败时返回失败退出码。
#>
[CmdletBinding()]
param(
    [switch]$FailWhenNotReady
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>本脚本只读本机 Edge IE mode 自动化准备状态，不安装驱动、不修改注册表或企业策略。</zh-CN>
#   <en>This script only reads local Edge IE mode automation readiness. It does not install drivers or modify registry/policies.</en>
# </lang>
$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>此 helper 将每项本机准备事实归一为固定字段，供表格和最终摘要同时消费，不改变检测结果。</zh-CN>
#   <en>This helper normalizes each local readiness fact into fixed fields for both the table and final summary without changing the detection result.</en>
# </lang>
function Add-PortalReadinessCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [bool]$Passed,

        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

# <lang>
#   <zh-CN>保留名称、通过状态和低敏详情，避免在后续输出阶段重新读取本机状态。</zh-CN>
#   <en>The name, pass state, and low-sensitivity detail are captured once so later output does not re-read local state.</en>
# </lang>
    $checks.Add([pscustomobject][ordered]@{
            Name   = $Name
            Passed = $Passed
            Detail = $Detail
        })
}

# <lang>
#   <zh-CN>按候选顺序返回第一个实际存在的文件；候选仅用于只读发现，不触发安装或修复。</zh-CN>
#   <en>Returns the first existing file in candidate order; candidates are discovery-only and never trigger installation or repair.</en>
# </lang>
function Get-FirstExistingFile {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Candidates
    )

# <lang>
#   <zh-CN>保持候选顺序，以稳定地优先使用标准 Edge 安装位置。</zh-CN>
#   <en>Candidate order is preserved so standard Edge installation locations are preferred deterministically.</en>
# </lang>
    foreach ($candidate in $Candidates) {
# <lang>
#   <zh-CN>只有叶文件存在才作为可用浏览器事实返回，目录或缺失路径不满足准备条件。</zh-CN>
#   <en>Only an existing leaf file is returned as an available browser fact; directories and missing paths do not satisfy readiness.</en>
# </lang>
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return Get-Item -LiteralPath $candidate
        }
    }

# <lang>
#   <zh-CN>没有候选命中时使用空值，让调用方生成可审计的失败检查而不是抛出路径异常。</zh-CN>
#   <en>When no candidate matches, null lets the caller emit an auditable failed check instead of a path exception.</en>
# </lang>
    return $null
}

# <lang>
#   <zh-CN>读取命令来源和版本信息，仅用于显示驱动可见性；版本探测失败仍保留路径事实。</zh-CN>
#   <en>Reads command source and version only to report driver visibility; a failed version probe still preserves the path fact.</en>
# </lang>
function Get-CommandDetail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

# <lang>
#   <zh-CN>命令查找采用静默缺失语义，把未安装或不在 PATH 转换为后续检查结果。</zh-CN>
#   <en>Command lookup treats absence silently so an uninstalled or non-PATH tool becomes a later check result.</en>
# </lang>
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        return $null
    }

# <lang>
#   <zh-CN>先准备可回退的版本文本；外部 --version 失败不得掩盖驱动路径已被发现的事实。</zh-CN>
#   <en>A fallback version text is prepared first; an external --version failure must not hide an already discovered driver path.</en>
# </lang>
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

# <lang>
#   <zh-CN>在两个策略根中只读查找指定 Edge policy value，返回来源根以便审计用户级/计算机级差异。</zh-CN>
#   <en>Reads the named Edge policy value from both policy roots and returns its source root so user-versus-machine scope remains auditable.</en>
# </lang>
function Get-EdgePolicyValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ValueName
    )

# <lang>
#   <zh-CN>策略根限定为 Edge 官方策略路径，不扩展到任意注册表位置。</zh-CN>
#   <en>Policy roots are limited to the Edge policy paths and are not expanded to arbitrary registry locations.</en>
# </lang>
    $policyRoots = @(
        'HKLM:\SOFTWARE\Policies\Microsoft\Edge',
        'HKCU:\SOFTWARE\Policies\Microsoft\Edge'
    )

# <lang>
#   <zh-CN>逐根读取并在不存在时继续，避免单个未配置范围阻断另一个范围的只读检查。</zh-CN>
#   <en>Each root is read independently and a missing root is skipped so one unconfigured scope cannot block the other read-only check.</en>
# </lang>
    foreach ($policyRoot in $policyRoots) {
        if (-not (Test-Path -LiteralPath $policyRoot)) {
            continue
        }

# <lang>
#   <zh-CN>只取得当前策略根属性，不写入注册表、不展开未知子键。</zh-CN>
#   <en>Only properties from the current policy root are read; no registry write or unknown subkey traversal occurs.</en>
# </lang>
        $properties = Get-ItemProperty -LiteralPath $policyRoot
        if ($properties.PSObject.Properties.Name -contains $ValueName) {
# <lang>
#   <zh-CN>返回策略根、名称和值三元组，保留策略来源并避免把原始注册表对象泄露到输出。</zh-CN>
#   <en>Returns a root/name/value tuple that preserves policy provenance without exposing the raw registry object.</en>
# </lang>
            return [pscustomobject][ordered]@{
                Root  = $policyRoot
                Name  = $ValueName
                Value = $properties.$ValueName
            }
        }
    }

# <lang>
#   <zh-CN>两个策略根都没有目标值时使用空值，让上层分别报告缺少策略的原因。</zh-CN>
#   <en>Null indicates that neither policy root contains the value, allowing the caller to report the missing policy reason.</en>
# </lang>
    return $null
}

# <lang>
#   <zh-CN>浏览器候选只读本机标准安装位置，未命中时保持准备事实，不尝试发现其它磁盘。</zh-CN>
#   <en>Browser discovery reads only standard local installation locations; a miss remains a readiness fact and does not scan other disks.</en>
# </lang>
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
# <lang>
#   <zh-CN>驱动检查只观察 PATH 可见性，并把缺失转换为低敏失败详情。</zh-CN>
#   <en>The driver check observes PATH visibility only and turns absence into a low-sensitivity failed detail.</en>
# </lang>
if ($null -eq $edgeDriver) {
    Add-PortalReadinessCheck -Name 'msedgedriver PATH' -Passed $false -Detail '未在 PATH 中发现 msedgedriver.exe；Chromium Edge 自动化需要单独准备。'
}
else {
    Add-PortalReadinessCheck -Name 'msedgedriver PATH' -Passed $true -Detail ('{0}; {1}' -f $edgeDriver.Path, $edgeDriver.Version)
}

$ieDriver = Get-CommandDetail -Name 'IEDriverServer.exe'
# <lang>
#   <zh-CN>IE Driver 与 Chromium driver 分开计入，避免任一自动化路线的事实覆盖另一条路线。</zh-CN>
#   <en>IE Driver and Chromium driver are tracked separately so one automation route cannot mask the facts of the other.</en>
# </lang>
if ($null -eq $ieDriver) {
    Add-PortalReadinessCheck -Name 'IEDriverServer PATH' -Passed $false -Detail '未在 PATH 中发现 IEDriverServer.exe；Edge IE mode 自动化需要 Internet Explorer Driver。'
}
else {
    Add-PortalReadinessCheck -Name 'IEDriverServer PATH' -Passed $true -Detail ('{0}; {1}' -f $ieDriver.Path, $ieDriver.Version)
}

$integrationLevel = Get-EdgePolicyValue -ValueName 'InternetExplorerIntegrationLevel'
$siteList = Get-EdgePolicyValue -ValueName 'InternetExplorerIntegrationSiteList'
# <lang>
#   <zh-CN>两个 policy value 独立评估：IE mode 集成级别和站点清单缺一不可。</zh-CN>
#   <en>The two policy values are evaluated independently because IE mode integration level and site list are both required.</en>
# </lang>

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
# <lang>
#   <zh-CN>准备状态只由已发现的浏览器/驱动/策略事实组合而成，不把外部浏览器启动结果臆造为已验证。</zh-CN>
#   <en>Readiness is composed only from discovered browser, driver, and policy facts; no browser launch result is inferred.</en>
# </lang>

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

# <lang>
#   <zh-CN>FailWhenNotReady 仅在明确请求时转为异常退出，默认仍输出完整只读摘要供人工判断。</zh-CN>
#   <en>FailWhenNotReady converts the result to a failing exit only when requested; by default the complete read-only summary remains available for review.</en>
# </lang>
if ($FailWhenNotReady -and -not $readyForIeModeAutomation) {
    throw 'Portal Edge IE mode readiness check failed.'
}
