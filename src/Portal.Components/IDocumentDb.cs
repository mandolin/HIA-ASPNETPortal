using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>文档模块记录及历史二进制内容的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for document-module records and legacy binary content.</en>
    /// </lang>
    /// </summary>
    public interface IDocumentsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个模块下的全部文档记录。</zh-CN>
        ///   <en>Reads all document records for one module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>模块记录集合。</zh-CN>
        ///   <en>Collection of module records.</en>
        /// </l>
        /// </returns>
        IEnumerable<IDocumentItem> GetDocuments(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按项目标识读取一条文档记录。</zh-CN>
        ///   <en>Reads one document record by item identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配记录；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching record, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        IDocumentItem GetSingleDocument(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按项目标识读取历史数据库内容。</zh-CN>
        ///   <en>Reads legacy database content by item identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配详情；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching detail, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        IDocumentItemDetails GetDocumentContent(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已通过调用方归属校验的文档记录。</zh-CN>
        ///   <en>Deletes a document record whose ownership was validated by the caller.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        void DeleteDocument(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建或更新文档记录；调用方负责授权、URL/上传策略和内容边界。</zh-CN>
        ///   <en>Creates or updates a document record; the caller is responsible for authorization, URL/upload policy, and content boundaries.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>所属模块实例标识。</zh-CN>
        ///   <en>Owning module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>已有项目标识；零表示新建。</zh-CN>
        ///   <en>Existing item identifier; zero creates a new record.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>写入用户名。</zh-CN>
        ///   <en>Writing user name.</en>
        /// </l>
        /// </param>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>显示名称。</zh-CN>
        ///   <en>Display name.</en>
        /// </l>
        /// </param>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>已验证浏览地址或服务器上传相对路径。</zh-CN>
        ///   <en>Validated browse address or server-upload relative path.</en>
        /// </l>
        /// </param>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>业务分类。</zh-CN>
        ///   <en>Business category.</en>
        /// </l>
        /// </param>
        /// <param name="content">
        /// <l>
        ///   <zh-CN>历史数据库内容；当前写入路径应为空数组。</zh-CN>
        ///   <en>Legacy database content; current write paths should use an empty array.</en>
        /// </l>
        /// </param>
        /// <param name="size">
        /// <l>
        ///   <zh-CN>历史数据库内容大小；当前写入路径应为零。</zh-CN>
        ///   <en>Legacy database content size; current write paths should use zero.</en>
        /// </l>
        /// </param>
        /// <param name="contentType">
        /// <l>
        ///   <zh-CN>历史 MIME 类型提示；当前写入路径应为空。</zh-CN>
        ///   <en>Legacy MIME-type hint; current write paths should use empty text.</en>
        /// </l>
        /// </param>
        void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                            byte[] content, int size, string contentType);
    }
}
