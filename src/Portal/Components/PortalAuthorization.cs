using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 门户后台授权小工具。本阶段只收拢旧 Admin 检查，不引入新的权限框架。
    /// </summary>
    public static class PortalAuthorization
    {
        private const string EditAccessDeniedUrl = "~/Admin/EditAccessDenied.aspx";

        /// <summary>
        /// 判断当前用户是否为旧门户管理员角色。
        /// </summary>
        public static bool IsAdmin()
        {
            return PortalSecurity.IsInRole(PortalRoleNames.Administrators);
        }

        /// <summary>
        /// 要求当前用户为管理员；不满足时沿用旧后台页面的拒绝访问跳转。
        /// </summary>
        public static void RequireAdmin()
        {
            if (!IsAdmin())
            {
                HttpContext.Current.Response.Redirect(EditAccessDeniedUrl);
            }
        }
    }
}
