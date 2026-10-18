// <lang>
//   <zh-CN>性能基线门禁（L2，需运行站点 + playwright）：首屏 / 列表行数上限 / 慢查询阈值三类断言。
//
//   为什么必须有它：性能没有"对错"只有"快慢"，编译与功能门禁对"N+1 查询 / 无界列表"完全失明——
//   一行代码就能让页面慢十倍而所有绿灯照常。本门禁把当前性能基线变成**可重复比较的判据**。
//
//   阈值不是拍的（W-anp-P144 实测，2026-10-18）：服务端渲染中位 11–38ms、方差 ±2ms；
//   默认阈值服务端 TTFB 2000ms / 浏览器 load 10000ms（50–100× 余量），只抓恶性回归。
//   可用 PORTAL_PERF_MAX_SERVER_MS / PORTAL_PERF_MAX_NAV_LOAD_MS 覆盖（负向测试即把阈值压到 1 验证门禁真的会比较并失败）。
//
//   断言：
//     A1 五个已知列表面（前台 4 业务模块 + 后台待办页）必须声明行数上限常量且 ≤ 100
//        （本项目"列表分页"的正确形态是服务端行数上限：WorkItemLimit=20 / Recent*Limit=10 / PageSize=50，
//        注释明言"避免首页/管理页承载无界结果集"）；
//     A2 src/Portal（DesktopModules + Admin，排除 obj/bin）内所有 `private const int *Limit|*PageSize` ≤ 100；
//     A3 每个目标页：登录后预热 1 次，再测 5 轮取中位数——服务端 TTFB（含其背后全部 DB 工作，
//        即"慢查询经 UI 的表现"）与浏览器 load 均须低于阈值；页面落通用错误页即失败。
//
//   证明边界：只证明"当前基线仍低于阈值"与"已知列表面声明了有界行数上限"；
//   不证明直接 SQL 层耗时（需 DB 驱动，后续工作）、不发现未知的新列表面（静态规则按已知清单 default-deny，
//   新增列表面须登记进 KNOWN_LIST_SURFACES）。
//
//   前置：IIS Express 运行中、temp\p65\p65-acceptance-context.json（baseUrl/管理员账号）——缺失时**显式 exit 2**
//   （W-anp-P139 口径：环境前置缺失 ≠ 代码缺陷）；playwright 由编排层统一预检，缺失时本门禁根本不会被调用。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalPerformanceBaseline.mjs'
//   产物：temp\p144\perf-baseline-summary.json；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>Performance baseline gate (L2; needs the running site plus playwright): assertions for first render, list row caps, and slow-query thresholds.
//
//   Why it is required: performance has no "correct/incorrect", only "fast/slow" — build and functional gates are entirely blind to
//   "N+1 queries / unbounded lists"; one line of code can make a page ten times slower while every gate stays green. This gate turns
//   the current performance baseline into **repeatable criteria**.
//
//   Thresholds are evidence-first (measured 2026-10-18): server-render medians 11–38ms with ±2ms variance; defaults are server TTFB
//   2000ms and browser load 10000ms (50–100x headroom) so only gross regressions trip. PORTAL_PERF_MAX_SERVER_MS /
//   PORTAL_PERF_MAX_NAV_LOAD_MS may override them (the negative test pushes thresholds to 1 to prove the gate really compares and fails).
//
//   Assertions:
//     A1 the five known list surfaces (4 front-end business modules + the admin work-items page) must declare a row-cap constant <= 100
//        (this project's shape of "list pagination" is a server-side row cap: WorkItemLimit=20 / Recent*Limit=10 / PageSize=50, with
//        comments stating the intent to avoid unbounded result sets);
//     A2 every `private const int *Limit|*PageSize` under src/Portal (DesktopModules + Admin, obj/bin excluded) is <= 100;
//     A3 per target page: sign in, warm up once, then 5 timed rounds taking the median — server TTFB (which includes all DB work behind
//        the page, i.e. how slow queries surface through the UI) and browser load must stay under threshold; a generic error page fails.
//
//   Proof boundary: proves only that "the current baseline is still under threshold" and "the known list surfaces declare bounded caps";
//   does not prove direct SQL-level timings (needs a DB driver, future work) and does not discover unknown new list surfaces (the static
//   rule is default-deny over the known list; new list surfaces must be registered in KNOWN_LIST_SURFACES).
//
//   Prerequisites: IIS Express running, temp\p65\p65-acceptance-context.json (baseUrl/admin account) — missing means **explicit exit 2**
//   (the W-anp-P139 convention: a missing environment prerequisite is not a code defect); playwright is pre-checked by the orchestrator,
//   so when it is missing this gate is never invoked at all.
//
//   Usage (repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalPerformanceBaseline.mjs'
//   Output: temp\p144\perf-baseline-summary.json; exits with code 1 when any assertion fails.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，与其它运行期门禁同口径。</zh-CN>
/// <en>Resolves the playwright module, same convention as the other runtime gates.</en></lang>
/// </summary>
async function loadChromium() {
  const configured = process.env.PORTAL_PLAYWRIGHT_MODULE;
  if (configured) {
    const lower = configured.toLowerCase();
    const entry = lower.endsWith('.mjs') || lower.endsWith('.js') || lower.endsWith('.cjs')
      ? configured
      : path.join(configured, 'index.mjs');
    const module = await import(pathToFileURL(entry).href);
    return module.chromium;
  }
  const module = await import('playwright');
  return module.chromium;
}

