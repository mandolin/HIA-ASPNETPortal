<%@ Page CodeBehind="ModuleSettings.aspx.cs" Language="c#" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.ModuleSettingsPage" MasterPageFile="~/Default.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 模块实例设置页只重构展示壳，实例归属校验、保存和审计仍由 code-behind 处理。 --%>
    <div class="portal-admin-page portal-admin-module-settings">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <h1 class="Head portal-admin-title">Module Settings</h1>
                <p class="Normal portal-admin-subtitle">Edit a module instance title, cache policy, editor roles, and legacy mobile visibility.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="ModuleCatalog.aspx">Module Catalog</a>
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
            </div>
        </div>

        <asp:Label ID="Message" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Instance Metadata</h2>
            </div>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Module Name</span>
                    <asp:TextBox ID="moduleTitle" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Cache Timeout (seconds)</span>
                    <asp:TextBox ID="cacheTime" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field portal-checkbox-field">
                    <span class="SubHead portal-form-label">Mobile Compatibility</span>
                    <asp:CheckBox ID="showMobile" Text="Show to mobile users" runat="server" />
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Editor Roles</h2>
            </div>
            <div class="portal-chip-list-wrap">
                <asp:CheckBoxList ID="authEditRoles" RepeatColumns="2"
                    CssClass="portal-chip-list" CellPadding="0" CellSpacing="0" runat="server" />
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton CssClass="CommandButton portal-primary-action" Text="Apply Module Changes" runat="server"
                    ID="ApplyButton" OnClick="ApplyChanges_Click" />
            </div>
        </div>
    </div>
</asp:Content>
