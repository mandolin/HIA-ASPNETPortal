// <lang>
//   <zh-CN>P77 补充运行期证据脚本（收口遗留）：把两项"只有运行期能闭合"的遗留做成可复现断言。
//
//   1) **深色皮肤验证**（`W77` 收口遗留第 4 项）：此前深色只按真实主题 CSS 出过原型图，未在站点上跑过。
//      本脚本对「我的待办」页签施加深色皮肤覆盖后，实测**文本与背景的对比度**（WCAG AA 4.5:1），
//      而不是只说"看起来清楚"—— `U5` 的教训正是"硬编码浅色 + 深色皮肤 = 浅字压白底"。
//   2) **`D1` 多实例取舍复演**（`W77` 收口遗留第 2 项）：此前只有单测。本脚本在夹具造出三个候选承载页签后，
//      断言解析器选中**顺序最小且用户有权**的那个：跳过"顺序更小但无权"的页签，也不选"顺序更大"的既有页签。
//
//   前置条件：
//     1) IIS Express / 站点已启动并服务当前工作树；外置连接串已配置；
//     2) 验收上下文 `temp\p65\p65-acceptance-context.json` 存在；
//     3) 业务模块档位已启用（`Portal.ModuleProfiles.Active=BusinessWorkflow`；验证后须还原）；
//     4) 待办夹具已造数（`dev\scripts\New-PortalP77ReachabilityFixture.ps1 -Action Seed`）；
//     5) 补充夹具已应用（`dev\scripts\New-PortalP77SupplementFixture.ps1 -Action DarkTheme` / `-Action MultiInstance`）；
//     6) playwright 通过 `PORTAL_PLAYWRIGHT_MODULE` 指向（开发期依赖，不入库）。
//
//   **多实例项的前置补充（实测踩到）**：`ModulesDb` 与 `TabsDb` 在**构造时**把配置表读入内存快照（`_items = _context.Modules.ToList()`），
//   因此**新插入的页签/模块实例在下次应用域回收前不可见**——第一次复演就因此失败（解析仍选中旧页签）。
//   触发回收的方式：以相同内容重写 `src\Portal\web.config`（内容不变、仅时间戳变化），等待数秒后再跑。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalP77SupplementEvidence.mjs'          # 两项都跑
//     $env:PORTAL_SUPPLEMENT_MODES = 'dark'                               # 或只跑其中一项
//   产物：`temp\p77\supplement\`（截图 + supplement-summary.json）；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>P77 supplementary runtime evidence script (closeout leftovers): turns the two leftovers that only runtime can close into reproducible assertions.
//
//   1) **Dark-skin verification** (closeout leftover 4): dark was previously covered only by prototypes rendered from the real theme CSS, never on the running site. After applying a dark-skin override to the "My To-Do Items" tab, this script measures the **contrast between text and background** (WCAG AA 4.5:1) rather than claiming "it looks clear" — the U5 lesson was exactly that hard-coded light colours plus a dark skin produce light text on a white ground.
//   2) **D1 multi-instance replay** (closeout leftover 2): previously covered only by unit tests. With the fixture creating three candidate hosting tabs, this script asserts the resolver picks the **smallest-order tab the user may access**: it skips the smaller-order tab the user cannot access and does not fall back to the larger-order tab chosen previously.
//
//   Prerequisites:
//     1) IIS Express / the site started and serving the current working tree, with the external connection string configured;
//     2) the acceptance context `temp\p65\p65-acceptance-context.json` present;
//     3) the business-module profile active (`Portal.ModuleProfiles.Active=BusinessWorkflow`; restore it afterwards);
//     4) the work-item fixture seeded (`dev\scripts\New-PortalP77ReachabilityFixture.ps1 -Action Seed`);
//     5) the supplementary fixture applied (`dev\scripts\New-PortalP77SupplementFixture.ps1 -Action DarkTheme` / `-Action MultiInstance`);
//     6) playwright reachable through `PORTAL_PLAYWRIGHT_MODULE` (development-only dependency, not committed).
//
//   Usage (from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalP77SupplementEvidence.mjs'          # both
//     $env:PORTAL_SUPPLEMENT_MODES = 'dark'                               # or a subset
//   Output: `temp\p77\supplement\` (screenshots plus supplement-summary.json); exits with code 1 when any assertion fails.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，优先环境变量指定的位置，否则按包名解析并给出明确指引。</zh-CN>
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
const outputDir = path.join(repoRoot, 'temp', 'p77', 'supplement');
fs.mkdirSync(outputDir, { recursive: true });

