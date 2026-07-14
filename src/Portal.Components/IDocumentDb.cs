using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：文档模块记录及历史二进制内容的数据访问契约。
    ///
    /// English: Data-access contract for document-module records and legacy binary content.
    /// </summary>
    public interface IDocumentsDb
    {
        /// <summary>
        /// 中文：读取一个模块下的全部文档记录。
        /// English: Reads all document records for one module.
        /// </summary>
        /// <param name="moduleId">中文：模块实例标识。English: Module-instance identifier.</param>
        /// <returns>中文：模块记录集合。English: Collection of module records.</returns>
        IEnumerable<IDocumentItem> GetDocuments(int moduleId);

        /// <summary>
        /// 中文：按项目标识读取一条文档记录。
        /// English: Reads one document record by item identifier.
        /// </summary>
        /// <param name="itemId">中文：文档项目标识。English: Document-item identifier.</param>
        /// <returns>中文：匹配记录；不存在时为 <c>null</c>。English: Matching record, or <c>null</c> when absent.</returns>
        IDocumentItem GetSingleDocument(int itemId);

        /// <summary>
        /// 中文：按项目标识读取历史数据库内容。
        /// English: Reads legacy database content by item identifier.
        /// </summary>
        /// <param name="itemId">中文：文档项目标识。English: Document-item identifier.</param>
        /// <returns>中文：匹配详情；不存在时为 <c>null</c>。English: Matching detail, or <c>null</c> when absent.</returns>
        IDocumentItemDetails GetDocumentContent(int itemId);

        /// <summary>
        /// 中文：删除已通过调用方归属校验的文档记录。
        /// English: Deletes a document record whose ownership was validated by the caller.
        /// </summary>
        /// <param name="itemId">中文：文档项目标识。English: Document-item identifier.</param>
        void DeleteDocument(int itemId);

        /// <summary>
        /// 中文：创建或更新文档记录；调用方负责授权、URL/上传策略和内容边界。
        /// English: Creates or updates a document record; the caller is responsible for authorization, URL/upload policy, and content boundaries.
        /// </summary>
        /// <param name="moduleId">中文：所属模块实例标识。English: Owning module-instance identifier.</param>
        /// <param name="itemId">中文：已有项目标识；零表示新建。English: Existing item identifier; zero creates a new record.</param>
        /// <param name="userName">中文：写入用户名。English: Writing user name.</param>
        /// <param name="name">中文：显示名称。English: Display name.</param>
        /// <param name="url">中文：已验证浏览地址或服务器上传相对路径。English: Validated browse address or server-upload relative path.</param>
        /// <param name="category">中文：业务分类。English: Business category.</param>
        /// <param name="content">中文：历史数据库内容；当前写入路径应为空数组。English: Legacy database content; current write paths should use an empty array.</param>
        /// <param name="size">中文：历史数据库内容大小；当前写入路径应为零。English: Legacy database content size; current write paths should use zero.</param>
        /// <param name="contentType">中文：历史 MIME 类型提示；当前写入路径应为空。English: Legacy MIME-type hint; current write paths should use empty text.</param>
        void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                            byte[] content, int size, string contentType);
    }
}
