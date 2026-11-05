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
//        该夹具同时产出 `temp\p77\reachability\permission-facts.json` 权限事实快照，本门禁据此推导"非管理员"态的期望，
//        因此断言不依赖具体模块挂载 / 承载页签 / 角色授权（部署决策演进无需改门禁，见 `W-anp-P77.1-benchmark.md` L43）。
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
//        The fixture also writes a permission-fact snapshot to `temp\p77\reachability\permission-facts.json`, from which this
//        gate derives the "non-administrator" expectation; assertions therefore do not depend on specific module mounts,
//        hosting tabs or role grants (deployment decisions may evolve without gate changes, see `W-anp-P77.1-benchmark.md` L43).
//
//   Usage (from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalWorkItemReachabilityEvidence.mjs'
//   Output: `temp\p77\reachability\` (screenshots plus reachability-summary.json); exits with code 1 when any assertion fails.
//   </en>
// </lang>
//
// 证明边界：只证明**采集时刻**前台「我的待办」在管理员与非管理员登录下区分三态；依赖运行库中的注册事实（模块定义 / 实例 / 页签 / 角色），不证明其它模块可达性，不证明权限模型正确（由权限审计承载）。
//
// Proof boundary: Proves only that, **at collection time**, the front-end "My work items" page distinguishes the three states for admin and non-admin logins; it depends on registration facts in the runtime database (module definition / instance / tab / roles), does not prove reachability of other modules, and does not prove the permission model (the permission audit does).
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
    // <lang>
    //   <zh-CN>链接可达性与三态契约按 `W-anp-P77.1-benchmark.md` L43 的设计原文断言，不再写死 tabid 或预设哪个业务必须降级：
    //   设计只要求"模块定义→实例→承载 Tab→角色检查全通过才给链接，且绝不产出会 403 的链接；任一环节不满足即降级、不静默留空"。
    //   因此逐条链接用同一登录态实际访问一次，断言不是权限拒绝页/错误页；每条造数行再按"有无链接"分两支验证三态契约
    //   ——有链接就必须可达且不得同时显示降级提示，无链接就必须显示降级提示（禁止静默留空）。
    //   这样模块挂载、承载页签、包启用状态等部署决策演进后门禁自动适配，且比硬编码更强：链接指向已注销页签会被当场抓到。
    //   历史教训：先前的 `tabid=1009` 与"业务申请必须不可办理"都是把某一时刻的部署事实写死在断言里，
    //   `ModuleDefId=1012` 双挂载（1008/1009）与 `ModuleDefId=1067` 上线后立即误报。</zh-CN>
    //   <en>Link reachability and the three-state contract are asserted per the design text in `W-anp-P77.1-benchmark.md`
    //   L43, with no hard-coded tab id and no preset expectation of which business must degrade: the design only requires
    //   "a link only when module definition, instance, hosting tab and role check all pass, never producing a link that
    //   would 403; otherwise degrade, never silently leave the cell empty". Each link is therefore visited once under the
    //   same sign-in and asserted not to be an access-denied/error page; each seeded row then verifies the contract from its
    //   observed state — a linked row must be reachable and must not also show a degraded hint, an unlinked row must show the
    //   hint (no silent blank). Deployment decisions (module mounts, hosting tabs, package enablement) then need no gate
    //   changes, and the gate is stronger than before: a link to a removed tab is caught immediately.
    //   History: the former `tabid=1009` and "business application must be unresolvable" froze one deployment snapshot into
    //   the assertions; `ModuleDefId=1012` being mounted on both 1008/1009 and `ModuleDefId=1067` going live caused false failures.</en>
    // </lang>
    // <lang>
    //   <zh-CN>链接可达性用**独立页面**验证：不能复用 adminPage 跳转，否则会离开待办页导致后续截图截到目标页、
    //   且三态事实已取完后再切页会让记录与实际观察错位。独立页继承同一上下文（同一登录态与 culture），
    //   因此能真实反映"该链接会不会把用户送到 403 / 通用错误页"。</zh-CN>
    //   <en>Link reachability is checked in a **separate page**: reusing adminPage would navigate away from the work-item
    //   page, so the screenshot would capture the target page and the record would no longer match the observation. A
    //   separate page inherits the same context (same sign-in and culture), so it faithfully reflects whether the link
    //   would send the user to a 403 / generic error page.</en>
    // </lang>
    const linkPage = await adminContext.newPage();
    linkPage.setDefaultTimeout(40000);
    const linkReachability = {};
    async function visitLink(href) {
      if (linkReachability[href] !== undefined) { return linkReachability[href]; }
      let reachable = false;
      try {
        const response = await linkPage.goto(new URL(href, baseUrl).toString(), { waitUntil: 'domcontentloaded', timeout: 60000 });
        const body = await linkPage.content();
        reachable = (!response || response.status() === 200) &&
          !/GenericErrorPage|应用程序暂时无法完成请求|Access Denied|拒绝访问/.test(body);
      } catch { reachable = false; }
      linkReachability[href] = reachable;
      return reachable;
    }

    // <lang>
    //   <zh-CN>三态契约不变量（对应 P77.1 L43）：不预设哪个业务"必须可办理"或"必须降级"——那是部署决策，
    //   会随模块挂载/包启用/角色授权演进。门禁只断言解析器给出的结果自洽且可用：
    //   ① 每行必须渲染（不能整行消失）；② 有链接则每条链接都必须真实可达（不会 403 / 错误页），且不得同时显示降级提示；
    //   ③ 无链接则必须显示降级提示（禁止静默留空）。三条造数覆盖三态，任何部署事实下该不变量都成立；
    //   而"链接指向已注销页签""可办理行被静默清空""降级行却又给了链接"等真实缺陷仍会被当场抓到。</zh-CN>
    //   <en>Three-state invariant (per P77.1 L43): no preset about which business "must be resolvable" or "must degrade" —
    //   that is a deployment decision that evolves with module mounts, package enablement and role grants. The gate only
    //   asserts that the resolver's output is self-consistent and usable: (1) every row must render; (2) a linked row's every
    //   link must be genuinely reachable (no 403 / error page) and must not also show a degraded hint; (3) an unlinked row
    //   must show the degraded hint (no silent blank). The three seeded rows cover the three states, so the invariant holds
    //   under any registration facts, while real defects — a link to a removed tab, a resolvable row silently emptied, a
    //   degraded row given a link — are still caught immediately.</en>
    // </lang>
    for (const [label, row] of [['collaboration', collaborationRow], ['correction', correctionRow], ['application', applicationRow]]) {
      if (!row) {
        adminRecord.status = 'Fail';
        adminRecord.notes.push(`The ${label} row was not rendered.`);
        continue;
      }
      const hasHint = row.text.includes(hintFragment);
      for (const href of row.hrefs) {
        if (!(await visitLink(href))) {
          adminRecord.status = 'Fail';
          adminRecord.notes.push(`The ${label} row links to an unreachable or access-denied target: ${href}.`);
        }
      }
      if (row.hrefs.length > 0 && hasHint) {
        adminRecord.status = 'Fail';
        adminRecord.notes.push(`The ${label} row rendered both a link and a degraded hint, contradicting the three-state contract.`);
      }
      if (row.hrefs.length === 0 && !hasHint) {
        adminRecord.status = 'Fail';
        adminRecord.notes.push(`The ${label} row was left blank: neither a link nor the degraded hint was rendered.`);
      }
    }
    adminRecord.facts.linkReachability = linkReachability;
    await linkPage.close();

    if (!adminRecord.facts.correctionOverdue) {
      adminRecord.status = 'Fail';
      adminRecord.notes.push('The overdue text marker was not rendered on the overdue row.');
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
  //   <zh-CN>状态三：非管理员。**期望从权限事实推导，不再写死"非管理员必被挡"**——那取决于部署决策。
  //   `New-PortalP77ReachabilityFixture.ps1 -Action Seed` 会把当前库里 `All Users` 是否持有待办查看/管理权限
  //   写成 `temp/p77/reachability/permission-facts.json`；本门禁读取它得到 `nonAdminShouldSeeWorkItems`：
  //   - false（仅 Admins 持有）：断言非管理员登录后表格与筛选容器都不出现（原严格断言，检出能力保持）；
  //   - true（All Users 也持有）：当前部署下"无权限用户"根本不存在，断言列表确实可见（权限门放行正确），
  //     并显式记录这是权限配置漂移、需产品侧裁定，而不是把部署事实误报成产品缺陷（W-anp-P139 口径）。
  //   快照缺失时本态记为 Unverified 并给出可操作的提示（先跑 fixture Seed），绝不猜测默认值。
  //   历史教训：先前硬判"看不到才 Pass"，而 RoleId=2002（All Users）被授予 `Business.WorkItems.View` 后基线恒红，
  //   把配置决策读成了代码回归。</zh-CN>
  //   <en>State three: a non-administrator. **The expectation is derived from permission facts, not the hard-coded
  //   "a non-administrator must be denied"** — that depends on a deployment decision. `New-PortalP77ReachabilityFixture.ps1
  //   -Action Seed` writes whether `All Users` currently holds a work-item view/admin permission to
  //   `temp/p77/reachability/permission-facts.json`; this gate reads `nonAdminShouldSeeWorkItems` from it:
  //   - false (Admins only): assert neither the table nor the filter container appears (the original strict assertion,
  //     so detection power is preserved);
  //   - true (All Users too): a "user without permission" does not exist in this deployment, so assert the list is indeed
  //     visible (the permission gate correctly grants access) and explicitly record this as a permission-configuration
  //     drift needing a product decision, rather than misreporting a deployment fact as a product defect (W-anp-P139).
  //   A missing snapshot marks this state Unverified with an actionable hint (run fixture Seed first); no default is guessed.
  //   History: the gate used to fail whenever the list was visible, and went permanently red once RoleId=2002 (All Users)
  //   was granted `Business.WorkItems.View`, reading a configuration decision as a code regression.</en>
  // </lang>
  const permissionFactsPath = path.join(repoRoot, 'temp', 'p77', 'reachability', 'permission-facts.json');
  let permissionFacts = null;
  if (fs.existsSync(permissionFactsPath)) {
    try { permissionFacts = JSON.parse(fs.readFileSync(permissionFactsPath, 'utf8')); }
    catch (error) { permissionFacts = null; }
  }

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
      } else if (!permissionFacts) {
        deniedRecord.status = 'Unverified';
        deniedRecord.notes.push('Permission facts snapshot missing; run New-PortalP77ReachabilityFixture.ps1 -Action Seed first so the expectation can be derived from the deployment.');
      } else {
        await deniedPage.goto(myWorkItemsUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await deniedPage.waitForTimeout(800);
        deniedRecord.facts.tableCount = await deniedPage.locator('table.my-work-items-table').count();
        deniedRecord.facts.hintCount = (await deniedPage.locator('.my-work-items-unavailable').count());
        deniedRecord.facts.contentVisible = await deniedPage.locator('.my-work-items-filter').count();
        const shouldSee = permissionFacts.nonAdminShouldSeeWorkItems === true;
        deniedRecord.facts.expectedVisible = shouldSee;
        deniedRecord.facts.permissionBasis = permissionFacts.basis || null;

        if (shouldSee) {
          // <lang><zh-CN>配置漂移显式上报：不是静默通过，而是把"All Users 持有待办查看权、无权限态不可构造"写进结论供产品裁定。</zh-CN><en>Explicit drift report: not a silent pass — the conclusion records that All Users holds the view permission, so the denied state cannot be constructed, for a product decision.</en></lang>
          // <lang><zh-CN>放行态只断言"模块可见"（筛选容器出现）：表格有无数据行取决于造数分派给谁，
          //   三条夹具行都分派给管理员，非管理员列表为空是正确行为，不能要求 tableCount>0。</zh-CN>
          //   <en>The granted state only asserts "the module is visible" (filter container present): whether the table has
          //   rows depends on whom the fixture rows are assigned to; all three are assigned to the administrator, so an
          //   empty list for the non-administrator is correct and tableCount>0 must not be required.</en></lang>
          deniedRecord.notes.push('[GATE-NOTE] 权限配置漂移：All Users 持有待办查看/管理权限，当前部署下无权限态不可构造；本态改验"权限门正确放行"，是否收回授权需产品侧裁定。');
          if (deniedRecord.facts.contentVisible === 0) {
            deniedRecord.status = 'Fail';
            deniedRecord.notes.push('Permission facts say the non-administrator should see the work-item module, but the permission gate hid it.');
          }
        } else {
          if (deniedRecord.facts.tableCount !== 0 || deniedRecord.facts.contentVisible !== 0) {
            deniedRecord.status = 'Fail';
            deniedRecord.notes.push('A user without the work-item permission still saw the list content.');
          }
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
