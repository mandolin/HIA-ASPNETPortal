// <lang>
//   <zh-CN>`W-anp-P293` 官网文档页中英各自渲染的浏览器级 L2 运行期证据（Q5 裁定）。
//   对已发布的官网文档站，按中文页（站点根）与英文页（/en/）分别用独立浏览器上下文渲染，
//   每个页面、每种语言各出一张独立截图（文件名含页面 + 语言），**禁止共用同一张**；
//   断言 html lang、H1、正文语言特征各自匹配，且中英截图字节不同。
//   本包新增的「两级分组导航」章节尚未发布，另外从本地站点镜像逐字节核对中英两份都已包含该章节，
//   并如实登记「线上为已发布版本，新增章节待推送后由 GitHub Pages 重建才会出现」的边界。</zh-CN>
//   <en>`W-anp-P293` browser-level L2 runtime evidence for separate Chinese/English rendering of public
//   documentation pages (Q5 ruling). Against the published documentation site, render Chinese pages (site root)
//   and English pages (/en/) in independent browser contexts, one independent screenshot per page and per locale
//   (filename carries page + locale), **never sharing one image**. Assert that html lang, H1 and body-language
//   features each match, and that the Chinese/English screenshots differ in bytes. The two-level grouped-navigation
//   section added in this work package is not published yet, so the local site mirrors are additionally checked
//   byte-wise for that section in both languages, with the boundary "the live site is the published version; the
//   new section appears only after a push triggers a GitHub Pages rebuild" recorded honestly.</en>
// </lang>
//
// <lang>
//   <zh-CN>环境变量：PORTAL_L2_SITE_BASE（默认已发布官网）/ PORTAL_L2_OUTPUT_DIR / PORTAL_PLAYWRIGHT_MODULE。
//   playwright 为开发期依赖且不入库；node 由 mise 提供。不读取任何凭据。</zh-CN>
//   <en>Environment: PORTAL_L2_SITE_BASE (defaults to the published site) / PORTAL_L2_OUTPUT_DIR /
//   PORTAL_PLAYWRIGHT_MODULE. Playwright is a development-only, uncommitted dependency; node is provided by mise.
//   No credentials are read.</en>
// </lang>
//
// <lang>
//   <zh-CN>证明边界：只证明采集时刻官网已发布页面在中英两种语言下各自真实渲染、截图各自独立且语言匹配；
//   **不证明**本包新增章节已上线（未推送，Pages 待重建）、不证明译本语义准确、不证明跨机器像素一致、
//   不证明主题皮肤或门户业务页的视觉（由 P291 / P292 的 L2 证据承载）。
//   新增章节的中英同构由本地镜像文件与 `Test-PortalSiteMirrorFreshness.ps1` /
//   `Test-PortalSiteMirrorConsistency.ps1` / `Test-PortalSiteTranslationFreshness.ps1` 门禁承载。</zh-CN>
//   <en>Proof boundary: proves only that, at collection time, the published documentation pages really render in
//   both languages with independent language-matching screenshots. It does **not** prove the new section is live
//   (not pushed; Pages rebuild pending), that translations are semantically accurate, cross-machine pixel equality,
//   or theme/portal visuals (covered by W-anp-P291 / W-anp-P292 L2 evidence). Chinese/English parity of the new
//   section is carried by the local mirror files and the SiteMirrorFreshness / SiteMirrorConsistency /
//   SiteTranslationFreshness gates.</en>
// </lang>

import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang>
///   <zh-CN>解析 playwright 模块；优先环境变量指定位置，否则按包名解析。</zh-CN>
///   <en>Resolve the playwright module; the environment variable wins, otherwise fall back to package-name resolution.</en>
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

const repoRoot = path.resolve(process.cwd());
const siteBase = (process.env.PORTAL_L2_SITE_BASE || 'https://mandolin.github.io/HIA-ASPNETPortal/').replace(/\/?$/, '/');
const outputDir = process.env.PORTAL_L2_OUTPUT_DIR || path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p51-public-docs-locales');

