<#
.SYNOPSIS
    Builds a read-only P14.3 enterprise-scan baseline evidence package.

.DESCRIPTION
    中文：本脚本为 P14.3 生成企业扫描准备基线，重点盘点 Web.config 中安全响应头、
    Cookie、Forms Authentication、错误页、上传限制、machineKey 和发布转换线索。
    脚本只读取仓库文件并输出证据包，不修改 Web.config，不连接数据库，不读取外置敏感配置，
    也不把开发基线或源码转换文件宣称为真实企业扫描通过。
    English: This script builds a P14.3 enterprise-scan preparation baseline by inventorying
    security headers, cookies, Forms Authentication, custom errors, upload limits, machineKey,
    and transform hints from Web.config. It only reads repository files and writes an evidence
    package; it does not modify Web.config, connect to a database, read external secret
    configuration, or claim that a development/source baseline passed a real enterprise scan.
#>
[CmdletBinding()]
param(
    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [string]$PortalPath,

    [string]$WebConfigPath,

    [string]$OutputRoot,

    [string]$ScanRegisterTemplatePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($PortalPath)) {
    $PortalPath = Join-Path $repoRoot 'src/Portal'
}

$resolvedPortalPath = (Resolve-Path -LiteralPath $PortalPath).Path
if ([string]::IsNullOrWhiteSpace($WebConfigPath)) {
    $WebConfigPath = Join-Path $resolvedPortalPath 'Web.config'
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p14.3'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p14.3'
    }
}

if ([string]::IsNullOrWhiteSpace($ScanRegisterTemplatePath)) {
    $ScanRegisterTemplatePath = Join-Path $repoRoot 'work-zone/dev/templates/enterprise-scan-issue-register-template.md'
}

$resolvedWebConfigPath = [System.IO.Path]::GetFullPath($WebConfigPath)
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path $resolvedOutputRoot ('{0}-{1}' -f $runId, $Profile)
$findings = New-Object 'System.Collections.Generic.List[object]'
$headers = [ordered]@{}

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

    if ($null -eq $Node) {
        return ''
    }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) {
        return ''
    }

    return [string]$attribute.Value
}

function Test-StrictDeploymentProfile {
    return $Profile -in @('Test', 'Prod', 'Scan')
}

function Test-ProductionLikeProfile {
    return $Profile -in @('Prod', 'Scan')
}

function Add-ScanFinding {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'PendingTargetEnvironment')]
        [string]$Status,

        [ValidateSet('Critical', 'High', 'Medium', 'Low', 'Info', 'NotApplicable')]
        [string]$EnterpriseSeverity,

        [string]$Code,

        [string]$Area,

        [string]$Title,

        [string]$Evidence = '',

        [string]$Recommendation = '',

        [string]$Source = '',

        [string]$RelatedException = ''
    )

    $findings.Add([pscustomobject][ordered]@{
            Code = $Code
            Area = $Area
            Status = $Status
            EnterpriseSeverity = $EnterpriseSeverity
            Title = $Title
            Evidence = $Evidence
            Recommendation = $Recommendation
            Source = $Source
            RelatedException = $RelatedException
        })

    $display = if ([string]::IsNullOrWhiteSpace($Evidence)) {
        '[{0}] {1}: {2}' -f $Status, $Code, $Title
    }
    else {
        '[{0}] {1}: {2} ({3})' -f $Status, $Code, $Title, $Evidence
    }

    Write-Host $display
}

function Get-HeaderMap {
    param([xml]$WebConfig)

    $headers = [ordered]@{}
    $headerNodes = $WebConfig.SelectNodes('/configuration/system.webServer/httpProtocol/customHeaders/add')
    foreach ($node in $headerNodes) {
        $name = $node.GetAttribute('name')
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            $headers[$name] = $node.GetAttribute('value')
        }
    }

    return $headers
}

