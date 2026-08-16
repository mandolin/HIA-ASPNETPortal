<#
.SYNOPSIS
.LANG en
Runs the portal automated test assembly through Visual Studio Test Platform.

.LANG zh-CN
通过 Visual Studio Test Platform 运行门户自动化测试程序集。

.DESCRIPTION
<lang>
  <zh-CN>还原并构建受控解决方案后，定位 VS Test Platform 的 `vstest.console.exe`，并用 MSTest adapter 运行 `Portal.Tests`。本脚本只执行本机测试入口，不连接真实数据库、不启动 IIS、不创建账号，也不读取生产配置。</zh-CN>
  <en>Restore and build the controlled solution, locate the Visual Studio Test Platform `vstest.console.exe`, and run `Portal.Tests` through the MSTest adapter. This script only executes the local test entrypoint; it does not connect to real databases, start IIS, create accounts, or read production configuration.</en>
</lang>

.PARAMETER Configuration
.LANG en
Build configuration for the solution and the test assembly path.

.LANG zh-CN
用于解决方案构建与测试程序集路径的构建配置。

.PARAMETER Platform
.LANG en
MSBuild platform value. The legacy solution normally uses Any CPU.

.LANG zh-CN
MSBuild 平台值。旧解决方案通常使用 Any CPU。

.PARAMETER NoRestore
.LANG en
Skips package restore when the caller already restored packages.

.LANG zh-CN
调用方已还原包时跳过包还原。

.PARAMETER NoBuild
.LANG en
Skips solution build and runs the existing test assembly.

.LANG zh-CN
跳过解决方案构建并运行现有测试程序集。
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$Platform = 'Any CPU',

    [switch]$NoRestore,

    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

function Find-PortalVsTestConsole {
    <#
    .SYNOPSIS
    .LANG en
    Finds `vstest.console.exe` from the current environment or installed Visual Studio instances.

    .LANG zh-CN
    从当前环境或已安装 Visual Studio 实例中查找 `vstest.console.exe`。

    .OUTPUTS
    .LANG en
    Absolute path to `vstest.console.exe`.

    .LANG zh-CN
    `vstest.console.exe` 的绝对路径。
    #>
    [CmdletBinding()]
    param()

    # <lang>
    #   <zh-CN>PATH 中的命令候选代表调用方显式提供的测试平台，优先级高于自动发现的 VS 安装。</zh-CN>
    #   <en>The command candidate from PATH represents a caller-provided test platform and takes precedence over auto-discovered Visual Studio installations.</en>
    # </lang>
    $pathCommand = Get-Command vstest.console.exe -ErrorAction SilentlyContinue
    if ($pathCommand) {
        # <lang>
        #   <zh-CN>返回命令来源路径，不继续扫描，避免同一次测试运行混用多个 VSTest 版本。</zh-CN>
        #   <en>Return the command source path without further scanning so one test run does not mix multiple VSTest versions.</en>
        # </lang>
        return $pathCommand.Source
    }

    # <lang>
    #   <zh-CN>vswhere 候选只覆盖 Visual Studio Installer 的标准位置；不存在的路径在枚举前过滤。</zh-CN>
    #   <en>vswhere candidates cover only standard Visual Studio Installer locations; missing paths are filtered before enumeration.</en>
    # </lang>
    $vswhereCandidates = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    foreach ($vswhere in $vswhereCandidates) {
        # <lang>
        #   <zh-CN>安装路径来自 vswhere 的最新实例查询，不读取或修改 VS 配置，只拼接测试平台的固定相对位置。</zh-CN>
        #   <en>The installation path comes from vswhere's latest-instance query; the script does not read or modify VS configuration and only appends the fixed test-platform relative path.</en>
        # </lang>
        $installationPath = & $vswhere -latest -property installationPath 2>$null
        if (-not $installationPath) {
            # <lang>
            #   <zh-CN>当前 vswhere 无实例结果时尝试下一个候选，不把单个安装器查询失败视为全局失败。</zh-CN>
            #   <en>When this vswhere candidate has no instance result, try the next candidate instead of treating one installer query as a global failure.</en>
            # </lang>
            continue
        }

        # <lang>
        #   <zh-CN>VS Test Platform 的固定路径用于 VS 17/18 系列本机安装，存在性检查是只读的。</zh-CN>
        #   <en>The fixed Visual Studio Test Platform path covers local VS 17/18 installations, and the existence check is read-only.</en>
        # </lang>
        $candidate = Join-Path $installationPath 'Common7\IDE\Extensions\TestPlatform\vstest.console.exe'
        if (Test-Path -LiteralPath $candidate) {
            # <lang>
            #   <zh-CN>找到首个可用测试平台后立即返回，保持测试日志中的工具来源唯一。</zh-CN>
            #   <en>Return the first usable test platform immediately so the test log has one unambiguous tool source.</en>
            # </lang>
            return $candidate
        }
    }

    # <lang>
    #   <zh-CN>无法定位测试平台时明确失败，避免构建成功后把未运行测试误报为通过。</zh-CN>
    #   <en>Fail explicitly when the test platform cannot be located so a successful build is not misreported as executed tests.</en>
    # </lang>
    throw 'vstest.console.exe not found. Install Visual Studio Test Platform or run from a Visual Studio developer environment.'
}

