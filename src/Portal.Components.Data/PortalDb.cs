using System.Data;
using System.Data.SqlClient;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// PortalDb 类实现了 IPortalDb 接口，用于与数据库交互，管理门户模块的数据。
    /// </summary>
    /// <seealso cref="ASPNET.StarterKit.Portal.IPortalDb" />
    public class PortalDb : IPortalDb
    {
        // 存储数据库连接字符串的私有字段
        private readonly string _connectionString;

        /// <summary>
        /// 初始化 PortalDb 类的新实例。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        public PortalDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region IPortalDb Members

        /// <summary>
        /// 删除数据库中与指定模块相关的所有信息。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        public void DeleteModule(int moduleId)
        {
            // 创建 SqlConnection 对象
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // 创建 SqlCommand 对象
                using (SqlCommand command = new SqlCommand("Portal_DeleteModule", connection))
                {
                    // 设置命令类型为存储过程
                    command.CommandType = CommandType.StoredProcedure;

                    // 添加参数 @ModuleID 到存储过程中
                    SqlParameter moduleIdParam = new SqlParameter("@ModuleID", SqlDbType.Int);
                    moduleIdParam.Value = moduleId;
                    command.Parameters.Add(moduleIdParam);

                    // 打开数据库连接
                    connection.Open();

                    // 执行存储过程
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion
    }
}