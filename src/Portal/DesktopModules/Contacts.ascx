<%-- 控制声明 --%>
<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Contacts" CodeBehind="Contacts.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %>

<%-- 使用自定义标题控件 --%>
<ASPNETPortal:title EditText="Add New Contact" EditUrl="~/DesktopModules/EditContacts.aspx" runat="server" id="Title1" />

<%-- 数据网格控件 --%>
<asp:DataGrid id="myDataGrid" Border="0" width="100%" AutoGenerateColumns="false" EnableViewState="false" runat="server">
    <%-- 数据网格的列定义 --%>
    <Columns>
        <%-- 自定义模板列用于显示编辑链接 --%>
        <asp:TemplateColumn>
            <ItemTemplate>
                <%-- 编辑链接 --%>
                <asp:HyperLink ImageUrl="~/images/edit.gif"
                    NavigateUrl='<%# "~/DesktopModules/EditContacts.aspx?ItemID=" + 
                                 DataBinder.Eval(Container.DataItem, "ItemID").ToString() + 
                                 "&mid=" + ModuleId %>'
                    Visible='<%# IsEditable %>' runat="server" />
            </ItemTemplate>
        </asp:TemplateColumn>
        
        <%-- 绑定列：显示联系人姓名 --%>
        <asp:BoundColumn HeaderText="Name" DataField="Name" ItemStyle-CssClass="Normal" HeaderStyle-CssClass="NormalBold" />
        
        <%-- 绑定列：显示联系人角色 --%>
        <asp:BoundColumn HeaderText="Role" DataField="Role" ItemStyle-CssClass="Normal" HeaderStyle-CssClass="NormalBold" />
        
        <%-- 超链接列：显示联系人电子邮件 --%>
        <asp:HyperLinkColumn HeaderText="Email" DataTextField="Email" DataNavigateUrlField="Email" 
                             DataNavigateUrlFormatString="mailto:{0}" ItemStyle-CssClass="Normal" HeaderStyle-CssClass="NormalBold" />
        
        <%-- 绑定列：显示联系人联系方式1 --%>
        <asp:BoundColumn HeaderText="Contact 1" DataField="Contact1" ItemStyle-CssClass="Normal" HeaderStyle-CssClass="NormalBold" />
        
        <%-- 绑定列：显示联系人联系方式2 --%>
        <asp:BoundColumn HeaderText="Contact 2" DataField="Contact2" ItemStyle-CssClass="Normal" HeaderStyle-CssClass="NormalBold" />
    </Columns>
</asp:DataGrid>