const repoRoot = path.resolve(process.cwd());
const outputDir = path.join(repoRoot, 'temp', 'p144');
fs.mkdirSync(outputDir, { recursive: true });

// <lang><zh-CN>环境前置：验收上下文缺失 → 显式 exit 2（W-anp-P139 口径），不与代码缺陷（exit 1）混报。</zh-CN>
// <en>Environment prerequisite: a missing acceptance context exits 2 explicitly (the W-anp-P139 convention), never conflated with a code defect.</en></lang>
const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
if (!fs.existsSync(contextPath)) {
  console.error('[PREREQ-MISSING] 验收上下文不存在：' + contextPath + '（环境前置缺失，exit 2）');
  process.exit(2);
}
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const base = String(context.baseUrl).replace(/\/+$/, '');

// <lang><zh-CN>A2 的全局扫描范围与排除规则；A1 的已知列表面清单 default-deny（新增列表面必须登记）。</zh-CN>
// <en>Scan scope and exclusions for A2; the A1 known-list list is default-deny (new list surfaces must be registered).</en></lang>
const scanDirs = [path.join(repoRoot, 'src', 'Portal', 'DesktopModules'), path.join(repoRoot, 'src', 'Portal', 'Admin')];
const KNOWN_LIST_SURFACES = [
  'src/Portal/DesktopModules/MyWorkItems/MyWorkItems.ascx.cs',
  'src/Portal/DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx.cs',
  'src/Portal/DesktopModules/EmployeeProfileCorrectionRequest/EmployeeProfileCorrectionRequest.ascx.cs',
  'src/Portal/DesktopModules/BusinessApplicationRequest/BusinessApplicationRequest.ascx.cs',
  'src/Portal/Admin/WorkItems.aspx.cs'
];
const CAP_MAX = 100;

// <lang><zh-CN>阈值：默认值来自 2026-10-18 实测（服务端中位 11–38ms），env 可覆盖供负向测试。</zh-CN>
// <en>Thresholds: defaults from the 2026-10-18 measurement (server medians 11–38ms); env-overridable for the negative test.</en></lang>
const maxServerMs = Number(process.env.PORTAL_PERF_MAX_SERVER_MS || 2000);
const maxNavLoadMs = Number(process.env.PORTAL_PERF_MAX_NAV_LOAD_MS || 10000);

// <lang><zh-CN>目标页集合：与模块运行期证据门禁同源的页签（tabid 为授权真源）；登录后测量避免匿名跳转带来的方差。</zh-CN>
// <en>Target pages: tabs shared with the module runtime evidence gate (tabid is the authorization source); measuring after sign-in avoids anonymous-redirect variance.</en></lang>
const targets = [
  { id: 'home', url: base + '/' },
  { id: 'workbench', url: base + '/DesktopDefault.aspx?tabindex=9&tabid=1010' },
  { id: 'myworkitems', url: base + '/DesktopDefault.aspx?tabindex=10&tabid=1011' },
  { id: 'adminTab', url: base + '/DesktopDefault.aspx?tabindex=9&tabid=6' }
];
if (context.tabUrl) targets.push({ id: 'correction', url: String(context.tabUrl) });

// <lang><zh-CN>可重试导航：应用域回收窗口内的瞬时失败重试即可恢复（与模块运行期门禁同一教训）。</zh-CN>
// <en>Retryable navigation: transient failures inside an app-domain recycle window recover on retry (same lesson as the module runtime gate).</en></lang>
const TRANSIENT_NAV_ERRORS = ['ERR_ABORTED', 'ERR_CONNECTION_RESET', 'ERR_EMPTY_RESPONSE', 'ERR_CONNECTION_REFUSED'];
async function gotoWithRetry(page, url, attempts = 4) {
  let lastError = null;
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await page.goto(url, { waitUntil: 'load', timeout: 60000 });
    } catch (error) {
      lastError = error;
      const message = error instanceof Error ? error.message : String(error);
      const transient = TRANSIENT_NAV_ERRORS.some((code) => message.includes(code));
      if (!transient || attempt === attempts) throw error;
      await page.waitForTimeout(1000 * attempt);
    }
  }
  throw lastError;
}

const median = (values) => {
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.floor(sorted.length / 2)];
};

