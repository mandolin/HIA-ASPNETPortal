<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Contacts" CodeBehind="Contacts.ascx.cs" AutoEventWireup="True" %>

<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<ASPNETPortal:title EditText="Add New Contact" EditUrl="~/DesktopModules/EditContacts.aspx" runat="server" id="Title1" />

<%--
    <lang>
        <zh-CN>联系人仍按数据表格呈现，外层使用统一门户表格容器承载主题滚动和边框。</zh-CN>
        <en>Contacts still render as a data table, with the shared portal table wrapper providing themed scrolling and borders.</en>
    </lang>
--%>
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
                    <%--
                        <lang>
                            <zh-CN>编辑链接只在当前用户具备模块编辑权限时显示。</zh-CN>
                            <en>The edit link is shown only when the current user has module edit permission.</en>
                        </lang>
                    --%>
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
