<%@ Page
    Language="c#"
    CodeBehind="EmployeeEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeEdit"
    MasterPageFile="~/Default.master" %>

<%-- P6.3-S4 员工主数据最小维护页：不提供账号绑定、工号登录启用、导入、导出或敏感个人资料字段。 --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 员工编辑页只重构展示壳，字段验证、保存和审计仍由 code-behind 处理。 --%>
    <div class="portal-admin-page portal-admin-employee-edit">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <asp:Label ID="TitleLabel" CssClass="Head portal-admin-title" runat="server" />
                <p class="Normal portal-admin-subtitle">Maintain employee master data used by directory, binding, and profile workflows.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                <a class="CommandButton" href="OrganizationUnitEdit.aspx">New Organization Unit</a>
                <a class="CommandButton" href="UserEmployeeBindingEdit.aspx">Bind User/Employee</a>
            </div>
        </div>

        <asp:Label ID="MessageLabel" CssClass="NormalRed portal-status-line" EnableViewState="false" runat="server" />
        <asp:HiddenField ID="EmployeeIdField" runat="server" />
        <asp:HiddenField ID="OriginalUpdatedUtcField" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Employee Profile</h2>
            </div>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Employee Code</span>
                    <asp:TextBox ID="EmployeeCodeTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="64" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Display Name</span>
                    <asp:TextBox ID="DisplayNameTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="150" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Preferred Name</span>
                    <asp:TextBox ID="PreferredNameTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="100" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Work Email</span>
                    <asp:TextBox ID="WorkEmailTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="256" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Organization</span>
                    <asp:DropDownList ID="OrganizationUnitList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Status</span>
                    <asp:DropDownList ID="EmploymentStatusList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Joined UTC</span>
                    <asp:TextBox ID="JoinedUtcTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="25" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Left UTC</span>
                    <asp:TextBox ID="LeftUtcTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="25" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">Source System</span>
                    <asp:TextBox ID="SourceSystemTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="80" runat="server" />
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
