<%@ Page
    Language="c#"
    CodeBehind="OrganizationUnitEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.OrganizationUnitEdit"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S4 组织单元最小维护页：不提供硬删除、导入、导出或批量同步。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 组织单元编辑页只重构展示壳，父级校验、保存和审计仍由 code-behind 处理。 --%>
    <div class="portal-admin-page portal-admin-organization-edit">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <asp:Label ID="TitleLabel" CssClass="Head portal-admin-title" runat="server" />
                <p class="Normal portal-admin-subtitle">Maintain organization units used by employee directory grouping and profile workflows.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                <a class="CommandButton" href="EmployeeEdit.aspx">New Employee</a>
                <a class="CommandButton" href="SystemHealth.aspx">System Health</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />
        <asp:HiddenField ID="OrganizationUnitIdField" runat="server" />
        <asp:HiddenField ID="OriginalUpdatedUtcField" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Organization Metadata</h2>
            </div>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Organization Code</span>
                    <asp:TextBox ID="OrganizationCodeTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="100" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Display Name</span>
                    <asp:TextBox ID="DisplayNameTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="150" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Parent</span>
                    <asp:DropDownList ID="ParentOrganizationList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Sort Order</span>
                    <asp:TextBox ID="SortOrderTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="10" runat="server" />
                </div>
                <div class="portal-form-field portal-checkbox-field">
                    <span class="SubHead portal-form-label">Active</span>
                    <asp:CheckBox ID="IsActiveCheckBox" Text="Enable this organization unit" runat="server" />
                </div>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton
                    ID="SaveButton"
                    CssClass="CommandButton portal-primary-action"
                    Text="Save"
                    CausesValidation="False"
                    OnClick="SaveButton_Click"
                    runat="server" />
                <a class="CommandButton" href="EmployeeDirectory.aspx">Cancel</a>
            </div>
        </div>
    </div>
</asp:Content>
