// <lang>
//   <zh-CN>协同事项后台布局运行期证据脚本（W-anp-P79 `P79.4`）：把 P79.3 的"末列拆三列 + 相对单位化"放到**运行中的站点**上断言，
//   并留档 100% / 200% 两套截图。
//
//   为什么必须运行期断言：本次改的是**列结构与宽度表达**，而 `src\Portal` 是 Web Site 项目 —— aspx 标记**不进编译**，
//   msbuild 看不到它；单测跑的是数据层（`ParticipantsText` 由 `PortalCollaborationParticipantText` 产出，与本轮布局无关）。
//   "9 列是否真的渲染成 9 个 `th`""三列内容是否各自独立""固定像素是否真的被相对宽度取代"都只能在真实渲染后测量。
//
//   缩放口径（本脚本显式声明，避免把"看起来没裁切"当成结论）——共三档，缺一不可：
//     100%    基线。
//     200%    `documentElement.style.zoom = 2`，等价**浏览器整体放大**：所有 CSS 长度（含文字）一起放大，可用视口宽度减半。
//              它比纯文字放大更严格，但**无法区分**"写死 px"与"相对宽度"——两者都会等比放大。
//     文字 200% `SC 1.4.4` 的真实场景（失败技术 F69 / F80）：**只有字号翻倍**，px 布局盒（含 `width=280` 这类固定宽度）保持不变。
//              这是唯一能让"写死像素"暴露失败的一档。实现上一次性遍历元素，读出计算字号再写回 2 倍的内联 px，
//              避免 `font-size: 200%` 沿 DOM 深度连乘。
//     判定"不裁切"的口径是逐个 `th` / `td` 比较 `scrollWidth` 与 `clientWidth`（允许 2px 舍入），即**内容必须重排而不是被切掉**；
//     表格整体横向滚动仍属 `SC 1.4.10` 对 data tables 明确允许的 2D 布局例外，不计为失败。
//     判定"相对宽度生效"的口径是 `fillRatio`（控件渲染宽度 ÷ 单元格内容宽度）应落在 0.95–1.05：
//     写死 `width="280"` 时该比值必然 >1（控件比列宽、直接溢出单元格）。
//
//   前置条件：
//     1) IIS Express 已启动（`dev\scripts\Start-IISExpress.ps1`，默认 40001）且服务当前工作树 `src\Portal`；
//     2) 外置连接串已配置（`%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`）；
//     3) 验收上下文 `temp\p65\p65-acceptance-context.json` 存在（提供 baseUrl / 管理员账号 / 口令，口令只在进程内使用）；
//     4) playwright 可解析 —— 开发期依赖且不入库（本机位于 `temp\node_modules\playwright`），执行前须设 `PORTAL_PLAYWRIGHT_MODULE`。
//
//   用法（在仓库根目录执行）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalCollaborationLayoutEvidence.mjs'
//   产物：`work-zone\dev\evidence\p79.4\<时间戳>-Dev\*.png` 与 `layout-summary.json`；任一断言失败以退出码 1 结束。
//   </zh-CN>
//   <en>Collaboration-items admin layout runtime evidence script (W-anp-P79 `P79.4`): asserts the P79.3 "split the last cell into three columns plus relative units" change against the **running site** and keeps screenshots at 100% and 200%.
//
//   Why runtime assertions are mandatory: this round changes **column structure and width expression**, and `src\Portal` is a Web Site project — aspx markup is **not compiled in**, so msbuild never sees it; unit tests exercise the data layer (`ParticipantsText` produced by `PortalCollaborationParticipantText`), which is unrelated to this layout change. Whether nine columns really render as nine `th` elements, whether the three columns hold independent content, and whether the fixed pixel widths were really replaced can only be measured after real rendering.
//
//   Zoom semantics (declared explicitly so "it does not look clipped" is never mistaken for a conclusion) — three passes, none redundant:
//     100%      baseline.
//     200%      `documentElement.style.zoom = 2`, equivalent to **overall browser zoom**: every CSS length including text scales and the usable viewport halves.
//                It is stricter than a text-only resize but **cannot distinguish** hard-coded px from relative widths, because both scale proportionally.
//     text 200% the real WCAG `SC 1.4.4` scenario (failure techniques F69 / F80): **only font sizes double** while px layout boxes (including a hard-coded `width=280`) stay unchanged.
//                This is the only pass in which hard-coded pixels actually fail. It is implemented by walking every element once, reading the computed font size, and writing back an inline doubled px value, which avoids `font-size: 200%` compounding down the DOM depth.
//     "Not clipped" is judged per `th` / `td` by comparing `scrollWidth` with `clientWidth` (2px rounding tolerance), meaning content must reflow rather than be cut off; whole-table horizontal scrolling remains the 2D layout exception that `SC 1.4.10` explicitly grants data tables and is not counted as a failure.
//     "The relative width is in effect" is judged by `fillRatio` (rendered control width ÷ cell content width) landing in 0.95–1.05: with a hard-coded `width="280"` this ratio is necessarily above 1, meaning the control is wider than its column and overflows the cell.
//
//   Prerequisites:
//     1) IIS Express running (`dev\scripts\Start-IISExpress.ps1`, port 40001 by default) serving the current working tree `src\Portal`;
//     2) external connection string configured at `%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`;
//     3) acceptance context `temp\p65\p65-acceptance-context.json` present (it supplies baseUrl, the administrator account, and the password; the password is used in-process only);
//     4) playwright resolvable — a development-only dependency that is not committed (on this machine `temp\node_modules\playwright`), so `PORTAL_PLAYWRIGHT_MODULE` must be set first.
//
//   Usage (run from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalCollaborationLayoutEvidence.mjs'
//   Output: `work-zone\dev\evidence\p79.4\<timestamp>-Dev\*.png` and `layout-summary.json`; exits with code 1 when any assertion fails.
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
const markupPath = path.join(repoRoot, 'src', 'Portal', 'Admin', 'CollaborationItems.aspx');

