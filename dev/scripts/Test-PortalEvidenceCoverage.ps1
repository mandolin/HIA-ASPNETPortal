# <lang>
#   <zh-CN>运行期证据覆盖台账门禁（L0，静态，无站点/无数据库依赖）——`W-anp-P140` 交付。
#
#   解决的问题（`C-anp-P26` 背景第 3 条）：运行期证据覆盖面"说不清"，全靠人工翻台账，
#   且"41 个模块实例仅 13 个有验证 URL，其余无覆盖、无所有者"。本门禁把覆盖陈述变成**每次运行自动产出、数字可复现**：
#
#   1) 台账 `dev\evidence-coverage.json` 是静态覆盖面的**唯一事实源**（后台管理页/后台管理型模块的入口与覆盖状态）；
#   2) 对 `inventory=static` 的面做**双向 default-deny** 核对：
#        源码存在但台账未分类 → Fail（新增页面/模块必须先进台账，不允许"没人认领"）；
#        台账登记但源码不存在 → Fail（改名/删除后台账遗留，防止统计口径漂移）；
#   3) 执行 `C-anp-P26` 验收 V3：`covered=false` 的项必须带 owner / reEntry / basis，
#      `covered=true` 的项必须带 entry —— "未覆盖面 100% 带所有者"由机器强制，不再靠自觉；
#   4) 对 `inventory=database` 的面（页签挂载模块实例）：数据库才是权威清单，静态硬编码必然漂移，
#      故只在台账登记其 L2 门禁归属并校验该门禁脚本存在；「已验证/总数/未覆盖」由
#      `Invoke-PortalModuleRuntimeGate.ps1` 每次运行写入 gate-summary.json 的 coverage 块。
#
#   用法（仓库根目录）：& 'dev\scripts\Test-PortalEvidenceCoverage.ps1'
#   产物：temp\evidence-coverage\coverage-stats.json（每次运行刷新，供覆盖率数字复现引用）。
#   退出码：0 全部通过；1 台账缺分类 / 漂移 / 未覆盖项缺所有者等任一失败。
#   有效性验证（注入法，沿用 W83/W87/P112 惯例）：临时删台账一项 → 应 Fail；清空 ThemeSettings 的 owner → 应 Fail；还原 → Pass。
#   </zh-CN>
#   <en>Runtime-evidence coverage ledger gate (L0; static; no site or database needed) — deliverable of `W-anp-P140`.
#
#   Problem it solves (`C-anp-P26` background item 3): the runtime-evidence coverage surface was unstateable without manually
#   digging through ledgers, with "41 module instances of which only 13 have validation URLs, the rest uncovered and unowned".
#   This gate turns the coverage statement into numbers **produced automatically on every run and reproducible**:
#
#   1) the ledger `dev\evidence-coverage.json` is the single source of truth for static coverage surfaces (admin pages / admin
#      modules: their entries and coverage state);
#   2) every surface with `inventory=static` is cross-checked **bidirectionally, default-deny**:
#        present in source but unclassified in the ledger → Fail (a new page/module must be classified first, nothing goes unclaimed);
#        classified in the ledger but absent from source → Fail (stale ledger entries after renames/deletions, preventing drift);
#   3) enforces `C-anp-P26` acceptance V3: an item with `covered=false` must carry owner / reEntry / basis, and a covered item
#      must carry entry — "100% of the uncovered surface is owned" is machine-enforced rather than voluntary;
#   4) for `inventory=database` surfaces (tab-mounted module instances): the database is the authoritative inventory and a static
#      copy would inevitably drift, so the ledger only records which L2 gate owns the surface (its existence is checked) and that
#      gate writes the "verified / total / uncovered" coverage block into gate-summary.json on every run.
#
#   Usage (repository root): & 'dev\scripts\Test-PortalEvidenceCoverage.ps1'
#   Output: temp\evidence-coverage\coverage-stats.json (refreshed per run; reference for reproducing coverage numbers).
#   Exit codes: 0 all pass; 1 any failure (missing classification / ledger drift / ownerless uncovered item).
#   Effectiveness proof (injection, per the W83/W87/P112 convention): temporarily remove a ledger item → must Fail; clear
#   ThemeSettings's owner → must Fail; restore → Pass.
#   </en>
# </lang>

