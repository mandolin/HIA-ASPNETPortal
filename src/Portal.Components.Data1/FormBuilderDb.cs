using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>表单构建器提交数据访问实现（基于 PortalBizDbContext，遵循既有模块 Db 的 HasTable 守卫生式）。</zh-CN>
    ///   <en>Form Builder submission data-access implementation (built on PortalBizDbContext, following the HasTable guard pattern of existing module Db classes).</en>
    /// </lang>
    /// </summary>
    public sealed class FormBuilderDb : IFormBuilderDb
    {
        private const string TableName = "PortalBiz_FormSubmissions";
        private readonly PortalBizDbContext context;

        /// <summary><lang><zh-CN>通过依赖注入获取业务数据上下文。</zh-CN><en>Obtains the business data context via dependency injection.</en></lang></summary>
        /// <param name="context">
        /// <lang>
        ///   <zh-CN>业务数据上下文。</zh-CN>
        ///   <en>Business data context.</en>
        /// </lang>
        /// </param>
        public FormBuilderDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(TableName);
        }

        /// <inheritdoc />
        public long Submit(FormBuilderSubmission submission)
        {
            if (submission == null || string.IsNullOrWhiteSpace(submission.FormId))
            {
                return 0;
            }
            if (!IsSchemaAvailable())
            {
                return 0;
            }

            try
            {
                var rows = context.Database.SqlQuery<long>(
                    @"INSERT INTO [dbo].[PortalBiz_FormSubmissions]
                        ([ModuleId], [FormId], [SubmittedByUserId], [SubmittedBy], [SubmittedUtc], [Data])
                      OUTPUT INSERTED.[SubmissionId]
                      VALUES (@ModuleId, @FormId, @SubmittedByUserId, @SubmittedBy, SYSUTCDATETIME(), @Data);",
                    new SqlParameter("@ModuleId", submission.ModuleId),
                    new SqlParameter("@FormId", submission.FormId ?? string.Empty),
                    new SqlParameter("@SubmittedByUserId", submission.SubmittedByUserId),
                    new SqlParameter("@SubmittedBy", (submission.SubmittedBy ?? string.Empty).Length > 100
                        ? submission.SubmittedBy.Substring(0, 100)
                        : (submission.SubmittedBy ?? string.Empty)),
                    new SqlParameter("@Data", SqlDbType.NVarChar, -1) { Value = submission.Data ?? string.Empty })
                    .ToList();
                return rows.Count == 0 ? 0 : rows[0];
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private bool HasTable(string tableName)
        {
            try
            {
                string sql = string.Format(
                    CultureInfo.InvariantCulture,
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[{0}]', N'U') IS NULL THEN 0 ELSE 1 END",
                    tableName);
                return context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
