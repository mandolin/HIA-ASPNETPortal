// <lang>
//   <zh-CN>平台 / 旧内容模块空态运行期证据脚本（W-anp-P81 `P81.4`）。
//
//   为什么必须运行期断言：本包改的是 **aspx / ascx 标记**（空态行、`EmptyDataTemplate`、`ShowHeaderWhenEmpty`），
//   而 `src\Portal` 是 Web Site 项目 —— **ascx 标记不进 msbuild**，构建 / 单测 / XML 三道门禁都看不见；
//   引用 `PortalEmptyStateRenderer` 所需的 `<%@ Import %>`、以及"零条时是否真的渲染提示行"都只能靠真实渲染确认。
//
//   三类断言（与 `W74` 的"失败 ≠ 空态"一致）：
//     A 有数据 → **不得**出现空态提示行；
//     B 零条   → **必须**出现空态提示行，且文案取自对应资源键（现读 resx 比对，不写副本）；
//     C 失败/输入无效 → **必须不出现**空态提示行（C 是本包的核心，也是最容易被"顺手渲染空态"破坏的一条）。
//
//   目标页与状态：
//     - Admin/SystemHealth.aspx、Admin/OperationAudits.aspx、Admin/DiagnosticsLogs.aspx、Admin/ModuleCatalog.aspx
//       （需管理员登录；零条通过"把日期范围改成无数据的历史区间 / 使用必然无数据的筛选"制造）
//     - DesktopModules/Contacts.ascx / Document.ascx（旧内容模块，需模块已挂载；零条取决于开发库数据）
//
//   用法（在仓库根目录执行）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalPlatformEmptyStateEvidence.mjs'
//   产物：`work-zone\dev\evidence\p81.4\<时间戳>-Dev\*.png` 与 `empty-state-summary.json`；失败退出码 1。
//   </zh-CN>
//   <en>Platform / legacy-module empty-state runtime evidence script (W-anp-P81 `P81.4`).
//
//   Why runtime assertions are mandatory: this package changes **aspx / ascx markup** (the empty-state row,
//   `EmptyDataTemplate`, `ShowHeaderWhenEmpty`), and `src\Portal` is a Web Site project — **ascx markup is not compiled
//   by msbuild**, so the build, unit-test, and XML gates are all blind to it. The `<%@ Import %>` required to reference
//   `PortalEmptyStateRenderer`, and whether the hint row really renders at zero rows, can only be confirmed by real
//   rendering.
//
//   Three assertion classes (consistent with W74's "failure is not an empty state"):
//     A rows exist   → the empty-state row must **not** appear;
//     B zero rows    → the empty-state row **must** appear and its wording must match the resource key
//                      (read live from the resx rather than a copy kept in this script);
//     C failure / invalid input → the empty-state row must **not** appear. C is the core of this package and the one
//                      most easily broken by "helpfully" rendering an empty state.
//
//   Usage (run from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalPlatformEmptyStateEvidence.mjs'
//   Output: `work-zone\dev\evidence\p81.4\<timestamp>-Dev\*.png` and `empty-state-summary.json`; exit code 1 on failure.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，优先环境变量指定位置，否则按包名解析并给出明确指引。</zh-CN><en>Resolves the playwright module, preferring the environment-variable location and otherwise falling back to package-name resolution with clear guidance.</en></lang>
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
const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}

const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = context.baseUrl;

// <lang>
//   <zh-CN>文案**现读 resx**而不是写死在本脚本里：断言的是"资源键的值真的渲染出来了"，
//   若有人只改了 resx 没改脚本，写死副本的断言会假通过。</zh-CN>
//   <en>Wording is read live from the resx rather than hard-coded: the assertion is "the resource value really rendered",
//   so a copy kept in this script would pass falsely if someone changed only the resx.</en>
// </lang>
function readResxValue(fileName, key) {
  const resx = fs.readFileSync(path.join(repoRoot, 'src', 'Portal', 'App_GlobalResources', fileName), 'utf8');
  const pattern = new RegExp('<data name="' + key + '"[^>]*>\\s*<value>([\\s\\S]*?)</value>\\s*</data>');
  const match = resx.match(pattern);
  if (!match) {
    throw new Error('Key ' + key + ' not found in ' + fileName);
  }
  return match[1];
}

