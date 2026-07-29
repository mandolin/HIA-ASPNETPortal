<#
.SYNOPSIS
.LANG en
Runs Portal HTTP smoke checks.

.LANG zh-CN
执行 Portal HTTP smoke 检查。

.LANG en
Runs lightweight HTTP smoke checks against a local or supplied Portal base URL.
It can optionally start IIS Express, attempt authenticated admin checks when
credentials are supplied, and verify generic error, document safety, and editor
safety paths. Do not pass or log plaintext credentials; use SecureString input
for AdminPassword and keep any captured evidence free of cookies or tokens.

.LANG zh-CN
针对本地或指定的 Portal BaseUrl 执行轻量 HTTP smoke 检查。它可以按需启动
IIS Express，在提供凭据时尝试管理员认证检查，并验证通用错误页、文档安全和
编辑器安全路径。不要传入或记录明文凭据；AdminPassword 应使用 SecureString，
并确保采集证据中不包含 Cookie 或 Token。

.PARAMETER BaseUrl
.LANG en
Portal HTTP base URL.

.LANG zh-CN
Portal HTTP 基础地址。

.PARAMETER StartIISExpress
.LANG en
Starts IIS Express before running checks.

.LANG zh-CN
在执行检查前启动 IIS Express。

.PARAMETER StopWhenComplete
.LANG en
Stops the IIS Express instance started by this script after checks finish.

.LANG zh-CN
检查结束后停止由本脚本启动的 IIS Express 实例。

.PARAMETER AdminPassword
.LANG en
SecureString password used only for authenticated smoke checks.

.LANG zh-CN
仅用于认证 smoke 检查的 SecureString 密码。
#>
[CmdletBinding()]
param(
    [ValidatePattern('^https?://')]
    [string]$BaseUrl = 'http://localhost:40001/',

    [switch]$StartIISExpress,

    [switch]$StopWhenComplete,

    [string]$AdminUser,

    [SecureString]$AdminPassword,

    [switch]$SkipAuthenticated,

    [switch]$CheckGenericErrorPage,

    [switch]$CheckDocumentSafety,

    [switch]$CheckEditorSafety
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# <lang>
#   <zh-CN>维护本次 smoke 的唯一检查结果集合；各断言只通过 Add-PortalCheck 追加 Name、Passed 和 Detail，终端汇总据此计算失败数并决定是否抛错。Detail 必须保持为可公开的状态/路径摘要，不得传入响应正文、Cookie、口令或其他敏感数据。</zh-CN>
#   <en>Maintain the invocation's single collection of smoke check results; assertions append only Name, Passed, and Detail through Add-PortalCheck, and terminal aggregation uses it to compute failures and decide whether to throw. Detail must remain a shareable status/path summary and must not receive response bodies, cookies, passwords, or other sensitive data.</en>
# </lang>
$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>记录本脚本是否已成功启动 IIS Express，供 finally 的受限停止门禁使用；端口已有监听者或启动失败均保持 false，不主张停止所有权。</zh-CN>
#   <en>Record whether this script successfully started IIS Express for the finally restricted-stop gate; an existing port listener or startup failure remains false and does not claim stop ownership.</en>
# </lang>
$startedByScript = $false

# <lang>
#   <zh-CN>将单个 smoke 断言的公开结果追加到本次集合并输出简短状态行。该 helper 不重新执行断言、不改变 Passed，也不净化 Detail；调用方负责只提供非敏感摘要。</zh-CN>
#   <en>Append one smoke assertion's shareable result to this invocation's collection and emit a short status line. This helper neither re-executes the assertion nor changes Passed or sanitizes Detail; callers are responsible for supplying only non-sensitive summaries.</en>
# </lang>
function Add-PortalCheck {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail
    )

    # <lang>
    #   <zh-CN>以固定 Name、Passed、Detail 形状写入检查事实，保持终端失败筛选和自动化使用者的稳定结果契约；不保存响应对象、会话或异常细节。</zh-CN>
    #   <en>Write the check fact with the fixed Name, Passed, Detail shape, preserving the stable contract for terminal failure filtering and automation consumers; do not retain response objects, sessions, or exception details.</en>
    # </lang>
    $checks.Add([pscustomobject]@{
            Name = $Name
            Passed = $Passed
            Detail = $Detail
        })

    # <lang>
    #   <zh-CN>从既有布尔结果派生稳定 PASS 或 FAIL 前缀，仅用于人类可读输出，不影响集合中记录的原始 Passed 值。</zh-CN>
    #   <en>Derive the stable PASS or FAIL prefix from the established Boolean result solely for human-readable output; it does not affect the raw Passed value recorded in the collection.</en>
    # </lang>
    $prefix = if ($Passed) { 'PASS' } else { 'FAIL' }

    # <lang>
    #   <zh-CN>输出名称和调用方提供的公开摘要，便于本地运行观察；不输出完整响应、Cookie、口令或传输异常。</zh-CN>
    #   <en>Output the name and caller-provided shareable summary for local run visibility; do not output full responses, cookies, passwords, or transport exceptions.</en>
    # </lang>
    Write-Host ('[{0}] {1}: {2}' -f $prefix, $Name, $Detail)
}

# <lang>
#   <zh-CN>判断 URI 是否属于本脚本可启动 IIS Express 的固定本地 HTTP 地址集合：仅接受 HTTP 协议，以及 `localhost`、IPv4 loopback `127.0.0.1` 或 IPv6 loopback `::1`。该 helper 只检查已解析的 URI 字段，不解析 DNS、不连接网络也不启动进程；HTTPS、远程主机和其他本地别名均返回 false，避免脚本把自身不拥有的地址当作可启动站点。</zh-CN>
#   <en>Determines whether a URI belongs to the fixed local HTTP address set for which this script may start IIS Express: only the HTTP scheme and `localhost`, IPv4 loopback `127.0.0.1`, or IPv6 loopback `::1` are accepted. This helper inspects only already-parsed URI fields; it neither resolves DNS, connects to the network, nor starts a process. HTTPS, remote hosts, and other local aliases return false so the script does not treat an address it does not own as a startable site.</en>
# </lang>
function Test-LocalHttpUri {
    param([Uri]$Uri)

    # <lang>
    #   <zh-CN>必须同时满足 HTTP 与固定 loopback 主机 allowlist；不跟随重定向、不验证监听状态，也不把任意主机名解析后扩展为“本地”。</zh-CN>
    #   <en>Require both HTTP and the fixed loopback-host allowlist; do not follow redirects, verify listener state, or expand “local” by resolving arbitrary host names.</en>
    # </lang>
    return $Uri.Scheme -eq 'http' -and
        ($Uri.Host -ieq 'localhost' -or $Uri.Host -eq '127.0.0.1' -or $Uri.Host -eq '::1')
}

# <lang>
#   <zh-CN>以一次短生命周期 TCP 连接探测指定主机端口是否可接受连接。成功返回 true，连接或传输异常返回 false；该 helper 不启动服务、不发送应用数据，也不改变既有同步连接或超时行为，调用方负责后续启动与就绪策略。</zh-CN>
#   <en>Probes whether a specified host port accepts a connection through one short-lived TCP connection. It returns true on success and false on connection or transport failure; this helper neither starts a service nor sends application data, and it preserves the existing synchronous connection and timeout behavior while callers own subsequent startup and readiness policy.</en>
# </lang>
function Test-TcpPort {
    param(
        [string]$ServerHost,
        [int]$Port
    )

    # <lang>
    #   <zh-CN>为本次探测创建专用 TcpClient，不与 HTTP 会话或后续 smoke 请求共享连接状态。</zh-CN>
    #   <en>Create a dedicated TcpClient for this probe without sharing connection state with HTTP sessions or subsequent smoke requests.</en>
    # </lang>
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        # <lang>
        #   <zh-CN>按调用方指定的主机和端口执行既有同步连接；此处只验证可建立 TCP 连接，不验证 HTTP 响应或业务可用性。</zh-CN>
        #   <en>Perform the existing synchronous connection to the caller-specified host and port; this verifies only that a TCP connection can be established, not an HTTP response or business availability.</en>
        # </lang>
        $client.Connect($ServerHost, $Port)

        # <lang>
        #   <zh-CN>仅在 Connect 成功返回后报告端口监听事实，保持调用方的现有布尔门禁语义。</zh-CN>
        #   <en>Report the listening-port fact only after Connect returns successfully, preserving the caller's existing Boolean gate semantics.</en>
        # </lang>
        return $true
    }
    catch {
        # <lang>
        #   <zh-CN>将连接失败收敛为 false，不向 smoke 输出可能含有主机或传输细节的异常文本；不把失败误判为服务可用。</zh-CN>
        #   <en>Collapse connection failure to false without emitting exception text that could contain host or transport details; do not misclassify failure as service availability.</en>
        # </lang>
        return $false
    }
    finally {
        # <lang>
        #   <zh-CN>无论连接成功、失败或后续控制流如何离开，都释放本次专用 TcpClient，避免端口探测遗留套接字资源。</zh-CN>
        #   <en>Release the dedicated TcpClient regardless of successful connection, failure, or other control-flow exit so the port probe cannot leave socket resources behind.</en>
        # </lang>
        $client.Dispose()
    }
}

