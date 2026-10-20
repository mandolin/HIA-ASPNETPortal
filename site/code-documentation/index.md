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
| **DotNetDoc（C# XML 文档）** | `src/Portal/bin/Portal.xml` 等 4 个 XML 文档产物 + `src/master.sln` | 生成**中间 JSON**（14 个产物 / 60.8 MB，暂无渲染面） | ⏸ **未上架**：待渲染/发布组件就位后按同一清单机制补入 |

两侧的入口脚本（`dev/scripts/` 下）与门禁彼此隔离，不改变门户运行时、前端包或既有 Task Runner 绑定：

- `Build-PortalDotNetDocPilot.ps1` —— XML 预检 → `npm ci` → `docs:build` → `docs:verify`；
- `Build-PortalJsdocPilot.ps1` —— 还原隔离工作区依赖 → `docs:build`（`jsdoc -c jsdoc.conf.json`）→ `docs:verify`。

## 已发布的代码文档（JSDoc）

- 中文：`/api/jsdoc/index.zh-CN.html`
- 英文：`/api/jsdoc/index.en.html`
- 默认入口：`/api/jsdoc/index.html`

产物形状经门禁看守：不得包含内部工作项编号（`W-anp-P*` / `C-anp-P*`）、不得包含本地绝对路径。

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
