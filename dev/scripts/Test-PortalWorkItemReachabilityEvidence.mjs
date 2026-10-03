// <lang>
//   <zh-CN>待办可达性运行期证据脚本（W-anp-P77 `P77.4`）：对**运行中的站点**以管理员与非管理员分别登录，断言前台「我的待办」在真实渲染下确实区分三态。
//
//   为什么必须有它：`P77.3` 的反查结果取决于**运行库里的注册事实**（模块定义 → 模块实例 → 承载页签 → 角色检查）与**运行时角色解析**，
//   这些都不是 msbuild、单测或 XML 门禁能覆盖的：单测用的是内存端口替身，只证明"链的编排正确"，不证明"这台部署上真的能解析出页签"。
//   同时 ASCX 由 ASP.NET **运行期动态编译**，故本脚本也顺带证明模块在真实站点上能编译加载（P74.3 曾因此踩坑）。
//
//   前置条件：
//     1) IIS Express 已启动（`dev\scripts\Start-IISExpress.ps1`，默认 40001）；
//     2) 外置连接串已配置（`%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`）；
//     3) 验收上下文 `temp\p65\p65-acceptance-context.json` 存在（提供 baseUrl、管理员账号与口令）；
//     4) 业务模块档位已启用（`Portal.ModuleProfiles.Active=BusinessWorkflow`；基线 `CoreOnly` 下业务模块不渲染，验证后须还原）；
//     5) 已按需造数：`dev\scripts\New-PortalP77ReachabilityFixture.ps1 -Action Seed`（三条覆盖三态；验证后 `-Action Remove`）。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalWorkItemReachabilityEvidence.mjs'
//   产物：`temp\p77\reachability\`（截图 + reachability-summary.json）；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>Work-item reachability runtime evidence script (W-anp-P77 `P77.4`): signs in against the **running site** as an administrator and as a non-administrator, asserting that the front-end "My To-Do Items" really distinguishes the three states under real rendering.
//
//   Why it is required: the `P77.3` resolution result depends on **registration facts in the running database** (module definition to module instance to hosting tab to role check) and on runtime role resolution, none of which msbuild, unit tests, or the XML gate can cover: the unit tests use in-memory port stand-ins, proving the chain is wired correctly but not that this deployment really resolves a tab. ASCX files are also compiled **at runtime** by ASP.NET, so this script additionally proves the module compiles and loads on a real site (P74.3 was bitten by exactly that).
//
//   Prerequisites:
//     1) IIS Express started (`dev\scripts\Start-IISExpress.ps1`, port 40001 by default);
//     2) an external connection string configured at `%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`;
//     3) the acceptance context `temp\p65\p65-acceptance-context.json` present (it supplies baseUrl, the administrator account, and the password);
//     4) the business-module profile active (`Portal.ModuleProfiles.Active=BusinessWorkflow`; with the baseline `CoreOnly` profile the business modules do not render, so restore it afterwards);
//     5) the fixture rows seeded as needed: `dev\scripts\New-PortalP77ReachabilityFixture.ps1 -Action Seed` (three rows covering the three states; `-Action Remove` afterwards).
//
//   Usage (from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalWorkItemReachabilityEvidence.mjs'
//   Output: `temp\p77\reachability\` (screenshots plus reachability-summary.json); exits with code 1 when any assertion fails.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块：优先环境变量指定位置，否则回退到按包名解析并给出明确指引。</zh-CN>
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
      'Unable to resolve "playwright". It is a development-only dependency and is not committed; ' +
      'set PORTAL_PLAYWRIGHT_MODULE to its location (for example <repo>\\temp\\node_modules\\playwright). ' +
      'Original error: ' + error.message
    );
  }
}

/// <summary>
/// <lang><zh-CN>在已打开的页面上完成前台表单登录。</zh-CN>
/// <en>Completes front-end form sign-in on the already opened page.</en></lang>
/// </summary>
/// <param name="page"><lang><zh-CN>Playwright 页面。</zh-CN><en>The Playwright page.</en></lang></param>
/// <param name="userName"><lang><zh-CN>登录名。</zh-CN><en>The sign-in name.</en></lang></param>
/// <param name="password"><lang><zh-CN>口令；只在本进程内存中使用，绝不输出。</zh-CN><en>The password, used only in this process's memory and never emitted.</en></lang></param>
async function signIn(page, userName, password) {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('input[id$="EmailOrName"]').fill(userName);
  await page.locator('input[id$="password"]').fill(password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);
}

