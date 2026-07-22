<#
.SYNOPSIS
    Performs a read-only P14.4 production-hardening preflight.

.DESCRIPTION
    中文：本脚本用于 P14.4 生产前硬化复核，读取源码或发布产物中的 Web.config、
    发布转换线索、目录存在性/ACL 摘要和 P14.2 release manifest 警告，并输出 JSON/Markdown 证据。
    它不会修改 IIS、Web.config、目录 ACL、外置配置或数据库；不会读取或输出真实连接串、密码、
    Token、Cookie 或证书私钥。源码基线中的 HTTPS、HSTS、machineKey 等目标环境事项只记录为
    PendingTargetEnvironment，不伪装为生产通过。
    English: This script performs the P14.4 production-hardening preflight against source or a
    filesystem-published package. It reads Web.config, transform hints, directory/ACL summaries,
    and P14.2 release-manifest warnings, then writes JSON/Markdown evidence. It does not modify
    IIS, Web.config, directory ACLs, external configuration, or databases, and it never reads or
    prints real connection strings, passwords, tokens, cookies, or certificate private keys.
#>
[CmdletBinding()]
param(
    [string]$PortalPath,

    [string]$PublishedPath,

    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Scan',

    [string]$OutputJson,

    [string]$OutputMarkdown,

    [string]$LogDirectoryPath,

    [string]$UploadDirectoryPath,

    [string]$ExternalConfigRoot,

    [string]$ReleaseManifestRoot,

    [switch]$TreatWarningsAsErrors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($PortalPath)) {
    $PortalPath = Join-Path $repoRoot 'src/Portal'
}

if ([string]::IsNullOrWhiteSpace($ReleaseManifestRoot)) {
    $ReleaseManifestRoot = Join-Path $repoRoot 'work-zone/dev/evidence/p14.2'
}

$isPublishedPackage = -not [string]::IsNullOrWhiteSpace($PublishedPath)
$targetRoot = if ($isPublishedPackage) { [System.IO.Path]::GetFullPath($PublishedPath) } else { [System.IO.Path]::GetFullPath($PortalPath) }
$webConfigPath = Join-Path $targetRoot 'Web.config'
$checks = New-Object 'System.Collections.Generic.List[object]'

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-RepoPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

function Get-Utf8Text {
    param([string]$LiteralPath)

    return Get-Content -LiteralPath $LiteralPath -Encoding UTF8 -Raw
}

function Get-XmlAttributeValue {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$Name
    )

    if ($null -eq $Node -or $null -eq $Node.Attributes[$Name]) {
        return ''
    }

    return [string]$Node.Attributes[$Name].Value
}

function Test-StrictProfile {
    return $Profile -in @('Test', 'Prod', 'Scan')
}

function Add-HardeningCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'PendingTargetEnvironment')]
        [string]$Status,

        [string]$Code,

        [string]$Area,

        [string]$Message,

        [string]$Evidence = '',

        [string]$Recommendation = ''
    )

    $checks.Add([pscustomobject][ordered]@{
            Code = $Code
            Area = $Area
            Status = $Status
            Message = $Message
            Evidence = $Evidence
            Recommendation = $Recommendation
        })

    if ([string]::IsNullOrWhiteSpace($Evidence)) {
        Write-Host ('[{0}] {1}: {2}' -f $Status, $Code, $Message)
    }
    else {
        Write-Host ('[{0}] {1}: {2} ({3})' -f $Status, $Code, $Message, $Evidence)
    }
}

function Get-HeaderMap {
    param([xml]$WebConfig)

    $headers = [ordered]@{}
    $nodes = $WebConfig.SelectNodes('/configuration/system.webServer/httpProtocol/customHeaders/add')
    foreach ($node in $nodes) {
        $name = $node.GetAttribute('name')
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            $headers[$name] = $node.GetAttribute('value')
        }
    }

    return $headers
}

