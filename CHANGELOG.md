# 更新日志

本项目变更记录。版本规则见 [版本策略](docs/versioning.md)。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [SemVer](https://semver.org/lang/zh-CN/)。

## [v0.8.0] - 2026-10-07

**文档就绪与开源就绪度**（`C-anp-P15`：`W93`–`W98`）。本版本补回 `C-anp-P10` 原本为 `C-anp-P14` 规划、但被门禁体系化顶替掉的主题，对应首发里程碑 `M-ANP-RELEASE-READY-PORTAL` 的 **R4 / R5** 两条判据。

### 已加入

- **README 重写**（2.5 KB → 6.1 KB）：补功能特性表、**7 步快速上手**、文档导航表、许可证与贡献指引、成熟度口径。每一步都指向实测存在的脚本与模板；明确写出"连接串必须放在仓库外"是安全要求而非配置负担。
- **`CONTRIBUTING.md` 与 `.github/`**：贡献指南 + 缺陷报告 / 功能建议两个 Issue 模板 + PR 模板。贡献指南的重点是一张**"改动什么范围就额外跑哪个门禁"**的对照表 —— 本项目有 22 个门禁在统一编排之外，它们同样有效但不会自动执行。
- **模块开发指南扩容**（4.8 KB → 10.9 KB）：原指南只讲清单、安装与生命周期，缺新模块必须遵守的约定。新增代码骨架、权限与数据范围、**语义标记与可访问性约定**、冒烟门禁十项 Fail 级硬约束对照表、常见故障定位表、提交前检查清单。
- **通用密钥泄露扫描门禁**（`Test-PortalSecretLeakage.ps1`）：覆盖私钥块、云厂商访问键、访问令牌、JWT 等高置信度形态。**缺口实测**：此前 `Test-PortalDefaultCredentialRisk.ps1` 只覆盖"默认凭据风险"语义、`Test-PortalPublicDocumentation.ps1` 只覆盖公开 Markdown 的赋值形态 —— 提交一个私钥或 `AKIA` 开头的键，**全部现有门禁都会通过**。

### 已修复

- **3 篇公开文档指向私有仓库 `work-zone/`**（外部点击必然 404）：改为保留阶段号文本、标注"内部计划，未公开"，保留可追溯性而不暴露私有路径。`docs/documentation-artifacts-guide.md` 自己就写着"公开文档不得链接到其中的文件" —— 规则已立，另 3 篇违反了它。
- **4 篇公开文档未被 `docs/README.md` 索引**，含 **`versioning.md`**（版本策略文档，而 `CHANGELOG.md` 首页正链接它）。
- **门禁的"假失败"**：新密钥门禁的计数原本按传入 `Severity` 递增，导致 `Severity=Fail` 但 `Passed=true` 的检查（"未命中密钥"本就期望通过）也把 `failCount` 加一，于是四项全显示 PASS 却仍 `exit 1`。这与本项目反复遇到的"假通过"同源 —— 信号与事实不一致时，门禁会被当成噪声整体忽略。
- **运行期门禁的时序缺陷**：统一编排跑全量时，L1 构建替换站点 `bin` 触发一次应用域回收，L2 切档位改 `web.config` 又触发一次，导航恰落在回收窗口内会以 `net::ERR_ABORTED` 失败，表现为**"单独跑 L2 通过、跑全量 L2 失败"**，极易误判为代码回归。改为对导航做有限次递增间隔重试，而不是把猜测的等待时长写死。

### 验证

- 门禁套件 **18 个全绿**（L0 由 6 扩到 **10**，L1 八项，L2 一项），失败 0、跳过 0。
- `Test-PortalPublicDocumentation` 由 **Fail 转 Pass**。
- 两个新门禁均用**注入法双向证明有效**：植入 `AKIA` 开头的键后 `exit 1` 并精确报出 `Web.config:479`，还原后 `exit 0`；编排层删掉 `docs/README.md` 一行索引后立即报 `Fail`，还原后转 `Pass`。

### 一条方法论教训

`Test-PortalPublicDocumentation` **早已存在**，并且准确报出了上述两个真实文档问题。它之所以从未被处理，**唯一原因是未接入统一编排**。

> **门禁写好了但没接进编排，等于没写。**

`v0.6.0` 建了统一编排，但只统一了 14 个门禁，把另外 22 个留在编排之外 —— 于是"门禁体系化"这个成果的**覆盖率被高估了**。

## [v0.7.0] - 2026-10-07

**界面与语义打磨收口**（`C-anp-P14`：B2 + C1–C4）。本版本是 `v0.6.0` 门禁体系化的**首次应用** —— 上一版本建立的门禁套件在本批改动全程保持全绿。

### 已加入

- **C1 三个业务模块补标题语义**（`W82` 遗留未闭合项）：实测它们并非"无标题"，而是标题用**普通 `div`**，共享标题控件的语义覆盖不到。改为只加 `role="heading"` 与 `aria-level`，`class` 与元素名不动 → 视觉零变化。运行期实测三个模块均 `role=heading aria-level=1`。
- **C2 只读字段语义化**（用户裁定方案 A，接受视觉变化）：`EmployeeProfileConfirm` **7 对** + `EmployeeProfileCorrectionRequest` **5 对**只读字段由 `span` 标签改 `<dl>/<dt>/<dd>`（中间层 `div` 保留 —— HTML5 允许 `dl` 内用 `div` 分组）；6 套皮肤补**作用域限定**的 `dl{margin}` 等规则，不污染其他 `dl`。
- **C3 参与人逐人一行**：数据层新增 `BuildParticipantLines`，与逗号串**共用同一格式串**（避免两处格式漂移）；前台工作台与后台协同事项均改 `ul/li` 渲染。
- **C4 后台列表页列宽相对化**：**7 个页面**逐表配平（沿用 `W79` 已确认的 D2 方案）；`EmployeeDirectory` 三张表分别配平；`DiagnosticLogDetail` 仅 1 个固定列，未纳入。
- **B2 对标证据索引**：新增 `Get-PortalBenchmarkIndex.ps1`（inventory 工具，非门禁），输出 JSON + Markdown 双视图。

### 已修复

- **验证脚本盲区**（配套 C1）：原脚本只查共享标题控件 `.portal-module-title`，抓不到自带标题的模块 —— 会把"已修好"误判成"未修复"。已改为同时覆盖两类来源。
- **账本编号冲突**（如实登记，暂不擅自修正）：`C-anp-P14` 中 C1 的包号与 A3 重复（同为 `W87`），实际编号待确认后再改。

### 验证

- 门禁套件 **14 个全绿**（`Invoke-PortalGateSuite.ps1`，L0/L1/L2 分层，失败 0、跳过 0）。
- 语义标记门禁**双档位**通过：`BusinessWorkflow` 档位 front 4 模块全 Pass（含 C1 新增的三个 `div` 标题）；`LegacyContent` 档位 legacy 组全 Pass，且模块标题计算样式与 `v0.6.0` 基线**逐项一致**（`21px/600`、`margin 0px,0px`、`line-height 27.3px`）→ 本批改动未引入意外视觉变化。
- 模块运行期门禁 Pass：**38 个模块实例 / 11 个 URL**。

## [v0.6.0] - 2026-10-07

**门禁体系化**（`C-anp-P14`：W85–W88）。本版本不含功能改动，全部是**机制性改进**：把散落、靠人记的门禁变成一条命令可跑、失败信号统一、覆盖面可核查的体系。

### 已加入

- **W85 门禁统一编排入口**：新增 `dev/scripts/Invoke-PortalGateSuite.ps1`，按 **L0（只依赖源码）/ L1（构建后）/ L2（需站点）** 三层编排，避免"环境没起"与"代码有问题"两种失败互相掩盖。采用**退出码 + 输出双判定**（38 个 `Test-*` 里只有 14 个设了退出码，只靠 `$LASTEXITCODE` 会把只打印 PASS/FAIL 的判成通过），且每个失败标志配否定式（`Failed: False` 必须判通过）。失败不中断、一次跑完再汇总，每个门禁的完整输出留档。
- **W87 ASCX 编译契约门禁**：新增 `Test-PortalAscxCompilationContract.ps1`，覆盖**全部 24 个 ASCX**（含 5 个无可访问 URL 的后台管理型模块）。四类检查中的 **C3 把 `CS0103` 从"运行期才能发现"前移到静态可判定**（`P74.3` / `P81` 的故障类型）。
- **W88 对标检查表门禁**：新增 `Test-PortalBenchmarkChecklist.ps1`，把规范 §七 的 7 条检查表固化为可重复运行的检查（规范：`work-zone/docs/standards/detail-level-benchmark-spec.md`）。

### 已修复

- **W86 业务模块冒烟门禁**：修复三处同源缺陷（包名重复加 `HIA.` 前缀、`desktopEntry` 越界误判、迁移文件按模块名通配找不到），此前对当前项目结构**必然失败**；修复后 5 个模块全部接回编排。
- **模块 selector 提取**（`W83` 遗留）：旧内容模块只有 `portal-` 前缀的 class，而提取逻辑恰好排除该前缀，导致 11 个目标**全部** `module=undefined`、断言退化为"只断言未落错误页"（比改造前更弱却显示通过）。修正为"在 ascx 中只出现一次的 class"后，11 个目标全部 `module=true`，断言由 1 条变 2 条。

### 验证

- 门禁套件 **14 个门禁全绿**（从 `v0.5.0` 时的 7 个扩到 14 个）。
- 三个新/修门禁均用**注入法**证明有效（只跑通不算有效）：注入违规 → 门禁 `Fail` → 还原 → `Pass`。

## [v0.5.0] - 2026-10-05

**Cycle 13 打磨收口**（`C-anp-P13`：W76–W83）。本 Cycle 围绕"实测优先、登记可纠"推进，多处台账登记在勘察后被实测推翻并修正。

### 已加入

- **W77 可达性与深链**：新增可达目标解析器（反查链每环独立降级）与共享页签地址构造，业务对象在前台可跳转；跳转审计常量收敛并追加解析统计。
- **W80 双语一致性治理**：补齐 **283 条** en-us 资源条目、**16 个**强类型属性、**17 个**缺失的 `<summary>` 双语块；新增可复用门禁 `dev/scripts/Test-PortalResourceContract.ps1`（六项契约检查，修复前 36 条违规 → 修复后 0）。
- **W81 平台与旧内容模块空态**：**7 个列表**接入共享空态（6 个 Repeater 走 `FooterTemplate`，`ModuleCatalog` 走 `EmptyDataTemplate` + `ShowHeaderWhenEmpty`）；**3 处失败/输入无效分支抑制空态**（"服务不可用"不是"没有数据"）；新增 8 条资源键。
- **W82 语义标记补齐**：**28 处** `<th>` 补 `scope="col"`（5 模块）、**13 处**表单标签改 `<label for>`（映射逐字段人工确认）、模块标题加 `role="heading"` + `aria-level`（前台 1 / 后台 2）。
- **W83 运行期门禁目标自动发现**：新增编排层 `dev/scripts/Invoke-PortalModuleRuntimeGate.ps1`，模块目标改由**数据库**枚举（原先硬编码 4 个），覆盖 **4 → 38 个模块实例 / 11 个 URL**；档位由脚本自管并 `try/finally` 兜底还原。

### 已改进

- **W76 八维度盘点**：产出逐可见面的「现状 / 对标对象 / 来源强度 / 差异与决定」清单，为后续打磨包提供排序依据。
- **W78 异常与反馈文案打磨**：**13 处** `(none)` + 2 处 `" / Overdue"` + 1 处 `"—"` + 2 处 `"(not reviewed)"` 全部资源化为可理解、可行动的本地化文案；两处字符串拼接改为整句本地化。
- **W79 协同事项后台列结构重排**：末列 1 → 3（处理意见 / 操作 / 参与人），表头 7 → 9 列全部带 `scope="col"`；控件宽度全部相对单位化；**命令名、参数、授权判定一行未动**。

### 验证与门禁

- 新增可复用运行期证据脚本：`Test-PortalSemanticMarkupEvidence.mjs`（双轮取证，证实"改语义标签而 class 不动"确为**零视觉变化**）、`Test-PortalPlatformEmptyStateEvidence.mjs`（三态验证 11/11）。
- `W83` 以"注入未知类型 → 门禁 Fail → 还原 → Pass"**证明门禁本身有效**，而非仅跑通。

### 登记纠正（如实记录，供后续核对）

本 Cycle 多处台账登记在实测后被推翻：`W76` 记的"前台 3 面缺 `scope`"实为 **5 模块 / 28 处**；`W80` 记的"`lang.designer.cs` 113 行乱码"早在 `W78` 已由提交 `2a2901e` 闭合；`W82` 记的"25 处表单标签"实为**仅 13 处该改**（另 12 处是只读展示，`for` 指向非表单控件属错误语义）；`W83` 记的"运行期门禁待建"实为 `P74.6` 已建、真实缺口是覆盖不足。

## [v0.4.0] - 2026-10-02



**BasicBusiness（基础业务）能力初步完善**（`C-anp-P12`：W67–W75）。

### 已加入

- **W67 `WorkItems` 前台「我的待办」聚合入口 + 操作审计**：数据层 `PortalWorkItemQueryResult` 与 `IPortalWorkItemDb.GetWorkItemsForUser`（fail-closed、空/失败语义分离、角色键逐参数绑定）；前台模块 `HIA.MyWorkItems`；查看与筛选操作审计。
- **W68 `EnterpriseDirectory` 组织子树查询 + 异常语义修正**：`EmployeeDirectoryQuery` 增加 `OrganizationUnitId`（fail-closed）与 `IncludeDescendants`；`IEmployeeDirectoryDb.GetOrganizationUnitSubtreeIds`；纯函数 `PortalOrganizationTreeExpander`（数据范围深化的数据层前置）。
- **W69 `CorrectionRequest` 闭环（审核 → 回写主数据）**：审核通过按服务端白名单真正回写员工主数据（此前只改状态），含回写策略纯函数与事件/审计；按裁定 `D4` **不引入二次审批**。
- **W70 `EmployeeProfileConfirm` 前台权限 + 幂等 + 本地化**：渲染期权限门禁 + 提交前二次校验 + 授权失败审计；确认由"快照追加"改为幂等；清理未本地化硬编码。
- **W71 `ApplicationRequest` 状态机显式化 + Resubmit**：显式迁移表作为单一事实源（含 `ViaReviewPath` 隔离自助动作），映射与 SQL 守卫均由表派生，并附**等价性单测**；新增 `ResubmitApplication`。
- **W72 负责人角色键读取边界对齐**：修复 `CanParticipate`（写资格）与 `CanView`（读资格）不一致，持有负责人角色键须两者同时通过。
- **W73 治理**：`Business.Workflow` 归属合并到 `BusinessApplicationRequest`（`D1`）；`EmployeeProfileConfirm` / `EmployeeProfileCorrectionRequest` 补 `capabilityId`，`ModuleProbe` 明确归 Platform（`D2`）。

### 已改进

- **W74 UI 打磨（空态 / 资源键 / 角色名 / N+1）**：
  - **列表空态**：业务前后台共 **8 个列表**接入共享空态（表头保留 + 整行居中弱化提示），并区分"无数据"与"筛选无结果"；服务不可用路径**抑制空态**，不把故障说成"暂无"。
  - **资源键归属**：消除跨模块借用（借用方建自有键；角色词表等共享领域事实改用共享键），清理失效键。
  - **参与人角色名本地化**：界面不再暴露内部权限键（`Collaborator` / `Watcher`），改为本地化角色名，未知键回退原始键；空集合用本地化占位。
  - **N+1**：协同事项列表改为批量取数（逐行约 4N 次往返 → 常数次），**逐条可见性校验保留**。
  - **主题适配**：`MyWorkItems` 与工作台在 6 套正式皮肤补齐主题作用域规则，修复深色皮肤下"浅字压白底"（实测对比度由约 1:1 提升到 12.9:1）。
  - **模块注册**：通用场景脚本 `New-PortalModuleScenarioSql.ps1`；`HIA.MyWorkItems` 补齐模块定义/包状态/页签/实例，并归入新增 `BasicBusiness` 档位。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`）：R1 随本周期推进（BasicBusiness 七方向达"初步完善"）、R7 达成；R2/R3/R4/R5 部分达成；R6 未达成 |

### 说明

- 本版本为**内部基线锚点**，不对外宣传；`v1.0.0` 之前口径限于"可用 / 参考 / 研究基线"，不得宣称生产级可信。
- 本周期起将**运行期验证**（LocalDB + IIS Express + 浏览器断言）纳入常规门禁，并新增 `Test-PortalModuleRuntimeEvidence.mjs` 与 `Test-PortalAdminListUiEvidence.mjs` 两个证据脚本；它当场发现了静态门禁看不到的问题（ASCX 运行期编译错误、深色皮肤可读性）。

## [v0.3.0] - 2026-09-28

**Foundation（核心）能力初步完善**（`C-anp-P11`：W61–W66）。

### 已加入

- **W61 密码成本与版本语义**：PBKDF2 迭代下限 `210000 → 600000`（依据 OWASP Password Storage Cheat Sheet，目标环境实测 407 ms < 1 s）；`minimumPortalVersion` 语义在 `docs/versioning.md` 第八节定义（模块对宿主门户的兼容性声明，与产品版本号是两个独立维度）；上传白名单移除 `zip`（OWASP 不建议）。
- **W62 审计补齐**：审计门面支持 `Outcome` 参数化；**认证失败**入审计（`Signin.ascx.cs` 失败分支）；**授权失败**入审计（`PortalNavigationPolicy` 两个集中拒绝出口）——补齐 OWASP Logging Cheat Sheet 要求的必须事件类型；新增保留期策略 `docs/audit-retention-policy.md`。
- **W63 数据范围契约**：新增 `CollaborationItemDataScope` 与 `CollaborationItemDataScopePolicy.CanView`（判定顺序 管理员 → 归属 → 参与人 → 组织，**fail-closed**）；列表与详情共用同一判定且由服务端强制；既有 `CanParticipate` 语义未改变；**新增 10 例单测**。
- **W64 状态机显式化**：新增 `PortalCollaborationItemTransitions`（8 状态 / 8 动作 / 15 条迁移的单一事实源）；`MapActionToStatus` 与 SQL 写入守卫谓词均由该表派生，消除映射漂移；`ApplyAction` 新增 `IsLegalTransition` 服务端门禁；迁移必写 `WorkflowAction` 事件（`FromStatus`/`ToStatus` 留痕）；新增状态流转说明 `docs/collaboration-item-state-machine.md`；**新增 6 例单测**。
- **W65 Watcher 语义修正**：`Watcher` / `Collaborator` 描述与"待办即通知"定位对齐，消除"关注者仅接收通知"语义与实际无通知发送之间的落差；全仓确证协同事项无独立通知发送。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`）：**Foundation 侧已达初步完善**，BasicBusiness 侧待 `C-anp-P12` 执行（见 `work-zone/dev/milestones/M-ANP-RELEASE-READY-PORTAL.md`） |

### 说明

- 本版本为**内部基线锚点**，不对外宣传；`v1.0.0` 之前口径限于"可用 / 参考 / 研究基线"。
- 质量门禁：构建 `0 错 0 警`、单测 `86/86`、XML 文档门禁通过。
- `C-anp-P11` 的规划产出（`W66` closeout 与 `C-anp-P12` 候选蓝图）属内部计划，见 `work-zone/`。

## [v0.2.0] - 2026-09-28

**版本机制 + 两大能力层盘点与深度对标**（`C-anp-P10`：W56–W60）。

### 已加入

- **W56 版本号机制建立**：`docs/versioning.md` 版本策略（SemVer + 版本与程序集联动 + 发行流程 + 宣传口径约束 + `minimumPortalVersion` 语义）、`CHANGELOG.md`、`git tag v0.1.0`（已推送）、一致性门禁 `dev/scripts/Test-PortalVersionConsistency.ps1`，5 个程序集版本联动。
- **W57 Foundation 能力盘点与深度对标**：八个条目（身份账户、组织拓扑、授权与数据范围、流程编排、消息、文件、审计、集成标识）逐一给出现状 / 缺口 / 对标 / 建议，产出 6 份研究文档并登记来源强度与不适用清单；并产出 `C-anp-P11` 候选蓝图。
- **W58 BasicBusiness 能力盘点与对标**：七能力方向盘点（能力登记 2 条、孤儿模块 3 个、**业务单测覆盖 1/7**、Top 共性缺口 8 项）+ 外部对标（转引本项目已核实的一手来源）+ `C-anp-P12` 候选清单定稿为 P1/P2/P3。
- **W59 细节级对标规范**：`docs/detail-level-benchmark-spec.md`（八维度 + 来源强度标注 + 不适用清单 + 验收检查表），供 P11–P13 复用。
- **W60 P10 收口**：closeout、建立 L10 里程碑文档 `M-ANP-RELEASE-READY-PORTAL` 并登记七条判据与基线证据、本版本 tag。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`；**本 Cycle 新登记**，判据与基线证据见 `work-zone/dev/milestones/M-ANP-RELEASE-READY-PORTAL.md`） |

### 说明

- 本版本为**内部基线锚点**，不对外宣传；`v1.0.0` 之前口径限于"可用 / 参考 / 研究基线"，不得宣称生产级可信。
- `C-anp-P11`（Foundation 能力初步完善）的源码成果已合入，其版本锚点 `v0.3.0` 按推荐序列作为**后续发行动作**推进（不在本版本内）。

## [v0.1.0] - 2026-09-25

**机制闭环基线**（`C-anp-P9`：企业能力机制闭环深化与边界确证，W50–W54）。

### 已加入

- **W50 模块装配双模式原型验证**：本机确证 `CodeBehind`/`CodeFile` 精确运行时语义与 WAP 内用 `CodeFile` 的后果、C1-CMS 切换细节；裁定"实现另立项，不碰在线编译 / 在线上传"。
- **W51 第二能力样板复制验证**：`HIA.BusinessApplicationRequest` 补 `manifest v2` `capability` 段，能力词表注册 `BasicBusiness.ApplicationRequest`，确认 `BusinessWorkflow` Profile gate；验证能力归属机制可复制（构建 0 错 0 警、单测 70/70）。
- **W52 能力管理独立页复核**：维持不建独立页，保持 `Admin/CapabilityPermissionMatrix.aspx` 只读诊断视图（能力 Primary 样板 2 块 < 阈值 ≥5）。
- **W53 边界治理端到端验证**：`EnterpriseCapability.*` 分层键族、Profile gate、装配态迁移三块边界受控验证；零代码改动。
- **W54 W44/W45 残留收尾**：
  - `Admin/CollaborationItems.aspx` 参与人角色下拉两处硬编码显示名本地化（新增 `Admin_CollaborationItems_RoleType_Collaborator/Watcher` 三语资源键 + ROP 注释）；
  - 英文侧 gate 开态截图 46 张自动补采并归档（LocalDB + IIS Express + Chrome headless），UI 文本级验证 `ParticipantRoleList` = `Collaborator`/`Watcher`；关闭 W44.5/W45.5 长期登记的"英文侧 gate 开态未采"已知边界。

### 已修复

- 账目一致性：`TASK_STATE` 的 Current Goal 字段长期停留在旧周期（`C-anp-P6`/`W35`），已更新至 `C-anp-P9`/`W55` 并登记下一周期组 `C-anp-P10`。

### 里程碑状态（截至本版本）

| 里程碑 | 级别 | 状态 |
| --- | --- | --- |
| `M-ANP-MAINTAINABLE-BASE` | L3 | 已达成 |
| `M-ANP-OPERABLE-PORTAL` | L4 | 已达成（当前基线） |
| `M-ANP-EXTENSIBLE-PORTAL` | L5 | 已达成 |
| `M-ANP-DOCUMENTED-PORTAL` | L6 | 已达成 |
| `M-ANP-TRUSTED-PORTAL` | L7 | 评估中，未无条件达成 |
| `M-ANP-BUSINESS-READY-PORTAL` | L8 | 条件式达成 |
| `M-ANP-ENTERPRISE-UI-PORTAL` | L9 | 条件式达成 |
| `M-ANP-RELEASE-READY-PORTAL` | L10 | 未达成（目标 `v1.0.0`） |

### 说明

- 本版本为**内部基线锚点**，不对外宣传。
- 能力词表当前登记 2 条（`BasicBusiness.Collaboration`、`BasicBusiness.ApplicationRequest`），均属 `BasicBusiness` 层。
