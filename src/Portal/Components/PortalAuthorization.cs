using System;
using System.Linq;
using System.Web;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户后台授权小工具，集中旧 Admin 角色检查与拒绝访问跳转。
    ///
    /// English: Portal administration authorization helper that centralizes legacy Admin-role checks and access-denied redirects.
    /// </summary>
    /// <remarks>
    /// 中文：P5.3 起，新增权限 key facade；<see cref="PortalRoleNames.Administrators"/> 在过渡期自动拥有全部
    /// 已定义权限，保持旧后台不被权限模型引入破坏。
    ///
    /// English: Starting with P5.3, this helper exposes a permission-key facade. During the transition,
    /// <see cref="PortalRoleNames.Administrators"/> automatically owns every defined permission so legacy administration
    /// paths are not broken by the new model.
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
        /// 中文：判断当前请求身份是否具有指定权限。
        ///
        /// English: Determines whether the current request identity has the specified permission.
        /// </summary>
        /// <param name="permissionKey">中文：稳定权限键名。English: Stable permission key.</param>
        /// <returns>中文：拥有权限时为 <c>true</c>。English: <c>true</c> when the permission is granted.</returns>
        /// <remarks>
        /// 中文：未知权限键一律拒绝并记录诊断。`Admins` 作为过渡兼容角色拥有所有已定义权限；其它角色从
        /// <c>PortalCfg_RolePermissions</c> 读取映射。权限表缺失时，非管理员不会获得额外权限。
        ///
        /// English: Unknown permission keys are always denied and logged. <c>Admins</c> acts as a transition role with
        /// every defined permission; other roles read mappings from <c>PortalCfg_RolePermissions</c>. When the table is
        /// missing, non-admin users do not gain additional permissions.
        /// </remarks>
        public static bool HasPermission(string permissionKey)
        {
            HttpContext context = HttpContext.Current;
            string normalizedKey;
            if (!TryNormalizePermissionKey(permissionKey, context, out normalizedKey))
            {
                return false;
            }

            if (IsAdmin())
            {
                return true;
            }

            string userName = GetCurrentUserName(context);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            try
            {
                IRolesDb rolesDb = ResolveRolesDb();
                if (rolesDb == null)
                {
                    PortalDiagnostics.Warn(
                        "Authorization.PermissionLookup",
                        "Permission lookup skipped because IRolesDb is unavailable. PermissionKey=" + normalizedKey,
                        context);
                    return false;
                }

                return rolesDb
                    .GetPermissionKeysByUserName(userName)
                    .Any(key => string.Equals(key, normalizedKey, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "Authorization.PermissionLookup",
                    "Permission lookup failed. PermissionKey=" + normalizedKey,
                    exception,
                    context);
                return false;
            }
        }

        /// <summary>
        /// 中文：确认当前请求为管理员；未授权时安全跳转到既有拒绝访问页，并返回 <c>false</c>。
        ///
        /// English: Confirms that the current request is administrative; safely redirects unauthorized requests to the
        /// existing access-denied page and returns <c>false</c>.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文。English: Current HTTP context.</param>
        /// <returns>中文：当前请求可继续执行后台逻辑时为 <c>true</c>。English: <c>true</c> when the current request may continue administration logic.</returns>
        /// <remarks>
        /// 中文：调用方应采用 <c>if (!EnsureAdmin(Context)) return;</c>，避免拒绝重定向后继续执行写入。
        ///
        /// English: Callers should use <c>if (!EnsureAdmin(Context)) return;</c> to prevent writes from continuing
        /// after an access-denied redirect.
        /// </remarks>
        public static bool EnsureAdmin(HttpContext context)
        {
            if (IsAdmin())
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(context ?? HttpContext.Current);
            return false;
        }

        /// <summary>
        /// 中文：确认当前请求具有指定权限；未授权时记录诊断并跳转到既有拒绝访问页。
        ///
        /// English: Confirms that the current request has the specified permission; unauthorized requests are logged
        /// and redirected to the existing access-denied page.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文。English: Current HTTP context.</param>
        /// <param name="permissionKey">中文：稳定权限键名。English: Stable permission key.</param>
        /// <returns>中文：当前请求可继续执行敏感逻辑时为 <c>true</c>。English: <c>true</c> when the request may continue sensitive logic.</returns>
        public static bool EnsurePermission(HttpContext context, string permissionKey)
        {
            context = context ?? HttpContext.Current;
            string normalizedKey;
            if (!TryNormalizePermissionKey(permissionKey, context, out normalizedKey))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(context);
                return false;
            }

            if (HasPermission(normalizedKey))
            {
                return true;
            }

            PortalDiagnostics.Warn(
                "Authorization.PermissionDenied",
                "Permission denied. PermissionKey=" + normalizedKey,
                context);
            PortalNavigationPolicy.RedirectToEditAccessDenied(context);
            return false;
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

        /// <summary>
        /// 中文：要求当前请求具有指定权限；未授权时跳转到既有后台拒绝访问页。
        ///
        /// English: Requires the current request to have the specified permission; otherwise redirects to the existing
        /// administration access-denied page.
        /// </summary>
        /// <param name="permissionKey">中文：稳定权限键名。English: Stable permission key.</param>
        public static void RequirePermission(string permissionKey)
        {
            HttpContext context = HttpContext.Current;
            if (!EnsurePermission(context, permissionKey) && context != null)
            {
                // 中文：Require* 兼容旧 Response.Redirect 默认中止语义，避免调用方漏写 return 后继续执行敏感逻辑。
                // English: Require* keeps the legacy aborting redirect semantics so missed returns cannot continue sensitive logic.
                context.Response.Redirect(EditAccessDeniedUrl, true);
            }
        }

        private static bool TryNormalizePermissionKey(string permissionKey, HttpContext context, out string normalizedKey)
        {
            normalizedKey = string.Empty;
            try
            {
                normalizedKey = PortalPermissionRegistry.NormalizeDefinedKey(permissionKey);
                return true;
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "Authorization.PermissionKey",
                    "Undefined or invalid permission key requested.",
                    exception,
                    context);
                return false;
            }
        }

        private static string GetCurrentUserName(HttpContext context)
        {
            if (context == null || context.User == null || context.User.Identity == null ||
                !context.User.Identity.IsAuthenticated)
            {
                return string.Empty;
            }

            return context.User.Identity.Name;
        }

        private static IRolesDb ResolveRolesDb()
        {
            if (Global.Container == null)
            {
                return null;
            }

            return Global.Container.Resolve<IRolesDb>();
        }
    }
}
