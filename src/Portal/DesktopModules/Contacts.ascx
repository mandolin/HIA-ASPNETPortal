<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Contacts" CodeBehind="Contacts.ascx.cs" AutoEventWireup="True" %>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title EditText="Add New Contact" EditUrl="~/DesktopModules/EditContacts.aspx" runat="server" id=Title1 />
<asp:datagrid id="myDataGrid" Border="0" width="100%" AutoGenerateColumns="false" EnableViewState="false" runat="server">
    <Columns>
        <asp:TemplateColumn>
            <ItemTemplate>
                <asp:HyperLink ImageUrl="~/images/edit.gif" NavigateUrl='<%# "~/DesktopModules/EditContacts.aspx?ItemID=" +
                              DataBinder.Eval(Container.DataItem, "ItemID") + "&mid=" + ModuleId %>' Visible="<%# IsEditable %>" runat="server" />
            </ItemTemplate>
        </asp:TemplateColumn>
        <asp:BoundColumn HeaderText="Name" DataField="Name" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
        <asp:BoundColumn HeaderText="Role" DataField="Role" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
        <asp:HyperLinkColumn HeaderText="Email" DataTextField="Email" DataNavigateUrlField="Email" DataNavigateUrlFormatString="mailto:{0}" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
        <asp:BoundColumn HeaderText="Contact 1" DataField="Contact1" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
        <asp:BoundColumn HeaderText="Contact 2" DataField="Contact2" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
    </Columns>
</asp:datagrid>