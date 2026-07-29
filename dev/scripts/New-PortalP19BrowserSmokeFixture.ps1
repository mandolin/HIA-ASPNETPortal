<#
.SYNOPSIS
.LANG en
Creates, inspects, or removes a minimum-privilege P19.5 browser-smoke fixture in an explicit test database.

.LANG zh-CN
在显式 test 数据库中创建、检查或删除最小权限的 P19.5 浏览器 smoke fixture。

.DESCRIPTION
.LANG en
This helper is deliberately limited to an external connectionStrings.config whose
parent directory is named test. Create adds one ordinary participant without a
physical role and one administrator with only the existing Admins role. Passwords
are accepted as SecureString values, are used only in memory to derive the current
PBKDF2-HMAC-SHA256 credential material, and are never emitted. Remove deletes only
the two deterministic fixture accounts and their P19 business-application records
in one transaction; it never accepts an arbitrary user name or role identifier.

.LANG zh-CN
本 helper 被刻意限制为父目录名为 test 的外置 connectionStrings.config。Create 创建一个
没有实体角色的普通参与者和一个只拥有既有 Admins 角色的管理员。密码以 SecureString 接收，
仅在内存中短暂用于派生当前 PBKDF2-HMAC-SHA256 凭据材料，绝不输出。Remove 在同一事务中
只删除两个确定性 fixture 账号及其 P19 业务申请记录；它不接受任意用户名或角色标识。

.PARAMETER Action
.LANG en
Create adds the two accounts, Inspect reports only non-sensitive fixture facts, and
Remove deletes the fixture plus P19 records created or reviewed by it.

.LANG zh-CN
Create 新建两个账号；Inspect 只报告非敏感 fixture 事实；Remove 删除 fixture 及由其创建或
审核的 P19 记录。

.PARAMETER ConnectionStringsConfigPath
.LANG en
Required path to an external test connectionStrings.config file. The helper rejects
paths whose direct parent directory is not named test.

.LANG zh-CN
必填的外置 test connectionStrings.config 文件路径。helper 会拒绝直接父目录不叫 test 的路径。

.PARAMETER FixtureId
.LANG en
Lower-case, deterministic identifier used only to derive two bounded fixture login
names. It is not a password and must not contain personal data.

.LANG zh-CN
仅用于派生两个受限 fixture 登录名的小写确定性标识。它不是密码，且不得包含个人资料。

.PARAMETER ParticipantPassword
.LANG en
Password for the ordinary participant, required for Create only. Pass a newly
generated SecureString from the short-lived caller process; never place it on a
command line, in source code, or in a log.

.LANG zh-CN
普通参与者密码，仅 Create 时必填。应由短生命周期调用进程新生成 SecureString 传入；不得放入
命令行、源码或日志。

.PARAMETER AdministratorPassword
.LANG en
Password for the administrator, required for Create only. It follows the same
in-memory-only boundary as ParticipantPassword.

.LANG zh-CN
管理员密码，仅 Create 时必填。它与 ParticipantPassword 一样只能处于内存边界内。
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath,

    [ValidateSet('Create', 'Inspect', 'Remove')]
    [string]$Action = 'Create',

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9][a-z0-9-]{2,20}$')]
    [string]$FixtureId,

    [System.Security.SecureString]$ParticipantPassword,

    [System.Security.SecureString]$AdministratorPassword,

    [ValidateNotNullOrEmpty()]
    [string]$ConnectionStringName = 'Portal'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>当前应用的凭据格式与数据访问层 PortalPasswordHasher 保持一致；任何算法升级都必须同时更新该 helper、应用实现和验证。</zh-CN>
#   <en>The current credential format stays aligned with the data-access PortalPasswordHasher; any algorithm upgrade must update this helper, the application implementation, and validation together.</en>
# </lang>
$credentialFormat = 'PBKDF2-HMAC-SHA256'

# <lang>
#   <zh-CN>迭代次数、盐长度和哈希长度直接镜像当前应用契约，避免 fixture 形成低强度旁路。</zh-CN>
#   <en>The iteration count, salt length, and hash length mirror the current application contract so the fixture does not become a low-strength bypass.</en>
# </lang>
$credentialIterationCount = 210000
$credentialSaltLength = 32
$credentialHashLength = 32

