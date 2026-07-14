using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示事件列表。
    ///
    /// English: Renders the event list.
    /// </summary>
    public partial class Events : PortalModuleControl<Events>
    {
        /// <summary>
        /// 中文：事件数据访问服务。English: Event data-access service.
        /// </summary>
        [Dependency]
        public IEventsDb EventsDB { private get; set; }

        /// <summary>
        /// 中文：读取并绑定当前模块的未过期事件。English: Reads and binds non-expired events for the current module.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataList.DataSource = EventsDB.GetEvents(ModuleId);
            myDataList.DataBind();
        }
    }
}