function Get-TransformInventory {
    $transformNames = @(
        'Web.Debug.Template.config',
        'Web.Test.Template.config',
        'Web.Release.Template.config',
        'Web.Debug.config',
        'Web.Test.config',
        'Web.Release.config'
    )

    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($name in $transformNames) {
        $path = Join-Path $targetRoot $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            # 中文：发布产物中转换文件应被排除；源码目录中本机转换文件可能被 .gitignore 忽略。
            # English: Transform files should be excluded from published packages; local source transforms may be ignored by Git.
            $items.Add([pscustomobject][ordered]@{
                    Name = $name
                    Exists = $false
                    Path = ConvertTo-RepoPath -Path $path
                    SetsCookieRequireSsl = $false
                    SetsFormsRequireSsl = $false
                    SetsDebugFalse = $false
                    AddsHsts = $false
                    TightensReferrerPolicy = $false
                })
            continue
        }

        $text = Get-Utf8Text -LiteralPath $path
        $items.Add([pscustomobject][ordered]@{
                Name = $name
                Exists = $true
                Path = ConvertTo-RepoPath -Path $path
                SetsCookieRequireSsl = $text -match 'httpCookies[^>]+requireSSL\s*=\s*"true"'
                SetsFormsRequireSsl = $text -match 'forms[^>]+requireSSL\s*=\s*"true"'
                SetsDebugFalse = $text -match 'compilation[^>]+debug\s*=\s*"false"'
                AddsHsts = $text -match 'Strict-Transport-Security'
                TightensReferrerPolicy = $text -match 'strict-origin-when-cross-origin'
            })
    }

    return [object[]]$items.ToArray()
}

function Find-LatestReleaseManifest {
    param([string]$Root)

    if ([string]::IsNullOrWhiteSpace($Root) -or -not (Test-Path -LiteralPath $Root -PathType Container)) {
        return $null
    }

    $matches = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter 'release-manifest.json' |
        Sort-Object LastWriteTimeUtc -Descending)
    if ($matches.Count -eq 0) {
        return $null
    }

    return $matches[0].FullName
}

function Add-DirectoryReadOnlyCheck {
    param(
        [string]$Code,
        [string]$Area,
        [string]$Path,
        [bool]$RequiredForProduction
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code $Code -Area $Area -Message '目录路径未提供，需在目标环境补证。'
        return
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        $status = if ($RequiredForProduction) { 'PendingTargetEnvironment' } else { 'Info' }
        Add-HardeningCheck -Status $status -Code $Code -Area $Area -Message '目录当前不存在，未执行写入测试。' -Evidence $fullPath -Recommendation '目标环境需预创建目录并授予应用池身份最小写权限。'
        return
    }

    try {
        $acl = Get-Acl -LiteralPath $fullPath
        $rules = @($acl.Access | Select-Object -First 6 | ForEach-Object {
                '{0}:{1}:{2}' -f $_.IdentityReference.Value, $_.FileSystemRights, $_.AccessControlType
            })
        Add-HardeningCheck -Status 'Info' -Code $Code -Area $Area -Message '目录存在，已只读记录 ACL 摘要。' -Evidence (($rules -join '; ') -replace '\|', '/') -Recommendation '真实目标环境仍需人工确认应用池身份是否仅具备必要权限。'
    }
    catch {
        Add-HardeningCheck -Status 'Warning' -Code $Code -Area $Area -Message '目录存在，但 ACL 无法读取。' -Evidence $_.Exception.Message
    }
}

function Format-MarkdownCell {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    return ($Value -replace '\|', '\|' -replace '\r?\n', '<br>')
}

Add-HardeningCheck -Status 'Info' -Code 'P14.4-SCOPE' -Area 'Process' -Message '当前执行生产前硬化只读复核。' -Evidence ('Profile={0}; PublishedPackage={1}' -f $Profile, $isPublishedPackage)

