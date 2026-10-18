// <lang>
//   <zh-CN>默认主题（Default 回退主题）视觉回归基线门禁（L2，需运行站点 + playwright + 临时主题 fixture）。
//
//   为什么必须有它：`C-anp-P24` 裁定的残余项——"`Default` 回退主题纳入视觉回归（补 Default 规则的运行期证据）"。
//   既有 `C-anp-P18/W101` 出过 258 张跨皮肤截图矩阵，但那是**人工复核的截图证据，不是机器判据**；
//   收口亦建议"截图由全量入库转为按需生成 + 摘要入库"。故本门禁把视觉回归固化成**可重复比较的结构化指标**，
//   不入库任何图片：每页采集关键元素的**计算样式**（由 CSS 决定，跨机器确定）与**几何**（带容差），
//   与基线图 `dev/default-theme-visual-baseline.json` 比对。
//
//   前置与生效条件：主题只能由「Tab 覆盖 → 数据库运行设置 → appSettings → Default」决定
//   （`PortalThemeResolver` 明确不接受 URL 覆盖），因此本门禁依赖 fixture
//   `New-PortalDefaultThemeFixture.ps1` 把主题临时切到 `Default` 并在结束后恢复；
//   A0 断言每页 body 必须带 `portal-theme-default` class —— fixture 未生效即失败，不假装通过。
//
//   断言：
//     A0 每个目标页 body 必须带 `portal-theme-default` class（证明当前渲染确为 Default 回退主题）；
//     A1 基线图必须存在（缺失即失败，并把本次观测写入 temp 供人工固化，不自动放行）；
//     A2 每个探针：元素存在性与基线一致；计算样式值逐项相等；几何值在容差内（默认 8px，env 可覆盖）；
//     A3 default-deny：本次观测到但基线未登记的探针 → 失败（新增探针必须显式固化）。
//
//   软指标：Default.css 中 `body.portal-theme-default` 作用域规则总数与「本次采样页上有元素命中」
//   的规则数（即 Default 规则的运行期证据覆盖面）——只上报不阻断，避免页面内容正常变化造成抖动。
//
//   证明边界：只证明"采样页上关键元素的 Default 主题计算样式与几何未漂移"；
//   不证明像素级外观（不比对截图，字体/抗锯齿/DPI 会造成不可靠噪声）、
//   不证明全部 Default 规则都有运行期证据（未命中的规则如实列为软指标，需扩展采样页覆盖）。
//
//   前置：IIS Express 运行中、temp\p65\p65-acceptance-context.json（baseUrl/管理员账号）、主题 fixture 已 Apply
//   —— 缺失时显式 exit 2（W-anp-P139 口径：环境前置缺失 ≠ 代码缺陷）；playwright 由编排层统一预检。
//
//   用法（仓库根目录）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Apply
//     & $node 'dev\scripts\Test-PortalDefaultThemeVisualBaseline.mjs'
//     & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Remove
//   固化基线：PORTAL_VISUAL_CAPTURE=1 时把本次观测写入 temp\p149\observed-baseline.json（人工核对后入库）。
//   产物：temp\p149\visual-baseline-summary.json；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>Default-theme (fallback theme) visual regression baseline gate (L2; needs the running site, playwright and the temporary theme fixture).
//
//   Why it is required: the residual item ruled by `C-anp-P24` — "bring the `Default` fallback theme into visual regression (add
//   runtime evidence for the Default rules)". `C-anp-P18/W101` produced a 258-shot cross-skin screenshot matrix, but that is
//   **human-reviewed screenshot evidence, not a machine criterion**; its closeout also recommended moving from "commit every
//   screenshot" to "generate on demand + commit a summary". This gate therefore turns visual regression into **repeatable
//   structured metrics** with no image committed: per page it captures key elements' **computed styles** (CSS-determined, stable
//   across machines) and **geometry** (with tolerance), and compares them against the baseline
//   `dev/default-theme-visual-baseline.json`.
//
//   Prerequisites and effect: a theme is selected only by "tab override → database runtime setting → appSettings → Default"
//   (`PortalThemeResolver` explicitly does not accept URL overrides), so this gate relies on the fixture
//   `New-PortalDefaultThemeFixture.ps1` to switch the theme to `Default` temporarily and restore it afterwards; A0 asserts that
//   every page's body carries the `portal-theme-default` class — if the fixture did not take effect the gate fails rather than
//   pretending to pass.
//
//   Assertions:
//     A0 every target page's body must carry the `portal-theme-default` class (proving the render really is the Default fallback theme);
//     A1 the baseline must exist (missing fails, and the observation is written to temp for deliberate固化 instead of auto-passing);
//     A2 per probe: element presence matches the baseline; computed style values match exactly; geometry within tolerance
//        (default 8px, env-overridable);
//     A3 default-deny: a probe observed now but unregistered in the baseline fails (new probes must be固化 explicitly).
//
//   Soft metric: the number of `body.portal-theme-default`-scoped rules in Default.css and how many of them match at least one
//   element on the sampled pages (i.e. the runtime-evidence coverage of Default rules) — reported, not blocking, so ordinary
//   content changes do not cause flakiness.
//
//   Proof boundary: proves only that "the Default computed styles and geometry of key elements on the sampled pages have not
//   drifted"; does not prove pixel-level appearance (no screenshot diffing: fonts/anti-aliasing/DPI produce unreliable noise),
//   nor that every Default rule has runtime evidence (unmatched rules are listed honestly as a soft metric; covering them means
//   extending the sampled pages).
//
//   Prerequisites: IIS Express running, temp\p65\p65-acceptance-context.json (baseUrl/admin account), theme fixture applied —
//   missing means explicit exit 2 (the W-anp-P139 convention: a missing environment prerequisite is not a code defect);
//   playwright is pre-checked by the orchestrator.
//
//   Usage (repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Apply
//     & $node 'dev\scripts\Test-PortalDefaultThemeVisualBaseline.mjs'
//     & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Remove
//   Baselines: PORTAL_VISUAL_CAPTURE=1 writes the observation to temp\p149\observed-baseline.json for review before committing.
//   Output: temp\p149\visual-baseline-summary.json; exits 1 when any assertion fails.
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang><zh-CN>解析 playwright 模块，与其它运行期门禁同口径。</zh-CN>
/// <en>Resolves the playwright module, same convention as the other runtime gates.</en></lang>
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
const outputDir = path.join(repoRoot, 'temp', 'p149');
fs.mkdirSync(outputDir, { recursive: true });