function Test-HeaderValue {
    param(
        [System.Collections.IDictionary]$Headers,
        [string]$Name,
        [string]$Pattern,
        [string]$Recommendation
    )

    if (-not $Headers.Contains($Name)) {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Medium' -Code ('HDR-{0}' -f $Name) -Area 'SecurityHeader' -Title ('安全响应头缺失：{0}' -f $Name) -Recommendation $Recommendation -Source 'Web.config'
        return
    }

    $value = [string]$Headers[$Name]
    if ($value -notmatch $Pattern) {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Low' -Code ('HDR-{0}' -f $Name) -Area 'SecurityHeader' -Title ('安全响应头值需要复核：{0}' -f $Name) -Evidence $value -Recommendation $Recommendation -Source 'Web.config'
        return
    }

    Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code ('HDR-{0}' -f $Name) -Area 'SecurityHeader' -Title ('安全响应头已配置：{0}' -f $Name) -Evidence $value -Source 'Web.config'
}

function Get-TransformInventory {
    $transformNames = @(
        'Web.Debug.config',
        'Web.Debug.Template.config',
        'Web.Test.config',
        'Web.Test.Template.config',
        'Web.Release.config',
        'Web.Release.Template.config'
    )

    $inventory = New-Object 'System.Collections.Generic.List[object]'
    foreach ($name in $transformNames) {
        $path = Join-Path $resolvedPortalPath $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $inventory.Add([pscustomobject][ordered]@{
                    Name = $name
                    Exists = $false
                    Path = ConvertTo-RepoPath -Path $path
                    SetsCookieRequireSsl = $false
                    SetsFormsRequireSsl = $false
                    SetsCompilationDebugFalse = $false
                    AddsHsts = $false
                    TightensReferrerPolicy = $false
                })
            continue
        }

        $text = Get-Utf8Text -LiteralPath $path
        $inventory.Add([pscustomobject][ordered]@{
                Name = $name
                Exists = $true
                Path = ConvertTo-RepoPath -Path $path
                SetsCookieRequireSsl = $text -match 'httpCookies[^>]+requireSSL\s*=\s*"true"'
                SetsFormsRequireSsl = $text -match 'forms[^>]+requireSSL\s*=\s*"true"'
                SetsCompilationDebugFalse = $text -match 'compilation[^>]+debug\s*=\s*"false"'
                AddsHsts = $text -match 'Strict-Transport-Security'
                TightensReferrerPolicy = $text -match 'strict-origin-when-cross-origin'
            })
    }

    return [object[]]$inventory.ToArray()
}

function Format-MarkdownTableCell {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    return ($Value -replace '\|', '\|' -replace '\r?\n', '<br>')
}

New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

Add-ScanFinding -Status 'Info' -EnterpriseSeverity 'Info' -Code 'P14.3-SCOPE' -Area 'Process' -Title '当前生成企业扫描准备 baseline，不代表真实扫描通过。' -Evidence $Profile -Recommendation '真实绿盟等报告到来后登记到 WorkZone 脱敏目录并逐项分流。'

