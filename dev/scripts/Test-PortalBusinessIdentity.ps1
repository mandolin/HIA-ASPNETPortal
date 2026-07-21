<#
.SYNOPSIS
    Checks the P12.2 business-identity contract without changing the database.

.DESCRIPTION
    中文：本脚本只做静态门禁，确认员工号登录标识、用户资料字段、员工主数据和账号员工绑定的关键契约仍存在。
    English: This script performs static checks only, ensuring the employee-code sign-in identifier, user-profile
    fields, employee master data, and user/employee binding contracts remain present.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

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

function Get-PortalText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

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

if ($summary.Fail -gt 0) {
    exit 1
}
