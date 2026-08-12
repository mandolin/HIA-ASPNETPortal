/*
<lang>
  <zh-CN>旧 Portal 示例数据清理脚本。本脚本直接删除 `Portal` 数据库中的核心内容、用户、角色和用户角色记录，只适合受控开发/重置场景；生产环境或含真实用户数据的环境不得直接执行。</zh-CN>
  <en>Legacy Portal sample-data cleanup script. This script directly deletes core content, user, role, and user-role records from the `Portal` database and is suitable only for controlled development/reset scenarios; it must not be executed directly in production or in environments containing real user data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>显式切换到旧示例数据库，避免删除语句落到调用者当前数据库；执行前仍必须由人工确认连接目标。</zh-CN>
--   <en>Explicitly switch to the legacy sample database so delete statements do not land in the caller's current database; a human must still confirm the connection target before execution.</en>
-- </lang>
USE [Portal]

-- <lang>
--   <zh-CN>以下删除序列清空旧内容模块、账号和角色数据；它不是幂等数据迁移，而是破坏性重置入口。</zh-CN>
--   <en>The delete sequence below clears legacy content-module, account, and role data; it is not an idempotent data migration, but a destructive reset entry point.</en>
-- </lang>
DELETE Portal_Announcements
DELETE Portal_Contacts
DELETE Portal_Discussion
DELETE Portal_Documents
DELETE Portal_Events
DELETE Portal_HtmlText
DELETE Portal_Links
DELETE Portal_Users
DELETE Portal_Roles
DELETE Portal_UserRoles
