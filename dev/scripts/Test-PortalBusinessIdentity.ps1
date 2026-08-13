<#
.SYNOPSIS
    Checks the P12.2 business-identity contract without changing the database.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本只做静态门禁，确认员工号登录标识、用户资料字段、员工主数据和账号员工绑定的关键契约仍存在。</zh-CN>
      <en>This script performs static checks only, ensuring the employee-code sign-in identifier, user-profile fields, employee master data, and user/employee binding contracts remain present.</en>
    </lang>
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>追加一条低敏静态检查结果；状态和证据路径供汇总/JSON 使用，不代表真实数据库或登录已通过。</zh-CN>
#   <en>Add one low-sensitivity static-check result; status and evidence paths feed the summary/JSON and do not prove a real database or sign-in passed.</en>
# </lang>
function Add-BusinessIdentityCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject]@{
            Code     = $Code
            Status   = $Status
            Message  = $Message
            Evidence = $Evidence
        })
}

# <lang>
#   <zh-CN>按仓库相对路径读取必需文本；缺失立即失败，函数不打开数据库或外部配置。</zh-CN>
#   <en>Read required text by repository-relative path; fail immediately when missing, without opening a database or external configuration.</en>
# </lang>
function Get-PortalText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

# <lang>
#   <zh-CN>确认文本包含全部固定锚点；这是序数静态断言，不解析或执行 C#/SQL。</zh-CN>
#   <en>Verify that text contains all fixed anchors; this is an ordinal static assertion and does not parse or execute C#/SQL.</en>
# </lang>
function Test-ContainsAll {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string[]]$Needles
    )

    foreach ($needle in $Needles) {
        if ($Text.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
            return $false
        }
    }

    return $true
}

# <lang>
#   <zh-CN>确认固定锚点按既定顺序出现；同名锚点不构成运行时流程证明。</zh-CN>
#   <en>Verify that fixed anchors appear in the intended order; matching anchors is not proof of a runtime flow.</en>
# </lang>
function Test-InOrder {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string[]]$Needles
    )

    $lastIndex = -1
    foreach ($needle in $Needles) {
        $index = $Text.IndexOf($needle, [StringComparison]::Ordinal)
        if ($index -lt 0 -or $index -le $lastIndex) {
            return $false
        }

        $lastIndex = $index
    }

    return $true
}

# <lang>
#   <zh-CN>截取两个锚点之间的文本以限定局部静态检查范围；找不到起点返回空文本，找不到终点则取到末尾。</zh-CN>
#   <en>Slice text between two anchors to constrain a local static check; return empty when the start is missing and continue to the end when the end is missing.</en>
# </lang>
function Get-PortalTextSlice {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$StartNeedle,
        [Parameter(Mandatory = $true)][string]$EndNeedle
    )

    $startIndex = $Text.IndexOf($StartNeedle, [StringComparison]::Ordinal)
    if ($startIndex -lt 0) {
        return ''
    }

    $endIndex = $Text.IndexOf($EndNeedle, $startIndex + $StartNeedle.Length, [StringComparison]::Ordinal)
    if ($endIndex -lt 0) {
        return $Text.Substring($startIndex)
    }

    return $Text.Substring($startIndex, $endIndex - $startIndex)
}

