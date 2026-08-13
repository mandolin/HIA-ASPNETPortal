using System.Data;
using System.Data.SqlClient;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户模块数据访问的历史数据库门面。</zh-CN>
    ///   <en>Legacy database facade for portal-module data access.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型只保留旧模块删除契约；连接字符串由调用方提供，连接和命令均在单次操作内创建并释放。</zh-CN>
    ///   <en>This type retains only the legacy module-deletion contract; the caller supplies the connection string, and each operation creates and disposes its connection and command within one call.</en>
    /// </lang>
    /// </remarks>
    /// <seealso cref="ASPNET.StarterKit.Portal.IPortalDb" />
    public class PortalDb : IPortalDb
    {
        // <lang>
        //   <zh-CN>保存由组合根提供的连接字符串；此字段只用于当前数据库操作，不记录或输出凭据内容。</zh-CN>
        //   <en>Retain the connection string supplied by the composition root for the current database operation; this field neither logs nor exposes credential content.</en>
        // </lang>
        private readonly string _connectionString;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化门户模块数据库门面。</zh-CN>
        ///   <en>Initializes the portal-module database facade.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>由外部配置组合根提供的数据库连接字符串；本类只保存引用，不验证或输出其内容。</zh-CN>
        ///   <en>Database connection string supplied by the external-configuration composition root; this type stores it without validating or exposing its content.</en>
        /// </l>
        /// </param>
        public PortalDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region IPortalDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>调用受控存储过程删除指定模块的历史数据。</zh-CN>
        ///   <en>Calls the controlled stored procedure that deletes legacy data for a specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>要删除的模块数值标识；具体存在性和删除范围由数据库存储过程契约决定。</zh-CN>
        ///   <en>Numeric identifier of the module to delete; existence and deletion scope are governed by the database stored-procedure contract.</en>
        /// </l>
        /// </param>
        public void DeleteModule(int moduleId)
        {
            // <lang>
            //   <zh-CN>为本次删除建立短生命周期连接；连接不会跨调用复用，避免持有过期或跨请求状态。</zh-CN>
            //   <en>Create a short-lived connection for this deletion; do not reuse it across calls so stale or cross-request state cannot be retained.</en>
            // </lang>
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // <lang>
                //   <zh-CN>使用固定存储过程名称创建命令；模块标识通过显式参数传递，不拼接进 SQL 文本。</zh-CN>
                //   <en>Create a command with a fixed stored-procedure name; pass the module identifier as an explicit parameter rather than concatenating it into SQL text.</en>
                // </lang>
                using (SqlCommand command = new SqlCommand("Portal_DeleteModule", connection))
                {
                    // <lang>
                    //   <zh-CN>将命令解释为存储过程调用，确保固定名称不会被当作普通 SQL 文本执行。</zh-CN>
                    //   <en>Interpret the command as a stored-procedure invocation so the fixed name is not executed as ordinary SQL text.</en>
                    // </lang>
                    command.CommandType = CommandType.StoredProcedure;

                    // <lang>
                    //   <zh-CN>创建强类型模块标识参数并保留存储过程既有参数名，避免输入值改变命令结构。</zh-CN>
                    //   <en>Create a strongly typed module-identifier parameter using the established name so input values cannot alter command structure.</en>
                    // </lang>
                    SqlParameter moduleIdParam = new SqlParameter("@ModuleID", SqlDbType.Int);
                    // <lang>
                    //   <zh-CN>把已由调用方解析的数值标识放入参数值；此处不做新的业务存在性判断。</zh-CN>
                    //   <en>Assign the caller-resolved numeric identifier to the parameter; this layer does not add a separate business-existence check.</en>
                    // </lang>
                    moduleIdParam.Value = moduleId;
                    command.Parameters.Add(moduleIdParam);

                    // <lang>
                    //   <zh-CN>仅在命令已完成类型和参数配置后打开连接，缩短连接处于可用状态但尚未执行的窗口。</zh-CN>
                    //   <en>Open the connection only after command type and parameters are configured, minimizing the window in which an available connection is waiting to execute.</en>
                    // </lang>
                    connection.Open();

                    // <lang>
                    //   <zh-CN>执行删除存储过程并忽略其行数结果；提交、回滚和具体删除范围由数据库过程契约负责。</zh-CN>
                    //   <en>Execute the deletion procedure and ignore its row-count result; commit, rollback, and exact deletion scope belong to the database-procedure contract.</en>
                    // </lang>
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion
    }
}
