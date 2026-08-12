/*
<lang>
  <zh-CN>旧 ASP.NET Portal Starter Kit 本地数据库授权脚本。脚本中的 Windows 账号是历史开发机示例值，只能作为受控本地安装样例；执行前必须替换为当前环境的最小权限账号。</zh-CN>
  <en>Legacy ASP.NET Portal Starter Kit local database permission script. The Windows account in this script is a historical development-machine sample value and is only a controlled local-install example; it must be replaced with the current environment's least-privilege account before execution.</en>
</lang>
*/

-- <lang>
--   <zh-CN>局部变量保存待授权 Windows 账号；当前字面量属于历史样例，不应被解释为真实生产账号。</zh-CN>
--   <en>The local variable stores the Windows account to grant; the current literal is a historical sample and must not be interpreted as a real production account.</en>
-- </lang>
DECLARE @username sysname
SELECT @username = 'CH01WW5042\ASPNET'

-- <lang>
--   <zh-CN>在 master 中注册服务器登录；该步骤会改变实例级安全状态，执行前必须人工确认目标实例。</zh-CN>
--   <en>Register the server login from master; this changes instance-level security state and requires human confirmation of the target instance before execution.</en>
-- </lang>
USE master
EXEC sp_grantlogin @username

-- <lang>
--   <zh-CN>在 Portal 数据库中授予访问并加入 db_owner；这是旧样例的宽权限设置，现代部署应由后续 hardening 收紧。</zh-CN>
--   <en>Grant database access in Portal and add db_owner membership; this is the legacy sample's broad permission setting and modern deployments should tighten it through later hardening.</en>
-- </lang>
USE [Portal]
EXEC sp_grantdbaccess @username
EXEC sp_addrolemember N'db_owner', @username