if (!fs.existsSync(contextPath)) {
  throw new Error('Acceptance context was not found: ' + contextPath);
}

const context = JSON.parse(fs.readFileSync(contextPath, 'utf8'));
const baseUrl = context.baseUrl;
const targetUrl = new URL('Admin/CollaborationItems.aspx', baseUrl).toString();

// <lang>
//   <zh-CN>证据目录：按时间戳分桶，避免不同轮次的断言互相覆盖；`PORTAL_P79_EVIDENCE_DIR` 可显式指定以便复跑比对。</zh-CN>
//   <en>Evidence directory: bucketed by timestamp so successive runs never overwrite each other; `PORTAL_P79_EVIDENCE_DIR` can pin a directory for repeatable comparison.</en>
// </lang>
const stamp = process.env.PORTAL_P79_EVIDENCE_STAMP || new Date().toISOString().replace(/[-:]/g, '').replace(/\..+$/, '');
const outputDir = process.env.PORTAL_P79_EVIDENCE_DIR
  ? path.resolve(process.env.PORTAL_P79_EVIDENCE_DIR)
  : path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p79.4', stamp + '-Dev');
fs.mkdirSync(outputDir, { recursive: true });

// <lang>
//   <zh-CN>期望的 9 个列头文案**不写死在本脚本里**，而是"从 aspx 源文件按列序取出资源键 → 到对应语言的 resx 查值"。
//   这样做有两个额外收益：① 断言真正覆盖了"标记层 → 资源键 → resx"整条链路，而不是比对脚本里的副本；
//   ② 新增/改名列头键时脚本不会悄悄失真。列序仍由源文件的 `&lt;th&gt;` 出现顺序决定。</zh-CN>
//   <en>The nine expected column-header texts are **not hard-coded here**: they are taken as "resource keys in column order from the aspx source → values looked up in the resx for the active language". This buys two extra benefits: (1) the assertion genuinely covers the whole "markup → resource key → resx" chain instead of comparing against a copy inside the script; (2) adding or renaming a header key cannot silently invalidate the script. Column order still comes from the order the `&lt;th&gt;` elements appear in the source.</en>
// </lang>
function readResxValues(resxName) {
  const resx = fs.readFileSync(path.join(repoRoot, 'src', 'Portal', 'App_GlobalResources', resxName), 'utf8');
  const values = {};
  const pattern = /<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>\s*<\/data>/g;
  let match;
  while ((match = pattern.exec(resx)) !== null) {
    values[match[1]] = match[2];
  }
  return values;
}

function readHeaderKeys() {
  const markup = fs.readFileSync(markupPath, 'utf8');
  const headerRow = markup.match(/<HeaderTemplate>[\s\S]*?<\/HeaderTemplate>/);
  if (!headerRow) {
    throw new Error('HeaderTemplate was not found in ' + markupPath);
  }
  return [...headerRow[0].matchAll(/<th\b[^>]*>\s*<%=\s*lang\.([A-Za-z0-9_]+)\s*%>\s*<\/th>/g)].map((match) => match[1]);
}

const headerKeys = readHeaderKeys();
const resxByLocale = { 'zh-CN': 'lang.zh-cn.resx', 'en-US': 'lang.en-us.resx' };
const expectedHeaders = {};
for (const [locale, resxName] of Object.entries(resxByLocale)) {
  const values = readResxValues(resxName);
  expectedHeaders[locale] = headerKeys.map((key) => values[key]);
}

// <lang>
//   <zh-CN>9 个状态动作按钮的控件 ID 集合。运行时按钮的 `name` 含 UniqueID（含 ID），因此可直接断言 9 个动作**一个不少、顺序未变**；
//   `CommandName` 本身不进 HTML，故另用标记层静态扫描（见下方 `readCommandNames`）做第二重回归。</zh-CN>
//   <en>The control-ID set of the nine status action buttons. A rendered button's `name` carries its UniqueID (which contains the ID), so this asserts directly that all nine actions are present and unreordered; `CommandName` itself is not emitted into HTML, so a markup-level static scan (`readCommandNames` below) provides the second layer of regression cover.</en>
// </lang>
const expectedActionButtonIds = [
  'StartButton',
  'CompleteButton',
  'ReturnButton',
  'ResubmitButton',
  'RejectButton',
  'CancelButton',
  'CloseButton',
  'AddParticipantCommentButton',
  'AddAdministratorCommentButton'
];
const expectedParticipantButtonIds = ['AddParticipantButton', 'RemoveParticipantButton'];

