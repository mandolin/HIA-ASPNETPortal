<%@ Control Inherits="ASPNET.StarterKit.Portal.SiteSettings" CodeBehind="SiteSettings.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id=Title1 />

<%--
<lang>
  <zh-CN>站点基础设置仍由旧控件保存门户标题和编辑按钮显示策略；系统设置总 UI 覆盖前，本控件先完成后台视觉兼容治理。</zh-CN>
  <en>Basic site settings still use the legacy control to persist the portal title and edit-button visibility policy; before the unified system-settings UI takes over, this control only receives admin-visual compatibility treatment.</en>
</lang>
--%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-site-settings">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Site Settings</h2>
            <p class="Normal portal-admin-subtitle">Maintain portal title and legacy edit-button visibility.</p>
        </div>
    </div>

    <%--
    <lang>
      <zh-CN>状态消息由服务器写入，用于反馈站点设置保存结果；页面不把客户端文本当作成功证明。</zh-CN>
      <en>The server writes the status message to report site-settings persistence; the page does not treat client text as proof of success.</en>
    </lang>
    --%>
    <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

    <div class="portal-admin-section">
        <div class="portal-section-header">
            <h3 class="Head portal-section-title">Portal Metadata</h3>
        </div>
        <div class="portal-form-grid">
            <%--
            <lang>
              <zh-CN>站点标题和编辑按钮可见性组成旧门户元数据输入；规范化、权限和兼容策略仍由 code-behind 决定。</zh-CN>
              <en>Site title and edit-button visibility form the legacy portal metadata input; normalization, authorization, and compatibility policy remain decided by the code-behind.</en>
            </lang>
            --%>
            <div class="portal-form-field">
                <span class="SubHead portal-form-label">Site Title</span>
                <asp:TextBox ID="SiteName" CssClass="NormalTextBox portal-form-input" MaxLength="150" runat="server" />
            </div>
            <div class="portal-form-field portal-checkbox-field">
                <span class="SubHead portal-form-label">Edit Button Visibility</span>
                <asp:CheckBox ID="showEdit" Text="Always show edit button" runat="server" />
            </div>
            <div class="portal-form-field portal-form-actions-field">
                <span class="SubHead portal-form-label">&nbsp;</span>
                <%--
                <lang>
                  <zh-CN>Apply_Click 负责将设置提交到既有持久化流程；标记层只提供命令入口，不实现保存或回滚。</zh-CN>
                  <en>Apply_Click submits settings to the existing persistence flow; the markup provides only the command entry and does not implement saving or rollback.</en>
                </lang>
                --%>
                <asp:LinkButton
                    ID="applyBtn"
                    CssClass="portal-button portal-button-primary"
                    Text="Apply Changes"
                    OnClick="Apply_Click"
                    runat="server" />
            </div>
        </div>
    </div>
</div>
