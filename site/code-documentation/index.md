---
layout: default
lang: zh-CN
parent: 开发
permalink: /code-documentation/
title: 代码文档（Code documentation）
---
# 代码文档（Code documentation）

本页说明本项目的**代码文档体系**——由 `HIA-Documentation-Sys` 从源码注释生成 API 级文档，以及它当前发布到哪里、如何刷新。

## 体系组成（HIA-Documentation-Sys）

| 半边 | 输入 | 产物形态 | 当前状态 |
| --- | --- | --- | --- |
| **JSDoc（JavaScript）** | `src/Portal/gulpfile.js`、`src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js` | **双语 HTML 页面** + 主题资源 | ✅ **已发布**：见下方「已发布的代码文档」 |
| **DotNetDoc（C# XML 文档）** | `src/Portal/bin/Portal.xml` 等 4 个 XML 文档产物 + `src/master.sln` | 生成**中间 JSON**（14 个产物 / 60.8 MB，暂无渲染面；该数字为打通生成链路时的一次实测，重跑后可能不同） | ⏸ **未上架**：待渲染/发布组件就位后按同一清单机制补入 |

两侧的入口脚本（`dev/scripts/` 下）与门禁彼此隔离，不改变门户运行时、前端包或既有 Task Runner 绑定：

- `Build-PortalDotNetDocPilot.ps1` —— XML 预检 → `npm ci` → `docs:build` → `docs:verify`；
- `Build-PortalJsdocPilot.ps1` —— 还原隔离工作区依赖 → `docs:build`（`jsdoc -c jsdoc.conf.json`）→ `docs:verify`。

## 已发布的代码文档（JSDoc）

- 中文：`/api/jsdoc/index.zh-CN.html`
- 英文：`/api/jsdoc/index.en.html`
- 默认入口：`/api/jsdoc/index.html`

产物形状经门禁看守：不得包含内部工作项编号（`W-anp-P*` / `C-anp-P*`）、不得包含本地绝对路径。

## 已发布的代码文档（DotNetDoc）

- 入口：`/api/dotnetdoc/`（该页按半边与语言列出全部链接）
- 覆盖：**4 个 API 工程**（`Portal` / `Portal.Components` / `Portal.Components.Data` / `Portal.Components.Data1`）与**标记层注释**，均为**中英双语**——因为 C# 的 XML 文档注释本身就按 `<lang><zh-CN>…<en>…</lang>` 双语惯例书写。
- **页面表层与工程发现两个半边只有英文**：它们是结构描述（路由清单、解决方案构成），不是人工撰写的注释，因此没有中文内容。**如实按英文发布，不为凑双语而伪造中文目录。**
- 体积：约 16.8 MB 渲染产物（对比 JSDoc 半边的约 0.5 MB）。
- 刷新：脚本 `dev/scripts/Publish-PortalDotNetDoc.ps1`（先跑 `Build-PortalDotNetDocPilot.ps1` 生成 HIA 工件，再由本脚本逐半边按**该工件自己声明的语言**渲染并落到 `site/api/dotnetdoc/`）。

## 刷新方式（快照机制）

官网由**签入的静态文件**发布，因此代码文档是**快照**而非实时生成。刷新步骤：

1. 运行 `dev/scripts/Build-PortalJsdocPilot.ps1`（产物落在 `temp/documentation/jsdoc`，该目录被 git 忽略）；
2. 将产物拷贝到 `site/api/jsdoc/`，并更新同目录 `code-docs-manifest.json` 的 `generatedUtc` 与 `sourceFingerprint`；
3. 提交并推送。

L0 门禁 `Test-PortalCodeDocs.ps1` 会核对**源指纹**（对 JSDoc 的 2 个输入文件计算 SHA256）与产物形状：源码已变而快照未刷新 → 门禁失败。

## 依赖安装注意事项（环境相关）

JSDoc 试点工作区（`dev/documentation/jsdoc`）通过工作区级 `.npmrc` 固定使用 **npmjs 官方源**。原因是 `npm ci` 会按当前配置的 registry 改写 tarball 地址，而部分镜像对 npm 的 tarball 请求返回 400（元数据请求却正常），会导致安装必然失败。若你所在网络访问官方源不稳定，可改用本地镜像——但必须保证该镜像能正常返回 tarball，否则按项目约定视为前置缺失，不会静默跳过。

## 相关文档

- 模块与主题契约：`contracts/module-manifest.md`、`contracts/app-settings.md`；
- 源码注释规范（中英双语 `<lang>` 块）：见「设计与开发」相关页；
- 文档族总览与缺口：`documentation-map.md`。

<!-- build-stamp: 5931A67A091E -->