// <lang>
//   <zh-CN>静态回归：扫描 aspx 源文件的 `CommandName` 列表，验证 `D4`（本轮只平铺、不按状态隐藏）与"授权判定仍在数据层"的约束
//   没有被排版动作顺手改掉。顺序也纳入断言，因为 `ItemsRepeater_ItemCommand` 按命令名分派。</zh-CN>
//   <en>Static regression: scan the aspx source for its `CommandName` list to prove the `D4` constraint (this round only flattens the layout, never hides actions by state) and the "authorization stays in the data layer" rule were not disturbed by the markup reshuffle. Order is asserted too, because `ItemsRepeater_ItemCommand` dispatches by command name.</en>
// </lang>
function readCommandNames() {
  const markup = fs.readFileSync(markupPath, 'utf8');
  return [...markup.matchAll(/CommandName="([^"]+)"/g)].map((match) => match[1]);
}

const expectedCommandNames = [
  'Start',
  'Complete',
  'Return',
  'Resubmit',
  'Reject',
  'Cancel',
  'Close',
  'AddParticipantComment',
  'AddAdministratorComment',
  'AddParticipant',
  'RemoveParticipant'
];

/// <summary>
/// <lang>
///   <zh-CN>主题层覆盖检查：参与人列控件的 `box-sizing` 规则必须**六套正式皮肤齐全**。
///   运行期只可能验到当前皮肤那一份，其余五套靠静态核对；只写模块自带样式而不在主题层补规则，模块会在多数皮肤下丢失主题化样式
///   （AGENTS.md 硬性要求），因此这一项必须显式断言而不是假定。</zh-CN>
///   <en>Theme-layer coverage check: the `box-sizing` rule for the participants column controls must be **present in all six formal skins**. A runtime pass can only ever verify the active skin, so the other five are checked statically; writing only a module-local stylesheet without theme-layer rules loses the themed styling under most skins (a hard AGENTS.md requirement), so this is asserted explicitly rather than assumed.</en>
/// </lang>
/// </summary>
function checkThemeCoverage() {
  const themeRoot = path.join(repoRoot, 'src', 'Portal', 'App_Themes');
  const expected = [
    { folder: 'EnterpriseLight', scope: 'enterpriselight' },
    { folder: 'EnterpriseDark', scope: 'enterprisedark' },
    { folder: 'OaLight', scope: 'oalight' },
    { folder: 'OaDark', scope: 'oadark' },
    { folder: 'StateClassicLight', scope: 'stateclassiclight' },
    { folder: 'StateClassicDark', scope: 'stateclassicdark' }
  ];

  return expected.map((theme) => {
    const cssPath = path.join(themeRoot, theme.folder, 'Default.css');
    if (!fs.existsSync(cssPath)) {
      return { theme: theme.folder, present: false, reason: 'Default.css not found' };
    }
    const css = fs.readFileSync(cssPath, 'utf8');
    const pattern = new RegExp(
      'body\\.portal-theme-' + theme.scope + '\\s+\\.portal-participant-actions\\s+\\.NormalTextBox\\s*\\{[^}]*box-sizing:\\s*border-box',
      'i'
    );
    return { theme: theme.folder, scope: theme.scope, present: pattern.test(css) };
  });
}

const themeCoverageCheck = checkThemeCoverage();

/// <summary>
/// <lang><zh-CN>建立并验证管理员登录态；未确认登录成功即失败，避免把拒绝访问页当作目标页断言。</zh-CN><en>Establishes and verifies the administrator sign-in state; failure to confirm sign-in aborts so an access-denied page is never asserted as the target page.</en></lang>
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
  if (!/欢迎|Logoff|Log out|注销/.test(text)) {
    throw new Error('Sign-in did not complete.');
  }
}

