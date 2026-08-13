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
        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的模块定义编辑回跳 Tab 标识。</zh-CN>
        ///   <en>The optional Tab identifier preserved for module-definition edit return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的模块定义编辑回跳 Tab 索引。</zh-CN>
        ///   <en>The optional Tab index preserved for module-definition edit return navigation.</en>
        /// </lang>
        /// </summary>
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
            // <lang>
            //   <zh-CN>模块定义查看权限和导航参数是列表、目录入口及编辑入口的共同门禁。</zh-CN>
            //   <en>Definition-view permission and navigation parameters gate the list, catalog entry, and edit entry points.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleDefinitionEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求绑定既有定义，保留回发控件状态。</zh-CN>
            //   <en>Bind existing definitions only on the initial request, preserving postback control state.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>新增入口使用目录查看权限，并只导航到受信模块目录，不恢复手填路径创建。</zh-CN>
            //   <en>Use catalog-view permission and navigate only to the trusted module catalog without restoring hand-entered path creation.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>编辑命令再次验证权限、导航参数和 DataList 目标，避免仅凭行索引构造地址。</zh-CN>
            //   <en>Revalidate permission, navigation parameters, and the DataList target so a URL cannot be built from a row index alone.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleDefinitionEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            // <lang>
            //   <zh-CN>旧 DataList 只给出行索引，因此这里先把索引、DataKeys 和真实定义集合串起来复核，避免构造不存在的定义编辑地址。</zh-CN>
            //   <en>The legacy DataList provides only a row index, so this block cross-checks the index, DataKeys, and actual definition set before composing an edit URL.</en>
            // </lang>
            // <lang>
            //   <zh-CN>定义标识必须同时通过行边界、DataKeys 正整数和当前定义集合校验。</zh-CN>
            //   <en>The definition id must pass row bounds, DataKeys positive-integer parsing, and current-definition-set validation together.</en>
            // </lang>
            int moduleDefId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= defsList.DataKeys.Count ||
                !PortalNavigationPolicy.TryReadPositiveInt32(defsList.DataKeys[e.Item.ItemIndex].ToString(), out moduleDefId) ||
                !ModuleDefConfig.GetModuleDefinitions().Any(item => item.ModuleDefId == moduleDefId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            // <lang>
            //   <zh-CN>编辑地址只携带已验证定义和兼容导航参数，并交给安全导航策略。</zh-CN>
            //   <en>Carry only the verified definition and compatibility navigation parameters through the safe navigation policy.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>同时读取可选 Tab 标识和非负索引；任一非法输入都阻断后续操作。</zh-CN>
            //   <en>Read the optional Tab id and non-negative index together; any invalid input blocks subsequent operations.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>缺失参数保持兼容默认值 0；存在参数必须通过正整数策略。</zh-CN>
            //   <en>Keep the compatibility default of 0 when absent; a supplied value must pass positive-integer validation.</en>
            // </lang>
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法导航参数统一导向编辑拒绝页，不回显原始输入。</zh-CN>
            //   <en>Route invalid navigation input to edit-denied without echoing the raw value.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>索引允许零，但必须在参与返回 URL 前完成非负整数校验。</zh-CN>
            //   <en>The index permits zero but must be validated as non-negative before it participates in a return URL.</en>
            // </lang>
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法索引不降级为默认值，直接拒绝继续处理。</zh-CN>
            //   <en>Do not downgrade an invalid index to a default; reject further processing directly.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>从旧模块定义数据源绑定只读列表，不在控件绑定阶段执行写入。</zh-CN>
            //   <en>Bind the read-only list from the legacy definition source without performing writes during binding.</en>
            // </lang>
            defsList.DataSource = ModuleDefConfig.GetModuleDefinitions();
            defsList.DataBind();
        }
    }
}
