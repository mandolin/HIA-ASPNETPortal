# 转接文档（Handoff）—— 2026-09-26

> 供**新会话**恢复上下文并继续推进。恢复顺序：先读 `TASK_STATE.md`（主仓持久账本）→ 再读本文件 → `git status --short`（主仓）与 `git -C work-zone status --short`。

## 一、当前入口

**`W-anp-P63` 数据范围契约（`C-anp-P11` 第 3 阶段）**：`P63.0` 细节对标已完成；当前进入 **`P63.1` 契约收敛 → `P63.2` 落地协同事项**。

## 二、周期位置（重要：周期组有并行/插队）

| 周期组 | 状态 |
| --- | --- |
| `C-anp-P9`（W50–W55） | ✅ 已收口 |
| `C-anp-P10`（W56–W60） | ⚠ **未收口**：`W56`✅ `W57`✅ `W59`✅（按方案 B 插队先做）｜**`W58`（BasicBusiness 盘点对标）、`W60`（P10 closeout）待做**——按用户裁定的**方案 B**，二者排在 **`C-anp-P12` 之前**完成（W58 是 P12 的前置调研）。 |
| `C-anp-P11`（W61–W66） | 进行中：`W61`✅ `W62`✅ ｜ `W63` 进行中（P63.0 完成）｜ `W64` 状态机显式化、`W65` Watcher 语义、`W66` closeout 待做 |

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
| `W63 P63.0` | 数据范围八维度对标（采纳 fail-closed、先应用层；不采纳立即 RLS/hierarchyid） |

**版本**：当前 `v0.1.0`（tag 已推送）；`C-anp-P10` 产出 `v0.2.0`、`C-anp-P11` 产出 `v0.3.0`。

## 五、下一步：`P63.1` → `P63.2`（精确实施步骤）

**目标**：为协同事项（`BasicBusiness.Collaboration`）落地**数据范围**过滤：越权数据不可见（fail-closed）。

**关键文件**：`src/Portal.Components.Data1/CollaborationItemDb.cs`

| 位置 | 说明 |
| --- | --- |
| `GetRecentItemsForUser(int userId, int take)`（约 L312） | **用户视角列表**，已有 `userId` → 施加范围过滤的主入口 |
| `GetAdminItems(status, take)`（约 L335） | 管理员视角（保持既有权限约束，不扩权） |
| `QueryItems(whereClause, take, params)`（约 L992） | 内部查询辅助 |
| `CanParticipate(item, actor)`（约 L1132） | 既有**写**权限判定（并集扩展）→ **不改其语义**，只**追加**可见性层 |

**步骤**

1. 新增 `private bool CanView(CollaborationItemInfo item, CollaborationItemActorAuthorization actor)`：
   - 维度：① **归属**（`InitiatorUserId` / `OwnerUserId` == 当前用户）；② **参与人**（参与人集合含当前用户，`Collaborator`/`Watcher`）；③ **组织**（涉及员工的 `OrganizationUnitId` 落在当前用户可见组织范围内）。
   - **fail-closed**：`item` 或 `actor` 为空 / 信息不足 → 返回 `false`。
2. 在 `GetRecentItemsForUser` 的返回前，用 `CanView` 过滤结果（列表与详情共用同一判定；不引入"部分可见"UI 状态）。
3. 新增单测（`src/Portal.Tests/`）：范围外用户读取该事项 → **不可见/被拒**（必须有 fail-closed 证据）。
4. 构建 **0 错 0 警** + `Portal.Tests` 单测全绿（当前基线 **70** 个）。

**约束**：只落**应用层**，不引入 DB 层 RLS / 视图 / `SESSION_CONTEXT`；不改 `CanParticipate`。

## 六、剩余待办

**C-anp-P11**：`W63`（P63.1/P63.2 落地）→ `W64`（状态机显式化：合法迁移表 + 强制校验 + 迁移必写事件 + 流转说明）
→ `W65`（`Watcher` 语义落差：按 Q2 裁定**修正描述**为主，契约明确"站内通知由待办投影承担"）
→ `W66`（closeout + `C-anp-P12` 蓝图）
**方案 B 剩余**：`W58`（BasicBusiness 盘点对标）→ `W60`（P10 closeout），均在 `C-anp-P12` 之前。

**待回看项（5 项，P57.9/W66 决定处置）**
1. `Watcher` 语义—实现落差（已在 W65 处理）
2. `minimumPortalVersion:"1.0"` 与产品版本 `v0.1.0` 冲突（W61 已定义语义，待各 `module.json` 按约定收敛）
3. PBKDF2 迭代 210000 仅达 OWASP 35%（W61 已修复为 600000）
4. 上传白名单 `zip`（OWASP 不建议，W61 已移除）
5. 认证/授权审计缺失（W62 已补齐）

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
- 阶段文档：`work-zone/dev/plans/W-anp-P61.md`、`W-anp-P62.md`、`W-anp-P63.md`、`W-anp-P59.md`
- 调研（Foundation 八条目）：`work-zone/dev/research/foundation-*.md`
- 索引：`work-zone/dev/plans/W-anp-INDEX.md`（最新条目 466）、`work-zone/dev/research/README.md`
