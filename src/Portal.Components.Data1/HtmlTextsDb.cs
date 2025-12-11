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
        /// 获取指定模块ID的HTML文本。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>HTML文本对象。</returns>
        public IHtmlTextItem GetHtmlText(int moduleId)
        {
            // 使用 LINQ 查询获取指定模块ID的HTML文本对象
            return _context.HtmlTexts.Single(i => i.ModuleId == moduleId);
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