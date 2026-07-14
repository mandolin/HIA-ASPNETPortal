using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：展示既有模块定义并进入受信任部署模块目录的后台控件。
    ///
    /// English: Administration control that displays legacy module definitions and enters the trusted deployment
    /// module catalog.
    /// </summary>
    public partial class ModuleDefs : PortalModuleControl<ModuleDefs>
    {
        private int tabId;
        private int tabIndex;

        /// <summary>
        /// 中文：旧模块定义数据访问依赖。
        ///
        /// English: Legacy module-definition data-access dependency.
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// 中文：授权并读取可选后台导航参数，在首次请求绑定既有定义。
        ///
        /// English: Authorizes and reads optional administration navigation parameters, then binds existing definitions
        /// on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context) || !TryReadNavigationParameters())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 中文：打开受信任部署模块目录；不恢复在线手填模块路径。
        ///
        /// English: Opens the trusted deployment module catalog without restoring online entry of module paths.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void AddDef_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context) || !TryReadNavigationParameters())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/Admin/ModuleCatalog.aspx"));
        }

        /// <summary>
        /// 中文：进入当前选择的既有模块定义编辑页。
        ///
        /// English: Opens the editing page for the currently selected legacy module definition.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：包含 DataList 项索引的事件数据。English: Event data containing a DataList item index.</param>
        protected void DefsList_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context) || !TryReadNavigationParameters())
            {
                return;
            }

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

        private bool TryReadNavigationParameters()
        {
            return TryReadOptionalPositiveParameter("tabid", out tabId) &&
                   TryReadOptionalNonNegativeParameter("tabindex", out tabIndex);
        }

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

        private void BindData()
        {
            defsList.DataSource = ModuleDefConfig.GetModuleDefinitions();
            defsList.DataBind();
        }
    }
}
