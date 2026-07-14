# HIA ASP.NET Portal 前端构建 API

此工具项目只生成 `src/Portal/gulpfile.js` 的 HIA JSDoc pilot，用于验证中英双语内容、
源码链接、HIA metadata 和 integration JSON。它不参与门户运行时、Visual Studio Task Runner
或原有 Gulp 资源构建。

This tool project generates an HIA JSDoc pilot for `src/Portal/gulpfile.js` only. It verifies
bilingual content, source links, HIA metadata, and the integration JSON without participating in
the portal runtime, Visual Studio Task Runner, or the existing Gulp asset build.

运行 `npm ci` 后执行 `npm run docs`。生成物只写入被忽略的
`temp/documentation/jsdoc/`，不得提交。
