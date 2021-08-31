using System;

namespace ASPNET.StarterKit.Portal
{
    public partial class CDefault : PortalPage<CDefault>
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //if (Request.Browser["IsMobileDevice"] == "true" ) {

            //    Response.Redirect("MobileDefault.aspx");
            //}
            //else {

            //    Response.Redirect("DesktopDefault.aspx");
            //}
            Response.Redirect("DesktopDefault.aspx");
        }
    }
}