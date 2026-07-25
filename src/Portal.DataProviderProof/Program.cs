using ASPNET.StarterKit.Portal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;

namespace ASPNET.StarterKit.Portal.ProviderProof
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>执行独立 SQLite provider 能力验证的控制台入口。</zh-CN>
    ///   <en>Console entry point for running the isolated SQLite provider capability proof.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该 proof 只验证多数据库抽象的最小行为，不读取门户真实连接串，也不把测试库注册到运行站点。</zh-CN>
    ///   <en>This proof verifies only the minimal behavior of the multi-database abstraction. It does not read the portal's real connection string or register the test database with the running site.</en>
    /// </lang>
    /// </remarks>
    internal static class Program
    {
        private const string SchemaStep = "P3DP01.Schema";
        private const string ReadWriteStep = "P3DP02.ParameterizedReadWrite";
        private const string CommitStep = "P3DP03.TransactionCommit";
        private const string RollbackStep = "P3DP04.TransactionRollback";
        private const string UniqueStep = "P3DP05.UniqueConstraint";

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取命令行参数、建立隔离 profile，并按固定步骤执行 provider proof。</zh-CN>
        ///   <en>Reads command-line arguments, creates an isolated profile, and executes provider proof steps in a fixed order.</en>
        /// </lang>
        /// </summary>
        /// <param name="args">
        /// <l>
        ///   <zh-CN>必须包含 <c>--database</c> 和 <c>--schema</c> 的参数数组。</zh-CN>
        ///   <en>Argument array that must contain <c>--database</c> and <c>--schema</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>0 表示全部验证通过，1 表示验证失败，2 表示参数无效。</zh-CN>
        ///   <en>Returns 0 when all checks pass, 1 for proof failure, and 2 for invalid arguments.</en>
        /// </l>
        /// </returns>
        private static int Main(string[] args)
        {
            string databasePath;
            string schemaPath;
            if (!TryReadArguments(args, out databasePath, out schemaPath))
            {
                Console.Error.WriteLine("Usage: Portal.DataProviderProof.exe --database <path> --schema <path>");
                return 2;
            }

            try
            {
                // proof 仅使用 SQLite profile，不复用或改写门户主业务连接串。
                var profile = new PortalDatabaseProfile(
                    "ProviderProof",
                    PortalDatabaseProviderNames.Sqlite,
                    BuildSqliteConnectionString(databasePath),
                    "test",
                    PortalDatabasePurpose.ProviderProof);

                var proof = new PortalDatabaseCapabilityProof(
                    new PortalDbConnectionFactory(),
                    profile,
                    schemaPath);
                IReadOnlyList<ProofResult> results = proof.Run();

                bool passed = true;
                foreach (ProofResult result in results)
                {
                    Console.WriteLine((result.Passed ? "PASS " : "FAIL ") + result.Step + " - " + result.Message);
                    passed &= result.Passed;
                }

                return passed ? 0 : 1;
            }
            catch (Exception exception)
            {
                // 控制台输出不回显连接串、数据库路径或原始 provider 异常文本。
                Console.Error.WriteLine("FAIL P3DP00.Startup - " + GetExceptionTypeChain(exception));
                return 1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成不含敏感消息正文的异常类型链。</zh-CN>
        ///   <en>Builds an exception type chain without sensitive message text.</en>
        /// </lang>
        /// </summary>
        private static string GetExceptionTypeChain(Exception exception)
        {
            var names = new List<string>();
            Exception current = exception;
            while (current != null && names.Count < 4)
            {
                names.Add(current.GetType().Name);
                current = current.InnerException;
            }

            return string.Join(" > ", names);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并校验 proof 必需的数据库路径和 schema 路径。</zh-CN>
        ///   <en>Reads and validates the database path and schema path required by the proof.</en>
        /// </lang>
        /// </summary>
        private static bool TryReadArguments(string[] args, out string databasePath, out string schemaPath)
        {
            databasePath = ReadOption(args, "--database");
            schemaPath = ReadOption(args, "--schema");
            return !string.IsNullOrWhiteSpace(databasePath) &&
                   !string.IsNullOrWhiteSpace(schemaPath) &&
                   File.Exists(schemaPath);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从简单命令行数组中读取一个成对选项值。</zh-CN>
        ///   <en>Reads one paired option value from a simple command-line array.</en>
        /// </lang>
        /// </summary>
        private static string ReadOption(string[] args, string optionName)
        {
            if (args == null)
            {
                return string.Empty;
            }

            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为隔离 proof 数据库生成 SQLite 连接串并确保目录存在。</zh-CN>
        ///   <en>Builds a SQLite connection string for the isolated proof database and ensures its directory exists.</en>
        /// </lang>
        /// </summary>
        private static string BuildSqliteConnectionString(string databasePath)
        {
            string fullPath = Path.GetFullPath(databasePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return "Data Source=" + fullPath + ";Version=3;Foreign Keys=True;";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把数据库抽象的关键能力封装成可重复运行的 proof 步骤。</zh-CN>
        ///   <en>Encapsulates key database-abstraction capabilities as repeatable proof steps.</en>
        /// </lang>
        /// </summary>
        private sealed class PortalDatabaseCapabilityProof
        {
            private readonly IPortalDbConnectionFactory _connectionFactory;
            private readonly PortalDatabaseProfile _profile;
            private readonly string _schemaPath;

            /// <summary>
            /// <lang>
            ///   <zh-CN>创建 provider proof 运行器。</zh-CN>
            ///   <en>Creates a provider proof runner.</en>
            /// </lang>
            /// </summary>
            /// <param name="connectionFactory">
            /// <l>
            ///   <zh-CN>按 profile 创建连接的工厂。</zh-CN>
            ///   <en>Factory that creates connections from profiles.</en>
            /// </l>
            /// </param>
            /// <param name="profile">
            /// <l>
            ///   <zh-CN>隔离 proof 数据库 profile。</zh-CN>
            ///   <en>Isolated proof database profile.</en>
            /// </l>
            /// </param>
            /// <param name="schemaPath">
            /// <l>
            ///   <zh-CN>受版本库控制的 proof schema 文件路径。</zh-CN>
            ///   <en>Repository-controlled proof schema file path.</en>
            /// </l>
            /// </param>
            public PortalDatabaseCapabilityProof(
                IPortalDbConnectionFactory connectionFactory,
                PortalDatabaseProfile profile,
                string schemaPath)
            {
                _connectionFactory = connectionFactory;
                _profile = profile;
                _schemaPath = schemaPath;
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>运行 schema、参数化读写、事务提交/回滚和唯一约束验证。</zh-CN>
            ///   <en>Runs schema, parameterized read/write, transaction commit/rollback, and unique-constraint checks.</en>
            /// </lang>
            /// </summary>
            /// <returns>
            /// <l>
            ///   <zh-CN>每个 proof 步骤的脱敏结果集合。</zh-CN>
            ///   <en>Redacted result collection for each proof step.</en>
            /// </l>
            /// </returns>
            public IReadOnlyList<ProofResult> Run()
            {
                var results = new List<ProofResult>();
                // 所有能力验证共享一个短生命周期连接，避免 proof 引入门户运行期的连接管理策略。
                using (DbConnection connection = _connectionFactory.CreateConnection(_profile))
                {
                    connection.Open();
                    results.Add(RunStep(SchemaStep, () => ApplySchema(connection)));
                    results.Add(RunStep(ReadWriteStep, () => VerifyParameterizedReadWrite(connection)));
                    results.Add(RunStep(CommitStep, () => VerifyTransactionCommit(connection)));
                    results.Add(RunStep(RollbackStep, () => VerifyTransactionRollback(connection)));
                    results.Add(RunStep(UniqueStep, () => VerifyUniqueConstraint(connection)));
                }

                return results;
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>执行单个 proof 步骤，并将异常压缩为安全的类型名。</zh-CN>
            ///   <en>Runs one proof step and compresses exceptions into safe type names.</en>
            /// </lang>
            /// </summary>
            private static ProofResult RunStep(string step, Action action)
            {
                try
                {
                    action();
                    return new ProofResult(step, true, "verified");
                }
                catch (Exception exception)
                {
                    // 错误类型足以定位 proof 失败类别，避免在控制台暴露敏感连接信息。
                    return new ProofResult(step, false, exception.GetType().Name);
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>将受控 schema 应用到隔离 proof 数据库。</zh-CN>
            ///   <en>Applies the controlled schema to the isolated proof database.</en>
            /// </lang>
            /// </summary>
            private void ApplySchema(DbConnection connection)
            {
                string schema = File.ReadAllText(_schemaPath);
                // SQLite proof schema 受版本库控制，只含本 proof 的独立表。
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = schema;
                    command.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>验证参数化写入和读取能够完整往返 UTC 文本。</zh-CN>
            ///   <en>Verifies that parameterized write and read can round-trip UTC text.</en>
            /// </lang>
            /// </summary>
            private static void VerifyParameterizedReadWrite(DbConnection connection)
            {
                string recordedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                Insert(connection, "read-write", recordedUtc, "parameterized");

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT RecordedUtc FROM PortalDataProviderProof WHERE ProofKey = @ProofKey;";
                    AddTextParameter(command, "@ProofKey", "read-write");
                    string actual = Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                    if (!string.Equals(actual, recordedUtc, StringComparison.Ordinal))
                    {
                        throw new DataException("The UTC value did not round-trip.");
                    }
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>验证事务提交后的数据可见性。</zh-CN>
            ///   <en>Verifies data visibility after transaction commit.</en>
            /// </lang>
            /// </summary>
            private static void VerifyTransactionCommit(DbConnection connection)
            {
                // 提交后必须可见，确认 provider 的基本事务提交语义。
                using (DbTransaction transaction = connection.BeginTransaction())
                {
                    Insert(connection, "transaction-commit", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), "commit", transaction);
                    transaction.Commit();
                }

                if (Count(connection, "transaction-commit") != 1)
                {
                    throw new DataException("The committed row was not found.");
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>验证事务回滚不会留下 proof 测试行。</zh-CN>
            ///   <en>Verifies that transaction rollback leaves no proof test row behind.</en>
            /// </lang>
            /// </summary>
            private static void VerifyTransactionRollback(DbConnection connection)
            {
                // 回滚后不得留下测试行，避免把仅能执行命令误判为具备事务能力。
                using (DbTransaction transaction = connection.BeginTransaction())
                {
                    Insert(connection, "transaction-rollback", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), "rollback", transaction);
                    transaction.Rollback();
                }

                if (Count(connection, "transaction-rollback") != 0)
                {
                    throw new DataException("The rolled-back row still exists.");
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>验证 provider 会拒绝重复唯一键。</zh-CN>
            ///   <en>Verifies that the provider rejects duplicate unique keys.</en>
            /// </lang>
            /// </summary>
            private static void VerifyUniqueConstraint(DbConnection connection)
            {
                Insert(connection, "unique-key", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), "first");

                try
                {
                    Insert(connection, "unique-key", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), "duplicate");
                }
                catch (DbException)
                {
                    return;
                }

                throw new DataException("The provider accepted a duplicate unique key.");
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>用参数化命令插入一条 proof 记录。</zh-CN>
            ///   <en>Inserts one proof record with a parameterized command.</en>
            /// </lang>
            /// </summary>
            private static void Insert(
                DbConnection connection,
                string proofKey,
                string recordedUtc,
                string note,
                DbTransaction transaction = null)
            {
                using (DbCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
INSERT INTO PortalDataProviderProof (ProofKey, RecordedUtc, Note)
VALUES (@ProofKey, @RecordedUtc, @Note);";
                    AddTextParameter(command, "@ProofKey", proofKey);
                    AddTextParameter(command, "@RecordedUtc", recordedUtc);
                    AddTextParameter(command, "@Note", note);
                    command.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>按 proof key 统计记录数。</zh-CN>
            ///   <en>Counts rows by proof key.</en>
            /// </lang>
            /// </summary>
            private static int Count(DbConnection connection, string proofKey)
            {
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(1) FROM PortalDataProviderProof WHERE ProofKey = @ProofKey;";
                    AddTextParameter(command, "@ProofKey", proofKey);
                    return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                }
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>创建并添加字符串参数，统一空值处理。</zh-CN>
            ///   <en>Creates and adds a string parameter with unified null handling.</en>
            /// </lang>
            /// </summary>
            private static void AddTextParameter(DbCommand command, string name, string value)
            {
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.DbType = DbType.String;
                parameter.Value = value ?? string.Empty;
                command.Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>单个 provider proof 步骤的脱敏结果。</zh-CN>
        ///   <en>Redacted result of a single provider proof step.</en>
        /// </lang>
        /// </summary>
        private sealed class ProofResult
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>创建 proof 步骤结果。</zh-CN>
            ///   <en>Creates a proof-step result.</en>
            /// </lang>
            /// </summary>
            public ProofResult(string step, bool passed, string message)
            {
                Step = step;
                Passed = passed;
                Message = message;
            }

            /// <summary>
            /// <l>
            ///   <zh-CN>步骤标识。</zh-CN>
            ///   <en>Step identifier.</en>
            /// </l>
            /// </summary>
            public string Step { get; private set; }

            /// <summary>
            /// <l>
            ///   <zh-CN>步骤是否通过。</zh-CN>
            ///   <en>Whether the step passed.</en>
            /// </l>
            /// </summary>
            public bool Passed { get; private set; }

            /// <summary>
            /// <l>
            ///   <zh-CN>不包含连接串或路径的结果说明。</zh-CN>
            ///   <en>Result message that excludes connection strings and paths.</en>
            /// </l>
            /// </summary>
            public string Message { get; private set; }
        }
    }
}