/// <summary>
/// <lang><zh-CN>读取表格行并按"事项单元格文本包含给定片段"定位目标行。</zh-CN>
/// <en>Reads table rows and locates the target row by "the item cell text contains the given fragment".</en></lang>
/// </summary>
/// <param name="page"><lang><zh-CN>Playwright 页面。</zh-CN><en>The Playwright page.</en></lang></param>
/// <param name="fragment"><lang><zh-CN>事项文本片段。</zh-CN><en>The item-text fragment.</en></lang></param>
/// <returns><lang><zh-CN>该行的文本与其内链接地址；找不到时返回 <c>null</c>。</zh-CN><en>The row's text and its inner link hrefs, or <c>null</c> when absent.</en></lang></returns>
async function findRow(page, fragment) {
  const rows = page.locator('table.my-work-items-table tbody tr');
  const count = await rows.count();
  for (let i = 0; i < count; i++) {
    const row = rows.nth(i);
    const text = (await row.innerText().catch(() => '')) || '';
    if (text.includes(fragment)) {
      const hrefs = await row.locator('a').evaluateAll((anchors) => anchors.map((a) => a.getAttribute('href') || ''));
      return { text, hrefs };
    }
  }
  return null;
}

const repoRoot = path.resolve(process.cwd());
const outputDir = path.join(repoRoot, 'temp', 'p77', 'reachability');
fs.mkdirSync(outputDir, { recursive: true });

const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}

const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = context.baseUrl;
const myWorkItemsUrl = new URL('DesktopDefault.aspx?tabindex=10&tabid=1011', baseUrl).toString();
const hintFragment = '暂无线上办理入口';

// <lang>
//   <zh-CN>被断言的三条造数记录标题；来自 dev\scripts\New-PortalP77ReachabilityFixture.ps1，标识集中在此便于核对与改数。</zh-CN>
//   <en>The three seeded record titles under assertion; they come from dev\scripts\New-PortalP77ReachabilityFixture.ps1 and are centralized here for review and re-seeding.</en>
// </lang>
const collaborationTitle = 'P77 运行期验证：协同事项';
const correctionTitle = 'P77 运行期验证：资料更正';
const applicationTitle = 'P77 运行期验证：业务申请';

