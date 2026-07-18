<%@ Page Language="c#" CodeBehind="ManageUsers.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.ManageUsers"
    MasterPageFile="~/Default.master" %>
<%@ Import Namespace="Resources" %>

<%-- 
    ManageUsers.aspx 页面用于创建和编辑门户应用中的用户。
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%-- 中文 / English: 管理用户页只重排展示结构，账号更新、注册审核、生命周期和角色命令仍由 code-behind 控制。 --%>
    <div class="portal-admin-page portal-admin-manage-users">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <%-- 显示标题。使用 Label 避免包含代码块的 HtmlControl 在运行时修改文本时报错。 --%>
                <asp:Label ID="TitleText" CssClass="Head portal-admin-title" Text="<%$ Resources:lang,Admin_ManageUsers_ManageUser %>"
                    runat="server" />
                <p class="Normal portal-admin-subtitle">Account profile, registration review, lifecycle, and role membership.</p>
            </div>
            <div class="portal-admin-actions">
                <a class="CommandButton" href="EmployeeDirectory.aspx">Employee Directory</a>
                <a class="CommandButton" href="OperationAudits.aspx">Operation Audits</a>
            </div>
        </div>

        <div class="portal-admin-summary-grid portal-user-summary-grid">
            <%-- P2.3 注册审核状态，只展示并提供最小批准动作。 --%>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Registration Status</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="RegistrationStatus" runat="server" />
                </div>
                <div class="portal-inline-actions">
                    <asp:LinkButton ID="ApproveRegistrationBtn" CssClass="CommandButton" Text="Approve Registration"
                        CausesValidation="False" Visible="False" runat="server" OnClick="ApproveRegistration_Click" />
                    <asp:LinkButton ID="RejectRegistrationBtn" CssClass="CommandButton" Text="Reject Registration"
                        CausesValidation="False" Visible="False" runat="server" OnClick="RejectRegistration_Click" />
                </div>
            </div>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Employee Binding</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="EmployeeBindingText" runat="server" />
                </div>
                <div class="portal-inline-actions">
                    <asp:HyperLink ID="EmployeeBindingLink" CssClass="CommandButton" Text="Manage Binding" runat="server" />
                </div>
            </div>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Profile Status</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="ProfileStatusText" runat="server" />
                </div>
                <div class="portal-inline-actions">
                    <asp:LinkButton ID="DisableUserBtn" CssClass="CommandButton" Text="禁用账号"
                        CausesValidation="False" Visible="False" runat="server" OnClick="DisableUser_Click"
                        OnClientClick="return confirm('确认禁用此账号？');" />
                    <asp:LinkButton ID="RestoreUserBtn" CssClass="CommandButton" Text="恢复启用"
                        CausesValidation="False" Visible="False" runat="server" OnClick="RestoreUser_Click" />
                </div>
            </div>
        </div>

        <asp:Label ID="RegistrationMessage" CssClass="NormalRed portal-status-line" runat="server" />

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Registration Metadata</h2>
            </div>
            <div class="portal-field-grid">
                <div class="portal-field">
                    <span class="SubHead portal-field-label">Registration Source</span>
                    <span class="Normal portal-field-value"><asp:Label ID="RegistrationSource" runat="server" /></span>
                </div>
                <div class="portal-field">
                    <span class="SubHead portal-field-label">Employee Code</span>
                    <span class="Normal portal-field-value"><asp:Label ID="EmployeeCodeText" runat="server" /></span>
                </div>
                <div class="portal-field">
                    <span class="SubHead portal-field-label">Invite Code</span>
                    <span class="Normal portal-field-value"><asp:Label ID="InviteCodeText" runat="server" /></span>
                </div>
                <div class="portal-field">
                    <span class="SubHead portal-field-label">Registered UTC</span>
                    <span class="Normal portal-field-value"><asp:Label ID="RegisteredUtcText" runat="server" /></span>
                </div>
                <div class="portal-field">
                    <span class="SubHead portal-field-label">Approved UTC</span>
                    <span class="Normal portal-field-value"><asp:Label ID="ApprovedUtcText" runat="server" /></span>
                </div>
                <div class="portal-field">
                    <span class="SubHead portal-field-label">Profile Source</span>
                    <span class="Normal portal-field-value"><asp:Label ID="ProfileSourceText" runat="server" /></span>
                </div>
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Account Profile</h2>
            </div>
            <div class="portal-form-grid">
                <%-- 旧账号名继续只读展示，用于兼容历史 URL、角色和审计引用。 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">旧账号名</span>
                    <span class="Normal portal-field-value"><asp:Label ID="LegacyUserNameText" runat="server" /></span>
                </div>
                <%-- P6.2 新登录名。 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">登录名</span>
                    <asp:TextBox ID="LoginName" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%-- P6.2 显示名。 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">显示名</span>
                    <asp:TextBox ID="DisplayName" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%-- P6.2 昵称。 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">昵称</span>
                    <asp:TextBox ID="Nickname" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%-- 邮箱 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_Email %></span>
                    <asp:TextBox ID="Email" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%-- 密码 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_Password %></span>
                    <asp:TextBox ID="Password" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" TextMode="Password" />
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*"
                        ControlToValidate="Password" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:RequiredFieldValidator>
                </div>
                <%-- 确认密码 --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_ConfirmPwd %></span>
                    <asp:TextBox ID="ConfirmPassword" Width="200" CssClass="NormalTextBox portal-form-input" runat="server"
                        TextMode="Password" />
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ErrorMessage="*"
                        ControlToValidate="ConfirmPassword" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:RequiredFieldValidator>
                    <asp:CompareValidator ID="CompareValidator1" runat="server" ErrorMessage="*" ControlToValidate="ConfirmPassword"
                        ControlToCompare="Password" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:CompareValidator>
                </div>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton Text="<%$ Resources:lang,Admin_ManageUsers_ApplyNamePwdChange %>"
                    CssClass="CommandButton" runat="server" ID="UpdateUserBtn" OnClick="UpdateUser_Click" />
            </div>
        </div>

        <div class="portal-admin-section">
            <div class="portal-section-header">
                <h2 class="Head portal-section-title">Role Membership</h2>
            </div>
            <div class="portal-filter-panel">
                <div class="portal-filter-grid">
                    <div class="portal-filter-field">
                        <span class="SubHead portal-filter-label">Role</span>
                        <asp:DropDownList ID="allRoles" CssClass="NormalTextBox portal-filter-input" DataTextField="RoleName" DataValueField="RoleID"
                            runat="server" />
                    </div>
                    <div class="portal-filter-actions">
                        <asp:LinkButton ID="addExisting" CssClass="CommandButton" Text="<%$ Resources:lang,Admin_ManageUsers_AddUserToRole %>"
                            runat="server" CausesValidation="False" OnClick="AddRole_Click" />
                    </div>
                </div>
            </div>
            <div class="portal-chip-list-wrap">
                <asp:DataList ID="userRoles" CssClass="portal-chip-list" RepeatColumns="2" DataKeyField="RoleId" OnItemCommand="UserRoles_ItemCommand" runat="server">
                    <ItemStyle Width="225" CssClass="portal-chip-item" />
                    <ItemTemplate>
                        <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" AlternateText="<%$ Resources:lang,Admin_ManageUsers_RemoveFromRoleAlt %>"
                            CssClass="portal-chip-delete" CausesValidation="False" runat="server" ID="Imagebutton1" />
                        <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "RoleName") %>' CssClass="Normal portal-chip-text"
                            runat="server" ID="Label1" />
                    </ItemTemplate>
                </asp:DataList>
            </div>
        </div>

        <div class="portal-form-actions">
            <asp:LinkButton ID="saveBtn" class="CommandButton" Text="<%$ Resources:lang,Admin_ManageUsers_SaveUserChange %>"
                runat="server" CausesValidation="False" OnClick="Save_Click" />
        </div>
    </div>
</asp:Content>