const results = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
try {
  const browserContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'zh-CN' });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(40000);

  // <lang><zh-CN>登录一次（后续全部测量在管理员会话内进行）。</zh-CN>
  // <en>Sign in once; all measurements then run inside the administrator session.</en></lang>
  await gotoWithRetry(page, base + '/');
  await page.locator('input[id$="EmailOrName"]').fill(context.adminUserName);
  await page.locator('input[id$="password"]').fill(context.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);

  for (const target of targets) {
    const record = { id: target.id, url: target.url, status: 'Pass', notes: [] };
    try {
      // <lang><zh-CN>预热 1 次：填充 JIT/模块运行期缓存，避免首访计入测量。</zh-CN>
      // <en>One warm-up pass: primes JIT and the module runtime cache so first visits never enter the measurement.</en></lang>
      await gotoWithRetry(page, target.url);
      const loads = [];
      const ttfbs = [];
      let errorPage = false;
      for (let i = 0; i < 5; i += 1) {
        const t0 = Date.now();
        await gotoWithRetry(page, target.url);
        loads.push(Date.now() - t0);
        const nav = await page.evaluate(() => {
          const entries = performance.getEntriesByType('navigation');
          return entries && entries.length > 0 ? { responseEnd: entries[0].responseEnd } : null;
        });
        ttfbs.push(nav && nav.responseEnd > 0 ? nav.responseEnd : Date.now() - t0);
        const html = await page.content();
        if (/GenericErrorPage|应用程序暂时无法完成请求/.test(html)) errorPage = true;
      }
      record.medianLoadMs = median(loads);
      record.medianTtfbMs = Math.round(median(ttfbs));
      record.samples = { loads, ttfbs };
      if (errorPage) {
        record.status = 'Fail';
        record.notes.push('页面落通用错误页（运行期编译或加载失败）。/ The page fell back to the generic error page.');
      }
      if (record.medianTtfbMs > maxServerMs) {
        record.status = 'Fail';
        record.notes.push(`服务端 TTFB 中位 ${record.medianTtfbMs}ms > 阈值 ${maxServerMs}ms（慢查询/服务端回归）。`);
      }
      if (record.medianLoadMs > maxNavLoadMs) {
        record.status = 'Fail';
        record.notes.push(`浏览器 load 中位 ${record.medianLoadMs}ms > 阈值 ${maxNavLoadMs}ms。`);
      }
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }
    results.push(record);
  }
  await browserContext.close();
} finally {
  await browser.close();
}

// ---------------------------------------------------------------------------
// A1/A2：列表行数上限静态断言
// ---------------------------------------------------------------------------
// <lang><zh-CN>递归收集 .cs 文件（排除 obj/bin 构建输出）。</zh-CN>
// <en>Recursively collect .cs files (build outputs under obj/bin excluded).</en></lang>
function collectCsFiles(dir) {
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === 'obj' || entry.name === 'bin') continue;
      out.push(...collectCsFiles(full));
    } else if (entry.name.endsWith('.cs')) {
      out.push(full);
    }
  }
  return out;
}
const capRegex = /private\s+const\s+int\s+(\w*(?:Limit|PageSize))\s*=\s*(\d+)/g;
const capFindings = [];
for (const dir of scanDirs) {
  for (const file of collectCsFiles(dir)) {
    const text = fs.readFileSync(file, 'utf8');
    for (const match of text.matchAll(capRegex)) {
      capFindings.push({ file: path.relative(repoRoot, file).replace(/\\/g, '/'), name: match[1], value: Number(match[2]) });
    }
  }
}
for (const surface of KNOWN_LIST_SURFACES) {
  const found = capFindings.filter((f) => f.file === surface);
  if (found.length === 0) {
    results.push({ id: 'A1:' + surface, status: 'Fail', notes: ['已知列表面未声明行数上限常量（*Limit/*PageSize）。'] });
  }
}
for (const finding of capFindings) {
  if (finding.value > CAP_MAX) {
    results.push({
      id: 'A2:' + finding.file + ':' + finding.name,
      status: 'Fail',
      notes: [`行数上限常量 ${finding.name}=${finding.value} > ${CAP_MAX}（列表可无界增长）。`]
    });
  }
}

const failed = results.filter((r) => r.status === 'Fail');
const summary = {
  generatedUtc: new Date().toISOString(),
  thresholds: { maxServerMs, maxNavLoadMs, capMax: CAP_MAX },
  results,
  capFindings
};
fs.writeFileSync(path.join(outputDir, 'perf-baseline-summary.json'), JSON.stringify(summary, null, 2), 'utf8');

for (const r of results) {
  const label = r.medianTtfbMs !== undefined
    ? `${r.id}  TTFB 中位 ${r.medianTtfbMs}ms ｜ load 中位 ${r.medianLoadMs}ms`
    : r.id;
  console.log(`[${r.status}] ${label}`);
  for (const note of r.notes) console.log('    ' + note);
}
console.log(`阈值：server=${maxServerMs}ms nav=${maxNavLoadMs}ms cap<=${CAP_MAX} ｜ 行数上限常量 ${capFindings.length} 个`);
if (failed.length > 0) process.exit(1);