// <lang><zh-CN>环境前置：验收上下文缺失 → 显式 exit 2（W-anp-P139 口径），不与代码缺陷（exit 1）混报。</zh-CN>
// <en>Environment prerequisite: a missing acceptance context exits 2 explicitly (the W-anp-P139 convention), never conflated with a code defect.</en></lang>
const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
if (!fs.existsSync(contextPath)) {
  console.error('[PREREQ-MISSING] 验收上下文不存在：' + contextPath + '（环境前置缺失，exit 2）');
  process.exit(2);
}
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const base = String(context.baseUrl).replace(/\/+$/, '');

const baselinePath = path.join(repoRoot, 'dev', 'default-theme-visual-baseline.json');
const capture = process.env.PORTAL_VISUAL_CAPTURE === '1';
const geometryTolerance = Number(process.env.PORTAL_VISUAL_GEOM_TOLERANCE || 8);

// <lang><zh-CN>采样页：与性能基线门禁同源的页签（tabid 为授权真源）。</zh-CN>
// <en>Sampled pages: the same tabs as the performance baseline gate (tabid is the authorization source).</en></lang>
// <lang><zh-CN>第 5 页（tabid=1101 请假报销）是调研实测的结果：`Default.css` 中 95 条 `body.portal-theme-default`
// 作用域规则**全部指向业务模块/编辑页专属 class**，前四页命中 0 条（指标空转）；该页实测命中 23/95，
// 使"Default 规则运行期证据"成为有覆盖的真实证据而非"通过却什么都没断言"。</zh-CN>
// <en>The fifth page (tabid=1101, leave/expense) comes from measurement: all 95 `body.portal-theme-default`-scoped rules in
// `Default.css` target business-module/editor-only classes, and the first four pages matched none of them (a vacuous metric);
// this page matches 23/95, making "Default rule runtime evidence" real coverage instead of "passing while asserting nothing".</en></lang>
const targets = [
  { id: 'home', url: base + '/' },
  { id: 'workbench', url: base + '/DesktopDefault.aspx?tabindex=9&tabid=1010' },
  { id: 'myworkitems', url: base + '/DesktopDefault.aspx?tabindex=10&tabid=1011' },
  { id: 'adminTab', url: base + '/DesktopDefault.aspx?tabindex=9&tabid=6' },
  { id: 'leaveExpense', url: base + '/DesktopDefault.aspx?tabindex=990&tabid=1101' }
  // <lang><zh-CN>实测记录（P150 阶段）：曾追加员工资料确认（tabid=1008）与更正（tabid=1009）两页以扩大
  // Default 规则证据覆盖面，实测**两页命中均为 0/95**（这些页不渲染 Default 作用域规则所指向的 class），
  // 只增加运行耗时而无证据增量 → 已回退。保留此记录以免后续重复尝试。</zh-CN>
  // <en>Measurement note (P150 phase): the employee-profile confirmation (tabid=1008) and correction (tabid=1009) tabs were added
  // to widen Default rule evidence, but both measured **0/95** (they do not render the classes those Default-scoped rules target),
  // adding runtime cost with no evidence gain → reverted. Kept as a note so the attempt is not repeated blindly.</en></lang>
];

