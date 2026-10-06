# 贡献指南

感谢参与 HIA-ASPNETPortal。本文档说明从环境准备到提交合并的完整约定。

## 我想先了解项目

- 项目定位与能力：见 [README.md](README.md)
- 系统结构：[docs/architecture.md](docs/architecture.md)
- 成熟度与宣传口径约束：见 [docs/versioning.md](docs/versioning.md) 第六节

## 开发环境

见 [README.md](README.md) 的「快速上手」或更完整的 [docs/dev-guide.md](docs/dev-guide.md)。要点：

- **数据库连接串必须放在仓库外**（默认 `%USERPROFILE%\Web\HIA-ASPNETPortal\dev\connectionStrings.config`）。请勿把真实连接串或任何凭据提交进仓库。
- 站点默认以端口 `40001` 启动，自动化脚本据此定位。

## 提交前必须跑门禁

本项目把质量要求固化为**可重复运行的门禁**，提交前请运行：

```powershell
& 'C:\Program Files\PowerShell\7\pwsh.exe' -NoLogo -NoProfile -File dev\scripts\Invoke-PortalGateSuite.ps1
```

分层运行：`-Layer L0`（仅源码，最快）/ `-Layer L1`（需构建）/ `-Layer L2`（需站点）。耗时门禁用 `-IncludeSlow` 开启。

**为什么值得跑**：本项目有 22 个门禁在统一编排之外，它们同样有效，但不会自动执行。改动涉及以下范围时，请额外运行对应门禁：

| 改动范围 | 额外门禁 |
| --- | --- |
| 公开文档（`README.md` / `docs/`） | `Test-PortalPublicDocumentation.ps1`、`Test-PortalDocumentationReadiness.ps1` |
| 模块包（`module.json`） | `Test-PortalAscxCompilationContract.ps1`、`Test-PortalBusinessModuleSmoke.ps1 -ModuleName <包名>` |
| 资源配置或初始化脚本 | `Test-PortalResourceContract.ps1`、`Test-PortalSqlCompatibility.ps1` |
| 凭据、部署或运维相关 | `Test-PortalDefaultCredentialRisk.ps1`、`Test-PortalOperationsReadiness.ps1` |
| C# 代码注释 | `Test-PortalXmlDocumentation.ps1`（公开类型需双语 `<summary>`） |
| 版本相关 | `Test-PortalVersionConsistency.ps1`（CHANGELOG、tag、`AssemblyVersion` 三处必须一致） |

## 代码约定

- **编码**：所有文本文件为 UTF-8，CRLF 换行。C# 注释使用 `<lang><zh-CN>…</zh-CN><en>…</en></lang>` 双语块。
- **公开 API**：需要 XML 文档注释，门禁会校验双语块完整性。
- **敏感信息**：不得提交连接串、口令、Token、Cookie、证书私钥或真实环境截图。门禁会扫描，但**请在提交前自查**。
- **不要提交构建产物**：`bin/`、`obj/`、`temp/`、`Uploads/` 下的业务文件与真实配置。

## 分支与提交

- 分支名建议：`feat/<简述>`、`fix/<简述>`、`docs/<简述>`、`chore/<简述>`。
- 提交信息使用中文正文，首行以 Conventional Commits 前缀开头（如 `feat(a11y):`、`fix(auth):`、`docs(plan):`、`test(gate):`）。
- 一次提交只做一件事。重构与功能变更请分开提交，便于回溯。

## Pull Request

1. 先确认门禁通过（L0 必跑；改动运行期行为时加跑 L1/L2）。
2. PR 描述请写明：**改了什么、为什么、怎么验证的**。
3. 若变更可视化或行为，附运行期证据截图（`work-zone/dev/evidence/` 下的证据包）。
4. 若引入第三方依赖，按 [docs/third-party-dependencies.md](docs/third-party-dependencies.md) 说明用途、许可证与分发边界。

## Issue

提交缺陷前请尽量提供：复现步骤、实际与预期结果、门禁输出、以及运行环境（Windows / SQL Server / IIS Express 版本）。若涉及界面问题，附截图。

## 口径约束（重要）

本项目对外定位为 **可用 / 参考 / 研究基线**，**不宣称生产级可信**。在 Issue、PR、文档与对外说明中，请勿使用未经证据支持的表述（如"生产级可用"、"企业级可靠"）。真实生产就绪需额外的真实环境证据，不在当前里程碑范围内。