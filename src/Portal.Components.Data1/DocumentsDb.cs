using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 Entity Framework 的旧文档模块数据访问实现。</zh-CN>
    ///   <en>Entity Framework implementation of legacy document-module data access.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>读取单项时不存在记录会返回 <c>null</c>，由页面将编辑请求收敛到拒绝页或将下载请求收敛到中性未找到响应。写入方法不验证 URL、上传类型或权限，这些边界由调用页面和 <c>PortalDocumentPolicy</c> 负责。</zh-CN>
    ///   <en>Reads of a missing single item return <c>null</c>, allowing pages to converge edit requests to an access-denied page and download requests to a neutral not-found response. Write operations do not validate URLs, upload types, or permission; those boundaries belong to calling pages and <c>PortalDocumentPolicy</c>.</en>
    /// </lang>
    /// </remarks>
    public class DocumentsDb : IDocumentsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户业务 EF 上下文，承载旧文档模块表的查询和写入跟踪。</zh-CN>
        ///   <en>Portal business EF context that carries query and write tracking for the legacy document-module table.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该上下文由 Unity 注入并随数据访问对象生命周期使用；本类不持有连接串、凭据或上传文件句柄。</zh-CN>
        ///   <en>The context is injected by Unity and used for the lifetime of this data-access object; this class does not hold connection strings, credentials, or uploaded-file handles.</en>
        /// </lang>
        /// </remarks>
        private readonly PortalDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化文档实体上下文。</zh-CN>
        ///   <en>Initializes the document entity context.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>门户业务数据库上下文。</zh-CN>
        ///   <en>Portal business database context.</en>
        /// </l>
        /// </param>
        public DocumentsDb(PortalDbContext context)
        {
            // <lang>
            //   <zh-CN>保存调用方提供的 EF 上下文引用；构造器不主动访问数据库，避免 DI 创建阶段产生隐式 I/O。</zh-CN>
            //   <en>Store the caller-provided EF context reference; the constructor does not query the database, avoiding implicit I/O during DI creation.</en>
            // </lang>
            _context = context;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块下的全部文档记录。</zh-CN>
        ///   <en>Reads all document records for the specified module.</en>
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
        ///   <zh-CN>已物化的模块文档集合。</zh-CN>
        ///   <en>Materialized collection of module documents.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IDocumentItem> GetDocuments(int moduleId)
        {
            // <lang>
            //   <zh-CN>只按模块实例过滤并立即物化；排序、空列表展示和下载入口策略由页面/控件层继续处理。</zh-CN>
            //   <en>Filter only by module instance and materialize immediately; ordering, empty-list display, and download-link policy remain with the page/control layer.</en>
            // </lang>
            return _context.Documents.Where(item => item.ModuleId == moduleId).ToList<IDocumentItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按文档项目标识读取记录。</zh-CN>
        ///   <en>Reads a record by document-item identifier.</en>
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
        public IDocumentItem GetSingleDocument(int itemId)
        {
            // <lang>
            //   <zh-CN>编辑页会把请求中的 ItemId 传入这里；未命中返回空值，重复记录仍作为完整性故障暴露。</zh-CN>
            //   <en>Editor pages pass request item identifiers here; misses return null while duplicate records still surface as integrity failures.</en>
            // </lang>
            return _context.Documents.SingleOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按文档项目标识读取历史数据库内容。</zh-CN>
        ///   <en>Reads legacy database content by document-item identifier.</en>
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
        public IDocumentItemDetails GetDocumentContent(int itemId)
        {
            // <lang>
            //   <zh-CN>详情读取复用同一实体集，但返回包含二进制内容的接口；调用方必须避免把内容展开到列表、日志或诊断输出。</zh-CN>
            //   <en>The detail read uses the same entity set but returns the interface that includes binary content; callers must avoid expanding that content into lists, logs, or diagnostics.</en>
            // </lang>
            return _context.Documents.SingleOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定文档记录；调用方必须先完成模块归属和编辑权限校验。</zh-CN>
        ///   <en>Deletes a document record; the caller must validate module ownership and edit permission first.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        public void DeleteDocument(int itemId)
        {
            // <lang>
            //   <zh-CN>待删除实体必须唯一存在；调用方已经验证模块归属，因此这里不再用宽松空值分支吞掉损坏状态。</zh-CN>
            //   <en>The entity to delete must exist uniquely; caller-side module ownership has already been validated, so this layer does not swallow damaged state with a loose null branch.</en>
            // </lang>
            DocumentItem item = _context.Documents.Single(record => record.ItemId == itemId);

            // <lang>
            //   <zh-CN>从 EF 跟踪集中标记删除；真实文件系统清理不在该旧表数据访问方法内完成。</zh-CN>
            //   <en>Mark the entity for deletion in the EF tracking set; real filesystem cleanup is not performed inside this legacy-table data-access method.</en>
            // </lang>
            _context.Documents.Remove(item);

            // <lang>
            //   <zh-CN>提交单条文档表删除；权限审计和安全回跳由调用页在本方法外处理。</zh-CN>
            //   <en>Commit the single document-table deletion; permission auditing and safe return navigation are handled by the caller page outside this method.</en>
            // </lang>
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建或更新文档记录，并保留旧表的数据库内容字段以兼容已有 schema。</zh-CN>
        ///   <en>Creates or updates a document record while retaining legacy database-content fields for schema compatibility.</en>
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
        ///   <zh-CN>写入用户名；空值规范为 <c>unknown</c>。</zh-CN>
        ///   <en>Writing user name; blank becomes <c>unknown</c>.</en>
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
        ///   <zh-CN>调用方已验证的浏览地址或上传路径。</zh-CN>
        ///   <en>Browse address or upload path validated by the caller.</en>
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
        ///   <zh-CN>历史数据库二进制内容。</zh-CN>
        ///   <en>Legacy database binary content.</en>
        /// </l>
        /// </param>
        /// <param name="size">
        /// <l>
        ///   <zh-CN>历史数据库内容大小。</zh-CN>
        ///   <en>Legacy database content size.</en>
        /// </l>
        /// </param>
        /// <param name="contentType">
        /// <l>
        ///   <zh-CN>历史 MIME 类型提示。</zh-CN>
        ///   <en>Legacy MIME-type hint.</en>
        /// </l>
        /// </param>
        public void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                                   byte[] content, int size, string contentType)
        {
            // <lang>
            //   <zh-CN>用户名只做旧表必需的空值回退，不做身份重判；真实身份和权限已经由页面层建立。</zh-CN>
            //   <en>The user name receives only the legacy-table required blank fallback and is not re-authenticated here; real identity and permission are established by the page layer.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // <lang>
            //   <zh-CN>本地实体变量在新增和更新分支之间共享，生命周期只覆盖本次保存批次。</zh-CN>
            //   <en>The local entity variable is shared by the create and update branches and lives only for this save batch.</en>
            // </lang>
            DocumentItem item;
            if (itemId == 0)
            {
                // <lang>
                //   <zh-CN>零标识保持旧约定表示新增；新实体先纳入 EF 集合，随后统一赋值，避免新增/更新字段漂移。</zh-CN>
                //   <en>The zero identifier keeps the legacy convention for creation; add the new entity to the EF set first, then use the common assignment block to avoid create/update field drift.</en>
                // </lang>
                item = new DocumentItem();
                _context.Documents.Add(item);
            }
            else
            {
                // <lang>
                //   <zh-CN>非零标识表示更新既有记录；缺失或重复记录应作为完整性/归属前置校验失败暴露。</zh-CN>
                //   <en>A non-zero identifier updates an existing record; missing or duplicate records should surface as integrity or prevalidated-ownership failures.</en>
                // </lang>
                item = _context.Documents.Single(record => record.ItemId == itemId);
            }

            // <lang>
            //   <zh-CN>以下赋值是旧文档表的完整持久化投影；策略校验后的 URL/上传路径、分类和历史内容占位由调用方按契约传入。</zh-CN>
            //   <en>The assignments below are the full persistence projection for the legacy document table; the caller supplies policy-validated URL/upload path, category, and legacy-content placeholders according to contract.</en>
            // </lang>
            item.ModuleId = moduleId;
            item.CreatedByUser = userName;
            item.CreatedDate = DateTime.Now;
            item.FileFriendlyName = name;
            item.FileNameUrl = url;
            item.Category = category;
            item.Content = content;
            item.ContentSize = size;
            item.ContentType = contentType;

            // <lang>
            //   <zh-CN>提交当前新增或更新批次；本方法不附带文件移动、MIME 复核、审计或浏览器可见输出。</zh-CN>
            //   <en>Commit the current create or update batch; this method does not perform file moves, MIME re-checks, auditing, or browser-visible output.</en>
            // </lang>
            _context.SaveChanges();
        }
    }
}
