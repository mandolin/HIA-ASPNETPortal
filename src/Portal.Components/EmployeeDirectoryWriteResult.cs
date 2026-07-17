namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工组织后台写入操作的受控结果。
    ///
    /// English: Controlled result for employee-directory administration write operations.
    /// </summary>
    /// <remarks>
    /// 中文：结果对象只暴露管理员可安全读取的状态和提示，不包含数据库异常全文、连接串或敏感资料原文。
    ///
    /// English: The result exposes only administrator-safe status and message text, never raw database exceptions,
    /// connection strings, or sensitive profile values.
    /// </remarks>
    public sealed class EmployeeDirectoryWriteResult
    {
        /// <summary>
        /// 中文：创建员工组织后台写入结果。
        ///
        /// English: Creates an employee-directory administration write result.
        /// </summary>
        public EmployeeDirectoryWriteResult(
            bool succeeded,
            int entityId,
            string message,
            bool conflict,
            bool notFound)
        {
            Succeeded = succeeded;
            EntityId = entityId;
            Message = message ?? string.Empty;
            Conflict = conflict;
            NotFound = notFound;
        }

        /// <summary>中文：写入是否成功。English: Whether the write succeeded.</summary>
        public bool Succeeded { get; private set; }

        /// <summary>中文：被写入实体的数值标识。English: Numeric identifier of the written entity.</summary>
        public int EntityId { get; private set; }

        /// <summary>中文：可展示给管理员的安全提示。English: Administrator-safe display message.</summary>
        public string Message { get; private set; }

        /// <summary>中文：是否因为并发更新被拒绝。English: Whether the write was rejected by a concurrency conflict.</summary>
        public bool Conflict { get; private set; }

        /// <summary>中文：目标记录是否不存在。English: Whether the target row was not found.</summary>
        public bool NotFound { get; private set; }

        /// <summary>
        /// 中文：创建成功结果。
        ///
        /// English: Creates a success result.
        /// </summary>
        public static EmployeeDirectoryWriteResult Success(int entityId, string message)
        {
            return new EmployeeDirectoryWriteResult(true, entityId, message, false, false);
        }

        /// <summary>
        /// 中文：创建普通失败结果。
        ///
        /// English: Creates a regular failure result.
        /// </summary>
        public static EmployeeDirectoryWriteResult Failed(string message)
        {
            return new EmployeeDirectoryWriteResult(false, 0, message, false, false);
        }

        /// <summary>
        /// 中文：创建目标不存在结果。
        ///
        /// English: Creates a not-found result.
        /// </summary>
        public static EmployeeDirectoryWriteResult Missing(string message)
        {
            return new EmployeeDirectoryWriteResult(false, 0, message, false, true);
        }

        /// <summary>
        /// 中文：创建并发冲突结果。
        ///
        /// English: Creates a concurrency-conflict result.
        /// </summary>
        public static EmployeeDirectoryWriteResult ConcurrencyConflict(string message)
        {
            return new EmployeeDirectoryWriteResult(false, 0, message, true, false);
        }
    }
}
