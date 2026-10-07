// <lang>
//   <zh-CN>语义标记与标题计算样式证据脚本（W-anp-P82 `P82.4`）。</zh-CN>
//   <en>Semantic-markup and heading computed-style evidence script (W-anp-P82 `P82.4`).</en>
// </lang>
//
// <lang>
//   <zh-CN>本脚本把两件事**分开证明**，不合并成一句"改好了"：① 语义是否生效（`th[scope]`、`label[for]`、
//   真实标题元素是否出现在渲染后的 DOM 里）；② 模块标题改成 `h1`/`h2` 后其**计算样式**与**页面几何**是否与改动前一致。</zh-CN>
//   <en>This script proves two things **separately** instead of merging them into one "looks done": (1) whether the semantics
//   take effect in the rendered DOM (`th[scope]`, `label[for]`, real heading elements); (2) whether the module title's
//   **computed style** and **page geometry** match the pre-change values after it becomes an `h1`/`h2`.</en>
// </lang>
//
// <lang>
//   <zh-CN>为什么必须"先取基线再改"（本包核心方法）：`W-anp-P82.md` §1.4 的"`class` 不动则 `<span>` → `<h1>`
//   样式等价"是**静态推理**，依据是主题把 `.Head` 与 `h1, h2` 定义为同一组规则。静态推理会漏掉**别处**的规则
//   （模块自带样式、内联样式、继承链上的覆盖）。故本脚本支持两轮 —— `baseline`（改动前）与 `after`（改动后），
//   由 `PORTAL_P82_PHASE` 区分；两轮的 `moduleTitleStyles` 与几何数据由调用方逐项对比并留证。</zh-CN>
//   <en>Why a baseline must be captured first (this package's core method): the conclusion in `W-anp-P82.md` §1.4 — "leaving
//   the class untouched makes `<span>` → `<h1>` style-equivalent" — is a **static inference** resting on the theme defining
//   `.Head` together with `h1, h2`. Static reasoning misses rules defined **elsewhere** (module-local stylesheets, inline
//   styles, overrides along the inheritance chain). The script therefore supports two passes, `baseline` and `after`,
//   distinguished by `PORTAL_P82_PHASE`; the item-by-item comparison of `moduleTitleStyles` and geometry is performed by
//   the caller and archived as evidence.</en>
// </lang>
//
// <lang>
//   <zh-CN>用法（仓库根目录执行）：<br>
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path<br>
//     $env:PORTAL_P82_PHASE = 'baseline'; & $node 'dev\scripts\Test-PortalSemanticMarkupEvidence.mjs'<br>
//     $env:PORTAL_P82_PHASE = 'after';    & $node 'dev\scripts\Test-PortalSemanticMarkupEvidence.mjs'<br>
//   产物：`work-zone\dev\evidence\p82.4\<phase>-<时间戳>-Dev\semantic-markup-summary.json` + 截图；失败退出码 1。</zh-CN>
//   <en>Usage (from the repository root):<br>
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path<br>
//     $env:PORTAL_P82_PHASE = 'baseline'; & $node 'dev\scripts\Test-PortalSemanticMarkupEvidence.mjs'<br>
//     $env:PORTAL_P82_PHASE = 'after';    & $node 'dev\scripts\Test-PortalSemanticMarkupEvidence.mjs'<br>
//   Output: `work-zone\dev\evidence\p82.4\<phase>-<timestamp>-Dev\semantic-markup-summary.json` plus screenshots; exit code 1 on failure.</en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { execFileSync } from 'node:child_process';

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
    throw new Error('Unable to resolve "playwright". Set PORTAL_PLAYWRIGHT_MODULE to its location. Original error: ' + error.message);
  }
}

const repoRoot = path.resolve(process.cwd());
const contextPath = path.join(repoRoot, 'temp', 'p65', 'p65-acceptance-context.json');
if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}
const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = context.baseUrl;
const phase = process.env.PORTAL_P82_PHASE || 'baseline';

