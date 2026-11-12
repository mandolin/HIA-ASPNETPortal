// <lang>
//   <zh-CN>文档全链路走查门禁（`W-anp-P319` / `C-anp-P56`）：把「按文档逐步执行能走通」从人工走查升级为
//   参数化、可复跑的机器证据。由 `work-zone/dev/evidence/p265/first-use-walkthrough.mjs` 的独立视角原型正式化而来。
//
//   断言分两组：
//     D1–D3、D9（静态，无需站点）四段学习文档链路齐备且中英同构、公开教学配图清单（Q5）逐条核对
//        文件存在 / 字节数 / sha256 / 中英成对且哈希不同 / 两侧文档引用、脚本与 SQL 示例路径真实存在、
//        README 快速上手七步结构与仓库内路径引用可核对（不执行任何命令）；
//     D4–D8（运行期）匿名首页页签与文档一致、无注册链接、错误口令不区分账号/口令、
//        正确登录后欢迎语与页签集合与文档一致、我的待办可达。
//   期望值**从文档正文解析**（匿名两个页签、登录后六项列表），不在脚本里硬编码数据行；
//   这样文档改了而页面没改会被判红，而不是让门禁与文档各自漂移。
//
//   为什么要有独立的文档走查：既有 L2 门禁断言「设计意图」（页签该存在、待办该可达），
//   本门禁断言「文档描述是否与页面一致」。同一套断言自证等于没走查，两者不可互相替代。
//
//   语言：门户每请求按 Cookie `lang` → `Accept-Language` 首项 → en-US 回退决定 UI 语言
//   （`src/Portal/Global.asax.cs:405-425`）。本脚本所有浏览器上下文都固定 `locale: 'zh-CN'` 并写
//   `lang=zh-CN` cookie（沿用 `PortalLocalizedNameRuntimeProbe.mjs:38-43` 的双保险范式），
//   否则中文文档会对着英文页签比对，产生假失败。
//
//   额外页签：`docs/first-use-portal.md` 描述的是**导航种子基线**（`Portal_NavigationSeed.sql`，由
//   `Test-PortalNavigationShape.ps1` 硬绑定）。部署了可选独立模块后导航会出现增量页签（本机 40001 因
//   `PortalCfg_FormBuilder_Provision.sql` 含 `Form Builder`）。因此断言是「文档列出的核心页签必须按序出现」，
//   额外页签**不判 Fail 也不静默忽略**，一律写入摘要 `extraTabs` / `undocumentedExtraTabs` 透明上报。
//
//   证明边界：只证明采集时刻、指定 baseUrl、中文 UI 下，四段文档链路齐备、公开教学配图清单与磁盘文件
//   （字节数 / sha256 / 中英哈希不同 / 两侧引用）一致，且 `first-use-portal.md` 的匿名/登录后核心页签叙述
//   与真实浏览器渲染一致；不证明「部署到 IIS」与「从零加模块」两段已被人工端到端执行
//   （它们只做静态链路与路径存在性核对，实际执行须按各自文档的走查清单另行留证）、
//   **不证明教学配图内容与页面语义相符、不证明图上语言与页面语言一致、不证明配图时效性**
//   （这三项由人工核对承载，清单的 `captureMetadata: not-recorded` 明示采集参数未登记）、
//   D9 只核对文档结构与被引用文件存在，**不执行任何命令**，故不证明「下载安装」七步在干净机器上真能跑通
//   （该段实做由一次性浅克隆留证承载，见 W-anp-P320 工作包文档）；
//   不证明可选模块增量页签已被文档覆盖（如实登记为欠账）、不证明跨机器像素一致、
//   不证明读屏器语义（a11y-runtime 未覆盖面）、不证明英文页面叙述一致
//   （本脚本只走中文页面，英文叙述一致性属 W-anp-P320 分语言证据范围）。
//   Proof boundary: this gate proves only that, at capture time and for the configured base URL, the four-stage
//   documentation chain is complete and the anonymous / signed-in narrative in `first-use-portal.md` matches a real
//   browser render. It does not prove that the IIS deployment or the from-scratch module walkthroughs were executed
//   end to end (those are checked for chain and path existence only), nor cross-machine pixel equality, screen-reader
//   semantics (the `a11y-runtime` uncovered surface) or parity of the English narrative (covered separately by the
//   per-locale evidence in `W-anp-P320`).
//
//   前置：运行中的门户、`temp/p65/p65-acceptance-context.json`（baseUrl / 管理员账号）、playwright。
//   任一缺失时**显式 exit 2**（INCOMPLETE / SKIP，不当作通过）；静态断言结果仍写入摘要，便于区分
//   「文档链路本身不成立」与「环境未就绪」。凭据只在进程内存使用，绝不写入产物。
//   套件层对 mjs 门禁的 exit 2 识别为 Skip（`Invoke-PortalGateSuite.ps1`）。
//
//   参数（全部环境变量，凭据不入库）：
//     PORTAL_ACCEPTANCE_CONTEXT   验收上下文路径，默认 temp/p65/p65-acceptance-context.json
//     PORTAL_BASE_URL             覆盖 baseUrl（否则取上下文 baseUrl / 默认 http://localhost:40001/）
//     PORTAL_WALKTHROUGH_OUT      产物目录，默认 temp/p319
//     PORTAL_PLAYWRIGHT_MODULE    playwright 模块路径（文件或目录），否则 import('playwright')
//     PORTAL_WALKTHROUGH_STATIC  为 1 时只跑静态断言 D1–D3，不启动浏览器（用于无站点环境的链路自检）
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalDocumentationWalkthrough.mjs'
//   产物：temp/p319/walkthrough-summary.json + 4 张 PNG；退出码 0 通过 / 1 断言失败 / 2 前置缺失。
//   </zh-CN>
//   <en>Documentation chain walkthrough gate (`W-anp-P319` / `C-anp-P56`): turns "follow the docs and it works" from a
//   manual walkthrough into parameterised, repeatable machine evidence. Formalised from the independent-perspective
//   prototype at `work-zone/dev/evidence/p265/first-use-walkthrough.mjs`.
//
//   Two assertion groups: D1-D3 and D9 (static, no site needed) check that the four-stage learning chain is complete
//   and bilingual, that the public teaching image manifest (Q5) matches every file's bytes/sha256 and keeps zh/en pairs
//   distinct with both guides referencing their own images, that script/SQL examples resolve to real paths, and that the
//   README quick-start seven-step structure and in-repo references are checkable (no command is executed by D9);
//   D4-D8 (runtime) check anonymous tabs against the document, absence of a registration link, a wrong-password
//   response that does not disclose whether the account exists, a signed-in welcome message and tab set that match the
//   document, and reachability of "My Work Items". Expectations are **parsed from the document body** rather than
//   hard-coded, so a doc change without a page change fails the gate.
//
//   Why a separate documentation walkthrough: existing L2 gates assert design intent; this one asserts that the
//   document matches the page. Self-certifying with the same assertions is not a walkthrough.
//
//   Proof boundary: see the Chinese block above (this literal marker is required by the coverage-map M6 check).
//
//   Locale: the portal resolves UI language per request (Cookie `lang` -> Accept-Language -> en-US), so every browser
//   context here fixes `locale: 'zh-CN'` plus a `lang=zh-CN` cookie, following `PortalLocalizedNameRuntimeProbe.mjs`.
//   Otherwise a Chinese document would be compared against English tabs and fail spuriously.
//
//   Extra tabs: `docs/first-use-portal.md` describes the navigation-seed baseline (hard-bound by
//   `Test-PortalNavigationShape.ps1`). Installing optional independent modules adds tabs (this machine's 40001 has
//   `Form Builder` from `PortalCfg_FormBuilder_Provision.sql`). The assertion is therefore that every documented core
//   tab appears; extra tabs are **not a failure and not silently ignored** — they are reported transparently
//   in the summary as `extraTabs` / `undocumentedExtraTabs`.
//
//   Proof boundary: the gate proves the four-stage chain, that the teaching-image manifest matches the files (bytes,
//   sha256, distinct zh/en hashes, both guides referencing their own images), and that the anonymous / signed-in
//   narrative matches a real Chinese render. It does **not** prove that a teaching image's content matches the page
//   semantics, that the on-screen language matches the image, or that the illustration is current — those are human
//   review items, and the manifest's `captureMetadata: not-recorded` states that capture parameters were not recorded.
//   D9 checks document structure and referenced file existence only and **executes no command**, so it does not prove
//   the seven quick-start steps run on a clean machine (that stage is evidenced separately by a one-off shallow clone).
//   It does not prove the IIS deployment or from-scratch module walkthroughs were executed end to end, nor
//   cross-machine pixel equality, screen-reader semantics (`a11y-runtime`), or parity of the English narrative.
//
//   Prerequisites: a running portal, `temp/p65/p65-acceptance-context.json` (baseUrl / admin account), playwright.
//   Any missing prerequisite yields an explicit exit 2 (INCOMPLETE / SKIP, never a pass); static results are still
//   written to the summary so a broken documentation chain is distinguishable from an unready environment.
//   Credentials stay in process memory and are never written into artifacts.
//
//   Parameters (all environment variables; no credentials in the repository):
//     PORTAL_ACCEPTANCE_CONTEXT   acceptance context path, default temp/p65/p65-acceptance-context.json
//     PORTAL_BASE_URL             overrides baseUrl
//     PORTAL_WALKTHROUGH_OUT      output directory, default temp/p319
//     PORTAL_PLAYWRIGHT_MODULE    playwright module (file or directory), otherwise import('playwright')
//     PORTAL_WALKTHROUGH_STATIC  set to 1 to run D1-D3 only, without a browser
//
//   Usage (repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalDocumentationWalkthrough.mjs'
//   Output: temp/p319/walkthrough-summary.json plus four PNGs; exit 0 pass / 1 assertion failed / 2 prerequisite missing.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';

