-- ===============================================
-- CONFIG ASP.NET Portal STARTER KIT DATABASE
-- Grant permissions for Local DB Install
-- 
-- Version:	1.2 - 10/02 (mho)
--
-- ===============================================

DECLARE @username sysname
SELECT @username = 'CH01WW5042\ASPNET'

USE master
EXEC sp_grantlogin @username

USE [Portal]
EXEC sp_grantdbaccess @username
EXEC sp_addrolemember N'db_owner', @username