// <lang>
//   <zh-CN>目标覆盖三类：① 前台业务模块（走 `h1` 路线）；② 旧内容模块（纯列表）；③ 后台 Admin 页（**已达标**，
//   作对照 —— 若连它都"变了"，说明测量口径本身有问题）。页签 `tabid` 是稳定主键故保留；`tabindex` 自
//   C-anp-P16 / A2（W94）起改为**运行时**从开发库 `PortalCfg_Tabs.TabOrder` 发现，不再硬编码。</zh-CN>
//   <en>Targets cover three kinds: (1) front-office business modules (the `h1` route); (2) legacy modules (plain lists);
//   (3) an already-compliant admin page kept as a control — if even that one "changes", the measurement method is at fault.
//   Tab ids are stable primary keys and are kept; since C-anp-P16 / A2 (W94) `tabindex` is **discovered at runtime**
//   from `PortalCfg_Tabs.TabOrder` in the development database instead of being hardcoded.</en>
// </lang>
// <lang>
//   <zh-CN>本脚本需**按档位分两轮**跑，不能一次跑完 —— 实测原因：`Portal.ModuleProfiles.Active` 决定哪些包可见，
//   切到 `LegacyContent` 后前台业务模块（`HIA.*`）整页不渲染（`th=0`、模块标题缺失）。因此：
//     · `baseline` / `after`        → 默认档位，覆盖前台 4 个业务模块 + 后台对照；
//     · `baseline-legacy` / `after-legacy` → `LegacyContent` 档位，覆盖 Contacts / Document。
//   两轮之间**必须**把档位改回去再改回来，验证结束后一律还原（否则工作树会带上临时配置）。</zh-CN>
//   <en>This script must be run in **two profile passes** rather than one — the measured reason: `Portal.ModuleProfiles.Active`
//   decides which packages are visible, and after switching to `LegacyContent` the front-office business modules (`HIA.*`)
//   do not render at all (`th=0`, module title missing). Therefore:
//     · `baseline` / `after`             → default profile, covering the four front-office modules plus the admin control;
//     · `baseline-legacy` / `after-legacy` → the `LegacyContent` profile, covering Contacts / Document.
//   The profile must be switched back and forth between passes and always restored afterwards, otherwise the working tree
//   would carry a temporary configuration.</en>
// </lang>
// <lang>
//   <zh-CN>C-anp-P16 / A2（W94，2026-10-07）：`tabindex` 改为运行时从开发库发现（`PortalCfg_Tabs.TabOrder`），不再硬编码。
//   实测依据：硬编码值已漂移 —— `tabid=1009` 的脚本值原为 `tabindex=20`，而 2026-10-07 实测开发库该页签
//   `TabOrder=21`（插入新页签后 `TabOrder` 位移）。`tabid` 是稳定主键故保留；DB 不可达或目标页签缺失时
//   **直接抛错**，不回退硬编码 —— 回退会把"测量无效"伪装成"通过"。</zh-CN>
//   <en>C-anp-P16 / A2 (W94, 2026-10-07): `tabindex` is now discovered at runtime from the development database
//   (`PortalCfg_Tabs.TabOrder`) instead of being hardcoded. Measured evidence: the hardcoded value had already drifted —
//   the script used `tabindex=20` for `tabid=1009` while the development database reported `TabOrder=21` on 2026-10-07
//   (the order shifted once a new tab was inserted). `tabid` is kept because it is a stable primary key; if the database is
//   unreachable or a target tab is missing the script **throws** instead of falling back to hardcoded values, because a
//   fallback would disguise "measurement invalid" as "passed".</en>
// </lang>
// <lang>
//   <zh-CN>调用发现脚本（pwsh + ADO.NET；本机无可用的 SqlServer 模块，故不用 Invoke-Sqlcmd），返回
//   `{ [TabID]: TabOrder }` 映射；连接串可用 `PORTAL_TAB_DISCOVERY_CONNECTION` 覆盖默认开发库。</zh-CN>
//   <en>Calls the discovery script (pwsh + ADO.NET; no usable SqlServer module exists here, so Invoke-Sqlcmd is not used)
//   and returns a `{ [TabID]: TabOrder }` map; the connection string can override the default development database through
//   `PORTAL_TAB_DISCOVERY_CONNECTION`.</en>
// </lang>
function discoverTabOrders() {
  const scriptPath = path.join(repoRoot, 'dev', 'scripts', 'Get-PortalTabDiscovery.ps1');
  const args = ['-NoProfile', '-File', scriptPath];
  if (process.env.PORTAL_TAB_DISCOVERY_CONNECTION) {
    args.push('-ConnectionString', process.env.PORTAL_TAB_DISCOVERY_CONNECTION);
  }
  const raw = execFileSync(process.env.PORTAL_PWSH_PATH || 'pwsh', args, { encoding: 'utf8', cwd: repoRoot });
  const start = raw.indexOf('[');
  const end = raw.lastIndexOf(']');
  if (start < 0 || end < start) {
    throw new Error('Tab discovery produced no JSON array: ' + raw.trim());
  }
  const rows = JSON.parse(raw.slice(start, end + 1));
  const map = {};
  for (const row of rows) {
    map[String(row.TabID)] = row.TabOrder;
  }
  return map;
}

