// <lang>
//   <zh-CN>模块运行期证据脚本（发布前门禁）：对**运行中的站点**以管理员登录，逐个访问模块目标页并断言"能编译加载并渲染"。
//
//   为什么必须有它：ASCX 由 ASP.NET **运行期动态编译**，msbuild 不校验 ASCX —— 因此"构建 0 错 0 警 + 单测全绿 + XML 门禁通过"
//   仍可能对 ASCX 里的编译错误（例如引用类型缺 <%@ Import Namespace %> 导致 CS0103）完全失明。P74.3 就踩过：三个前台模块
//   同时中招，只有被部署的那个在真实站点加载时才暴露。
//
//   前置条件：
//     1) IIS Express 已启动（`dev\scripts\Start-IISExpress.ps1`，默认 40001）；
//     2) 外置连接串已配置（`%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`）；
//     3) 验收上下文 `temp\p65\p65-acceptance-context.json` 与 `temp\p64\p64-regression-context.json` 存在（提供 baseUrl、管理员账号、口令、页签 URL）；
//     4) 目标模块所在的档位已启用（见 appSettings.json 的 Portal.ModuleProfiles.*；基线为 CoreOnly 时前台业务模块不会渲染，
//        需用 `appSettings.dev.json` 临时覆盖 Active，验证后还原）；
//     5) playwright 可解析 —— 它是**开发期依赖且不入库**（本机位于 `temp\node_modules\playwright`），
//        故执行前须通过 `PORTAL_PLAYWRIGHT_MODULE` 指向该模块。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalModuleRuntimeEvidence.mjs'
//   自定义目标（可选）：
//     $env:PORTAL_MODULE_TARGETS = '[{"id":"myworkitems","url":"http://localhost:40001/DesktopDefault.aspx?tabindex=10&tabid=1011","moduleSelector":".my-work-items"}]'
//   产物：`temp\p74\module-verify\`（截图 + module-verify-summary.json）；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>Module runtime evidence script (pre-release gate): signs in as an administrator against the **running site** and asserts, per module target page, that it compiles, loads, and renders.
//
//   Why it is required: ASCX files are compiled **at runtime** by ASP.NET and are not validated by msbuild, so "build with zero warnings plus green unit tests plus the XML gate" can still be blind to
//   ASCX compile errors (for example CS0103 caused by a missing &lt;%@ Import Namespace %&gt;). P74.3 hit exactly that: three front-end modules were affected and only the deployed one surfaced it.
//
//   Prerequisites:
//     1) IIS Express started (`dev\scripts\Start-IISExpress.ps1`, port 40001 by default);
//     2) an external connection string configured at `%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`;
//     3) the acceptance contexts `temp\p65\p65-acceptance-context.json` and `temp\p64\p64-regression-context.json` present (they supply baseUrl, the administrator account, the password, and tab URLs);
//     4) the profile that owns the target modules is active (see Portal.ModuleProfiles.* in appSettings.json; with the baseline CoreOnly profile the business modules do not render,
//        so override Active temporarily through `appSettings.dev.json` and restore it afterwards);
//     5) playwright resolvable — it is a **development-only dependency that is not committed** (on this machine it lives in `temp\node_modules\playwright`),
//        so `PORTAL_PLAYWRIGHT_MODULE` must point at that module before running.
//
//   Usage (from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalModuleRuntimeEvidence.mjs'
//   Optional custom targets:
//     $env:PORTAL_MODULE_TARGETS = '[{"id":"myworkitems","url":"http://localhost:40001/DesktopDefault.aspx?tabindex=10&tabid=1011","moduleSelector":".my-work-items"}]'
//   Output: `temp\p74\module-verify\` (screenshots plus module-verify-summary.json); exits with code 1 when any assertion fails.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，优先使用环境变量指定的位置，否则回退到按包名解析。</zh-CN>
/// <en>Resolves the playwright module, preferring an environment-variable location and otherwise falling back to package-name resolution.</en></lang>
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
// <lang>
//   <zh-CN>输出目录默认沿用 `temp\p74\module-verify`（保持既有调用方行为不变），但允许用
//   `PORTAL_MODULE_EVIDENCE_DIR` 覆盖 —— 编排层 `Invoke-PortalModuleRuntimeGate.ps1` 要按"组"分别留证，
//   三组写进同一目录会互相覆盖截图与摘要。这是**只增不改**：不设该变量时行为与之前完全一致。</zh-CN>
//   <en>The output directory keeps the existing `temp\p74\module-verify` default (existing callers are unaffected) but may be
//   overridden through `PORTAL_MODULE_EVIDENCE_DIR`, because the orchestration layer
//   `Invoke-PortalModuleRuntimeGate.ps1` archives evidence **per group** and three groups writing into one directory would
//   overwrite each other's screenshots and summary. This is additive only: with the variable unset the behavior is identical
//   to before.</en>
// </lang>
const outputDir = process.env.PORTAL_MODULE_EVIDENCE_DIR
  ? path.resolve(process.env.PORTAL_MODULE_EVIDENCE_DIR)
  : path.join(repoRoot, 'temp', 'p74', 'module-verify');
fs.mkdirSync(outputDir, { recursive: true });

const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
const p64Path = path.join(repoRoot, 'temp', 'p64', 'p64-regression-context.json');
if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}

const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const p64 = fs.existsSync(p64Path) ? JSON.parse(fs.readFileSync(p64Path, 'utf8')) : null;