if (-not (Test-Path -LiteralPath $resolvedWebConfigPath -PathType Leaf)) {
    Add-ScanFinding -Status 'Fail' -EnterpriseSeverity 'High' -Code 'CFG-WEBCONFIG' -Area 'Configuration' -Title 'Web.config 未找到。' -Evidence $resolvedWebConfigPath -Recommendation '确认 PortalPath 或 WebConfigPath。'
}
else {
    Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'CFG-WEBCONFIG' -Area 'Configuration' -Title 'Web.config 已找到。' -Evidence (ConvertTo-RepoPath -Path $resolvedWebConfigPath)
    [xml]$webConfig = Get-Utf8Text -LiteralPath $resolvedWebConfigPath

    $headers = Get-HeaderMap -WebConfig $webConfig
    Test-HeaderValue -Headers $headers -Name 'X-Content-Type-Options' -Pattern '\bnosniff\b' -Recommendation '保留 nosniff。'
    Test-HeaderValue -Headers $headers -Name 'X-XSS-Protection' -Pattern '^1;\s*mode=block$' -Recommendation '当前仍需兼容旧 IE 和企业扫描。'
    Test-HeaderValue -Headers $headers -Name 'Content-Security-Policy' -Pattern '\bdefault-src\b' -Recommendation '后续逐步收紧 Web Forms CSP 兼容项。'
    Test-HeaderValue -Headers $headers -Name 'Referrer-Policy' -Pattern '.+' -Recommendation '生产 profile 推荐 strict-origin-when-cross-origin。'
    Test-HeaderValue -Headers $headers -Name 'X-Permitted-Cross-Domain-Policies' -Pattern '.+' -Recommendation '保留跨域策略限制。'
    Test-HeaderValue -Headers $headers -Name 'X-Download-Options' -Pattern '\bnoopen\b' -Recommendation '保留下载打开限制。'
    Test-HeaderValue -Headers $headers -Name 'X-Frame-Options' -Pattern '^(SAMEORIGIN|DENY)$' -Recommendation '保留点击劫持防护。'

    if ($headers.Contains('Strict-Transport-Security')) {
        $hsts = [string]$headers['Strict-Transport-Security']
        $hstsStatus = if ($hsts -match '\bmax-age=') { 'Pass' } else { 'Warning' }
        Add-ScanFinding -Status $hstsStatus -EnterpriseSeverity 'Low' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Title 'HSTS 当前出现在基础 Web.config。' -Evidence $hsts -Recommendation '确认是否只由 Release/Test/生产发布配置注入。' -Source 'Web.config' -RelatedException 'P10-EX-0003'
    }
    elseif (Test-StrictDeploymentProfile) {
        Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Medium' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Title '源码基础配置未声明 HSTS，需在发布产物或目标 HTTPS 环境复核。' -Recommendation 'P14.4 复核 Web.Release/Test transform 和真实 HTTPS 响应。' -Source 'Web.config' -RelatedException 'P10-EX-0003'
    }
    else {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'HDR-HSTS' -Area 'SecurityHeader' -Title '开发/旧浏览器基线中 HSTS 按设计不放入基础 Web.config。' -Source 'Web.config' -RelatedException 'P10-EX-0003'
    }

    if ($headers.Contains('Content-Security-Policy') -and $headers['Content-Security-Policy'] -match 'unsafe-inline|unsafe-eval') {
        $cspStatus = if (Test-ProductionLikeProfile) { 'Warning' } else { 'Info' }
        Add-ScanFinding -Status $cspStatus -EnterpriseSeverity 'Medium' -Code 'HDR-CSP-COMPAT' -Area 'SecurityHeader' -Title 'CSP 保留 Web Forms 兼容项。' -Evidence ([string]$headers['Content-Security-Policy']) -Recommendation '沿用 P10-EX-0002 例外并在后续 CSP 深化阶段收敛。' -Source 'Web.config' -RelatedException 'P10-EX-0002'
    }

    if ($headers.Contains('Access-Control-Allow-Origin') -and [string]$headers['Access-Control-Allow-Origin'] -eq '*') {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Medium' -Code 'HDR-CORS-WILDCARD' -Area 'SecurityHeader' -Title '发现启用的 CORS 通配响应头。' -Evidence 'Access-Control-Allow-Origin=*' -Recommendation '除非有明确业务边界，否则生产环境不应开启通配 CORS。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'HDR-CORS-WILDCARD' -Area 'SecurityHeader' -Title '未发现启用的 CORS 通配响应头。' -Source 'Web.config'
    }

    $httpCookiesNode = $webConfig.SelectSingleNode('/configuration/system.web/httpCookies')
    $cookieRequireSsl = (Get-XmlAttributeValue -Node $httpCookiesNode -Name 'requireSSL') -ieq 'true'
    $cookieHttpOnly = (Get-XmlAttributeValue -Node $httpCookiesNode -Name 'httpOnlyCookies') -ieq 'true'
    if (Test-StrictDeploymentProfile) {
        if ($cookieRequireSsl) {
            Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Title 'Cookie requireSSL 已开启。' -Source 'Web.config'
        }
        else {
            Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Medium' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Title '基础 Web.config 未强制 Cookie requireSSL，需在发布产物复核。' -Recommendation 'P14.4 验证 Release/Test transform 和真实 HTTPS 目标环境。' -Source 'Web.config'
        }
    }
    else {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'COOKIE-REQUIRESSL' -Area 'Cookie' -Title '开发/旧浏览器基线中 Cookie requireSSL 未强制，避免破坏 HTTP 调试。' -Evidence ('requireSSL={0}' -f (Get-XmlAttributeValue -Node $httpCookiesNode -Name 'requireSSL')) -Source 'Web.config'
    }

    if ($cookieHttpOnly) {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'COOKIE-HTTPONLY' -Area 'Cookie' -Title 'httpOnlyCookies 已显式开启。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Low' -Code 'COOKIE-HTTPONLY' -Area 'Cookie' -Title 'httpOnlyCookies 未显式声明。' -Recommendation 'P14.4 评估在 Web.config 或发布配置中显式声明 httpOnlyCookies=true。' -Source 'Web.config'
    }

    $cookieSameSite = Get-XmlAttributeValue -Node $httpCookiesNode -Name 'sameSite'
    if ([string]::IsNullOrWhiteSpace($cookieSameSite)) {
        Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Low' -Code 'COOKIE-SAMESITE' -Area 'Cookie' -Title 'SameSite 未显式声明。' -Recommendation '结合 IE9/IE8/IE6 兼容边界和目标浏览器策略，在 P14.4 或后续发布配置中决定。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Info' -EnterpriseSeverity 'Info' -Code 'COOKIE-SAMESITE' -Area 'Cookie' -Title 'SameSite 已声明。' -Evidence $cookieSameSite -Source 'Web.config'
    }

    $formsNode = $webConfig.SelectSingleNode('/configuration/system.web/authentication/forms')
    $formsRequireSsl = (Get-XmlAttributeValue -Node $formsNode -Name 'requireSSL') -ieq 'true'
    $formsProtection = Get-XmlAttributeValue -Node $formsNode -Name 'protection'
    if ($formsProtection -ieq 'All') {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'AUTH-FORMS-PROTECTION' -Area 'Authentication' -Title 'Forms Authentication protection=All。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Medium' -Code 'AUTH-FORMS-PROTECTION' -Area 'Authentication' -Title 'Forms Authentication protection 需要复核。' -Evidence $formsProtection -Recommendation '推荐保持 protection=All。' -Source 'Web.config'
    }

    if (Test-StrictDeploymentProfile) {
        if ($formsRequireSsl) {
            Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Title 'Forms Authentication requireSSL 已开启。' -Source 'Web.config'
        }
        else {
            Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Medium' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Title '基础 Web.config 未强制 Forms requireSSL，需在发布产物复核。' -Recommendation 'P14.4 验证 Release/Test transform 和真实 HTTPS 目标环境。' -Source 'Web.config'
        }
    }
    else {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'AUTH-FORMS-REQUIRESSL' -Area 'Authentication' -Title '开发/旧浏览器基线中 Forms requireSSL 未强制，避免破坏 HTTP 调试。' -Source 'Web.config'
    }

    $customErrorsNode = $webConfig.SelectSingleNode('/configuration/system.web/customErrors')
    $customErrorsMode = Get-XmlAttributeValue -Node $customErrorsNode -Name 'mode'
    if ($customErrorsMode -in @('RemoteOnly', 'On')) {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'ERROR-CUSTOMERRORS' -Area 'ErrorHandling' -Title 'customErrors 已启用。' -Evidence $customErrorsMode -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Medium' -Code 'ERROR-CUSTOMERRORS' -Area 'ErrorHandling' -Title 'customErrors 未处于 RemoteOnly/On。' -Evidence $customErrorsMode -Recommendation '生产环境应避免向用户显示堆栈。' -Source 'Web.config'
    }

    $compilationNode = $webConfig.SelectSingleNode('/configuration/system.web/compilation')
    $debugEnabled = (Get-XmlAttributeValue -Node $compilationNode -Name 'debug') -ieq 'true'
    if ($debugEnabled -and (Test-StrictDeploymentProfile)) {
        Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Medium' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Title '基础 Web.config 仍为 debug=true，需在发布产物复核。' -Recommendation 'P14.4 验证 Release/Test transform 输出 debug=false。' -Source 'Web.config'
    }
    elseif ($debugEnabled) {
        Add-ScanFinding -Status 'Info' -EnterpriseSeverity 'Info' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Title '开发基线 debug=true。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'CFG-COMPILATION-DEBUG' -Area 'Configuration' -Title 'compilation debug=false。' -Source 'Web.config'
    }

    $httpRuntimeNode = $webConfig.SelectSingleNode('/configuration/system.web/httpRuntime')
    $enableVersionHeader = Get-XmlAttributeValue -Node $httpRuntimeNode -Name 'enableVersionHeader'
    if ($enableVersionHeader -ieq 'false') {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'CFG-ASP.NET-VERSION-HEADER' -Area 'Configuration' -Title 'ASP.NET 版本响应头已显式禁用。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Low' -Code 'CFG-ASP.NET-VERSION-HEADER' -Area 'Configuration' -Title 'httpRuntime 未显式禁用 ASP.NET 版本响应头。' -Evidence ('enableVersionHeader={0}' -f $enableVersionHeader) -Recommendation 'P14.4 评估 enableVersionHeader=false 与实际运行头。' -Source 'Web.config'
    }

    $maxRequestLength = Get-XmlAttributeValue -Node $httpRuntimeNode -Name 'maxRequestLength'
    $requestLimitNode = $webConfig.SelectSingleNode('/configuration/system.webServer/security/requestFiltering/requestLimits')
    $maxAllowedContentLength = Get-XmlAttributeValue -Node $requestLimitNode -Name 'maxAllowedContentLength'
    Add-ScanFinding -Status 'Info' -EnterpriseSeverity 'Info' -Code 'UPLOAD-LIMITS' -Area 'Upload' -Title '上传大小限制当前基线已盘点。' -Evidence ('httpRuntime.maxRequestLength={0}; requestLimits.maxAllowedContentLength={1}' -f $maxRequestLength, $maxAllowedContentLength) -Recommendation 'P14.4 与系统配置 MaxUploadBytes、真实 IIS 限制一起复核。' -Source 'Web.config'

    $machineKeyNode = $webConfig.SelectSingleNode('/configuration/system.web/machineKey')
    if ($null -eq $machineKeyNode) {
        Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Medium' -Code 'CFG-MACHINEKEY' -Area 'Configuration' -Title '基础 Web.config 未声明 machineKey。' -Recommendation '真实生产单/多实例拓扑明确后，由部署侧提供受控 machineKey 或托管密钥；真实值不得入库。' -Source 'Web.config'
    }
    else {
        Add-ScanFinding -Status 'Info' -EnterpriseSeverity 'Info' -Code 'CFG-MACHINEKEY' -Area 'Configuration' -Title 'machineKey 节存在，已仅记录算法和键存在性，不输出真实键值。' -Evidence ('validation={0}; decryption={1}; validationKeyPresent={2}; decryptionKeyPresent={3}' -f (Get-XmlAttributeValue -Node $machineKeyNode -Name 'validation'), (Get-XmlAttributeValue -Node $machineKeyNode -Name 'decryption'), (-not [string]::IsNullOrWhiteSpace((Get-XmlAttributeValue -Node $machineKeyNode -Name 'validationKey'))), (-not [string]::IsNullOrWhiteSpace((Get-XmlAttributeValue -Node $machineKeyNode -Name 'decryptionKey')))) -Source 'Web.config'
    }

    $pagesNode = $webConfig.SelectSingleNode('/configuration/system.web/pages')
    Add-ScanFinding -Status 'Info' -EnterpriseSeverity 'Info' -Code 'LEGACY-RENDERING' -Area 'Compatibility' -Title 'WebForms 渲染兼容设置已盘点。' -Evidence ('controlRenderingCompatibilityVersion={0}; clientIDMode={1}' -f (Get-XmlAttributeValue -Node $pagesNode -Name 'controlRenderingCompatibilityVersion'), (Get-XmlAttributeValue -Node $pagesNode -Name 'clientIDMode')) -Recommendation '旧浏览器降级继续按 P9/P14 证据补齐。' -Source 'Web.config'
}