if (-not (Test-Path -LiteralPath $webConfigPath -PathType Leaf)) {
    Add-HardeningCheck -Status 'Fail' -Code 'CFG-WEBCONFIG' -Area 'Configuration' -Message 'Web.config 未找到。' -Evidence $webConfigPath
}
else {
    Add-HardeningCheck -Status 'Pass' -Code 'CFG-WEBCONFIG' -Area 'Configuration' -Message 'Web.config 已找到。' -Evidence (ConvertTo-RepoPath -Path $webConfigPath)
    [xml]$webConfig = Get-Utf8Text -LiteralPath $webConfigPath
    $headers = Get-HeaderMap -WebConfig $webConfig

    $httpCookiesNode = $webConfig.SelectSingleNode('/configuration/system.web/httpCookies')
    $httpRuntimeNode = $webConfig.SelectSingleNode('/configuration/system.web/httpRuntime')
    $formsNode = $webConfig.SelectSingleNode('/configuration/system.web/authentication/forms')
    $customErrorsNode = $webConfig.SelectSingleNode('/configuration/system.web/customErrors')
    $compilationNode = $webConfig.SelectSingleNode('/configuration/system.web/compilation')
    $machineKeyNode = $webConfig.SelectSingleNode('/configuration/system.web/machineKey')

    $httpOnlyCookies = (Get-XmlAttributeValue -Node $httpCookiesNode -Name 'httpOnlyCookies') -ieq 'true'
    if ($httpOnlyCookies) {
        Add-HardeningCheck -Status 'Pass' -Code 'COOKIE-HTTPONLY' -Area 'Cookie' -Message 'httpOnlyCookies 已显式开启。' -Evidence 'httpOnlyCookies=true'
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'COOKIE-HTTPONLY' -Area 'Cookie' -Message 'httpOnlyCookies 未显式开启。' -Recommendation 'P14.4 推荐设置 httpOnlyCookies=true。'
    }

    $enableVersionHeader = Get-XmlAttributeValue -Node $httpRuntimeNode -Name 'enableVersionHeader'
    if ($enableVersionHeader -ieq 'false') {
        Add-HardeningCheck -Status 'Pass' -Code 'CFG-ASP.NET-VERSION-HEADER' -Area 'Configuration' -Message 'ASP.NET 版本响应头已显式禁用。' -Evidence 'enableVersionHeader=false'
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'CFG-ASP.NET-VERSION-HEADER' -Area 'Configuration' -Message 'ASP.NET 版本响应头未显式禁用。' -Evidence ('enableVersionHeader={0}' -f $enableVersionHeader) -Recommendation 'P14.4 推荐设置 enableVersionHeader=false。'
    }

    $cookieRequireSsl = (Get-XmlAttributeValue -Node $httpCookiesNode -Name 'requireSSL') -ieq 'true'
    if (Test-StrictProfile) {
        if ($cookieRequireSsl) {
            Add-HardeningCheck -Status 'Pass' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Message 'Cookie requireSSL 已开启。'
        }
        elseif ($isPublishedPackage) {
            Add-HardeningCheck -Status 'Fail' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Message '生产/扫描发布产物未启用 Cookie requireSSL。' -Recommendation '检查 Web.Release/Test transform 或目标环境配置。'
        }
        else {
            Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Message '源码基线未强制 Cookie requireSSL，需发布产物或目标 HTTPS 环境复核。'
        }
    }
    else {
        Add-HardeningCheck -Status 'Pass' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Message '开发/旧浏览器基线不强制 Cookie requireSSL。' -Evidence ('requireSSL={0}' -f (Get-XmlAttributeValue -Node $httpCookiesNode -Name 'requireSSL'))
    }

    $cookieSameSite = Get-XmlAttributeValue -Node $httpCookiesNode -Name 'sameSite'
    if ([string]::IsNullOrWhiteSpace($cookieSameSite)) {
        Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'COOKIE-SAMESITE' -Area 'Cookie' -Message 'SameSite 未显式声明。' -Recommendation '结合 IE9/IE8/IE6 和目标浏览器策略决定。'
    }
    else {
        Add-HardeningCheck -Status 'Info' -Code 'COOKIE-SAMESITE' -Area 'Cookie' -Message 'SameSite 已声明。' -Evidence $cookieSameSite
    }

    $formsRequireSsl = (Get-XmlAttributeValue -Node $formsNode -Name 'requireSSL') -ieq 'true'
    if (Test-StrictProfile) {
        if ($formsRequireSsl) {
            Add-HardeningCheck -Status 'Pass' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Message 'Forms requireSSL 已开启。'
        }
        elseif ($isPublishedPackage) {
            Add-HardeningCheck -Status 'Fail' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Message '生产/扫描发布产物未启用 Forms requireSSL。' -Recommendation '检查 Web.Release/Test transform 或目标环境配置。'
        }
        else {
            Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Message '源码基线未强制 Forms requireSSL，需发布产物或目标 HTTPS 环境复核。'
        }
    }
    else {
        Add-HardeningCheck -Status 'Pass' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Message '开发/旧浏览器基线不强制 Forms requireSSL。'
    }

    $formsProtection = Get-XmlAttributeValue -Node $formsNode -Name 'protection'
    if ($formsProtection -ieq 'All') {
        Add-HardeningCheck -Status 'Pass' -Code 'AUTH-FORMS-PROTECTION' -Area 'Authentication' -Message 'Forms protection=All。'
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'AUTH-FORMS-PROTECTION' -Area 'Authentication' -Message 'Forms protection 需要复核。' -Evidence $formsProtection
    }

    $hsts = if ($headers.Contains('Strict-Transport-Security')) { [string]$headers['Strict-Transport-Security'] } else { '' }
    if (Test-StrictProfile) {
        if ($hsts -match '\bmax-age=') {
            Add-HardeningCheck -Status 'Pass' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Message 'HSTS 已配置。' -Evidence $hsts
        }
        elseif ($isPublishedPackage) {
            Add-HardeningCheck -Status 'Fail' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Message '生产/扫描发布产物缺少 HSTS。' -Recommendation '确认 HTTPS 目标环境和 Release/Test transform。'
        }
        else {
            Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Message '源码基线不声明 HSTS，需发布产物或目标 HTTPS 环境复核。'
        }
    }
    else {
        Add-HardeningCheck -Status 'Pass' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Message '开发/旧浏览器基线中 HSTS 可缺省。'
    }

    $referrerPolicy = if ($headers.Contains('Referrer-Policy')) { [string]$headers['Referrer-Policy'] } else { '' }
    if (Test-StrictProfile) {
        if ($referrerPolicy -ieq 'strict-origin-when-cross-origin') {
            Add-HardeningCheck -Status 'Pass' -Code 'HDR-REFERRER-POLICY' -Area 'SecurityHeader' -Message 'Referrer-Policy 已收紧。' -Evidence $referrerPolicy
        }
        elseif ($isPublishedPackage) {
            Add-HardeningCheck -Status 'Warning' -Code 'HDR-REFERRER-POLICY' -Area 'SecurityHeader' -Message '生产/扫描发布产物 Referrer-Policy 未收紧。' -Evidence $referrerPolicy
        }
        else {
            Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'HDR-REFERRER-POLICY' -Area 'SecurityHeader' -Message '源码基线保留开发策略，需发布产物复核。' -Evidence $referrerPolicy
        }
    }
    elseif ([string]::IsNullOrWhiteSpace($referrerPolicy)) {
        Add-HardeningCheck -Status 'Warning' -Code 'HDR-REFERRER-POLICY' -Area 'SecurityHeader' -Message 'Referrer-Policy 缺失。'
    }
    else {
        Add-HardeningCheck -Status 'Pass' -Code 'HDR-REFERRER-POLICY' -Area 'SecurityHeader' -Message 'Referrer-Policy 已配置。' -Evidence $referrerPolicy
    }

    if ($headers.Contains('Content-Security-Policy') -and $headers['Content-Security-Policy'] -match 'unsafe-inline|unsafe-eval') {
        Add-HardeningCheck -Status 'Warning' -Code 'HDR-CSP-COMPAT' -Area 'SecurityHeader' -Message 'CSP 保留 Web Forms 兼容项。' -Evidence ([string]$headers['Content-Security-Policy']) -Recommendation '继续引用 P10-EX-0002；后续 CSP 深化阶段逐步收敛。'
    }
    elseif ($headers.Contains('Content-Security-Policy')) {
        Add-HardeningCheck -Status 'Pass' -Code 'HDR-CSP' -Area 'SecurityHeader' -Message 'CSP 已配置。' -Evidence ([string]$headers['Content-Security-Policy'])
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'HDR-CSP' -Area 'SecurityHeader' -Message 'CSP 缺失。'
    }

    if ($headers.Contains('Access-Control-Allow-Origin') -and [string]$headers['Access-Control-Allow-Origin'] -eq '*') {
        Add-HardeningCheck -Status 'Warning' -Code 'HDR-CORS-WILDCARD' -Area 'SecurityHeader' -Message '发现启用的 CORS 通配响应头。' -Recommendation '除非有明确业务边界，否则生产环境不应开启通配 CORS。'
    }
    else {
        Add-HardeningCheck -Status 'Pass' -Code 'HDR-CORS-WILDCARD' -Area 'SecurityHeader' -Message '未发现启用的 CORS 通配响应头。'
    }

    $customErrorsMode = Get-XmlAttributeValue -Node $customErrorsNode -Name 'mode'
    if ($customErrorsMode -in @('RemoteOnly', 'On')) {
        Add-HardeningCheck -Status 'Pass' -Code 'ERROR-CUSTOMERRORS' -Area 'ErrorHandling' -Message 'customErrors 已启用。' -Evidence $customErrorsMode
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'ERROR-CUSTOMERRORS' -Area 'ErrorHandling' -Message 'customErrors 需要复核。' -Evidence $customErrorsMode
    }

    $debugEnabled = (Get-XmlAttributeValue -Node $compilationNode -Name 'debug') -ieq 'true'
    if (Test-StrictProfile) {
        if (-not $debugEnabled) {
            Add-HardeningCheck -Status 'Pass' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Message 'compilation debug=false。'
        }
        elseif ($isPublishedPackage) {
            Add-HardeningCheck -Status 'Fail' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Message '生产/扫描发布产物仍为 debug=true。'
        }
        else {
            Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Message '源码基线为 debug=true，需发布产物复核。'
        }
    }
    elseif ($debugEnabled) {
        Add-HardeningCheck -Status 'Info' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Message '开发基线 debug=true。'
    }
    else {
        Add-HardeningCheck -Status 'Pass' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Message 'debug=false。'
    }

    if ($null -eq $machineKeyNode) {
        Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'CFG-MACHINEKEY' -Area 'Configuration' -Message '未声明 machineKey。' -Recommendation '生产单/多实例拓扑明确后由部署侧提供真实值；不得提交真实 key。'
    }
    else {
        $validation = Get-XmlAttributeValue -Node $machineKeyNode -Name 'validation'
        $decryption = Get-XmlAttributeValue -Node $machineKeyNode -Name 'decryption'
        Add-HardeningCheck -Status 'Info' -Code 'CFG-MACHINEKEY' -Area 'Configuration' -Message 'machineKey 节存在，仅记录算法和键存在性，不输出真实键值。' -Evidence ('validation={0}; decryption={1}; validationKeyPresent={2}; decryptionKeyPresent={3}' -f $validation, $decryption, (-not [string]::IsNullOrWhiteSpace((Get-XmlAttributeValue -Node $machineKeyNode -Name 'validationKey'))), (-not [string]::IsNullOrWhiteSpace((Get-XmlAttributeValue -Node $machineKeyNode -Name 'decryptionKey'))))
    }

    $externalCfgSetting = $webConfig.SelectSingleNode('/configuration/appSettings/add[@key="ExternalCfgPath"]')
    if ($null -ne $externalCfgSetting) {
        Add-HardeningCheck -Status 'Pass' -Code 'CFG-EXTERNALCFG-SETTING' -Area 'Configuration' -Message 'ExternalCfgPath 设置项存在。' -Evidence 'value redacted/empty-aware'
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'CFG-EXTERNALCFG-SETTING' -Area 'Configuration' -Message 'ExternalCfgPath 设置项缺失。'
    }
}