# <lang>
#   <zh-CN>查找命令行指向当前仓库 Portal 物理目录的一个 IIS Express 候选进程，供后续启动门禁判断既有站点是否占用该脚本范围。该 helper 只读取进程元数据，不启动、停止或终止任何进程；返回单个候选而非进程枚举。</zh-CN>
#   <en>Finds one IIS Express candidate process whose command line points to the current repository's Portal physical directory, so a later startup gate can determine whether an existing site occupies this script's scope. This helper only reads process metadata and neither starts, stops, nor terminates a process; it returns one candidate rather than a process enumeration.</en>
# </lang>
function Get-PortalIISExpressProcess {
    # <lang>
    #   <zh-CN>解析脚本控制的 Portal 目录为规范物理路径，避免以相对路径或通配符解释的路径判断进程归属。</zh-CN>
    #   <en>Resolve the script-controlled Portal directory to a canonical physical path so process ownership is not judged from relative paths or wildcard interpretation.</en>
    # </lang>
    $sitePath = (Resolve-Path -LiteralPath (Join-Path $repoRoot 'src/Portal')).Path

    # <lang>
    #   <zh-CN>将物理路径转义为正则字面量，确保路径中的点、反斜杠或其他正则字符不能扩大命令行匹配范围。</zh-CN>
    #   <en>Escape the physical path as a regular-expression literal so dots, backslashes, or other regex characters in the path cannot broaden command-line matching.</en>
    # </lang>
    $escapedSitePath = [regex]::Escape($sitePath)

    # <lang>
    #   <zh-CN>先由 CIM 限定为 IIS Express，再以已转义的站点路径筛选命令行并至多返回一个候选；未匹配或缺少命令行时自然返回空值，不推断进程可安全终止。</zh-CN>
    #   <en>First restrict CIM results to IIS Express, then filter command lines with the escaped site path and return at most one candidate; no match or a missing command line naturally returns null and does not imply a process is safe to terminate.</en>
    # </lang>
    return Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match $escapedSitePath } |
        Select-Object -First 1
}

# <lang>
#   <zh-CN>以调用方提供的 WebSession 统一执行 Portal HTTP GET 或 POST 请求，并将原始响应交还给各 smoke 断言。HTTP 非成功状态保留为可检查响应，传输或命令调用失败仍按 Stop 语义抛出；仅当 Body 非 null 时按现有 application/x-www-form-urlencoded 契约提交表单。该 helper 不创建会话、不跟随自定义认证策略、不记录 URI、正文、Cookie 或响应内容，调用方负责授权、敏感输入和结果判定。</zh-CN>
#   <en>Executes a Portal HTTP GET or POST consistently in the caller-provided WebSession and returns the raw response to individual smoke assertions. Non-success HTTP statuses remain inspectable responses, while transport or cmdlet failures still throw under Stop semantics; a form is submitted with the existing application/x-www-form-urlencoded contract only when Body is non-null. This helper does not create a session, apply a custom authentication policy, or log the URI, body, cookies, or response content; callers own authorization, sensitive input, and result interpretation.</en>
# </lang>
function Invoke-PortalRequest {
    param(
        [string]$Uri,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [ValidateSet('Get', 'Post')]
        [string]$Method = 'Get',
        [hashtable]$Body
    )

    # <lang>
    #   <zh-CN>构造仅供本次 Invoke-WebRequest 调用使用的参数表，保留调用方 URI、会话和已限制的 HTTP 方法；SkipHttpErrorCheck 使 smoke 能断言预期的 4xx/5xx 响应，而 ErrorAction Stop 仍让非 HTTP 传输失败进入调用方的既有异常路径。</zh-CN>
    #   <en>Build a parameter table used only for this Invoke-WebRequest call, preserving the caller URI, session, and restricted HTTP method; SkipHttpErrorCheck lets smoke assert expected 4xx/5xx responses, while ErrorAction Stop still sends non-HTTP transport failures into the caller's existing exception path.</en>
    # </lang>
    $parameters = @{
        Uri = $Uri
        Method = $Method
        WebSession = $WebSession
        SkipHttpErrorCheck = $true
        ErrorAction = 'Stop'
    }

    # <lang>
    #   <zh-CN>只有调用方实际提供正文时才附加表单正文和 ContentType；null 正文保留 GET 或无正文 POST 的既有参数形状，不构造或记录替代值。</zh-CN>
    #   <en>Attach a form body and ContentType only when the caller actually supplies a body; a null body preserves the existing parameter shape for GET or body-less POST and does not construct or log a substitute value.</en>
    # </lang>
    if ($null -ne $Body) {
        $parameters.Body = $Body
        $parameters.ContentType = 'application/x-www-form-urlencoded'
    }

    # <lang>
    #   <zh-CN>以参数 splatting 发出既有请求并原样返回响应，避免在公共 helper 中提前断言状态码、读取内容或泄露会话数据。</zh-CN>
    #   <en>Issue the established request through parameter splatting and return its response unchanged, avoiding premature status assertions, content reads, or session-data disclosure in this shared helper.</en>
    # </lang>
    return Invoke-WebRequest @parameters
}

# <lang>
#   <zh-CN>从 HTTP 响应的最终请求 URI 提取仅含路径的值，供 root site 与虚拟目录部署下的重定向断言使用。成功时不返回主机、查询、Cookie 或响应正文；响应形状不兼容时返回空字符串且不输出异常细节。该 helper 不验证重定向是否安全、同源、已授权或成功，调用方负责这些判定。</zh-CN>
#   <en>Extracts only the path from an HTTP response's final request URI for redirect assertions under root-site and virtual-directory deployment. On success it returns no host, query, cookie, or response body; an incompatible response shape returns an empty string without emitting exception details. This helper does not validate whether a redirect is safe, same-origin, authorized, or successful; callers own those determinations.</en>
# </lang>
function Get-PortalResponsePath {
    param($Response)

    try {
        # <lang>
        #   <zh-CN>仅沿既有响应链读取最终 URI 的 AbsolutePath，刻意丢弃 authority 和 query，避免路径断言依赖环境主机名或无关查询参数。</zh-CN>
        #   <en>Read only AbsolutePath from the established response chain's final URI, deliberately discarding authority and query so path assertions do not depend on environment host names or unrelated query parameters.</en>
        # </lang>
        return $Response.BaseResponse.RequestMessage.RequestUri.AbsolutePath
    }
    catch {
        # <lang>
        #   <zh-CN>响应对象缺少任一嵌套成员时收敛为稳定空字符串，避免诊断/传输细节写入 smoke 输出；空值由各调用方按其路径断言视为未匹配。</zh-CN>
        #   <en>Collapse a response lacking any nested member to a stable empty string, avoiding diagnostic or transport-detail output; each caller treats the empty value as no match for its own path assertion.</en>
        # </lang>
        return ''
    }
}

