# 转接文档（Handoff）—— 2026-09-26

> 供**新会话**恢复上下文并继续推进。恢复顺序：先读 `TASK_STATE.md`（主仓持久账本）→ 再读本文件 → `git status --short`（主仓）与 `git -C work-zone status --short`。

## 一、当前入口

**`W67` 前台「我的待办」聚合入口 + 操作审计（`C-anp-P12` 第 1 阶段，原则二 gate）**：`C-anp-P10`（W56–W60，`v0.2.0`）与 `C-anp-P11`（W61–W66，`v0.3.0`）已全部收口并发布；`W58`/`W60` 两项方案 B 前置已全部完成。**`C-anp-P12`（BasicBusiness 完善，`v0.4.0`）已固化**（D1–D4 裁定已确认），首个执行项 **`W67`（前台「我的待办」）已全部完成并收口**（P67.0–P67.6：数据层契约 + 前台模块 + 三语资源 + T3 可达性确证 + 审计与权限复核；构建 0 错 0 警、单测 `90/90`）。**`W68`（组织子树查询）已全部完成并收口**（P68.0–P68.4：纯函数 `PortalOrganizationTreeExpander` + 7 例单测；构建 0 错 0 警、单测 `97/97`）。**`W69`（资料更正闭环）已全部完成并收口**（P69.0–P69.5：回写策略 + 审核回写接线 + 自撤回 + 前台权限与审计；构建 0 错 0 警、单测 `103/103`）。**当前入口 `W70`（`EmployeeProfileConfirm` 前台权限 + 幂等 + 本地化，P2 首项）**。详见 `work-zone/dev/plans/W-anp-P67.md` 与 `work-zone/dev/plans/W-anp-P67.1-prototype-and-design.md`。

## 二、周期位置（重要：周期组有并行/插队）

| 周期组 | 状态 |
| --- | --- |
| `C-anp-P9`（W50–W55） | ✅ 已收口 |
| `C-anp-P10`（W56–W60） | ✅ **已收口**（2026-09-28）：`W56`✅ `W57`✅ `W58`✅ `W59`✅ `W60`✅；`v0.2.0` tag 已推送，一致性门禁 Pass；closeout 见 `work-zone/dev/plans/C-anp-P10-closeout.md`；L10 里程碑文档已建立 `work-zone/dev/milestones/M-ANP-RELEASE-READY-PORTAL.md`。 |
| `C-anp-P11`（W61–W66） | ✅ 已收口（`W61`–`W66` 全部完成；closeout 见 `work-zone/dev/plans/C-anp-P11-closeout.md`） |
| **`C-anp-P12`（W67–W75）** | **进行中**：已**固化**（D1–D4 已裁定，见 `work-zone/dev/plans/C-anp-P12.md`）；`W67` 处于 **`P67.1` 原型与设计稿 gate（待确认）**；后续 `W68` 目录子树 → `W69` 更正闭环 → P2/P3 → `W75` closeout（`v0.4.0`） |

## 三、本机 dev 环境（已跑通，可复现）

- **数据库**：LocalDB 实例 `MSSQLLocalDB`，库 `Portal`（39 张表，已初始化）。
- **外置连接串**：`C:\Users\admin\Web\HIA-ASPNETPortal\dev\connectionStrings.config`（已存在，指向上述库；**不入仓库**）。
- **启动站点**：`dev/scripts/Build-Solution.ps1 -Configuration Debug` 构建；IIS Express：`& "C:\Program Files\IIS Express\iisexpress.exe" /path:"<repo>\src\Portal" /port:8080 /clr:v4.0`，首页 `http://localhost:8080/DesktopDefault.aspx`。
- **登录**：`admin` / `admin`（库中 `admin` 密码哈希为 MD5("admin")；凭据表用 PBKDF2）。
- **英文切换**：设置 cookie `lang=en-US`（`Global.asax.cs` 的 `SetLanguage` 优先 cookie）。
- **截图链路**：playwright-core（经 **mise** node 20）+ 本机 Chrome（`channel:'chrome'`，headless）。
- **注意**：`C:\Program Files\IIS Express` 与 `d:\Program Files\Microsoft Visual Studio\18\Enterprise`（MSBuild/vstest）为实际路径。

## 四、已完成工作摘要