$transforms = @(Get-TransformInventory)
if ($isPublishedPackage) {
    $publishedTransforms = @($transforms | Where-Object { $_.Exists })
    if ($publishedTransforms.Count -eq 0) {
        Add-HardeningCheck -Status 'Pass' -Code 'PUBLISH-TRANSFORM-EXCLUSION' -Area 'Publish' -Message '发布产物未包含 Web.*.config 转换文件。'
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'PUBLISH-TRANSFORM-EXCLUSION' -Area 'Publish' -Message '发布产物包含 Web.*.config 转换文件，需复核。' -Evidence (($publishedTransforms.Name | Select-Object -First 8) -join '; ')
    }
}
else {
    $releaseTemplates = @($transforms | Where-Object { $_.Name -match 'Release|Test' -and $_.Name -match 'Template' -and $_.Exists })
    $hardenedTemplates = @($releaseTemplates | Where-Object { $_.SetsCookieRequireSsl -and $_.SetsFormsRequireSsl -and $_.SetsDebugFalse -and $_.AddsHsts -and $_.TightensReferrerPolicy })
    if ($hardenedTemplates.Count -eq $releaseTemplates.Count -and $releaseTemplates.Count -gt 0) {
        Add-HardeningCheck -Status 'Pass' -Code 'TRANSFORM-HARDENING-TEMPLATES' -Area 'Transform' -Message 'Release/Test 模板包含关键硬化转换。' -Evidence ('templates={0}' -f $releaseTemplates.Count)
    }
    else {
        Add-HardeningCheck -Status 'Warning' -Code 'TRANSFORM-HARDENING-TEMPLATES' -Area 'Transform' -Message 'Release/Test 模板硬化转换需要复核。'
    }
}