function Assert-PortalP19TestConfigPath {
    <#
    .SYNOPSIS
    .LANG en
    Resolves and constrains the external configuration path to the test directory convention.

    .LANG zh-CN
    解析外置配置路径，并将其限制为 test 目录约定。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    # <lang>
    #   <zh-CN>解析后的绝对路径消除相对路径歧义，随后只把该路径用于读取连接配置。</zh-CN>
    #   <en>The resolved absolute path removes relative-path ambiguity and is then used only to read the connection configuration.</en>
    # </lang>
    $resolvedPath = (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path

    # <lang>
    #   <zh-CN>直接父目录是本 helper 的显式环境边界；不要以数据库名推断安全性，因为开发库可能同名。</zh-CN>
    #   <en>The direct parent directory is this helper's explicit environment boundary; do not infer safety from a database name because a development database can share it.</en>
    # </lang>
    $parentDirectory = Split-Path -Parent $resolvedPath

    # <lang>
    #   <zh-CN>目录叶名只作为本地测试配置约定，不输出配置内容或任何连接信息。</zh-CN>
    #   <en>The directory leaf is only a local test-configuration convention; no configuration content or connection detail is emitted.</en>
    # </lang>
    $parentLeaf = Split-Path -Leaf $parentDirectory

    # <lang>
    #   <zh-CN>拒绝非 test 路径可降低误写实际环境的风险；如部署布局改变，应审查后修改 helper，而不是绕过此检查。</zh-CN>
    #   <en>Rejecting non-test paths reduces the risk of modifying a real environment; if deployment layout changes, review and update this helper rather than bypassing the check.</en>
    # </lang>
    if (-not [string]::Equals($parentLeaf, 'test', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The P19 browser fixture accepts only an external connectionStrings.config whose direct parent directory is named test.'
    }

    return $resolvedPath
}

function Get-ExternalPortalConnectionString {
    <#
    .SYNOPSIS
    .LANG en
    Loads one named SQL Server connection string without writing it to output.

    .LANG zh-CN
    读取一个具名 SQL Server 连接串，但不将其写入输出。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    # <lang>
    #   <zh-CN>UTF-8 无 BOM 读取避免旧 Windows PowerShell 默认编码改变外置 XML；内容始终停留在当前进程内。</zh-CN>
    #   <en>UTF-8-without-BOM reading avoids legacy Windows PowerShell default-encoding changes to external XML; the content stays in the current process.</en>
    # </lang>
    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))

    # <lang>
    #   <zh-CN>正式配置以 connectionStrings 为根，也兼容人工临时文件常见的 configuration 包装，兼容范围仅限读取。</zh-CN>
    #   <en>The production configuration uses a connectionStrings root, while a configuration wrapper remains compatible for hand-created temporary files; compatibility is read-only.</en>
    # </lang>
    $connectionStringsNode = if ($document.DocumentElement -and $document.DocumentElement.Name -eq 'connectionStrings') {
        $document.DocumentElement
    }
    elseif ($document.configuration -and $document.configuration.connectionStrings) {
        $document.configuration.connectionStrings
    }
    else {
        throw 'The external connection-string file must contain a <connectionStrings> section.'
    }

    # <lang>
    #   <zh-CN>只接受唯一且非空的逻辑条目，避免同名配置导致连接目标不确定。</zh-CN>
    #   <en>Accept only one non-empty logical entry so duplicate configuration cannot make the connection target ambiguous.</en>
    # </lang>
    $entries = @($connectionStringsNode.add | Where-Object { $_.name -eq $Name })
    if ($entries.Count -ne 1 -or [string]::IsNullOrWhiteSpace($entries[0].connectionString)) {
        throw "The external connection-string file must contain one non-empty '$Name' entry."
    }

    # <lang>
    #   <zh-CN>本 helper 只使用项目既有的 System.Data.SqlClient 实现；拒绝其它 provider 可避免参数语义悄然变化。</zh-CN>
    #   <en>This helper uses only the project's existing System.Data.SqlClient implementation; rejecting other providers prevents silent parameter-semantics changes.</en>
    # </lang>
    if ($entries[0].providerName -and $entries[0].providerName -ne 'System.Data.SqlClient') {
        throw 'The P19 browser fixture supports only System.Data.SqlClient.'
    }

    # <lang>
    #   <zh-CN>连接串只作为函数返回值供 SqlConnection 使用；调用方不得格式化、记录或展示它。</zh-CN>
    #   <en>The connection string is returned only for SqlConnection use; callers must not format, log, or display it.</en>
    # </lang>
    return $entries[0].connectionString
}

function New-PortalP19PasswordMaterial {
    <#
    .SYNOPSIS
    .LANG en
    Derives the current Portal PBKDF2 credential material from a SecureString.

    .LANG zh-CN
    从 SecureString 派生当前 Portal PBKDF2 凭据材料。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [System.Security.SecureString]$Password,

        [Parameter(Mandatory = $true)]
        [int]$IterationCount,

        [Parameter(Mandatory = $true)]
        [int]$SaltLength,

        [Parameter(Mandatory = $true)]
        [int]$HashLength
    )

    # <lang>
    #   <zh-CN>空或过短口令会产生不可审计的弱 smoke 账号，因此在数据库写入前拒绝；实际明文值绝不输出。</zh-CN>
    #   <en>An empty or too-short password would create an unauditable weak smoke account, so reject it before database writes; the actual plain-text value is never emitted.</en>
    # </lang>
    if ($Password.Length -lt 16) {
        throw 'Fixture passwords must contain at least 16 characters.'
    }

    # <lang>
    #   <zh-CN>非托管 BSTR 指针是将 SecureString 交给 PBKDF2 API 所需的最小短生命周期明文桥接，finally 中必须归零释放。</zh-CN>
    #   <en>The unmanaged BSTR pointer is the minimum short-lived clear-text bridge required for the PBKDF2 API and must be zeroed and freed in finally.</en>
    # </lang>
    $passwordPointer = [IntPtr]::Zero

    # <lang>
    #   <zh-CN>不可变 .NET 字符串无法承诺原地清零，因此其作用域仅限派生块且不存入对象、日志或输出。</zh-CN>
    #   <en>An immutable .NET string cannot promise in-place clearing, so its scope is limited to the derivation block and it is never stored in an object, log, or output.</en>
    # </lang>
    $plainTextPassword = $null

    try {
        # <lang>
        #   <zh-CN>将 SecureString 转为 BSTR 后立即读取，避免把明文复制到调用方变量或命令历史。</zh-CN>
        #   <en>Convert the SecureString to a BSTR and read it immediately, avoiding clear-text copies in caller variables or command history.</en>
        # </lang>
        $passwordPointer = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
        $plainTextPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)

        # <lang>
        #   <zh-CN>加密随机盐与每个账号独立绑定，防止相同测试密码形成可比较的数据库值。</zh-CN>
        #   <en>A cryptographically random salt is bound independently to each account, preventing equal test passwords from producing comparable database values.</en>
        # </lang>
        $salt = [byte[]]::new($SaltLength)
        $randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $randomNumberGenerator.GetBytes($salt)
        }
        finally {
            $randomNumberGenerator.Dispose()
        }

        # <lang>
        #   <zh-CN>派生器显式指定 SHA-256、应用一致的迭代次数和固定哈希长度，避免使用框架默认算法或长度。</zh-CN>
        #   <en>The derivation explicitly specifies SHA-256, the application-matching iteration count, and fixed hash length, avoiding framework-default algorithms or lengths.</en>
        # </lang>
        $deriveBytes = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
            $plainTextPassword,
            $salt,
            $IterationCount,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        try {
            $hash = $deriveBytes.GetBytes($HashLength)
        }
        finally {
            $deriveBytes.Dispose()
        }

        # <lang>
        #   <zh-CN>返回的字节材料只用于绑定 SQL varbinary 参数；调用方在命令结束后负责清零数组。</zh-CN>
        #   <en>The returned byte material is used only to bind SQL varbinary parameters; the caller clears the arrays after command completion.</en>
        # </lang>
        return [pscustomobject]@{
            Salt = $salt
            Hash = $hash
        }
    }
    finally {
        # <lang>
        #   <zh-CN>无论派生成功或失败，都归零并释放 BSTR，缩短明文在非托管内存中的驻留时间。</zh-CN>
        #   <en>Whether derivation succeeds or fails, zero and free the BSTR to shorten clear-text residency in unmanaged memory.</en>
        # </lang>
        if ($passwordPointer -ne [IntPtr]::Zero) {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
        }
    }
}