function New-PortalMSTestAdapterDirectory {
    <#
    .SYNOPSIS
    .LANG en
    Creates a temporary MSTest adapter directory with runtime dependencies for VSTest.

    .LANG zh-CN
    为 VSTest 创建包含运行时依赖的临时 MSTest adapter 目录。

    .PARAMETER RepositoryRoot
    .LANG en
    Absolute repository root used to resolve restored package paths.

    .LANG zh-CN
    用于解析已还原包路径的仓库根目录绝对路径。

    .OUTPUTS
    .LANG en
    Absolute path to the temporary adapter directory.

    .LANG zh-CN
    临时 adapter 目录的绝对路径。
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    # <lang>
    #   <zh-CN>adapter 源目录来自 MSTest.TestAdapter 包的 net462 buildTransitive 输出，是 VSTest 发现 MSTest v4 执行器的主入口。</zh-CN>
    #   <en>The adapter source directory comes from the MSTest.TestAdapter package's net462 buildTransitive output and is the main entrypoint for VSTest to discover the MSTest v4 executor.</en>
    # </lang>
    $adapterSource = Join-Path $RepositoryRoot 'src\packages\MSTest.TestAdapter.4.3.3\buildTransitive\net462'

    if (-not (Test-Path -LiteralPath $adapterSource)) {
        # <lang>
        #   <zh-CN>adapter 源目录缺失通常表示 restore 尚未成功完成；此时停止，避免 VSTest 以无 adapter 状态运行。</zh-CN>
        #   <en>A missing adapter source usually means restore has not completed successfully; stop instead of running VSTest without an adapter.</en>
        # </lang>
        throw "MSTest adapter source not found: $adapterSource"
    }

    # <lang>
    #   <zh-CN>临时目录放在系统 temp 下并带随机 GUID，避免多个测试运行互相覆盖 adapter 文件。</zh-CN>
    #   <en>The temporary directory lives under the system temp path and uses a random GUID so concurrent test runs do not overwrite adapter files.</en>
    # </lang>
    $temporaryAdapterPath = Join-Path ([System.IO.Path]::GetTempPath()) ('hia-portal-mstest-adapter-' + [System.Guid]::NewGuid().ToString('N'))

    # <lang>
    #   <zh-CN>创建目录后只复制测试 adapter 与运行时依赖；这些文件均来自 NuGet restore 产物，不进入 Git 提交范围。</zh-CN>
    #   <en>After creating the directory, copy only the test adapter and runtime dependencies; all files come from NuGet restore output and stay outside the Git commit scope.</en>
    # </lang>
    New-Item -ItemType Directory -Path $temporaryAdapterPath | Out-Null

    try {
        # <lang>
        #   <zh-CN>adapter 文件集合保留 MSTest.TestAdapter、PlatformServices、framework bridge 和 parallelize targets 的同目录装载关系。</zh-CN>
        #   <en>The adapter file set preserves same-directory loading for MSTest.TestAdapter, PlatformServices, framework bridge, and parallelize targets.</en>
        # </lang>
        Get-ChildItem -LiteralPath $adapterSource -File | Copy-Item -Destination $temporaryAdapterPath

        # <lang>
        #   <zh-CN>依赖 DLL 清单来自 MSTest.TestAdapter 4.3.3 net462 nuspec 依赖树和 System.Memory 4.6.3 的 net462 依赖；顺序不表达加载优先级。</zh-CN>
        #   <en>The dependency DLL list comes from the MSTest.TestAdapter 4.3.3 net462 nuspec dependency tree and System.Memory 4.6.3 net462 dependencies; order does not express load priority.</en>
        # </lang>
        $dependencyRelativePaths = @(
            'src\packages\System.Memory.4.6.3\lib\net462\System.Memory.dll',
            'src\packages\System.Threading.Tasks.Extensions.4.5.4\lib\net461\System.Threading.Tasks.Extensions.dll',
            'src\packages\System.Buffers.4.6.1\lib\net462\System.Buffers.dll',
            'src\packages\System.Numerics.Vectors.4.6.1\lib\net462\System.Numerics.Vectors.dll',
            'src\packages\System.Runtime.CompilerServices.Unsafe.6.1.2\lib\net462\System.Runtime.CompilerServices.Unsafe.dll'
        )

        foreach ($relativePath in $dependencyRelativePaths) {
            # <lang>
            #   <zh-CN>每个依赖在复制前都做存在性检查，缺包时给出精确路径，避免落到 VSTest 的模糊装载异常。</zh-CN>
            #   <en>Each dependency is checked before copying so missing packages report an exact path instead of falling through to VSTest's less precise load exception.</en>
            # </lang>
            $dependencySource = Join-Path $RepositoryRoot $relativePath
            if (-not (Test-Path -LiteralPath $dependencySource)) {
                # <lang>
                #   <zh-CN>缺失依赖说明 packages.config restore 尚未覆盖 adapter 的 runtime 图，必须停止并提示恢复包。</zh-CN>
                #   <en>A missing dependency means packages.config restore has not covered the adapter runtime graph, so stop and ask for package restore.</en>
                # </lang>
                throw "MSTest adapter dependency not found: $dependencySource. Run Restore-NuGetPackages.ps1 first."
            }

            # <lang>
            #   <zh-CN>复制到临时 adapter 根目录，让 .NET Framework loader 能在 adapter 同目录解析依赖。</zh-CN>
            #   <en>Copy the dependency to the temporary adapter root so the .NET Framework loader can resolve it beside the adapter.</en>
            # </lang>
            Copy-Item -LiteralPath $dependencySource -Destination $temporaryAdapterPath
        }

        # <lang>
        #   <zh-CN>返回临时目录路径；清理由调用方在 VSTest 结束后的 finally 块负责。</zh-CN>
        #   <en>Return the temporary directory path; cleanup is handled by the caller's finally block after VSTest exits.</en>
        # </lang>
        return $temporaryAdapterPath
    }
    catch {
        # <lang>
        #   <zh-CN>准备 adapter 失败时清理刚创建的 temp 子目录，避免缺包场景留下半成品可执行文件集合。</zh-CN>
        #   <en>When adapter preparation fails, clean the temp subdirectory just created so missing-package scenarios do not leave partial executable file sets.</en>
        # </lang>
        $resolvedAdapterPath = [System.IO.Path]::GetFullPath($temporaryAdapterPath)

        # <lang>
        #   <zh-CN>清理仍使用系统 temp 前缀校验，确保失败路径不会扩展到仓库或用户目录。</zh-CN>
        #   <en>Cleanup still uses the system-temp prefix check so a failure path cannot expand to the repository or user directories.</en>
        # </lang>
        $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())

        if ($resolvedAdapterPath.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
            -not [string]::Equals($resolvedAdapterPath, $tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $resolvedAdapterPath)) {
            # <lang>
            #   <zh-CN>删除当前失败运行创建的临时目录，不触碰其它测试运行或系统 temp 根。</zh-CN>
            #   <en>Delete only the temporary directory created for this failed run, not other test runs or the system temp root.</en>
            # </lang>
            Remove-Item -LiteralPath $resolvedAdapterPath -Recurse -Force
        }

        throw
    }
}