$transformInventory = @(Get-TransformInventory)
$releaseLikeTransforms = @($transformInventory | Where-Object { $_.Exists -and $_.Name -match 'Release|Test' })
if ($releaseLikeTransforms.Count -gt 0) {
    $hardenedTransformCount = @($releaseLikeTransforms | Where-Object { $_.SetsCookieRequireSsl -and $_.SetsFormsRequireSsl -and $_.SetsCompilationDebugFalse -and $_.AddsHsts }).Count
    if ($hardenedTransformCount -gt 0) {
        Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'TRANSFORM-HARDENING-HINTS' -Area 'Transform' -Title 'Release/Test 转换文件包含硬化线索。' -Evidence ('matched={0}; total={1}' -f $hardenedTransformCount, $releaseLikeTransforms.Count) -Recommendation 'P14.4 仍需验证真实发布产物，而不是只看源码转换文件。'
    }
    else {
        Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Medium' -Code 'TRANSFORM-HARDENING-HINTS' -Area 'Transform' -Title 'Release/Test 转换硬化线索不足。' -Recommendation 'P14.4 复核 requireSSL、debug=false、HSTS 和 Referrer-Policy。'
    }
}
else {
    Add-ScanFinding -Status 'PendingTargetEnvironment' -EnterpriseSeverity 'Medium' -Code 'TRANSFORM-HARDENING-HINTS' -Area 'Transform' -Title '未找到 Release/Test 转换文件。' -Recommendation 'P14.4 复核发布 profile 和目标环境配置来源。'
}