if ([string]::IsNullOrWhiteSpace($LogDirectoryPath)) {
    $LogDirectoryPath = Join-Path $targetRoot 'App_Data/Logs'
}

if ([string]::IsNullOrWhiteSpace($UploadDirectoryPath)) {
    $UploadDirectoryPath = Join-Path $targetRoot 'Uploads'
}

Add-DirectoryReadOnlyCheck -Code 'DIR-LOGS' -Area 'Directory' -Path $LogDirectoryPath -RequiredForProduction:$true
Add-DirectoryReadOnlyCheck -Code 'DIR-UPLOADS' -Area 'Directory' -Path $UploadDirectoryPath -RequiredForProduction:$true

if ([string]::IsNullOrWhiteSpace($ExternalConfigRoot)) {
    Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'CFG-EXTERNALCFG-ROOT' -Area 'Configuration' -Message '未提供目标外置配置根目录，需在目标环境补证。'
}
elseif (Test-Path -LiteralPath $ExternalConfigRoot -PathType Container) {
    Add-HardeningCheck -Status 'Info' -Code 'CFG-EXTERNALCFG-ROOT' -Area 'Configuration' -Message '外置配置根目录存在，未读取其中敏感文件。' -Evidence $ExternalConfigRoot
}
else {
    Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'CFG-EXTERNALCFG-ROOT' -Area 'Configuration' -Message '外置配置根目录不存在或不可访问。' -Evidence $ExternalConfigRoot
}

