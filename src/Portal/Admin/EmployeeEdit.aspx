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
                <p class="Normal portal-admin-subtitle"><%= lang.Admin_EmployeeEdit_Subtitle %></p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx"><%= lang.Admin_EmployeeDirectory_Title %></a>
                <a class="CommandButton" href="OrganizationUnitEdit.aspx"><%= lang.Admin_EmployeeDirectory_LinkNewOrganizationUnit %></a>
                <a class="CommandButton" href="UserEmployeeBindingEdit.aspx"><%= lang.Admin_EmployeeDirectory_LinkBindUserEmployee %></a>
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
                <h2 class="Head portal-section-title"><%= lang.Admin_EmployeeEdit_SectionEmployeeProfile %></h2>
            </div>
            <%--
              <lang>
                <zh-CN>员工资料控件的 MaxLength 只是输入上限；员工代码唯一性、邮箱格式、组织/状态组合、日期关系和来源系统策略仍由服务端校验。</zh-CN>
                <en>MaxLength on employee fields is only an input ceiling; the server still validates code uniqueness, email format, organization/status combinations, date relationships, and source-system policy.</en>
              </lang>
            --%>
            <div class="portal-form-grid">
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeDirectory_ColumnEmployeeCode %></span>
                    <asp:TextBox ID="EmployeeCodeTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="64" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_LabelDisplayName %></span>
                    <asp:TextBox ID="DisplayNameTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="150" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeEdit_LabelPreferredName %></span>
                    <asp:TextBox ID="PreferredNameTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="100" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeDirectory_ColumnWorkEmail %></span>
                    <asp:TextBox ID="WorkEmailTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="256" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeDirectory_ColumnOrganization %></span>
                    <asp:DropDownList ID="OrganizationUnitList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_SystemHealth_ColumnStatus %></span>
                    <asp:DropDownList ID="EmploymentStatusList" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeEdit_LabelJoinedUtc %></span>
                    <asp:TextBox ID="JoinedUtcTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="25" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeEdit_LabelLeftUtc %></span>
                    <asp:TextBox ID="LeftUtcTextBox" CssClass="NormalTextBox portal-form-input" MaxLength="25" runat="server" />
                </div>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_EmployeeEdit_LabelSourceSystem %></span>
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
                    Text="<%$ Resources:lang,Admin_EmployeeEdit_ButtonSave %>"
                    CausesValidation="False"
                    OnClick="SaveButton_Click"
                    runat="server" />
                <a class="CommandButton" href="EmployeeDirectory.aspx"><%= lang.Admin_ModuleDefinitions_ButtonCancel %></a>
            </div>
        </div>
    </div>
</asp:Content>
