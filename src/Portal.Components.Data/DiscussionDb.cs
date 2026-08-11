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
            // <lang>
            //   <zh-CN>保存已由外部配置边界解析出的连接串引用；构造阶段不打开 SQL 连接，避免 DI 创建时产生隐式数据库 I/O。</zh-CN>
            //   <en>Store the connection string reference already resolved by the external-configuration boundary; construction does not open SQL connections, avoiding implicit database I/O during DI creation.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>输出列表只在本次读取内累积，承载已物化的顶级主题 DTO，不延迟持有 reader 或连接。</zh-CN>
            //   <en>The output list accumulates only within this read and carries materialized top-level topic DTOs without retaining the reader or connection lazily.</en>
            // </lang>
            var list = new List<IDiscussionItem>();

            // <lang>
            //   <zh-CN>连接和命令均限制在当前查询作用域；存储过程名称沿用旧数据库契约，不在代码层重写查询。</zh-CN>
            //   <en>Both connection and command are scoped to the current query; the stored-procedure name preserves the legacy database contract rather than rewriting the query in code.</en>
            // </lang>
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_GetTopLevelMessages", connection))
            {
                // <lang>
                //   <zh-CN>明确声明存储过程调用，避免命令文本被解释为即席 SQL。</zh-CN>
                //   <en>Explicitly declare a stored-procedure call so the command text is not interpreted as ad-hoc SQL.</en>
                // </lang>
                command.CommandType = CommandType.StoredProcedure;

                // <lang>
                //   <zh-CN>模块标识作为强类型整型参数传入；模块权限与可见性已由调用页在进入数据层前处理。</zh-CN>
                //   <en>The module identifier is passed as a strongly typed integer parameter; module permission and visibility are handled by the caller page before entering the data layer.</en>
                // </lang>
                command.Parameters.Add(new SqlParameter("@ModuleID", SqlDbType.Int) { Value = moduleId });

                // <lang>
                //   <zh-CN>仅在命令完全配置后打开连接，缩短连接占用时间并保持异常定位清晰。</zh-CN>
                //   <en>Open the connection only after the command is fully configured, shortening connection hold time and keeping exception origin clear.</en>
                // </lang>
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
                        // <lang>
                        //   <zh-CN>每一行立即投影为讨论项；可空列按旧表兼容语义保留为 null，展示层再决定空值呈现。</zh-CN>
                        //   <en>Each row is immediately projected into a discussion item; nullable columns preserve legacy-table null semantics and the presentation layer decides empty display.</en>
                        // </lang>
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

                        // <lang>
                        //   <zh-CN>按 reader 返回顺序追加，保留存储过程负责的顶级主题排序。</zh-CN>
                        //   <en>Append in reader order, preserving the top-level topic ordering owned by the stored procedure.</en>
                        // </lang>
                        list.Add(item);
                    }
                }
            }

            // <lang>
            //   <zh-CN>返回已脱离连接生命周期的列表；空模块结果以空列表表达，而不是 null。</zh-CN>
            //   <en>Return the list after it has been detached from the connection lifetime; an empty module result is represented as an empty list rather than null.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>输出列表只在本次线程读取内累积，保持和顶级主题查询一致的已物化返回契约。</zh-CN>
            //   <en>The output list accumulates only within this thread read, keeping the same materialized return contract as the top-level topic query.</en>
            // </lang>
            var list = new List<IDiscussionItem>();

            // <lang>
            //   <zh-CN>连接和命令作用域限定到一次线程查询；父路径解释继续由旧存储过程完成。</zh-CN>
            //   <en>The connection and command scopes are limited to one thread query; parent-path interpretation remains with the legacy stored procedure.</en>
            // </lang>
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_GetThreadMessages", connection))
            {
                // <lang>
                //   <zh-CN>明确使用存储过程执行模式，保持旧 SQL 契约和执行计划边界。</zh-CN>
                //   <en>Use stored-procedure execution mode explicitly to preserve the legacy SQL contract and execution-plan boundary.</en>
                // </lang>
                command.CommandType = CommandType.StoredProcedure;

                // <lang>
                //   <zh-CN>父级 DisplayOrder 路径最大长度沿用旧过程定义；null 显式转为数据库空值以表达根/缺省语义。</zh-CN>
                //   <en>The parent DisplayOrder path length follows the legacy procedure definition; null is explicitly converted to database null to express root/default semantics.</en>
                // </lang>
                SqlParameter param = new SqlParameter("@Parent", SqlDbType.NVarChar, 750);
                param.Value = parent ?? (object)DBNull.Value;
                command.Parameters.Add(param);

                // <lang>
                //   <zh-CN>参数配置完成后才打开连接，避免在本地参数准备阶段占用 SQL 连接。</zh-CN>
                //   <en>Open the connection only after parameters are configured so local parameter preparation does not hold a SQL connection.</en>
                // </lang>
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
                        // <lang>
                        //   <zh-CN>每条回复使用数据库返回的 ModuleID，以便调用方后续按真实归属复核安全边界。</zh-CN>
                        //   <en>Each reply uses the ModuleID returned by the database so callers can later re-check safety boundaries against true ownership.</en>
                        // </lang>
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

                        // <lang>
                        //   <zh-CN>按存储过程返回顺序追加，保留 DisplayOrder 线程排序。</zh-CN>
                        //   <en>Append in stored-procedure return order, preserving DisplayOrder thread ordering.</en>
                        // </lang>
                        list.Add(item);
                    }
                }
            }

            // <lang>
            //   <zh-CN>返回已物化回复列表；未找到子线程时保持空列表，便于页面安全展示“无回复”。</zh-CN>
            //   <en>Return the materialized reply list; when no child thread is found, keep an empty list so pages can safely display “no replies”.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>单条详情查询使用短生命周期连接和命令；调用方仍负责把 itemId 与当前模块上下文关联。</zh-CN>
            //   <en>The single-message detail query uses a short-lived connection and command; callers still associate itemId with the current module context.</en>
            // </lang>
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_GetSingleMessage", connection))
            {
                // <lang>
                //   <zh-CN>明确声明存储过程模式并使用强类型消息标识参数，避免字符串拼接 SQL。</zh-CN>
                //   <en>Declare stored-procedure mode explicitly and use a strongly typed message identifier parameter, avoiding string-concatenated SQL.</en>
                // </lang>
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ItemID", SqlDbType.Int) { Value = itemId });

                // <lang>
                //   <zh-CN>命令配置完成后打开连接；reader 关闭即释放连接作用域。</zh-CN>
                //   <en>Open the connection after command configuration; closing the reader releases the connection scope.</en>
                // </lang>
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // <lang>
                    //   <zh-CN>只读取第一行，保持“单条消息”查询契约；存储过程若返回重复行，应由数据库侧约束修复。</zh-CN>
                    //   <en>Read only the first row to preserve the single-message query contract; if the stored procedure returns duplicate rows, the database constraints should be corrected.</en>
                    // </lang>
                    if (reader.Read())
                    {
                        // <lang>
                        //   <zh-CN>列序号只在确认存在一行后解析；空结果不依赖 schema 元数据。</zh-CN>
                        //   <en>Resolve column ordinals only after confirming a row exists; empty results do not depend on schema metadata.</en>
                        // </lang>
                        int idxItemID = reader.GetOrdinal("ItemID");
                        int idxModuleID = reader.GetOrdinal("ModuleID");
                        int idxTitle = reader.GetOrdinal("Title");
                        int idxCreatedDate = reader.GetOrdinal("CreatedDate");
                        int idxBody = reader.GetOrdinal("Body");
                        int idxDisplayOrder = reader.GetOrdinal("DisplayOrder");
                        int idxCreatedByUser = reader.GetOrdinal("CreatedByUser");

                        // <lang>
                        //   <zh-CN>直接返回已物化详情对象；可空字段保留 null，展示/错误处理由调用页完成。</zh-CN>
                        //   <en>Return the materialized detail object directly; nullable fields remain null and display/error handling stays with the caller page.</en>
                        // </lang>
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

            // <lang>
            //   <zh-CN>没有匹配消息时返回 null，不在数据层泄露原因或构造用户可见错误文案。</zh-CN>
            //   <en>Return null when no message matches, without leaking a cause or constructing user-visible error text in the data layer.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>空白用户名降级为旧占位值，只满足历史显示字段；认证身份和发帖权限不从该值推断。</zh-CN>
            //   <en>A blank user name falls back to the legacy placeholder only for the historical display field; authenticated identity and posting permission are not inferred from this value.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "unknown";
            }

            // <lang>
            //   <zh-CN>新增消息调用封装一次旧存储过程执行；线程 DisplayOrder 分配由数据库过程维护。</zh-CN>
            //   <en>The add-message call wraps one legacy stored-procedure execution; thread DisplayOrder allocation is maintained by the database procedure.</en>
            // </lang>
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("Portal_AddMessage", connection))
            {
                // <lang>
                //   <zh-CN>明确以存储过程模式执行，避免把过程名称作为文本 SQL。</zh-CN>
                //   <en>Execute explicitly in stored-procedure mode so the procedure name is not treated as text SQL.</en>
                // </lang>
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

                // <lang>
                //   <zh-CN>参数全部绑定后再打开连接；执行过程时不在代码层拼接标题或正文。</zh-CN>
                //   <en>Open the connection after all parameters are bound; title and body are not concatenated into SQL at the code layer.</en>
                // </lang>
                connection.Open();

                // <lang>
                //   <zh-CN>执行写入过程；数据库负责插入、父子路径维护和输出参数赋值。</zh-CN>
                //   <en>Execute the write procedure; the database owns insertion, parent/child path maintenance, and output parameter assignment.</en>
                // </lang>
                command.ExecuteNonQuery();

                // <lang>
                //   <zh-CN>读取输出参数；未返回标识时保持旧契约的 -1 回退，调用页再决定提示或重试策略。</zh-CN>
                //   <en>Read the output parameter; when no identifier is returned, keep the legacy -1 fallback and let the caller page decide messaging or retry policy.</en>
                // </lang>
                return itemIdParam.Value == DBNull.Value ? -1 : Convert.ToInt32(itemIdParam.Value);
            }
        }

        #endregion
    }
}
