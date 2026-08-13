<#
.SYNOPSIS
.LANG en
Creates the VM task-agent package for legacy-browser automation.

.LANG zh-CN
创建用于旧浏览器自动化的 VM 任务代理包。

.LANG en
<lang>
  <en>Builds a portable VM-side task-agent package that polls a shared tasks folder, executes approved task commands, and writes structured logs/results back to the shared root. The package generator writes files and optionally a zip archive; it does not start the VM agent, does not run browser tests locally, and does not embed passwords or production secrets.</en>
  <zh-CN>生成 VM 侧任务代理包。该代理会轮询共享 tasks 目录，执行经任务清单声明的命令，并把结构化日志和结果写回共享根目录。包生成脚本只写入文件并可选生成 zip；它不会启动 VM 代理、不会在本机运行浏览器测试，也不会内嵌密码或生产密钥。</zh-CN>
</lang>

.PARAMETER OutputRoot
.LANG en
Local folder where generated package folders and zip files are written.

.LANG zh-CN
写入生成包目录和 zip 文件的本地目录。

.PARAMETER DeployRoot
.LANG en
Optional shared VM folder where the generated package is copied.

.LANG zh-CN
可选的 VM 共享目录，用于复制生成后的代理包。

.PARAMETER PollSeconds
.LANG en
Polling interval used by the generated VM agent.

.LANG zh-CN
生成的 VM 代理使用的轮询间隔。

.PARAMETER MaxTaskSeconds
.LANG en
Maximum execution time enforced by the generated VM agent.

.LANG zh-CN
生成的 VM 代理强制执行的单任务最长运行时间。
#>
[CmdletBinding()]
param(
    [string]$OutputRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'temp/vm-task-agent-packages'),

    [string]$PackageName = ('PortalVmTaskAgent-' + (Get-Date -Format 'yyyyMMdd-HHmmss')),

    [int]$PollSeconds = 5,

    [int]$MaxTaskSeconds = 1800,

    [string]$DeployRoot,

    [switch]$NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>以无 BOM UTF-8 和 CRLF 写入包内脚本、cmd、README 和 secret 提示，保持生成包的跨环境读取稳定。</zh-CN>
#   <en>Writes package scripts, cmd files, README text, and secret guidance as UTF-8 without BOM and CRLF for stable cross-environment reads.</en>
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
#   <zh-CN>把已生成的代理包内容复制到可选部署目录；不启动代理，也不复制 VM-local secret 正文。</zh-CN>
#   <en>Copies generated agent-package content to the optional deployment directory without starting the agent or copying VM-local secret contents.</en>
# </lang>
function Copy-AgentPackage {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Destination)) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }

    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
    }
}

