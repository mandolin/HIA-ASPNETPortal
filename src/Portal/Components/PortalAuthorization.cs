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
        // <lang>
        //   <zh-CN>既有后台拒绝访问页的应用相对路径；所有 Require* 兼容入口都复用这一固定目标。</zh-CN>
        //   <en>Application-relative path of the existing administration access-denied page; every Require* compatibility entry point reuses this fixed target.</en>
        // </lang>
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
            // <lang>
            //   <zh-CN>读取当前请求上下文，仅用于身份来源和低敏诊断；不把请求数据本身带入授权查询。</zh-CN>
            //   <en>Capture the current request context only for identity sourcing and low-sensitivity diagnostics; do not carry request data into the authorization query.</en>
            // </lang>
            HttpContext context = HttpContext.Current;
            // <lang>
            //   <zh-CN>规范化后的权限键是后续管理员快速路径、角色查询和诊断消息的唯一键值。</zh-CN>
            //   <en>The normalized permission key is the sole key used by the administrator fast path, role lookup, and diagnostic messages.</en>
            // </lang>
            string normalizedKey;
            if (!TryNormalizePermissionKey(permissionKey, context, out normalizedKey))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>兼容管理员角色拥有已定义权限；在查询数据库前短路，避免无必要的角色存储依赖。</zh-CN>
            //   <en>The compatibility administrator role owns defined permissions; short-circuit before database lookup to avoid an unnecessary role-store dependency.</en>
            // </lang>
            if (IsAdmin())
            {
                return true;
            }

            // <lang>
            //   <zh-CN>只取已认证身份名称；缺少用户主体时 fail-closed 拒绝权限。</zh-CN>
            //   <en>Read only the authenticated identity name; fail closed when no user principal is available.</en>
            // </lang>
            string userName = GetCurrentUserName(context);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>从受控 Unity 容器取得角色数据门面；容器缺失时保持拒绝而不伪造权限。</zh-CN>
                //   <en>Resolve the role-data facade from the controlled Unity container; deny when the container is unavailable instead of fabricating permission.</en>
                // </lang>
                IRolesDb rolesDb = ResolveRolesDb();
                if (rolesDb == null)
                {
                    PortalDiagnostics.Warn(
                        "Authorization.PermissionLookup",
                        "Permission lookup skipped because IRolesDb is unavailable. PermissionKey=" + normalizedKey,
                        context);
                    return false;
                }

                // <lang>
                //   <zh-CN>只在角色数据返回的稳定权限键中做不区分大小写匹配，不把角色名或未知键直接当作权限。</zh-CN>
                //   <en>Match case-insensitively only against stable permission keys returned by the role store; neither role names nor unknown keys become permissions directly.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>保持同一请求上下文供规范化失败和空集合诊断使用。</zh-CN>
            //   <en>Keep the same request context for normalization-failure and empty-set diagnostics.</en>
            // </lang>
            HttpContext context = HttpContext.Current;
            // <lang>
            //   <zh-CN>把输入集合转换为已登记且去重的权限键，避免任意字符串扩大检查范围。</zh-CN>
            //   <en>Convert the input collection into registered, normalized permission keys so arbitrary strings cannot expand the check scope.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>调用方可传入空上下文；统一回退到当前请求后，拒绝跳转和诊断使用同一个对象。</zh-CN>
            //   <en>Callers may pass a null context; fall back to the current request once so redirect and diagnostics use the same object.</en>
            // </lang>
            context = context ?? HttpContext.Current;
            // <lang>
            //   <zh-CN>复用统一规范化结果，使拒绝路径不会以未验证原文构造权限消息或查询。</zh-CN>
            //   <en>Reuse the unified normalized result so the denial path never builds messages or queries from unvalidated input.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>权限失败先记录稳定键，再跳转既有拒绝页；调用方必须停止后续敏感操作。</zh-CN>
            //   <en>Record the stable key before redirecting to the existing denial page; callers must stop subsequent sensitive work.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>统一当前上下文，保证空键、拒绝和跳转的边界一致。</zh-CN>
            //   <en>Normalize the current context once so empty-key handling, denial, and redirect share one boundary.</en>
            // </lang>
            context = context ?? HttpContext.Current;
            // <lang>
            //   <zh-CN>只允许已登记、规范化后的键参与任一权限检查；空集合直接拒绝。</zh-CN>
            //   <en>Allow only registered, normalized keys into the any-permission check; an empty set is denied directly.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>任一键均未获权时记录规范键集合并跳转，不将失败当作管理员回退。</zh-CN>
            //   <en>When no key is granted, record the normalized set and redirect; failure is never treated as an administrator fallback.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>保留当前上下文引用，使 EnsurePermission 的拒绝结果可以进入兼容性终止重定向。</zh-CN>
            //   <en>Keep the current context reference so EnsurePermission's denial can enter the compatibility aborting redirect.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把单个权限键规范化为注册表中的已定义键，并将非法键 fail-closed。</zh-CN>
        ///   <en>Normalizes one permission key to a registry-defined key and fails closed for invalid keys.</en>
        /// </lang>
        /// </summary>
        /// <param name="permissionKey">
        /// <l>
        ///   <zh-CN>调用方提供的候选权限键。</zh-CN>
        ///   <en>Candidate permission key supplied by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>非法键诊断所使用的 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>HTTP context used for invalid-key diagnostics; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedKey">
        /// <l>
        ///   <zh-CN>成功时返回注册表规范键；失败时为空。</zh-CN>
        ///   <en>Registry-normalized key on success; empty on failure.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选键已定义且规范化成功时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the candidate is defined and normalized successfully.</en>
        /// </l>
        /// </returns>
        private static bool TryNormalizePermissionKey(string permissionKey, HttpContext context, out string normalizedKey)
        {
            // <lang>
            //   <zh-CN>失败默认为空，避免异常或注册表失败时把调用方原文当成可信权限键。</zh-CN>
            //   <en>Default to empty so an exception or registry failure never promotes caller input to a trusted permission key.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化任一权限检查的键集合，并在任一键非法时整体 fail-closed。</zh-CN>
        ///   <en>Normalizes the key set for an any-permission check and fails the whole operation closed when any key is invalid.</en>
        /// </lang>
        /// </summary>
        /// <param name="permissionKeys">
        /// <l>
        ///   <zh-CN>调用方提供的候选权限键集合。</zh-CN>
        ///   <en>Candidate permission-key collection supplied by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>非法集合诊断所使用的 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>HTTP context used for invalid-collection diagnostics; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedKeys">
        /// <l>
        ///   <zh-CN>成功时返回去重后的注册表规范键集合；失败时为空集合。</zh-CN>
        ///   <en>Deduplicated registry-normalized keys on success; an empty array on failure.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>集合中所有键均已定义且规范化成功时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when every key is defined and the collection normalizes successfully.</en>
        /// </l>
        /// </returns>
        private static bool TryNormalizePermissionKeys(
            string[] permissionKeys,
            HttpContext context,
            out string[] normalizedKeys)
        {
            // <lang>
            //   <zh-CN>失败时提供空数组，避免调用方枚举到部分成功结果并意外放宽任一权限判断。</zh-CN>
            //   <en>Use an empty array on failure so callers cannot enumerate partial success and accidentally broaden any-permission evaluation.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>从已认证 HTTP 主体读取当前用户名；上下文或身份不完整时返回空字符串。</zh-CN>
        ///   <en>Reads the current user name from an authenticated HTTP principal and returns an empty string when context or identity is incomplete.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已认证主体名称，或空字符串。</zh-CN>
        ///   <en>Authenticated principal name, or an empty string.</en>
        /// </l>
        /// </returns>
        private static string GetCurrentUserName(HttpContext context)
        {
            if (context == null || context.User == null || context.User.Identity == null ||
                !context.User.Identity.IsAuthenticated)
            {
                return string.Empty;
            }

            return context.User.Identity.Name;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从全局 Unity 容器解析角色数据门面；容器不存在时返回 <c>null</c>。</zh-CN>
        ///   <en>Resolves the role-data facade from the global Unity container and returns <c>null</c> when the container is unavailable.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色数据门面，或表示基础设施不可用的 <c>null</c>。</zh-CN>
        ///   <en>Role-data facade, or <c>null</c> when the infrastructure is unavailable.</en>
        /// </l>
        /// </returns>
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
