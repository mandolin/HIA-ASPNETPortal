<%@ Control Language="c#" Inherits="ASPNET.StarterKit.Portal.Discussion" CodeBehind="Discussion.ascx.cs" AutoEventWireup="True" %>
<%@ Import Namespace="ASPNET.StarterKit.Portal" %> <%-- 导入命名空间 --%>
<%@ Register TagPrefix="ASPNETPortal" TagName="Title" Src="~/DesktopModuleTitle.ascx" %> <%-- 注册自定义标签 --%>

<%-- 标题控件 --%>
<ASPNETPortal:title id="Title1" runat="server" EditTarget="_new" EditUrl="~/DesktopModules/DiscussDetails.aspx" EditText="Add New Thread"></ASPNETPortal:title>

<%-- 中文：讨论列表仍使用 DataList 和原展开命令，只把输出改为主题化线程列表。English: The discussion list keeps DataList and the original expand command while rendering a themed thread list. --%>
<asp:datalist id="TopLevelList" CssClass="portal-discussion-list" RepeatLayout="Flow" OnItemCommand="TopLevelList_OnItemCommand" runat="server" DataKeyField="Parent">
    <ItemTemplate>
        <div class="portal-discussion-row">
            <%-- 中文 / English: P8.2 用文字状态按钮替代旧 plus/node GIF，保留原展开命令。 --%>
            <asp:LinkButton id="btnSelect"
                            CssClass='<%# NodeToggleCssClass((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>'
                            Text='<%# NodeToggleText((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>'
                            Enabled='<%# HasChildMessages((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>'
                            ToolTip="Expand this thread"
                            CommandName='<%# NodeCommandName((int) DataBinder.Eval(Container.DataItem, "ChildCount")) %>'
                            runat="server" />
            <span class="portal-discussion-main">
                <%-- 超链接显示讨论主题 --%>
                <asp:hyperlink CssClass="portal-discussion-title" Text='<%# EncodeDisplayText(DataBinder.Eval(Container.DataItem, "Title")) %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_blank" runat="server" ID="Hyperlink1" />
                <span class="portal-discussion-meta">
                    from <%# EncodeDisplayText(DataBinder.Eval(Container.DataItem, "CreatedByUser")) %>
                    , posted <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
                </span>
            </span>
        </div>
    </ItemTemplate>
    
    <SelectedItemTemplate>
        <div class="portal-discussion-row portal-discussion-row-selected">
            <%-- 中文 / English: P8.2 用文字状态按钮替代旧 minus GIF，保留原折叠命令。 --%>
            <asp:LinkButton id="btnCollapse" CssClass="CommandButton portal-discussion-toggle portal-secondary-action portal-discussion-toggle-selected"
                Text="Collapse" ToolTip="Collapse this thread" runat="server" CommandName="collapse" />
            <span class="portal-discussion-main">
                <%-- 超链接显示讨论主题 --%>
                <asp:hyperlink CssClass="portal-discussion-title" Text='<%# EncodeDisplayText(DataBinder.Eval(Container.DataItem, "Title")) %>' NavigateUrl='<%# FormatUrl((int) DataBinder.Eval(Container.DataItem, "ItemID")) %>' Target="_blank" runat="server" ID="Hyperlink2" />
                <span class="portal-discussion-meta">
                    from <%# EncodeDisplayText(DataBinder.Eval(Container.DataItem, "CreatedByUser")) %>
                    , posted <%# DataBinder.Eval(Container.DataItem, "CreatedDate", "{0:g}") %>
                </span>
            </span>
        </div>

        <%-- 子级讨论列表（回复列表）--%>
        <asp:DataList ID="DetailList" CssClass="portal-discussion-children" RepeatLayout="Flow" DataSource='<%# GetThreadMessages((string) DataBinder.Eval(Container.DataItem, "DisplayOrder")) %>' runat="server">
            <ItemTemplate>
                <div class="portal-discussion-row portal-discussion-reply">
                    <%-- 缩进 --%>
                    <%# GetIndentHtml(Eval("DisplayOrder")) %>
                    <span class="portal-discussion-main">
                        <%-- 标题超链接 --%>
                        <asp:HyperLink
                            CssClass="portal-discussion-title"
                            Text='<%# EncodeDisplayText(Eval("Title")) %>'
                            NavigateUrl='<%# FormatUrl((int)Eval("ItemID")) %>'
                            Target="_blank"
                            runat="server" />
                        <span class="portal-discussion-meta">
                            from <%# EncodeDisplayText(Eval("CreatedByUser")) %>
                            , posted <%# FormatDate(Eval("CreatedDate")) %>
                        </span>
                    </span>
                </div>
            </ItemTemplate>
        </asp:DataList>
    </SelectedItemTemplate>
</asp:datalist>