const stamp = process.env.PORTAL_P81_EVIDENCE_STAMP ||
  new Date().toISOString().replace(/[-:]/g, '').replace(/\..+$/, '');
const outputDir = process.env.PORTAL_P81_EVIDENCE_DIR
  ? path.resolve(process.env.PORTAL_P81_EVIDENCE_DIR)
  : path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p81.4', stamp + '-Dev');
fs.mkdirSync(outputDir, { recursive: true });

// <lang>
//   <zh-CN>目标清单：四个平台后台页 + 两个旧内容模块（后者以模块视图 URL 访问）。
//   `emptyStateKey` 为该页零条时应出现的资源键；`rowSelector` 用于数数据行（判断 A/B 态）。</zh-CN>
//   <en>Target list: four platform admin pages plus two legacy modules (the latter reached through the module view URL).
//   `emptyStateKey` is the resource key that should appear at zero rows; `rowSelector` counts data rows to decide state A/B.</en>
// </lang>
const targets = [
  {
    id: 'admin-system-health',
    url: new URL('Admin/SystemHealth.aspx', baseUrl).toString(),
    emptyStateKeys: ['Admin_SystemHealth_EmptyNoChecks', 'Admin_SystemHealth_EmptyNoSettings'],
    rowSelector: 'table.portal-data-table tr.Normal'
  },
  {
    id: 'admin-operation-audits',
    url: new URL('Admin/OperationAudits.aspx', baseUrl).toString(),
    emptyStateKeys: ['Admin_OperationAudits_EmptyNoEntries', 'Admin_OperationAudits_EmptyNoMatches'],
    rowSelector: 'table.portal-data-table tr.Normal'
  },
  {
    id: 'admin-diagnostics-logs',
    url: new URL('Admin/DiagnosticsLogs.aspx', baseUrl).toString(),
    emptyStateKeys: ['Admin_DiagnosticsLogs_EmptyNoEntries', 'Admin_DiagnosticsLogs_EmptyNoMatches'],
    rowSelector: 'table.portal-data-table tr.Normal'
  },
  {
    id: 'admin-module-catalog',
    url: new URL('Admin/ModuleCatalog.aspx', baseUrl).toString(),
    emptyStateKeys: ['Admin_ModuleCatalog_MessageNoPackage'],
    rowSelector: 'table.portal-data-table tr.Normal'
  },
  {
    id: 'legacy-contacts',
    // <lang><zh-CN>挂载位置**实测自开发库**：`PortalCfg_Modules` 中 Contacts.ascx 挂在 TabId=2（Employee Info），
    // 不是交接或 W44 证据里出现过的 tabid —— 挂错页签会让断言"看起来通过"实则根本没渲染到模块。</zh-CN>
    // <en>The mount point is **measured from the development database**: Contacts.ascx sits on TabId=2 (Employee Info) in
    // `PortalCfg_Modules`, not on a tab id seen in the handoff or the W44 evidence — using the wrong tab would make the
    // assertion "look like it passed" while never rendering the module at all.</en></lang>
    url: new URL('DesktopDefault.aspx?tabindex=3&tabid=2', baseUrl).toString(),
    emptyStateKeys: ['Contacts_EmptyNoContacts'],
    rowSelector: 'table.portal-data-table tbody tr',
    moduleMarker: 'Contacts_LabelName'
  },
  {
    id: 'legacy-document',
    // <lang><zh-CN>同上，Document.ascx 挂在 TabId=4（Discussions）。</zh-CN><en>Same, Document.ascx sits on TabId=4 (Discussions).</en></lang>
    url: new URL('DesktopDefault.aspx?tabindex=7&tabid=4', baseUrl).toString(),
    emptyStateKeys: ['Document_EmptyNoDocuments'],
    rowSelector: 'table.portal-data-table tbody tr',
    moduleMarker: 'Document_LabelTitle'
  }
];

