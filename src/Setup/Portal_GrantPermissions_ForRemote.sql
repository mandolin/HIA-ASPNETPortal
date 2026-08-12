/*
<lang>
  <zh-CN>旧 ASP.NET Portal Starter Kit 远程数据库授权脚本。`PortalUser`/`PortalUser` 是历史示例登录与密码，不得作为真实环境凭据；生产或共享环境必须改用受控账号、强凭据和最小权限。</zh-CN>
  <en>Legacy ASP.NET Portal Starter Kit remote database permission script. `PortalUser`/`PortalUser` is a historical sample login and password and must not be used as real environment credentials; production or shared environments must use a governed account, strong credential, and least privilege.</en>
</lang>
*/

-- <lang>
--   <zh-CN>在 master 中检查登录是否存在；此脚本会修改服务器级安全状态，执行前必须人工确认实例和凭据策略。</zh-CN>
--   <en>Check whether the login exists from master; this script changes server-level security state and requires human confirmation of the instance and credential policy before execution.</en>
-- </lang>
USE master
IF NOT EXISTS (SELECT * FROM master.dbo.syslogins WHERE loginname = 'PortalUser')
BEGIN
    -- <lang>
    --   <zh-CN>默认数据库与语言变量只服务于当前登录创建批次，缺失或不可用时回退到 master/当前语言。</zh-CN>
    --   <en>The default database and language variables serve only the current login-creation batch and fall back to master/current language when missing or unavailable.</en>
    -- </lang>
    declare @logindb nvarchar(132), @loginlang nvarchar(132) select @logindb = N'master', @loginlang = N'us_english'
    if @logindb is null or not exists (select * from master.dbo.sysdatabases where name = @logindb)
        select @logindb = N'master'
    if @loginlang is null or (not exists (select * from master.dbo.syslanguages where name = @loginlang) and @loginlang <> N'us_english')
        select @loginlang = @@language
    -- <lang>
    --   <zh-CN>创建历史示例 SQL 登录；该语句保留原样用于兼容脚本证明，不代表批准的安全配置。</zh-CN>
    --   <en>Create the historical sample SQL login; the statement remains unchanged for compatibility proof and does not represent an approved security configuration.</en>
    -- </lang>
    exec sp_addlogin 'PortalUser', 'PortalUser', @logindb, @loginlang
END


-- <lang>
--   <zh-CN>在 Portal 数据库中授予示例登录访问并加入 db_owner；这是旧远程安装样例的宽权限边界。</zh-CN>
--   <en>Grant the sample login access in Portal and add db_owner membership; this is the broad-permission boundary of the legacy remote-install sample.</en>
-- </lang>
USE [Portal]
EXEC sp_grantdbaccess N'PortalUser'
EXEC sp_addrolemember N'db_owner', N'PortalUser'
