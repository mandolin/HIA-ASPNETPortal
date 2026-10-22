# 模块详细使用说明（索引）

本目录为门户**每个内容模块**提供独立的使用说明页。一句话速查见[用户指南](../user-guide.md)；这里给出**逐模块**的：用途与前置、操作步骤、字段表、必填与校验、存储与审计、权限、常见问题、边界与不做。

## 按用途挑一页

| 页面 | 模块 | 一句话 | 控件 | 必填 | 存储 |
| --- | --- | --- | --- | --- | --- |
| [公告](./announcements.md) | 公告（Announcements） | 带有效期的站内通知，到期不再显示 | 5 | 3 | `AnnouncementsDB` |
| [联系人](./contacts.md) | 联系人（Contacts） | 人员名录，本仓校验最轻的模块 | 5 | 1 | `ContactsDB` |
| [讨论](./discussion.md) | 讨论（Discussion） | 主题式讨论区，**零必填** | 2 | 0 | `DiscussionDB` |
| [文档](./document.md) | 文档（Document） | 文档条目，带**上传与路径安全策略** | 5 | 1 | `DocumentDB` |
| [事件](./events.md) | 事件（Events） | 活动信息，四项**全必填** | 4 | 4 | `EventsDB` |
| [图片](./image-module.md) | 图片（ImageModule） | 单张图片，只存文本设置、**不支持上传** | 3 | 0 | 模块设置（`src` 等） |
| [链接](./links.md) | 链接（Links） | 链接列表，**仅测试页签**在用 | 5 | 3 | `LinkDB` |
| [快速链接](./quick-links.md) | 快速链接（QuickLinks） | 与链接**共用同一份数据与编辑页** | 5（同一编辑页） | 3 | `LinkDB`（同一份） |
| [HTML 文本](./html-module.md) | HTML 文本（HtmlModule） | 受信任管理员维护的原始 HTML，**不做净化** | 3 | 0 | `HtmlTexts`（`HtmlTextDB`） |
| [XML / XSL](./xml-module.md) | XML / XSL（XmlModule） | 渲染站内 XML，路径合规校验 + 中性降级 | 2 | 0 | 模块设置（`xmlsrc` / `xslsrc`） |
| [登录](./signin.md) | 登录（Signin） | 登录表单，**唯一写运营审计**、**无编辑页**、**0 实例** | 视图控件 6 | — | `UsersDB.SignIn` |

## 较新（目录式）模块的独立页

这一批模块位于 `src/Portal/DesktopModules/<模块名>/<模块名>.ascx`，**没有独立的 `Edit*.aspx`**——表单与列表写在同一个 `ascx` 内。因此这几页的字段表直接取自该 `ascx` 的标记，并且**门禁把 `edit` 指向这同一个 `ascx`**，使"字段集覆盖源码控件""必填数与源码一致"两项断言真正生效（若留空，门禁会跳过这两项，字段表就没人核了）。

| 页面 | 模块 | 一句话 | 输入控件 | 写入 | 审计事件 |
| --- | --- | --- | --- | --- | --- |
| [业务申请](./business-application-request.md) | 业务申请（BusinessApplicationRequest） | **抽象**申请骨架，不含任何具体业务字段 | 4 | 业务记录 + 待办 | `BusinessApplicationSubmitted` |
| [员工资料确认](./employee-profile-confirm.md) | 员工资料确认（EmployeeProfileConfirm） | **只读确认**自己绑定的低敏资料，不能改 | 0 | 不写入业务数据 | `EmployeeProfileConfirmed` |
| [员工资料更正申请](./employee-profile-correction-request.md) | 员工资料更正申请（EmployeeProfileCorrectionRequest） | 提交**更正申请**，不是直接改资料 | 3 | 申请（资料变更在后续环节） | `EmployeeProfileCorrectionRequested` |
| [企业能力工作台](./enterprise-capability-workbench.md) | 企业能力工作台（EnterpriseCapabilityWorkbench） | 协作条目：提交 / 重新提交 / 评论 | 7 | 协作条目 | `CollaborationItemSubmitted` / `Resubmitted` / `CommentAdded` |
| [费用报销](./expense-reimbursement.md) | 费用报销（ExpenseReimbursement） | 报销单，可附单据 | 3（+附件） | `PortalBiz_ExpenseReimbursements` + 待办 | `BusinessModuleCategory` |
| [请假申请](./leave-request.md) | 请假申请（LeaveRequest） | 请假单，可附证明材料 | 5（+附件） | `PortalBiz_LeaveRequests` + 待办 | `BusinessModuleCategory` |
| [模块探针](./module-probe.md) | 模块探针（ModuleProbe） | **只读验证样例**，不是业务办理入口 | 0 | **不写入任何数据** | **不写审计** |
| [我的待办](./my-work-items.md) | 我的待办（MyWorkItems） | 待办聚合入口，**不提供就地办理** | 0 | 不写入，只读聚合 | `WorkItemsViewed` |

**这批模块与经典模块最关键的一处差别**：它们的**标记层没有必填校验器**（`RequiredFieldValidator` 全为 0），必填由**提交时代码侧判定**——**页面不会标红，填错了是提交之后才被拒**。尤其是 `LeaveRequest` 的日期区间与 `ExpenseReimbursement` 的金额：标记层看起来"随便填"，服务端才会拦。

## 读之前要知道的三条横向事实

1. **审计面**：**经典内容模块与较新业务模块正好相反**——十一个经典内容模块里，**只有登录模块写运营审计**（`SignInFailed` / `LegacyCredentialUpgraded` / `CredentialCostUpgraded`），其余十个内容编辑路径**都不写**；而较新的业务模块（业务申请 / 员工资料确认 / 员工资料更正申请 / 企业能力工作台 / 我的待办）**都写运营审计**，事件分别见各页。**唯一一个既不写业务数据也不写审计的是模块探针**（只读验证样例，没有任何业务动作）。需要"谁改了什么"的可追溯记录时，经典内容模块只能到门户之外安排。
2. **校验面差异极大**：事件四项全必填、讨论零必填、联系人除姓名外**无任何格式校验**（邮箱与电话都是"填什么存什么"）。**不要因为某个模块不报错就以为填对了。**
3. **部署状态不同**：链接**仅测试页签**在用（正式页签暂未部署）；登录在开发库 **0 个实例**，因此**只有源码级说明、无运行期证据**；HTML 文本 12 个实例、XML / XSL 1 个实例。各页的部署读数是 **2026-10-21 的交付时快照**，新增或迁移页签后需重新核对。

## 两处"看起来像缺陷"但属既定行为

- **HTML 文本不做净化**：渲染时**不做 HTML 白名单净化**（沿用"入库前编码"的历史约定）。它**只能由受信任管理员维护**、**不得承载普通用户输入**；真正的防线是 `ContentRawHtmlEdit` 窄权限，而不是 `ValidateRequest="false"`（后者只是回发兼容）。
- **快速链接改一处、链接也变**：两者**共用同一份链接数据**与同一个编辑页（`EditLinks.aspx`）。这是设计如此，不是数据串了。

## 边界与不做

- 本目录是**使用说明**，不是源码文档：不改模块行为，也不替代[代码文档](../code-documentation.md)与[文档地图](../documentation-map.md)。
- 各页**不依赖运行期截图**：结论均回溯到源码标记与已核实的部署读数；需要截图证据的场景（如登录模块）已在对应页写明**再入条件**。
- 页面字段、必填与校验由门禁看守（`Test-PortalModuleDocPages.ps1`）：字段集须覆盖源码控件集、必填数量须与源码一致、八节须齐全、登记表中要求的**部署边界语句必须写明**。