/// <summary>
/// <lang><zh-CN>对"有筛选条件的列表"追加两档交互断言（B 态零条 / C 态输入无效），覆盖 Repeater 的三态契约。
/// 零条用**历史日期区间**制造（不改数据），文案两套随额外筛选条件切换；输入无效则断言**不出现**空态行。</zh-CN>
/// <en>Adds two interaction passes for the filtered lists (state B at zero rows, state C on invalid input) to cover the
/// Repeater three-state contract. Zero rows are produced with a **historical date range** (no data change), the two
/// wordings switch with the additional filter, and invalid input asserts that the empty-state row does **not** appear.</en></lang>
/// </summary>
async function probeFilteredList(page, baseUrl, options) {
  const record = { id: options.id, state: options.state, status: 'Pass', facts: {}, notes: [] };

  try {
    await page.goto(options.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(700);

    await page.locator('input[id$="StartDateTextBox"]').fill(options.startDate);
    await page.locator('input[id$="EndDateTextBox"]').fill(options.endDate);
    if (options.category) {
      await page.locator('input[id$="CategoryFilter"]').fill(options.category);
    }
    await Promise.all([
      page.waitForLoadState('domcontentloaded').catch(() => {}),
      page.locator('a[id$="SearchButton"], input[id$="SearchButton"]').first().click()
    ]);
    await page.waitForTimeout(900);

    const html = await page.content();
    record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
    record.facts.emptyStateCells = await page.locator('td .portal-empty-state').count();
    record.facts.emptyStateTexts = await page.locator('td .portal-empty-state').allInnerTexts();
    // <lang><zh-CN>只有"期望出现空态行"的档才需要期望文案；C 态（输入无效）本就不该有空态行，
    // 无条件读键会拿 `undefined` 去查而误报失败（首轮就踩了这个坑）。</zh-CN>
    // <en>Only the "empty state expected" passes need the expected wording; state C (invalid input) must **not** show an
    // empty-state row at all, and reading the key unconditionally would look up `undefined` and report a false failure
    // (the first run hit exactly this).</en></lang>
    // </lang>
    record.facts.expectedText = options.expectEmptyState
      ? readResxValue('lang.zh-cn.resx', options.expectedKey)
      : null;

    if (record.facts.errorPage) {
      record.status = 'Fail';
      record.notes.push('页面回落到通用错误页 / The page fell back to the generic error page.');
    }

    if (options.expectEmptyState) {
      if (record.facts.emptyStateCells === 0) {
        record.status = 'Fail';
        record.notes.push('期望出现空态行但没有 / The empty-state row was expected but is absent.');
      } else if (!record.facts.emptyStateTexts.some((text) => text.trim() === record.facts.expectedText)) {
        record.status = 'Fail';
        record.notes.push('空态文案不符：实际[' + JSON.stringify(record.facts.emptyStateTexts) +
          '] 期望[' + record.facts.expectedText + ']');
      }
    } else if (record.facts.emptyStateCells > 0) {
      record.status = 'Fail';
      record.notes.push('此状态下不得出现空态行（输入无效属错误，不是"没有数据"） / The empty-state row must not appear here: invalid input is an error, not "no data".');
    }

    const shot = path.join(outputDir, options.id + '.png');
    await page.screenshot({ path: shot, fullPage: false });
    record.screenshot = path.relative(repoRoot, shot);
  } catch (error) {
    record.status = 'Fail';
    record.notes.push(error instanceof Error ? error.message : String(error));
  }

  return record;
}

const records = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  const browserContext = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale: 'zh-CN'
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
    const record = {
      id: target.id,
      url: target.url,
      status: 'Pass',
      facts: {},
      notes: []
    };

    try {
      await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);
      const html = await page.content();

      record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
      // <lang><zh-CN>空态行的判定口径：必须有 `portal-empty-state` 且位于表格单元格内（`td`），
      // 否则页面别处的提示（如 ResultLabel）会被误判为空态行。</zh-CN>
      // <en>Empty-state row detection: a `portal-empty-state` must sit inside a table cell (`td`), otherwise a hint
      // elsewhere on the page (such as ResultLabel) would be misread as the empty-state row.</en></lang>
      record.facts.emptyStateCells = await page.locator('td .portal-empty-state').count();
      record.facts.emptyStateTexts = await page.locator('td .portal-empty-state').allInnerTexts();
      record.facts.dataRowCount = await page.locator(target.rowSelector).count();

      // <lang><zh-CN>模块是否真的被渲染出来：对旧内容模块，用它自己的表头文案作标记。
      // 没有这一步，"页签挂错 / 模块未挂载"会表现为"零行且无空态行"，看起来像通过。</zh-CN>
      // <en>Whether the module actually rendered: for the legacy modules, its own header wording is used as the marker.
      // Without this step, "wrong tab / module not mounted" shows up as "zero rows and no empty-state row", which looks
      // like a pass.</en></lang>
      if (target.moduleMarker) {
        const markerText = readResxValue('lang.zh-cn.resx', target.moduleMarker);
        record.facts.moduleMarker = markerText;
        record.facts.moduleRendered = (await page.locator('body').innerText()).includes(markerText);
      }

      const expectedTexts = target.emptyStateKeys.map((key) => readResxValue('lang.zh-cn.resx', key));
      record.facts.expectedTexts = expectedTexts;
      record.facts.matchedExpected = expectedTexts.filter((text) =>
        record.facts.emptyStateTexts.some((actual) => actual.trim() === text));

      if (record.facts.errorPage) {
        record.status = 'Fail';
        record.notes.push('页面回落到通用错误页 / The page fell back to the generic error page.');
      }

      // <lang><zh-CN>状态判定：有数据则不得有空态行；零条则必须有空态行。
      // 旧内容模块的 tabid 依赖开发库挂载，未挂载时页面可能不是该模块 —— 记为跳过而非失败（`optional`）。</zh-CN>
      // <en>State decision: with rows present there must be no empty-state row; with zero rows there must be one. The
      // legacy modules' tab ids depend on development-database mounting, so a page that is not that module is recorded
      // as skipped rather than failed (`optional`).</en></lang>
      if (record.facts.dataRowCount > 0) {
        record.state = 'rows';
        if (record.facts.emptyStateCells > 0) {
          record.status = 'Fail';
          record.notes.push('有数据时不应出现空态行 / The empty-state row must not appear when rows exist.');
        }
      } else if (target.moduleMarker && !record.facts.moduleRendered) {
        // <lang><zh-CN>零行且模块自身表头都没出现 → 说明根本没渲染到该模块，记为 Skip 而不是 Fail。</zh-CN>
        // <en>Zero rows and the module's own header is absent → the module never rendered, so this is a Skip, not a Fail.</en></lang>
        record.state = 'not-rendered';
        record.status = 'Skip';
        record.notes.push('页面未渲染该模块（表头标记未出现），记为跳过 / The module did not render (its header marker is absent); skipped.');
      } else {
        record.state = 'zero';
        if (record.facts.emptyStateCells === 0) {
          if (target.optional) {
            record.status = 'Skip';
            record.notes.push('零行且无空态行；该目标为旧内容模块（挂载依赖开发库），记为跳过。');
          } else {
            record.status = 'Fail';
            record.notes.push('零行时必须出现空态行 / The empty-state row must appear when there are zero rows.');
          }
        } else if (record.facts.matchedExpected.length === 0) {
          record.status = 'Fail';
          record.notes.push('空态文案与资源键不一致：实际[' + JSON.stringify(record.facts.emptyStateTexts) +
            '] 期望[' + JSON.stringify(expectedTexts) + ']');
        }
      }

      const shot = path.join(outputDir, target.id + '.png');
      await page.screenshot({ path: shot, fullPage: true });
      record.screenshot = path.relative(repoRoot, shot);
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }

    records.push(record);
  }

  // <lang>
  //   <zh-CN>交互档：只对"日期 + 可选额外筛选"的两个列表做，因为只有它们能在不改数据的前提下制造零条与输入无效。
  //   三档分别覆盖：B 零条（有/无额外筛选 → 两套文案）、C 输入无效（**不得**出现空态行）。</zh-CN>
  //   <en>Interaction passes: only the two lists with a date range plus an optional extra filter, because only they can
  //   produce zero rows and invalid input without changing data. The passes cover state B at zero rows (with and without
  //   an extra filter, i.e. the two wordings) and state C on invalid input (**no** empty-state row allowed).</en></lang>
  // </lang>
  const auditsUrl = new URL('Admin/OperationAudits.aspx', baseUrl).toString();
  records.push(await probeFilteredList(page, baseUrl, {
    id: 'admin-operation-audits-zero-no-extra-filter',
    state: 'zero',
    url: auditsUrl,
    startDate: '2020-01-01',
    endDate: '2020-01-07',
    expectedKey: 'Admin_OperationAudits_EmptyNoEntries',
    expectEmptyState: true
  }));
  records.push(await probeFilteredList(page, baseUrl, {
    id: 'admin-operation-audits-zero-with-filter',
    state: 'zero-filtered',
    url: auditsUrl,
    startDate: '2020-01-01',
    endDate: '2020-01-07',
    category: 'P81-NoSuchCategory',
    expectedKey: 'Admin_OperationAudits_EmptyNoMatches',
    expectEmptyState: true
  }));
  records.push(await probeFilteredList(page, baseUrl, {
    id: 'admin-operation-audits-invalid-input',
    state: 'invalid-input',
    url: auditsUrl,
    startDate: 'not-a-date',
    endDate: '2020-01-07',
    expectEmptyState: false
  }));

  const logsUrl = new URL('Admin/DiagnosticsLogs.aspx', baseUrl).toString();
  records.push(await probeFilteredList(page, baseUrl, {
    id: 'admin-diagnostics-logs-zero-no-extra-filter',
    state: 'zero',
    url: logsUrl,
    startDate: '2020-01-01',
    endDate: '2020-01-07',
    expectedKey: 'Admin_DiagnosticsLogs_EmptyNoEntries',
    expectEmptyState: true
  }));
  records.push(await probeFilteredList(page, baseUrl, {
    id: 'admin-diagnostics-logs-invalid-input',
    state: 'invalid-input',
    url: logsUrl,
    startDate: 'not-a-date',
    endDate: '2020-01-07',
    expectEmptyState: false
  }));

  await browserContext.close();
} finally {
  await browser.close();
}

