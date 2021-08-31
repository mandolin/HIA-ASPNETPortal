namespace ASPNET.StarterKit.Portal
{
    public interface IHtmlTextsDb
    {
        IHtmlTextItem GetHtmlText(int moduleId);
        void UpdateHtmlText(int moduleId, string desktopHtml, string mobileSummary, string mobileDetails);
    }
}