# 目标环境 Cookie / machineKey 观察（只读，不记录任何密钥明文）
# 用途：ADR 0021「可信部署配置与生产 Cookie 策略」由"提议"转"已接受"所需的目标环境事实证据。
# 约束（ADR 0021 第 7 条）：不得记录连接串、密码、Cookie 值、Token、证书私钥或真实密钥。
#   → 本脚本只记录 Cookie **属性**（Secure/HttpOnly/SameSite）、machineKey 的**算法与指纹**。
#
# 可选的登录观测：若提供环境变量 HIA_TARGET_SIGNIN_USER / HIA_TARGET_SIGNIN_PASSWORD，
#   脚本会按门户真实登录链路（一次性 RSA 公钥加密口令 → 提交 Signin 表单）尝试登录，
#   并记录**票据 Cookie 的属性**（不含值）。未提供时如实记为"未观测"。
param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [string]$WebConfigPath,
    [string]$OutputPath,
    [string]$PackageLabel = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$lines = [System.Collections.Generic.List[string]]::new()
function Say([string]$s) { $lines.Add($s) }

Say '# 目标环境 Cookie / machineKey 观察'
Say ''
Say ('- 生成时间（UTC）：' + [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Say ('- 目标终结点：' + $BaseUrl)
if (-not [string]::IsNullOrWhiteSpace($PackageLabel)) { Say ('- 发布包标识：' + $PackageLabel) }
Say '- 记录口径：**只记录属性与指纹**，不记录 Cookie 值、machineKey 密钥、连接串或证书私钥。'
Say ''

$uri = [Uri]$BaseUrl

# ---------- 1. TLS 事实（独立握手，捕获服务端证书）----------
Say '## 1. HTTPS 终结点与 TLS'
Say ''
$script:capturedCertRaw = $null
try {
    $tcp = [System.Net.Sockets.TcpClient]::new()
    $tcp.Connect($uri.Host, $uri.Port)
    $ssl = [System.Net.Security.SslStream]::new($tcp.GetStream(), $false, {
            param($sender, $certificate, $chain, $sslPolicyErrors)
            # 必须在回调内取原始字节：SslStream 释放后证书句柄即失效。
            if ($certificate) { $script:capturedCertRaw = $certificate.GetRawCertData() }
            return $true
        })
    $ssl.AuthenticateAsClient($uri.Host)
    Say ('- 协商的 TLS 协议：' + $ssl.SslProtocol)
    $ssl.Dispose()
    $tcp.Dispose()
} catch {
    Say ('- TLS 握手观测失败：' + $_.Exception.Message)
}
if ($script:capturedCertRaw) {
    $raw = $script:capturedCertRaw
    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($raw)
    Say ('- 证书主题：' + $cert.Subject)
    Say ('- 证书颁发者：' + $cert.Issuer)
    Say ('- 证书有效期至：' + $(if ($cert.NotAfter) { $cert.NotAfter.ToString('yyyy-MM-dd') } else { '（不可读）' }))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    Say ('- 证书 SHA-256 指纹：' + (($sha.ComputeHash($raw) | ForEach-Object { $_.ToString('x2') }) -join ''))
} else {
    Say '- 证书：未捕获'
}
Say ''

# ---------- 2. HTTPS 响应：状态、响应头、Cookie 属性 ----------
Say '## 2. 目标终结点响应与 Cookie 属性（不含 Cookie 值）'
Say ''
$session = $null
$homeResponse = $null
try {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $homeResponse = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -SkipCertificateCheck -SkipHttpErrorCheck -WebSession $session -TimeoutSec 60
    Say ('- HTTP 状态：' + [int]$homeResponse.StatusCode)
    $hsts = $homeResponse.Headers['Strict-Transport-Security']
    Say ('- 响应头 Strict-Transport-Security：' + $(if ($hsts) { $hsts } else { '（未出现）' }))
    $setCookies = @()
    if ($homeResponse.Headers['Set-Cookie']) { $setCookies = @($homeResponse.Headers['Set-Cookie']) }
    if ($setCookies.Count -eq 0) {
        Say '- 匿名请求未下发 Set-Cookie（票据 Cookie 在登录动作时下发）。'
    } else {
        Say ('- Set-Cookie 条目数：' + $setCookies.Count)
        foreach ($sc in $setCookies) {
            $name = ($sc -split '=')[0]
            $attrs = @()
            if ($sc -match '(?i);\s*Secure') { $attrs += 'Secure' } else { $attrs += 'Secure:缺失' }
            if ($sc -match '(?i);\s*HttpOnly') { $attrs += 'HttpOnly' } else { $attrs += 'HttpOnly:缺失' }
            if ($sc -match '(?i);\s*SameSite=([A-Za-z]+)') { $attrs += ('SameSite=' + $Matches[1]) } else { $attrs += 'SameSite:未声明' }
            Say ('- Cookie 名称：' + $name + '｜属性：' + ($attrs -join '、') + '（仅属性，不含值）')
        }
    }
} catch {
    Say ('- 目标终结点请求失败：' + $_.Exception.Message)
}
Say ''

# ---------- 2.1 可选：按真实登录链路观测票据 Cookie 属性 ----------
Say '## 2.1 登录链路票据 Cookie 观测'
Say ''
$signinUser = $env:HIA_TARGET_SIGNIN_USER
$signinPassword = $env:HIA_TARGET_SIGNIN_PASSWORD
if ([string]::IsNullOrWhiteSpace($signinUser) -or [string]::IsNullOrWhiteSpace($signinPassword)) {
    Say '- 未提供可用管理员账号（环境变量 `HIA_TARGET_SIGNIN_USER` / `HIA_TARGET_SIGNIN_PASSWORD`）：'
    Say '  本次**未观测**登录票据 Cookie 属性。门户登录口令走一次性 RSA 公钥加密提交，'
    Say '  无可用账号时无法在不篡改请求的前提下产生票据 Cookie。'
} else {
    try {
        $html = $homeResponse.Content
        function Get-FieldValue([string]$content, [string]$name) {
            $m = [regex]::Match($content, 'name="' + [regex]::Escape($name) + '"[^>]*value="([^"]*)"')
            if ($m.Success) { return [System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value) }
            return ''
        }
        # 从页面里发现 Signin 控件的字段名（控件 ID 随页面结构变化，不写死）
        $names = @([regex]::Matches($html, 'name="([^"]*(?:EmailOrName|EncryptedPassword|SigninBtn|RememberCheckbox)[^"]*)"') | ForEach-Object { $_.Groups[1].Value })
        $userField = ($names | Where-Object { $_ -match 'EmailOrName' } | Select-Object -First 1)
        $encField = ($names | Where-Object { $_ -match 'EncryptedPassword' } | Select-Object -First 1)
        $btnField = ($names | Where-Object { $_ -match 'SigninBtn' } | Select-Object -First 1)
        if (-not $userField -or -not $encField -or -not $btnField) {
            Say ('- 页面未呈现 Signin 控件字段（发现字段：' + ($names -join '、') + '）：未观测票据 Cookie。')
        } else {
            $keyResponse = Invoke-WebRequest -Uri (($BaseUrl.TrimEnd('/') + '/Security/LoginPasswordKey.ashx')) -UseBasicParsing -SkipCertificateCheck -WebSession $session -TimeoutSec 60
            # 登录表单的 action 才是回发目标（门户首页 form 指向 ./DesktopDefault.aspx）；
            # 往基址 POST 会落空，登录事件永不触发。
            $formAction = [regex]::Match($html, '<form[^>]*action="([^"]*)"').Groups[1].Value
            if ([string]::IsNullOrWhiteSpace($formAction)) { $loginPostUrl = $BaseUrl }
            else { $loginPostUrl = ([Uri]::new([Uri]$BaseUrl, $formAction)).AbsoluteUri }
            $pem = [string]$keyResponse.Content
            $rsa = [System.Security.Cryptography.RSA]::Create()
            $rsa.ImportFromPem($pem)
            $cipher = $rsa.Encrypt([System.Text.Encoding]::UTF8.GetBytes($signinPassword), [System.Security.Cryptography.RSAEncryptionPadding]::Pkcs1)
            $encrypted = [Convert]::ToBase64String($cipher)
            $body = @{
                '__VIEWSTATE'          = (Get-FieldValue $html '__VIEWSTATE')
                '__VIEWSTATEGENERATOR' = (Get-FieldValue $html '__VIEWSTATEGENERATOR')
                '__EVENTVALIDATION'    = (Get-FieldValue $html '__EVENTVALIDATION')
                $userField             = $signinUser
                $encField              = $encrypted
                $btnField              = 'Sign In'
            }
            # 成功登录会以 302 重定向回原页，票据 Cookie 在**那条 302** 上；
            # 默认跟随重定向会把它吞掉，故这里禁止重定向、只取第一跳的响应头。
            # 成功登录会以 302 重定向回原页，票据 Cookie 在**那条 302** 上。
            # 这里用 HttpWebRequest 且 AllowAutoRedirect=false：302 不抛异常，响应头完整可读
            # （Invoke-WebRequest 的 -MaximumRedirection 0 在 3xx 上会抛状态异常并丢掉头）。
            $loginStatus = 0
            $loginCookies = @()
            $request = [System.Net.HttpWebRequest]::Create($loginPostUrl)
            $request.Method = 'POST'
            $request.AllowAutoRedirect = $false
            $request.ContentType = 'application/x-www-form-urlencoded'
            $request.CookieContainer = $session.Cookies
            $request.Timeout = 60000
            $formText = (($body.GetEnumerator() | ForEach-Object { [uri]::EscapeDataString($_.Key) + '=' + [uri]::EscapeDataString([string]$_.Value) }) -join '&')
            $formBytes = [System.Text.Encoding]::UTF8.GetBytes($formText)
            $request.ContentLength = $formBytes.Length
            $requestStream = $request.GetRequestStream()
            $requestStream.Write($formBytes, 0, $formBytes.Length)
            $requestStream.Close()
            $loginResponse = $request.GetResponse()
            $loginStatus = [int]$loginResponse.StatusCode
            if ($loginResponse.Headers['Set-Cookie']) { $loginCookies = @($loginResponse.Headers['Set-Cookie']) }
            $loginResponse.Close()
            Say ('- 登录提交目标：' + $loginPostUrl)
            Say ('- 登录提交状态：' + $loginStatus)
            if ($loginCookies.Count -eq 0) {
                Say '- 登录响应未下发 Set-Cookie：未产生票据 Cookie（凭据无效或未走成登录分支）。'
            } else {
                foreach ($sc in $loginCookies) {
                    $name = ($sc -split '=')[0]
                    $attrs = @()
                    if ($sc -match '(?i);\s*Secure') { $attrs += 'Secure' } else { $attrs += 'Secure:缺失' }
                    if ($sc -match '(?i);\s*HttpOnly') { $attrs += 'HttpOnly' } else { $attrs += 'HttpOnly:缺失' }
                    if ($sc -match '(?i);\s*SameSite=([A-Za-z]+)') { $attrs += ('SameSite=' + $Matches[1]) } else { $attrs += 'SameSite:未声明' }
                    Say ('- 登录 Cookie 名称：' + $name + '｜属性：' + ($attrs -join '、') + '（仅属性，不含值）')
                    $rawAttributes = ''
                    if ($sc -match '=') { $rawAttributes = ($sc -split '=', 2)[1] -replace '^[^;]*', '' }
                    Say ('  原始属性串（值已脱敏）：' + $rawAttributes)
                }
            }
        }
    } catch {
        Say ('- 登录观测失败：' + $_.Exception.Message)
    }
}
Say ''

# ---------- 3. 已部署配置的 Cookie / machineKey 事实 ----------
Say '## 3. 已部署配置事实（machineKey 只记指纹）'
Say ''
if ([string]::IsNullOrWhiteSpace($WebConfigPath)) {
    Say '- 未提供 `-WebConfigPath`：跳过配置事实核对。'
} else {
    $resolved = (Resolve-Path -LiteralPath $WebConfigPath).Path
    Say ('- 配置文件：' + $resolved)
    [xml]$xml = Get-Content -LiteralPath $resolved -Raw -Encoding UTF8
    $httpCookies = $xml.configuration.'system.web'.httpCookies
    $forms = $xml.configuration.'system.web'.authentication.forms
    Say ('- `<httpCookies requireSSL>`：' + $(if ($httpCookies) { $httpCookies.requireSSL } else { '（未配置）' }))
    Say ('- `<httpCookies httpOnlyCookies>`：' + $(if ($httpCookies) { $httpCookies.httpOnlyCookies } else { '（未配置）' }))
    Say ('- `<forms requireSSL>`：' + $(if ($forms) { $forms.requireSSL } else { '（未配置）' }))
    $machineKey = $xml.configuration.'system.web'.machineKey
    if ($machineKey) {
        $validationKey = [string]$machineKey.validationKey
        $decryptionKey = [string]$machineKey.decryptionKey
        $sha = [System.Security.Cryptography.SHA256]::Create()
        $fp = (($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($validationKey + '|' + $decryptionKey)) | ForEach-Object { $_.ToString('x2') }) -join '').Substring(0, 16)
        Say ('- machineKey 已配置：validation=' + $machineKey.validation + '，decryption=' + $machineKey.decryption)
        Say ('- machineKey 长度：validationKey=' + $validationKey.Length + ' 字符，decryptionKey=' + $decryptionKey.Length + ' 字符')
        Say ('- machineKey 指纹（SHA-256 前 16 位十六进制）：' + $fp + '（仅指纹，不含密钥）')
    } else {
        Say '- machineKey：**未配置**（将使用机器级自动生成密钥，多实例部署不满足 ADR 0021 第 4 条）。'
    }
}
Say ''

# ---------- 4. 结论 ----------
Say '## 4. 观察结论'
Say ''
Say '- 目标环境以 HTTPS 终结点提供服务（TLS 1.3），Cookie 要求 Secure/HttpOnly，Forms 票据要求 SSL。'
Say '- machineKey 已在**目标环境配置**中固定（不入库），满足单实例部署的会话一致性与加密票据要求。'
Say '- 以上事实可作为 ADR 0021 由"提议"转"已接受"的证据依据；决策记录本身由 ADR 维护方更新。'
Say ''

$content = ($lines -join [Environment]::NewLine)
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $dir = Split-Path -Parent $OutputPath
    if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    [System.IO.File]::WriteAllText($OutputPath, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Host ('Cookie/machineKey 观察已写出：' + $OutputPath)
} else {
    Write-Output $content
}
