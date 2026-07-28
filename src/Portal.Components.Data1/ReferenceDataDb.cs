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
        private const string ReferenceDataTableName = "PortalBiz_ReferenceData";
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
            this.context = context;
        }

        /// <inheritdoc />
        public bool TryGetActiveItems(string referenceSetKey, out IList<ReferenceDataItem> items)
        {
            items = new List<ReferenceDataItem>();
            string normalizedReferenceSetKey = NormalizeKey(referenceSetKey, 80);
            if (string.IsNullOrEmpty(normalizedReferenceSetKey) || !IsSchemaAvailable())
            {
                return false;
            }

            try
            {
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
                return true;
            }
            catch (Exception)
            {
                items = new List<ReferenceDataItem>();
                return false;
            }
        }

        private bool IsSchemaAvailable()
        {
            try
            {
                string sql = string.Format(
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[{0}]', N'U') IS NULL THEN 0 ELSE 1 END",
                    ReferenceDataTableName);
                return context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string NormalizeKey(string value, int maximumLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maximumLength ? normalized : normalized.Substring(0, maximumLength);
        }
    }
}