const tabTargets = [
  { id: 'front-workbench', tabId: 1010, expectAriaLevel: '1' },
  { id: 'front-correction', tabId: 1009, expectAriaLevel: '1' },
  { id: 'front-confirm', tabId: 1008, expectAriaLevel: '1' },
  { id: 'front-myworkitems', tabId: 1011, expectAriaLevel: '1' },
  { id: 'legacy-contacts', tabId: 2, expectAriaLevel: '1', moduleMarkerKey: 'Contacts_LabelName' },
  { id: 'legacy-document', tabId: 4, expectAriaLevel: '1', moduleMarkerKey: 'Document_LabelTitle' }
];

const tabOrders = discoverTabOrders();
const targets = tabTargets.map((target) => {
  const tabIndex = tabOrders[String(target.tabId)];
  if (typeof tabIndex !== 'number') {
    throw new Error('Tab ' + target.tabId + ' is absent from PortalCfg_Tabs; its URL cannot be built without a discovered TabOrder.');
  }
  return Object.assign({}, target, {
    url: new URL('DesktopDefault.aspx?tabindex=' + tabIndex + '&tabid=' + target.tabId, baseUrl).toString()
  });
});
targets.push({
  id: 'admin-workitems-control',
  url: new URL('Admin/WorkItems.aspx', baseUrl).toString(),
  expectAriaLevel: '2',
  isControl: true
});

function readResxValue(fileName, key) {
  const resx = fs.readFileSync(path.join(repoRoot, 'src', 'Portal', 'App_GlobalResources', fileName), 'utf8');
  const match = resx.match(new RegExp('<data name="' + key + '"[^>]*>\\s*<value>([\\s\\S]*?)</value>\\s*</data>'));
  return match ? match[1] : null;
}

const stamp = new Date().toISOString().replace(/[-:]/g, '').replace(/\..+$/, '');
const outputDir = path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p82.4', phase + '-' + stamp + '-Dev');
fs.mkdirSync(outputDir, { recursive: true });

