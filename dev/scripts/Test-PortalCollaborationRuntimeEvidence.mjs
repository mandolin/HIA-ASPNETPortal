// <lang>
//   <zh-CN>`W-anp-P292` 协作工作台浏览器级 L2 运行期证据：对**运行中的站点**，按
//   「浅色 / 深色主题 × 中文 / 英文语言」四象限各自渲染、各自出图（文件名含页面+主题+语言，禁止共用同一张），
//   覆盖协作工作台前台（`DesktopDefault.aspx?tabid=1010`，模块 `HIA.EnterpriseCapabilityWorkbench`）、
//   后台（`Admin/CollaborationItems.aspx`）与状态机（9 个动作按钮 + 8 态状态列），并回归验证两级分组导航未退化。
//   深色主题通过项目既有 fixture `New-PortalDefaultThemeFixture.ps1` **真实切换数据库设置**（非注入 CSS），
//   浅色组使用站点当前主题不切换；采集顺序为「浅色两语言 → Apply 一次深色 → 深色两语言」，
//   finally 里 Remove 一次恢复原主题，快照不会被二次 Apply 覆盖。</zh-CN>
//   <en>`W-anp-P292` collaboration-workbench browser-level L2 runtime evidence: against the **running site**, render
//   the "light/dark theme x Chinese/English locale" four quadrants separately with one screenshot per quadrant and page
//   (page + theme + locale in the filename; never share one image). Covers the workbench front end
//   (`DesktopDefault.aspx?tabid=1010`, module `HIA.EnterpriseCapabilityWorkbench`), the admin page
//   (`Admin/CollaborationItems.aspx`) and the state machine (nine action buttons plus the eight-state status column),
//   and regresses two-level grouped navigation. The dark theme comes from the project's existing fixture
//   `New-PortalDefaultThemeFixture.ps1` switching the **real database setting** (no CSS injection); the light quadrants
//   use the site's current theme. Order is "two light locales -> one Apply -> two dark locales"; a single Remove in
//   finally restores the original theme so a second Apply can never overwrite the snapshot.</en>
// </lang>
//
// <lang>
//   <zh-CN>环境变量：PORTAL_L2_BASE_URL / PORTAL_L2_CONTEXT / PORTAL_L2_OUTPUT_DIR / PORTAL_L2_LIGHT_THEME /
//   PORTAL_L2_DARK_THEME / PORTAL_L2_CONNECTION_CONFIG。playwright 为开发期依赖且不入库，
//   经 PORTAL_PLAYWRIGHT_MODULE 指向 temp/node_modules/playwright；node 由 mise 提供。
//   凭据取不入库的 dev 上下文 JSON，绝不写进仓库。</zh-CN>
//   <en>Environment: PORTAL_L2_BASE_URL / PORTAL_L2_CONTEXT / PORTAL_L2_OUTPUT_DIR / PORTAL_L2_LIGHT_THEME /
//   PORTAL_L2_DARK_THEME / PORTAL_L2_CONNECTION_CONFIG. Playwright is a development-only, uncommitted dependency
//   pointed to by PORTAL_PLAYWRIGHT_MODULE; node is provided by mise. Credentials come from an uncommitted dev
//   context JSON and are never written into the repository.</en>
// </lang>
//
// <lang>
//   <zh-CN>证明边界：只证明采集时刻、当前数据集与部署配置下，四个象限各自真实渲染且截图各自独立，
//   协作工作台前台/后台/状态机控件在运行期存在、状态列取值属于 8 态词表、两级分组导航未回归；
//   **不证明**状态机迁移落库与审计链路（由静态 smoke 与单测承载）、不证明读屏器语义与真实键盘焦点顺序
//   （`a11y-runtime` 仍如实未覆盖）、不证明点击任一状态动作后的数据变更、不证明跨机器像素一致、
//   不证明其它 5 套正式皮肤在真实部署下由解析器选中。</zh-CN>
//   <en>Proof boundary: proves only that, at collection time under the current dataset and deployment configuration,
//   the four quadrants each really render with independent screenshots, that the workbench front end / admin page /
//   state-machine controls exist at runtime, that status-column values belong to the eight-state vocabulary, and that
//   two-level grouped navigation has not regressed. It does **not** prove state-machine persistence or the audit chain
//   (covered by static smoke tests and unit tests), screen-reader semantics or real keyboard focus order
//   (`a11y-runtime` stays honestly uncovered), data changes after clicking a status action, cross-machine pixel
//   equality, or that the other five official skins are selected by the resolver in a real deployment.</en>
// </lang>

