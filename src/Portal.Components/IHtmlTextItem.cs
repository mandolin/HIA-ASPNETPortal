namespace ASPNET.StarterKit.Portal
{
    public interface IHtmlTextItem
    {
        int ModuleId { get; set; }
        string DesktopHtml { get; set; }
        string MobileSummary { get; set; }
        string MobileDetails { get; set; }
    }
}