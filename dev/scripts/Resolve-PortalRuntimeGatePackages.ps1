<#
.SYNOPSIS
  <lang>
    <zh-CN>解析运行期门禁切档位时应当写入 `Portal.ModulePackages.Enabled` 的包集合（基线显式启用包 ∪ 本组被测包）。</zh-CN>
    <en>Resolves the package set that runtime gates must write into `Portal.ModulePackages.Enabled` when switching profiles (baseline explicitly enabled packages ∪ the packages probed by this group).</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>本脚本只定义函数，供运行期门禁 dot-source 后调用，自身不产生副作用、不连接数据库、不写配置。
    存在的理由（`W-anp-P137` 实测根因）：`Portal.ModulePackages.Enabled` 是**部署级显式启用**列表，
    `Web.config` 与 `src/Portal/Config/appSettings.json` 的基线上写着 `HIA.LeaveRequest,HIA.ExpenseReimbursement` ——
    这两个包不在任何 Profile 的 `Packages`/`Includes` 图里，只靠 `Enabled` 进入门户。
    而 `Invoke-PortalModuleRuntimeGate.ps1` 与 `Invoke-PortalGateSuite.ps1` 切档位时把该键**整体替换**为
    `HIA.ModuleProbe`，于是 `LeaveRequest` / `ExpenseReimbursement` 被 Profile 门禁阻断、**不渲染**，
    门禁却把"部署策略不允许"读成"模块根找不到"（`modulePresent=false`，`errorPage=false`）——产品侧没问题，是门禁自伤。
    正确语义是**并集**：基线显式启用的是本部署既成事实，被测包是本次验证必须可见的对象，二者都不能被替换掉。
    证明边界：本脚本只证明"包集合按并集求得且非空"，不证明包已安装、模块已挂载或页面已授权。</zh-CN>
    <en>This script only defines functions for runtime gates to dot-source; it has no side effects, opens no database, and writes no configuration.
    Reason it exists (measured root cause of `W-anp-P137`): `Portal.ModulePackages.Enabled` is the **deployment-level explicit
    enablement** list, and the baseline in `Web.config` / `src/Portal/Config/appSettings.json` carries
    `HIA.LeaveRequest,HIA.ExpenseReimbursement` — two packages that appear in **no** Profile's `Packages`/`Includes` graph and
    reach the Portal only through `Enabled`. When switching profiles, `Invoke-PortalModuleRuntimeGate.ps1` and
    `Invoke-PortalGateSuite.ps1` **replaced** that key wholesale with `HIA.ModuleProbe`, so `LeaveRequest` /
    `ExpenseReimbursement` were blocked by the profile gate and **never rendered**; the gate then read "deployment policy
    disallows this" as "the module root was not found" (`modulePresent=false`, `errorPage=false`). The product was fine; the
    gate injured itself. The correct semantics is a **union**: baseline enablement is a fact about this deployment and the
    probed packages must be visible for this verification, so neither may be replaced away.
    Proof boundary: this proves only that the package set is formed as a union and is non-empty; it does not prove a package
    is installed, a module is mounted, or a page is authorized.</en>
  </lang>
#>

# <lang>
#   <zh-CN>拆分 CSV 包列表为已去空白的非空片段；空输入返回空数组，绝不返回含空项的序列（空项会写进配置值）。</zh-CN>
#   <en>Splits a CSV package list into trimmed non-empty segments; blank input yields an empty array, never a sequence containing blank entries (a blank entry would be written into the configuration value).</en>
# </lang>
function Split-PortalPackageCsv {
    param(
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return @()
    }

    # <lang>
    #   <zh-CN>仅按逗号切分并去除每项边缘空白；名称合法性由稳定名称门禁负责，此处不做字符集判断。</zh-CN>
    #   <en>Split on commas only and trim each entry; name validity belongs to the stable-name gate and no character-set decision is made here.</en>
    # </lang>
    return @($Value.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 })
}