function Add-PortalP19SqlParameter {
    <#
    .SYNOPSIS
    .LANG en
    Adds one explicitly typed SQL parameter without string interpolation.

    .LANG zh-CN
    添加一个显式类型的 SQL 参数，不进行字符串插值。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.SqlClient.SqlCommand]$Command,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [System.Data.SqlDbType]$SqlDbType,

        [Parameter(Mandatory = $true)]
        [int]$Size,

        [AllowNull()]
        [object]$Value
    )

    # <lang>
    #   <zh-CN>参数对象把 fixture 标识、登录名和派生字节与 SQL 语法隔离，避免注入和二进制文本化问题。</zh-CN>
    #   <en>The parameter object separates fixture identifiers, login names, and derived bytes from SQL syntax, avoiding injection and binary text-conversion issues.</en>
    # </lang>
    $parameter = $Command.Parameters.Add($Name, $SqlDbType, $Size)

    # <lang>
    #   <zh-CN>数据库 NULL 必须使用 DBNull 表达；本 helper 的当前调用不传密码相关 NULL，但保留通用且明确的语义。</zh-CN>
    #   <en>Database NULL must be expressed with DBNull; current helper calls do not pass password-related NULLs, but retain explicit general semantics.</en>
    # </lang>
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
    return $parameter
}

function Add-PortalP19BinaryParameter {
    <#
    .SYNOPSIS
    .LANG en
    Adds one explicitly typed binary SQL parameter while preserving the byte-array boundary.

    .LANG zh-CN
    添加一个显式类型的二进制 SQL 参数，并保持字节数组边界。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.SqlClient.SqlCommand]$Command,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [ValidateRange(1, 64)]
        [int]$Size,

        [Parameter(Mandatory = $true)]
        [byte[]]$Value
    )

    # <lang>
    #   <zh-CN>PowerShell 会把 byte[] 视为可枚举对象；显式 byte[] 参数和 VarBinary 类型共同防止盐或哈希在命令绑定时被展开为 Object[]。</zh-CN>
    #   <en>PowerShell treats byte[] as enumerable; the explicit byte[] parameter plus VarBinary type prevents a salt or hash from expanding into Object[] during command binding.</en>
    # </lang>
    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::VarBinary, $Size)
    $parameter.Value = $Value
    return $parameter
}

function Invoke-PortalP19FixtureRowCommand {
    <#
    .SYNOPSIS
    .LANG en
    Executes one fixture command and returns the single non-sensitive result row.

    .LANG zh-CN
    执行一个 fixture 命令并返回唯一的非敏感结果行。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString,

        [Parameter(Mandatory = $true)]
        [string]$CommandText,

        [Parameter(Mandatory = $true)]
        [scriptblock]$ConfigureParameters
    )

    # <lang>
    #   <zh-CN>连接只在单个命令周期内打开；连接串不进入对象输出、异常摘要或磁盘文件。</zh-CN>
    #   <en>The connection opens only for one command lifetime; the connection string never enters object output, exception summaries, or disk files.</en>
    # </lang>
    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()

        # <lang>
        #   <zh-CN>命令批次在 SQL 内部显式设置 XACT_ABORT 和事务边界；120 秒上限防止 fixture 操作静默无限等待。</zh-CN>
        #   <en>The command batch explicitly sets XACT_ABORT and transaction boundaries in SQL; the 120-second ceiling prevents fixture operations from silently waiting forever.</en>
        # </lang>
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = $CommandText
            $command.CommandTimeout = 120
            & $ConfigureParameters $command

            # <lang>
            #   <zh-CN>读取器仅消费脚本末尾的计数/标识结果行，禁止 SELECT 凭据材料、连接属性或正文数据。</zh-CN>
            #   <en>The reader consumes only the final count/identifier result row; selecting credential material, connection properties, or body text is prohibited.</en>
            # </lang>
            $reader = $command.ExecuteReader()
            try {
                if (-not $reader.Read()) {
                    throw 'The fixture command did not return its required result row.'
                }

                # <lang>
                #   <zh-CN>有序键值表保持结果字段与 SQL SELECT 顺序一致，便于后续只读审计而不依赖 DataTable 序列化。</zh-CN>
                #   <en>The ordered key/value table preserves result fields in SQL SELECT order for later read-only auditing without DataTable serialization.</en>
                # </lang>
                $result = [ordered]@{}
                for ($columnIndex = 0; $columnIndex -lt $reader.FieldCount; $columnIndex++) {
                    $result[$reader.GetName($columnIndex)] = $reader.GetValue($columnIndex)
                }

                return [pscustomobject]$result
            }
            finally {
                $reader.Dispose()
            }
        }
        finally {
            $command.Dispose()
        }
    }
    finally {
        $connection.Dispose()
    }
}