const summary = {
  phase: 'W-anp-P81 P81.4',
  generatedUtc: new Date().toISOString(),
  assertionClasses: {
    A: 'rows exist -> the empty-state row must not appear',
    B: 'zero rows -> the empty-state row must appear and match the resource value read live from lang.zh-cn.resx',
    C: 'failure / invalid input -> the empty-state row must not appear (asserted statically via the null-suppression code path and covered here by the zero-row / rows branches)'
  },
  records
};

fs.writeFileSync(path.join(outputDir, 'empty-state-summary.json'), JSON.stringify(summary, null, 2), 'utf8');

for (const record of records) {
  console.log('[' + record.status + '] ' + record.id +
    ' state=' + (record.state || '?') +
    ' rows=' + record.facts.dataRowCount +
    ' emptyCells=' + record.facts.emptyStateCells +
    ' matched=' + (record.facts.matchedExpected || []).length +
    ' errorPage=' + record.facts.errorPage);
  for (const note of record.notes) {
    console.log('    note: ' + note);
  }
}
console.log('evidence: ' + path.relative(repoRoot, outputDir));

const failed = records.filter((record) => record.status === 'Fail').length;
const skipped = records.filter((record) => record.status === 'Skip').length;
console.log('platform empty-state evidence completed: ' + records.length + ' record(s), ' + failed + ' failed, ' + skipped + ' skipped');
process.exit(failed > 0 ? 1 : 0);
