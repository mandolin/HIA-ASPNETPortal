<%@ Page
    Language="c#"
    CodeBehind="CapabilityPermissionMatrix.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.CapabilityPermissionMatrix"
    MasterPageFile="~/Default.master" %>

<%@ Import Namespace="ASPNET.StarterKit.Portal" %>
<%@ Import Namespace="Resources" %>

<%--
<lang>
  <zh-CN>P45.5 只读能力权限矩阵页：按"能力层 → 权限分类 → 权限键"呈现角色与权限键的映射现状，不提供在线赋权、映射写入或角色编辑能力。矩阵 HTML 由 PortalCapabilityPermissionMatrixRenderer 生成，页面只负责取数与权限门禁。</zh-CN>
  <en>The P45.5 read-only capability permission matrix page presents the current role-to-permission-key mappings by capability layer, permission category, and permission key. It offers no online granting, mapping write, or role editing; the matrix HTML comes from PortalCapabilityPermissionMatrixRenderer while the page only fetches data and enforces its permission gate.</en>
</lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="portal-admin-page portal-admin-capability-matrix">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title"><%= lang.Admin_CapabilityPermissionMatrix_Title %></h1>
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_CapabilityPermissionMatrix_Subtitle %></p>
            </div>
            <%= PortalNavigationEntryRenderer.RenderActions("Admin.Capability.PermissionMatrix", Context) %>
        </div>

        <%--
        <lang>
          <zh-CN>图例必须与矩阵单元格语义同时出现：三态为"已授权 / 未授权 / 已禁用（映射存在但未生效）"，避免维护者把"已禁用"误读为"未授权"。</zh-CN>
          <en>The legend must appear together with the matrix cell semantics: the three states are granted, not granted, and disabled (the mapping exists but is not effective), so a maintainer cannot misread "disabled" as "not granted".</en>
        </lang>
        --%>
        <p class="Normal portal-capability-matrix-legend"><%= lang.Admin_CapabilityPermissionMatrix_Legend %></p>

        <%--
        <lang>
          <zh-CN>矩阵区域只承载渲染器输出：PassThrough 不会再次编码，渲染器已对全部文本与属性做 HTML 编码。</zh-CN>
          <en>The matrix area carries renderer output only: PassThrough does not re-encode, and the renderer already HTML-encodes every text node and attribute.</en>
        </lang>
        --%>
        <asp:Literal ID="MatrixLiteral" runat="server" Mode="PassThrough" />

        <asp:Label ID="UnavailableLabel" runat="server" Visible="false" CssClass="Normal portal-matrix-unavailable" />
    </div>
</asp:Content>
