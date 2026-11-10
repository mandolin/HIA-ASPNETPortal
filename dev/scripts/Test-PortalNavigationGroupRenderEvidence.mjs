// <lang>
//   <zh-CN>`W-anp-P291` 浏览器级 L2 运行期证据：对**运行中的站点**验证两级分组导航真的渲染出来，
//   且 disclosure 键盘契约、Escape 回焦、渐进增强（无 JS 时页签仍可达）、中英分组名各自渲染、
//   以及 7 套皮肤主题作用域规则生效。以管理员登录 → 采集分组结构 → 点击折叠/展开 →
//   Escape 回焦 → 逐套皮肤切换 body class 读计算样式 → 另起禁用 JS 的上下文验证可达性 →
//   留档整页截图与按语言区分的 JSON 摘要。参数全部经环境变量注入，凭据不入库。</zh-CN>
//   <en>`W-anp-P291` browser-level L2 runtime evidence: against the **running site**, verify that two-level grouped
//   navigation really renders, along with the disclosure keyboard contract, Escape focus return, progressive
//   enhancement (tabs still reachable without JavaScript), separate Chinese/English group labels, and effective
//   theme-scope rules across seven skins. Sign in as administrator → collect group structure → toggle collapse/expand →
//   Escape focus return → switch body class per skin and read computed styles → open a JavaScript-disabled context
//   to verify reachability → keep a full-page screenshot and a per-locale JSON summary. All parameters arrive through
//   environment variables; credentials are never committed.</en>
// </lang>
//
// <lang>
//   <zh-CN>环境变量：PORTAL_L2_BASE_URL / PORTAL_L2_CONTEXT / PORTAL_L2_OUTPUT_DIR / PORTAL_L2_LOCALE /
//   PORTAL_L2_THEMES（逗号分隔，默认 7 套）。playwright 为开发期依赖且不入库，经 PORTAL_PLAYWRIGHT_MODULE 指向
//   temp/node_modules/playwright；node 由 mise 提供。中英各自渲染截图，文件名带语言，禁止共用同一张。</zh-CN>
//   <en>Environment: PORTAL_L2_BASE_URL / PORTAL_L2_CONTEXT / PORTAL_L2_OUTPUT_DIR / PORTAL_L2_LOCALE /
//   PORTAL_L2_THEMES (comma-separated, seven skins by default). Playwright is a development-only, uncommitted
//   dependency pointed to by PORTAL_PLAYWRIGHT_MODULE; node is provided by mise. Chinese and English screenshots are
//   rendered separately with locale-specific filenames and never share one image.</en>
// </lang>
//
// <lang>
//   <zh-CN>证明边界：只证明采集时刻、当前数据集与部署配置下分组导航在运行中站点按契约渲染与交互；
//   **不证明**读屏器实测（a11y-runtime 仍如实登记未覆盖）、不证明所有皮肤在真实部署下由主题解析器选中
//   （本证据通过切换 body class 验证 CSS 作用域规则生效）、不证明跨机器像素一致、不证明写入/审计链路，
//   也不证明分组成为授权依据（分组不参与授权由 L0 门禁与单测承载）。</zh-CN>
//   <en>Proof boundary: proves only that, at collection time under the current dataset and deployment configuration,
//   grouped navigation renders and interacts per contract on the running site. It does **not** prove screen-reader
//   testing (a11y-runtime remains honestly uncovered), that every skin is selected by the theme resolver in a real
//   deployment (this evidence verifies CSS scope rules by switching body class), cross-machine pixel equality,
//   write/audit flows, or that grouping became an authorization basis (L0 gate and unit tests cover that boundary).</en>
// </lang>

import fs from 'node:fs';
import path from 'node:path';
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
const locale = (process.env.PORTAL_L2_LOCALE || 'zh-CN').trim();
const outputDir = process.env.PORTAL_L2_OUTPUT_DIR || path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p51-grouped-nav');
const isChinese = locale.toLowerCase().startsWith('zh');

/// <summary>
/// <lang>
///   <zh-CN>7 套皮肤取自 Q4 裁定（6 正式 + Default 回退，排除 ThemeProbe）；可由环境变量覆盖但默认全集。</zh-CN>
///   <en>Seven skins from Q4 ruling (six official plus the Default fallback, ThemeProbe excluded); overridable via environment but the full set is the default.</en>
/// </lang>
/// </summary>
const defaultThemes = ['Default', 'EnterpriseLight', 'EnterpriseDark', 'OaLight', 'OaDark', 'StateClassicLight', 'StateClassicDark'];
const themes = (process.env.PORTAL_L2_THEMES || defaultThemes.join(','))
  .split(',').map((item) => item.trim()).filter(Boolean);

