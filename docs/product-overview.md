# 产品概览与定位（HIA-ASPNETPortal）

> **P21 `W-anp-P124` 交付物**。本页回答"它是什么、给谁用、能做什么、边界在哪"，是产品族的定位入口；具体操作步骤见 [user-guide.md](user-guide.md)，部署与运维见 `deployment-checklist.md` 与 `operations-runbook.md`。

## 一、产品概览

**HIA-ASPNETPortal** 是一个**企业内网 Web Forms 门户平台**，提供**权限与数据范围、流程状态机、审计、模块包治理、主题治理与自动化门禁**六类能力。

技术形态：**ASP.NET Web Forms**（.NET Framework 4.8），Windows + IIS Express + SQL Server / LocalDB。它不是 SPA，也没有单页路由；页面与模块以 Web Forms 控件树方式呈现。

> 溯源：本项目源自 CodePlex 上的 ASP.NET Portal Starter Kit，已在其基础上大量重构与扩展。

当前版本 **v1.0.0**（2026-10-09 首发发行版）。

### 适用场景

本版本为**首个正式发行版**，具备完整的能力覆盖、质量门禁、文档与开源就绪、版本锚点。

但**尚未提供生产环境验证证据**（真实 IIS、TLS、应用池 ACL、目标 SQL Server 与企业扫描等），因此**不建议直接用于生产**。完整版本规则见 [versioning.md](versioning.md)；目标环境证据的现状与缺口记录在本项目的**内部阶段证据**中（公开文档不登记待补证据）。

## 二、适用对象

**适合**：

- 需要在**企业内网**快速搭建门户（页签 + 模块化内容 + 角色权限 + 后台管理）的场景；
- 希望沿用 .NET Framework 4.8 / Web Forms 既有技术栈与运维体系的团队；
- 把本项目作为**参考基线**研究"门户权限模型、模块包治理、主题治理、自动化门禁"如何落地的读者。

**不适合**：

- 需要**对外 REST / GraphQL API** 的集成场景 —— 本项目为 Web Forms 门户，**无此类对外面**（边界判定见 [contracts/README.md](contracts/README.md)）；
- 需要现代前端框架（React / Vue）或前后端分离架构的场景；
- 直接上生产且要求生产环境验证证据的场景 —— 该证据尚未提供（见上节）。

## 三、能力地图

| 能力域 | 内容 | 权威文档 |
| --- | --- | --- |
| 身份与授权 | 账户注册、角色权限实体授权、**按组织子树的数据范围控制**、认证角色 Cookie | [architecture.md](architecture.md) |
| 流程状态机 | 业务申请与协同事项的**显式迁移表**；写入前 fail-closed，迁移必写事件 | `business-application-state-machine.md`、`collaboration-item-state-machine.md` |
| 审计 | 运营审计表、保留期策略、日志维护与归档 | `audit-retention-policy.md`、`operations-runbook.md` |
| 模块包治理 | 受信任模块包（`module.json`）的注册、启停、移除与边界校验 | `module-development-guide.md` |
| 主题治理 | 主题包契约校验与回退；7 套皮肤的 `--portal-*` 语义变量矩阵 | `theme-package-guide.md`、`theme-semantic-variables.md`、`theme-module-contract.md` |
| 业务模块 | 员工目录、待办工作台、员工资料确认 / 更正、业务申请，以及员工自助的请假申请与费用报销 | [user-guide.md](user-guide.md) |
| 国际化 | 中文 / 英文双语资源、强类型资源键、资源契约门禁 | `dev-guide.md` |
| 自动化门禁 | 按 **L0（静态）/ L1（构建后）/ L2（运行期）** 分层统一编排，退出码与输出双判定 | `testing-checklist.md`、`dev-guide.md` |

> **门禁数量以实跑为准**：编排层按层执行，规模随治理推进而增长；不要引用历史文档中的固定数字，应运行 `dev/scripts/Invoke-PortalGateSuite.ps1` 看当期汇总。

## 四、边界（明确不做）

- **不提供对外 REST / GraphQL API**，也不提供 OpenAPI 一类的机器可读接口契约；
- **不提供生产环境验证证据**；
- 不支持现代前端框架或前后端分离；
- 生成文档目录（`src/Documentation/`、`src/Portal.Components.Data/Documentation/`、`src/DoxyGen/`）为**生成物输出**，不是维护文档入口（见 `documentation-artifacts-guide.md`）；
- 内部计划、ADR、AI 协作资料统一留在**独立的私有仓库**中，不进公开 `docs/`。

## 五、兼容性与运行环境

| 项 | 要求 / 说明 |
| --- | --- |
| 操作系统 | Windows |
| 运行时 | **.NET Framework 4.8**（需 Developer Pack） |
| 构建 | Visual Studio 2022，或可构建 .NET Framework 4.8 的 VS / Build Tools |
| 数据库 | SQL Server 或 SQL Server LocalDB |
| Web 服务器 | IIS Express（本地）/ IIS（部署） |
| 浏览器兼容 | 面向 **IE9+** 的兼容基线（CSS 采用"显式值在前、`var()` 在后"的双声明范式提供回退） |
| Node.js | **仅在**需要维护前端资源构建时需要 |

浏览器兼容与 CSS 回退约定的细节见 `frontend-asset-guide.md` 与 `theme-module-contract.md`。

## 六、许可证与归属

本项目采用 **[MIT](../LICENSE)** 许可（`LICENSE` 在仓库根目录）。

它 fork 自 **ASP.NET Portal Starter Kit**；上游的许可与归属说明见 [README-old.md](../README-old.md)（同样在仓库根目录）。第三方依赖的用途、许可证与分发边界见 [third-party-dependencies.md](third-party-dependencies.md)，字体相关见 `font-policy-and-audit.md`。

## 七、下一步读什么

- 想**跑起来**：[user-guide.md](user-guide.md)「本地运行概览」→ `dev-guide.md`
- 想**用起来**（模块、请假 / 报销）：[user-guide.md](user-guide.md)
- 想**开发模块**：`module-development-guide.md`
- 想**改主题**：`theme-package-guide.md` → `theme-semantic-variables.md`
- 想**部署**：`deployment-checklist.md` → `operations-runbook.md`
- 想**查全部文档**：[documentation-map.md](documentation-map.md)