const repoRoot = process.cwd();
const contextPath = path.resolve(repoRoot, process.env.PORTAL_ACCEPTANCE_CONTEXT || 'temp/p65/p65-acceptance-context.json');
const outDir = path.resolve(repoRoot, process.env.PORTAL_WALKTHROUGH_OUT || 'temp/p319');
const staticOnly = process.env.PORTAL_WALKTHROUGH_STATIC === '1';

const summary = {
  generatedAt: new Date().toISOString(),
  repoRoot,
  staticOnly,
  checks: [],
  prerequisites: {},
  status: 'Pass'
};

function record(id, title, pass, detail) {
  summary.checks.push({ id, title, status: pass ? 'Pass' : 'Fail', detail });
  if (!pass) summary.status = 'Fail';
  console.log(`[${pass ? 'PASS' : 'FAIL'}] ${id} ${title}`);
  return pass;
}

function readText(rel) {
  const p = path.join(repoRoot, rel);
  return fs.existsSync(p) ? fs.readFileSync(p, 'utf8') : null;
}

// <lang><zh-CN>D1：四段学习文档链路齐备且中英同构（下载安装 / 部署运行 / 日常使用 / 模块开发）。</zh-CN>
// <en>D1: the four-stage learning chain is complete in both languages (get source / deploy / daily use / module dev).</en></lang>
function checkChain() {
  const stages = [
    { id: 'source', zh: 'README.md', en: 'README.md', zhNeedle: '先取得源码', enNeedle: 'source only' },
    { id: 'guide', zh: 'docs/user-guide.md', en: 'site/_translations/user-guide.md', zhNeedle: '取得源码', enNeedle: 'Getting the source' },
    { id: 'deploy', zh: 'docs/operations-runbook.md', en: 'site/_translations/operations-runbook.md', zhNeedle: '## 首次部署到 IIS', enNeedle: '## First-time deployment to IIS' },
    { id: 'dev', zh: 'docs/module-development-guide.md', en: 'site/_translations/module-development-guide.md', zhNeedle: '## 端到端示例', enNeedle: '## End-to-end example' },
    { id: 'daily', zh: 'docs/first-use-portal.md', en: 'site/_translations/first-use-portal.md', zhNeedle: '## 登录后左侧有什么', enNeedle: '## What the left side shows after signing in' }
  ];
  const detail = [];
  let pass = true;
  for (const s of stages) {
    const zh = readText(s.zh);
    const en = readText(s.en);
    const zhOk = !!zh && zh.includes(s.zhNeedle);
    const enOk = !!en && (en.includes(s.enNeedle) || (s.id === 'source' && en.includes('source')));
    detail.push({ stage: s.id, zh: s.zh, zhOk, en: s.en, enOk });
    if (!zhOk || !enOk) pass = false;
  }
  record('D1', 'four-stage bilingual documentation chain', pass, detail);
}