/// <summary>
/// <lang><zh-CN>在页面上采集单张列表的全部事实。`mode` 为 `normal` / `zoom200` / `text200`；返回的对象同时用于断言与留档。</zh-CN><en>Collects every fact about the list from the live page. `mode` is `normal` / `zoom200` / `text200`; the returned object feeds both assertions and the evidence archive.</en></lang>
/// </summary>
async function collectFacts(page, mode) {
  const facts = await page.evaluate((zoomMode) => {
    const table = document.querySelector('table.portal-data-table');
    if (!table) {
      return { tableFound: false };
    }

    // <lang>
    //   <zh-CN>两档放大各自模拟不同的真实场景，缺一不可：
    //     · `zoom200` ＝ 浏览器**整体**放大 200%：所有 CSS 长度（含 px 固定宽度）一起放大，可用视口宽度同时减半。
    //       固定 px 与相对宽度在这一档都会等比放大，因此它验证的是"重排是否成立"，不能区分固定 px。
    //     · `text200` ＝ **纯文字**放大 200%（WCAG `SC 1.4.4` 的真实场景，失败技术 F69 / F80）：只有字号翻倍，
    //       px 布局盒（含 `width=280` 这类固定宽度）**保持不变**。这才是"写死像素"会失败的唯一一档 ——
    //       故这里用一次性遍历把每个元素的计算字号读出来再写成 2 倍的内联 px，避免 `font-size: 200%` 沿 DOM 深度连乘。</zh-CN>
    //   <en>The two zoom passes emulate different real scenarios and neither replaces the other:
    //     · `zoom200` = **overall** browser zoom at 200%: every CSS length (including fixed px widths) scales and the usable viewport halves. Fixed px and relative widths both scale proportionally here, so this pass validates reflow but cannot tell fixed px apart.
    //     · `text200` = **text-only** resize at 200% (the real WCAG `SC 1.4.4` scenario, failure techniques F69 / F80): only font sizes double while px layout boxes (including a hard-coded `width=280`) stay put. This is the only pass where hard-coded pixels actually fail — so it walks every element once, reads the computed font size, and writes back an inline doubled px value, which avoids `font-size: 200%` compounding down the DOM.</en>
    // </lang>
    let textZoomApplied = 0;
    if (zoomMode === 'zoom200') {
      document.documentElement.style.zoom = '2';
    } else if (zoomMode === 'text200') {
      // <lang>
      //   <zh-CN>必须**先读完再统一写**。若边读边写，子元素会继承到已翻倍的父级字号，再乘 2 就成了 4 倍，
      //   沿 DOM 深度呈指数放大（实测行高被推到上万像素，完全失去 200% 的意义）。</zh-CN>
      //   <en>All computed sizes must be **read before any write**. Interleaving reads and writes makes a child inherit the already-doubled parent size and multiply it again into 4x, compounding exponentially down the DOM depth (the measured row height blew up to tens of thousands of pixels, which destroys the meaning of a 200% test).</en>
      // </lang>
      const elements = [document.documentElement, ...document.querySelectorAll('*')];
      const sizes = elements.map((element) => {
        const parsed = Number.parseFloat(window.getComputedStyle(element).fontSize);
        return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
      });
      for (let index = 0; index < elements.length; index++) {
        if (sizes[index] === null) {
          continue;
        }
        elements[index].style.fontSize = (sizes[index] * 2) + 'px';
        textZoomApplied++;
      }
    }

    const headers = [...table.querySelectorAll('th[scope="col"]')];
    const headerTexts = headers.map((th) => (th.textContent || '').trim());
    const headerWidths = headers.map((th) => Math.round(th.getBoundingClientRect().width));
    const headerAllScopeCol = table.querySelectorAll('th').length === headers.length;

    // <lang>
    //   <zh-CN>数据行的取法必须排除表头行：Repeater 的 `HeaderTemplate` 也输出一个 `&lt;tr&gt;`（只含 `th`），
    //   HTML 解析器又会把 `&lt;tr&gt;` 自动包进 `&lt;tbody&gt;`，所以 `tbody tr` 的第一项其实是表头而不是数据行。
    //   这里按"含 `td`"筛选，避免把表头当成数据行而误判"单元格为空"。</zh-CN>
    //   <en>Data rows must exclude the header row: the Repeater's `HeaderTemplate` also emits a `&lt;tr&gt;` (th only), and the HTML parser wraps `&lt;tr&gt;` elements in an implicit `&lt;tbody&gt;`, so the first `tbody tr` is the header rather than a data row. Filtering on "contains a `td`" avoids misreading the header as an empty cell.</en>
    // </lang>
    const dataRows = [...table.querySelectorAll('tr')].filter((row) => row.querySelector('td'));
    const cells = dataRows.map((tr) => [...tr.querySelectorAll('td')]);
    const firstRow = cells[0] || [];

    // <lang>
    //   <zh-CN>裁切判定逐格比较 scrollWidth 与 clientWidth。表格整体可横向滚动（SC 1.4.10 对 data tables 的例外），
    //   但单个单元格的内容不允许被切掉，故这里只统计"格内溢出"。</zh-CN>
    //   <en>Clipping is judged per cell by comparing scrollWidth with clientWidth. The table as a whole may scroll horizontally (the data-table exception in SC 1.4.10), but content inside a single cell must not be cut off, so only in-cell overflow is counted here.</en>
    // </lang>
    const clippedCells = [];
    for (const cell of table.querySelectorAll('th, td')) {
      const overflow = cell.scrollWidth - cell.clientWidth;
      if (overflow > 2) {
        clippedCells.push({
          tag: cell.tagName,
          columnIndex: cell.cellIndex,
          text: (cell.textContent || '').trim().slice(0, 40),
          overflowPx: overflow
        });
      }
    }

    // <lang>
    //   <zh-CN>三列职责是否各自独立：第 7 列只放意见框、第 8 列只放 9 个状态动作、第 9 列放参与人文本与增删控件。
    //   任何一列越界都说明"拆列"没有真正生效（内容仍混装在同一格）。</zh-CN>
    //   <en>Whether the three columns hold independent content: column 7 only the comment box, column 8 only the nine status actions, column 9 the participant text plus add/remove controls. Content crossing a boundary means the split did not really happen (it is still mixed inside one cell).</en>
    // </lang>
    const commentCell = firstRow[6];
    const actionCell = firstRow[7];
    const participantCell = firstRow[8];
    const commentBox = commentCell ? commentCell.querySelector('textarea') : null;
    const actionSubmits = actionCell ? [...actionCell.querySelectorAll('input[type="submit"], input[type="button"]')] : [];
    const participantSubmits = participantCell ? [...participantCell.querySelectorAll('input[type="submit"], input[type="button"]')] : [];
    const participantInput = participantCell ? participantCell.querySelector('input[type="text"]') : null;
    const participantSelect = participantCell ? participantCell.querySelector('select') : null;

    const actionButtonNames = actionSubmits.map((input) => input.getAttribute('name') || '');
    const participantButtonNames = participantSubmits.map((input) => input.getAttribute('name') || '');

    // <lang>
    //   <zh-CN>宽度一律用 `offsetWidth` / `offsetHeight`（布局像素）而不是 `getBoundingClientRect`。
    //   原因（实测踩过）：`getBoundingClientRect` 在 `zoom` 下返回**放大后**的设备像素，而 `clientWidth` 仍是未放大的布局像素，
    //   两者相除会整体偏大 2 倍（`zoom200` 档所有 fillRatio 都恰好等于 2 就是这个坑），
    //   会把"控件是否充满列"的结论彻底带偏。布局像素不受 `zoom` 影响，才是可比的量。</zh-CN>
    //   <en>Widths always use `offsetWidth` / `offsetHeight` (layout pixels) rather than `getBoundingClientRect`. The reason, established by measurement: under `zoom`, `getBoundingClientRect` returns **scaled** device pixels while `clientWidth` is still unscaled layout pixels, so dividing the two inflates every ratio by exactly 2 (that is why every fillRatio in the `zoom200` pass came out as 2) and would completely invalidate the "does the control fill its column" conclusion. Layout pixels are unaffected by `zoom` and are therefore the comparable quantity.</en>
    // </lang>
    const measure = (element) => (element ? element.offsetWidth : null);
    const measureHeight = (element) => (element ? element.offsetHeight : null);

    // <lang>
    //   <zh-CN>事项列内层不再含"参与人"行：这是步骤 4 的可观测后果。取该列 `portal-value-stack` 的直接子 div 数量（应为 7），
    //   并核对其文本里不出现参与人展示串。</zh-CN>
    //   <en>The item column's inner stack no longer contains a "Participants" line: the observable consequence of step 4. It counts the direct child divs of that column's `portal-value-stack` (7 expected) and checks the participants display text is absent from it.</en>
    // </lang>
    const itemCell = firstRow[4];
    const valueStack = itemCell ? itemCell.querySelector('div.portal-value-stack') : null;
    const stackChildren = valueStack ? [...valueStack.children].filter((child) => child.tagName === 'DIV') : [];
    const stackLabels = stackChildren.map((child) => {
      const label = child.querySelector('span.SubHead');
      return label ? (label.textContent || '').trim() : '';
    });

    const participantCellText = participantCell ? (participantCell.textContent || '').trim() : '';

    // <lang>
    //   <zh-CN>控件"充满所在列"的量化口径：控件渲染宽度 ÷ 单元格内容宽度。
    //   相对宽度下该比值≈1（控件跟随列伸缩）；写死像素时比值会 &gt;1（控件比列宽，直接溢出单元格）
    //   或在列变窄后明显小于 1（控件不跟随列）。这比"宽度是否等于某个魔数"更能证明 `width:100%` 真的生效。</zh-CN>
    //   <en>Quantified "the control fills its column" measure: rendered control width ÷ cell content width. With a relative width the ratio is about 1 (the control follows the column); with a hard-coded pixel width the ratio exceeds 1 (the control is wider than the column and overflows the cell) or drops well below 1 once the column narrows (the control ignores the column). This proves `width:100%` is genuinely in effect far better than comparing against some magic number.</en>
    // </lang>
    const cellContentWidth = (cell) => {
      if (!cell) {
        return null;
      }
      const style = window.getComputedStyle(cell);
      const padding = Number.parseFloat(style.paddingLeft || '0') + Number.parseFloat(style.paddingRight || '0');
      return Math.round(cell.clientWidth - padding);
    };
    const fillRatio = (control, cell) => {
      const controlWidth = measure(control);
      const available = cellContentWidth(cell);
      if (!controlWidth || !available) {
        return null;
      }
      return Number((controlWidth / available).toFixed(3));
    };

    return {
      tableFound: true,
      zoomMode,
      textZoomApplied,
      columnCount: headers.length,
      headerAllScopeCol,
      headerTexts,
      headerWidths,
      rowCount: cells.length,
      firstRowCellCount: firstRow.length,
      clippedCells,
      commentBoxInlineStyle: commentBox ? (commentBox.getAttribute('style') || '') : null,
      commentBoxWidthPx: measure(commentBox),
      commentBoxFillRatio: fillRatio(commentBox, commentCell),
      commentCellHasAction: commentCell ? commentCell.querySelectorAll('input[type="submit"]').length : -1,
      actionCellHasTextarea: actionCell ? (actionCell.querySelector('textarea') ? 1 : 0) : -1,
      actionButtonCount: actionSubmits.length,
      actionButtonNames,
      participantCellHasTextarea: participantCell ? (participantCell.querySelector('textarea') ? 1 : 0) : -1,
      participantButtonCount: participantSubmits.length,
      participantButtonNames,
      participantInputWidthPx: measure(participantInput),
      participantInputFillRatio: fillRatio(participantInput, participantCell),
      participantSelectWidthPx: measure(participantSelect),
      participantSelectFillRatio: fillRatio(participantSelect, participantCell),
      participantCellTextSample: participantCellText.slice(0, 120),
      valueStackChildCount: stackChildren.length,
      valueStackLabels: stackLabels,
      rowHeightPx: firstRow.length > 0 ? measureHeight(firstRow[0].parentElement) : null
    };
  }, mode);

  return facts;
}