// <lang><zh-CN>探针：取 Default.css 定义的历史门户 class（portal 基础外观层），计算样式跨机器确定；几何带容差。</zh-CN>
// <en>Probes: legacy portal classes defined by Default.css (the base appearance layer); computed styles are machine-independent, geometry carries a tolerance.</en></lang>
const PROBES = [
  { id: 'body', selector: 'body', props: ['backgroundColor', 'color'] },
  { id: 'siteTitle', selector: '.SiteTitle', props: ['color', 'fontSize'] },
  { id: 'selectedTab', selector: '.SelectedTab', props: ['color', 'fontSize'] },
  { id: 'otherTabs', selector: '.OtherTabs', props: ['color'] },
  { id: 'head', selector: '.Head', props: ['backgroundColor', 'color'], geometry: true },
  { id: 'normal', selector: '.Normal', props: ['color'] },
  { id: 'tabBg', selector: '.TabBg', props: ['backgroundColor'] },
  { id: 'headBg', selector: '.HeadBg', props: ['backgroundColor'] }
];

const TRANSIENT_NAV_ERRORS = ['ERR_ABORTED', 'ERR_CONNECTION_RESET', 'ERR_EMPTY_RESPONSE', 'ERR_CONNECTION_REFUSED'];
async function gotoWithRetry(page, url, attempts = 4) {
  let lastError = null;
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await page.goto(url, { waitUntil: 'load', timeout: 60000 });
    } catch (error) {
      lastError = error;
      const message = error instanceof Error ? error.message : String(error);
      const transient = TRANSIENT_NAV_ERRORS.some((code) => message.includes(code));
      if (!transient || attempt === attempts) throw error;
      await page.waitForTimeout(1000 * attempt);
    }
  }
  throw lastError;
}

const failures = [];
const softNotes = [];
const observed = { version: 1, generatedUtc: new Date().toISOString(), pages: {}, defaultRuleEvidence: {} };
const baselineExists = fs.existsSync(baselinePath);
const baseline = baselineExists ? JSON.parse(fs.readFileSync(baselinePath, 'utf8')) : null;