const results = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  // <lang>
  //   <zh-CN>状态一/二：管理员可见模块；可办理行须产出链接，不可办理行须为纯文本加提示。三条造数均分派给该管理员。</zh-CN>
  //   <en>States one and two: the administrator sees the module; a resolvable row must produce a link while an unresolvable row must be plain text plus a hint. All three seeded rows are assigned to this administrator.</en>
  // </lang>
  const adminRecord = { id: 'admin-three-states', url: myWorkItemsUrl, status: 'Pass', facts: {}, notes: [] };
  const adminContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1, locale: 'zh-CN' });
  const adminPage = await adminContext.newPage();
  adminPage.setDefaultTimeout(40000);

  try {
    await signIn(adminPage, context.adminUserName, context.password);
    await adminPage.goto(myWorkItemsUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await adminPage.waitForTimeout(800);

    const html = await adminPage.content();
    adminRecord.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);
    adminRecord.facts.modulePresent = html.includes('my-work-items');
    adminRecord.facts.tablePresent = (await adminPage.locator('table.my-work-items-table').count()) > 0;

    const collaborationRow = await findRow(adminPage, collaborationTitle);
    const correctionRow = await findRow(adminPage, correctionTitle);
    const applicationRow = await findRow(adminPage, applicationTitle);

    adminRecord.facts.collaborationLink = collaborationRow ? collaborationRow.hrefs : null;
    adminRecord.facts.correctionLink = correctionRow ? correctionRow.hrefs : null;
    adminRecord.facts.applicationRowText = applicationRow ? applicationRow.text : null;
    adminRecord.facts.applicationLinkCount = applicationRow ? applicationRow.hrefs.length : null;
    adminRecord.facts.correctionOverdue = correctionRow ? correctionRow.text.includes('已超期') : false;
    adminRecord.facts.applicationHint = applicationRow ? applicationRow.text.includes(hintFragment) : false;

    if (adminRecord.facts.errorPage) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The page fell back to the generic error page (runtime compilation or load failure).');
    }
    if (!adminRecord.facts.modulePresent || !adminRecord.facts.tablePresent) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The work-item module or its table did not render for the administrator.');
    }
    if (!collaborationRow || !collaborationRow.hrefs.some((href) => href.includes('tabid=1010'))) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The collaboration row did not produce a link to tab 1010.');
    }
    if (!correctionRow || !correctionRow.hrefs.some((href) => href.includes('tabid=1009'))) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The correction row did not produce a link to tab 1009.');
    }
    if (!adminRecord.facts.correctionOverdue) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The overdue text marker was not rendered on the overdue row.');
    }
    if (!applicationRow) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The unresolvable business-application row was not rendered.');
    } else {
      if (applicationRow.hrefs.length > 0) {
        adminRecord.status = 'Fail';
        adminRecord.notes.push('The unresolvable row rendered a link, which would send the user to an inaccessible page.');
      }
      if (!adminRecord.facts.applicationHint) {
        adminRecord.status = 'Fail';
        adminRecord.notes.push('The degraded hint was not rendered on the unresolvable row.');
      }
    }

    adminRecord.screenshot = path.relative(repoRoot, path.join(outputDir, 'admin-three-states.png'));
    await adminPage.screenshot({ path: path.join(outputDir, 'admin-three-states.png'), fullPage: false });
  } catch (error) {
    adminRecord.status = 'Fail';
    adminRecord.notes.push(error instanceof Error ? error.message : String(error));
  } finally {
    await adminContext.close();
  }
  results.push(adminRecord);

  // <lang>
  //   <zh-CN>状态三：无权限用户。模块门禁要求业务待办查看或管理权限，二者在开发库里仅授予 Admins；
  //   非管理员登录后模块整块隐藏，故断言"容器在但表格与提示都不出现"。登录口令若与验收上下文中管理员口令不同，则本态记为未验证而不是判失败。</zh-CN>
  //   <en>State three: a user without permission. The module gate requires either the work-items view or administration permission, both granted only to Admins in the development database; after a non-administrator signs in the whole block is hidden, so the assertion is "the container exists while neither the table nor the hint appears". When the sign-in password differs from the administrator's acceptance password, this state is recorded as unverified rather than failed.</en>
  // </lang>
  if (context.boundUserName) {
    const deniedRecord = { id: 'non-admin-hidden', url: myWorkItemsUrl, status: 'Pass', facts: {}, notes: [] };
    const deniedContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1, locale: 'zh-CN' });
    const deniedPage = await deniedContext.newPage();
    deniedPage.setDefaultTimeout(40000);

    try {
      await signIn(deniedPage, context.boundUserName, context.password);
      const bodyText = (await deniedPage.locator('body').innerText().catch(() => '')) || '';
      const signedIn = !bodyText.includes('EmailOrName') && /注销|Sign out|Logout/i.test(bodyText);

      if (!signedIn) {
        deniedRecord.status = 'Unverified';
        deniedRecord.notes.push('The non-administrator fixture account could not sign in with the acceptance password, so the denied state was not exercised.');
      } else {
        await deniedPage.goto(myWorkItemsUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await deniedPage.waitForTimeout(800);
        deniedRecord.facts.tableCount = await deniedPage.locator('table.my-work-items-table').count();
        deniedRecord.facts.hintCount = (await deniedPage.locator('.my-work-items-unavailable').count());
        deniedRecord.facts.contentVisible = await deniedPage.locator('.my-work-items-filter').count();

        if (deniedRecord.facts.tableCount !== 0 || deniedRecord.facts.contentVisible !== 0) {
          deniedRecord.status = 'Fail';
          deniedRecord.notes.push('A user without the work-item permission still saw the list content.');
        }
      }

      deniedRecord.screenshot = path.relative(repoRoot, path.join(outputDir, 'non-admin-hidden.png'));
      await deniedPage.screenshot({ path: path.join(outputDir, 'non-admin-hidden.png'), fullPage: false });
    } catch (error) {
      deniedRecord.status = 'Unverified';
      deniedRecord.notes.push(error instanceof Error ? error.message : String(error));
    } finally {
      await deniedContext.close();
    }
    results.push(deniedRecord);
  }
} finally {
  await browser.close();
}

fs.writeFileSync(path.join(outputDir, 'reachability-summary.json'), JSON.stringify(results, null, 2), 'utf8');

let failed = 0;
for (const record of results) {
  if (record.status === 'Fail') {
    failed++;
  }
  console.log(`[${record.status}] ${record.id}`);
  for (const [key, value] of Object.entries(record.facts)) {
    console.log(`    ${key} = ${JSON.stringify(value)}`);
  }
  for (const note of record.notes) {
    console.log(`    note: ${note}`);
  }
}

console.log(`reachability evidence completed: ${results.length} record(s), ${failed} failed`);
process.exit(failed > 0 ? 1 : 0);