# <lang>
#   <zh-CN>在 smoke 请求开始前有界轮询指定 URI 的 HTTP 就绪状态。调用方可提供重试上限，默认最多尝试 20 次；仅收到 HTTP 200 才正常返回。每次尝试使用独立 WebSession，并且不启动或停止服务、不复用认证会话；非 200 或传输异常会继续按既有一秒间隔重试，耗尽后抛出固定超时异常而不暴露传输细节。</zh-CN>
#   <en>Boundedly polls the specified URI for HTTP readiness before smoke requests begin. The caller may provide a retry limit, with at most 20 attempts by default; only HTTP 200 returns normally. Each attempt uses an independent WebSession, and this helper neither starts nor stops a service nor reuses an authentication session. A non-200 response or transport exception continues retrying at the existing one-second interval, and exhaustion throws a fixed timeout exception without exposing transport details.</en>
# </lang>
function Wait-PortalReady {
    param(
        [string]$Uri,
        [int]$RetryCount = 20
    )

    # <lang>
    #   <zh-CN>从 1 迭代至调用方上限，保留当前有界轮询次数；若上限不为正，循环不执行并直接进入既有超时路径。</zh-CN>
    #   <en>Iterate from 1 through the caller-supplied limit, preserving the current bounded poll count; a non-positive limit performs no attempts and proceeds directly to the existing timeout path.</en>
    # </lang>
    for ($attempt = 1; $attempt -le $RetryCount; $attempt++) {
        try {
            # <lang>
            #   <zh-CN>为本次就绪探测创建独立 WebSession，避免临时响应 Cookie 或连接状态影响后续匿名或认证 smoke 会话。</zh-CN>
            #   <en>Create an independent WebSession for this readiness probe so temporary response cookies or connection state cannot affect subsequent anonymous or authenticated smoke sessions.</en>
            # </lang>
            $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

            # <lang>
            #   <zh-CN>复用统一请求 helper 访问调用方 URI；该请求仅提供本轮就绪观察，不传递或保存认证凭据。</zh-CN>
            #   <en>Use the shared request helper for the caller URI; this request provides only this round's readiness observation and neither supplies nor retains authentication credentials.</en>
            # </lang>
            $response = Invoke-PortalRequest -Uri $Uri -WebSession $session

            # <lang>
            #   <zh-CN>只有 HTTP 200 是可继续执行 smoke 的就绪事实；其他 HTTP 状态不会被误判为 ready，而是保留给下方重试间隔处理。</zh-CN>
            #   <en>Only HTTP 200 is the readiness fact that permits smoke execution to continue; other HTTP statuses are not misclassified as ready and remain subject to the retry interval below.</en>
            # </lang>
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            # <lang>
            #   <zh-CN>启动、首次编译或瞬态传输故障都可能使本轮请求失败；保持异常细节不写入输出，并将结果交给既有重试路径，而不把失败当作就绪。</zh-CN>
            #   <en>Startup, first-request compilation, or transient transport failure can make this round fail; keep exception details out of output and defer to the existing retry path rather than treating failure as readiness.</en>
            # </lang>
        }

        # <lang>
        #   <zh-CN>在每个未成功的轮次（包括最后一轮）后保持一秒等待，避免紧密轮询改变 IIS Express 启动节奏或现有超时上限。</zh-CN>
        #   <en>Keep the one-second wait after every unsuccessful round, including the final one, so tight polling does not change IIS Express startup pacing or the existing timeout bound.</en>
        # </lang>
        Start-Sleep -Seconds 1
    }

    # <lang>
    #   <zh-CN>尝试耗尽后使用固定、无端点或传输细节的异常终止 smoke；调用方据此停止后续 HTTP 检查。</zh-CN>
    #   <en>After attempts are exhausted, stop smoke processing with the fixed exception that contains no endpoint or transport detail; callers consequently do not run subsequent HTTP checks.</en>
    # </lang>
    throw 'Portal did not become ready before the smoke-test timeout.'
}

