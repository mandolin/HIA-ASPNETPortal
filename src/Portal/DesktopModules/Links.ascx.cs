using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示链接列表，并隔离旧记录中不再符合地址策略的链接。</zh-CN>
    ///   <en>Renders the link list and isolates legacy links that no longer pass URL policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>普通浏览路径只输出通过 `PortalNavigationPolicy` 的地址；编辑者仍进入站内编辑页，以便修复历史不合规链接。</zh-CN>
    ///   <en>The browse path outputs only URLs accepted by `PortalNavigationPolicy`; editors still enter the internal edit page so legacy non-compliant links can be repaired.</en>
    /// </lang>
    /// </remarks>
    public partial class Links : PortalModuleControl<Links>
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
        ///   <zh-CN>读取并绑定当前模块链接。</zh-CN>
        ///   <en>Reads and binds links for the current module.</en>
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
            //   <zh-CN>旧链接模块每次请求都重新绑定，保持编辑回跳后能立即看到最新排序、标题和地址策略结果。</zh-CN>
            //   <en>The legacy Links module binds on every request so edits show the latest ordering, title and URL-policy result immediately after returning.</en>
            // </lang>
            myDataList.DataSource = LinkDB.GetLinks(ModuleId);
            myDataList.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑者使用本页编辑地址；普通浏览者只能获得通过地址策略的链接。</zh-CN>
        ///   <en>Editors receive the current page's edit URL; ordinary visitors receive only a URL that passes navigation policy.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>当前链接条目的数据库标识；编辑路径使用它定位记录。</zh-CN>
        ///   <en>The current link item's database identifier, used by the edit path to locate the row.</en>
        /// </l>
        /// </param>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>当前链接条目的候选浏览地址。</zh-CN>
        ///   <en>The current link item's candidate browse URL.</en>
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
            //   <zh-CN>编辑状态优先进入站内修复路径，即使旧 URL 当前不符合浏览策略也允许维护者修改。</zh-CN>
            //   <en>Edit mode prefers the in-site repair path so maintainers can update a legacy URL even when it currently fails browse policy.</en>
            // </lang>
            if (IsEditable)
            {
                return "~/DesktopModules/EditLinks.aspx?ItemID=" + Convert.ToString(itemId) + "&mid=" + ModuleId;
            }

            // <lang>
            //   <zh-CN>非编辑状态只允许规范化后的浏览地址，失败时由空字符串阻断链接输出。</zh-CN>
            //   <en>Non-edit mode allows only normalized browse URLs, with an empty string blocking link output on failure.</en>
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
        ///   <zh-CN>当前链接条目的候选浏览地址。</zh-CN>
        ///   <en>The current link item's candidate browse URL.</en>
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
            //   <zh-CN>编辑者始终看到导航图标作为修复入口；浏览者必须先通过地址策略。</zh-CN>
            //   <en>Editors always see the navigation icon as a repair entry, while viewers must first pass URL policy.</en>
            // </lang>
            return IsEditable || !string.IsNullOrEmpty(GetSafeBrowseUrl(url));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回链接目标窗口名称。</zh-CN>
        ///   <en>Returns the link target window name.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>编辑状态留在当前窗口；普通浏览状态在新窗口打开。</zh-CN>
        ///   <en>Edit mode stays in the current window; ordinary browse mode opens in a new window.</en>
        /// </l>
        /// </returns>
        protected string ChooseTarget()
        {
            // <lang>
            //   <zh-CN>目标窗口跟随 URL 语义：站内编辑页保持当前上下文，外部或内容链接避免替换门户页。</zh-CN>
            //   <en>The target follows URL semantics: the in-site edit page keeps the current context, while external or content links avoid replacing the portal page.</en>
            // </lang>
            return IsEditable ? "_self" : "_blank";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑状态展示固定提示，浏览状态展示原始描述；属性编码由 ASP.NET 输出层处理。</zh-CN>
        ///   <en>Uses a fixed hint while editing and the original description while browsing; ASP.NET output handling encodes the attribute value.</en>
        /// </lang>
        /// </summary>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>当前链接条目的描述文本。</zh-CN>
        ///   <en>The current link item's description text.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>编辑提示或原始描述文本。</zh-CN>
        ///   <en>The edit hint or original description text.</en>
        /// </l>
        /// </returns>
        protected string ChooseTip(object description)
        {
            // <lang>
            //   <zh-CN>编辑状态不暴露旧描述作为 tooltip，固定提示能清楚表达点击会进入编辑页。</zh-CN>
            //   <en>Edit mode does not expose the legacy description as the tooltip; the fixed hint clearly states the click opens editing.</en>
            // </lang>
            return IsEditable ? "Edit" : Convert.ToString(description);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回符合导航策略的浏览地址；非法旧值返回空字符串。</zh-CN>
        ///   <en>Returns a browse URL that passes navigation policy, or an empty string for an invalid legacy value.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自链接记录的候选 URL。</zh-CN>
        ///   <en>The candidate URL from the link row.</en>
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
            //   <zh-CN>normalizedUrl 只在导航策略成功时被读取，避免把旧不合规 URL 回显给浏览者。</zh-CN>
            //   <en>normalizedUrl is read only after navigation policy succeeds, avoiding echoing legacy non-compliant URLs to viewers.</en>
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
        ///   <zh-CN>来自链接记录的候选 URL。</zh-CN>
        ///   <en>The candidate URL from the link row.</en>
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
            //   <zh-CN>可见性与最终 href 共享同一规范化入口，避免链接容器和实际地址状态不一致。</zh-CN>
            //   <en>Visibility and final href share the same normalization entry so the link container and actual address cannot diverge.</en>
            // </lang>
            return !string.IsNullOrEmpty(GetSafeBrowseUrl(value));
        }
    }
}