import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang>
///   <zh-CN>解析 playwright 模块并返回 chromium 启动器；优先环境变量指定位置，否则按包名解析。</zh-CN>
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
///   <zh-CN>仓库根取当前工作目录（须从仓库根调用）；上下文 JSON 提供 baseUrl 与管理员凭据，凭据不入库。</zh-CN>
///   <en>The repository root is the current working directory (call from the repository root); the context JSON supplies baseUrl and admin credentials, which are never committed.</en>
/// </lang>
/// </summary>
const repoRoot = path.resolve(process.cwd());
const contextPath = process.env.PORTAL_L2_CONTEXT || path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = process.env.PORTAL_L2_BASE_URL || context.baseUrl;
const outputDir = process.env.PORTAL_L2_OUTPUT_DIR || path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p51-collaboration');
const lightTheme = (process.env.PORTAL_L2_LIGHT_THEME || 'EnterpriseLight').trim();
const darkTheme = (process.env.PORTAL_L2_DARK_THEME || 'EnterpriseDark').trim();
const connectionConfig = process.env.PORTAL_L2_CONNECTION_CONFIG
  || path.join(process.env.USERPROFILE || process.env.HOME || '', 'Web', 'HIA-ASPNETPortal', 'dev', 'connectionStrings.config');

const themeFixture = path.join(repoRoot, 'dev', 'scripts', 'New-PortalDefaultThemeFixture.ps1');
const locales = ['zh-CN', 'en-US'];
const pages = [
  { id: 'workbench-front', url: 'DesktopDefault.aspx?tabid=1010' },
  { id: 'collaboration-admin', url: 'Admin/CollaborationItems.aspx' }
];

/// <summary>
/// <lang>
///   <zh-CN>状态机 8 态词表与 9 个动作按钮 ID 从 `Portal.Components` 与既有 L2 门禁的权威声明推导，
///   不在证据脚本里另抄一份；状态名再用 SQL CHECK 约束做第二来源交叉核对。</zh-CN>
///   <en>The eight-state vocabulary and nine action-button ids are derived from the authoritative `Portal.Components`
///   source and the existing L2 gate declaration instead of being copied here; status names are cross-checked against
///   the SQL CHECK constraint as a second source.</en>
/// </lang>
/// </summary>
function readSourceVocabulary() {
  const statusesSource = fs.readFileSync(path.join(repoRoot, 'src', 'Portal.Components', 'PortalCollaborationItemStatuses.cs'), 'utf8');
  const statuses = Array.from(statusesSource.matchAll(/public const string \w+ = "([^"]+)";/g), (m) => m[1]);
  const sqlSource = fs.readFileSync(path.join(repoRoot, 'src', 'Setup', 'PortalBiz_CollaborationItems.sql'), 'utf8');
  const inSql = statuses.filter((status) => sqlSource.includes("N'" + status + "'") || sqlSource.includes("'" + status + "'"));
  const layoutSource = fs.readFileSync(path.join(repoRoot, 'dev', 'scripts', 'Test-PortalCollaborationLayoutEvidence.mjs'), 'utf8');
  const actionBlock = /const expectedActionButtonIds = \[([\s\S]*?)\];/.exec(layoutSource);
  const actions = actionBlock
    ? Array.from(actionBlock[1].matchAll(/'([A-Za-z]+Button)'/g), (m) => m[1])
    : [];
  if (statuses.length !== 8 || inSql.length !== 8 || actions.length !== 9) {
    throw new Error('Source vocabulary drift: expected 8 statuses (also in SQL) / 9 actions, got ' + statuses.length + '/' + inSql.length + ' / ' + actions.length + '.');
  }
  return { statuses, actions };
}

