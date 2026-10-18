// <lang>
//   <zh-CN>运行截图基线采集工具（`C-anp-P28` / `W-anp-P151`，按需执行，不是门禁）。
//
//   为什么要有它：`docs/documentation-registry.md` 与 `docs/documentation-map.md` 长期登记
//   「运行截图」为剩余缺口（`user-guide.md` 更是把它列入"目前没有文档支持"）。截图若手工贴一次，
//   必然随时间腐坏且与源码脱节——故必须**可复现**：固定视口、统一命名、产物纳管、清单入库。
//
//   规范（沿用 `Capture-PortalThemeScreenshots.ps1` 既有口径，不另起一套）：
//     视口    1440 × 1000、`deviceScaleFactor: 1`、locale `zh-CN`
//     命名    `p151-<area>-<target>-zh-CN-1440x1000.png`
//     采集    管理员登录后按 TARGETS 逐个采集；只截视口（fullPage=false）——
//             固定视口是可复现的前提，整页截图会随内容高度变化而不可比
//     校验    落通用错误页 / 文件过小 / 导航失败 = 该目标失败（不静默产出坏图）
//     产物    截图 → `work-zone/dev/evidence/p151/screenshots/`（**不入库**，P18 收口的
//             "按需生成 + 摘要入库"口径：`docs/operations-runbook.md` 亦要求不把截图放入根仓库）
//             清单 → `work-zone/dev/evidence/p151/screenshot-manifest.json`（**入库**，含
//             sha256/字节数/视口/locale/目标 URL/采集时间，供门禁核对）
//
//   与既有主题截图工具的区别：本工具**只读**（不切换主题、不写库），采集站点当前主题下的
//   关键后台与业务页面；主题矩阵截图由 `Capture-PortalThemeScreenshots.ps1` 承担。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Capture-PortalRuntimeScreenshots.mjs'
//   退出码：0 全部成功；1 有目标失败；2 环境前置缺失（W-anp-P139 口径）。
//   </zh-CN>
//   <en>Runtime screenshot baseline capture tool (`C-anp-P28` / `W-anp-P151`; run on demand, not a gate).
//
//   Why it exists: `docs/documentation-registry.md` and `docs/documentation-map.md` have long listed "runtime screenshots"
//   as a remaining gap (`user-guide.md` even lists it among items "with no documentation support"). Screenshots pasted by
//   hand inevitably rot and drift from the source, so they must be **reproducible**: fixed viewport, uniform naming,
//   governed artefacts, and a committed manifest.
//
//   Spec (following the existing `Capture-PortalThemeScreenshots.ps1` conventions rather than inventing new ones):
//     viewport  1440 x 1000, `deviceScaleFactor: 1`, locale `zh-CN`
//     naming    `p151-<area>-<target>-zh-CN-1440x1000.png`
//     capture   signed in as the administrator, one shot per TARGETS entry; viewport only (`fullPage=false`) — a fixed
//               viewport is the precondition for reproducibility, whereas full-page shots vary with content height
//     check     a generic error page, an undersized file, or a navigation failure fails that target (never emit a bad
//               image silently)
//     output    screenshots → `work-zone/dev/evidence/p151/screenshots/` (**not committed**; the P18 closeout rule of
//               "generate on demand + commit the summary", and `docs/operations-runbook.md` keeps screenshots out of the
//               root repository) and the manifest → `work-zone/dev/evidence/p151/screenshot-manifest.json`
//               (**committed**: sha256 / byte size / viewport / locale / target URL / capture time, for the gate to check)
//
//   Difference from the existing theme screenshot tool: this one is **read-only** (no theme switching, no database writes)
//   and captures key admin and business pages under the site's current theme; theme-matrix screenshots remain the job of
//   `Capture-PortalThemeScreenshots.ps1`.
//
//   Usage (repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Capture-PortalRuntimeScreenshots.mjs'
//   Exit codes: 0 all targets succeeded; 1 some target failed; 2 missing environment prerequisite (W-anp-P139 convention).
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，与其它运行期工具/门禁同口径。</zh-CN>
/// <en>Resolves the playwright module, same convention as the other runtime tools and gates.</en></lang>
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
const evidenceDir = path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p151');
const outputDir = process.env.PORTAL_SCREENSHOT_OUTPUT
  ? path.resolve(process.env.PORTAL_SCREENSHOT_OUTPUT)
  : path.join(evidenceDir, 'screenshots');
const manifestPath = path.join(evidenceDir, 'screenshot-manifest.json');

// <lang><zh-CN>固定采集规范：与既有主题截图工具同源（1440×1000 / DPR 1 / zh-CN），改动即视为规范变更。</zh-CN>
// <en>Fixed capture spec: shared with the existing theme screenshot tool (1440x1000 / DPR 1 / zh-CN); any change is a spec change.</en></lang>
const SPEC = {
  viewport: { width: 1440, height: 1000 },
  deviceScaleFactor: 1,
  locale: 'zh-CN',
  fullPage: false,
  naming: 'p151-<area>-<target>-zh-CN-1440x1000.png'
};