// <lang><zh-CN>D2：公开教学配图清单驱动校验（Q5 / W-anp-P320）。清单登记文件事实，
// 本断言逐条核对文件存在、字节数、sha256、中英成对且哈希不同，并把结果写进摘要。
// 这是「清单不是摆设」的机器保证：改图不更新清单、或中英共用同一张，都会判红。</zh-CN>
// <en>D2: manifest-driven validation of public teaching images (Q5 / W-anp-P320). The manifest records file
// facts; this assertion verifies existence, byte count, sha256 and that each zh/en pair is distinct. Changing an
// image without updating the manifest, or sharing one image across languages, fails the gate.</en></lang>
function checkImages() {
  const manifestRel = 'site/assets/img/first-use/teaching-images-manifest.json';
  const manifestPath = path.join(repoRoot, manifestRel);
  const doc = readText('docs/first-use-portal.md') || '';
  const enDoc = readText('site/_translations/first-use-portal.md') || '';
  const referenced = [...doc.matchAll(/home-|signin-card|my-work-items/g)].length;

  if (!fs.existsSync(manifestPath)) {
    record('D2', 'public teaching image manifest', false, { missing: manifestRel });
    return;
  }
  let manifest;
  try {
    manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  } catch (e) {
    record('D2', 'public teaching image manifest', false, { parseError: String(e.message || e) });
    return;
  }

  const imgDir = path.dirname(manifestPath);
  const pairs = Array.isArray(manifest.pairs) ? manifest.pairs : [];
  const problems = [];
  const facts = [];
  for (const pair of pairs) {
    for (const side of ['zh', 'en']) {
      const item = pair[side];
      if (!item || !item.file || !item.sha256) { problems.push(`${pair.id}.${side}: manifest entry incomplete`); continue; }
      const p = path.join(imgDir, item.file);
      if (!fs.existsSync(p)) { problems.push(`${item.file}: missing`); continue; }
      const buffer = fs.readFileSync(p);
      const actualSha = crypto.createHash('sha256').update(buffer).digest('hex');
      const shaOk = actualSha === item.sha256;
      const bytesOk = buffer.length === item.bytes;
      facts.push({ pair: pair.id, locale: item.locale, file: item.file, bytes: buffer.length, declaredBytes: item.bytes, bytesOk, shaOk });
      if (!shaOk) problems.push(`${item.file}: sha256 mismatch`);
      if (!bytesOk) problems.push(`${item.file}: byte count mismatch`);
      const ref = side === 'zh' ? doc : enDoc;
      if (!ref.includes(item.file)) problems.push(`${item.file}: not referenced by the ${side === 'zh' ? 'Chinese' : 'English'} guide`);
    }
    if (pair.zh && pair.en && pair.zh.sha256 === pair.en.sha256) {
      problems.push(`${pair.id}: zh/en share the same image (prohibited)`);
    }
  }
  const pairCountOk = pairs.length === manifest.pairCount && pairs.length >= 4;
  if (!pairCountOk) problems.push(`pairCount mismatch: declared ${manifest.pairCount}, found ${pairs.length}`);
  record('D2', 'public teaching image manifest and per-locale images',
    problems.length === 0 && referenced >= 4 && pairCountOk,
    { manifest: manifestRel, artifactClass: manifest.artifactClass, captureMetadata: manifest.captureMetadata,
      pairCount: pairs.length, referencedMentions: referenced, problems, images: facts });
}

