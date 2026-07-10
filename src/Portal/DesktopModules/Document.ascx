<%-- 指定控件的语言、继承关系以及代码隐藏文件 --%>
<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Document" CodeBehind="Document.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件，用于显示标题 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%-- 开始定义用户控件的 HTML 输出 --%>
<ASPNETPortal:title EditText="Add New Document" EditUrl="~/DesktopModules/EditDocs.aspx" runat="server" id=Title1 />

<%-- 使用 Repeater 输出表格，避免旧 DataGrid 模板列在当前运行时触发解析兼容问题。 --%>
<asp:Repeater ID="myDataGrid" EnableViewState="false" runat="server">
    <HeaderTemplate>
        <table border="0" width="100%">
            <tr>
                <td></td>
                <td class="NormalBold">Title</td>
                <td class="NormalBold">Owner</td>
                <td class="NormalBold">Area</td>
                <td class="NormalBold">Last Updated</td>
            </tr>
    </HeaderTemplate>
    <ItemTemplate>
            <tr>
                <td>
                    <%-- 编辑链接只在当前用户具备模块编辑权限时显示。 --%>
                    <asp:HyperLink
                        ID="editLink"
                        ImageUrl="~/images/edit.gif"
                        NavigateUrl='<%# "~/DesktopModules/EditDocs.aspx?ItemID=" + DataBinder.Eval(Container.DataItem, "ItemID") + "&mid=" + ModuleId %>'
                        Visible="<%# IsEditable %>"
                        runat="server" />
                </td>
                <td>
                    <asp:HyperLink
                        ID="docLink"
                        Text='<%# DataBinder.Eval(Container.DataItem, "FileFriendlyName") %>'
                        NavigateUrl='<%# GetBrowsePath(DataBinder.Eval(Container.DataItem, "FileNameUrl").ToString(), DataBinder.Eval(Container.DataItem, "Size"), (int) DataBinder.Eval(Container.DataItem, "ItemId")) %>'
                        CssClass="Normal"
                        Target="_new"
                        runat="server" />
                </td>
                <td class="Normal"><%# DataBinder.Eval(Container.DataItem, "CreatedByUser") %></td>
                <td class="Normal" nowrap="nowrap"><%# DataBinder.Eval(Container.DataItem, "Category") %></td>
                <td class="Normal"><%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:d}") %></td>
            </tr>
    </ItemTemplate>
    <FooterTemplate>
        </table>
    </FooterTemplate>
</asp:Repeater>