$templateExists = Test-Path -LiteralPath $ScanRegisterTemplatePath -PathType Leaf
if ($templateExists) {
    Add-ScanFinding -Status 'Pass' -EnterpriseSeverity 'NotApplicable' -Code 'SCAN-REGISTER-TEMPLATE' -Area 'Process' -Title '企业扫描登记模板已存在。' -Evidence (ConvertTo-RepoPath -Path $ScanRegisterTemplatePath)
}
else {
    Add-ScanFinding -Status 'Warning' -EnterpriseSeverity 'Low' -Code 'SCAN-REGISTER-TEMPLATE' -Area 'Process' -Title '企业扫描登记模板未找到。' -Evidence (ConvertTo-RepoPath -Path $ScanRegisterTemplatePath) -Recommendation '提交模板后重新生成 P14.3 baseline。'
}

$summary = [pscustomobject][ordered]@{
    SchemaVersion = 'p14.3.enterprise-scan-baseline.v1'
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Profile = $Profile
    RunId = $runId
    RunDirectory = ConvertTo-RepoPath -Path $runDirectory
    PortalPath = ConvertTo-RepoPath -Path $resolvedPortalPath
    WebConfigPath = ConvertTo-RepoPath -Path $resolvedWebConfigPath
    ScanRegisterTemplatePath = ConvertTo-RepoPath -Path $ScanRegisterTemplatePath
    ModifiedConfiguration = $false
    ReadsExternalSecrets = $false
    RealEnterpriseScanReportImported = $false
    RealRetestPerformed = $false
    PendingTargetEnvironmentCount = @($findings | Where-Object { $_.Status -eq 'PendingTargetEnvironment' }).Count
    FailedChecks = @($findings | Where-Object { $_.Status -eq 'Fail' }).Count
    WarningChecks = @($findings | Where-Object { $_.Status -eq 'Warning' }).Count
    InfoChecks = @($findings | Where-Object { $_.Status -eq 'Info' }).Count
    PassedChecks = @($findings | Where-Object { $_.Status -eq 'Pass' }).Count
}

