using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>把入口可见性策略接入 Admin 页头动作区的渲染器（Web 侧组装上下文，策略本身仍在组件层）。</zh-CN>
    ///   <en>Renderer that connects the entry visibility policy to the Admin page header action area (the web side assembles the context while the policy stays in the component layer).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型只输出导航类入口；页面自身的写库主操作（新增用户、新增角色、新增 Tab 等）与既有服务器控件由页面自身保留，不在本渲染器范围内。可见入口为 0 时返回空字符串，使动作区容器整体不渲染。</zh-CN>
    ///   <en>This type emits navigation entries only; page-owned persistence actions (add user, add role, add tab) and existing server controls stay owned by the page and are out of scope. When no entry is visible it returns an empty string so the action container is not rendered at all.</en>
    /// </lang>
    /// </remarks>
    public static class PortalNavigationEntryRenderer
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>渲染指定入口的相关导航入口；无可见入口时返回空字符串。</zh-CN>
        ///   <en>Render the related navigation entries for an entry; returns an empty string when none is visible.</en>
        /// </lang>
        /// </summary>
        /// <param name="entryKey">
        /// <l>
        ///   <zh-CN>当前入口稳定键。</zh-CN>
        ///   <en>Stable key of the current entry.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文；可为 null（此时按无上下文处理）。</zh-CN>
        ///   <en>Current HTTP context; may be null (treated as no context).</en>
        /// </l>
        /// </param>
        /// <param name="maxCount">
        /// <l>
        ///   <zh-CN>返回条数上限。</zh-CN>
        ///   <en>Maximum number of entries to render.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>动作区 HTML；无可见入口时为空字符串。</zh-CN>
        ///   <en>Action-area HTML; empty when no entry is visible.</en>
        /// </l>
        /// </returns>
        public static string RenderActions(string entryKey, HttpContext context, int maxCount = PortalNavigationVisibilityPolicy.DefaultMaxRelatedEntries)
        {
            // <lang>
            //   <zh-CN>先取同组候选（已含可导航过滤与常用入口兜底），再按当前上下文逐个判定；候选为空时直接返回空字符串。</zh-CN>
            //   <en>Take same-group candidates first (already filtered for navigability and topped up from common entries), then evaluate each against the current context; return an empty string when there is no candidate.</en>
            // </lang>
            IList<PortalNavigationEntry> candidates = PortalNavigationVisibilityPolicy.GetRelatedEntries(entryKey, maxCount);
            if (candidates == null || candidates.Count == 0)
            {
                return string.Empty;
            }

            PortalNavigationVisibilityContext visibilityContext = BuildContext(candidates);

            StringBuilder builder = new StringBuilder();
            int rendered = 0;

            foreach (PortalNavigationEntry entry in candidates)
            {
                if (rendered >= maxCount)
                {
                    break;
                }

                string reason = PortalNavigationVisibilityPolicy.GetBlockedReason(entry, visibilityContext);
                if (string.IsNullOrEmpty(reason))
                {
                    builder.Append(RenderLink(entry));
                    rendered++;
                }
                else if (visibilityContext.IsAdministrator)
                {
                    // <lang>
                    //   <zh-CN>管理员额外看到被阻断入口的禁用态与原因，便于排查"入口为何不显示"。</zh-CN>
                    //   <en>Administrators additionally see blocked entries as disabled with a reason, which helps diagnose why an entry is missing.</en>
                    // </lang>
                    builder.Append(RenderDisabled(entry, reason));
                    rendered++;
                }
            }

            // <lang>
            //   <zh-CN>一条都没有渲染时不输出容器，避免留下空动作区。</zh-CN>
            //   <en>Emit no container when nothing was rendered, so an empty action area is never left behind.</en>
            // </lang>
            if (rendered == 0)
            {
                return string.Empty;
            }

            return "<div class=\"portal-admin-actions\">" + builder + "</div>";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按候选入口所需的角色与权限键组装可见性上下文：只探测候选项实际声明的依赖，避免全量枚举权限。</zh-CN>
        ///   <en>Assemble the visibility context from the role and permission dependencies actually declared by the candidates, avoiding a full permission enumeration.</en>
        /// </lang>
        /// </summary>
        private static PortalNavigationVisibilityContext BuildContext(IEnumerable<PortalNavigationEntry> candidates)
        {
            // <lang>
            //   <zh-CN>管理员身份由既有授权入口判定；它不是放行条件，后续仍逐项校验角色、权限、包与 Profile。</zh-CN>
            //   <en>Administrator identity comes from the existing authorization entry point; it is not a bypass, because roles, permissions, packages, and Profiles are still checked individually.</en>
            // </lang>
            bool isAdministrator = PortalAuthorization.IsAdmin();

            List<string> roles = new List<string>();
            List<string> permissions = new List<string>();

            foreach (PortalNavigationEntry entry in candidates)
            {
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>渲染一个可导航入口链接：只输出语义类，不内联颜色或尺寸。</zh-CN>
        ///   <en>Render one navigable entry link: emit semantic classes only, with no inline colors or sizes.</en>
        /// </lang>
        /// </summary>
        private static string RenderLink(PortalNavigationEntry entry)
        {
            string text = HttpUtility.HtmlEncode(GetDisplayText(entry));
            string href = HttpUtility.HtmlAttributeEncode(ResolveTarget(entry));

            return "<a class=\"CommandButton portal-secondary-action\" href=\"" + href + "\" title=\"" + text + "\">" + text + "</a>";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>渲染管理员可见的禁用态入口：不可点击，title 携带阻断原因代码。</zh-CN>
        ///   <en>Render an entry as disabled for administrators: not clickable, with the blocked reason code in the title.</en>
        /// </lang>
        /// </summary>
        private static string RenderDisabled(PortalNavigationEntry entry, string reason)
        {
            string text = HttpUtility.HtmlEncode(GetDisplayText(entry));

            return "<span class=\"CommandButton portal-secondary-action portal-disabled-text\" aria-disabled=\"true\" title=\"" +
                   HttpUtility.HtmlAttributeEncode(text + " (" + reason + ")") + "\">" + text + "</span>";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取入口显示文本：优先中文，缺省回落英文。</zh-CN>
        ///   <en>Get the entry display text: Chinese first, falling back to English.</en>
        /// </lang>
        /// </summary>
        private static string GetDisplayText(PortalNavigationEntry entry)
        {
            // <lang>
            //   <zh-CN>按当前界面文化解析显示名：与 Web.config 的 globalization uiCulture 同源，切换语言时页头链接与页面文案一起切换，避免中英混排。</zh-CN>
            //   <en>Resolve the display name from the current UI culture: it shares the Web.config globalization uiCulture source, so switching languages switches the header links and the page text together and avoids a mixed-language surface.</en>
            // </lang>
            return entry.GetDisplayName(System.Globalization.CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把 registry 中的应用相对目标解析为可访问 URL；只有页面型目标会被渲染。</zh-CN>
        ///   <en>Resolve the registry's application-relative target into a reachable URL; only page-style targets are rendered.</en>
        /// </lang>
        /// </summary>
        private static string ResolveTarget(PortalNavigationEntry entry)
        {
            // <lang>
            //   <zh-CN>目标以仓库相对路径登记，渲染时统一加 ~/ 交给 WebForms 解析，避免硬编码站点路径。</zh-CN>
            //   <en>Targets are registered as repository-relative paths and are rendered with a leading ~/ so WebForms resolves them, avoiding hardcoded site paths.</en>
            // </lang>
            return VirtualPathUtility.ToAbsolute("~/" + entry.Target.TrimStart('~', '/'));
        }
    }
}