$manifestPath = Find-LatestReleaseManifest -Root $ReleaseManifestRoot
if ($null -eq $manifestPath) {
    Add-HardeningCheck -Status 'PendingTargetEnvironment' -Code 'PUBLISH-MANIFEST' -Area 'Publish' -Message '未找到 P14.2 release manifest。'
}
else {
    try {
        $manifest = Get-Utf8Text -LiteralPath $manifestPath | ConvertFrom-Json
        Add-HardeningCheck -Status 'Pass' -Code 'PUBLISH-MANIFEST' -Area 'Publish' -Message '已找到最新 release manifest。' -Evidence (ConvertTo-RepoPath -Path $manifestPath)

        $manifestChecks = @($manifest.Checks)
        foreach ($code in @('REVIEW-PATHS', 'SENSITIVE-CONTENT-SIGNALS')) {
            $matched = @($manifestChecks | Where-Object { $_.Code -eq $code })
            if ($matched.Count -eq 0) {
                Add-HardeningCheck -Status 'Info' -Code ('MANIFEST-{0}' -f $code) -Area 'Publish' -Message ('Release manifest 未包含 {0}。' -f $code)
                continue
            }

            $status = [string]$matched[0].Status
            if ($status -eq 'Warning') {
                Add-HardeningCheck -Status 'Warning' -Code ('MANIFEST-{0}' -f $code) -Area 'Publish' -Message ('{0} 仍需作为 P14.4 硬化 backlog 复核。' -f $code) -Evidence ([string]$matched[0].Message)
            }
            elseif ($status -eq 'Fail') {
                Add-HardeningCheck -Status 'Fail' -Code ('MANIFEST-{0}' -f $code) -Area 'Publish' -Message ('{0} 在 release manifest 中为 Fail。' -f $code) -Evidence ([string]$matched[0].Message)
            }
            else {
                Add-HardeningCheck -Status 'Pass' -Code ('MANIFEST-{0}' -f $code) -Area 'Publish' -Message ('{0} 当前无阻断。' -f $code) -Evidence $status
            }
        }
    }
    catch {
        Add-HardeningCheck -Status 'Warning' -Code 'PUBLISH-MANIFEST' -Area 'Publish' -Message 'release manifest 无法解析。' -Evidence $_.Exception.Message
    }
}

