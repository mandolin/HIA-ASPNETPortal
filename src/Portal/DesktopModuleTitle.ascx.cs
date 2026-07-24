using System;
using System.Web;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>桌面模块标题栏控件，负责显示模块标题和可选编辑入口。</zh-CN>
    ///   <en>Desktop module header control that renders a module title and optional edit action.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P7.4 起标记从旧 table 标题栏切换为语义化容器；权限判断和编辑链接生成仍沿用旧模块配置。</zh-CN>
    ///   <en>Starting with P7.4, the markup changes from the legacy table title bar to semantic containers, while permission checks and edit-link generation continue to use the legacy module configuration. P8.3 further separates the title and action areas so themes can style module actions consistently.</en>
    /// </lang>
    /// </remarks>
    public partial class DesktopModuleTitle : UserControl
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑页面打开目标窗口；为空时使用浏览器默认行为。</zh-CN>
        ///   <en>Target window for the edit page; when empty, the browser default behavior is used.</en>
        /// </lang>
        /// </summary>
        public string EditTarget;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户可编辑模块时显示的编辑入口文本。</zh-CN>
        ///   <en>Edit-action text shown when the current user can edit the module.</en>
        /// </lang>
        /// </summary>
        public string EditText;

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块编辑页面的相对 URL，不包含当前模块 ID 查询参数。</zh-CN>
        ///   <en>Relative URL for the module edit page, excluding the current module-id query parameter.</en>
        /// </lang>
        /// </summary>
        public string EditUrl;

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据父模块配置写入标题，并在用户具备编辑权限时显示编辑入口。</zh-CN>
        ///   <en>Writes the title from the parent module configuration and shows the edit action when the user has permission.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>PortalSettings 由页面生命周期预先放入当前请求上下文；标题控件只读取快照，不主动重新加载门户配置。</zh-CN>
            //   <en>PortalSettings is prepared earlier in the page lifecycle and stored in the current request context; the title control reads that snapshot instead of reloading Portal configuration.</en>
            // </lang>
            var portalSettings = PortalContext.GetPortalSettings();

            // <lang>
            //   <zh-CN>标题控件必须挂在模块控件下方，才能读取模块标题、模块 ID 和编辑角色；这是旧 Web Forms 模块容器契约的一部分。</zh-CN>
            //   <en>The title control must be hosted under a module control so it can read the module title, module id, and edit roles; this is part of the legacy Web Forms module-container contract.</en>
            // </lang>
            var portalModule = (IPortalModuleControl) Parent;

            // <lang>
            //   <zh-CN>每次加载都按当前模块配置重写标题，并默认隐藏动作区；后续条件满足时再显式打开编辑入口。</zh-CN>
            //   <en>Each load writes the title from the current module configuration and hides the action area by default; the edit action is then explicitly enabled only when all conditions pass.</en>
            // </lang>
            ModuleTitle.Text = portalModule.ModuleConfiguration.ModuleTitle;
            ModuleActions.Visible = false;
            EditButton.Visible = false;

            // <lang>
            //   <zh-CN>编辑入口同时受控件配置、全局强制显示开关和模块编辑角色约束；没有实际文本时也必须隐藏，避免 P7 主题渲染空按钮。</zh-CN>
            //   <en>The edit action is constrained by control configuration, the global always-show switch, and module edit roles; it also stays hidden without text so P7 themes do not render empty buttons.</en>
            // </lang>
            if (!string.IsNullOrWhiteSpace(EditText) &&
                (portalSettings.AlwaysShowEditButton ||
                 PortalSecurity.IsInRoles(portalModule.ModuleConfiguration.AuthorizedEditRoles)))
            {
                // <lang>
                //   <zh-CN>旧模块编辑页通过 <c>mid</c> 查询参数定位模块实例；标题栏只追加当前模块 ID，不额外推断返回地址。</zh-CN>
                //   <en>Legacy module edit pages locate the module instance through the <c>mid</c> query parameter; the title bar only appends the current module id and does not infer return URLs.</en>
                // </lang>
                EditButton.Text = EditText;
                EditButton.NavigateUrl = EditUrl + "?mid=" + portalModule.ModuleId;
                EditButton.Target = EditTarget;
                EditButton.ToolTip = "Open module action: " + EditText;
                EditButton.Visible = true;
                ModuleActions.Visible = true;
            }
        }
    }
}