// <lang><zh-CN>D3：新增章节里的脚本/SQL 示例路径真实存在（与 Test-PortalDocumentationExamples.ps1 的 E1/E2 同口径）。</zh-CN>
// <en>D3: script/SQL examples in the new sections resolve to real paths.</en></lang>
function checkExamplePaths() {
  const files = ['docs/operations-runbook.md', 'site/_translations/operations-runbook.md',
    'docs/module-development-guide.md', 'site/_translations/module-development-guide.md'];
  const refs = new Set();
  for (const f of files) {
    const text = readText(f) || '';
    for (const m of text.matchAll(/dev[\\/]scripts[\\/][A-Za-z0-9._-]+\.ps1/g)) refs.add(m[0].replace(/\\/g, '/'));
    for (const m of text.matchAll(/src[\\/]Setup[\\/][A-Za-z0-9._-]+\.sql/g)) refs.add(m[0].replace(/\\/g, '/'));
  }
  // <lang><zh-CN>占位路径豁免与 Test-PortalDocumentationExamples.ps1 E1/E2 同口径：教学示例里的
  // `PortalBiz_YourEntities.sql` 注定不存在，按普通引用校验会让门禁永久 FAIL。豁免数透明上报，不静默放过。</zh-CN>
  // <en>Placeholder-path exemption, matching E1/E2 of Test-PortalDocumentationExamples.ps1: teaching examples such as
  // `PortalBiz_YourEntities.sql` can never exist. Exemptions are reported transparently, never silently skipped.</en></lang>
  const isPlaceholder = (r) => /Your[A-Za-z]/.test(r) || /[<>{}]/.test(r);
  const exempted = [...refs].filter(isPlaceholder);
  const results = [...refs].filter((r) => !isPlaceholder(r)).sort()
    .map((r) => ({ ref: r, exists: fs.existsSync(path.join(repoRoot, r)) }));
  const missing = results.filter((r) => !r.exists).map((r) => r.ref);
  record('D3', 'documented script/SQL example paths exist', missing.length === 0 && results.length > 0,
    { checked: results.length, missing, exempted, exemptionCount: exempted.length, refs: results });
}

