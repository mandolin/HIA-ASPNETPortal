# Web Forms 呈现与前端资产维护

## 适用范围

本指南描述当前已纳入主仓库的 Web Forms 呈现与前端构建契约。它适用于维护默认门户主题、受信任模块样式和
既有 Gulp 自动化；不构成响应式重构、前端框架迁移或动态资产管理方案。

## 正式输入与加载顺序

1. `src/Portal/Components/PortalPage.cs` 在 `PreInit` 解析并应用唯一的原生 Web Forms Theme。
2. `src/Portal/App_Themes/{ThemeName}/` 提供主题清单和 `Default.css`；主题选择、Tab 覆盖和回退规则见
   [theme-package-guide.md](theme-package-guide.md)。
3. `src/Portal/Default.master` 提供默认 Banner、唯一的 server form 和 `MainContent` 占位符。
4. `src/Portal/Default.master.cs` 在渲染前写入稳定的主题/Tab CSS class，并仅为当前 Tab 的已启用、受信任模块包
   挂载已验证的站内 CSS。
5. 模块包样式例如 `DesktopModules/ModuleProbe/Styles/ModuleProbe.css` 必须通过模块 catalog 校验。主题资源、外部 URL
   和模块 JavaScript 不会由 Master 页面自动加载。

默认主题继续支持 IE9+ 兼容 CSS。不要在主题中依赖 CSS variables、Grid、Flexbox、远程字体、远程资源或主题脚本。
普通文字使用开源字体优先栈和 generic fallback，具体许可证边界见 [font-policy-and-audit.md](font-policy-and-audit.md)。

## Gulp 与开发工具边界

`src/Portal/gulpfile.js` 的以下绑定属于既有 Visual Studio Task Runner 行为，不能由 VSCode 任务替换：

```js
/// <binding ProjectOpened='startWatch' />
```

- `startWatch`：Visual Studio 打开项目时启动 watcher，不执行首次构建。
- `stopWatch`：写入 watcher 信号文件，使既有 watcher 正常退出。
- `assets:build`：面向 VSCode 与 AI 自动化的一次性构建，不修改上述 Visual Studio 绑定。

Gulp 只在 `src/Portal` 工作目录中查找 `js/**/*.src.js`、`js/**/*.coffee`、`css/**/*.scss` 和 `css/**/*.sass`，并将生成的
`.js`、`.css` 与 source map 写回相应目录。前端构建不是门户编译或 IIS Express 启动的隐含前置条件；仅在已明确维护这些
输入时运行：

```powershell
cd src\Portal
npm run assets:build
npm run assets:watch
npm run assets:stop-watch
```

## 未跟踪资产边界

当前 `src/Portal/js/`、`src/Portal/css/` 以及若干生成文档目录尚未完成来源、许可证、输入/输出关系和提交策略确认。
在独立资产治理任务明确之前，日常维护不得读取、不移动、不提交或以这些目录为构建/启动门户的必需前提。需要新增正式前端
源码或构建产物时，应先确定所有权、许可证、浏览器兼容范围、`.gitignore` 规则和验证方式。

## 验证

以下脚本只读取正式追踪的 Master、主题、模块 CSS、Gulp、`package.json` 和本指南；它不调用 Node、npm、Gulp、IIS Express
或数据库：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalFrontendContracts.ps1
```

脚本验证默认 Master 结构、主题和模块 CSS 加载边界、Visual Studio/VSCode Gulp 任务、IE9+ Browserslist、主题清单、字体禁用
清单和公开文档入口。真实页面加载仍应结合 [testing-checklist.md](testing-checklist.md) 中的 IIS Express smoke 复核。

旧浏览器兼容专项还应执行静态门禁：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Test-PortalLegacyCssCompatibility.ps1
```

该脚本只读取 Git 已追踪的正式主题和模块 CSS。Flex/Grid、CSS 变量、现代单位、现代滤镜和 CSS 渐变依赖会作为阻断项；圆角、
阴影、透明度等 IE8 视觉降级项默认只警告。真实 IE8/IE9 或国产旧内核表现仍需通过云测试平台、旧 VM 或目标环境人工补证。
