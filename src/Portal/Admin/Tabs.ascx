<%@ Control Inherits="ASPNET.StarterKit.Portal.Tabs" CodeBehind="Tabs.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<ASPNETPortal:Title runat="server" ID="Title1" />

<%-- 中文 / English: 旧 Tab 管理控件保留真实排序、删除和创建行为；截图矩阵只打开页面，不点击这些写库按钮。 --%>
<div class="portal-admin-page portal-legacy-admin-module portal-legacy-tabs">
    <div class="portal-admin-header">
        <div class="portal-admin-heading">
            <h2 class="Head portal-admin-title">Legacy Tab Administration</h2>
            <p class="Normal portal-admin-subtitle">Manage tab order and enter the modern tab layout editor.</p>
        </div>
        <div class="portal-admin-actions">
            <asp:LinkButton
                ID="addBtn"
                CssClass="portal-button portal-button-primary"
                Text="Add New Tab"
                CausesValidation="False"
                OnClick="AddTab_Click"
                runat="server" />
        </div>
    </div>

    <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

    <div class="portal-admin-section">
        <div class="portal-section-header">
            <h3 class="Head portal-section-title">Portal Tabs</h3>
        </div>
        <div class="portal-form-grid portal-legacy-tab-grid">
            <div class="portal-form-field portal-form-field-wide">
                <span class="SubHead portal-form-label">Tabs</span>
                <asp:ListBox
                    ID="tabList"
                    CssClass="NormalTextBox portal-form-input portal-legacy-listbox"
                    DataSource="<%# PortalTabs %>"
                    DataTextField="TabName"
                    DataValueField="TabId"
                    Rows="8"
                    runat="server" />
            </div>
            <div class="portal-form-field portal-form-actions-field">
                <span class="SubHead portal-form-label">Actions</span>
                <div class="portal-action-row portal-legacy-action-stack">
                    <asp:LinkButton
                        ID="upBtn"
                        CssClass="portal-button portal-button-secondary portal-button-compact"
                        Text="Move Up"
                        CommandName="up"
                        CausesValidation="False"
                        OnClick="UpDown_Click"
                        runat="server" />
                    <asp:LinkButton
                        ID="downBtn"
                        CssClass="portal-button portal-button-secondary portal-button-compact"
                        Text="Move Down"
                        CommandName="down"
                        CausesValidation="False"
                        OnClick="UpDown_Click"
                        runat="server" />
                    <asp:LinkButton
                        ID="editBtn"
                        CssClass="portal-button portal-button-primary portal-button-compact"
                        Text="Edit Layout"
                        CausesValidation="False"
                        OnClick="EditBtn_Click"
                        runat="server" />
                    <asp:LinkButton
                        ID="deleteBtn"
                        CssClass="portal-button portal-button-danger portal-button-compact"
                        Text="Delete Selected Tab"
                        CausesValidation="False"
                        OnClick="DeleteBtn_Click"
                        runat="server" />
                </div>
                <p class="Normal portal-status-line">Delete removes the selected non-core tab and its module instances. Core Admin tab remains protected.</p>
            </div>
        </div>
    </div>
</div>
