# 测试清单

## 构建检查

- [ ] 使用 Visual Studio 打开 `src/master.sln`。
- [ ] NuGet 包还原成功。
- [ ] `Debug|Any CPU` 构建成功。
- [ ] `Release|Any CPU` 构建成功。
- [ ] 构建日志中无新增错误。

## 数据库检查

- [ ] `Portal_CreateDB.sql` 可执行。
- [ ] `Portal_LoadConfig.sql` 可执行。
- [ ] `Portal_LoadData.sql` 可执行。
- [ ] 外置 `connectionStrings.config` 存在于 `{ExternalCfgPath}\{env}\`。
- [ ] `Portal` 连接串指向预期本地或测试数据库。
- [ ] 不使用生产数据库执行开发验证。

## 运行检查

- [ ] 站点可启动。
- [ ] 首页可访问。
- [ ] `dev/scripts/Test-PortalSmoke.ps1` 对已运行站点通过。
- [ ] 登录流程可用。
- [ ] 默认账号已仅限本地验证，非本地环境已修改密码。
- [ ] 管理入口可访问。

## 核心模块手工回归

- [ ] 公告模块列表和编辑流程。
- [ ] 联系人模块列表和编辑流程。
- [ ] 讨论模块列表、详情和编辑流程。
- [ ] 文档模块上传、查看和编辑流程。
- [ ] 事件模块列表和编辑流程。
- [ ] HTML、图片、链接、XML 模块展示流程。
- [ ] 用户、角色、权限配置流程。
- [ ] 页面布局和模块设置流程。

## 前端资源检查

- [ ] 确认是否需要运行 Gulp 或 Grunt。
- [ ] 如需构建前端资源，记录实际命令。
- [ ] CSS、JS、图片资源路径正确。
- [ ] 浏览器控制台无新增关键错误。

## 发布前检查

- [ ] 未提交真实连接字符串、密码、Token、证书。
- [ ] 未提交本地数据库文件、`node_modules/`、`bin/`、`obj/`。
- [ ] `Web.Debug.config`、`Web.Release.config`、`Web.Test.config` 的处理方式符合仓库规则。
- [ ] README、`docs/`、`dev/` 已按变更更新。
- [ ] 已记录验证结果和残留风险。
- [ ] 已按 `docs/deployment-checklist.md` 完成 SQL Server、IIS、外置配置和回滚检查。
