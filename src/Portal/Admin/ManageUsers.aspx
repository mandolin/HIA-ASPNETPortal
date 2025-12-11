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
                            <%-- 显示标题 --%>
                            <span id="title" class="Head" runat="server">
                                <%= lang.Admin_ManageUsers_ManageUser %></span>
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
                    ControlToValidate="Password" CssClass="NormalRed" Display="Dynamic"></asp:RequiredFieldValidator>
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
                    ControlToValidate="ConfirmPassword" CssClass="NormalRed" Display="Dynamic"></asp:RequiredFieldValidator>
                <asp:CompareValidator ID="CompareValidator1" runat="server" ErrorMessage="*" ControlToValidate="ConfirmPassword"
                    ControlToCompare="Password" CssClass="NormalRed" Display="Dynamic"></asp:CompareValidator>
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
                <asp:DataList ID="userRoles" RepeatColumns="2" DataKeyField="RoleId" runat="server">
                    <ItemStyle Width="225" />
                    <ItemTemplate>
                        &nbsp;&nbsp;
                        <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" AlternateText="<%$ Resources:lang,Admin_ManageUsers_RemoveFromRoleAlt %>"
                            runat="server" ID="Imagebutton1" />
                        <asp:Label Text='<%# DataBinder.Eval(Container.DataItem, "RoleName") %>' CssClass="Normal"
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
