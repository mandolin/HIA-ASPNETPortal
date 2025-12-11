using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// DiscussionsDb 类实现了 IDiscussionsDb 接口，用于与数据库交互，管理讨论区的数据。
    /// </summary>
    /// <seealso cref="ASPNET.StarterKit.Portal.IDiscussionsDb" />
    public class DiscussionsDb : IDiscussionsDb
    {
        // 存储数据库连接字符串的私有字段
        private readonly string _connectionString;

        /// <summary>
        /// 初始化 DiscussionsDb 类的新实例。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        public DiscussionsDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region IDiscussionsDb Members

        /// <summary>
        /// 获取给定模块ID下的顶级消息。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>包含顶层消息的数据读取器。</returns>
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
                    // 一次性获取所有列索引（性能最佳）
                    int idxItemID = reader.GetOrdinal("ItemID");
                    int idxChildCount = reader.GetOrdinal("ChildCount");
                    //int idxModuleID = reader.GetOrdinal("ModuleID");
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
                            ModuleID = moduleId,//reader.GetInt32(idxModuleID),
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
        /// 获取指定父级ID的消息线程（回复列表）
        /// </summary>
        /// <param name="parent">父级 DisplayOrder 路径，如 "0001." 或 "0001.0002."</param>
        /// <returns>线程中的所有消息列表</returns>
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
        /// 获取单条消息的详细信息
        /// </summary>
        /// <param name="itemId">消息 ItemID</param>
        /// <returns>单条消息对象，未找到返回 null</returns>
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
        /// 向数据库添加一条新消息（发帖或回复）
        /// </summary>
        /// <returns>新添加的消息的 ItemID</returns>
        public int AddMessage(int moduleId, int parentId, string userName, string title, string body)
        {
            if (string.IsNullOrWhiteSpace(userName))
                userName = "unknown";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_AddMessage", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                // 输出参数
                SqlParameter itemIdParam = new SqlParameter("@ItemID", SqlDbType.Int);
                itemIdParam.Direction = ParameterDirection.Output;
                command.Parameters.Add(itemIdParam);

                // 输入参数
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