$baseline = [pscustomobject][ordered]@{
    Summary = $summary
    SecurityHeaders = @(
        foreach ($key in $headers.Keys) {
            [pscustomobject][ordered]@{
                Name = [string]$key
                Value = [string]$headers[$key]
                Source = 'Web.config'
            }
        }
    )
    TransformInventory = $transformInventory
    Findings = [object[]]$findings.ToArray()
}

$jsonPath = Join-Path $runDirectory 'enterprise-scan-baseline.json'
$tablePath = Join-Path $runDirectory 'enterprise-scan-baseline.md'
$readmePath = Join-Path $runDirectory 'README.md'

Write-Utf8NoBomFile -Path $jsonPath -Content (($baseline | ConvertTo-Json -Depth 8) + [Environment]::NewLine)

$tableLines = @(
    '# P14.3 企业扫描准备 Baseline',
    '',
    ('生成时间 UTC：`{0}`' -f $summary.GeneratedAtUtc),
    ('Profile：`{0}`' -f $Profile),
    '',
    '| Code | Area | Status | Severity | Title | Evidence | Related Exception |',
    '| --- | --- | --- | --- | --- | --- | --- |'
)

foreach ($finding in $findings) {
    $tableLines += ('| `{0}` | {1} | `{2}` | `{3}` | {4} | {5} | {6} |' -f
        (Format-MarkdownTableCell -Value $finding.Code),
        (Format-MarkdownTableCell -Value $finding.Area),
        (Format-MarkdownTableCell -Value $finding.Status),
        (Format-MarkdownTableCell -Value $finding.EnterpriseSeverity),
        (Format-MarkdownTableCell -Value $finding.Title),
        (Format-MarkdownTableCell -Value $finding.Evidence),
        (Format-MarkdownTableCell -Value $finding.RelatedException))
}