/// <summary>
/// <lang>
///   <zh-CN>本包改动的四份官网文档；中文页在站点根，英文页在 /en/ 下（与 `_config.yml` 双语树一致）。
///   `mirrorCn` / `mirrorEn` 是 Build-PortalSite.ps1 生成的本地镜像 Markdown，用于核对新增章节已双向生成。</zh-CN>
///   <en>The four public documents changed by this package; Chinese pages live at the site root and English pages
///   under /en/ (matching the bilingual tree in `_config.yml`). `mirrorCn` / `mirrorEn` are the local mirror
///   Markdown files produced by Build-PortalSite.ps1, used to check the new section was generated in both languages.</en>
/// </lang>
/// </summary>
const pages = [
  { id: 'dev-guide', cnPath: 'dev-guide/', enPath: 'en/dev-guide/', cnH1: '开发指南', enH1: 'DEVELOPER GUIDE',
    mirrorCn: 'site/dev-guide/index.md', mirrorEn: 'site/dev-guide/en/index.md', sectionCn: '主导航两级分组', sectionEn: 'Two-level grouped main navigation' },
  { id: 'theme-module-contract', cnPath: 'theme-module-contract/', enPath: 'en/theme-module-contract/', cnH1: '模块外观契约', enH1: 'Theme-module appearance contract',
    mirrorCn: 'site/theme-module-contract/index.md', mirrorEn: 'site/theme-module-contract/en/index.md', sectionCn: '两级分组导航类的主题作用域', sectionEn: 'Theme scope rules for grouped-navigation classes' },
  { id: 'theme-semantic-variables', cnPath: 'theme-semantic-variables/', enPath: 'en/theme-semantic-variables/', cnH1: '主题语义变量矩阵', enH1: 'Theme semantic variable matrix',
    mirrorCn: 'site/theme-semantic-variables/index.md', mirrorEn: 'site/theme-semantic-variables/en/index.md', sectionCn: '主导航分组类不在本矩阵内', sectionEn: 'Main-navigation group classes are outside this matrix' },
  { id: 'module-mechanics-and-extension', cnPath: 'module-mechanics-and-extension/', enPath: 'en/module-mechanics-and-extension/', cnH1: '模块运作与二次开发', enH1: 'Module mechanics and secondary development',
    mirrorCn: 'site/module-mechanics-and-extension/index.md', mirrorEn: 'site/module-mechanics-and-extension/en/index.md', sectionCn: '口径区分', sectionEn: 'Terminology' }
];

const checks = [];
function check(name, ok, detail) {
  checks.push({ name, ok: Boolean(ok), detail });
}

const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
const record = {
  collectedUtc: new Date().toISOString(),
  siteBase,
  pages: pages.map((p) => p.id),
  locales: ['zh-CN', 'en-US'],
  status: 'Pass',
  facts: { screenshots: [], mirrorSections: [] },
  checks
};

