<%@ Control Inherits="ASPNET.StarterKit.Portal.Users" CodeBehind="Users.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Import Namespace="Resources" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>
<%-- 
    本页代码创建了一个简单的用户管理界面，其中包含了显示用户列表、编辑用户、删除用户和添加新用户的控件。
--%>

<%-- 使用自定义标签创建标题 --%>
<ASPNETPortal:title runat="server" id="Title1" />

<%-- 表格布局 --%>
<table cellpadding="2" cellspacing="0" border="0">
    <%-- 第一行 --%>
    <tr valign="top">
        <%-- 左侧空白列 --%>
        <td width="100">
            &nbsp;
        </td>
        <%-- 右侧列 --%>
        <td class="Normal">
            <%-- 显示消息的文本 --%>
            <asp:Literal id="Message" runat="server" />
            <br><br>
        </td>
    </tr>
    <%-- 第二行 --%>
    <tr valign="top">
        <%-- 左侧空白列 --%>
        <td>
            &nbsp;
        </td>
        <%-- 右侧列 --%>
        <td class="Normal">
            <%-- 显示注册用户的提示文本 --%>
            <%=lang.Admin_Users_RegisteredUsers%>&nbsp;
            <%-- 下拉列表显示所有用户 --%>
            <asp:DropDownList id="ddl_AllUsers" DataTextField="Email" DataValueField="UserID" runat="server" />
            &nbsp;
            <%-- 编辑用户按钮 --%>
            <asp:ImageButton ImageUrl="~/images/edit.gif" CommandName="edit" AlternateText="<%$ Resources:lang,Admin_User_EditUser %>" runat="server" ID="btn_EditUser" OnClick="EditUser_Click" />
            <%-- 删除用户按钮 --%>
            <asp:ImageButton ImageUrl="~/images/delete.gif" AlternateText="<%$ Resources:lang,Admin_User_DelUser %>" runat="server" ID="btn_DeleteUser" onclick="btn_DeleteUser_Click" />
            &nbsp;
            <%-- 添加新用户按钮 --%>
            <asp:LinkButton id="btn_AddUser" cssclass="CommandButton" CommandName="Add" Text="<%$ Resources:lang,Admin_User_AddUser %>" runat="server" OnClick="AddUser_Click" />
        </td>
    </tr>
</table>