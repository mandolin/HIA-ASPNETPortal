# 更新日志

本项目变更记录。版本规则见 [版本策略](docs/versioning.md)。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [SemVer](https://semver.org/lang/zh-CN/)。

## [Unreleased]

**首页布局改为「上二下一」**：按产品草图把首页内容区从"三栏并排"改为"第一行账户登录 + 门户公告、第二行帮助与说明通栏"，并同步重拍文档站首页截图。

### 变更

- **首页布局（上二下一）**：原布局把三个窗格排成同一行（账户登录 260px｜门户公告 300px｜帮助与说明），公告与帮助两张卡内容都短，右侧长期留白、首页显得空。现改为第一行「账户登录（58%）+ 门户公告（40%）」并排、第二行「帮助与说明」通栏。改动落在 6 套主题 CSS（Enterprise / StateClassic / Oa 的 Light + Dark），实现口径 IE9 安全（`float` + `clear`，**不用 Grid / Flexbox** —— 主题包约束明令禁止），并且只影响首页：作用域限定 `body.portal-tab-1`（真实 TabId=1），其它页签通常只有一个模块，套用两列浮动会被挤到 40% 宽，故仍保持原三栏等高表格布局。
- **文档站首页截图同步重拍**：8 张实拍图（中文 / 英文各 4 张）在新布局下重拍，临时账号拍完即删（残留 0）。
- **第一行两卡等高**：按产品意见"增加登录区的高度达成一致、公告区适当拉宽并降低"，登录卡两个字段由并排改为**纵排**（卡片自然加高）、**登录按钮贴卡片底部**（表单呼吸区留在中部）、公告卡宽度 40% → **44%** 并收紧条目内距与行高。实测两卡高度**精确一致（354px = 354px）**；`min-height` 作用在内容盒，公告条数变化后需复核该值（已在主题 CSS 注释中写明）。
- **登录卡增加一行提示**：`忘记口令？请联系平台管理员重置。`（英文界面为对应英文文案）。该提示只陈述既有事实——帮助模块本就写着"忘记密码请联系平台管理员重置"——不含任何请求参数或动态内容；文案由新资源键 `Signin_forgotPasswordHint` 提供（中 / 英 / 中性三份），界面语言切换时随之切换。
- **登录卡底部归拢为一行**：「记住登录状态」靠左、登录按钮靠右，同一行底对齐；两行移出正常流后，字段与提示行留在上方，中部留白成为表单的呼吸区。纯 CSS 改动（6 套主题同步），两卡等高不受影响（实测仍为 354px = 354px）。
- **文档站首页截图改用视口截图**：侧栏是 `position: fixed` 定位，浏览器渲染**整页截图**时只按一屏绘制 fixed 元素 —— 图上会出现"侧栏在页面中部断掉"的假象（实站侧栏是通底的：`position: fixed; top: 0; bottom: 0`）。截图产线因此改为视口截图，首页图视口高 1200px。

### 修复

- **文本节点位置不能用资源表达式**：把 `<%$ Resources:lang, X %>` 直接写在标记文本里会抛 `HttpParseException`（页面整体 500 跳错误页）；资源表达式只允许出现在**服务器控件属性**中，故改由 `asp:Literal` 承载。
- **运行时编译的全局资源有影子拷贝缓存**：`App_GlobalResources/*.resx` 是 Content（运行时编译），其编译产物缓存在 ASP.NET 影子拷贝目录（本次为 10/07 的旧程序集）；只换 resx 而不清理影子拷贝，新键在运行期取不到（**静默失败**：界面空白、日志无痕）。本次回收应用后资源程序集重建（15:45:40，新哈希名），新键才生效。
- **学习轨补 3 条端到端任务路径 + 运营者手册，参考轨补模块运作总纲**：新增《业务申请：从提交到办结》《请假与报销：从填写到出现在待办里》《员工资料：确认与更正》《运营者手册：把门户用起来》《模块运作与二次开发》五篇（中英同构）。`p12-sample-business-flow.md` 保留为**实施与回归**视角并移入参考分区，避免与使用者路径复述。
- **更正**：门户名（侧栏品牌）此前被记为"运行时由后台「站点设置」可改" —— 实测**旧版站点设置控件没有任何宿主页**（`SiteSettings.ascx` / `Tabs.ascx` / `Roles.ascx` / `Users.ascx` / `ModuleDefs.ascx` 均无 `.aspx` 引用），这些后台入口**当前不可达**，门户名与页签排序只能由部署侧在数据层调整。运营者手册按实况写明，并登记为待办。
- **更正（续 2026-10-30）**：上条「无宿主页 → 后台入口不可达」的判定**前提有误**。这些控件虽无独立 `.aspx`，但作为模块实例挂载在 **TabId=6「后台管理」**页签下、由 `DesktopDefault.aspx` 动态加载，访问 `~/DesktopDefault.aspx?tabid=6` 即可打开；数据库已确认挂载（moduleId 28–32 对应这 5 个控件），且站点对该 URL 返回 200。故"门户名与页签排序只能部署侧改"应改为"经『后台管理』页签可达，但导航未暴露（Target 为 `.ascx` 被可见性判定排除），且已运行期验证（以管理员登录访问 tabid=6 渲染出五个旧版模块，站点设置含可编辑『站点标题』输入框）；后台导航本就有「后台管理」入口（`Tab.Admin`，指向 tabid=6）可进入该页签。运营者手册已据实改写，D22 备忘改判。
- **新登记 D23（2026-10-30）**：写审核侧文档时核实——**请假与报销没有后台审核入口**。后台待办页 `/Admin/WorkItems.aspx` 只读（不写入待办状态、不做转办/加签/会签/通知）；其业务入口 URL 映射白名单只认资料更正 / 业务申请 / 协同事项三种，请假（`LeaveRequest`）与报销（`ExpenseReimbursement`）落到 `/Admin/NotImplemented.aspx` 占位页，`src/Portal/Admin/` 下**没有** `LeaveRequests.aspx` / `ExpenseReimbursements.aspx`。这与业务申请（有 `/Admin/BusinessApplications.aspx`）、资料更正（有 `/Admin/EmployeeProfileCorrectionRequests.aspx`）不同。属**产品能力缺口**（非文档可闭合），需新增后台审核页并纳入导航登记。详见《请假与报销：审核侧现状》。

### 验证

- 实站计算样式实测：`.portal-dashboard-layout` = `display: block` / 960px；登录卡 `float: left` 557px（58%）；公告卡 `float: right` 384px（40%）；帮助卡 `clear: both` **960px 通栏**。
- L0 54 项全绿；站点发布 0 失败。

## [v2.1.1] - 2026-10-29
**品牌一致性与发布一致性收口**：把门户品牌统一为 **HIA Enterprise Portal**，并修掉两处"看起来做了、实际没生效"的机制问题。对应 `C-anp-P46`（W-anp-P259–W-anp-P263）。
### 变更
- **品牌名双源对齐**：侧栏品牌取库内 `PortalCfg_Globals.PortalName`（运行时由后台「站点设置」可改），`<title>` 取全局资源 `lang.Portal_Title`——两处此前不一致，会出现"侧栏新名、标签页旧名"。现统一为 `HIA Enterprise Portal` / `HIA 企业门户`，并同步建库种子（`Portal_LoadData.sql`），新装即得新品牌。
- **副标题改为定位语**：由与品牌重复的 `Enterprise Portal` 改为「统一身份 · 协同事项 · 运营审计」/「Unified Identity · Workflow · Audit」（取自项目自身定位，不与品牌重复）。
### 修复
- **全局资源替换不生效的部署机制事实**：`App_GlobalResources/*.resx` 在本工程是 **Content**（随站点发布、运行时编译），并非编译进 `Portal.dll`——只替换程序集不会更新页面文案。首轮部署因此未生效，现已连同资源文件一并部署并复核。
- **封版前检查 F7 改两模式判定**：备忘中的"阻塞封版"条目原判定在两种模式下都 FAIL，会让**每日 L0 长期红灯**（与"告警天天响等于没有告警"相悖）。现沿用 F1 已确立的"同一判据两模式判定"范式：**封版前模式判 FAIL**（拦住下次封版）、**日常自检判 PASS 但逐条点名待处理项**（不靠沉默掩盖）。
- **目标环境测试账号清理**（备忘 D19）：封版前按用户决定删除目标环境测试账号 `p27biztest`（UserId 1016，五张相关表共 5 行依赖按顺序清除，残留 0），D19 关闭。
### 验证
- 匿名首页 `<title>` 与侧栏品牌同源，旧名不再出现在渲染输出中。
- F7 双向实测：日常自检 PASS 且点名待处理项；封版前模式 FAIL 并指名条目。
- L0 54 项全绿；封版前检查 F1–F8 全量 PASS。
## [v2.1.0] - 2026-10-29
**门户信息架构与导航可用性收口**：把用户实测提出的 5 条槽点（不居中 / 菜单空白 / 首页单薄 / 缺 logo / 缺版权）背后的**结构性**问题一并收口。对应 `C-anp-P45`（W-anp-P253–W-anp-P258）、ADR 0026（内置内容能力包与导航可见性配置）、ADR 0027（匿名可见内容与公告游客可见属性）。
### 新增
- **导航可见性进入 L3 系统设置**：`Portal.Navigation.HideTabsWithoutRenderableModules` / `HiddenNavigationKeys` / `ExcludedNavigationKeyPrefixes` / `LogHiddenTabs` 四键落 `PortalCfg_SystemSettings`，由 `PortalNavigationSettings` 强类型读取（数据库覆盖 → appSettings → 代码默认；解析失败退回"不隐藏任何页签"的保守默认）。
- **仅对匿名隐藏**：`Portal.Navigation.AnonymousHiddenNavigationKeys` 让业务功能入口对登录用户可见、对匿名不可见，避免"匿名点进因未认证而无内容的页面"。
- **公告条目游客可见属性**：`Portal_Announcements.IsGuestVisible`（默认 0，存量升级后不自动公开）；匿名只读取标记为游客可见的条目，登录用户保持全量视图。
- **模块包契约门禁** `Test-PortalModulePackageContract.ps1`（M1–M5）+ 登记清单 `dev/module-package-registry.json`：`Enabled` 追加必须先登记；每个 `module.json` 的 `packageId` 必须被档位并集、`Enabled` 或显式"不挂载"清单覆盖——治住"模块在仓库里却永远不渲染、无人发现"的静默漏覆盖。
- **`HIA.Help` 静态帮助模块**：纯静态、不读库、不接受请求参数、**不开放后台编辑**（规避 ADR 0027 的 XSS 面），作为菜单项对匿名可见。
- **品牌标识**：`src/Portal/Images/portal-logo.png`（海洋之心主题，蓝白圆形，128×128 / 16.7KB）。
### 变更
- **`tabId` 权威化**：`PortalSettings` 只按 `tabId` 定位页签并由当前导航集合重算索引；隐藏页签不再让 `tabindex` 整体位移而使历史书签与截图清单失效（同时闭合备忘 D16 的异常）。
- **居中修正**：`.portal-content-stage` 补自动外边距（此前只有 `max-width` 而无自动外边距，宽屏下内容贴左、右侧大片空白）。
- **版权页脚**：新增 `.portal-footer`，文案「happy-dev 版权所有 2026-」与「© 2026- happy-dev. All rights reserved.」；页脚置于主壳层内，左对齐随各主题自身偏移，不硬编码列宽。
- **首页信息架构**：形成「账户登录 | 门户公告 | 帮助与说明」三栏；公告仅展示标记为游客可见的条目，演示型存量公告一律不展示。
- **移除公开导航死链**：横幅「门户文档」原指向 `/admin/NotImplemented.aspx`（未实现占位页），已移除。
### 修复
- **契约门禁读了不生效的配置源**：`Config/appSettings.json` 为嵌套结构 `{ appSettings: {...} }`，覆盖逻辑只遍历顶层导致一个 `Portal.*` 键都取不到、门禁一直读 `Web.config`（"写了等于没写"）；现显式下潜并在解析为空时报失败而非静默跳过。
- **无障碍门禁解析器截断标签**：标签扫描正则 `<img\b[^>]*>` 会被属性内 ASP.NET 内联表达式（`%>` 含 `>`）提前截断，使已写 `alt` 的 `img` 被误判缺 `alt`；现先归一化内联表达式再扫描，断言强度只增不减（注入真缺 `alt` 的 `img` 仍必须 Fail，已双向验证）。
- **Legacy CSS 兼容违规**：新增样式曾使用 `display:flex` 与 `var()` 兜底写法，违反 IE9/IE8 基础路径；改为块级堆叠 + `float` 两栏，颜色遵循 P18 双声明范式（显式 hex 在前、`var(--portal-*)` 在后），并同步进入全部 8 套主题。
### 验证
- 导航：匿名仅见首页与帮助；登录后见业务功能入口；直接访问被隐藏页签仍按 `AccessRoles` 判定。
- 登录链路：建测试用户走真实链路（一次性 RSA 公钥加密口令 → Signin 表单 → 取 `.ASPXAUTH` 票据），业务页对登录用户渲染真实内容。
- 门禁：L0 54 项全绿；契约门禁注入验证（清空"不挂载"清单必须干净失败并指名缺失包）；无障碍门禁注入验证（真缺 `alt` 必须 Fail）。
## [v2.0.0] - 2026-10-28
**生产就绪收口（`M-ANP-TRUSTED-PORTAL` / L7 达成）**：真实目标环境证据闭环后封版 `v2.0.0`。对应 `C-anp-P40`（目标环境部署与运行期基线）、`C-anp-P41`（运行期证据采集）、`C-anp-P44`（真实 IE9 复测）、`C-anp-P42`（生产就绪收口）。本版以环境证据闭环与发布收口为主，并修复目标环境暴露的 5 个真实缺陷。
### 新增
- **真实目标环境部署可信（T5）**：真实 IIS 站点（应用池 CLR v4.0 / Integrated）绑定 `http:8090` + `https:8443`（TLS 1.3）；`App_Data`、`App_Data\Logs`、`Uploads` 仅授予应用池 Modify；`machineKey` 在目标环境固定（只记指纹、不入库）；连接串经 `ExternalCfgPath` 指向仓库外目录。
- **Cookie 与 machineKey 目标环境实证（阻断项 2）**：真实登录 302 并签发 `.ASPXAUTH`，属性实测 `; path=/; secure; HttpOnly; SameSite=Lax`；ADR 0021 由「提议」转「已接受」。
- **SQL Server 版本矩阵（阻断项 3）**：目标实例只读实时版本核查，实测 SQL Server 2019（`15.0.4382.1` / 兼容级别 150）；静态预检确认脚本对 2016+ 兼容无 2017+ 专有语法。
- **企业安全扫描基线（阻断项 3）**：离线只读基线 Pass 15 / Warning 1 / 待目标环境 6 / Fail 0；6 项待目标环境项由已出证的部署与 Cookie 证据覆盖。
- **真实 IE9 复测（阻断项 1）**：Win7 + IE9 `9.0.8112.16421` 真机复测通过（登录取票、非管理员越权被拦截、登出后票据失效）。
- **业务签收（L8）**：出具 `business-signoff-record.md`，并记录三项如实限制。
### 变更
- **公开口径升级**：README 中英与运维手册中「不宣称生产级可信」改为「可宣称生产级可信（L7 达成）」。
### 修复
- **发布包缺 DLL 导致启动 500**：`Portal.csproj` 缺 `Portal.Components.Data` / `Data1` 项目引用，Unity 按名解析失败。
- **EF6 无法反射发现 SqlClient 提供程序**：`EntityFramework.SqlServer.dll` 不被 IL 引用而被 MSBuild 剪枝，改由部署入口工程显式引用。
- **证据包无条件追加 `-BaseUrl`**：不接受该参数的生成器直接失败、两项被误记 Failed；改为按参数集探测。
- **就绪包默认 `MatrixPath` 漏 `works/`**：输入解析失败，且合规基线只读源码开发基线；补默认路径并新增 `-WebConfigPath` 转发。
- **台账期望产物无人产出却记已出证**：新增真实生成器与期望产物落地校验，消除假阳性。
### 验证
- 环境证据清单十项齐备：`Test-PortalTargetEnvironmentEvidenceMap.ps1` 判据覆盖 7/7；`Test-PortalL7CriteriaRegistry.ps1` 判据表自洽。
- 账本一致性 `Test-PortalPlanLedgerConsistency.ps1` 三处一致。
## [v1.4.0] - 2026-10-23

**发布与门禁自动化**：质量门禁从"只在本地被人跑"变成 **CI 真在跑、且红线由结构保证**；发布流程从"版本锚点分散在多个文件里人工逐处改、一致性门禁只查三类、封版前检查靠人记得、Release 说明手写"变成**半自动 + 机器可判**。对应 `C-anp-P38`（门禁接入 CI）与 `C-anp-P39`（发布流程半自动化），产品行为改动为零。
### 新增
- **L1 门禁进 CI、红即阻断发布**：`publish-site.yml` 的 `gates` 作业跑 L1（构建 + 单元测试 + 业务模块冒烟），`publish` 作业 `needs: gates`——门禁红则发布根本不开始，"发布前必须门禁绿"由**结构**保证，而不是靠人记得先跑一遍。
- **L0 每日报告工作流**（`gate-report.yml`）：每日定时跑 L0 全量，失败不阻断但作业摘要标红 + 附证据；区分真失败（exit 1 = 红）与环境性前置缺失（exit 2 = 不阻断），不让"CI 取不到某数据源"误报红。
- **版本锚点同步半自动脚本**（`Sync-PortalVersionAnchors.ps1`）：从单一来源（版本号 + 日期）一次性写全部 8 类锚点（CHANGELOG / 5 个 `AssemblyInfo.cs` / README 中英 / `versioning.md` 第六节 / 首页中英统计），幂等、支持 `-WhatIf` 预览；只改当前版本锚点、不动历史版本提及。
- **封版前检查门禁接入发布流程**（`release-gate.yml`）：手动触发，跑 `Test-PortalReleaseReadiness.ps1` 全量 F1–F8，作为人工封版前的必跑检查点；独立于每 push 的发布流程（避免封版间期的正常状态误阻日常发布）。
- **Release 说明自动生成**（`New-PortalReleaseNotes.ps1`）：从 CHANGELOG 指定版本段**逐字**生成 GitHub Release 说明，无编造、无遗漏；与文件系统发布包 manifest 脚本职责区分。
- **并发两层互斥**：CI `concurrency` 组（同组排队、绝不取消）+ 门禁套件命名互斥体（崩溃由 OS 自动释放、无残留锁），避免并发运行互相覆盖构建产物。
### 变更
- **版本一致性门禁扩面**（`Test-PortalVersionConsistency.ps1`）：从"CHANGELOG / tag / 程序集"三查扩到 **8 类锚点全查**——新增首页版本统计（`site/index.md` 与 `site/en/index.md`）检查，补齐 v1.2.0 漏改暴露的最后一类盲区；任一锚点漂移即 fail，不再依赖别的检查恰好兜底。
- **`Build-Solution.ps1` 补 NuGet 还原**：`src/packages/` 被 gitignore、零文件入库，此前干净检出上不还原会导致构建失败。
### 修复
- **CI 首跑暴露的真缺陷**（"本地绿、CI 红"）：`Build-Solution.ps1` 不还原 `packages/`，本地因存在未跟踪包假绿、CI 干净检出必红（`MSB3245` + 一连串 `CS0234`）；已补 NuGet 还原 + `gates` 作业加 Ensure NuGet CLI 步骤。
- **`publish-site.yml` 的 `paths` 过滤器**：原逐个列举脚本、漏列 `Build-Solution.ps1`（改门禁自身脚本却不触发门禁的典型失效）；改为整个 `dev/scripts/**` 计入触发条件。
### 验证
- 门禁套件 L0 53 项（失败 0、跳过 1 为环境性）、L1 9/9；账本一致性 PASS；编号门禁 FailedChecks 0。
- **模拟封版演练**：临时副本跑 `Sync` → 封版后一致性**零漂移 PASS** → Release 说明与 CHANGELOG **逐字节一致**。
- **注入验证 3 项**：故意把程序集 / 首页版本统计 / README 当前版本改旧，一致性门禁均 fail 抓回，还原后零漂移恢复。
## [v1.3.0] - 2026-10-23

**文档体系完整化**：代码文档从"只有 JSDoc 半边、说明页写着待渲染组件就位"变成**双半边上架且被门禁看守**；模块使用说明从 11 个扩到 **19 个**（含此前既无页面也未登记的 8 个较新模块）。与 `C-anp-P36`、`C-anp-P37` 两个周期对应，产品行为改动为零。

### 新增

- **代码文档双半边上架**：`/api/jsdoc/`（JSDoc）与 `/api/dotnetdoc/`（4 个 API 工程 + 标记层注释）作为官网静态页发布，共 12 个语言组合 / 51 文件 / 约 16.8 MB；每个半边按**其 HIA 工件自己声明的语言**渲染——`surface` 与 `project` 两个发现型半边只声明英文，**如实只出英文，不为凑双语伪造中文入口**。
- **语言专属入口页**：DotNetDoc 顶层入口从单一双语页改为三个（双语默认 / 中文 / 英文）。侧边导航外链按语言过滤（2026-10-22 修的机制），中文导航落中文页、英文导航落英文页——只用一个双语页会让两种语言落在同一页上，语言过滤等于白做。
- **19 个模块的逐项详细使用说明**：11 个经典模块 + 8 个较新模块各一页，每页八节（用途与前置 / 操作步骤 / 字段表 / 必填与校验 / 存储与审计 / 权限 / 常见问题 / 边界与不做），中英双语；字段、必填与长度上限**机械取自源码**，不凭记忆写。
- **模块文档门禁**（`Test-PortalModuleDocPages.ps1`）：字段集 ⊇ 源码控件集（词边界匹配）、必填数与源码 `RequiredFieldValidator` 数一致、八节齐全且**须为二级标题**、H1 唯一且指明本模块、部署边界语句必须写明；登记表带 default-deny 自检（无待交付时模式必须为 hard）。
- **代码文档门禁扩展**（`Test-PortalCodeDocs.ps1`）：源指纹（覆盖配置及其声明的全部输入）、产物形状**递归扫所有文件**（内部编号 / 本地绝对路径）、语言声明与产物目录**完全一致**（少一种 = 漏渲染，多一种 = 伪造双语）、产物非空壳、快照与清单**正反两向**互洽（防残留目录变 404 幽灵）。

### 变更

- **模块文档门禁 M3 收紧**：原判据只查"节名这个词是否出现在页面任何位置"，实测把八个词散在正文里也能通过；改为必须是 `## <节名>` 二级标题。收紧前逐页核对 19 个页面，0 个不合规。
- **英文审校**：补 6 节英文缺失内容（`audit-retention-policy` 3.1 已登记的例外、`documentation-artifacts-guide` 运行截图基线、`documentation-map` 4. 开发者与贡献者、`operations-runbook` L7 目标环境证据、`site-visual-baseline` 9. `<html lang>` 覆盖、`third-party-dependencies` 机器可读基线）；修 1 处英文断链引用（正文指引的标题在英文里不存在）；9 个模块页的中文部署页签名**保留原文**（真实部署值，读者要靠它定位页签）另加英文注解。
- **中文侧结构修复**：`user-guide.md` 的 FAQ 条目原无自己的标题、混在"审批流转边界"节内，补 `### 常见问题（FAQ）`，中英同时归位。
- **已上架页面 42 → 50**；`docs/README.md` 与模块索引页的模块计数改为实测值。

### 修复

- **一处"门禁全绿但覆盖不全"**：`src/Portal/DesktopModules/` 下 8 个较新模块既无文档页也未登记进模块登记表——**正因为未登记，M1 对它们不适用，L0 全绿不代表覆盖完整，而是门禁压根不知道它们存在**。已建页（中英）并登记，模块文档门禁从 11 个扩到 19 个。
- **一处内部编号外泄**：首版 DotNetDoc 发布后扫描发现 6 个页面含内部编号，追源到 C# XML 注释与 `Default.master` 的"延期项登记"注释；**修源头而非发布端过滤**（过滤会让发布产物与源不可复现），另修 JSDoc manifest 的 `publishedBy`。复扫 51 文件零违规。

### 验证

- 门禁套件 53 项全绿（L0 / L1，跳过 0）；模块文档门禁 PASS；账本一致性 PASS；编号门禁 FailedChecks 0。
- 模块文档门禁注入 **7/7**（删页 / 改名节标题 / 删控件 id / 加必填标记 / 删 H1 / 加第二个 H1 / 删边界定性，每项字节级还原）。
- 代码文档门禁注入 **9/9**（含子目录注入内部编号、伪造中文目录、幽灵半边、清空产物）。
- 52 个英译本与中文源标题对齐：**7 处不一致 → 0**。

## [v1.2.0] - 2026-10-22

### 新增

- **发布后线上复核**：发布完成后逐页核对页面可达性、标题与本地构建产物一致、导航可见、页面内不含内部编号；带传播等待窗口，并区分「页面真的不在」（缺陷）与「网络抖动导致无法确认」（未确认）两种状态。
- **构建戳与精确传播判据**：构建把源文档指纹写入每个页面，发布后复核据此判断「线上是否已是本次构建的产物」，替代此前「页面能打开就算传播完成」的粗判据。
- **手写页面新鲜度核对**：首页、营销页与八个分区页现在与导航数据逐项核对（分区标题、顺序、页内标题、双语成对、链接命名空间）。
- **构建预演开关**：`-DryRun` 只报告将生成什么、不写任何文件；`-OutputRoot` 可把产物写到指定目录，便于在不改动工作区的前提下比较「现在应该生成什么」。
- **本地注入轨工具**：一键在本地复现「模拟发布页」上的各类缺陷，验证复核门禁确实能发现它们，无需访问线上。
- **封版前检查门禁**：把版本锚点一致、门禁套件新鲜运行且无失败、镜像新鲜度、线上复核结论、备忘无阻塞项等八项做成可复跑判据。

### 变更

- **判据消息带稳定编号**（`C1-HTTP` / `C1-TRANSPORT` / `C2-TITLE` / `C3-NAV` / `C4-INTERNAL-ID`）：外部断言匹配编号而不是中文措辞，改一句消息措辞不再让验证脚本静默失效。
- **构建戳按归一化后的源内容计算**：与行尾表示无关，因此本地与 CI 检出一致。
- **公开文档**：补齐文档子站构建说明（构建脚本与两个只读开关）；部署读数与代码文档产物体积注明日期或实测来源，便于判断新旧。

### 验证

- 门禁套件 53 项全绿（L0 / L1）；线上复核 84 页通过；本地注入轨 9/9；判据解耦验收 4/4；封版前检查 F1–F8 注入证明 13/13。
## [v1.1.0] - 2026-10-17

**官网与文档子站整体改版**：主站从文档索引页升级为产品官网（landing），文档子站重写为 docs.orchardcore.net（Material for MkDocs）风格，并完成公开内容的去内部化。按 `docs/versioning.md` 第二节以 MINOR 递增（完成一个 Cycle 的主题目标）。

### 新增

- **产品官网 landing**：hero（真实命令块 + 双 CTA）、统计行、能力/功能卡片栅格、CTA 带、入手方式、FAQ 折叠、多列页脚；对标基准为 orchardcore.net 主站（结构照搬、配色保持 HIA 令牌）。
- **营销三页**：`/overview/`（产品总览）、`/features/`（功能）、`/compare/`（选型对比），中英双语；文档页「产品概览与定位」让路改名 `/product-overview/`。
- **全站中英语言切换**：每页头部可切换中英文（两棵页面树一一对应，由手写页成对门禁保证）。

### 变更

- **文档子站**照 docs.orchardcore.net 风格重写：深色顶栏（品牌 + 搜索框 + 仓库链接 + 语言切换）、左侧导航树、右栏 sticky「本页目录」（按页面 h2 自动生成）、上一篇/下一篇分页、深色代码块、admonition 式提示框、深色页脚条。
- **几何对齐**：顶栏与三栏锁进同一居中容器，宽度复刻对标站点的根字号阶梯（1220 / 1342 / 1464px 三档），正文内边距逐值一致；两端头部 fixed 并与容器居中对齐，锚点跳转补 `scroll-margin-top`。
- **公开内容去内部化**：移除内部协作与阶段编号表述（HIA 外围协作、Documentation-Sys 通知机制、Pxx 里程碑号等）；纯内部治理文档（文档工程总账、人工走查清单）不再随公开文档发布，公开文档统一为读者视角；溯源表述移至产品概览的溯源小注。

### 验证

- 站点门禁：82 个检查页面站内链接无坏链，手写页中英成对，生成一致性 Pass；产物扫描无内部编号痕迹。
- 浏览器实测（本地 serve 与线上，1264/1920 视口）：头部 fixed、容器对称居中、三栏宽度与对标站点逐值一致。

## [v1.0.0] - 2026-10-09

**首发发行版**（`C-anp-P17`）：达成 `M-ANP-RELEASE-READY-PORTAL`（L10）七条判据，按 `docs/versioning.md` 第四节切 `v1.0.0`。本版本不对功能做新增，只做判据复核、版本锚点与对外口径（原则 4：文档化贯穿开发周期）。

### 已达成（判据复核）

- **R1 能力覆盖** ✅：Foundation + BasicBusiness 登记能力均达"初步完善"。
- **R2 对标深度** ✅：逐条目对标覆盖率 **19/19 = 100%**、八维度 **8/8**、来源强度标注率 **82/82 = 100%（全部一手）**，`Test-PortalBenchmarkChecklist` **Pass（阻断级违规 0）**。
- **R3 质量门禁** ✅：统一编排 **44 门禁条目**，L0 **23/23**、L1 **9/9**、**L2 15/15 全绿**；单测 **123/123**；构建 **0 错 0 警**。
- **R4 文档就绪** ✅：README（7 步快速上手）、模块开发指南（10.9 KB）、架构说明、公开文档索引全覆盖。
- **R5 开源就绪度** ✅：LICENSE（MIT）、CONTRIBUTING、`.github/` 模板、通用密钥泄露扫描门禁。
- **R6 版本锚点** ✅：本版本切 `v1.0.0` 标签，CHANGELOG / 标签 / 5 个 `AssemblyInfo` 三锚点一致（一致性门禁守护）。
- **R7 口径约束** ✅：`docs/versioning.md` 第六节约束已确立。

### 已修复（门禁可信度）

- **ModuleRuntimeGate 对 ModuleProbe 的误判**（`a869907`）：`Set-Profile` 曾把 `appSettings.dev.json` 整体覆盖、抹掉 `Portal.ModulePackages.Enabled=HIA.ModuleProbe`，导致 business 组切档后 ModuleProbe 包被禁用、模块不渲染、门禁误报 `module=false`（L2 14/15 的元凶，非模块缺陷）。改为读-合并既有键、business 组显式启用该包，并新增切档后实例预热。L2 恢复 **15/15**。
- **语义标记门禁默认档弱化**（`ad2b10c`）：`Test-PortalSemanticMarkupEvidence` 默认 `baseline` 档只断言"模块已渲染"，语义断言仅在 `after` 档生效；编排新增 `Env` 清单声明以 `after` 档运行该门禁（注入法双向证明：缺 `scope` 的 `<th>` 在 `baseline` 抓不到、`after` 抓得到）。
- **IeModeReadiness 假绿**（`03f604d`）：新增 `[GATE-NOTE]` 注释通道，使 `exit 0` 但内部就绪指标全 False 的结论在编排汇总行显式可见（仍不计入 L10 硬性判定）。

### 验证

- 门禁套件 **47 个全绿**（L0 23 / L1 9 / L2 15），失败 0、跳过 0。
- 版本一致性门禁 Pass（CHANGELOG 最新 `v1.0.0` == 标签 `v1.0.0` == 全部 `AssemblyVersion` / `AssemblyFileVersion` / `AssemblyInformationalVersion` = `1.0.0.0`）。

### 对外口径（R7 / D2）

- 口径限于"**可用 / 参考 / 研究基线**"。**不得宣称生产级 / 企业级可信**：`M-ANP-TRUSTED-PORTAL`（L7，真实环境证据下的生产就绪）仍为独立里程碑，未达成前禁用该表述。
- 本版本是四大一级工程原则（设计基线优先 / 调研先行 / 测试与复核为验收标准 / 文档化贯穿开发周期）的**对外生效节点**；closeout 逐条声明四项原则遵守情况。

> **发布即承诺**：首发口径必须明确标注 L7 未达成，避免使用者误判适用范围。

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