$resolver = Get-PortalText 'src/Portal.Components.Data1/PortalLoginIdentifierResolver.cs'
$usersDb = Get-PortalText 'src/Portal.Components.Data1/UsersDb.cs'
$usersContract = Get-PortalText 'src/Portal.Components/IUsersDb.cs'
$signinCode = Get-PortalText 'src/Portal/DesktopModules/Signin.ascx.cs'
$signinMarkup = Get-PortalText 'src/Portal/DesktopModules/Signin.ascx'
$langNeutral = Get-PortalText 'src/Portal/App_GlobalResources/lang.resx'
$langZh = Get-PortalText 'src/Portal/App_GlobalResources/lang.zh-cn.resx'
$langEn = Get-PortalText 'src/Portal/App_GlobalResources/lang.en-us.resx'
$userProfilesSql = Get-PortalText 'src/Setup/PortalBiz_UserProfiles.sql'
$employeesSql = Get-PortalText 'src/Setup/PortalBiz_Employees.sql'
$bindingsSql = Get-PortalText 'src/Setup/PortalBiz_UserEmployeeBindings.sql'
$employeeCodeResolver = Get-PortalTextSlice `
    -Text $resolver `
    -StartNeedle 'private PortalLoginIdentifierResolution ResolveEmployeeCode' `
    -EndNeedle 'private PortalLoginIdentifierResolution ResolveEmail'

$resolverOrderOk = Test-InOrder $resolver @(
    '[dbo].[PortalBiz_UserProfiles] WHERE [LoginName] = @p0',
    '[dbo].[Portal_Users] WHERE [Name] = @p0',
    'ResolveEmployeeCode(normalizedInput)',
    'return ResolveEmail(normalizedInput);'
)
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-RESOLUTION-ORDER' `
    -Status $(if ($resolverOrderOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Login identifier resolution keeps the intended order: profile login name, legacy user name, active employee code, then email.' `
    -Evidence 'src/Portal.Components.Data1/PortalLoginIdentifierResolver.cs'

$employeeCodeBoundaryOk = (Test-ContainsAll $employeeCodeResolver @(
    '[Binding].[BindingStatus] = N''Active''',
    '[Employee].[EmploymentStatus] = N''Active''',
    '[Employee].[EmployeeCode] = @p0'
)) -and ($employeeCodeResolver.IndexOf('Password', [StringComparison]::OrdinalIgnoreCase) -lt 0)
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-EMPLOYEE-CODE-BOUNDARY' `
    -Status $(if ($employeeCodeBoundaryOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Employee-code sign-in is identity resolution only and requires active employee plus active binding.' `
    -Evidence 'ResolveEmployeeCode'

$signinUsesResolverOk = Test-ContainsAll $signinCode @(
    'var loginIdentifier = EmailOrName.Text.Trim();',
    'UsersDB.SignIn(loginIdentifier, submittedPassword)'
)
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-SIGNIN-ENTRY' `
    -Status $(if ($signinUsesResolverOk) { 'Pass' } else { 'Fail' }) `
    -Message 'SignIn module passes the raw login identifier to IUsersDb.SignIn instead of pre-classifying it in the page.' `
    -Evidence 'src/Portal/DesktopModules/Signin.ascx.cs'

$labelOk = (Test-ContainsAll $signinMarkup @('lang.Signin_EmailOrName')) -and
    (Test-ContainsAll $langNeutral @('邮箱、用户名或员工号')) -and
    (Test-ContainsAll $langZh @('邮箱、用户名或员工号')) -and
    (Test-ContainsAll $langEn @('Email, username, or employee code:'))
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-SIGNIN-LABEL' `
    -Status $(if ($labelOk) { 'Pass' } else { 'Fail' }) `
    -Message 'SignIn label advertises email, username, and employee-code sign-in identifiers.' `
    -Evidence 'App_GlobalResources/lang*.resx'

$contractDocsOk = (Test-ContainsAll $usersContract @('邮箱、登录名称或员工号', 'Email, sign-in name, or employee code')) -and
    (Test-ContainsAll $usersDb @('邮箱、登录名称或员工号', 'Email, sign-in name, or employee code'))
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-CONTRACT-DOCS' `
    -Status $(if ($contractDocsOk) { 'Pass' } else { 'Fail' }) `
    -Message 'IUsersDb and UsersDb documentation describe employee code as a supported sign-in identifier.' `
    -Evidence 'IUsersDb.cs; UsersDb.cs'

$profileSchemaOk = Test-ContainsAll $userProfilesSql @(
    '[LoginName] NVARCHAR(100) NOT NULL',
    '[DisplayName] NVARCHAR(150) NULL',
    '[Nickname] NVARCHAR(100) NULL',
    '[PreferredEmail] NVARCHAR(256) NULL',
    '[Status] NVARCHAR(40) NOT NULL',
    'CONSTRAINT [UQ_PortalBiz_UserProfiles_LoginName]',
    'CREATE UNIQUE INDEX [UX_PortalBiz_UserProfiles_PreferredEmail]'
)
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-USER-PROFILE-SCHEMA' `
    -Status $(if ($profileSchemaOk) { 'Pass' } else { 'Fail' }) `
    -Message 'User profile schema preserves login name, display name, nickname, preferred email, and lifecycle status.' `
    -Evidence 'src/Setup/PortalBiz_UserProfiles.sql'

$employeeSchemaOk = Test-ContainsAll $employeesSql @(
    '[EmployeeCode] NVARCHAR(64) NOT NULL',
    '[DisplayName] NVARCHAR(150) NOT NULL',
    '[PreferredName] NVARCHAR(100) NULL',
    '[WorkEmail] NVARCHAR(256) NULL',
    '[OrganizationUnitId] INT NULL',
    '[EmploymentStatus] NVARCHAR(40) NOT NULL',
    'CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]'
)
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-EMPLOYEE-SCHEMA' `
    -Status $(if ($employeeSchemaOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Employee schema preserves code, display/preferred names, work email, organization, and employment status.' `
    -Evidence 'src/Setup/PortalBiz_Employees.sql'

$bindingSchemaOk = Test-ContainsAll $bindingsSql @(
    '[UserId] INT NOT NULL',
    '[EmployeeId] INT NOT NULL',
    '[BindingStatus] NVARCHAR(40) NOT NULL',
    'CREATE UNIQUE INDEX [UX_PortalBiz_UserEmployeeBindings_ActiveUser]',
    'CREATE UNIQUE INDEX [UX_PortalBiz_UserEmployeeBindings_ActiveEmployee]',
    'WHERE [BindingStatus] = N''Active'''
)
Add-BusinessIdentityCheck `
    -Code 'P12-BIZID-BINDING-SCHEMA' `
    -Status $(if ($bindingSchemaOk) { 'Pass' } else { 'Fail' }) `
    -Message 'User/employee binding schema keeps one active binding per user and one active binding per employee.' `
    -Evidence 'src/Setup/PortalBiz_UserEmployeeBindings.sql'

# <lang>
#   <zh-CN>汇总静态检查状态并生成结果对象；计数不代表真实环境 proof。</zh-CN>
#   <en>Summarize static-check states into a result object; counts do not represent real-environment proof.</en>
# </lang>
$summary = [pscustomobject]@{
    Pass    = @($checks | Where-Object { $_.Status -eq 'Pass' }).Count
    Warning = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count
    Fail    = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
    Info    = @($checks | Where-Object { $_.Status -eq 'Info' }).Count
}

$result = [pscustomobject]@{
    GeneratedAtUtc = [DateTime]::UtcNow.ToString('o')
    RepoRoot       = $RepoRoot
    Summary        = $summary
    Checks         = $checks
}

# <lang>
#   <zh-CN>仅在显式请求时写入 UTF-8 无 BOM JSON；输出内容只包含低敏检查结果。</zh-CN>
#   <en>Write UTF-8-no-BOM JSON only when explicitly requested; output contains low-sensitivity check results only.</en>
# </lang>
if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $outputPath = if ([System.IO.Path]::IsPathRooted($OutputJson)) { $OutputJson } else { Join-Path $RepoRoot $OutputJson }
    $outputDir = Split-Path -Parent $outputPath
    if (-not [string]::IsNullOrWhiteSpace($outputDir)) {
        New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    }

    [System.IO.File]::WriteAllText(
        $outputPath,
        ($result | ConvertTo-Json -Depth 6),
        [System.Text.UTF8Encoding]::new($false))
}

$checks | Format-Table -AutoSize Status, Code, Message
Write-Output ("Summary: Pass={0}; Warning={1}; Fail={2}; Info={3}" -f $summary.Pass, $summary.Warning, $summary.Fail, $summary.Info)

# <lang>
#   <zh-CN>任一 Fail 使静态门禁以非零退出；未运行数据库、登录或页面操作。</zh-CN>
#   <en>Any Fail produces a non-zero static-gate exit; no database, sign-in, or page operation is run.</en>
# </lang>
if ($summary.Fail -gt 0) {
    exit 1
}
