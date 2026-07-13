using ASPNET.StarterKit.Portal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;

namespace ASPNET.StarterKit.Portal.ProviderProof
{
    internal static class Program
    {
        private const string SchemaStep = "P3DP01.Schema";
        private const string ReadWriteStep = "P3DP02.ParameterizedReadWrite";
        private const string CommitStep = "P3DP03.TransactionCommit";
        private const string RollbackStep = "P3DP04.TransactionRollback";
        private const string UniqueStep = "P3DP05.UniqueConstraint";

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

        private static bool TryReadArguments(string[] args, out string databasePath, out string schemaPath)
        {
            databasePath = ReadOption(args, "--database");
            schemaPath = ReadOption(args, "--schema");
            return !string.IsNullOrWhiteSpace(databasePath) &&
                   !string.IsNullOrWhiteSpace(schemaPath) &&
                   File.Exists(schemaPath);
        }

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

        private sealed class PortalDatabaseCapabilityProof
        {
            private readonly IPortalDbConnectionFactory _connectionFactory;
            private readonly PortalDatabaseProfile _profile;
            private readonly string _schemaPath;

            public PortalDatabaseCapabilityProof(
                IPortalDbConnectionFactory connectionFactory,
                PortalDatabaseProfile profile,
                string schemaPath)
            {
                _connectionFactory = connectionFactory;
                _profile = profile;
                _schemaPath = schemaPath;
            }

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

            private static int Count(DbConnection connection, string proofKey)
            {
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(1) FROM PortalDataProviderProof WHERE ProofKey = @ProofKey;";
                    AddTextParameter(command, "@ProofKey", proofKey);
                    return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                }
            }

            private static void AddTextParameter(DbCommand command, string name, string value)
            {
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.DbType = DbType.String;
                parameter.Value = value ?? string.Empty;
                command.Parameters.Add(parameter);
            }
        }

        private sealed class ProofResult
        {
            public ProofResult(string step, bool passed, string message)
            {
                Step = step;
                Passed = passed;
                Message = message;
            }

            public string Step { get; private set; }

            public bool Passed { get; private set; }

            public string Message { get; private set; }
        }
    }
}
