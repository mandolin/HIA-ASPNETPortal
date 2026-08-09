<%@ Control language="c#" Inherits="ASPNET.StarterKit.Portal.QuickLinks" CodeBehind="QuickLinks.ascx.cs" AutoEventWireup="True" %>
<%--
    <lang>
        <zh-CN>快捷链接模块以紧凑链接组呈现，保留新增入口和原有数据绑定。</zh-CN>
        <en>The quick-links module renders as a compact link group while preserving the add entry and existing data binding.</en>
    </lang>
--%>
<div class="portal-quicklinks">
    <%--
        <lang>
            <zh-CN>P8.3 让快捷链接自有头部接入统一模块标题/动作区契约，避免与外层模块标题重复。</zh-CN>
            <en>P8.3 connects the quick-links local header to the shared module title/action contract without duplicating the outer module title.</en>
        </lang>
    --%>
    <div class="portal-module-header portal-quicklinks-header">
        <div class="portal-module-title-wrap">
            <span class="SubSubHead portal-module-title portal-quicklinks-title">Quick Launch</span>
        </div>
        <%--
            <lang>
                <zh-CN>动作区是否可见由服务器角色和模块编辑上下文决定；隐藏或显示按钮都不替代后端授权检查。</zh-CN>
                <en>The server decides action-area visibility from roles and module-edit context; hiding or showing a button does not replace backend authorization.</en>
            </lang>
        --%>
        <asp:Panel id="QuickLinkActions" cssclass="portal-module-actions" EnableViewState="false" runat="server">
            <asp:hyperlink id="EditButton" cssclass="CommandButton portal-module-action portal-secondary-action portal-content-edit-action" enableviewstate="false" runat="server" />
        </asp:Panel>
    </div>
<%--
    <lang>
        <zh-CN>链接列表由当前模块的数据服务绑定且关闭 ViewState；标题使用编码绑定，地址必须经过服务器导航策略后才能进入浏览链接。</zh-CN>
        <en>The current module's data service binds the link list with ViewState disabled; titles use encoded binding, and addresses must pass the server navigation policy before becoming browse links.</en>
    </lang>
--%>
<asp:datalist id="myDataList" CssClass="portal-content-link-list portal-quicklinks-list" RepeatLayout="Flow" enableviewstate="false" runat="server">
    <itemtemplate>
        <div class="portal-content-link-row">
            <%--
                <lang>
                    <zh-CN>编辑链接仅在当前模块可编辑时生成编辑地址；无效或不允许的旧地址回退为编码文本，不产生外部导航副作用。</zh-CN>
                    <en>The edit link gets an edit address only when the current module is editable; invalid or disallowed legacy addresses fall back to encoded text without external-navigation side effects.</en>
                </lang>
            --%>
            <asp:hyperlink id="editLink" CssClass="CommandButton portal-content-edit-action" Text="Edit"
                navigateurl='<%# ChooseUrl(DataBinder.Eval(Container.DataItem, "ItemID"), DataBinder.Eval(Container.DataItem, "Url")) %>'
                visible='<%# IsEditable %>' runat="server" />
            <span class="Normal portal-content-link-main">
                <asp:HyperLink ID="quickLink" CssClass="portal-content-link-title"
                    Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>'
                    NavigateUrl='<%# GetSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' Target="_blank"
                    Visible='<%# HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
                <asp:Label ID="quickLinkText" CssClass="portal-content-link-title portal-disabled-text"
                    Text='<%#: DataBinder.Eval(Container.DataItem, "Title") %>'
                    Visible='<%# !HasSafeBrowseUrl(DataBinder.Eval(Container.DataItem, "Url")) %>' runat="server" />
            </span>
        </div>
    </itemtemplate>
</asp:datalist>
</div>
