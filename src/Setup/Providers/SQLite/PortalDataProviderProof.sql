/*
<lang>
  <zh-CN>SQLite 数据提供器 proof fixture 脚本。本脚本只创建低敏 `PortalDataProviderProof` 证明表，用于验证 SQLite 提供器建表、唯一键和文本时间戳能力；它不属于 SQL Server 生产迁移链。</zh-CN>
  <en>SQLite data-provider proof fixture script. This script only creates the low-sensitivity `PortalDataProviderProof` proof table for validating SQLite provider table creation, unique keys, and text timestamp capability; it is not part of the SQL Server production migration chain.</en>
</lang>
*/

-- <lang>
--   <zh-CN>先删除旧 proof 表，保证每次 fixture 运行都从空表结构开始；该操作只应指向隔离 SQLite proof 数据库。</zh-CN>
--   <en>Drop the old proof table first so every fixture run starts from an empty table structure; this operation should target only an isolated SQLite proof database.</en>
-- </lang>
DROP TABLE IF EXISTS PortalDataProviderProof;

-- <lang>
--   <zh-CN>证明表覆盖自增主键、唯一业务键、UTC 文本时间戳和可选备注四类最小提供器能力。</zh-CN>
--   <en>The proof table covers four minimal provider capabilities: autoincrement primary key, unique business key, UTC text timestamp, and optional note.</en>
-- </lang>
CREATE TABLE PortalDataProviderProof
(
    ProofId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProofKey TEXT NOT NULL UNIQUE,
    RecordedUtc TEXT NOT NULL,
    Note TEXT NULL
);