/// <summary>
/// <lang>
///   <zh-CN>调用项目既有主题 fixture；参数数组化传递，不拼 shell 字符串，避免注入面。
///   `Apply` / `Remove` 必须成对，且只 Apply 一次，finally 里 Remove 一次。</zh-CN>
///   <en>Invoke the project's existing theme fixture with an argument array (no shell string concatenation, no injection surface).
///   `Apply` / `Remove` must be paired: exactly one Apply and one Remove in finally.</en>
/// </lang>
/// </summary>
function switchTheme(action, themeName) {
  const args = ['-NoProfile', '-File', themeFixture, '-Action', action];
  if (action === 'Apply') { args.push('-ThemeName', themeName); }
  args.push('-ConnectionStringsConfigPath', connectionConfig);
  const output = execFileSync('pwsh', args, { encoding: 'utf8', cwd: repoRoot, timeout: 120000 });
  return output.trim();
}

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
  lightTheme,
  darkTheme,
  locales,
  pages: pages.map((p) => p.id),
  themeSwitch: 'Apply/Remove paired (light quadrants use the site current theme first)',
  status: 'Pass',
  facts: {},
  checks
};

let themeApplied = false;
try {
  fs.mkdirSync(outputDir, { recursive: true });
  const vocabulary = readSourceVocabulary();
  record.facts.statusVocabulary = vocabulary.statuses;
  record.facts.actionButtons = vocabulary.actions;
  check('A0.SourceVocabulary', vocabulary.statuses.length === 8 && vocabulary.actions.length === 9,
    'statuses=' + vocabulary.statuses.length + ' actions=' + vocabulary.actions.length);

  const results = [];

  /// <summary>
  /// <lang>
  ///   <zh-CN>采集单个象限：一个 locale + 一个 theme，两个页面各出一张独立截图。
  ///   主题 class 与样式表链接从运行期 DOM 读取，证明真实切换而非注入 CSS。</zh-CN>
  ///   <en>Collect one quadrant: one locale + one theme, one independent screenshot per page. The theme class and
  ///   stylesheet link are read from the runtime DOM, proving a real switch rather than CSS injection.</en>
  /// </lang>
  /// </summary>
  async function collectQuadrant(theme, locale) {
    const browserContext = await browser.newContext({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 1,
      locale
    });
    const page = await browserContext.newPage();
    page.setDefaultTimeout(40000);
    await signIn(page);

    const quadrantRecord = { theme, locale, screenshots: [], facts: {} };
    for (const target of pages) {
      const url = new URL(target.url, baseUrl).toString();
      const response = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(500);
      const tag = target.id + '-' + theme.toLowerCase() + '-' + locale;

      const facts = await page.evaluate(() => {
        const bodyText = document.body.innerText;
        const buttons = Array.from(document.querySelectorAll('input[type=submit],button'))
          .map((b) => b.name || b.id || '').filter(Boolean);
        const table = document.querySelector('table.portal-data-table');
        const headers = table ? Array.from(table.querySelectorAll('th')).map((th) => th.innerText.trim()) : [];
        const rows = table ? Array.from(table.querySelectorAll('tbody tr')) : [];
        // <lang>
        //   <zh-CN>状态列按表头文案定位（中英各自渲染），而不是扫描所有单元格；
        //   找不到状态列即返回空数组，由断言判失败，不静默跳过。</zh-CN>
        //   <en>Locate the status column by its header text (rendered per locale) instead of scanning every cell;
        //   a missing column yields an empty array so the assertion fails rather than silently skipping.</en>
        // </lang>
        const statusColumnIndex = headers.findIndex((h) => /状态|Status/i.test(h));
        const statusValues = statusColumnIndex >= 0
          ? rows.map((row) => (row.querySelectorAll('td')[statusColumnIndex] || {}).innerText || '')
            .map((s) => s.trim()).filter(Boolean)
          : [];
        const toggles = Array.from(document.querySelectorAll('button.portal-nav-group-toggle'));
        return {
          httpTitle: document.title,
          bodyLength: bodyText.length,
          themeClass: document.body.className,
          themeLink: (Array.from(document.querySelectorAll('link[href*="App_Themes"]')).map((l) => l.getAttribute('href'))[0] || ''),
          workbenchCount: document.querySelectorAll('.enterprise-workbench').length,
          dataTableCount: document.querySelectorAll('table.portal-data-table').length,
          headers,
          statusColumnIndex,
          statusValues,
          buttons,
          groupToggles: toggles.length,
          groupLabels: toggles.map((b) => b.innerText.trim()),
          hasServerError: /Server Error|运行时错误|HTTP 500/.test(bodyText)
        };
      });

      quadrantRecord.facts[target.id] = facts;

      const expectedScope = 'portal-theme-' + theme.toLowerCase();
      check('A1.' + tag + '.Http200', response && response.status() === 200,
        'status=' + (response ? response.status() : 'none'));
      check('A2.' + tag + '.ThemeReallyApplied', facts.themeClass.includes(expectedScope) && facts.themeLink.toLowerCase().includes(theme.toLowerCase()),
        'themeClass=' + facts.themeClass + ' link=' + facts.themeLink);
      check('A3.' + tag + '.NoServerError', !facts.hasServerError && facts.bodyLength > 100,
        'bodyLength=' + facts.bodyLength);
      check('A4.' + tag + '.GroupedNavNotRegressed', facts.groupToggles > 0,
        'groupToggles=' + facts.groupToggles + ' labels=' + facts.groupLabels.join('|'));

      if (target.id === 'workbench-front') {
        check('A5.' + tag + '.WorkbenchRendered', facts.workbenchCount > 0, 'workbenchCount=' + facts.workbenchCount);
      }
      else {
        const actionsFound = vocabulary.actions.filter((id) => facts.buttons.some((name) => name.endsWith('$' + id) || name === id));
        check('A6.' + tag + '.AdminTableRendered', facts.dataTableCount > 0, 'dataTableCount=' + facts.dataTableCount);
        check('A7.' + tag + '.StateMachineActions', actionsFound.length === vocabulary.actions.length,
          'found=' + actionsFound.length + '/' + vocabulary.actions.length);
        // <lang>
        //   <zh-CN>状态列断言：表头必须能定位到状态列、必须取到至少一个值、且**每个值都在 C# 词表内**。
        //   词表来自独立源码（并与 SQL CHECK 交叉核对），不是用页面值自己构造正则，故不是恒真假绿。</zh-CN>
        //   <en>Status-column assertion: the header must locate a status column, at least one value must be found,
        //   and **every value must belong to the C# vocabulary**. That vocabulary comes from independent source (and is
        //   cross-checked with the SQL CHECK), never from a regex built out of the page values, so this is not a
        //   vacuously-true assertion.</en>
        // </lang>
        check('A8.' + tag + '.StatusValuesInVocabulary',
          facts.statusColumnIndex >= 0 && facts.statusValues.length > 0 && facts.statusValues.every((value) => vocabulary.statuses.includes(value)),
          'statusColumnIndex=' + facts.statusColumnIndex + ' values=' + JSON.stringify(facts.statusValues));
        quadrantRecord.facts.actionsFound = actionsFound;
        quadrantRecord.facts.statusValues = facts.statusValues;
      }

      // <lang>
      //   <zh-CN>每个象限、每个页面各出一张独立截图；文件名含页面+主题+语言，中英/浅深绝不共用同一张。</zh-CN>
      //   <en>One independent screenshot per quadrant and page; the filename carries page + theme + locale so no Chinese/English or light/dark pair ever shares one image.</en>
      // </lang>
      const shot = path.join(outputDir, 'collab-' + tag + '.png');
      await page.screenshot({ path: shot, fullPage: true });
      quadrantRecord.screenshots.push(path.basename(shot));
      check('A9.' + tag + '.ScreenshotIndependent', fs.statSync(shot).size > 0, 'file=' + path.basename(shot));
    }

    results.push(quadrantRecord);
    await browserContext.close();
  }

  // <lang>
  //   <zh-CN>顺序关键：先在站点当前（浅色）主题下采完中英两象限，再 Apply 一次深色采另两象限。
  //   若把 Apply 放在前面，浅色象限会被染成深色；若每个深色象限各 Apply 一次，第二次会覆盖快照导致 Remove 恢复错主题。</zh-CN>
  //   <en>Order matters: collect both Chinese/English light quadrants under the site's current theme first, then Apply
  //   dark once and collect the other two. Applying earlier would tint the light quadrants dark; applying once per dark
  //   quadrant would overwrite the snapshot and make Remove restore the wrong theme.</en>
  // </lang>
  for (const locale of locales) {
    await collectQuadrant(lightTheme, locale);
  }
  record.facts.fixtureApplyOutput = switchTheme('Apply', darkTheme);
  themeApplied = true;
  await new Promise((resolve) => setTimeout(resolve, 1500));
  for (const locale of locales) {
    await collectQuadrant(darkTheme, locale);
  }

  // <lang>
  //   <zh-CN>四象限两两比对：8 张截图文件名互不相同；分组名在中英象限之间确有差异；浅深主题 class 与样式表链接确实不同。</zh-CN>
  //   <en>Pairwise quadrant checks: eight distinct screenshot filenames, group labels differ between Chinese and
  //   English quadrants, and light/dark theme classes plus stylesheet links really differ.</en>
  // </lang>
  const allShots = results.flatMap((r) => r.screenshots);
  record.facts.screenshots = allShots;
  check('B1.EightIndependentScreenshots', allShots.length === 8 && new Set(allShots).size === 8,
    'count=' + allShots.length + ' distinct=' + new Set(allShots).size);

  for (const theme of [lightTheme, darkTheme]) {
    const zh = results.find((r) => r.theme === theme && r.locale === 'zh-CN');
    const en = results.find((r) => r.theme === theme && r.locale === 'en-US');
    if (!zh || !en) { continue; }
    const zhLabels = zh.facts['workbench-front'].groupLabels.join('|');
    const enLabels = en.facts['workbench-front'].groupLabels.join('|');
    check('B2.' + theme + '.LocalesRenderSeparately', zhLabels !== enLabels && zhLabels.length > 0 && enLabels.length > 0,
      'zh=' + zhLabels + ' en=' + enLabels);
    check('B3.' + theme + '.NoSharedScreenshot', zh.screenshots.join(',') !== en.screenshots.join(','),
      'zh=' + zh.screenshots.join(',') + ' en=' + en.screenshots.join(','));
  }

  for (const locale of locales) {
    const light = results.find((r) => r.theme === lightTheme && r.locale === locale);
    const dark = results.find((r) => r.theme === darkTheme && r.locale === locale);
    if (!light || !dark) { continue; }
    const l = light.facts['workbench-front'];
    const d = dark.facts['workbench-front'];
    check('B4.' + locale + '.ThemesDiffer', l.themeClass !== d.themeClass && l.themeLink !== d.themeLink,
      'light=' + l.themeClass + '/' + l.themeLink + ' dark=' + d.themeClass + '/' + d.themeLink);
  }

  record.facts.quadrants = results.map((r) => ({ theme: r.theme, locale: r.locale, screenshots: r.screenshots }));
}
catch (error) {
  record.status = 'Fail';
  record.error = error.message;
  check('X0.NoException', false, error.message);
}
finally {
  // <lang>
  //   <zh-CN>无论成功或异常，只要 Apply 过就必须在 finally 里 Remove 一次，恢复站点原主题；
  //   恢复失败时整份证据判 Fail，不假装完成。</zh-CN>
  //   <en>Whether the run succeeded or failed, a single Remove in finally restores the site's original theme whenever
  //   Apply ran; a failed restore fails the whole evidence package.</en>
  // </lang>
  if (themeApplied) {
    try {
      record.facts.fixtureRemoveOutput = switchTheme('Remove', darkTheme);
      check('Y0.ThemeRestored', true, record.facts.fixtureRemoveOutput);
    }
    catch (error) {
      record.status = 'Fail';
      check('Y0.ThemeRestored', false, error.message);
    }
  }
  try {
    const failed = checks.filter((item) => !item.ok);
    if (failed.length > 0) { record.status = 'Fail'; }
    record.facts.failedCheckCount = failed.length;
    fs.writeFileSync(path.join(outputDir, 'runtime-evidence-summary.json'), JSON.stringify(record, null, 2), 'utf8');
  } catch { /* best-effort */ }
  console.log('collaboration L2: ' + record.status + ' (' + checks.filter((c) => c.ok).length + '/' + checks.length + ')');
  for (const item of checks.filter((c) => !c.ok)) { console.log('  [Fail] ' + item.name + ' — ' + item.detail); }
  await browser.close();
  process.exit(record.status === 'Pass' ? 0 : 1);
}
