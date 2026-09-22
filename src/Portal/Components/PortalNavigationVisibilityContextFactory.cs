using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>在 Web 侧把当前请求的用户与部署状态组装为导航可见性上下文，供策略类做纯内存判定。</zh-CN>
    ///   <en>Assembles the navigation visibility context on the web side from the current request user and deployment state so the policy classes can decide purely in memory.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>Admin 页头动作区与主导航 Tab 门控共用本工厂，确保两处的可见性语义完全一致。本类只读取既有授权入口（PortalAuthorization / PortalSecurity）与 Profile resolver，不自行判定权限，也不改变任何授权结果。</zh-CN>
    ///   <en>The Admin header action area and the main-navigation tab gate share this factory so both apply identical visibility semantics. It only reads existing authorization entry points (PortalAuthorization / PortalSecurity) and the Profile resolver; it never decides permissions itself and never changes an authorization outcome.</en>
    /// </lang>
    /// </remarks>
    public static class PortalNavigationVisibilityContextFactory
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按候选项实际声明的依赖组装可见性上下文，避免全量枚举角色与权限。</zh-CN>
        ///   <en>Assembles the visibility context from the dependencies actually declared by the candidates, avoiding a full enumeration of roles and permissions.</en>
        /// </lang>
        /// </summary>
        /// <param name="entries">
        /// <l>
        ///   <zh-CN>本次需要判定的入口集合；为 null 时按空集合处理，仍返回可用于部署侧判定的上下文。</zh-CN>
        ///   <en>Entries to be evaluated; null is treated as an empty set, and a context usable for deployment-side decisions is still returned.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前用户与部署状态的可见性上下文。</zh-CN>
        ///   <en>Visibility context describing the current user and deployment state.</en>
        /// </l>
        /// </returns>
        public static PortalNavigationVisibilityContext Create(IEnumerable<PortalNavigationEntry> entries)
        {
            // <lang>
            //   <zh-CN>管理员身份由既有授权入口判定；它不是放行条件，后续仍逐项校验角色、权限、包与 Profile。</zh-CN>
            //   <en>Administrator identity comes from the existing authorization entry point; it is not a bypass, because roles, permissions, packages, and Profiles are still checked individually.</en>
            // </lang>
            bool isAdministrator = PortalAuthorization.IsAdmin();

            // <lang>
            //   <zh-CN>角色与权限集合按候选项声明逐个探测：只为"确实被声明且当前用户确实拥有"的项建集合，减少无谓的权限查询。</zh-CN>
            //   <en>Probe role and permission collections per declared dependency: only entries that are actually declared and actually held by the current user are collected, reducing needless permission lookups.</en>
            // </lang>
            List<string> roles = new List<string>();
            List<string> permissions = new List<string>();

            if (entries != null)
            {
                foreach (PortalNavigationEntry entry in entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    foreach (string role in entry.RequiredRoles)
                    {
                        if (!roles.Contains(role) && PortalSecurity.IsInRole(role))
                        {
                            roles.Add(role);
                        }
                    }

                    foreach (string key in entry.RequiredPermissionKeys)
                    {
                        if (!permissions.Contains(key) && PortalAuthorization.HasPermission(key))
                        {
                            permissions.Add(key);
                        }
                    }
                }
            }

            // <lang>
            //   <zh-CN>部署侧状态取自 Profile resolver；读取失败时退化为"无允许包、无包含 Profile"，宁可不显示也不放行。</zh-CN>
            //   <en>Deployment state comes from the Profile resolver; on failure it degrades to "no allowed packages, no included profiles", preferring to hide rather than grant.</en>
            // </lang>
            string activeProfile = string.Empty;
            IList<string> allowedPackages = new string[0];
            IList<string> includedProfiles = new string[0];

            try
            {
                PortalModuleProfileSnapshot snapshot = PortalModuleProfileResolver.Resolve(HttpContext.Current);
                if (snapshot != null)
                {
                    activeProfile = snapshot.ActiveProfile ?? string.Empty;
                    allowedPackages = snapshot.AllowedPackageIds ?? new string[0];
                    includedProfiles = ReadIncludedProfiles(activeProfile);
                }
            }
            catch
            {
                // <lang>
                //   <zh-CN>Profile 解析异常不影响页面其余部分：按最保守状态处理。</zh-CN>
                //   <en>A Profile resolution failure must not affect the rest of the page: fall back to the most conservative state.</en>
                // </lang>
            }

            return new PortalNavigationVisibilityContext(
                isAdministrator,
                roles,
                permissions,
                allowedPackages,
                activeProfile,
                includedProfiles);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前 Profile 通过 Includes 传递包含的 Profile 列表；配置缺失或异常时返回空集合。</zh-CN>
        ///   <en>Read the Profiles transitively included by the current Profile through Includes; returns an empty set when configuration is missing or faulty.</en>
        /// </lang>
        /// </summary>
        /// <param name="activeProfile">
        /// <l>
        ///   <zh-CN>当前启用的 Profile 名称；空白时直接返回空集合。</zh-CN>
        ///   <en>Currently active Profile name; blank input yields an empty set.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>只读的传递包含 Profile 列表。</zh-CN>
        ///   <en>Read-only list of transitively included Profiles.</en>
        /// </l>
        /// </returns>
        private static IList<string> ReadIncludedProfiles(string activeProfile)
        {
            if (string.IsNullOrWhiteSpace(activeProfile))
            {
                return new string[0];
            }

            string configured = ConfigurationManager.AppSettings["Portal.ModuleProfiles." + activeProfile + ".Includes"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                return new string[0];
            }

            return configured
                .Split(',')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList()
                .AsReadOnly();
        }
    }
}