# <lang>
#   <zh-CN>读取部署基线里显式启用的包列表。**取全部来源的并集**而不是"第一个非空即返回"：
#   候选来源有本地覆盖 `appSettings.dev.json`、入库基线 `appSettings.json` 与 `Web.config`，其中 `dev.json`
#   正是门禁自己临时重写的文件 —— 若按"覆盖优先、取到即用"的常规覆盖语义读取，一次门禁写过的残留值
#   （例如只有 `HIA.ModuleProbe`）就会把入库基线里的包定义掉，正好复现本脚本要防的那类故障。
#   并集语义下，任一来源声明启用的包都不会被另一来源的缺失悄悄抹掉。</zh-CN>
#   <en>Reads the explicitly enabled package list from the deployment baseline. It takes the **union of every source** rather
#   than "first non-empty wins": the candidates are the local override `appSettings.dev.json`, the committed baseline
#   `appSettings.json`, and `Web.config`, and `dev.json` is precisely the file the gates temporarily rewrite — under the usual
#   "override wins, use it and stop" semantics, a leftover value written by a previous gate run (say, only `HIA.ModuleProbe`)
#   would define away the packages carried by the committed baseline, reproducing exactly the class of failure this script
#   exists to prevent. Under union semantics no source's absence can quietly erase a package another source enables.</en>
# </lang>
function Get-PortalBaselineEnabledPackages {
    param(
        [string] $RepoRoot,
        [string] $SettingsPath
    )

    # <lang>
    #   <zh-CN>候选来源按"本地覆盖 → 入库基线"排列，但顺序只影响证据里的出现次序，不影响结果集合。</zh-CN>
    #   <en>Candidates are ordered "local override → committed baseline", but the order only affects appearance in evidence, never the resulting set.</en>
    # </lang>
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($SettingsPath)) { $candidates += $SettingsPath }
    $candidates += (Join-Path $RepoRoot 'src\Portal\Config\appSettings.dev.json')
    $candidates += (Join-Path $RepoRoot 'src\Portal\Config\appSettings.json')

    $collected = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $candidates) {
        if (-not (Test-Path $path)) { continue }
        try {
            $json = Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
            # <lang>
            #   <zh-CN>键名含点号，必须用引号属性访问；JSON 覆盖文件的键与运行期 appSettings 平面点号键同名。</zh-CN>
            #   <en>The key contains dots, so it must be read as a quoted property; JSON override files use the same flat dotted key names as runtime appSettings.</en>
            # </lang>
            foreach ($packageId in (Split-PortalPackageCsv $json.appSettings.'Portal.ModulePackages.Enabled')) {
                [void] $collected.Add($packageId)
            }
        } catch {
            # <lang>
            #   <zh-CN>坏 JSON 不应中断门禁解析；跳过该文件继续下一个来源，最终由并集为空时的显式报错暴露问题。</zh-CN>
            #   <en>Malformed JSON must not abort gate resolution; skip the file and move to the next source, and the explicit error raised when the union is empty surfaces the problem.</en>
            # </lang>
        }
    }

    # <lang>
    #   <zh-CN>`Web.config` 同样参与并集：部分部署只在这里维护该键，没有环境 JSON。</zh-CN>
    #   <en>`Web.config` also joins the union: some deployments maintain the key only there and ship no environment JSON.</en>
    # </lang>
    $webConfigPath = Join-Path $RepoRoot 'src\Portal\web.config'
    if (Test-Path $webConfigPath) {
        $match = [regex]::Match(
            (Get-Content $webConfigPath -Raw -Encoding UTF8),
            '<add\s+key="Portal\.ModulePackages\.Enabled"\s+value="([^"]*)"')
        if ($match.Success) {
            foreach ($packageId in (Split-PortalPackageCsv $match.Groups[1].Value)) {
                [void] $collected.Add($packageId)
            }
        }
    }

    return @($collected | Sort-Object)
}

