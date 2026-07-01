<%-- 指定控件的语言、继承关系以及代码隐藏文件 --%>
<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.Document" CodeBehind="Document.ascx.cs" AutoEventWireup="True" %>

<%-- 注册自定义控件，用于显示标题 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx"%>

<%-- 开始定义用户控件的 HTML 输出 --%>
<ASPNETPortal:title EditText="Add New Document" EditUrl="~/DesktopModules/EditDocs.aspx" runat="server" id=Title1 />

<%-- 定义一个 DataGrid 控件用于显示文档列表 --%>
<asp:datagrid ID="myDataGrid" Border="0" width="100%" AutoGenerateColumns="false" EnableViewState="false" runat="server">
    <Columns>
        <%--自定义模板列，用于显示编辑链接--%>
        <asp:TemplateColumn>
            <ItemTemplate>
                <%-- 编辑链接 --%>
                <asp:HyperLink id="editLink" ImageUrl="~/images/edit.gif" NavigateUrl='<%# "~/DesktopModules/EditDocs.aspx?ItemID=" + DataBinder.Eval(Container.DataItem, "ItemID") +
                              "&mid=" + ModuleId %>' Visible="<%# IsEditable %>" runat="server" />
            </ItemTemplate>
        </asp:TemplateColumn>
        
        <%--自定义模板列，用于显示文档标题--%>
        <asp:TemplateColumn HeaderText="Title" HeaderStyle-CssClass="NormalBold">
            <ItemTemplate>
                <%-- 文档标题链接 --%>
                <asp:HyperLink id="docLink" Text='<%# DataBinder.Eval(Container.DataItem, "FileFriendlyName") %>' NavigateUrl='<%# GetBrowsePath(DataBinder.Eval(Container.DataItem, "FileNameUrl").ToString(),
                                            DataBinder.Eval(Container.DataItem, "Size"),
                                            (int) DataBinder.Eval(Container.DataItem, "ItemId")) %>' CssClass="Normal" Target="_new" runat="server" />
            </ItemTemplate>
        </asp:TemplateColumn>
        
        <%--绑定列，用于显示文档的所有者--%>
        <asp:BoundColumn HeaderText="Owner" DataField="CreatedByUser" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
        
        <%--绑定列，用于显示文档的区域--%>
        <asp:BoundColumn HeaderText="Area" DataField="Category" ItemStyle-Wrap="false" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
        
        <%--绑定列，用于显示文档最后更新的时间--%>
        <asp:BoundColumn HeaderText="Last Updated" DataField="CreatedDate" DataFormatString="{0:d}" ItemStyle-CssClass="Normal" HeaderStyle-Cssclass="NormalBold" />
    </Columns>
</asp:datagrid>