| 阶段 | 成果 |
| --- | --- |
| `W55` | C-anp-P9 closeout；L5 复核（2026-09-25）；战略备忘（未来范围/宣传/文档化） |
| `W56` | **版本号机制建立**：`docs/versioning.md`、`CHANGELOG.md`、`git tag v0.1.0`（已推送）、一致性门禁 `dev/scripts/Test-PortalVersionConsistency.ps1`；5 个程序集版本 → `0.1.0.0` |
| `W57` | Foundation 八条目**盘点 + 深度对标**（6 份 research 文档）；产出 `C-anp-P11` 蓝图 |
| `W61` | PBKDF2 下限 `210000 → 600000`（OWASP；实测 407ms < 1s）；`minimumPortalVersion` 语义定义；上传白名单移除 `zip` |
| `W62` | 审计补齐：门面 `Outcome` 参数化；**认证失败**入审计（`Signin.ascx.cs` 失败分支）；**授权失败**入审计（`PortalNavigationPolicy` 两个集中出口）；保留期策略 `docs/audit-retention-policy.md` |
| `W59` | 细节级对标规范 `docs/detail-level-benchmark-spec.md`（八维度 + 模板 + 检查表） |
| `W63`（P63.0–P63.3） | 数据范围契约：八维度对标（采纳 fail-closed、先应用层；不采纳立即 RLS/hierarchyid）+ `CanView` 落地（列表/详情共用、10 例单测）+ closeout（见 `W-anp-P63.3-closeout.md`） |
| `W64`（P64.0–P64.4） | 状态机显式化：显式迁移表单一事实源 + `IsLegalTransition` 门禁 + SQL 守卫由表生成 + 迁移必写事件 + 流转说明（`docs/collaboration-item-state-machine.md`）+ 6 例单测 |
| `W65`（P65.0–P65.3） | Watcher 语义修正：与"待办即通知"定位一致；确证无独立通知发送 |
| `W58`（P58.0–P58.3） | BasicBusiness 盘点对标（方案 B 前置调研）：七能力方向现状盘点 + 外部对标（转引本项目已核实的一手来源）+ `C-anp-P12` 候选清单定稿为 P1/P2/P3；**纯调研无代码改动** |
| `W60`（P60.0–P60.3） | `C-anp-P10` 收口 + **L10 里程碑文档建立**（`M-ANP-RELEASE-READY-PORTAL`，七条判据 R1–R7 与基线证据）+ **`v0.2.0` 版本推进**：CHANGELOG 条目、5 个程序集 `0.2.0.0`、`git tag v0.2.0`、一致性门禁 Pass；构建 0 错 0 警、单测 86/86 |

**版本**：当前 **`v0.3.0`**（tag 已推送，2026-09-28；CHANGELOG + 5 个程序集 `0.3.0.0` 联动，一致性门禁 Pass）。`v0.2.0`（`C-anp-P10`）与 `v0.3.0`（`C-anp-P11`）均已按推荐序列发布，无版本滞后。

## 五、下一步：`W67` 原型 gate → 实现 → `W68` → `W69`

**当前入口**：`W67` 前台"我的待办"聚合入口 + 操作审计，处于 **`P67.1` 原型与设计稿 gate（待用户确认）**。

**状态**

| 周期组 | 状态 |
| --- | --- |
| `C-anp-P10`（W56–W60） | ✅ 已收口；`v0.2.0` tag 已推送 |
| `C-anp-P11`（W61–W66） | ✅ 已收口；`v0.3.0` tag 已推送（当前版本锚点） |
| **`C-anp-P12`（W67–W75）** | **已固化**（D1–D4 已裁定）；`W67` 原型产出，**待确认后进实现** |

**D1–D4 裁定（用户 2026-09-28 确认）**：① `Business.Workflow` **合并到 `BusinessApplicationRequest`**（两键保留为历史兼容别名，不再使用）；② `EmployeeProfileConfirm`/`EmployeeProfileCorrectionRequest` **补 `capabilityId`** 并新增两条能力登记，`ModuleProbe` 归 Platform 不补；③ **先做前台"我的待办"**（`W67`），原型确认后方可实现；④ 更正回写**不引入二次审批**（单一审批 + 白名单 4 低敏字段 + 必写审计与事件）。

**`W67` 原型要点**：新增 DesktopModule `HIA.MyWorkItems`；仅本人未完成待办（fail-closed）；不可就地办理（跳转到业务对象）；10 项关键状态（含空态、读取失败、权限不足、禁用、超期）；a11y + IE9+ + 六套主题兼容；双语串清单；待裁定 T1–T5（含模块挂载 Tab、BusinessKind 前台可达性）。
**已识别风险**：既有 `GetBusinessUrl` 映射到**后台 Admin 页**，普通用户跳转可能 403 → 原型规定不可达则禁用链接 + 提示。

**`W58` 关键结论**：7 个 BasicBusiness 能力方向中仅 `Collaboration` 达"初步完善"，**单测覆盖 1/7**；P1＝目录组织子树查询（原 `W67` 的数据层前置）+ `WorkItems` 前台"我的待办"入口（GitLab 一手印证）+ `CorrectionRequest` 批准回写闭环；详见研究文档。

**`C-anp-P11` 已收口**（`W61`–`W66`）：closeout 与 `v0.3.0` 就绪证据见 `work-zone/dev/plans/C-anp-P11-closeout.md`；**版本推进已完成**（2026-09-28，按序列 `v0.2.0 → v0.3.0`：`CHANGELOG` + 程序集 `0.3.0.0` + `git tag v0.3.0` + 一致性门禁 Pass）。

## 六、剩余待办

