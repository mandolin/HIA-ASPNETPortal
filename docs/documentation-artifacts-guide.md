# 文档产物与工具链边界

本文说明 HIA-ASPNETPortal 的公开维护文档、文档化输入和可再生产物之间的边界。目标是让维护者能够在不依赖
WorkZone、门户运行时或其他 HIA 项目本地目录的前提下，重复执行已有验证。

## 公开文档与私有资料

- 根 `README.md` 与本目录下的 Markdown 是主仓库的公开维护文档。
- `work-zone/` 是独立私有仓库，用于计划、ADR、AI 协作资料和阶段证据；公开文档不得链接到其中的文件。
- 文档可以使用 `{ExternalCfgPath}`、`$env:USERPROFILE`、模板文件名和逻辑连接串名称说明可复现的配置方式，
  不得写入真实连接串、密码、Token、Cookie、证书、生产备份或项目本机绝对路径。

## JavaScript 文档化

`dev/documentation/jsdoc/` 是独立的 HIA JSDoc pilot 工具项目。当前范围只读取已追踪的
`src/Portal/gulpfile.js` 与 `src/Portal/Scripts/Security/PortalLoginPasswordEncryption.js`，用于验证中英双语内容、
源码链接、HIA metadata 和 integration JSON。两个输入均为受控白名单；增删时必须同步审查配置、readiness gate 和本说明。