# <lang>
#   <zh-CN>仓库根目录由脚本位置推导，避免调用者当前目录改变测试对象。</zh-CN>
#   <en>Derive the repository root from the script location so the caller's current directory cannot change the test target.</en>
# </lang>
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path

# <lang>
#   <zh-CN>解决方案路径固定为受控主 solution；P28.1 不扫描或猜测其它 `.sln` 文件。</zh-CN>
#   <en>The solution path is fixed to the controlled main solution; P28.1 does not scan for or guess other `.sln` files.</en>
# </lang>
$solutionPath = Join-Path $repoRoot 'src\master.sln'

# <lang>
#   <zh-CN>restore/build helper 使用既有项目入口，减少测试 runner 对 NuGet/MSBuild 发现逻辑的重复实现。</zh-CN>
#   <en>The restore/build helpers reuse existing project entrypoints to avoid duplicating NuGet and MSBuild discovery logic in the test runner.</en>
# </lang>
$restoreScript = Join-Path $PSScriptRoot 'Restore-NuGetPackages.ps1'
$buildScript = Join-Path $PSScriptRoot 'Build-Solution.ps1'

# <lang>
#   <zh-CN>测试程序集路径随配置变化，但固定在新增测试项目输出目录下，不从用户输入拼接任意 DLL。</zh-CN>
#   <en>The test assembly path varies by configuration but remains fixed under the new test project's output directory instead of being assembled from arbitrary user input.</en>
# </lang>
$testAssembly = Join-Path $repoRoot ("src\Portal.Tests\bin\$Configuration\Portal.Tests.dll")

