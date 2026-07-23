using System;
using System.Linq;
using System.Web;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户后台授权小工具，集中旧 Admin 角色检查与拒绝访问跳转。</zh-CN>
    ///   <en>Portal administration authorization helper that centralizes legacy Admin-role checks and access-denied redirects.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P5.3 起，新增权限 key facade；<see cref="PortalRoleNames.Administrators"/> 在过渡期自动拥有全部 已定义权限，保持旧后台不被权限模型引入破坏。</zh-CN>
    ///   <en>Starting with P5.3, this helper exposes a permission-key facade. During the transition, <see cref="PortalRoleNames.Administrators"/> automatically owns every defined permission so legacy administration paths are not broken by the new model.</en>
    /// </lang>
    /// </remarks>
    public static class PortalAuthorization
    {
        private const string EditAccessDeniedUrl = "~/Admin/EditAccessDenied.aspx";

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求身份是否具有旧门户管理员角色。</zh-CN>
        ///   <en>Determines whether the current request identity has the legacy Portal administrator role.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前身份属于 <c>Admins</c> 时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the current identity belongs to <c>Admins</c>.</en>
        /// </l>
        /// </returns>
        public static bool IsAdmin()
        {
            return PortalSecurity.IsInRole(PortalRoleNames.Administrators);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求身份是否具有指定权限。</zh-CN>
        ///   <en>Determines whether the current request identity has the specified permission.</en>
        /// </lang>
        /// </summary>
        /// <param name="permissionKey">
        /// <l>
        ///   <zh-CN>稳定权限键名。</zh-CN>
        ///   <en>Stable permission key.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>拥有权限时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the permission is granted.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>未知权限键一律拒绝并记录诊断。`Admins` 作为过渡兼容角色拥有所有已定义权限；其它角色从 <c>PortalCfg_RolePermissions</c> 读取映射。权限表缺失时，非管理员不会获得额外权限。</zh-CN>
        ///   <en>Unknown permission keys are always denied and logged. <c>Admins</c> acts as a transition role with every defined permission; other roles read mappings from <c>PortalCfg_RolePermissions</c>. When the table is missing, non-admin users do not gain additional permissions.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>判断当前请求身份是否至少具有给定权限中的任意一个。</zh-CN>
        ///   <en>Determines whether the current request identity has at least one of the specified permissions.</en>
        /// </lang>
        /// </summary>
        /// <param name="permissionKeys">
        /// <l>
        ///   <zh-CN>稳定权限键名集合。</zh-CN>
        ///   <en>Stable permission keys.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>任一权限被授予时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when any specified permission is granted.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法用于 P12.4 这类权限拆分过渡点，让新细粒度权限和旧聚合权限可以并行存在。 未定义权限键会被拒绝并记录诊断，不能扩大访问范围。</zh-CN>
        ///   <en>This method supports transition points such as P12.4 where new fine-grained permissions and old aggregate permissions coexist. Undefined keys are denied and diagnosed, never broadening access.</en>
        /// </lang>
        /// </remarks>
        public static bool HasAnyPermission(params string[] permissionKeys)
        {
            HttpContext context = HttpContext.Current;
            string[] normalizedKeys;
            if (!TryNormalizePermissionKeys(permissionKeys, context, out normalizedKeys))
            {
                return false;
            }

            if (normalizedKeys.Length == 0)
            {
                PortalDiagnostics.Warn(
                    "Authorization.PermissionKey",
                    "No permission keys were supplied for an any-permission check.",
                    context);
                return false;
            }

            return normalizedKeys.Any(HasPermission);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认当前请求为管理员；未授权时安全跳转到既有拒绝访问页，并返回 <c>false</c>。</zh-CN>
        ///   <en>Confirms that the current request is administrative; safely redirects unauthorized requests to the existing access-denied page and returns <c>false</c>.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前请求可继续执行后台逻辑时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the current request may continue administration logic.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方应采用 <c>if (!EnsureAdmin(Context)) return;</c>，避免拒绝重定向后继续执行写入。</zh-CN>
        ///   <en>Callers should use <c>if (!EnsureAdmin(Context)) return;</c> to prevent writes from continuing after an access-denied redirect.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>确认当前请求具有指定权限；未授权时记录诊断并跳转到既有拒绝访问页。</zh-CN>
        ///   <en>Confirms that the current request has the specified permission; unauthorized requests are logged and redirected to the existing access-denied page.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <param name="permissionKey">
        /// <l>
        ///   <zh-CN>稳定权限键名。</zh-CN>
        ///   <en>Stable permission key.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前请求可继续执行敏感逻辑时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request may continue sensitive logic.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>确认当前请求至少具有给定权限中的任意一个；未授权时记录诊断并跳转到既有拒绝访问页。</zh-CN>
        ///   <en>Confirms that the current request has at least one of the specified permissions; unauthorized requests are logged and redirected to the existing access-denied page.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <param name="permissionKeys">
        /// <l>
        ///   <zh-CN>稳定权限键名集合。</zh-CN>
        ///   <en>Stable permission keys.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前请求可继续执行敏感逻辑时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request may continue sensitive logic.</en>
        /// </l>
        /// </returns>
        public static bool EnsureAnyPermission(HttpContext context, params string[] permissionKeys)
        {
            context = context ?? HttpContext.Current;
            string[] normalizedKeys;
            if (!TryNormalizePermissionKeys(permissionKeys, context, out normalizedKeys) ||
                normalizedKeys.Length == 0)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(context);
                return false;
            }

            if (normalizedKeys.Any(HasPermission))
            {
                return true;
            }

            PortalDiagnostics.Warn(
                "Authorization.PermissionDenied",
                "Permission denied. PermissionKeys=" + string.Join(",", normalizedKeys),
                context);
            PortalNavigationPolicy.RedirectToEditAccessDenied(context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>要求当前请求身份为管理员；不满足时跳转到既有后台拒绝访问页。</zh-CN>
        ///   <en>Requires the current request identity to be an administrator; otherwise redirects to the existing administration access-denied page.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法只能在存在当前 HTTP 响应的页面请求中使用。重定向后调用方不应继续执行敏感写操作。</zh-CN>
        ///   <en>This method can be used only during a page request with a current HTTP response. Callers must not continue sensitive writes after the redirect.</en>
        /// </lang>
        /// </remarks>
        public static void RequireAdmin()
        {
            if (!IsAdmin())
            {
                HttpContext.Current.Response.Redirect(EditAccessDeniedUrl);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>要求当前请求具有指定权限；未授权时跳转到既有后台拒绝访问页。</zh-CN>
        ///   <en>Requires the current request to have the specified permission; otherwise redirects to the existing administration access-denied page.</en>
        /// </lang>
        /// </summary>
        /// <param name="permissionKey">
        /// <l>
        ///   <zh-CN>稳定权限键名。</zh-CN>
        ///   <en>Stable permission key.</en>
        /// </l>
        /// </param>
        public static void RequirePermission(string permissionKey)
        {
            HttpContext context = HttpContext.Current;
            if (!EnsurePermission(context, permissionKey) && context != null)
            {
                // <lang>
                //   <zh-CN>Require* 兼容旧 Response.Redirect 默认中止语义，避免调用方漏写 return 后继续执行敏感逻辑。</zh-CN>
                //   <en>Require* keeps the legacy aborting redirect semantics so missed returns cannot continue sensitive logic.</en>
                // </lang>
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

        private static bool TryNormalizePermissionKeys(
            string[] permissionKeys,
            HttpContext context,
            out string[] normalizedKeys)
        {
            normalizedKeys = new string[0];
            try
            {
                normalizedKeys = PortalPermissionRegistry.NormalizeDefinedKeys(permissionKeys);
                return true;
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "Authorization.PermissionKey",
                    "Undefined or invalid permission key requested in an any-permission check.",
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
