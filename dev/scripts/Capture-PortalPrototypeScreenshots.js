// <lang>
//   <zh-CN>原型设计稿截图工具：把指定目录下的每个 `*.html` 原型页整页截图成同名 `*.png`。
//   原型页必须引用**真实主题 CSS**（`src/Portal/App_Themes/<Theme>/Default.css`）与真实门户类，因此产物是"真实渲染"的设计稿，而不是绘制的示意图或文生图。
//
//   依赖说明：`playwright` 属**开发期依赖且不入库**（本机位于 `temp/node_modules/playwright`，而 `temp/` 已由 `.gitignore` 忽略），
//   因此 ESM 无法凭自身解析它。执行前须通过环境变量 `PORTAL_PLAYWRIGHT_MODULE` 指向该模块（目录或入口文件均可）。
//
//   用法（node 由 mise 提供，路径见 HANDOFF.md）：
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Capture-PortalPrototypeScreenshots.js' (Resolve-Path 'work-zone\dev\research\prototypes').Path
//   </zh-CN>
//   <en>Prototype screenshot tool: captures every `*.html` prototype page in the given directory into a same-named `*.png` full-page image.
//   A prototype page must link the **real theme CSS** (`src/Portal/App_Themes/<Theme>/Default.css`) and real portal classes, so the artifact is a real render rather than a drawn mock-up or a text-to-image guess.
//
//   Dependency note: `playwright` is a **development-only dependency that is not committed** (on this machine it lives in `temp/node_modules/playwright`, and `temp/` is ignored by `.gitignore`),
//   so ESM cannot resolve it on its own. Point the environment variable `PORTAL_PLAYWRIGHT_MODULE` at that module (a directory or an entry file both work) before running.
//
//   Usage (node is provided by mise; see HANDOFF.md for its path):
//     $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path 'temp\node_modules\playwright').Path
//     & $node 'dev\scripts\Capture-PortalPrototypeScreenshots.js' (Resolve-Path 'work-zone\dev\research\prototypes').Path
//   </en>
// </lang>
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

/// <summary>
/// <lang>
///   <zh-CN>解析 playwright 模块并返回 chromium 启动器：优先使用环境变量指定的位置，否则回退到按包名解析。</zh-CN>
///   <en>Resolves the playwright module and returns the chromium launcher: an environment-variable location takes precedence, otherwise package-name resolution is used.</en>
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

const prototypeDirectory = process.argv[2];
if (!prototypeDirectory) {
  throw new Error('Usage: node Capture-PortalPrototypeScreenshots.js <prototype-directory>');
}

const sources = fs
  .readdirSync(prototypeDirectory)
  .filter((name) => name.toLowerCase().endsWith('.html'))
  .sort();

if (sources.length === 0) {
  throw new Error('No *.html prototype pages were found in ' + prototypeDirectory);
}

const chromium = await loadChromium();
const browser = await chromium.launch({ headless: true });
try {
  for (const sourceName of sources) {
    const outputName = sourceName.replace(/\.html$/i, '.png');
    const page = await browser.newPage({
      viewport: { width: 1440, height: 1000 },
      deviceScaleFactor: 2
    });
    const url = 'file:///' + path.join(prototypeDirectory, sourceName).replace(/\\/g, '/');
    await page.goto(url, { waitUntil: 'load', timeout: 30000 });
    await page.screenshot({ path: path.join(prototypeDirectory, outputName), fullPage: true });
    await page.close();
    console.log('captured ' + outputName);
  }
} finally {
  await browser.close();
}

console.log('prototype capture completed');