if (-not (Test-Path -LiteralPath $solutionPath)) {
    # <lang>
    #   <zh-CN>主 solution 缺失代表仓库结构不完整，必须停止。</zh-CN>
    #   <en>A missing main solution means the repository structure is incomplete and must stop the run.</en>
    # </lang>
    throw "Solution file not found: $solutionPath"
}

if (-not $NoRestore) {
    # <lang>
    #   <zh-CN>默认先还原 packages.config 依赖，确保新测试项目的 MSTest framework 和 adapter 可被 MSBuild 与 VSTest 找到。</zh-CN>
    #   <en>By default, restore packages.config dependencies first so MSBuild and VSTest can find the new test project's MSTest framework and adapter.</en>
    # </lang>
    & $restoreScript
    if ($LASTEXITCODE -ne 0) {
        # <lang>
        #   <zh-CN>还原失败直接传递退出码，避免后续构建产生二次噪声。</zh-CN>
        #   <en>Propagate restore failure directly to avoid secondary build noise.</en>
        # </lang>
        exit $LASTEXITCODE
    }
}

if (-not $NoBuild) {
    # <lang>
    #   <zh-CN>测试运行前构建主 solution，确保项目引用和 test assembly 都来自同一源码状态。</zh-CN>
    #   <en>Build the main solution before running tests so project references and the test assembly come from the same source state.</en>
    # </lang>
    & $buildScript -Configuration $Configuration -Platform $Platform
    if ($LASTEXITCODE -ne 0) {
        # <lang>
        #   <zh-CN>构建失败时返回 MSBuild 退出码，不尝试运行旧 DLL。</zh-CN>
        #   <en>When the build fails, return MSBuild's exit code and do not attempt to run an old DLL.</en>
        # </lang>
        exit $LASTEXITCODE
    }
}

