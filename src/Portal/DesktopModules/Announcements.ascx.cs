using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示公告列表，并仅为符合地址策略的“查看更多”链接生成可点击地址。</zh-CN>
    ///   <en>Renders announcements and creates clickable Read More URLs only when they pass the navigation policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>公告正文由标记层控件负责编码显示；本控件只在 URL 输出处追加站内/受信任地址策略，避免旧记录把危险地址重新渲染成链接。</zh-CN>
    ///   <en>Announcement text is encoded by the markup controls; this control only applies the in-site/trusted URL policy at URL output so legacy records cannot render unsafe addresses as links.</en>
    /// </lang>
    /// </remarks>
    public partial class Announcements : PortalModuleControl<Announcements>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>公告数据访问服务。</zh-CN>
        ///   <en>Announcement data-access service.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>由 Unity 注入，页面生命周期中只读取当前模块的有效公告，不在展示控件内直接拼接 SQL。</zh-CN>
        ///   <en>Injected by Unity; during the page lifecycle it only reads active announcements for the current module and does not compose SQL in the display control.</en>
        /// </lang>
        /// </remarks>
        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并绑定当前模块的有效公告。</zh-CN>
        ///   <en>Reads and binds active announcements for the current module.</en>
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
        ///   <zh-CN>页面加载事件参数。</zh-CN>
        ///   <en>The page-load event arguments.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataList.DataSource = AnnouncementsDB.GetAnnouncements(ModuleId);
            myDataList.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回符合导航策略的浏览地址；非法历史值返回空字符串。</zh-CN>
        ///   <en>Returns a browse URL that passes navigation policy, or an empty string for an invalid historical value.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自旧公告记录的可选更多链接值。</zh-CN>
        ///   <en>The optional read-more link value from a legacy announcement record.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可安全渲染的浏览地址；不符合策略时为空字符串。</zh-CN>
        ///   <en>A safely renderable browse URL, or an empty string when the value fails policy.</en>
        /// </l>
        /// </returns>
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
        /// <param name="value">
        /// <l>
        ///   <zh-CN>来自数据绑定项的候选浏览地址。</zh-CN>
        ///   <en>The candidate browse URL from the data-bound item.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>地址通过导航策略并且不为空时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the URL passes navigation policy and is not empty.</en>
        /// </l>
        /// </returns>
        protected bool HasSafeBrowseUrl(object value)
        {
            return !string.IsNullOrEmpty(GetSafeBrowseUrl(value));
        }
    }
}
