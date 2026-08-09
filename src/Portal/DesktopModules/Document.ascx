<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Document" CodeBehind="Document.ascx.cs" AutoEventWireup="True" %>

<%--
    <lang>
        <zh-CN>共享模块标题控件承载文档编辑入口和主题化标题；动作是否可见仍由服务器编辑上下文决定。</zh-CN>
        <en>The shared module-title control hosts the document-edit entry and themed title; action visibility remains a server-side edit-context decision.</en>
    </lang>
--%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<ASPNETPortal:title EditText="Add New Document" EditUrl="~/DesktopModules/EditDocs.aspx" runat="server" id=Title1 />

<%--
    <lang>
        <zh-CN>文档列表仍按数据表格呈现，外层提供主题化滚动与边框。</zh-CN>
        <en>The document list still renders as a data table, with the wrapper providing themed scrolling and borders.</en>
    </lang>
--%>
<div class="portal-content-table-wrap">
<%--
    <lang>
        <zh-CN>文档列表由当前模块数据服务绑定并关闭 ViewState；文件名、所有者和分类按安全文本规则输出，不把客户端值当作下载来源。</zh-CN>
        <en>The current module data service binds the document list with ViewState disabled; file name, owner, and category follow safe-text output rules, and client values are not trusted as download sources.</en>
    </lang>
--%>
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
                    <%--
                        <lang>
                            <zh-CN>文档链接地址由服务器根据数据库内容大小选择受控下载入口或校验后的旧链接；原始路径不直接进入页面输出。</zh-CN>
                            <en>The server chooses a controlled download endpoint or a validated legacy link from the database-content size; raw paths do not enter page output directly.</en>
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