Write-Utf8NoBomFile -Path $tablePath -Content (($tableLines -join [Environment]::NewLine) + [Environment]::NewLine)

$readmeLines = @(
    '# P14.3 Evidence Package',
    '',
    '本目录由 `dev/scripts/New-PortalEnterpriseScanBaseline.ps1` 生成。',
    '',
    '1. `enterprise-scan-baseline.json`：机器可读 baseline。',
    '2. `enterprise-scan-baseline.md`：人工复核表。',
    '3. 本证据包只表示企业扫描准备基线，不表示真实绿盟或其它工具扫描已通过。',
    '',
    '## Summary',
    '',
    ('- Profile: `{0}`' -f $summary.Profile),
    ('- Pass: `{0}`' -f $summary.PassedChecks),
    ('- Warning: `{0}`' -f $summary.WarningChecks),
    ('- Fail: `{0}`' -f $summary.FailedChecks),
    ('- PendingTargetEnvironment: `{0}`' -f $summary.PendingTargetEnvironmentCount),
    ('- RealEnterpriseScanReportImported: `{0}`' -f $summary.RealEnterpriseScanReportImported),
    ('- RealRetestPerformed: `{0}`' -f $summary.RealRetestPerformed),
    '',
    '## Boundary',
    '',
    '1. 未读取外置连接串、密码、Token、Cookie 或证书私钥。',
    '2. 未修改 Web.config 或发布配置。',
    '3. 真实扫描报告后续需脱敏后放入 WorkZone 扫描目录，再抽取摘要登记。'
)

Write-Utf8NoBomFile -Path $readmePath -Content (($readmeLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('P14.3 enterprise scan baseline: {0}' -f (ConvertTo-RepoPath -Path $runDirectory))
Write-Host ('Pass={0}; Warning={1}; Fail={2}; PendingTargetEnvironment={3}' -f $summary.PassedChecks, $summary.WarningChecks, $summary.FailedChecks, $summary.PendingTargetEnvironmentCount)

if ($summary.FailedChecks -gt 0) {
    exit 1
}