// <lang><zh-CN>环境前置：验收上下文缺失 → 显式 exit 2（P139 口径）。</zh-CN>
// <en>Environment prerequisite: a missing acceptance context exits 2 explicitly (P139 convention).</en></lang>
const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
if (!fs.existsSync(contextPath)) {
  console.error('[PREREQ-MISSING] 验收上下文不存在：' + contextPath + '（环境前置缺失，exit 2）');
  process.exit(2);
}
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const base = String(context.baseUrl).replace(/\/+$/, '');

// <lang><zh-CN>目标清单（default-deny：新增目标必须在此登记，否则不在证据包内）。
// 覆盖两类：业务页（前台门户页签）与后台页（/Admin/*.aspx，后台管理主路径）。</zh-CN>
// <en>Target list (default-deny: a new target must be registered here or it is not in the evidence package). It covers two
// areas: business pages (front portal tabs) and admin pages (`/Admin/*.aspx`, the main administration paths).</en></lang>
const TARGETS = [
  { id: 'home', area: 'business', url: base + '/' },
  { id: 'employee-directory', area: 'business', url: base + '/DesktopDefault.aspx?tabindex=3&tabid=2' },
  { id: 'discussion', area: 'business', url: base + '/DesktopDefault.aspx?tabindex=7&tabid=4' },
  { id: 'workbench', area: 'business', url: base + '/DesktopDefault.aspx?tabindex=21&tabid=1010' },
  { id: 'my-work-items', area: 'business', url: base + '/DesktopDefault.aspx?tabindex=23&tabid=1011' },
  { id: 'leave-expense', area: 'business', url: base + '/DesktopDefault.aspx?tabindex=990&tabid=1101' },
  { id: 'work-items', area: 'admin', url: base + '/Admin/WorkItems.aspx' },
  { id: 'employee-directory', area: 'admin', url: base + '/Admin/EmployeeDirectory.aspx' },
  { id: 'module-definitions', area: 'admin', url: base + '/Admin/ModuleDefinitions.aspx' },
  { id: 'security-roles', area: 'admin', url: base + '/Admin/SecurityRoles.aspx' },
  { id: 'business-applications', area: 'admin', url: base + '/Admin/BusinessApplications.aspx' },
  { id: 'collaboration-items', area: 'admin', url: base + '/Admin/CollaborationItems.aspx' },
  { id: 'operation-audits', area: 'admin', url: base + '/Admin/OperationAudits.aspx' },
  { id: 'diagnostics-logs', area: 'admin', url: base + '/Admin/DiagnosticsLogs.aspx' }
];

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

fs.mkdirSync(outputDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });

const entries = [];
const failures = [];
const capturedUtc = new Date().toISOString();

const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
try {
  const browserContext = await browser.newContext({
    viewport: SPEC.viewport,
    deviceScaleFactor: SPEC.deviceScaleFactor,
    locale: SPEC.locale
  });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(40000);

  // <lang><zh-CN>登录一次：全部目标在管理员会话内采集（后台页本就需授权）。</zh-CN>
  // <en>Sign in once: every target is captured inside the administrator session (admin pages require authorization anyway).</en></lang>
  await gotoWithRetry(page, base + '/');
  await page.locator('input[id$="EmailOrName"]').fill(context.adminUserName);
  await page.locator('input[id$="password"]').fill(context.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);

  for (const target of TARGETS) {
    const fileName = `p151-${target.area}-${target.id}-${SPEC.locale}-${SPEC.viewport.width}x${SPEC.viewport.height}.png`;
    const filePath = path.join(outputDir, fileName);
    const record = { id: target.id, area: target.area, url: target.url, file: fileName, status: 'Pass', notes: [] };
    try {
      await gotoWithRetry(page, target.url);
      await page.waitForLoadState('networkidle').catch(() => {});
      await page.waitForTimeout(900);
      const html = await page.content();
      if (/GenericErrorPage|应用程序暂时无法完成请求/.test(html)) {
        throw new Error('页面落通用错误页（运行期编译或加载失败）。');
      }
      await page.screenshot({ path: filePath, fullPage: SPEC.fullPage });
      const stats = fs.statSync(filePath);
      if (stats.size < 1024) {
        throw new Error(`截图文件过小（${stats.size} 字节），疑似空白页。`);
      }
      const buffer = fs.readFileSync(filePath);
      record.bytes = stats.size;
      record.sha256 = crypto.createHash('sha256').update(buffer).digest('hex');
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
      failures.push(`${target.area}/${target.id}: ${record.notes[0]}`);
    }
    entries.push(record);
  }
}
finally {
  await browser.close();
}

const manifest = {
  schemaVersion: 1,
  workItem: 'W-anp-P151',
  capturedUtc,
  spec: SPEC,
  targetCount: TARGETS.length,
  passed: entries.filter((entry) => entry.status === 'Pass').length,
  failed: failures.length,
  entries
};
fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2), 'utf8');
console.log('清单已写入：' + manifestPath);
console.log(`截图目录：${outputDir}（不入库，按需生成）`);
console.log(`采集目标 ${TARGETS.length} 个：成功 ${manifest.passed}、失败 ${manifest.failed}`);

if (failures.length > 0) {
  console.error('[FAIL] 以下目标采集失败：');
  for (const failure of failures) console.error('  - ' + failure);
  process.exit(1);
}
console.log('[PASS] 运行截图基线采集完成。');
process.exit(0);
