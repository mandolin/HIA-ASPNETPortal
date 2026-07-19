using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示快捷链接，并隔离不符合当前地址策略的历史链接。
    ///
    /// English: Renders quick links and isolates legacy links that do not pass the current URL policy.
    /// </summary>
    public partial class QuickLinks : PortalModuleControl<QuickLinks>
    {
        /// <summary>
        /// 中文：链接数据访问服务。English: Link data-access service.
        /// </summary>
        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        /// <summary>
        /// 中文：绑定快捷链接，并仅向具有模块编辑权限的用户显示新增入口。
        ///
        /// English: Binds quick links and exposes the add entry only to users with module-edit permission.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataList.DataSource = LinkDB.GetLinks(ModuleId);
            myDataList.DataBind();

            // 中文：快捷链接新增入口默认隐藏，避免匿名浏览时渲染空按钮框。
            // English: Hide the quick-link add action by default so anonymous browsing does not render an empty button.
            EditButton.Visible = false;
            if (PortalSecurity.IsInRoles(ModuleConfiguration.AuthorizedEditRoles))
            {
                EditButton.Text = "Add Link";
                EditButton.NavigateUrl = "~/DesktopModules/EditLinks.aspx?mid=" + ModuleId;
                EditButton.Visible = true;
            }
        }

        /// <summary>
        /// 中文：编辑者使用本页编辑地址；普通浏览者只能获得通过地址策略的链接。
        ///
        /// English: Editors receive the current page's edit URL; ordinary visitors receive only a URL that passes navigation policy.
        /// </summary>
        protected string ChooseUrl(object itemId, object url)
        {
            if (IsEditable)
            {
                return "~/DesktopModules/EditLinks.aspx?ItemID=" + Convert.ToString(itemId) + "&mid=" + ModuleId;
            }

            return GetSafeBrowseUrl(url);
        }

        /// <summary>
        /// 中文：判断导航图标在当前上下文中是否应显示。English: Determines whether the navigation icon should be shown in the current context.
        /// </summary>
        protected bool CanRenderNavigation(object url)
        {
            return IsEditable || !string.IsNullOrEmpty(GetSafeBrowseUrl(url));
        }

        /// <summary>
        /// 中文：返回符合导航策略的浏览地址；非法旧值返回空字符串。English: Returns a browse URL that passes navigation policy, or an empty string for an invalid legacy value.
        /// </summary>
        protected string GetSafeBrowseUrl(object value)
        {
            string normalizedUrl;
            return PortalNavigationPolicy.TryNormalizeBrowseUrl(Convert.ToString(value), Context.Request, out normalizedUrl)
                ? normalizedUrl
                : string.Empty;
        }

        /// <summary>
        /// 中文：判断旧记录中的浏览地址是否仍可安全渲染为链接。English: Determines whether a legacy browse URL can still be safely rendered as a link.
        /// </summary>
        protected bool HasSafeBrowseUrl(object value)
        {
            return !string.IsNullOrEmpty(GetSafeBrowseUrl(value));
        }
    }
}