const records = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  for (const locale of ['zh-CN', 'en-US']) {
    const browserContext = await browser.newContext({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 1,
      locale
    });
    const page = await browserContext.newPage();
    page.setDefaultTimeout(40000);
    await signIn(page);

    for (const mode of ['normal', 'zoom200', 'text200']) {
      const record = {
        locale,
        mode,
        url: targetUrl,
        status: 'Pass',
        facts: {},
        notes: []
      };

      try {
        await page.goto(targetUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await page.waitForTimeout(700);
        const html = await page.content();

        record.facts = Object.assign(record.facts, await collectFacts(page, mode));
        record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);

        // <lang>
        //   <zh-CN>断言 1：页面未回落到通用错误页（标记层语法错误、资源键缺失都会在这里暴露）。</zh-CN>
        //   <en>Assertion 1: the page did not fall back to the generic error page (markup syntax errors and missing resource keys surface here).</en>
        // </lang>
        if (record.facts.errorPage) {
          record.status = 'Fail';
          record.notes.push('页面回落到通用错误页 / The page fell back to the generic error page.');
        }

        if (!record.facts.tableFound) {
          record.status = 'Fail';
          record.notes.push('未找到 portal-data-table / portal-data-table was not found.');
          records.push(record);
          continue;
        }

        // <lang>
        //   <zh-CN>断言 2：表头 9 列、列序与文案对齐、且全部带 scope="col"（新增列同样要有列头语义）。</zh-CN>
        //   <en>Assertion 2: nine header columns in the expected order with the expected wording, all carrying scope="col" (the new columns need header semantics too).</en>
        // </lang>
        if (record.facts.columnCount !== 9) {
          record.status = 'Fail';
          record.notes.push(`表头列数为 ${record.facts.columnCount}，期望 9。`);
        }
        if (!record.facts.headerAllScopeCol) {
          record.status = 'Fail';
          record.notes.push('存在缺 scope="col" 的表头。');
        }
        const expected = expectedHeaders[locale];
        if (JSON.stringify(record.facts.headerTexts) !== JSON.stringify(expected)) {
          record.status = 'Fail';
          record.notes.push('列头文案或列序不符：' + JSON.stringify(record.facts.headerTexts));
        }

        if (record.facts.rowCount === 0) {
          record.status = 'Fail';
          record.notes.push('列表零行，无法验证单元格职责拆分（需要先造数）。');
          records.push(record);
          continue;
        }

        // <lang>
        //   <zh-CN>断言 3：三列内容各自独立，任一列越界即视为"拆列未生效"。</zh-CN>
        //   <en>Assertion 3: the three columns hold independent content; any boundary crossing means the split did not take effect.</en>
        // </lang>
        if (record.facts.commentCellHasAction !== 0) {
          record.status = 'Fail';
          record.notes.push('处理意见列里仍有动作按钮（职责未拆开）。');
        }
        if (record.facts.actionCellHasTextarea !== 0) {
          record.status = 'Fail';
          record.notes.push('操作列里仍有意见文本框（职责未拆开）。');
        }
        if (record.facts.participantCellHasTextarea !== 0) {
          record.status = 'Fail';
          record.notes.push('参与人列里仍有意见文本框（职责未拆开）。');
        }
        if (record.facts.participantSelectWidthPx === null) {
          record.status = 'Fail';
          record.notes.push('参与人列缺少角色下拉。');
        }

        // <lang>
        //   <zh-CN>断言 4：意见框走相对宽度，且实测"充满所在列"。写死 `width="280"` 时该比值会远大于 1
        //   （控件比列宽、直接溢出单元格），改相对宽度后应贴近 1。这一条同时覆盖 SC 1.4.4 的失败技术 F80 根因。</zh-CN>
        //   <en>Assertion 4: the comment box uses a relative width and measurably fills its column. With a hard-coded `width="280"` this ratio is far above 1 (the control is wider than the column and overflows the cell); with a relative width it sits close to 1. This also covers the root cause of SC 1.4.4 failure technique F80.</en>
        // </lang>
        if (!/width\s*:\s*100%/i.test(record.facts.commentBoxInlineStyle || '')) {
          record.status = 'Fail';
          record.notes.push('意见框未使用相对宽度，实际 style=' + record.facts.commentBoxInlineStyle);
        }
        for (const [label, ratio] of [
          ['意见框', record.facts.commentBoxFillRatio],
          ['参与人输入框', record.facts.participantInputFillRatio],
          ['参与人角色下拉', record.facts.participantSelectFillRatio]
        ]) {
          if (ratio === null || ratio === undefined) {
            record.status = 'Fail';
            record.notes.push(label + '未能测得"充满列"比值。');
          } else if (ratio < 0.95 || ratio > 1.05) {
            record.status = 'Fail';
            record.notes.push(label + '未充满所在列（比值 ' + ratio + '，期望 0.95–1.05）。');
          }
        }

        // <lang>
        //   <zh-CN>断言 5（回归）：9 个状态动作与 2 个参与人按钮**一个不少、ID 与顺序未变**（D4：本轮只平铺）。</zh-CN>
        //   <en>Assertion 5 (regression): all nine status actions and both participant buttons are present with unchanged IDs and order (D4: this round only flattens).</en>
        // </lang>
        if (record.facts.actionButtonCount !== 9) {
          record.status = 'Fail';
          record.notes.push(`动作按钮数为 ${record.facts.actionButtonCount}，期望 9。`);
        }
        for (const id of expectedActionButtonIds) {
          if (!record.facts.actionButtonNames.some((name) => name.includes(id))) {
            record.status = 'Fail';
            record.notes.push('缺少动作按钮 ' + id);
          }
        }
        if (record.facts.participantButtonCount !== 2) {
          record.status = 'Fail';
          record.notes.push(`参与人按钮数为 ${record.facts.participantButtonCount}，期望 2。`);
        }
        for (const id of expectedParticipantButtonIds) {
          if (!record.facts.participantButtonNames.some((name) => name.includes(id))) {
            record.status = 'Fail';
            record.notes.push('缺少参与人按钮 ' + id);
          }
        }

        // <lang>
        //   <zh-CN>断言 6：事项列内层已移除"参与人"行，且参与人展示串确实出现在参与人列里（信息没有整体丢失）。</zh-CN>
        //   <en>Assertion 6: the item column's inner stack no longer carries a "Participants" line, while the participants display text does appear in the participants column (the information is not lost altogether).</en>
        // </lang>
        if (record.facts.valueStackChildCount !== 7) {
          record.status = 'Fail';
          record.notes.push(`事项列内层行数为 ${record.facts.valueStackChildCount}，期望 7（原 8 行减去参与人行）。`);
        }
        if (!record.facts.participantCellTextSample) {
          record.status = 'Fail';
          record.notes.push('参与人列为空，参与人信息丢失。');
        }

        // <lang>
        //   <zh-CN>断言 7（两档放大都跑）：格内不裁切。表格整体横向滚动仍属 SC 1.4.10 允许的 2D 布局例外，不在此列。
        //   `text200` 一档尤其关键：它是"纯文字放大"这一真实场景，只在这一档里写死像素才会失败（F69 / F80）。</zh-CN>
        //   <en>Assertion 7 (runs on both zoom passes): no in-cell clipping. Whole-table horizontal scrolling remains the 2D layout exception SC 1.4.10 grants and is not counted here. The `text200` pass is the decisive one: it is the real text-only resize scenario, and the only pass in which hard-coded pixels actually fail (F69 / F80).</en>
        // </lang>
        if (record.facts.clippedCells.length > 0) {
          record.status = 'Fail';
          record.notes.push('存在格内裁切 ' + record.facts.clippedCells.length + ' 处：' + JSON.stringify(record.facts.clippedCells.slice(0, 5)));
        }

        if (locale === 'zh-CN') {
          const shot = path.join(outputDir, 'collaboration-items-' + locale + '-' + mode + '.png');
          await page.screenshot({ path: shot, fullPage: mode === 'normal' });
          record.screenshot = path.relative(repoRoot, shot);
        }
      } catch (error) {
        record.status = 'Fail';
        record.notes.push(error instanceof Error ? error.message : String(error));
      }

      records.push(record);
    }

    await browserContext.close();
  }
} finally {
  await browser.close();
}