if (-not (Test-Path -LiteralPath $testAssembly)) {
    # <lang>
    #   <zh-CN>缺少测试程序集说明构建被跳过或输出目录异常，必须停止以免误报测试通过。</zh-CN>
    #   <en>A missing test assembly means the build was skipped or the output directory is unexpected, so stop to avoid a false test pass.</en>
    # </lang>
    throw "Test assembly not found: $testAssembly"
}

# <lang>
#   <zh-CN>VSTest 路径在最后解析，保证 restore/build 的错误先暴露，工具定位失败不会掩盖编译问题。</zh-CN>
#   <en>Resolve VSTest last so restore/build errors surface first and tool discovery does not hide compilation problems.</en>
# </lang>
$vstest = Find-PortalVsTestConsole

# <lang>
#   <zh-CN>临时 adapter 目录把 MSTest adapter 与 packages.config 显式依赖放到同一目录，避免 VSTest 在 adapter 装载阶段缺 runtime DLL。</zh-CN>
#   <en>The temporary adapter directory places the MSTest adapter and packages.config explicit dependencies side by side, avoiding missing runtime DLLs during VSTest adapter loading.</en>
# </lang>
$testAdapterPath = New-PortalMSTestAdapterDirectory -RepositoryRoot $repoRoot

# <lang>
#   <zh-CN>输出本次测试运行的核心输入，方便 WorkZone 证据记录，同时不包含凭据、连接串或 Cookie。</zh-CN>
#   <en>Report the core inputs for this test run so WorkZone evidence can record them without credentials, connection strings, or cookies.</en>
# </lang>
Write-Host "Testing $testAssembly"
Write-Host "VSTest: $vstest"
Write-Host "Adapter: $testAdapterPath"

try {
    # <lang>
    #   <zh-CN>通过明确的 adapter 路径运行 MSTest 程序集，并先保存 VSTest 退出码以便 finally 清理临时目录。</zh-CN>
    #   <en>Run the MSTest assembly with an explicit adapter path and store VSTest's exit code before the finally block removes the temporary directory.</en>
    # </lang>
    & $vstest $testAssembly "/TestAdapterPath:$testAdapterPath" /Logger:trx
    $testExitCode = $LASTEXITCODE
}
finally {
    # <lang>
    #   <zh-CN>只删除本脚本创建的系统 temp 子目录；路径前缀校验防止误删仓库、用户目录或宽泛临时根。</zh-CN>
    #   <en>Remove only the system-temp subdirectory created by this script; prefix checks prevent accidental deletion of the repository, user directories, or the broad temp root.</en>
    # </lang>
    $resolvedAdapterPath = [System.IO.Path]::GetFullPath($testAdapterPath)

    # <lang>
    #   <zh-CN>系统 temp 根用于约束清理范围，必须是 adapter 路径的前缀且二者不能相等。</zh-CN>
    #   <en>The system temp root constrains cleanup scope and must prefix the adapter path while not being equal to it.</en>
    # </lang>
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())

    if ($resolvedAdapterPath.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        -not [string]::Equals($resolvedAdapterPath, $tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedAdapterPath)) {
        # <lang>
        #   <zh-CN>清理临时 adapter 文件，避免多次测试运行在用户 temp 中堆积可执行 DLL。</zh-CN>
        #   <en>Clean temporary adapter files so repeated test runs do not accumulate executable DLLs in the user's temp directory.</en>
        # </lang>
        Remove-Item -LiteralPath $resolvedAdapterPath -Recurse -Force
    }
}

exit $testExitCode
