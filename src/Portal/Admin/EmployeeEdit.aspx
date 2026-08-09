<%@ Page
    Language="c#"
    CodeBehind="EmployeeEdit.aspx.cs"
    AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.EmployeeEdit"
    MasterPageFile="~/Default.master" %>

<%--
  <lang>
    <zh-CN>P6.3-S4 员工主数据最小维护页不提供账号绑定、工号登录启用、导入、导出或敏感个人资料字段。</zh-CN>
    <en>The P6.3-S4 minimal employee master-data page does not provide account binding, employee-code login enablement, import, export, or sensitive personal-profile fields.</en>
  </lang>
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>员工编辑页只重构展示壳，字段验证、保存和审计仍由 code-behind 处理。</zh-CN>
        <en>The employee edit page only rebuilds the presentation shell; field validation, saving, and audit writing remain handled by code-behind.</en>
      </lang>
    --%>
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
        <%--
          <lang>
            <zh-CN>EmployeeIdField 和 OriginalUpdatedUtcField 只是回发状态载体，客户端可以修改；实体身份和并发版本必须由 code-behind 重新解析与校验。</zh-CN>
            <en>EmployeeIdField and OriginalUpdatedUtcField are postback state carriers that the client can modify; code-behind must resolve and validate entity identity and concurrency version again.</en>
          </lang>
        --%>
        <asp:HiddenField ID="EmployeeIdField" runat="server" />
        <asp:HiddenField ID="OriginalUpdatedUtcField" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Employee Profile</h2>
            </div>
            <%--
              <lang>
                <zh-CN>员工资料控件的 MaxLength 只是输入上限；员工代码唯一性、邮箱格式、组织/状态组合、日期关系和来源系统策略仍由服务端校验。</zh-CN>
                <en>MaxLength on employee fields is only an input ceiling; the server still validates code uniqueness, email format, organization/status combinations, date relationships, and source-system policy.</en>
              </lang>
            --%>
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
            <%--
              <lang>
                <zh-CN>保存按钮只触发服务端员工资料更新；并发版本、授权、审计和失败回退不由标记层或取消链接决定。</zh-CN>
                <en>The save button only triggers the server-side employee update; concurrency version, authorization, audit, and failure fallback are not decided by markup or the cancel link.</en>
              </lang>
            --%>
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
