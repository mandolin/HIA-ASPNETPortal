using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示链接列表，并隔离旧记录中不再符合地址策略的链接。
    ///
    /// English: Renders the link list and isolates legacy links that no longer pass URL policy.
    /// </summary>
    public partial class Links : PortalModuleControl<Links>
    {
        /// <summary>
        /// 中文：链接数据访问服务。English: Link data-access service.
        /// </summary>
        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        /// <summary>
        /// 中文：读取并绑定当前模块链接。English: Reads and binds links for the current module.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataList.DataSource = LinkDB.GetLinks(ModuleId);
            myDataList.DataBind();
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
        /// 中文：返回链接目标窗口名称。English: Returns the link target window name.
        /// </summary>
        protected string ChooseTarget()
        {
            return IsEditable ? "_self" : "_blank";
        }

        /// <summary>
        /// 中文：编辑状态展示固定提示，浏览状态展示原始描述；属性编码由 ASP.NET 输出层处理。
        ///
        /// English: Uses a fixed hint while editing and the original description while browsing; ASP.NET output handling encodes the attribute value.
        /// </summary>
        protected string ChooseTip(object description)
        {
            return IsEditable ? "Edit" : Convert.ToString(description);
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
