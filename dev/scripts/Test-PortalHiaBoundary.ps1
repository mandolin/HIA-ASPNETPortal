[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$projectPath = Join-Path $repoRoot 'src\Portal.HiaBoundaryProof\Portal.HiaBoundaryProof.csproj'
$fixtureDirectory = Join-Path $repoRoot 'src\Portal.HiaBoundaryProof\Fixtures'
$findMsBuild = Join-Path $PSScriptRoot 'Find-MsBuild.ps1'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "HIA boundary proof project not found: $projectPath"
}

if (-not (Test-Path -LiteralPath $fixtureDirectory)) {
    throw "HIA boundary proof fixtures not found: $fixtureDirectory"
}

$msbuild = & $findMsBuild

# proof 不加入主解决方案，单独构建以验证默认门户路径不需要 HIA 运行时依赖。
Write-Host "Building HIA boundary proof project"
& $msbuild $projectPath /t:Build "/p:Configuration=$Configuration" '/p:Platform=AnyCPU' /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputDirectory = Join-Path $repoRoot ("temp\hia-boundary-proof\bin\{0}" -f $Configuration)
$proofExecutable = Join-Path $outputDirectory 'Portal.HiaBoundaryProof.exe'
if (-not (Test-Path -LiteralPath $proofExecutable)) {
    throw "HIA boundary proof executable not found: $proofExecutable"
}

Write-Host "Running HIA boundary contract fixture proof"
& $proofExecutable '--fixtures' $fixtureDirectory
exit $LASTEXITCODE
