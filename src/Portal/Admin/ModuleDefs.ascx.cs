using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>展示既有模块定义并进入受信任部署模块目录的后台控件。</zh-CN>
    ///   <en>Administration control that displays legacy module definitions and enters the trusted deployment module catalog.</en>
    /// </lang>
    /// </summary>
    public partial class ModuleDefs : PortalModuleControl<ModuleDefs>
    {
        private int tabId;
        private int tabIndex;

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧模块定义数据访问依赖。</zh-CN>
        ///   <en>Legacy module-definition data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权并读取可选后台导航参数，在首次请求绑定既有定义。</zh-CN>
        ///   <en>Authorizes and reads optional administration navigation parameters, then binds existing definitions on the initial request.</en>
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
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleDefinitionEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>打开受信任部署模块目录；不恢复在线手填模块路径。</zh-CN>
        ///   <en>Opens the trusted deployment module catalog without restoring online entry of module paths.</en>
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
        protected void AddDef_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleCatalogView) || !TryReadNavigationParameters())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/Admin/ModuleCatalog.aspx"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>进入当前选择的既有模块定义编辑页。</zh-CN>
        ///   <en>Opens the editing page for the currently selected legacy module definition.</en>
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
        ///   <zh-CN>包含 DataList 项索引的事件数据。</zh-CN>
        ///   <en>Event data containing a DataList item index.</en>
        /// </l>
        /// </param>
        protected void DefsList_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleDefinitionEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            // <lang>
            //   <zh-CN>旧 DataList 只给出行索引，因此这里先把索引、DataKeys 和真实定义集合串起来复核，避免构造不存在的定义编辑地址。</zh-CN>
            //   <en>The legacy DataList provides only a row index, so this block cross-checks the index, DataKeys, and actual definition set before composing an edit URL.</en>
            // </lang>
            int moduleDefId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= defsList.DataKeys.Count ||
                !PortalNavigationPolicy.TryReadPositiveInt32(defsList.DataKeys[e.Item.ItemIndex].ToString(), out moduleDefId) ||
                !ModuleDefConfig.GetModuleDefinitions().Any(item => item.ModuleDefId == moduleDefId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            string url = ResolveUrl(
                "~/Admin/ModuleDefinitions.aspx?defId=" + moduleDefId +
                "&tabindex=" + tabIndex +
                "&tabid=" + tabId);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, url);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台返回导航所需的可选 Tab 参数。</zh-CN>
        ///   <en>Reads optional Tab parameters used by administration return navigation.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或格式合法时返回 <c>true</c>；非法参数会重定向到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when parameters are missing or valid; invalid parameters redirect to edit access denied and return <c>false</c>.</en>
        /// </l>
        /// </returns>
        private bool TryReadNavigationParameters()
        {
            return TryReadOptionalPositiveParameter("tabid", out tabId) &&
                   TryReadOptionalNonNegativeParameter("tabindex", out tabIndex);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可缺省的正整数查询参数。</zh-CN>
        ///   <en>Reads an optional positive integer request parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>查询参数名。</zh-CN>
        ///   <en>Request parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>读取到的数值；参数缺省时保持为 <c>0</c>。</zh-CN>
        ///   <en>Read value; remains <c>0</c> when the parameter is omitted.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺省或合法时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is omitted or valid.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可缺省的非负整数查询参数。</zh-CN>
        ///   <en>Reads an optional non-negative integer request parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>查询参数名。</zh-CN>
        ///   <en>Request parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>读取到的数值；参数缺省时保持为 <c>0</c>。</zh-CN>
        ///   <en>Read value; remains <c>0</c> when the parameter is omitted.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺省或合法时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is omitted or valid.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前门户可见的旧模块定义清单。</zh-CN>
        ///   <en>Binds the list of legacy module definitions visible to the current Portal.</en>
        /// </lang>
        /// </summary>
        private void BindData()
        {
            defsList.DataSource = ModuleDefConfig.GetModuleDefinitions();
            defsList.DataBind();
        }
    }
}
