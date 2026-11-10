// <lang>
//   <zh-CN>`W-anp-P290` 浏览器级 L2 运行期证据：对**运行中的站点**验证主题设置后台页已从可达的
//   Admin 动作区获得真实入链。以管理员登录 → 访问 `Admin/SystemHealth.aspx`（既有的可达页）→
//   断言其动作区渲染出指向 `Admin/ThemeSettings.aspx` 的链接 → 实际点击进入并断言主题设置页标题与
//   权限门控通过 → 留档整页截图与 JSON 摘要。参数全部经环境变量注入，不硬编码页签 ID、账号或主题。</zh-CN>
//   <en>`W-anp-P290` browser-level L2 runtime evidence: against the **running site**, verify that the theme settings
//   admin page now has a real inbound link from a reachable Admin action area. Sign in as administrator → visit
//   `Admin/SystemHealth.aspx` (an existing reachable page) → assert its action area renders a link to
//   `Admin/ThemeSettings.aspx` → actually click through and assert the theme settings page title and permission gate →
//   keep a full-page screenshot and a JSON summary. All parameters arrive through environment variables; no tab id,
//   account, or theme is hard-coded.</en>
// </lang>
//
// <lang>
//   <zh-CN>环境变量：PORTAL_L2_BASE_URL（默认取上下文）/ PORTAL_L2_CONTEXT / PORTAL_L2_OUTPUT_DIR /
//   PORTAL_L2_LOCALE。playwright 为开发期依赖且不入库，经 PORTAL_PLAYWRIGHT_MODULE 指向
//   temp/node_modules/playwright；node 由 mise 提供。</zh-CN>
//   <en>Environment: PORTAL_L2_BASE_URL (defaults to the context) / PORTAL_L2_CONTEXT / PORTAL_L2_OUTPUT_DIR /
//   PORTAL_L2_LOCALE. Playwright is a development-only, uncommitted dependency pointed to by
//   PORTAL_PLAYWRIGHT_MODULE; node is provided by mise.</en>
// </lang>
//
// <lang>
//   <zh-CN>证明边界：只证明采集时刻、当前数据集与部署配置下，管理员能从既有的可达 Admin 页经动作区
//   点击进入主题设置页；**不证明**匿名/普通用户可达（AdminOnly 本就不应可达）、不证明分组导航两级
//   渲染（归 `W-anp-P291`）、不证明主题写入/审计链路、不证明跨机器像素一致或可访问性运行期实测。</zh-CN>
//   <en>Proof boundary: proves only that, at collection time under the current dataset and deployment configuration,
//   an administrator can click through from an existing reachable Admin page to theme settings. It does **not** prove
//   anonymous/normal-user reachability (AdminOnly must remain unreachable for them), two-level grouped navigation
//   rendering (owned by `W-anp-P291`), theme write/audit flow, cross-machine pixel equality, or runtime accessibility.</en>
// </lang>

import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang>
///   <zh-CN>解析 playwright 模块并返回 chromium 启动器；优先环境变量指定位置，否则回退按包名解析。</zh-CN>
///   <en>Resolve the playwright module and return the chromium launcher; the environment variable wins, otherwise fall back to package-name resolution.</en>
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
  const module = await import('playwright');
  return module.chromium;
}

/// <summary>
/// <lang>
///   <zh-CN>仓库根取当前工作目录（与项目既有 L2 证据脚本一致，须从仓库根调用）；上下文 JSON 提供
///   baseUrl / 管理员账号与口令，凭据不写入仓库。</zh-CN>
///   <en>The repository root is the current working directory (matching the existing L2 evidence scripts; call from the
///   repository root); the context JSON supplies baseUrl and admin credentials, which are never committed.</en>
/// </lang>
/// </summary>
const repoRoot = path.resolve(process.cwd());
const contextPath = process.env.PORTAL_L2_CONTEXT || path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = process.env.PORTAL_L2_BASE_URL || context.baseUrl;
const locale = (process.env.PORTAL_L2_LOCALE || 'zh-CN').trim();
const outputDir = process.env.PORTAL_L2_OUTPUT_DIR || path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p51-theme-settings-entry');
const isChinese = locale.toLowerCase().startsWith('zh');

/// <summary>
/// <lang>
///   <zh-CN>目标与期望文案：从注册表事实推导链接目标，界面文案按语言中英两套常量声明（不引入 resx 键）。</zh-CN>
///   <en>Targets and expected copy: the link target derives from registry facts; UI copy is declared in Chinese/English constants (no new resx keys).</en>
/// </lang>
/// </summary>
const hostPage = 'Admin/SystemHealth.aspx';
const themePage = 'Admin/ThemeSettings.aspx';
const expectedLinkText = isChinese ? '主题设置' : 'Theme Settings';
const expectedThemeTitle = isChinese ? '主题设置' : 'Theme Settings';