# <lang>
#   <zh-CN>以下状态只描述本次包生成：输出根、包名、轮询和超时参数；生成过程不访问 VM 或运行浏览器。</zh-CN>
#   <en>The state below describes only this package generation—output root, package name, polling, and timeout settings—and never accesses the VM or runs a browser.</en>
# </lang>
$packageRoot = Join-Path $OutputRoot $PackageName
if (Test-Path -LiteralPath $packageRoot) {
    throw "Package folder already exists: $packageRoot"
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
# <lang>
#   <zh-CN>创建固定目录协议，包含任务、运行中、归档、日志、结果、包和 secret 边界；secret 目录仅留 VM 本地。</zh-CN>
#   <en>Creates the fixed task, running, archive, log, result, package, and secret directory contract; secret contents remain VM-local.</en>
# </lang>
foreach ($dir in @('tasks', 'running', 'archive/done', 'archive/failed', 'logs', 'results', 'packages', 'secrets')) {
    New-Item -ItemType Directory -Path (Join-Path $packageRoot $dir) -Force | Out-Null
}

$agentScript = @'
param(
    [int]$PollSeconds = __POLL_SECONDS__,
    [int]$MaxTaskSeconds = __MAX_TASK_SECONDS__,
    [string]$Root = ''
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>代理脚本只在生成包内部执行：所有路径均派生自 Root，轮询和任务处理不依赖主机进程。</zh-CN>
#   <en>The agent script runs only inside the generated package; all paths derive from Root and polling/task handling do not depend on host processes.</en>
# </lang>
if ([string]::IsNullOrEmpty($Root)) {
    $Root = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$Root = [System.IO.Path]::GetFullPath($Root)
$TaskDir = Join-Path $Root 'tasks'
$RunningDir = Join-Path $Root 'running'
$DoneDir = Join-Path $Root 'archive\done'
$FailedDir = Join-Path $Root 'archive\failed'
$LogsDir = Join-Path $Root 'logs'
$ResultsDir = Join-Path $Root 'results'
$PackagesDir = Join-Path $Root 'packages'
$SecretsDir = Join-Path $Root 'secrets'
$StopSignal = Join-Path $Root 'stop.signal'
$LockPath = Join-Path $Root 'agent.lock'

# <lang>
#   <zh-CN>为生成代理补齐固定目录，保持任务、运行中、归档、日志、结果、包和 secret 的边界。</zh-CN>
#   <en>Creates the fixed generated-agent directories so tasks, running items, archives, logs, results, packages, and secrets remain separated.</en>
# </lang>
function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

# <lang>
#   <zh-CN>以 ASCII 写入代理协议文本；调用方负责确保正文不包含需要保留的非 ASCII 敏感资料。</zh-CN>
#   <en>Writes agent-protocol text as ASCII; callers must keep the protocol body free of sensitive non-ASCII material that needs preservation.</en>
# </lang>
function Write-Text {
    param(
        [string]$Path,
        [string]$Text
    )
    $Text | Out-File -FilePath $Path -Encoding ASCII
}

# <lang>
#   <zh-CN>追加代理日志行，不主动读取或净化任务正文；上层只传入低敏状态摘要。</zh-CN>
#   <en>Appends an agent-log line without reading or sanitizing task bodies; callers pass only low-sensitivity status summaries.</en>
# </lang>
function Append-Text {
    param(
        [string]$Path,
        [string]$Text
    )
    $Text | Out-File -FilePath $Path -Append -Encoding ASCII
}

# <lang>
#   <zh-CN>生成本地代理协议使用的稳定时间文本，不承担时区转换或审计签名。</zh-CN>
#   <en>Generates the stable local time text used by the agent protocol without performing timezone conversion or audit signing.</en>
# </lang>
function Get-TimeStamp {
    return (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
}

# <lang>
#   <zh-CN>按日期写入代理日志文件，日志消息只记录任务状态和路径事实，不应包含密码、Token 或 Cookie。</zh-CN>
#   <en>Writes agent messages to a date-partitioned log while restricting callers to task state and path facts, never passwords, tokens, or cookies.</en>
# </lang>
function Write-AgentLog {
    param([string]$Message)
    $dateName = Get-Date -Format 'yyyyMMdd'
    $logPath = Join-Path $LogsDir ('agent-' + $dateName + '.log')
    Append-Text -Path $logPath -Text ((Get-TimeStamp) + ' ' + $Message)
}

# <lang>
#   <zh-CN>仅依据进程 ID 判断代理是否仍存活；查询失败回退为 false，不把异常细节写入结果。</zh-CN>
#   <en>Checks only whether a process ID is alive; query failures fall back to false without putting exception details into results.</en>
# </lang>
function Test-ProcessAlive {
    param([string]$ProcessIdText)
    if ([string]::IsNullOrEmpty($ProcessIdText)) {
        return $false
    }
    try {
        $processId = [int]$ProcessIdText
        $null = Get-Process -Id $processId -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

# <lang>
#   <zh-CN>从任务文件名去除固定扩展名得到任务 ID，不解析任务正文。</zh-CN>
#   <en>Derives a task ID by removing the fixed file suffix without parsing the task body.</en>
# </lang>
function Get-TaskId {
    param([string]$FileName)
    return ($FileName -replace '\.task\.cmd$', '')
}

# <lang>
#   <zh-CN>按日期构造成功或失败归档路径并确保目录存在，保持原任务文件名用于追溯。</zh-CN>
#   <en>Builds a date-partitioned success or failure archive path, ensuring its directory while preserving the original task file name for traceability.</en>
# </lang>
function Get-DateArchivePath {
    param(
        [string]$BasePath,
        [string]$TaskFileName
    )
    $dateName = Get-Date -Format 'yyyyMMdd'
    $archiveDir = Join-Path $BasePath $dateName
    Ensure-Directory -Path $archiveDir
    return Join-Path $archiveDir $TaskFileName
}

# <lang>
#   <zh-CN>将任务执行状态写为固定 INI 字段；仅写路径、时间和退出码，不复制任务正文或 secret。</zh-CN>
#   <en>Writes task status as fixed INI fields containing paths, times, and exit code only, never copying task bodies or secrets.</en>
# </lang>
function Write-ResultFile {
    param(
        [string]$TaskId,
        [string]$Status,
        [int]$ExitCode,
        [string]$StartedAt,
        [string]$EndedAt,
        [string]$TaskLog,
        [string]$ArchivePath
    )

    $resultPath = Join-Path $ResultsDir ($TaskId + '.result.ini')
    $lines = @(
        '[result]',
        ('TaskId=' + $TaskId),
        ('Status=' + $Status),
        ('ExitCode=' + $ExitCode),
        ('StartedAt=' + $StartedAt),
        ('EndedAt=' + $EndedAt),
        ('TaskLog=' + $TaskLog),
        ('ArchivePath=' + $ArchivePath)
    )
    Write-Text -Path $resultPath -Text ($lines -join "`r`n")
}

# <lang>
#   <zh-CN>领取并执行单个任务，负责进程超时、日志/结果和成功/失败归档；异常分支保持低敏摘要与最终清理。</zh-CN>
#   <en>Claims and executes one task, managing process timeout, logs/results, and success/failure archiving while keeping exception summaries low-sensitivity and cleanup deterministic.</en>
# </lang>
function Invoke-TaskFile {
    param([System.IO.FileInfo]$TaskFile)

    $taskId = Get-TaskId -FileName $TaskFile.Name
    $runningPath = Join-Path $RunningDir $TaskFile.Name
    $taskLog = Join-Path $LogsDir ($taskId + '.log')
    $startedAt = Get-TimeStamp
    $exitCode = 1
    $status = 'Failed'
    $archivePath = ''

    try {
        # <lang>
        #   <zh-CN>先将任务文件移动到 running 目录取得单一领取权；失败时只记录低敏跳过事实。</zh-CN>
        #   <en>Claims the task by moving it into running so only one agent owns it; failures record only a low-sensitivity skip fact.</en>
        # </lang>
        Move-Item -LiteralPath $TaskFile.FullName -Destination $runningPath -Force
    }
    catch {
        Write-AgentLog ('SKIP could not claim task ' + $TaskFile.Name + ': ' + $_.Exception.Message)
        return
    }

    # <lang>
    #   <zh-CN>建立任务日志和固定路径摘要，正文单独追加且不把 secret 内容提升到结果对象。</zh-CN>
    #   <en>Creates the task log and fixed path summary; the body is appended separately and never promoted into result objects.</en>
    # </lang>
    Write-Text -Path $taskLog -Text ('===== TASK ' + $taskId + ' START ' + $startedAt + ' =====')
    Append-Text -Path $taskLog -Text ('Root=' + $Root)
    Append-Text -Path $taskLog -Text ('TaskFile=' + $runningPath)
    Append-Text -Path $taskLog -Text ('Package=' + (Join-Path $PackagesDir $taskId))
    Append-Text -Path $taskLog -Text '----- TASK BODY -----'
    try {
        Get-Content -LiteralPath $runningPath | Out-File -FilePath $taskLog -Append -Encoding ASCII
    }
    catch {
        Append-Text -Path $taskLog -Text ('WARN cannot read task body: ' + $_.Exception.Message)
    }
    Append-Text -Path $taskLog -Text '----- PROCESS OUTPUT -----'

    Write-AgentLog ('START task=' + $taskId)

    $process = $null
    try {
        # <lang>
        #   <zh-CN>构造受限 cmd 子进程并只注入任务 ID、Root、包、日志和 secret 目录环境变量，避免凭据值进入参数。</zh-CN>
        #   <en>Starts a constrained cmd child process with only task ID, root, package, log, and secret-directory environment variables, keeping credential values out of arguments.</en>
        # </lang>
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $env:ComSpec
        $psi.Arguments = '/d /c call "' + $runningPath + '" >> "' + $taskLog + '" 2>&1'
        $psi.WorkingDirectory = $Root
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $psi.EnvironmentVariables['PORTAL_VM_AGENT_ROOT'] = $Root
        $psi.EnvironmentVariables['PORTAL_VM_TASK_ID'] = $taskId
        $psi.EnvironmentVariables['PORTAL_VM_TASK_PACKAGE'] = Join-Path $PackagesDir $taskId
        $psi.EnvironmentVariables['PORTAL_VM_LOG_PATH'] = $taskLog
        $psi.EnvironmentVariables['PORTAL_VM_SECRETS_DIR'] = $SecretsDir

        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $psi
        $null = $process.Start()

        # <lang>
        #   <zh-CN>等待受 MaxTaskSeconds 限制；超时尝试终止并写固定 124 事实，终止异常不覆盖超时结论。</zh-CN>
        #   <en>Waits within MaxTaskSeconds; on timeout it attempts termination and records fixed code 124 without replacing the timeout conclusion when kill fails.</en>
        # </lang>
        $completed = $process.WaitForExit($MaxTaskSeconds * 1000)
        if (-not $completed) {
            try {
                $process.Kill()
            }
            catch {
            }
            $exitCode = 124
            Append-Text -Path $taskLog -Text ('TASK TIMEOUT after ' + $MaxTaskSeconds + ' seconds.')
        }
        else {
            $exitCode = $process.ExitCode
        }
    }
    catch {
        $exitCode = 125
        Append-Text -Path $taskLog -Text ('TASK RUNNER ERROR: ' + $_.Exception.Message)
    }

    # <lang>
    #   <zh-CN>依据退出码选择成功/失败日期归档目录，再移动已领取任务；归档失败仅追加警告并继续写结果。</zh-CN>
    #   <en>Selects a success/failure date archive from the exit code, moves the claimed task, and appends a warning if archiving fails before writing the result.</en>
    # </lang>
    $endedAt = Get-TimeStamp
    if ($exitCode -eq 0) {
        $status = 'Done'
        $archivePath = Get-DateArchivePath -BasePath $DoneDir -TaskFileName $TaskFile.Name
    }
    else {
        $status = 'Failed'
        $archivePath = Get-DateArchivePath -BasePath $FailedDir -TaskFileName $TaskFile.Name
    }

    try {
        Move-Item -LiteralPath $runningPath -Destination $archivePath -Force
    }
    catch {
        Append-Text -Path $taskLog -Text ('WARN cannot archive task file: ' + $_.Exception.Message)
    }

    Append-Text -Path $taskLog -Text ('===== TASK ' + $taskId + ' END ' + $endedAt + ' EXIT ' + $exitCode + ' =====')
    Write-ResultFile -TaskId $taskId -Status $status -ExitCode $exitCode -StartedAt $startedAt -EndedAt $endedAt -TaskLog $taskLog -ArchivePath $archivePath
    Write-AgentLog ('END task=' + $taskId + ' status=' + $status + ' exit=' + $exitCode)
}

foreach ($dir in @($TaskDir, $RunningDir, $DoneDir, $FailedDir, $LogsDir, $ResultsDir, $PackagesDir, $SecretsDir)) {
    # <lang>
    #   <zh-CN>代理启动前确保所有运行目录存在；不触碰外部站点、数据库或浏览器状态。</zh-CN>
    #   <en>Ensures all runtime directories exist before agent startup without touching external sites, databases, or browser state.</en>
    # </lang>
    Ensure-Directory -Path $dir
}

if (Test-Path -LiteralPath $LockPath -PathType Leaf) {
    # <lang>
    #   <zh-CN>锁文件只用于单代理归属：活动 PID 导致退出，过期锁仅删除后继续，不输出任务正文。</zh-CN>
    #   <en>The lock file establishes single-agent ownership: an active PID exits, while a stale lock is removed before continuing without exposing task content.</en>
    # </lang>
    $oldPid = ''
    try {
        $oldPid = (Get-Content -LiteralPath $LockPath | Select-Object -First 1)
    }
    catch {
    }

    if (Test-ProcessAlive -ProcessIdText $oldPid) {
        Write-AgentLog ('EXIT another agent appears to be running pid=' + $oldPid)
        exit 2
    }
    else {
        Write-AgentLog ('REMOVE stale lock pid=' + $oldPid)
        Remove-Item -LiteralPath $LockPath -Force -ErrorAction SilentlyContinue
    }
}

# <lang>
#   <zh-CN>写入当前进程 PID 并记录低敏启动事实；代理 finally 负责删除锁文件。</zh-CN>
#   <en>Writes the current process ID and records a low-sensitivity startup fact; the agent finally block removes the lock.</en>
# </lang>
Write-Text -Path $LockPath -Text ([string]$PID)
Write-AgentLog ('START agent root=' + $Root + ' poll=' + $PollSeconds + ' maxTaskSeconds=' + $MaxTaskSeconds + ' pid=' + $PID)

try {
    while ($true) {
        # <lang>
        #   <zh-CN>每轮先消费一次性 stop.signal，再按文件时间顺序领取任务并等待 PollSeconds；轮询不访问浏览器或 HTTP。</zh-CN>
        #   <en>Each cycle consumes the one-shot stop.signal, claims tasks by file order, and sleeps PollSeconds without browser or HTTP access.</en>
        # </lang>
        if (Test-Path -LiteralPath $StopSignal -PathType Leaf) {
            Remove-Item -LiteralPath $StopSignal -Force -ErrorAction SilentlyContinue
            Write-AgentLog 'STOP signal received.'
            break
        }

        $tasks = @(Get-ChildItem -LiteralPath $TaskDir -Filter '*.task.cmd' -ErrorAction SilentlyContinue | Where-Object { -not $_.PSIsContainer } | Sort-Object LastWriteTime)
        foreach ($task in $tasks) {
            Invoke-TaskFile -TaskFile $task
        }

        Start-Sleep -Seconds $PollSeconds
    }
}
finally {
    # <lang>
    #   <zh-CN>无论轮询如何结束都删除锁并写停止事实，避免残留锁阻断下一次代理启动。</zh-CN>
    #   <en>Regardless of how polling ends, removes the lock and records a stop fact so stale ownership cannot block the next agent.</en>
    # </lang>
    Remove-Item -LiteralPath $LockPath -Force -ErrorAction SilentlyContinue
    Write-AgentLog 'EXIT agent stopped.'
}
'@

$agentScript = $agentScript.Replace('__POLL_SECONDS__', [string]$PollSeconds).Replace('__MAX_TASK_SECONDS__', [string]$MaxTaskSeconds)

# <lang>
#   <zh-CN>以下固定文件只构成包协议：启动/停止 cmd、样例任务、README 和 secret 占位说明，不包含真实凭据。</zh-CN>
#   <en>The fixed files below form only the package contract—start/stop cmd files, a sample task, README, and secret placeholders—and contain no real credentials.</en>
# </lang>
$startCmd = @'
@echo off
setlocal
pushd "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0x.ps1"
set AGENT_EXIT=%ERRORLEVEL%
popd
exit /b %AGENT_EXIT%
'@

$stopCmd = @'
@echo off
setlocal
type nul > "%~dp0stop.signal"
exit /b 0
'@

$sampleTask = @'
@echo off
echo Sample task started.
echo Agent root: %PORTAL_VM_AGENT_ROOT%
echo Task id: %PORTAL_VM_TASK_ID%
echo Package: %PORTAL_VM_TASK_PACKAGE%
ver
echo Sample task finished.
exit /b 0
'@

$readme = @'
Portal VM Task Agent

Purpose
-------
Run x.cmd once inside the Win7 VM shared folder. The agent polls tasks\*.task.cmd every few seconds, executes each task, writes logs and result files, and archives the task file.

Directory contract
------------------
tasks\*.task.cmd       New task records. Each file is a normal cmd script.
running\               Claimed task records while executing.
archive\done\yyyyMMdd  Successful task records.
archive\failed\yyyyMMdd Failed task records.
logs\                  Agent and task logs.
results\               *.result.ini status files.
packages\{TaskId}\     Optional task payload folder.
secrets\               VM-local secrets. Do not copy this folder back to git.
secrets\users\         Per-user password files: {username}.password.txt.

Usage
-----
1. Double-click x.cmd in the VM and leave it running.
2. On the host, drop a fully written *.task.cmd file into tasks.
3. Check results\{TaskId}.result.ini and logs\{TaskId}.log.
4. To stop the agent, run stop.cmd or create stop.signal.

Notes
-----
Task scripts must finish by themselves. Do not use pause or "Press any key".
Passwords, cookies and tokens must live in VM-local files under secrets\. For user switching, prefer secrets\users\{username}.password.txt. Task execution must not prompt for them.
'@

Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'x.ps1') -Content $agentScript
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'x.cmd') -Content $startCmd
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'stop.cmd') -Content $stopCmd
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'tasks/sample.task.cmd') -Content $sampleTask
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'README.txt') -Content $readme
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'secrets/README.txt') -Content 'Put VM-local secret files here, for example admin-password.txt. Do not commit or copy secrets back to git.'
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'secrets/users') -Force | Out-Null
Write-Utf8NoBomFile -Path (Join-Path $packageRoot 'secrets/users/README.txt') -Content 'Put per-user password files here, for example admin.password.txt or normal-user.password.txt. Do not commit or copy secrets back to git.'

