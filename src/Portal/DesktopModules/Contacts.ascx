<%-- 控制声明 --%>
<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Contacts" CodeBehind="Contacts.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<%-- 使用自定义标题控件 --%>
<ASPNETPortal:title EditText="Add New Contact" EditUrl="~/DesktopModules/EditContacts.aspx" runat="server" id="Title1" />

<%-- 中文 / English: 联系人仍是数据表格，但使用统一门户表格容器承载。 --%>
<div class="portal-content-table-wrap">
<asp:Repeater ID="myDataGrid" EnableViewState="false" runat="server">
    <HeaderTemplate>
        <table class="portal-data-table portal-content-table" cellspacing="0" cellpadding="0" border="0" width="100%">
            <tr>
                <th></th>
                <th>Name</th>
                <th>Role</th>
                <th>Email</th>
                <th>Contact 1</th>
                <th>Contact 2</th>
            </tr>
    </HeaderTemplate>
    <ItemTemplate>
            <tr>
                <td class="portal-content-action-cell">
                    <%-- 编辑链接只在当前用户具备模块编辑权限时显示。 --%>
                    <asp:HyperLink
                        ID="editLink"
                        CssClass="CommandButton portal-content-edit-action"
                        Text="Edit"
                        NavigateUrl='<%# "~/DesktopModules/EditContacts.aspx?ItemID=" + DataBinder.Eval(Container.DataItem, "ItemID") + "&mid=" + ModuleId %>'
                        Visible='<%# IsEditable %>'
                        runat="server" />
                </td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Name") %></td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Role") %></td>
                <td class="Normal">
                    <asp:HyperLink
                        ID="emailLink"
                        Text='<%#: DataBinder.Eval(Container.DataItem, "Email") %>'
                        NavigateUrl='<%# GetMailToUrl(DataBinder.Eval(Container.DataItem, "Email")) %>'
                        Visible='<%# HasEmail(DataBinder.Eval(Container.DataItem, "Email")) %>'
                        runat="server" />
                    <asp:Label
                        ID="emailText"
                        Text='<%#: DataBinder.Eval(Container.DataItem, "Email") %>'
                        Visible='<%# !HasEmail(DataBinder.Eval(Container.DataItem, "Email")) %>'
                        runat="server" />
                </td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Contact1") %></td>
                <td class="Normal"><%#: DataBinder.Eval(Container.DataItem, "Contact2") %></td>
            </tr>
    </ItemTemplate>
    <FooterTemplate>
        </table>
    </FooterTemplate>
</asp:Repeater>
</div>