const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
try {
  const browserContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'zh-CN' });
  const page = await browserContext.newPage();
  page.setDefaultTimeout(40000);

  // <lang><zh-CN>登录一次（后续全部采集在管理员会话内进行，保证后台页与业务页可见）。</zh-CN>
  // <en>Sign in once; all capture then runs inside the administrator session so admin and business pages stay visible.</en></lang>
  await gotoWithRetry(page, base + '/');
  await page.locator('input[id$="EmailOrName"]').fill(context.adminUserName);
  await page.locator('input[id$="password"]').fill(context.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded').catch(() => {}),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(1200);

  // <lang><zh-CN>预热：每个目标页**各自**预热一次再采集。教训（2026-10-19 实测）：只预热首页时，
  // myworkitems 冷访问首次渲染不出 `.Head`（基线记 absent，热访问记 present），且各页"Default 规则命中数"
  // 从冷访问的 0~2 变成热访问的 6~14——即**冷访问下模块内容尚未渲染完整**，用冷访问采基线必然漂移。
  // 故每页先访问一次填充 JIT/模块运行期缓存，第二次访问后才取值。</zh-CN>
  // <en>Warm-up: each target page is warmed up **individually** before capture. Lesson (measured 2026-10-19): with only the first
  // page warmed, a cold visit to myworkitems did not render `.Head` (baseline recorded absent, a warm visit records present), and
  // per-page "Default rule hit counts" moved from 0–2 (cold) to 6–14 (warm) — i.e. **module content is not fully rendered on a cold
  // visit**, so baselines captured cold inevitably drift. Each page is therefore visited once to prime JIT/module runtime caches,
  // and values are taken on the second visit.</en></lang>
  for (const target of targets) {
    await gotoWithRetry(page, target.url);
    await page.waitForLoadState('networkidle').catch(() => {});
    await gotoWithRetry(page, target.url);
    const collected = await page.evaluate((probeList) => {
      const bodyClass = document.body ? String(document.body.className || '') : '';
      const probes = {};
      for (const probe of probeList) {
        const element = document.querySelector(probe.selector);
        if (!element) {
          probes[probe.id] = { present: false };
          continue;
        }
        const style = window.getComputedStyle(element);
        const values = {};
        for (const prop of probe.props) values[prop] = String(style[prop] || '');
        const record = { present: true, values };
        if (probe.geometry) {
          const rect = element.getBoundingClientRect();
          record.geometry = { width: Math.round(rect.width), height: Math.round(rect.height) };
        }
        probes[probe.id] = record;
      }

      // <lang><zh-CN>软指标：统计 Default 主题作用域规则的"运行期证据覆盖面"（有多少规则在采样页上命中了元素）。</zh-CN>
      // <en>Soft metric: runtime-evidence coverage of Default-scoped rules (how many rules match an element on the sampled page).</en></lang>
      let total = 0;
      let withEvidence = 0;
      try {
        for (const sheet of Array.from(document.styleSheets)) {
          let rules;
          try {
            rules = Array.from(sheet.cssRules || []);
          } catch (error) {
            continue; // <lang><zh-CN>跨域样式表无法读 cssRules，跳过（不影响同源 Default.css 统计）。</zh-CN><en>Cross-origin sheets cannot be read; skip (Default.css is same-origin).</en></lang>
          }
          // <lang><zh-CN>递归遍历：@media / @supports 内还有嵌套规则，扁平遍历会把它们漏掉
          // （实测分母 95 低于 Default.css 静态 111，即为此因）。</zh-CN>
          // <en>Recursive walk: @media / @supports hold nested rules, and a flat walk misses them (measured denominator 95 vs
          // the static 111 in Default.css — this was the cause).</en></lang>
          const walk = (list) => {
            for (const rule of list) {
              if (rule.cssRules && rule.cssRules.length > 0) {
                walk(Array.from(rule.cssRules));
                continue;
              }
              const selectorText = rule && rule.selectorText ? String(rule.selectorText) : '';
              if (!selectorText.includes('portal-theme-default')) continue;
              total += 1;
              try {
                if (document.querySelectorAll(selectorText).length > 0) withEvidence += 1;
              } catch (error) {
                // <lang><zh-CN>选择器语法当前浏览器不支持 → 不计入命中。</zh-CN><en>Unsupported selector syntax: not counted as a hit.</en></lang>
              }
            }
          };
          walk(rules);
        }
      } catch (error) {
        // <lang><zh-CN>样式表遍历整体失败时保留已统计值，不阻断。</zh-CN><en>If the sheet walk fails entirely, keep what was counted and continue.</en></lang>
      }
      return { bodyClass, probes, ruleEvidence: { total, withEvidence } };
    }, PROBES);

    observed.pages[target.id] = { probes: collected.probes };
    observed.defaultRuleEvidence[target.id] = collected.ruleEvidence;

    // <lang><zh-CN>A0：fixture 生效核验——当前渲染必须确为 Default 回退主题。</zh-CN>
    // <en>A0: fixture-effect check — the render must really be the Default fallback theme.</en></lang>
    if (!/\bportal-theme-default\b/.test(collected.bodyClass)) {
      failures.push(`A0 [${target.id}] body class 不含 portal-theme-default（实际："${collected.bodyClass}"）——主题 fixture 未生效，本次采集无意义。`);
    }

    if (!baselineExists) continue;

    const pageBaseline = baseline.pages ? baseline.pages[target.id] : null;
    if (!pageBaseline) {
      failures.push(`A1 [${target.id}] 基线图缺少该页（新增采样页必须显式固化基线）。`);
      continue;
    }
    for (const [probeId, record] of Object.entries(collected.probes)) {
      const expected = pageBaseline.probes ? pageBaseline.probes[probeId] : null;
      if (!expected) {
        failures.push(`A3 [${target.id}/${probeId}] 观测到未登记的探针（新增探针必须固化进基线图）。`);
        continue;
      }
      // <lang><zh-CN>存在性断言是不对称的（2026-10-19 实测依据）：基线存在而本次消失 = 布局/结构回归 → 失败；
      // 基线不存在而本次出现 = 数据状态变化（如编排运行到本门禁时其它门禁的 fixture 数据仍在，
      // myworkitems 的 `.Head` 即随待办数据有无而出现）→ 只作软指标，不阻断。
      // 反向对称断言会把"数据更多"误判为缺陷，属假阳性。</zh-CN>
      // <en>The presence assertion is asymmetric (measured basis, 2026-10-19): present in the baseline but missing now is a
      // layout/structure regression → fail; absent in the baseline but present now is a data-state variation (for example, when
      // the suite reaches this gate another gate's fixture data may still be present, and myworkitems' `.Head` appears only when
      // work items exist) → soft metric only. A symmetric assertion would misread "more data" as a defect — a false positive.</en></lang>
      if (Boolean(expected.present) !== Boolean(record.present)) {
        if (expected.present) {
          failures.push(`A2 [${target.id}/${probeId}] 元素消失：基线存在 → 实际不存在（布局/结构回归）。`);
        }
        else {
          softNotes.push(`[soft] ${target.id}/${probeId} 元素本次出现（基线不存在）——数据状态变化，非视觉回归。`);
        }
        continue;
      }
      if (!record.present) continue;
      for (const [prop, value] of Object.entries(record.values)) {
        const expectedValue = expected.values ? expected.values[prop] : undefined;
        if (expectedValue === undefined) {
          failures.push(`A3 [${target.id}/${probeId}] 属性 ${prop} 未在基线登记。`);
          continue;
        }
        if (String(expectedValue) !== String(value)) {
          failures.push(`A2 [${target.id}/${probeId}] ${prop} 漂移：基线 ${expectedValue} → 实际 ${value}。`);
        }
      }
      if (record.geometry) {
        const expectedGeometry = expected.geometry || {};
        for (const axis of ['width', 'height']) {
          const expectedValue = Number(expectedGeometry[axis]);
          const actualValue = Number(record.geometry[axis]);
          if (!Number.isFinite(expectedValue) || !Number.isFinite(actualValue)) {
            failures.push(`A3 [${target.id}/${probeId}] 几何 ${axis} 未在基线登记为数值。`);
            continue;
          }
          if (Math.abs(expectedValue - actualValue) > geometryTolerance) {
            failures.push(`A2 [${target.id}/${probeId}] 几何 ${axis} 漂移：基线 ${expectedValue}px → 实际 ${actualValue}px（容差 ${geometryTolerance}px）。`);
          }
        }
      }
    }
  }
}
finally {
  await browser.close();
}

