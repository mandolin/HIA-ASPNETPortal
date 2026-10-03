// <lang>
//   <zh-CN>界面语言泄漏检测器：在 `en-US` 渲染下断言指定区域**不含中文字符**。
//
//   为什么需要它：源码里的硬编码中文（字符串字面量而非资源键）在中文界面下**完全看不出来**，
//   只有切到英文界面才会暴露为"英文界面里露出中文"。`W76` 盘点因此登记了
//   `EmployeeProfileCorrectionRequest.ascx.cs` 的 3 处硬编码中文；而同一家族在仓内还有另外若干处，
//   逐个人工判断既慢又容易漏。本脚本把该判定做成实测：**同一页面、同一账号，分别以 zh-CN 与 en-US 渲染，
//   再对目标区域做中文字符检测**。
//
//   它能回答的问题：这个面上还有没有硬编码中文在英文界面里泄漏。
//   它不能回答的问题：文案是否准确、是否可行动、术语是否一致（那是文案打磨的范围，不是本脚本）。
//
//   前置条件：
//     1) 站点已启动并服务当前工作树；外置连接串已配置；目标模块所在档位已启用；
//     2) 验收上下文 `temp\p65\p65-acceptance-context.json` 存在（提供 baseUrl 与账号口令）；
//     3) playwright 通过 `PORTAL_PLAYWRIGHT_MODULE` 指向（开发期依赖，不入库）。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalLocalizationLeakEvidence.mjs'
//   可通过环境变量覆盖目标：
//     PORTAL_LEAK_USER        登录名（默认取验收上下文中的 unboundUserName，用于命中"无在职绑定"分支）
//     PORTAL_LEAK_URL         目标页 URL（默认取验收上下文中的 tabUrl）
//     PORTAL_LEAK_SELECTOR    目标区域选择器（默认 '.employee-profile-correction'）
//   产物：`temp\localization-leak\`（两种语言的截图 + localization-leak-summary.json）；en-US 检出中文即以退出码 1 结束。
//   </zh-CN>
//   <en>UI language-leak detector: asserts that a given region contains **no CJK characters** when rendered under `en-US`.
//
//   Why it is needed: hard-coded Chinese in source (a string literal instead of a resource key) is completely invisible in the Chinese UI and only surfaces in the English UI as "Chinese leaking into an English page". The W76 inventory therefore registered three such literals in `EmployeeProfileCorrectionRequest.ascx.cs`, and the same family has further instances elsewhere in the repository, which are slow and error-prone to judge by hand. This script makes the judgement empirical: **the same page and account rendered under zh-CN and then en-US, followed by a CJK check on the target region**.
//
//   What it answers: whether this surface still leaks hard-coded Chinese into the English UI.
//   What it does not answer: whether the wording is accurate, actionable, or terminologically consistent (that belongs to wording polish, not this script).
//
//   Prerequisites:
//     1) the site started and serving the current working tree, the external connection string configured, and the profile owning the target module active;
//     2) the acceptance context `temp\p65\p65-acceptance-context.json` present (it supplies baseUrl and the account password);
//     3) playwright reachable through `PORTAL_PLAYWRIGHT_MODULE` (development-only dependency, not committed).
//
//   Usage (from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalLocalizationLeakEvidence.mjs'
//   Targets can be overridden through environment variables:
//     PORTAL_LEAK_USER        sign-in name (defaults to the acceptance context's unboundUserName, which hits the "no active binding" branch)
//     PORTAL_LEAK_URL         target page URL (defaults to the acceptance context's tabUrl)
//     PORTAL_LEAK_SELECTOR    target region selector (defaults to '.employee-profile-correction')
//   Output: `temp\localization-leak\` (screenshots for both languages plus localization-leak-summary.json); exits with code 1 when CJK is detected under en-US.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，优先环境变量指定位置。</zh-CN>
/// <en>Resolves the playwright module, preferring the environment-variable location.</en></lang>
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
const outputDir = path.join(repoRoot, 'temp', 'localization-leak');
fs.mkdirSync(outputDir, { recursive: true });

const context = JSON.parse(fs.readFileSync(path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json'), 'utf8'));
const userName = process.env.PORTAL_LEAK_USER || context.unboundUserName;
const targetUrl = process.env.PORTAL_LEAK_URL || context.tabUrl;
const selector = process.env.PORTAL_LEAK_SELECTOR || '.employee-profile-correction';

// <lang>
//   <zh-CN>中文字符范围：CJK 统一表意文字基本区。只检测该范围，避免把日文假名或标点误判进来。</zh-CN>
//   <en>The CJK range checked here is the unified ideographs block, so kana or punctuation are not misreported.</en>
// </lang>
const cjkPattern = /[\u4e00-\u9fff]/;

const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
const observations = [];

try {
  for (const locale of ['zh-CN', 'en-US']) {
    const record = { locale, url: targetUrl, user: userName, status: 'Pass', facts: {}, notes: [] };
    const browserContext = await browser.newContext({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 1,
      locale
    });
    const page = await browserContext.newPage();
    page.setDefaultTimeout(40000);

    try {
      await page.goto(context.baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.locator('input[id$="EmailOrName"]').fill(userName);
      await page.locator('input[id$="password"]').fill(context.password);
      await Promise.all([
        page.waitForLoadState('domcontentloaded').catch(() => {}),
        page.locator('input[id$="SigninBtn"]').click()
      ]);
      await page.waitForTimeout(1200);

      await page.goto(targetUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);

      const html = await page.content();
      const region = page.locator(selector).first();
      const regionPresent = (await region.count()) > 0;
      const regionText = regionPresent ? ((await region.innerText().catch(() => '')) || '') : '';

      record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
      record.facts.regionPresent = regionPresent;
      record.facts.regionText = regionText.replace(/\s+/g, ' ').trim();
      record.facts.containsCjk = cjkPattern.test(regionText);

      if (record.facts.errorPage) {
        record.status = 'Fail';
        record.notes.push('The page fell back to the generic error page.');
      }
      if (!regionPresent) {
        record.status = 'Fail';
        record.notes.push(`The target region ${selector} was not rendered, so nothing could be judged.`);
      }
      if (locale === 'en-US' && regionPresent && record.facts.containsCjk) {
        record.status = 'Fail';
        record.notes.push('The en-US render still contains CJK characters: hard-coded Chinese is leaking into the English UI.');
      }

      record.screenshot = path.relative(repoRoot, path.join(outputDir, `leak-${locale}.png`));
      await page.screenshot({ path: path.join(outputDir, `leak-${locale}.png`), fullPage: false });
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    } finally {
      await browserContext.close();
    }

    observations.push(record);
  }
} finally {
  await browser.close();
}

fs.writeFileSync(path.join(outputDir, 'localization-leak-summary.json'), JSON.stringify(observations, null, 2), 'utf8');

let failed = 0;
for (const record of observations) {
  if (record.status === 'Fail') failed++;
  console.log(`[${record.status}] locale=${record.locale}`);
  for (const [key, value] of Object.entries(record.facts)) {
    console.log(`    ${key} = ${JSON.stringify(value)}`);
  }
  for (const note of record.notes) {
    console.log(`    note: ${note}`);
  }
}

console.log(`localization leak evidence completed: ${observations.length} record(s), ${failed} failed`);
process.exit(failed > 0 ? 1 : 0);