**`C-anp-P11` 已收口**（`W61`–`W66`）：见 `C-anp-P11-closeout.md`，5 项历史待回看均已处置（Watcher 语义→W65；`minimumPortalVersion` 语义→W61/`docs/versioning.md` §八；PBKDF2→W61 600000；zip→W61 移除；认证/授权审计→W62 补齐）。

**方案 B 剩余**：~~`W58`~~ ✅ ~~`W60`~~ ✅ 全部完成（均在 `C-anp-P12` 之前）。**待执行**：`v0.3.0` 版本推进（发行动作）。

**`C-anp-P12` 候选蓝图**（已由 `W58` 对标定稿，待裁定）：**P1**＝`W-ED-1` 目录组织子树查询（原 `W67` 的数据层前置）+ `W-WI-1` `WorkItems` 前台"我的待办"入口与审计（原 `W69`）+ `W-CR-1` `CorrectionRequest` 批准回写闭环与自撤回与前台权限；**P2**＝`W-PC-1` `Confirm` 权限/幂等/本地化、`W-AR-1` `ApplicationRequest` 显式迁移表 + Resubmit + 单测、`W-DS-2` 数据范围沿用 `CanView`、`W-CR-2` 负责人角色键边界；**P3**＝`W-GV-1` `Business.Workflow` 死键归属裁定与孤儿模块补 `capabilityId`、`W-UI-1` UI 打磨（需先出原型）。见 `C-anp-P12-candidate-blueprint.md`。

**结转开放项**：① 组织子树展开；② 负责人角色键读取边界；③ 列表 SQL 放宽/"我可见"语义（需先出 UI 原型）；④ 真实通知呈现（依赖工作项投影 + UI）；以上均归入 `C-anp-P12` 候选包。

## 七、纪律与注意事项

1. **HIA ROP 双语注释**：新增/修改的函数与关键节点须带 `<lang><zh-CN>/<en>` 注释；XML 文档参数须完整（缺 `<param>` 会触发 CS1573，项目要求 0 警）。
2. **两套坐标系**：模块包 `HIA.<Name>` vs 能力词表 `<Domain>.<Capability>`（如 `BasicBusiness.Collaboration`）——不可混用。
3. **先对标后实现**（`docs/detail-level-benchmark-spec.md`）：一手优先、标注来源强度；无一手标 `待核实` 且不逐条断言；不适用入清单。
4. **构建/单测门禁**：`dev/scripts/Build-Solution.ps1` 须 0 错 0 警；`Portal.Tests` 单测通过（70+）。
5. **提交分区**：主仓＝源码/文档/账目；WorkZone（私有）＝计划/调研/证据。提交前 `git status` 确认无临时产物。
6. **敏感信息**：不提交连接串、凭据、Token；诊断/审计不得写口令/令牌/敏感 PII。
7. **不得用"改判据"代替"补证据"**：里程碑判据（尤其 L7/L8/L9）稳定，本机证据不替代真实环境证据。

## 八、关键文件指针

- 账本：`TASK_STATE.md`
- 版本/CHANGELOG：`docs/versioning.md`、`CHANGELOG.md`
- 审计保留期：`docs/audit-retention-policy.md`
- 对标规范：`docs/detail-level-benchmark-spec.md`
- 周期组：`work-zone/dev/plans/C-anp-P10.md`、`C-anp-P11.md`
- 阶段文档：`work-zone/dev/plans/W-anp-P61.md`、`W-anp-P62.md`、`W-anp-P63.md`、`W-anp-P63.3-closeout.md`、`W-anp-P64.md`、`W-anp-P65.md`、`W-anp-P66.md`、`W-anp-P58.md`、`W-anp-P59.md`、`W-anp-P60.md`
- 周期组：`work-zone/dev/plans/C-anp-P12.md`（已固化，D1–D4）、`C-anp-P10.md`、`C-anp-P11.md`
- 周期组收口：`work-zone/dev/plans/C-anp-P10-closeout.md`、`C-anp-P11-closeout.md`
- `C-anp-P12` 阶段：`work-zone/dev/plans/W-anp-P67.md`；原型与设计稿 `work-zone/dev/plans/W-anp-P67.1-prototype-and-design.md`（原则二 gate）
- 里程碑 L10：`work-zone/dev/milestones/M-ANP-RELEASE-READY-PORTAL.md`（索引见 `work-zone/dev/milestones/README.md`）
- BasicBusiness 能力对标（`W58`）：`work-zone/dev/research/basicbusiness-capability-reference-2026-09-28.md`
- `C-anp-P11` 收口：`work-zone/dev/plans/C-anp-P11-closeout.md`
- `C-anp-P12` 候选蓝图：`work-zone/dev/plans/C-anp-P12-candidate-blueprint.md`
- 调研（Foundation 八条目）：`work-zone/dev/research/foundation-*.md`
- 状态机文档：`docs/collaboration-item-state-machine.md`
- 索引：`work-zone/dev/plans/W-anp-INDEX.md`（最新条目 472）、`work-zone/dev/research/README.md`