// <lang><zh-CN>观测落盘：基线缺失或显式采集时写入 temp，供人工核对后固化（不自动放行）。</zh-CN>
// <en>Observation output: written to temp when the baseline is missing or capture is requested — for review before固化 (never auto-pass).</en></lang>
if (capture || !baselineExists) {
  fs.writeFileSync(path.join(outputDir, 'observed-baseline.json'), JSON.stringify(observed, null, 2), 'utf8');
}
const summary = {
  generatedUtc: new Date().toISOString(),
  baselineExists,
  geometryTolerance,
  observed,
  softNotes,
  failures
};
fs.writeFileSync(path.join(outputDir, 'visual-baseline-summary.json'), JSON.stringify(summary, null, 2), 'utf8');
console.log('统计已写入：' + path.join(outputDir, 'visual-baseline-summary.json'));

if (!baselineExists) {
  console.error('[FAIL] 基线图不存在：' + baselinePath + '（本次观测已写入 temp\\p149\\observed-baseline.json，人工核对后固化；不自动放行）');
  process.exit(1);
}

const ruleEvidenceEntries = Object.entries(observed.defaultRuleEvidence || {});
if (ruleEvidenceEntries.length > 0) {
  console.log('Default 规则运行期证据（软指标）：');
  for (const [pageId, evidence] of ruleEvidenceEntries) {
    console.log(`  ${pageId}: ${evidence.withEvidence}/${evidence.total} 条 portal-theme-default 规则命中元素`);
  }
}

if (softNotes.length > 0) {
  console.log(`软指标 ${softNotes.length} 条（不阻断）：`);
  for (const note of softNotes) console.log('  - ' + note);
}

if (failures.length > 0) {
  console.error(`[FAIL] 默认主题视觉基线校验失败 ${failures.length} 处：`);
  for (const failure of failures) console.error('  - ' + failure);
  process.exit(1);
}

console.log('[PASS] 默认主题视觉基线一致（计算样式 + 几何均在基线/容差内）。');
process.exit(0);
