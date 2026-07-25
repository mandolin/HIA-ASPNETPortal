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
        ///   <zh-CN>返回链接目标窗口名称。</zh-CN>
        ///   <en>Returns the link target window name.</en>
        /// </lang>
        /// </summary>
        protected string ChooseTarget()
        {
            return IsEditable ? "_self" : "_blank";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑状态展示固定提示，浏览状态展示原始描述；属性编码由 ASP.NET 输出层处理。</zh-CN>
        ///   <en>Uses a fixed hint while editing and the original description while browsing; ASP.NET output handling encodes the attribute value.</en>
        /// </lang>
        /// </summary>
        protected string ChooseTip(object description)
        {
            return IsEditable ? "Edit" : Convert.ToString(description);
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
