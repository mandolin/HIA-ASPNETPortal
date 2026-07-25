using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>通过旧版讨论区存储过程读写讨论主题和回复。</zh-CN>
    ///   <en>Reads and writes discussion topics and replies through the legacy discussion stored procedures.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该实现保留 ASP.NET Portal Starter Kit 的 DisplayOrder 线程排序约定，并只负责数据访问；模块权限、父消息归属和页面级安全检查由调用方在进入数据库层前完成。</zh-CN>
    ///   <en>This implementation preserves the ASP.NET Portal Starter Kit DisplayOrder threading convention and is responsible only for data access; module permissions, parent-message ownership, and page-level security checks are completed by callers before entering the database layer.</en>
    /// </lang>
    /// </remarks>
    /// <seealso cref="ASPNET.StarterKit.Portal.IDiscussionsDb" />
    public class DiscussionsDb : IDiscussionsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前门户数据源连接串，由外部配置加载后注入。</zh-CN>
        ///   <en>Connection string for the current portal data source, injected after external configuration loading.</en>
        /// </lang>
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化讨论区数据访问对象。</zh-CN>
        ///   <en>Initializes the discussion data-access object.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>门户数据库连接串；调用方负责保证它来自已验证的配置源。</zh-CN>
        ///   <en>Portal database connection string; the caller is responsible for ensuring it comes from a verified configuration source.</en>
        /// </l>
        /// </param>
        public DiscussionsDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region IDiscussionsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定模块下的顶级讨论主题。</zh-CN>
        ///   <en>Gets top-level discussion topics for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>讨论模块实例 ID。</zh-CN>
        ///   <en>Discussion module instance ID.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按存储过程排序返回的顶级主题列表。</zh-CN>
        ///   <en>Top-level topic list returned in stored-procedure order.</en>
        /// </l>
        /// </returns>
        public List<IDiscussionItem> GetTopLevelMessages(int moduleId)
        {
            var list = new List<IDiscussionItem>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_GetTopLevelMessages", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ModuleID", SqlDbType.Int) { Value = moduleId });

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // <lang>
                    //   <zh-CN>循环前缓存列序号，减少每行读取时的名称查找；当前顶级主题过程不返回 ModuleID，投影时使用入参保持模块上下文。</zh-CN>
                    //   <en>Cache column ordinals before the loop to avoid per-row name lookup; the current top-level-topic procedure does not return ModuleID, so projection uses the input value to preserve module context.</en>
                    // </lang>
                    int idxItemID = reader.GetOrdinal("ItemID");
                    int idxChildCount = reader.GetOrdinal("ChildCount");
                    int idxTitle = reader.GetOrdinal("Title");
                    int idxCreatedDate = reader.GetOrdinal("CreatedDate");
                    int idxBody = reader.GetOrdinal("Body");
                    int idxDisplayOrder = reader.GetOrdinal("DisplayOrder");
                    int idxCreatedByUser = reader.GetOrdinal("CreatedByUser");

                    while (reader.Read())
                    {
                        var item = new DiscussionItem
                        {
                            ItemID = reader.GetInt32(idxItemID),
                            ChildCount = reader.GetInt32(idxChildCount),
                            ModuleID = moduleId,
                            Title = reader.IsDBNull(idxTitle) ? null : reader.GetString(idxTitle),
                            CreatedDate = reader.IsDBNull(idxCreatedDate) ? (DateTime?)null : reader.GetDateTime(idxCreatedDate),
                            Body = reader.IsDBNull(idxBody) ? null : reader.GetString(idxBody),
                            DisplayOrder = reader.IsDBNull(idxDisplayOrder) ? null : reader.GetString(idxDisplayOrder),
                            CreatedByUser = reader.IsDBNull(idxCreatedByUser) ? null : reader.GetString(idxCreatedByUser)
                        };
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按父级 DisplayOrder 路径获取某个讨论主题下的回复线程。</zh-CN>
        ///   <en>Gets the reply thread beneath a discussion topic by parent DisplayOrder path.</en>
        /// </lang>
        /// </summary>
        /// <param name="parent">
        /// <l>
        ///   <zh-CN>父级 DisplayOrder 路径，例如 <c>0001.</c> 或 <c>0001.0002.</c>；空值会按旧存储过程约定传入数据库空值。</zh-CN>
        ///   <en>Parent DisplayOrder path, for example <c>0001.</c> or <c>0001.0002.</c>; a blank value is passed as database null to preserve the legacy stored-procedure convention.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>线程中的回复消息列表。</zh-CN>
        ///   <en>Reply message list in the thread.</en>
        /// </l>
        /// </returns>
        public List<IDiscussionItem> GetThreadMessages(string parent)
        {
            var list = new List<IDiscussionItem>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_GetThreadMessages", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter param = new SqlParameter("@Parent", SqlDbType.NVarChar, 750);
                param.Value = parent ?? (object)DBNull.Value;
                command.Parameters.Add(param);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // <lang>
                    //   <zh-CN>回复线程过程会返回 ModuleID；这里以数据库值为准，方便调用方在详情页继续做模块归属判断。</zh-CN>
                    //   <en>The reply-thread procedure returns ModuleID; this method keeps the database value so callers can continue module-ownership checks on detail pages.</en>
                    // </lang>
                    int idxItemID = reader.GetOrdinal("ItemID");
                    int idxModuleID = reader.GetOrdinal("ModuleID");
                    int idxTitle = reader.GetOrdinal("Title");
                    int idxCreatedDate = reader.GetOrdinal("CreatedDate");
                    int idxBody = reader.GetOrdinal("Body");
                    int idxDisplayOrder = reader.GetOrdinal("DisplayOrder");
                    int idxCreatedByUser = reader.GetOrdinal("CreatedByUser");

                    while (reader.Read())
                    {
                        var item = new DiscussionItem
                        {
                            ItemID = reader.GetInt32(idxItemID),
                            ModuleID = reader.GetInt32(idxModuleID),
                            Title = reader.IsDBNull(idxTitle) ? null : reader.GetString(idxTitle),
                            CreatedDate = reader.IsDBNull(idxCreatedDate) ? (DateTime?)null : reader.GetDateTime(idxCreatedDate),
                            Body = reader.IsDBNull(idxBody) ? null : reader.GetString(idxBody),
                            DisplayOrder = reader.IsDBNull(idxDisplayOrder) ? null : reader.GetString(idxDisplayOrder),
                            CreatedByUser = reader.IsDBNull(idxCreatedByUser) ? null : reader.GetString(idxCreatedByUser)
                        };
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单条讨论消息的详细信息。</zh-CN>
        ///   <en>Gets the details of a single discussion message.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>消息标识符。</zh-CN>
        ///   <en>Message identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>消息对象；未找到时为 <c>null</c>，调用方负责转换为页面级提示或错误页。</zh-CN>
        ///   <en>Message object, or <c>null</c> when it is not found; the caller is responsible for converting that result into a page-level message or error page.</en>
        /// </l>
        /// </returns>
        public IDiscussionItem GetSingleMessage(int itemId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_GetSingleMessage", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ItemID", SqlDbType.Int) { Value = itemId });

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // <lang>
                    //   <zh-CN>只读取第一行，保持“单条消息”查询契约；存储过程若返回重复行，应由数据库侧约束修复。</zh-CN>
                    //   <en>Read only the first row to preserve the single-message query contract; if the stored procedure returns duplicate rows, the database constraints should be corrected.</en>
                    // </lang>
                    if (reader.Read())
                    {
                        int idxItemID = reader.GetOrdinal("ItemID");
                        int idxModuleID = reader.GetOrdinal("ModuleID");
                        int idxTitle = reader.GetOrdinal("Title");
                        int idxCreatedDate = reader.GetOrdinal("CreatedDate");
                        int idxBody = reader.GetOrdinal("Body");
                        int idxDisplayOrder = reader.GetOrdinal("DisplayOrder");
                        int idxCreatedByUser = reader.GetOrdinal("CreatedByUser");

                        return new DiscussionItem
                        {
                            ItemID = reader.GetInt32(idxItemID),
                            ModuleID = reader.GetInt32(idxModuleID),
                            Title = reader.IsDBNull(idxTitle) ? null : reader.GetString(idxTitle),
                            CreatedDate = reader.IsDBNull(idxCreatedDate) ? (DateTime?)null : reader.GetDateTime(idxCreatedDate),
                            Body = reader.IsDBNull(idxBody) ? null : reader.GetString(idxBody),
                            DisplayOrder = reader.IsDBNull(idxDisplayOrder) ? null : reader.GetString(idxDisplayOrder),
                            CreatedByUser = reader.IsDBNull(idxCreatedByUser) ? null : reader.GetString(idxCreatedByUser)
                        };
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向数据库添加一条新讨论消息，可以是主题或回复。</zh-CN>
        ///   <en>Adds a new discussion message to the database, either as a topic or as a reply.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方必须先完成模块访问、父消息归属和发帖权限检查；本方法只封装旧存储过程调用。空用户名会降级为历史占位值 <c>unknown</c>，该值不能被视作真实认证身份。</zh-CN>
        ///   <en>Callers must complete module access, parent-message ownership, and posting permission checks before this method is reached; this method only wraps the legacy stored procedure call. A blank user name falls back to the historical placeholder <c>unknown</c>, which must not be treated as a real authenticated identity.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>目标讨论模块实例 ID。</zh-CN>
        ///   <en>Target discussion module instance ID.</en>
        /// </l>
        /// </param>
        /// <param name="parentId">
        /// <l>
        ///   <zh-CN>父消息 ID；创建顶级主题时由旧存储过程解释为根级写入。</zh-CN>
        ///   <en>Parent message ID; when creating a top-level topic, the legacy stored procedure interprets it as a root-level insert.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>用于显示和历史记录的用户名，不是授权判断来源。</zh-CN>
        ///   <en>User name used for display and historical records, not an authorization source.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>主题或回复标题；空值按数据库空值传入。</zh-CN>
        ///   <en>Topic or reply title; a blank value is passed as database null.</en>
        /// </l>
        /// </param>
        /// <param name="body">
        /// <l>
        ///   <zh-CN>消息正文；空值按数据库空值传入，展示层继续负责编码输出。</zh-CN>
        ///   <en>Message body; a blank value is passed as database null, and the display layer remains responsible for encoded output.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新消息标识符；存储过程未返回标识时为 <c>-1</c>。</zh-CN>
        ///   <en>New message identifier, or <c>-1</c> when the stored procedure does not return one.</en>
        /// </l>
        /// </returns>
        public int AddMessage(int moduleId, int parentId, string userName, string title, string body)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "unknown";
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_AddMessage", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                // <lang>
                //   <zh-CN>旧存储过程通过输出参数返回新消息 ID；如果没有返回，方法尾部会统一降级为 -1。</zh-CN>
                //   <en>The legacy stored procedure returns the new message ID through an output parameter; when it does not, the method tail falls back to -1.</en>
                // </lang>
                SqlParameter itemIdParam = new SqlParameter("@ItemID", SqlDbType.Int);
                itemIdParam.Direction = ParameterDirection.Output;
                command.Parameters.Add(itemIdParam);

                // <lang>
                //   <zh-CN>保留旧过程的参数名称，避免影响现有 SQL 脚本和 SQL Server 2016+ 兼容矩阵。</zh-CN>
                //   <en>Keep the legacy procedure parameter names so existing SQL scripts and the SQL Server 2016+ compatibility matrix are not disturbed.</en>
                // </lang>
                command.Parameters.AddWithValue("@ModuleID", moduleId);
                command.Parameters.AddWithValue("@ParentID", parentId);
                command.Parameters.AddWithValue("@UserName", userName);
                command.Parameters.AddWithValue("@Title", title ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Body", body ?? (object)DBNull.Value);

                connection.Open();
                command.ExecuteNonQuery();

                return itemIdParam.Value == DBNull.Value ? -1 : Convert.ToInt32(itemIdParam.Value);
            }
        }

        #endregion
    }
}
