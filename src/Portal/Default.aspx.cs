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

            /*  #todo
                1.改为跳转到统一首页（Index.aspx）
                2.DesktopDefault.aspx作为体系核心管理首页，手动输入地址进入；或者在Index.aspx中，如果有权限则显示其链接
                3.将DesktopDefault.aspx改为CoreDefault.aspx(核心管理界面首页。另：业务系统首页可直接用Index.aspx)

             */
            Response.Redirect("DesktopDefault.aspx");
        }
    }
}