# <lang>
#   <zh-CN>FixtureId 只生成确定性、低敏账号名；它从不作为 SQL 标识符，也不影响数据库架构。</zh-CN>
#   <en>FixtureId generates only deterministic, low-sensitivity account names; it is never a SQL identifier and does not affect database schema.</en>
# </lang>
$participantUserName = "p24p-$FixtureId"
$administratorUserName = "p24a-$FixtureId"

# <lang>
#   <zh-CN>invalid 顶级域名确保 fixture 电子邮件不会成为真实外发目标，同时满足旧 Portal 必填邮箱约束。</zh-CN>
#   <en>The invalid top-level domain ensures fixture email addresses are never real delivery targets while satisfying the legacy Portal required-email constraint.</en>
# </lang>
$participantEmail = "$participantUserName@fixture.invalid"
$administratorEmail = "$administratorUserName@fixture.invalid"

# <lang>
#   <zh-CN>先完成路径约束再读取配置，保证任何数据库 I/O 都不能由非 test 目录的外置配置触发。</zh-CN>
#   <en>Constrain the path before reading configuration so no database I/O can be triggered by an external configuration outside the test directory.</en>
# </lang>
$resolvedConfigPath = Assert-PortalP19TestConfigPath -Path $ConnectionStringsConfigPath

# <lang>
#   <zh-CN>连接串仅在当前进程的私有变量中保存，并被传给受控 SqlConnection 工厂；禁止 Write-Host、Format-List 或异常拼接。</zh-CN>
#   <en>The connection string stays only in a private current-process variable and is passed to the controlled SqlConnection factory; Write-Host, Format-List, or exception concatenation is prohibited.</en>
# </lang>
$connectionString = Get-ExternalPortalConnectionString -Path $resolvedConfigPath -Name $ConnectionStringName

# <lang>
#   <zh-CN>连接串构造器只验证存在数据库段，避免把无明确 catalog 的连接串用于可变更 fixture；不输出实际数据库名。</zh-CN>
#   <en>The connection-string builder only validates that a database segment exists, avoiding mutable fixture use with an unspecified catalog; the actual database name is not emitted.</en>
# </lang>
$connectionStringBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ([string]::IsNullOrWhiteSpace($connectionStringBuilder.InitialCatalog)) {
    throw 'The external test connection string must specify an initial catalog.'
}

if ($Action -eq 'Create') {
    # <lang>
    #   <zh-CN>Create 需要两份独立密码；先在内存中检查，避免事务开始后才发现不完整输入。</zh-CN>
    #   <en>Create needs two independent passwords; check them in memory before a transaction starts to avoid discovering incomplete input after writes begin.</en>
    # </lang>
    if ($null -eq $ParticipantPassword -or $null -eq $AdministratorPassword) {
        throw 'ParticipantPassword and AdministratorPassword are required when Action is Create.'
    }

    # <lang>
    #   <zh-CN>参与者的派生材料与管理员完全隔离，即使调用方意外提供相同密码也会因独立随机盐不同。</zh-CN>
    #   <en>The participant's derived material is fully isolated from the administrator's; even if a caller accidentally supplies equal passwords, independent random salts differ.</en>
    # </lang>
    $participantCredential = New-PortalP19PasswordMaterial -Password $ParticipantPassword -IterationCount $credentialIterationCount -SaltLength $credentialSaltLength -HashLength $credentialHashLength
    $administratorCredential = New-PortalP19PasswordMaterial -Password $AdministratorPassword -IterationCount $credentialIterationCount -SaltLength $credentialSaltLength -HashLength $credentialHashLength
    try {
        # <lang>
        #   <zh-CN>此批次先验证所有 P2/P5/P6/P19 依赖和 Admins 角色，再开启事务；普通用户不写 Portal_UserRoles，管理员只插入 Admins。</zh-CN>
        #   <en>This batch validates every P2/P5/P6/P19 dependency and the Admins role before opening a transaction; the ordinary user receives no Portal_UserRoles row and the administrator receives only Admins.</en>
        # </lang>
        $createCommandText = @'
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Portal_Roles]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Portal_UserRoles]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Portal_UserCredentials]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Portal_UserSecurityStates]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_UserProfiles]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_RolePermissions]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]', N'U') IS NULL
BEGIN
    THROW 51000, 'P19 fixture requires the P2/P5/P6/P12/P19 schema milestones.', 1;