// <lang>
//   <zh-CN>默认目标对应本项目开发库已挂载的场景页签；页签序号会随新页签插入而位移，需用 -SqlMigrationFile 之类显式参数或本脚本文档的方式校正，
//   也可以通过 PORTAL_MODULE_TARGETS 环境变量整体覆盖。</zh-CN>
//   <en>The default targets correspond to the scenario tabs mounted in this project's development database; tab indexes shift when new tabs are inserted,
//   so correct them as documented here or override them entirely through PORTAL_MODULE_TARGETS.</en>
// </lang>
const defaultTargets = [
  {
    id: 'workbench',
    url: new URL('DesktopDefault.aspx?tabindex=9&tabid=1010', context.baseUrl).toString(),
    moduleSelector: '.enterprise-workbench'
  },
  {
    id: 'myworkitems',
    url: new URL('DesktopDefault.aspx?tabindex=10&tabid=1011', context.baseUrl).toString(),
    moduleSelector: '.my-work-items',
    requireSharedTitle: true,
    expectEmptyStateMarker: ['暂无待办', '没有符合条件的待办']
  }
];

if (p64 && p64.tabUrl) {
  defaultTargets.push({ id: 'p64-confirm', url: p64.tabUrl, moduleSelector: '.employee-profile-confirm' });
}
if (context.tabUrl) {
  defaultTargets.push({ id: 'p65-correction', url: context.tabUrl, moduleSelector: '.employee-profile-correction' });
}

let targets = defaultTargets;
if (process.env.PORTAL_MODULE_TARGETS) {
  targets = JSON.parse(process.env.PORTAL_MODULE_TARGETS);
}

const results = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
try {
  const browserContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1, locale: 'zh-CN' });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(40000);

  await page.goto(context.baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('input[id$="EmailOrName"]').fill(context.adminUserName);
  await page.locator('input[id$="password"]').fill(context.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);

  for (const target of targets) {
    const record = { id: target.id, url: target.url, status: 'Pass', facts: {}, notes: [] };
    try {
      await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);
      const html = await page.content();
      const text = await page.locator('body').innerText().catch(() => '');

      record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
      // <lang>
      //   <zh-CN>`moduleSelector` 是**可选**的加强断言，不是必需项：编排层按 ascx 标记推导根 class，通用模块
      //   （HtmlModule / XmlModule / ImageModule 等）没有专属根 class，推导会失败。此时**只**保留"未落通用错误页"
      //   这一条核心断言 —— 它已经等价于"运行期编译与加载成功"，也正是本门禁存在的理由。
      //   早先版本在这里无条件调用 `target.moduleSelector.slice(1)`，选择器缺失时抛
      //   "Cannot read properties of null"，把"覆盖不足"伪装成"门禁崩溃"，反而更难定位。</zh-CN>
      //   <en>`moduleSelector` is an **optional** stronger assertion, not a requirement: the orchestration layer derives the root
      //   class from the ascx markup, and generic modules (HtmlModule / XmlModule / ImageModule and the like) have no dedicated
      //   root class, so derivation yields nothing for them. In that case only the core assertion — "did not fall back to the
      //   generic error page" — is kept, and that alone already means "runtime compilation and load succeeded", which is the
      //   whole reason this gate exists. An earlier version called `target.moduleSelector.slice(1)` unconditionally, so a
      //   missing selector threw "Cannot read properties of null", disguising "insufficient coverage" as "the gate crashed",
      //   which is much harder to diagnose.</en>
      // </lang>
      const selector = target.moduleSelector || null;
      record.facts.selectorAvailable = Boolean(selector);
      if (selector) {
        record.facts.modulePresent = html.includes(selector.slice(1));
      }
      record.facts.emptyStateClass = html.includes('portal-empty-state');
      record.facts.emptyMarkers = (target.expectEmptyStateMarker || []).filter((marker) => text.includes(marker));
      record.facts.sharedTitleText = await page.locator('.portal-module-header .portal-module-title').first().innerText().catch(() => '');
      record.facts.sharedTitlePresent = (record.facts.sharedTitleText || '').trim().length > 0;

      if (record.facts.errorPage) {
        record.status = 'Fail';
        record.notes.push('The page fell back to the generic error page (runtime compilation or load failure).');
      }
      if (selector && !record.facts.modulePresent) {
        record.status = 'Fail';
        record.notes.push(`Module root for ${target.moduleSelector} was not found.`);
      }
      if (target.expectEmptyStateMarker && record.facts.emptyMarkers.length === 0) {
        record.status = 'Fail';
        record.notes.push('Expected the module empty state but no marker matched.');
      }
      if (target.requireSharedTitle && !record.facts.sharedTitlePresent) {
        record.status = 'Fail';
        record.notes.push('The shared module-title control rendered no title text.');
      }

      const shot = path.join(outputDir, `${target.id}.png`);
      await page.screenshot({ path: shot, fullPage: false });
      record.screenshot = path.relative(repoRoot, shot);
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

fs.writeFileSync(path.join(outputDir, 'module-verify-summary.json'), JSON.stringify(results, null, 2), 'utf8');
for (const record of results) {
  console.log(`[${record.status}] ${record.id}`);
  console.log(`    module=${record.facts.modulePresent} errorPage=${record.facts.errorPage} emptyClass=${record.facts.emptyStateClass} markers=[${(record.facts.emptyMarkers || []).join(',')}]`);
  console.log(`    sharedTitle="${record.facts.sharedTitleText}" present=${record.facts.sharedTitlePresent}`);
  for (const note of record.notes) {
    console.log(`    note: ${note}`);
  }
}

if (results.some((record) => record.status !== 'Pass')) {
  process.exitCode = 1;
}
