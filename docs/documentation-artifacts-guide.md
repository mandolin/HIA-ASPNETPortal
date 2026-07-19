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
`src/Portal/gulpfile.js`，用于验证中英双语内容、源码链接、HIA metadata 和 integration JSON。

执行方式见 [开发指南](dev-guide.md#javascript-文档化-pilot)。首次运行会在该工具目录执行 `npm ci`；之后可使用
`portal: build JavaScript documentation pilot` VSCode 任务，或执行 `dev/scripts/Build-PortalJsdocPilot.ps1`。

生成的 HTML、metadata 和 integration JSON 只写入被忽略的 `temp/documentation/jsdoc/`。这些文件是本机验证产物，
不是门户运行时输入、Gulp 资产输入或公开发布物；不得将其复制到 `src/Documentation/`、`src/DoxyGen/`、
`src/Portal/Documentation/` 或提交到主仓库。

## .NET XML 文档

现阶段使用标准 C# XML 文档输出，`Test-PortalXmlDocumentation.ps1` 会验证 Debug 构建生成的 `Portal`、
`Portal.Components`、`Portal.Components.Data` 与 `Portal.Components.Data1` XML 文件的存在、程序集名称和基本 XML 结构。
该检查不改写 `.csproj`、不把 `CS1591` 数量当作质量门禁，也不生成项目私有文档格式。

首个 HIA .NET pilot 候选是 `Portal.Components.xml`。只有 HIA-Documentation-Sys 提供稳定、已版本化的 producer/
consumer contract，且其许可证、输入、输出、source linkage、失败和回退方式经过独立审查后，才可以另立接入批次。
在此之前，不安装跨项目依赖、不建立本地目录链接，也不让门户构建、IIS 启动或发布流程依赖该工具链。

## HIA 文档化通知

HIA-Documentation-Sys 的目标项目通知已改为拉取式通知。本项目不再等待外部项目写入 `dev/notify/`，而是在需要检查
文档化工具链变化时主动读取 HIA-Documentation-Sys 的 `work-zone/notify/`。

可使用 `dev/scripts/Get-HiaDocumentationNotifications.ps1` 查看最近通知。该脚本只读取通知内容，不复制文件、不修改源码、
不改变依赖，也不把 HIA-Documentation-Sys 变成本项目构建、测试、IIS Express 启动或发布流程的硬依赖。

## 生成目录与提交策略

以下目录或文件类型当前属于生成物、历史输出或尚未确定所有权的区域：

- `temp/documentation/`：JSDoc pilot 的本机验证输出，受 `.gitignore` 保护。
- `bin/`：Visual Studio 与 MSBuild 生成的程序集和 XML 文档输出，受 `.gitignore` 保护。
- `src/Documentation/`、`src/DoxyGen/`、`src/Portal/Documentation/`：当前不作为公开文档输入或发布目录。

`portal: verify public documentation` 会检查这些未确认生成目录没有进入 Git 已追踪文件。若将来需要发布版本化文档，
必须先定义所有权、许可证、清理方式、发布位置、访问边界和回退方案，并更新本指南、公开索引和门禁。

## 本地验证顺序

1. 运行 `portal: verify public documentation`，检查公开入口、相对链接、隐私和生成目录边界。
2. 需要确认 JavaScript 文档工具时，运行 JSDoc pilot；生成物保持在被忽略目录。
3. 需要确认 C# XML 输出时，运行 `portal: verify .NET XML documentation`；必要时由该任务先构建 Debug 配置。

这些验证均是本地/VSCode 门禁。CI、在线链接监测、文档站、搜索和版本发布将在需求、密钥、缓存、分支与发布策略明确后另行设计。
