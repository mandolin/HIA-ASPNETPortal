---
layout: default
lang: zh-CN
parent: 产品概览
permalink: /docs-index/
title: HIA-ASPNETPortal 文档
---
# HIA-ASPNETPortal 文档

本目录是项目的公开维护文档入口，面向使用者、开发者和后续维护人员。

## 文档索引

- `architecture.md`：系统结构和主要模块说明。
- `dev-guide.md`：本地开发、构建、配置和调试指南。
- `versioning.md`：版本号规则、版本与里程碑的关系、程序集版本联动、发布流程与宣传口径约束。
- `frontend-asset-guide.md`：Web Forms 呈现、主题、模块 CSS、Gulp 与前端资产边界。
- `theme-package-guide.md`：受信任主题包的目录、启用、回退和发布边界。
- `theme-semantic-variables.md`：主题层 `--portal-*` 语义变量的矩阵（7 套主题 × **14 个变量**）、用途速查、使用规则与复现命令。
- `theme-module-contract.md`：模块外观契约（主题层与模块层的分工、七族外观类与所用变量、`var` 回退值的真实含义与已审计差异）。
- `module-development-guide.md`：受信任部署模块包的开发、注册、启停和移除边界。
- `product-overview.md`：产品概览与定位（是什么、给谁用、能力地图、边界、兼容性与许可证、适用场景）。
- `user-guide.md`：运行和使用入口说明，含门户配置流程、管理员常用操作与故障排除。
- `testing-checklist.md`：构建、测试、手工验证和发布前检查清单。
- `documentation-artifacts-guide.md`：公开文档、JSDoc、XML 文档和生成物的输入输出边界。
- `documentation-review-checklist.md`：文档复核**人工走查清单**（正确性 / 双语对等 / 读者可达性 / 不误信门禁），对应自动化门禁无法证明的部分。
- `contracts/`：技术契约目录 —— 边界判定（无对外 REST/GraphQL）、[模块契约]({{ site.baseurl }}/contracts-module-manifest/)（`module.json`）、[配置契约]({{ site.baseurl }}/contracts-app-settings/)（`appSettings`）；入口见 [contracts/README.md]({{ site.baseurl }}/contracts-readme/)。
- `site-visual-baseline.md`：**官网视觉基线**（配色 / 字体 / 排版层级 / 代码块与表格取色 / 导航骨架），是 `site/` 取值的依据。
- `documentation-registry.md`：7 个文档族的适用性登记（适用对象、维护责任、更新触发条件）与已登记缺口。
- `documentation-map.md`：7 族文档的导航地图、阅读路径与缺口指向，用于按角色快速定位文档。
- `deployment-checklist.md`：SQL Server、IIS、外置配置和发布后回归清单。
- `deployment-rollback-guide.md`：文件、配置、数据库和证据回滚准备说明。
- `release-notes-template.md`：版本号、变更、验证摘要、延期补证和回滚入口模板。
- `operations-runbook.md`：运维入口、例行检查、日志维护、审计查询、备份提醒和证据包说明。
- `deployment-default-credentials.md`：默认账号、初始化脚本样例口令和旧凭据治理说明。
- `p12-sample-business-flow.md`：员工资料更正样板业务路径、样例数据 SQL 和 验收入口。
- `business-application-state-machine.md`：业务申请的状态词汇、允许迁移与 fail-closed 写入约束。
- `collaboration-item-state-machine.md`：协同事项的状态词汇、允许迁移与处理角色语义。
- `audit-retention-policy.md`：运营审计表的保留期原则与依据。
- `font-policy-and-audit.md`：字体使用原则、许可证边界和当前审计结果。
- `third-party-dependencies.md`：新增第三方依赖的用途、许可证和分发边界。
- `modules/announcements.md`：**公告模块**详细使用说明——字段表、必填与日期比较校验、有效期语义、存储与审计、权限、常见问题。
- `modules/contacts.md`：**联系人模块**详细使用说明——字段表、唯一必填项、无格式校验的风险、存储与审计、权限、常见问题。
- `modules/discussion.md`：**讨论模块**详细使用说明——详情页参数、主题与回复层级、无必填校验的事实、存储与审计、权限、常见问题。
- `modules/document.md`：**文档模块**详细使用说明——字段表、上传与路径安全策略、存储与审计、权限、常见问题。
- `modules/events.md`：**事件模块**详细使用说明——字段表、四项全必填、到期过滤、存储与审计、权限、常见问题。
- `modules/image-module.md`：**图片模块**详细使用说明——字段表、零必填零校验、只存文本设置不做上传、存储与审计、权限、常见问题。
- `modules/links.md`：**链接模块**详细使用说明——字段表、三处必填与整数顺序校验、链接归属校验、仅测试页签的部署现状、存储与审计、常见问题。
- `modules/quick-links.md`：**快速链接**详细使用说明——与链接模块共用数据与编辑页、导航策略对不合规地址的抑制、权限模型、存储与审计、常见问题。
- `modules/html-module.md`：**HTML 文本模块**详细使用说明——字段表、零必填、原始 HTML 窄权限与不做净化的安全边界、存储与审计、常见问题。
- `modules/xml-module.md`：**XML / XSL 模块**详细使用说明——字段表、路径合规校验与运行期中性降级、只存虚拟路径不做上传、存储与审计、常见问题。
- `modules/index.md`：**模块详细使用说明的索引**——19 个模块各一页的详细说明（用途与前置 / 操作步骤 / 字段表 / 必填与校验 / 存储与审计 / 权限 / 常见问题 / 边界与不做），含三条横向事实（只有登录模块写运营审计、各模块校验强度差异极大、部署状态不同）。
- `modules/signin.md`：**登录模块**详细使用说明——视图控件与提交保护（一次性 RSA 公钥加密）、校验流程、本仓**唯一写运营审计**的路径、`ContentRawHtmlEdit` 之外的权限模型，以及**无编辑页 / 0 实例 / 仅源码级说明**的证据边界。
- `modules/business-application-request.md`：**业务申请模块**详细使用说明——**抽象**申请骨架（不含任何具体业务字段）、四个输入控件与长度上限、**标记层无必填校验器**（提交时由代码判定）、落表与 `BusinessApplicationSubmitted` 审计。
- `modules/employee-profile-confirm.md`：**员工资料确认模块**详细使用说明——**只读确认**自己绑定的低敏资料（无输入控件、无编辑页）、确认动作写 `EmployeeProfileConfirmed`、无权限写 `AccessDenied`、不写入业务数据。
- `modules/employee-profile-correction-request.md`：**员工资料更正申请模块**详细使用说明——提交**更正申请**而非直接改资料、可更正字段受下拉框限制、拟改值的合法性按目标字段在代码侧判定、`EmployeeProfileCorrectionRequested` 审计。
- `modules/enterprise-capability-workbench.md`：**企业能力工作台模块**详细使用说明——协作条目的提交 / 重新提交 / 参与者评论三种动作、七个输入控件与 UTC 截止时间格式、`CollaborationItemSubmitted` / `Resubmitted` / `CommentAdded` 审计、`.GetFallbackItems(` 降级路径属既定行为。
- `modules/expense-reimbursement.md`：**费用报销模块**详细使用说明——报销单字段与可选附件、落表 `PortalBiz_ExpenseReimbursements` 并写入「我的待办」、金额格式由代码侧判定（标记层不标红）。
- `modules/leave-request.md`：**请假申请模块**详细使用说明——假别 / 起止日期 / 天数 / 事由与可选证明、落表 `PortalBiz_LeaveRequests` 并写入「我的待办」、日期区间与天数在标记层无校验器（提交后才拒）。
- `modules/module-probe.md`：**模块探针模块**详细使用说明——**只读验证样例、不是业务办理入口**：呈现模块 / 包 / 位置 / 渲染时刻 / 主题作用域五项只读事实，**不写入任何数据、不写审计、无输入控件与提交动作**。
- `modules/my-work-items.md`：**我的待办模块**详细使用说明——待办聚合入口（本人或本人持有角色键所指派的未完成项）、**不提供就地办理**属既定设计、查看动作写 `WorkItemsViewed` 审计、无输入控件。
- `code-documentation.md`：**代码文档体系**——由 HIA-Documentation-Sys 从源码注释生成的 API 级文档（JSDoc 半边已发布中英双语页面，DotNetDoc 半边为中间产物）、刷新方式与依赖安装注意事项。

## 使用说明

- 本站文档同时提供中文与英文两个版本，可随时切换。
- 文档按「产品概览 / 评估 / 快速上手 / 学习 / 参考 / 开发 / 运维 / 变更」八个分区组织，见[文档地图]({{ site.baseurl }}/documentation-map/)。

<!-- build-stamp: 1FEA9315FAF9 -->