$failCount = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
$warningCount = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count
$pendingCount = @($checks | Where-Object { $_.Status -eq 'PendingTargetEnvironment' }).Count
$passCount = @($checks | Where-Object { $_.Status -eq 'Pass' }).Count
$infoCount = @($checks | Where-Object { $_.Status -eq 'Info' }).Count

$summary = [pscustomobject][ordered]@{
    SchemaVersion = 'p14.4.production-hardening-preflight.v1'
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Profile = $Profile
    TargetRoot = ConvertTo-RepoPath -Path $targetRoot
    PublishedPackage = $isPublishedPackage
    ModifiedEnvironment = $false
    ReadsExternalSecrets = $false
    FailedChecks = $failCount
    WarningChecks = $warningCount
    PendingTargetEnvironmentChecks = $pendingCount
    PassedChecks = $passCount
    InfoChecks = $infoCount
}

$result = [pscustomobject][ordered]@{
    Summary = $summary
    TransformInventory = $transforms
    Checks = [object[]]$checks.ToArray()
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($result | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $lines = @(
        '# P14.4 Production Hardening Preflight',
        '',
        ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
        ('Profile: `{0}`' -f $Profile),
        ('PublishedPackage: `{0}`' -f $isPublishedPackage),
        '',
        '| Code | Area | Status | Message | Evidence |',
        '| --- | --- | --- | --- | --- |'
    )

    foreach ($check in $checks) {
        $lines += ('| `{0}` | {1} | `{2}` | {3} | {4} |' -f
            (Format-MarkdownCell -Value $check.Code),
            (Format-MarkdownCell -Value $check.Area),
            (Format-MarkdownCell -Value $check.Status),
            (Format-MarkdownCell -Value $check.Message),
            (Format-MarkdownCell -Value $check.Evidence))
    }

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content (($lines -join [Environment]::NewLine) + [Environment]::NewLine)
}

Write-Host ('P14.4 production hardening preflight: Pass={0}; Warning={1}; Fail={2}; PendingTargetEnvironment={3}; Info={4}' -f $passCount, $warningCount, $failCount, $pendingCount, $infoCount)

if ($failCount -gt 0) {
    exit 1
}

if ($TreatWarningsAsErrors -and $warningCount -gt 0) {
    exit 2
}
