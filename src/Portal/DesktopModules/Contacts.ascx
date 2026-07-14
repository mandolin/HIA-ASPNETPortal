<%-- 控制声明 --%>
<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Contacts" CodeBehind="Contacts.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<%-- 使用自定义标题控件 --%>
<ASPNETPortal:title EditText="Add New Contact" EditUrl="~/DesktopModules/EditContacts.aspx" runat="server" id="Title1" />

<%-- 使用 Repeater 输出表格，避开旧 DataGrid 模板列的解析兼容问题。 --%>
<asp:Repeater ID="myDataGrid" EnableViewState="false" runat="server">
    <HeaderTemplate>
        <table border="0" width="100%">
            <tr>
                <td></td>
                <td class="NormalBold">Name</td>
                <td class="NormalBold">Role</td>
                <td class="NormalBold">Email</td>
                <td class="NormalBold">Contact 1</td>
                <td class="NormalBold">Contact 2</td>
            </tr>
    </HeaderTemplate>
    <ItemTemplate>
            <tr>
                <td>
                    <%-- 编辑链接只在当前用户具备模块编辑权限时显示。 --%>
                    <asp:HyperLink
                        ID="editLink"
                        ImageUrl="~/images/edit.gif"
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