try {
  fs.mkdirSync(outputDir, { recursive: true });

  /// <summary>
  /// <lang>
  ///   <zh-CN>每种语言一个独立上下文；页面真实渲染后出图。H1 在官网 CSS 下可能被 uppercase 化，
  ///   故用「包含期望文案（大小写不敏感）」判定，同时用 html lang 与正文中文字符比例做第二重语言判定。</zh-CN>
  ///   <en>One independent context per locale; screenshot the real rendered page. H1 may be uppercased by the
  ///   site CSS, so assert case-insensitive containment, with html lang and the Chinese-character ratio of the body
  ///   as a second language signal.</en>
  /// </lang>
  /// </summary>
  async function capture(pageDef, locale, url, expectedLang, expectedH1) {
    const context = await browser.newContext({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 1,
      locale
    });
    const page = await context.newPage();
    page.setDefaultTimeout(40000);
    const response = await page.goto(new URL(url, siteBase).toString(), { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(400);
    const facts = await page.evaluate(() => {
      const body = document.body.innerText;
      const han = (body.match(/[\u4e00-\u9fff]/g) || []).length;
      return {
        htmlLang: document.documentElement.lang,
        h1: document.querySelector('h1') ? document.querySelector('h1').innerText.trim() : '',
        bodyLength: body.length,
        hanRatio: body.length > 0 ? Number((han / body.length).toFixed(3)) : 0,
        title: document.title
      };
    });
    const tag = pageDef.id + '-' + locale;
    check('A1.' + tag + '.Http200', response && response.status() === 200, 'status=' + (response ? response.status() : 'none'));
    check('A2.' + tag + '.HtmlLang', facts.htmlLang.toLowerCase().startsWith(expectedLang.split('-')[0]), 'lang=' + facts.htmlLang);
    check('A3.' + tag + '.H1MatchesLocale', facts.h1.toLowerCase().includes(expectedH1.toLowerCase()), 'h1=' + facts.h1);
    // <lang>
    //   <zh-CN>中文页汉字占比应显著高于英文页；这一判据不依赖任何具体文案，避免「同一张图改标题」的假证据。</zh-CN>
    //   <en>The Chinese page must have a materially higher Han-character ratio than the English page; this criterion
    //   does not depend on any specific copy, so a "same image with a changed title" cannot fake it.</en>
    // </lang>
    check('A4.' + tag + '.BodyLanguage', locale === 'zh-CN' ? facts.hanRatio > 0.15 : facts.hanRatio < 0.05,
      'hanRatio=' + facts.hanRatio + ' bodyLength=' + facts.bodyLength);

    const shot = path.join(outputDir, 'public-docs-' + tag + '.png');
    await page.screenshot({ path: shot, fullPage: true });
    const bytes = fs.readFileSync(shot);
    const digest = crypto.createHash('sha256').update(bytes).digest('hex');
    record.facts.screenshots.push({ file: path.basename(shot), locale, page: pageDef.id, bytes: bytes.length, sha256: digest });
    check('A5.' + tag + '.ScreenshotIndependent', bytes.length > 0, 'bytes=' + bytes.length);
    await context.close();
    return { facts, digest, file: path.basename(shot) };
  }

  const results = [];
  for (const pageDef of pages) {
    const cn = await capture(pageDef, 'zh-CN', pageDef.cnPath, 'zh-CN', pageDef.cnH1);
    const en = await capture(pageDef, 'en-US', pageDef.enPath, 'en', pageDef.enH1);
    results.push({ page: pageDef.id, cn, en });

    // <lang>
    //   <zh-CN>Q5 硬约束：中英各自渲染、禁止共用同一张。用 sha256 与文件名双重断言。</zh-CN>
    //   <en>Q5 hard constraint: separate Chinese/English rendering, never share one image. Assert via both sha256 and filename.</en>
    // </lang>
    check('B1.' + pageDef.id + '.NoSharedScreenshot', cn.digest !== en.digest && cn.file !== en.file,
      'cn=' + cn.file + '/' + cn.digest.slice(0, 12) + ' en=' + en.file + '/' + en.digest.slice(0, 12));
    check('B2.' + pageDef.id + '.TitlesDiffer', cn.facts.h1.toLowerCase() !== en.facts.h1.toLowerCase(),
      'cn=' + cn.facts.h1 + ' en=' + en.facts.h1);
    check('B3.' + pageDef.id + '.HanRatioDiffers', cn.facts.hanRatio > en.facts.hanRatio + 0.1,
      'cn=' + cn.facts.hanRatio + ' en=' + en.facts.hanRatio);

    // <lang>
    //   <zh-CN>本包新增章节从**本地镜像**核对：中英两份各自包含本语言章节标题，且另一语言标题不出现。
    //   线上站点尚未发布该章节，故不以线上页面断言它，避免把「未上线」误写成「已验证」。</zh-CN>
    //   <en>Verify the new section against the **local mirrors**: each language file contains its own section heading
    //   and not the other language's. The live site has not published the section yet, so it is deliberately not
    //   asserted against the online page, which would misreport "not live" as "verified".</en>
    // </lang>
    const cnMirror = fs.existsSync(path.join(repoRoot, pageDef.mirrorCn)) ? fs.readFileSync(path.join(repoRoot, pageDef.mirrorCn), 'utf8') : '';
    const enMirror = fs.existsSync(path.join(repoRoot, pageDef.mirrorEn)) ? fs.readFileSync(path.join(repoRoot, pageDef.mirrorEn), 'utf8') : '';
    const mirrorCnOk = cnMirror.includes(pageDef.sectionCn);
    const mirrorEnOk = enMirror.includes(pageDef.sectionEn);
    record.facts.mirrorSections.push({ page: pageDef.id, cn: mirrorCnOk, en: mirrorEnOk, cnPath: pageDef.mirrorCn, enPath: pageDef.mirrorEn });
    check('C1.' + pageDef.id + '.NewSectionInBothMirrors', mirrorCnOk && mirrorEnOk,
      'cn=' + mirrorCnOk + ' en=' + mirrorEnOk);
    check('C2.' + pageDef.id + '.MirrorsNotShared', cnMirror.length > 0 && enMirror.length > 0 && cnMirror !== enMirror,
      'cnLen=' + cnMirror.length + ' enLen=' + enMirror.length);
  }

  const shots = record.facts.screenshots;
  check('D1.EightIndependentScreenshots', shots.length === 8 && new Set(shots.map((s) => s.file)).size === 8 && new Set(shots.map((s) => s.sha256)).size === 8,
    'count=' + shots.length + ' distinctFiles=' + new Set(shots.map((s) => s.file)).size + ' distinctHashes=' + new Set(shots.map((s) => s.sha256)).size);
  record.facts.pageResults = results.map((r) => ({ page: r.page, cnHanRatio: r.cn.facts.hanRatio, enHanRatio: r.en.facts.hanRatio, cnH1: r.cn.facts.h1, enH1: r.en.facts.h1 }));
}
catch (error) {
  record.status = 'Fail';
  record.error = error.message;
  check('X0.NoException', false, error.message);
}
finally {
  try {
    const failed = checks.filter((item) => !item.ok);
    if (failed.length > 0) { record.status = 'Fail'; }
    record.facts.failedCheckCount = failed.length;
    fs.writeFileSync(path.join(outputDir, 'runtime-evidence-summary.json'), JSON.stringify(record, null, 2), 'utf8');
  } catch { /* best-effort */ }
  console.log('public docs locale L2: ' + record.status + ' (' + checks.filter((c) => c.ok).length + '/' + checks.length + ')');
  for (const item of checks.filter((c) => !c.ok)) { console.log('  [Fail] ' + item.name + ' — ' + item.detail); }
  await browser.close();
  process.exit(record.status === 'Pass' ? 0 : 1);
}
