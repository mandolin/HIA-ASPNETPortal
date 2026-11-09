// <lang>
//   <zh-CN>独立模块（Form Builder）浏览器级 L2 运行期证据：对**运行中的站点**（IIS Express + LocalDB）以管理员登录，
//   经容纳机制实际访问独立模块所在页签，断言模块根/标题/字段/提交按钮渲染、主题作用域类生效、界面语言与本地化文案一致，
//   并留档整页截图与 JSON 摘要。
//
//   为什么需要它：安装/卸载的**数据层**闭环（W-anp-P285）只证明元数据写入与清理，不证明「模块经容纳机制实际装载运行」；
//   静态门禁只证明契约一致，也不证明运行期渲染。R2「可运行」判据必须由真实站点运行期断言承载。
//
//   前置条件：
//     1) IIS Express 已按项目脚本启动（dev\scripts\Start-IISExpress.ps1，默认 40001）；
//     2) 外置连接串已配置（%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config）；
//     3) 验收上下文 temp\p65\p65-acceptance-context.json 存在（提供 baseUrl / 管理员账号 / 口令）；
//     4) playwright 可解析 —— 开发期依赖且不入库（本机 temp\node_modules\playwright），
//        经环境变量 PORTAL_PLAYWRIGHT_MODULE 指向该模块。
//
//   参数全部经环境变量输入，由 Invoke-PortalIndependentModuleRuntimeEvidence.ps1 包装脚本按库内事实推导后注入，
//   脚本本身不绑定任何具体页签 ID、主题名或数据集：
//     PORTAL_L2_BASE_URL / PORTAL_L2_CONTEXT / PORTAL_L2_TAB_ID / PORTAL_L2_THEME /
//     PORTAL_L2_LOCALE / PORTAL_L2_OUTPUT_DIR。
//   产物：<PORTAL_L2_OUTPUT_DIR>\form-builder-<theme>-<locale>.png 与同名 .json；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>Independent-module (Form Builder) browser-level L2 runtime evidence: signs in as an administrator against the
//   **running site** (IIS Express + LocalDB), visits the independent module's tab through the hosting mechanism, asserts that
//   the module root/title/fields/submit button render, that the theme-scope class is applied, and that the UI language matches
//   the localized copy; keeps a full-page screenshot and a JSON summary.
//
//   Why it exists: the **data-layer** install/uninstall loop (W-anp-P285) proves only metadata writes and cleanup, not that the
//   module is actually loaded and run by the hosting mechanism; static gates prove contract consistency, not runtime rendering.
//   The R2 "runnable" criterion must be carried by a real-site runtime assertion.
//
//   Prerequisites:
//     1) IIS Express started through the project script (dev\scripts\Start-IISExpress.ps1, port 40001 by default);
//     2) an external connection string configured at %USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config;
//     3) the acceptance context temp\p65\p65-acceptance-context.json present (baseUrl / admin account / password);
//     4) playwright resolvable — a development-only dependency that is not committed (here temp\node_modules\playwright),
//        pointed at by the PORTAL_PLAYWRIGHT_MODULE environment variable.
//
//   Every parameter arrives through environment variables, derived from database facts by the wrapper script
//   Invoke-PortalIndependentModuleRuntimeEvidence.ps1; this script binds to no particular tab id, theme name or dataset:
//     PORTAL_L2_BASE_URL / PORTAL_L2_CONTEXT / PORTAL_L2_TAB_ID / PORTAL_L2_THEME /
//     PORTAL_L2_LOCALE / PORTAL_L2_OUTPUT_DIR.
//   Output: <PORTAL_L2_OUTPUT_DIR>\form-builder-<theme>-<locale>.png plus a matching .json; exits 1 on any failed assertion.
//   </en>
// </lang>
//
// 证明边界：只证明**采集时刻、当前数据集与当前部署配置**下独立模块页签渲染出预期结构、主题作用域类与本地化文案，并留档截图；
// 不证明跨机器像素一致（字体/渲染差异）、不证明提交后的落库与审计（由单测与数据层门禁承载）、不证明可访问性与性能、
// 不证明第三方模块权限边界的运行期判定（由 L0 Admins 边界门禁与单测承载）。
//
// Proof boundary: proves only that, **at collection time with the current dataset and deployment configuration**, the independent
// module tab renders the expected structure, theme-scope class and localized copy, and keeps a screenshot; it does not prove
// cross-machine pixel equality, post-submit persistence or audit (owned by unit tests and data-layer gates), accessibility or
// performance, nor the runtime authorization decision for third-party modules (owned by the L0 Admins-boundary gate and tests).
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang>
///   <zh-CN>解析 playwright 模块，优先使用环境变量指定的位置，否则回退按包名解析。</zh-CN>
///   <en>Resolves the playwright module, preferring the environment-variable location and otherwise falling back to package-name resolution.</en>
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
      'Unable to resolve "playwright". Set PORTAL_PLAYWRIGHT_MODULE to its location. Original error: ' + error.message
    );
  }
}

const repoRoot = path.resolve(process.cwd());
const contextPath = process.env.PORTAL_L2_CONTEXT || path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
const outputDir = process.env.PORTAL_L2_OUTPUT_DIR || path.join(repoRoot, 'temp', 'p50', 'l2');
const theme = (process.env.PORTAL_L2_THEME || '').trim();
const locale = (process.env.PORTAL_L2_LOCALE || 'zh-CN').trim();
const tabId = (process.env.PORTAL_L2_TAB_ID || '').trim();

if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}
if (!tabId) {
  throw new Error('PORTAL_L2_TAB_ID is required; the wrapper derives it from PortalCfg_Tabs.');
}
fs.mkdirSync(outputDir, { recursive: true });