END;

DECLARE @AdminsRoleId INT;
SELECT TOP (1) @AdminsRoleId = [RoleID]
FROM [dbo].[Portal_Roles]
WHERE [RoleName] = N'Admins'
ORDER BY [RoleID];

IF @AdminsRoleId IS NULL
BEGIN
    THROW 51001, 'P19 fixture requires the existing Admins role.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM [dbo].[PortalCfg_RolePermissions] AS [Permissions]
    INNER JOIN [dbo].[Portal_Roles] AS [Roles]
        ON [Roles].[RoleID] = [Permissions].[RoleId]
    WHERE [Roles].[RoleName] = N'All Users'
      AND [Permissions].[PermissionKey] = N'Business.Application.Submit'
      AND [Permissions].[IsEnabled] = 1
)
BEGIN
    THROW 51002, 'P19 fixture requires the enabled All Users Business.Application.Submit grant.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_Users]
    WHERE [Name] IN (@ParticipantUserName, @AdministratorUserName)
       OR [Email] IN (@ParticipantEmail, @AdministratorEmail)
)
BEGIN
    THROW 51003, 'The deterministic fixture accounts already exist. Inspect or remove them before creating again.', 1;
END;

BEGIN TRANSACTION;

INSERT INTO [dbo].[Portal_Users] ([Name], [Password], [Email])
VALUES (@ParticipantUserName, N'', @ParticipantEmail);
DECLARE @ParticipantUserId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO [dbo].[Portal_Users] ([Name], [Password], [Email])
VALUES (@AdministratorUserName, N'', @AdministratorEmail);
DECLARE @AdministratorUserId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO [dbo].[Portal_UserCredentials]
    ([UserId], [CredentialVersion], [PasswordFormat], [PasswordHash], [PasswordSalt], [IterationCount], [RequiresReset])
VALUES
    (@ParticipantUserId, 1, @CredentialFormat, @ParticipantHash, @ParticipantSalt, @CredentialIterationCount, 0),
    (@AdministratorUserId, 1, @CredentialFormat, @AdministratorHash, @AdministratorSalt, @CredentialIterationCount, 0);

INSERT INTO [dbo].[Portal_UserSecurityStates] ([UserId], [SecurityVersion], [ChangedUtc], [ChangeReason])
VALUES
    (@ParticipantUserId, 1, SYSUTCDATETIME(), N'P24.3BrowserFixture'),
    (@AdministratorUserId, 1, SYSUTCDATETIME(), N'P24.3BrowserFixture');

INSERT INTO [dbo].[PortalBiz_UserProfiles]
    ([UserId], [LoginName], [DisplayName], [Nickname], [PreferredEmail], [Status], [StatusReason], [CreatedBy], [UpdatedBy])
VALUES
    (@ParticipantUserId, @ParticipantUserName, N'P24.3 Browser Participant', NULL, @ParticipantEmail, N'Active', N'P24.3BrowserFixture', N'P24.3BrowserFixture', N'P24.3BrowserFixture'),
    (@AdministratorUserId, @AdministratorUserName, N'P24.3 Browser Administrator', NULL, @AdministratorEmail, N'Active', N'P24.3BrowserFixture', N'P24.3BrowserFixture', N'P24.3BrowserFixture');

INSERT INTO [dbo].[PortalCfg_UserRegistrations]
    ([UserId], [Status], [RequiresApproval], [RegisteredUtc], [ApprovedUtc], [ApprovedBy], [ReviewNote])
VALUES
    (@ParticipantUserId, N'Approved', 0, SYSUTCDATETIME(), SYSUTCDATETIME(), N'P24.3BrowserFixture', N'Test fixture approved for browser smoke only.'),
    (@AdministratorUserId, N'Approved', 0, SYSUTCDATETIME(), SYSUTCDATETIME(), N'P24.3BrowserFixture', N'Test fixture approved for browser smoke only.');

INSERT INTO [dbo].[Portal_UserRoles] ([UserID], [RoleID])
VALUES (@AdministratorUserId, @AdminsRoleId);

COMMIT TRANSACTION;

SELECT
    @ParticipantUserId AS [ParticipantUserId],
    @AdministratorUserId AS [AdministratorUserId],
    @AdminsRoleId AS [AdminsRoleId];