// <lang>
//   <zh-CN>固定像素回归的量化口径改为"控件是否充满所在列"（`fillRatio`），不再用"控件宽度是否随放大翻倍"。
//   原因（实测得出，写在这里避免后人重犯）：在**整体** 200% 放大下，写死 px 与相对宽度都会等比放大，
//   比值必然≈2，该口径**无法区分**两者；到了表格 `min-width` 生效时（可用视口减半），列本身可能不再变宽，比值甚至只有 1.2。
//   真正能判别的口径是两条：① `fillRatio`≈1（控件跟随列，写死 280px 时必然 >1 即溢出）；
//   ② `text200` 档格内不裁切（纯文字放大时 px 盒不变，写死像素必然失败 = F69 / F80）。</zh-CN>
//   <en>The quantified fixed-pixel check is expressed as "does the control fill its column" (`fillRatio`) rather than "does the control width double under zoom". The reason, established by measurement and recorded here so it is not repeated: under **overall** 200% zoom both hard-coded px and relative widths scale proportionally, so the ratio is necessarily about 2 and the measure **cannot** tell them apart; once the table's `min-width` binds (viewport halved) the column may stop widening and the ratio can be as low as 1.2. The two measures that genuinely discriminate are (1) `fillRatio` around 1 (the control follows its column, whereas a hard-coded 280px is necessarily greater than 1, i.e. an overflow), and (2) no in-cell clipping in the `text200` pass (under text-only resize px boxes do not change, so hard-coded pixels necessarily fail: F69 / F80).</en>
// </lang>
const fillRatioCheck = {
  commentBox: records.map((record) => ({ locale: record.locale, mode: record.mode, ratio: record.facts.commentBoxFillRatio })),
  participantInput: records.map((record) => ({ locale: record.locale, mode: record.mode, ratio: record.facts.participantInputFillRatio })),
  participantSelect: records.map((record) => ({ locale: record.locale, mode: record.mode, ratio: record.facts.participantSelectFillRatio }))
};

