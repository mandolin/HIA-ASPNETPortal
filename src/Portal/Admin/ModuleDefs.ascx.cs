using System;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 用户控件用于管理模块定义。
    /// </summary>
    public partial class ModuleDefs : PortalModuleControl<ModuleDefs>
    {
        private int tabId;
        private int tabIndex;

        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// 页面加载时初始化模块定义。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权访问此页面
            if (!PortalSecurity.IsInRoles("Admins"))
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // 获取请求参数中的 tabid 和 tabindex
            if (Request.Params["tabid"] != null)
            {
                tabId = int.Parse(Request.Params["tabid"]);
            }
            if (Request.Params["tabindex"] != null)
            {
                tabIndex = int.Parse(Request.Params["tabindex"]);
            }

            // 如果这不是回发请求，则绑定数据到 DataList 控件
            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 添加新的模块定义时的事件处理程序。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void AddDef_Click(Object Sender, EventArgs e)
        {
            // 重定向到编辑页面
            Response.Redirect("~/Admin/ModuleDefinitions.aspx?defId=-1&tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 处理用户从 DataList 控件编辑模块定义的命令。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void DefsList_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            // 获取当前项的模块定义 ID
            var moduleDefId = (int)defsList.DataKeys[e.Item.ItemIndex];

            // 重定向到编辑页面
            Response.Redirect("~/Admin/ModuleDefinitions.aspx?defId=" + moduleDefId + "&tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 绑定模块定义列表到 DataList 控件。
        /// </summary>
        private void BindData()
        {
            // 从当前上下文中获取 PortalSettings
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // 从数据库中获取门户的模块定义
            defsList.DataSource = ModuleDefConfig.GetModuleDefinitions();
            defsList.DataBind();
        }
    }
}