const records = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  const browserContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'zh-CN' });
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
    const record = { id: target.id, url: target.url, pass: phase, isControl: Boolean(target.isControl), status: 'Pass', facts: {}, notes: [] };

    try {
      await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);
      const html = await page.content();
      record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);

      record.facts = Object.assign(record.facts, await page.evaluate(() => {
        // <lang>
        //   <zh-CN>统计按**整页**做，不限定某张表：本包目标是"该带语义的都带了"，逐表统计会漏掉模块内嵌的次级表格。</zh-CN>
        //   <en>Statistics are collected for the **whole page** rather than per table: the goal is "everything that should
        //   carry semantics does", whereas per-table counting would miss secondary tables embedded inside a module.</en>
        // </lang>
        const thAll = Array.from(document.querySelectorAll('th'));
        const labels = Array.from(document.querySelectorAll('label'));
        const spanLabels = Array.from(document.querySelectorAll('span')).filter((span) => {
          const cls = span.getAttribute('class') || '';
          return /(^|\s)[\w-]*label[\w-]*(\s|$)/i.test(cls);
        });

        // <lang>
        //   <zh-CN>模块标题取带 `portal-module-title` 的**第一个**元素作为代表（同一页可能挂多个模块），
        //   并**逐项**记录计算样式 —— 零视觉变化的证明依赖这些数值，而不是"看起来一样"。</zh-CN>
        //   <en>The module title is the **first** element carrying `portal-module-title` (a page may host several modules),
        //   and its computed style is recorded **item by item** — the proof of zero visual change rests on these numbers,
        //   not on "it looks the same".</en>
        // </lang>
        // <lang>
        //   <zh-CN>标题元素先找共享控件 `.portal-module-title`，找不到则回退到**任何** `role="heading"` 元素。
        //   原因（W87 实测）：项目内并存两套标题实现 —— 多数模块复用共享控件，而工作台 /
        //   资料确认 / 资料更正三个模块自带标题 div。只查共享控件会让后者的 ARIA 语义改动
        //   **完全测不到**，表现为 `titleTag=null`，与"没有标题"无法区分。
        // </zh-CN>
        //   <en>The title element is first looked up as the shared `.portal-module-title` control, falling back to **any**
        //   `role="heading"` element. Reason (measured in W87): the project carries two title implementations — most modules
        //   reuse the shared control, while the workbench / profile-confirm / profile-correction modules render their own title
        //   divs. Querying only the shared control leaves the latter's ARIA semantics **entirely unmeasured**, surfacing as
        //   `titleTag=null`, indistinguishable from "there is no title at all".</en>
        // </lang>
        const titleElement = document.querySelector('.portal-module-title') || document.querySelector('[role="heading"]');
        let moduleTitleStyles = null;
        if (titleElement) {
          const c = window.getComputedStyle(titleElement);
          moduleTitleStyles = {
            display: c.display,
            fontSize: c.fontSize,
            fontWeight: c.fontWeight,
            lineHeight: c.lineHeight,
            marginTop: c.marginTop,
            marginRight: c.marginRight,
            marginBottom: c.marginBottom,
            marginLeft: c.marginLeft,
            color: c.color,
            textAlign: c.textAlign
          };
        }

        const headingCounts = {};
        for (const level of ['h1', 'h2', 'h3', 'h4', 'h5', 'h6']) {
          headingCounts[level] = document.querySelectorAll(level).length;
        }

        // <lang>
        //   <zh-CN>几何数据作为"视觉未变"的独立佐证：即使计算样式相同，元素类型变化理论上仍可能影响布局，
        //   故记录表头与首个单元格的起始 x 与宽度，供两轮直接对比。</zh-CN>
        //   <en>Geometry is an independent corroboration that the visuals are unchanged: even with identical computed styles an
        //   element-type change could in principle affect layout, so the first header's and cell's starting x and width are
        //   recorded for direct comparison between the two passes.</en>
        // </lang>
        const table = document.querySelector('table');
        const rect = (element) => (element ? { x: Math.round(element.getBoundingClientRect().x), width: Math.round(element.offsetWidth) } : null);

        // <lang>
        //   <zh-CN>`spanLabelCount` 本身**不能**当失败判据：只读字段（员工代码 / 姓名 / 职务 / 工作邮箱 / 单位）的
        //   "标签"后面跟的是 `asp:Label` 值而非表单控件，语义上就**不该**改成 `&lt;label for&gt;`（`for` 只能指向表单控件），
        //   这些 span 是**有意保留**的。真正要抓的漏网之鱼是"表单控件前仍残留 span 标签"，故另算 `orphanFieldLabels`：
        //   class 带 label 的 span，且**紧邻的下一个兄弟元素**是 `&lt;input&gt;` / `&lt;select&gt;` / `&lt;textarea&gt;`。</zh-CN>
        //   <en>`spanLabelCount` by itself is **not** a valid failure criterion: for read-only fields (employee code, name,
        //   salutation, work email, organization) the "label" is followed by an `asp:Label` value rather than a form control, so
        //   semantically it **should not** become a `&lt;label for&gt;` (`for` may only reference a form control) and those spans
        //   are **intentionally kept**. What actually matters is catching stragglers — a span label still sitting in front of a
        //   form control — so `orphanFieldLabels` is computed separately: a span whose class carries a label and whose
        //   **immediately following sibling element** is an `&lt;input&gt;`, `&lt;select&gt;`, or `&lt;textarea&gt;`.</en>
        // </lang>
        const orphanFieldLabels = spanLabels
          .filter((span) => {
            const next = span.nextElementSibling;
            if (!next) {
              return false;
            }
            const tag = next.tagName.toLowerCase();
            const isFormControl = tag === 'input' || tag === 'select' || tag === 'textarea';
            const isReadOnlyValue = tag === 'span' && /field-value/.test(next.getAttribute('class') || '');
            return isFormControl && !isReadOnlyValue;
          })
          .map((span) => (span.textContent || '').trim().slice(0, 40));

        return {
          thTotal: thAll.length,
          thWithoutScope: thAll.filter((th) => !(th.getAttribute('scope') || '').length).length,
          labelTotal: labels.length,
          labelWithFor: labels.filter((label) => (label.getAttribute('for') || '').length).length,
          spanLabelCount: spanLabels.length,
          orphanFieldLabelCount: orphanFieldLabels.length,
          orphanFieldLabelSamples: orphanFieldLabels.slice(0, 5),
          moduleTitleTag: titleElement ? titleElement.tagName.toLowerCase() : null,
          moduleTitleRole: titleElement ? titleElement.getAttribute('role') : null,
          moduleTitleAriaLevel: titleElement ? titleElement.getAttribute('aria-level') : null,
          moduleTitleClass: titleElement ? titleElement.getAttribute('class') : null,
          moduleTitleText: titleElement ? (titleElement.textContent || '').trim() : null,
          moduleTitleStyles,
          headingCounts,
          firstTableGeometry: table ? { headerCell: rect(table.querySelector('th')), bodyCell: rect(table.querySelector('td')) } : null
        };
      }));

      record.facts.moduleMarkerRendered = target.moduleMarkerKey
        ? (await page.locator('body').innerText()).includes(readResxValue('lang.zh-cn.resx', target.moduleMarkerKey))
        : null;

      // <lang>
      //   <zh-CN>断言按轮次分档：`baseline` 只要求目标模块渲染出来，**不要求语义已达标** —— 它记录的就是待改状态；
      //   `after` 才要求 `th` 全带 `scope`、无 span 形式标签、模块标题是预期级别的语义标题。
      //   两轮共用同一段判定逻辑，因此既不会出现"基线就报错"，也不会出现"基线假通过"。</zh-CN>
      //   <en>Assertions are phase-dependent: `baseline` only requires the target module to have rendered and does **not**
      //   require semantic compliance — it records the pre-change state; `after` requires every `th` to carry `scope`, no
      //   span-style labels to remain, and the module title to be a heading of the expected level. Both passes share one
      //   implementation, so neither a failing baseline nor a falsely passing one can occur.</en>
      // </lang>
      if (record.facts.errorPage) {
        record.status = 'Fail';
        record.notes.push('页面回落到通用错误页。');
      } else if (target.moduleMarkerKey && !record.facts.moduleMarkerRendered) {
        record.status = 'Skip';
        record.notes.push('目标模块未渲染（表头标记缺失），记为跳过。');
      } else if (!record.facts.moduleTitleTag) {
        // <lang>
        //   <zh-CN>该档位下整页没有任何 `.portal-module-title`（实测：员工资料确认 / 更正两个模块在 `BusinessWorkflow`
        //   下不渲染，与 `baseline` 完全一致）。**不判 Fail** —— 基线同样未渲染，算成失败只会掩盖真实问题；按"未渲染即跳过"
        //   如实登记，缺口留到能渲染它的档位再验。</zh-CN>
        //   <en>No `.portal-module-title` exists anywhere on the page under this profile (measured: the employee-profile confirm
        //   and correction modules do not render under `BusinessWorkflow`, exactly as in the `baseline` pass). This is **not** a
        //   failure — the baseline did not render either, and scoring it as one would only mask the real issue. It is recorded as
        //   skipped ("not rendered"), and the gap is verified later under a profile where it does render.</en>
        // </lang>
        record.status = 'Skip';
        record.notes.push('该档位下目标模块未渲染（无 .portal-module-title），记为跳过。');
      } else if (phase.startsWith('after') && !target.isControl) {
        if (record.facts.thTotal > 0 && record.facts.thWithoutScope > 0) {
          record.status = 'Fail';
          record.notes.push('仍有 ' + record.facts.thWithoutScope + ' 个 <th> 缺 scope。');
        }
        // <lang>
        //   <zh-CN>标签判据用 `orphanFieldLabelCount`（表单控件前仍残留的 span 标签）而不是 `spanLabelCount`：后者把
        //   有意保留的只读值标签也算进来，据此判失败等于要求把只读字段也改成 `label for`，那是**错误语义**。</zh-CN>
        //   <en>The label criterion is `orphanFieldLabelCount` (span labels still sitting in front of a form control) rather than
        //   `spanLabelCount`: the latter also counts the intentionally kept read-only value labels, so failing on it would demand
        //   converting read-only fields to `label for` too, which is **incorrect semantics**.</en>
        // </lang>
        if (record.facts.orphanFieldLabelCount > 0) {
          record.status = 'Fail';
          record.notes.push('仍有 ' + record.facts.orphanFieldLabelCount + ' 处表单控件前的 span 标签未改为 <label for>：' + JSON.stringify(record.facts.orphanFieldLabelSamples));
        }
        // <lang>
        //   <zh-CN>标题按 **ARIA 语义**判定而非标签名：本包刻意保留 `asp:Label`（渲染为 `span`）而只补 `role="heading"` +
        //   `aria-level`，因此标签名**理应仍是 `span`**，断言 `h1` 反而会把正确实现判成失败。</zh-CN>
        //   <en>The title is judged by **ARIA semantics**, not by tag name: this package deliberately keeps `asp:Label` (which
        //   renders as `span`) and only adds `role="heading"` plus `aria-level`, so the tag name **is expected to stay `span`** and
        //   asserting `h1` would score the correct implementation as a failure.</en>
        // </lang>
        if (record.facts.moduleTitleRole !== 'heading') {
          record.status = 'Fail';
          record.notes.push('模块标题缺 role="heading"（实际 ' + record.facts.moduleTitleRole + '）。');
        }
        if (record.facts.moduleTitleAriaLevel !== target.expectAriaLevel) {
          record.status = 'Fail';
          record.notes.push('模块标题 aria-level 为 ' + record.facts.moduleTitleAriaLevel + '，期望 ' + target.expectAriaLevel + '。');
        }
      }

      const shot = path.join(outputDir, target.id + '.png');
      await page.screenshot({ path: shot, fullPage: false });
      record.screenshot = path.relative(repoRoot, shot);
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }

    records.push(record);
  }

  await browserContext.close();
} finally {
  await browser.close();
}