'@

        # <lang>
        #   <zh-CN>ShouldProcess 文本只描述 test fixture 动作，不包含数据库名、连接串或任何凭据材料。</zh-CN>
        #   <en>The ShouldProcess text describes only the test-fixture action and contains no database name, connection string, or credential material.</en>
        # </lang>
        if ($PSCmdlet.ShouldProcess("P19 browser fixture '$FixtureId'", 'Create the participant and administrator test accounts')) {
            $createResult = Invoke-PortalP19FixtureRowCommand -ConnectionString $connectionString -CommandText $createCommandText -ConfigureParameters {
                param($command)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@ParticipantUserName' -SqlDbType NVarChar -Size 50 -Value $participantUserName)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@AdministratorUserName' -SqlDbType NVarChar -Size 50 -Value $administratorUserName)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@ParticipantEmail' -SqlDbType NVarChar -Size 100 -Value $participantEmail)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@AdministratorEmail' -SqlDbType NVarChar -Size 100 -Value $administratorEmail)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@CredentialFormat' -SqlDbType NVarChar -Size 40 -Value $credentialFormat)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@CredentialIterationCount' -SqlDbType Int -Size 0 -Value $credentialIterationCount)
                [void](Add-PortalP19BinaryParameter -Command $command -Name '@ParticipantHash' -Size $credentialHashLength -Value $participantCredential.Hash)
                [void](Add-PortalP19BinaryParameter -Command $command -Name '@ParticipantSalt' -Size $credentialSaltLength -Value $participantCredential.Salt)
                [void](Add-PortalP19BinaryParameter -Command $command -Name '@AdministratorHash' -Size $credentialHashLength -Value $administratorCredential.Hash)
                [void](Add-PortalP19BinaryParameter -Command $command -Name '@AdministratorSalt' -Size $credentialSaltLength -Value $administratorCredential.Salt)
            }

            # <lang>
            #   <zh-CN>输出仅保留后续浏览器及清理所需的账号名、数据库主键和角色事实；密码、哈希、盐和连接属性都不在对象中。</zh-CN>
            #   <en>Output retains only account names, database keys, and role facts required by later browser work and cleanup; passwords, hashes, salts, and connection properties are absent.</en>
            # </lang>
            [pscustomobject]@{
                Action = 'Create'
                FixtureId = $FixtureId
                ParticipantUserName = $participantUserName
                ParticipantUserId = $createResult.ParticipantUserId
                ParticipantPhysicalRoleMembership = 'None'
                AdministratorUserName = $administratorUserName
                AdministratorUserId = $createResult.AdministratorUserId
                AdministratorPhysicalRoleMembership = 'Admins'
                AdminsRoleId = $createResult.AdminsRoleId
                CredentialFormat = $credentialFormat
                CredentialIterationCount = $credentialIterationCount
            }
        }
    }
    finally {
        # <lang>
        #   <zh-CN>SQL 参数执行后立即清零派生数组，降低盐和哈希材料在托管内存中的残留时间；不会影响已持久化的 fixture 凭据。</zh-CN>
        #   <en>Clear derived arrays immediately after SQL parameter execution to reduce salt and hash residency in managed memory; this does not affect the persisted fixture credential.</en>
        # </lang>
        [Array]::Clear($participantCredential.Salt, 0, $participantCredential.Salt.Length)
        [Array]::Clear($participantCredential.Hash, 0, $participantCredential.Hash.Length)
        [Array]::Clear($administratorCredential.Salt, 0, $administratorCredential.Salt.Length)
        [Array]::Clear($administratorCredential.Hash, 0, $administratorCredential.Hash.Length)
    }
}
else {
    # <lang>
    #   <zh-CN>Inspect 和 Remove 共用精确账号名选择，拒绝模糊匹配，确保 helper 不能变成任意用户管理工具。</zh-CN>
    #   <en>Inspect and Remove share exact account-name selection and reject fuzzy matching, ensuring this helper cannot become a general user-management tool.</en>
    # </lang>
    $inspectCommandText = @'
SET NOCOUNT ON;

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Portal_UserRoles]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]', N'U') IS NULL
BEGIN
    THROW 51010, 'P19 fixture inspect/remove requires the P2/P12/P19 schema milestones.', 1;
END;

DECLARE @FixtureUsers TABLE ([UserId] INT NOT NULL PRIMARY KEY, [UserName] NVARCHAR(50) NOT NULL);
INSERT INTO @FixtureUsers ([UserId], [UserName])
SELECT [UserID], [Name]
FROM [dbo].[Portal_Users]
WHERE [Name] IN (@ParticipantUserName, @AdministratorUserName);

DECLARE @FixtureApplicationIds TABLE ([ApplicationId] BIGINT NOT NULL PRIMARY KEY);
INSERT INTO @FixtureApplicationIds ([ApplicationId])
SELECT [ApplicationId]
FROM [dbo].[PortalBiz_BusinessApplications]
WHERE [ApplicantUserId] IN (SELECT [UserId] FROM @FixtureUsers)
   OR [ReviewedByUserId] IN (SELECT [UserId] FROM @FixtureUsers);

DECLARE @FixtureWorkItemIds TABLE ([WorkItemId] BIGINT NOT NULL PRIMARY KEY);
INSERT INTO @FixtureWorkItemIds ([WorkItemId])
SELECT [WorkItemId]
FROM [dbo].[PortalBiz_WorkItems]
WHERE [BusinessKind] = N'BusinessApplication'
  AND [BusinessId] IN (SELECT CONVERT(NVARCHAR(80), [ApplicationId]) FROM @FixtureApplicationIds);

