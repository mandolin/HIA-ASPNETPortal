using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户后台授权小工具，集中旧 Admin 角色检查与拒绝访问跳转。
    ///
    /// English: Portal administration authorization helper that centralizes legacy Admin-role checks and access-denied redirects.
    /// </summary>
    /// <remarks>
    /// 中文：当前只以 <see cref="PortalRoleNames.Administrators"/> 判定管理员，不引入新的细粒度权限框架。
    /// 后续可将此入口迁移到独立权限项，但应保持现有后台页面的拒绝访问路径兼容。
    ///
    /// English: The current implementation recognizes administrators only through <see cref="PortalRoleNames.Administrators"/>
    /// and does not introduce a new fine-grained permission framework. A later migration may use distinct permissions,
    /// but must preserve the existing administration access-denied path.
    /// </remarks>
    public static class PortalAuthorization
    {
        private const string EditAccessDeniedUrl = "~/Admin/EditAccessDenied.aspx";

        /// <summary>
        /// 中文：判断当前请求身份是否具有旧门户管理员角色。
        ///
        /// English: Determines whether the current request identity has the legacy Portal administrator role.
        /// </summary>
        /// <returns>中文：当前身份属于 <c>Admins</c> 时为 <c>true</c>。English: <c>true</c> when the current identity belongs to <c>Admins</c>.</returns>
        public static bool IsAdmin()
        {
            return PortalSecurity.IsInRole(PortalRoleNames.Administrators);
        }

        /// <summary>
        /// 中文：要求当前请求身份为管理员；不满足时跳转到既有后台拒绝访问页。
        ///
        /// English: Requires the current request identity to be an administrator; otherwise redirects to the existing administration access-denied page.
        /// </summary>
        /// <remarks>
        /// 中文：此方法只能在存在当前 HTTP 响应的页面请求中使用。重定向后调用方不应继续执行敏感写操作。
        ///
        /// English: This method can be used only during a page request with a current HTTP response. Callers must not continue sensitive writes after the redirect.
        /// </remarks>
        public static void RequireAdmin()
        {
            if (!IsAdmin())
            {
                HttpContext.Current.Response.Redirect(EditAccessDeniedUrl);
            }
        }
    }
}
