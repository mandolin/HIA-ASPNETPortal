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

function Write-Utf8NoBomFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    $normalized = [regex]::Replace($Content, "`r?`n", "`r`n")
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

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

$packageRoot = Join-Path $OutputRoot $PackageName
if (Test-Path -LiteralPath $packageRoot) {
    throw "Package folder already exists: $packageRoot"
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
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

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Write-Text {
    param(
        [string]$Path,
        [string]$Text
    )
    $Text | Out-File -FilePath $Path -Encoding ASCII
}

function Append-Text {
    param(
        [string]$Path,
        [string]$Text
    )
    $Text | Out-File -FilePath $Path -Append -Encoding ASCII
}

function Get-TimeStamp {
    return (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
}

function Write-AgentLog {
    param([string]$Message)
    $dateName = Get-Date -Format 'yyyyMMdd'
    $logPath = Join-Path $LogsDir ('agent-' + $dateName + '.log')
    Append-Text -Path $logPath -Text ((Get-TimeStamp) + ' ' + $Message)
}

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

function Get-TaskId {
    param([string]$FileName)
    return ($FileName -replace '\.task\.cmd$', '')
}

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
        Move-Item -LiteralPath $TaskFile.FullName -Destination $runningPath -Force
    }
    catch {
        Write-AgentLog ('SKIP could not claim task ' + $TaskFile.Name + ': ' + $_.Exception.Message)
        return
    }

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
    Ensure-Directory -Path $dir
}

if (Test-Path -LiteralPath $LockPath -PathType Leaf) {
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

Write-Text -Path $LockPath -Text ([string]$PID)
Write-AgentLog ('START agent root=' + $Root + ' poll=' + $PollSeconds + ' maxTaskSeconds=' + $MaxTaskSeconds + ' pid=' + $PID)

try {
    while ($true) {
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
    Remove-Item -LiteralPath $LockPath -Force -ErrorAction SilentlyContinue
    Write-AgentLog 'EXIT agent stopped.'
}
'@

$agentScript = $agentScript.Replace('__POLL_SECONDS__', [string]$PollSeconds).Replace('__MAX_TASK_SECONDS__', [string]$MaxTaskSeconds)

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
    $zipPath = $packageRoot + '.zip'
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -Force
}

if (-not [string]::IsNullOrWhiteSpace($DeployRoot)) {
    Copy-AgentPackage -Source $packageRoot -Destination $DeployRoot
}

[pscustomobject]@{
    PackageRoot = $packageRoot
    ZipPath = $zipPath
    DeployRoot = $DeployRoot
    PollSeconds = $PollSeconds
    MaxTaskSeconds = $MaxTaskSeconds
}
