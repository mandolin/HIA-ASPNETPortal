<%@ Control Inherits="ASPNET.StarterKit.Portal.Roles" CodeBehind="Roles.ascx.cs" Language="c#" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%-- 注册标题模块 --%>
<ASPNETPortal:title runat="server" id="Title1" />

<%-- 表格开始 --%>
<table cellpadding="2" cellspacing="0" border="0">
    <%-- 数据列表行 --%>
    <tr valign="top">
        <td class="Normal" width="100">
            &nbsp;
        </td>
        <td>
            <%-- 角色列表 --%>
            <asp:DataList id="rolesList" DataKeyField="RoleID" runat="server">
                <ItemTemplate>
                    <%-- 编辑按钮 --%>
                    <asp:ImageButton ImageUrl="~/images/edit.gif" CommandName="edit" AlternateText="Edit this item" runat="server" />
                    <%-- 删除按钮 --%>
                    <asp:ImageButton ImageUrl="~/images/delete.gif" CommandName="delete" AlternateText="Delete this item" runat="server" />
                    &nbsp;&nbsp;
                    <%-- 角色名标签 --%>
                    <asp:Label Text='<%# DataBinder.Eval(Container.DataItem, "RoleName") %>' cssclass="Normal" runat="server" />
                </ItemTemplate>
                <EditItemTemplate>
                    <%-- 可编辑的角色名输入框 --%>
                    <asp:TextBox id="roleName" width="200" cssclass="NormalTextBox" Text='<%# DataBinder.Eval(Container.DataItem, "RoleName") %>' runat="server" />
                    &nbsp;
                    <%-- 应用更改按钮 --%>
                    <asp:LinkButton Text="Apply" CommandName="apply" cssclass="CommandButton" runat="server" />
                    &nbsp;
                    <%-- 更改角色成员按钮 --%>
                    <asp:LinkButton Text="Change Role Members" CommandName="members" cssclass="CommandButton" runat="server" />
                </EditItemTemplate>
            </asp:DataList>
        </td>
    </tr>
    <%-- 添加新角色行 --%>
    <tr>
        <td>
            &nbsp;
        </td>
        <td>
            <%-- 添加新角色按钮 --%>
            <asp:LinkButton cssclass="CommandButton" Text="Add New Role" runat="server" id="AddRoleBtn" onclick="AddRole_Click">
                Add New Role</asp:LinkButton>
        </td>
    </tr>
</table>
<%-- 表格结束 --%>