// <lang>
//   <zh-CN>静态回归：`CommandName` 序列必须与实现前完全一致。运行时 HTML 里没有 `CommandName`，只能靠源文件扫描补上这一重。</zh-CN>
//   <en>Static regression: the `CommandName` sequence must be identical to before the change. Runtime HTML carries no `CommandName`, so a source scan supplies this layer.</en>
// </lang>
const commandNames = readCommandNames();
const commandNameCheck = {
  actual: commandNames,
  expected: expectedCommandNames,
  unchanged: JSON.stringify(commandNames) === JSON.stringify(expectedCommandNames)
};
if (!commandNameCheck.unchanged) {
  for (const record of records) {
    record.status = 'Fail';
    record.notes.push('CommandName 序列与实现前不一致。');
  }
}

for (const theme of themeCoverageCheck) {
  if (!theme.present) {
    for (const record of records) {
      record.status = 'Fail';
      record.notes.push('主题层缺少参与人列控件规则：' + theme.theme);
    }
  }
}

const summary = {
  phase: 'W-anp-P79 P79.4',
  generatedUtc: new Date().toISOString(),
  targetUrl,
  zoomSemantics: {
    normal: 'No scaling; the baseline.',
    zoom200: 'documentElement.style.zoom = 2 — overall browser-style zoom; every CSS length scales and the usable viewport halves. Verifies reflow but cannot distinguish hard-coded px from relative widths.',
    text200: 'Text-only 200% resize (the real WCAG SC 1.4.4 scenario, failures F69 / F80): every element font size is doubled in place while px layout boxes stay unchanged. The only pass in which hard-coded pixels fail.',
    clippingRule: 'Per th/td, scrollWidth - clientWidth > 2 counts as in-cell clipping. Whole-table horizontal scrolling is the data-table 2D exception SC 1.4.10 grants and is not counted.'
  },
  commandNameCheck,
  themeCoverageCheck,
  fillRatioCheck,
  records
};

