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

function Write-Utf8NoBomFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    $normalized = [regex]::Replace($Content, "`r?`n", "`r`n")
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

function Get-SafeName {
    param([string]$Value)

    $safe = $Value -replace '[^A-Za-z0-9_.@-]+', '-'
    $safe = $safe.Trim('-')
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'PortalVmTask'
    }

    return $safe
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

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

$agentRootFull = [System.IO.Path]::GetFullPath($AgentRoot)
if (-not (Test-Path -LiteralPath $agentRootFull -PathType Container)) {
    throw "Agent root not found: $agentRootFull"
}

foreach ($dir in @('tasks', 'packages', 'results', 'logs')) {
    Ensure-Directory -Path (Join-Path $agentRootFull $dir)
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskId = (Get-SafeName -Value $TaskName) + '-' + $stamp
$packagePath = Join-Path (Join-Path $agentRootFull 'packages') $taskId

if (-not [string]::IsNullOrWhiteSpace($PackageZip) -or -not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    Ensure-Directory -Path $packagePath
}

if (-not [string]::IsNullOrWhiteSpace($PackageZip)) {
    if (-not (Test-Path -LiteralPath $PackageZip -PathType Leaf)) {
        throw "Package zip not found: $PackageZip"
    }

    Expand-Archive -LiteralPath $PackageZip -DestinationPath $packagePath -Force
}

if (-not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
        throw "Package directory not found: $PackageDirectory"
    }

    Get-ChildItem -LiteralPath $PackageDirectory -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $packagePath -Recurse -Force
    }
}

if ($PSCmdlet.ParameterSetName -eq 'CommandFile') {
    if (-not (Test-Path -LiteralPath $CommandFile -PathType Leaf)) {
        throw "Command file not found: $CommandFile"
    }
    $taskBody = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $CommandFile).Path, [System.Text.UTF8Encoding]::new($false))
}
elseif ($Command -and $Command.Count -gt 0) {
    $taskBody = ($Command -join "`r`n")
}
else {
    $runUserSecretName = Get-SafeName -Value $RunUser
    $taskBody = New-DefaultPackageTaskBody -TaskId $taskId -RunUser $RunUser -RunUserSecretName $runUserSecretName
}

$tasksDir = Join-Path $agentRootFull 'tasks'
$taskPath = Join-Path $tasksDir ($taskId + '.task.cmd')
$tempPath = $taskPath + '.tmp'
Write-Utf8NoBomFile -Path $tempPath -Content $taskBody
Move-Item -LiteralPath $tempPath -Destination $taskPath -Force

$resultPath = Join-Path (Join-Path $agentRootFull 'results') ($taskId + '.result.ini')

if ($Wait) {
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
