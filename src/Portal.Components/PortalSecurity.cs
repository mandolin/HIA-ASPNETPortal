using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>提供旧门户的角色检查、模块编辑判定和历史密码摘要辅助方法。</zh-CN>
    ///   <en>Provides legacy Portal role checks, module-edit decisions, and a historical password-digest helper.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>模块编辑必须同时满足父 Tab 的访问角色与模块的编辑角色。<see cref="Encrypt"/> 使用无盐 MD5， 仅为既有用户数据兼容保留；新密码体系不得采用该方法，后续迁移需支持旧摘要验证后的渐进强哈希更新。</zh-CN>
    ///   <en>Module editing requires both parent-tab access roles and module edit roles. <see cref="Encrypt"/> uses unsalted MD5 and remains only for existing user-data compatibility; new password systems must not use it, and a later migration must support gradual strong-hash updates after legacy-digest verification.</en>
    /// </lang>
    /// </remarks>
    public class PortalSecurity : IPortalSecurity
    {
        private readonly IModulesDb _modulesConfig;
        private readonly ITabsDb _tabsConfig;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化模块与页签数据访问依赖。</zh-CN>
        ///   <en>Initializes module and tab data-access dependencies.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabsConfig">
        /// <l>
        ///   <zh-CN>读取父 Tab 访问角色的数据访问接口。</zh-CN>
        ///   <en>Data-access interface used to read parent-tab access roles.</en>
        /// </l>
        /// </param>
        /// <param name="modulesConfig">
        /// <l>
        ///   <zh-CN>读取模块编辑角色和所属 Tab 的数据访问接口。</zh-CN>
        ///   <en>Data-access interface used to read module edit roles and parent tabs.</en>
        /// </l>
        /// </param>
        public PortalSecurity(ITabsDb tabsConfig, IModulesDb modulesConfig)
        {
            _tabsConfig = tabsConfig;
            _modulesConfig = modulesConfig;
        }

        #region IPortalSecurity Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求身份是否可以编辑指定模块。</zh-CN>
        ///   <en>Determines whether the current request identity may edit the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>要检查的模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier to check.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>父 Tab 访问角色和模块编辑角色均满足时为 <c>true</c>；模块、父 Tab 或必要关联缺失时为 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when both parent-Tab access roles and module edit roles are satisfied; <c>false</c> when the module, parent Tab, or a required relationship is missing.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法处于请求标识进入授权判断的边界，因此缺失记录必须 fail closed。可缺失查找仍会对重复数据抛出， 以保留配置完整性故障的可观察性。</zh-CN>
        ///   <en>This method is a boundary where request identifiers enter authorization, so missing records must fail closed. Nullable lookups still throw for duplicate data, preserving observability of configuration-integrity failures.</en>
        /// </lang>
        /// </remarks>
        public bool HasEditPermissions(int moduleId)
        {
            // <lang>
            //   <zh-CN>模块编辑不是单独授权，必须先读取模块所属 Tab 的访问规则。</zh-CN>
            //   <en>Module editing is not standalone authorization; the parent-tab access rule is evaluated first.</en>
            // </lang>
            IModuleItem module = _modulesConfig.FindModuleById(moduleId);
            if (module == null || !module.TabId.HasValue)
            {
                return false;
            }

            ITabItem tab = _tabsConfig.FindTabById(module.TabId.Value);
            if (tab == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>两个角色集合都必须匹配，保持旧门户的严格组合规则。</zh-CN>
            //   <en>Both role sets must match to preserve the legacy Portal's strict combined rule.</en>
            // </lang>
            string editRoles = module.EditRoles;
            string accessRoles = tab.AccessRoles;

            // <lang>
            //   <zh-CN>任一集合不匹配即拒绝编辑。</zh-CN>
            //   <en>Editing is denied when either role set does not match.</en>
            // </lang>
            if (!IsInRoles(accessRoles) || !IsInRoles(editRoles))
            {
                return false;
            }
            return true;
        }

        #endregion

        /// <summary>
        /// <lang>
        ///   <zh-CN>计算既有用户表兼容的无盐 MD5 十六进制摘要。</zh-CN>
        ///   <en>Computes the unsalted MD5 hexadecimal digest compatible with the legacy user table.</en>
        /// </lang>
        /// </summary>
        /// <param name="cleanString">
        /// <l>
        ///   <zh-CN>要计算摘要的明文输入，不能为 <c>null</c>。</zh-CN>
        ///   <en>Plain-text input to digest; cannot be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带连字符的 MD5 十六进制摘要。</zh-CN>
        ///   <en>Hyphen-separated MD5 hexadecimal digest.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="cleanString"/> 为 <c>null</c> 时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="cleanString"/> is <c>null</c>.</en>
        /// </l>
        /// </exception>
        public static string Encrypt(string cleanString)
        {
            // <lang>
            //   <zh-CN>保留现有 UTF-8 与 BitConverter 格式，避免破坏旧数据库摘要匹配。</zh-CN>
            //   <en>Preserve the current UTF-8 and BitConverter format to avoid breaking legacy database digest matches.</en>
            // </lang>
            byte[] clearBytes = Encoding.UTF8.GetBytes(cleanString);

            // <lang>
            //   <zh-CN>MD5 实例只用于旧摘要兼容，不得扩展到新凭据写入路径。</zh-CN>
            //   <en>The MD5 instance is used only for legacy digest compatibility and must not be extended to new credential-write paths.</en>
            // </lang>
            using (HashAlgorithm algorithm = MD5.Create())
            {
                byte[] hashedBytes = algorithm.ComputeHash(clearBytes);

                // <lang>
                //   <zh-CN>此处不能添加盐或更换算法；安全升级必须经独立兼容迁移实施。</zh-CN>
                //   <en>Do not add salt or change algorithms here; security upgrades require a separate compatible migration.</en>
                // </lang>
                return BitConverter.ToString(hashedBytes);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前 HTTP 请求身份是否具有指定角色。</zh-CN>
        ///   <en>Determines whether the current HTTP request identity has the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>要检查的角色名称。</zh-CN>
        ///   <en>Role name to check.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前上下文存在且身份具有该角色时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a current context exists and its identity has the role.</en>
        /// </l>
        /// </returns>
        public static bool IsInRole(string role)
        {
            // <lang>
            //   <zh-CN>后台任务或测试中不存在 HTTP 上下文时安全拒绝。</zh-CN>
            //   <en>Safely deny when no HTTP context exists, such as in background work or tests.</en>
            // </lang>
            return HttpContext.Current?.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求身份是否命中旧格式角色列表中的任一角色。</zh-CN>
        ///   <en>Determines whether the current request identity matches any role in a legacy-format role list.</en>
        /// </lang>
        /// </summary>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>分号分隔的旧角色列表。</zh-CN>
        ///   <en>Semicolon-separated legacy role list.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>命中角色，或列表包含 <see cref="PortalRoleNames.AllUsers"/> 时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a role matches or the list contains <see cref="PortalRoleNames.AllUsers"/>.</en>
        /// </l>
        /// </returns>
        public static bool IsInRoles(string roles)
        {
            // <lang>
            //   <zh-CN>无请求上下文时不能形成授权结论。</zh-CN>
            //   <en>No authorization decision can be made without a request context.</en>
            // </lang>
            HttpContext context = HttpContext.Current;

            if (context == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>统一解析末尾分号、空项与多余空白，避免各页面自行拆分角色字符串。</zh-CN>
            //   <en>Normalize trailing separators, empty items, and whitespace instead of splitting role strings in each page.</en>
            // </lang>
            foreach (string role in PortalRoleParser.Parse(roles))
            {
                // <lang>
                //   <zh-CN>All Users 是旧门户虚拟角色，表示当前请求身份可访问。</zh-CN>
                //   <en>All Users is a legacy virtual role meaning the current request identity may access.</en>
                // </lang>
                if (string.Equals(role, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase) ||
                    (context.User?.IsInRole(role) ?? false))
                {
                    return true;
                }
            }

            // <lang>
            //   <zh-CN>未命中任何角色时保持拒绝。</zh-CN>
            //   <en>Keep access denied when no role matches.</en>
            // </lang>
            return false;
        }
    }
}