// <lang><zh-CN>D9：README「快速上手」七步的结构与仓库内路径引用可核对（`W-anp-P320` 承接 P319-③ 的
// 「下载安装」段按文档逐步实做）。只断言文档结构与被引用文件存在，不执行任何构建/数据库/配置写入；
// 绝对路径与含环境变量的目标路径透明登记、不检查（它们依赖目标机器）。</zh-CN>
// <en>D9: the README quick-start seven-step structure and its in-repo path references are checkable
// (`W-anp-P320`, taking over the "get source / install" stage from P319-3). Only document structure and referenced
// file existence are asserted; no build, database or configuration write is performed. Absolute paths and targets
// containing environment variables are reported but not checked, since they depend on the target machine.</en></lang>
function checkQuickStartSteps() {
  const readme = readText('README.md') || '';
  const section = readme.split('## 快速上手（本地开发）')[1];
  const body = section ? section.split(/\n## /)[0] : '';
  const headings = [...body.matchAll(/^###\s*(\d+)\.\s*(.+)$/gm)].map((m) => ({ n: Number(m[1]), title: m[2].trim() }));
  const sequential = headings.length >= 7 && headings.every((h, i) => h.n === i + 1);
  const repoRefs = new Set();
  for (const m of body.matchAll(/(?:dev[\\/]scripts[\\/][A-Za-z0-9._-]+\.ps1|src[\\/][A-Za-z0-9._/\-\\]+|docs[\\/][A-Za-z0-9._-]+\.md)/g)) {
    repoRefs.add(m[0].replace(/\\/g, '/'));
  }
  const checked = [];
  const missing = [];
  for (const ref of [...repoRefs].sort()) {
    // 模板目录/通配路径只作登记，不作文件存在性断言（例如 src/Setup/ 是目录）。
    const isFileRef = /\.(ps1|sql|config|md|sln)$/.test(ref);
    const exists = fs.existsSync(path.join(repoRoot, ref));
    checked.push({ ref, isFileRef, exists });
    if (isFileRef && !exists) missing.push(ref);
  }
  const externalTargets = [...body.matchAll(/(?:[A-Z]:[\\/][^\s`"']+|\$env:[A-Za-z_]+[\\/][^\s`"']+)/g)].map((m) => m[0]);
  record('D9', 'quick-start seven-step structure and in-repo references',
    sequential && missing.length === 0 && headings.length >= 7,
    { sectionFound: !!section, steps: headings, sequential, checked, missing,
      externalTargetsReportedOnly: [...new Set(externalTargets)],
      note: 'Structure and referenced files only; no command was executed by this assertion.' });
}

// <lang><zh-CN>从 first-use-portal.md 解析期望值，避免硬编码数据行。</zh-CN>
// <en>Parses expectations from first-use-portal.md instead of hard-coding data rows.</en></lang>
function parseDocumentExpectations() {
  const doc = readText('docs/first-use-portal.md') || '';
  const anonMatch = doc.match(/没登录的时候[^\n]*两个页签/);
  const anonTabs = anonMatch ? [...anonMatch[0].matchAll(/「([^」]+)」/g)].map((m) => m[1]) : [];
  const section = doc.split('## 登录后左侧有什么')[1];
  const listPart = section ? section.split('## ')[0] : '';
  const signedTabs = [...listPart.matchAll(/^- (.+?) ——/gm)].map((m) => m[1].trim());
  return { anonTabs, signedTabs, noRegister: /页面上没有注册链接/.test(doc), welcome: /欢迎/.test(doc) };
}

// <lang><zh-CN>解析 playwright：优先环境变量，兼容 ESM/CJS 两种包布局。</zh-CN>
// <en>Resolves playwright, preferring the env var and accepting both ESM and CJS package layouts.</en></lang>
async function loadChromium() {
  const configured = process.env.PORTAL_PLAYWRIGHT_MODULE;
  if (configured) {
    const lower = configured.toLowerCase();
    if (/\.(mjs|js|cjs)$/.test(lower)) return (await import(pathToFileURL(configured).href)).chromium;
    for (const entry of ['index.mjs', 'index.js', 'index.cjs']) {
      const p = path.join(configured, entry);
      if (fs.existsSync(p)) return (await import(pathToFileURL(p).href)).chromium;
    }
    throw new Error('no importable playwright entry under ' + configured);
  }
  return (await import('playwright')).chromium;
}

function norm(s) { return (s || '').replace(/\s+/g, ' ').trim(); }

// <lang><zh-CN>固定中文 UI：locale + `lang` cookie 双保险（门户按 Cookie 优先解析语言）。</zh-CN>
// <en>Fix the Chinese UI: locale plus the `lang` cookie (the portal prefers the cookie).</en></lang>
async function newChineseContext(browser, baseUrl) {
  const context = await browser.newContext({ locale: 'zh-CN', viewport: { width: 1440, height: 900 } });
  const url = new URL(baseUrl);
  await context.addCookies([{ name: 'lang', value: 'zh-CN', domain: url.hostname, path: '/' }]);
  return context;
}

// <lang><zh-CN>断言＝文档列出的核心页签全部出现；顺序差异（分组渲染）与额外页签（可选模块增量）
// 一律透明上报，不判 Fail 也不静默忽略——避免为过门禁而把真实漂移藏起来。</zh-CN>
// <en>The assertion is that every documented core tab appears; order differences (grouped rendering) and extra tabs
// (optional-module increments) are reported transparently, neither failed nor silently ignored.</en></lang>
function matchDocumentedTabs(expected, observed, documentedExtras = []) {
  const positions = expected.map((t) => observed.findIndex((n) => n.includes(t)));
  const allPresent = positions.every((p) => p >= 0);
  const inOrder = allPresent && positions.every((p, i) => i === 0 || positions[i - 1] < p);
  const extraTabs = observed.filter((n) => !expected.some((t) => n.includes(t)));
  const undocumentedExtraTabs = extraTabs.filter((n) => !documentedExtras.some((t) => n.includes(t)));
  return { allPresent, inOrder, positions, extraTabs, undocumentedExtraTabs };
}

async function collectTabs(page) {
  return page.$$eval('.portal-tab', (nodes) => nodes.map((n) => ({
    text: (n.innerText || '').trim(), href: n.getAttribute('href') || ''
  })));
}

async function runRuntime(ctx) {
  const chromium = await loadChromium();
  const browser = await chromium.launch({ headless: true });
  const context = await newChineseContext(browser, ctx.baseUrl);
  try {
    const expect = parseDocumentExpectations();
    summary.documentExpectations = expect;
    record('D4', 'document expectations parsed', expect.anonTabs.length >= 2 && expect.signedTabs.length >= 6, expect);

    let page = await context.newPage();
    await page.goto(ctx.baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1500);
    const anonTabs = await collectTabs(page);
    const anonText = norm(await page.evaluate(() => document.body.innerText || ''));
    const hasSigninCard = (await page.locator('input[id$="EmailOrName"]').count()) > 0;
    const registerLinks = await page.$$eval('a', (as) => as.filter((a) => /注册|register|sign\s*up/i.test((a.innerText || ''))).length);
    await page.screenshot({ path: path.join(outDir, '01-home-anonymous.png'), fullPage: true });
    const anonNames = anonTabs.map((t) => norm(t.text));
    const anonMatch = matchDocumentedTabs(expect.anonTabs, anonNames);
    record('D5', 'anonymous home matches the document',
      anonMatch.allPresent && hasSigninCard && registerLinks === 0,
      { expectedAnonTabs: expect.anonTabs, observedTabs: anonNames, hasSigninCard, registerLinks, ...anonMatch });

    // <lang><zh-CN>失败登录与成功登录分用独立 context：一次性口令公钥与 __EVENTVALIDATION 会污染表单状态。</zh-CN>
    // <en>Separate contexts for failed and successful sign-in: one-time password keys and __EVENTVALIDATION taint form state.</en></lang>
    const failCtx = await newChineseContext(browser, ctx.baseUrl);
    const failPage = await failCtx.newPage();
    await failPage.goto(ctx.baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await failPage.waitForTimeout(1000);
    const failName = failPage.locator('input[id$="EmailOrName"]');
    if (await failName.count()) {
      await failName.fill(ctx.adminUserName);
      await failPage.locator('input[id$="password"]').fill('definitely-not-the-password-2026');
      await failPage.locator('input[id$="SigninBtn"]').click();
      await failPage.waitForTimeout(2500);
    }
    const failText = norm(await failPage.evaluate(() => document.body.innerText || ''));
    await failPage.screenshot({ path: path.join(outDir, '02-signin-wrong-password.png'), fullPage: true });
    const failed = /登录失败|invalid|failed|incorrect|无效|错误/i.test(failText);
    const discloses = /账号不存在|用户不存在|user\s+not\s+found|unknown\s+account/i.test(failText);
    record('D6', 'wrong password fails without disclosing account existence',
      failed && !discloses, { failureHintFound: failed, accountDisclosureFound: discloses });
    await failCtx.close();

    const okCtx = await newChineseContext(browser, ctx.baseUrl);
    const okPage = await okCtx.newPage();
    await okPage.goto(ctx.baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await okPage.waitForTimeout(1000);
    const okName = okPage.locator('input[id$="EmailOrName"]');
    if (await okName.count()) {
      await okName.fill(ctx.adminUserName);
      await okPage.locator('input[id$="password"]').fill(ctx.password);
      await okPage.locator('input[id$="SigninBtn"]').click();
      await okPage.waitForTimeout(3000);
    }
    const signedTabs = await collectTabs(okPage);
    const signedText = norm(await okPage.evaluate(() => document.body.innerText || ''));
    await okPage.screenshot({ path: path.join(outDir, '03-home-signed-in.png'), fullPage: true });
    const signedNames = signedTabs.map((t) => norm(t.text));
    const welcomeFound = /欢迎|welcome/i.test(signedText);
    // <lang><zh-CN>文档已明说管理员会多两项，故这两项属「已文档化的额外页签」；
    // 其余（如可选独立模块的 Form Builder）才是真正未覆盖的增量，透明登记为欠账。</zh-CN>
    // <en>The document already states administrators get two more tabs; only the remaining extras (e.g. Form Builder
    // from an optional independent module) are genuinely undocumented and reported as a gap.</en></lang>
    const documentedExtras = ['平台管理', '企业能力工作台'];
    const signedMatch = matchDocumentedTabs(expect.signedTabs, signedNames, documentedExtras);
    record('D7', 'signed-in welcome and documented tab set match the document',
      welcomeFound && signedMatch.allPresent,
      { expectedSignedTabs: expect.signedTabs, observedTabs: signedNames, welcomeFound, ...signedMatch });
    summary.observations = {
      groupedRendering: !signedMatch.inOrder,
      undocumentedExtraTabs: signedMatch.undocumentedExtraTabs,
      note: 'Grouped tab rendering and optional-module tabs are reported, not silently ignored.'
    };

    const workTab = signedTabs.find((t) => /待办|work\s*item/i.test(t.text));
    let reachable = false;
    let workTextSample = '';
    if (workTab && workTab.href) {
      const target = new URL(workTab.href, ctx.baseUrl).href;
      await okPage.goto(target, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await okPage.waitForTimeout(2000);
      workTextSample = norm(await okPage.evaluate(() => document.body.innerText || '')).slice(0, 300);
      reachable = (await okPage.locator('body').count()) > 0 && !/error|异常|错误页/i.test(workTextSample.slice(0, 80));
      await okPage.screenshot({ path: path.join(outDir, '04-my-work-items.png'), fullPage: true });
    }
    record('D8', 'my work items tab is reachable after sign-in', reachable,
      { tabFound: !!workTab, reachable, workTextSample });
    await okCtx.close();
  } finally {
    await context.close();
    await browser.close();
  }
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  checkChain();
  checkImages();
  checkExamplePaths();
  checkQuickStartSteps();

  if (staticOnly) {
    summary.status = summary.checks.every((c) => c.status === 'Pass') ? 'Pass' : 'Fail';
    summary.note = 'static-only mode: runtime assertions D4-D8 were not executed';
    fs.writeFileSync(path.join(outDir, 'walkthrough-summary.json'), JSON.stringify(summary, null, 2), 'utf8');
    console.log('[STATIC-ONLY] ' + summary.status);
    process.exit(summary.status === 'Pass' ? 0 : 1);
  }

  let ctx = null;
  if (fs.existsSync(contextPath)) {
    try {
      const raw = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
      ctx = {
        baseUrl: process.env.PORTAL_BASE_URL || raw.baseUrl || 'http://localhost:40001/',
        adminUserName: raw.adminUserName,
        password: raw.password
      };
    } catch (e) {
      summary.prerequisites.context = 'parse error: ' + e.message;
    }
  } else {
    summary.prerequisites.context = 'missing: ' + contextPath;
  }
  summary.prerequisites.contextAvailable = !!(ctx && ctx.adminUserName && ctx.password);
  summary.prerequisites.baseUrl = ctx ? ctx.baseUrl : null;
  // <lang><zh-CN>摘要只记录凭据是否可用，绝不写入账号名或口令。</zh-CN>
  // <en>The summary records only credential availability, never the account name or password.</en></lang>

  let siteUp = false;
  if (ctx && ctx.baseUrl) {
    try {
      const res = await fetch(ctx.baseUrl, { signal: AbortSignal.timeout(6000) });
      siteUp = res.ok;
      summary.prerequisites.siteHttpStatus = res.status;
    } catch (e) {
      summary.prerequisites.siteError = String(e.message || e);
    }
  }
  summary.prerequisites.siteUp = siteUp;

  let playwrightOk = false;
  try { await loadChromium(); playwrightOk = true; }
  catch (e) { summary.prerequisites.playwrightError = String(e.message || e); }
  summary.prerequisites.playwrightOk = playwrightOk;

  const runtimeReady = summary.prerequisites.contextAvailable && siteUp && playwrightOk;
  if (!runtimeReady) {
    summary.status = 'Incomplete';
    summary.note = 'runtime prerequisites missing; static results retained, runtime assertions not executed (exit 2)';
    fs.writeFileSync(path.join(outDir, 'walkthrough-summary.json'), JSON.stringify(summary, null, 2), 'utf8');
    console.error('[PREREQ-MISSING] ' + JSON.stringify(summary.prerequisites));
    process.exit(2);
  }

  await runRuntime(ctx);
  fs.writeFileSync(path.join(outDir, 'walkthrough-summary.json'), JSON.stringify(summary, null, 2), 'utf8');
  console.log('RESULT: ' + summary.status + ' -> ' + path.join(outDir, 'walkthrough-summary.json'));
  process.exit(summary.status === 'Pass' ? 0 : 1);
}

main().catch((error) => {
  summary.status = 'Fail';
  summary.error = String(error && error.stack ? error.stack : error);
  try { fs.writeFileSync(path.join(outDir, 'walkthrough-summary.json'), JSON.stringify(summary, null, 2), 'utf8'); } catch { /* ignore */ }
  console.error(error);
  process.exit(1);
});
