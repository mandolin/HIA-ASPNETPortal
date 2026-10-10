# 用户指南

## 项目用途

HIA-ASPNETPortal 是一个 ASP.NET Web Forms 门户应用示例和改造项目，提供门户页、模块化内容、角色权限和后台管理能力。

## 本地运行概览

本地运行通常需要：

1. 准备 SQL Server 或 LocalDB。
2. 执行 `src/Setup/` 下的数据库脚本，至少包含基础脚本、P2/P3 增量脚本和 `Portal_UserCredentials.sql`。
3. 从 `src/Portal/Config/Templates/connectionStrings.config` 复制模板到外置配置目录，并配置 `Portal` 连接串。
4. 使用 Visual Studio 构建并运行 `src/master.sln`。
5. 访问启动后的站点。

默认外置配置目录为 `{当前进程用户目录}\Web\HIA-ASPNETPortal\{env}\`。本地开发通常使用 `dev` 环境，即 `...\HIA-ASPNETPortal\dev\connectionStrings.config`。

## 初始账号

历史说明中提到可使用 `admin/admin` 登录。该信息仅用于本地旧版本验证，任何共享、测试或生产环境都必须修改默认账号和密码策略。P5.2 起，旧账号首次成功登录会迁移到强哈希凭据；新建、注册和重置密码不会再写入旧 MD5 摘要。

## 常见模块

当前源码包含的门户模块包括公告、联系人、讨论、文档、事件、HTML、图片、链接、快速链接和 XML 模块。具体可见 `src/Portal/DesktopModules/`。

## 管理入口

后台管理相关页面位于 `src/Portal/Admin/`，包括用户、角色、模块定义、模块设置、站点设置和页面布局管理等。

## 业务模块：请假申请与费用报销

这两个模块（`HIA.LeaveRequest`、`HIA.ExpenseReimbursement`）是面向员工自助（ESS）的业务模块，由 `C-anp-P20` 交付。它们按模块包方式部署，**默认不随核心档位启用**。

### 启用前提

1. 执行业务表迁移脚本：`src/Setup/PortalBiz_LeaveRequests.sql` 与 `src/Setup/PortalBiz_ExpenseReimbursements.sql`。两个脚本可重复执行（建表前用 `OBJECT_ID` 守卫判断）。
2. 在模块包白名单中放行：`Portal.ModulePackages.Enabled` 追加 `HIA.LeaveRequest,HIA.ExpenseReimbursement`（`appSettings.json` 及环境覆盖文件）。该键与 Profile 无关，用于跨档位放行部署包。
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

### 常见问题

- **页面上看不到模块**：先确认包已加入 `Portal.ModulePackages.Enabled` 且已挂载模块实例；未启用或未挂载的模块会被跳过，并在诊断日志中记录原因。
- **始终显示「暂无…记录」且提交报错**：业务表未创建，请执行上文的迁移脚本；模块在表不可用时降级为空态而不是报错。
- **两个模块提交后只有表单没有待办**：待办需要指定审批人（用户或角色），两个模块固定指派给 `Admins` 角色；若该角色不存在则待办不会生成。

## 待补充

- 当前运行截图。
- 典型门户配置流程。
- 管理员常用操作说明。
- 生产部署注意事项。
- 请假与报销的审批流转界面（本期仅落库与生成待办，审批操作不在本模块内）。
