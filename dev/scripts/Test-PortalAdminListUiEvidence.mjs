// <lang>
//   <zh-CN>后台列表 UI 运行期证据脚本：对**运行中的站点**（LocalDB + IIS Express）以管理员登录，逐页采集事实并断言，同时留档整页截图。
//
//   为什么需要它：静态渲染原型只能验证"样式与类"，无法验证"服务端数据分支"——
//   例如空态是否只在真正零条时出现、服务不可用时是否抑制空态、页面是否泄漏内部标识符。
//   这些必须对真实站点断言。
//
//   前置条件：
//     1) IIS Express 已按项目脚本启动（`dev\scripts\Start-IISExpress.ps1`，默认 40001）；
//     2) 外置连接串已配置（`%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`）；
//     3) 验收上下文 `temp\p65\p65-acceptance-context.json` 存在（提供 baseUrl / 管理员账号 / 口令）；
//     4) playwright 可解析 —— 它是**开发期依赖且不入库**（本机位于 `temp\node_modules\playwright`），
//        故执行前须通过环境变量 `PORTAL_PLAYWRIGHT_MODULE` 指向该模块。
//
//   用法（在仓库根目录执行）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalAdminListUiEvidence.mjs'
//   产物：`temp\p74\ui-verify\*.png` 与 `verify-summary.json`；任一断言失败时以退出码 1 结束。
//   </zh-CN>
//   <en>Admin-list UI runtime evidence script: signs in as an administrator against the **running site** (LocalDB + IIS Express), collects facts page by page, asserts, and keeps full-page screenshots.
//
//   Why it exists: a static rendered prototype can validate styles and classes but not server-side data branches —
//   for example whether an empty state appears only with genuinely zero rows, whether it is suppressed while a service is unavailable, and whether a page leaks an internal identifier.
//   Those must be asserted against the real site.
//
//   Prerequisites:
//     1) IIS Express started through the project script (`dev\scripts\Start-IISExpress.ps1`, port 40001 by default);
//     2) an external connection string configured at `%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`;
//     3) the acceptance context `temp\p65\p65-acceptance-context.json` present (it supplies baseUrl, the administrator account, and the password);
//     4) playwright resolvable — it is a **development-only dependency that is not committed** (on this machine it lives in `temp\node_modules\playwright`),
//        so `PORTAL_PLAYWRIGHT_MODULE` must point at that module before running.
//
//   Usage (run from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalAdminListUiEvidence.mjs'
//   Output: `temp\p74\ui-verify\*.png` and `verify-summary.json`; exits with code 1 when any assertion fails.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang>
///   <zh-CN>解析 playwright 模块，优先使用环境变量指定的位置，否则回退到按包名解析。</zh-CN>
///   <en>Resolves the playwright module, preferring an environment-variable location and otherwise falling back to package-name resolution.</en>
/// </lang>
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

  try {
    const module = await import('playwright');
    return module.chromium;
  } catch (error) {
    throw new Error(
      'Unable to resolve "playwright". It is a development-only dependency and is not committed; ' +
      'set PORTAL_PLAYWRIGHT_MODULE to its location (for example <repo>\\temp\\node_modules\\playwright). ' +
      'Original error: ' + error.message
    );
  }
}

const repoRoot = path.resolve(process.cwd());
const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
const outputDir = path.join(repoRoot, 'temp', 'p74', 'ui-verify');

if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}

fs.mkdirSync(outputDir, { recursive: true });
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = context.baseUrl;
const userName = context.adminUserName;

// <lang>
//   <zh-CN>目标页与期望：`expectEmptyState=true` 要求渲染空态；`false` 要求**不得**渲染空态（失败抑制）；
//   `null` 表示只采集事实不断言（该页数据量随夹具变化）。</zh-CN>
//   <en>Targets and expectations: `expectEmptyState=true` requires a rendered empty state; `false` requires that no empty state renders (failure suppression);
//   `null` collects facts without asserting because the row count depends on fixtures.</en>
// </lang>
const targets = [
  { id: 'admin-workitems', url: 'Admin/WorkItems.aspx', expectEmptyState: true, expectSuppressed: false },
  { id: 'admin-businessapplications', url: 'Admin/BusinessApplications.aspx', expectEmptyState: false, expectSuppressed: true },
  { id: 'admin-correctionrequests', url: 'Admin/EmployeeProfileCorrectionRequests.aspx', expectEmptyState: null, expectSuppressed: false },
  { id: 'admin-collaborationitems', url: 'Admin/CollaborationItems.aspx', expectEmptyState: null, expectSuppressed: false },
  { id: 'admin-employeedirectory', url: 'Admin/EmployeeDirectory.aspx', expectEmptyState: null, expectSuppressed: false }
];

