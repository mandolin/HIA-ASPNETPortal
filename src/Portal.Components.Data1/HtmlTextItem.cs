using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_HtmlText")]
    public class HtmlTextItem : IHtmlTextItem
    {
        #region IHtmlTextItem Members

        [Key]
        public int ModuleId { get; set; }

        public string DesktopHtml { get; set; }
        public string MobileSummary { get; set; }
        public string MobileDetails { get; set; }

        #endregion
    }
}