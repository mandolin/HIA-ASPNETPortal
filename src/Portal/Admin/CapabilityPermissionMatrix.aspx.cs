using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>只读能力权限矩阵页面（P45.5 Step4）。</zh-CN>
    ///   <en>Read-only capability permission matrix page (P45.5 Step4).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面职责只有三件：按 `EnterpriseCapability.View` 做权限门禁、从只读数据源取"权限定义 + 角色映射"、把矩阵交给渲染器输出。它**不写数据库、不改配置、不提供赋权入口**，映射表缺失时降级为提示文案而不抛异常。</zh-CN>
    ///   <en>The page has exactly three responsibilities: enforce the `EnterpriseCapability.View` gate, read definitions and role mappings from read-only sources, and hand the matrix to the renderer. It writes no database row, changes no configuration, offers no granting entry point, and degrades to a hint message (instead of throwing) when the mapping table is missing.</en>
    /// </lang>
    /// </remarks>
    public partial class CapabilityPermissionMatrix : PortalPage<CapabilityPermissionMatrix>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>加载页面：先复核查看权限，再在首次请求渲染矩阵。</zh-CN>
        ///   <en>Loads the page: recheck the view permission first, then render the matrix on the initial request.</en>
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
        ///   <en>Page-load event arguments.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>每次生命周期入口复核能力治理查看权限；拒绝时不取数、不渲染，拒绝出口沿用既有集中重定向。</zh-CN>
            //   <en>Recheck the capability-governance view permission on every lifecycle entry; on denial, fetch nothing and render nothing, reusing the existing centralized denial exit.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EnterpriseCapabilityView))
            {
                return;
            }

            // <lang>
            //   <zh-CN>页面为纯只读视图、无回发动作；仅在首次请求渲染，避免无意义的重算。</zh-CN>
            //   <en>The page is a pure read-only view with no postback action, so it renders only on the initial request to avoid pointless recomputation.</en>
            // </lang>
            if (IsPostBack)
            {
                return;
            }

            RenderMatrix();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取只读数据并渲染矩阵；映射表不可用时显示提示文案。</zh-CN>
        ///   <en>Fetches read-only data and renders the matrix, showing a hint when the mapping table is unavailable.</en>
        /// </lang>
        /// </summary>
        private void RenderMatrix()
        {
            // <lang>
            //   <zh-CN>角色数据门面从受控容器解析；容器缺失时按"无映射"处理，不伪造任何授权事实。</zh-CN>
            //   <en>Resolve the role-data facade from the controlled container; when it is unavailable, treat the state as "no mapping" and never fabricate an authorization fact.</en>
            // </lang>
            IRolesDb rolesDb = ResolveRolesDb();

            // <lang>
            //   <zh-CN>映射集合只来自只读查询，保持页面无写入路径；查询失败已由数据层降级为空集合。</zh-CN>
            //   <en>Mappings come from the read-only query only, keeping the page free of any write path; query failures are already degraded to an empty set by the data layer.</en>
            // </lang>
            List<RolePermissionEntry> grants = rolesDb == null
                ? new List<RolePermissionEntry>()
                : rolesDb.GetRolePermissionEntries().Where(entry => entry != null).ToList();

            // <lang>
            //   <zh-CN>无映射（或表不可用）时给出显式提示：这既可能是"尚未配置"，也可能是"表未部署"，两者都需要维护者知情。</zh-CN>
            //   <en>Show an explicit hint when there is no mapping (or the table is unavailable): the cause may be "nothing configured yet" or "table not deployed", and a maintainer needs to know either way.</en>
            // </lang>
            if (grants.Count == 0)
            {
                UnavailableLabel.Text = lang.Admin_CapabilityPermissionMatrix_Unavailable;
                UnavailableLabel.Visible = true;
            }

            // <lang>
            //   <zh-CN>矩阵内容仍然渲染：即使没有映射，表头与"未授权"态可以让维护者确认权限定义是否已登记。</zh-CN>
            //   <en>The matrix is still rendered: even without mappings, the header and the not-granted state let a maintainer confirm whether the permission definitions are registered.</en>
            // </lang>
            MatrixLiteral.Text = PortalCapabilityPermissionMatrixRenderer.Render(
                PortalPermissionRegistry.Definitions,
                grants,
                ResolveMatrixText);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把渲染器的文案键解析为本地化文本。</zh-CN>
        ///   <en>Resolves the renderer's copy keys into localized text.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>渲染器文案键（不含页面前缀）。</zh-CN>
        ///   <en>Renderer copy key without the page prefix.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>本地化文本；未知键原样返回，保证渲染器不因文案缺失而失败。</zh-CN>
        ///   <en>Localized text; unknown keys are returned unchanged so the renderer never fails because of missing copy.</en>
        /// </l>
        /// </returns>
        private string ResolveMatrixText(string key)
        {
            // <lang>
            //   <zh-CN>白名单式映射：只解析渲染器实际使用的键，未知键原样返回（渲染器已对其做 HTML 编码）。</zh-CN>
            //   <en>Whitelist mapping: only the keys the renderer actually uses are resolved, and unknown keys pass through unchanged (the renderer already HTML-encodes them).</en>
            // </lang>
            switch (key)
            {
                case "EmptyDefinitions":
                    return lang.Admin_CapabilityPermissionMatrix_EmptyDefinitions;
                case "ColumnPermissionKey":
                    return lang.Admin_CapabilityPermissionMatrix_ColumnPermissionKey;
                case "LayerFoundation":
                    return lang.Admin_CapabilityPermissionMatrix_LayerFoundation;
                case "LayerBasicBusiness":
                    return lang.Admin_CapabilityPermissionMatrix_LayerBasicBusiness;
                case "LayerPlatform":
                    return lang.Admin_CapabilityPermissionMatrix_LayerPlatform;
                case "MarkGranted":
                    return lang.Admin_CapabilityPermissionMatrix_MarkGranted;
                case "MarkNotGranted":
                    return lang.Admin_CapabilityPermissionMatrix_MarkNotGranted;
                case "MarkDisabled":
                    return lang.Admin_CapabilityPermissionMatrix_MarkDisabled;
                case "StateGranted":
                    return lang.Admin_CapabilityPermissionMatrix_StateGranted;
                case "StateNotGranted":
                    return lang.Admin_CapabilityPermissionMatrix_StateNotGranted;
                case "StateDisabled":
                    return lang.Admin_CapabilityPermissionMatrix_StateDisabled;
                default:
                    return key;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从全局 Unity 容器解析角色数据门面；容器不可用时返回 <c>null</c>。</zh-CN>
        ///   <en>Resolves the role-data facade from the global Unity container and returns <c>null</c> when the container is unavailable.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色数据门面，或表示基础设施不可用的 <c>null</c>。</zh-CN>
        ///   <en>Role-data facade, or <c>null</c> when the infrastructure is unavailable.</en>
        /// </l>
        /// </returns>
        private static IRolesDb ResolveRolesDb()
        {
            if (Global.Container == null)
            {
                return null;
            }

            return Global.Container.Resolve<IRolesDb>();
        }
    }
}
