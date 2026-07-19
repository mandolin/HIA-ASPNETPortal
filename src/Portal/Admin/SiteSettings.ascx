<%@ Control Inherits="ASPNET.StarterKit.Portal.SiteSettings" CodeBehind="SiteSettings.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id=Title1 />

<%-- 中文 / English: 站点基础设置仍由旧控件保存；系统设置总 UI 覆盖前先完成视觉兼容治理。 --%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-site-settings">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Site Settings</h2>
            <p class="Normal portal-admin-subtitle">Maintain portal title and legacy edit-button visibility.</p>
        </div>
    </div>

    <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

    <div class="portal-admin-section">
        <div class="portal-section-header">
            <h3 class="Head portal-section-title">Portal Metadata</h3>
        </div>
        <div class="portal-form-grid">
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
