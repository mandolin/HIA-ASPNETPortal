<%@ Page Language="c#" CodeBehind="ManageUsers.aspx.cs" AutoEventWireup="True" Inherits="ASPNET.StarterKit.Portal.ManageUsers"
    MasterPageFile="~/Default.master" %>
<%@ Import Namespace="Resources" %>

<%-- 
    ManageUsers.aspx 页面用于创建和编辑门户应用中的用户。
--%>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <table width="450" cellspacing="0" cellpadding="4" border="0">
        <%-- 标题 --%>
        <tr height="*" valign="top">
            <td colspan="2">
                <table width="100%" cellspacing="0" cellpadding="0">
                    <tr>
                        <td align="left">
                            <%-- 显示标题。使用 Label 避免包含代码块的 HtmlControl 在运行时修改文本时报错。 --%>
                            <asp:Label ID="TitleText" CssClass="Head" Text="<%$ Resources:lang,Admin_ManageUsers_ManageUser %>"
                                runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <%-- 分隔线 --%>
                            <hr noshade size="1">
                        </td>
                    </tr>
                </table>
            </td>
        </tr>

        <%-- P2.3 注册审核状态，只展示并提供最小批准动作。 --%>
        <tr>
            <td class="Normal">
                Registration Status:
            </td>
            <td class="Normal">
                <asp:Label ID="RegistrationStatus" runat="server" />
                &nbsp;
                <asp:LinkButton ID="ApproveRegistrationBtn" CssClass="CommandButton" Text="Approve Registration"
                    CausesValidation="False" Visible="False" runat="server" OnClick="ApproveRegistration_Click" />
                &nbsp;
                <asp:LinkButton ID="RejectRegistrationBtn" CssClass="CommandButton" Text="Reject Registration"
                    CausesValidation="False" Visible="False" runat="server" OnClick="RejectRegistration_Click" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Registration Source:
            </td>
            <td class="Normal">
                <asp:Label ID="RegistrationSource" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Employee Code:
            </td>
            <td class="Normal">
                <asp:Label ID="EmployeeCodeText" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Employee Binding:
            </td>
            <td class="Normal">
                <asp:Label ID="EmployeeBindingText" runat="server" />
                &nbsp;
                <asp:HyperLink ID="EmployeeBindingLink" CssClass="CommandButton" Text="Manage Binding" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Invite Code:
            </td>
            <td class="Normal">
                <asp:Label ID="InviteCodeText" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Registered UTC:
            </td>
            <td class="Normal">
                <asp:Label ID="RegisteredUtcText" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Approved UTC:
            </td>
            <td class="Normal">
                <asp:Label ID="ApprovedUtcText" runat="server" />
            </td>
        </tr>
        <tr>
            <td>
                &nbsp;
            </td>
            <td class="Normal">
                <asp:Label ID="RegistrationMessage" CssClass="NormalRed" runat="server" />
            </td>
        </tr>
        
        <%-- 旧账号名继续只读展示，用于兼容历史 URL、角色和审计引用。 --%>
        <tr>
            <td class="Normal">
                旧账号名:
            </td>
            <td class="Normal">
                <asp:Label ID="LegacyUserNameText" runat="server" />
            </td>
        </tr>

        <%-- P6.2 新登录名。 --%>
        <tr>
            <td class="Normal">
                登录名:
            </td>
            <td>
                <asp:TextBox ID="LoginName" Width="200" CssClass="NormalTextBox" runat="server" />
            </td>
        </tr>

        <%-- P6.2 显示名。 --%>
        <tr>
            <td class="Normal">
                显示名:
            </td>
            <td>
                <asp:TextBox ID="DisplayName" Width="200" CssClass="NormalTextBox" runat="server" />
            </td>
        </tr>

        <%-- P6.2 昵称。 --%>
        <tr>
            <td class="Normal">
                昵称:
            </td>
            <td>
                <asp:TextBox ID="Nickname" Width="200" CssClass="NormalTextBox" runat="server" />
            </td>
        </tr>

        <%-- Profile 状态只读展示；禁用/恢复使用下面的显式生命周期动作。 --%>
        <tr>
            <td class="Normal">
                Profile Status:
            </td>
            <td class="Normal">
                <asp:Label ID="ProfileStatusText" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Profile Source:
            </td>
            <td class="Normal">
                <asp:Label ID="ProfileSourceText" runat="server" />
            </td>
        </tr>
        <tr>
            <td class="Normal">
                Lifecycle Action:
            </td>
            <td class="Normal">
                <asp:LinkButton ID="DisableUserBtn" CssClass="CommandButton" Text="禁用账号"
                    CausesValidation="False" Visible="False" runat="server" OnClick="DisableUser_Click"
                    OnClientClick="return confirm('确认禁用此账号？');" />
                &nbsp;
                <asp:LinkButton ID="RestoreUserBtn" CssClass="CommandButton" Text="恢复启用"
                    CausesValidation="False" Visible="False" runat="server" OnClick="RestoreUser_Click" />
            </td>
        </tr>

        <%-- 邮箱 --%>
        <tr>
            <td class="Normal">
                <%= lang.Admin_ManageUsers_Email %>
            </td>
            <td>
                <asp:TextBox ID="Email" Width="200" CssClass="NormalTextBox" runat="server" />
            </td>
        </tr>
        
        <%-- 密码 --%>
        <tr>
            <td class="Normal">
                <%= lang.Admin_ManageUsers_Password %>
            </td>
            <td>
                <asp:TextBox ID="Password" Width="200" CssClass="NormalTextBox" runat="server" TextMode="Password" />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*"
                    ControlToValidate="Password" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:RequiredFieldValidator>
            </td>
        </tr>
        
        <%-- 确认密码 --%>
        <tr>
            <td class="Normal">
                <%= lang.Admin_ManageUsers_ConfirmPwd %>
            </td>
            <td>
                <asp:TextBox ID="ConfirmPassword" Width="200" CssClass="NormalTextBox" runat="server"
                    TextMode="Password" />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ErrorMessage="*"
                    ControlToValidate="ConfirmPassword" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:RequiredFieldValidator>
                <asp:CompareValidator ID="CompareValidator1" runat="server" ErrorMessage="*" ControlToValidate="ConfirmPassword"
                    ControlToCompare="Password" CssClass="NormalRed" Display="Dynamic" Enabled="False"></asp:CompareValidator>
            </td>
        </tr>
        
        <%-- 更新用户按钮 --%>
        <tr>
            <td colspan="3">
                <asp:LinkButton Text="<%$ Resources:lang,Admin_ManageUsers_ApplyNamePwdChange %>"
                    CssClass="CommandButton" runat="server" ID="UpdateUserBtn" OnClick="UpdateUser_Click" />
                <br>
                <br>
            </td>
        </tr>
        
        <%-- 角色添加 --%>
        <tr>
            <td colspan="2">
                <asp:DropDownList ID="allRoles" DataTextField="RoleName" DataValueField="RoleID"
                    runat="server" />
                &nbsp;<asp:LinkButton ID="addExisting" CssClass="CommandButton" Text="<%$ Resources:lang,Admin_ManageUsers_AddUserToRole %>"
                    runat="server" CausesValidation="False" OnClick="AddRole_Click" />
            </td>
        </tr>
        
        <%-- 当前用户的角色列表 --%>
        <tr valign="top">
            <td>
                &nbsp;
            </td>
            <td>
                <asp:DataList ID="userRoles" RepeatColumns="2" DataKeyField="RoleId" OnItemCommand="UserRoles_ItemCommand" runat="server">
                    <ItemStyle Width="225" />
                    <ItemTemplate>
                        &nbsp;&nbsp;
                        <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" AlternateText="<%$ Resources:lang,Admin_ManageUsers_RemoveFromRoleAlt %>"
                            CausesValidation="False" runat="server" ID="Imagebutton1" />
                        <asp:Label Text='<%#: DataBinder.Eval(Container.DataItem, "RoleName") %>' CssClass="Normal"
                            runat="server" ID="Label1" />
                    </ItemTemplate>
                </asp:DataList>
            </td>
        </tr>
        
        <%-- 分隔线 --%>
        <tr>
            <td colspan="2">
                <hr noshade size="1">
            </td>
        </tr>
        
        <%-- 保存更改 --%>
        <tr>
            <td colspan="2">
                <asp:LinkButton ID="saveBtn" class="CommandButton" Text="<%$ Resources:lang,Admin_ManageUsers_SaveUserChange %>"
                    runat="server" CausesValidation="False" OnClick="Save_Click" />
            </td>
        </tr>
    </table>
</asp:Content>
