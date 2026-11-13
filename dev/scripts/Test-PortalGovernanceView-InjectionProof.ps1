<#
.SYNOPSIS
    W-anp-P325 注入法验证：治理视图 L0 门禁能否捕获真实契约漂移。

.DESCRIPTION
    可复用、参数化的注入证明（非硬编码数据行）：
      1. 备份目标源文件；
      2. 把一个能力权限前缀常量（默认 BusinessCollaborationKeyPrefix）的值改写为一个
         与任何 PortalPermissionKeys 键族都不匹配的漂移值；
      3. 运行 Test-PortalGovernanceView.ps1，断言其以非 0 退出（L0 硬断言捕获漂移）；
      4. 用备份恢复原值；
      5. 再次运行门禁，断言其以 0 退出（漂移消除后回到 PASS）。

    本脚本只改一次常量取值并立即还原，不残留任何漂移；失败时会尽力还原备份。

.PARAMETER RepoRoot
    仓库根目录（默认取脚本两级上层目录）。
.PARAMETER GateScript
    被验证的治理视图门禁脚本路径。
.PARAMETER TargetFile
    注入漂移所在的源文件（默认 src/Portal.Components/PortalPermissions.cs）。
.PARAMETER ConstName
    被改写的权限前缀常量名（默认 BusinessCollaborationKeyPrefix）。
.PARAMETER DriftValue
    注入的漂移取值（默认 Business.DriftInjected，刻意不匹配任何键族）。
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string]$GateScript = (Join-Path $RepoRoot 'dev/scripts/Test-PortalGovernanceView.ps1'),
    [string]$TargetFile = (Join-Path $RepoRoot 'src/Portal.Components/PortalPermissions.cs'),
    [string]$ConstName = 'BusinessCollaborationKeyPrefix',
    [string]$DriftValue = 'Business.DriftInjected'
)

$ErrorActionPreference = 'Stop'
$bak = "$TargetFile.injection-bak"

function Invoke-Gate {
    & pwsh -NoProfile -File $GateScript 2>&1 | Out-Null
    return $LASTEXITCODE
}

if (-not (Test-Path $TargetFile)) { Write-Host "目标文件缺失: $TargetFile" -ForegroundColor Red; exit 2 }
if (-not (Test-Path $GateScript)) { Write-Host "门禁脚本缺失: $GateScript" -ForegroundColor Red; exit 2 }

# 1. 备份
Copy-Item $TargetFile $bak -Force
try {
    $text = Get-Content $TargetFile -Raw -Encoding UTF8
    # 匹配：public const string <ConstName> = "<任意>";
    $pat = 'public const string ' + [regex]::Escape($ConstName) + ' = "([^"]*)"'
    $m = [regex]::Match($text, $pat)
    if (-not $m.Success) {
        Write-Host ("未能在目标文件定位常量 {0}，注入中止" -f $ConstName) -ForegroundColor Red
        Remove-Item $bak -Force
        exit 2
    }
    $original = $m.Groups[1].Value
    if ($original -eq $DriftValue) {
        Write-Host ("常量 {0} 当前值已是漂移值，跳过以免误判" -f $ConstName) -ForegroundColor Yellow
        Remove-Item $bak -Force
        exit 0
    }
    Write-Host ("已备份 {0}（原值 {1}）" -f $ConstName, $original) -ForegroundColor Cyan

    # 2. 注入漂移
    $drifted = $text -replace $pat, ('public const string ' + $ConstName + ' = "' + $DriftValue + '"')
    Set-Content $TargetFile $drifted -Encoding UTF8 -NoNewline

    # 3. 断言门禁捕获漂移（exit != 0）
    $exitDrift = Invoke-Gate
    if ($exitDrift -eq 0) {
        Write-Host ("[FAIL] 注入漂移后门禁未拦截（exit=0），L0 断言失效" ) -ForegroundColor Red
        throw "gate did not catch injected drift"
    }
    Write-Host ("[ OK ] 注入漂移后门禁拦截（exit=$exitDrift），L0 断言有效" ) -ForegroundColor Green

    # 4. 还原
    Copy-Item $bak $TargetFile -Force
    Write-Host ("已还原 {0} 原值" -f $ConstName) -ForegroundColor Cyan

    # 5. 断言恢复后回到 PASS（exit == 0）
    $exitRestore = Invoke-Gate
    if ($exitRestore -ne 0) {
        Write-Host ("[FAIL] 还原后门禁仍非 0（exit=$exitRestore），可能存在残留漂移" ) -ForegroundColor Red
        throw "gate still failing after restore"
    }
    Write-Host ("[ OK ] 还原后门禁回到 PASS（exit=0）" ) -ForegroundColor Green

    Write-Host ''
    Write-Host '=== 注入法验证通过：治理视图 L0 门禁可捕获真实契约漂移且可恢复 ===' -ForegroundColor Cyan
    exit 0
}
finally {
    if (Test-Path $bak) { Remove-Item $bak -Force }
}