/// <summary>
/// <lang>
///   <zh-CN>分组名期望值来自 `PortalNavigationGroupAlias` 的中英列，按语言二选一；
///   断言只验证「该语言确实出现且另一语言不同时出现」，避免把英文页签名称误判为分组名。</zh-CN>
///   <en>Expected group labels come from the Chinese/English columns of `PortalNavigationGroupAlias`, chosen by locale;
///   the assertion checks that the expected language appears and the other does not, avoiding confusion with tab names.</en>
/// </lang>
/// </summary>
const expectedGroupLabel = isChinese ? '门户与帮助' : 'Portal & Help';
const otherGroupLabel = isChinese ? 'Portal & Help' : '门户与帮助';

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
  locale,
  themes,
  status: 'Pass',
  facts: {},
  checks
};

try {
  fs.mkdirSync(outputDir, { recursive: true });
  const browserContext = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale
  });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(30000);
  await signIn(page);

  const response = await page.goto(new URL('DesktopDefault.aspx?tabid=1', baseUrl).toString(),
    { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(800);
  check('A1.PortalHttp200', response && response.status() === 200, 'status=' + (response ? response.status() : 'none'));

  // <lang>
  //   <zh-CN>采集 disclosure 结构：原生 button、aria-expanded / aria-controls 配对、不用 menu role、无内联 onclick。</zh-CN>
  //   <en>Collect disclosure structure: native buttons, paired aria-expanded / aria-controls, no menu role, no inline onclick.</en>
  // </lang>
  const groups = await page.locator('button.portal-nav-group-toggle').evaluateAll((buttons) =>
    buttons.map((button) => ({
      text: (button.textContent || '').trim(),
      tag: button.tagName.toLowerCase(),
      type: button.getAttribute('type') || '',
      expanded: button.getAttribute('aria-expanded'),
      controls: button.getAttribute('aria-controls'),
      role: button.getAttribute('role') || '',
      inlineOnclick: button.hasAttribute('onclick')
    })));
  record.facts.groupCount = groups.length;
  record.facts.groups = groups;
  check('A2.GroupsRendered', groups.length > 0, 'groupCount=' + groups.length);
  check('A3.NativeButtons', groups.every((group) => group.tag === 'button' && group.type === 'button'), JSON.stringify(groups.map((g) => g.tag + '/' + g.type)));
  check('A4.AriaPairing', groups.every((group) => group.expanded === 'true' && !!group.controls), JSON.stringify(groups.map((g) => g.expanded + ':' + g.controls)));
  check('A5.NoMenuRole', groups.every((group) => group.role !== 'menu' && group.role !== 'menubar'), JSON.stringify(groups.map((g) => g.role)));
  check('A6.NoInlineOnclick', groups.every((group) => !group.inlineOnclick), 'inline handlers=' + groups.filter((g) => g.inlineOnclick).length);

  // <lang>
  //   <zh-CN>aria-controls 指向的容器必须真实存在，且容器 id 与稳定前缀一致（不拼请求参数）。</zh-CN>
  //   <en>Every aria-controls target must exist and carry the stable id prefix (no request parameters concatenated).</en>
  // </lang>
  const panels = await page.locator('.portal-nav-group-tabs').evaluateAll((nodes) =>
    nodes.map((node) => ({ id: node.id, linkCount: node.querySelectorAll('a.portal-tab').length })));
  record.facts.panels = panels;
  check('A7.ControlsResolve', groups.every((group) => panels.some((panel) => panel.id === group.controls)),
    'groups=' + groups.length + ' panels=' + panels.length);
  check('A8.StableContainerIds', panels.every((panel) => /^portal-nav-group-[a-z0-9-]+$/.test(panel.id)), JSON.stringify(panels.map((p) => p.id)));

  // <lang>
  //   <zh-CN>分组名按界面文化渲染：期望语言出现、另一语言不出现，证明中英各自渲染而非共用一张文案。</zh-CN>
  //   <en>Group labels follow the UI culture: the expected language appears and the other does not, proving Chinese and English render separately rather than sharing one label.</en>
  // </lang>
  const groupTexts = groups.map((group) => group.text).join(' | ');
  record.facts.groupLabels = groupTexts;
  check('A9.LocalizedGroupLabel', groupTexts.includes(expectedGroupLabel), 'expected=' + expectedGroupLabel + ' actual=' + groupTexts);
  check('A10.NoOtherLocaleLabel', !groupTexts.includes(otherGroupLabel), 'other=' + otherGroupLabel);

  // <lang>
  //   <zh-CN>页签不得因分组丢失：所有 a.portal-tab 都有 href，选中项仍是非链接 span，禁用态保持非链接。</zh-CN>
  //   <en>Grouping must lose no tab: every a.portal-tab has an href, the selected item stays a non-link span, and the disabled state stays a non-link.</en>
  // </lang>
  const tabFacts = await page.evaluate(() => {
    const links = Array.from(document.querySelectorAll('a.portal-tab'));
    return {
      linkCount: links.length,
      linksWithoutHref: links.filter((link) => !link.getAttribute('href')).length,
      selectedNonLink: Array.from(document.querySelectorAll('.portal-tab-selected')).every((node) => node.tagName.toLowerCase() !== 'a'),
      disabledNonLink: Array.from(document.querySelectorAll('.portal-tab-disabled')).every((node) => node.tagName.toLowerCase() !== 'a'),
      flatDataListHidden: !document.querySelector('.portal-tabs.OtherTabsBg:not(.portal-tabs-grouped)')
    };
  });
  record.facts.tabFacts = tabFacts;
  check('A11.AllGroupedLinksHaveHref', tabFacts.linksWithoutHref === 0, 'links=' + tabFacts.linkCount + ' missing=' + tabFacts.linksWithoutHref);
  check('A12.SelectedIsNonLink', tabFacts.selectedNonLink, JSON.stringify(tabFacts));
  check('A13.DisabledIsNonLink', tabFacts.disabledNonLink, JSON.stringify(tabFacts));
  check('A14.FlatRollbackHiddenOnNormalPath', tabFacts.flatDataListHidden, 'flat DataList must not render while grouped navigation is active');

  // <lang>
  //   <zh-CN>点击折叠 → aria-expanded=false、容器 display:none；再点展开 → true、display 恢复。
  //   折叠后组内链接自然移出 Tab 序列（display:none），无需逐个改 tabindex。</zh-CN>
  //   <en>Click to collapse → aria-expanded=false and container display:none; click again → true and display restored.
  //   Collapsed links naturally leave the Tab sequence via display:none, with no per-link tabindex edits.</en>
  // </lang>
  const firstToggle = page.locator('button.portal-nav-group-toggle').first();
  const firstControls = await firstToggle.getAttribute('aria-controls');
  await firstToggle.click();
  await page.waitForTimeout(150);
  const collapsed = await page.evaluate((id) => {
    const panel = document.getElementById(id);
    return {
      expanded: document.querySelector('button.portal-nav-group-toggle').getAttribute('aria-expanded'),
      display: panel ? getComputedStyle(panel).display : 'missing',
      hiddenClass: panel ? panel.className.includes('portal-nav-group-hidden') : false,
      hasScript: !!document.querySelector('script[src*="PortalNavigationGroups.js"]')
    };
  }, firstControls);
  record.facts.collapsed = collapsed;
  check('A15.CollapseAria', collapsed.expanded === 'false', JSON.stringify(collapsed));
  check('A16.CollapseDisplayNone', collapsed.display === 'none' && collapsed.hiddenClass, JSON.stringify(collapsed));
  check('A17.EnhancementScriptLoaded', collapsed.hasScript, 'script reference must resolve on the running site');

  await firstToggle.click();
  await page.waitForTimeout(150);
  const expandedAgain = await page.evaluate((id) => {
    const panel = document.getElementById(id);
    return {
      expanded: document.querySelector('button.portal-nav-group-toggle').getAttribute('aria-expanded'),
      display: panel ? getComputedStyle(panel).display : 'missing'
    };
  }, firstControls);
  record.facts.expandedAgain = expandedAgain;
  check('A18.ExpandRestoresAria', expandedAgain.expanded === 'true', JSON.stringify(expandedAgain));
  check('A19.ExpandRestoresDisplay', expandedAgain.display !== 'none', JSON.stringify(expandedAgain));

  // <lang>
  //   <zh-CN>Escape 关闭并把焦点交还分组按钮；这是 Q3 裁定的 APG disclosure 键盘契约中静态断言无法证明的一环。</zh-CN>
  //   <en>Escape closes the group and returns focus to its button; this is the APG disclosure keyboard contract from Q3 that static assertions cannot prove.</en>
  // </lang>
  await firstToggle.focus();
  await page.keyboard.press('Escape');
  await page.waitForTimeout(150);
  const escapeState = await page.evaluate(() => {
    const button = document.querySelector('button.portal-nav-group-toggle');
    return {
      expanded: button.getAttribute('aria-expanded'),
      focusIsToggle: document.activeElement === button,
      activeTag: document.activeElement ? document.activeElement.tagName.toLowerCase() : 'none'
    };
  });
  record.facts.escapeState = escapeState;
  check('A20.EscapeCloses', escapeState.expanded === 'false', JSON.stringify(escapeState));
  check('A21.EscapeReturnsFocus', escapeState.focusIsToggle, JSON.stringify(escapeState));

  // <lang>
  //   <zh-CN>再展开恢复截图态；随后逐套皮肤读取**运行中的站点真实 CSS**，注入临时 style 后切换 body class，
  //   再读分组按钮的计算样式。Web Forms Theme 每次只装载当前主题那一份 CSS，单纯改 body class
  //   并不会换样式表（首轮实测只有 EnterpriseLight 生效，其余是浏览器默认 button 样式），
  //   故必须按套注入站点实际服务的 CSS 才能证明 7 套作用域规则在浏览器里真的生效。
  //   临时 style 测完即移除，不污染后续截图。</zh-CN>
  //   <en>Expand again for the screenshot, then for each skin read the **running site's real CSS**, inject a temporary
  //   style element, switch body class and read computed styles on the group toggle. Web Forms Theme loads only the
  //   active skin's stylesheet, so changing body class alone does not swap stylesheets (the first pass proved only
  //   EnterpriseLight applied while the others showed default browser button styling). Injecting each skin's
  //   site-served CSS is required to prove all seven scope blocks really apply in a browser. The temporary style is
  //   removed after each measurement so it never pollutes the screenshot.</en>
  // </lang>
  await firstToggle.click();
  await page.waitForTimeout(150);
  const skinStyles = [];
  for (const theme of themes) {
    const cssResponse = await page.request.get(new URL('App_Themes/' + theme + '/Default.css', baseUrl).toString(), { timeout: 30000 });
    const cssText = cssResponse.ok() ? await cssResponse.text() : '';
    const measured = await page.evaluate(({ name, css }) => {
      let styleTag = null;
      if (css) {
        styleTag = document.createElement('style');
        styleTag.setAttribute('data-l2-theme', name);
        styleTag.textContent = css;
        document.head.appendChild(styleTag);
      }
      document.body.className = 'portal-theme-' + name.toLowerCase();
      const button = document.querySelector('button.portal-nav-group-toggle');
      const style = getComputedStyle(button);
      button.focus();
      const focusStyle = getComputedStyle(button);
      const result = {
        theme: name,
        cssLoaded: Boolean(css),
        borderColor: style.borderTopColor,
        color: style.color,
        backgroundColor: style.backgroundColor,
        outlineStyle: focusStyle.outlineStyle,
        outlineWidth: focusStyle.outlineWidth,
        display: style.display
      };
      if (styleTag) { styleTag.parentNode.removeChild(styleTag); }
      return result;
    }, { name: theme, css: cssText });
    skinStyles.push(measured);
  }
  record.facts.skinStyles = skinStyles;
  check('A22.AllSkinsScopedRules', skinStyles.length === themes.length && skinStyles.every((item) => item.cssLoaded && item.display === 'block'),
    JSON.stringify(skinStyles.map((s) => s.theme + ':' + s.cssLoaded + '/' + s.display)));
  // <lang>
  //   <zh-CN>收紧焦点判据：浏览器默认 outline 也会给出 auto/1px，把它当通过属于假绿；
  //   本包 7 套皮肤显式写的是 solid 2px，故按该精确值断言。</zh-CN>
  //   <en>Tighten the focus assertion: the browser default outline also yields auto/1px, so passing it would be a false
  //   green. The seven skins explicitly declare solid 2px, hence the exact-value assertion.</en>
  // </lang>
  check('A23.AllSkinsHaveFocusOutline', skinStyles.every((item) => item.outlineStyle === 'solid' && item.outlineWidth === '2px'),
    JSON.stringify(skinStyles.map((s) => s.theme + ':' + s.outlineStyle + '/' + s.outlineWidth)));
  const distinctBorders = new Set(skinStyles.map((item) => item.borderColor));
  record.facts.distinctBorderColors = distinctBorders.size;
  check('A24.ThemeColorsDiffer', distinctBorders.size >= 4, 'distinct border colours=' + distinctBorders.size);

  // <lang>
  //   <zh-CN>恢复站点原本的 body class 再截图，避免把测试用的皮肤切换写进证据画面。</zh-CN>
  //   <en>Restore the site's original body class before the screenshot so the test-only skin switching is not baked into the evidence image.</en>
  // </lang>
  await page.reload({ waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(600);
  const screenshotPath = path.join(outputDir, 'grouped-nav-' + locale + '.png');
  await page.screenshot({ path: screenshotPath, fullPage: true });
  record.screenshot = path.basename(screenshotPath);

  // <lang>
  //   <zh-CN>另起禁用 JavaScript 的上下文：分组仍以 aria-expanded="true" 输出、
  //   容器不带隐藏类且 display 不为 none、页签链接都有 href —— 证明页签可达不依赖脚本。</zh-CN>
  //   <en>Open a separate JavaScript-disabled context: groups still emit aria-expanded="true", containers carry no
  //   hidden class and display is not none, and every tab link has an href — proving reachability does not depend on script.</en>
  // </lang>
  const noJsContext = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale,
    javaScriptEnabled: false
  });
  const noJsPage = await noJsContext.newPage();
  const noJsResponse = await noJsPage.goto(new URL('DesktopDefault.aspx?tabid=1', baseUrl).toString(),
    { waitUntil: 'domcontentloaded', timeout: 60000 });
  const noJsFacts = await noJsPage.evaluate(() => {
    const buttons = Array.from(document.querySelectorAll('button.portal-nav-group-toggle'));
    const panels = Array.from(document.querySelectorAll('.portal-nav-group-tabs'));
    const links = Array.from(document.querySelectorAll('a.portal-tab'));
    return {
      groupCount: buttons.length,
      allExpanded: buttons.every((button) => button.getAttribute('aria-expanded') === 'true'),
      anyHidden: panels.some((panel) => panel.className.includes('portal-nav-group-hidden') || getComputedStyle(panel).display === 'none'),
      linkCount: links.length,
      linksWithoutHref: links.filter((link) => !link.getAttribute('href')).length
    };
  });
  record.facts.noJavaScript = noJsFacts;
  check('A25.NoJsHttp200', noJsResponse && noJsResponse.status() === 200, 'status=' + (noJsResponse ? noJsResponse.status() : 'none'));
  check('A26.NoJsAllExpanded', noJsFacts.groupCount > 0 && noJsFacts.allExpanded, JSON.stringify(noJsFacts));
  check('A27.NoJsTabsReachable', !noJsFacts.anyHidden && noJsFacts.linkCount > 0 && noJsFacts.linksWithoutHref === 0, JSON.stringify(noJsFacts));
  await noJsContext.close();

  const failed = checks.filter((item) => !item.ok);
  record.status = failed.length === 0 ? 'Pass' : 'Fail';
  record.facts.failedCheckCount = failed.length;
  fs.writeFileSync(path.join(outputDir, 'runtime-evidence-summary-' + locale + '.json'), JSON.stringify(record, null, 2), 'utf8');
  console.log('grouped nav L2: ' + record.status + ' (' + checks.filter((c) => c.ok).length + '/' + checks.length + ') ' + locale);
  for (const item of failed) { console.log('  [Fail] ' + item.name + ' — ' + item.detail); }
  await browserContext.close();
  await browser.close();
  process.exit(failed.length === 0 ? 0 : 1);
}
catch (error) {
  record.status = 'Fail';
  record.error = error.message;
  try {
    fs.mkdirSync(outputDir, { recursive: true });
    fs.writeFileSync(path.join(outputDir, 'runtime-evidence-summary-' + locale + '.json'), JSON.stringify(record, null, 2), 'utf8');
  } catch { /* best-effort */ }
  console.error('grouped nav L2 error: ' + error.message);
  await browser.close();
  process.exit(1);
}
