using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>显示当前模块的事件列表。</zh-CN>
    ///     <en>Renders the event list for the current module.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该控件沿用旧 Web Forms 数据绑定模型：从事件数据访问服务读取当前模块未过期事件，再交给标记层
    ///       <c>DataList</c> 展示。事件标题、地点时间、描述和 URL 的最终输出仍依赖标记层编码与导航策略。
    ///     </zh-CN>
    ///     <en>
    ///       This control keeps the legacy Web Forms data-binding model: it reads non-expired events for the current
    ///       module from the event data access service and passes them to the markup <c>DataList</c>. Final output of
    ///       event title, time/place text, description, and URL still depends on markup encoding and navigation policy.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public partial class Events : PortalModuleControl<Events>
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>事件数据访问服务。</zh-CN>
        ///     <en>Event data-access service.</en>
        ///   </lang>
        /// </summary>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>由 Unity 注入；调用方不在控件内创建数据库连接或切换数据源。</zh-CN>
        ///     <en>Injected by Unity; the control does not create database connections or switch data sources directly.</en>
        ///   </lang>
        /// </remarks>
        [Dependency]
        public IEventsDb EventsDB { private get; set; }

        /// <summary>
        ///   <lang>
        ///     <zh-CN>读取并绑定当前模块的未过期事件。</zh-CN>
        ///     <en>Reads and binds non-expired events for the current module.</en>
        ///   </lang>
        /// </summary>
        /// <param name="sender">
        ///   <l>
        ///     <zh-CN>触发页面加载的 Web Forms 对象。</zh-CN>
        ///     <en>Web Forms object that raised the load event.</en>
        ///   </l>
        /// </param>
        /// <param name="e">
        ///   <l>
        ///     <zh-CN>页面加载事件参数。</zh-CN>
        ///     <en>Page load event arguments.</en>
        ///   </l>
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>
        ///       当前实现每次加载都重新绑定，保持旧模块对编辑后刷新和缓存策略的行为。数据访问层负责按模块归属和过期时间筛选；
        ///       控件层不直接拼接 SQL，也不在这里判定编辑权限。
        ///     </zh-CN>
        ///     <en>
        ///       The current implementation rebinds on every load to preserve the legacy module behavior after edits
        ///       and cache decisions. The data access layer filters by module ownership and expiry time; this control
        ///       does not concatenate SQL or decide edit permission here.
        ///     </en>
        ///   </lang>
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>事件查询限制在当前模块实例，由数据访问层按旧规则过滤过期事件。</zh-CN>
            //   <en>The event query is scoped to the current module instance, and the data-access layer applies legacy expiry filtering.</en>
            // </lang>
            myDataList.DataSource = EventsDB.GetEvents(ModuleId);
            // <lang>
            //   <zh-CN>立即绑定保持旧模块“编辑后回到首页即刷新”的行为，标记层随后负责字段输出。</zh-CN>
            //   <en>Immediate binding preserves the legacy module behavior where returning to the home page after editing refreshes the list, and markup then owns field output.</en>
            // </lang>
            myDataList.DataBind();
        }
    }
}
