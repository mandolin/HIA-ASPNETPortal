<%@ Page Language="c#" CodeBehind="ManageUsers.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.ManageUsers"
    MasterPageFile="~/Default.master" %>
<%@ Import Namespace="Resources" %>

<%--
  <lang>
    <zh-CN>`ManageUsers.aspx` 用于创建、审核和维护门户账号，并作为角色成员关系维护的入口。</zh-CN>
    <en>`ManageUsers.aspx` creates, reviews, and maintains portal accounts and also acts as the role-membership maintenance entry.</en>
  </lang>
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <%--
      <lang>
        <zh-CN>管理用户页只重排展示结构，账号更新、注册审核、生命周期和角色命令仍由 code-behind 控制。</zh-CN>
        <en>The manage-user page only rearranges presentation structure; account updates, registration review, lifecycle commands, and role commands remain controlled by code-behind.</en>
      </lang>
    --%>
    <div class="portal-admin-page portal-admin-manage-users">
        <div class="portal-admin-header">
            <div class="portal-admin-heading">
                <%--
                  <lang>
                    <zh-CN>标题使用服务器端 Label，避免包含代码块的 HtmlControl 在运行时修改文本时报错。</zh-CN>
                    <en>The title uses a server-side Label to avoid runtime text-update errors on an HtmlControl that contains code blocks.</en>
                  </lang>
                --%>
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
            <%--
              <lang>
                <zh-CN>P2.3 注册审核状态区域只展示当前状态，并提供最小批准/拒绝动作。</zh-CN>
                <en>The P2.3 registration-review status area displays the current state and exposes only minimal approve/reject actions.</en>
              </lang>
            --%>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Registration Status</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="RegistrationStatus" runat="server" />
                </div>
                <div class="portal-inline-actions">
                    <asp:LinkButton ID="ApproveRegistrationBtn" CssClass="CommandButton portal-primary-action" Text="Approve Registration"
                        CausesValidation="False" Visible="False" runat="server" OnClick="ApproveRegistration_Click" />
                    <asp:LinkButton ID="RejectRegistrationBtn" CssClass="CommandButton portal-danger-action" Text="Reject Registration"
                        CausesValidation="False" Visible="False" runat="server" OnClick="RejectRegistration_Click" />
                </div>
            </div>
            <%--
              <lang>
                <zh-CN>员工绑定卡片只提供进入单绑定维护页的导航；绑定目标、版本刷新和审计动作不在此标记层执行。</zh-CN>
                <en>The employee-binding card only navigates to the single-binding maintenance page; binding targets, security-version refresh, and audit actions are not executed by this markup.</en>
              </lang>
            --%>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Employee Binding</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="EmployeeBindingText" runat="server" />
                </div>
                <div class="portal-inline-actions">
                    <asp:HyperLink ID="EmployeeBindingLink" CssClass="CommandButton" Text="Manage Binding" runat="server" />
                </div>
            </div>
            <%--
              <lang>
                <zh-CN>账号状态卡片仅在服务端授权后显示禁用/恢复按钮；禁用动作额外保留浏览器确认，但确认不替代服务端授权。</zh-CN>
                <en>The account-status card shows disable/restore buttons only after server authorization; the disable action keeps a browser confirmation, which never replaces server authorization.</en>
              </lang>
            --%>
            <div class="portal-admin-summary-item">
                <div class="SubHead portal-summary-label">Profile Status</div>
                <div class="Normal portal-summary-value">
                    <asp:Label ID="ProfileStatusText" runat="server" />
                </div>
                <div class="portal-inline-actions">
                    <asp:LinkButton ID="DisableUserBtn" CssClass="CommandButton portal-danger-action" Text="禁用账号"
                        CausesValidation="False" Visible="False" runat="server" OnClick="DisableUser_Click"
                        OnClientClick="return confirm('确认禁用此账号？');" />
                    <asp:LinkButton ID="RestoreUserBtn" CssClass="CommandButton portal-primary-action" Text="恢复启用"
                        CausesValidation="False" Visible="False" runat="server" OnClick="RestoreUser_Click" />
                </div>
            </div>
        </div>

        <asp:Label ID="RegistrationMessage" CssClass="NormalRed portal-status-line" runat="server" />

        <%--
          <lang>
            <zh-CN>注册元数据区只读展示邀请、员工和时间信息；这些值由后台加载，不作为账号更新表单的可编辑输入。</zh-CN>
            <en>The registration-metadata section renders invitation, employee, and timestamp information read-only; code-behind loads these values and the markup does not treat them as editable account-update inputs.</en>
          </lang>
        --%>
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
                <%--
                  <lang>
                    <zh-CN>旧账号名继续只读展示，用于兼容历史 URL、角色和审计引用。</zh-CN>
                    <en>The legacy user name remains read-only for compatibility with historical URLs, roles, and audit references.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">旧账号名</span>
                    <span class="Normal portal-field-value"><asp:Label ID="LegacyUserNameText" runat="server" /></span>
                </div>
                <%--
                  <lang>
                    <zh-CN>P6.2 新登录名字段由后台校验唯一性和格式。</zh-CN>
                    <en>The P6.2 login-name field has uniqueness and format validation in code-behind.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">登录名</span>
                    <asp:TextBox ID="LoginName" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%--
                  <lang>
                    <zh-CN>P6.2 显示名用于后台列表和低敏展示，不作为登录凭据。</zh-CN>
                    <en>The P6.2 display name is used by Admin lists and low-sensitivity display surfaces, not as a login credential.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">显示名</span>
                    <asp:TextBox ID="DisplayName" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%--
                  <lang>
                    <zh-CN>P6.2 昵称是可选展示资料，保存策略由 code-behind 统一处理。</zh-CN>
                    <en>The P6.2 nickname is optional display profile data and its save policy is handled centrally by code-behind.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label">昵称</span>
                    <asp:TextBox ID="Nickname" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%--
                  <lang>
                    <zh-CN>邮箱字段可作为联系信息或登录标识输入之一，最终解析仍由认证服务控制。</zh-CN>
                    <en>The email field may serve as contact information or one login identifier input; final resolution remains controlled by the authentication service.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_Email %></span>
                    <asp:TextBox ID="Email" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" />
                </div>
                <%--
                  <lang>
                    <zh-CN>密码输入配合隐藏加密字段使用；服务端仍会重新校验明文/密文边界和策略。</zh-CN>
                    <en>The password input works with the hidden encrypted field; the server still revalidates plaintext/ciphertext boundaries and policy.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_Password %></span>
                    <asp:TextBox ID="Password" Width="200" CssClass="NormalTextBox portal-form-input" runat="server" TextMode="Password" />
                    <asp:HiddenField ID="EncryptedPassword" runat="server" />
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*"
                        ControlToValidate="Password" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:RequiredFieldValidator>
                </div>
                <%--
                  <lang>
                    <zh-CN>确认密码字段用于前后台一致性校验，并同样配合隐藏加密字段提交。</zh-CN>
                    <en>The confirm-password field supports client/server consistency validation and is also submitted with a hidden encrypted field.</en>
                  </lang>
                --%>
                <div class="portal-form-field">
                    <span class="SubHead portal-form-label"><%= lang.Admin_ManageUsers_ConfirmPwd %></span>
                    <asp:TextBox ID="ConfirmPassword" Width="200" CssClass="NormalTextBox portal-form-input" runat="server"
                        TextMode="Password" />
                    <asp:HiddenField ID="EncryptedConfirmPassword" runat="server" />
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ErrorMessage="*"
                        ControlToValidate="ConfirmPassword" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:RequiredFieldValidator>
                    <asp:CompareValidator ID="CompareValidator1" runat="server" ErrorMessage="*" ControlToValidate="ConfirmPassword"
                        ControlToCompare="Password" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:CompareValidator>
                </div>
            </div>
            <div class="portal-form-actions">
                <asp:LinkButton Text="<%$ Resources:lang,Admin_ManageUsers_ApplyNamePwdChange %>"
                    CssClass="CommandButton portal-primary-action" runat="server" ID="UpdateUserBtn" OnClick="UpdateUser_Click" />
            </div>
        </div>

        <%--
          <lang>
            <zh-CN>角色成员区将可选角色和当前成员作为回发命令载体；角色身份、去重、授权和审计仍由 code-behind 在写入前重新判断。</zh-CN>
            <en>The role-membership section carries selected roles and current members through postback commands; code-behind rechecks role identity, duplication, authorization, and audit requirements before writing.</en>
          </lang>
        --%>
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
                        <asp:LinkButton ID="addExisting" CssClass="CommandButton portal-primary-action" Text="<%$ Resources:lang,Admin_ManageUsers_AddUserToRole %>"
                            runat="server" CausesValidation="False" OnClick="AddRole_Click" />
                    </div>
                </div>
            </div>
            <div class="portal-chip-list-wrap">
                <asp:DataList ID="userRoles" CssClass="portal-chip-list" RepeatColumns="2" DataKeyField="RoleId" OnItemCommand="UserRoles_ItemCommand" runat="server">
                    <ItemStyle Width="225" CssClass="portal-chip-item" />
                    <ItemTemplate>
                        <asp:LinkButton ID="Imagebutton1" CommandName="delete" Text="<%$ Resources:lang,Admin_ManageUsers_RemoveFromRoleText %>"
                            ToolTip="<%$ Resources:lang,Admin_ManageUsers_RemoveFromRoleAlt %>" CssClass="CommandButton portal-chip-delete portal-danger-action"
                            CausesValidation="False" runat="server" />
                        <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "RoleName") %>' CssClass="Normal portal-chip-text"
                            runat="server" ID="Label1" />
                    </ItemTemplate>
                </asp:DataList>
            </div>
        </div>

        <div class="portal-form-actions">
            <asp:LinkButton ID="saveBtn" CssClass="CommandButton portal-primary-action" Text="<%$ Resources:lang,Admin_ManageUsers_SaveUserChange %>"
                runat="server" CausesValidation="False" OnClick="Save_Click" />
        </div>
    </div>
</asp:Content>
