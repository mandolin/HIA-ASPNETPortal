using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>SQL Server 业务参考数据目录的只读实现。</zh-CN>
    ///   <en>Read-only SQL Server implementation of the business reference-data catalog.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该服务只读取已启用的低敏目录值。目录写入须经独立的权限、审计和停用兼容规则实现，P23.2 不开放写入口。</zh-CN>
    ///   <en>This service reads active low-sensitivity catalog values only. Catalog writes require separate permission, audit, and deactivation-compatibility rules; P23.2 exposes no write entry point.</en>
    /// </lang>
    /// </remarks>
    public sealed class ReferenceDataDb : IReferenceDataDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>受治理参考数据目录的固定表名，用于只读 schema 探测。</zh-CN>
        ///   <en>Fixed governed reference-data table name used for read-only schema probing.</en>
        /// </lang>
        /// </summary>
        private const string ReferenceDataTableName = "PortalBiz_ReferenceData";

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业业务基础数据上下文；本服务只通过它执行只读目录查询。</zh-CN>
        ///   <en>Enterprise business-foundation data context; this service uses it only for read-only catalog queries.</en>
        /// </lang>
        /// </summary>
        private readonly PortalBizDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>使用企业业务基础数据上下文初始化目录读取服务。</zh-CN>
        ///   <en>Initializes the catalog reader with the enterprise business-foundation data context.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l><zh-CN>企业业务基础数据上下文。</zh-CN><en>Enterprise business-foundation data context.</en></l>
        /// </param>
        public ReferenceDataDb(PortalBizDbContext context)
        {
            // <lang>
            //   <zh-CN>保留调用方注入的上下文实例，使 schema 探测与目录查询共享同一连接和事务边界。</zh-CN>
            //   <en>Keep the caller-injected context so schema probing and catalog querying share the same connection and transaction boundary.</en>
            // </lang>
            this.context = context;
        }

        /// <inheritdoc />
        public bool TryGetActiveItems(string referenceSetKey, out IList<ReferenceDataItem> items)
        {
            // <lang>
            //   <zh-CN>输出集合先初始化为空列表，确保任何早退或异常路径都不会泄露上一轮调用结果。</zh-CN>
            //   <en>The output collection starts as an empty list so any early-return or exception path cannot leak results from a previous call.</en>
            // </lang>
            items = new List<ReferenceDataItem>();
            // <lang>
            //   <zh-CN>集合键来自页面或业务服务输入，先按数据库列宽裁剪，作为后续参数化查询的唯一值。</zh-CN>
            //   <en>The set key comes from page or business-service input and is trimmed to the database column width before becoming the only value for the parameterized query.</en>
            // </lang>
            string normalizedReferenceSetKey = NormalizeKey(referenceSetKey, 80);
            // <lang>
            //   <zh-CN>空集合键或目录表不可用都视为“目录读取失败”，调用方据此决定是否使用受限兼容回退。</zh-CN>
            //   <en>An empty set key or unavailable catalog table is treated as a catalog-read failure, allowing callers to decide whether the restricted compatibility fallback applies.</en>
            // </lang>
            if (string.IsNullOrEmpty(normalizedReferenceSetKey) || !IsSchemaAvailable())
            {
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>目录查询只投影低敏字段并限制为已启用值，避免把停用目录项重新用于新业务事实。</zh-CN>
                //   <en>The catalog query projects only low-sensitivity fields and restricts results to active values so deactivated catalog items are not reused for new business facts.</en>
                // </lang>
                items = context.Database.SqlQuery<ReferenceDataItem>(
                    @"
SELECT
    [ReferenceDataId],
    [ReferenceSetKey],
    [ValueKey],
    [DisplayName],
    [Description],
    [SortOrder],
    [IsActive],
    [IsSystemSeed]
FROM [dbo].[PortalBiz_ReferenceData]
WHERE [ReferenceSetKey] = @ReferenceSetKey
  AND [IsActive] = 1
ORDER BY [SortOrder] ASC, [ValueKey] ASC;",
                    new SqlParameter("@ReferenceSetKey", normalizedReferenceSetKey)).ToList();
                // <lang>
                //   <zh-CN>能到达这里表示表存在且查询已完成；即使集合为空，也代表目录可读而不是需要回退。</zh-CN>
                //   <en>Reaching this point means the table exists and the query completed; even an empty set means the catalog is readable rather than requiring fallback.</en>
                // </lang>
                return true;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>读取异常转换为空结果和失败标志，避免目录问题中断协同页面，同时不吞入半成品列表。</zh-CN>
                //   <en>Read exceptions become an empty result and failure flag so catalog issues do not break collaboration pages and no partial list is retained.</en>
                // </lang>
                items = new List<ReferenceDataItem>();
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断受治理参考数据目录表是否已经部署并可由当前上下文读取。</zh-CN>
        ///   <en>Determines whether the governed reference-data catalog table is deployed and readable through the current context.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>目录表存在且探测查询成功时返回 <c>true</c>；缺表或探测失败时返回 <c>false</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when the catalog table exists and the probe query succeeds; returns <c>false</c> when the table is missing or probing fails.</en>
        /// </l>
        /// </returns>
        private bool IsSchemaAvailable()
        {
            try
            {
                // <lang>
                //   <zh-CN>表名来自内部常量而非用户输入，拼接 SQL 仅用于 SQL Server 的 OBJECT_ID schema 探测。</zh-CN>
                //   <en>The table name comes from an internal constant rather than user input, so string SQL is used only for SQL Server OBJECT_ID schema probing.</en>
                // </lang>
                string sql = string.Format(
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[{0}]', N'U') IS NULL THEN 0 ELSE 1 END",
                    ReferenceDataTableName);
                // <lang>
                //   <zh-CN>探测结果按单个整数读取，保持目录表不可用与空目录集合之间的语义区分。</zh-CN>
                //   <en>The probe result is read as one integer, preserving the semantic distinction between an unavailable catalog table and an empty catalog set.</en>
                // </lang>
                return context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>schema 探测失败时关闭数据库目录路径，由调用方使用明确的兼容回退或返回失败。</zh-CN>
                //   <en>When schema probing fails, close the database catalog path so callers can use the explicit compatibility fallback or report failure.</en>
                // </lang>
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将参考数据键规范化为数据库列允许的稳定比较值。</zh-CN>
        ///   <en>Normalizes a reference-data key into the stable comparison value allowed by the database column.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自调用方的候选键值。</zh-CN>
        ///   <en>Candidate key value provided by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="maximumLength">
        /// <l>
        ///   <zh-CN>数据库列允许的最大字符数。</zh-CN>
        ///   <en>Maximum character count allowed by the database column.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪空白和长度后的键；空输入返回空字符串。</zh-CN>
        ///   <en>The key after whitespace and length trimming; empty input returns an empty string.</en>
        /// </l>
        /// </returns>
        private static string NormalizeKey(string value, int maximumLength)
        {
            // <lang>
            //   <zh-CN>空引用按空字符串处理，避免目录读取路径把 null 与未部署 schema 混成异常。</zh-CN>
            //   <en>A null reference is treated as an empty string so the catalog-read path does not turn null into an exception confused with an undeployed schema.</en>
            // </lang>
            string normalized = (value ?? string.Empty).Trim();
            // <lang>
            //   <zh-CN>长度裁剪与数据库列宽保持一致，后续参数化查询不会因过长键值失败。</zh-CN>
            //   <en>Length trimming stays aligned with the database column width so the later parameterized query does not fail because of an overlong key.</en>
            // </lang>
            return normalized.Length <= maximumLength ? normalized : normalized.Substring(0, maximumLength);
        }
    }
}