SELECT
    (SELECT COUNT(*) FROM @FixtureUsers) AS [FixtureUserCount],
    (SELECT COUNT(*) FROM [dbo].[Portal_UserRoles] WHERE [UserID] IN (SELECT [UserId] FROM @FixtureUsers)) AS [PhysicalRoleMembershipCount],
    (SELECT COUNT(*) FROM @FixtureApplicationIds) AS [BusinessApplicationCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_BusinessApplications] WHERE [ApplicationId] IN (SELECT [ApplicationId] FROM @FixtureApplicationIds) AND [ApplicationStatus] = N'Approved') AS [ApprovedBusinessApplicationCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkflowEvents] WHERE [BusinessKind] = N'BusinessApplication' AND [BusinessId] IN (SELECT CONVERT(NVARCHAR(80), [ApplicationId]) FROM @FixtureApplicationIds)) AS [WorkflowEventCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkflowEvents] WHERE [BusinessKind] = N'BusinessApplication' AND [BusinessId] IN (SELECT CONVERT(NVARCHAR(80), [ApplicationId]) FROM @FixtureApplicationIds) AND [ActionKey] = N'Submit') AS [SubmitWorkflowEventCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkflowEvents] WHERE [BusinessKind] = N'BusinessApplication' AND [BusinessId] IN (SELECT CONVERT(NVARCHAR(80), [ApplicationId]) FROM @FixtureApplicationIds) AND [ActionKey] = N'Approve') AS [ApproveWorkflowEventCount],
    (SELECT COUNT(*) FROM @FixtureWorkItemIds) AS [WorkItemCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkItems] WHERE [WorkItemId] IN (SELECT [WorkItemId] FROM @FixtureWorkItemIds) AND [WorkItemStatus] = N'Completed') AS [CompletedWorkItemCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkItemEvents] WHERE [WorkItemId] IN (SELECT [WorkItemId] FROM @FixtureWorkItemIds)) AS [WorkItemEventCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkItemEvents] WHERE [WorkItemId] IN (SELECT [WorkItemId] FROM @FixtureWorkItemIds) AND [EventType] = N'Approved') AS [ApprovedWorkItemEventCount],
    (SELECT COUNT(*) FROM [dbo].[PortalCfg_OperationAudits] WHERE [TargetType] = N'BusinessApplication' AND [TargetId] IN (SELECT CONVERT(NVARCHAR(200), [ApplicationId]) FROM @FixtureApplicationIds) AND [ActorUserName] IN (@ParticipantUserName, @AdministratorUserName)) AS [OperationAuditCount];
'@

    # <lang>
    #   <zh-CN>先进行同一精确范围的只读事实检查；Remove 使用其结果作可审计前态，Inspect 则直接结束。</zh-CN>
    #   <en>First perform a read-only fact check over the same exact scope; Remove uses the result as its auditable pre-state, while Inspect ends here.</en>
    # </lang>
    $inspectResult = Invoke-PortalP19FixtureRowCommand -ConnectionString $connectionString -CommandText $inspectCommandText -ConfigureParameters {
        param($command)
        [void](Add-PortalP19SqlParameter -Command $command -Name '@ParticipantUserName' -SqlDbType NVarChar -Size 50 -Value $participantUserName)
        [void](Add-PortalP19SqlParameter -Command $command -Name '@AdministratorUserName' -SqlDbType NVarChar -Size 50 -Value $administratorUserName)
    }

    if ($Action -eq 'Inspect') {
        # <lang>
        #   <zh-CN>检查输出是计数级证据，不读取业务正文、邮箱、凭据或连接信息。</zh-CN>
        #   <en>Inspect output is count-level evidence and does not read business body text, email, credentials, or connection information.</en>
        # </lang>
        [pscustomobject]@{
            Action = 'Inspect'
            FixtureId = $FixtureId
            ParticipantUserName = $participantUserName
            AdministratorUserName = $administratorUserName
            FixtureUserCount = $inspectResult.FixtureUserCount
            PhysicalRoleMembershipCount = $inspectResult.PhysicalRoleMembershipCount
            BusinessApplicationCount = $inspectResult.BusinessApplicationCount
            ApprovedBusinessApplicationCount = $inspectResult.ApprovedBusinessApplicationCount
            WorkflowEventCount = $inspectResult.WorkflowEventCount
            SubmitWorkflowEventCount = $inspectResult.SubmitWorkflowEventCount
            ApproveWorkflowEventCount = $inspectResult.ApproveWorkflowEventCount
            WorkItemCount = $inspectResult.WorkItemCount
            CompletedWorkItemCount = $inspectResult.CompletedWorkItemCount
            WorkItemEventCount = $inspectResult.WorkItemEventCount
            ApprovedWorkItemEventCount = $inspectResult.ApprovedWorkItemEventCount
            OperationAuditCount = $inspectResult.OperationAuditCount
        }
    }
    else {
        # <lang>
        #   <zh-CN>清理批次严格按先依赖记录、后申请、再账号的顺序删除；任一步失败时 XACT_ABORT 回滚整个事务。</zh-CN>
        #   <en>The cleanup batch deletes dependent records first, then applications, then accounts; XACT_ABORT rolls back the entire transaction if any step fails.</en>
        # </lang>
        $removeCommandText = @'
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Portal_UserRoles]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]', N'U') IS NULL
BEGIN
    THROW 51011, 'P19 fixture removal requires the P2/P12/P19 schema milestones.', 1;
END;

BEGIN TRANSACTION;

DECLARE @FixtureUsers TABLE ([UserId] INT NOT NULL PRIMARY KEY, [UserName] NVARCHAR(50) NOT NULL);
INSERT INTO @FixtureUsers ([UserId], [UserName])
SELECT [UserID], [Name]
FROM [dbo].[Portal_Users]
WHERE [Name] IN (@ParticipantUserName, @AdministratorUserName);

IF (SELECT COUNT(*) FROM @FixtureUsers) <> 2
BEGIN
    THROW 51012, 'Fixture removal requires exactly the two deterministic fixture accounts.', 1;
END;

DECLARE @FixtureApplicationIds TABLE ([ApplicationId] BIGINT NOT NULL PRIMARY KEY);
INSERT INTO @FixtureApplicationIds ([ApplicationId])
SELECT [ApplicationId]
FROM [dbo].[PortalBiz_BusinessApplications]
WHERE [ApplicantUserId] IN (SELECT [UserId] FROM @FixtureUsers)
   OR [ReviewedByUserId] IN (SELECT [UserId] FROM @FixtureUsers);

