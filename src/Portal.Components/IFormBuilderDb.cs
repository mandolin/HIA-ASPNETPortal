using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>表单构建器提交数据访问契约。</zh-CN>
    ///   <en>Data-access contract for Form Builder submissions.</en>
    /// </lang>
    /// </summary>
    public interface IFormBuilderDb
    {
        /// <summary><lang><zh-CN>提交表结构是否就绪。</zh-CN><en>Whether the submission table schema is ready.</en></lang></summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化一条表单提交，返回新提交标识；结构缺失或入参非法时返回 0。</zh-CN>
        ///   <en>Persists one form submission and returns the new submission id; returns 0 when the schema is missing or the input is invalid.</en>
        /// </lang>
        /// </summary>
        /// <param name="submission">
        /// <lang>
        ///   <zh-CN>待持久化的提交数据。</zh-CN>
        ///   <en>Submission data to persist.</en>
        /// </lang>
        /// </param>
        long Submit(FormBuilderSubmission submission);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>一条表单提交的持久化载体。</zh-CN>
    ///   <en>Persistence carrier for one form submission.</en>
    /// </lang>
    /// </summary>
    public sealed class FormBuilderSubmission
    {
        /// <summary><lang><zh-CN>来源模块实例标识。</zh-CN><en>Owning module instance identifier.</en></lang></summary>
        public int ModuleId { get; set; }

        /// <summary><lang><zh-CN>表单定义标识（取自 form.json）。</zh-CN><en>Form definition identifier (from form.json).</en></lang></summary>
        public string FormId { get; set; }

        /// <summary><lang><zh-CN>提交者登录名。</zh-CN><en>Submitter login name.</en></lang></summary>
        public string SubmittedBy { get; set; }

        /// <summary><lang><zh-CN>提交者用户标识；未解析为 0。</zh-CN><en>Submitter user id; 0 when unresolved.</en></lang></summary>
        public int SubmittedByUserId { get; set; }

        /// <summary><lang><zh-CN>字段值 JSON（键为字段名，值为字符串）。</zh-CN><en>Field-value JSON (keys are field names, values are strings).</en></lang></summary>
        public string Data { get; set; }
    }
}
