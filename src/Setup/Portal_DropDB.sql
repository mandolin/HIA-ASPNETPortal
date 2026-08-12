/*
<lang>
  <zh-CN>旧 ASP.NET Portal Starter Kit 数据库删除脚本。它会终止连接到 `Portal` 数据库的会话并删除整个数据库，只能在受控本地/测试重建流程中由人工确认后执行。</zh-CN>
  <en>Legacy ASP.NET Portal Starter Kit database drop script. It terminates sessions connected to the `Portal` database and drops the entire database, and may only be executed after human confirmation in controlled local/test rebuild workflows.</en>
</lang>
*/

-- <lang>
--   <zh-CN>在 master 中检查并删除目标数据库，避免在待删除数据库上下文内执行连接清理和 DROP。</zh-CN>
--   <en>Check and drop the target database from master so connection cleanup and DROP do not execute inside the database being removed.</en>
-- </lang>
USE [master]

-- <lang>
--   <zh-CN>存在性保护让脚本在数据库已不存在时安全退出，但数据库存在时仍是破坏性操作。</zh-CN>
--   <en>The existence guard lets the script exit safely when the database is already absent, but it remains destructive when the database exists.</en>
-- </lang>
IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'Portal')
BEGIN
	-- <lang>
	--   <zh-CN>游标状态保存待终止的 SQL Server 会话标识和动态 KILL 语句；作用域仅限本删除批次。</zh-CN>
	--   <en>The cursor state stores the SQL Server session id to terminate and the dynamic KILL statement; its scope is limited to this drop batch.</en>
	-- </lang>
	DECLARE @spid smallint
	DECLARE @sql varchar(4000)

	-- <lang>
	--   <zh-CN>游标枚举当前连接到 `Portal` 数据库的会话，为后续 DROP 清除数据库占用。</zh-CN>
	--   <en>The cursor enumerates sessions currently connected to the `Portal` database so the later DROP can release database usage.</en>
	-- </lang>
	DECLARE crsr CURSOR FAST_FORWARD FOR
		SELECT spid FROM sysprocesses p INNER JOIN sysdatabases d ON d.[name] = 'Portal' AND p.dbid = d.dbid

	OPEN crsr
	FETCH NEXT FROM crsr INTO @spid

	-- <lang>
	--   <zh-CN>逐个 KILL 目标数据库连接；这是有意的破坏性中断，不能用于共享或生产数据库。</zh-CN>
	--   <en>Terminate target-database connections one by one; this is an intentional destructive interruption and must not be used against shared or production databases.</en>
	-- </lang>
	WHILE @@FETCH_STATUS != -1
	BEGIN
		SET @sql = 'KILL ' + CAST(@spid AS varchar)
		EXEC(@sql) 
		FETCH NEXT FROM crsr INTO @spid
	END

	CLOSE crsr
	DEALLOCATE crsr

	-- <lang>
	--   <zh-CN>连接清理完成后删除整个 Portal 数据库；脚本不做备份、不导出数据，也不提供恢复路径。</zh-CN>
	--   <en>After connection cleanup, drop the entire Portal database; the script performs no backup, export, or recovery path.</en>
	-- </lang>
	DROP DATABASE [Portal]
END
GO
