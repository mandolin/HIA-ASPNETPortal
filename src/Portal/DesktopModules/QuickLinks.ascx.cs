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
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；当前实现不读取其内容。</zh-CN>
        ///   <en>The page-load event arguments; the current implementation does not read them.</en>
        /// </l>
        /// </param>
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
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>当前快捷链接条目的数据库标识。</zh-CN>
        ///   <en>The current quick-link item's database identifier.</en>
        /// </l>
        /// </param>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>当前快捷链接条目的候选浏览地址。</zh-CN>
        ///   <en>The current quick-link item's candidate browse URL.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>编辑者获得站内编辑页地址；浏览者获得通过策略的 URL 或空字符串。</zh-CN>
        ///   <en>Editors receive an in-site edit URL; viewers receive a policy-approved URL or an empty string.</en>
        /// </l>
        /// </returns>
        protected string ChooseUrl(object itemId, object url)
        {
            // <lang>
            //   <zh-CN>快捷链接在编辑态优先进入维护页面，让编辑者能修复当前不可浏览的旧 URL。</zh-CN>
            //   <en>Quick Links prefer the maintenance page in edit mode so editors can repair legacy URLs that are not currently browsable.</en>
            // </lang>
            if (IsEditable)
            {
                return "~/DesktopModules/EditLinks.aspx?ItemID=" + Convert.ToString(itemId) + "&mid=" + ModuleId;
            }

            // <lang>
            //   <zh-CN>浏览态只返回通过当前导航策略的 URL，策略失败则由空字符串抑制链接。</zh-CN>
            //   <en>Browse mode returns only URLs accepted by current navigation policy, with an empty string suppressing links on failure.</en>
            // </lang>
            return GetSafeBrowseUrl(url);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断导航图标在当前上下文中是否应显示。</zh-CN>
        ///   <en>Determines whether the navigation icon should be shown in the current context.</en>
        /// </lang>
        /// </summary>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>当前快捷链接条目的候选浏览地址。</zh-CN>
        ///   <en>The current quick-link item's candidate browse URL.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>编辑状态或地址可安全浏览时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the row is editable or the URL is safe to browse.</en>
        /// </l>
        /// </returns>
        protected bool CanRenderNavigation(object url)
        {
            // <lang>
            //   <zh-CN>编辑者始终看到图标作为修复入口；浏览者只在 URL 通过策略时看到图标。</zh-CN>
            //   <en>Editors always see the icon as a repair entry; viewers see it only when the URL passes policy.</en>
            // </lang>
            return IsEditable || !string.IsNullOrEmpty(GetSafeBrowseUrl(url));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回符合导航策略的浏览地址；非法旧值返回空字符串。</zh-CN>
        ///   <en>Returns a browse URL that passes navigation policy, or an empty string for an invalid legacy value.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自快捷链接记录的候选 URL。</zh-CN>
        ///   <en>The candidate URL from the quick-link row.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>策略允许的 URL；失败时为空字符串。</zh-CN>
        ///   <en>The policy-approved URL, or an empty string on failure.</en>
        /// </l>
        /// </returns>
        protected string GetSafeBrowseUrl(object value)
        {
            // <lang>
            //   <zh-CN>normalizedUrl 只在策略成功时作为 href 输出；失败时不回显历史原始地址。</zh-CN>
            //   <en>normalizedUrl is emitted as href only when policy succeeds; failures do not echo the legacy raw address.</en>
            // </lang>
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
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自快捷链接记录的候选 URL。</zh-CN>
        ///   <en>The candidate URL from the quick-link row.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选 URL 通过策略并且非空时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the candidate URL passes policy and is non-empty.</en>
        /// </l>
        /// </returns>
        protected bool HasSafeBrowseUrl(object value)
        {
            // <lang>
            //   <zh-CN>链接可见性使用与 href 相同的规范化结果，避免显示无法访问的快捷链接。</zh-CN>
            //   <en>Link visibility uses the same normalization result as href generation, avoiding visible quick links that cannot be reached.</en>
            // </lang>
            return !string.IsNullOrEmpty(GetSafeBrowseUrl(value));
        }
    }
}
