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

## 待补充

- 当前运行截图。
- 典型门户配置流程。
- 管理员常用操作说明。
- 生产部署注意事项。
