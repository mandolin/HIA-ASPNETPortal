using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示公告列表，并仅为符合地址策略的“查看更多”链接生成可点击地址。
    ///
    /// English: Renders announcements and creates clickable Read More URLs only when they pass the navigation policy.
    /// </summary>
    public partial class Announcements : PortalModuleControl<Announcements>
    {
        /// <summary>
        /// 中文：公告数据访问服务。English: Announcement data-access service.
        /// </summary>
        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; }

        /// <summary>
        /// 中文：读取并绑定当前模块的有效公告。English: Reads and binds active announcements for the current module.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataList.DataSource = AnnouncementsDB.GetAnnouncements(ModuleId);
            myDataList.DataBind();
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