# <lang><zh-CN>参数：无必填。证据输出路径可覆盖，缺省写入 temp\evidence-coverage\（gitignore，不干扰版本库）。</zh-CN>
# <en>Parameters: none required. The stats output path may be overridden; the default writes into temp\evidence-coverage\ (gitignored, kept out of version control).</en></lang>
[CmdletBinding()]
param(
    [string] $StatsPath = ''
)

$ErrorActionPreference = 'Stop'

# <lang><zh-CN>仓库根 = 本脚本目录（dev\scripts）上两级；台账与源码枚举都相对它定位。</zh-CN>
# <en>Repository root = two levels up from this script's directory (dev\scripts); the ledger and source enumeration are resolved relative to it.</en></lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$ledgerPath = Join-Path $repoRoot 'dev\evidence-coverage.json'

if (-not (Test-Path -LiteralPath $ledgerPath)) {
    Write-Host "[FAIL] 覆盖台账不存在：$ledgerPath（契约文件缺失即失败，不允许静默跳过）"
    exit 1
}
# <lang><zh-CN>台账读取：显式 UTF-8，避免 PowerShell 旧默认编码把中文条目读乱。</zh-CN>
# <en>Ledger read: explicit UTF-8 so legacy default encodings cannot garble the Chinese entries.</en></lang>
$ledger = Get-Content -LiteralPath $ledgerPath -Raw -Encoding UTF8 | ConvertFrom-Json

$failures = @()
$stats = @()

