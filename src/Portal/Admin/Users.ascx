<%@ Control Inherits="ASPNET.StarterKit.Portal.Users" CodeBehind="Users.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Import Namespace="Resources" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title runat="server" id="Title1" />
<table cellpadding="2" cellspacing="0" border="0">
    <tr valign="top">
        <td width="100">
            &nbsp;
        </td>
        <td class="Normal">
            <asp:Literal id="Message" runat="server" />
            <br><br>
        </td>
    </tr>
    <tr valign="top">
        <td>
            &nbsp;
        </td>
        <td class="Normal">
            <%=lang.Admin_Users_RegisteredUsers%>&nbsp;
            <asp:DropDownList id="allUsers" DataTextField="Email" DataValueField="UserID" runat="server" />
            &nbsp;
            <asp:ImageButton ImageUrl="~/images/edit.gif" CommandName="edit" AlternateText="<%$ Resources:lang,Admin_User_EditUser %>" runat="server" ID="EditBtn" />
            <asp:ImageButton ImageUrl="~/images/delete.gif" AlternateText="<%$ Resources:lang,Admin_User_DelUser %>" runat="server" ID="DeleteBtn" onclick="DeleteUser_Click" />
            &nbsp;
            <asp:LinkButton id="addNew" cssclass="CommandButton" CommandName="Add" Text="<%$ Resources:lang,Admin_User_AddUser %>" runat="server" />
        </td>
    </tr>
</table>