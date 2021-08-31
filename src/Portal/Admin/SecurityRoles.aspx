<%@ Page Language="c#" CodeBehind="SecurityRoles.aspx.cs" AutoEventWireup="True"
    Inherits="ASPNET.StarterKit.Portal.SecurityRoles" MasterPageFile="~/Default.master" %>

<%--
    The SecurityRoles.aspx page is used to create and edit security roles within
    the Portal application.
--%>
<asp:Content ID="Content1" ContentPlaceHolderID="maincontent" runat="server">
    <table width="98%" cellspacing="0" cellpadding="4" border="0">
        <tr height="*" valign="top">
            <td width="100">
                &nbsp;
            </td>
            <td width="*">
                <table width="450" cellpadding="2" cellspacing="4" border="0">
                    <tr>
                        <td colspan="2">
                            <table width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td align="left">
                                        <span id="title" class="Head" runat="server">Role Membership</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <hr noshade size="1">
                                    </td>
                                </tr>
                            </table>
                            <asp:Label ID="Message" CssClass="NormalRed" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <table width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td>
                                        <asp:TextBox ID="windowsUserName" Text="DOMAIN\username" Visible="False" runat="server" />
                                    </td>
                                    <td class="Normal">
                                        <asp:LinkButton ID="addNew" CssClass="CommandButton" Text="Create new user and add to role"
                                            Visible="False" runat="server" OnClick="AddUser_Click" />
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:DropDownList ID="allUsers" DataTextField="Email" DataValueField="UserID" runat="server" />
                                    </td>
                                    <td>
                                        <asp:LinkButton ID="addExisting" CssClass="CommandButton" Text="Add existing user to role"
                                            runat="server" OnClick="AddUser_Click" />
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr valign="top">
                        <td>
                            &nbsp;
                        </td>
                        <td>
                            <asp:DataList ID="usersInRole" RepeatColumns="2" DataKeyField="UserId" runat="server">
                                <ItemStyle Width="225" />
                                <ItemTemplate>
                                    &nbsp;&nbsp;
                                    <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" AlternateText="Remove this user from role"
                                        runat="server" />
                                    <asp:Label Text='<%# DataBinder.Eval(Container.DataItem, "Email") %>' CssClass="Normal"
                                        runat="server" />
                                </ItemTemplate>
                            </asp:DataList>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <hr noshade size="1">
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <asp:LinkButton ID="saveBtn" class="CommandButton" Text="Save Role Changes" runat="server"
                                OnClick="Save_Click" />
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</asp:Content>
