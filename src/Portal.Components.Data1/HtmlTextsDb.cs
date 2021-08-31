using System.Linq;

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

        public IHtmlTextItem GetHtmlText(int moduleId)
        {
            return _context.HtmlTexts.Single(i => i.ModuleId == moduleId);
        }


        public void UpdateHtmlText(int moduleId, string desktopHtml, string mobileSummary, string mobileDetails)
        {
            HtmlTextItem item = _context.HtmlTexts.SingleOrDefault(i => i.ModuleId == moduleId);

            if (item != default(HtmlTextItem))
            {
                item.DesktopHtml = desktopHtml;
                item.MobileSummary = mobileSummary;
                item.MobileDetails = mobileDetails;
            }
            else
            {
                var newItem = new HtmlTextItem
                                  {
                                      ModuleId = moduleId,
                                      DesktopHtml = desktopHtml,
                                      MobileDetails = mobileDetails,
                                      MobileSummary = mobileSummary
                                  };
                _context.HtmlTexts.Add(newItem);
            }
            _context.SaveChanges();
        }

        #endregion
    }
}