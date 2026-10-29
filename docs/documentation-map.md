# 文档地图 / 导航索引（HIA-ASPNETPortal）

> 按 7 个文档族对 `docs/` 现有文档做导航索引：本图记「在哪、怎么串」。

## 读者路径
- **用户/实施方**：产品功能与使用 → 运维部署安全 → 变更兼容发布
- **开发者/贡献者**：架构设计决策 → 开发者与贡献者 → API 与技术契约 → 代码内文档（源码）
- **审计/安全**：运维部署安全 → 架构设计决策 → 变更兼容发布

## 两条读者路径与分区归属（2026-10-29 确立）

站点按**读者**分轨，两轨互不复述（复述即漂移）：

| 分区 | 读者 | 回答的问题 | 内容来源 |
| --- | --- | --- | --- |
| **学习** | 站点运营者 / 使用者 | 「这项功能怎么用？我要办一件事该怎么走？」 | 20 篇模块使用说明（`docs/modules/`）+ 业务路径（`p12-sample-business-flow.md` 等） |
| **参考** | 开发者 / 二次开发者 | 「它怎么运作？我要改它或加它该动哪里？API 在哪？」 | 契约与状态机（`docs/contracts/`、两个状态机页）、主题与资产类文档、**开发指南族**（模块开发 / 主题包 / 主题模块契约 / 前端资源 / 开发总览 / 代码文档说明页）、测试清单与文档产物指南 |

> **判定口诀**：读者要动手**改系统** → 参考；读者要动手**用系统** → 学习。
> 原「开发」分区已并入「参考」——开发者视角统一收在参考区，导航分区由 8 段变为 7 段：
> 产品概览 → 评估 → 快速上手 → 学习 → 参考 → 运维 → 变更。
> 写作体例与禁忌见内部规范（内部资料，不对外发布）。

## 文档族索引

### 1. 代码内文档（源码注释）
不在 `docs/`，位于源码节点/流程注释。

### 2. API 与技术契约（`docs/contracts/`）
- 边界判定：Web Forms 门户**无对外 REST/GraphQL API**；适用"模块契约 / 配置契约 / 企业能力对象契约"。
- `contracts/README.md` — 契约总览、边界判定表（REST/GraphQL/OpenAPI **不适用**）、权威源指向与验证方式
- `contracts/module-manifest.md` — 模块契约（`module.json` 字段、`schemaVersion` 1/2、可选 `capability`、被显式拒绝的声明）
- `contracts/app-settings.md` — 配置契约（`appSettings` 键族、两层生效机制、档位与包放行的区别）

### 3. 产品功能与使用
- `product-overview.md` — **产品概览与定位**：是什么 / 给谁用 / 能力地图 / 边界 / 兼容性与运行环境 / 许可证与归属 / 适用场景
- `user-guide.md` — 运行与使用入口；含**典型门户配置流程**、**管理员常用操作**、**故障排除**
- `p12-sample-business-flow.md` — 示例业务流程
- `first-use-portal.md` — **第一次使用本站**：使用者轨的入口页（登录 → 认识首页三栏 → 认识登录后菜单 → 看待办 → 回来源模块办理 → 排查表），按双轨规范的「学习」骨架写成
- `business-application-workflow.md` — **业务申请：从提交到办结**（端到端任务路径：提交 → 提交后发生了什么 → 退回后重提 → 为什么看不到审批过程 → 没提交成功时）
- `leave-and-expense-workflow.md` — **请假与报销：从填写到出现在待办里**（两个同构模块一次讲清：字段差异、提交后落库与待办、失败路径）
- `profile-confirm-and-correct.md` — **员工资料：确认与更正**（只读确认 → 发现错误 → 提交更正申请 → 谁在处理 → 看不到进度时）
- `operator-handbook.md` — **运营者手册：把门户用起来**（发公告 → 调整页签与可见性 → 模块启停与设置 → 后台页面速查；含"公告游客可见暂无后台入口""页签排序暂无后台入口"两处如实说明）
- `module-mechanics-and-extension.md` — **模块运作与二次开发**（参考轨总纲：五层结构 → 档位 → 加一个模块 → 改外观 → 四道门禁 → API 文档在哪）
- **双轨重划后的缺口（2026-10-29 登记并已补齐）**：端到端任务路径 3 条（业务申请 / 请假与报销 / 资料确认与更正）、运营者手册 1 本、参考轨总纲 1 篇均已上架；`p12-sample-business-flow.md` 保留为**实施与回归**视角并移入参考分区。
- **剩余缺口**：**模块逐项说明已无缺口**。（~~运行截图~~ 已交付为可复现的运行截图基线；~~既有内容模块逐项操作说明~~ 已交付；~~请假与报销的审批流转界面~~ 已交付为明确的边界声明，见 `user-guide.md`「审批流转边界（必读）」；~~`HTML` 模块~~ 已交付；~~`QuickLinks` 模块~~ 已交付；~~`XML / XSL` 模块~~ 已交付；~~`Signin` 模块~~ 已交付，见 `user-guide.md`「登录（Signin）」）
 - **证据边界（如实登记）**：`Signin` 模块在开发库中 **0 个部署实例**，因此**只有源码级说明、无运行期证据**；再入条件：目标环境挂载该模块实例后采集运行期证据并补齐实测读数。
 - **代码文档**：JSDoc 半边已发布（`/api/jsdoc/`，中英双语，说明页 `code-documentation.md`，源指纹由 L0 门禁看守）；DotNetDoc 半边仍是**中间 JSON、未上架**，再入条件：渲染/发布组件就位后按同一快照清单机制补入。


### 4. 开发者与贡献者
- `dev-guide.md` — 开发总指南
- `module-development-guide.md` — 模块开发指南
- `frontend-asset-guide.md` — 前端资源指南
- `theme-package-guide.md` — 主题包指南
- `theme-semantic-variables.md` — 主题语义变量矩阵（7 主题 × **14 变量**、用途速查、使用规则）
- `theme-module-contract.md` — 模块外观契约（层间分工、外观类族、回退值含义与差异）
- `documentation-artifacts-guide.md` — 文档产物指南
- `testing-checklist.md` — 测试清单

### 5. 运维部署安全
- `deployment-checklist.md` — 部署清单
- `deployment-default-credentials.md` — 默认凭据处置
- `deployment-rollback-guide.md` — 回滚指南
- `operations-runbook.md` — 运维手册
- `audit-retention-policy.md` — 审计留存策略
- `font-policy-and-audit.md` — 字体策略与审计
- `third-party-dependencies.md` — 第三方依赖

### 6. 架构设计决策
- `architecture.md` — 总体架构
- `business-application-state-machine.md` — 业务应用状态机
- `collaboration-item-state-machine.md` — 协作项状态机


### 7. 变更兼容发布
- `versioning.md` — 版本与兼容策略
- `release-notes-template.md` — 发布说明模板
- `CHANGELOG.md`（仓库根）— 变更日志

## 元文档
- `site-visual-baseline.md` — **官网视觉基线**（配色 / 字体 / 排版层级 / 代码块与表格取色 / 导航骨架）
- `README.md` — 仓库入口（含文档分区导航指向本图）
