---
layout: default
lang: zh-CN
parent: 快速上手
permalink: /user-guide/
title: 用户指南
---
# 用户指南

> 产品定位、适用对象、能力地图与边界见 [product-overview.md]({{ site.baseurl }}/product-overview/)。本页聚焦**怎么跑起来、怎么用、出问题怎么办**。

## 项目用途

HIA-ASPNETPortal 是一个 ASP.NET Web Forms 门户应用示例和改造项目，提供门户页、模块化内容、角色权限和后台管理能力。

## 本地运行概览

本地运行通常需要：

1. 准备 SQL Server 或 LocalDB。
2. 执行 `src/Setup/` 下的数据库脚本，至少包含基础脚本、增量脚本和 `Portal_UserCredentials.sql`。
3. 从 `src/Portal/Config/Templates/connectionStrings.config` 复制模板到外置配置目录，并配置 `Portal` 连接串。
4. 使用 Visual Studio 构建并运行 `src/master.sln`。
5. 访问启动后的站点。

默认外置配置目录为 `{当前进程用户目录}\Web\HIA-ASPNETPortal\{env}\`。本地开发通常使用 `dev` 环境，即 `...\HIA-ASPNETPortal\dev\connectionStrings.config`。

## 初始账号

历史说明中提到可使用 `admin/admin` 登录。该信息仅用于本地旧版本验证，任何共享、测试或生产环境都必须修改默认账号和密码策略。旧账号首次成功登录会迁移到强哈希凭据；新建、注册和重置密码不会再写入旧 MD5 摘要。

## 常见模块

当前源码包含的门户模块包括公告、联系人、讨论、文档、事件、HTML、图片、链接、快速链接和 XML 模块。具体可见 `src/Portal/DesktopModules/`。

下面逐个说明**既有内容模块**的用途、显示内容、编辑字段与入口。字段一律写成「中文标签 + 源码控件 id」，校验取自 `Edit*.aspx` 源码的验证控件，部署位置取自开发库实测（2026-10-19）；所有已部署实例的编辑角色均为 `Admins`，即**编辑入口仅管理员可见**。（文档与源码的一致性由门禁 `Test-PortalContentModuleDocs.ps1` 机器核对：源码控件漏写、或必填项数量与源码不符即失败。）

### 公告（Announcements）

- 用途：发布带有效期的站内公告，过期后不再显示。
- 显示：标题 + 描述 + 过期时间；每条右侧有编辑入口（仅编辑权限可见）。
- 编辑页 `DesktopModules/EditAnnouncements.aspx`：
 - `TitleField` 标题（**必填**）
 - `MoreLinkField` 详情链接（可选）
 - `MobileMoreField` 移动端链接（可选）
 - `DescriptionField` 描述（**必填**）
 - `ExpireField` 过期日期（**必填**，另有日期格式比较校验 `VerifyExpireDate`）
- 新建入口：模块标题栏「Add New Announcement」。
- 当前部署：TabId=1「新闻与特写」、TabId=2「人事福利」、TabId=3「竞赛：TradeCraft / 监视 / 保护」。

### 联系人（Contacts）

- 用途：维护联系人名录（姓名、角色、邮箱、两个联系方式）。
- 显示：姓名 / 角色 / 邮箱（可点击发信）/ 联系方式；每条有编辑入口。
- 编辑页 `DesktopModules/EditContacts.aspx`：
 - `NameField` 姓名（**必填**）
 - `RoleField` 角色
 - `EmailField` 邮箱
 - `Contact1Field` 联系方式一
 - `Contact2Field` 联系方式二
- **注意：仅姓名必填**，角色 / 邮箱 / 联系方式均无必填与格式校验，可留空——录入时须自行核对。
- 新建入口：「Add New Contact」。当前部署：TabId=2「员工联系信息」。

### 讨论（Discussion）

- 用途：主题式讨论区，支持主题下的回复层级。
- 显示：主题列表（可展开 / 折叠子回复，展开状态按子回复数显示），每条显示作者与发布时间，标题链接到详情页（新窗口）。
- 详情页 `DesktopModules/DiscussDetails.aspx?ItemID=<编号>&mid=<模块号>`：显示标题 / 作者 / 日期 / 正文；提供回复表单（`TitleField` 最长 100 字符、`BodyField` 多行、`ReplyBtn` 提交），并可在上一条 / 下一条之间翻页。
- 新建入口：模块标题栏「Add New Thread」（新窗口打开详情页）。
- 当前部署：TabId=4「TradeCraft 技巧与装备」「实战手册」。

### 文档（Document）

- 用途：文档条目（上传文件或指向已有路径），供下载或新窗口查看。
- 显示：文档链接（新窗口打开）+ 编辑入口。
- 编辑页 `DesktopModules/EditDocs.aspx`：
 - `NameField` 文档名（**必填**）
 - `CategoryField` 分类
 - `PathField` 路径
 - `Upload` 是否上传（勾选）
 - `storeInDatabase` 是否存入数据库（勾选）
- 查看走 `DesktopModules/ViewDocument.aspx`。新建入口：「Add New Document」。
- 当前部署：TabId=2「新员工文档」、TabId=4「好书推荐」。

### 事件（Events）

- 用途：发布活动（含时间地点），过期后不再显示。
- 显示：标题 + 描述 + 时间地点。
- 编辑页 `DesktopModules/EditEvents.aspx`：
 - `TitleField` 标题（**必填**）
 - `DescriptionField` 描述（**必填**）
 - `WhereWhenField` 时间地点（**必填**）
 - `ExpireField` 过期日期（**必填**，另有日期格式比较校验 `VerifyExpireDate`）
- 新建入口：「Add New Event」。当前部署：TabId=1「近期活动」。

### 图片（ImageModule）

- 用途：在页签上展示单张图片。
- 编辑页 `DesktopModules/EditImage.aspx`：`Src` 图片路径、`Width` 宽、`Height` 高。**均无必填校验**。
- 新建 / 编辑入口：「Edit」。当前部署：TabId=3「夜视镜」。

### 链接（Links）

- 用途：维护链接列表（站点导航或资源入口）。
- 显示：链接标题（可点击跳转）+ 编辑入口。
- 编辑页 `DesktopModules/EditLinks.aspx`：
 - `TitleField` 标题（**必填**）
 - `UrlField` 地址（**必填**）
 - `MobileUrlField` 移动端地址（可选）
 - `DescriptionField` 描述（可选）
 - `ViewOrderField` 显示顺序（**必填**，另有数值比较校验 `VerifyViewOrder`）
- 新建入口：「Add Link」。当前部署：**TabId=1004 测试页「测试链接模块」**——即当前仅部署在测试页，正式页签暂未使用。

### HTML 文本模块（HtmlModule）

- **用途**：在页签上渲染一段由受信任管理员维护的**原始 HTML**（欢迎语、说明块等）。
- **显示**：`DesktopModules/HtmlModule.ascx`——按 `ModuleId` 读取已保存内容，`HtmlDecode` 后以 `LiteralControl` 原样输出；**记录缺失或内容为空时不输出任何内容**（普通页面不暴露配置状态）。
- **编辑页** `DesktopModules/EditHtml.aspx`（标记层 `ValidateRequest="false"`）：
 - `DesktopText` 桌面端原始 HTML（可选）
 - `MobileSummary` 移动端摘要（可选）
 - `MobileDetails` 移动端详情（可选）
- **必填项**：该编辑页**没有任何 `RequiredFieldValidator`**——三个字段均可留空保存。
- **存储**：`HtmlTexts` 按 `ModuleId` 唯一定位（`UpdateHtmlText`）：首次保存创建记录，之后只替换三个内容字段；入库前统一 HTML 编码、显示时解码（历史兼容契约）。
- **权限**：编辑需**同时**具备模块编辑权限与更窄的「原始 HTML」权限（`PortalPermissionKeys.ContentRawHtmlEdit`）；`Mid` 来自查询串或回发的未验证集合，必须为正整数，否则跳转编辑拒绝页。
- **审计**：本保存路径**不写运营审计**（同族旧内容编辑页亦无）。审计覆盖集中在业务申请与关键运营动作，见「审计与留存」。
- **安全边界（如实说明）**：这是"展示时 HTML 编码"规则的**明确例外**——渲染时**不做 HTML 白名单净化**，因此只能由受信任管理员维护，**不得用于承载普通用户输入**；`ValidateRequest="false"` 仅满足旧 Web Forms 回发兼容，**不构成安全边界**。
- **当前部署**（开发库实测）：**12 个实例**——首页 2、员工信息、产品信息、讨论区、关于门户 7，以及 `G1P2-Test-A` 测试页。

### 快速链接（QuickLinks）

- **用途**：以紧凑链接组呈现常用入口；与「链接（Links）」**共享同一份链接数据与同一个编辑页**。
- **显示**：`DesktopModules/QuickLinks.ascx`——每次请求重新绑定 `ILinksDb.GetLinks(ModuleId)`（关闭 ViewState）：
 - **编辑者**（属于模块 `AuthorizedEditRoles`）额外看到「新增链接」入口与逐条「编辑」入口（均指向 `EditLinks.aspx`）；
 - **浏览者**只能看到**通过导航策略**的链接——不合规的历史地址被抑制为**编码文本**（不再是链接），且不回显原始地址。
- **编辑页**：与「链接（Links）」**共用** `DesktopModules/EditLinks.aspx`：
 - `TitleField` 标题（**必填**）
 - `UrlField` 地址（**必填**）
 - `MobileUrlField` 移动端地址（可选）
 - `DescriptionField` 描述（可选）
 - `ViewOrderField` 显示顺序（**必填**，另有数值比较校验 `VerifyViewOrder`）
- **权限**：模块编辑权限（**不要求** HTML 模块那种「原始 HTML」窄权限）+ **链接归属校验**（不能凭 `ItemID` 改、删其它模块的链接）。
- **审计**：与同族旧内容编辑页一致，该保存 / 删除路径**不写运营审计**。
- **当前部署**（开发库实测）：**3 个实例**——首页「快速链接」、产品信息「值得关注的竞争者」、关于门户「快速链接」。

### XML / XSL 模块（XmlModule）

- **用途**：渲染**部署在站内**的 XML 数据，并可选用 XSL/T 转换后呈现（当前部署样例：首页「热门动态」）。
- **数据来源**：XML 与 XSL 都是**受信任部署目录内的文件**（样例 `~/Data/sales.xml` 与 `~/Data/sales.xsl`），路径作为模块设置 `xmlsrc` / `xslsrc` 存入 `PortalCfg_ModuleSettings`——**本模块没有自己的业务数据表**。
- **显示**：`DesktopModules/XmlModule.ascx`——两个路径各自经 `TryNormalizeTrustedDeploymentResourcePath` 与物理文件存在性校验后，才交给 `Xml` 控件（`DocumentSource` / `TransformSource`）：
 - 任一路径不合规或文件不存在 → 只显示**中性的编码提示**（`Legacy_XmlModule_MessageXmlUnavailable` / `Legacy_XmlModule_MessageXsltUnavailable`），**不回显原始路径、不输出物理路径或异常堆栈**；
 - 只配 XML、不配 XSL 也能正常显示（两者独立校验）。
- **编辑页** `DesktopModules/EditXml.aspx`：
 - `XmlDataSrc` XML 数据文件的应用内虚拟路径（可选，最长 250 字符）
 - `XslTransformSrc` XSL/T 转换文件的应用内虚拟路径（可选，最长 250 字符）
- **必填项**：该编辑页**没有任何 `RequiredFieldValidator`**——两个字段都可留空保存。
- **保存校验**：两个值都经同一路径策略规范化；**任一不合规即拒绝保存**，并提示 `EditXml_MessageInvalidResourcePath`（不含原始输入）。
- **权限**：正整数 `Mid` + `HasEditPermissions(moduleId)`（**不要求** HTML 模块那种 `ContentRawHtmlEdit` 窄权限）。
- **不做的事（如实说明）**：不提供上传、在线编辑、外部 URL 或任意物理路径能力；不自动加载外部 XML、远程样式表或脚本。
- **审计**：该保存路径**不写运营审计**。
- **当前部署**（开发库实测）：**1 个实例**——首页「热门动态」（`ModuleId=6`：`xmlsrc=~/data/sales.xml`、`xslsrc=~/data/sales.xsl`）。

### 登录（Signin）

- **用途**：在页签上提供登录表单（登录标识 + 口令 + 记住登录）。
- **显示**：`DesktopModules/Signin.ascx`——`EmailOrName`（登录标识）、`password`（口令）、`RememberCheckbox`（记住登录）、`SigninBtn`（登录）、`RegisterLink`（自主注册入口，指向 `~/Admin/Register.aspx`，**仅当 `PortalRegistrationOptions.AllowSelfRegistration` 显式开启时可见，默认关闭**）。
- **提交保护**：提交前用**一次性 RSA 公钥**把口令写入隐藏字段 `EncryptedPassword`（公钥由 `~/Security/LoginPasswordKey.ashx` 提供），服务端解密后只在**当前请求内**传递；是否强制加密提交由 `PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired` 决定，未要求时才接受明文回退。
- **校验与票据**：口令校验交给 `IUsersDb.SignIn`（强哈希 + 旧 MD5 兼容升级 + 多标识解析：登录名 / 旧名 / 在职员工号 / 邮箱）；成功 → `PortalAuthenticationService.SignIn` 签发**带安全版本**的票据（角色 Cookie 在认证请求阶段按需建立）；**失败提示是通用的**，不暴露用户是否存在、审核状态或摘要比较细节。
- **审计（与前面几个内容模块不同，如实说明）**：**登录失败会写运营审计**（`SignInFailed`，`Outcome=Failure`，只携带登录标识，**绝不写口令、盐或哈希材料**）；旧凭据升级、口令迭代次数升级也各写一条审计。
- **编辑页**：**无编辑页**——本模块没有 `EditSignin` 之类的设置页，登录行为由运行期设置与认证服务决定，**不在模块实例上配置**。
- **当前部署与证据边界（如实登记）**：开发库中 **0 个实例**（`PortalCfg_Modules` 无 `Signin.ascx` 记录），因此**本模块没有运行期证据**，以上均为**源码级说明**；再入条件：在目标环境挂载该模块实例后可采集运行期证据并补齐本节的实测读数。

### 未在本节逐项展开的模块

无——本节已覆盖 `user-guide` 登记的所有内容模块（含 `HTML` / `QuickLinks` / `XML` / `Signin`）。

## 管理入口

后台管理相关页面位于 `src/Portal/Admin/`，包括用户、角色、模块定义、模块设置、站点设置和页面布局管理等。

## 业务模块：请假申请与费用报销

这两个模块（`HIA.LeaveRequest`、`HIA.ExpenseReimbursement`）是面向员工自助（ESS）的业务模块。它们按模块包方式部署，**默认不随核心档位启用**。

### 启用前提

1. 执行业务表迁移脚本：`src/Setup/PortalBiz_LeaveRequests.sql` 与 `src/Setup/PortalBiz_ExpenseReimbursements.sql`。两个脚本可重复执行（建表前用 `OBJECT_ID` 守卫判断）。
2. 在模块包白名单中放行：`Portal.ModulePackages.Enabled` 追加 `HIA.LeaveRequest,HIA.ExpenseReimbursement`（`appSettings.json` 及环境覆盖文件）。该键与 Profile 无关，用于跨档位放行部署包。

 > **覆盖生效的前提**：`appSettings.json` 只有**部署到站点根目录**时才会覆盖 `Web.config` 的同名 `<appSettings>` 键——`AppSettingsLoader` 在文件不存在时静默跳过，此时运行期读到的仍是 `Web.config` 的基础值。开发机上改动不入库的 `appSettings.dev.json` 不会落地。完整机制见 [module-development-guide.md]({{ site.baseurl }}/module-development/)「新增模块的隐藏陷阱」第 4 条。
3. 由管理员在页签上挂载模块实例（模块定义在包启用后才会出现在可挂载列表）。

### 请假申请

- 表单字段：请假类型（年假 / 事假 / 病假 / 调休）、开始日期、结束日期、请假天数、请假附件、请假事由。
- 提交后状态固定为 `Pending`，并在「我的请假」列表显示本人最近 20 条申请。
- 提交同时生成一条待办，指派给审批角色（当前固定为 `Admins`），待办业务类型为 `LeaveRequest`。
- 校验：类型、起止日期、天数必填；天数必须为正数；结束日期不得早于开始日期（数据库层亦有 `CHECK` 约束）。

### 费用报销

- 表单字段：费用类别（差旅 / 餐饮 / 办公 / 其他）、报销金额、发票或附件、费用事由。
- 提交后状态固定为 `Pending`，并在「我的报销」列表显示本人最近 20 条报销单。
- 提交同时生成一条待办，业务类型为 `ExpenseReimbursement`，同样指派给 `Admins`。
- 校验：金额必填且必须为正数（数据库层有 `CHECK` 约束）。

### 审批流转边界（必读）

请假申请与费用报销两个模块**只负责提交**，**不包含任何审批动作**。这条边界由源码实测得出（2026-10-19），并由门禁 `Test-PortalApprovalBoundaryDocs.ps1` 机器看守——将来若真的加入审批动作而文档没有同步更新，门禁会失败。

- **本模块做了什么**（提交按钮的完整动作链）：字段校验 → 写入业务表（状态固定 `Pending`）→ 生成一条待办（指派给审批角色，当前固定为 `Admins`）→ 写一条操作审计。
- **本模块没做什么**：审批通过 / 驳回、状态流转（`Pending` 之后没有任何状态变化）、审批意见、撤回或修改已提交单据。源码中不存在任何审批动作方法或按钮。
- **「我的待办」模块同样不含审批**：它只对待办做**只读投影**（可按「全部 / 仅超期」筛选），既不处理业务、也不拥有流程语义；按设计，处理动作应跳转到关联业务对象——而当前版本没有提供该处理界面。
- **因此**：审批操作不在本模块内，需由外部流程或后续版本承载。在页签上找不到审批界面是**预期行为**，不是缺陷，也不要据此提 issue。
- 提交成功后用户看到的提示（“可在「我的待办」跟踪审批进度”）指的是**跟踪待办本身**，不代表本门户提供审批处理。

- **页面上看不到模块**：先确认包已加入 `Portal.ModulePackages.Enabled` 且已挂载模块实例；未启用或未挂载的模块会被跳过，并在诊断日志中记录原因。
- **始终显示「暂无…记录」且提交报错**：业务表未创建，请执行上文的迁移脚本；模块在表不可用时降级为空态而不是报错。
- **两个模块提交后只有表单没有待办**：待办需要指定审批人（用户或角色），两个模块固定指派给 `Admins` 角色；若该角色不存在则待办不会生成。

## 典型门户配置流程（how-to）

从零到"能看到一个带模块的门户页"的典型顺序：

1. **准备数据库**：执行 `src/Setup/` 下的脚本（基础脚本、增量脚本、`Portal_UserCredentials.sql`）。
2. **配置连接串**：从 `src/Portal/Config/Templates/connectionStrings.config` 复制模板到外置配置目录并填好 `Portal` 连接串（默认外置目录见上文「本地运行概览」）。连接串**不入库**。
3. **构建并启动**：Visual Studio 构建并运行 `src/master.sln`，或按 `dev-guide.md` 用命令行构建。
4. **登录**：用管理员账号登录（默认账号的安全约束见「初始账号」一节）。
5. **建页签（Tab）**：在 `src/Portal/Admin/` 的页面布局管理中新增页签，并设置该页签的**访问角色**。
6. **挂载模块**：在该页签上挂载模块实例。模块必须**先启用**才会出现在可挂载列表 —— 受信任模块包经 `Portal.ModulePackages.Enabled` 放行（详见下文「业务模块」节）。
7. **设主题**：在站点设置中选择皮肤。主题变量与回退约定见 `theme-semantic-variables.md`。

> **改了配置没生效？** 先看配置生效源：`Web.config` 的 `<appSettings>` 是基础值，只有**部署到站点根目录**的 `appSettings.json` 才会覆盖同名键；开发机上改动不入库的 `appSettings.dev.json` 不会落地。完整机制见 [module-development-guide.md]({{ site.baseurl }}/module-development/)「新增模块的隐藏陷阱」第 4 条。

## 管理员常用操作（how-to）

后台管理页面位于 `src/Portal/Admin/`，常用操作如下：

| 想做的事 | 入口 | 要点 |
| --- | --- | --- |
| 管理用户 | 用户管理 | 新建 / 停用账号；密码体系见下文 |
| 管理角色 | 角色管理 | 角色是授权的基本单位；`Admins` 为兼容管理员角色 |
| 管理模块定义 | 模块定义 | 模块**定义**与页面上的模块**实例**是两层，新增模块需先有定义才能挂载 |
| 模块设置 | 模块设置 | 针对某个已挂载实例的配置 |
| 站点设置 | 站点设置 | 含皮肤（主题）选择等站点级配置 |
| 页面布局 | 页面布局管理 | 页签的增删改与排序、页签访问角色 |

**密码相关**：旧账号首次成功登录会迁移到强哈希（PBKDF2）凭据；新建、注册和重置密码**不会再写入旧 MD5 摘要**。旧 MD5 摘要仅作存量兼容保留，处置策略见 `deployment-default-credentials.md`。

## 故障排除

先做三件通用的事：**看诊断日志**（模块被跳过、权限被拒都会记录原因）、**确认配置生效源**（见上）、**确认数据库脚本已执行**。

| 现象 | 可能原因 | 处理 |
| --- | --- | --- |
| 页面上看不到某个模块 | 模块包未加入 `Portal.ModulePackages.Enabled`，或未在页签上挂载实例 | 先放行包，再挂载实例；被跳过的模块会在诊断日志中记录原因 |
| 模块显示但没有样式 / 在深色皮肤下出现浅块 | 模块 CSS 绕过了 `--portal-*` 语义变量而写了裸色值 | 见 `theme-module-contract.md`；颜色一律经语义变量取用 |
| 始终显示「暂无…记录」且提交报错 | 业务表未创建 | 执行对应迁移脚本；模块在表不可用时降级为空态而非报错 |
| 提交成功但没有生成待办 | 待办需指定审批人（用户或角色）；两个业务模块固定指派给 `Admins` | 确认 `Admins` 角色存在 |
| 登录失败 / 提示凭据问题 | 默认账号策略、旧凭据迁移状态 | 见 `deployment-default-credentials.md` |
| 改了配置但运行期没变 | 改的是不生效的那份配置 | 见上文"配置生效源"提示 |
| 数据库连接失败 | 外置 `connectionStrings.config` 未配置或路径不对 | 确认外置配置目录与 `Portal` 连接串 |

## 尚未覆盖（据实登记）

以下**目前没有文档支持**，遇到时请查阅源码或提 issue，不要假定已有说明：

- ~~运行截图~~ —— **已交付**：建立**运行截图基线**（固定视口 1440×1000 的可复现采集脚本、统一命名规范、产物进证据包、清单入库），规范见 `documentation-artifacts-guide.md`「运行截图基线」节。
- 生产部署注意事项（部署相关请以 `deployment-checklist.md` 与 `operations-runbook.md` 为准；本页不重复给出部署步骤）。
- ~~请假与报销的**审批流转界面**~~ —— **已交付**：审批流转边界已在公开文档明确声明（见上文「审批流转边界（必读）」：本模块仅落库 + 生成待办，审批操作不在本模块内，且由门禁机器看守）。
- ~~各既有内容模块（公告、联系人、讨论、文档、事件、图片、链接等）的逐项操作说明~~ —— **已交付**：见上文「常见模块」各子节。
- ~~`HTML`、`快速链接（QuickLinks）`、`XML`、`登录（Signin）` 四个模块的逐项操作说明~~ —— **已交付**：见上文「常见模块」各子节（其中 `Signin` 在开发库中 **0 个实例**，仅有源码级说明、**无运行期证据**，再入条件见该节末）。