执行方式见 [开发指南](dev-guide.md#javascript-文档化-pilot)。首次运行会在该工具目录执行 `npm ci`；之后可使用
`portal: build JavaScript documentation pilot` VSCode 任务，或执行 `dev/scripts/Build-PortalJsdocPilot.ps1`。

生成的 HTML、metadata 和 integration JSON 只写入被忽略的 `temp/documentation/jsdoc/`。这些文件是本机验证产物，
不是门户运行时输入、Gulp 资产输入或公开发布物；不得将其复制到 `src/Documentation/`、`src/DoxyGen/`、
`src/Portal/Documentation/` 或提交到主仓库。

## .NET DotNetDoc Pilot

`dev/documentation/dotnetdoc/` 是独立的 HIA DotNetDoc pilot 工具项目。其 `package.json` 声明
`@hia-doc/dotnetdoc-runner@^0.1.8`，而受版本控制的锁文件解析为 `0.1.8`；两者共同构成当前可复现契约。
工具目录局部 `.npmrc` 使用 npm 官方 registry，以避免镜像源元数据延迟导致版本不可用。该工具包不复用
`src/Portal/package.json`，不参与 Gulp、IIS Express、Visual Studio Task Runner 或发布包构建。

默认配置为根级 `dotnetdoc.config.json`，首轮只读取四份 Debug XML 文档、代表性 Web Forms 标记注释、一个 Web Forms
surface 和 solution/project discovery。生成的 DotNetDoc extraction、HIA document 与 producer result 只写入被忽略的
`temp/documentation/dotnetdoc/`，不得提交。

可使用 `dev/scripts/Build-PortalDotNetDocPilot.ps1` 或 VSCode 任务 `portal: build .NET DotNetDoc pilot` 执行。脚本默认先调用
`Test-PortalXmlDocumentation.ps1 -Build` 生成 XML 文档，再运行 runner 和输出检查；已有 Debug XML 时可显式使用 `-SkipXmlBuild`。

`dotnetdoc.api-only.config.json` 是排障回退配置，只消费四份 XML 文档。`dotnetdoc.source-probe.config.json` 是 source relation
研究配置，用于观察 C# source extractor 在缺少完整项目上下文时的诊断表现；它不作为默认门禁，也不阻塞门户文档化主线。
当 HIA-Documentation-Sys 后续提供更稳定的项目级 source linkage 后，再把 source relation 提升为默认 contract。

## 覆盖率分层

P13.3 起，文档化覆盖率不使用单一百分比表示，而按风险和交付价值拆成三档：

| 分层 | 范围 | 当前要求 |
| --- | --- | --- |
| Required | 发布、安全、配置、外置连接串、日志、审计、权限、上传、路径校验和公共契约。 | 新增或结构性修改时必须补准确的中英 XML/JSDoc 注释，并进入相关门禁或证据包。 |
| Recommended | 主要业务服务、业务数据对象、Admin 关键页面 code-behind、HIA 外围 proof 和样板业务流程。 | 本次触达时补齐语义说明；历史代码按 touch-improve 渐进推进。 |
| Deferred | 历史 Web Forms 页面细节、designer 生成成员、老旧示例、临时生成物和未确定所有权目录。 | 不作为当前阻断项，等工具链和重构边界稳定后再分批治理。 |

`Get-PortalDocumentationBaseline.ps1` 输出的是 inventory，而不是覆盖率分数。它可以记录哪些区域含 XML 注释、哪些目录属于生成物边界，但不替代人工判断注释是否准确。

## HIA 文档化通知

HIA-Documentation-Sys 的目标项目通知已改为拉取式通知。本项目不再等待外部项目写入 `dev/notify/`，而是在需要检查
文档化工具链变化时主动读取 HIA-Documentation-Sys 的 `work-zone/notify/`。

可使用 `dev/scripts/Get-HiaDocumentationNotifications.ps1` 查看最近通知。该脚本只读取通知内容，不复制文件、不修改源码、
不改变依赖，也不把 HIA-Documentation-Sys 变成本项目构建、测试、IIS Express 启动或发布流程的硬依赖。

P13.3 的 `Test-PortalDocumentationReadiness.ps1` 会把通知来源可读性作为固定检查项。若本机没有 HIA-Documentation-Sys，
结果记录为 `Pending`，不代表门户自身构建失败。

## 生成目录与提交策略

以下目录或文件类型当前属于生成物、历史输出或尚未确定所有权的区域：

- `temp/documentation/`：JSDoc 与 DotNetDoc pilot 的本机验证输出，受 `.gitignore` 保护。
- `bin/`：Visual Studio 与 MSBuild 生成的程序集和 XML 文档输出，受 `.gitignore` 保护。
- `src/Documentation/`、`src/Portal/Documentation/`、`src/Portal.Components.Data/Documentation/`：SHFB/Doxygen 历史生成输出，当前不作为公开文档输入或发布目录。
- `src/DoxyGen/`、`src/Portal.shfbproj`：Doxygen/SHFB 历史工具现场或配置候选，当前不入正式发布包。

`portal: verify public documentation` 会检查这些未确认生成目录没有进入 Git 已追踪文件。若将来需要发布版本化文档，
必须先定义所有权、许可证、清理方式、发布位置、访问边界和回退方案，并更新本指南、公开索引和门禁。

当 HIA-Documentation-Sys 的 .NET 文档化机制、输出 contract、source linkage 和生成产物边界稳定后，旧 SHFB/Doxygen
现场不再作为长期兼容目标保留。届时可按内部清理计划删除上述历史生成输出和空配置足迹；若仍需保留少量信息，应先迁移为
HIA 文档化工具链输入说明，而不是继续提交或发布 SHFB/Doxygen 生成物。

## 本地验证顺序

1. 运行 `portal: verify public documentation`，检查公开入口、相对链接、隐私和生成目录边界。
2. 需要确认 JavaScript 文档工具时，运行 JSDoc pilot；生成物保持在被忽略目录。
3. 需要确认 C# XML 输出时，运行 `portal: verify .NET XML documentation`；必要时由该任务先构建 Debug 配置。
4. 需要确认 HIA DotNetDoc 输出 contract 时，运行 `portal: build .NET DotNetDoc pilot`。
5. 需要留存 P13.3 文档化证据时，运行：

```powershell
dev/scripts/New-PortalDocumentationEvidencePackage.ps1
```

该证据包会编排 readiness、文档化 baseline、公开文档门禁、.NET XML 文档验证、JSDoc pilot 和 HIA 通知读取。它不修改源码注释、
不提交生成物、不读取敏感配置，也不连接数据库。

这些验证均是本地/VSCode 门禁。CI、在线链接监测、文档站、搜索和版本发布将在需求、密钥、缓存、分支与发布策略明确后另行设计。
