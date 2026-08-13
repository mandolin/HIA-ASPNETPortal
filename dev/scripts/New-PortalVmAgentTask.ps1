<#
.SYNOPSIS
.LANG en
Creates a task for the Win7/legacy-browser VM task agent.

.LANG zh-CN
为 Win7/旧浏览器 VM 任务代理创建任务。

.LANG en
<lang>
  <en>Writes a task manifest and optional package reference into the shared VM agent directory so the already-running VM agent can execute it. The script may copy command text and package paths to the shared folder, but it does not execute the task locally and should never include raw passwords, tokens, cookies, or connection strings in the task record. Use password placeholders resolved inside the VM-side credential files when authentication is required.</en>
  <zh-CN>向共享的 VM 代理目录写入任务清单和可选包引用，使已经运行的 VM 代理能够执行该任务。本脚本可能把命令文本和包路径写入共享目录，但不会在本机执行任务；任务记录中绝不能写入原始密码、Token、Cookie 或连接串。需要认证时，应使用占位符，由 VM 侧凭据文件解析。</zh-CN>
</lang>

.PARAMETER AgentRoot
.LANG en
Shared root directory watched by the VM task agent.

.LANG zh-CN
VM 任务代理监听的共享根目录。

.PARAMETER TaskName
.LANG en
Human-readable task name used in manifest and result files.

.LANG zh-CN
写入任务清单和结果文件的人类可读任务名称。

.PARAMETER Command
.LANG en
Command lines to execute inside the VM agent.

.LANG zh-CN
由 VM 代理在虚拟机内执行的命令行。

.PARAMETER RunUser
.LANG en
Logical user key used by the VM-side credential resolver.

.LANG zh-CN
供 VM 侧凭据解析器使用的逻辑用户键。
#>
[CmdletBinding(DefaultParameterSetName = 'Command')]
param(
    [string]$AgentRoot = '\\192.168.199.124\Temp\HIA-ASPNETPortal',

    [string]$TaskName = 'PortalVmTask',

    [Parameter(ParameterSetName = 'Command')]
    [string[]]$Command,

    [Parameter(ParameterSetName = 'CommandFile')]
    [string]$CommandFile,

    [string]$PackageZip,

    [string]$PackageDirectory,

    [string]$RunUser = 'admin',

    [switch]$Wait,

    [int]$TimeoutSeconds = 1800
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>以无 BOM UTF-8 和 CRLF 写入任务正文，避免任务文件因编码差异破坏 cmd/PowerShell 解析。</zh-CN>
#   <en>Writes task content as UTF-8 without BOM and CRLF so cmd/PowerShell parsing is not affected by encoding differences.</en>
# </lang>
function Write-Utf8NoBomFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    $normalized = [regex]::Replace($Content, "`r?`n", "`r`n")
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

# <lang>
#   <zh-CN>将任务标签压缩为可安全用于文件名和任务标识的字符集；空结果回退为稳定的 PortalVmTask。</zh-CN>
#   <en>Reduces a task label to characters safe for file names and task identifiers, falling back to the stable PortalVmTask name when empty.</en>
# </lang>
function Get-SafeName {
    param([string]$Value)

    $safe = $Value -replace '[^A-Za-z0-9_.@-]+', '-'
    $safe = $safe.Trim('-')
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'PortalVmTask'
    }

    return $safe
}

# <lang>
#   <zh-CN>确保共享代理所需目录存在；只负责目录准备，不执行任务或清理既有文件。</zh-CN>
#   <en>Ensures the shared-agent directory exists without executing tasks or deleting existing files.</en>
# </lang>
function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

# <lang>
#   <zh-CN>生成默认任务 cmd 正文，只写入逻辑用户和 secret 文件名占位符，不嵌入密码正文。</zh-CN>
#   <en>Generates the default task cmd body with only a logical user and secret-file name placeholder, never embedding password material.</en>
# </lang>
function New-DefaultPackageTaskBody {
    param(
        [string]$TaskId,
        [string]$RunUser,
        [string]$RunUserSecretName
    )

    return @"
@echo off
setlocal
echo TASK %PORTAL_VM_TASK_ID% START
set "PORTAL_VM_RUN_USER=$RunUser"
if exist "%PORTAL_VM_TASK_PACKAGE%\run.cmd" (
    call "%PORTAL_VM_TASK_PACKAGE%\run.cmd"
) else if exist "%PORTAL_VM_TASK_PACKAGE%\run-smoke.ps1" (
    set "PORTAL_VM_PASSWORD_FILE=%PORTAL_VM_SECRETS_DIR%\users\$RunUserSecretName.password.txt"
    if not exist "%PORTAL_VM_PASSWORD_FILE%" (
        set "PORTAL_VM_PASSWORD_FILE=%PORTAL_VM_SECRETS_DIR%\admin-password.txt"
    )
    if not exist "%PORTAL_VM_PASSWORD_FILE%" (
        echo Missing secret file for user %PORTAL_VM_RUN_USER%: %PORTAL_VM_SECRETS_DIR%\users\$RunUserSecretName.password.txt
        echo Legacy fallback also missing: %PORTAL_VM_SECRETS_DIR%\admin-password.txt
        exit /b 20
    )
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PORTAL_VM_TASK_PACKAGE%\run-smoke.ps1" -AdminUser "%PORTAL_VM_RUN_USER%" -AdminPasswordFile "%PORTAL_VM_PASSWORD_FILE%"
) else (
    echo Package command not found for task $TaskId
    exit /b 3
)
set TASK_EXIT=%ERRORLEVEL%
echo TASK %PORTAL_VM_TASK_ID% END EXIT %TASK_EXIT%
exit /b %TASK_EXIT%
"@
}