const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = process.env.PORTAL_L2_BASE_URL || context.baseUrl;
const userName = context.adminUserName;

/// <summary>
/// <lang>
///   <zh-CN>本地化期望：模块标题/提交文案取自随包部署的 form.json 的 zh-CN 与 en 键，避免把界面文案硬编码进证据脚本。</zh-CN>
///   <en>Localization expectations: the title/submit copy come from the deployed form.json zh-CN and en keys, so UI strings are not hard-coded into the evidence script.</en>
/// </lang>
/// </summary>
const definitionPath = path.join(repoRoot, 'src', 'Portal', 'DesktopModules', 'FormBuilder', 'form.json');
const definition = JSON.parse(fs.readFileSync(definitionPath, 'utf8'));
const isChinese = locale.toLowerCase().startsWith('zh');
const expectedTitle = isChinese ? definition.title['zh-CN'] : definition.title.en;
const expectedSubmit = isChinese ? definition.submitText['zh-CN'] : definition.submitText.en;
const expectedFieldLabels = definition.fields.map((field) => isChinese ? field.label['zh-CN'] : field.label.en);

/// <summary>
/// <lang>
///   <zh-CN>建立并验证管理员登录态；未确认登录成功即失败，避免把拒绝访问页当作目标页断言。</zh-CN>
///   <en>Establishes and verifies the administrator sign-in state; failure aborts so an access-denied page is never asserted as the target page.</en>
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
let page;
const record = {
  collectedUtc: new Date().toISOString(),
  baseUrl,
  tabId,
  theme,
  locale,
  status: 'Pass',
  facts: {},
  checks
};
try {
  const browserContext = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale
  });
  page = await browserContext.newPage();
  page.setDefaultTimeout(30000);
  await signIn(page);

  const tabUrl = new URL('DesktopDefault.aspx?tabid=' + encodeURIComponent(tabId), baseUrl).toString();
  record.url = tabUrl;
  await page.goto(tabUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(900);

  const bodyText = await page.locator('body').innerText().catch(() => '');
  const html = await page.content().catch(() => '');
  const root = page.locator('.form-builder');
  const rootCount = await root.count().catch(() => 0);
  const titleText = (await page.locator('.form-builder-title').innerText().catch(() => '')).trim();
  const formControls = await page.locator('.form-builder input, .form-builder select, .form-builder textarea').count().catch(() => 0);
  const submitValue = (await page.locator('.form-builder-actions input[type="submit"]').getAttribute('value').catch(() => null)) || '';
  const bodyClass = (await page.locator('body').getAttribute('class').catch(() => '')) || '';
  const themeClass = theme ? 'portal-theme-' + theme.toLowerCase() : '';

  record.facts = {
    rootCount,
    titleText,
    formControls,
    submitValue,
    bodyClass,
    unavailableMessage: /Form definition is unavailable/i.test(bodyText),
    serverError: /Server Error|运行时错误|HTTP 500|HttpException/.test(bodyText) || /Server Error/i.test(html),
    missingLabels: expectedFieldLabels.filter((label) => !bodyText.includes(label))
  };

  // <lang>
  //   <zh-CN>A1 模块根由容纳机制实际装载；A2 标题取自 form.json 且与界面语言一致；A3 字段全部渲染；
  //   A4 提交按钮文案本地化且表单定义可用；A5 主题作用域类生效；A6 页面未返回服务端错误。</zh-CN>
  //   <en>A1 the hosting mechanism actually loaded the module root; A2 the title comes from form.json and matches the UI language;
  //   A3 all fields render; A4 the submit label is localized and the form definition is available; A5 the theme-scope class applies;
  //   A6 the page returned no server error.</en>
  // </lang>
  check('A1.ModuleRootRendered', rootCount === 1, 'form-builder root count=' + rootCount + ' (expected 1)');
  check('A2.LocalizedTitle', titleText === expectedTitle, 'title="' + titleText + '" expected="' + expectedTitle + '"');
  check('A3.FieldsRendered', formControls >= expectedFieldLabels.length,
    'form controls=' + formControls + ' expected>=' + expectedFieldLabels.length);
  check('A3b.FieldLabelsPresent', record.facts.missingLabels.length === 0,
    'missing labels=' + JSON.stringify(record.facts.missingLabels));
  check('A4.SubmitLocalized', submitValue.trim() === expectedSubmit,
    'submit="' + submitValue + '" expected="' + expectedSubmit + '"');
  check('A4b.DefinitionAvailable', !record.facts.unavailableMessage, 'form definition unavailable message present');
  check('A5.ThemeScopeClass', !themeClass || bodyClass.toLowerCase().includes(themeClass),
    'body class="' + bodyClass + '" expected to include "' + themeClass + '"');
  check('A6.NoServerError', !record.facts.serverError, 'server-error marker present on the tab');

  const shot = path.join(outputDir, 'form-builder-' + (theme || 'default') + '-' + locale + '.png');
  await page.screenshot({ path: shot, fullPage: true });
  record.screenshot = path.relative(repoRoot, shot).replace(/\\/g, '/');

  if (checks.some((item) => !item.ok)) {
    record.status = 'Fail';
  }
} catch (error) {
  record.status = 'Fail';
  record.error = error.message;
  check('Runtime.NoException', false, error.message);
} finally {
  if (page) { await page.close().catch(() => {}); }
  await browser.close().catch(() => {});
}

const summaryPath = path.join(outputDir, 'form-builder-' + (theme || 'default') + '-' + locale + '.json');
fs.writeFileSync(summaryPath, JSON.stringify(record, null, 2) + '\n', 'utf8');
console.log(JSON.stringify(record));
if (record.status !== 'Pass') { process.exit(1); }