# <lang>
#   <zh-CN>从调用方已提取的单个标记文本中读取一个属性值，供受控 Web Forms 登录表单发现使用。属性名先转义为正则字面量，支持大小写不敏感的单双引号或无引号值，并对捕获值执行 HTML 实体解码；属性不存在时返回 null。该 helper 不解析完整 HTML 文档、不验证标记结构、不过滤或净化返回值，调用方必须只将结果用于既定表单契约。</zh-CN>
#   <en>Reads one attribute value from a single tag text already extracted by the caller for controlled Web Forms sign-in form discovery. The attribute name is escaped as a regex literal, case-insensitive double-quoted, single-quoted, and unquoted values are supported, and the captured value is HTML-entity decoded; a missing attribute returns null. This helper does not parse a complete HTML document, validate tag structure, filter, or sanitize the returned value, so callers must use it only for the established form contract.</en>
# </lang>
function Get-HtmlAttribute {
    param(
        [string]$Tag,
        [string]$Name
    )

    # <lang>
    #   <zh-CN>将调用方属性名转义为正则字面量，再组合属性边界、可选空白和三种既有值形式；转义确保属性名中的正则元字符不能扩大匹配范围。</zh-CN>
    #   <en>Escape the caller-supplied attribute name as a regex literal, then compose the attribute boundary, optional whitespace, and the three existing value forms; escaping prevents regex metacharacters in the name from broadening the match.</en>
    # </lang>
    $pattern = '\b' + [regex]::Escape($Name) + '\s*=\s*(?:"(?<value>[^"]*)"|''(?<value>[^'']*)''|(?<value>[^\s>]+))'

    # <lang>
    #   <zh-CN>在单个标记文本中执行大小写不敏感匹配；不跨标记搜索，也不将原始页面内容当作可安全解析的 HTML 文档。</zh-CN>
    #   <en>Perform a case-insensitive match within the single tag text; do not search across tags or treat raw page content as a safely parsed HTML document.</en>
    # </lang>
    $match = [regex]::Match($Tag, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    # <lang>
    #   <zh-CN>缺失目标属性以 null 表示，保留调用方区分“未提供”与显式空值的既有契约，不猜测替代属性。</zh-CN>
    #   <en>Represent a missing target attribute as null, preserving the caller's existing distinction between absent and explicitly empty values without guessing an alternative attribute.</en>
    # </lang>
    if (-not $match.Success) {
        return $null
    }

    # <lang>
    #   <zh-CN>仅对已捕获属性值执行 HTML 实体解码，使 Web Forms 编码的 name、id 或 value 能按原有契约参与后续比较或 POST；解码不是验证、净化或输出编码。</zh-CN>
    #   <en>HTML-entity decode only the captured attribute value so Web Forms-encoded name, id, or value data can participate in the existing comparisons or POST contract; decoding is not validation, sanitization, or output encoding.</en>
    # </lang>
    return [System.Net.WebUtility]::HtmlDecode($match.Groups['value'].Value)
}

# <lang>
#   <zh-CN>按稳定控件 id 后缀从页面中的 input 标记顺序查找第一个匹配标记，供登录表单字段发现使用。匹配使用经 Get-HtmlAttribute 解码的 id 与序号不敏感后缀比较；未找到时返回 null。该 helper 只作受限字符串扫描，不验证完整页面、不过滤 HTML，也不输出或提交找到的标记。</zh-CN>
#   <en>Finds the first input tag whose stable control-id suffix matches in page order for sign-in form field discovery. Matching uses the id decoded by Get-HtmlAttribute and an ordinal case-insensitive suffix comparison; no match returns null. This helper performs only a constrained string scan: it does not validate a complete page, filter HTML, or output or submit the tag it finds.</en>
# </lang>
function Get-InputTagByIdSuffix {
    param(
        [string]$Html,
        [string]$IdSuffix
    )

    # <lang>
    #   <zh-CN>按文档出现顺序枚举既有正则可识别的 input 标记；该范围有意限于登录页控件发现，不扩大为通用 HTML 解析。</zh-CN>
    #   <en>Enumerate input tags recognized by the existing regex in document order; this scope is intentionally limited to login-page control discovery and is not expanded into general HTML parsing.</en>
    # </lang>
    foreach ($match in [regex]::Matches($Html, '<input\b[^>]*>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        # <lang>
        #   <zh-CN>保留当前候选的原始单标记文本，仅交给受限属性 helper 提取 id；不记录或修改页面内容。</zh-CN>
        #   <en>Keep the current candidate's raw single-tag text solely for the constrained attribute helper to extract id; do not log or modify page content.</en>
        # </lang>
        $tag = $match.Value

        # <lang>
        #   <zh-CN>读取候选 id，缺失 id 保持 null 并自然不参与后缀匹配。</zh-CN>
        #   <en>Read the candidate id; a missing id remains null and naturally does not participate in suffix matching.</en>
        # </lang>
        $id = Get-HtmlAttribute -Tag $tag -Name 'id'

        # <lang>
        #   <zh-CN>仅在存在非空 id 且其稳定后缀与调用方预期相符时返回第一个候选；保持大小写不敏感的控件兼容性和首个匹配优先级。</zh-CN>
        #   <en>Return the first candidate only when a non-empty id has the caller-expected stable suffix; preserve case-insensitive control compatibility and first-match precedence.</en>
        # </lang>
        if ($id -and $id.EndsWith($IdSuffix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $tag
        }
    }

    # <lang>
    #   <zh-CN>没有匹配控件时返回 null，让认证调用方统一按页面契约缺失失败，而不猜测生成 id 或替代输入框。</zh-CN>
    #   <en>Return null when no control matches so the authentication caller can fail consistently for a missing page contract rather than guessing generated ids or alternate inputs.</en>
    # </lang>
    return $null
}

# <lang>
#   <zh-CN>从页面 input 标记收集可提交的 hidden 字段，保留 Web Forms 的 ViewState 等既有往返契约。只接受 type 为 hidden 且 name 非空白的字段；属性值沿用 Get-HtmlAttribute 的实体解码，重复 name 按页面顺序由后者覆盖前者。结果仅供同次登录 POST 构造使用，不记录、不验证字段真实性，也不是通用 HTML 解析、输入净化或字段 allowlist。</zh-CN>
#   <en>Collects submittable hidden fields from page input tags to preserve existing Web Forms round-trip contracts such as ViewState. Only fields whose type is hidden and whose name is non-blank are accepted; attribute values retain Get-HtmlAttribute entity decoding, and a later page-order duplicate name overwrites an earlier one. The result is used only to construct the same sign-in POST: it is not logged, does not validate field authenticity, and is not general HTML parsing, input sanitization, or a field allowlist.</en>
# </lang>
function Get-HiddenFormFields {
    param([string]$Html)

    # <lang>
    #   <zh-CN>创建本次调用专用的可变 hashtable，用于按字段名收集隐藏输入；不会共享到其他会话或登录尝试。</zh-CN>
    #   <en>Create a mutable hashtable dedicated to this invocation for collecting hidden inputs by field name; it is not shared with other sessions or sign-in attempts.</en>
    # </lang>
    $fields = @{}

    # <lang>
    #   <zh-CN>按页面顺序枚举既有正则可识别的 input 标记，保持重复字段的既有后写覆盖语义。</zh-CN>
    #   <en>Enumerate input tags recognized by the existing regex in page order, preserving the existing later-write-wins behavior for duplicate fields.</en>
    # </lang>
    foreach ($match in [regex]::Matches($Html, '<input\b[^>]*>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        # <lang>
        #   <zh-CN>保留当前原始标记文本，以受限属性 helper 提取 type、name 和 value；不记录或修改页面内容。</zh-CN>
        #   <en>Keep the current raw tag text so the constrained attribute helper can extract type, name, and value; do not log or modify page content.</en>
        # </lang>
        $tag = $match.Value

        # <lang>
        #   <zh-CN>读取 type 并以序号不敏感比较识别 hidden；缺失或其他类型均不进入提交字段集合。</zh-CN>
        #   <en>Read type and identify hidden with an ordinal case-insensitive comparison; a missing type or any other type does not enter the submitted field collection.</en>
        # </lang>
        $type = Get-HtmlAttribute -Tag $tag -Name 'type'

        # <lang>
        #   <zh-CN>跳过非 hidden 标记，避免用户名、口令和提交按钮等可见控件被误复制到隐藏字段 POST 基础集合。</zh-CN>
        #   <en>Skip non-hidden tags so visible controls such as user name, password, and submit button are not inadvertently copied into the hidden-field POST base collection.</en>
        # </lang>
        if (-not [string]::Equals($type, 'hidden', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        # <lang>
        #   <zh-CN>读取提交字段名；name 为空白时不创建不可寻址的 hashtable 键，也不猜测字段名称。</zh-CN>
        #   <en>Read the submission field name; do not create an unaddressable hashtable key when name is blank, and do not guess a field name.</en>
        # </lang>
        $name = Get-HtmlAttribute -Tag $tag -Name 'name'

        # <lang>
        #   <zh-CN>跳过缺失或空白 name，保持登录表单只携带具名 hidden 字段的既有契约。</zh-CN>
        #   <en>Skip a missing or blank name, preserving the existing contract that the login form carries only named hidden fields.</en>
        # </lang>
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        # <lang>
        #   <zh-CN>以解码后的 name 作为键写入解码后的 value；若页面后续标记使用相同名称则覆盖前值，保留既有页面顺序语义。value 缺失可保持 null，交由后续 POST 契约处理。</zh-CN>
        #   <en>Write the decoded value under the decoded name; a later page tag with the same name overwrites the previous value, preserving existing page-order semantics. A missing value may remain null for the subsequent POST contract to handle.</en>
        # </lang>
        $fields[$name] = Get-HtmlAttribute -Tag $tag -Name 'value'
    }

    # <lang>
    #   <zh-CN>返回仅含已筛选 hidden 字段的本次 hashtable；不在此处提交、记录或复用字段。</zh-CN>
    #   <en>Return the invocation's hashtable containing only the filtered hidden fields; do not submit, log, or reuse fields here.</en>
    # </lang>
    return $fields
}

# <lang>
#   <zh-CN>在调用方提供的会话中提交 Web Forms 管理员登录，并仅以认证 Cookie 是否写入该会话判断成功。该 helper 读取当前登录页的受控字段契约，口令只在提交窗口内从 SecureString 还原为 BSTR，且无论请求结果如何均在 finally 中释放；不输出口令、表单内容或 Cookie。</zh-CN>
#   <en>Submits the Web Forms administrator sign-in in the caller-provided session and judges success only by whether the authentication cookie is written to that session. This helper reads the current login page's controlled field contract, materializes the password from SecureString to BSTR only during the submission window, and releases it in finally regardless of request outcome; it does not output the password, form content, or cookies.</en>
# </lang>
function Invoke-PortalAdminLogin {
    param(
        [string]$LoginUri,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [string]$UserName,
        [SecureString]$Password
    )

    # <lang>
    #   <zh-CN>在同一调用方会话中读取登录页，以保留该站点已有的会话状态和 Web Forms 隐藏字段来源。</zh-CN>
    #   <en>Read the login page in the same caller-provided session to preserve existing site session state and the source of Web Forms hidden fields.</en>
    # </lang>
    $loginPage = Invoke-PortalRequest -Uri $LoginUri -WebSession $WebSession

    # <lang>
    #   <zh-CN>记录最终响应路径，用于兼容登录入口在根站点或虚拟目录中发生的受控重定向。</zh-CN>
    #   <en>Capture the final response path to support controlled redirection of the login entry under either a root site or virtual directory.</en>
    # </lang>
    $loginPagePath = Get-PortalResponsePath -Response $loginPage

    # <lang>
    #   <zh-CN>若响应未提供可用路径则保留调用方登录地址；否则仅以该地址为基准组合站内相对路径，不接受外部目标。</zh-CN>
    #   <en>Keep the caller's login address when the response provides no usable path; otherwise combine only the site-relative path against that address and do not accept an external target.</en>
    # </lang>
    $resolvedLoginUri = if ([string]::IsNullOrWhiteSpace($loginPagePath)) {
        $LoginUri
    }
    else {
        [Uri]::new([Uri]$LoginUri, $loginPagePath).AbsoluteUri
    }

    # <lang>
    #   <zh-CN>按稳定控件标识后缀定位用户名、口令和提交按钮标签，避免硬编码整页生成 ID。</zh-CN>
    #   <en>Locate the user-name, password, and submit-button tags by stable control-id suffixes instead of hard-coding whole-page generated IDs.</en>
    # </lang>
    $userTag = Get-InputTagByIdSuffix -Html $loginPage.Content -IdSuffix 'EmailOrName'
    $passwordTag = Get-InputTagByIdSuffix -Html $loginPage.Content -IdSuffix 'password'
    $buttonTag = Get-InputTagByIdSuffix -Html $loginPage.Content -IdSuffix 'SigninBtn'

    # <lang>
    #   <zh-CN>从每个已发现标签提取 Web Forms 提交名称；缺失标签保持 null，随后统一按页面契约失败处理。</zh-CN>
    #   <en>Extract the Web Forms submission name from each discovered tag; a missing tag remains null and is subsequently handled as one page-contract failure.</en>
    # </lang>
    $userField = if ($userTag) { Get-HtmlAttribute -Tag $userTag -Name 'name' } else { $null }
    $passwordField = if ($passwordTag) { Get-HtmlAttribute -Tag $passwordTag -Name 'name' } else { $null }
    $buttonField = if ($buttonTag) { Get-HtmlAttribute -Tag $buttonTag -Name 'name' } else { $null }

    # <lang>
    #   <zh-CN>任一必需提交字段缺失均停止认证尝试；不猜测替代字段，也不提交不完整表单或回显页面内容。</zh-CN>
    #   <en>Stop the authentication attempt when any required submission field is absent; do not guess alternate fields, submit an incomplete form, or echo page content.</en>
    # </lang>
    if ([string]::IsNullOrWhiteSpace($userField) -or
        [string]::IsNullOrWhiteSpace($passwordField) -or
        [string]::IsNullOrWhiteSpace($buttonField)) {
        throw 'The sign-in form no longer exposes the expected Web Forms fields.'
    }

    # <lang>
    #   <zh-CN>复制登录页中的隐藏字段以保持 ViewState 等 Web Forms 往返契约；该 hashtable 只用于本次 POST，绝不写入日志。</zh-CN>
    #   <en>Copy hidden fields from the login page to preserve round-trip contracts such as ViewState; this hashtable is used only for this POST and is never logged.</en>
    # </lang>
    $form = Get-HiddenFormFields -Html $loginPage.Content

    # <lang>
    #   <zh-CN>以零指针初始化非托管口令句柄，供 finally 判断是否确实需要清零并释放。</zh-CN>
    #   <en>Initialize the unmanaged password handle to a zero pointer so finally can determine whether zeroing and release are actually required.</en>
    # </lang>
    $passwordBstr = [IntPtr]::Zero
    try {
        # <lang>
        #   <zh-CN>仅在构造本次 POST 表单时将 SecureString 转为 BSTR；控制流离开本块后由 finally 清零并释放该非托管缓冲区。</zh-CN>
        #   <en>Convert SecureString to BSTR only while constructing this POST form; finally zeroes and releases the unmanaged buffer after control leaves this block.</en>
        # </lang>
        $passwordBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)

        # <lang>
        #   <zh-CN>按当前页面发现的字段名称写入用户名和短暂还原的口令；不使用固定字段名，也不将敏感值写入输出。</zh-CN>
        #   <en>Write the user name and temporarily materialized password under the field names discovered from the current page; do not use fixed field names or write sensitive values to output.</en>
        # </lang>
        $form[$userField] = $UserName
        $form[$passwordField] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBstr)

        # <lang>
        #   <zh-CN>保留既有图像按钮坐标字段，满足该 Web Forms 提交按钮的协议而不改变点击语义。</zh-CN>
        #   <en>Retain the established image-button coordinate fields to satisfy this Web Forms submit-button protocol without changing click semantics.</en>
        # </lang>
        $form[$buttonField + '.x'] = '1'
        $form[$buttonField + '.y'] = '1'

        # <lang>
        #   <zh-CN>在同一会话中提交表单以接收认证状态；响应内容不参与断言，也不输出到调用方。</zh-CN>
        #   <en>Submit the form in the same session to receive authentication state; response content is neither asserted nor emitted to the caller.</en>
        # </lang>
        [void](Invoke-PortalRequest -Uri $resolvedLoginUri -WebSession $WebSession -Method Post -Body $form)
    }
    finally {
        # <lang>
        #   <zh-CN>仅当 BSTR 已成功分配时清零并释放，确保请求或字段处理失败也不会跳过敏感非托管缓冲区清理。</zh-CN>
        #   <en>Zero and release the BSTR only when allocation succeeded, ensuring a request or field-processing failure cannot skip cleanup of the sensitive unmanaged buffer.</en>
        # </lang>
        if ($passwordBstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBstr)
        }
    }

    # <lang>
    #   <zh-CN>只检查调用方会话的 Cookie jar 是否收到既有认证 Cookie；返回布尔事实，不返回或记录 Cookie 值。</zh-CN>
    #   <en>Check only whether the caller session's cookie jar received the established authentication cookie; return a Boolean fact without returning or recording a cookie value.</en>
    # </lang>
    $cookies = $WebSession.Cookies.GetCookies([Uri]$resolvedLoginUri)
    return [bool]($cookies | Where-Object { $_.Name -eq '.ASPXAUTH' })
}

# <lang>
#   <zh-CN>仅允许停止本脚本显式请求启动 IIS Express 的流程，避免 StopWhenComplete 在未取得启动所有权时暗示可停止既有服务。</zh-CN>
#   <en>Allow stopping only in a flow that explicitly requests IIS Express startup, preventing StopWhenComplete from implying authority to stop an existing service when startup ownership was never requested.</en>
# </lang>
if ($StopWhenComplete -and -not $StartIISExpress) {
    throw 'StopWhenComplete is valid only when StartIISExpress is also specified.'
}

# <lang>
#   <zh-CN>当调用方同时提供管理员用户和显式跳过认证开关时保留信息提示；这不是参数冲突，也不会读取口令或执行认证请求。</zh-CN>
#   <en>Retain the informational notice when the caller supplies an admin user and explicitly skips authentication; this is not a parameter conflict and neither reads a password nor performs an authentication request.</en>
# </lang>
if (-not [string]::IsNullOrWhiteSpace($AdminUser) -and $SkipAuthenticated) {
    Write-Host '[INFO] Authenticated smoke checks were explicitly skipped.'
}

# <lang>
#   <zh-CN>拒绝没有用户名的口令输入，避免孤立的 SecureString 进入后续流程或触发交互式认证语义；不回显或转换口令。</zh-CN>
#   <en>Reject password input without a user name so an orphaned SecureString cannot enter later flow or trigger interactive authentication semantics; do not echo or convert the password.</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($AdminUser) -and $null -ne $AdminPassword) {
    throw 'AdminPassword requires AdminUser.'
}

# <lang>
#   <zh-CN>将调用方 BaseUrl 解析为 URI，供后续绝对地址、主机和端口判断共用；此处只解析文本，不连接网络或启动服务。</zh-CN>
#   <en>Parse the caller BaseUrl as a URI for shared subsequent absolute-address, host, and port decisions; this only parses text and neither connects to the network nor starts a service.</en>
# </lang>
$baseUri = [Uri]$BaseUrl

# <lang>
#   <zh-CN>要求绝对 URI，确保后续就绪、端口和相对路径组合都有明确的 scheme、host 与 port；不以相对地址推断本地目标。</zh-CN>
#   <en>Require an absolute URI so subsequent readiness, port, and relative-path composition have an explicit scheme, host, and port; do not infer a local target from a relative address.</en>
# </lang>
if (-not $baseUri.IsAbsoluteUri) {
    throw 'BaseUrl must be an absolute HTTP or HTTPS URI.'
}

# <lang>
#   <zh-CN>只有调用方显式请求时才进入本地 IIS Express 启动协调；未设置该开关时，脚本不会在此处探测端口、枚举进程或启动服务。</zh-CN>
#   <en>Enter local IIS Express startup coordination only when the caller explicitly requests it; without this switch, the script neither probes ports, enumerates processes, nor starts a service here.</en>
# </lang>
if ($StartIISExpress) {
    # <lang>
    #   <zh-CN>在任何端口或进程操作前限制为本地 HTTP loopback 地址，拒绝 HTTPS、远程地址和其他本地别名，避免脚本把启动行为指向不归其所有的终结点。</zh-CN>
    #   <en>Restrict to a local HTTP loopback address before any port or process operation, rejecting HTTPS, remote addresses, and other local aliases so startup cannot target an endpoint outside the script's ownership.</en>
    # </lang>
    if (-not (Test-LocalHttpUri -Uri $baseUri)) {
        throw 'StartIISExpress only supports a local HTTP BaseUrl.'
    }

    # <lang>
    #   <zh-CN>记录目标主机端口当前是否接受 TCP 连接；true 仅表示存在监听者，不证明其属于 Portal 或本脚本，且不在此处发送 HTTP 请求。</zh-CN>
    #   <en>Record whether the target host port currently accepts a TCP connection; true means only that a listener exists, not that it belongs to Portal or this script, and no HTTP request is sent here.</en>
    # </lang>
    $portAlreadyListening = Test-TcpPort -ServerHost $baseUri.Host -Port $baseUri.Port

    # <lang>
    #   <zh-CN>读取指向当前仓库 Portal 物理目录的 IIS Express 候选，用于区分“未监听目标端口”与“同一站点已在另一端口运行”；该查询不终止或接管进程。</zh-CN>
    #   <en>Read the IIS Express candidate pointing to the current repository's Portal physical directory to distinguish “target port is not listening” from “the same site is already running on another port”; this query neither terminates nor takes ownership of a process.</en>
    # </lang>
    $existingPortalProcess = Get-PortalIISExpressProcess

    # <lang>
    #   <zh-CN>若目标端口未监听而 Portal 候选已存在，则拒绝启动第二个端口实例，要求调用方显式处理既有实例；端口已监听时保持既有行为，不在此处推断监听者归属。</zh-CN>
    #   <en>When the target port is not listening but a Portal candidate already exists, refuse to start a second-port instance and require the caller to handle the existing instance explicitly; when the port is listening, preserve existing behavior and do not infer listener ownership here.</en>
    # </lang>
    if (-not $portAlreadyListening -and $existingPortalProcess) {
        throw 'Portal IIS Express is already running on a different port. Stop that instance explicitly before requesting a new port.'
    }

    # <lang>
    #   <zh-CN>仅在目标端口未监听时调用既有启动脚本并传入解析后的端口；若端口已有监听者则不启动、不接管，也不设置本脚本所有权。</zh-CN>
    #   <en>Call the established startup script with the parsed port only when the target port is not listening; if a listener already exists, do not start, take over, or mark script ownership.</en>
    # </lang>
    if (-not $portAlreadyListening) {
        & (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Port $baseUri.Port

        # <lang>
        #   <zh-CN>只在启动脚本成功返回后记录本脚本启动事实，供 finally 中的 StopWhenComplete 清理门禁使用；启动失败不会错误标记可停止所有权。</zh-CN>
        #   <en>Record that this script started IIS Express only after the startup script returns successfully, for the finally StopWhenComplete cleanup gate; a startup failure cannot incorrectly mark stoppable ownership.</en>
        # </lang>
        $startedByScript = $true
    }
}

# <lang>
#   <zh-CN>在完成可选启动协调后开始实际 HTTP smoke；无论后续匿名或可选检查成功、失败或抛错，finally 都保留已启动实例的受限清理职责。</zh-CN>
#   <en>Begin actual HTTP smoke after optional startup coordination; whether subsequent anonymous or optional checks succeed, fail, or throw, finally retains the restricted cleanup responsibility for a started instance.</en>
# </lang>
try {
    # <lang>
    #   <zh-CN>先等待 BaseUrl 达到既有 HTTP 200 就绪条件，避免在服务尚未可用时把后续匿名断言误记录为页面回归；超时按 Wait-PortalReady 的固定异常终止本次 smoke。</zh-CN>
    #   <en>First wait for BaseUrl to meet the established HTTP 200 readiness condition, avoiding recording subsequent anonymous assertions as page regressions while the service is unavailable; timeout terminates this smoke under Wait-PortalReady's fixed exception.</en>
    # </lang>
    Wait-PortalReady -Uri $baseUri.AbsoluteUri

    # <lang>
    #   <zh-CN>创建独立且无认证状态的匿名会话，供首页、后台拒绝和可选错误页检查共享；不复用管理员会话或写入登录 Cookie。</zh-CN>
    #   <en>Create an independent session with no authentication state, shared by the home, admin-denial, and optional error-page checks; do not reuse an administrator session or write sign-in cookies.</en>
    # </lang>
    $anonymousSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession

    # <lang>
    #   <zh-CN>从 BaseUrl 组合固定首页路径并在匿名会话中请求，验证门户基本可访问性；不读取或输出首页正文。</zh-CN>
    #   <en>Combine the fixed home path with BaseUrl and request it in the anonymous session to verify basic portal reachability; do not read or output home-page content.</en>
    # </lang>
    $homeResponse = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, 'Default.aspx').AbsoluteUri) -WebSession $anonymousSession

    # <lang>
    #   <zh-CN>仅以 HTTP 200 记录首页通过事实，保持既有不依赖主题、正文或重定向内容的基础可用性断言。</zh-CN>
    #   <en>Record home-page success solely from HTTP 200, preserving the established basic-availability assertion without dependence on theme, content, or redirect-body details.</en>
    # </lang>
    Add-PortalCheck -Name 'Home page' -Passed ($homeResponse.StatusCode -eq 200) -Detail ('HTTP ' + $homeResponse.StatusCode)

    # <lang>
    #   <zh-CN>构造固定后台系统健康页地址，作为匿名访问受保护后台资源的代表性检查；该 URI 不携带用户输入或诊断参数。</zh-CN>
    #   <en>Construct the fixed admin system-health address as the representative anonymous access check for a protected backend resource; this URI carries no user input or diagnostic parameter.</en>
    # </lang>
    $healthUri = [Uri]::new($baseUri, 'Admin/SystemHealth.aspx').AbsoluteUri

    # <lang>
    #   <zh-CN>使用同一匿名会话请求受保护页，使 Web Forms 的既有重定向行为可以在后续最终路径断言中被观察；不向会话添加认证状态。</zh-CN>
    #   <en>Request the protected page in the same anonymous session so established Web Forms redirect behavior can be observed by the subsequent final-path assertion; do not add authentication state to the session.</en>
    # </lang>
    $anonymousHealth = Invoke-PortalRequest -Uri $healthUri -WebSession $anonymousSession

    # <lang>
    #   <zh-CN>读取响应最终路径而非初始请求路径，兼容根站点或虚拟目录下到拒绝页的既有重定向；不把路径本身视为授权证明。</zh-CN>
    #   <en>Read the response's final path rather than the initial request path, accommodating established redirects to denial pages under root-site or virtual-directory hosting; do not treat the path itself as proof of authorization.</en>
    # </lang>
    $anonymousHealthPath = Get-PortalResponsePath -Response $anonymousHealth

    # <lang>
    #   <zh-CN>仅当响应为 HTTP 200 且最终路径为两种既有 AccessDenied 页面之一时，认定匿名后台保护通过；其他状态、路径或空路径均不误判为受保护。</zh-CN>
    #   <en>Consider anonymous backend protection passed only when the response is HTTP 200 and the final path is one of the two established AccessDenied pages; any other status, path, or empty path is not misclassified as protected.</en>
    # </lang>
    $denied = $anonymousHealth.StatusCode -eq 200 -and
        $anonymousHealthPath -match '/Admin/(AccessDenied|EditAccessDenied)\.aspx$'

    # <lang>
    #   <zh-CN>以状态和最终路径的公开摘要记录该访问控制事实，不输出受保护页内容或会话数据。</zh-CN>
    #   <en>Record this access-control fact with a shareable status and final-path summary, without outputting protected-page content or session data.</en>
    # </lang>
    Add-PortalCheck -Name 'Anonymous admin protection' -Passed $denied -Detail ('HTTP ' + $anonymousHealth.StatusCode + '; final path ' + $anonymousHealthPath)

    # <lang>
    #   <zh-CN>仅在调用方显式请求时执行通用错误页检查，避免默认 smoke 增加额外缺失资源请求；该开关不改变其它匿名基线断言。</zh-CN>
    #   <en>Run the generic-error-page check only when the caller explicitly requests it, avoiding an extra missing-resource request in default smoke; this switch does not change the other anonymous baseline assertions.</en>
    # </lang>
    if ($CheckGenericErrorPage) {
        # <lang>
        #   <zh-CN>以固定前缀和随机 GUID 构造本次唯一的不存在页面路径，降低与真实资源、先前运行或缓存状态碰撞的可能；仅发出 GET，不创建或删除文件。</zh-CN>
        #   <en>Construct this invocation's unique nonexistent-page path from the fixed prefix and a random GUID, reducing collision with real resources, earlier runs, or cache state; issue only a GET and create or delete no file.</en>
        # </lang>
        $missingUri = [Uri]::new($baseUri, ('P25SmokeMissing-' + [Guid]::NewGuid().ToString('N') + '.aspx')).AbsoluteUri

        # <lang>
        #   <zh-CN>在同一匿名会话中请求该缺失路径，观察统一错误处理的现有响应，而不提交表单或认证信息。</zh-CN>
        #   <en>Request the missing path in the same anonymous session to observe the established unified error-handling response, without submitting a form or authentication information.</en>
        # </lang>
        $genericError = Invoke-PortalRequest -Uri $missingUri -WebSession $anonymousSession

        # <lang>
        #   <zh-CN>对响应正文执行 HTML 实体解码，使既有中文用户提示即使经实体编码仍可被稳定匹配；解码结果不输出、不作为 HTML 净化或安全判定。</zh-CN>
        #   <en>HTML-entity decode response content so the established Chinese user messages remain stably matchable even when entity encoded; the decoded result is not output and is not HTML sanitization or a security determination.</en>
        # </lang>
        $genericErrorContent = [System.Net.WebUtility]::HtmlDecode($genericError.Content)

        # <lang>
        #   <zh-CN>读取错误响应最终路径以断言统一错误页，而非假定初始缺失 URI 在所有部署拓扑下保持不变。</zh-CN>
        #   <en>Read the error response's final path to assert the unified error page rather than assuming the initial missing URI remains unchanged under every deployment topology.</en>
        # </lang>
        $genericErrorPath = Get-PortalResponsePath -Response $genericError

        # <lang>
        #   <zh-CN>根站点与虚拟目录具有不同应用路径前缀，因此同时要求 HTTP 200、GenericErrorPage 最终路径和两个稳定中文提示之一；不硬编码部署根路径，也不将任意 200 或任意错误正文视为通过。</zh-CN>
        #   <en>Root-site and virtual-directory hosting have different application-path prefixes, so require HTTP 200, the GenericErrorPage final path, and one of two stable Chinese messages together; do not hard-code a deployment root or treat arbitrary 200 responses or error bodies as passing.</en>
        # </lang>
        $isGenericError = $genericError.StatusCode -eq 200 -and
            $genericErrorPath -match '/GenericErrorPage\.aspx$' -and
            $genericErrorContent -match '应用程序暂时无法完成请求|系统已记录本次错误'

        # <lang>
        #   <zh-CN>只以 HTTP 状态写入通用错误页检查详情，避免将可能包含诊断内容的错误正文带入公开结果集合。</zh-CN>
        #   <en>Write only the HTTP status into the generic-error-page check detail, preventing an error body that could contain diagnostic content from entering the shareable result collection.</en>
        # </lang>
        Add-PortalCheck -Name 'Generic error page' -Passed $isGenericError -Detail ('HTTP ' + $genericError.StatusCode)
    }

    # <lang>
    #   <zh-CN>仅在调用方显式请求时执行上传与诊断安全检查，避免默认 smoke 访问上传目录或错误页；所有本组请求继续使用既有匿名会话，不创建、修改或删除文件。</zh-CN>
    #   <en>Run upload and diagnostics-safety checks only when the caller explicitly requests them, avoiding upload-directory or error-page access in default smoke; every request in this group continues to use the established anonymous session and creates, modifies, or deletes no file.</en>
    # </lang>
    if ($CheckDocumentSafety) {
        # <lang>
        #   <zh-CN>读取仓库已有的允许扩展名 sample.doc，验证匿名静态服务和全局 nosniff 响应头；该 GET 不上传内容，也不把文件正文写入结果或输出。</zh-CN>
        #   <en>Read the repository's existing allowed-extension sample.doc to verify anonymous static serving and the global nosniff response header; this GET uploads no content and does not write the file body into results or output.</en>
        # </lang>
        $allowedUpload = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, 'uploads/sample.doc').AbsoluteUri) -WebSession $anonymousSession

        # <lang>
        #   <zh-CN>同时要求 HTTP 200 和 X-Content-Type-Options 的序号不敏感 nosniff 值；缺失、不同值或其他状态均不误判为允许扩展名服务安全通过。</zh-CN>
        #   <en>Require both HTTP 200 and an ordinal case-insensitive nosniff value for X-Content-Type-Options; a missing or different value, or any other status, is not misclassified as safe allowed-extension serving.</en>
        # </lang>
        $allowedUploadPassed = $allowedUpload.StatusCode -eq 200 -and
            [string]::Equals(
                [string]$allowedUpload.Headers['X-Content-Type-Options'],
                'nosniff',
                [System.StringComparison]::OrdinalIgnoreCase)

        # <lang>
        #   <zh-CN>仅记录 HTTP 状态作为可公开详情，避免允许文件的正文或响应头集合进入结果输出。</zh-CN>
        #   <en>Record only the HTTP status as shareable detail, keeping the allowed file's body and response-header collection out of result output.</en>
        # </lang>
        Add-PortalCheck -Name 'Upload allowed-extension serving' -Passed $allowedUploadPassed -Detail ('HTTP ' + $allowedUpload.StatusCode)

        # <lang>
        #   <zh-CN>以固定前缀和随机 GUID 构造不存在的 uploads .aspx 路径，降低与真实文件、先前运行或缓存状态碰撞的可能；请求只观察目录级过滤，不创建或删除上传文件。</zh-CN>
        #   <en>Construct a nonexistent uploads .aspx path from the fixed prefix and a random GUID, reducing collision with real files, earlier runs, or cache state; the request observes only directory-level filtering and creates or deletes no upload file.</en>
        # </lang>
        $blockedUpload = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, ('uploads/P44Blocked-' + [Guid]::NewGuid().ToString('N') + '.aspx')).AbsoluteUri) -WebSession $anonymousSession

        # <lang>
        #   <zh-CN>同时要求 HTTP 404 和 IIS 404.7 标记，确认扩展名在 requestFiltering 阶段被拒绝而未落入页面处理器；不将普通 404 或任意错误正文视为通过。</zh-CN>
        #   <en>Require both HTTP 404 and the IIS 404.7 marker, confirming the extension is rejected at requestFiltering before it reaches a page handler; do not treat an ordinary 404 or arbitrary error body as passing.</en>
        # </lang>
        $blockedUploadPassed = $blockedUpload.StatusCode -eq 404 -and $blockedUpload.Content -match '404\.7'

        # <lang>
        #   <zh-CN>仅以 HTTP 状态写入过滤检查详情，避免 IIS 错误正文进入可公开的 smoke 结果集合。</zh-CN>
        #   <en>Write only the HTTP status into filtering-check detail, keeping the IIS error body out of the shareable smoke result collection.</en>
        # </lang>
        Add-PortalCheck -Name 'Upload blocked-extension filtering' -Passed $blockedUploadPassed -Detail ('HTTP ' + $blockedUpload.StatusCode)

        # <lang>
        #   <zh-CN>以固定伪造事件编号请求通用错误页，验证不存在的诊断引用只显示“未提供”回退；该匿名 GET 不创建诊断事件、日志或业务数据。</zh-CN>
        #   <en>Request the generic error page with the fixed forged event id to verify that a nonexistent diagnostic reference displays only the “not provided” fallback; this anonymous GET creates no diagnostic event, log entry, or business data.</en>
        # </lang>
        $forgedError = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, 'GenericErrorPage.aspx?id=P44-forged').AbsoluteUri) -WebSession $anonymousSession

        # <lang>
        #   <zh-CN>对错误页正文执行实体解码，使固定中文回退文案即使经实体编码仍可稳定匹配；解码内容不输出，也不构成诊断信息净化。</zh-CN>
        #   <en>HTML-entity decode error-page content so the fixed Chinese fallback remains stably matchable even when entity encoded; decoded content is not output and does not constitute diagnostic-information sanitization.</en>
        # </lang>
        $forgedErrorContent = [System.Net.WebUtility]::HtmlDecode($forgedError.Content)

        # <lang>
        #   <zh-CN>仅当 HTTP 200 且显示固定“事件编号：未提供”文本时通过，避免伪造 id 成为管理员无法在日志中追溯的表面编号；不接受任意 200 或任意错误正文。</zh-CN>
        #   <en>Pass only when HTTP 200 and the fixed “event id: not provided” text are displayed, preventing a forged id from becoming an apparent identifier administrators cannot trace in logs; do not accept arbitrary 200 responses or error bodies.</en>
        # </lang>
        $forgedErrorPassed = $forgedError.StatusCode -eq 200 -and $forgedErrorContent -match '事件编号：\s*未提供'

        # <lang>
        #   <zh-CN>只把 HTTP 状态加入公开结果详情，不泄露错误页正文或任何潜在诊断文本。</zh-CN>
        #   <en>Put only the HTTP status into shareable result detail, without disclosing error-page content or any potential diagnostic text.</en>
        # </lang>
        Add-PortalCheck -Name 'Forged diagnostics event id' -Passed $forgedErrorPassed -Detail ('HTTP ' + $forgedError.StatusCode)
    }

    # <lang>
    #   <zh-CN>仅在调用方显式请求时执行匿名编辑器安全检查，避免默认 smoke 遍历编辑器页；本组复用既有匿名会话，不提交表单、不使用凭据且不写入业务数据。</zh-CN>
    #   <en>Run anonymous editor-safety checks only when explicitly requested, avoiding editor-page traversal in default smoke; this group reuses the established anonymous session, submits no form, uses no credential, and writes no business data.</en>
    # </lang>
    if ($CheckEditorSafety) {
        # <lang>
        #   <zh-CN>使用测试契约中固定不存在的正数 Mid，验证编辑器在到达严格数据访问或通用错误页之前拒绝匿名请求；这不是有效模块标识发现，也不读取或修改模块数据。</zh-CN>
        #   <en>Use the fixed positive Mid that is absent by test contract to verify editors reject anonymous requests before strict data access or the generic error page; this is neither valid-module discovery nor module-data reading or mutation.</en>
        # </lang>
        $missingModuleId = '2147483647'

        # <lang>
        #   <zh-CN>固定列出九个已迁移编辑器，名称作为稳定结果标签，路径共享同一伪造 Mid；该清单锁定既有回归范围，不是动态发现或完整编辑器目录。</zh-CN>
        #   <en>List the nine migrated editors explicitly: names are stable result labels and paths share the same forged Mid; this pins the established regression scope and is neither dynamic discovery nor a complete editor catalogue.</en>
        # </lang>
        $editorPages = @(
            @{ Name = 'Announcements editor missing module'; Path = ('DesktopModules/EditAnnouncements.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Contacts editor missing module'; Path = ('DesktopModules/EditContacts.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Events editor missing module'; Path = ('DesktopModules/EditEvents.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Links editor missing module'; Path = ('DesktopModules/EditLinks.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Image editor missing module'; Path = ('DesktopModules/EditImage.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'XML editor missing module'; Path = ('DesktopModules/EditXml.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'HTML editor missing module'; Path = ('DesktopModules/EditHtml.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Documents editor missing module'; Path = ('DesktopModules/EditDocs.aspx?Mid=' + $missingModuleId) },
            @{ Name = 'Discussion editor missing module'; Path = ('DesktopModules/DiscussDetails.aspx?Mid=' + $missingModuleId) }
        )

        # <lang>
        #   <zh-CN>按固定顺序遍历既有清单并共享匿名会话，使每页只进行一次无副作用 GET；不对页面内容做表单操作或输出。</zh-CN>
        #   <en>Traverse the established list in fixed order while sharing the anonymous session, making one side-effect-free GET per page; do not operate forms or output page content.</en>
        # </lang>
        foreach ($editorPage in $editorPages) {
            # <lang>
            #   <zh-CN>将每个站点相对编辑器路径与已解析 BaseUrl 组合后请求，保持虚拟目录兼容；请求不携带认证状态以外的自定义头或正文。</zh-CN>
            #   <en>Combine each site-relative editor path with the parsed BaseUrl before requesting it, preserving virtual-directory compatibility; the request carries no custom header or body beyond the anonymous session state.</en>
            # </lang>
            $response = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, $editorPage.Path).AbsoluteUri) -WebSession $anonymousSession

            # <lang>
            #   <zh-CN>读取最终响应路径以覆盖根站点和虚拟目录下的重定向；路径仅用于既有拒绝页断言，不单独作为授权成功依据。</zh-CN>
            #   <en>Read the final response path to cover redirects under root sites and virtual directories; use it only for the established denial-page assertion, never alone as authorization evidence.</en>
            # </lang>
            $responsePath = Get-PortalResponsePath -Response $response

            # <lang>
            #   <zh-CN>仅当 HTTP 200 且最终路径精确落在 EditAccessDenied 页时通过；原编辑器页、通用错误页、其他状态或其他路径都必须失败，避免把数据访问异常或意外放行视为安全。</zh-CN>
            #   <en>Pass only when HTTP 200 and the final path lands exactly on EditAccessDenied; the original editor, generic error page, any other status, or any other path must fail so data-access faults or unexpected access are not treated as safe.</en>
            # </lang>
            $passed = $response.StatusCode -eq 200 -and $responsePath -match '/Admin/EditAccessDenied\.aspx$'

            # <lang>
            #   <zh-CN>结果详情仅公开 HTTP 状态和最终路径，支持定位重定向而不收集响应正文、会话或任何编辑器数据。</zh-CN>
            #   <en>Expose only HTTP status and final path in result detail, supporting redirect diagnosis without collecting response content, session material, or editor data.</en>
            # </lang>
            Add-PortalCheck -Name $editorPage.Name -Passed $passed -Detail ('HTTP ' + $response.StatusCode + ' -> ' + $responsePath)
        }
    }

    if (-not $SkipAuthenticated -and -not [string]::IsNullOrWhiteSpace($AdminUser)) {
        if ($null -eq $AdminPassword) {
            $AdminPassword = Read-Host -Prompt 'Admin password' -AsSecureString
        }

        $authenticatedSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        $loginSucceeded = Invoke-PortalAdminLogin -LoginUri ([Uri]::new($baseUri, 'Default.aspx').AbsoluteUri) -WebSession $authenticatedSession -UserName $AdminUser -Password $AdminPassword
        $loginDetail = if ($loginSucceeded) { 'Authentication cookie received.' } else { 'Authentication cookie was not received.' }
        Add-PortalCheck -Name 'Admin sign-in' -Passed $loginSucceeded -Detail $loginDetail

        if ($loginSucceeded) {
            $adminPages = @(
                @{ Name = 'System health'; Path = 'Admin/SystemHealth.aspx'; Marker = 'System Health' },
                @{ Name = 'Diagnostics logs'; Path = 'Admin/DiagnosticsLogs.aspx'; Marker = 'Diagnostics Logs' },
                @{ Name = 'Operation audits'; Path = 'Admin/OperationAudits.aspx'; Marker = 'Operation Audits' },
                @{ Name = 'Theme settings'; Path = 'Admin/ThemeSettings.aspx'; Marker = 'Theme Settings' },
                @{ Name = 'Module catalog'; Path = 'Admin/ModuleCatalog.aspx'; Marker = 'Module Catalog' }
            )

            foreach ($page in $adminPages) {
                $response = Invoke-PortalRequest -Uri ([Uri]::new($baseUri, $page.Path).AbsoluteUri) -WebSession $authenticatedSession
                $passed = $response.StatusCode -eq 200 -and $response.Content -match [regex]::Escape($page.Marker)
                Add-PortalCheck -Name $page.Name -Passed $passed -Detail ('HTTP ' + $response.StatusCode)
            }
        }
    }

    # <lang>
    #   <zh-CN>从本次唯一结果集合筛选 Passed 为 false 的检查并物化为数组，使零、一或多项失败都具有稳定 Count 和后续名称枚举语义；不重新请求页面或检查 Detail。</zh-CN>
    #   <en>Filter checks whose Passed value is false from the invocation's single result collection and materialize an array so zero, one, or many failures have stable Count and subsequent name-enumeration semantics; do not re-request pages or inspect Detail.</en>
    # </lang>
    $failedChecks = @($checks | Where-Object { -not $_.Passed })

    # <lang>
    #   <zh-CN>在可能抛出失败异常前写出最小结果对象，包含已解析 BaseUrl、总数、失败数和本脚本启动事实；不返回检查 Detail、响应内容、Cookie 或口令。</zh-CN>
    #   <en>Write the minimal result object before a possible failure exception, containing the parsed BaseUrl, total and failed counts, and the script-started fact; do not return check Detail, response content, cookies, or passwords.</en>
    # </lang>
    [pscustomobject]@{
        BaseUrl = $baseUri.AbsoluteUri
        TotalChecks = $checks.Count
        FailedChecks = $failedChecks.Count
        StartedIISExpress = $startedByScript
    }

    # <lang>
    #   <zh-CN>任一失败检查都以固定前缀终止 smoke，并仅串联失败检查名称，避免 Detail 或响应内容进入异常消息；零失败则正常离开 try 块。</zh-CN>
    #   <en>Terminate smoke with the fixed prefix when any check failed and concatenate only failed check names, keeping Detail and response content out of the exception message; zero failures leave the try block normally.</en>
    # </lang>
    if ($failedChecks.Count -gt 0) {
        throw ('Portal smoke test failed: ' + (($failedChecks | ForEach-Object { $_.Name }) -join ', '))
    }
}
finally {
    # <lang>
    #   <zh-CN>只有本脚本已成功启动 IIS Express 且调用方明确要求完成后停止时才执行清理，避免停止预先存在的监听者；未取得所有权或未请求停止时不执行任何进程操作。</zh-CN>
    #   <en>Perform cleanup only when this script successfully started IIS Express and the caller explicitly requests stopping on completion, avoiding termination of a pre-existing listener; no process operation occurs without ownership or a stop request.</en>
    # </lang>
    if ($startedByScript -and $StopWhenComplete) {
        # <lang>
        #   <zh-CN>将同一已解析 BaseUrl 的端口交给既有停止脚本，保持启动/停止目标一致；停止失败按现有 Stop 语义向调用方可见，不在 finally 中吞并错误。</zh-CN>
        #   <en>Pass the same parsed BaseUrl port to the established stop script, keeping startup and stop targets aligned; a stop failure remains visible to the caller under the existing Stop semantics and is not swallowed in finally.</en>
        # </lang>
        & (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') -Port $baseUri.Port
    }
}