const context = JSON.parse(fs.readFileSync(path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json'), 'utf8'));
const baseUrl = context.baseUrl;
const myWorkItemsUrl = new URL('DesktopDefault.aspx?tabindex=10&tabid=1011', baseUrl).toString();

const modes = (process.env.PORTAL_SUPPLEMENT_MODES || 'dark,multi').split(',').map((m) => m.trim()).filter(Boolean);
const expectedAllowedTabId = Number(process.env.PORTAL_SUPPLEMENT_ALLOWED_TAB || 1099);
const expectedBlockedTabId = Number(process.env.PORTAL_SUPPLEMENT_BLOCKED_TAB || 1098);
const expectedExistingTabId = Number(process.env.PORTAL_SUPPLEMENT_EXISTING_TAB || 1010);
const minimumContrastRatio = Number(process.env.PORTAL_SUPPLEMENT_MIN_CONTRAST || 4.5);

const results = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  const browserContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1, locale: 'zh-CN' });
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

  if (modes.includes('dark')) {
    const record = { id: 'dark-skin-contrast', url: myWorkItemsUrl, status: 'Pass', facts: {}, notes: [] };
    try {
      await page.goto(myWorkItemsUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);

      const html = await page.content();
      record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
      record.facts.modulePresent = html.includes('my-work-items');

      // <lang>
      //   <zh-CN>实测"文本色 vs 有效背景色"的对比度：背景色逐级向上取第一个不透明值，避免把 transparent 当成白底。</zh-CN>
      //   <en>Measure the contrast between text colour and the effective background: walk upward for the first opaque background so a transparent background is not mistaken for white.</en>
      // </lang>
      const measurement = await page.evaluate(() => {
        const parse = (value) => {
          const match = value.match(/rgba?\(([^)]+)\)/);
          if (!match) return null;
          const parts = match[1].split(',').map((p) => parseFloat(p.trim()));
          return { r: parts[0], g: parts[1], b: parts[2], a: parts.length > 3 ? parts[3] : 1 };
        };

        const effectiveBackground = (element) => {
          let current = element;
          while (current) {
            const color = parse(window.getComputedStyle(current).backgroundColor);
            if (color && color.a > 0) return color;
            current = current.parentElement;
          }
          return { r: 255, g: 255, b: 255, a: 1 };
        };

        const luminance = (color) => {
          const channel = (raw) => {
            const value = raw / 255;
            return value <= 0.03928 ? value / 12.92 : Math.pow((value + 0.055) / 1.055, 2.4);
          };
          return 0.2126 * channel(color.r) + 0.7152 * channel(color.g) + 0.0722 * channel(color.b);
        };

        const contrast = (foreground, background) => {
          const l1 = luminance(foreground);
          const l2 = luminance(background);
          return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
        };

        const rows = Array.from(document.querySelectorAll('table.my-work-items-table tbody tr'));
        const linkRow = rows.find((row) => (row.innerText || '').includes('P77 运行期验证：协同事项'));
        const plainRow = rows.find((row) => (row.innerText || '').includes('P77 运行期验证：业务申请'));

        const measure = (element) => {
          if (!element) return null;
          const background = effectiveBackground(element);
          const foreground = parse(window.getComputedStyle(element).color);
          if (!foreground) return null;
          return {
            color: window.getComputedStyle(element).color,
            background: `rgb(${background.r}, ${background.g}, ${background.b})`,
            contrast: Number(contrast(foreground, background).toFixed(2))
          };
        };

        return {
          bodyBackground: window.getComputedStyle(document.body).backgroundColor,
          link: measure(linkRow ? linkRow.querySelector('a') : null),
          hint: measure(plainRow ? plainRow.querySelector('.my-work-items-unavailable') : null),
          plainTitle: measure(plainRow ? plainRow.children[1].querySelector('span') : null)
        };
      });

      record.facts.measurement = measurement;

      if (record.facts.errorPage || !record.facts.modulePresent) {
        record.status = 'Fail';
        record.notes.push('The module did not render under the dark skin.');
      }
      if (!measurement.link || !measurement.hint) {
        record.status = 'Fail';
        record.notes.push('The link or the hint element was not found for measurement.');
      } else {
        if (measurement.link.contrast < minimumContrastRatio) {
          record.status = 'Fail';
          record.notes.push(`Link contrast ${measurement.link.contrast} is below ${minimumContrastRatio}.`);
        }
        if (measurement.hint.contrast < minimumContrastRatio) {
          record.status = 'Fail';
          record.notes.push(`Hint contrast ${measurement.hint.contrast} is below ${minimumContrastRatio}.`);
        }
      }

      record.screenshot = path.relative(repoRoot, path.join(outputDir, 'dark-skin.png'));
      await page.screenshot({ path: path.join(outputDir, 'dark-skin.png'), fullPage: false });
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }
    results.push(record);
  }

  if (modes.includes('multi')) {
    const record = { id: 'multi-instance-selection', url: myWorkItemsUrl, status: 'Pass', facts: {}, notes: [] };
    try {
      await page.goto(myWorkItemsUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);

      const rows = page.locator('table.my-work-items-table tbody tr');
      const count = await rows.count();
      let hrefs = null;
      for (let i = 0; i < count; i++) {
        const row = rows.nth(i);
        const text = (await row.innerText().catch(() => '')) || '';
        if (text.includes('P77 运行期验证：协同事项')) {
          hrefs = await row.locator('a').evaluateAll((anchors) => anchors.map((a) => a.getAttribute('href') || ''));
          break;
        }
      }

      record.facts.collaborationLinks = hrefs;

      if (!hrefs || hrefs.length === 0) {
        record.status = 'Fail';
        record.notes.push('The collaboration row produced no link under the multi-instance fixture.');
      } else {
        record.facts.selectedAllowedTab = hrefs.some((href) => href.includes('tabid=' + expectedAllowedTabId));
        record.facts.selectedBlockedTab = hrefs.some((href) => href.includes('tabid=' + expectedBlockedTabId));
        record.facts.selectedExistingTab = hrefs.some((href) => href.includes('tabid=' + expectedExistingTabId));

        if (!record.facts.selectedAllowedTab) {
          record.status = 'Fail';
          record.notes.push(`Expected the smallest accessible tab ${expectedAllowedTabId} to be selected.`);
        }
        if (record.facts.selectedBlockedTab) {
          record.status = 'Fail';
          record.notes.push(`The inaccessible tab ${expectedBlockedTabId} was selected despite failing the role check.`);
        }
        if (record.facts.selectedExistingTab) {
          record.status = 'Fail';
          record.notes.push(`The larger-order tab ${expectedExistingTabId} was selected although a smaller accessible tab exists.`);
        }
      }

      record.screenshot = path.relative(repoRoot, path.join(outputDir, 'multi-instance.png'));
      await page.screenshot({ path: path.join(outputDir, 'multi-instance.png'), fullPage: false });
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }
    results.push(record);
  }
} finally {
  await browser.close();
}

fs.writeFileSync(path.join(outputDir, 'supplement-summary.json'), JSON.stringify(results, null, 2), 'utf8');

let failed = 0;
for (const record of results) {
  if (record.status === 'Fail') failed++;
  console.log(`[${record.status}] ${record.id}`);
  for (const [key, value] of Object.entries(record.facts)) {
    console.log(`    ${key} = ${JSON.stringify(value)}`);
  }
  for (const note of record.notes) {
    console.log(`    note: ${note}`);
  }
}

console.log(`supplement evidence completed: ${results.length} record(s), ${failed} failed`);
process.exit(failed > 0 ? 1 : 0);