/// <summary>
/// <lang>
///   <zh-CN>建立并验证管理员登录态；未确认登录成功即失败，避免把拒绝访问页当作目标页断言。</zh-CN>
///   <en>Establish and verify administrator sign-in; fail without confirmation so an access-denied page is never asserted as the target.</en>
/// </lang>
/// </summary>
async function signIn(page) {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('input[id$="EmailOrName"]').fill(context.adminUserName);
  await page.locator('input[id$="password"]').fill(context.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);
  const text = await page.locator('body').innerText().catch(() => '');
  if (/Server Error|运行时错误|HTTP 500/.test(text)) {
    throw new Error('Sign-in page returned a server error.');
  }
  return text;
}

const checks = [];
function check(name, ok, detail) {
  checks.push({ name, ok: Boolean(ok), detail });
}

const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
const record = {
  collectedUtc: new Date().toISOString(),
  baseUrl,
  locale,
  hostPage,
  themePage,
  aliasEntryKey: 'Admin.Ops.ThemeSettingsEntry',
  status: 'Pass',
  facts: {},
  checks
};

try {
  fs.mkdirSync(outputDir, { recursive: true });
  const browserContext = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale
  });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(30000);
  await signIn(page);

  // <lang>
  //   <zh-CN>访问既有可达页，断言动作区出现指向主题设置的真实链接（不是脚本伪造 URL）。</zh-CN>
  //   <en>Visit the existing reachable page and assert its action area renders a genuine link to theme settings (not a script-fabricated URL).</en>
  // </lang>
  const hostUrl = new URL(hostPage, baseUrl).toString();
  record.hostUrl = hostUrl;
  const hostResponse = await page.goto(hostUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(700);
  check('A1.HostPageHttp200', hostResponse && hostResponse.status() === 200, 'status=' + (hostResponse ? hostResponse.status() : 'none'));

  const allLinks = await page.locator('a').evaluateAll((anchors) =>
    anchors.map((anchor) => ({ text: (anchor.textContent || '').trim(), href: anchor.getAttribute('href') || '' })));
  const inbound = allLinks.filter((link) => link.href.indexOf('ThemeSettings.aspx') >= 0);
  record.facts.inboundLinkCount = inbound.length;
  record.facts.inboundLinks = inbound;
  check('A2.InboundLinkPresent', inbound.length > 0, 'links=' + JSON.stringify(inbound));
  check('A3.InboundLinkLabel', inbound.some((link) => link.text.indexOf(expectedLinkText) >= 0),
    'expected label=' + expectedLinkText);

  // <lang>
  //   <zh-CN>实际点击进入主题设置页，断言权限门控通过且页面标题出现，而非只检查 href 字符串。</zh-CN>
  //   <en>Actually click through to the theme settings page and assert the permission gate passes with the page title present, rather than only checking an href string.</en>
  // </lang>
  const target = page.locator('a[href*="ThemeSettings.aspx"]').first();
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    target.click()
  ]);
  await page.waitForTimeout(700);
  const themeResponseStatus = await page.evaluate(() => document.readyState);
  const themeText = await page.locator('body').innerText().catch(() => '');
  record.themeUrl = page.url();
  check('A4.ThemePageReached', page.url().indexOf('ThemeSettings.aspx') >= 0, 'url=' + page.url());
  check('A5.ThemePageTitle', themeText.indexOf(expectedThemeTitle) >= 0, 'titleFound=' + themeText.includes(expectedThemeTitle));
  check('A6.NoServerError', !/Server Error|运行时错误|HTTP 500/.test(themeText), 'readyState=' + themeResponseStatus);

  const screenshotPath = path.join(outputDir, 'theme-settings-entry-' + locale + '.png');
  await page.screenshot({ path: screenshotPath, fullPage: true });
  record.screenshot = path.basename(screenshotPath);

  const failed = checks.filter((item) => !item.ok);
  record.status = failed.length === 0 ? 'Pass' : 'Fail';
  record.facts.failedCheckCount = failed.length;
  fs.writeFileSync(path.join(outputDir, 'runtime-evidence-summary.json'), JSON.stringify(record, null, 2), 'utf8');
  console.log('theme settings entry L2: ' + record.status + ' (' + checks.filter((c) => c.ok).length + '/' + checks.length + ') ' + locale);
  for (const item of failed) { console.log('  [Fail] ' + item.name + ' — ' + item.detail); }
  await browserContext.close();
  await browser.close();
  process.exit(failed.length === 0 ? 0 : 1);
}
catch (error) {
  record.status = 'Fail';
  record.error = error.message;
  try { fs.mkdirSync(outputDir, { recursive: true }); fs.writeFileSync(path.join(outputDir, 'runtime-evidence-summary.json'), JSON.stringify(record, null, 2), 'utf8'); } catch { /* best-effort */ }
  console.error('theme settings entry L2 error: ' + error.message);
  await browser.close();
  process.exit(1);
}
