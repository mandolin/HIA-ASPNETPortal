// <lang>
//   <zh-CN>`Test-PortalLocalizedNameRuntimeEvidence.ps1` 的 playwright 辅助脚本。
//   从 spec.json 读取 baseUrl、凭据、选择器与目标页列表，逐个文化登录并取 HTML，结果写入 out.json。
//   独立成文件（而非嵌在 ps1 的 here-string 里）的原因：here-string 内的 `//` 注释与 `<` 字符
//   会被 PowerShell 解析层误判，实测踩过两次。</zh-CN>
//   <en>Playwright helper for `Test-PortalLocalizedNameRuntimeEvidence.ps1`.
//   Reads baseUrl, credentials, selectors, and the target-page list from spec.json, signs in per culture and fetches the HTML,
//   writing the results to out.json. It is a separate file (rather than embedded in the ps1 here-string) because `//` comments and
//   `<` characters inside a here-string are misjudged by the PowerShell parsing layer — hit twice in practice.</en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <lang>
///   <zh-CN>与运行期门禁同款：优先环境变量指定位置，否则按包名回退解析。</zh-CN>
///   <en>Same as the runtime gate: prefer the environment-variable location, otherwise fall back to package-name resolution.</en>
/// </lang>
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

const spec = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const chromium = await loadChromium();
const browser = await chromium.launch();
const out = {};

for (const item of spec.targets) {
  const context = await browser.newContext({ locale: item.culture, viewport: { width: 1600, height: 1200 } });
  const page = await context.newPage();
  await context.addCookies([{ name: 'lang', value: item.culture, domain: 'localhost', path: '/' }]);

  await page.goto(spec.base + '/DesktopDefault.aspx?tabid=1', { waitUntil: 'networkidle' });
  const username = await page.$(spec.selectors.username);
  const password = await page.$(spec.selectors.password);
  if (username && password) {
    await username.fill(spec.adminUser);
    await password.fill(spec.adminPassword);
    const submit = await page.$(spec.selectors.submit);
    if (submit) { await submit.click(); }
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1200);
  }

  await page.goto(spec.base + '/DesktopDefault.aspx?tabid=' + item.tabId, { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  out[item.key] = await page.content();
  await context.close();
}

fs.writeFileSync(process.argv[3], JSON.stringify(out));
await browser.close();