DECLARE @FixtureWorkItemIds TABLE ([WorkItemId] BIGINT NOT NULL PRIMARY KEY);
INSERT INTO @FixtureWorkItemIds ([WorkItemId])
SELECT [WorkItemId]
FROM [dbo].[PortalBiz_WorkItems]
WHERE [BusinessKind] = N'BusinessApplication'
  AND [BusinessId] IN (SELECT CONVERT(NVARCHAR(80), [ApplicationId]) FROM @FixtureApplicationIds);

DELETE FROM [dbo].[PortalBiz_WorkflowEvents]
WHERE [BusinessKind] = N'BusinessApplication'
  AND [BusinessId] IN (SELECT CONVERT(NVARCHAR(80), [ApplicationId]) FROM @FixtureApplicationIds);
DECLARE @DeletedWorkflowEventCount INT = @@ROWCOUNT;

DELETE FROM [dbo].[PortalBiz_WorkItemEvents]
WHERE [WorkItemId] IN (SELECT [WorkItemId] FROM @FixtureWorkItemIds);
DECLARE @DeletedWorkItemEventCount INT = @@ROWCOUNT;

DELETE FROM [dbo].[PortalBiz_WorkItems]
WHERE [WorkItemId] IN (SELECT [WorkItemId] FROM @FixtureWorkItemIds);
DECLARE @DeletedWorkItemCount INT = @@ROWCOUNT;

DELETE FROM [dbo].[PortalCfg_OperationAudits]
WHERE [TargetType] = N'BusinessApplication'
  AND [TargetId] IN (SELECT CONVERT(NVARCHAR(200), [ApplicationId]) FROM @FixtureApplicationIds)
  AND [ActorUserName] IN (@ParticipantUserName, @AdministratorUserName);
DECLARE @DeletedOperationAuditCount INT = @@ROWCOUNT;

DELETE FROM [dbo].[PortalBiz_BusinessApplications]
WHERE [ApplicationId] IN (SELECT [ApplicationId] FROM @FixtureApplicationIds);
DECLARE @DeletedBusinessApplicationCount INT = @@ROWCOUNT;

DELETE FROM [dbo].[Portal_UserRoles]
WHERE [UserID] IN (SELECT [UserId] FROM @FixtureUsers);
DECLARE @DeletedPhysicalRoleMembershipCount INT = @@ROWCOUNT;

DELETE FROM [dbo].[Portal_Users]
WHERE [UserID] IN (SELECT [UserId] FROM @FixtureUsers);
DECLARE @DeletedFixtureUserCount INT = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    @DeletedFixtureUserCount AS [DeletedFixtureUserCount],
    @DeletedPhysicalRoleMembershipCount AS [DeletedPhysicalRoleMembershipCount],
    @DeletedBusinessApplicationCount AS [DeletedBusinessApplicationCount],
    @DeletedWorkflowEventCount AS [DeletedWorkflowEventCount],
    @DeletedWorkItemCount AS [DeletedWorkItemCount],
    @DeletedWorkItemEventCount AS [DeletedWorkItemEventCount],
    @DeletedOperationAuditCount AS [DeletedOperationAuditCount];
'@

        # <lang>
        #   <zh-CN>Remove 显式要求 ShouldProcess，同一 FixtureId 的准确双账号范围是唯一删除目标，避免按前缀或展示名扩大删除面。</zh-CN>
        #   <en>Remove explicitly requires ShouldProcess; the exact two-account scope for the same FixtureId is the only deletion target, avoiding prefix- or display-name-based expansion.</en>
        # </lang>
        if ($PSCmdlet.ShouldProcess("P19 browser fixture '$FixtureId'", 'Remove fixture accounts and their scoped P19 records')) {
            $removeResult = Invoke-PortalP19FixtureRowCommand -ConnectionString $connectionString -CommandText $removeCommandText -ConfigureParameters {
                param($command)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@ParticipantUserName' -SqlDbType NVarChar -Size 50 -Value $participantUserName)
                [void](Add-PortalP19SqlParameter -Command $command -Name '@AdministratorUserName' -SqlDbType NVarChar -Size 50 -Value $administratorUserName)
            }

            # <lang>
            #   <zh-CN>清理结果只报告删除计数和确定性账号名，作为审计证据而不泄露凭据或业务文本。</zh-CN>
            #   <en>Cleanup results report only deletion counts and deterministic account names as audit evidence without exposing credentials or business text.</en>
            # </lang>
            [pscustomobject]@{
                Action = 'Remove'
                FixtureId = $FixtureId
                ParticipantUserName = $participantUserName
                AdministratorUserName = $administratorUserName
                PreRemoveFixtureUserCount = $inspectResult.FixtureUserCount
                PreRemoveBusinessApplicationCount = $inspectResult.BusinessApplicationCount
                DeletedFixtureUserCount = $removeResult.DeletedFixtureUserCount
                DeletedPhysicalRoleMembershipCount = $removeResult.DeletedPhysicalRoleMembershipCount
                DeletedBusinessApplicationCount = $removeResult.DeletedBusinessApplicationCount
                DeletedWorkflowEventCount = $removeResult.DeletedWorkflowEventCount
                DeletedWorkItemCount = $removeResult.DeletedWorkItemCount
                DeletedWorkItemEventCount = $removeResult.DeletedWorkItemEventCount
                DeletedOperationAuditCount = $removeResult.DeletedOperationAuditCount
            }
        }
    }
}
