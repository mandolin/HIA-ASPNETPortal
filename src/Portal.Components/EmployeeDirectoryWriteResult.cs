namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工组织后台写入操作的受控结果。</zh-CN>
    ///   <en>Controlled result for employee-directory administration write operations.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>结果对象只暴露管理员可安全读取的状态和提示，不包含数据库异常全文、连接串或敏感资料原文。</zh-CN>
    ///   <en>The result exposes only administrator-safe status and message text, never raw database exceptions, connection strings, or sensitive profile values.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeDirectoryWriteResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建员工组织后台写入结果。</zh-CN>
        ///   <en>Creates an employee-directory administration write result.</en>
        /// </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>写入是否成功。</zh-CN>
        ///   <en>Whether the write succeeded.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>被写入实体的数值标识。</zh-CN>
        ///   <en>Numeric identifier of the written entity.</en>
        /// </lang>
        /// </summary>
        public int EntityId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可展示给管理员的安全提示。</zh-CN>
        ///   <en>Administrator-safe display message.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否因为并发更新被拒绝。</zh-CN>
        ///   <en>Whether the write was rejected by a concurrency conflict.</en>
        /// </lang>
        /// </summary>
        public bool Conflict { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>目标记录是否不存在。</zh-CN>
        ///   <en>Whether the target row was not found.</en>
        /// </lang>
        /// </summary>
        public bool NotFound { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建成功结果。</zh-CN>
        ///   <en>Creates a success result.</en>
        /// </lang>
        /// </summary>
        public static EmployeeDirectoryWriteResult Success(int entityId, string message)
        {
            return new EmployeeDirectoryWriteResult(true, entityId, message, false, false);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建普通失败结果。</zh-CN>
        ///   <en>Creates a regular failure result.</en>
        /// </lang>
        /// </summary>
        public static EmployeeDirectoryWriteResult Failed(string message)
        {
            return new EmployeeDirectoryWriteResult(false, 0, message, false, false);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建目标不存在结果。</zh-CN>
        ///   <en>Creates a not-found result.</en>
        /// </lang>
        /// </summary>
        public static EmployeeDirectoryWriteResult Missing(string message)
        {
            return new EmployeeDirectoryWriteResult(false, 0, message, false, true);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建并发冲突结果。</zh-CN>
        ///   <en>Creates a concurrency-conflict result.</en>
        /// </lang>
        /// </summary>
        public static EmployeeDirectoryWriteResult ConcurrencyConflict(string message)
        {
            return new EmployeeDirectoryWriteResult(false, 0, message, true, false);
        }
    }
}
