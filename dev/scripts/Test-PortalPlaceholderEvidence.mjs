// <lang>
//   <zh-CN>占位文案运行期证据脚本（W-anp-P78 `P78.4`）：对**运行中的站点**逐页断言渲染结果里**不再出现**占位类硬编码字面量。</zh-CN>
//   <en>Placeholder-wording runtime evidence script (W-anp-P78 `P78.4`): visits each page of the **running site** and asserts that placeholder hard-coded literals no longer appear in the rendered output.</en>
// </lang>
//
// <lang>
//   <zh-CN>为什么需要它：占位类硬编码（`(none)` / `" / Overdue"` / `(not reviewed)` / `"—"`）的特点是"中文界面下也刺眼"，
//   但"是否真的换掉了"属于**运行期事实** —— msbuild 只看到资源键引用，单测用的是内存替身，都无法证明页面输出里没有残留字面量。
//   本脚本按语言（zh-CN / en-US）各跑一遍，覆盖后台 5 个列表页与前台 4 个业务页签。</zh-CN>
//   <en>Why it is required: whether the placeholders were actually replaced is a **runtime fact** — msbuild only sees resource-key
//   references and unit tests use in-memory stand-ins, so neither can prove the page output is free of literals. This script runs both
//   locales (zh-CN and en-US) across five administration list pages and four front-end business tabs.</en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，优先环境变量指定位置，否则按包名解析并给出明确指引。</zh-CN>
/// <en>Resolves the playwright module, preferring the environment-variable location and otherwise falling back to package-name resolution with clear guidance.</en></lang>
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
      'Unable to resolve "playwright". Set PORTAL_PLAYWRIGHT_MODULE to its location ' +
      '(for example <repo>\\temp\\node_modules\\playwright). Original error: ' + error.message
    );
  }
}

const repoRoot = path.resolve(process.cwd());
const outputDir = path.join(repoRoot, 'temp', 'p78', 'placeholder');
fs.mkdirSync(outputDir, { recursive: true });

const context = JSON.parse(fs.readFileSync(path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json'), 'utf8'));
const baseUrl = context.baseUrl;

// <lang>
//   <zh-CN>待扫描的字面量，按语言区分：zh-CN 侧扫 4 项（占位、硬编码英文超期后缀、未审核占位、破折号占位）；
//   en-US 侧**不扫 `(none)`** —— 因为 `Common_NonePlaceholder` 的英文值**就是** `(none)`（沿用既有英文风格），
//   英文界面出现它是**预期结果**而非残留。第一轮实测正是把这一点误判为 Fail，据此收紧断言口径。</zh-CN>
//   <en>Literals under scan, per locale: the zh-CN pass checks all four (placeholder, hard-coded English overdue suffix,
//   not-reviewed placeholder, em-dash placeholder); the en-US pass deliberately **skips `(none)`** because the English value
//   of `Common_NonePlaceholder` **is** `(none)` (the established English wording), so seeing it in the English UI is the
//   intended result rather than a leftover. The first run misjudged exactly this point as a failure, so the assertion was tightened.</en>
// </lang>
const literalsByLocale = {
  'zh-CN': ['(none)', ' / Overdue', '(not reviewed)', '—'],
  'en-US': [' / Overdue', '(not reviewed)', '—']
};

// <lang>
//   <zh-CN>目标页：后台 5 个列表页 + 前台 4 个业务页签（页签 id 取自开发库注册）。</zh-CN>
//   <en>Target pages: five administration list pages plus four front-end business tabs (tab identifiers taken from the development database registration).</en>
// </lang>
const targets = [
  { id: 'admin-business-applications', url: new URL('Admin/BusinessApplications.aspx', baseUrl).toString() },
  { id: 'admin-correction-requests', url: new URL('Admin/EmployeeProfileCorrectionRequests.aspx', baseUrl).toString() },
  { id: 'admin-manage-users', url: new URL('Admin/ManageUsers.aspx', baseUrl).toString() },
  { id: 'admin-work-items', url: new URL('Admin/WorkItems.aspx', baseUrl).toString() },
  { id: 'admin-collaboration-items', url: new URL('Admin/CollaborationItems.aspx', baseUrl).toString() },
  { id: 'front-workbench', url: new URL('DesktopDefault.aspx?tabindex=21&tabid=1010', baseUrl).toString() },
  { id: 'front-correction', url: new URL('DesktopDefault.aspx?tabindex=20&tabid=1009', baseUrl).toString() },
  { id: 'front-confirm', url: new URL('DesktopDefault.aspx?tabindex=20&tabid=1008', baseUrl).toString() },
  { id: 'front-myworkitems', url: new URL('DesktopDefault.aspx?tabindex=23&tabid=1011', baseUrl).toString() }
];

const observations = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  for (const locale of ['zh-CN', 'en-US']) {
    const browserContext = await browser.newContext({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 1,
      locale
    });
    const page = await browserContext.newPage();
    page.setDefaultTimeout(40000);

    await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('input[id$="EmailOrName"]').fill(context.adminUserName);
    await page.locator('input[id$="password"]').fill(context.password);
    await Promise.all([
      page.waitForLoadState('domcontentloaded').catch(() => {}),
      page.locator('input[id$="SigninBtn"]').click()
    ]);
    await page.waitForTimeout(1200);

    for (const target of targets) {
      const record = { locale, id: target.id, url: target.url, status: 'Pass', facts: {}, notes: [] };
      try {
        await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await page.waitForTimeout(600);
        const html = await page.content();

        record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
        record.facts.foundLiterals = (literalsByLocale[locale] || []).filter((literal) => html.includes(literal));

        if (record.facts.errorPage) {
          record.status = 'Fail';
          record.notes.push('The page fell back to the generic error page.');
        }
        if (record.facts.foundLiterals.length > 0) {
          record.status = 'Fail';
          record.notes.push('Placeholder literals still rendered: ' + record.facts.foundLiterals.join(' | '));
        }

        if (locale === 'zh-CN') {
          record.screenshot = path.relative(repoRoot, path.join(outputDir, target.id + '.png'));
          await page.screenshot({ path: path.join(outputDir, target.id + '.png'), fullPage: false });
        }
      } catch (error) {
        record.status = 'Fail';
        record.notes.push(error instanceof Error ? error.message : String(error));
      }
      observations.push(record);
    }

    await browserContext.close();
  }
} finally {
  await browser.close();
}

fs.writeFileSync(path.join(outputDir, 'placeholder-summary.json'), JSON.stringify(observations, null, 2), 'utf8');

let failed = 0;
for (const record of observations) {
  if (record.status === 'Fail') {
    failed++;
  }
  console.log('[' + record.status + '] ' + record.locale + ' ' + record.id +
    ' errorPage=' + record.facts.errorPage +
    ' literals=' + JSON.stringify(record.facts.foundLiterals || []));
  for (const note of record.notes) {
    console.log('    note: ' + note);
  }
}

console.log('placeholder evidence completed: ' + observations.length + ' record(s), ' + failed + ' failed');
process.exit(failed > 0 ? 1 : 0);
