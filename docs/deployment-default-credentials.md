# 默认凭据与旧口令治理

本说明用于发布前确认默认账号、初始化脚本样例口令和旧 MD5 兼容路径的处理口径。它不记录任何真实密码，也不要求自动尝试默认密码登录。

## 当前边界

1. `src/Setup/Portal_LoadData.sql` 仍保留旧 Starter Kit 的本地样例 `admin` 用户和旧 `Portal_Users.Password` 摘要。
2. `src/Setup/Portal_UserCredentials.sql` 只创建强哈希凭据表和安全版本表，不写入任何新强哈希口令 seed。
3. P5.2 起，新建、注册和管理员重置密码写入 `Portal_UserCredentials`；旧 MD5 只允许作为历史账号首次登录兼容升级路径。
4. 生产、共享测试和客户演示环境不得沿用默认 admin 口令，也不得把共享开发密码写入仓库、WorkZone、日志或截图。

## 发布前要求

1. 执行 `dev/scripts/Test-PortalDefaultCredentialRisk.ps1`，记录输出摘要。
2. 如果使用 `Portal_LoadData.sql` 初始化数据库，必须在开放访问前完成默认 admin 的密码替换、账号禁用或显式删除。
3. 默认 admin 处理结果应写入发布记录；记录只写“已替换/已禁用/已删除/已迁移”，不得写密码明文。
4. 旧 `Portal_Users.Password` 仅作为兼容字段存在；新增或重置口令不得回写旧 MD5 摘要。
5. 后续首登强制改密、默认账号检测、历史口令和锁定策略进入独立安全设计，不在本说明中临时拼接。

## 推荐命令

```powershell
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoLogo -NoProfile -File dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Dev
```

测试或生产发布前可使用更严格口径并保留 JSON 证据：

```powershell
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoLogo -NoProfile -File dev/scripts/Test-PortalDefaultCredentialRisk.ps1 -Profile Prod -OutputJson temp/compliance/default-credential-risk.json
```