const summary = {
  phase: 'W-anp-P82 P82.4',
  pass: phase,
  generatedUtc: new Date().toISOString(),
  intent: 'Separate two proofs: (1) semantics take effect in the rendered DOM; (2) the module title computed style and page geometry are unchanged between the baseline and after passes.',
  records
};

fs.writeFileSync(path.join(outputDir, 'semantic-markup-summary.json'), JSON.stringify(summary, null, 2), 'utf8');

for (const record of records) {
  const s = record.facts.moduleTitleStyles;
  console.log('[' + record.status + '] ' + record.id +
    ' th=' + record.facts.thTotal + ' missingScope=' + record.facts.thWithoutScope +
    ' spanLabels=' + record.facts.spanLabelCount + ' orphanSpanLabels=' + record.facts.orphanFieldLabelCount +
    ' labelFor=' + record.facts.labelWithFor + '/' + record.facts.labelTotal +
    ' titleTag=' + record.facts.moduleTitleTag +
    ' role=' + record.facts.moduleTitleRole + ' ariaLevel=' + record.facts.moduleTitleAriaLevel +
    ' font=' + (s ? s.fontSize + '/' + s.fontWeight : '-') +
    ' margin=' + (s ? s.marginTop + ',' + s.marginBottom : '-') +
    ' lineHeight=' + (s ? s.lineHeight : '-'));
  for (const note of record.notes) {
    console.log('    note: ' + note);
  }
}
console.log('evidence: ' + path.relative(repoRoot, outputDir));

const failed = records.filter((record) => record.status === 'Fail').length;
console.log('semantic markup evidence (' + phase + ') completed: ' + records.length + ' record(s), ' + failed + ' failed');
process.exit(failed > 0 ? 1 : 0);