using System;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>通过 EF 上下文读写旧 HTML 模块文本数据。</zh-CN>
    ///   <en>Reads and writes legacy HTML-module text data through the EF context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>HTML 模块是普通展示编码规则的例外；调用页负责确保内容来自受信任管理员，并在入库前按旧约定编码。</zh-CN>
    ///   <en>The HTML module is an exception to ordinary display-encoding rules; caller pages must ensure content comes from a trusted administrator and is encoded before persistence according to the legacy convention.</en>
    /// </lang>
    /// </remarks>
    public class HtmlTextsDb : IHtmlTextsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户业务 EF 上下文，封装旧 HTML 文本表映射。</zh-CN>
        ///   <en>Portal business EF context that wraps the legacy HTML text table mapping.</en>
        /// </lang>
        /// </summary>
        private readonly PortalDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化 HTML 文本数据访问对象。</zh-CN>
        ///   <en>Initializes the HTML-text data-access object.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l><zh-CN>由 Unity 注入的门户 EF 上下文。</zh-CN><en>Portal EF context injected by Unity.</en></l></param>
        public HtmlTextsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IHtmlTextsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定模块的 HTML 文本；尚未创建记录时返回 <c>null</c>，以便编辑页提供受控的首次保存流程。</zh-CN>
        ///   <en>Gets HTML text for a module. Returns <c>null</c> before a record exists so the editor can provide a controlled first-save flow.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块标识符。</zh-CN>
        ///   <en>Module identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>模块 HTML 文本；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Module HTML text, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        public IHtmlTextItem GetHtmlText(int moduleId)
        {
            // <lang>
            //   <zh-CN>HTML 模块允许首次编辑时创建记录，缺失记录属于正常状态；展示控件遇到空值时不输出内容。</zh-CN>
            //   <en>HTML modules create their record during the first edit, so a missing record is an expected state; display controls emit no content for null.</en>
            // </lang>
            return _context.HtmlTexts.SingleOrDefault(i => i.ModuleId == moduleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新或创建指定模块的 HTML 文本记录。</zh-CN>
        ///   <en>Updates or creates the HTML-text record for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法不净化 HTML，也不判断操作者权限；编辑页必须先完成模块编辑授权、原始 HTML 入口控制和低敏校验提示。</zh-CN>
        ///   <en>This method does not sanitize HTML and does not authorize operators; the editor page must first complete module-edit authorization, raw-HTML entry control, and low-sensitivity validation messaging.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId"><l><zh-CN>HTML 模块实例 ID。</zh-CN><en>HTML module instance ID.</en></l></param>
        /// <param name="desktopHtml"><l><zh-CN>已按旧约定编码存储的桌面端 HTML 内容。</zh-CN><en>Desktop HTML content stored after encoding according to the legacy convention.</en></l></param>
        /// <param name="mobileSummary"><l><zh-CN>历史移动端摘要内容。</zh-CN><en>Legacy mobile summary content.</en></l></param>
        /// <param name="mobileDetails"><l><zh-CN>历史移动端详情内容。</zh-CN><en>Legacy mobile detail content.</en></l></param>
        public void UpdateHtmlText(int moduleId, string desktopHtml, string mobileSummary, string mobileDetails)
        {
            // <lang>
            //   <zh-CN>旧 HTML 模块以 ModuleId 作为唯一定位；首次保存时可能还没有对应记录。</zh-CN>
            //   <en>The legacy HTML module is uniquely located by ModuleId; its first save may occur before a record exists.</en>
            // </lang>
            var item = _context.HtmlTexts.SingleOrDefault(i => i.ModuleId == moduleId);

            if (item != null)
            {
                // <lang>
                //   <zh-CN>更新现有记录时只替换三个内容字段，模块归属保持不变。</zh-CN>
                //   <en>When updating an existing record, replace only the three content fields and keep module ownership unchanged.</en>
                // </lang>
                item.DesktopHtml = desktopHtml;
                item.MobileSummary = mobileSummary;
                item.MobileDetails = mobileDetails;
            }
            else
            {
                // <lang>
                //   <zh-CN>首次保存创建记录；调用页已保证 moduleId 来自当前可编辑模块。</zh-CN>
                //   <en>The first save creates the record; the caller page has already ensured that moduleId comes from the current editable module.</en>
                // </lang>
                var newItem = new HtmlTextItem
                {
                    ModuleId = moduleId,
                    DesktopHtml = desktopHtml,
                    MobileSummary = mobileSummary,
                    MobileDetails = mobileDetails
                };
                _context.HtmlTexts.Add(newItem);
            }

            _context.SaveChanges();
        }

        #endregion
    }
}