$zipPath = $null
if (-not $NoZip) {
    # <lang>
    #   <zh-CN>可选压缩仅打包已生成的包根内容；不读取 secret 正文之外的外部目录。</zh-CN>
    #   <en>Optional compression archives only the generated package-root content and does not read external directories or secret bodies.</en>
    # </lang>
    $zipPath = $packageRoot + '.zip'
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -Force
}

if (-not [string]::IsNullOrWhiteSpace($DeployRoot)) {
    # <lang>
    #   <zh-CN>仅在显式提供 DeployRoot 时复制包内容；部署动作不启动代理，也不执行其中任务。</zh-CN>
    #   <en>Copies package content only when DeployRoot is explicit; deployment does not start the agent or execute tasks.</en>
    # </lang>
    Copy-AgentPackage -Source $packageRoot -Destination $DeployRoot
}

# <lang>
#   <zh-CN>返回包路径和固定运行参数，不返回 secret、任务日志或生成代理的执行结果。</zh-CN>
#   <en>Returns package paths and fixed runtime settings without returning secrets, task logs, or execution results from the generated agent.</en>
# </lang>
[pscustomobject]@{
    PackageRoot = $packageRoot
    ZipPath = $zipPath
    DeployRoot = $DeployRoot
    PollSeconds = $PollSeconds
    MaxTaskSeconds = $MaxTaskSeconds
}