# <lang>
#   <zh-CN>按模块入口解析其 manifest 声明的 `packageId`；无 manifest（旧内容模块与后台管理模块）时返回 `$null`，
#   由调用方决定如何处理，绝不猜测一个"看起来像"的包名。</zh-CN>
#   <en>Resolves the `packageId` declared by a module entry's manifest; returns `$null` when there is no manifest (legacy content
#   modules and admin modules) and leaves the decision to the caller, never guessing a package name that merely looks right.</en>
# </lang>
function Get-PortalModulePackageId {
    param(
        [string] $RepoRoot,
        [string] $SourceFile
    )

    if ([string]::IsNullOrWhiteSpace($SourceFile)) { return $null }

    # <lang>
    #   <zh-CN>manifest 与 ascx 同级目录；路径分隔符统一为本机分隔符，避免 Linux 风格斜杠在 Windows 上拼出无效路径。</zh-CN>
    #   <en>The manifest sits in the same directory as the ascx; separators are normalized to the platform's so a Linux-style slash cannot compose an invalid path on Windows.</en>
    # </lang>
    $ascxPath = Join-Path (Join-Path $RepoRoot 'src\Portal') ($SourceFile -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    $manifestPath = Join-Path (Split-Path -Parent $ascxPath) 'module.json'
    if (-not (Test-Path $manifestPath)) { return $null }

    try {
        $manifest = Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace($manifest.packageId)) { return $null }
        return [string] $manifest.packageId
    } catch {
        return $null
    }
}

# <lang>
#   <zh-CN>求"基线显式启用包 ∪ 本组被测包"的并集，返回可直接写入配置值的稳定 CSV。
#   **并集而非替换**是本函数的全部意义：替换会静默丢掉基线里只靠 `Enabled` 进入门户的包，
#   表现为被测模块不渲染、门禁却报"模块找不到"（`W-anp-P137` 实测）。
#   并集为空时**显式抛错**：空集合会被写成空字符串配置值，等于把全部显式启用包一次抹掉，
#   这比报错危险得多（门禁看起来是绿的，实际什么都没验证）。</zh-CN>
#   <en>Computes the union "baseline explicitly enabled packages ∪ packages probed by this group" and returns a stable CSV ready to
#   be written into the configuration value. **Union rather than replacement** is this function's whole point: replacement silently
#   drops packages that reach the Portal only through baseline `Enabled`, which surfaces as the probed module not rendering while
#   the gate reports "module not found" (measured in `W-anp-P137`).
#   An empty union **throws explicitly**: an empty set would be written as a blank configuration value, wiping every explicitly
#   enabled package at once, which is far more dangerous than an error (the gate would look green while verifying nothing).</en>
# </lang>
function Resolve-PortalRuntimeGatePackages {
    param(
        [string] $RepoRoot,
        [string[]] $GroupPackages,
        [string] $SettingsPath
    )

    $baseline = @(Get-PortalBaselineEnabledPackages -RepoRoot $RepoRoot -SettingsPath $SettingsPath)
    $group = @($GroupPackages | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    # <lang>
    #   <zh-CN>大小写不敏感去重：配置值里同一包的大小写差异不应产生重复项，也不应扩大集合。</zh-CN>
    #   <en>Deduplicate case-insensitively: casing differences for the same package must not produce duplicates or enlarge the set.</en>
    # </lang>
    $merged = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($packageId in ($baseline + $group)) { [void] $merged.Add($packageId) }

    if ($merged.Count -eq 0) {
        throw 'Resolve-PortalRuntimeGatePackages: 并集为空，拒绝写出会抹掉全部显式启用包的空配置值。'
    }

    # <lang>
    #   <zh-CN>稳定排序后拼接，保证同一输入产出同一配置文本，便于证据比对与复查。</zh-CN>
    #   <en>Join after a stable sort so the same input always yields the same configuration text, making evidence comparable and reviewable.</en>
    # </lang>
    return (@($merged | Sort-Object) -join ',')
}
