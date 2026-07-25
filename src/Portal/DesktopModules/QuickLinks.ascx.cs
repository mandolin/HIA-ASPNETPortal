using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示快捷链接，并隔离不符合当前地址策略的历史链接。</zh-CN>
    ///   <en>Renders quick links and isolates legacy links that do not pass the current URL policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>快捷链接与普通链接共享地址策略；不同之处是它还会在模块内呈现编辑者新增入口。</zh-CN>
    ///   <en>Quick Links share the same URL policy as regular Links; the difference is that they also render an in-module add action for editors.</en>
    /// </lang>
    /// </remarks>
    public partial class QuickLinks : PortalModuleControl<QuickLinks>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>链接数据访问服务。</zh-CN>
        ///   <en>Link data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定快捷链接，并仅向具有模块编辑权限的用户显示新增入口。</zh-CN>
        ///   <en>Binds quick links and exposes the add entry only to users with module-edit permission.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>快捷链接每次请求都重新绑定，确保编辑完成回到门户页后立即反映最新链接和地址策略结果。</zh-CN>
            //   <en>Quick Links bind on every request so returning from edit immediately reflects the latest links and URL-policy results.</en>
            // </lang>
            myDataList.DataSource = LinkDB.GetLinks(ModuleId);
            myDataList.DataBind();

            // <lang>
            //   <zh-CN>快捷链接新增入口默认隐藏，避免匿名浏览时渲染空按钮框。</zh-CN>
            //   <en>Hide the quick-link add action by default so anonymous browsing does not render an empty button.</en>
            // </lang>
            QuickLinkActions.Visible = false;
            EditButton.Visible = false;
            if (PortalSecurity.IsInRoles(ModuleConfiguration.AuthorizedEditRoles))
            {
                // <lang>
                //   <zh-CN>新增入口只给模块编辑者显示；普通访问者即使能看链接，也不能看到编辑页地址。</zh-CN>
                //   <en>The add action is shown only to module editors; ordinary visitors may see links but not edit-page URLs.</en>
                // </lang>
                EditButton.Text = "Add Link";
                EditButton.NavigateUrl = "~/DesktopModules/EditLinks.aspx?mid=" + ModuleId;
                EditButton.ToolTip = "Open module action: Add Link";
                EditButton.Visible = true;
                QuickLinkActions.Visible = true;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑者使用本页编辑地址；普通浏览者只能获得通过地址策略的链接。</zh-CN>
        ///   <en>Editors receive the current page's edit URL; ordinary visitors receive only a URL that passes navigation policy.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>判断导航图标在当前上下文中是否应显示。</zh-CN>
        ///   <en>Determines whether the navigation icon should be shown in the current context.</en>
        /// </lang>
        /// </summary>
        protected bool CanRenderNavigation(object url)
        {
            return IsEditable || !string.IsNullOrEmpty(GetSafeBrowseUrl(url));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回符合导航策略的浏览地址；非法旧值返回空字符串。</zh-CN>
        ///   <en>Returns a browse URL that passes navigation policy, or an empty string for an invalid legacy value.</en>
        /// </lang>
        /// </summary>
        protected string GetSafeBrowseUrl(object value)
        {
            string normalizedUrl;
            return PortalNavigationPolicy.TryNormalizeBrowseUrl(Convert.ToString(value), Context.Request, out normalizedUrl)
                ? normalizedUrl
                : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断旧记录中的浏览地址是否仍可安全渲染为链接。</zh-CN>
        ///   <en>Determines whether a legacy browse URL can still be safely rendered as a link.</en>
        /// </lang>
        /// </summary>
        protected bool HasSafeBrowseUrl(object value)
        {
            return !string.IsNullOrEmpty(GetSafeBrowseUrl(value));
        }
    }
}