fs.writeFileSync(path.join(outputDir, 'layout-summary.json'), JSON.stringify(summary, null, 2), 'utf8');

for (const record of records) {
  console.log('[' + record.status + '] ' + record.locale + ' ' + record.mode +
    ' cols=' + record.facts.columnCount +
    ' rows=' + record.facts.rowCount +
    ' actions=' + record.facts.actionButtonCount +
    ' participantBtns=' + record.facts.participantButtonCount +
    ' stackRows=' + record.facts.valueStackChildCount +
    ' clipped=' + (record.facts.clippedCells || []).length +
    ' rowH=' + record.facts.rowHeightPx +
    ' commentFill=' + record.facts.commentBoxFillRatio +
    ' inputFill=' + record.facts.participantInputFillRatio +
    ' selectFill=' + record.facts.participantSelectFillRatio);
  for (const note of record.notes) {
    console.log('    note: ' + note);
  }
}
console.log('commandNameUnchanged=' + commandNameCheck.unchanged);
console.log('themeCoverage=' + themeCoverageCheck.map((theme) => theme.theme + ':' + theme.present).join(' '));
console.log('evidence: ' + path.relative(repoRoot, outputDir));

const failed = records.filter((record) => record.status !== 'Pass').length;
console.log('collaboration layout evidence completed: ' + records.length + ' record(s), ' + failed + ' failed');
process.exit(failed > 0 ? 1 : 0);
