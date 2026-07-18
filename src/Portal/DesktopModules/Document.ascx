<%-- 指定控件的语言、继承关系以及代码隐藏文件 --%>
<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Document" CodeBehind="Document.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件，用于显示标题 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%-- 开始定义用户控件的 HTML 输出 --%>
<ASPNETPortal:title EditText="Add New Document" EditUrl="~/DesktopModules/EditDocs.aspx" runat="server" id=Title1 />

<%-- 中文 / English: 文档列表仍按数据表格呈现，外层提供主题化滚动与边框。 --%>
<div class="portal-content-table-wrap">
<asp:Repeater ID="myDataGrid" EnableViewState="false" runat="server">
    <HeaderTemplate>
        <table class="portal-data-table portal-content-table" cellspacing="0" cellpadding="0" border="0" width="100%">
            <tr>
                <th></th>
                <th>Title</th>
                <th>Owner</th>
                <th>Area</th>
                <th>Last Updated</th>
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
                        NavigateUrl='<%# "~/DesktopModules/EditDocs.aspx?ItemID=" + DataBinder.Eval(Container.DataItem, "ItemID") + "&mid=" + ModuleId %>'
                        Visible="<%# IsEditable %>"
                        runat="server" />
                </td>
                <td>
                    <asp:HyperLink
                        ID="docLink"
                        Text='<%# EncodeText(DataBinder.Eval(Container.DataItem, "FileFriendlyName")) %>'
                        NavigateUrl='<%# GetBrowsePath(Convert.ToString(DataBinder.Eval(Container.DataItem, "FileNameUrl")), DataBinder.Eval(Container.DataItem, "Size"), (int) DataBinder.Eval(Container.DataItem, "ItemId")) %>'
                        CssClass="Normal"
                        Target="_new"
                        runat="server" />
                </td>
                <td class="Normal"><%# EncodeText(DataBinder.Eval(Container.DataItem, "CreatedByUser")) %></td>
                <td class="Normal" nowrap="nowrap"><%# EncodeText(DataBinder.Eval(Container.DataItem, "Category")) %></td>
                <td class="Normal"><%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:d}") %></td>
            </tr>
    </ItemTemplate>
    <FooterTemplate>
        </table>
    </FooterTemplate>
</asp:Repeater>
</div>
