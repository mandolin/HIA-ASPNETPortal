// <lang>
//   <zh-CN>资源契约运行期证据脚本（W-anp-P80 `P80.4`）：对**运行中的站点**证明本包的资源改动**零用户可见变化**。
//
//   为什么要它：`P80.3` 改了四处资源文件（中性 resx 的 1 条值、designer 的 16 个属性与 17 个 summary、
//   en-us 的 283 条补齐）。其中**唯一有可能改变界面**的是 C5 —— 把 `Signin_EmailOrName` 的中性值
//   由中文「邮箱、用户名或员工号」改为英文。静态推理说它安全（该键在 zh-CN 与 en-US 两侧都有条目，
//   中性值只在"找不到特定文化条目"时才被用到），但**"中性值对用户不可见"是一个要靠运行期证明的断言**，
//   不是靠推理成立的结论 —— 这正是本脚本存在的唯一理由。
//
//   断言口径：
//     ① zh-CN 下登录框该 label 渲染为「邮箱、用户名或员工号」（走 `lang.zh-cn.resx`）；
//     ② en-US 下渲染为「Email, username, or employee code:」（走 `lang.en-us.resx` 显式条目，
//        **不回退到中性**，故改中性值对它无影响）；
//     ③ en-US 整页不含源码中文（数据里的中文不算，故只扫页面可见文本且排除已登录区域的动态内容）；
//     ④ 无通用错误页 —— 资源文件若被写坏（XML 不合法 / 键重复），ASP.NET 会在首次请求时整站报错。
//
//   前置条件：同 `Test-PortalAdminListUiEvidence.mjs`（IIS Express 40001 + 外置连接串 + playwright）。
//
//   用法（在仓库根目录执行）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalResourceContractEvidence.mjs'
//   产物：`work-zone\dev\evidence\p80.4\<时间戳>-Dev\*.png` 与 `contract-summary.json`；失败退出码 1。
//   </zh-CN>
//   <en>Resource-contract runtime evidence script (W-anp-P80 `P80.4`): proves against the **running site** that
//   this package's resource changes produce **zero user-visible change**.
//
//   Why it exists: `P80.3` touched four places (one neutral resx value, 16 designer properties and 17 summaries,
//   plus 283 new en-us entries). The **only** change that could possibly alter the UI is C5 — replacing the
//   neutral value of `Signin_EmailOrName`, which was Chinese, with English. Static reasoning says it is safe
//   (the key has entries in both zh-CN and en-US, and the neutral value is used only when no culture-specific
//   entry is found), but **"the neutral value is invisible to users" is an assertion that must be proved at
//   runtime**, not a conclusion established by reasoning — that is the sole reason this script exists.
//
//   Assertion rules:
//     ① under zh-CN the sign-in label renders as the Chinese text (resolved from `lang.zh-cn.resx`);
//     ② under en-US it renders as the English text (resolved from the explicit `lang.en-us.resx` entry, which
//        does **not** fall back to neutral, so changing the neutral value cannot affect it);
//     ③ the whole en-US page contains no source-code Chinese (data Chinese does not count, so only visible page
//        text is scanned);
//     ④ no generic error page — if a resx is corrupted (invalid XML or duplicate keys), ASP.NET fails the whole
//        site on the first request.
//
//   Prerequisites: same as `Test-PortalAdminListUiEvidence.mjs` (IIS Express 40001 + external connection string
//   + playwright).
//
//   Usage (run from the repository root):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Test-PortalResourceContractEvidence.mjs'
//   Output: `work-zone\dev\evidence\p80.4\<timestamp>-Dev\*.png` and `contract-summary.json`; exit code 1 on failure.
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
//   <zh-CN>期望文案**不写死在本脚本里**，而是从对应语言的 resx 现读现比 —— 这样断言的是"resx 的值真的渲染出来了"，
//   而不是"脚本里的副本与页面相同"。若有人只改了 resx 没改脚本，副本式断言会假通过。</zh-CN>
//   <en>Expected wording is **not hard-coded here**; it is read from the resx of the matching language and compared
//   live. This asserts that "the resx value really rendered" rather than "a copy inside the script matches the
//   page" — a copy-based assertion would pass falsely if someone changed only the resx.</en>
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

const expectations = [
  { locale: 'zh-CN', resx: 'lang.zh-cn.resx', key: 'Signin_EmailOrName' },
  { locale: 'en-US', resx: 'lang.en-us.resx', key: 'Signin_EmailOrName' }
].map((item) => Object.assign(item, { expectedText: readResxValue(item.resx, item.key) }));

const stamp = process.env.PORTAL_P80_EVIDENCE_STAMP ||
  new Date().toISOString().replace(/[-:]/g, '').replace(/\..+$/, '');
const outputDir = process.env.PORTAL_P80_EVIDENCE_DIR
  ? path.resolve(process.env.PORTAL_P80_EVIDENCE_DIR)
  : path.join(repoRoot, 'work-zone', 'dev', 'evidence', 'p80.4', stamp + '-Dev');
fs.mkdirSync(outputDir, { recursive: true });

const records = [];
const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });

