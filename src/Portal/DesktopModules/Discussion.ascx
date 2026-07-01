<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Discussion" CodeBehind="Discussion.ascx.cs" AutoEventWireup="True" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %> <%-- 导入命名空间 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %> <%-- 注册自定义标签 --%>

<%-- 标题控件 --%>
<ASPNETPortal:title id="Title1" runat="server" EditTarget="_new" EditUrl="~/DesktopModules/DiscussDetails.aspx" EditText="Add New Thread"></ASPNETPortal:title>

<%-- 讨论列表 --%>
<asp:datalist id="TopLevelList" OnItemCommand="TopLevelList_OnItemCommand" runat="server" DataKeyField="Parent" ItemStyle-Cssclass="Normal" width="98%">
    <ItemTemplate>
        <%-- 图像按钮用于选择 --%>
        <asp:ImageButton id="btnSelect" ImageUrl='<%# NodeImage((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>' CommandName='<%# NodeCommandName((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>' runat="server" />
        
        <%-- 超链接显示讨论主题 --%>
        <asp:hyperlink Text='<%# DataBinder.Eval(Container.DataItem, "Title") %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_new" runat="server" ID="Hyperlink1" />,
        
        <%-- 显示发布者 --%>
        from
        <%# DataBinder.Eval(Container.DataItem, "CreatedByUser") %>
        
        <%-- 显示发布时间 --%>
        , posted
        <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
    </ItemTemplate>
    
    <SelectedItemTemplate>
        <%-- 图像按钮用于折叠 --%>
        <asp:ImageButton id="btnCollapse" ImageUrl="~/images/minus.gif" runat="server" CommandName="collapse" />
        
        <%-- 超链接显示讨论主题 --%>
        <asp:hyperlink Text='<%# DataBinder.Eval(Container.DataItem, "Title") %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_new" runat="server" ID="Hyperlink2" />,
        
        <%-- 显示发布者 --%>
        from
        <%# DataBinder.Eval(Container.DataItem, "CreatedByUser") %>
        
        <%-- 显示发布时间 --%>
        , posted
        <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
        
        <%-- 子级讨论列表（回复列表）--%>
        <asp:DataList ID="DetailList" ItemStyle-CssClass="Normal" DataSource='<%# GetThreadMessages((string) DataBinder.Eval(Container.DataItem, "DisplayOrder")) %>' runat="server">
            <ItemTemplate>
                <%-- 缩进 --%>
                <%# GetIndentHtml(Eval("DisplayOrder")) %>

                <img src="<%= Global.GetApplicationPath(Request) %>/images/1x1.gif" height="15" width="15" />

                <%-- 标题超链接 --%>
                <asp:HyperLink 
                    Text='<%# Eval("Title") %>' 
                    NavigateUrl='<%# FormatUrl((int)Eval("ItemID")) %>' 
                    Target="_blank" 
                    runat="server" />,
        
                from <%# Eval("CreatedByUser") %>

                <%-- 关键：用 FormatDate 方法，完美兼容老项目 --%>
                , posted <%# FormatDate(Eval("CreatedDate")) %>
            </ItemTemplate>
        </asp:DataList>
    </SelectedItemTemplate>
</asp:datalist>