# <lang>
#   <zh-CN>以下编排只生成任务投递事实：规范化 AgentRoot、任务 ID、可选包目录和命令正文；不会在本机执行代理。</zh-CN>
#   <en>The orchestration below only prepares task-delivery facts—normalized AgentRoot, task ID, optional package content, and command body—and never runs the agent locally.</en>
# </lang>
$agentRootFull = [System.IO.Path]::GetFullPath($AgentRoot)
if (-not (Test-Path -LiteralPath $agentRootFull -PathType Container)) {
    throw "Agent root not found: $agentRootFull"
}

foreach ($dir in @('tasks', 'packages', 'results', 'logs')) {
    # <lang>
    #   <zh-CN>任务投递前先建立固定目录契约，后续写入只落在 AgentRoot 子目录。</zh-CN>
    #   <en>Establishes the fixed directory contract before delivery so later writes stay under AgentRoot subdirectories.</en>
    # </lang>
    Ensure-Directory -Path (Join-Path $agentRootFull $dir)
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskId = (Get-SafeName -Value $TaskName) + '-' + $stamp
$packagePath = Join-Path (Join-Path $agentRootFull 'packages') $taskId

if (-not [string]::IsNullOrWhiteSpace($PackageZip) -or -not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    # <lang>
    #   <zh-CN>仅在调用方提供包来源时创建本次任务专属目录，避免无包任务产生无意义 payload。</zh-CN>
    #   <en>Creates a task-specific package directory only when a package source is supplied, avoiding meaningless payload folders.</en>
    # </lang>
    Ensure-Directory -Path $packagePath
}

if (-not [string]::IsNullOrWhiteSpace($PackageZip)) {
    # <lang>
    #   <zh-CN>校验并解压调用方指定的包；包内容仍由代理端按任务 ID 使用，本脚本不执行其中命令。</zh-CN>
    #   <en>Validates and expands the caller-supplied package; the agent consumes it by task ID, while this script never executes its commands.</en>
    # </lang>
    if (-not (Test-Path -LiteralPath $PackageZip -PathType Leaf)) {
        throw "Package zip not found: $PackageZip"
    }

    Expand-Archive -LiteralPath $PackageZip -DestinationPath $packagePath -Force
}

if (-not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    # <lang>
    #   <zh-CN>复制目录内容而非目录本身，保持生成包根与任务 ID 一致，并保留既有文件覆盖行为。</zh-CN>
    #   <en>Copies directory contents rather than the directory itself so the package root matches the task ID while retaining existing overwrite behavior.</en>
    # </lang>
    if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
        throw "Package directory not found: $PackageDirectory"
    }

    Get-ChildItem -LiteralPath $PackageDirectory -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $packagePath -Recurse -Force
    }
}

if ($PSCmdlet.ParameterSetName -eq 'CommandFile') {
    # <lang>
    #   <zh-CN>从文件读取已准备好的任务正文；不在此处解析、执行或记录其中可能存在的敏感文本。</zh-CN>
    #   <en>Reads prepared task content from a file without parsing, executing, or logging any potentially sensitive text.</en>
    # </lang>
    if (-not (Test-Path -LiteralPath $CommandFile -PathType Leaf)) {
        throw "Command file not found: $CommandFile"
    }
    $taskBody = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $CommandFile).Path, [System.Text.UTF8Encoding]::new($false))
}
elseif ($Command -and $Command.Count -gt 0) {
    # <lang>
    #   <zh-CN>把显式命令数组按 cmd 行分隔拼接，保持调用方给出的命令顺序。</zh-CN>
    #   <en>Joins explicit command lines with cmd-compatible separators while preserving caller order.</en>
    # </lang>
    $taskBody = ($Command -join "`r`n")
}
else {
    # <lang>
    #   <zh-CN>无显式正文时只生成引用 VM secret 文件的默认任务，不把凭据值带入任务文件。</zh-CN>
    #   <en>When no explicit body is supplied, creates a default task that references VM secret files without carrying credential values into the task file.</en>
    # </lang>
    $runUserSecretName = Get-SafeName -Value $RunUser
    $taskBody = New-DefaultPackageTaskBody -TaskId $taskId -RunUser $RunUser -RunUserSecretName $runUserSecretName
}

$tasksDir = Join-Path $agentRootFull 'tasks'
$taskPath = Join-Path $tasksDir ($taskId + '.task.cmd')
$tempPath = $taskPath + '.tmp'
# <lang>
#   <zh-CN>先写临时任务文件再替换到 tasks 目录，避免代理读取半写入正文。</zh-CN>
#   <en>Writes a temporary task file before replacing the final tasks entry so the agent cannot read a partially written body.</en>
# </lang>
Write-Utf8NoBomFile -Path $tempPath -Content $taskBody
Move-Item -LiteralPath $tempPath -Destination $taskPath -Force

$resultPath = Join-Path (Join-Path $agentRootFull 'results') ($taskId + '.result.ini')

if ($Wait) {
    # <lang>
    #   <zh-CN>等待仅轮询非敏感 result 文件是否出现，并受 TimeoutSeconds 限制；不读取或输出任务日志正文。</zh-CN>
    #   <en>Waits only for the non-sensitive result file to appear within TimeoutSeconds and never reads or emits task-log contents.</en>
    # </lang>
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
            break
        }
        Start-Sleep -Seconds 2
    }
}

[pscustomobject]@{
    AgentRoot = $agentRootFull
    TaskId = $taskId
    TaskPath = $taskPath
    PackagePath = if (Test-Path -LiteralPath $packagePath) { $packagePath } else { $null }
    ResultPath = $resultPath
    ResultExists = Test-Path -LiteralPath $resultPath -PathType Leaf
}