# <lang>
#   <zh-CN>逐面核对。`idBase` 把枚举文件映射成相对 src/Portal 的路径（与 DesktopSourceFile 同口径），
#   使台账条目 id 与模块定义表里的源码路径可以直接对照，不引入第二套路径写法。</zh-CN>
#   <en>Surface-by-surface cross-check. `idBase` maps enumerated files to paths relative to src/Portal (same convention as
#   DesktopSourceFile), so ledger item ids line up directly with the source paths in the module-definition table without
#   introducing a second path notation.</en>
# </lang>
foreach ($surface in $ledger.surfaces) {
    if ($surface.inventory -eq 'static') {
        # <lang><zh-CN>枚举目录/基路径/模式均来自台账（数据驱动），门禁本身不知道任何具体页面名。</zh-CN>
        # <en>Enumeration directory / id base / pattern all come from the ledger (data-driven); the gate itself knows no concrete page names.</en></lang>
        $enumDir = Join-Path $repoRoot ($surface.enum.dir -replace '/', '\')
        $idBase = Join-Path $repoRoot ($surface.enum.idBase -replace '/', '\')
        if (-not (Test-Path -LiteralPath $enumDir)) {
            $failures += ("[{0}] 枚举目录不存在：{1}" -f $surface.id, $surface.enum.dir)
            continue
        }
        $files = @(Get-ChildItem -LiteralPath $enumDir -Filter $surface.enum.pattern -File | Sort-Object Name)

        # <lang><zh-CN>实际 id：绝对路径截去 idBase 前缀并统一为正斜杠，得到 "Admin/Xxx.aspx" 形式。</zh-CN>
        # <en>Actual ids: strip the idBase prefix from the full path and normalize to forward slashes, yielding "Admin/Xxx.aspx".</en></lang>
        $actual = @()
        foreach ($f in $files) {
            $full = [System.IO.Path]::GetFullPath($f.FullName)
            $rel = $full.Substring($idBase.Length).TrimStart('\', '/') -replace '\\', '/'
            $actual += $rel
        }

        # <lang><zh-CN>分类集合 = items + excluded；两侧差集即 default-deny 的两种失败形态。</zh-CN>
        # <en>Classified set = items + excluded; the two differences are the two default-deny failure shapes.</en></lang>
        $classified = @()
        foreach ($item in @($surface.items)) { $classified += [string]$item.id }
        foreach ($ex in @($surface.excluded)) { $classified += [string]$ex.id }

        $missing = @($actual | Where-Object { $classified -notcontains $_ })
        $stale = @($classified | Where-Object { $actual -notcontains $_ })
        foreach ($m in $missing) {
            $failures += ("[{0}] 源码存在但台账未分类：{1}（default-deny：新增页面/模块必须先进台账）" -f $surface.id, $m)
        }
        foreach ($s in $stale) {
            $failures += ("[{0}] 台账登记但源码不存在（漂移/改名遗留）：{1}" -f $surface.id, $s)
        }

        # <lang>
        #   <zh-CN>V3 机器强制：未覆盖项必须带 owner/reEntry/basis（无主项 = 失败）；
        #   已覆盖项必须带 entry（说不出从哪儿进来的"覆盖"不是覆盖）。</zh-CN>
        #   <en>V3 machine enforcement: an uncovered item must carry owner/reEntry/basis (an ownerless item is a failure);
        #   a covered item must carry entry ("coverage" that cannot say how it is reached is not coverage).</en>
        # </lang>
        $coveredCount = 0
        foreach ($item in @($surface.items)) {
            if ($item.covered) {
                $coveredCount++
                if ([string]::IsNullOrWhiteSpace([string]$item.entry)) {
                    $failures += ("[{0}] 已覆盖项缺少 entry（入口说明）：{1}" -f $surface.id, $item.id)
                }
            } else {
                foreach ($field in @('owner', 'reEntry', 'basis')) {
                    $value = $item.PSObject.Properties[$field]
                    if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value.Value)) {
                        $failures += ("[{0}] 未覆盖项 {1} 缺 {2}（V3：未覆盖面 100% 带所有者与再入条件）" -f $surface.id, $item.id, $field)
                    }
                }
            }
        }

        $stats += [pscustomobject]@{
            surface = $surface.id
            inventory = 'static'
            total = $actual.Count
            covered = $coveredCount
            uncovered = (@($surface.items).Count - $coveredCount)
            excluded = @($surface.excluded).Count
        }
    } elseif ($surface.inventory -eq 'database') {
        # <lang>
        #   <zh-CN>数据库面：不静态枚举（会漂移），只核对归属的 L2 门禁脚本存在——
        #   台账若把 gate 指到不存在的脚本，覆盖率声明就成了空话。</zh-CN>
        #   <en>Database surface: not statically enumerated (it would drift); only the owning L2 gate script's existence is
        #   checked — if the ledger points `gate` at a missing script, the coverage claim is empty words.</en>
        # </lang>
        $gatePath = Join-Path $repoRoot ([string]$surface.gate)
        if (-not (Test-Path -LiteralPath $gatePath)) {
            $failures += ("[{0}] 归属门禁脚本不存在：{1}" -f $surface.id, $surface.gate)
        }
        $stats += [pscustomobject]@{
            surface = $surface.id
            inventory = 'database'
            gate = [string]$surface.gate
            total = $null
            covered = $null
            uncovered = $null
            excluded = 0
            note = '总数/已验证/未覆盖由归属 L2 门禁运行期枚举数据库并写入 gate-summary.json 的 coverage 块'
        }
    } else {
        $failures += ("[{0}] 未知 inventory 类型：{1}" -f $surface.id, $surface.inventory)
    }
}

# <lang><zh-CN>统计落盘：每次运行刷新，覆盖率数字引用它即可复现（与运行时间无关，同一输入同同一数字）。</zh-CN>
# <en>Stats persisted: refreshed per run; citing this file reproduces the coverage numbers (same input, same numbers, regardless of run time).</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $StatsPath = Join-Path (Join-Path $repoRoot 'temp') 'evidence-coverage\coverage-stats.json'
}
$statsDir = Split-Path -Parent $StatsPath
if (-not (Test-Path -LiteralPath $statsDir)) { New-Item -ItemType Directory -Force -Path $statsDir | Out-Null }
$report = [pscustomobject]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    ledger = 'dev/evidence-coverage.json'
    surfaces = $stats
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 运行期证据覆盖率（已覆盖 / 总数） ==='
foreach ($s in $stats) {
    if ($s.inventory -eq 'static') {
        Write-Host ("面 {0}: 已覆盖 {1} / 总数 {2}（另排除 {3} 个基础设施/非管理入口页）" -f $s.surface, $s.covered, $s.total, $s.excluded)
    } else {
        Write-Host ("面 {0}: 由 L2 门禁 {1} 运行期产出（数据库权威清单，gate-summary.json 的 coverage 块）" -f $s.surface, $s.gate)
    }
}
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host ("[FAIL] 覆盖台账校验失败 {0} 处：" -f $failures.Count)
    foreach ($f in $failures) { Write-Host ("  - " + $f) }
    exit 1
}

Write-Host ''
Write-Host '[PASS] 覆盖台账与源码一致；未覆盖项全部带所有者/再入条件/判定依据。'
exit 0
