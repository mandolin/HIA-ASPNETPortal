using System;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class HtmlTextsDb : IHtmlTextsDb
    {
        private readonly PortalDbContext _context;

        public HtmlTextsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IHtmlTextsDb Members

        /// <summary>
        /// 中文：获取指定模块的 HTML 文本；尚未创建记录时返回 <c>null</c>，以便编辑页提供受控的首次保存流程。
        ///
        /// English: Gets HTML text for a module. Returns <c>null</c> before a record exists so the editor can provide a controlled first-save flow.
        /// </summary>
        /// <param name="moduleId">中文：模块标识符。English: Module identifier.</param>
        /// <returns>中文：模块 HTML 文本；不存在时为 <c>null</c>。English: Module HTML text, or <c>null</c> when absent.</returns>
        public IHtmlTextItem GetHtmlText(int moduleId)
        {
            // 中文：HTML 模块允许首次编辑时创建记录，缺失记录属于正常状态。
            // English: HTML modules create their record during the first edit, so a missing record is an expected state.
            return _context.HtmlTexts.SingleOrDefault(i => i.ModuleId == moduleId);
        }

        /// <summary>
        /// 更新指定模块ID的HTML文本。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <param name="desktopHtml">桌面版HTML内容。</param>
        /// <param name="mobileSummary">移动端摘要内容。</param>
        /// <param name="mobileDetails">移动端详细内容。</param>
        public void UpdateHtmlText(int moduleId, string desktopHtml, string mobileSummary, string mobileDetails)
        {
            // 使用 SingleOrDefault 方法获取指定模块ID的HTML文本对象，如果不存在则返回默认值
            var item = _context.HtmlTexts.SingleOrDefault(i => i.ModuleId == moduleId);

            // 检查是否找到了相应的HTML文本对象
            if (item != null)
            {
                // 更新现有记录
                item.DesktopHtml = desktopHtml;
                item.MobileSummary = mobileSummary;
                item.MobileDetails = mobileDetails;
            }
            else
            {
                // 如果没有找到记录，则创建新的记录
                var newItem = new HtmlTextItem
                {
                    ModuleId = moduleId,
                    DesktopHtml = desktopHtml,
                    MobileSummary = mobileSummary,
                    MobileDetails = mobileDetails
                };
                _context.HtmlTexts.Add(newItem);
            }

            // 保存更改到数据库
            _context.SaveChanges();
        }

        #endregion
    }
}