const emptyStateMarkers = ['暂无', '没有符合条件的'];
const suppressionMarkers = ['服务未注册', '不可用', '未注册'];

/// <summary>
/// <lang>
///   <zh-CN>建立并验证管理员登录态；未确认登录成功即失败，避免把拒绝访问页当作目标页断言。</zh-CN>
///   <en>Establishes and verifies the administrator sign-in state; failure to confirm sign-in aborts so an access-denied page is never asserted as a target page.</en>
/// </lang>
/// </summary>
async function signIn(page) {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('input[id$="EmailOrName"]').fill(userName);
  await page.locator('input[id$="password"]').fill(context.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);
  const text = await page.locator('body').innerText().catch(() => '');
  if (!/欢迎|Logoff|注销/.test(text)) {
    throw new Error('Sign-in did not complete.');
  }
}

const results = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
try {
  const browserContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1, locale: 'zh-CN' });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(30000);
  await signIn(page);

  for (const target of targets) {
    const url = new URL(target.url, baseUrl).toString();
    const record = { id: target.id, url, status: 'Pass', facts: {}, notes: [] };
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(700);
      const text = await page.locator('body').innerText().catch(() => '');
      const html = await page.content().catch(() => '');

      record.facts.hasEmptyStateClass = html.includes('portal-empty-state');
      record.facts.emptyMarkers = emptyStateMarkers.filter((marker) => text.includes(marker));
      record.facts.hasSuppressionMarker = suppressionMarkers.some((marker) => text.includes(marker));
      record.facts.dataRowCount = (html.match(/<tr[^>]*class="Normal"/g) || []).length;

      // <lang>
      //   <zh-CN>原始角色键泄漏检测：界面出现 `(Collaborator)` / `(Watcher)` 即视为把内部标识符暴露给用户。</zh-CN>
      //   <en>Raw role-key leak detection: `(Collaborator)` or `(Watcher)` in the UI means an internal identifier was exposed to users.</en>
      // </lang>
      record.facts.rawRoleKeyLeak = /\((Collaborator|Watcher)\)/.test(text);
      record.facts.localizedRolePresent = /（协办）|（关注）/.test(text);
      record.facts.nonePlaceholderPresent = text.includes('（无）');

      if (target.expectEmptyState === true) {
        if (!record.facts.hasEmptyStateClass) {
          record.status = 'Fail';
          record.notes.push('Expected portal-empty-state but none rendered.');
        }
        if (record.facts.emptyMarkers.length === 0) {
          record.status = 'Fail';
          record.notes.push('Expected an empty-state wording but no marker matched.');
        }
      }
      if (target.expectEmptyState === false && record.facts.hasEmptyStateClass) {
        record.status = 'Fail';
        record.notes.push('Empty state must be suppressed on this page but it rendered.');
      }
      if (target.expectSuppressed === true && !record.facts.hasSuppressionMarker) {
        record.status = 'Fail';
        record.notes.push('Expected an unavailable message on this page.');
      }
      if (record.facts.rawRoleKeyLeak) {
        record.status = 'Fail';
        record.notes.push('Raw participant role key leaked into UI.');
      }

      const shot = path.join(outputDir, `${target.id}.png`);
      await page.screenshot({ path: shot, fullPage: true });
      record.screenshot = path.relative(repoRoot, shot);
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }

    results.push(record);
  }

  await browserContext.close();
} finally {
  // <lang>
  //   <zh-CN>无论断言是否失败都关闭浏览器，避免残留进程影响后续复核。</zh-CN>
  //   <en>Closes the browser regardless of assertion failures so no process remains for later review runs.</en>
  // </lang>
  await browser.close();
}

fs.writeFileSync(path.join(outputDir, 'verify-summary.json'), JSON.stringify(results, null, 2), 'utf8');
for (const record of results) {
  console.log(`[${record.status}] ${record.id}`);
  console.log(`    emptyClass=${record.facts.hasEmptyStateClass} markers=[${(record.facts.emptyMarkers || []).join(',')}] suppression=${record.facts.hasSuppressionMarker} rows=${record.facts.dataRowCount}`);
  console.log(`    rawRoleLeak=${record.facts.rawRoleKeyLeak} localizedRole=${record.facts.localizedRolePresent} nonePlaceholder=${record.facts.nonePlaceholderPresent}`);
  for (const note of record.notes) {
    console.log(`    note: ${note}`);
  }
}

if (results.some((record) => record.status !== 'Pass')) {
  process.exitCode = 1;
}
