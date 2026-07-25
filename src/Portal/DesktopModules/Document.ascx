<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Document" CodeBehind="Document.ascx.cs" AutoEventWireup="True" %>

<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title EditText="Add New Document" EditUrl="~/DesktopModules/EditDocs.aspx" runat="server" id=Title1 />

<%--
    <lang>
        <zh-CN>文档列表仍按数据表格呈现，外层提供主题化滚动与边框。</zh-CN>
        <en>The document list still renders as a data table, with the wrapper providing themed scrolling and borders.</en>
    </lang>
--%>
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