try {
  for (const expectation of expectations) {
    const record = {
      locale: expectation.locale,
      url: baseUrl,
      status: 'Pass',
      facts: {},
      notes: []
    };

    // <lang>
    //   <zh-CN>每个语言档用**独立浏览器上下文**，避免会话与 cookie 串档 —— 登录态会改变首页是否渲染 Signin 控件。</zh-CN>
    //   <en>Each locale uses an **independent browser context** so sessions and cookies cannot leak between them —
    //   being signed in changes whether the home page renders the Signin control at all.</en>
    // </lang>
    const browserContext = await browser.newContext({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 1,
      locale: expectation.locale
    });
    const page = await browserContext.newPage();
    page.setDefaultTimeout(40000);

    try {
      await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(900);

      const html = await page.content();
      record.facts.errorPage = /GenericErrorPage|应用程序暂时无法完成请求/.test(html);

      // <lang>
      //   <zh-CN>定位登录框的"邮箱、用户名或员工号"标签：`Signin.ascx:20` 用 `<label for="...EmailOrName...">` 绑定输入框，
      //   故按 `for` 属性里的 `EmailOrName` 片段选取，比按文本选取更稳（文本正是要验证的对象）。</zh-CN>
      //   <en>Locate the sign-in label: `Signin.ascx:20` binds the input with `<label for="...EmailOrName...">`, so the
      //   label is selected through the `EmailOrName` fragment in its `for` attribute — more reliable than selecting
      //   by text, since the text is exactly what is under test.</en>
      // </lang>
      const labelLocator = page.locator('label[for*="EmailOrName"]').first();
      record.facts.labelFound = (await labelLocator.count()) > 0;
      record.facts.labelText = record.facts.labelFound ? (await labelLocator.innerText()).trim() : null;

      record.facts.expectedText = expectation.expectedText;
      record.facts.matches = record.facts.labelText === expectation.expectedText;
      record.facts.sourceResx = expectation.resx;

      // <lang>
      //   <zh-CN>英文界面整页中文扫描：只取可见文本，避免把 `<script>`/属性里的东西算进来。
      //   数据里的中文（如页签名"测试页"）会命中，故该项只作**记录**不作断言（P78 已如实记录同一边界）。</zh-CN>
      //   <en>Whole-page Chinese scan for the English UI: only visible text is taken so `<script>` and attribute
      //   content is not counted. Chinese coming from data (such as a tab named "测试页") will match, so this is
      //   **recorded** rather than asserted (P78 recorded the same boundary honestly).</en>
      // </lang>
      if (expectation.locale === 'en-US') {
        const visible = await page.locator('body').innerText();
        record.facts.chineseOccurrences = (visible.match(/[\u4e00-\u9fff]+/g) || []).slice(0, 10);
      }

      if (record.facts.errorPage) {
        record.status = 'Fail';
        record.notes.push('页面回落到通用错误页 / The page fell back to the generic error page.');
      }
      if (!record.facts.labelFound) {
        record.status = 'Fail';
        record.notes.push('未找到 Signin 的 EmailOrName 标签。');
      }
      if (record.facts.labelFound && !record.facts.matches) {
        record.status = 'Fail';
        record.notes.push('渲染文本与 resx 值不一致：实际[' + record.facts.labelText + '] 期望[' + expectation.expectedText + ']');
      }

      const shot = path.join(outputDir, 'signin-' + expectation.locale + '.png');
      await page.screenshot({ path: shot, fullPage: false });
      record.screenshot = path.relative(repoRoot, shot);
    } catch (error) {
      record.status = 'Fail';
      record.notes.push(error instanceof Error ? error.message : String(error));
    }

    await browserContext.close();
    records.push(record);
  }
} finally {
  await browser.close();
}

const summary = {
  phase: 'W-anp-P80 P80.4',
  generatedUtc: new Date().toISOString(),
  targetUrl: baseUrl,
  intent: 'Prove that changing the neutral resx value of Signin_EmailOrName leaves the rendered UI unchanged: zh-CN resolves from lang.zh-cn.resx and en-US from the explicit lang.en-us.resx entry, neither falling back to neutral.',
  records
};

fs.writeFileSync(path.join(outputDir, 'contract-summary.json'), JSON.stringify(summary, null, 2), 'utf8');

for (const record of records) {
  console.log('[' + record.status + '] ' + record.locale +
    ' labelFound=' + record.facts.labelFound +
    ' matchesResx=' + record.facts.matches +
    ' expected=[' + record.facts.expectedText + ']' +
    ' actual=[' + record.facts.labelText + ']');
  if (record.facts.chineseOccurrences) {
    console.log('    en-US 页面可见中文（仅记录，数据中文会命中）: ' + JSON.stringify(record.facts.chineseOccurrences));
  }
  for (const note of record.notes) {
    console.log('    note: ' + note);
  }
}
console.log('evidence: ' + path.relative(repoRoot, outputDir));

const failed = records.filter((record) => record.status !== 'Pass').length;
console.log('resource contract evidence completed: ' + records.length + ' record(s), ' + failed + ' failed');
process.exit(failed > 0 ? 1 : 0);
